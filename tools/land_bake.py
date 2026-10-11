# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""Land use for the terrain shader (issue #33): where the ground is farmed, planted or terraced, from real data,
and what it looks like there, painted procedurally and baked (the shader only lays it over the biome ground:
StatecraftLand.hlsl).

Usage: uv run tools/land_bake.py   (about 15 seconds; no network)
Reads the committed heightmap (unity/Assets/StreamingAssets/map/heightmap.*), content/map/hexes.json (biomes),
content/map/precip_bio12.* + precip_stations.json (CHELSA mean annual rainfall, station-corrected exactly as
tools/hexgrid.py does) and content/map/settlements_1948.yaml. Where, on a WIDTH x HEIGHT grid (about 158 x 211 m):
  fields:   arable land: gentle slope (< ~7%) with enough rain for dry farming (smoothstep 180..380 mm; the Jordan
            valley floor, irrigated, at least 0.7), outside water
  orchards: the citrus belt: flat coastal plain or valley floor below 150 m within about 10 km of a town (exp(-d/7 km))
  wet:      rainfall smoothstep 250..600 mm: garrigue, terraces and scrub grow where it is high
What (paint_land, seeded by map position only): field parcels in rotated blocks (green, ochre, ploughed, fallow, grass,
groves in the citrus belt) with soft rims and hedges, some block edges as farm tracks; on the wet unfarmed hills patches of
stony soil and grass, terraces along the real contours, scrub. Writes, row 0 = north:
  unity/Assets/Textures/Map/landuse.png  RGBA8, COLOUR_W x COLOUR_H (about 79 x 105 m): rgb the land's colour (sRGB,
      spread into the empty texels), a its weight over the biome ground (2 x 2 samples per texel)
