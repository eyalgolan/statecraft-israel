#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Tiberias on the Sea of Galilee (1948), drawn Civ-style as a town hex. Designed for board hex 10,9: flat, with
    /// the lake to the east.
    /// - The tile finds the lake side: the bearing on which water comes nearest. With no water, east. Everything is laid in a
    ///   frame turned to it: a toward the lake, b along the shore (b positive is south when the lake is east).
    /// - The black-basalt old town by the lake, walled on its three landward sides and open to the lake. The walls are short
    ///   dark blocks with round towers (twelve at most), a gate in the landward wall and a cross street to the lake. The town
    ///   was built of basalt blocks; Daher al-Umar's walls had twelve projecting round towers, and the town was "open to the
    ///   lake and walled on the other three sides" (Pococke, 1737) (https://en.wikipedia.org/wiki/Tiberias, checked
    ///   2026-10-19).
    /// - The old-town houses are dark basalt stand-ins (tiles:tiberias_town/basalt_house), not the limestone kit, so the
    ///   basalt reads on every machine; the props team can drop a basalt model in under that id. They stand on a tight grid
    ///   of lots round a small open square kept for a unit.
    /// - A lakeside promenade: painted along the shore, with date palms, and up to two fishing boats on the shore, only where
    ///   the lake is near.
    /// - Newer houses (kit) climb the slope west of the landward wall, with olives between.
    /// - The hot springs to the south: a small generic domed bathhouse. Al-Jazzar built a domed bathhouse at the springs in
    ///   1800, and Ibrahim Pasha a larger bath in the 1830s (https://en.wikipedia.org/wiki/Tiberias, checked 2026-10-19).
    /// - Two townspeople on the square.
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound):
    /// - 6 newer houses (kit, up to 1,400 each) = 8,400. At most 16 basalt houses as stand-ins, about 200.
    /// - At most 10 palms x 280 = 2,800. 3 olives x 1,600 = 4,800. 2 townspeople x 4,000 = 8,000.
    /// - At most 2 fishing boats x 13,437 = 26,874, only where the lake is near (never in the FlatSurface test).
    /// - About 70 wall blocks, 12 round towers, the bathhouse and its dome as stand-ins: about 1,200.
    /// - Total about 52,300 of 60,000 with the boats, about 25,400 without them.
    ///
    /// The walls, towers, basalt houses, bathhouse and dome have no models (tiles:tiberias_town/...), so they always draw
    /// stand-ins: Rock (a squat round shape) for a round tower, Tree for the dome.</summary>
    [HexPrototype("tiberias_town", Kind = "town", Region = "north")]
    public sealed class TiberiasTown : IHexPrototype
    {
        public string Title => "Tiberias, the basalt town by the lake (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        // Measured 64,357 on the israel board, where the lake brings the boat in (the flat test hex has none); plus 15%.
        public int TriangleBudget => 75000;

        // Explicit house ids for the newer houses: kit_jerusalem_4 is the mosque-like b17 mesh.
        static readonly string[] NewHouses = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3", "sandbox:kit_stone_village_2" };
        static readonly string[] People = { "proto:figure-townsman-c02-1948-v2", "proto:figure-woman-blue-dress-c10-1948-v2" };
        const string BasaltHouse = "tiles:tiberias_town/basalt_house", Wall = "tiles:tiberias_town/wall", Tower = "tiles:tiberias_town/round_tower",
            Bathhouse = "tiles:tiberias_town/bathhouse", BathDome = "tiles:tiberias_town/bathhouse_dome",
            Boat = "proto:vehicle-fishing-boat-1948-v2", Palm = "sandbox:date_palm", Olive = "sandbox:olive_lite";

        static readonly Color Basalt = new Color(0.36f, 0.35f, 0.35f), WallBasalt = new Color(0.40f, 0.38f, 0.37f),
            TowerBasalt = new Color(0.43f, 0.41f, 0.39f), Limestone = new Color(0.84f, 0.79f, 0.68f), DomeWhite = new Color(0.86f, 0.85f, 0.80f),
            Hull = new Color(0.45f, 0.33f, 0.22f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        /// <summary>The landward wall's a, and the north and south walls' b, as shares of S.</summary>
        const float WestA = -0.40f, NorthB = -0.44f, SouthB = 0.36f;
        const int MaxHouses = 16, MaxBoats = 1, MaxPalms = 10;   // one boat: the proto fishing boat is heavy, and it is placed only beside water, which the test hex lacks

        static readonly float[] LotsA = { -0.30f, -0.17f, 0.10f, 0.23f, 0.36f };
        static readonly float[] LotsB = { -0.30f, -0.17f, 0.10f, 0.23f };

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;

            // 1. The lake side: the bearing on which water comes nearest the centre; with none, east.
            float lakeB = 90f, near = 1f;
            bool lake = false;
            for (int i = 0; i < 24; i++)
            {
                float bb = 15f * i;
                for (float r = 0.30f; r < near && r <= 0.9f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(bb, r))) { lakeB = bb; near = r; lake = true; break; }
            }
            Vector3 toLake = Dir(lakeB), alongShore = Dir(lakeB + 90f);
            Vector3 P(float a, float b) => ctx.Centre + toLake * (a * s) + alongShore * (b * s);
            // The shore at b, as a share of S along a: the first water, or 0.80 when there is none.
            float Shore(float b)
            {
                for (float a = 0.3f; a <= 0.9f; a += 0.02f) if (ctx.IsWater(P(a, b))) return a;
                return 0.80f;
            }
            // How far toward the lake the town's walls and lots reach at b.
            float TownEnd(float b) => Mathf.Min(Shore(b) - 0.08f, 0.50f);
            // The frame's yaw: a piece's local z points to the lake (a), its local x along the shore (b).
            float yaw = lakeB;

            // 2. Ground: beaten earth in the walled town, the open square, the cross street from the landward gate to the
            //    lake, and the road south to the springs.
            ctx.PaintGround("dirt", P(0.05f, -0.04f), yaw, 0.82f * s, 0.92f * s, 0.06f, 0.55f);
            ctx.PaintGround("beach", ctx.Centre, yaw, 0.22f * s, 0.22f * s, 0.25f, 0.8f);
            ctx.PaintStroke("dirt", P(-0.85f, 0f), P(Shore(0f), 0f), 0.05f * s, 0.9f);
            ctx.PaintStroke("dirt", P(0.05f, SouthB), P(0.18f, 0.80f), 0.04f * s, 0.85f);

            // 3. The walls on the three landward sides: dark blocks 0.04 S apart, alternately 0.045 and 0.034 S, a gate in the
            //    landward wall, and the north and south walls running down to the shore.
            for (int i = 0; i <= 25; i++)
            {
                float t = -0.42f + 0.04f * i, w = (i % 2 == 0 ? 0.045f : 0.034f) * s;
                if (t <= SouthB - 0.02f && Mathf.Abs(t) > 0.07f) ctx.PlaceByWidth(Wall, P(WestA, t), yaw, w, StandIn.Box, WallBasalt);
                float aw = WestA + 0.02f + 0.04f * i;
                if (aw <= TownEnd(NorthB)) ctx.PlaceByWidth(Wall, P(aw, NorthB), yaw, w, StandIn.Box, WallBasalt);
                if (aw <= TownEnd(SouthB)) ctx.PlaceByWidth(Wall, P(aw, SouthB), yaw, w, StandIn.Box, WallBasalt);
            }
            // Round towers: the two landward corners, two on the landward wall beside the gate and two more on it, and two on
            //    each of the north and south walls, the last at the shore.
            foreach (var (a, b) in new[]
                     {
                         (WestA, NorthB), (WestA, SouthB), (WestA, -0.09f), (WestA, 0.09f), (WestA, -0.27f), (WestA, 0.23f),
                         (-0.10f, NorthB), (TownEnd(NorthB), NorthB), (-0.10f, SouthB), (TownEnd(SouthB), SouthB),
                         (0.20f, NorthB), (0.20f, SouthB),
                     })
                ctx.PlaceByWidth(Tower, P(a, b), yaw, 0.075f * s, StandIn.Rock, TowerBasalt);

            // 4. Basalt houses on the lot grid, each turned square to the frame; lots on the square or too near the shore
            //    are skipped.
            int houses = 0;
            foreach (float la in LotsA)
                foreach (float lb in LotsB)
                {
                    float a = la + ctx.Range(-0.005f, 0.005f), b = lb + ctx.Range(-0.005f, 0.005f), w = ctx.Range(0.088f, 0.094f);
                    int turn = ctx.Rng.Next(4);
                    float half = 0.5f * w * HexScale.PieceScale;
                    if (Mathf.Abs(a) < 0.10f + half && Mathf.Abs(b) < 0.10f + half) continue;   // the square
                    if (a + half > TownEnd(b) - 0.02f || houses >= MaxHouses) continue;
                    if (ctx.PlaceByWidth(BasaltHouse, P(a, b), yaw + 90f * turn + ctx.Range(-3f, 3f), w * s, StandIn.Box, Basalt)) houses++;
                }

            // 5. The promenade along the shore with palms (only where there is room beyond the last houses), and the boats.
            int palms = 0;
            for (float b = -0.40f; b <= 0.70f && palms < MaxPalms; b += 0.12f)
            {
                float ap = Shore(b) - 0.07f;
                ctx.PaintGround("beach", P(ap, b), yaw, 0.12f * s, 0.08f * s, 0.3f, 0.9f);
                bool inTown = b > NorthB && b < SouthB;
                if (inTown && ap < 0.47f) continue;
                if (ctx.PlaceByHeight(Palm, P(ap - 0.01f, b), ctx.Range(0f, 360f), ctx.Range(0.11f, 0.13f) * s, StandIn.Palm, null, 0.3f)) palms++;
            }
            int boats = 0;
            if (lake)
                foreach (float b in new[] { -0.04f, 0.26f })
                {
                    if (boats >= MaxBoats) break;
                    for (float a = 0.40f; a <= 0.85f; a += 0.03f)
                    {
                        if (!ctx.IsWater(P(a + 0.09f, b))) continue;
                        for (int back = 0; back < 3; back++)
                            if (ctx.PlaceByWidth(Boat, P(a - 0.03f * back, b), lakeB + ctx.Range(-15f, 15f), 0.12f * s, StandIn.Box, Hull)) { boats++; break; }
                        break;
                    }
                }

            // 6. Newer houses climbing the slope west of the landward wall, in two staggered rows, with olives between, all
            //    kept apart.
            var taken = new List<(Vector3 at, float r)>();
            bool Put(Vector3 at, float r, System.Func<bool> place)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                if (!place()) return false;
                taken.Add((at, r));
                return true;
            }
            int n = 0;
            foreach (var (a, b) in new[] { (-0.55f, -0.30f), (-0.55f, -0.08f), (-0.55f, 0.14f), (-0.71f, -0.19f), (-0.71f, 0.03f), (-0.71f, 0.25f) })
            {
                var at = P(a, b);
                float w = ctx.Range(0.095f, 0.105f) * s;
                string model = NewHouses[n++ % NewHouses.Length];
                Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(model, at, yaw + 90f * ctx.Rng.Next(4), w, StandIn.Box, Limestone));
            }
            foreach (var (a, b) in new[] { (-0.60f, 0.40f), (-0.58f, -0.50f), (-0.72f, 0.42f) })
            {
                var at = P(a, b);
                float h = ctx.Range(0.11f, 0.13f) * s;
                Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), h, StandIn.Tree, OliveLeaf, 0.5f));
            }

            // 7. The hot springs south of the walls: a low bathhouse with its pale dome rising out of it.
            var bath = P(0.22f, 0.56f);
            ctx.PaintGround("beach", bath, yaw, 0.16f * s, 0.16f * s, 0.3f, 0.8f);
            if (ctx.PlaceByWidth(Bathhouse, bath, yaw, 0.08f * s, StandIn.Box, Limestone))
                ctx.PlaceByHeight(BathDome, bath, yaw, 0.11f * s, StandIn.Tree, DomeWhite, footprintShare: 0.6f);

            // 8. Life: two townspeople on the square, on the side toward the camera.
            for (int k = 0; k < People.Length; k++)
            {
                float b = 165f + 40f * k;
                ctx.PlaceByHeight(People[k], ctx.At(b, 0.08f), b + 180f + ctx.Range(-30f, 30f), 0.07f * s, StandIn.Figure, null, 0.4f);
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
