#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Upper Galilee's maquis on rounded limestone hills, 1948. Dense, wild woodland with the
    /// common oak dominant, a few clumps of Aleppo (Jerusalem) pine, terebinths among the oaks, grey-green garrigue in the
    /// gaps, pale limestone outcrops with rocky aprons, and a goat track winding across. No settlement. It is wilder and
    /// denser than olive_terraces: no terraces, no rows, oak rather than olive.
    ///
    /// Sources (checked 2026-10-19):
    /// - Dense Mediterranean maquis over much of the Upper Galilee (the densest in the country), with the common oak
    ///   (alon matsui), the Palestine terebinth (ela eretz-yisraelit) and the Jerusalem pine among its common trees:
    ///   https://he.wikipedia.org/wiki/הגליל_העליון
    /// - Maquis trees of the region (Palestine oak, terebinth, carob, olive) and Aleppo pine in the Levant:
    ///   https://en.wikipedia.org/wiki/Eastern_Mediterranean_conifer%E2%80%93sclerophyllous%E2%80%93broadleaf_forests
    /// The model sandbox:tabor_oak_lite stands in for the common oak (Quercus calliprinos); the sources do not list the
    /// Tabor oak for the Upper Galilee. The terebinth, garrigue and goats have no model and draw tinted stand-ins.
    ///
    /// Layout: the goat track runs between the fixed edge midpoints at bearings 210 and 30 (so it meets the track of a like
    /// neighbour), bowed and wiggled, the wiggle dying out at both ends. The woodland is a jittered lattice of cells
    /// (0.14 S apart) taken in a seeded order to the caps; cells within reach of one of three pine clumps take a pine. Tall
    /// pieces keep out of ctx.InUnitSpot, and the woodland runs out to the hex edge so two like hexes read as one hillside.
    /// All clearances use the drawn size (HexScale.PieceScale 1.35 x the asked size).
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing, so the caps are the worst case):
    /// 9 pines x 1,600 = 14,400; 40 oaks x 650 = 26,000; 5 limestone x 700 = 3,500; 8 terebinths + 30 garrigue + 3 goats
    /// = 41 stand-ins x under 100 = under 4,100. Worst case under 48,000 of 53,000.</summary>
    [HexPrototype("upper_galilee_hills", Kind = "nature", Region = "north")]
    public sealed class UpperGalileeHills : IHexPrototype
    {
        public string Title => "Upper Galilee maquis on limestone hills (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 53000;

        const string Oak = "sandbox:tabor_oak_lite", Pine = "sandbox:aleppo_pine_lite", Rock = "sandbox:limestone*",
            Terebinth = "tiles:upper_galilee_hills/terebinth", Garrigue = "tiles:upper_galilee_hills/garrigue",
            Goat = "tiles:upper_galilee_hills/goat";
        const int MaxPines = 9, MaxOaks = 40, MaxRocks = 5, MaxTerebinths = 8, MaxGarrigue = 30, Goats = 3;
        const float Scale = HexScale.PieceScale;

        static readonly Color OakGreen = new Color(0.27f, 0.38f, 0.19f), PineGreen = new Color(0.19f, 0.32f, 0.17f),
            TerebinthGreen = new Color(0.36f, 0.40f, 0.22f), GarrigueGrey = new Color(0.50f, 0.55f, 0.43f),
            Limestone = new Color(0.86f, 0.83f, 0.75f), GoatBlack = new Color(0.16f, 0.14f, 0.12f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The goat track: fixed edge midpoints (210 to 30), a seeded bow through the middle and a small wiggle.
            //    Its points are fixed here; its paint goes last (step 7), so it lies on top of the garrigue and rock aprons.
            Vector3 p0 = ctx.At(210f, 0.97f), p2 = ctx.At(30f, 0.97f);
            Vector3 across = (ctx.At(120f, 1f) - ctx.Centre) / s;
            Vector3 pc = ctx.Centre + across * (ctx.Range(-0.22f, 0.22f) * s);
            float wiggle = ctx.Range(0.03f, 0.05f) * s, phase = ctx.Range(0f, 6.283f);
            var track = new List<Vector3>();
            for (int k = 0; k <= 18; k++)
            {
                float t = k / 18f, u = 1f - t;
                var at = u * u * p0 + 2f * u * t * pc + t * t * p2;
                var tangent = Flat(2f * u * (pc - p0) + 2f * t * (p2 - pc)).normalized;
                var side = new Vector3(tangent.z, 0f, -tangent.x);
                track.Add(at + side * (wiggle * Mathf.Sin(Mathf.PI * t) * Mathf.Sin(4f * Mathf.PI * t + phase)));
            }
            bool NearTrack(Vector3 p, float within)
            {
                for (int k = 0; k < track.Count - 1; k++) if (DistToSegment(p, track[k], track[k + 1]) < within) return true;
                return false;
            }

            // 2. Grey-green garrigue ground: soft blotches of the grass layer over the earthy hills ground.
            for (int k = 0; k < 8; k++)
            {
                var at = ctx.RandomPoint(0.05f * s);
                ctx.PaintGround("grass", at, ctx.Range(0f, 180f), ctx.Range(0.20f, 0.34f) * s, ctx.Range(0.14f, 0.24f) * s, 0.5f, ctx.Range(0.5f, 0.75f));
            }

            // 3. Limestone outcrops on the open hillside, each with a rocky apron; never in the unit spot or on the track.
            var rocks = new List<(Vector3 at, float r)>();
            bool NearRock(Vector3 p, float within)
            {
                foreach (var c in rocks) if (Flat(p - c.at).magnitude < c.r + within) return true;
                return false;
            }
            for (int i = 0; i < 24 && rocks.Count < MaxRocks; i++)
            {
                var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.42f, 0.80f));
                float w = ctx.Range(0.08f, 0.13f) * s, yaw = ctx.Range(0f, 360f), r = 0.71f * Scale * w;   // half-diagonal, drawn size
                if (ctx.InUnitSpot(at, r) || NearTrack(at, r + 0.03f * s) || NearRock(at, r + 0.06f * s)) continue;
                if (!ctx.PlaceByWidth(Rock, at, yaw, w, StandIn.Rock, Limestone, maxSlopeDeg: 70f)) continue;
                rocks.Add((at, r));
                ctx.PaintGround("mountain", at, yaw, 2.0f * Scale * w, 1.7f * Scale * w, 0.45f, 0.85f);
            }

            // 4. Pine clumps: three centres on the outer hillside, off the track.
            var clumps = new List<Vector3>();
            for (int i = 0; i < 20 && clumps.Count < 3; i++)
            {
                var at = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.48f, 0.80f));
                bool apart = true;
                foreach (var c in clumps) if (Flat(at - c).magnitude < 0.45f * s) apart = false;
                if (apart && !NearTrack(at, 0.12f * s)) clumps.Add(at);
            }
            bool InClump(Vector3 p)
            {
                foreach (var c in clumps) if (Flat(p - c).magnitude < 0.17f * s) return true;
                return false;
            }

            // 5. The woodland lattice: jittered cells, a seeded key each, taken in key order to the caps.
            float gap = 0.14f * s;
            var cells = new List<(Vector3 at, double key)>();
            for (int j = -8; j <= 8; j++)
                for (int i = -8; i <= 8; i++)
                {
                    var at = ctx.Centre + new Vector3((i + 0.5f * (j & 1)) * gap, 0f, j * gap * 0.866f)
                        + new Vector3(ctx.Range(-0.03f, 0.03f) * s, 0f, ctx.Range(-0.03f, 0.03f) * s);
                    double key = ctx.Rng.NextDouble();
                    if (!ctx.Inside(at, 0.03f * s) || ctx.InUnitSpot(at, 0.06f * s) || NearTrack(at, 0.055f * s) || NearRock(at, 0.05f * s)) continue;
                    cells.Add((at, key));
                }
            var order = new int[cells.Count];
            for (int k = 0; k < order.Length; k++) order[k] = k;
            Array.Sort(order, (a, b) => cells[a].key != cells[b].key ? cells[a].key.CompareTo(cells[b].key) : a.CompareTo(b));

            int pines = 0, oaks = 0, terebinths = 0, garrigue = 0;
            foreach (int k in order)
            {
                var at = cells[k].at;
                float yaw = ctx.Range(0f, 360f);
                int roll = ctx.Rng.Next(10);
                if (InClump(at) && pines < MaxPines)
                {
                    if (ctx.PlaceByHeight(Pine, at, yaw, ctx.Range(0.16f, 0.19f) * s, StandIn.Pine, PineGreen, 0.3f, 50f)) pines++;
                    continue;
                }
                if (roll == 7 && terebinths < MaxTerebinths)
                {
                    if (ctx.PlaceByHeight(Terebinth, at, yaw, ctx.Range(0.11f, 0.13f) * s, StandIn.Tree, TerebinthGreen, 0.55f, 50f)) terebinths++;
                    continue;
                }
                if (roll < 8 && oaks < MaxOaks)
                {
                    if (ctx.PlaceByHeight(Oak, at, yaw, ctx.Range(0.12f, 0.15f) * s, StandIn.Tree, OakGreen, 0.45f, 50f)) oaks++;
                    continue;
                }
                // The rest (and any overflow) is garrigue: two or three low, grey-green bushes about the cell.
                for (int b = 0; b < 3 && garrigue < MaxGarrigue; b++)
                {
                    var p = at + new Vector3(ctx.Range(-0.04f, 0.04f) * s, 0f, ctx.Range(-0.04f, 0.04f) * s);
                    float h = ctx.Range(0.035f, 0.055f) * s;
                    if (ctx.InUnitSpot(p, 0.03f * s)) continue;
                    if (ctx.PlaceByHeight(Garrigue, p, ctx.Range(0f, 360f), h, StandIn.Tree, GarrigueGrey, 1.0f, 60f)) garrigue++;
                }
            }

            // 6. Life: three goats on the track, out where it nears the hex edge (never in the unit spot).
            var herd = track[14];
            for (int g = 0; g < Goats; g++)
            {
                var p = herd + new Vector3(ctx.Range(-0.05f, 0.05f) * s, 0f, ctx.Range(-0.05f, 0.05f) * s);
                if (ctx.InUnitSpot(p, 0.04f * s)) continue;
                ctx.PlaceByHeight(Goat, p, ctx.Range(0f, 360f), 0.055f * s, StandIn.Figure, GoatBlack, 1.2f, 50f);
            }

            // 7. The track's paint, last: paints apply in order.
            for (int k = 0; k < track.Count - 1; k++) ctx.PaintStroke("dirt", track[k], track[k + 1], 0.024f * s, 0.75f);
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
