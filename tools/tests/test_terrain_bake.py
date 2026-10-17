import json
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

import terrain_bake as tb
import heightmap_codec as hc

ROOT = Path(__file__).resolve().parents[2]


def png(path: Path) -> Image.Image:
    with Image.open(path) as im:
        return im.copy()


def meta_for(h: int, w: int, dx: float, dy: float) -> dict:
    return {"width": w, "height": h, "width_km": (w - 1) * dx / 1000, "length_km": (h - 1) * dy / 1000,
            "lon0": 34.2, "lon1": 35.9, "lat0": 29.45, "lat1": 33.35}


def test_flat_land_is_open_and_has_no_curvature():
    z = np.full((41, 41), 300.0, dtype=np.float32)
    assert np.allclose(tb.openness(z, 100, 100, 2000), 1.0)
    assert np.allclose(tb.curvature(z, 100, 100), 0.5)


def test_a_pit_sees_less_sky_than_its_rim():
    y, x = np.mgrid[0:65, 0:65]
    z = (100 + 2.0 * ((x - 32) ** 2 + (y - 32) ** 2)).astype(np.float32)   # a bowl: 0 at the centre, ~2 km at the corners
    sky = tb.openness(z, 100, 100, 2000)
    assert sky[32, 32] < sky[0, 0]
    # every ray from the centre reaches about 20 samples (2 km), where the rim stands 2 r^2 m above it at 100 r m: tan = r / 50
    assert sky[32, 32] == pytest.approx(1 - np.sin(np.arctan(20 / 50)), abs=0.01)


def test_ridges_are_convex_and_valleys_concave():
    x = np.arange(65)
    z = np.tile(500 + 300 * np.cos(2 * np.pi * x / 32), (65, 1)).astype(np.float32)   # crests at x = 0, 32, 64
    c = tb.curvature(z, 100, 100)
    assert c[32, 32] > 0.55 and c[32, 16] < 0.45
    # the tent (two 13-sample boxes) keeps f^2 of a cosine's amplitude: c = 0.5 +- 300 (1 - f^2) / (2 * 150)
    f = (1 + 2 * sum(np.cos(2 * np.pi * k / 32) for k in range(1, 7))) / 13
    assert c[32, 32] == pytest.approx(0.5 + (1 - f * f), abs=1e-3)
    assert c[32, 16] == pytest.approx(0.5 - (1 - f * f), abs=1e-3)


def test_sea_is_border_connected_zero_and_an_inland_zero_pocket_is_land():
    elev = np.full((40, 40), 50.0, dtype=np.float32)
    elev[:, :10] = 0.0                      # the sea along the west border
    elev[19:22, 29:32] = 0.0                # an enclosed 0 m pocket
    water = tb.water_mask(elev, 1000, 1000)
    assert water[:, :10].all()
    assert not water[19:22, 29:32].any()
    assert not water[:, 10:].any()


def test_flat_water_below_minus_150_is_a_lake_and_a_slope_is_not():
    elev = np.full((40, 40), 300.0, dtype=np.float32)
    elev[5:17, 5:17] = -420.0                                                 # the Dead Sea's surface: flat
    elev[22:34, 5:17] = (-200.0 - 10.0 * np.arange(12))[None, :]              # the rift floor: sloping
    water = tb.water_mask(elev, 200, 200)                                     # 12 x 12 samples of 200 m: 5.76 km2
    assert water[8:14, 8:14].all()
    assert not water[22:34, 5:17].any()


def test_coast_distance_is_signed_metres_across_a_straight_coast():
    elev = np.full((30, 60), 50.0, dtype=np.float32)
    elev[:, :20] = 0.0                                                        # sea columns 0..19
    water = tb.water_mask(elev, 100, 100)
    to_water = tb.distance_to(water, 100, 100, 12000)
    to_land = tb.distance_to(~water, 100, 100, 12000)
    assert to_water[15, 30] == pytest.approx(1100)                           # 11 columns east of the last sea column
    assert to_land[15, 9] == pytest.approx(1100)                             # 11 columns west of the first land column
    assert to_water[15, 59] == pytest.approx(4000)