The geography is real (slope, rain, towns); the parcels are procedural. Deterministic: the same PNG on every run."""
import json
import math
import warnings
from pathlib import Path

import numpy as np
import yaml
from PIL import Image

import hexgrid
import terrain_bake as tb

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
CONTENT = ROOT / "content" / "map"
OUT = ROOT / "unity" / "Assets" / "Textures" / "Map" / "landuse.png"
WIDTH, HEIGHT = 1024, 2048
SLOPE_SMOOTH_M = 300.0           # the slope that decides "flat" is averaged over about this radius
FLAT_SLOPE = (0.035, 0.075)      # rise/run: fully arable below the first, none above the second
RAIN_FIELDS_MM = (180.0, 380.0)  # dry farming (barley in the northern Negev at about 200-250 mm)
RAIN_WET_MM = (250.0, 600.0)
VALLEY_IRRIGATED = 0.7
ORCHARD_BELOW_M = 150.0
ORCHARD_DECAY_KM = 7.0
LAND_INTO_WATER = 3              # texels the land's masks reach past the shoreline (the shader's waterline cuts them)
# The painted land (paint_land): field parcels, rims, hedges, block-edge tracks, garrigue, terraces and scrub, baked here
# rather than drawn in the shader: the same pattern as procedural shader code cost about 5 ms a frame at gameplay03 on
# WebGL even where no pixel ran it (docs/visual-v1.md, "Promo track: land use").
COLOUR_W, COLOUR_H = 2048, 4096  # about 79 m east-west x 105 m north-south per texel
TEXEL_M = 92.0                   # mean texel size (m), for the terrace fade
DILATE_PASSES = 32               # texels the colour spreads into empty ground: past the 16-texel footprint of mip 4
SUPERSAMPLE = 2                  # samples per texel per axis, box-filtered (the parcels' edges stay soft, not jagged)
BLOCK_M = 5200.0                 # a farm block, one angle per block
STRIP_M = 1000.0                 # strip width across the block (symbolic scale, like the kits)
TERRACE_STEP_M = 40.0            # real metres of rise per terrace
VERTICAL_EXAGGERATION = tb.VERTICAL_EXAGGERATION
PALETTE_BRIGHTNESS, PALETTE_MAX = 1.107, 0.95   # MapPalette.Brightness and MaxChannel: the land sits in the biome palette
SRGB = {  # the land's colours (sRGB, before PALETTE_BRIGHTNESS), sampled from promo/final/03's ground and tuned on renders
    "green": (0.46, 0.50, 0.31), "ochre": (0.78, 0.66, 0.44), "plough": (0.58, 0.47, 0.34), "fallow": (0.72, 0.66, 0.51),
    "grass": (0.52, 0.54, 0.33), "orchard": (0.52, 0.47, 0.31), "hedge": (0.27, 0.30, 0.18), "dust": (0.84, 0.76, 0.61),
    "soil": (0.66, 0.57, 0.46), "hillgrass": (0.56, 0.55, 0.37), "stone": (0.74, 0.68, 0.56), "scrub": (0.34, 0.36, 0.22),
}


def smoothstep(e0: float, e1: float, x: np.ndarray) -> np.ndarray:
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def bilinear(a: np.ndarray, fy: np.ndarray, fx: np.ndarray) -> np.ndarray:
    """a sampled at fractional (row, col) positions, clamped to the edges."""
    h, w = a.shape
    fy = np.clip(fy, 0, h - 1); fx = np.clip(fx, 0, w - 1)
    y0 = np.minimum(np.floor(fy).astype(int), h - 2); x0 = np.minimum(np.floor(fx).astype(int), w - 2)
    ty, tx = fy - y0, fx - x0
    return ((a[y0, x0] * (1 - tx) + a[y0, x0 + 1] * tx) * (1 - ty) + (a[y0 + 1, x0] * (1 - tx) + a[y0 + 1, x0 + 1] * tx) * ty)


def texel_km(meta: dict) -> tuple[np.ndarray, np.ndarray]:
    """(x km east of lon0, y km south of lat1) of every output texel centre, shape (HEIGHT, WIDTH)."""
    ys, xs = np.mgrid[0:HEIGHT, 0:WIDTH]
    return (xs + 0.5) / WIDTH * meta["width_km"], (ys + 0.5) / HEIGHT * meta["length_km"]


def slope_map(elev: np.ndarray, meta: dict) -> np.ndarray:
    dx, dy = tb.spacing_m(meta)
    gy, gx = np.gradient(elev.astype(np.float64), dy, dx)
    s = np.hypot(gx, gy)
    return tb.box_mean(s, max(1, round(SLOPE_SMOOTH_M / dy)), max(1, round(SLOPE_SMOOTH_M / dx)))


def rain_at(xk: np.ndarray, yk: np.ndarray, meta: dict) -> np.ndarray:
    pr = hexgrid.load_corrected_precip(CONTENT)
    m = pr["meta"]
    mm = pr["mm"].copy()
    # Sea cells are NaN: fill each with the mean of its finite neighbours, repeatedly, so the coast's rain reaches the shore.
    for _ in range(4):
        nan = np.isnan(mm)
        if not nan.any():
            break
        p = np.pad(mm, 1, mode="edge")
        stack = np.stack([p[1 + oy:1 + oy + mm.shape[0], 1 + ox:1 + ox + mm.shape[1]] for oy in (-1, 0, 1) for ox in (-1, 0, 1)])
        with np.errstate(invalid="ignore"), warnings.catch_warnings():
            warnings.simplefilter("ignore", RuntimeWarning)
            fill = np.nanmean(stack, axis=0)
        mm = np.where(nan, fill, mm)
    mm = np.nan_to_num(mm, nan=0.0)
    lon = meta["lon0"] + xk / hexgrid.km_per_deg_lon(meta)
    lat = meta["lat1"] - yk / hexgrid.KM_PER_DEG_LAT
    return bilinear(mm, (m["lat1"] - lat) / m["cell_deg"] - 0.5, (lon - m["lon0"]) / m["cell_deg"] - 0.5)


def biome_at(xk: np.ndarray, yk: np.ndarray, hexes: dict) -> np.ndarray:
    lookup = {(h["q"], h["r"]): h["biome"] for h in hexes["hexes"]}
    q, r = hexgrid.point_to_hex(xk.ravel(), yk.ravel(), hexes["hex_size_km"])
    return np.array([lookup.get(k, "sea") for k in zip(q, r)]).reshape(xk.shape)


def settlements_km(meta: dict) -> list[tuple[str, float, float]]:
    doc = yaml.safe_load((CONTENT / "settlements_1948.yaml").read_text())
    return [(s["id"], *hexgrid.lonlat_to_km(s["lon"], s["lat"], meta)) for s in doc["settlements"]]


def linear(srgb: tuple[float, float, float]) -> np.ndarray:
    """An SRGB colour at the palette's brightness, in linear light (the texture holds linear values, like the biome
    texture's sampled colour)."""
    c = np.minimum(np.array(srgb) * PALETTE_BRIGHTNESS, PALETTE_MAX)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def hash01(*ints: np.ndarray) -> np.ndarray:
    """A deterministic hash of integer lattice coordinates to [0, 1) (uint64 arithmetic: the same on every platform)."""
    h = np.uint64(0x9E3779B97F4A7C15)
    for i, v in enumerate(ints):
        k = np.asarray(v).astype(np.int64).astype(np.uint64) * np.uint64(0xBF58476D1CE4E5B9 + 2 * i)
        h = (h ^ k) * np.uint64(0x94D049BB133111EB)
        h ^= h >> np.uint64(31)
    return (h >> np.uint64(11)).astype(np.float64) / float(1 << 53)


