#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy>=2", "pillow>=11"]
# ///
"""Paints the town kits' one shared atlas (#2428) from code: no photographs, no AI images, no input files.

    uv run asset-sources/kit-atlas/make_kit_atlas.py

writes unity/Assets/Art/Kits/kit_atlas.png: a 4 x 4 grid of 512 px cells, which Unity imports as one Texture2DArray of 16
layers (kit_atlas.png.meta), so each cell tiles and has its own mipmaps with no bleeding from its neighbours. The same
code gives the same bytes.

Rules for a cell:
- Albedo only. No lighting is painted in: no cast shadow, no sun side, no ambient occlusion. Glass and doors are dark
  because they are dark, not because they are shaded; the sun, shadows and grade come from Kit.shader.
- Plaster and stone are near white and neutral. The building's own vertex colour tints them, so each town keeps the palette
  it had, and a block's jitter still reads.
- Every cell tiles in both directions. A facade cell (the first five) is one ground storey (the bottom quarter) under three
  upper storeys, and Kit.shader repeats the upper three. So a facade cell's wall is 12 m wide and 13.2 m high, a storey being
  3.3 m as everywhere in the kits. KitMesh.Scale must match the sizes in CELLS.
"""
from __future__ import annotations

import hashlib
import sys
from pathlib import Path

import numpy as np
from PIL import Image

N = 512                 # cell side, px
STOREY = N // 4         # px: a storey is a quarter of a facade cell's height
OUT = Path(__file__).resolve().parents[2] / "unity/Assets/Art/Kits/kit_atlas.png"

# Layer order: KitMesh's Facade enum (layer + 1 = its value / 17). (name, metres across, metres high).
CELLS = [
    ("white_city", 12.0, 13.2), ("stone_facade", 12.0, 13.2), ("village_facade", 12.0, 13.2), ("mud_facade", 12.0, 13.2),
    ("moshava_facade", 12.0, 13.2), ("plaster", 12.0, 12.0), ("ashlar", 6.0, 6.0), ("rubble", 4.0, 4.0),
    ("roof_tile", 4.0, 4.0), ("flat_roof", 16.0, 16.0), ("ribbon_glass", 6.0, 1.2), ("balcony_rail", 12.0, 1.25),
    ("corrugated", 4.0, 4.0), ("planks", 4.0, 4.0), ("spare_a", 4.0, 4.0), ("spare_b", 4.0, 4.0),
]


# ---- periodic noise ----------------------------------------------------------------------------------------------------

def noise(seed: int, beta: float = 1.6, stretch: tuple[float, float] = (1.0, 1.0)) -> np.ndarray:
    """Tiling spectral noise in about [-1, 1]: beta sets how smooth; a large stretch on an axis elongates the noise along it."""
    rng = np.random.default_rng(seed)
    white = rng.standard_normal((N, N))
    fy, fx = np.fft.fftfreq(N)[:, None] * stretch[1], np.fft.fftfreq(N)[None, :] * stretch[0]
    r = np.sqrt(fx * fx + fy * fy)
    r[0, 0] = 1.0
    spec = np.fft.fft2(white) / r ** beta
    spec[0, 0] = 0.0
    out = np.fft.ifft2(spec).real
    return np.clip(out / (2.6 * out.std()), -1.0, 1.0)


def rgb(r: float, g: float, b: float) -> np.ndarray:
    return np.array([r, g, b], dtype=np.float64)


def fill(colour: np.ndarray) -> np.ndarray:
    return np.ones((N, N, 3)) * colour


def rect(a: np.ndarray, x0: float, y0: float, x1: float, y1: float, colour, wrap: bool = True) -> None:
    """Fills a rectangle (px, y up from the cell's bottom), wrapping across the cell's sides."""
    ys = np.arange(int(round(y0)), int(round(y1)))
    for x in range(int(round(x0)), int(round(x1))):
        a[ys, x % N if wrap else x] = colour


