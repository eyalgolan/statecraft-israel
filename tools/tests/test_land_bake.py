import math
import re
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

import land_bake as lb


def test_the_hash_is_deterministic_uniform_and_seeded():
    i = np.arange(-500, 500)
    h = lb.hash01(i, i * 3, 7)
    assert np.array_equal(h, lb.hash01(i, i * 3, 7)) and not np.array_equal(h, lb.hash01(i, i * 3, 8))
    assert h.min() >= 0.0 and h.max() < 1.0 and abs(h.mean() - 0.5) < 0.05


def test_the_painted_land_is_weighted_by_the_masks_and_holds_every_crop():
    x, y = np.meshgrid(np.arange(0, 20000, 50.0), np.arange(0, 20000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, w = lb.paint_samples(x, y, zero, zero, zero, zero + 50.0, zero)
    assert w.max() == 0.0                                    # no fields, not wet: the biome's ground
    c, w = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero)
    assert w.mean() > 0.8 and np.isfinite(c).all()          # farmed: parcels nearly everywhere
    # Each crop covers a share of the ground (its colour, give or take the rows and the parcel's shade).
    for name, share in (("green", 0.10), ("ochre", 0.08), ("plough", 0.06), ("fallow", 0.04)):
        ref = lb.linear(lb.SRGB[name])
        near = np.linalg.norm(c / np.maximum(c.mean(axis=-1, keepdims=True), 1e-6) - ref / ref.mean(), axis=-1) < 0.06
        assert near.mean() > share, name


@pytest.mark.parametrize("name, size_m, step_m", [("HEDGE_HALF_M", (8000, 2000), 5.0), ("TRACK_HALF_M", (36000, 4000), 14.0)])
def test_field_lines_are_wide_enough_for_the_texture(name, size_m, step_m, monkeypatch):
    # Land round 2: round 1's 18 m hedges and 30 m tracks were averaged away in the texture's 79 x 105 m texel. A line's
    # own samples are those that change when its half width is 0; runs of them along east-west rows cross the lines at
    # their angles (and the blocks' wobble stretches them by up to about 20% either way), so the median run measures the
    # line across: at least 1.5 texels of the texel's 79 m (east-west) side; across its 105 m north-south side that is about
    # 1.2 texels, as only east-west rows are sampled. (Tracks lie only on some 5.2 km block edges, so they need the larger
    # ground.)
    x, y = np.meshgrid(np.arange(0, size_m[0], step_m), np.arange(0, size_m[1], step_m))
    zero, one = np.zeros_like(x), np.ones_like(x)
    with_lines, _ = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero)
    monkeypatch.setattr(lb, name, 0.0)
    without, _ = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero)
    line = np.abs(with_lines - without).max(axis=-1) > 1e-6
    runs = []
    for row in line:
        edges = np.flatnonzero(np.diff(np.concatenate([[0], row.astype(np.int8), [0]])))
        runs += [(b - a) * step_m for a, b in zip(edges[::2], edges[1::2]) if a > 0 and b < row.size]
    assert line.mean() > 0.002 and len(runs) > 100
    assert np.median(runs) >= 1.5 * 79.0, f"{name}: lines {np.median(runs):.0f} m across, under 1.5 texels"


def test_empty_texels_take_the_neighbouring_land_colour():
    rgb = np.zeros((8, 8, 3)); filled = np.zeros((8, 8), dtype=bool)
    rgb[:, :2] = (0.5, 0.4, 0.2); filled[:, :2] = True
    out = lb.dilate_colour(rgb, filled, passes=8)
    assert np.allclose(out, (0.5, 0.4, 0.2)), "no black spreads into the mips"


