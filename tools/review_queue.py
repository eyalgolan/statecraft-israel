# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Lists the content an AI agent checked that no named person has reviewed yet (CONTENT_POLICY section 2, rule 6), so the
owner can work through it: docs/review-queue.md.

Read-only on content/: it parses the YAML and asks git, and writes only the Markdown page (or prints it).

Two kinds of entry:

1. **Agent-checked Hebrew names**: every `*_review: agent-checked` flag (`name_he_review`, `title_he_review`,
   `title_he_f_review`, `body_he_review`; #564). The flag itself says no person has read the name.
2. **Statuses an agent set**: every row whose `status` is `fact-checked` or `sensitivity-reviewed`, where the line that
   holds that status was last written by a commit that an AI agent co-wrote.

The heuristic for (2), because content has no reviewer field: `docs/HISTORIAN_REVIEW.md` keeps the reviewer's record in
the pull request, not in the YAML (no `reviewed_by` key), so the only signal in the repository is git history. For each
such status line, `git blame` gives the commit that last wrote it. A commit counts as agent-written when its message has
an AI co-author trailer or the Claude Code line ("Co-Authored-By: Claude ...", "Co-Authored-By: ... Codex ...",
"Generated with [Claude Code]", "Generated with Codex"). Such a row is listed with the commit and its pull request
number; a row whose status line came from a commit with no such marker is counted as human-set and not listed. Limits,
said plainly: a person may have reviewed an agent's row and merged it without touching the line (it stays listed, which
errs on the side of review), a person may have set a status without checking (not caught), and a shallow clone blames
every line on its one commit (run it on a full clone; the page names the commit it was generated from).

Usage: uv run tools/review_queue.py [--write]   (prints the page; --write writes docs/review-queue.md)
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
PAGE = ROOT / "docs" / "review-queue.md"
STATUSES = ("fact-checked", "sensitivity-reviewed")
FLAG = "agent-checked"
FLAG_KEY = re.compile(r"^(?P<field>[a-z_]+_he(?:_f)?)_review$")
AI_MARK = re.compile(r"co-authored-by:[^\n]*(claude|codex|copilot|gpt)|generated with \[?(claude code|codex)", re.I)
PR_NUMBER = re.compile(r"\(#(\d+)\)\s*$")


@dataclass
class Commit:
    sha: str
    author: str
    subject: str
    agent: bool

    @property
    def pr(self) -> str:
        m = PR_NUMBER.search(self.subject)
        return f"#{m.group(1)}" if m else ""

    def describe(self) -> str:
        who = "an AI agent" if self.agent else self.author
        where = f"PR {self.pr}" if self.pr else f"commit {self.sha[:8]}"
        return f"{who} ({where}: {self.subject[:80]})"


@dataclass
class Entry:
    kind: str            # "flag" or "status"
    file: str            # path relative to the repository root
    line: int            # 1-based line of the flag or the status
    row: str             # the row's id (or its position)
    field: str           # the field to review
    value: str
    sources: list[str] = field(default_factory=list)
    checked_by: str = ""


def _scalar(node) -> str:
    return node.value if isinstance(node, yaml.ScalarNode) else ""


def _urls(node) -> list[str]:
    """Every https URL under a node, in order (a `sources` list of strings or of {url, checked} maps)."""
    out: list[str] = []
    if isinstance(node, yaml.ScalarNode):
        if node.value.startswith("https://"):
            out.append(node.value)
    elif isinstance(node, yaml.SequenceNode):
        for n in node.value:
            out += _urls(n)
    elif isinstance(node, yaml.MappingNode):
        for _, v in node.value:
            out += _urls(v)
    return out


def _rows(node, path: str = ""):
    """Every mapping in the document with a label: its id, else its place in the tree."""
    if isinstance(node, yaml.MappingNode):
        keys = {_scalar(k): v for k, v in node.value}
        label = _scalar(keys["id"]) if "id" in keys else path or "(file)"
        yield label, keys
        for k, v in node.value:
            yield from _rows(v, f"{path}.{_scalar(k)}" if path else _scalar(k))
    elif isinstance(node, yaml.SequenceNode):
        for i, v in enumerate(node.value):
            yield from _rows(v, f"{path}[{i}]")


def scan(text: str, rel: str) -> list[Entry]:
    """The flags and the reviewed statuses in one YAML file, with their lines; no git."""
    root = yaml.compose(text)
    entries: list[Entry] = []
    if root is None:
        return entries
    for label, keys in _rows(root):
        for key, node in keys.items():
            m = FLAG_KEY.match(key)
            if m and _scalar(node) == FLAG:
                name = m.group("field")
                entries.append(Entry("flag", rel, node.start_mark.line + 1, label, name, _scalar(keys.get(name)) if name in keys else "",
                                     _urls(keys[name + "_sources"]) if name + "_sources" in keys else []))
        status = keys.get("status")
        if status is not None and _scalar(status) in STATUSES:
            entries.append(Entry("status", rel, status.start_mark.line + 1, label, "status", _scalar(status),
                                 _urls(keys["sources"]) if "sources" in keys else []))
    return entries


