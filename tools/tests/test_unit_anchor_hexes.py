"""content/map/unit_anchor_hexes.yaml is the committed output of tools/unit_anchor_hexes.py and must stay in sync: the sim
places the starting forces (R-UNIT-006) on these hexes instead of projecting lon/lat itself (docs/determinism.md)."""
import json
from pathlib import Path

import yaml12

import unit_anchor_hexes as uah

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
UNITS = yaml12.load((MAP / "units_1948.yaml").read_text(encoding="utf-8"))["units"]


def test_committed_file_is_up_to_date():
    assert (MAP / "unit_anchor_hexes.yaml").read_text() == uah.render(HEXES, UNITS), "rerun: uv run tools/unit_anchor_hexes.py"


def test_two_known_anchors_are_pinned_literally():
    rows = {(r["id"], r["anchor"]): (r["q"], r["r"]) for r in yaml12.load((MAP / "unit_anchor_hexes.yaml").read_text())["anchors"]}
    assert rows[("carmeli", 0)] == (5, 8)   # Haifa (#743, owner, 20 October 2026)
    assert rows[("kiryati", 0)] == (-3, 19)   # Tel Aviv, the hex settlement_hexes.yaml gives tel_aviv
