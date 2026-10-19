#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>Dresses the board's environment hexes (BoardHex.env) as one continuous landscape round the playable
    /// hexes: woods, scattered trees and rock outcrops whose density follows a noise field in world space, so they run
    /// across hex boundaries with no tile shape, no inset and no hex lines (the builder skips their edges). Deterministic.</summary>
    public static class HexEnvironment
    {
        const float GridM = 820f, WoodScaleM = 7000f, DetailScaleM = 1900f;

        public static IEnumerable<HexPlacement> Dress(HexBoard board, IHexSurface surface, int seed = 1948)
        {
            foreach (var h in board.Hexes.Values)
            {
                if (!h.env || HexBoard.TerrainOf(h) == HexTerrain.Water) continue;
                var ctx = new HexTileContext(h.q, h.r, surface, seed ^ 0x5eed, HexScale.HexM, insetShare: 0f);
                var c = ctx.Centre;
                bool hills = HexBoard.TerrainOf(h) != HexTerrain.Flat;
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
                        density = Mathf.Max(density * (0.6f + 0.6f * detail), 0.035f);
                        if (r1 * 0.7f + r2 * 0.3f > density) continue;
                        string model = hills ? (detail > 0.55f ? "sandbox:aleppo_pine_lite" : "sandbox:tabor_oak_lite") : (detail > 0.6f ? "sandbox:carob_lite" : "sandbox:olive_lite");
                        ctx.PlaceByHeight(model, p, r1 * 360f, (0.11f + 0.04f * r2) * HexScale.HexM, hills && detail > 0.55f ? StandIn.Pine : StandIn.Tree, null, 0.3f, 40f);
                    }
                if (hills)
                    for (int k = 0; k < 2; k++)
                        if (ctx.Rng.NextDouble() < 0.6)
                            ctx.PlaceByWidth("sandbox:limestone*", ctx.RandomPoint(), ctx.Range(0f, 360f), ctx.Range(0.06f, 0.1f) * HexScale.HexM, StandIn.Rock, null, 60f);
                foreach (var placement in ctx.Placements) yield return placement;
            }
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
