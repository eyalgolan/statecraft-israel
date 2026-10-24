#!/usr/bin/env python3
"""Markdown link check (#333): every relative link in a tracked *.md file must point at a file that is tracked in git.

`uv run scripts/oss/md_links.py [--root DIR]`. Ignores code (inline spans, and fences of three or more backticks or tildes), absolute URLs (any `scheme:`),
`#anchor` and `mailto:` links, and targets with brackets (reference-style leftovers). Anchors after `#` are not checked, only the file.
Handles a link title (`[a](b "t")`), `<angle>` targets, balanced parentheses, `%20` escapes and the outer link of a badge (`[![x](img)](page)`).
A target must match a tracked file or directory exactly, so a wrong letter case fails on macOS too (CI's Linux would).
One line per dead link (file: link); exit 1 if any."""
import argparse
import os
import re
import string
import subprocess
import sys
from collections.abc import Iterator
from urllib.parse import unquote
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LINK = re.compile(r"\]\((<?)")
LINK_END = re.compile(r">?(?:\s+(?:\"[^\"]*\"|'[^']*'))?\)")
BACKSLASH_ESCAPE = re.compile(r"\\([" + re.escape(string.punctuation) + r"])")
# A fence opens with 3+ backticks or tildes and closes with the same run; an inline span is a backtick run closed by an
# equal run and may not cross a blank line, so one stray backtick cannot swallow the rest of the file.
FENCE = re.compile(r"(`{3,}|~{3,}).*?\1", re.S)
INLINE_CODE = re.compile(r"(?<!`)(`+)(?!`)(?:(?!\n[ \t]*\n).)+?(?<!`)\1(?!`)", re.S)


def link_targets(text: str) -> Iterator[str]:
    """Keep balanced inner parentheses; angle-wrapped targets end at > instead."""
    consumed = 0
    for match in LINK.finditer(text):
        if match.start() < consumed:
            continue
        start = end = match.end()
        angle = bool(match.group(1))
        depth = 0
        while end < len(text):
            char = text[end]
            if char == "\\" and end + 1 < len(text) and text[end + 1] in string.punctuation:
                end += 2
                continue
            if char.isspace() or char == ">" or (char == ")" and depth == 0 and not angle):
                break
            if char == "(" and not angle:
                depth += 1
            elif char == ")" and not angle:
                depth -= 1
            end += 1
        suffix = LINK_END.match(text, end)
        if end > start and depth == 0 and suffix:
            consumed = suffix.end()
            yield text[start:end]


def tracked_markdown(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "*.md"], capture_output=True, text=True, check=True).stdout
    return [f for f in out.split("\0") if f]


def tracked_paths(root: Path) -> set[str]:
    """Every tracked file and every directory above one, with the exact letter case git stores."""
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, text=True, check=True).stdout
    paths = {"."}  # the repository root, which `[top](..)` from docs/ or `[x](./)` resolves to
    for f in filter(None, out.split("\0")):
        parts = f.split("/")
        paths.update("/".join(parts[:i]) for i in range(1, len(parts) + 1))
    return paths


def dead_links(root: Path) -> list[tuple[str, str]]:
    bad = []
    known = tracked_paths(root)
    for f in tracked_markdown(root):
        text = INLINE_CODE.sub("", FENCE.sub("", (root / f).read_text(encoding="utf-8")))
        for target in link_targets(text):
            normalized = BACKSLASH_ESCAPE.sub(r"\1", target)
            if re.match(r"[A-Za-z][A-Za-z0-9+.-]*:|#", normalized) or "[" in normalized:
                continue
            path = unquote(normalized.split("#")[0].split("?")[0])
            if path and os.path.normpath(os.path.join(os.path.dirname(f), path)).replace(os.sep, "/").rstrip("/") not in known:
                bad.append((f, target))
    return bad


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("--root", type=Path, default=ROOT)
    bad = dead_links(p.parse_args(argv).root)
    for f, t in bad:
        print(f"{f}: {t}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
