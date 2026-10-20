#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>Puts a hex board on the game's real map. The board's hexes are the game's own q/r on the same lattice
    /// (HexGrid), so its root sits at (0, sea level, map length) with no turn or scale. Under the board's land the real
    /// ground is eased down to the board's hex-shaped ground (blending out over about one hex; its water is left to the
    /// real sea), the board's terrain is cut away everywhere but its land, and the real settlement markers and building
    /// prototypes on that land are hidden. Destroying the returned root puts the real heights and renderers back.</summary>
    public static class HexRealMap
    {
        const float SinkM = 30f, BlendFromShare = 0.9f, BlendToShare = 1.7f;

        public static (GameObject root, List<HexTileReport> reports) Place(MapScene map, HexBoard board, Func<string, (Texture2D?, Texture2D?)>? textures, Transform? parent = null, Action? restoreDressing = null)
        {
            float sea = Shader.GetGlobalVector(MapShaderGlobals.Map).z;   // the map's sea level, set when it is built
            var origin = new Vector3(0f, sea, map.Frame.LengthM);
            var surface = new HexBoardSurface(board);
            // Distance to the nearest board hex centre, looking only at the two rings round the hex under the point.
            float Near(Vector3 b)
            {
                var (q0, r0) = HexGeometry.Axial(b, HexScale.HexM);
                float best = float.MaxValue;
                for (int dq = -2; dq <= 2; dq++)
                    for (int dr = Mathf.Max(-2, -dq - 2); dr <= Mathf.Min(2, -dq + 2); dr++)
                    {
                        if (!board.Hexes.ContainsKey((q0 + dq, r0 + dr))) continue;
                        var c = HexGeometry.Centre(q0 + dq, r0 + dr, HexScale.HexM);
                        best = Mathf.Min(best, new Vector2(b.x - c.x, b.z - c.z).magnitude);
                    }
                return best;
            }

            // 1. Ease the real ground down to the board's ground under and around its land; remember it for teardown.
            var t = map.Terrain; var data = t.terrainData; var tp = t.transform.position;
            int res = data.heightmapResolution;
            var rect = HexBoardBuilder.Bounds(board);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((origin.x + rect.xMin - tp.x) / data.size.x * (res - 1)), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((origin.x + rect.xMax - tp.x) / data.size.x * (res - 1)), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((origin.z + rect.yMin - tp.z) / data.size.z * (res - 1)), 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((origin.z + rect.yMax - tp.z) / data.size.z * (res - 1)), 0, res - 1);
            var before = data.GetHeights(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
            var heights = (float[,])before.Clone();
            for (int z = 0; z < heights.GetLength(0); z++)
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    var b = new Vector3(tp.x + data.size.x * (x0 + x) / (res - 1f), 0f, tp.z + data.size.z * (z0 + z) / (res - 1f)) - origin;
                    float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BlendToShare * HexScale.HexM, BlendFromShare * HexScale.HexM, Near(b)));
                    if (w <= 0f) continue;
                    float hb = surface.Height(b);
                    if (hb < 0f) continue;   // the board's water: the real sea and its depth shading stay
                    heights[z, x] = Mathf.Lerp(heights[z, x], Mathf.Clamp01((sea + hb - SinkM - tp.y) / data.size.y), w);
                }
            data.SetHeights(x0, z0, heights);

            // 2. The board on top.
            var (root, reports) = HexBoardBuilder.Build(board, textures, options: new HexBoardBuilder.Options { OwnSea = false, HolesOutsideLand = true, Origin = origin, Parent = parent });
            var assets = root.GetComponent<HexBoardAssets>();
            // Dressing was disabled before placement. Restore it after the renderer states below, so the kits can
            // hide their placeholder markers again (teardown runs last registered first).
            if (restoreDressing != null) assets.OnTeardown(restoreDressing);
            assets.OnTeardown(() => { if (data != null) data.SetHeights(x0, z0, before); });

            // 3. Hide the real markers and building prototypes standing on the board's land (its tiles replace them).
            var hidden = new List<Renderer>();
            foreach (var r in map.GetComponentsInChildren<Renderer>(false))
            {
                if (r.transform.IsChildOf(root.transform) || r.GetComponent<Terrain>() != null || !r.enabled) continue;
                var b = r.bounds.center - origin;
                if (r.bounds.size.x > 3f * HexScale.HexM) continue;   // map-wide meshes (sea, skirt, overlays) stay
                var (q, rr) = HexGeometry.Axial(b, HexScale.HexM);
                if (!board.Hexes.TryGetValue((q, rr), out var h) || HexBoard.TerrainOf(h) == HexTerrain.Water) continue;
                r.enabled = false; hidden.Add(r);
            }
            assets.OnTeardown(() => { foreach (var r in hidden) if (r != null) r.enabled = true; });
            return (root, reports);
        }
    }
}
