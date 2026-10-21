# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pyyaml==6.0.3"]
# ///
"""Derive each corridor segment's hexes from its route's traced line (GDD Phase 4 PR 2, #786).

Usage: uv run tools/corridor_hexes.py [--check]
Reads content/corridors.yaml (hand-written: each corridor's route and its segments, each ending at a route waypoint),
content/map/routes_1948.yaml (the waypoints) and content/map/route_paths_1948.yaml (the traced lines), and rewrites the
`hexes: [...]` of every segment line in corridors.yaml in place, so its comments survive. A segment runs along the
traced line from the point nearest the previous waypoint (the route's first waypoint for the first segment) to the
point nearest its own `to` waypoint; its hexes are the hexes that stretch crosses, in order (tools/hexgrid.py's grid),
sampled every SAMPLE_KM; a point in a sea hex counts in its nearest land neighbour, as a shore town does. Consecutive
segments share the waypoint's hex. --check writes nothing and exits 1 when the committed hexes differ from a fresh
derivation (tools/tests/test_corridor_hexes.py runs the same check in CI).
"""
from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))  # under python -I the script's folder is not on sys.path
import hexgrid as hg  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
CORRIDORS = ROOT / "content" / "corridors.yaml"
ROUTES = ROOT / "content" / "map" / "routes_1948.yaml"
PATHS = ROOT / "content" / "map" / "route_paths_1948.yaml"
HEXES = ROOT / "content" / "map" / "hexes.json"

# A tenth of a kilometre against hexes 8.7 km apart: no hex the line crosses is stepped over.
SAMPLE_KM = 0.1
SEGMENT_LINE = re.compile(r"^(\s*- \{ to: )(?P<to>[^,]+)(, hexes: )\[.*\]( \})\s*$")


def _project(line_km: list[tuple[float, float]], p: tuple[float, float], start: float) -> float:
    """The arc length (km) of the point on the line nearest p, searching from arc length `start` on, so a route that
    passes near one waypoint twice splits in order."""
    best, best_d, s = start, math.inf, 0.0
    for (ax, ay), (bx, by) in zip(line_km, line_km[1:]):
        seg = math.hypot(bx - ax, by - ay)
        if seg == 0.0:
            continue
        t = max(0.0, min(1.0, ((p[0] - ax) * (bx - ax) + (p[1] - ay) * (by - ay)) / seg ** 2))
        at = s + t * seg
        d = math.hypot(ax + t * (bx - ax) - p[0], ay + t * (by - ay) - p[1])
        if at >= start and d < best_d:
            best, best_d = at, d
        s += seg
    return best


def _point_at(line_km: list[tuple[float, float]], at: float) -> tuple[float, float]:
    s = 0.0
    for (ax, ay), (bx, by) in zip(line_km, line_km[1:]):
        seg = math.hypot(bx - ax, by - ay)
        if seg > 0.0 and s + seg >= at:
            t = (at - s) / seg
            return ax + t * (bx - ax), ay + t * (by - ay)
        s += seg
    return line_km[-1]


def derive(corridors: list[dict], routes: list[dict], paths: list[dict], grid: dict) -> dict[str, list[list[list[int]]]]:
    """corridor id -> one [[q, r], ...] list per segment. Raises ValueError naming the corridor on a bad row."""
    b, size = grid["bounds"], grid["hex_size_km"]
    land = {(h["q"], h["r"]) for h in grid["hexes"] if h["land"]}
    by_route = {r["id"]: r for r in routes}
    by_path = {p["id"]: p for p in paths}
    out = {}
    for c in corridors:
        cid, rid = c["id"], c.get("route")
        if rid not in by_route or rid not in by_path:
            raise ValueError(f"{cid}: route {rid!r} is not traced in routes_1948.yaml and route_paths_1948.yaml")
        via = {w["name"]: hg.lonlat_to_km(w["lon"], w["lat"], b) for w in by_route[rid]["via"]}
        line = [hg.lonlat_to_km(lon, lat, b) for lon, lat in by_path[rid]["points"]]
        ends = [c["segments"][i]["to"] for i in range(len(c["segments"]))]
        names = [w["name"] for w in by_route[rid]["via"]]
        if any(e not in via for e in ends) or ends[-1] != names[-1]:
            raise ValueError(f"{cid}: each segment ends at a waypoint of {rid}, the last at {names[-1]!r}")
        at = _project(line, via[names[0]], 0.0)
        segments = []
        for e in ends:
            end = _project(line, via[e], at)
            if end <= at:
                raise ValueError(f"{cid}: the segment to {e!r} has no length (waypoints out of order?)")
            n = max(2, int(math.ceil((end - at) / SAMPLE_KM)) + 1)
            pts = [_point_at(line, at + (end - at) * i / (n - 1)) for i in range(n)]
            qs, rs = hg.point_to_hex(np.array([p[0] for p in pts]), np.array([p[1] for p in pts]), size)
            hexes: list[list[int]] = []
            for p, q, r in zip(pts, qs, rs):
                if (q, r) not in land:
                    # A shore point in a hex the grid counts as sea (the Tel Aviv-Netanya road along the coast) belongs to
                    # the land neighbour with the nearest centre: hexgrid.settlement_hex's rule for shore towns.
                    options = [(q + dq, r + dr) for dq, dr in hg.NEIGHBOURS if (q + dq, r + dr) in land]
                    if not options:
                        raise ValueError(f"{cid}: the segment to {e!r} crosses hex q={q} r={r}, with no land hex next to it")
                    q, r = min(options, key=lambda h: math.dist(hg.hex_center_km(*h, size), p))
                if not hexes or hexes[-1] != [q, r]:
                    hexes.append([q, r])
            segments.append(hexes)
            at = end
        out[cid] = segments
    return out


def _flow(hexes: list[list[int]]) -> str:
    return "[" + ", ".join(f"[{q}, {r}]" for q, r in hexes) + "]"


def render(text: str, derived: dict[str, list[list[list[int]]]]) -> str:
    """corridors.yaml with every segment line's hexes replaced, in file order."""
    queue = [(cid, i) for cid, segs in derived.items() for i in range(len(segs))]
    lines, k = [], 0
    for raw in text.splitlines(keepends=True):
        m = SEGMENT_LINE.match(raw.rstrip("\n"))
        if m:
            if k >= len(queue):
                raise ValueError("corridors.yaml has more segment lines than segments")
            cid, i = queue[k]
            k += 1
            raw = f"{m.group(1)}{m.group('to')}{m.group(3)}{_flow(derived[cid][i])}{m.group(4)}\n"
        lines.append(raw)
    if k != len(queue):
        raise ValueError("every segment must be one line: '- { to: NAME, hexes: [...] }'")
    return "".join(lines)


def build() -> tuple[str, str]:
    """(current text, the text with fresh hexes)."""
    text = CORRIDORS.read_text(encoding="utf-8")
    doc = yaml12.load(text)
    derived = derive(doc["corridors"], yaml12.load(ROUTES.read_text(encoding="utf-8"))["routes"],
                     yaml12.load(PATHS.read_text(encoding="utf-8"))["routes"], json.loads(HEXES.read_text()))
    return text, render(text, derived)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--check", action="store_true", help="fail if the committed hexes are stale; write nothing")
    args = ap.parse_args(argv)
    old, new = build()
    if args.check:
        if old != new:
            print("content/corridors.yaml: hexes are stale; run uv run tools/corridor_hexes.py", file=sys.stderr)
            return 1
        return 0
    CORRIDORS.write_text(new, encoding="utf-8")
    print(f"wrote {CORRIDORS.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
