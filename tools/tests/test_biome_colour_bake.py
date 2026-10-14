"""The biome colours' bake (unity/Assets/Resources/Map/BiomeColours.bytes, issue #321) is for the shipped map.

The game refuses a bake whose header does not match what it loaded (BiomeColourFile.TryRead: the bake's version, the
texture's resolution, and BiomeColourFile.Check, FNV-1a over the frame, the hex size, the blend radius, the palette and
every hex's centre and biome) and then blends the colours at boot, which is slower. CI runs no Unity, so this checks the
same header here, with the constants read from the C# source: a change to hexes.json, the heightmap's bounds, the palette
or the blend radius without a re-bake (BiomeColourBake.Bake) fails CI, as the water's and the route ribbons' bakes do.

The header holds the blend's data, not its code. Two more checks cover the code (review of #529, S1):
- a numpy float32 port of MapBuilder.BiomePixels re-blends the texels from the repository's inputs and compares them with
  the bake, within 1 per channel (the port and the editor round a few x.5 products differently);
- a fingerprint of the C# that makes the texels (BiomeBlender, GeoFrame.ToWorld, BiomePixels, BiomeLayerColours,
  MapPalette.Lit; comments and whitespace left out), so an edit to it fails here until the bake is re-made, the port
  matches it and the fingerprint is re-pinned. The port alone would not notice an edit made without a re-bake.
Unity's BiomeColourBakeTests.TheBakedBiomeColoursAreTheOnesTheMapWouldBlend compares every texel exactly."""
import hashlib
import json
import re
import struct
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
BAKE = ROOT / "unity/Assets/Resources/Map/BiomeColours.bytes"
MAP = ROOT / "unity/Assets/StreamingAssets/map"
HEXES = ROOT / "content/map/hexes.json"
SCRIPTS = ROOT / "unity/Assets/Scripts/Map"
MAGIC = 0x31424353   # "SCB1"
REBAKE = "run Statecraft.Game.EditorTools.BiomeColourBake.Bake in Unity (BiomeColourFile)"


def source(name: str) -> str:
    return (SCRIPTS / name).read_text()


def const(name: str, rel: str) -> str:
    m = re.search(rf"\b{name}\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\b", source(rel))
    assert m, f"{name} not found in {rel}"
    return m.group(1)


def f32(v) -> np.float32:
    return np.float32(v)


def palette() -> list[tuple[np.float32, ...]]:
    """MapPalette.Ground in MapBuilder.Biome's key order: Lit(r, g, b) = min(c * Brightness, MaxChannel), alpha 1."""
    order = re.findall(r'\["(\w+)"\]\s*=\s*new Color', source("MapBuilder.cs"))
    lit = dict(re.findall(r'\["(\w+)"\]\s*=\s*Lit\(([^)]*)\)', source("MapPalette.cs")))
    assert set(order) == set(lit), (order, lit)
    bright, top = f32(const("Brightness", "MapPalette.cs")), f32(const("MaxChannel", "MapPalette.cs"))
    out = []
    for name in order:
        rgb = [f32(v.strip().rstrip("f")) for v in lit[name].split(",")]
        out.append(tuple(min(f32(c * bright), top) for c in rgb) + (f32(1.0),))
    return out


def check() -> int:
    """BiomeColourFile.Check, as a signed int32."""
    h = 2166136261

    def bits(v: int):
        nonlocal h
        v &= 0xFFFFFFFFFFFFFFFF
        for k in range(8):
            h = ((h ^ ((v >> (8 * k)) & 0xFF)) * 16777619) & 0xFFFFFFFF

    def single(x: np.float32):
        bits(struct.unpack("<i", struct.pack("<f", float(x)))[0])

    def double(x: float):
        bits(struct.unpack("<q", struct.pack("<d", x))[0])

    meta = json.loads((MAP / "heightmap.json").read_text())
    hexes = json.loads(HEXES.read_text())
    bits(int(const("BiomeTextureResolution", "MapBuilder.cs")))
    for k in ("lon0", "lon1", "lat0", "lat1"):
        double(float(meta[k]))
    single(f32(f32(meta["width_km"]) * f32(1000)))       # MapBuilder.TerrainSize: width_km * 1000f
    single(f32(f32(meta["length_km"]) * f32(1000)))
    single(f32(hexes["hex_size_km"]))
    single(f32(const("BlendRadiusHexes", "BiomeBlender.cs")))
    for colour in palette():
        for c in colour:
            single(c)
    for hx in hexes["hexes"]:
        double(float(hx["lon"]))
        double(float(hx["lat"]))
        for ch in hx["biome"]:
            bits(ord(ch))
        bits(-1)
    return struct.unpack("<i", struct.pack("<I", h))[0]


