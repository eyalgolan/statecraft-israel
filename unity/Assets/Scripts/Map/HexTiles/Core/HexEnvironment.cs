#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>Dresses the board's environment hexes (BoardHex.env) as one continuous landscape round the playable
    /// hexes: woods, scattered trees and rock outcrops whose density follows a noise field in world space, so they run
    /// across hex boundaries with no tile shape, no inset and no hex lines (the builder skips their edges). Deterministic.
    /// The species, their density and the rocks come from the hex's region (BoardRegion, from tools/hex_tiles/regions.yaml:
    /// oak and pine woods in the north, citrus and date palm on the coast, acacia and desert rock in the south); a hex with
    /// no region gets the built-in mix (BoardRegion.BuiltIn), which plants exactly what this did before regions.</summary>
    public static class HexEnvironment
    {
        const float GridM = 820f, WoodScaleM = 7000f, DetailScaleM = 1900f;

        public static IEnumerable<HexPlacement> Dress(HexBoard board, IHexSurface surface, int seed = 1948) =>
            board.Hexes.Values.SelectMany(h => DressHex(board, h, surface, seed));

        /// <summary>One environment hex's trees and rocks (none for a play or water hex). Each hex plants only inside
        /// itself, so a hex can be redressed alone (the Hex Painter).</summary>
        public static IEnumerable<HexPlacement> DressHex(HexBoard board, BoardHex h, IHexSurface surface, int seed = 1948)
        {
            if (!h.env || HexBoard.TerrainOf(h) == HexTerrain.Water) yield break;
            var ctx = new HexTileContext(h.q, h.r, surface, seed ^ 0x5eed, HexScale.HexM, insetShare: 0f);
            var c = ctx.Centre;
            bool hills = HexBoard.TerrainOf(h) != HexTerrain.Flat;
            var region = board.RegionOf(h);
            var species = Mix(hills ? region.hills : region.flat, hills ? BoardRegion.BuiltIn.hills : BoardRegion.BuiltIn.flat);
            int n = Mathf.CeilToInt(HexScale.HexM / GridM);
            for (int i = -n; i <= n; i++)
                for (int j = -n; j <= n; j++)
                {
                    // A world-aligned jittered grid, so neighbouring hexes continue the same pattern.
                    float gx = Mathf.Round(c.x / GridM) * GridM + i * GridM, gz = Mathf.Round(c.z / GridM) * GridM + j * GridM;
                    uint hsh = unchecked((uint)(Mathf.FloorToInt(gx) * 73856093 ^ Mathf.FloorToInt(gz) * 19349663 ^ seed));
                    float r1 = (hsh & 0xFFFF) / 65535f, r2 = ((hsh >> 16) & 0xFFFF) / 65535f;
                    var p = new Vector3(gx + (r1 - 0.5f) * GridM * 0.8f, 0f, gz + (r2 - 0.5f) * GridM * 0.8f);
                    if (!HexGeometry.InHex(p, c, HexScale.HexM)) continue;
                    float wood = Mathf.PerlinNoise(p.x / WoodScaleM + 3.3f, p.z / WoodScaleM + 7.7f);
                    float detail = Mathf.PerlinNoise(p.x / DetailScaleM + 1.1f, p.z / DetailScaleM + 2.2f);
                    // Woods in clumps with open country between (a few lone trees), never an even sprinkle.
                    float density = hills ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.6f, wood)) : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.56f, 0.68f, wood)) * 0.85f;
                    density = Mathf.Max(density * (0.6f + 0.6f * detail), 0.035f) * region.density;
                    if (r1 * 0.7f + r2 * 0.3f > density) continue;
                    // The species by the detail noise, so each kind clumps: the first whose share of the mix reaches it.
                    var kind = Pick(species, detail);
                    ctx.PlaceByHeight(kind.model, p, r1 * 360f, (0.11f + 0.04f * r2) * HexScale.HexM, kind.StandIn, null, 0.3f, 40f);
                }
            // Rounded so the built-in 0.6 compares exactly as the double it always was.
            double rockChance = Math.Round(region.rockChance, 4);
            if (hills || region.rocksOnFlat)
                for (int k = 0; k < 2; k++)
                    if (ctx.Rng.NextDouble() < rockChance)
                        ctx.PlaceByWidth(region.rock, ctx.RandomPoint(), ctx.Range(0f, 360f), ctx.Range(0.06f, 0.1f) * HexScale.HexM, StandIn.Rock, null, 60f);
            foreach (var placement in ctx.Placements) yield return placement;
        }

        static BoardSpecies[] Mix(BoardSpecies[]? own, BoardSpecies[] builtIn) => own != null && own.Sum(s => s.weight) > 0f ? own : builtIn;

        /// <summary>The first species whose running share of the mix reaches <paramref name="u"/> (0..1); the last otherwise.</summary>
        public static BoardSpecies Pick(BoardSpecies[] mix, float u)
        {
            float total = 0f; foreach (var s in mix) total += s.weight;
            float acc = 0f;
            for (int i = 0; i < mix.Length - 1; i++) { acc += mix[i].weight; if (u * total <= acc) return mix[i]; }
            return mix[mix.Length - 1];
        }
    }

    /// <summary>The hex-tile look's picture, Civ's warm and saturated diorama light (Tal: "fascinating to watch and to be
    /// in", not depressing): a livelier grade, a warmer, brighter sun and brighter ambient. Apply returns the undo.</summary>
    public static class HexLook
    {
        public const float Saturation = 30f, Contrast = 16f, PostExposure = 0.32f, Temperature = 12f, SunBoost = 1.22f;

        public static System.Action Apply(UnityEngine.Rendering.VolumeProfile? profile, Light? sun)
        {
            var undo = new List<System.Action>();
            if (profile != null && profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments ca))
            {
                var o = (ca.saturation.value, ca.saturation.overrideState, ca.contrast.value, ca.contrast.overrideState, ca.postExposure.value, ca.postExposure.overrideState);
                ca.saturation.Override(Saturation); ca.contrast.Override(Contrast); ca.postExposure.Override(PostExposure);
                undo.Add(() => { ca.saturation.value = o.Item1; ca.saturation.overrideState = o.Item2; ca.contrast.value = o.Item3; ca.contrast.overrideState = o.Item4; ca.postExposure.value = o.Item5; ca.postExposure.overrideState = o.Item6; });
            }
            if (profile != null)
            {
                bool added = !profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance wb);
                if (added) wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>();
                var o = (wb.temperature.value, wb.temperature.overrideState);
                wb.temperature.Override(Temperature);
                undo.Add(() => { if (added) profile.Remove<UnityEngine.Rendering.Universal.WhiteBalance>(); else { wb.temperature.value = o.Item1; wb.temperature.overrideState = o.Item2; } });
            }
            if (sun != null)
            {
                float i0 = sun.intensity; var c0 = sun.color;
                sun.intensity = i0 * SunBoost; sun.color = Color.Lerp(c0, new Color(1f, 0.88f, 0.70f), 0.35f);
                undo.Add(() => { if (sun != null) { sun.intensity = i0; sun.color = c0; } });
            }
            var sky = RenderSettings.ambientSkyColor; var eq = RenderSettings.ambientEquatorColor;
            RenderSettings.ambientSkyColor = sky * 1.18f; RenderSettings.ambientEquatorColor = eq * 1.15f;
            undo.Add(() => { RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = eq; });
            return () => { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); };
        }
    }
}
