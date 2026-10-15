# /// script
# requires-python = ">=3.11"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Only a person signs a row off (owner, 15 October 2026, #375: "Yes, humans only"). For every commit in a pull request's
range, this fails when a commit that an AI agent co-wrote adds, changes or removes a `reviewed_by` or `reviewed_on` line
in a content/ YAML file, and when a person's added `reviewed_on` is after the day of their own commit.

"Co-wrote" is the trailer test of tools/review_queue.py (AI_MARK: "Co-Authored-By: Claude / Codex / Copilot / GPT ...",
"Generated with [Claude Code]" or "Generated with Codex"), read from each commit's full message. A comment line that
only mentions the keys does not count; a key inside a flow mapping (`{ id: a, reviewed_by: x }`) does. A pure rename
changes no line. Each commit is diffed against its first parent, so a merge commit is checked too.

It fails closed: a base or head that does not resolve, a range git cannot list, a shallow clone, or a commit whose
message or diff cannot be read is a failure, never a pass.

Usage: uv run scripts/oss/reviewed_by_guard.py --base <base-sha> [--head HEAD] [--repo .]"""
from __future__ import annotations

import argparse
import datetime
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))
from review_queue import AI_MARK  # noqa: E402  (one definition of "an AI co-author trailer")

EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"
KEY = re.compile(r"(?:^|[\s{,\-])reviewed_(by|on)\s*:\s*([^,}\s#]*)")


class GuardError(Exception):
    """Something the guard cannot determine: a failure, never a pass."""


def _git(args: list[str], cwd: Path) -> str:
    return subprocess.run(["git", *args], cwd=cwd, check=True, capture_output=True, text=True).stdout


def _resolve(repo: Path, ref: str) -> str:
    try:
        return _git(["rev-parse", "--verify", "--quiet", ref + "^{commit}"], repo).strip()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot resolve {ref!r} to a commit") from e


def touched(diff: str) -> list[tuple[str, str, str, str]]:
    """(path, '+' or '-', key, value) for every added or removed sign-off line in a unified diff, comments left out."""
    out, path = [], ""
    for line in diff.splitlines():
        if line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else path
            continue
        if line.startswith("--- "):
            if line.startswith("--- a/"):
                path = line[6:]
            continue
        if not line[:1] in ("+", "-"):
            continue
        body = line[1:]
        if body.lstrip().startswith("#"):
            continue
        for m in KEY.finditer(body):
            out.append((path, line[0], "reviewed_" + m.group(1), m.group(2).strip("'\"")))
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
        shas = _git(["rev-list", "--reverse", f"{b}..{h}"], repo).split()
    except subprocess.CalledProcessError as e:
        raise GuardError(f"cannot list the commits in {base}..{head}") from e
    problems = []
    for sha in shas:
        try:
            message = _git(["log", "-1", "--format=%B", sha], repo)
            day = datetime.date.fromisoformat(_git(["log", "-1", "--format=%as", sha], repo).strip())
            parents = _git(["rev-list", "--parents", "-n", "1", sha], repo).split()[1:]
            diff = _git(["diff", "-M", "--unified=0", parents[0] if parents else EMPTY_TREE, sha, "--", "content"], repo)
        except (subprocess.CalledProcessError, ValueError) as e:
            raise GuardError(f"cannot read commit {sha[:10]}") from e
        lines = [t for t in touched(diff) if t[0].endswith((".yaml", ".yml"))]
        if not lines:
            continue
        if AI_MARK.search(message):
            files = sorted({t[0] for t in lines})
            problems.append(f"{sha[:10]}: a commit with an AI co-author trailer changes a reviewed_by / reviewed_on line in "
                            f"{', '.join(files)}; only a person signs a row off (#375). Remove those lines from this commit.")
            continue
        for path, sign, key, value in lines:
            if sign == "+" and key == "reviewed_on":
                try:
                    signed_on = datetime.date.fromisoformat(value)
                except ValueError:
                    problems.append(f"{sha[:10]}: {path}: reviewed_on {value!r} is not a day (YYYY-MM-DD)")
                    continue
                if signed_on > day:
                    problems.append(f"{sha[:10]}: {path}: reviewed_on {value} is after the commit's own day, {day}")
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
    print("reviewed_by guard: no AI-co-authored commit touches a sign-off.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
