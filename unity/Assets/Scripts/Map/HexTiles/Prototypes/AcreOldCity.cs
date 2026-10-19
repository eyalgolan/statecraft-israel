#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Acre's walled old city on its promontory (1948), drawn Civ-style as a city-centre hex. Designed for board hex
    /// 7,6: flat and coastal, with the sea to the west.
    /// - The tile finds the sea side: the bearing on which water comes nearest. With no water, west. Everything is laid in a
    ///   frame turned to it: a toward the sea, b along the coast (b positive is north when the sea is west).
    /// - The walls make a square ring, half-side 0.48 S, of short blocks of two alternating sizes. The sea wall on the sea
    ///   side follows the shore (ctx.IsWater) and stops short of the water. The heavy land wall runs along the north and the
    ///   east with towers, a larger corner bastion at the north-east (Burj al-Kumandar) and a land gate in the east wall. The
    ///   citadel stands in the north wall. Jazzar Pasha built the land wall in 1800 to 1814; the sea wall is Daher al-Umar's,
    ///   later reinforced; the citadel lies in the northern wall (https://en.wikipedia.org/wiki/Acre,_Israel, checked
    ///   2026-10-19).
    /// - The fishing port: a gap in the sea wall toward the south, with a painted quay and up to two fishing boats on the
    ///   shore beyond it, only where the sea is near. A generic small harbour: the cited article describes the small
    ///   fishing harbour of today, so its 1948 use is inferred.
    /// - Inside, dense stone houses on a tight grid of lots (kit_jaffa_2, kit_jaffa_3, kit_jerusalem 1 to 3), with a few
    ///   courtyard gardens, round a small open square at the centre kept for a unit. Two streets cross at the square.
    /// - The one named landmark, low-key: the al-Jazzar Mosque (1781) in the north-east quarter, a small prayer hall with a
    ///   pale silver-white dome and a slim minaret. The dome was originally silvery-white
    ///   (https://en.wikipedia.org/wiki/Al-Jazzar_Mosque, checked 2026-10-19).
    /// - Outside the walls, on the inland side: gardens, a few newer houses, olives and palms.
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound):
    /// - At most 20 houses inside and 4 outside (kit, up to 1,400 each) = 33,600.
    /// - At most 3 courtyard trees and 3 outside olives x 1,600 = 9,600, 2 palms x 280 = 560.
    /// - At most 2 fishing boats x 13,437 = 26,874. They appear only where the sea is near, so the FlatSurface test never
    ///   places them.
    /// - About 92 wall blocks and 14 towers as stand-ins, about 1,300. Citadel, prayer hall, dome and minaret: about 80.
    /// - Total about 72,000 of 80,000 with the port, about 45,100 without it.
    ///
    /// The walls, towers, citadel, prayer hall, dome and minaret have no models (tiles:acre_old_city/...), so they always
    /// draw stand-ins. The dome is a Tree stand-in whose crown rises out of the prayer hall.</summary>
    [HexPrototype("acre_old_city", Kind = "city", Region = "north")]
    public sealed class AcreOldCity : IHexPrototype
    {
        public string Title => "Acre, the walled old city (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        // Measured 150,895 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 170000;

        // Explicit house ids: kit_jaffa and kit_jerusalem_4 are the mosque-like b17 mesh, kept out of the house rows.
        static readonly string[] Houses = { "sandbox:kit_jaffa_2", "sandbox:kit_jaffa_3", "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3" };
        const string Boat = "proto:vehicle-fishing-boat-1948-v2", Olive = "sandbox:olive_lite", Palm = "sandbox:date_palm",
            Wall = "tiles:acre_old_city/wall", Tower = "tiles:acre_old_city/tower", Citadel = "tiles:acre_old_city/citadel",
            PrayerHall = "tiles:acre_old_city/jazzar_prayer_hall", Dome = "tiles:acre_old_city/jazzar_dome", Minaret = "tiles:acre_old_city/jazzar_minaret";

        static readonly Color Limestone = new Color(0.84f, 0.77f, 0.64f), WallStone = new Color(0.78f, 0.71f, 0.57f),
            TowerStone = new Color(0.72f, 0.65f, 0.52f), SilverWhite = new Color(0.88f, 0.89f, 0.88f), Hull = new Color(0.45f, 0.33f, 0.22f),
            OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        const float Half = 0.48f;
        const int MaxHouses = 20, MaxYards = 3, MaxBoats = 2;

        /// <summary>House lot centres on each frame axis, as shares of S; the wider gap about 0 leaves the cross streets open.</summary>
        static readonly float[] Lots = { -0.36f, -0.23f, -0.10f, 0.10f, 0.23f, 0.36f };

        /// <summary>Where houses may not go, in the frame (a, b, half-sizes, shares of S): the square, the citadel, the mosque
        /// and the fishing port.</summary>
        static readonly (float a, float b, float ha, float hb)[] KeepOut =
        {
            (0f, 0f, 0.14f, 0.14f), (-0.06f, 0.40f, 0.10f, 0.08f), (-0.30f, 0.16f, 0.08f, 0.08f), (0.36f, -0.28f, 0.06f, 0.10f),
        };

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The sea side: the bearing on which water comes nearest the centre; with none, west.
            float seaB = 270f, near = 1f;
            bool sea = false;
            for (int i = 0; i < 24; i++)
            {
                float bb = 15f * i;
                for (float r = 0.30f; r < near && r <= 0.9f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(bb, r))) { seaB = bb; near = r; sea = true; break; }
            }
            Vector3 toSea = Dir(seaB), alongCoast = Dir(seaB + 90f);
            Vector3 P(float a, float b) => ctx.Centre + toSea * (a * s) + alongCoast * (b * s);
            // How far toward the sea the sea wall may stand at b: just short of the first water, never beyond the square.
            float SeaWall(float b)
            {
                for (float a = 0.2f; a <= Half; a += 0.02f) if (ctx.IsWater(P(a + 0.06f, b))) return a;
                return Half;
            }
            bool Free(float a, float b, float half)
            {
                foreach (var k in KeepOut) if (Mathf.Abs(a - k.a) < half + k.ha && Mathf.Abs(b - k.b) < half + k.hb) return false;
                return true;
            }
            // The frame's yaw: a piece's local z points to the sea (a) and its local x along the coast (b), so a painted
            // rectangle's width runs along b and its depth along a.
            float yaw = seaB;

            // 2. Ground: beaten earth inside the walls, gardens outside on the inland side, and two streets crossing at the
            //    open square (painted, kept clear for a unit).
            ctx.PaintGround("dirt", ctx.Centre, yaw, 0.94f * s, 0.94f * s, 0.05f, 0.55f);
            ctx.PaintGround("green_crop", P(-0.64f, -0.05f), yaw, 0.90f * s, 0.22f * s, 0.3f, 0.6f);
            ctx.PaintGround("green_crop", P(-0.05f, 0.64f), yaw, 0.20f * s, 0.70f * s, 0.3f, 0.6f);
            ctx.PaintGround("beach", ctx.Centre, yaw, 0.24f * s, 0.24f * s, 0.25f, 0.8f);
            ctx.PaintStroke("dirt", P(-0.85f, 0f), P(SeaWall(0f), 0f), 0.05f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0f, -Half), P(0f, 0.34f), 0.05f * s, 0.9f);

            // 3. The walls: blocks 0.04 S apart, alternately 0.045 and 0.034 S. Gaps: the land gate (east wall, on the axis),
            //    the citadel (north wall) and the fishing port (sea wall, toward the south). The sea wall follows the shore.
            for (int i = 0; i <= 22; i++)
            {
                float t = -0.44f + 0.04f * i, w = (i % 2 == 0 ? 0.045f : 0.034f) * s;
                if (Mathf.Abs(t + 0.06f) > 0.10f) ctx.PlaceByWidth(Wall, P(t, Half), yaw, w, StandIn.Box, WallStone);       // north (land)
                if (Mathf.Abs(t) > 0.08f) ctx.PlaceByWidth(Wall, P(-Half, t), yaw, w, StandIn.Box, WallStone);              // east (land)
                ctx.PlaceByWidth(Wall, P(t, -Half), yaw, w, StandIn.Box, WallStone);                                          // south
                if (t < -0.38f || t > -0.18f) ctx.PlaceByWidth(Wall, P(SeaWall(t), t), yaw, w, StandIn.Box, WallStone);      // sea
            }
            // Towers: the corners, the gate pair, two more on each land wall, and the Burj al-Kumandar at the north-east.
            ctx.PlaceByWidth(Tower, P(-Half, Half), yaw, 0.10f * s, StandIn.Box, TowerStone);
            foreach (var (a, b) in new[] { (Half, Half), (-Half, -Half), (Half, -Half) })
                ctx.PlaceByWidth(Tower, P(a > 0f ? SeaWall(b) : a, b), yaw, 0.075f * s, StandIn.Box, TowerStone);
            foreach (var (a, b) in new[] { (-Half, 0.08f), (-Half, -0.08f), (-Half, 0.26f), (-Half, -0.28f), (-0.30f, Half), (0.26f, Half) })
                ctx.PlaceByWidth(Tower, P(a, b), yaw, 0.055f * s, StandIn.Box, TowerStone);
            ctx.PlaceByWidth(Citadel, P(-0.06f, 0.42f), yaw + 180f, 0.15f * s, StandIn.Box, TowerStone);

            // 4. The al-Jazzar Mosque in the north-east quarter: a painted courtyard, a low prayer hall with the pale dome
            //    rising out of it, and the minaret at the courtyard's corner.
            var mosque = P(-0.30f, 0.16f);
            ctx.PaintGround("beach", mosque, yaw, 0.20f * s, 0.20f * s, 0.25f, 0.9f);
            ctx.PlaceByWidth(PrayerHall, mosque, yaw, 0.08f * s, StandIn.Box, Limestone);
            ctx.PlaceByHeight(Dome, mosque, yaw, 0.12f * s, StandIn.Tree, SilverWhite, footprintShare: 0.6f);
            ctx.PlaceByHeight(Minaret, P(-0.21f, 0.24f), yaw, 0.17f * s, StandIn.Pine, Limestone, footprintShare: 0.2f);

            // 5. Houses on the lot grid, each turned square to the frame. Lots on the square, the citadel, the mosque or the
            //    port, or past the sea wall, are skipped. About one lot in seven is a courtyard garden with an olive or palm.
            int houses = 0, yards = 0;
            foreach (float la in Lots)
                foreach (float lb in Lots)
                {
                    float a = la + ctx.Range(-0.005f, 0.005f), b = lb + ctx.Range(-0.005f, 0.005f), w = ctx.Range(0.088f, 0.094f);
                    int roll = ctx.Rng.Next(100), turn = ctx.Rng.Next(4);
                    float half = 0.5f * w * HexScale.PieceScale;
                    if (!Free(a, b, half) || a + half > SeaWall(b) - 0.03f) continue;
                    var at = P(a, b);
                    if (roll < 14 && yards < MaxYards)
                    {
                        ctx.PaintGround("grass", at, yaw, 0.09f * s, 0.09f * s, 0.3f, 0.6f);
                        bool placed = roll % 2 == 0
                            ? ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), 0.12f * s, StandIn.Palm, null, 0.3f)
                            : ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), 0.11f * s, StandIn.Tree, OliveLeaf, 0.5f);
                        if (placed) yards++;
                        continue;
                    }
                    if (houses < MaxHouses && ctx.PlaceByWidth(Houses[houses % Houses.Length], at, yaw + 90f * turn + ctx.Range(-3f, 3f), w * s, StandIn.Box, Limestone)) houses++;
                }

            // 6. The fishing port: a painted quay in the wall gap, and, only where the sea is near, up to two boats on the
            //    shore beyond it (stepping out until water shows just ahead, as Jaffa does).
            ctx.PaintGround("beach", P(SeaWall(-0.28f) - 0.02f, -0.28f), yaw, 0.20f * s, 0.12f * s, 0.3f, 0.9f);
            int boats = 0;
            if (sea)
                foreach (float b in new[] { -0.24f, -0.33f })
                {
                    if (boats >= MaxBoats) break;
                    for (float a = 0.40f; a <= 0.85f; a += 0.03f)
                    {
                        if (!ctx.IsWater(P(a + 0.09f, b))) continue;
                        for (int back = 0; back < 3; back++)
                            if (ctx.PlaceByWidth(Boat, P(a - 0.03f * back, b), seaB + ctx.Range(-15f, 15f), 0.12f * s, StandIn.Box, Hull)) { boats++; break; }
                        break;
                    }
                }

            // 7. Outside the walls on the inland side: four newer houses, and olives and palms between them, kept apart.
            var outside = new List<(Vector3 at, float r)>();
            bool Out(Vector3 at, float r, System.Func<bool> place)
            {
                foreach (var t in outside) if (Planar(at - t.at) < r + t.r) return false;
                if (!place()) return false;
                outside.Add((at, r));
                return true;
            }
            int n = 0;
            foreach (var (a, b) in new[] { (-0.64f, -0.30f), (-0.64f, 0.10f), (-0.20f, 0.64f), (0.14f, 0.64f) })
            {
                var at = P(a, b);
                float w = ctx.Range(0.10f, 0.11f) * s;
                string model = Houses[(n++ + 2) % Houses.Length];
                Out(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(model, at, yaw + 90f * ctx.Rng.Next(4), w, StandIn.Box, Limestone));
            }
            foreach (var (a, b, palm) in new[] { (-0.66f, -0.10f, false), (-0.03f, 0.66f, false), (0.38f, 0.62f, false), (-0.62f, 0.36f, true), (-0.64f, -0.50f, true) })
            {
                var at = P(a, b);
                float h = ctx.Range(0.12f, 0.14f) * s;
                Out(at, 0.3f * h * HexScale.PieceScale, () => palm
                    ? ctx.PlaceByHeight(Palm, at, ctx.Range(0f, 360f), h, StandIn.Palm, null, 0.3f)
                    : ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), h, StandIn.Tree, OliveLeaf, 0.5f));
            }
        }

        static Vector3 Dir(float bearingDeg)
        {
            float a = bearingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
