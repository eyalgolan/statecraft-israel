import json
from pathlib import Path

import numpy as np
import pytest

import hexgrid as hg
import heightmap_codec as hc

ROOT = Path(__file__).resolve().parents[2]


def write_uniform_precip(d, meta, mm, cell=0.01):
    """Synthetic precip clip covering meta's bounds (plus one cell) at a constant value."""
    w = round((meta["lon1"] - meta["lon0"]) / cell) + 2
    h = round((meta["lat1"] - meta["lat0"]) / cell) + 2
    hg.write_precip(np.full((h, w), mm), meta["lon0"] - cell, meta["lat1"] + cell, cell, d, {"source": "synthetic", "attribution": "chelsa-credit"})
    write_no_stations(d)


def write_no_stations(d):
    (Path(d) / "precip_stations.json").write_text(json.dumps({"attribution": "ims-credit", "stations": []}))


def test_hex_center_round_trips_through_point_to_hex():
    for q, r in [(0, 0), (3, 5), (-2, 7), (10, 40)]:
        x, y = hg.hex_center_km(q, r, 5.0)
        assert hg.point_to_hex(np.array([x]), np.array([y]), 5.0) == ([q], [r])


def test_neighbouring_centres_are_one_hex_width_apart():
    x0, y0 = hg.hex_center_km(0, 0, 5.0)
    x1, y1 = hg.hex_center_km(1, 0, 5.0)
    assert x1 - x0 == pytest.approx(5.0 * 3 ** 0.5) and y1 == y0


@pytest.mark.parametrize("stats, lat, biome", [
    ({"sea_share": 0.6, "median": 0.0, "water_share": 0.0, "precip_mm": 550.0}, 32.0, "sea"),
    ({"sea_share": 0.0, "water_share": 0.7, "median": -212.0, "precip_mm": 400.0}, 32.8, "lake"),
    ({"sea_share": 0.0, "median": -250.0, "water_share": 0.0, "precip_mm": 300.0}, 32.3, "valley"),
    ({"sea_share": 0.0, "median": 780.0, "water_share": 0.0, "precip_mm": 600.0}, 31.78, "mountains"),
    ({"sea_share": 0.0, "median": 900.0, "water_share": 0.0, "precip_mm": 80.0}, 30.6, "mountains"),  # rule 4 before 5
    ({"sea_share": 0.0, "median": 290.0, "water_share": 0.0, "precip_mm": 150.0}, 31.24, "desert"),
    ({"sea_share": 0.0, "median": 300.0, "water_share": 0.0, "precip_mm": 199.9}, 31.6, "desert"),
    ({"sea_share": 0.0, "median": 300.0, "water_share": 0.0, "precip_mm": 200.0}, 31.0, "hills"),  # no latitude rule
    ({"sea_share": 0.0, "median": 400.0, "water_share": 0.0, "precip_mm": 700.0}, 32.7, "hills"),
    ({"sea_share": 0.1, "median": 25.0, "water_share": 0.0, "precip_mm": 550.0, "coast_km": 4.4}, 32.08, "coastal_plain"),
    # rule 7 against rule 8: the same hex is coastal_plain up to COASTAL_PLAIN_MAX_KM from the shore and lowland beyond it
    ({"sea_share": 0.0, "median": 70.4, "water_share": 0.0, "precip_mm": 450.0, "coast_km": 25.0}, 32.8, "coastal_plain"),
    ({"sea_share": 0.0, "median": 70.4, "water_share": 0.0, "precip_mm": 450.0, "coast_km": 25.1}, 32.8, "lowland"),
    ({"sea_share": 0.0, "median": 70.4, "water_share": 0.0, "precip_mm": 450.0, "coast_km": float("inf")}, 32.8, "lowland"),
    ({"sea_share": 0.1, "median": 60.0, "water_share": 0.0, "precip_mm": 120.0}, 31.3, "desert"),
    # Dead Sea basin and northern Arava (below 0 m, south of the Dead Sea's north tip 31.77) are hyper-arid desert
    ({"sea_share": 0.0, "median": -380.0, "water_share": 0.1, "precip_mm": 90.0}, 31.5, "desert"),
    ({"sea_share": 0.0, "median": -50.0, "water_share": 0.0, "precip_mm": 300.0}, 31.76, "desert"),
    ({"sea_share": 0.0, "median": -300.0, "water_share": 0.0, "precip_mm": 150.0}, 31.78, "valley"),
    ({"sea_share": 0.0, "water_share": 0.8, "median": -430.0, "precip_mm": 60.0}, 31.5, "lake"),  # lake still wins
])
def test_biome_rules(stats, lat, biome):
    stats = {"coast_km": 4.4, **stats}   # the rules before 7 do not look at the distance
    assert hg.classify(stats, lat) == biome


