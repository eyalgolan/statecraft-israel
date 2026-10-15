# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0", "pyyaml==6.0.3"]
# ///
"""The minimap's painted relief picture (HUD round 2, #398), in the look of promo/final/03-gameplay-1936.png's minimap.

Usage: uv run tools/minimap_bake.py   (a few seconds; no network)
Reads only files already in the repository, all of them the map's own bakes or data:
  unity/Assets/StreamingAssets/map/heightmap.*      the relief (hill shade, lit from the north-west)
  unity/Assets/Resources/Map/BiomeColours.bytes     the ground colour the map draws (MapBuilder.BiomePixels, row 0 south)
  unity/Assets/Textures/Map/landuse.png             the fields, orchards and terraces over it (tools/land_bake.py)
  unity/Assets/Textures/Map/coast.png               the signed distance to the shore (tools/terrain_bake.py): the sea and lakes
  content/map/subdistrict_hexes_1948.yaml           which land hexes lie outside Mandatory Palestine (dimmed, with a rim)
Writes unity/Assets/Resources/Map/MinimapRelief.png, RGB8, WIDTH x HEIGHT (powers of two), row 0 = north, over the heightmap's bounds
(the hex grid's bounds, so MinimapPresenter maps a click exactly as before). It is drawn once by the GPU as a UI
background: no runtime painting, no per-frame cost. Deterministic; tools/tests/test_minimap_bake.py re-bakes it in CI and
compares the pixels."""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import heightmap_codec  # noqa: E402
import hexgrid  # noqa: E402
import yaml12  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
BIOME = ROOT / "unity" / "Assets" / "Resources" / "Map" / "BiomeColours.bytes"
LANDUSE = ROOT / "unity" / "Assets" / "Textures" / "Map" / "landuse.png"
COAST = ROOT / "unity" / "Assets" / "Textures" / "Map" / "coast.png"
REGIONS = ROOT / "content" / "map" / "subdistrict_hexes_1948.yaml"
HEXES = ROOT / "content" / "map" / "hexes.json"
OUT = ROOT / "unity" / "Assets" / "Resources" / "Map" / "MinimapRelief.png"

WIDTH, HEIGHT = 256, 1024       # powers of two, so Unity block-compresses it with its mipmaps (an NPOT texture stays
                                # uncompressed RGB: review of #584). The frame is 161.5 x 431.2 km, so a pixel is about
                                # 0.63 x 0.42 km; MinimapPresenter sizes the picture to the frame's aspect, not the texture's.
SUPER = 2                       # samples per pixel per axis, box-filtered
COAST_RANGE_M = 12000.0         # coast.png: 0.5 + d / (2 COAST_RANGE_M), d < 0 in water
EXAGGERATION = 12.0             # relief for a 0.5 km pixel: the hills must read at minimap size
RELIEF_SMOOTH = 3               # heights averaged over 3 x 3 samples first: promo 03's broad ridges, not every gully
SUN_AZIMUTH, SUN_ALTITUDE = 315.0, 38.0   # the cartographer's light, from the north-west
LANDUSE_WEIGHT = 0.55           # the fields and orchards, softened: a painting, not the ground texture
PARCHMENT = np.array([196, 178, 142], np.float32) / 255   # the promo's dry land between the tinted areas
PARCHMENT_MIX = 0.30
BRUSH = 6                       # the land colour is averaged over BRUSH x BRUSH samples (3 x 3 pixels, about 1.5 km)
SATURATION = 1.10
LAND_TONE = 0.86                # the land a little darker, toward promo 03 minimap's mean (luma about 0.47)
SHADOW = np.array([0.66, 0.60, 0.62], np.float32)         # warm-cool split: shadows a little violet,
LIGHT = np.array([1.10, 1.06, 0.96], np.float32)          # lit slopes a little warm
SEA_DEEP = np.array([17, 41, 66], np.float32) / 255       # promo 03 minimap's open sea
SEA_SHALLOW = np.array([47, 92, 118], np.float32) / 255   # its lighter water along the shore
SEA_SHALLOW_M = 6000.0
SURF = np.array([196, 206, 204], np.float32) / 255        # the thin pale line at the water's edge
OUTSIDE_DIM = 0.72              # land outside Mandatory Palestine: paler and greyer, as the promo's land beyond the play area
RIM = np.array([214, 178, 104], np.float32) / 255         # the gold rim of Mandatory Palestine
RIM_ALPHA = 0.45
OUTSIDE_ID = 17


def resize(a: np.ndarray, w: int, h: int) -> np.ndarray:
    """Float resample of a 2-D array (row 0 north) to h x w: box-filtered down, bilinear up."""
    up = w > a.shape[1]
    f = Image.Resampling.BILINEAR if up else Image.Resampling.BOX
    return np.asarray(Image.fromarray(a.astype(np.float32), mode="F").resize((w, h), f), np.float32)


def resize_rgb(a: np.ndarray, w: int, h: int) -> np.ndarray:
    return np.stack([resize(a[..., c], w, h) for c in range(a.shape[-1])], axis=-1)


def heights(w: int, h: int) -> tuple[np.ndarray, float, float]:
    meta = json.loads((MAP_DIR / "heightmap.json").read_text())
    raw = heightmap_codec.read(MAP_DIR, meta)
    elev = meta["elev_min_m"] + raw.astype(np.float64) / 65535.0 * (meta["elev_max_m"] - meta["elev_min_m"])
    return resize(elev, w, h), meta["width_km"] * 1000 / w, meta["length_km"] * 1000 / h


