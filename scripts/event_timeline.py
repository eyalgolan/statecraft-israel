#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyyaml==6.0.3"]
# ///
"""Print every event sorted by window (#324): `uv run scripts/event_timeline.py [--root DIR] [--status draft]`.

One line per event: from, to, era, anchor, status, id, tags. An event with no `to` shows `open`. A final line counts
events per status, so unreviewed ones stand out. Reads content/events/*.yaml and content/eras.yaml; writes nothing."""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))  # tools/yaml12.py
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
# The sim's defaults for a missing key (GameEvent in sim/Statecraft.Sim/Content.cs; a test keeps them equal).
DEFAULT_ANCHOR = "soft"
DEFAULT_STATUS = "draft"
STATUS_CHOICES = ("draft", "fact-checked", "historian-reviewed", "sensitivity-reviewed")


def load(root: Path) -> list[dict]:
    eras = yaml12.load((root / "content/eras.yaml").read_text(encoding="utf-8"))["eras"]
    rows = []
    for f in sorted((root / "content/events").glob("*.yaml")):
        e = yaml12.load(f.read_text(encoding="utf-8"))
        start = str(e["from"])
        era = next((x["id"] for x in eras
                    if str(x["from"]) <= start and (not x.get("to") or start < str(x["to"]))), "-")
        rows.append({"from": start, "to": str(e["to"]) if e.get("to") else "open", "era": era,
                     "anchor": e.get("anchor", DEFAULT_ANCHOR), "status": e.get("status", DEFAULT_STATUS),
                     "id": e["id"], "tags": ",".join(e.get("tags") or [])})
    return sorted(rows, key=lambda r: (r["from"], r["id"]))


def render(rows: list[dict]) -> str:
    lines = [f"{r['from']}  {r['to']:<10}  {r['era']:<10}  {r['anchor']:<4}  {r['status']:<20}  {r['id']}"
             + (f"  [{r['tags']}]" if r["tags"] else "") for r in rows]
    counts: dict[str, int] = {}
    for r in rows:
        counts[r["status"]] = counts.get(r["status"], 0) + 1
    summary = ", ".join(f"{n} {s}" for s, n in sorted(counts.items()))
    lines.append(f"{len(rows)} events: {summary}" if summary else "0 events")
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    p.add_argument("--root", type=Path, default=ROOT)
    p.add_argument("--status", choices=STATUS_CHOICES, help="only events with this review status")
    a = p.parse_args(argv)
    rows = load(a.root)
    if a.status:
        rows = [r for r in rows if r["status"] == a.status]
    print(render(rows))
    return 0


if __name__ == "__main__":
    sys.exit(main())
