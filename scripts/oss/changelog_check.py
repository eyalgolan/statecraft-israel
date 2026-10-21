# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Per-pull-request changelog rule: if the PR changes a user-visible path, it must touch CHANGELOG.md, add a
changelog.d/<name>.md fragment with at least one entry, or carry the `no-changelog` label. Looks at the PR's own diff
only, never at earlier history.

Usage: uv run scripts/oss/changelog_check.py --base <base-sha> [--head HEAD] [--skip-label true|false]"""
from __future__ import annotations

import argparse
import subprocess
import sys
from collections.abc import Sequence

USER_VISIBLE = ("content/", "sim/Statecraft.Sim/", "sim/Statecraft.Cli/", "unity/Assets/", "unity/ProjectSettings/",
                "unity/Packages/")
NOT_USER_VISIBLE = ("unity/Assets/Tests/",)
FRAGMENTS = "changelog.d/"


def user_visible(paths: list[str]) -> list[str]:
    return [p for p in paths if p.startswith(USER_VISIBLE) and not p.startswith(NOT_USER_VISIBLE)]


def is_fragment(path: str) -> bool:
    name = path.removeprefix(FRAGMENTS)
    return path.startswith(FRAGMENTS) and name.endswith(".md") and "/" not in name and name != "README.md"


def has_entry(text: str) -> bool:
    """A fragment counts only with a bullet that says something; a blank file or a bare heading does not."""
    return any(line.startswith("- ") and line[2:].strip() for line in text.splitlines())


def verdict(paths: list[str], skip_label: bool, fragments: Sequence[str] = ()) -> tuple[bool, str]:
    visible = user_visible(paths)
    if not visible:
        return True, "No user-visible change; no CHANGELOG entry needed."
    if "CHANGELOG.md" in paths:
        return True, "CHANGELOG.md is updated."
    if fragments:
        return True, "A changelog fragment is added: " + ", ".join(fragments)
    if skip_label:
        return True, "The no-changelog label is set."
    listed = "\n".join(f"  {p}" for p in visible)
    return False, ("These user-visible paths changed without a CHANGELOG.md entry:\n" + listed +
                   "\nAdd a changelog.d/<name>.md fragment (see changelog.d/README.md) or a line under ## [Unreleased] "
                   "in CHANGELOG.md, or ask a maintainer for the no-changelog label.")


def changed_paths(base: str, head: str) -> list[str]:
    out = subprocess.run(["git", "diff", "--name-only", f"{base}...{head}"], capture_output=True, text=True, check=True)
    return [p for p in out.stdout.splitlines() if p]


def added_fragments(base: str, head: str) -> list[str]:
    """Fragments the PR adds with an entry in them. An edited, renamed or deleted fragment does not count."""
    out = subprocess.run(["git", "diff", "--name-only", "--find-renames", "--diff-filter=A", f"{base}...{head}", "--",
                          FRAGMENTS], capture_output=True, text=True, check=True)
    return [p for p in out.stdout.splitlines() if is_fragment(p) and has_entry(
        subprocess.run(["git", "show", f"{head}:{p}"], capture_output=True, text=True, check=True).stdout)]


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", required=True)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--skip-label", default="false")
    a = ap.parse_args(argv)
    ok, msg = verdict(changed_paths(a.base, a.head), a.skip_label.lower() == "true", added_fragments(a.base, a.head))
    print(msg)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
