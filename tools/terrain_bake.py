# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0"]
# ///
"""Heightmap -> the baked shading textures of the terrain and sea shaders (no network: reads only the committed heightmap).

Usage: uv run tools/terrain_bake.py   (about 10 seconds)
Reads unity/Assets/StreamingAssets/map/heightmap.r16z (heightmap_codec) + heightmap.json; writes, 2048 x 2048, row 0 = north:
  unity/Assets/Textures/Map/terrain_bake.png  RGB8: R ambient occlusion (horizon search to AO_RADIUS_M; 1 = open),
      G curvature (0.5 flat, > 0.5 convex), B sky-view factor (horizon search to SKY_RADIUS_M)
  unity/Assets/Textures/Map/ao.png            L8: the same ambient occlusion as terrain_bake.png's R, on its own so Unity
      imports it uncompressed (R8): block compression stepped it by 2+ levels on most broad slopes (Task 6 fix round 1)
  unity/Assets/Textures/Map/coast.png         L8: 0.5 + d / (2 COAST_RANGE_M), clamped; d = signed distance (m) to the
      nearest shoreline, < 0 in water (open sea: |elevation| < SEA_EPS_M and connected to the map's border; lakes: flat
      water below LAKE_BELOW_M covering at least MIN_LAKE_AREA_KM2), the rules tools/hexgrid.py uses for its sea and lake
      biomes; the area floor drops the Karameh reservoir (0.3 km2, built in the 1990s) and dike slivers between the pans.
      The land side is beach-scaled, not metres, on the dune coasts (BEACH_BANDS): beach_widened takes BEACH_EXTRA_M out
      of the distance past the wet sand, so the shader's beach (ScShore) reaches that much further inland there
Heights are multiplied by VERTICAL_EXAGGERATION (MapBuilder.VerticalExaggeration) so the shading matches the relief the
game draws. Texel (j, i) is the mean of heightmap samples j..j+1, i..i+1, so its centre sits at terrain UV (j + 0.5) / 2048.
Distances are exact Euclidean (a Felzenszwalb-Huttenlocher two-pass transform) with each axis' own sample spacing (about
79 m east-west, 211 m north-south). The PNG pixels are reproducible; their bytes are identical only on the same toolchain."""
import json
import math
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

import heightmap_codec

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT_DIR = ROOT / "unity" / "Assets" / "Textures" / "Map"
VERTICAL_EXAGGERATION = 3.0
AO_RADIUS_M = 2000.0
SKY_RADIUS_M = 12000.0
DIRECTIONS = 16
STEPS = 16
CURVATURE_RADIUS_M = 600.0
CURVATURE_SCALE_M = 150.0      # exaggerated metres above or below the local mean that reach 1 or 0
SEA_EPS_M = 0.5
LAKE_BELOW_M = -150.0
LAKE_FLAT_M = 0.25             # largest height range in a 3x3 window that is still a water surface
MIN_LAKE_AREA_KM2 = 1.0        # smaller flat pools are land (Karameh 0.30 km2; the smallest kept pan is 1.05 km2)
COAST_RANGE_M = 12000.0        # StatecraftMap.hlsl ScCoastRangeM
# Sea round 3 (#388): a wider beach on the dune coasts, as promo 03 draws south of Jaffa (owner, 15 October 2026: geographic,
# through the coast map, about 2.5-3 km). The sands lie from Tel Aviv southwards and line Haifa Bay; the kurkar ridges hold
# the coast from Tel Aviv northwards (Wikipedia: "Kurkar", "Haifa Bay", "Holon", "Nitzanim", retrieved 2026-10-15). The
# widths are art direction, and the cut-off at Jaffa is interpolated between Jaffa (32.05 N) and Tel Aviv, not a sourced
# line. Each band is (lon west, lon east, lat south, lat north, ramp in degrees): the extra width is full inside it and
# falls to 0 over the ramp at each edge. Unity's tree keep-out reads a twin (Kits/BeachBands.cs, pinned by a test).
BEACH_EXTRA_M = 1800.0         # ScShore's 900 m beach becomes about 2.7 km
BEACH_BANDS = ((34.0, 34.86, 31.0, 32.07, 0.04),     # Jaffa south to the map's southern coast (full south of 32.03 N)
               (35.02, 35.2, 32.77, 32.94, 0.02))    # Haifa Bay, from the Kishon's mouth to Acre
