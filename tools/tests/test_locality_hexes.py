"""content/map/locality_hexes.yaml is the committed output of tools/locality_hexes.py and must stay in sync: the sim places
gdd-0.3 region members that are localities (owner, 2 November 2026: "Accept localities") on these hexes instead of
projecting lon/lat itself (docs/determinism.md)."""
import json
import re
from pathlib import Path

import pytest
import yaml12

import locality_hexes as lh

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "content" / "map"
HEXES = json.loads((MAP / "hexes.json").read_text())
LAND = {(h["q"], h["r"]) for h in HEXES["hexes"] if h["land"]}


def rows():
    return yaml12.load((MAP / "locality_hexes.yaml").read_text())["localities"]


def test_committed_file_is_up_to_date():
    assert (MAP / "locality_hexes.yaml").read_text() == lh.render(HEXES, lh.places()), "rerun: uv run tools/locality_hexes.py"


def test_every_place_has_one_land_hex_in_input_order():
    got = rows()
    assert [r["id"] for r in got] == [p["id"] for p in lh.places()]
    assert len({r["id"] for r in got}) == len(got)
    assert not [r for r in got if (r["q"], r["r"]) not in LAND]


def test_known_places_are_pinned_literally():
    at = {r["id"]: (r["q"], r["r"]) for r in rows()}
    assert at["tarshiha"] == (10, 6)        # a Galilee member (localities_1948.yaml, 33.009 N, 35.2683 E)
    assert at["gesher"] == (11, 12)         # region_places_1948.yaml: Old Gesher, a Valleys member
    assert at["nitzanim"] == (-8, 27)       # region_places_1948.yaml: the 1943 site, a Southern Coast member


def test_a_repeated_id_is_refused():
    with pytest.raises(ValueError, match="duplicate place id x"):
        lh.render(HEXES, [{"id": "x", "lon": 35.2683, "lat": 33.009}, {"id": "x", "lon": 35.2683, "lat": 33.009}])


def test_region_places_are_sourced_drafts_with_checked_dates():
    places = yaml12.load((MAP / "region_places_1948.yaml").read_text(encoding="utf-8"))["places"]
    assert [p["id"] for p in places] == ["gesher", "mishmar_haemek", "nitzanim"]
    for p in places:
        assert p["status"] == "draft" and p["coordinate_source"] and p["notes"], p["id"]
        assert p["sources"], p["id"]
        for s in p["sources"]:
            assert s["url"].startswith("https://"), p["id"]
            assert re.fullmatch(r"\d{4}-\d{2}-\d{2}", str(s["checked"])), p["id"]