def test_coastal_plain_threshold_is_a_pure_function_of_distance():
    base = {"sea_share": 0.0, "median": 100.0, "water_share": 0.0, "precip_mm": 500.0}
    for km in (0.0, 10.0, hg.COASTAL_PLAIN_MAX_KM):
        assert hg.classify({**base, "coast_km": km}, 32.0) == "coastal_plain"
    for km in (hg.COASTAL_PLAIN_MAX_KM + 0.1, 60.0):
        assert hg.classify({**base, "coast_km": km}, 32.0) == "lowland"
    assert 15.0 <= hg.COASTAL_PLAIN_MAX_KM <= 40.0   # the coastal plain is roughly 15 to 40 km wide


def test_coast_points_are_the_shore_of_sea_hexes_only():
    sea = np.zeros((6, 6), dtype=bool)
    sea[:, :3] = True                       # west half sea; its shore is column 2
    sea[0, 5] = True                        # a lone 0 m pixel of inland no-data, in a land hex
    q = np.zeros(36, dtype=int); r = np.zeros(36, dtype=int)
    q[np.arange(36) % 6 >= 3] = 1           # hex (0, 0) holds the west half, hex (1, 0) the east half
    pts = hg.coast_points_km(sea, q, r, {(0, 0)}, 5.0, 5.0)
    assert sorted(set(pts[:, 0])) == [2.0]  # x = col 2 / 5 * 5 km
    assert len(pts) == 6                    # one shore pixel per row; the inland speck is dropped
    assert hg.distance_to_coast_km(5.0, 2.0, pts) == pytest.approx(3.0)
    assert hg.distance_to_coast_km(5.0, 2.0, np.zeros((0, 2))) == float("inf")


def test_water_share_counts_flat_water_below_minus_150():
    v = np.r_[np.full(60, -430.0), np.linspace(-400, 200, 40)]   # lake surface + cliffs
    assert hg.water_share(v) == pytest.approx(0.6, abs=0.02)
    assert hg.water_share(np.full(10, 300.0)) == 0.0


def test_build_hexes_on_synthetic_map(tmp_path):
    n = 201
    elev = np.zeros((n, n), dtype="float32")
    elev[:, n // 2:] = 300.0                       # west half sea, east half land at 300 m
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0,
            "attribution": "dem-credit"}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    hc.write(tmp_path, np.round(elev / 300.0 * 65535).astype("<u2"))
    write_uniform_precip(tmp_path, meta, 500.0)
    out = hg.build(tmp_path, 5.0, precip_dir=tmp_path)
    assert out["layout"] == "pointy" and out["hex_size_km"] == 5.0
    assert set(out["hexes"][0]) == {"q", "r", "lon", "lat", "elev_m", "land", "biome"}
    west = [h for h in out["hexes"] if h["lon"] < 35.2]
    east = [h for h in out["hexes"] if h["lon"] > 35.3]
    assert west and all(h["biome"] == "sea" and not h["land"] for h in west)
    assert east and all(h["biome"] == "hills" and h["land"] for h in east)
    assert len({(h["q"], h["r"]) for h in out["hexes"]}) == len(out["hexes"])


def synthetic_inputs(d):
    n = 201
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0, "attribution": "dem-credit"}
    (d / "heightmap.json").write_text(json.dumps(meta))
    hc.write(d, np.full((n, n), 65535, dtype="<u2"))
    write_uniform_precip(d, meta, 500.0)


def test_build_credits_the_dem_chelsa_and_ims_inputs(tmp_path):
    synthetic_inputs(tmp_path)
    out = hg.build(tmp_path, 5.0, precip_dir=tmp_path)
    assert out.get("attribution") == ["dem-credit", "chelsa-credit", "ims-credit"]


@pytest.mark.parametrize("name", ["heightmap.json", "precip_bio12.json", "precip_stations.json"])
def test_build_fails_loudly_when_an_input_has_no_attribution(tmp_path, name):
    synthetic_inputs(tmp_path)
    doc = json.loads((tmp_path / name).read_text())
    doc.pop("attribution")
    (tmp_path / name).write_text(json.dumps(doc))
    with pytest.raises(ValueError, match=f"{name}.*attribution"):
        hg.build(tmp_path, 5.0, precip_dir=tmp_path)