def test_the_biome_colour_bake_is_for_the_shipped_map():
    data = BAKE.read_bytes()
    magic, version, res, baked = struct.unpack_from("<4i", data)
    assert magic == MAGIC, f"{BAKE.name} is not a biome colour bake"
    assert version == int(const("Version", "BiomeColourFile.cs")), f"made by another version: {REBAKE}"
    assert res == int(const("BiomeTextureResolution", "MapBuilder.cs")), f"made at {res} texels a side: {REBAKE}"
    assert len(data) == 16 + 3 * res * res, "truncated"
    assert baked == check(), f"made from other hexes, heightmap bounds, palette or blend radius: {REBAKE}"


# The fingerprint of the blend's C# code (code_fingerprint). Re-pin only after re-baking (BiomeColourBake.Bake), bumping
# BiomeColourFile.Version, and checking that test_the_bake_is_the_blend_of_the_shipped_inputs still passes.
PINNED_CODE = "63e392d75d278b96c016575d47b22b3e76dd4df9cb0af67167f975073174fd17"


def method(text: str, signature: str) -> str:
    """The text of the member starting at signature, through its closing brace (or its ';' for an expression body)."""
    i = text.index(signature)
    depth, j = 0, i
    while j < len(text):
        c = text[j]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return text[i:j + 1]
        elif c == ";" and depth == 0:
            return text[i:j + 1]
        j += 1
    raise AssertionError(f"{signature}: no end")


def code_fingerprint() -> str:
    parts = [
        source("BiomeBlender.cs"),
        method(source("GeoFrame.cs"), "public Vector3 ToWorld("),
        method(source("MapBuilder.cs"), "public static Color32[] BiomePixels("),
        method(source("MapBuilder.cs"), "public static Color[] BiomeLayerColours("),
        method(source("MapPalette.cs"), "static Color Lit("),
    ]
    code = "\n".join(re.sub(r"\s+", " ", re.sub(r"//[^\n]*", "", p)).strip() for p in parts)
    return hashlib.sha256(code.encode()).hexdigest()


def test_the_blend_code_is_the_one_the_bake_was_made_with():
    assert code_fingerprint() == PINNED_CODE, (
        f"the C# that makes the biome texels changed (fingerprint {code_fingerprint()}): {REBAKE}, bump "
        "BiomeColourFile.Version, update the numpy port below if the rule changed, then re-pin PINNED_CODE")


