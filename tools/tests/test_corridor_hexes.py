"""content/corridors.yaml's segment hexes are the committed output of tools/corridor_hexes.py (GDD Phase 4 PR 2, #786;
the plan's Review focus 3: the sim's hexes and the traced route agree)."""
import json
from pathlib import Path

import pytest

import corridor_hexes as ch
import yaml12

ROOT = Path(__file__).resolve().parents[2]
GRID = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())
ROUTES = yaml12.load((ROOT / "content" / "map" / "routes_1948.yaml").read_text(encoding="utf-8"))["routes"]
PATHS = yaml12.load((ROOT / "content" / "map" / "route_paths_1948.yaml").read_text(encoding="utf-8"))["routes"]
TEXT = (ROOT / "content" / "corridors.yaml").read_text(encoding="utf-8")
CORRIDORS = yaml12.load(TEXT)["corridors"]


def test_committed_hexes_are_up_to_date():
    old, new = ch.build()
    assert old == new, "rerun: uv run tools/corridor_hexes.py"


def test_moving_one_traced_point_without_rederiving_fails():
    # Shift the Burma Road's middle point by about 9 km east (one hex): the derivation no longer matches the file.
    moved = [dict(p) for p in PATHS]
    burma = next(p for p in moved if p["id"] == "burma_road")
    pts = [list(x) for x in burma["points"]]
    mid = len(pts) // 2
    pts[mid] = [pts[mid][0] + 0.09, pts[mid][1]]
    burma["points"] = pts
    assert ch.render(TEXT, ch.derive(CORRIDORS, ROUTES, moved, GRID)) != TEXT


def test_every_segment_is_land_and_steps_one_hex_at_a_time():
    land = {(h["q"], h["r"]) for h in GRID["hexes"] if h["land"]}
    steps = {(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)}
    for c in CORRIDORS:
        prev = None
        for seg in c["segments"]:
            assert seg["hexes"], c["id"]
            for q, r in seg["hexes"]:
                assert (q, r) in land, (c["id"], q, r)
            chain = ([prev] if prev else []) + [tuple(h) for h in seg["hexes"]]
            for a, b in zip(chain, chain[1:]):
                assert a == b or (b[0] - a[0], b[1] - a[1]) in steps, (c["id"], a, b)
            prev = tuple(seg["hexes"][-1])


def test_a_segment_must_end_at_a_waypoint_of_its_route():
    bad = [dict(CORRIDORS[0], segments=[{"to": "Nowhere", "hexes": []}])]
    with pytest.raises(ValueError, match="ends at a waypoint"):
        ch.derive(bad, ROUTES, PATHS, GRID)
