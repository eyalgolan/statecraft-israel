# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Release step (docs/RELEASING.md, step 2): moves every changelog.d/<name>.md fragment into CHANGELOG.md under
## [Unreleased] and deletes the fragments. Fragments are read in filename order. A fragment's entries go under the
`### <Section>` line above them in the fragment (Added when there is none), at the top of the first such section in
Unreleased, which is added at Unreleased's end if missing. It refuses, and changes nothing, if a fragment has no entry.

Usage: uv run scripts/oss/changelog_fold.py [--root .]"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from changelog_check import FRAGMENTS, has_entry, is_fragment

UNRELEASED = "## [Unreleased]"


def fold(changelog: str, fragments: list[tuple[str, str]]) -> str:
    """fragments: (file name, text) pairs, in any order."""
    sections: dict[str, list[str]] = {}
    for name, text in sorted(fragments):
        if not has_entry(text):
            raise ValueError(f"{name}: no entry (a line starting with '- ')")
        section = "Added"
        for line in text.splitlines():
            if line.startswith("### "):
                section = line[4:].strip()
            elif line.strip():
                sections.setdefault(section, []).append(line)
    lines = changelog.splitlines()
    if UNRELEASED not in lines:
        raise ValueError(f"CHANGELOG.md has no '{UNRELEASED}' line")
    start = lines.index(UNRELEASED)
    for section, entries in sections.items():
        end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith("## ")), len(lines))
        heading = next((i for i in range(start + 1, end) if lines[i] == f"### {section}"), None)
        if heading is None:
            last = max(i for i in range(start, end) if lines[i].strip())
            lines[last + 1:last + 1] = ["", f"### {section}", *entries]
        else:
            lines[heading + 1:heading + 1] = entries
    return "\n".join(lines) + "\n"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    root = Path(ap.parse_args(argv).root)
    paths = sorted(p for p in (root / FRAGMENTS).glob("*.md") if is_fragment(f"{FRAGMENTS}{p.name}"))
    log = root / "CHANGELOG.md"
    try:
        folded = fold(log.read_text(encoding="utf-8"), [(p.name, p.read_text(encoding="utf-8")) for p in paths])
    except ValueError as e:
        print(f"REFUSED: {e}")
        return 1
    log.write_text(folded, encoding="utf-8")
    for p in paths:
        p.unlink()
    print(f"Folded {len(paths)} fragments into CHANGELOG.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
