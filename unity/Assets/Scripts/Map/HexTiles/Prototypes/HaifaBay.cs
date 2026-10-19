#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>The Haifa Bay belt (1948): industry along the bay, workers' suburbs and the railway, as Haifa's edge hex.
    /// Designed for board hex 6,8: flat, with Haifa Bay to the north-west. On the board this hex carries Kiryat Ata, which
    /// lay in this belt; the tile names no place.
    /// - The tile finds the bay: the bearing on which water comes nearest. With no water, north-west. Everything is laid in a
    ///   frame turned to it: v toward the bay, u along the shore.
    /// - Between the railway and the bay stands the oil refinery: six round tanks in two rows, a tall stack, a process block
    ///   and a factory hall (kit_port), on painted works ground. The Haifa refinery was begun in 1938 by Consolidated
    ///   Refineries at the end of the Kirkuk pipeline, and stood by 1948 (https://en.wikipedia.org/wiki/Oil_Refineries_Ltd,
    ///   checked 2026-10-19).
    /// - The railway runs along the bay: a ballast stroke across the hex with a thin rail flat on it, and a windbreak of
    ///   shade trees on its inland side (tabor oaks stand in for eucalyptus).
    /// - Inland, the workers' suburbs (the Krayot): low rows of small houses (kit_kibbutz, the low quarters) with a painted
    ///   garden behind each. Kiryat Haim was founded in 1933, Kiryat Motzkin and Kiryat Bialik in 1934, Kiryat Ata in 1925
    ///   (https://en.wikipedia.org/wiki/Krayot, checked 2026-10-19).
    /// - Fields at the inland edge, painted in wheat, green crop and ploughing.
    /// - The unit spot stays clear of tall pieces (a context tile); only the railway's flats run near it.
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound):
    /// - 10 suburb houses (sandbox:kit_kibbutz, about 700; counted at 1,400) = 14,000.
    /// - Factory hall kit_port 1,400. 5 tabor oaks x 650 = 3,250.
    /// - Tanks, stack and process block as stand-ins, about 200. 3 rail flats: 36.
    /// - Total about 18,900 of 25,000.
    ///
    /// The tanks, stack and process block have no models (tiles:haifa_bay/...), so they always draw stand-ins: Rock (a squat
    /// round shape) for a tank, Pine (a slim spire) for the stack.</summary>
    [HexPrototype("haifa_bay", Kind = "town", Region = "north")]
    public sealed class HaifaBay : IHexPrototype
    {
        public string Title => "Haifa Bay, refinery and workers' suburbs (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 25000;

        const string House = "sandbox:kit_kibbutz", Hall = "sandbox:kit_port", ShadeTree = "sandbox:tabor_oak_lite",
            Tank = "tiles:haifa_bay/tank", Stack = "tiles:haifa_bay/stack", Process = "tiles:haifa_bay/process_block";

        static readonly Color TankWhite = new Color(0.86f, 0.86f, 0.82f), StackBrick = new Color(0.40f, 0.33f, 0.29f),
            Works = new Color(0.62f, 0.60f, 0.56f), Plaster = new Color(0.90f, 0.87f, 0.80f), Rail = new Color(0.30f, 0.27f, 0.24f),
            Leaf = new Color(0.22f, 0.36f, 0.18f);

        const int MaxHouses = 10;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                return true;
            }
            // Tall pieces stay out of the unit spot and clear of every piece already placed (clearance counts PieceScale).
            bool Put(Vector3 at, float r, System.Func<bool> place)
            {
                if (ctx.InUnitSpot(at, r) || !Free(at, r) || !place()) return false;
                taken.Add((at, r));
                return true;
            }

            // 1. The bay: the bearing on which water comes nearest the centre; with none, north-west.
            float bay = 315f, near = 1f;
            for (int i = 0; i < 24; i++)
            {
                float b = 15f * i;
                for (float r = 0.25f; r < near && r <= 0.9f; r += 0.03f)
                    if (ctx.IsWater(ctx.At(b, r))) { bay = b; near = r; break; }
            }
            Vector3 toBay = Dir(bay), alongShore = Dir(bay + 90f);
            Vector3 P(float u, float v) => ctx.Centre + toBay * (v * s) + alongShore * (u * s);

            // 2. Fields at the inland edge: large parcels on the frame's grid, clear of the suburbs.
            string[] crops = { "wheat", "green_crop", "ploughed", "wheat" };
            foreach (var (u, v) in new[] { (0.28f, -0.30f), (0.50f, -0.24f), (0.30f, -0.56f), (0.52f, -0.50f), (0.06f, -0.74f), (-0.24f, -0.74f) })
                ctx.PaintGround(crops[ctx.Rng.Next(crops.Length)], P(u, v), bay, 0.20f * s, 0.22f * s, 0.1f, 0.9f);

            // 3. The railway along the bay, v = 0.18: a ballast stroke right across the hex, three rail flats with small gaps
            //    between them (so no two flats share a plane), and a windbreak on the inland side beyond the unit spot.
            ctx.PaintStroke("dirt", P(-1.0f, 0.18f), P(1.0f, 0.18f), 0.04f * s, 0.9f);
            foreach (var (u0, u1) in new[] { (-0.64f, -0.23f), (-0.21f, 0.20f), (0.22f, 0.63f) })
                ctx.PlaceFlat(P((u0 + u1) * 0.5f, 0.18f), bay + 90f, 0.014f * s, (u1 - u0) * s, Rail, 150f);
            foreach (float u in new[] { -0.62f, -0.46f, 0.40f, 0.56f, 0.70f })
            {
                var at = P(u, 0.08f);
                float h = ctx.Range(0.13f, 0.15f) * s;
                Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(ShadeTree, at, ctx.Range(0f, 360f), h, StandIn.Tree, Leaf, 0.5f));
            }

            // 4. The refinery between the railway and the bay, on painted works ground: six tanks in two rows, the stack, the
            //    process block beside it and the factory hall. A tank that would stand in the water is refused, which is fine.
            ctx.PaintGround("dirt", P(-0.06f, 0.48f), bay, 0.90f * s, 0.36f * s, 0.2f, 0.85f);
            foreach (float v in new[] { 0.38f, 0.54f })
                foreach (float u in new[] { -0.46f, -0.30f, -0.14f })
                {
                    var at = P(u + ctx.Range(-0.005f, 0.005f), v);
                    float w = ctx.Range(0.085f, 0.095f) * s;
                    Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(Tank, at, 0f, w, StandIn.Rock, TankWhite));
                }
            var stack = P(0.10f, 0.44f);
            Put(stack, 0.03f * s, () => ctx.PlaceByHeight(Stack, stack, 0f, 0.22f * s, StandIn.Pine, StackBrick, 0.2f));
            var process = P(0.12f, 0.58f);
            Put(process, 0.6f * 0.08f * s * HexScale.PieceScale, () => ctx.PlaceByWidth(Process, process, bay, 0.08f * s, StandIn.Box, Works));
            var hall = P(0.34f, 0.46f);
            Put(hall, 0.6f * 0.16f * s * HexScale.PieceScale, () => ctx.PlaceByWidth(Hall, hall, bay + 180f, 0.16f * s, StandIn.Box, Works));

            // 5. The workers' suburbs inland: two low rows of four houses on the frame's grid and two more at the row ends,
            //    each with a garden painted behind it, a painted street in front of the first row and behind the second. Houses
            //    are 0.18 S apart, wider than their scaled clearance.
            ctx.PaintStroke("dirt", P(-0.66f, -0.30f), P(0.14f, -0.30f), 0.025f * s, 0.8f);
            ctx.PaintStroke("dirt", P(-0.66f, -0.68f), P(0.14f, -0.68f), 0.025f * s, 0.8f);
            int houses = 0;
            foreach (var (u, v) in new[]
                     {
                         (-0.48f, -0.40f), (-0.30f, -0.40f), (-0.12f, -0.40f), (0.06f, -0.40f),
                         (-0.48f, -0.58f), (-0.30f, -0.58f), (-0.12f, -0.58f), (0.06f, -0.58f),
                         (-0.36f, -0.20f), (-0.56f, -0.20f),
                     })
            {
                if (houses >= MaxHouses) break;
                var at = P(u, v);
                float w = ctx.Range(0.095f, 0.105f) * s;
                if (!Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(House, at, bay + 180f, w, StandIn.Box, Plaster))) continue;
                ctx.PaintGround("grass", P(u, v - 0.075f), bay, 0.12f * s, 0.05f * s, 0.3f, 0.8f);
                houses++;
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
