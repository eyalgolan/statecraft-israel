#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: the Shephelah's low rolling hills, 1948. Wide terraces with low dry-stone walls follow the
    /// contours as concentric arcs on one side of the hill, old olives in rows along the terraces with a few carobs; on
    /// the other side broad contour strips of wheat and green fodder with almond trees in pale blossom; a stone field hut
    /// on the outer terrace; a farm track over the bare crown. No settlement. Lower and softer than olive_terraces: plains
    /// ground, two wide terraces on half the hill instead of three full rings, and more wheat between.
    ///
    /// Sources (checked 2026-10-19):
    /// - Gently rolling hills of soft chalk under marl between the Judean hills and the coastal plain, mostly under
    ///   250 to 450 m, largely rural and farmed: https://en.wikipedia.org/wiki/Shephelah
    /// - Olive was the dominant orchard tree of the Shephelah, with carob, almond, fig and sycamore fig also documented
    ///   (charcoal from Tel Azekah and Tel Lachish, an archaeobotanical source, not a 1948 one):
    ///   https://cris.tau.ac.il/en/publications/the-vegetation-history-of-the-shephelah-southern-levant-middle-br/
    /// - Terrace fields on the slopes and in the valleys of the southern Shephelah:
    ///   https://cris.iucc.ac.il/en/publications/agricultural-systems-and-terrace-pattern-distribution-and-preserv-3/
    /// The hut is a generic stone field hut (kit_stone_village_2, named explicitly so the wildcard never picks the
    /// mosque-like kit building). The almond and the wall stones draw tinted stand-ins.
    ///
    /// Layout: contours on the hill dome are circles about the centre, so the terrace walls are arcs at 0.36, 0.60 and
    /// 0.84 S over a seeded 220-degree sector (painted riser lines plus short runs of wall stones), the terraces between
    /// them 0.24 S wide. The wheat side carries contour strips. The track runs between the fixed edge midpoints at bearings
    /// 210 and 30, over the crown (the unit spot, kept free of tall pieces), so like neighbours join.
    ///
    /// Triangle budget by arithmetic (hard caps below; FlatSurface refuses nothing): 14 olives x 1,600 = 22,400;
    /// 4 carobs x 650 = 2,600; the hut 1 x 1,400; 36 wall stones + 10 almonds = 46 stand-ins x under 100 = under 4,600.
    /// Worst case under 31,000 of 34,500.</summary>
    [HexPrototype("shephelah_terraces", Kind = "nature", Region = "centre")]
    public sealed class ShephelahTerraces : IHexPrototype
    {
        public string Title => "Shephelah: low terraced hills, olives and wheat (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "plains";
        public int TriangleBudget => 34500;

        const string Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite", Hut = "sandbox:kit_stone_village_2",
            Wall = "tiles:shephelah_terraces/wall", Almond = "tiles:shephelah_terraces/almond";
        const int MaxOlives = 14, MaxCarobs = 4, MaxWall = 36, MaxAlmonds = 10, Segments = 14;
        const float Half = 110f;

        static readonly float[] Walls = { 0.36f, 0.60f, 0.84f };
        static readonly float[] Terraces = { 0.48f, 0.72f };

        static readonly Color OliveLeaf = new Color(0.40f, 0.46f, 0.30f), CarobLeaf = new Color(0.20f, 0.33f, 0.16f),
            Blossom = new Color(0.95f, 0.86f, 0.88f), WallStone = new Color(0.84f, 0.80f, 0.70f), HutStone = new Color(0.78f, 0.72f, 0.60f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            float face = ctx.Range(0f, 360f);   // the middle of the terraced side
            float phase = ctx.Range(0f, 360f);
            float Wobble(float b) => 1f + 0.04f * Mathf.Sin((b + phase) * Mathf.Deg2Rad);
            Vector3 On(float r, float b) => ctx.At(b, r * Wobble(b));

            // 1. The track over the crown, between fixed edge midpoints (painted last, over the terraces).
            Vector3 t0 = ctx.At(210f, 0.97f), t1 = ctx.At(30f, 0.97f);
            bool NearTrack(Vector3 p, float within) => DistToSegment(p, t0, t1) < within;

            // 2. Ground. The terraced side: a wheat terrace inside, a ploughed olive terrace outside, rocky riser lines.
            //    The other side: broad contour strips of wheat and green fodder.
            Arc("wheat", Terraces[0], face - Half, face + Half, 0.20f, 0.8f);
            Arc("ploughed", Terraces[1], face - Half, face + Half, 0.20f, 0.7f);
            foreach (float r in Walls) Arc("mountain", r, face - Half, face + Half, 0.02f, 0.8f);
            float other = face + 180f, otherHalf = 180f - Half - 6f;
            Arc("wheat", 0.46f, other - otherHalf, other + otherHalf, 0.16f, 0.85f);
            Arc("green_crop", 0.63f, other - otherHalf, other + otherHalf, 0.14f, 0.8f);
            Arc("wheat", 0.80f, other - otherHalf, other + otherHalf, 0.16f, 0.85f);

            // 3. The field hut on the outer terrace near one end of the sector, its path to the hex edge and a small yard.
            float hutB = face + (ctx.Rng.Next(2) == 0 ? -1f : 1f) * (Half - 18f);
            var hut = On(0.66f, hutB);   // the inner half of the outer terrace, so the hut stays inside the hex
            ctx.PaintGround("dirt", hut, hutB, 0.14f * s, 0.12f * s, 0.3f, 0.8f);
            ctx.PaintStroke("dirt", hut, ctx.At(hutB, 0.97f), 0.018f * s, 0.8f);
            bool hutUp = !NearTrack(hut, 0.12f * s) && ctx.PlaceByWidth(Hut, hut, hutB, 0.10f * s, StandIn.Box, HutStone, 40f);
            bool NearHut(Vector3 p) => hutUp && Flat(p - hut).magnitude < 0.15f * s;

            // 4. Wall stones: short runs along the riser arcs, where the walls show.
            int stones = 0;
            for (int run = 0; run < 12 && stones < MaxWall; run++)
            {
                float r = Walls[ctx.Rng.Next(Walls.Length)], b0 = face + ctx.Range(-Half + 8f, Half - 8f);
                float step = Mathf.Rad2Deg * (0.04f / r);
                for (int i = 0; i < 4 && stones < MaxWall; i++)
                {
                    var p = On(r, b0 + i * step);
                    if (ctx.InUnitSpot(p, 0.03f * s) || NearTrack(p, 0.04f * s) || NearHut(p)) continue;
                    if (ctx.PlaceByWidth(Wall, p, b0 + i * step + 90f, ctx.Range(0.025f, 0.032f) * s, StandIn.Rock, WallStone, 60f)) stones++;
                }
            }

            // 5. Old olives in a row along each terrace, with now and then a carob or a gap.
            int olives = 0, carobs = 0;
            foreach (float r in new[] { Terraces[1], Terraces[0] })
            {
                float step = Mathf.Rad2Deg * (0.13f / r), start = face - Half + ctx.Range(0.3f, 0.7f) * step;
                for (float b = start; b <= face + Half; b += step)
                {
                    var p = On(r + ctx.Range(-0.01f, 0.01f), b);
                    if (ctx.InUnitSpot(p, 0.06f * s) || NearTrack(p, 0.07f * s) || NearHut(p)) continue;
                    int roll = ctx.Rng.Next(8);
                    if (roll == 0) continue;
                    if (roll == 1 && carobs < MaxCarobs)
                    {
                        if (ctx.PlaceByHeight(Carob, p, ctx.Range(0f, 360f), ctx.Range(0.13f, 0.15f) * s, StandIn.Tree, CarobLeaf, 0.5f, 50f)) carobs++;
                        continue;
                    }
                    if (olives < MaxOlives && ctx.PlaceByHeight(Olive, p, ctx.Range(0f, 360f), ctx.Range(0.11f, 0.13f) * s, StandIn.Tree, OliveLeaf, 0.5f, 50f)) olives++;
                }
            }

            // 6. Almonds in pale blossom on the wheat side, loosely along the field edges between the strips.
            int almonds = 0;
            for (int i = 0; i < 30 && almonds < MaxAlmonds; i++)
            {
                float r = ctx.Rng.Next(2) == 0 ? 0.545f : 0.715f, b = other + ctx.Range(-otherHalf + 6f, otherHalf - 6f);
                var p = On(r, b);
                if (ctx.InUnitSpot(p, 0.06f * s) || NearTrack(p, 0.07f * s) || NearHut(p)) continue;
                if (ctx.PlaceByHeight(Almond, p, ctx.Range(0f, 360f), ctx.Range(0.10f, 0.12f) * s, StandIn.Tree, Blossom, 0.5f, 50f)) almonds++;
            }

            // 7. The track's paint goes last: paints apply in order, so it lies on top of the terrace bands.
            ctx.PaintStroke("dirt", t0, t1, 0.026f * s, 0.85f);

            // An open contour arc of one ground layer, as a polyline of strokes.
            void Arc(string layer, float r, float fromB, float toB, float widthShare, float strength)
            {
                Vector3 prev = On(r, fromB);
                for (int k = 1; k <= Segments; k++)
                {
                    var next = On(r, Mathf.Lerp(fromB, toB, k / (float)Segments));
                    ctx.PaintStroke(layer, prev, next, widthShare * s, strength);
                    prev = next;
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