def value_noise(x: np.ndarray, y: np.ndarray, seed: int) -> np.ndarray:
    xi, yi = np.floor(x), np.floor(y)
    fx, fy = x - xi, y - yi
    ux, uy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    xi, yi = xi.astype(np.int64), yi.astype(np.int64)
    a, b = hash01(xi, yi, seed), hash01(xi + 1, yi, seed)
    c, d = hash01(xi, yi + 1, seed), hash01(xi + 1, yi + 1, seed)
    return (a + (b - a) * ux) * (1 - uy) + (c + (d - c) * ux) * uy


def paint_samples(x: np.ndarray, y: np.ndarray, fields: np.ndarray, orchards: np.ndarray, wet: np.ndarray,
                  elev: np.ndarray, grad: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Linear colour (..., 3) and weight (...) of the land at map points (x m east, y m north; elev real metres, grad its
    rise per metre). Weight 0 leaves the biome's own ground."""
    col = {k: linear(v) for k, v in SRGB.items()}
    patch = value_noise(x / 2600.0 + 3.1, y / 2600.0 + 3.1, 1)          # one patch noise for every patchy choice
    # Farm blocks: a wobbled grid, each block turned by up to +-20 degrees (a few by 90) about its own centre.
    wx = 0.275 * np.sin(y / 4100.0 + np.sin(x / 6700.0))
    wy = 0.275 * np.sin(x / 3900.0 + np.sin(y / 5300.0))
    bpx, bpy = x / BLOCK_M + wx, y / BLOCK_M + wy
    bix, biy = np.floor(bpx), np.floor(bpy)
    bfx, bfy = bpx - bix, bpy - biy
    bh = hash01(bix, biy, 2)
    ang = (bh - 0.5) * 0.7 + np.where(bh > 0.8, math.pi / 2, 0.0)
    qx, qy = (bfx - 0.5) * BLOCK_M, (bfy - 0.5) * BLOCK_M
    lx, ly = np.cos(ang) * qx - np.sin(ang) * qy, np.sin(ang) * qx + np.cos(ang) * qy
    # Strips across the block, each cut into parcels of its own length (1.1-2.6 km).
    sx = lx / STRIP_M + bh * 7.0
    ci = np.floor(sx)
    ch = hash01(ci, bix, biy, 3)
    len_m = 1100.0 + 1500.0 * ch
    sy = ly / len_m + ch * 5.0
    pj = np.floor(sy)
    ph, pv, po = hash01(ci, pj, bix, biy, 4), hash01(ci, pj, bix, biy, 5), hash01(ci, pj, bix, biy, 6)
    edge_m = np.minimum(np.minimum(sx - ci, ci + 1 - sx) * STRIP_M, np.minimum(sy - pj, pj + 1 - sy) * len_m)
    # The crop: 0.30 green, 0.20 ochre, 0.17 ploughed, 0.13 fallow, 0.20 grass; in the citrus belt up to 55% groves.
    kinds = [(0.30, "green", 0.20), (0.50, "ochre", 0.08), (0.67, "plough", 0.24), (0.80, "fallow", 0.0), (1.01, "grass", 0.0)]
    crop = np.zeros(x.shape + (3,)); gain = np.zeros(x.shape); done = np.zeros(x.shape, dtype=bool)
    for upto, name, g in kinds:
        m = ~done & (ph < upto)
        crop[m] = col[name]; gain[m] = g; done |= m
    grove = po < orchards * 0.55
    crop[grove] = col["orchard"]; gain[grove] = 0.12
    rows = 0.6 * np.sin(lx * (2 * math.pi / 320.0))                     # rows (symbolic: 320 m apart, about 3 texels)
    crop *= ((1 + gain * rows) * (0.92 + 0.16 * pv))[..., None]
    rim = smoothstep(20.0, 90.0, edge_m)[..., None]                     # soft grassy rims (promo 03)
    farmed = col["grass"] * (1 - rim) + crop * rim
    hedge = ((edge_m < 18.0) & (pv > 0.45))[..., None]
    farmed = np.where(hedge, farmed * 0.3 + col["hedge"] * 0.7, farmed)
    block_edge_m = np.minimum(np.minimum(bfx, 1 - bfx), np.minimum(bfy, 1 - bfy)) * BLOCK_M
    edge_track = ((block_edge_m < 30.0) & (hash01(np.floor(bpx + 0.5), np.floor(bpy + 0.5), bix, biy, 7) > 0.65))[..., None]
    farmed = np.where(edge_track, col["dust"] * 0.85 + farmed * 0.15, farmed)
    farm_w = fields * smoothstep(0.08, 0.22, patch * 0.6 + fields * 0.5)

    # The wet unfarmed hills: patches of stony soil and grass (garrigue), terraces on the slopes, scrub.
    slope = 1.0 - 1.0 / np.sqrt(1.0 + (VERTICAL_EXAGGERATION * grad) ** 2)   # the shader's 1 - n.y on the drawn relief
    wild = wet * (1 - farm_w)
    hill = (col["soil"] * (1 - smoothstep(0.35, 0.65, patch))[..., None] + col["hillgrass"] * smoothstep(0.35, 0.65, patch)[..., None])
    tf = np.mod(elev / TERRACE_STEP_M, 1.0)
    steps_per_texel = grad * TEXEL_M / TERRACE_STEP_M
    terrace = (smoothstep(0.05, 0.10, slope) * (1 - smoothstep(0.45, 0.65, slope)) * smoothstep(120.0, 320.0, elev)
               * smoothstep(0.55, 0.70, 1 - patch) * (1 - smoothstep(0.35, 0.8, steps_per_texel)))[..., None]
    terraced = hill * (0.78 + 0.24 * tf)[..., None]
    terraced = np.where((tf > 0.75)[..., None], terraced * 0.65 + col["stone"] * 0.35, terraced)
    hill = hill * (1 - terrace) + terraced * terrace
    scrub = (0.14 + 0.2 * smoothstep(0.55, 0.85, patch)) * (1 - smoothstep(0.5, 0.8, slope))
    speck = (hash01(np.floor(x / 160.0), np.floor(y / 160.0), 8) < 0.30) * (1 - smoothstep(0.5, 0.8, slope)) * 0.7   # bushes
    hill = hill * (1 - np.maximum(scrub, speck))[..., None] + col["scrub"] * np.maximum(scrub, speck)[..., None]
    # Not on the steep faces, where the shader draws rock (ScGroundBase: smoothstep(0.22, 0.45, slope), review M5).
    wild_w = wild * smoothstep(80.0, 250.0, elev) * 0.7 * (1 - smoothstep(0.22, 0.45, slope))

    w = farm_w + wild_w
    colour = (farmed * farm_w[..., None] + hill * wild_w[..., None]) / np.maximum(w, 1e-6)[..., None]
    return colour, np.clip(w, 0.0, 1.0)


def paint_land(fields: np.ndarray, orchards: np.ndarray, wet: np.ndarray, elev: np.ndarray, meta: dict) -> np.ndarray:
    """RGBA8 COLOUR_W x COLOUR_H (row 0 = north): rgb the land's linear colour, a its weight, from SUPERSAMPLE^2 samples per
    texel. The masks are WIDTH x HEIGHT, the elevation the heightmap's (row 0 = north)."""
    width_m, length_m = meta["width_km"] * 1000, meta["length_km"] * 1000
    dx, dy = tb.spacing_m(meta)
    gy, gx = np.gradient(elev.astype(np.float64), dy, dx)
    grad = np.hypot(gx, gy)
    out = np.zeros((COLOUR_H, COLOUR_W, 4))
    ss = SUPERSAMPLE
    offs = (np.arange(ss) + 0.5) / ss
    chunk = 256
    for r0 in range(0, COLOUR_H, chunk):
        rows = np.arange(r0, min(r0 + chunk, COLOUR_H))
        fr = (rows[:, None] + offs[None, :]).ravel()                    # sample rows (texel units, 0 = north edge)
        fc = (np.arange(COLOUR_W)[:, None] + offs[None, :]).ravel()
        FR, FC = np.meshgrid(fr, fc, indexing="ij")
        x = FC / COLOUR_W * width_m
        y = length_m - FR / COLOUR_H * length_m
        my, mx = FR / COLOUR_H * HEIGHT - 0.5, FC / COLOUR_W * WIDTH - 0.5
        hy, hx = FR / COLOUR_H * (meta["height"] - 1), FC / COLOUR_W * (meta["width"] - 1)
        c, w = paint_samples(x, y, bilinear(fields, my, mx), bilinear(orchards, my, mx), bilinear(wet, my, mx),
                             bilinear(elev.astype(np.float64), hy, hx), bilinear(grad, hy, hx))
        rgbw = np.concatenate([c * w[..., None], w[..., None]], axis=-1)   # premultiplied, so the box filter weighs colour
        n = len(rows)
        rgbw = rgbw.reshape(n, ss, COLOUR_W, ss, 4).mean(axis=(1, 3))
        out[rows] = np.concatenate([rgbw[..., :3] / np.maximum(rgbw[..., 3:], 1e-6), rgbw[..., 3:]], axis=-1)
    out[..., :3] = dilate_colour(out[..., :3], out[..., 3] > 0.5 / 255)
    out[..., :3] = encode_srgb(out[..., :3])
    return tb.to_u8(out)


def encode_srgb(c: np.ndarray) -> np.ndarray:
    """Linear -> sRGB: the texture imports as sRGB, so the GPU decodes it and 8 bits (and BC3's endpoints) spend their
    precision where the eye does (review M3: linear storage stepped the dark hedge and scrub tones by 5-15%)."""
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def shift(a: np.ndarray, oy: int, ox: int) -> np.ndarray:
    """out[y, x] = a[y + oy, x + ox] over the first two axes, the border repeated beyond it."""
    p = np.pad(a, ((1, 1), (1, 1)) + ((0, 0),) * (a.ndim - 2), mode="edge")
    return p[1 + oy:1 + oy + a.shape[0], 1 + ox:1 + ox + a.shape[1]]


def dilate_colour(rgb: np.ndarray, filled: np.ndarray, passes: int = DILATE_PASSES) -> np.ndarray:
    """Spreads the land's colour into the empty texels (weight 0): each pass gives every empty texel the mean colour of its
    filled 4-neighbours, the rest end as the mean land colour. Mips and BC3 blocks then blend land colour with land
    colour, not with black (review M4: the colour darkened by 8% at partial weights by mip 4)."""
    rgb, filled = rgb.copy(), filled.copy()
    for _ in range(passes):
        acc = np.zeros_like(rgb); n = np.zeros(filled.shape)
        for oy, ox in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            f = shift(filled.astype(np.float64), oy, ox)
            acc += shift(rgb, oy, ox) * f[..., None]; n += f
        grow = ~filled & (n > 0)
        rgb[grow] = acc[grow] / n[grow][..., None]
        filled |= grow
    rgb[~filled] = rgb[filled].mean(axis=0) if filled.any() else 0.0
    return rgb


def bake() -> np.ndarray:
    elev, meta = tb.load_heightmap(MAP_DIR)
    hexes = json.loads((CONTENT / "hexes.json").read_text())
    xk, yk = texel_km(meta)
    hy = yk / meta["length_km"] * (meta["height"] - 1)       # heightmap row (0 = north) and column of each texel
    hx = xk / meta["width_km"] * (meta["width"] - 1)
    e = bilinear(elev.astype(np.float64), hy, hx)
    slope = bilinear(slope_map(elev, meta), hy, hx)
    dx, dy = tb.spacing_m(meta)
    water_hm = tb.water_mask(elev, dx, dy).astype(np.float64)
    water = bilinear(water_hm, hy, hx) > 0.5
    rain = rain_at(xk, yk, meta)
    biome = biome_at(xk, yk, hexes)
    # Hex-wise inputs, softened over about 1.5 km so the land does not step at hex edges (review M6).
    sx_t, sy_t = WIDTH / meta["width_km"], HEIGHT / meta["length_km"]
    soften = lambda a: tb.box_mean(tb.box_mean(a.astype(np.float64), round(0.75 * sy_t), round(0.75 * sx_t)),
                                   round(0.75 * sy_t), round(0.75 * sx_t))
    valley = soften(biome == "valley")

    flat = 1.0 - smoothstep(*FLAT_SLOPE, slope)
    farm_rain = np.maximum(smoothstep(*RAIN_FIELDS_MM, rain), valley * VALLEY_IRRIGATED)
    fields = flat * farm_rain

    towns = settlements_km(meta)
    d_town = np.full(xk.shape, np.inf)
    for _, x, y in towns:
        d_town = np.minimum(d_town, np.hypot(xk - x, yk - y))
    lowland = soften((e < ORCHARD_BELOW_M) & np.isin(biome, ("coastal_plain", "valley")))
    orchards = lowland * flat * farm_rain * np.clip(1.4 * np.exp(-d_town / ORCHARD_DECAY_KM) - 0.15, 0, 1)

    wet = smoothstep(*RAIN_WET_MM, rain)

    # The land reaches LAND_INTO_WATER texels (about 0.5 km) into the water, so it meets the shore wherever the shader's
    # waterline puts it, with no bare strip (review M7); the shader draws water over it.
    land = water.copy()
    for _ in range(LAND_INTO_WATER):
        land = tb.shrink(land)
    land = ~land
    rgba = paint_land(fields * land, orchards * land, wet * land, elev, meta)
    return rgba


def main() -> None:
    rgba = bake()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(OUT)
    print(f"wrote {OUT} ({COLOUR_W} x {COLOUR_H}): land weight mean {rgba[..., 3].mean() / 255:.3f}")


if __name__ == "__main__":
    main()