def reblend() -> np.ndarray:
    """MapBuilder.BiomePixels in numpy float32 (BiomeBlender.Weights over GeoFrame.ToWorld centres, the palette mix,
    Color -> Color32): texels (res * res, 3), row 0 = south. From the review of #529 (personal2's port), with the palette,
    the layer order and the constants read from the C# source."""
    meta = json.loads((MAP / "heightmap.json").read_text())
    hexes = json.loads(HEXES.read_text())
    names = re.findall(r'\["(\w+)"\]\s*=\s*new Color', source("MapBuilder.cs"))
    cols = np.array([c[:3] for c in palette()], dtype=np.float32)
    res = int(const("BiomeTextureResolution", "MapBuilder.cs"))
    W = f32(f32(meta["width_km"]) * f32(1000))
    L = f32(f32(meta["length_km"]) * f32(1000))
    lon0, lon1, lat0, lat1 = (float(meta[k]) for k in ("lon0", "lon1", "lat0", "lat1"))
    cx = np.array([f32((h["lon"] - lon0) / (lon1 - lon0) * float(W)) for h in hexes["hexes"]], dtype=np.float32)
    cy = np.array([f32((h["lat"] - lat0) / (lat1 - lat0) * float(L)) for h in hexes["hexes"]], dtype=np.float32)
    lay = np.array([names.index(h["biome"]) for h in hexes["hexes"]])
    radius = f32(f32(const("BlendRadiusHexes", "BiomeBlender.cs")) * f32(f32(hexes["hex_size_km"]) * f32(1000)))
    cell = radius
    kx = np.floor(cx / cell).astype(int)
    ky = np.floor(cy / cell).astype(int)
    minX, minY = kx.min(), ky.min()
    nx, ny = kx.max() - minX + 1, ky.max() - minY + 1
    cells = [[] for _ in range(nx * ny)]
    for i in range(len(cx)):                                   # insertion order within each cell, as BiomeBlender
        cells[(kx[i] - minX) + (ky[i] - minY) * nx].append(i)
    maxn = max(len(c) for c in cells)
    tab = -np.ones((nx * ny, maxn), dtype=int)
    for c, items in enumerate(cells):
        tab[c, :len(items)] = items
    xs = np.arange(res, dtype=np.float32)
    X, Y = np.meshgrid(xs, xs)
    px = ((X + f32(0.5)) / f32(res) * W).astype(np.float32).ravel()
    py = ((Y + f32(0.5)) / f32(res) * L).astype(np.float32).ravel()
    pkx = np.floor(px / cell).astype(int)
    pky = np.floor(py / cell).astype(int)
    n = px.size
    w = np.zeros((len(names), n), dtype=np.float32)
    total = np.zeros(n, dtype=np.float32)
    for dx in (-1, 0, 1):
        gx = pkx + dx - minX
        for dy in (-1, 0, 1):
            gy = pky + dy - minY
            ok = (gx >= 0) & (gx < nx) & (gy >= 0) & (gy < ny)
            c = np.where(ok, gx + gy * nx, 0)
            for s in range(maxn):
                k = np.where(ok, tab[c, s], -1)
                v = k >= 0
                kk = np.where(v, k, 0)
                ddx = (cx[kk] - px).astype(np.float32)
                ddy = (cy[kk] - py).astype(np.float32)
                mag = np.sqrt((ddx * ddx + ddy * ddy).astype(np.float64)).astype(np.float32)
                t = (mag / radius).astype(np.float32)
                v &= t < f32(1)
                f = (f32(1) - t * t * (f32(3) - f32(2) * t)).astype(np.float32)
                for layer in range(len(names)):
                    m = v & (lay[kk] == layer)
                    w[layer] = np.where(m, w[layer] + f, w[layer])
                total = np.where(v, total + f, total)
    pos = total > 0
    w = np.where(pos, w / np.where(pos, total, f32(1)), w).astype(np.float32)
    for i in np.nonzero(~pos)[0]:                              # no centre in reach: the nearest centre's biome
        d = ((cx - px[i]) ** 2 + (cy - py[i]) ** 2).astype(np.float32)
        w[:, i] = 0
        w[lay[int(np.argmin(d))], i] = 1
    rgb = np.zeros((n, 3), dtype=np.float32)
    for layer in range(len(names)):
        rgb = (rgb + cols[layer][None, :] * w[layer][:, None]).astype(np.float32)
    return np.round(np.clip(rgb, 0, 1) * f32(255)).astype(np.uint8)


def test_the_bake_is_the_blend_of_the_shipped_inputs():
    data = BAKE.read_bytes()
    baked = np.frombuffer(data, dtype=np.uint8, offset=16).reshape(-1, 3)
    diff = np.abs(reblend().astype(int) - baked.astype(int))
    assert diff.max() <= 1, f"a texel differs by {diff.max()} from the blend: {REBAKE}"
    assert int((diff > 0).sum()) <= 64, f"{int((diff > 0).sum())} channels differ by 1 from the blend: {REBAKE}"