def test_distance_uses_each_axis_own_spacing():
    elev = np.full((40, 30), 50.0, dtype=np.float32)
    elev[:20, :] = 0.0                                                        # sea in the north rows (row 0 = north)
    d = tb.distance_to(tb.water_mask(elev, 79.0, 211.0), 79.0, 211.0, 12000)
    assert d[25, 15] == pytest.approx(6 * 211.0)


def test_a_flat_pool_below_minus_150_smaller_than_the_minimum_lake_area_is_land():
    elev = np.full((60, 60), 300.0, dtype=np.float32)
    elev[5:45, 5:45] = -420.0                    # 40 x 40 samples of 79 x 211 m: 26.7 km2, a lake
    elev[50:57, 50:57] = -312.0                  # 7 x 7: 0.82 km2 (Karameh is 0.30 km2): opened, but under 1 km2
    water = tb.water_mask(elev, 79.0, 211.0)
    assert water[10:40, 10:40].all()
    assert not water[48:60, 48:60].any()
    assert tb.MIN_LAKE_AREA_KM2 == 1.0


def test_sea_is_strictly_within_half_a_metre_as_hexgrid_rules():
    elev = np.full((20, 20), 50.0, dtype=np.float32)
    elev[:, :5] = 0.5
    elev[3:17, 5:8] = 0.49                       # off the top and bottom border rows
    water = tb.water_mask(elev, 100, 100)
    assert not water[:, :5].any()                # 0.5 m is not sea (hexgrid.py: |elevation| < 0.5); the border run is land
    assert not water[:, 5:8].any()               # so the 0.49 m strip is not connected to the border
    elev[:, :5] = 0.49
    water = tb.water_mask(elev, 100, 100)
    assert water[:, :5].all() and water[3:17, 5:8].all()


def brute_distance(target: np.ndarray, dx: float, dy: float) -> np.ndarray:
    ty, tx = np.nonzero(target)
    y, x = np.mgrid[0:target.shape[0], 0:target.shape[1]]
    d2 = ((y[..., None] - ty) * dy) ** 2 + ((x[..., None] - tx) * dx) ** 2
    return np.sqrt(d2.min(axis=-1))


@pytest.mark.parametrize("seed", range(6))
def test_distance_is_the_exact_euclidean_distance_on_an_anisotropic_grid(seed):
    rng = np.random.default_rng(seed)
    h, w = int(rng.integers(5, 40)), int(rng.integers(5, 40))
    target = rng.random((h, w)) < float(rng.choice([0.01, 0.05, 0.3]))
    target[rng.integers(h), rng.integers(w)] = True
    dx, dy = float(rng.uniform(20, 300)), float(rng.uniform(20, 300))
    got = tb.distance_to(target, dx, dy, 1e9)
    assert np.allclose(got, brute_distance(target, dx, dy), rtol=0, atol=1e-6)
    capped = tb.distance_to(target, dx, dy, 500.0)
    assert np.allclose(capped, np.minimum(brute_distance(target, dx, dy), 500.0), rtol=0, atol=1e-6)


def test_distance_from_a_point_is_round_in_metres_in_every_octant():
    dx, dy = 78.87, 210.56                       # the real heightmap's spacing (the chamfer was 22% off at 45 degrees)
    y, x = np.mgrid[0:61, 0:161]
    r = np.hypot((x - 80) * dx, (y - 30) * dy)
    d = tb.distance_to(r == 0, dx, dy, 12000)
    ring = np.abs(r - 3000) < 110                # the 3 km ring, every octant
    assert ring.sum() > 50
    assert np.abs(d[ring] - r[ring]).max() < 1e-6
    assert np.allclose(d, np.minimum(r, 12000), rtol=0, atol=1e-6)


def test_texels_average_four_samples_and_coast_encoding_is_centred():
    a = np.arange(9, dtype=np.float32).reshape(3, 3)
    assert np.array_equal(tb.to_texels(a), np.array([[2, 3], [5, 6]], dtype=np.float32))
    enc = tb.encode_coast(np.array([0.0, -12000.0, 12000.0, -50000.0, 120.0], dtype=np.float32))
    assert enc.tolist() == [128, 0, 255, 0, 129]