def test_the_land_reaches_into_the_water_and_avoids_the_rock():
    x, y = np.meshgrid(np.arange(0, 5000, 50.0), np.arange(0, 5000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, w_flat = lb.paint_samples(x, y, zero, zero, one, zero + 400.0, zero)
    _, w_cliff = lb.paint_samples(x, y, zero, zero, one, zero + 400.0, zero + 1.0)   # rise 1 per metre: rock
    assert w_flat.mean() > 0.5 and w_cliff.max() < 0.01


def test_furrows_follow_the_crop_and_keep_off_the_rims_hedges_and_groves():
    x, y = np.meshgrid(np.arange(0, 20000, 50.0), np.arange(0, 20000, 50.0))
    zero, one = np.zeros_like(x), np.ones_like(x)
    _, _, none = lb.paint_samples(x, y, zero, zero, zero, zero + 50.0, zero, furrows=True)
    assert none.max() == 0.0, "no fields, no furrows"
    _, _, fs = lb.paint_samples(x, y, one, zero, zero, zero + 50.0, zero, furrows=True)
    f = lb.field_layout(x, y)
    inner = np.minimum(f["across_m"], f["along_m"]) > 150.0
    plough, fallow = (f["ph"] >= 0.50) & (f["ph"] < 0.67), f["ph"] >= 0.67
    assert np.median(fs[plough & inner]) > 0.9, "ploughland furrows fully inside the parcel"
    assert fs[fallow].max() == 0.0, "fallow and grass have none"
    assert fs[np.minimum(f["across_m"], f["along_m"]) < 30.0].max() == 0.0, "none on the parcels' rims"
    _, _, groves = lb.paint_samples(x, y, one, one, zero, zero + 50.0, zero, furrows=True)
    assert groves.mean() < 0.6 * fs.mean(), "the citrus belt's groves have none"


def rows_from_table(x, y, table):
    """What GroundDetail.shader does with fieldblocks.png (ScFieldRows): the point's block from the wobbled grid, that
    block's angle and strip offset from its texel, then its across-the-strips coordinate lx and its strip phase."""
    bpx, bpy = lb.block_wobble(x, y)
    bix, biy = np.floor(bpx).astype(int), np.floor(bpy).astype(int)
    t = table[table.shape[0] - 1 - (biy + lb.BLOCK_PAD), bix + lb.BLOCK_PAD].astype(np.int64)
    ang = lb.ANGLE_MIN + (t[..., 0] * 256 + t[..., 1]) / 65535 * lb.ANGLE_SPAN
    off = (t[..., 2] * 256 + t[..., 3]) / 65536
    qx, qy = (bpx - bix - 0.5) * lb.BLOCK_M, (bpy - biy - 0.5) * lb.BLOCK_M
    lx = np.cos(ang) * qx - np.sin(ang) * qy
    return lx, np.mod(lx / lb.STRIP_M + off, 1.0)


def test_the_block_table_rebuilds_the_bakes_rows():
    # #2917: the shader cannot repeat the 64-bit hash, so it reads each block's angle and strip offset from the table. With
    # them it must put every point where the bake does: the same distance across the strips (to well under a furrow) and
    # the same place in its strip, everywhere on the map, the wobbled edges included.
    meta = {"width_km": 161.53, "length_km": 431.22}
    table = lb.block_table(meta)
    assert table.shape == lb.blocks_size(meta)[::-1] + (4,)
    rng = np.random.default_rng(2917)
    x, y = rng.uniform(0, meta["width_km"] * 1000, 200000), rng.uniform(0, meta["length_km"] * 1000, 200000)
    lx, phase = rows_from_table(x, y, table)
    f = lb.field_layout(x, y)
    assert np.abs(lx - f["lx"]).max() < 0.5
    d = np.abs(phase - np.mod(f["sx"], 1.0))
    assert np.minimum(d, 1 - d).max() * lb.STRIP_M < 0.5


SHADER = Path(__file__).resolve().parents[2] / "unity/Assets/Shaders/GroundDetail.shader"
NUMBER = r"(-?[\d.]+)"


def test_the_shader_holds_the_bakes_constants():
    # #2917: GroundDetail.shader keeps its own copies of the farm blocks' constants and of block_wobble's formula (ScFieldRows),
    # and of the table's 16-bit encodings. If one side changes alone, the furrows leave the bake's parcels; this fails instead.
    src = SHADER.read_text()
    consts = re.search(rf"static const float ScBlockM = {NUMBER}, ScStripM = {NUMBER}, ScAngleMin = {NUMBER}, "
                       rf"ScAngleSpan = {NUMBER} \+ {NUMBER};", src)
    assert consts, "ScFieldRows' constants line changed shape: update this test with it"
    block_m, strip_m, angle_min, span_a, span_b = map(float, consts.groups())
    assert (block_m, strip_m, angle_min) == (lb.BLOCK_M, lb.STRIP_M, lb.ANGLE_MIN)
    assert span_a + span_b == pytest.approx(lb.ANGLE_SPAN, abs=1e-6)
    assert lb.ANGLE_SPAN == 0.7 + math.pi / 2               # block_angle's range: 0.7 wide, a quarter turn on top
    pad = re.search(r"static const int ScBlockPad = (\d+);", src)
    assert pad and int(pad.group(1)) == lb.BLOCK_PAD
    wobble = re.search(rf"float2 bp = p / ScBlockM \+ {NUMBER} \* float2\(sin\(p\.y / {NUMBER} \+ sin\(p\.x / {NUMBER}\)\), "
                       rf"sin\(p\.x / {NUMBER} \+ sin\(p\.y / {NUMBER}\)\)\);", src)
    assert wobble, "ScFieldRows' wobble line changed shape: update this test with it"
    a, py1, px1, px2, py2 = map(float, wobble.groups())
    rng = np.random.default_rng(2917)
    x, y = rng.uniform(0, 161530, 10000), rng.uniform(0, 431220, 10000)
    bpx, bpy = lb.block_wobble(x, y)
    assert np.abs(bpx - (x / block_m + a * np.sin(y / py1 + np.sin(x / px1)))).max() < 1e-9
    assert np.abs(bpy - (y / block_m + a * np.sin(x / px2 + np.sin(y / py2)))).max() < 1e-9
    # block_table's encodings, as the shader decodes them: the angle over 65535, the strip offset over 65536.
    assert "(b.r * 256.0 + b.g) / 65535.0 * ScAngleSpan" in src and "(b.b * 256.0 + b.a) / 65536.0" in src


@pytest.fixture(scope="module")
def baked():
    return lb.bake()


def test_the_committed_texture_is_the_bake(baked):
    # Review M11: the committed PNGs must be what tools/land_bake.py makes from today's inputs (about 30 s).
    land, furrows, blocks = baked
    assert np.array_equal(np.asarray(Image.open(lb.OUT)), land), "landuse.png is stale: run uv run tools/land_bake.py"
    assert np.array_equal(np.asarray(Image.open(lb.FURROWS_OUT)), furrows), "furrows.png is stale: run uv run tools/land_bake.py"
    assert np.array_equal(np.asarray(Image.open(lb.BLOCKS_OUT)), blocks), "fieldblocks.png is stale: run uv run tools/land_bake.py"


def test_the_furrows_lie_on_the_farmed_plains():
    furrows = np.asarray(Image.open(lb.FURROWS_OUT))
    assert furrows.shape == (lb.FURROW_H, lb.FURROW_W)

    def strength(lon, lat):
        r, c = int((33.35 - lat) / 3.9 * furrows.shape[0]), int((lon - 34.2) / 1.7 * furrows.shape[1])
        return furrows[r - 6:r + 7, c - 6:c + 7].mean() / 255
    assert strength(34.84, 31.92) > 0.1 and strength(35.30, 32.62) > 0.1   # the coastal plain and the Jezreel valley
    assert strength(34.90, 30.60) == 0 and strength(35.50, 31.50) == 0      # the central Negev and the Dead Sea


def test_the_committed_texture_has_its_size_and_follows_the_ground():
    land = np.asarray(Image.open(lb.OUT))
    assert land.shape == (lb.COLOUR_H, lb.COLOUR_W, 4)

    def weight(lon, lat):
        r, c = int((33.35 - lat) / 3.9 * land.shape[0]), int((lon - 34.2) / 1.7 * land.shape[1])
        return land[r - 6:r + 7, c - 6:c + 7, 3].mean() / 255
    assert weight(34.84, 31.92) > 0.8 and weight(35.30, 32.62) > 0.8    # the coastal plain and the Jezreel valley farmed
    assert weight(34.90, 30.60) < 0.02 and weight(35.50, 31.50) < 0.02  # the central Negev and the Dead Sea untouched
    assert weight(35.15, 31.78) > 0.4                                   # garrigue and terraces on the Judean hills
    assert land[..., :3][land[..., 3] == 0].min(axis=0).max() > 0, "the empty texels hold land colour, not black"
