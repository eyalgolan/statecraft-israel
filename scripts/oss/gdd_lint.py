#!/usr/bin/env python3
"""GDD feature-section linter (#285). Checks every docs/gdd/*.md except TEMPLATE.md and README.md against the template
in docs/gdd/TEMPLATE.md, so the writer finds an incomplete spec at write time. One line per finding, exit 1 if any.

  missing-section  a template section is absent or empty
  no-done-when     the Done when section has no bullet
  tbd              TBD, TODO, FIXME or ??? is left in the text
  placeholder      a <...> placeholder from the template is left in the text
  no-unit          a number in Rules or Done when has no unit (put `(unitless)` on the line if it truly has none)
  no-source        a line with a year or a dated claim has no [S<n>] source, or cites an [S<n>] that Sources does not define,
                   or a source's link has no checked date (docs/CONTENT_POLICY.md)

HTML comments are ignored, so the template's guidance comments may stay in a copy.

Usage: uv run scripts/oss/gdd_lint.py [--root DIR] [FILE ...]"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

SECTIONS = ["Player story", "Rules", "Data touched", "Done when", "Out of scope", "Sources", "Open questions"]
UNIT_SECTIONS = {"Rules", "Done when"}
CLAIM_SECTIONS = {"Player story", "Rules", "Data touched", "Done when"}
UNITS = {"turn", "turns", "day", "days", "week", "weeks", "month", "months", "quarter", "quarters", "year", "years",
         "il", "percent", "point", "points", "pp", "person", "people", "persons", "event", "events", "hex", "hexes",
         "settlement", "settlements", "test", "tests", "line", "lines", "option", "options", "era", "eras", "seat",
         "seats", "member", "members", "second", "seconds", "ms", "minute", "minutes", "hour", "hours", "mb", "kb",
         "x", "per", "unit", "units", "command", "commands", "attention", "bit", "bits", "word", "words", "file", "files",
         "s", "warning", "warnings", "error", "errors", "brigade", "brigades", "immigrant", "immigrants", "vote", "votes",
         "percentage"}
SCALE = {"thousand", "million", "billion", "m", "bn", "k"}   # "1.6 million people": the unit is the word after
ORDINAL = {"st", "nd", "rd", "th"}
MONTHS = ("January|February|March|April|May|June|July|August|September|October|November|December"
          "|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sept|Sep|Oct|Nov|Dec")
YEAR = re.compile(r"\b(1[89]\d\d|20\d\d)s?\b")   # 1950 and the 1950s
ISO_DATE = re.compile(r"\b\d{4}-\d{2}-\d{2}\b")
DATE = re.compile(rf"\b\d{{1,2}} (?:{MONTHS})\b|\b(?:{MONTHS}) \d{{1,2}}\b")
TBD = re.compile(r"\b(TBD|TODO|FIXME)\b|\?\?\?")
PLACEHOLDER = re.compile(r"<(?!https?://[^\s<>]+>)[^<>\n]+>")   # <player role>, but not an autolink <https://...>
COMMENT = re.compile(r"<!--.*?-->", re.S)
SOURCE_REF = re.compile(r"\[S(\d+)\]")
SOURCE_DEF = re.compile(r"^\s*[-*]\s+\[S(\d+)\]\s+(.*)$")
NUMBER = re.compile(r"(?<![\w.])(\d+(?:[.,]\d+)?)(%?)(?:\s*([A-Za-z$]+))?")
STRIP = [re.compile(r"`[^`]*`"), re.compile(r"\[S\d+\]"), re.compile(r"\bR-[A-Z]{2,6}-\d{3}\b"),
         re.compile(r"https?://\S+"), ISO_DATE, re.compile(r"\b(?:1[89]|20)\d\d[–-]\d\d\b"),   # 1948-49
         re.compile(r"§\s*\d+(?:\.\d+)*"),
         re.compile(rf"\b\d{{1,2}} (?:{MONTHS})(?: \d{{4}})?\b"),
         re.compile(rf"\b(?:{MONTHS}) \d{{1,2}}(?:, \d{{4}})?\b"), YEAR, re.compile(r"^\s*(?:[-*]|\d+\.)\s+"),
         re.compile(r"#\d+\b"), re.compile(r"\bS\d+\b")]


def sections(text: str) -> dict[str, list[tuple[int, str]]]:
    out: dict[str, list[tuple[int, str]]] = {}
    current = None
    for n, line in enumerate(text.splitlines(), 1):
        m = re.match(r"^##\s+(.+?)\s*$", line)
        if m:
            current = m.group(1)
            out.setdefault(current, [])
        elif current is not None:
            out[current].append((n, line))
    return out


def uncomment(text: str) -> str:
    """Blanks out HTML comments, keeping the line count so findings keep their line numbers."""
    return COMMENT.sub(lambda c: "\n" * c.group(0).count("\n"), text)


def lint(path: Path, rel: str) -> list[str]:
    text = uncomment(path.read_text(encoding="utf-8"))
    found: list[str] = []
    secs = sections(text)
    for name in SECTIONS:
        body = [l for _, l in secs.get(name, []) if l.strip()]
        if not body:
            found.append(f"{rel}: missing-section: '{name}' is absent or empty")
    done = [l for _, l in secs.get("Done when", []) if re.match(r"^\s*[-*]\s+\S", l)]
    if secs.get("Done when") and not done:
        found.append(f"{rel}: no-done-when: 'Done when' has no bullet")
    for n, line in enumerate(text.splitlines(), 1):
        if TBD.search(line):
            found.append(f"{rel}:{n}: tbd: leftover TBD/TODO/FIXME/???")
        if PLACEHOLDER.search(re.sub(r"`[^`]*`", "", line)):
            found.append(f"{rel}:{n}: placeholder: a <...> placeholder from the template is left in")
    defined = {m.group(1) for _, l in secs.get("Sources", []) if (m := SOURCE_DEF.match(l)) and
               ("://" in m.group(2) or "/" in m.group(2))}
    for n, line in secs.get("Sources", []):
        m = SOURCE_DEF.match(line)
        if m and "://" in m.group(2) and not ISO_DATE.search(m.group(2)):
            found.append(f"{rel}:{n}: no-source: [S{m.group(1)}] has a link but no checked date (checked YYYY-MM-DD)")
    for name, lines in secs.items():
        for n, line in lines:
            if name in CLAIM_SECTIONS:
                refs = set(SOURCE_REF.findall(line))
                bad = refs - defined
                if bad:
                    found.append(f"{rel}:{n}: no-source: cites [S{sorted(bad)[0]}] but Sources does not define it with a link or path")
                if (YEAR.search(re.sub(r"`[^`]*`", "", line)) or DATE.search(line)) and not (refs & defined):
                    found.append(f"{rel}:{n}: no-source: a year or date with no [S<n>] source on the line")
            if name in UNIT_SECTIONS and "(unitless)" not in line:
                bare = line
                for rx in STRIP:
                    bare = rx.sub(" ", bare)
                for m in NUMBER.finditer(bare):
                    unit = (m.group(3) or "").lower()
                    if unit in SCALE:
                        after = re.match(r"\s*([A-Za-z]+)", bare[m.end():])
                        unit = after.group(1).lower() if after else ""
                    if m.group(2) == "%" or unit in UNITS or unit in ORDINAL:
                        continue
                    if unit in ("and", "to", "or", "of", "in") and re.match(r"\s*(?:and|to|or|of|in)\s+\d", bare[m.end(1):]):
                        continue   # a range or a share ("7 to 21 days", "61 of 120 seats"): the last number carries the unit
                    if bare[max(0, m.start() - 3):m.start()].strip() in ("IL", "$") or bare[max(0, m.start() - 1):m.start()] == "$":
                        continue
                    found.append(f"{rel}:{n}: no-unit: '{m.group(1)}' has no unit")
    return found


def files(root: Path, given: list[str]) -> list[Path]:
    if given:
        return [Path(g) for g in given]
    return [f for f in sorted((root / "docs" / "gdd").glob("*.md")) if f.name not in ("TEMPLATE.md", "README.md")]


def check(root: Path, given: list[str] | None = None) -> list[str]:
    out: list[str] = []
    for f in files(root, given or []):
        try:
            rel = f.resolve().relative_to(root.resolve()).as_posix()
        except ValueError:
            rel = f.as_posix()
        out += lint(f, rel)
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(Path(__file__).resolve().parents[2]))
    ap.add_argument("files", nargs="*")
    a = ap.parse_args()
    problems = check(Path(a.root), a.files)
    for p in problems:
        print(p)
    if not problems:
        print("OK: every GDD section has its parts, a done-when, units, sources and no TBD")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