def test_main_writes_both_textures_one_texel_smaller_and_deterministically(tmp_path):
    n = 33
    y, x = np.mgrid[0:n, 0:n]
    elev = np.where(x < 8, 0.0, 40.0 + 30.0 * np.sin(y / 3.0) + 20.0 * x).astype(np.float32)
    lo, hi = float(elev.min()), float(elev.max())
    map_dir = tmp_path / "map"; map_dir.mkdir()
    hc.write(map_dir, np.round((elev - lo) / (hi - lo) * 65535).astype("<u2"))
    (map_dir / "heightmap.json").write_text(json.dumps({**meta_for(n, n, 79.0, 211.0), "elev_min_m": lo, "elev_max_m": hi}))
    tb.main(map_dir, tmp_path / "a"); tb.main(map_dir, tmp_path / "b")
    bake = png(tmp_path / "a" / "terrain_bake.png"); coast = png(tmp_path / "a" / "coast.png")
    assert (bake.mode, bake.size) == ("RGB", (n - 1, n - 1))
    assert (coast.mode, coast.size) == ("L", (n - 1, n - 1))
    ao = png(tmp_path / "a" / "ao.png")
    assert (ao.mode, ao.size) == ("L", (n - 1, n - 1))
    assert np.array_equal(np.asarray(ao), np.asarray(bake)[..., 0]), "ao.png is the bake's R channel, on its own"
    for name in ("terrain_bake.png", "coast.png", "ao.png"):
        assert (tmp_path / "a" / name).read_bytes() == (tmp_path / "b" / name).read_bytes()
    c = np.asarray(coast)
    assert c[:, :5].max() < 128 and c[:, 12:].min() > 128                   # water west, land east
    # pixels, not bytes, are the reproducible part across toolchains (zlib builds differ; docs/visual-v1.md)
    assert np.array_equal(np.asarray(png(tmp_path / "b" / "coast.png")), c)