def test_committed_hexes_credit_the_committed_inputs():
    attribution = json.loads((ROOT / "content" / "map" / "hexes.json").read_text()).get("attribution")
    dem = json.loads((hg.MAP_DIR / "heightmap.json").read_text())["attribution"]
    chelsa = json.loads((ROOT / "content" / "map" / "precip_bio12.json").read_text())["attribution"]
    ims = json.loads((ROOT / "content" / "map" / "precip_stations.json").read_text())["attribution"]
    assert attribution == [dem, chelsa, ims]
    assert dem.startswith("produced using Copernicus WorldDEM-30") and "CHELSA" in chelsa and "Israel Meteorological Service" in ims


def test_precip_round_trips_and_samples_bilinearly(tmp_path):
    mm = np.array([[100.0, 200.0], [300.0, np.nan]])          # row 0 = north; NaN = no data (sea)
    hg.write_precip(mm, 35.0, 32.0, 0.5, tmp_path, {"source": "synthetic", "attribution": "chelsa-credit"})
    pr = hg.load_precip(tmp_path)
    assert np.isnan(pr["mm"][1, 1]) and pr["mm"][0, 1] == 200.0
    assert pr["meta"]["source"] == "synthetic" and pr["meta"]["units"] == "mm/year"
    assert hg.precip_at(35.25, 31.75, pr) == pytest.approx(100.0)   # centre of the NW cell
    assert hg.precip_at(35.5, 31.75, pr) == pytest.approx(150.0)    # halfway between NW and NE centres
    assert hg.precip_at(35.25, 31.5, pr) == pytest.approx(200.0)    # halfway between NW and SW centres


