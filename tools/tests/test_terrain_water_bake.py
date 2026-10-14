"""The terrain water's baked mesh (unity/Assets/Textures/Map/terrain_water.bytes, issue #195) is for the shipped heightmap.

The game refuses a bake whose header does not match the heightmap it loaded (MapBuilder.TryReadWaterMesh: the resolution and
HeightsCheck, FNV-1a over every 997th height's float bits) and then derives the mesh at boot, which is slower. CI runs no
Unity, so this checks the same header here: a heightmap change without a re-bake (TerrainWaterBake.Bake) fails CI, as
localities_bake's does. Unity's SeaTests.TheBakedWaterMeshMatchesTheHeightmap compares the whole mesh.

The header holds the heights, not the code that makes the mesh or the constants and heightmap.json values it uses. Two more
checks cover those (issue #535, as #529 did for the biome colours):
- a numpy port of MapBuilder.WaterMeshGrid re-makes the mesh from the shipped heightmap, with the constants read from the
  C# source, and compares its vertices and triangles with the bake's (as sets: the port does not keep the C#'s order);
- a fingerprint of the C# that makes the mesh (comments and whitespace left out), so an edit to it fails here until the
  mesh is re-baked, the port matches it and the fingerprint is re-pinned. The port alone would not notice an edit made
  without a re-bake. The bake has no version field, so the fingerprint is all that ties it to the code."""
import hashlib
import json
import re
import struct
from pathlib import Path

import numpy as np

import heightmap_codec as hc

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "unity/Assets/StreamingAssets/map"
BAKE = ROOT / "unity/Assets/Textures/Map/terrain_water.bytes"
MAGIC = 0x31574353   # "SCW1"
UNITY = ROOT / "unity/Assets"
REBAKE = "run Statecraft.Game.EditorTools.TerrainWaterBake.Bake in Unity"


def heights_check(heights: np.ndarray) -> int:
    """MapBuilder.HeightsCheck over Unity's heights[y, x] (float32, y = 0 at the south)."""
    bits = heights.astype(np.float32).ravel()[::997].view(np.uint32)
    h = 2166136261
    for b in bits.tolist():
        for k in range(4):
            h = ((h ^ ((b >> (8 * k)) & 0xFF)) * 16777619) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


def unity_heights() -> np.ndarray:
    """HeightmapLoader.ToUnityHeights: raw / 65535f, file row 0 (north) as the last row."""
    meta = json.loads((MAP / "heightmap.json").read_text())
    raw = hc.decode((MAP / "heightmap.r16z").read_bytes(), meta["width"], meta["height"])
    return (raw.astype(np.float32) / np.float32(65535.0))[::-1]


def test_the_terrain_water_bake_is_for_the_shipped_heightmap():
    magic, res, check, nverts, nindices = struct.unpack_from("<5i", BAKE.read_bytes())
    heights = unity_heights()
    assert magic == MAGIC
    assert res == heights.shape[1]
    assert BAKE.stat().st_size == 20 + 4 * (nverts + nindices)
    assert check == heights_check(heights), "stale: " + REBAKE