def tint(a: np.ndarray, x0: float, y0: float, x1: float, y1: float, k) -> None:
    ys = np.arange(int(round(y0)), int(round(y1)))
    for x in range(int(round(x0)), int(round(x1))):
        a[ys, x % N] *= k


def grain(a: np.ndarray, seed: int, big: float, fine: float, streak: float = 0.0) -> np.ndarray:
    """Weathering: broad patches, fine grain and, with streak, rain runs (long up and down). Multiplies the albedo."""
    k = 1.0 + big * noise(seed, 1.9) + fine * noise(seed + 1, 0.7)
    if streak:
        k += streak * np.minimum(noise(seed + 2, 1.3, (0.7, 9.0)), 0.0)
    return a * k[..., None]


def normalise(a: np.ndarray, mean: float) -> np.ndarray:
    """Scales a tintable surface so it dims the vertex colour by a known amount, not by whatever the noise summed to."""
    return np.clip(a * (mean / a.mean()), 0.0, 1.0)


# ---- window and door pieces (px, y up) ---------------------------------------------------------------------------------

GLASS = rgb(0.13, 0.15, 0.17)
FRAME = rgb(0.62, 0.61, 0.58)
OLIVE = rgb(0.40, 0.47, 0.40)    # painted shutter
BROWN = rgb(0.36, 0.28, 0.21)    # old wood
TEAL = rgb(0.32, 0.45, 0.47)     # painted wood


def casement(a, x0, y0, w, h, panes=2, frame=FRAME, glass=GLASS):
    rect(a, x0, y0, x0 + w, y0 + h, frame)
    t = max(2, round(w * 0.06))
    pw = (w - t * (panes + 1)) / panes
    rows = 2
    ph = (h - t * (rows + 1)) / rows
    for i in range(panes):
        for j in range(rows):
            px = x0 + t + i * (pw + t)
            py = y0 + t + j * (ph + t)
            rect(a, px, py, px + pw, py + ph, glass * (1.0 + 0.12 * ((i * 3 + j * 5) % 3)))


def slats(a, x0, y0, w, h, colour, step=4):
    rect(a, x0, y0, x0 + w, y0 + h, colour)
    for y in range(int(y0), int(y0 + h), step):
        tint(a, x0, y, x0 + w, y + 1, 0.74)


def shuttered(a, x0, y0, w, h, colour, seed):
    """A window with its two leaves folded back on the wall."""
    casement(a, x0, y0, w, h)
    leaf = w * 0.48
    slats(a, x0 - leaf, y0, leaf, h, colour)
    slats(a, x0 + w, y0, leaf, h, colour)


def arched(a, x0, y0, w, h, colour, surround=None, pad=7):
    """An arched opening of width w and full height h (the arch's crown), in colour, with a pale dressed-stone surround."""
    yy, xx = np.mgrid[0:N, 0:N]
    cx = x0 + 0.5 * w
    def mask(pw, ph_extra):
        spring = y0 + h - 0.5 * w
        dx = np.minimum(np.abs(xx - cx), N - np.abs(xx - cx))
        low = (dx < 0.5 * pw) & (yy >= y0 - ph_extra) & (yy < spring)
        top = (dx ** 2 + (yy - spring) ** 2 < (0.5 * pw) ** 2) & (yy >= spring)
        return low | top
    if surround is not None:
        a[mask(w + 2 * pad, pad)] *= surround
    a[mask(w, 0)] = colour


def door(a, x0, y0, w, h, colour, panels=2):
    rect(a, x0, y0, x0 + w, y0 + h, colour)
    t = max(3, round(w * 0.1))
    pw = (w - t * 3) / 2
    ph = (h - t * (panels + 1)) / panels
    for i in range(2):
        for j in range(panels):
            px = x0 + t + i * (pw + t)
            py = y0 + t + j * (ph + t)
            tint(a, px, py, px + pw, py + ph, 0.82)