def test_build_marks_dry_land_desert_wherever_it_is(tmp_path):
    n = 201
    elev = np.full((n, n), 300.0, dtype="float32")
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0,
            "attribution": "dem-credit"}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    hc.write(tmp_path, np.round(elev / 300.0 * 65535).astype("<u2"))
    cell = 0.01
    w = round(0.5 / cell) + 2; h = round(0.5 / cell) + 2
    mm = np.full((h, w), 600.0); mm[h // 2:, :] = 120.0          # dry southern half, far north of any old lat rule
    hg.write_precip(mm, 35.0 - cell, 32.7 + cell, cell, tmp_path, {"source": "synthetic", "attribution": "chelsa-credit"})
    write_no_stations(tmp_path)
    out = hg.build(tmp_path, 5.0, precip_dir=tmp_path)
    north = [x for x in out["hexes"] if x["lat"] > 32.55]
    south = [x for x in out["hexes"] if x["lat"] < 32.35]
    assert north and all(x["biome"] == "hills" for x in north)
    assert south and all(x["biome"] == "desert" for x in south)


def test_hex_precip_averages_only_land_cells(tmp_path):
    # West half sea (0 m), east half land at 300 m. CHELSA is "extrapolated" to a dry 20 mm over the sea and is
    # 220 mm over land. The shore land hexes hold ~18% sea cells, so counting them gives ~184 mm (desert); with
    # land cells only every land hex is 220 mm -> hills.
    n = 201
    elev = np.zeros((n, n), dtype="float32")
    elev[:, n // 2:] = 300.0
    meta = {"width": n, "height": n, "lon0": 35.0, "lon1": 35.5, "lat0": 32.2, "lat1": 32.7,
            "elev_min_m": 0.0, "elev_max_m": 300.0,
            "attribution": "dem-credit"}
    (tmp_path / "heightmap.json").write_text(json.dumps(meta))
    hc.write(tmp_path, np.round(elev / 300.0 * 65535).astype("<u2"))
    cell = 0.01
    w = round(0.5 / cell) + 2; h = round(0.5 / cell) + 2
    lons = 35.0 - cell + (np.arange(w) + 0.5) * cell
    mm = np.where(lons[None, :] < 35.25, 20.0, 220.0) * np.ones((h, 1))
    hg.write_precip(mm, 35.0 - cell, 32.7 + cell, cell, tmp_path, {"source": "synthetic", "attribution": "chelsa-credit"})
    write_no_stations(tmp_path)
    out = hg.build(tmp_path, 5.0, precip_dir=tmp_path)
    land = [x for x in out["hexes"] if x["land"]]
    coastal = [x for x in land if x["lon"] < 35.28]                # land hexes touching the shore
    assert coastal, "fixture has no coastal land hex"
    assert land and all(x["biome"] == "hills" for x in land), [(x["q"], x["r"], x["biome"]) for x in land
                                                                if x["biome"] != "hills"]


# --- Station bias correction (synthetic) ---

def test_correction_scales_field_by_idw_of_station_log_ratio(tmp_path):
    mm = np.full((4, 4), 400.0)
    hg.write_precip(mm, 35.0, 32.0, 0.25, tmp_path, {"source": "synthetic", "attribution": "chelsa-credit"})
    pr = hg.load_precip(tmp_path)
    # one station halves CHELSA, the other doubles it; cell centres at equal distance get the geometric mean (1x)
    st = [{"ids": [1], "name": "A", "lon": 35.125, "lat": 31.875, "mm": 200.0, "years": 30},
          {"ids": [2], "name": "B", "lon": 35.875, "lat": 31.875, "mm": 800.0, "years": 30}]
    out = hg.correct_precip(pr, st)
    assert out["mm"][0, 0] == pytest.approx(200.0, rel=1e-3)      # the cell holding station A
    assert out["mm"][0, 3] == pytest.approx(800.0, rel=1e-3)      # the cell holding station B
    assert out["mm"][3, 1] < 400.0 < out["mm"][3, 2]               # nearer A -> drier, nearer B -> wetter
    assert hg.correct_precip(pr, [])["mm"] is not pr["mm"] and np.array_equal(hg.correct_precip(pr, [])["mm"], pr["mm"])


@pytest.mark.parametrize("bad, station_mm, label", [
    (np.nan, 300.0, "nan"),            # CHELSA has no data at the station
    (0.0, 300.0, "0"),                 # CHELSA zero: ratio would be infinite
    (-5.0, 300.0, "-5"),               # CHELSA negative
    (400.0, 0.0, "ratio"),             # station 0 mm: log(0) is not finite
])
def test_correction_fails_loudly_on_a_bad_station(tmp_path, bad, station_mm, label):
    mm = np.full((4, 4), 400.0)
    mm[0, 0] = bad                                                  # the cell holding the second station
    pr = {"meta": {"lon0": 35.0, "lat1": 32.0, "cell_deg": 0.25, "width": 4, "height": 4}, "mm": mm}
    st = [{"ids": [7], "name": "OK", "lon": 35.875, "lat": 31.125, "mm": 400.0, "years": 30},
          {"ids": [246950], "name": "BAD", "lon": 35.125, "lat": 31.875, "mm": station_mm, "years": 30}]
    with pytest.raises(ValueError, match=f"246950.*{label}"):
        hg.correct_precip(pr, st)
    st[1].pop("ids")                                                # no id: the message names the index
    with pytest.raises(ValueError, match=f"index 1.*{label}"):
        hg.correct_precip(pr, st)


# --- Real inputs: CHELSA clip + IMS station normals (content/map/precip_*) ---
MAP_INPUTS = ROOT / "content" / "map"
HAVE_REAL = (MAP_INPUTS / "precip_bio12.json").exists() and (MAP_INPUTS / "precip_stations.json").exists()
REAL = hg.load_corrected_precip(MAP_INPUTS) if HAVE_REAL else None
STATIONS = json.loads((MAP_INPUTS / "precip_stations.json").read_text()) if HAVE_REAL else None
PLACES = {"eilat": (34.951, 29.557), "mitzpe_ramon": (34.801, 30.610), "dimona": (35.033, 31.069),
          "arad": (35.213, 31.259), "tel_aviv": (34.781, 32.085), "ashkelon": (34.571, 31.669),
          "gaza_city": (34.454, 31.515), "jerusalem": (35.214, 31.778), "hebron": (35.099, 31.532),
          "haifa": (34.989, 32.794), "netanya": (34.857, 32.332), "tiberias": (35.533, 32.794),
          "afula": (35.289, 32.610), "kiryat_shmona": (35.570, 33.208)}
# Not asserted (borderline semi-arid, ~200 mm): Beersheba, Rafah, and Mar Saba (31.705, 35.330), whose own IMS gauge
# DEIR MAR SABA (station 246950) averaged 216 mm in 1942-48, i.e. just above the 200 mm arid line.


@pytest.mark.parametrize("place, dry", [("eilat", True), ("mitzpe_ramon", True), ("dimona", True), ("arad", True),
                                        ("tel_aviv", False), ("ashkelon", False), ("gaza_city", False),
                                        ("jerusalem", False), ("hebron", False), ("haifa", False)])
def test_real_corrected_precip_matches_arid_boundary(place, dry):
    assert REAL is not None, "content/map/precip_* missing: run tools/precip_clip.py and tools/ims_normals.py"
    lon, lat = PLACES[place]
    mm = hg.precip_at(lon, lat, REAL)
    assert (mm < hg.ARID_MM) == dry, f"{place}: {mm:.0f} mm"


def test_real_corrected_field_reproduces_every_station_within_5_percent():
    assert REAL is not None
    assert len(STATIONS["stations"]) >= 12
    bad = [(s["name"], s["mm"], round(hg.precip_at(s["lon"], s["lat"], REAL), 1)) for s in STATIONS["stations"]
           if abs(hg.precip_at(s["lon"], s["lat"], REAL) / s["mm"] - 1) > 0.05]
    assert not bad, bad


def test_real_inputs_carry_sources_and_licences():
    assert REAL is not None
    m = REAL["meta"]
    assert m["units"] == "mm/year" and "CHELSA" in m["attribution"] and m["licence"] == "CC0-1.0"
    b = json.loads((ROOT / "unity" / "Assets" / "StreamingAssets" / "map" / "heightmap.json").read_text())
    assert m["lon0"] <= b["lon0"] and m["lat1"] >= b["lat1"]
    assert m["lon0"] + m["width"] * m["cell_deg"] >= b["lon1"] and m["lat1"] - m["height"] * m["cell_deg"] <= b["lat0"]
    for k in ("source", "source_urls", "licence", "attribution", "retrieved", "period", "method", "selection"):
        assert STATIONS[k], k
    assert "IDW" in STATIONS["method"] and "Israel Meteorological Service" in STATIONS["attribution"]
    for s in STATIONS["stations"]:
        assert s["years"] >= 20 and s["mm"] > 0 and s["ids"] and s["source_url"].startswith("https://")


def test_build_is_deterministic(tmp_path):
    a = json.dumps(hg.build(hg.MAP_DIR), separators=(",", ":"))
    b = json.dumps(hg.build(hg.MAP_DIR), separators=(",", ":"))
    assert a == b
    assert a == (ROOT / "content" / "map" / "hexes.json").read_text(), "hexes.json is stale: run uv run tools/hexgrid.py"


HEXES = ROOT / "content" / "map" / "hexes.json"


@pytest.mark.parametrize("place, dry", [("eilat", True), ("mitzpe_ramon", True), ("dimona", True), ("arad", True),
                                        ("tel_aviv", False), ("ashkelon", False), ("gaza_city", False),
                                        ("jerusalem", False), ("hebron", False), ("haifa", False)])
def test_committed_hex_biome_matches_arid_boundary(place, dry):
    grid = json.loads(HEXES.read_text())
    lon, lat = PLACES[place]
    q, r = hg.settlement_hex(lon, lat, grid)
    biome = next(h["biome"] for h in grid["hexes"] if (h["q"], h["r"]) == (q, r))
    assert (biome == "desert") == dry, f"{place} hex ({q}, {r}) is {biome}"


@pytest.mark.parametrize("place, biome", [("tel_aviv", "coastal_plain"), ("netanya", "coastal_plain"),
                                          ("gaza_city", "coastal_plain"), ("ashkelon", "coastal_plain"),
                                          ("haifa", "coastal_plain"),
                                          ("tiberias", "lowland"), ("afula", "lowland")])
def test_committed_hex_biome_is_coastal_only_near_the_sea(place, biome):
    """Issue #204: the Tiberias hex (slopes above the Sea of Galilee, 70 m, ~39 km from the Mediterranean) was labelled
    coastal plain."""
    grid = json.loads(HEXES.read_text())
    lon, lat = PLACES[place]
    q, r = hg.settlement_hex(lon, lat, grid)
    assert next(h["biome"] for h in grid["hexes"] if (h["q"], h["r"]) == (q, r)) == biome, place


def test_no_committed_coastal_plain_hex_is_far_from_the_sea():
    """Drift guard on the data: every committed coastal_plain hex is within the threshold of a sea hex's shore (a hex
    centre is at most the threshold plus one hex circumradius from the nearest sea hex centre)."""
    grid = json.loads(HEXES.read_text())
    size = grid["hex_size_km"]
    sea = [hg.hex_center_km(h["q"], h["r"], size) for h in grid["hexes"] if h["biome"] == "sea"]
    for h in grid["hexes"]:
        if h["biome"] == "coastal_plain":
            c = hg.hex_center_km(h["q"], h["r"], size)
            d = min(((c[0] - x) ** 2 + (c[1] - y) ** 2) ** 0.5 for x, y in sea)
            assert d <= hg.COASTAL_PLAIN_MAX_KM + size, (h["q"], h["r"], d)
