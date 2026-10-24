#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""List every source in content/ with its check date and age (#330):
`uv run scripts/source_report.py [--root DIR] [--older-than DAYS] [--today YYYY-MM-DD]`.

A source is any mapping with `url` and `checked` anywhere in content/**/*.yaml, or a url in a plain `sources` list next
to a `checked` date. A YAML alias counts once. One line per source, oldest check first:
age in days, checked date, file, url. `--older-than N` keeps only sources checked more than N days ago. A non-https url
is marked `http`; a source with no valid check date is marked `NO-DATE`. A report, not a gate: exits 0 for an existing
content/ directory, even when empty. A root without content/ is a usage error: prints to stderr and exits 2. Reads only."""
import argparse
import sys
from datetime import date
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))  # tools/yaml12.py
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


def walk(node, out: list[dict], seen: set[int]) -> None:
    # A YAML alias (*name) is the same object as its anchor: count it once, where it is defined.
    if isinstance(node, (dict, list)):
        if id(node) in seen:
            return
        seen.add(id(node))
    if isinstance(node, dict):
        if "url" in node and "checked" in node:
            out.append(node)
        elif "checked" in node and isinstance(node.get("sources"), list):
            # a row with one check date and a plain list of urls (content/map/localities_1948.yaml)
            out += [{"url": u, "checked": node["checked"]} for u in node["sources"] if isinstance(u, str)]
        for v in node.values():
            walk(v, out, seen)
    elif isinstance(node, list):
        for v in node:
            walk(v, out, seen)


def to_date(v) -> date | None:
    if isinstance(v, date):
        return v
    try:
        return date.fromisoformat(str(v))
    except ValueError:
        return None


def collect(root: Path, today: date) -> list[dict]:
    rows = []
    for f in sorted((root / "content").rglob("*.yaml")):
        found: list[dict] = []
        walk(yaml12.load(f.read_text(encoding="utf-8")), found, set())
        for s in found:
            d = to_date(s["checked"])
            rows.append({"file": str(f.relative_to(root)), "url": str(s["url"]), "checked": d,
                         "age": (today - d).days if d else None})
    # no date first (worst), then oldest
    return sorted(rows, key=lambda r: (r["age"] is not None, -(r["age"] or 0), r["file"], r["url"]))


def render(rows: list[dict]) -> str:
    lines = []
    for r in rows:
        flags = (" NO-DATE" if r["checked"] is None else "") + ("" if r["url"].startswith("https://") else " http")
        age = "?" if r["age"] is None else str(r["age"])
        lines.append(f"{age:>5}d  {str(r['checked'] or '-'):<10}  {r['file']}  {r['url']}{flags}")
    lines.append(f"{len(rows)} sources")
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("--root", type=Path, default=ROOT)
    p.add_argument("--older-than", type=int, metavar="DAYS")
    p.add_argument("--today", type=date.fromisoformat, default=date.today())
    a = p.parse_args(argv)
    content = a.root / "content"
    if not content.is_dir():
        print(f"source_report: error: content directory not found: {content}", file=sys.stderr)
        return 2
    rows = collect(a.root, a.today)
    if a.older_than is not None:
        rows = [r for r in rows if r["age"] is None or r["age"] > a.older_than]
    print(render(rows))
    return 0


if __name__ == "__main__":
    sys.exit(main())
