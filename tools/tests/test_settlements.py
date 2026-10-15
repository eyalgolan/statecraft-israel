"""Checks the committed map content: content/map/settlements_1948.yaml against content/map/hexes.json."""
import json
from pathlib import Path

import numpy as np
import pytest
import yaml12

import hexgrid as hg

ROOT = Path(__file__).resolve().parents[2]
TYPES = {"city", "town", "kibbutz", "moshav", "arab_town", "village"}
HEXES = json.loads((ROOT / "content" / "map" / "hexes.json").read_text())
TOWNS = yaml12.load((ROOT / "content" / "map" / "settlements_1948.yaml").read_text())["settlements"]
BY_QR = {(h["q"], h["r"]): h for h in HEXES["hexes"]}


def test_ids_unique_and_fields_valid():
    assert len({s["id"] for s in TOWNS}) == len(TOWNS) >= 20
    for s in TOWNS:
        assert s["type"] in TYPES, s["id"]
        assert (s["founded"] is None and s.get("pre_modern") is True) or s["founded"] <= 1948, s["id"]
        assert s["sources"] and all(src["url"].startswith("https://") and src["checked"] for src in s["sources"])


@pytest.mark.parametrize("s", TOWNS, ids=lambda s: s["id"])
def test_settlement_inside_bounds_on_land_hex(s):
    b = HEXES["bounds"]
    assert b["lon0"] <= s["lon"] <= b["lon1"] and b["lat0"] <= s["lat"] <= b["lat1"]
    q, r = hg.settlement_hex(s["lon"], s["lat"], HEXES)
    assert BY_QR[(q, r)]["land"], f"{s['id']} placed on {BY_QR[(q, r)]['biome']} hex ({q},{r})"


def test_shore_settlement_moves_to_nearest_land_neighbour():
    tiberias = next(s for s in TOWNS if s["id"] == "tiberias")
    x, y = hg.lonlat_to_km(tiberias["lon"], tiberias["lat"], HEXES["bounds"])
    (q0,), (r0,) = hg.point_to_hex(np.array([x]), np.array([y]), HEXES["hex_size_km"])
    assert BY_QR[(q0, r0)]["biome"] == "lake"          # its own hex is mostly Sea of Galilee
    q, r = hg.settlement_hex(tiberias["lon"], tiberias["lat"], HEXES)
    assert (q - q0, r - r0) in hg.NEIGHBOURS


POPULATION = yaml12.load((ROOT / "content" / "map" / "settlement_population_1945.yaml").read_text())["settlements"]


def test_population_rows_match_the_settlements_and_carry_sources():
    """settlement_population_1945.yaml (the settlement kits' footprints): one row per settlement id, a non-negative
    population or null, a status, and at least one source with an https url and a check date."""
    ids = [r["id"] for r in POPULATION]
    assert len(set(ids)) == len(ids)
    assert set(ids) == {s["id"] for s in TOWNS}
    for r in POPULATION:
        assert r["population"] is None or (isinstance(r["population"], int) and r["population"] >= 0), r["id"]
        assert r["population"] is not None or r.get("note"), f"{r['id']}: a missing figure needs a note"
        assert r["status"] in {"owner-verify", "fact-checked", "verified"}, r["id"]
        assert r["sources"] and all(src["url"].startswith("https://") and src["checked"] for src in r["sources"]), r["id"]
