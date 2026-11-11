"""content/map/water.yaml (#16): the dated water bodies the map draws beyond the heightmap's own sea and lakes."""
import re
from datetime import date
from pathlib import Path

import numpy as np

import borders_bake as bb
import terrain_bake as tb
import yaml12

ROOT = Path(__file__).resolve().parents[2]


def doc() -> dict:
    return yaml12.load(tb.WATER.read_text())


def dead_sea() -> dict:
    return next(b for b in doc()["bodies"] if b["id"] == "dead_sea_1948")


def test_every_body_is_dated_sourced_and_drawable():
    bodies = doc()["bodies"]
    assert len({b["id"] for b in bodies}) == len(bodies)
    for b in bodies:
        assert set(b) >= {"id", "from", "to", "kind", "level_m", "box", "sources", "gaps"}, b["id"]
        start = date.fromisoformat(b["from"])
        assert b["to"] is None or date.fromisoformat(b["to"]) > start, b["id"]
        assert b["kind"] == "level" and isinstance(b["level_m"], float), b["id"]
        (lat0, lat1), (lon0, lon1) = b["box"]["lat"], b["box"]["lon"]
        assert lat0 < lat1 and lon0 < lon1, b["id"]
        assert b["sources"] and all(s["url"].startswith("https://") and date.fromisoformat(s["checked"]) and s["note"]
                                    for s in b["sources"]), b["id"]
        assert not any(k.endswith("_he") for k in b), "no Hebrew in a data file the player does not read"


def test_the_dead_sea_is_at_the_level_the_1946_sheet_prints_everywhere_the_repo_keeps_it():
    b = dead_sea()
    assert b["level_m"] == -392.0, "Survey of Palestine 1:250,000 (1946): -392 (M.W.L. 1938)"
    assert any("-392 (M.W.L. 1938)" in s["note"] for s in b["sources"])
    assert bb.DEAD_SEA_1948_M == b["level_m"]
    digitise = (ROOT / "tools/borders_digitise.py").read_text()      # scipy: read, not imported
    assert float(re.search(r"^DEAD_SEA_1948_M = (-?[0-9.]+)$", digitise, re.M).group(1)) == b["level_m"]
    cs = (ROOT / "unity/Assets/Scripts/Map/MapBuilder.cs").read_text()
    assert float(re.search(r"\bDeadSea1948M = (-?[0-9.]+)f", cs).group(1)) == b["level_m"], "MapBuilder.DeadSea1948M"
    kit = (ROOT / "unity/Assets/Scripts/Map/Kits/KitGround.cs").read_text()
    assert "MapBuilder.DeadSea1948M" in kit, "KitGround.Water keeps lots and trees out of the 1948 water"


def test_the_dead_sea_box_is_the_borders_bake_box():
    src = (ROOT / "tools/borders_bake.py").read_text()
    m = re.search(r"\(lat >= ([0-9.]+)\) & \(lat < ([0-9.]+)\)\); cols = np.flatnonzero\(\(lon >= ([0-9.]+)\) & \(lon <= ([0-9.]+)\)\)", src)
    lat0, lat1, lon0, lon1 = map(float, m.groups())
    assert dead_sea()["box"] == {"lat": [lat0, lat1], "lon": [lon0, lon1]}


def test_the_game_opens_inside_one_water_epoch():
    """The game's span (docs/GDD.md: 15 May 1948 to 15 January 1952) holds one set of bodies, so one coast.png."""
    gdd = (ROOT / "docs/GDD.md").read_text()
    assert "the span is 15 May 1948 to 15 January 1952" in gdd and tb.MAP_DAY == date(1948, 5, 15)
    first = [b["id"] for b in tb.water_bodies(tb.MAP_DAY)]
    assert first == ["dead_sea_1948"]
    for day in (date(1949, 7, 20), date(1952, 1, 15)):
        assert [b["id"] for b in tb.water_bodies(day)] == first
    assert tb.water_bodies(date(1948, 5, 14)) == []


def meta_for(h: int, w: int) -> dict:
    return {"lon0": 35.0, "lon1": 36.0, "lat0": 31.0, "lat1": 32.0, "width": w, "height": h}


def test_a_level_fill_reaches_only_ground_joined_to_the_water_inside_its_box():
    elev = np.full((11, 11), 100.0, dtype=np.float32)        # 0.1 degree a sample; row 0 = 32.0 N, column 0 = 35.0 E
    elev[4:7, 4:7] = -420.0                                    # the lake
    elev[4:7, 7:9] = -400.0                                    # its 1948 shore, below the level, joined to it
    elev[1, 1] = -400.0                                        # a pit below the level, not joined
    elev[5, 9:11] = -400.0                                     # joined: 35.9 E on the box's edge, 36.0 E east of it
    body = {"id": "t", "kind": "level", "level_m": -392.0, "box": {"lat": [31.1, 31.9], "lon": [35.1, 35.9]}}
    lake = elev == -420.0
    fill = tb.level_fill(elev, meta_for(11, 11), body, lake)
    assert fill[4:7, 4:9].all()
    assert not fill[1, 1], "a pit the water does not reach stays land"
    assert fill[5, 9] and not fill[5, 10], "the box's edge is in it, nothing outside it is"
    assert fill.sum() == 9 + 6 + 1


def test_the_1948_dead_sea_changes_the_map_water_only_inside_its_box():
    elev, meta = tb.load_heightmap(tb.MAP_DIR)
    dx, dy = tb.spacing_m(meta)
    today, fill = tb.water_mask(elev, dx, dy), tb.map_water(elev, meta, [dead_sea()])
    assert (fill >= today).all(), "a union: today's water (the ponds too) stays water"
    h, w = elev.shape
    rows, cols = np.nonzero(fill != today)
    lat = meta["lat1"] - rows * (meta["lat1"] - meta["lat0"]) / (h - 1)
    lon = meta["lon0"] + cols * (meta["lon1"] - meta["lon0"]) / (w - 1)
    assert lat.min() >= 30.85 and lat.max() <= 31.9 and lon.min() >= 35.25 and lon.max() <= 35.7
    km2 = len(rows) * dx * dy / 1e6
    # 290.1 km2 when written: 149.6 north of 31.30 N, 137.1 in 31.06-31.30 (the strait), 3.4 south of 31.06 (water.yaml gaps)
    assert 285 < km2 < 295, f"{km2:.1f} km2 of new water"
    south = (lat < 31.06).sum() * dx * dy / 1e6
    assert 3.0 < south < 4.0, f"{south:.1f} km2 south of 31.06 N: water.yaml's gap says about 3.4 km2"
