#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The desert edge, 1948: the Judean desert east of Jerusalem, or the northern Negev loess. Bare tan slopes
    /// (the desert layer, painted to the rim so neighbouring ground does not soften it) are streaked with paler wind-blown
    /// sand. A dry wadi meanders across the dome's flank, painted as dirt with a pale gravel bed, with a few acacias along
    /// its banks. Desert rocks are scattered, two of them large. A Bedouin camp of three long, low, dark goat-hair tents
    /// sits on the far flank. There are no people or animals, few pieces, and the unit spot stays open.
    ///
    /// Each tent is two or three dark Box stand-ins in a row along its heading (tiles:desert_edge/tent has no model), because
    /// the Box stand-in is fixed at 0.65 of its width tall. A row reads long and low; boxes sit just apart, so their tops
    /// never overlap.
    ///
    /// Triangle budget by arithmetic (caps below, with measured per-model counts): 6 acacias x ~1,160 = 6,960. 9 rocks x
    /// ~700 = 6,300. 9 tent boxes x under 50 = under 450. Total under 13,800 of 25,000.</summary>
    [HexPrototype("desert_edge")]
    public sealed class DesertEdge : IHexPrototype
    {
        public string Title => "Desert edge: wadi, acacias, Bedouin tents (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "desert";
        public int TriangleBudget => 25000;

        // Models: EZ-Tree trees and Poly Haven rocks are local-only for now; the stand-ins draw without them.
        const string Acacia = "sandbox:acacia_lite", Rock = "sandbox:desert_rock*", Tent = "tiles:desert_edge/tent";
        // Caps that keep the tile inside its budget whatever the models' exact counts.
        const int MaxAcacias = 6, MaxBigRocks = 2, MaxRocks = 9, MaxTents = 3;

        static readonly Color AcaciaGreen = new Color(0.48f, 0.50f, 0.30f), RockTan = new Color(0.70f, 0.58f, 0.44f),
            TentBlack = new Color(0.17f, 0.14f, 0.12f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. Bare tan slopes: the desert layer over the whole hex, soft at the rim; then paler sand streaks, long and thin,
            //    all lying roughly along one wind.
            ctx.PaintGround("desert", ctx.Centre, 0f, 1.75f * s, 2.0f * s, 0.2f, 0.7f);
            float wind = ctx.Range(0f, 180f);
            for (int k = 0; k < 5; k++)
            {
                var at = ctx.RandomPoint(0.08f * s);
                ctx.PaintGround("beach", at, wind + ctx.Range(-10f, 10f), ctx.Range(0.30f, 0.50f) * s, ctx.Range(0.05f, 0.08f) * s, 0.5f, 0.65f);
            }

            // 2. The wadi: a meandering line across the flank, about 0.4 S off the centre (so it keeps clear of the unit
            //    spot), painted as dirt banks with a narrower pale gravel bed.
            float side = ctx.Range(0f, 360f), offset = ctx.Range(0.38f, 0.45f) * s;
            float amp = ctx.Range(0.04f, 0.06f) * s, phase = ctx.Range(0f, 6.283f);
            var normal = Flat(ctx.At(side, 1f) - ctx.Centre).normalized;
            var along = new Vector3(normal.z, 0f, -normal.x);
            var wadi = new List<Vector3>();
            for (int k = 0; k <= 20; k++)
            {
                float t = -1f + k * 0.1f;
                wadi.Add(ctx.Centre + normal * (offset + amp * Mathf.Sin(3.4f * t + phase)) + along * (t * s));
            }
            for (int k = 0; k < wadi.Count - 1; k++)
            {
                ctx.PaintStroke("dirt", wadi[k], wadi[k + 1], 0.06f * s, 0.85f);
                ctx.PaintStroke("beach", wadi[k], wadi[k + 1], 0.022f * s, 0.7f);
            }
            bool NearWadi(Vector3 p, float within)
            {
                for (int k = 0; k < wadi.Count - 1; k++) if (DistToSegment(p, wadi[k], wadi[k + 1]) < within) return true;
                return false;
            }

            // Pieces already down, as circles (centre, radius in metres), so later pieces keep off them.
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Flat(at - t.at).magnitude < r + t.r) return false;
                return true;
            }

            // 3. The Bedouin camp on the far flank: up to three tents along the contour, staggered up and down the slope,
            //    each a row of two or three dark boxes along its heading.
            float campBearing = side + 180f + ctx.Range(-35f, 35f);
            var camp = ctx.At(campBearing, ctx.Range(0.52f, 0.58f));
            var radial = Flat(camp - ctx.Centre).normalized;
            var contour = new Vector3(radial.z, 0f, -radial.x);
            float[] stagger = { 0f, 0.07f, -0.02f };
            for (int j = 0; j < MaxTents; j++)
            {
                int boxes = 2 + ctx.Rng.Next(2);
                float w = ctx.Range(0.045f, 0.055f) * s, turn = ctx.Range(-10f, 10f);
                var centre = camp + contour * ((j - 1) * 0.21f * s) + radial * (stagger[j] * s);
                var dir = Quaternion.Euler(0f, turn, 0f) * contour;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                for (int b = 0; b < boxes; b++)
                {
                    var at = centre + dir * ((b - 0.5f * (boxes - 1)) * 1.04f * w);
                    if (ctx.InUnitSpot(at, 0.75f * w) || NearWadi(at, 0.75f * w + 0.03f * s)) continue;
                    if (ctx.PlaceByWidth(Tent, at, yaw, w, StandIn.Box, TentBlack, maxSlopeDeg: 40f)) taken.Add((at, 0.75f * w));
                }
            }

            // 4. Acacias along the wadi banks, alternating sides, a few only.
            int acacias = 0;
            for (int k = 2; k < wadi.Count - 1 && acacias < MaxAcacias; k += 3)
            {
                var dirK = Flat(wadi[k + 1] - wadi[k]).normalized;
                var bank = new Vector3(dirK.z, 0f, -dirK.x) * ((k / 3) % 2 == 0 ? 1f : -1f);
                var at = wadi[k] + bank * (ctx.Range(0.045f, 0.06f) * s);
                float height = ctx.Range(0.10f, 0.12f) * s, yaw = ctx.Range(0f, 360f);
                if (!ctx.Inside(at, 0.05f * s) || ctx.InUnitSpot(at, 0.05f * s) || !Free(at, 0.05f * s)) continue;
                if (ctx.PlaceByHeight(Acacia, at, yaw, height, StandIn.Tree, AcaciaGreen, footprintShare: 0.8f, maxSlopeDeg: 40f))
                {
                    taken.Add((at, 0.05f * s)); acacias++;
                }
            }

            // 5. Desert rocks: two large ones out on the flanks, then small ones scattered, off the wadi, camp and unit spot.
            int rocks = 0, big = 0;
            for (int i = 0; i < 40 && rocks < MaxRocks; i++)
            {
                bool large = big < MaxBigRocks;
                float w = (large ? ctx.Range(0.11f, 0.15f) : ctx.Range(0.04f, 0.07f)) * s, yaw = ctx.Range(0f, 360f);
                var at = large ? ctx.At(ctx.Range(0f, 360f), ctx.Range(0.48f, 0.66f)) : ctx.RandomPoint(0.04f * s);
                float r = 0.75f * w;
                if (ctx.InUnitSpot(at, r) || NearWadi(at, r + 0.03f * s) || !Free(at, r + 0.03f * s)) continue;
                if (ctx.PlaceByWidth(Rock, at, yaw, w, StandIn.Rock, RockTan, maxSlopeDeg: 45f))
                {
                    taken.Add((at, r)); rocks++;
                    if (large) big++;
                }
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = Flat(b - a), ap = Flat(p - a);
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return (ap - ab * t).magnitude;
        }
    }
}