def plinth(a, colour, px=14):
    """The wall's foot, below the ground storey's openings. Kit.shader clamps a wall's buried part to this band."""
    a[:px, :] = a[:px, :] * colour


def voronoi_stones(seed: int, count: int, squash: float = 1.0):
    """Rough stones: per pixel, the tiling distance to the mortar joint (px) and the stone's id."""
    rng = np.random.default_rng(seed)
    pts = rng.random((count, 2)) * N
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float64)
    best = np.full((N, N), 1e12)
    second = np.full((N, N), 1e12)
    ids = np.zeros((N, N), dtype=np.int64)
    for i, (px, py) in enumerate(pts):
        dx = np.minimum(np.abs(xx - px), N - np.abs(xx - px))
        dy = np.minimum(np.abs(yy - py), N - np.abs(yy - py)) * squash
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < best
        second = np.where(closer, best, np.minimum(second, d))
        ids = np.where(closer, i, ids)
        best = np.where(closer, d, best)
    return second - best, ids, rng.random(count)


def coursed_stone(seed: int, course_px: int, base: float = 0.92, joint: float = 0.70) -> np.ndarray:
    """Coursed dressed stone: courses a whole fraction of the cell, blocks of random length, each its own tone."""
    rng = np.random.default_rng(seed)
    a = np.ones((N, N))
    rows = N // course_px
    h = N / rows
    for r in range(rows):
        y0, y1 = round(r * h), round((r + 1) * h)
        cuts = [0.0]
        while cuts[-1] < N:
            cuts.append(cuts[-1] + rng.uniform(1.3, 2.9) * h)
        scale = N / cuts[-1]
        shift = int(rng.integers(0, N))
        for c0, c1 in zip(cuts, cuts[1:]):
            x0, x1 = round(c0 * scale), round(c1 * scale)
            xs = (np.arange(x0, x1) + shift) % N
            a[np.ix_(np.arange(y0, y1), xs)] = base * rng.uniform(0.91, 1.07)
            a[np.ix_(np.arange(y0, y1), xs[:2])] = joint
        a[y0:y0 + 2, :] = joint
    return a


# ---- the cells ----------------------------------------------------------------------------------------------------------

def white_city() -> np.ndarray:
    """Plastered International Style wall, 4 bays of 3 m: windows above, doors and a shop in the ground storey."""
    a = fill(rgb(0.97, 0.97, 0.96))
    bay = N / 4
    # Upper storeys: casements, one with its blind half down, one with the slatted shutter drawn.
    for s in range(1, 4):
        for b in range(4):
            # 1.4 to 2.6 m above the floor, as the ribbon windows (Ribbons), which stand in front of them on a long side.
            x, y = (b + 0.15) * bay, s * STOREY + 0.424 * STOREY
            w, h = 0.7 * bay, 0.364 * STOREY
            k = (b * 2 + s * 3) % 5
            casement(a, x, y, w, h, panes=3)
            if k == 1: slats(a, x, y + 0.45 * h, w, 0.55 * h, rgb(0.72, 0.70, 0.64))
            if k == 3: slats(a, x, y, w, h, rgb(0.55, 0.60, 0.56))
        # A thin string course at each floor line: a paler render band, no shadow.
        tint(a, 0, s * STOREY - 3, N, s * STOREY + 3, 0.93)
    # Ground storey: window, door, window, shopfront.
    casement(a, 0.15 * bay, 0.424 * STOREY, 0.7 * bay, 0.364 * STOREY, panes=3)
    door(a, 1.26 * bay, 0.08 * STOREY, 0.48 * bay, 0.72 * STOREY, TEAL)
    rect(a, 1.26 * bay, 0.8 * STOREY, 1.74 * bay, 0.9 * STOREY, GLASS)
    casement(a, 2.15 * bay, 0.424 * STOREY, 0.7 * bay, 0.364 * STOREY, panes=3)
    slats(a, 3.1 * bay, 0.08 * STOREY, 0.8 * bay, 0.72 * STOREY, rgb(0.48, 0.47, 0.44), step=5)
    plinth(a, rgb(0.82, 0.81, 0.78), 6)
    return np.clip(grain(a, 11, 0.018, 0.012, 0.05), 0, 1)


