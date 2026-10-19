#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Carmel ridge, 1948. A long wooded ridge of Aleppo pine and common oak maquis, a dirt
    /// track along the crest, a line of pale limestone outcrops and low cliffs above the steep north-eastern face, and a
    /// few carob trees. Greener and denser than judean_highlands, and laid along a line (the ridge) rather than round crags
    /// at the corners. No settlement and no landmark.
    ///
    /// Sources (checked 2026-10-19):
    /// - The range slopes gently to the south-west and forms a steep ridge on its north-eastern face; its slopes carry
    ///   oak, pine, olive and laurel: https://en.wikipedia.org/wiki/Mount_Carmel
    /// - Mount Carmel is covered mainly by natural forest of common oak (Quercus calliprinos) maquis mixed with Aleppo
    ///   (Jerusalem) pine: https://kalanit.org.il/?p=42283
    /// - Carob is a predominant maquis tree of the region (named for the ecoregion, not for the Carmel itself):
    ///   https://en.wikipedia.org/wiki/Eastern_Mediterranean_conifer%E2%80%93sclerophyllous%E2%80%93broadleaf_forests
    /// The lane brief asked for cliffs on the north-west face; the source above gives the north-east, so the cliffs are
    /// there. The model sandbox:tabor_oak_lite stands in for the common oak; the maquis scrub draws a tinted stand-in.
    ///
    /// Layout: the crest runs on a fixed axis between the edge midpoints at bearings 150 and 330 (the hex lattice's nearest
    /// direction to the ridge's north-west to south-east line), so like neighbours along the ridge join. The outcrops sit
    /// in a row parallel to the crest, 0.5 S out on the north-east side (bearing 60), clear of the unit spot by their
    /// drawn half-diagonal; below them the steep face is sparser. The woodland is a jittered lattice (0.13 S) taken in a
    /// seeded order to the caps, running out to the hex edge so two like hexes read as one forest. Tall pieces keep out
    /// of ctx.InUnitSpot. All clearances use the drawn size (HexScale.PieceScale 1.35 x the asked size).
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing): 14 pines x 1,600 = 22,400; 30 oaks
    /// x 650 = 19,500; 5 carobs x 650 = 3,250; 7 limestone x 700 = 4,900; 24 scrub stand-ins x under 100 = under 2,400.
    /// Worst case under 52,450 of 58,000.</summary>
    [HexPrototype("carmel_ridge", Kind = "nature", Region = "north")]
    public sealed class CarmelRidge : IHexPrototype
    {
        public string Title => "Carmel ridge: pine and oak forest, limestone face (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 58000;

        const string Pine = "sandbox:aleppo_pine_lite", Oak = "sandbox:tabor_oak_lite", Carob = "sandbox:carob_lite",
            Rock = "sandbox:limestone*", Scrub = "tiles:carmel_ridge/scrub";
        const int MaxPines = 14, MaxOaks = 30, MaxCarobs = 5, MaxRocks = 7, MaxScrub = 24;
        const float Axis = 330f, Face = 60f, CliffOffset = 0.50f, Scale = HexScale.PieceScale;

        static readonly Color PineGreen = new Color(0.18f, 0.32f, 0.16f), OakGreen = new Color(0.25f, 0.38f, 0.18f),
            CarobGreen = new Color(0.22f, 0.36f, 0.17f), ScrubGreen = new Color(0.36f, 0.46f, 0.26f),
            Limestone = new Color(0.90f, 0.87f, 0.79f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            Vector3 along = (ctx.At(Axis, 1f) - ctx.Centre) / s, outward = (ctx.At(Face, 1f) - ctx.Centre) / s;
            float OutOf(Vector3 p) => Vector3.Dot(Flat(p - ctx.Centre), outward) / s;   // + is the steep north-east side

            // 1. Ground: green forest floor on the gentle south-west flank, a rocky band along the cliff line, earthier
            //    ground on the steep face below it.
            ctx.PaintGround("grass", ctx.Centre - outward * (0.35f * s), Axis, 0.70f * s, 2.0f * s, 0.35f, 0.7f);
            ctx.PaintStroke("mountain", ctx.Centre + outward * (CliffOffset * s) - along * s, ctx.Centre + outward * (CliffOffset * s) + along * s, 0.16f * s, 0.85f);
            ctx.PaintGround("hills", ctx.Centre + outward * (0.72f * s), Axis, 0.24f * s, 2.0f * s, 0.4f, 0.6f);

            // 2. The crest track: fixed edge midpoints (150 to 330), a small wiggle dying out at both ends.
            Vector3 p0 = ctx.At(Axis + 180f, 0.97f), p1 = ctx.At(Axis, 0.97f);
            float wiggle = ctx.Range(0.02f, 0.04f) * s, phase = ctx.Range(0f, 6.283f);
            var track = new List<Vector3>();
            for (int k = 0; k <= 16; k++)
            {
                float t = k / 16f;
                track.Add(Vector3.Lerp(p0, p1, t) + outward * (wiggle * Mathf.Sin(Mathf.PI * t) * Mathf.Sin(3f * Mathf.PI * t + phase)));
            }
            for (int k = 0; k < track.Count - 1; k++) ctx.PaintStroke("dirt", track[k], track[k + 1], 0.026f * s, 0.8f);
            bool NearTrack(Vector3 p, float within)
            {
                for (int k = 0; k < track.Count - 1; k++) if (DistToSegment(p, track[k], track[k + 1]) < within) return true;
                return false;
            }

            // 3. The limestone face: outcrops in a row parallel to the crest, never in the unit spot.
            var rocks = new List<(Vector3 at, float r)>();
            float t0 = ctx.Range(-0.08f, 0.0f);
            for (int k = 0; k < 10 && rocks.Count < MaxRocks; k++)
            {
                float t = -0.68f + t0 + 0.19f * k, w = ctx.Range(0.10f, 0.15f) * s, r = 0.71f * Scale * w;
                var at = ctx.Centre + along * (t * s) + outward * ((CliffOffset + ctx.Range(-0.03f, 0.03f)) * s);
                float yaw = Axis + ctx.Range(-15f, 15f);
                if (ctx.InUnitSpot(at, r)) continue;
                if (ctx.PlaceByWidth(Rock, at, yaw, w, StandIn.Rock, Limestone, maxSlopeDeg: 70f)) rocks.Add((at, r));
            }
            bool NearRock(Vector3 p, float within)
            {
                foreach (var c in rocks) if (Flat(p - c.at).magnitude < c.r + within) return true;
                return false;
            }

            // 4. The forest lattice: jittered cells, a seeded key each, taken in key order to the caps. Beyond the cliff
            //    line (the steep face) only about one cell in three is kept.
            float gap = 0.13f * s;
            var cells = new List<(Vector3 at, double key)>();
            for (int j = -9; j <= 9; j++)
                for (int i = -9; i <= 9; i++)
                {
                    var at = ctx.Centre + new Vector3((i + 0.5f * (j & 1)) * gap, 0f, j * gap * 0.866f)
                        + new Vector3(ctx.Range(-0.025f, 0.025f) * s, 0f, ctx.Range(-0.025f, 0.025f) * s);
                    double key = ctx.Rng.NextDouble();
                    bool face = OutOf(at) > CliffOffset + 0.05f;
                    if (face && key > 0.35) continue;
                    if (!ctx.Inside(at, 0.03f * s) || ctx.InUnitSpot(at, 0.06f * s) || NearTrack(at, 0.06f * s) || NearRock(at, 0.05f * s)) continue;
                    cells.Add((at, key));
                }
            var order = new int[cells.Count];
            for (int k = 0; k < order.Length; k++) order[k] = k;
            Array.Sort(order, (a, b) => cells[a].key != cells[b].key ? cells[a].key.CompareTo(cells[b].key) : a.CompareTo(b));

            int pines = 0, oaks = 0, carobs = 0, scrub = 0;
            foreach (int k in order)
            {
                var at = cells[k].at;
                float yaw = ctx.Range(0f, 360f);
                int roll = ctx.Rng.Next(100);
                if (roll < 38 && pines < MaxPines)
                {
                    if (ctx.PlaceByHeight(Pine, at, yaw, ctx.Range(0.16f, 0.20f) * s, StandIn.Pine, PineGreen, 0.3f, 50f)) pines++;
                    continue;
                }
                if (roll >= 38 && roll < 46 && carobs < MaxCarobs)
                {
                    if (ctx.PlaceByHeight(Carob, at, yaw, ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, CarobGreen, 0.45f, 50f)) carobs++;
                    continue;
                }
                if (oaks < MaxOaks)
                {
                    if (ctx.PlaceByHeight(Oak, at, yaw, ctx.Range(0.12f, 0.15f) * s, StandIn.Tree, OakGreen, 0.45f, 50f)) oaks++;
                    continue;
                }
                if (scrub < MaxScrub && ctx.PlaceByHeight(Scrub, at, yaw, ctx.Range(0.05f, 0.07f) * s, StandIn.Tree, ScrubGreen, 0.8f, 60f)) scrub++;
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