BEACH_KNEE_M = (230.0, 280.0)  # metres past the wet sand (ScShore: 60-160 m) and past a shore texel's diagonal sample
                               # (hypot(79, 211) m), so every texel the waterline crosses keeps its metric value; the extra
                               # is gone by 280 m, which encodes as level 130 (235 m) at most, inside ScShore's narrowest
                               # full sand (0.55 x 0.6 x 900 = 297 m): the squeezed stretch is all full sand, so its 8-bit
                               # steps never show


def load_heightmap(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = heightmap_codec.read(map_dir, meta)
    elev = meta["elev_min_m"] + raw.astype(np.float64) / 65535.0 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev.astype(np.float32), meta


def spacing_m(meta: dict) -> tuple[float, float]:
    return meta["width_km"] * 1000 / (meta["width"] - 1), meta["length_km"] * 1000 / (meta["height"] - 1)


def shifted(a: np.ndarray, oy: int, ox: int) -> np.ndarray:
    """out[y, x] = a[y + oy, x + ox], the border's values repeated beyond it."""
    h, w = a.shape
    ys = np.clip(np.arange(h) + oy, 0, h - 1)
    xs = np.clip(np.arange(w) + ox, 0, w - 1)
    return a[ys[:, None], xs[None, :]]


def openness(z: np.ndarray, dx: float, dy: float, radius_m: float, directions: int = DIRECTIONS, steps: int = STEPS) -> np.ndarray:
    """Sky-view factor: 1 - the mean over directions of sin(horizon elevation), the horizon searched out to radius_m and
    never below 0 (a peak sees the whole upper hemisphere: 1)."""
    acc = np.zeros(z.shape, dtype=np.float32)
    for k in range(directions):
        theta = 2 * math.pi * k / directions
        best = np.zeros(z.shape, dtype=np.float32)      # tan of the highest horizon so far
        seen: set[tuple[int, int]] = set()
        for s in range(1, steps + 1):
            r = radius_m * (s / steps) ** 1.5             # denser near the point
            ox, oy = int(round(r * math.cos(theta) / dx)), int(round(-r * math.sin(theta) / dy))
            if (ox, oy) == (0, 0) or (ox, oy) in seen:
                continue
            seen.add((ox, oy))
            np.maximum(best, (shifted(z, oy, ox) - z) / math.hypot(ox * dx, oy * dy), out=best)
        acc += np.sin(np.arctan(best))
    return (1.0 - acc / directions).astype(np.float32)


def box_mean(a: np.ndarray, ry: int, rx: int) -> np.ndarray:
    """Mean over the (2 ry + 1) x (2 rx + 1) window, the border repeated (integral image)."""
    p = np.pad(a.astype(np.float64), ((ry, ry), (rx, rx)), mode="edge")
    c = np.zeros((p.shape[0] + 1, p.shape[1] + 1))
    c[1:, 1:] = p.cumsum(0).cumsum(1)
    h, w = a.shape
    ky, kx = 2 * ry + 1, 2 * rx + 1
    s = c[ky:ky + h, kx:kx + w] - c[0:h, kx:kx + w] - c[ky:ky + h, 0:w] + c[0:h, 0:w]
    return (s / (ky * kx)).astype(np.float32)


def curvature(z: np.ndarray, dx: float, dy: float) -> np.ndarray:
    """0.5 + (z - local mean) / (2 CURVATURE_SCALE_M), clamped to [0, 1]; the local mean is two box passes (a tent)."""
    rx, ry = max(1, round(CURVATURE_RADIUS_M / dx)), max(1, round(CURVATURE_RADIUS_M / dy))
    mean = box_mean(box_mean(z, ry, rx), ry, rx)
    return np.clip(0.5 + (z - mean) / (2 * CURVATURE_SCALE_M), 0.0, 1.0).astype(np.float32)


def grow(mask: np.ndarray) -> np.ndarray:
    out = mask.copy()
    out[1:, :] |= mask[:-1, :]; out[:-1, :] |= mask[1:, :]
    out[:, 1:] |= mask[:, :-1]; out[:, :-1] |= mask[:, 1:]
    return out


def shrink(mask: np.ndarray) -> np.ndarray:
    return ~grow(~mask)


def border_connected(candidate: np.ndarray) -> np.ndarray:
    """The pixels of candidate connected (4-neighbour) to the map's border through candidate pixels."""
    reached = np.zeros_like(candidate)
    for edge in (np.s_[0, :], np.s_[-1, :], np.s_[:, 0], np.s_[:, -1]):
        reached[edge] = candidate[edge]
    while True:
        before = reached
        for _ in range(16):                          # 16 growth steps per convergence check
            reached = grow(reached) & candidate
        if np.array_equal(before, reached):
            return reached


def components(mask: np.ndarray) -> list[tuple[np.ndarray, np.ndarray]]:
    """The 4-connected components of mask, each as (rows, cols), in scan order."""
    label = np.zeros(mask.shape, dtype=np.int32)
    h, w = mask.shape
    out = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if label[y0, x0]:
            continue
        n = len(out) + 1
        label[y0, x0] = n
        queue, ys, xs = deque([(int(y0), int(x0))]), [], []
        while queue:
            y, x = queue.popleft()
            ys.append(y); xs.append(x)
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not label[ny, nx]:
                    label[ny, nx] = n
                    queue.append((ny, nx))
        out.append((np.array(ys), np.array(xs)))
    return out


def lake_mask(elev: np.ndarray, dx: float, dy: float) -> np.ndarray:
    hi, lo = elev.copy(), elev.copy()
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            s = shifted(elev, oy, ox)
            np.maximum(hi, s, out=hi); np.minimum(lo, s, out=lo)
    flat = (elev < LAKE_BELOW_M) & (hi - lo <= LAKE_FLAT_M)
    opened = grow(grow(shrink(shrink(flat)))) & flat     # an opening: specks of flat rift floor drop out
    lakes = np.zeros_like(opened)
    for ys, xs in components(opened):
        if len(ys) * dx * dy / 1e6 >= MIN_LAKE_AREA_KM2:
            lakes[ys, xs] = True
    return lakes


def water_mask(elev: np.ndarray, dx: float, dy: float) -> np.ndarray:
    return border_connected(np.abs(elev) < SEA_EPS_M) | lake_mask(elev, dx, dy)


def column_distance(target: np.ndarray) -> np.ndarray:
    """Samples to the nearest target pixel in the same column (inf where the column has none)."""
    h = target.shape[0]
    up = np.full(target.shape, np.inf)
    down = np.full(target.shape, np.inf)
    run = np.full(target.shape[1], np.inf)
    for y in range(h):
        run = np.where(target[y], 0.0, run + 1); up[y] = run
    run = np.full(target.shape[1], np.inf)
    for y in range(h - 1, -1, -1):
        run = np.where(target[y], 0.0, run + 1); down[y] = run
    return np.minimum(up, down)


def lower_envelope(f: np.ndarray, step: float) -> np.ndarray:
    """Felzenszwalb-Huttenlocher: d[q] = min over p of (step (q - p))^2 + f[p], in O(n); f may hold inf."""
    n = len(f)
    finite = np.nonzero(np.isfinite(f))[0]
    if len(finite) == 0:
        return np.full(n, np.inf)
    pos = [float(p) * step for p in finite]
    val = [float(f[p]) for p in finite]
    v = [0]                                   # indices into pos/val of the parabolas on the envelope
    z = [-math.inf, math.inf]                 # their boundaries
    for k in range(1, len(pos)):
        while True:
            j = v[-1]
            s = ((val[k] + pos[k] * pos[k]) - (val[j] + pos[j] * pos[j])) / (2 * (pos[k] - pos[j]))
            if s <= z[-2]:
                v.pop(); z.pop()
            else:
                break
        z[-1] = s
        v.append(k); z.append(math.inf)
    out = np.empty(n)
    k = 0
    for q in range(n):
        x = q * step
        while z[k + 1] < x:
            k += 1
        j = v[k]
        out[q] = (x - pos[j]) ** 2 + val[j]
    return out


def distance_to(target: np.ndarray, dx: float, dy: float, cap: float) -> np.ndarray:
    """Exact Euclidean distance (m, float64, each axis its own spacing) to the nearest target pixel, capped at cap: the
    column pass, then the Felzenszwalb-Huttenlocher lower envelope along each row."""
    g = column_distance(target) * dy
    d2 = np.stack([lower_envelope(row * row, dx) for row in g])
    return np.minimum(np.sqrt(d2), cap)


def to_texels(a: np.ndarray) -> np.ndarray:
    return (0.25 * (a[:-1, :-1] + a[1:, :-1] + a[:-1, 1:] + a[1:, 1:])).astype(np.float32)


def to_u8(a: np.ndarray) -> np.ndarray:
    return np.clip(np.round(a * 255), 0, 255).astype(np.uint8)


def smoothstep01(t: np.ndarray) -> np.ndarray:
    t = np.clip(t, 0.0, 1.0)
    return t * t * (3 - 2 * t)


def beach_extra_m(lon: np.ndarray, lat: np.ndarray) -> np.ndarray:
    """The extra beach width (m) at each point: BEACH_EXTRA_M inside a band of BEACH_BANDS, ramped to 0 at its edges."""
    w = np.zeros(np.broadcast(lon, lat).shape)
    for lon0, lon1, lat0, lat1, ramp in BEACH_BANDS:
        w = np.maximum(w, smoothstep01((lon - lon0) / ramp) * smoothstep01((lon1 - lon) / ramp)
                       * smoothstep01((lat - lat0) / ramp) * smoothstep01((lat1 - lat) / ramp))
    return w * BEACH_EXTRA_M


def beach_widened(d: np.ndarray, extra: np.ndarray) -> np.ndarray:
    """The land side's beach-scaled distance: metres to BEACH_KNEE_M[0] (the waterline's ramps and the wet sand), then
    squeezed so that BEACH_KNEE_M[1] + extra lands on BEACH_KNEE_M[1], then metres less the extra. Water (d < 0) and
    extra 0 pass through unchanged."""
    a, b = BEACH_KNEE_M
    squeezed = a + (d - a) * (b - a) / (b - a + extra)
    return np.where((d <= a) | (extra <= 0), d, np.where(d >= b + extra, d - extra, squeezed))


def encode_coast(signed_m: np.ndarray, range_m: float = COAST_RANGE_M) -> np.ndarray:
    return to_u8(0.5 + signed_m / (2 * range_m))


def bake(elev: np.ndarray, meta: dict) -> tuple[np.ndarray, np.ndarray]:
    dx, dy = spacing_m(meta)
    z = elev * np.float32(VERTICAL_EXAGGERATION)
    ao, sky, curv = openness(z, dx, dy, AO_RADIUS_M), openness(z, dx, dy, SKY_RADIUS_M), curvature(z, dx, dy)
    water = water_mask(elev, dx, dy)
    h, w = elev.shape
    lon = meta["lon0"] + np.arange(w)[None, :] / (w - 1) * (meta["lon1"] - meta["lon0"])
    lat = meta["lat1"] - np.arange(h)[:, None] / (h - 1) * (meta["lat1"] - meta["lat0"])   # row 0 = north
    land_m = beach_widened(distance_to(water, dx, dy, COAST_RANGE_M), beach_extra_m(lon, lat))
    signed = np.where(water, -distance_to(~water, dx, dy, COAST_RANGE_M), land_m)
    rgb = np.stack([to_u8(to_texels(ao)), to_u8(to_texels(curv)), to_u8(to_texels(sky))], axis=-1)
    return rgb, encode_coast(to_texels(signed.astype(np.float32)))


def main(map_dir: Path = MAP_DIR, out_dir: Path = OUT_DIR) -> None:
    elev, meta = load_heightmap(map_dir)
    rgb, coast = bake(elev, meta)
    out_dir.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgb, "RGB").save(out_dir / "terrain_bake.png")
    Image.fromarray(coast, "L").save(out_dir / "coast.png")
    Image.fromarray(np.ascontiguousarray(rgb[..., 0]), "L").save(out_dir / "ao.png")
    print(f"wrote {out_dir / 'terrain_bake.png'}, {out_dir / 'ao.png'} and {out_dir / 'coast.png'} ({coast.shape[1]} x {coast.shape[0]}): "
          f"water {np.mean(coast < 128):.1%} of texels; AO mean {rgb[..., 0].mean() / 255:.3f}, sky-view mean {rgb[..., 2].mean() / 255:.3f}")


if __name__ == "__main__":
    main()