def stone_facade() -> np.ndarray:
    """Dressed stone in courses, 3 bays of 4 m: tall arched windows, shop arches below."""
    s = coursed_stone(21, 13)
    a = np.repeat(s[..., None], 3, axis=2) * rgb(1.0, 0.985, 0.95)
    bay = N / 3
    for st in range(1, 4):
        for b in range(3):
            w, h = 0.36 * bay, 0.72 * STOREY
            x, y = (b + 0.32) * bay, st * STOREY + 0.14 * STOREY
            arched(a, x, y, w, h, GLASS, surround=1.1)
            k = (b + st * 2) % 4
            if k != 2:
                rect(a, x + 0.47 * w, y, x + 0.53 * w, y + 0.8 * h, FRAME * 0.8)
                rect(a, x, y + 0.42 * h, x + w, y + 0.46 * h, FRAME * 0.8)
            else:
                arched(a, x, y, w, h, BROWN)
            # sill
            tint(a, x - 7, y - 5, x + w + 7, y, 1.09)
    for b in range(3):
        if b == 1:
            arched(a, (b + 0.24) * bay, 0.08 * STOREY, 0.52 * bay, 0.84 * STOREY, BROWN, surround=1.1)
        else:
            arched(a, (b + 0.24) * bay, 0.08 * STOREY, 0.52 * bay, 0.84 * STOREY, GLASS * 1.3, surround=1.1)
            rect(a, (b + 0.24) * bay, 0.08 * STOREY, (b + 0.76) * bay, 0.42 * STOREY, rgb(0.42, 0.34, 0.27))
    plinth(a, rgb(0.86, 0.84, 0.80), 8)
    return np.clip(grain(a, 22, 0.03, 0.022, 0.05), 0, 1)


def rubble_field(seed: int) -> np.ndarray:
    d, ids, tone = voronoi_stones(seed, 190, 1.7)
    stone = 0.86 + 0.13 * tone[ids]
    joint = np.clip(d / 5.0, 0.0, 1.0)
    return 0.74 + (stone - 0.74) * joint


def small_opening(a, x, y, w, h, colour):
    tint(a, x - 5, y - 5, x + w + 5, y + h + 5, 1.1)
    rect(a, x, y, x + w, y + h, colour)


def village_facade() -> np.ndarray:
    """A hill village's rough limestone, 2 bays of 6 m: small deep windows, one plank door."""
    a = np.repeat(rubble_field(31)[..., None], 3, axis=2) * rgb(1.0, 0.98, 0.94)
    bay = N / 2
    for st in range(1, 4):
        for b in range(2):
            small_opening(a, (b + 0.22 + 0.34 * ((st + b) % 2)) * bay, st * STOREY + 0.4 * STOREY, 0.2 * bay, 0.4 * STOREY,
                          BROWN if (st * 2 + b) % 3 == 0 else GLASS)
    small_opening(a, 0.26 * bay, 0.05 * STOREY, 0.24 * bay, 0.72 * STOREY, TEAL * 0.9)
    small_opening(a, 1.56 * bay, 0.42 * STOREY, 0.2 * bay, 0.36 * STOREY, GLASS)
    plinth(a, rgb(0.85, 0.83, 0.80), 10)
    return np.clip(grain(a, 32, 0.04, 0.02), 0, 1)