def texel(lon: float, lat: float) -> tuple[int, int]:
    meta = json.loads((ROOT / "unity/Assets/StreamingAssets/map/heightmap.json").read_text())
    col = int((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * 2048)
    row = int((meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * 2048)
    return row, col


def test_the_committed_coast_texture_marks_the_seas_and_lakes():
    c = np.asarray(png(ROOT / "unity/Assets/Textures/Map/coast.png"))
    assert c.shape == (2048, 2048)
    assert c[texel(34.40, 32.00)] < 64, "the Mediterranean off Tel Aviv, well offshore"
    assert c[texel(35.59, 32.82)] < 128, "the Sea of Galilee"
    assert c[texel(35.50, 31.50)] < 128, "the Dead Sea"
    assert c[texel(34.95, 29.50)] < 128, "the Gulf of Aqaba off Eilat"
    assert c[texel(35.21, 31.78)] > 192, "Jerusalem, far inland"
    assert c[texel(35.41, 31.18)] < 128, "the largest evaporation pan (45 km2) south of the Dead Sea"
    assert c[texel(35.558, 32.019)] > 192, "the Karameh reservoir (0.3 km2, built in the 1990s) is not a lake"
    assert c[texel(35.50, 32.50)] > 192, "Beit She'an in the rift (-120 m) is land, far from water"
    b = np.asarray(png(ROOT / "unity/Assets/Textures/Map/terrain_bake.png"))
    assert b.shape == (2048, 2048, 3)


def test_the_committed_coast_crosses_one_half_at_the_shoreline():
    c = np.asarray(png(ROOT / "unity/Assets/Textures/Map/coast.png")).astype(int)
    for lat in (31.5, 32.0, 32.5, 32.9):                                     # west-east across the Mediterranean shore
        row, _ = texel(34.2, lat)
        line = c[row, :texel(35.1, lat)[1]]
        cross = int(np.argmax(line >= 128))                                  # the first land-side texel
        assert cross > 0 and (line[:cross] < 128).all(), lat
        assert line[cross - 1] >= 124 and line[cross] <= 131, (lat, line[cross - 3:cross + 3])   # within ~300 m of 0.5
        assert (np.diff(line[cross - 40:cross + 40]) >= 0).all(), lat        # monotone through the shore


def box(b: np.ndarray, lon0: float, lon1: float, lat0: float, lat1: float) -> np.ndarray:
    r0, c0 = texel(lon0, lat1)
    r1, c1 = texel(lon1, lat0)
    return b[r0:r1, c0:c1]


def test_the_committed_bake_shades_real_gorges_ridges_and_plateaus():
    b = np.asarray(png(ROOT / "unity/Assets/Textures/Map/terrain_bake.png")).astype(np.float64) / 255
    elev, meta = tb.load_heightmap(ROOT / "unity/Assets/StreamingAssets/map")
    # the Mujib gorge (Wadi Mujib, about 900 m deep): its thalweg is each column's lowest texel in the box
    r0, c0 = texel(35.58, 31.50)
    r1, c1 = texel(35.70, 31.43)
    floor = r0 + np.argmin(tb.to_texels(elev)[r0:r1, c0:c1], axis=0)
    gorge = b[floor, np.arange(c0, c1)].mean(axis=0)
    plateau = box(b, 35.75, 35.85, 31.68, 31.75).reshape(-1, 3).mean(axis=0)   # the Madaba plateau, 720-840 m
    sea = box(b, 34.30, 34.50, 31.90, 32.10).reshape(-1, 3).mean(axis=0)       # the open Mediterranean
    assert gorge[0] < 0.6 and plateau[0] > 0.85 and sea[0] > 0.99, (gorge, plateau)          # R: AO
    assert gorge[1] < 0.2 and abs(sea[1] - 0.5) < 0.01                                     # G: a hollow; the sea is flat
    assert gorge[2] < 0.65 and plateau[2] > 0.85 and sea[2] > 0.99                        # B: sky-view
    assert gorge[2] < plateau[2] < sea[2]
    for lon, lat in ((35.390, 32.687), (35.4097, 32.9997)):                               # Mount Tabor, Mount Meron
        r, col = texel(lon, lat)
        assert b[r - 1:r + 2, col - 1:col + 2, 1].mean() > 0.9, "a summit is convex"
    judean = box(b, 35.2, 35.4, 31.5, 31.8)[..., 0].mean()
    coastal = box(b, 34.85, 34.95, 31.9, 32.3)[..., 0].mean()
    assert judean < 0.8 and judean < coastal - 0.1, "the gullied Judean desert is darker than the coastal plain"


def test_the_committed_ao_texture_is_the_bake_ao_uncompressed_on_its_own():
    # Task 6 fix round 1: BC1 stepped the AO by 2+ levels on 59% of the broad Judean slopes; the shader samples this R8 copy.
    ao = png(ROOT / "unity/Assets/Textures/Map/ao.png")
    b = np.asarray(png(ROOT / "unity/Assets/Textures/Map/terrain_bake.png"))
    assert ao.mode == "L" and np.array_equal(np.asarray(ao), b[..., 0])


def test_constants_duplicated_in_csharp_and_hlsl_match_the_bake():
    import re
    cs = (ROOT / "unity/Assets/Scripts/Map/MapBuilder.cs").read_text()
    hlsl = (ROOT / "unity/Assets/Shaders/StatecraftMap.hlsl").read_text()
    ve = float(re.search(r"const float VerticalExaggeration\s*=\s*([0-9.]+)f", cs).group(1))
    cr = float(re.search(r"static const float ScCoastRangeM\s*=\s*([0-9.]+)", hlsl).group(1))
    assert tb.VERTICAL_EXAGGERATION == ve
    assert tb.COAST_RANGE_M == cr


# Sea round 3 (#388): a wider beach on the dune coasts, baked into coast.png's land side (no shader change).

def test_the_beach_widening_keeps_the_waterline_metric_and_takes_the_extra_out_beyond_the_full_sand():
    d = np.linspace(0, 12000, 24001)
    assert np.array_equal(tb.beach_widened(d, np.zeros_like(d)), d), "no extra: metres"
    e = np.full_like(d, 1800.0)
    w = tb.beach_widened(d, e)
    a, b = tb.BEACH_KNEE_M
    assert np.array_equal(w[d <= a], d[d <= a]), "the waterline, the land use's fade and the wet sand stay in metres"
    assert np.allclose(w[d >= b + 1800], d[d >= b + 1800] - 1800), "beyond the full sand: the extra taken out, metric again"
    assert (np.diff(w) > 0).all(), "monotone, so the shore's ramps keep their order"
    assert np.abs(np.diff(w)).max() <= np.diff(d).max() + 1e-9, "continuous: no step"
    assert tb.encode_coast(np.array([b]))[0] / 255 * 2 * tb.COAST_RANGE_M - tb.COAST_RANGE_M < 0.55 * 0.6 * 900, (
        "the squeezed stretch, as 8-bit levels, reads as full sand at ScShore's narrowest width")
    assert np.array_equal(tb.beach_widened(-d, e), -d), "the water side is untouched"


def test_the_extra_width_is_south_of_jaffa_and_in_haifa_bay_only():
    lon = np.array([34.72, 34.55, 34.42, 35.06, 35.05, 34.78, 34.89, 34.96, 35.08, 34.95, 35.60])
    lat = np.array([31.95, 31.70, 31.50, 32.88, 32.84, 32.15, 32.50, 32.70, 33.00, 29.50, 32.80])
    extra = tb.beach_extra_m(lon, lat)
    assert np.allclose(extra[:5], tb.BEACH_EXTRA_M), "Rishon, Ashdod, Gaza's coast, Haifa Bay's dunes"
    assert np.allclose(extra[5:], 0), "Herzliya, Netanya, the Carmel coast, north of Acre, Eilat, the Sea of Galilee"
    ramp = tb.beach_extra_m(np.full(81, 34.75), np.linspace(32.00, 32.10, 81))
    assert (np.diff(ramp) <= 0).all() and ramp[0] == tb.BEACH_EXTRA_M and ramp[-1] == 0, "a smooth ramp at Jaffa"
    assert np.abs(np.diff(ramp)).max() < 0.06 * tb.BEACH_EXTRA_M


def test_the_bake_widens_only_the_land_side_inside_a_band(tmp_path, monkeypatch):
    n = 41
    y, x = np.mgrid[0:n, 0:n]
    elev = np.where(x < 15, 0.0, 5.0 + 0.5 * x).astype(np.float32)
    meta = {**meta_for(n, n, 79.0, 211.0), "lon0": 34.55, "lon1": 34.59, "lat0": 31.80, "lat1": 31.88}   # off Ashdod
    _, wide = tb.bake(elev, meta)
    monkeypatch.setattr(tb, "BEACH_EXTRA_M", 0.0)
    _, metric = tb.bake(elev, meta)
    wet = metric < 128
    assert wet[:, :10].all() and np.array_equal(wide[wet], metric[wet]), "the water texels are the metric encoding"
    assert (wide[:, 25:] < metric[:, 25:]).all(), "the land beyond the full sand reads nearer the sea"
    assert (wide <= metric).all()


def lonlat(row: int, col: int) -> tuple[float, float]:
    return 34.2 + (col + 0.5) / 2048 * 1.7, 33.35 - (row + 0.5) / 2048 * 3.9


def land_m_inland(lat: float, km: float) -> float:
    """coast.png's land-side metres (as ScShore decodes them) km east of the Mediterranean shore on lat's row."""
    c = np.asarray(png(ROOT / "unity/Assets/Textures/Map/coast.png")).astype(np.float64) / 255
    row, _ = texel(34.2, lat)
    line = c[row, :texel(35.1, lat)[1]]
    shore = int(np.argmax(line >= 0.5))
    col = shore + round(km * 1000 / (161529.4 / 2048))
    return (line[col] - 0.5) * 2 * tb.COAST_RANGE_M


def test_the_committed_coast_draws_a_wide_beach_south_of_jaffa_and_a_narrow_one_under_the_kurkar():
    # ScShore (StatecraftMap.hlsl): full sand to 0.55 W and none beyond W, W = ScBeachM (900 m) x 0.6..1.4.
    # 31.90 N (off Yavne's dunes): 31.95 N, by Rishon LeZion, is now 2.3 km from Nabi Rubin, whose own ground the beach
    # yields to (option (c)).
    assert land_m_inland(31.90, 2.0) < 0.55 * 0.6 * 900, "2 km inland south of Jaffa: full sand at any width"
    assert land_m_inland(32.15, 2.0) > 1.4 * 900, "2 km inland at Herzliya: no sand at any width"


def test_the_csharp_twin_of_the_beach_bands_matches_the_bake():
    """#690: the bands are one file, DuneCoasts.json, which the bake and BeachBands (through DuneCoasts.cs) both read;
    neither keeps a copy of them."""
    import json
    import re
    cs = (ROOT / "unity/Assets/Scripts/Map/Kits/BeachBands.cs").read_text()
    hlsl = (ROOT / "unity/Assets/Shaders/StatecraftMap.hlsl").read_text()
    assert float(re.search(r"const float ExtraM = ([0-9.]+)f", cs).group(1)) == tb.BEACH_EXTRA_M
    dune = json.loads((ROOT / "unity/Assets/Resources/Map/DuneCoasts.json").read_text(encoding="utf-8"))["bands"]
    assert tb.BEACH_BANDS == tuple((b["lon0"], b["lon1"], b["lat0"], b["lat1"], b["ramp"]) for b in dune)
    assert "DuneCoasts.Weight(lon, lat)" in cs and "DuneCoasts.Bands" in cs and "Bands =" not in cs, \
        "BeachBands reads DuneCoasts.json, no copy of its own"
    beach = float(re.search(r"static const float ScBeachM = ([0-9.]+);", hlsl).group(1))
    full = float(re.search(r"const float FullSandM = ([0-9.]+)f", cs).group(1))
    assert full == pytest.approx(0.55 * 1.4 * beach, abs=0.5), "ScShore's widest full sand"


# Option (c) (#685; owner, 16 October 2026): "Keep each settlement's own lots and fields off the sand, and narrow the beach
# ramp at Acre, which was not dune country."

def test_acre_and_the_naaman_mouth_get_no_extra_width_and_the_bays_dunes_keep_it():
    lon = np.array([35.082, 35.081, 35.07, 35.06])
    lat = np.array([32.928, 32.909, 32.885, 32.86])
    extra = tb.beach_extra_m(lon, lat)
    assert extra[0] == 0, "Acre's walled town (Wikipedia: 32.928 N 35.082 E), a cape of the bay, not dune country"
    assert extra[1] < 0.01 * tb.BEACH_EXTRA_M, "the Na'aman's mouth (he.wikipedia: 32.909 N 35.081 E), where the bay's dunes end"
    assert np.allclose(extra[2:], tb.BEACH_EXTRA_M), "the bay's dunes south of the Na'aman keep the wide beach"


def test_the_beach_yields_to_a_settlements_plot_reach_and_comes_back_beyond_it():
    x = np.linspace(0, 6000, 61)
    keep = tb.beach_keep(x, np.array([0.0]), [(0.0, 0.0, 2000.0)])[0]
    assert (keep[x <= 2000] == 0).all(), "no extra width over the settlement's own ground"
    assert (keep[x >= 2000 + tb.BEACH_YIELD_RAMP_M] == 1).all(), "the full extra width beyond the ramp"
    assert (np.diff(keep) >= 0).all() and np.abs(np.diff(keep)).max() < 0.2, "a smooth ramp"
    two = tb.beach_keep(x, np.array([0.0]), [(0.0, 0.0, 2000.0), (6000.0, 0.0, 1000.0)])[0]
    assert np.array_equal(two, np.minimum(keep, tb.beach_keep(x, np.array([0.0]), [(6000.0, 0.0, 1000.0)])[0]))


def test_the_plot_reaches_are_the_kits_footprints():
    meta = json.loads((ROOT / "unity/Assets/StreamingAssets/map/heightmap.json").read_text())
    reaches = tb.plot_reaches(meta)
    towns = len(tb.yaml12.load((ROOT / "content/map/settlements_1948.yaml").read_text())["settlements"])
    places = len(tb.yaml12.load((ROOT / "content/map/localities_1948.yaml").read_text())["places"])
    assert len(reaches) == towns + places, "every game settlement and every 1948 locality"
    hex_area = 1.5 * np.sqrt(3) * 5000.0 ** 2
    w_m, l_m = meta["width_km"] * 1000, meta["length_km"] * 1000
    def reach_at(lon: float, lat: float) -> float:
        x, z = (lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * w_m, (lat - meta["lat0"]) / (meta["lat1"] - meta["lat0"]) * l_m
        return next(r for cx, cz, r in reaches if abs(cx - x) < 1 and abs(cz - z) < 1)
    smallest = np.sqrt(tb.MIN_HEXES * hex_area / np.pi) * tb.PLOT_REACH
    assert reach_at(34.7503, 32.0231) == pytest.approx(smallest), "Bat Yam (2,000 people, a locality): the smallest footprint"
    assert reach_at(34.7531, 32.0522) == pytest.approx(np.sqrt(94310 / 60000 * hex_area / np.pi) * 1.1), "Jaffa: 94,310 people"


def test_the_kits_constants_and_the_yield_ramp_match_the_csharp():
    import re
    kits = (ROOT / "unity/Assets/Scripts/Map/Kits/SettlementKits.cs").read_text()
    cs = (ROOT / "unity/Assets/Scripts/Map/Kits/BeachBands.cs").read_text()
    assert float(re.search(r"const float PeoplePerHex = ([0-9.]+)f", kits).group(1)) == tb.PEOPLE_PER_HEX
    assert float(re.search(r"const float MinHexes = ([0-9.]+)f", kits).group(1)) == tb.MIN_HEXES
    assert float(re.search(r"const float PlotReach = ([0-9.]+)f", kits).group(1)) == tb.PLOT_REACH
    assert float(re.search(r"const float YieldRampM = ([0-9.]+)f", cs).group(1)) == tb.BEACH_YIELD_RAMP_M


def committed_and_metric_coast():
    elev, meta = tb.load_heightmap(tb.MAP_DIR)
    return elev, meta, np.asarray(png(ROOT / "unity/Assets/Textures/Map/coast.png")).astype(int)


def test_the_committed_coast_is_the_bake_of_todays_settlements(monkeypatch):
    # A locality added or resized on the dune coast without a re-bake would leave its ground under the extra sand.
    elev, meta, committed = committed_and_metric_coast()
    fresh = tb.coast(elev, meta, tb.plot_reaches(meta)).astype(int)
    diff = np.abs(fresh - committed)
    assert diff.max() <= 1 and (diff > 0).mean() < 1e-4, (
        "unity/Assets/Textures/Map/coast.png is stale against content/map/settlements_1948.yaml, localities_1948.yaml or "
        "settlement_population_1945.yaml (the dune beach yields to each place's plot reach, set by its coordinates and "
        "population): run `uv run tools/terrain_bake.py` from the repo root and commit unity/Assets/Textures/Map/coast.png")


def test_the_committed_coast_keeps_every_settlements_own_ground_at_scshores_beach(monkeypatch):
    elev, meta, committed = committed_and_metric_coast()
    monkeypatch.setattr(tb, "BEACH_EXTRA_M", 0.0)
    metric = tb.coast(elev, meta).astype(int)
    h, w = elev.shape
    dx, dy = tb.spacing_m(meta)
    x = (np.arange(w - 1) + 0.5) * dx                     # texel centres (to_texels: the mean of 2 x 2 samples)
    z = (h - 1 - (np.arange(h - 1) + 0.5)) * dy
    own = np.zeros((h - 1, w - 1), bool)
    for cx, cz, reach in tb.plot_reaches(meta):
        own |= (x[None, :] - cx) ** 2 + (z[:, None] - cz) ** 2 < reach ** 2
    assert own.sum() > 0
    worst = np.abs(committed - metric)[own].max()
    assert worst <= 1, f"a settlement's plot reach has extra sand (coast.png reads up to {worst} levels short)"
    assert (committed < metric - 1).sum() > 100_000, "the open dune coast keeps its wide beach"


def test_the_committed_coast_draws_acre_at_scshores_beach_and_the_bays_dunes_wide(monkeypatch):
    elev, meta, committed = committed_and_metric_coast()
    monkeypatch.setattr(tb, "BEACH_EXTRA_M", 0.0)
    metric = tb.coast(elev, meta).astype(int)
    r0, c0 = texel(35.05, 32.95)
    r1, c1 = texel(35.12, 32.912)
    acre = np.s_[r0:r1, c0:c1]   # Acre's walled town, its peninsula and its shore to just north of the Na'aman's mouth
    assert (committed[acre] >= 128).sum() > 50 and np.abs(committed[acre] - metric[acre]).max() <= 1, (
        "Acre has ScShore's own beach, no extra width")
    # ScShore (StatecraftMap.hlsl): full sand at any width under 0.55 x 0.6 x 900 m.
    assert land_m_inland(32.87, 2.0) < 0.55 * 0.6 * 900, "2 km inland on the bay's dunes south of the Na'aman: full sand"