def test_heights_check_reads_every_997th_sample():
    h = np.zeros((40, 50), np.float32)
    base = heights_check(h)
    h[997 // 50, 997 % 50] = 0.5
    assert heights_check(h) != base
    h[997 // 50, 997 % 50] = 0.0
    h[0, 1] = 0.5
    assert heights_check(h) == base


def csharp(rel: str) -> str:
    return (UNITY / rel).read_text()


def csharp_const(rel: str, name: str) -> float:
    m = re.search(rf"\b{name}\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)f?\b", csharp(rel))
    assert m, f"{name} not found in {rel}"
    return float(m.group(1))


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


def fingerprint(parts: list[str]) -> str:
    """SHA-256 of the code with // comments and whitespace runs left out, so a comment-only edit does not move it."""
    code = "\n".join(re.sub(r"\s+", " ", re.sub(r"//[^\n]*", "", p)).strip() for p in parts)
    return hashlib.sha256(code.encode()).hexdigest()


# The fingerprint of the water mesh's C# code (code_fingerprint). Re-pin only after re-baking (TerrainWaterBake.Bake) and
# checking that test_the_bake_is_the_water_mesh_of_the_shipped_heightmap still passes (update the port if the rule changed).
PINNED_CODE = "6f46eaf81b8c4b18841eb1c1e8e3e1eca19a89be1257542c7d4b269300bc6f06"


def code_fingerprint() -> str:
    """WaterMeshGrid, the terrain size it is given, the bytes it is written as and the editor bake that calls it. The
    constants it reads (SeaToleranceM, LakeBelowM, LakeFlatM, WaterMeshReach, WaterMeshBlock, VerticalExaggeration) are
    not in it: the port reads them from the source, so a change to one fails the port's test instead."""
    builder = csharp("Scripts/Map/MapBuilder.cs")
    return fingerprint([
        method(builder, "public static (int[] grid, int[] indices) WaterMeshGrid("),
        method(builder, "public static Vector3 TerrainSize("),
        method(builder, "public static byte[] WaterMeshBytes("),
        method(csharp("Editor/TerrainWaterBake.cs"), "public static byte[] Bytes("),
    ])


def test_the_water_mesh_code_is_the_one_the_bake_was_made_with():
    assert code_fingerprint() == PINNED_CODE, (
        f"the C# that makes the water mesh changed (fingerprint {code_fingerprint()}): {BAKE.relative_to(ROOT)} is stale "
        f"until you {REBAKE}, update the numpy port "
        "in tools/tests/test_terrain_water_bake.py if the rule changed, then re-pin PINNED_CODE there")


def water_mesh() -> tuple[np.ndarray, np.ndarray]:
    """MapBuilder.WaterMeshGrid in numpy: the vertices' samples (x | y << 16) and the triangles as samples (n, 3), in the
    C#'s winding. The quadtree is walked a level at a time over the whole map instead of block by block: a block of s
    cells is flat (one fan) when every one of its cells has four wet corners of one height, and it is looked at when its
    parent was looked at, was not flat and has a drawn cell."""
    meta = json.loads((MAP / "heightmap.json").read_text())
    h = unity_heights()
    res = h.shape[1]
    cells = res - 1
    f = np.float32
    rel = "Scripts/Map/MapBuilder.cs"
    ve, sea_tol = f(csharp_const(rel, "VerticalExaggeration")), f(csharp_const(rel, "SeaToleranceM"))
    lake_below_m, lake_flat_m = f(csharp_const(rel, "LakeBelowM")), f(csharp_const(rel, "LakeFlatM"))
    B, R = int(csharp_const(rel, "WaterMeshBlock")), int(csharp_const(rel, "WaterMeshReach"))
    assert cells % B == 0 and B & (B - 1) == 0, "the port walks whole power-of-two blocks only"
    emin = f(meta["elev_min_m"])
    size_y = f(f(f(meta["elev_max_m"]) - emin) * ve)                  # TerrainSize
    per_metre = f(ve / size_y)
    zero = f(-emin * per_metre)
    sea_lo, sea_hi = f(zero - f(sea_tol * per_metre)), f(zero + f(sea_tol * per_metre))
    lake_below, lake_flat = f(f(lake_below_m - emin) * per_metre), f(lake_flat_m * per_metre)

    padded = np.pad(h, 1, mode="edge")                                 # Wet's clamped 3x3 neighbourhood
    shifts = [padded[1 + oy:1 + oy + res, 1 + ox:1 + ox + res] for oy in (-1, 0, 1) for ox in (-1, 0, 1)]
    lo, hi = np.minimum.reduce(shifts), np.maximum.reduce(shifts)
    wet = ((h > sea_lo) & (h < sea_hi)) | ((h < lake_below) & (f(hi - lo) <= lake_flat))

    blocks = cells // B                                                # the last block takes the last row and column
    starts = np.arange(0, blocks * B, B)
    block_wet = np.logical_or.reduceat(np.logical_or.reduceat(wet, starts, axis=0), starts, axis=1)
    bw = np.pad(block_wet, 1)
    relevant = np.logical_or.reduce([bw[1 + j:1 + j + blocks, 1 + i:1 + i + blocks] for j in (-1, 0, 1) for i in (-1, 0, 1)])

    w = np.pad(wet, R)                                                 # near: a wet sample within R (out of the map: dry)
    near = np.logical_or.reduce([w[j:j + res, i:i + res] for j in range(2 * R + 1) for i in range(2 * R + 1)])
    drawn = near[:-1, :-1] | near[:-1, 1:] | near[1:, :-1] | near[1:, 1:]          # [cy, cx]
    c00, c10, c01, c11 = h[:-1, :-1], h[:-1, 1:], h[1:, :-1], h[1:, 1:]
    flat_cell = (wet[:-1, :-1] & wet[:-1, 1:] & wet[1:, :-1] & wet[1:, 1:]) & (c00 == c10) & (c00 == c01) & (c00 == c11)

    def per_block(a: np.ndarray, s: int, how) -> np.ndarray:
        n = cells // s
        return how(a.reshape(n, s, n, s), axis=(1, 3))

    fans = []                                                          # (s, by, bx) block indices of each fan
    look = relevant
    s = B
    while s > 1:
        if s < B:
            look = look.repeat(2, axis=0).repeat(2, axis=1) & per_block(drawn, s, np.any)
        fan = look & per_block(flat_cell, s, np.all)
        fans.append((s, np.nonzero(fan)))
        look = look & ~fan & per_block(drawn, s, np.any)
        s //= 2
    cy, cx = np.nonzero(look.repeat(2, axis=0).repeat(2, axis=1) & drawn)

    def key(x, y):
        return np.asarray(x, np.int64) | (np.asarray(y, np.int64) << 16)

    a, b, d, e = key(cx, cy), key(cx + 1, cy), key(cx, cy + 1), key(cx + 1, cy + 1)
    tris = [np.stack([a, e, b], 1), np.stack([a, d, e], 1)]           # Tri(a, b, e), Tri(a, e, d): (i, l, j)
    for s, (by, bx) in fans:
        x0, y0 = (bx * s)[:, None], (by * s)[:, None]
        i = np.arange(s)[None, :]
        ring = np.concatenate([key(x0 + i, y0), key(x0 + s, y0 + i), key(x0 + s - i, y0 + s), key(x0, y0 + s - i)], 1)
        centre = np.broadcast_to(key(x0 + s // 2, y0 + s // 2), ring.shape)
        tris.append(np.stack([centre, np.roll(ring, -1, 1), ring], 2).reshape(-1, 3))
    tris = np.concatenate(tris)
    return np.unique(tris), tris


def canonical(tris: np.ndarray) -> np.ndarray:
    """Each triangle turned (not flipped) to start at its smallest sample, then all sorted."""
    k = np.argmin(tris, axis=1)
    rows = np.arange(len(tris))[:, None]
    t = tris[rows, (k[:, None] + np.arange(3)[None, :]) % 3]
    return t[np.lexsort(t.T[::-1])]


def test_the_bake_is_the_water_mesh_of_the_shipped_heightmap():
    data = BAKE.read_bytes()
    nverts, nindices = struct.unpack_from("<2i", data, 12)
    grid = np.frombuffer(data, dtype="<i4", count=nverts, offset=20).astype(np.int64)
    indices = np.frombuffer(data, dtype="<i4", count=nindices, offset=20 + 4 * nverts)
    verts, tris = water_mesh()
    assert len(np.unique(grid)) == nverts, "a sample is in the baked mesh twice: " + REBAKE
    assert np.array_equal(np.sort(grid), verts), "the baked mesh has other vertices than WaterMeshGrid makes: " + REBAKE
    assert nindices == 3 * len(tris), f"the baked mesh has {nindices // 3} triangles, WaterMeshGrid makes {len(tris)}: " + REBAKE
    baked = grid[indices].reshape(-1, 3)
    assert np.array_equal(canonical(baked), canonical(tris)), "the baked mesh has other triangles than WaterMeshGrid makes: " + REBAKE