def mud_facade() -> np.ndarray:
    """The plain's mud plaster, 2 bays of 6 m: hand-smoothed, patched, with small openings."""
    a = fill(rgb(0.95, 0.94, 0.92))
    a = a * (1.0 + 0.05 * noise(41, 1.4, (1.0, 2.0)) + 0.04 * noise(42, 2.1))[..., None]
    bay = N / 2
    for st in range(1, 4):
        for b in range(2):
            x, y = (b + 0.3 + 0.2 * ((st + b) % 2)) * bay, st * STOREY + 0.42 * STOREY
            rect(a, x, y, x + 0.16 * bay, y + 0.36 * STOREY, BROWN * 0.7 if (b + st) % 3 == 1 else GLASS)
    rect(a, 0.3 * bay, 0.04 * STOREY, 0.52 * bay, 0.74 * STOREY, BROWN)
    rect(a, 1.5 * bay, 0.42 * STOREY, 1.66 * bay, 0.76 * STOREY, GLASS)
    plinth(a, rgb(0.84, 0.81, 0.77), 12)
    return np.clip(grain(a, 43, 0.02, 0.018, 0.05), 0, 1)


def moshava_facade() -> np.ndarray:
    """Whitewash, 3 bays of 4 m: windows with green shutters folded back, a panelled door."""
    a = fill(rgb(0.96, 0.955, 0.94))
    bay = N / 3
    for st in range(1, 4):
        for b in range(3):
            w, h = 0.27 * bay, 0.54 * STOREY
            shuttered(a, (b + 0.365) * bay, st * STOREY + 0.28 * STOREY, w, h, OLIVE, b)
    shuttered(a, 0.365 * bay, 0.28 * STOREY, 0.27 * bay, 0.54 * STOREY, OLIVE, 0)
    door(a, 1.31 * bay, 0.05 * STOREY, 0.38 * bay, 0.78 * STOREY, TEAL)
    shuttered(a, 2.365 * bay, 0.28 * STOREY, 0.27 * bay, 0.54 * STOREY, OLIVE, 0)
    plinth(a, rgb(0.84, 0.82, 0.78), 8)
    return np.clip(grain(a, 51, 0.02, 0.014, 0.05), 0, 1)


def plaster() -> np.ndarray:
    a = grain(fill(rgb(0.96, 0.96, 0.95)), 61, 0.02, 0.014, 0.06)
    return normalise(a, 0.93)


def ashlar() -> np.ndarray:
    a = np.repeat(coursed_stone(71, 34)[..., None], 3, axis=2) * rgb(1.0, 0.985, 0.95)
    return normalise(grain(a, 72, 0.03, 0.024), 0.90)


def rubble() -> np.ndarray:
    a = np.repeat(rubble_field(81)[..., None], 3, axis=2) * rgb(1.0, 0.98, 0.94)
    return normalise(grain(a, 82, 0.04, 0.02), 0.88)


def roof_tile() -> np.ndarray:
    """Pantile rows (32 rows over 4 m, ridge up the cell), neutral so the vertex colour keeps the roof's own red; each tile its own
    tone, a dark gap between rows. The gaps are the tile's colour there, not a shadow worked out for a sun."""
    rows, cols = 32, 24
    rng = np.random.default_rng(91)
    tone = rng.uniform(0.82, 1.14, (rows, cols))
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float64)
    ry, cx = yy / N * rows, xx / N * cols
    r = np.floor(ry).astype(int) % rows
    # Odd rows are half a tile along (the bond).
    cx = (cx + 0.5 * (r % 2)) % cols
    c = np.floor(cx).astype(int) % cols
    fy, fx = ry - np.floor(ry), cx - np.floor(cx)
    k = tone[r, c] * (0.86 + 0.14 * np.sin(np.pi * fx) ** 0.6)     # the tile's crown a little paler than its side
    k *= np.where(fy < 0.12, 0.62, 1.0)
    a = np.repeat(k[..., None], 3, axis=2)
    return normalise(grain(a, 92, 0.05, 0.03), 0.84)


