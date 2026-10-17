# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Lists the content that no named person has reviewed yet and that an AI agent may have checked (CONTENT_POLICY
section 2, rule 6), so the owner can work through it: docs/review-queue.md.

Read-only on content/: it parses the YAML and asks git, and writes only the Markdown page (or prints it).

Two kinds of entry:

1. **Agent-checked Hebrew names**: every `*_review: agent-checked` flag (`name_he_review`, `title_he_review`,
   `title_he_f_review`, `body_he_review`; #564). The flag itself says no person has read the name.
2. **Reviewed statuses last written by a commit with an AI co-author trailer**: every row whose `status` is
   `fact-checked`, `historian-reviewed` or `sensitivity-reviewed`, where the line that holds that status was last
   written by such a commit.

The heuristic for (2), because content has no reviewer field: `docs/HISTORIAN_REVIEW.md` keeps the reviewer's record in
the pull request, not in the YAML (no `reviewed_by` key), so the only signal in the repository is git history. For each
such status line, `git blame` on the working-tree file (the text this script reads, so a deleted flag or another
uncommitted edit does not shift the lines), with `-w -M`, gives the commit that last wrote it; a file git does not track
yet is read too, and every row in it counts as not committed. A commit counts when its message has an AI co-author trailer or the Claude Code line ("Co-Authored-By:
Claude ...", "Co-Authored-By: ... Codex / Copilot / GPT ...", "Generated with [Claude Code]", "Generated with Codex").
A row that carries a person's whole sign-off beside its status (`reviewed_by`, a GitHub handle, and `reviewed_on`, a day;
owner, 15 October 2026, #375: "Yes, humans only"; tools/review_record.py) leaves the queue and is counted as signed off,
whatever wrote its status line; a malformed sign-off does not count (fail closed). Any other such row is listed with
the commit and its pull request number. A status line not committed yet is listed too, as
"not committed yet": no commit vouches for it, so the queue fails closed. Any other row is counted, not listed.

Limits, said plainly, and on the page too:
- **Over-inclusive.** An agent-co-written commit may only record a person's decision (the owner's sensitivity sign-offs
  are recorded that way), and a person may have reviewed an agent's row and merged it without touching the line. Both
  stay listed: the list is "not shown to be human-reviewed", not "checked by an agent".
- **Blind spots.** An agent whose commits carry none of these trailers (another tool, or an outside contributor who does
  not mark AI use) counts as a person; a person who set a status without checking is not caught.
- **Edits that move the blame.** Blame ignores whitespace (`-w`) and follows lines moved within a file (`-M`), so a
  re-indent or a re-spacing keeps the earlier author. It cannot see through a key reordering that moves too little text
  for `-M` to match (git needs about 20 characters), nor a reflow between flow style (`{ id: x, status: ... }`) and block
  style: the commit that did either becomes the line's author.
- **History.** A shallow clone blames every line on its one commit, so the script refuses one (exit 2). The page names
  the commit it was generated from.

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

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import review_record  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
PAGE = ROOT / "docs" / "review-queue.md"
STATUSES = ("fact-checked", "historian-reviewed", "sensitivity-reviewed")
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
        """The commit, not a verdict on who checked the row: an agent-co-written commit may only record a person's
        decision (the owner's sensitivity sign-offs are recorded that way)."""
        who = "a commit with an AI co-author trailer" if self.agent else f"a commit by {self.author}"
        where = f"PR {self.pr}" if self.pr else f"commit {self.sha[:8]}"
        subject = PR_NUMBER.sub("", self.subject).rstrip()
        if len(subject) > 80:
            subject = subject[:79].rstrip() + "…"
        return f"{who} ({where}: {subject})"


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


def _value(node):
    """A scalar's text (None for a YAML null), or the node itself when it is not a scalar (never a valid sign-off)."""
    if not isinstance(node, yaml.ScalarNode):
        return node
    return None if node.tag.endswith(":null") else node.value


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


def _mappings(node, path: str = ""):
    """Every mapping node in the document with its label (as _rows)."""
    if isinstance(node, yaml.MappingNode):
        keys = {_scalar(k): v for k, v in node.value}
        yield (_scalar(keys["id"]) if "id" in keys else path or "(file)"), keys, node
        for k, v in node.value:
            yield from _mappings(v, f"{path}.{_scalar(k)}" if path else _scalar(k))
    elif isinstance(node, yaml.SequenceNode):
        for i, v in enumerate(node.value):
            yield from _mappings(v, f"{path}[{i}]")


@dataclass
class SignedRow:
    label: str
    first: int        # 1-based lines the row's mapping spans, inclusive
    last: int
    key_column: int   # 0-based column of its keys (a line inserted at this indent or deeper, right after it, belongs to it)
    flow: bool        # a one-line { ... } row: nothing inserted after it belongs to it
    # 1-based inclusive spans of nodes the row pulls in from elsewhere through an alias or a merge key (`*a`, `<<: *d`):
    # editing the anchor changes the row as surely as editing the row (people.yaml shares source anchors across rows).
    extra: list = field(default_factory=list)


def signed_rows(text: str) -> list[SignedRow]:
    """The rows of a YAML text that carry a person's whole sign-off (#375), with their line spans. Raises yaml.YAMLError on
    text that is not YAML."""
    root = yaml.compose(text)
    if root is None:
        return []
    lines = text.splitlines()

    def last_line(n) -> int:
        end = n.end_mark
        before = lines[end.line][:end.column] if end.line < len(lines) else ""
        return (end.line - 1 if before.strip() == "" else end.line) + 1

    out = []
    for label, keys, node in _mappings(root):
        if "reviewed_by" not in keys or not review_record.signed({k: _value(keys[k]) for k in review_record.FIELDS if k in keys}):
            continue
        first, last = node.start_mark.line + 1, last_line(node)
        extra, seen, todo = [], set(), [node]
        while todo:   # every node the row reaches; one whose text lies outside the row came in through an alias
            n = todo.pop()
            if id(n) in seen:
                continue
            seen.add(id(n))
            a, b = n.start_mark.line + 1, last_line(n)
            if a < first or a > last:
                extra.append((a, max(a, b)))
            if isinstance(n, yaml.MappingNode):
                todo.extend(v for kv in n.value for v in kv)
            elif isinstance(n, yaml.SequenceNode):
                todo.extend(n.value)
        out.append(SignedRow(label, first, last, node.start_mark.column, bool(node.flow_style), sorted(set(extra))))
    return out


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
                source_node = keys.get(name + "_sources")
                # People keep a field's sources under biography_sources.<field> (content/people.yaml).
                if source_node is None and isinstance(keys.get("biography_sources"), yaml.MappingNode):
                    source_node = {_scalar(k): v for k, v in keys["biography_sources"].value}.get(name)
                entries.append(Entry("flag", rel, node.start_mark.line + 1, label, name, _scalar(keys.get(name)) if name in keys else "",
                                     _urls(source_node) if source_node is not None else []))
        status = keys.get("status")
        if status is not None and _scalar(status) in STATUSES:
            # A whole sign-off by a person (#375) takes the row out of the queue; a malformed one does not (fail closed).
            signoff = {k: _value(keys[k]) for k in review_record.FIELDS if k in keys}
            entries.append(Entry("signed" if review_record.signed(signoff) else "status", rel, status.start_mark.line + 1, label,
                                 "status", _scalar(status), _urls(keys["sources"]) if "sources" in keys else []))
    return entries


def _git(args: list[str], cwd: Path) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout


UNCOMMITTED = "0" * 40


def blame(repo: Path, rel: str) -> dict[int, str]:
    """Line number of the working-tree file (the text scan() reads) to the sha of the commit that last wrote it; a line
    not committed yet maps to UNCOMMITTED. Blaming HEAD instead would misplace every line after an uncommitted edit, such
    as a reviewer's deleted flag (second review of #611)."""
    # -w: a whitespace-only change (re-spacing, re-indenting) is not a review, so it keeps the earlier author; -M: a line
    # moved within the file keeps its author too (independent re-review of #611).
    out, lines, sha = _git(["blame", "-w", "-M", "--line-porcelain", "--", rel], repo), {}, ""
    for row in out.splitlines():
        m = re.match(r"^([0-9a-f]{40}) \d+ (\d+)", row)
        if m:
            sha = m.group(1)
            lines[int(m.group(2))] = sha
    return lines


def commit(repo: Path, sha: str, cache: dict[str, Commit]) -> Commit:
    if sha == UNCOMMITTED:
        return Commit(sha, "nobody yet", "not committed yet", False)
    if sha not in cache:
        author, subject, body = _git(["log", "-1", "--format=%an%x00%s%x00%B", sha], repo).split("\x00", 2)
        cache[sha] = Commit(sha, author, subject, bool(AI_MARK.search(body)))
    return cache[sha]


class QueueError(Exception):
    """The working tree cannot be read as the page needs (run() prints it and exits 2)."""


def content_files(repo: Path) -> tuple[list[str], set[str]]:
    """Every YAML file under content/, in ordinal order: the tracked ones and the untracked ones git does not ignore (a new
    file is content too; independent re-review of #611); and the set of untracked ones. A tracked file missing from the
    working tree (a plain mv not yet committed) is an error, not a silent gap."""
    tracked = [f for f in _git(["ls-files", "content"], repo).splitlines() if f.endswith(".yaml")]
    missing = [f for f in tracked if not (repo / f).exists()]
    if missing:
        raise QueueError(f"{', '.join(missing)}: tracked but missing from the working tree (moved or deleted?); commit the "
                         "move or restore the file, then rerun")
    untracked = {f for f in _git(["ls-files", "--others", "--exclude-standard", "content"], repo).splitlines() if f.endswith(".yaml")}
    return sorted(set(tracked) | untracked), untracked


def queue(repo: Path) -> tuple[list[Entry], int, int]:
    """The entries to review; how many reviewed statuses were last written by a commit with no agent marker (not listed);
    and how many rows a person signed off (reviewed_by and reviewed_on, #375; not listed)."""
    cache: dict[str, Commit] = {}
    entries: list[Entry] = []
    human = signed = 0
    files, untracked = content_files(repo)
    for rel in files:
        found = scan((repo / rel).read_text(encoding="utf-8"), rel)
        if not found:
            continue
        lines = {e.line: UNCOMMITTED for e in found} if rel in untracked else blame(repo, rel)
        for e in found:
            if e.kind == "signed":
                signed += 1
                continue
            c = commit(repo, lines[e.line], cache)
            if e.kind == "flag":
                e.checked_by = f"an AI agent (the `{FLAG}` flag)"
                entries.append(e)
            elif c.sha == UNCOMMITTED:   # no commit vouches for it yet: list it, never hide it (fail closed)
                e.checked_by = "not committed yet"
                entries.append(e)
            elif c.agent:
                e.checked_by = c.describe()
                entries.append(e)
            else:
                human += 1
    return entries, human, signed


def _cell(text: str) -> str:
    return text.replace("|", "\\|").replace("\n", " ")


def page(entries: list[Entry], human: int, head: str, signed: int = 0) -> str:
    flags = [e for e in entries if e.kind == "flag"]
    statuses = [e for e in entries if e.kind == "status"]
    out = [
        "# Human-review queue",
        "",
        f"Generated by `uv run tools/review_queue.py --write` at commit `{head[:8]}`. Do not edit by hand: rerun the tool.",
        "It changes no status. Clearing an entry is a content change in its own pull request: a native speaker deletes a",
        "name's `*_review: agent-checked` line once they have read it against its source (#564); a person who has checked",
        "every claim of a listed row signs it off beside its status with `reviewed_by` (their GitHub handle) and",
        "`reviewed_on` (the day), as `docs/HISTORIAN_REVIEW.md` describes; only a person writes those (#375).",
        "",
        "**This list is over-inclusive, and it is not a verdict on who checked anything.** The second table lists every",
        "reviewed status whose line was last written by a commit with an AI co-author trailer. Some of those record a",
        "person's decision that an agent only typed in, such as the owner's sensitivity sign-offs; they are listed",
        "anyway until a person signs them off with `reviewed_by` and `reviewed_on`, the only reviewer record the queue",
        "reads. It also misses things: an agent whose commits carry no such trailer (another tool, or an",
        "outside contributor who does not mark AI use) is counted as a person, and a person who set a status without",
        "checking is not caught. Blame ignores whitespace and follows moved lines, but a key reordering too small for git",
        "to match, or a reflow between flow and block style, makes that commit the line's author. The heuristic in full:",
        "[tools/review_queue.py](../tools/review_queue.py)",
        "([CONTENT_POLICY.md](CONTENT_POLICY.md), section 2, rule 6).",
        "",
        f"- Hebrew names flagged `{FLAG}`: **{len(flags)}**",
        f"- `fact-checked`, `historian-reviewed` or `sensitivity-reviewed` statuses last written by a commit with an AI "
        f"co-author trailer, or not committed yet: **{len(statuses)}**",
        f"- the same statuses last written by a commit without one (not listed): {human}",
        f"- signed off by a person (`reviewed_by` and `reviewed_on`; not listed): {signed}",
        "",
    ]
    for title, items, cols in (
        ("Hebrew names to review (#564)", flags, ("Line", "Row", "Field", "Value", "Sources", "Checked by")),
        ("Reviewed statuses last written by a commit with an AI co-author trailer, or not committed yet", statuses,
         ("Line", "Row", "Status", "Sources", "Last written by")),
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


def run(repo: Path, write: bool) -> int:
    """Prints the page for <repo>, or writes it to its docs/review-queue.md; 2 on a shallow clone."""
    if _git(["rev-parse", "--is-shallow-repository"], repo).strip() == "true":
        print("review_queue: a shallow clone blames every line on one commit; run it on a full clone", file=sys.stderr)
        return 2
    try:
        entries, human, signed = queue(repo)
    except QueueError as e:
        print(f"review_queue: {e}", file=sys.stderr)
        return 2
    text = page(entries, human, _git(["rev-parse", "HEAD"], repo).strip(), signed)
    if write:
        target = repo / PAGE.relative_to(ROOT)
        target.write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {target.relative_to(repo)}: {len(entries)} entries")
    else:
        sys.stdout.write(text)
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--write", action="store_true", help=f"write {PAGE.relative_to(ROOT)} instead of printing it")
    args = ap.parse_args(argv)
    return run(ROOT, args.write)


if __name__ == "__main__":
    sys.exit(main())
