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


def test_order_targets_get_their_own_rows():
    # R-ENEMY-005 (owner, 2 November 2026, #2087, option 1): an order target is not an anchor, so it has no anchor row.
    hexes = yaml12.load((MAP / "unit_anchor_hexes.yaml").read_text())
    targets = {(r["id"], r["target"]): (r["q"], r["r"]) for r in hexes["order_targets"]}
    assert targets[("kiryati", 0)] == (-3, 21)   # Ramle, the ten_days card's order
    assert not any(r["id"] == "kiryati" and r["anchor"] == 1 for r in hexes["anchors"])