def flat_roof() -> np.ndarray:
    """A limed terrace: patched, seamed in big bays, a few darker repairs."""
    a = fill(rgb(0.96, 0.95, 0.93))
    a = grain(a, 101, 0.05, 0.02)
    for x in range(0, N, N // 4):
        a[:, x:x + 2] *= 0.86
        a[x:x + 2, :] *= 0.86
    a[(noise(102, 1.8) > 0.42)] *= rgb(0.88, 0.87, 0.85)
    return normalise(a, 0.92)


def ribbon_glass() -> np.ndarray:
    """A ribbon window's strip: steel casements, 8 bays over 6 m, some with a slatted wooden blind down. Dark, so it reads as
    a band far off; the frames only show close up."""
    a = fill(GLASS)
    rng = np.random.default_rng(111)
    bay = N // 8
    for b in range(8):
        x = b * bay
        a[:, x:x + bay] *= rng.uniform(0.86, 1.2)
        kind = rng.integers(0, 5)
        if kind == 0:
            slats(a, x, N * 0.5, bay, N * 0.5, rgb(0.46, 0.43, 0.38), step=24)
        if kind == 1:
            slats(a, x, 0, bay, N, rgb(0.40, 0.42, 0.40), step=24)
        a[:, x:x + 5] = FRAME * 0.72
        a[:, x + bay // 2 - 1:x + bay // 2 + 2] = FRAME * 0.55
    a[:16, :] = FRAME * 0.72
    a[-16:, :] = FRAME * 0.72
    return np.clip(a, 0, 1)


def balcony_rail() -> np.ndarray:
    """A balcony's solid plastered parapet: a pale cap on top, a band of render, rain runs; the slab's drip edge at its foot."""
    a = grain(fill(rgb(0.96, 0.96, 0.95)), 121, 0.016, 0.012, 0.09)
    a[-44:, :] *= 1.03
    a[:64, :] *= 0.88
    return normalise(a, 0.93)


def corrugated() -> np.ndarray:
    """Corrugated iron, 24 ribs over 4 m, grey to rust. The ribs differ in tone only faintly."""
    xx = np.arange(N)[None, :]
    k = 0.93 + 0.07 * np.sin(2 * np.pi * xx * 24 / N)
    a = np.repeat(np.repeat(k, N, axis=0)[..., None], 3, axis=2)
    rust = np.clip((noise(131, 1.9) - 0.16) * 3.0, 0, 1)[..., None]
    a = a * (1 - rust) + a * rgb(1.0, 0.74, 0.56) * rust
    return normalise(grain(a, 132, 0.04, 0.02), 0.88)


def planks() -> np.ndarray:
    """Upright boards, 16 over 4 m."""
    rng = np.random.default_rng(141)
    a = np.ones((N, N))
    w = N // 16
    for i in range(16):
        a[:, i * w:(i + 1) * w] = rng.uniform(0.84, 1.08)
        a[:, i * w:i * w + 2] = 0.58
    a = a * (1.0 + 0.05 * noise(142, 1.2, (0.6, 6.0)))
    return normalise(np.repeat(a[..., None], 3, axis=2), 0.88)


def spare() -> np.ndarray:
    return fill(rgb(1.0, 1.0, 1.0))


PAINT = [white_city, stone_facade, village_facade, mud_facade, moshava_facade, plaster, ashlar, rubble, roof_tile, flat_roof,
         ribbon_glass, balcony_rail, corrugated, planks, spare, spare]


def main() -> int:
    assert len(PAINT) == len(CELLS) == 16
    sheet = np.zeros((4 * N, 4 * N, 3))
    for i, paint in enumerate(PAINT):
        cell = paint()
        assert cell.shape == (N, N, 3)
        # Painted y up; an image's first row is its top, and the atlas's layer 0 is its top left cell.
        sheet[(i // 4) * N:(i // 4 + 1) * N, (i % 4) * N:(i % 4 + 1) * N] = cell[::-1]
    px = (np.clip(sheet, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(px, "RGB").save(OUT, optimize=True)
    print(f"{OUT.name} {OUT.stat().st_size} bytes sha256 {hashlib.sha256(OUT.read_bytes()).hexdigest()}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
