"""Checks GDD v0.2 map locations against the authored hex grid."""
import json
from pathlib import Path

import yaml12

import hexgrid as hg


ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
LOCATIONS = yaml12.load((MAP / "locations_1948.yaml").read_text())["locations"]


def test_sourced_coordinates_bind_to_the_authored_hexes():
    for location in LOCATIONS:
        assert hg.settlement_hex(location["lon"], location["lat"], HEXES) == (location["q"], location["r"]), location["id"]


def test_issue_1008_roster_and_separate_adjacent_positions():
    by_id = {location["id"]: location for location in LOCATIONS}
    assert set(by_id) == {
        "negba", "hulda", "nir_am", "latrun_fort", "iraq_suwaydan_fort", "shaar_hagai",
        "ad_halom_bridge", "shaar_haaliyah", "jerusalem", "east_jerusalem_old_city",
    }
    assert by_id["iraq_suwaydan_fort"]["id"] != "negba"
    assert by_id["ad_halom_bridge"]["id"] != "isdud"
    assert by_id["jerusalem"]["managed_city"] is True
    assert by_id["east_jerusalem_old_city"]["managed_city"] is False
