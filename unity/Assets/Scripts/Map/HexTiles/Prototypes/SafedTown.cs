#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Safed, the hill town of the upper Galilee (1948), drawn Civ-style as a town hex. Designed for board hex 11,6:
    /// hills (the board's height for it is 485 m).
    /// - The citadel hill at the top of the town is kept as an open green summit at the centre, where a unit stands. The
    ///   town's highest point is the citadel hill above the old city (https://en.wikipedia.org/wiki/Safed, checked
    ///   2026-10-19). The tile draws no castle remains: only the green summit.
    /// - Stone houses step down the slopes in three terraced rings (kit_jerusalem 1 to 3, kit_stone_village_2), each turned to
    ///   face the summit, with a painted lane along each terrace. The outer ring runs only round the south half, toward the
    ///   camera, and only where it fits the hex.
    /// - The old quarter on the west slope has two domes in the middle ring: generic stone domes, naming no synagogue or
    ///   mosque.
    /// - Olive slopes to the east and north-west, on painted terraces. A small stand of pines to the north, toward Mount
    ///   Canaan (the mountain north of the old town: https://he.wikipedia.org/wiki/הר_כנען, checked 2026-10-19). Its planting
    ///   date is uncertain (sources give the 1940s to 1955), so the stand is kept small.
    /// - A donkey on the summit as the tile's life.
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound):
    /// - At most 22 houses (kit, up to 1,400 each) = 30,800.
    /// - 6 Aleppo pines and at most 6 olives x 1,600 = 19,200. The donkey 2,500.
    /// - The two domes (a Box drum and a Tree dome each, stand-ins): about 60.
    /// - Total about 52,600 of 60,000.
    ///
    /// The domes have no models (tiles:safed_town/...), so they always draw stand-ins.</summary>
    [HexPrototype("safed_town", Kind = "town", Region = "north")]
    public sealed class SafedTown : IHexPrototype
    {
        public string Title => "Safed, the hill town (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        // Measured 150,628 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 170000;

        // Explicit house ids: kit_jerusalem_4 and kit_stone_village are the mosque-like b17 mesh, kept out of the rings.
        static readonly string[] Houses = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3", "sandbox:kit_stone_village_2" };
        const string Pine = "sandbox:aleppo_pine_lite", Olive = "sandbox:olive_lite", Donkey = "proto:figure-donkey-lite-1948",
            Drum = "tiles:safed_town/dome_drum", Dome = "tiles:safed_town/dome";

        static readonly Color Limestone = new Color(0.84f, 0.79f, 0.68f), DomeStone = new Color(0.86f, 0.84f, 0.78f),
            PineLeaf = new Color(0.20f, 0.33f, 0.18f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        const float Slope = 40f;
        const int MaxHouses = 22, MaxDomes = 2, MaxOlives = 6;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var taken = new List<(Vector3 at, float r)>();
            bool Free(Vector3 at, float r)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                return true;
            }
            bool Put(Vector3 at, float r, System.Func<bool> place)
            {
                if (!Free(at, r) || !place()) return false;
                taken.Add((at, r));
                return true;
            }
            float turn = ctx.Range(0f, 30f);
            bool InSector(float b, float from, float to) => Mathf.Repeat(b - from, 360f) <= Mathf.Repeat(to - from, 360f);

            // 1. Ground: the green summit at the centre (kept open for a unit), the terrace lanes, and ploughed olive terraces
            //    in the olive sectors.
            ctx.PaintGround("grass", ctx.Centre, 0f, 0.42f * s, 0.42f * s, 0.45f, 0.9f);
            taken.Add((ctx.Centre, 0.21f * s));
            for (float b = 0f; b < 360f; b += 12f)
            {
                ctx.PaintStroke("dirt", ctx.At(b, 0.445f), ctx.At(b + 12f, 0.445f), 0.025f * s, 0.8f);
                if (!InSector(b, 320f, 70f)) ctx.PaintStroke("dirt", ctx.At(b, 0.625f), ctx.At(b + 12f, 0.625f), 0.025f * s, 0.8f);
            }
            foreach (var (b0, b1) in new[] { (60f, 100f), (296f, 330f) })
                foreach (float r in new[] { 0.70f, 0.78f })
                    for (float b = b0; b < b1; b += 10f)
                        ctx.PaintStroke("ploughed", ctx.At(b, r), ctx.At(b + 10f, r), 0.05f * s, 0.7f);

            // 2. The rings of houses, each house facing the summit. The middle ring gives two slots on the west slope to the
            //    old quarter's domes. The outer ring sits between the middle ring's slots, round the south half only.
            int houses = 0, domes = 0;
            for (int k = 0; k < 8; k++)
            {
                float b = turn + 22.5f + 45f * k + ctx.Range(-5f, 5f), w = ctx.Range(0.115f, 0.125f) * s;
                var at = ctx.At(b, 0.35f + ctx.Range(-0.01f, 0.01f));
                string model = Houses[k % Houses.Length];
                if (houses < MaxHouses && Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(model, at, b + 180f + ctx.Range(-8f, 8f), w, StandIn.Box, Limestone, Slope))) houses++;
            }
            const float Step2 = 360f / 11f;
            for (int k = 0; k < 11; k++)
            {
                float b = turn + 10f + Step2 * k, w = ctx.Range(0.10f, 0.11f) * s;
                var at = ctx.At(b + ctx.Range(-4f, 4f), 0.54f + ctx.Range(-0.01f, 0.01f));
                if (domes < MaxDomes && InSector(b, 200f, 270f))
                {
                    if (Put(at, 0.05f * s, () => ctx.PlaceByWidth(Drum, at, b, 0.06f * s, StandIn.Box, DomeStone, Slope)))
                    {
                        ctx.PlaceByHeight(Dome, at, b, 0.10f * s, StandIn.Tree, DomeStone, footprintShare: 0.6f, maxSlopeDeg: Slope);
                        domes++;
                        continue;
                    }
                }
                string model = Houses[(k + 1) % Houses.Length];
                if (houses < MaxHouses && Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(model, at, b + 180f + ctx.Range(-8f, 8f), w, StandIn.Box, Limestone, Slope))) houses++;
            }
            for (int k = 0; k < 11; k++)
            {
                float b = turn + 10f + Step2 * (k + 0.5f), w = ctx.Range(0.09f, 0.10f) * s;
                if (!InSector(b, 100f, 290f)) continue;
                var at = ctx.At(b, 0.71f);
                string model = Houses[(k + 2) % Houses.Length];
                if (houses < MaxHouses && Put(at, 0.6f * w * HexScale.PieceScale, () => ctx.PlaceByWidth(model, at, b + 180f + ctx.Range(-8f, 8f), w, StandIn.Box, Limestone, Slope))) houses++;
            }

            // 3. The small pine stand to the north, toward Mount Canaan, and the olives east and north-west, kept apart from the
            //    houses and from each other.
            foreach (var (b, r) in new[] { (335f, 0.74f), (350f, 0.82f), (5f, 0.74f), (20f, 0.82f), (35f, 0.72f), (50f, 0.78f) })
            {
                var at = ctx.At(b, r);
                float h = ctx.Range(0.14f, 0.16f) * s;
                Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Pine, at, ctx.Range(0f, 360f), h, StandIn.Pine, PineLeaf, 0.4f, Slope));
            }
            int olives = 0;
            foreach (var (b, r) in new[] { (66f, 0.74f), (82f, 0.70f), (96f, 0.74f), (300f, 0.74f), (315f, 0.70f), (326f, 0.80f), (74f, 0.62f), (306f, 0.62f) })
            {
                if (olives >= MaxOlives) break;
                var at = ctx.At(b, r);
                float h = ctx.Range(0.11f, 0.13f) * s;
                if (Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), h, StandIn.Tree, OliveLeaf, 0.5f, Slope))) olives++;
            }

            // 4. Life: a donkey grazing on the summit, on the side toward the camera.
            ctx.PlaceByHeight(Donkey, ctx.At(195f, 0.14f), ctx.Range(0f, 360f), 0.06f * s, StandIn.Figure, null, 0.5f, Slope);
        }

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
