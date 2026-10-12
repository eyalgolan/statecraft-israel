#!/usr/bin/env python3
"""Stable rule IDs (#286). A rule is defined in docs/rules/*.md on a line that starts with its ID, like
`R-ECON-014: text`, `- **R-ECON-014** text` or `## R-ECON-014 Tax`. A test, a sim or Unity file or a content YAML file
cites it by writing the ID anywhere (a comment is enough).

Reports, one line each, and exits 1 if any:
  untested   an ID defined in docs/rules with no citation in sim/Statecraft.Sim.Tests
  dead       an ID cited in sim/, unity/Assets/ or content/ that no rule defines
  duplicate  an ID defined twice

Usage: uv run scripts/oss/rule_ids.py [--root DIR]"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ID = r"R-[A-Z]{2,6}-\d{3}"
DEFINITION = re.compile(rf"^\s*(?:#{{1,6}}\s+|[-*]\s+)?\**({ID})\**(?=[\s:.]|$)")
CITATION = re.compile(rf"\b({ID})\b")


def definitions(root: Path) -> tuple[dict[str, str], list[str]]:
    found: dict[str, str] = {}
    problems: list[str] = []
    for f in sorted((root / "docs" / "rules").glob("**/*.md")):
        if f.name == "README.md":
            continue
        rel = f.relative_to(root).as_posix()
        for n, line in enumerate(f.read_text(encoding="utf-8").splitlines(), 1):
            m = DEFINITION.match(line)
            if not m:
                continue
            if m.group(1) in found:
                problems.append(f"duplicate: {m.group(1)} at {rel}:{n} (first at {found[m.group(1)]})")
            else:
                found[m.group(1)] = f"{rel}:{n}"
    return found, problems


def citations(root: Path, patterns: list[str]) -> dict[str, list[str]]:
    found: dict[str, list[str]] = {}
    for pattern in patterns:
        for f in sorted(root.glob(pattern)):
            rel = f.relative_to(root).as_posix()
            if "/bin/" in rel or "/obj/" in rel:
                continue
            for n, line in enumerate(f.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
                for m in CITATION.finditer(line):
                    found.setdefault(m.group(1), []).append(f"{rel}:{n}")
    return found


def check(root: Path) -> list[str]:
    defined, problems = definitions(root)
    tests = citations(root, ["sim/Statecraft.Sim.Tests/**/*.cs"])
    anywhere = citations(root, ["sim/**/*.cs", "unity/Assets/**/*.cs", "content/**/*.yaml"])
    for rule, where in defined.items():
        if rule not in tests:
            problems.append(f"untested: {rule} ({where}) has no citing test in sim/Statecraft.Sim.Tests")
    for rule, where in anywhere.items():
        if rule not in defined:
            problems.append(f"dead: {rule} is cited at {where[0]} but no rule in docs/rules defines it")
    return problems


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(Path(__file__).resolve().parents[2]))
    problems = check(Path(ap.parse_args().root))
    for p in problems:
        print(p)
    if not problems:
        print("OK: every defined rule ID has a citing test and every cited ID is defined")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