def hillshade(z: np.ndarray, dx: float, dy: float) -> np.ndarray:
    """Lambert shade of the exaggerated surface, 0..1 (row 0 north, so +y is south)."""
    zz = z * EXAGGERATION
    gy, gx = np.gradient(zz, dy, dx)
    nx, ny, nz = -gx, gy, np.ones_like(zz)   # gy is d/d(south); north-up normal
    n = np.sqrt(nx * nx + ny * ny + nz * nz)
    az, alt = math.radians(SUN_AZIMUTH), math.radians(SUN_ALTITUDE)
    lx, ly, lz = math.sin(az) * math.cos(alt), math.cos(az) * math.cos(alt), math.sin(alt)
    return np.clip((nx * lx + ny * ly + nz * lz) / n, 0, 1)


def biome_rgb(w: int, h: int) -> np.ndarray:
    data = BIOME.read_bytes()
    res = int.from_bytes(data[8:12], "little")
    px = np.frombuffer(data, np.uint8, offset=16).reshape(res, res, 3)[::-1].astype(np.float32) / 255   # row 0 north
    return resize_rgb(px, w, h)


def landuse(w: int, h: int) -> tuple[np.ndarray, np.ndarray]:
    im = np.asarray(Image.open(LANDUSE).convert("RGBA"), np.float32) / 255
    a = im[..., 3]
    rgb = resize_rgb(im[..., :3] * a[..., None], w, h)
    alpha = resize(a, w, h)
    return rgb / np.maximum(alpha, 1e-4)[..., None], alpha


def coast_m(w: int, h: int) -> np.ndarray:
    c = np.asarray(Image.open(COAST).convert("L"), np.float32) / 255
    return resize((c - 0.5) * 2 * COAST_RANGE_M, w, h)


def outside(w: int, h: int) -> np.ndarray:
    """1 where the pixel's hex is land outside Mandatory Palestine (hexgrid's projection and rounding)."""
    doc = yaml12.load_path(REGIONS)
    out = {(q, r) for q, r, rid in doc["hexes"] if rid == OUTSIDE_ID}
    grid = json.loads(HEXES.read_text())
    b, size = grid["bounds"], grid["hex_size_km"]
    xs = (np.arange(w) + 0.5) / w * (b["lon1"] - b["lon0"]) * hexgrid.km_per_deg_lon(b)
    ys = (np.arange(h) + 0.5) / h * (b["lat1"] - b["lat0"]) * hexgrid.KM_PER_DEG_LAT
    X, Y = np.meshgrid(xs, ys)
    qs, rs = hexgrid.point_to_hex(X.ravel(), Y.ravel(), size)
    return np.array([(q, r) in out for q, r in zip(qs, rs)], np.float32).reshape(h, w)


def smooth(t: np.ndarray, a: float, b: float) -> np.ndarray:
    x = np.clip((t - a) / (b - a), 0, 1)
    return x * x * (3 - 2 * x)


def bake() -> np.ndarray:
    w, h = WIDTH * SUPER, HEIGHT * SUPER
    z, dx, dy = heights(w, h)
    z = resize(resize(z, w // RELIEF_SMOOTH, h // RELIEF_SMOOTH), w, h)
    shade = hillshade(z, dx, dy)
    flat = math.sin(math.radians(SUN_ALTITUDE))       # the shade of level ground
    ground = biome_rgb(w, h)
    land_rgb, land_a = landuse(w, h)
    k = (land_a * LANDUSE_WEIGHT)[..., None]
    land = ground * (1 - k) + land_rgb * k
    land = resize_rgb(resize_rgb(land, w // BRUSH, h // BRUSH), w, h)   # broad strokes: no field speckle at minimap size
    grey = land.mean(axis=-1, keepdims=True)
    land = grey + (land - grey) * SATURATION
    land = (land * (1 - PARCHMENT_MIX) + PARCHMENT * PARCHMENT_MIX) * LAND_TONE
    rel = np.clip((shade - flat) / (1 - flat), -1, 1)[..., None]   # < 0 turned away, > 0 towards the light
    land = np.where(rel < 0, land * (1 + (SHADOW - 1) * -rel), land * (1 + (LIGHT - 1) * rel))
    out = outside(w, h)[..., None]
    grey = land.mean(axis=-1, keepdims=True)
    land = land * (1 - out) + (land * 0.5 + grey * 0.5) * OUTSIDE_DIM * out + 0.06 * out

    d = coast_m(w, h)[..., None]                       # metres, < 0 in water
    sea = SEA_DEEP + (SEA_SHALLOW - SEA_DEEP) * (1 - smooth(-d, 0, SEA_SHALLOW_M))
    surf = (1 - smooth(-d, 0, 900)) * (d < 0)
    sea = sea + (SURF - sea) * surf * 0.55
    water = smooth(-d, -150, 150)                      # a soft shore line
    rgb = land * (1 - water) + sea * water

    # the rim of Mandatory Palestine: where a land pixel inside meets land outside
    o = out[..., 0]
    edge = np.zeros_like(o)
    for sy, sx in ((0, 1), (1, 0), (0, -1), (-1, 0)):
        edge = np.maximum(edge, np.abs(o - np.roll(o, (sy, sx), axis=(0, 1))))
    edge *= (1 - water[..., 0])
    rgb = rgb + (RIM - rgb) * (edge * RIM_ALPHA)[..., None]

    rgb = resize_rgb(np.clip(rgb, 0, 1), WIDTH, HEIGHT)
    return np.clip(np.round(rgb * 255), 0, 255).astype(np.uint8)


def main() -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(bake(), "RGB").save(OUT, optimize=True)
    print(f"wrote {OUT.relative_to(ROOT)} {WIDTH}x{HEIGHT}")


if __name__ == "__main__":
    main()
