#!/usr/bin/env python3
"""Flag a few generic AI phrases often seen in weak historical copy."""

from __future__ import annotations

import re
import sys
from pathlib import Path

BAD_PHRASES = [
    "in today\'s rapidly evolving landscape",
    "it is important to note that",
    "this historic moment",
    "at the end of the day",
    "without a doubt",
    "the world watched",
]


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: scan_for_generic_ai_phrases.py <file-or-directory>")
        return 1

    target = Path(sys.argv[1]).resolve()
    if target.is_file():
        files = [target]
    else:
        files = []
        for pattern in ("*.md", "*.yaml"):
            files.extend(target.rglob(pattern))

    hits: list[str] = []
    for path in sorted({p for p in files if p.is_file()}):
        if ".github" in path.parts or ".claude" in path.parts or path.name == "AGENTS.md":
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        for phrase in BAD_PHRASES:
            if re.search(re.escape(phrase), text, re.IGNORECASE):
                hits.append(f"{path}: {phrase}")

    if not hits:
        print("No generic AI phrases detected in the selected content files.")
        return 0

    for hit in hits:
        print(hit)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