def _git(args: list[str], cwd: Path) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout


def blame(repo: Path, rel: str) -> dict[int, str]:
    """Line number to the sha of the commit that last wrote it, at HEAD."""
    out, lines, sha = _git(["blame", "--line-porcelain", "HEAD", "--", rel], repo), {}, ""
    for row in out.splitlines():
        m = re.match(r"^([0-9a-f]{40}) \d+ (\d+)", row)
        if m:
            sha = m.group(1)
            lines[int(m.group(2))] = sha
    return lines


def commit(repo: Path, sha: str, cache: dict[str, Commit]) -> Commit:
    if sha not in cache:
        author, subject, body = _git(["log", "-1", "--format=%an%x00%s%x00%B", sha], repo).split("\x00", 2)
        cache[sha] = Commit(sha, author, subject, bool(AI_MARK.search(body)))
    return cache[sha]


def content_files(repo: Path) -> list[str]:
    """Every tracked YAML file under content/, in ordinal order."""
    return sorted(f for f in _git(["ls-files", "content"], repo).splitlines() if f.endswith(".yaml"))


def queue(repo: Path) -> tuple[list[Entry], int]:
    """The entries to review, and how many reviewed statuses were set by a commit with no agent marker (not listed)."""
    cache: dict[str, Commit] = {}
    entries: list[Entry] = []
    human = 0
    for rel in content_files(repo):
        found = scan((repo / rel).read_text(encoding="utf-8"), rel)
        if not found:
            continue
        lines = blame(repo, rel)
        for e in found:
            c = commit(repo, lines[e.line], cache)
            if e.kind == "flag":
                e.checked_by = f"an AI agent (the `{FLAG}` flag)"
                entries.append(e)
            elif c.agent:
                e.checked_by = c.describe()
                entries.append(e)
            else:
                human += 1
    return entries, human


def _cell(text: str) -> str:
    return text.replace("|", "\\|").replace("\n", " ")


def page(entries: list[Entry], human: int, head: str) -> str:
    flags = [e for e in entries if e.kind == "flag"]
    statuses = [e for e in entries if e.kind == "status"]
    out = [
        "# Human-review queue",
        "",
        f"Generated by `uv run tools/review_queue.py --write` at commit `{head[:8]}`. Do not edit by hand: rerun the tool.",
        "It changes no status. Clearing an entry is a content change in its own pull request: a native speaker deletes a",
        "name's `*_review: agent-checked` line once they have read it against its source (#564); a person who has checked",
        "every claim of an agent-set row records it as `docs/HISTORIAN_REVIEW.md` describes.",
        "",
        "How \"agent-set\" is decided, and its limits: the docstring of `tools/review_queue.py` "
        "([CONTENT_POLICY.md](CONTENT_POLICY.md), section 2, rule 6).",
        "",
        f"- Hebrew names flagged `{FLAG}`: **{len(flags)}**",
        f"- `fact-checked` or `sensitivity-reviewed` statuses last set by an agent-written commit: **{len(statuses)}**",
        f"- the same statuses set by a commit with no agent marker (not listed): {human}",
        "",
    ]
    for title, items, cols in (
        ("Hebrew names to review (#564)", flags, ("Line", "Row", "Field", "Value", "Sources", "Checked by")),
        ("Statuses an agent set", statuses, ("Line", "Row", "Status", "Sources", "Set by")),
    ):
        out += [f"## {title}", ""]
        if not items:
            out += ["None.", ""]
            continue
        for rel in sorted({e.file for e in items}):
            rows = [e for e in items if e.file == rel]
            out += [f"### [{rel}](../{rel}) ({len(rows)})", "", "| " + " | ".join(cols) + " |", "|" + "---|" * len(cols)]
            for e in rows:
                src = "<br>".join(f"<{u}>" for u in e.sources) or "(none in the row)"
                link = f"[{e.line}](../{e.file}#L{e.line})"
                cells = [link, e.row, e.field, e.value, src, e.checked_by] if e.kind == "flag" else [link, e.row, e.value, src, e.checked_by]
                out.append("| " + " | ".join(_cell(c) for c in cells) + " |")
            out.append("")
    return "\n".join(out).rstrip("\n") + "\n"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--write", action="store_true", help=f"write {PAGE.relative_to(ROOT)} instead of printing it")
    args = ap.parse_args(argv)
    if _git(["rev-parse", "--is-shallow-repository"], ROOT).strip() == "true":
        print("review_queue: a shallow clone blames every line on one commit; run it on a full clone", file=sys.stderr)
        return 2
    entries, human = queue(ROOT)
    text = page(entries, human, _git(["rev-parse", "HEAD"], ROOT).strip())
    if args.write:
        PAGE.write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {PAGE.relative_to(ROOT)}: {len(entries)} entries")
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
