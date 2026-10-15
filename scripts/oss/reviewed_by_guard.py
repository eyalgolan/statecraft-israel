# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Only a person signs a row off, and an agent may not edit a row a person has signed off. For every commit in a pull
request's range this fails when:

1. a commit that an AI agent co-wrote adds, changes or removes a `reviewed_by` or `reviewed_on` line in a content/ YAML
   file (owner, 15 October 2026, #375: "Yes, humans only");
2. a commit that an AI agent co-wrote changes any line, the id included, of a row that carries a person's whole sign-off
   in that commit's parent (so on the base, or signed earlier in the same pull request) (owner, 15 October 2026, on #629: "Block it in CI"), unless a later commit of the same pull
   request with no AI trailer signs that row again or removes its sign-off;
3. a person's added `reviewed_on` is after the day of their own commit.

"Co-wrote" is the trailer test of tools/review_queue.py (AI_MARK: "Co-Authored-By: Claude / Codex / Copilot / GPT ...",
"Generated with [Claude Code]" or "Generated with Codex"), read from each commit's full message. For (1) any non-comment
line that names either key counts, quoted or not, block or flow style. A row's span is its mapping's lines
(tools/review_queue.py, signed_rows); a line inserted right after a block row at its keys' indent or deeper belongs to it;
a comment inside a signed row counts as part of it. A commit is diffed against its parent with renames followed; a merge
commit by its combined diff (--cc), so lines it takes unchanged from one parent (a person's sign-off merged in from main)
are not its own.

The diff is read with every option that could change its shape pinned (no colour, no external diff or textconv, text even
where .gitattributes says binary, a/ and b/ prefixes, unquoted paths, no relative paths) and a pathspec for content/*.yaml
and *.yml, so a path with a space or a non-Latin letter, a -diff attribute or a local git setting cannot hide a line.

It fails closed: a base or head that does not resolve, a range git cannot list, a shallow clone, a commit whose message,
diff or files cannot be read, a content YAML file that does not parse, and a path git had to quote are failures, never
passes.

Usage: uv run scripts/oss/reviewed_by_guard.py --base <base-sha> [--head HEAD] [--repo .]"""
from __future__ import annotations

import argparse
import datetime
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))
import yaml  # noqa: E402
from review_queue import AI_MARK, signed_rows  # noqa: E402  (one definition of the trailer test and of a signed row)

EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"
PATHSPEC = [":(glob)content/**/*.yaml", ":(glob)content/**/*.yml"]
CONFIG = ["-c", "core.quotePath=false", "-c", "color.ui=never", "-c", "diff.noprefix=false", "-c", "diff.mnemonicPrefix=false",
          "-c", "diff.relative=false", "-c", "diff.external=", "-c", "core.attributesFile=/dev/null"]
DIFF = ["--no-color", "--no-ext-diff", "--no-textconv", "--text", "--src-prefix=a/", "--dst-prefix=b/", "--unified=0"]
NAMES = re.compile(r"reviewed_(?:by|on)")
ON_VALUE = re.compile(r"""["']?reviewed_on["']?\s*:\s*["']?([^,}\s#"']*)""")
HUNK = re.compile(r"^(@+) (.*?) @+")


class GuardError(Exception):
    """Something the guard cannot determine: a failure, never a pass."""


def _git(args: list[str], cwd: Path) -> str:
    return subprocess.run(["git", *CONFIG, *args], cwd=cwd, check=True, capture_output=True, text=True).stdout


def _resolve(repo: Path, ref: str) -> str:
    try:
        return _git(["rev-parse", "--verify", "--quiet", ref + "^{commit}"], repo).strip()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot resolve {ref!r} to a commit") from e


@dataclass
class Hunk:
    old: list[tuple[int, int]]          # per parent: (start, count) as in the hunk header
    new: tuple[int, int]
    lines: list[tuple[str, str]] = field(default_factory=list)   # (the parents' columns, the line's text)


def hunks(diff: str) -> list[Hunk]:
    """The hunks of a unified (one parent) or combined (--cc) diff of one file, with the -U0 ranges."""
    out: list[Hunk] = []
    cols = 1
    for line in diff.splitlines():
        m = HUNK.match(line)
        if m:
            cols = len(m.group(1)) - 1
            ranges = m.group(2).split()
            def rng(r):
                a, _, b = r[1:].partition(",")
                return int(a), int(b) if b else 1
            out.append(Hunk([rng(r) for r in ranges[:-1]], rng(ranges[-1])))
            continue
        if out and line[:cols].strip(" +-") == "" and len(line) >= cols and any(c in "+-" for c in line[:cols]):
            out[-1].lines.append((line[:cols], line[cols:]))
    return out


def _named(text: str) -> bool:
    return not text.lstrip().startswith("#") and NAMES.search(text) is not None


def _touches(row, start: int, count: int, inserted: list[str]) -> bool:
    """True when a change at old lines [start, start+count) (count 0: an insertion after line start) is inside the row."""
    if count > 0:
        return start <= row.last and start + count - 1 >= row.first
    if row.first <= start < row.last:
        return True
    if start == row.last and not row.flow and inserted:
        first = inserted[0]
        indent = len(first) - len(first.lstrip(" "))
        return first.strip() != "" and not first.lstrip().startswith("#") and indent >= row.key_column and not first.lstrip().startswith("- ")
    return False


def _show(repo: Path, rev: str, path: str) -> str | None:
    try:
        return _git(["show", f"{rev}:{path}"], repo)
    except subprocess.CalledProcessError:
        return None


def _signed(repo: Path, rev: str, path: str, must_exist: bool):
    """The signed rows of a file at a revision. A file the diff says was there but git cannot show is a failure."""
    text = _show(repo, rev, path)
    if text is None:
        if must_exist:
            raise GuardError(f"cannot read {path} at {rev[:10]}")
        return []
    try:
        return signed_rows(text)
    except yaml.YAMLError as e:
        raise GuardError(f"cannot parse {path} at {rev[:10]}") from e


@dataclass
class Change:
    path: str                           # the file after the commit (or before, for a deletion)
    old_paths: list[str]                # per parent: the file before the commit ("" when it did not exist)
    hunks: list[Hunk]


def changes(repo: Path, sha: str, parents: list[str]) -> list[Change]:
    """The content YAML files a commit changes, with their hunks against each parent."""
    if len(parents) <= 1:
        parent = parents[0] if parents else EMPTY_TREE
        names = _git(["diff", "--name-status", "-z", "-M", "--no-ext-diff", parent, sha, "--", *PATHSPEC], repo).split("\0")
        out, i = [], 0
        while i < len(names) - 1:
            status = names[i]
            if status[:1] in "RC":
                old, new, i = names[i + 1], names[i + 2], i + 3
            else:
                old = new = names[i + 1]
                i += 2
                if status[:1] == "A":
                    old = ""
            spec = [f":(literal){p}" for p in {old, new} if p]
            diff = _git(["diff", *DIFF, "-M", parent, sha, "--", *spec], repo)
            out.append(Change(new, [old], hunks(diff)))
        return out
    diff = _git(["diff-tree", *DIFF, "--cc", sha, "--", *PATHSPEC], repo)
    out = []
    for block in re.split(r"(?m)^diff --cc ", diff)[1:]:
        path = block.split("\n", 1)[0]
        if path.startswith('"'):
            raise GuardError(f"cannot read the quoted path {path} in merge {sha[:10]}")
        out.append(Change(path, [path] * len(parents), hunks(block)))
    return out


def check(repo: Path, base: str, head: str) -> list[str]:
    """The problems in base..head, one line each; raises GuardError for anything it cannot determine."""
    repo = Path(repo)
    try:
        if _git(["rev-parse", "--is-shallow-repository"], repo).strip() != "false":
            raise GuardError("cannot judge a shallow clone (fetch the full history: fetch-depth: 0)")
    except subprocess.CalledProcessError as e:
        raise GuardError("cannot read the repository") from e
    b, h = _resolve(repo, base), _resolve(repo, head)
    try:
        shas = _git(["rev-list", "--reverse", "--topo-order", f"{b}..{h}"], repo).split()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot list the commits in {base}..{head}") from e
    problems: list[str] = []
    edited: dict[tuple[str, str], str] = {}   # (file, row) an agent edited while signed -> the commit, until a person re-signs
    for sha in shas:
        try:
            message = _git(["log", "-1", "--format=%B", sha], repo)
            day = datetime.date.fromisoformat(_git(["log", "-1", "--format=%as", sha], repo).strip())
            parents = _git(["rev-list", "--parents", "-n", "1", sha], repo).split()[1:]
            files = changes(repo, sha, parents)
        except (subprocess.CalledProcessError, ValueError) as e:
            raise GuardError(f"cannot read commit {sha[:10]}") from e
        agent = AI_MARK.search(message) is not None
        for ch in files:
            named = [(cols, text) for hk in ch.hunks for cols, text in hk.lines if _named(text)]
            if agent:
                if named:
                    problems.append(f"{sha[:10]}: a commit with an AI co-author trailer changes a reviewed_by / reviewed_on line "
                                    f"in {ch.path}; only a person signs a row off (#375). Remove those lines from this commit.")
                for p, (parent, old) in enumerate(zip(parents or [EMPTY_TREE], ch.old_paths)):
                    if not old or parent == EMPTY_TREE:
                        continue
                    for row in _signed(repo, parent, old, must_exist=len(parents) == 1):
                        for hk in ch.hunks:
                            if p >= len(hk.old):
                                continue
                            inserted = [t for cols, t in hk.lines if "+" in cols]
                            if _touches(row, hk.old[p][0], hk.old[p][1], inserted):
                                edited.setdefault((ch.path, row.label), sha)
                                break
                continue
            # A person's commit: the sign-off's day, and re-signing (or unsigning) a row an agent edited.
            for cols, text in named:
                if "+" in cols:
                    m = ON_VALUE.search(text)
                    if m:
                        try:
                            signed_on = datetime.date.fromisoformat(m.group(1))
                        except ValueError:
                            problems.append(f"{sha[:10]}: {ch.path}: reviewed_on {m.group(1)!r} is not a day (YYYY-MM-DD)")
                            continue
                        if signed_on > day:
                            problems.append(f"{sha[:10]}: {ch.path}: reviewed_on {m.group(1)} is after the commit's own day, {day}")
            if named:
                touched_rows = set()
                rows_after = _signed(repo, sha, ch.path, must_exist=False)
                for hk in ch.hunks:
                    for row in rows_after:
                        if _touches(row, hk.new[0], hk.new[1], []) or hk.new[1] == 0 and row.first <= hk.new[0] <= row.last:
                            touched_rows.add(row.label)
                    for p, (parent, old) in enumerate(zip(parents or [EMPTY_TREE], ch.old_paths)):
                        if old and parent != EMPTY_TREE and p < len(hk.old):
                            for row in _signed(repo, parent, old, must_exist=len(parents) == 1):
                                if _touches(row, hk.old[p][0], hk.old[p][1], []):
                                    touched_rows.add(row.label)
                for label in touched_rows:
                    edited.pop((ch.path, label), None)
    for (path, label), sha in edited.items():
        problems.append(f"{sha[:10]}: a commit with an AI co-author trailer edits {path} row {label!r}, which a person signed "
                        f"off (owner, 15 October 2026: \"Block it in CI\"). A person must read it again and sign it again "
                        f"(new reviewed_by / reviewed_on), or remove its sign-off, in a commit of their own.")
    return problems


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--base", required=True)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--repo", default=".")
    a = ap.parse_args(argv)
    try:
        problems = check(Path(a.repo), a.base, a.head)
    except GuardError as e:
        print(f"reviewed_by guard: {e}. Failing closed.")
        return 1
    for p in problems:
        print(p)
    if problems:
        return 1
    print("reviewed_by guard: no AI-co-authored commit touches a sign-off or a signed-off row.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
