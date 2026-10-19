#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Nazareth in its bowl of hills (1948), drawn Civ-style as a town hex. Designed for board hex 7,10: hills.
    /// - A small market square at the centre, kept open for a unit, with a donkey on it as the tile's life.
    /// - Stone houses in three rings round it (kit_jerusalem 1 to 3, kit_stone_village_2), each house facing the square. The
    ///   houses grow a little wider ring by ring, so the town reads as rising up the sides of a bowl. The outer ring runs only
    ///   round the south half, where it fits the hex.
    /// - The one named landmark, low-key: the church at the Annunciation site as it stood in 1948, a modest stone church in
    ///   the inner ring on the south side, with a generic convent block beside it. That church was built in 1730,
    ///   enlarged in 1877 and demolished in 1954 or 1955; the present basilica was built from 1960 to 1969
    ///   (https://en.wikipedia.org/wiki/Basilica_of_the_Annunciation, checked 2026-10-19). No dome and no belfry: no source
    ///   found here describes its bell tower.
    /// - Generic red-roofed silhouettes, naming nothing: a church with a slim belfry in the middle ring and a monastery on the
    ///   ridge to the north. Their Box stand-ins are tinted terracotta, so from the camera they read as red roofs.
    /// - Olive terraces on the north slopes, painted as ploughed strips, with olives on them.
    ///
    /// Triangle estimate, at the maximum attempted counts (an upper bound):
    /// - At most 22 houses (kit, up to 1,400 each) = 30,800.
    /// - At most 8 olives x 1,600 = 12,800. The donkey 2,500.
    /// - The churches, convent, monastery and belfry as stand-ins: about 70.
    /// - Total about 46,200 of 55,000.
    ///
    /// The churches, convent, monastery and belfry have no models (tiles:nazareth_town/...), so they always draw stand-ins.</summary>
    [HexPrototype("nazareth_town", Kind = "town", Region = "north")]
    public sealed class NazarethTown : IHexPrototype
    {
        public string Title => "Nazareth, the town in its bowl of hills (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 55000;

        // Explicit house ids: kit_jerusalem_4 and kit_stone_village are the mosque-like b17 mesh, kept out of the rings.
        static readonly string[] Houses = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3", "sandbox:kit_stone_village_2" };
        const string Annunciation = "tiles:nazareth_town/annunciation_church_1948", Convent = "tiles:nazareth_town/convent_block",
            Church = "tiles:nazareth_town/church", Belfry = "tiles:nazareth_town/belfry", Monastery = "tiles:nazareth_town/monastery",
            Olive = "sandbox:olive_lite", Donkey = "proto:figure-donkey-lite-1948";

        static readonly Color Limestone = new Color(0.84f, 0.79f, 0.68f), ChurchStone = new Color(0.86f, 0.82f, 0.72f),
            Terracotta = new Color(0.72f, 0.38f, 0.28f), OliveLeaf = new Color(0.42f, 0.50f, 0.34f);

        const float Slope = 40f;
        const int MaxHouses = 22, MaxOlives = 8;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            var taken = new List<(Vector3 at, float r)>();
            bool Put(Vector3 at, float r, System.Func<bool> place)
            {
                foreach (var t in taken) if (Planar(at - t.at) < r + t.r) return false;
                if (!place()) return false;
                taken.Add((at, r));
                return true;
            }
            bool InSector(float b, float from, float to) => Mathf.Repeat(b - from, 360f) <= Mathf.Repeat(to - from, 360f);
            float Clear(float w) => 0.6f * w * HexScale.PieceScale;

            // 1. Ground: the town's beaten earth, the market square (kept open for a unit), the lanes between the rings, and
            //    the ploughed olive terraces on the north slopes.
            ctx.PaintGround("dirt", ctx.Centre, 0f, 1.1f * s, 1.1f * s, 0.45f, 0.4f);
            ctx.PaintGround("beach", ctx.Centre, 0f, 0.30f * s, 0.30f * s, 0.3f, 0.8f);
            taken.Add((ctx.Centre, 0.17f * s));
            for (float b = 0f; b < 360f; b += 12f)
            {
                ctx.PaintStroke("dirt", ctx.At(b, 0.45f), ctx.At(b + 12f, 0.45f), 0.025f * s, 0.8f);
                if (InSector(b, 96f, 288f)) ctx.PaintStroke("dirt", ctx.At(b, 0.625f), ctx.At(b + 12f, 0.625f), 0.025f * s, 0.8f);
            }
            foreach (var (b0, b1) in new[] { (290f, 336f), (14f, 72f) })
                foreach (float r in new[] { 0.68f, 0.77f })
                    for (float b = b0; b < b1; b += 10f)
                        ctx.PaintStroke("ploughed", ctx.At(b, r), ctx.At(b + 10f, r), 0.05f * s, 0.7f);

            // 2. The landmarks first, so the house rings make room for them:
            //    - the 1948 church at the Annunciation site in the inner ring on the south side, its front turned to the
            //      camera, a small painted forecourt before it, and a generic convent block beside it;
            //    - a generic church with a slim belfry in the middle ring to the east;
            //    - a generic monastery on the ridge to the north.
            var church = ctx.At(202f, 0.37f);
            ctx.PaintGround("beach", ctx.At(202f, 0.48f), 202f, 0.12f * s, 0.08f * s, 0.3f, 0.8f);
            Put(church, Clear(0.13f * s), () => ctx.PlaceByWidth(Annunciation, church, 202f, 0.13f * s, StandIn.Box, ChurchStone, Slope));
            var convent = ctx.At(232f, 0.40f);
            Put(convent, Clear(0.10f * s), () => ctx.PlaceByWidth(Convent, convent, 232f, 0.10f * s, StandIn.Box, ChurchStone, Slope));
            var church2 = ctx.At(70f, 0.54f);
            if (Put(church2, Clear(0.11f * s), () => ctx.PlaceByWidth(Church, church2, 250f, 0.11f * s, StandIn.Box, Terracotta, Slope)))
                ctx.PlaceByHeight(Belfry, church2 + Dir(160f) * (0.075f * s), 0f, 0.15f * s, StandIn.Pine, ChurchStone, 0.2f, Slope);
            var monastery = ctx.At(352f, 0.70f);
            Put(monastery, Clear(0.14f * s), () => ctx.PlaceByWidth(Monastery, monastery, 172f, 0.14f * s, StandIn.Box, Terracotta, Slope));

            // 3. The house rings, wider ring by ring, each house facing the square.
            int houses = 0;
            void House(float b, float r, float w, int k)
            {
                if (houses >= MaxHouses) return;
                var at = ctx.At(b, r);
                float yaw = b + 180f + ctx.Range(-8f, 8f);
                string model = Houses[k % Houses.Length];
                if (Put(at, Clear(w), () => ctx.PlaceByWidth(model, at, yaw, w, StandIn.Box, Limestone, Slope))) houses++;
            }
            for (int k = 0; k < 8; k++)
                House(22.5f + 45f * k + ctx.Range(-4f, 4f), 0.35f + ctx.Range(-0.01f, 0.01f), ctx.Range(0.105f, 0.11f) * s, k);
            const float Step2 = 360f / 11f;
            for (int k = 0; k < 11; k++)
                House(5f + Step2 * k + ctx.Range(-4f, 4f), 0.54f + ctx.Range(-0.01f, 0.01f), ctx.Range(0.11f, 0.115f) * s, k + 1);
            for (int k = 0; k < 11; k++)
            {
                float b = 5f + Step2 * (k + 0.5f);
                if (InSector(b, 100f, 285f)) House(b, 0.71f, ctx.Range(0.095f, 0.10f) * s, k + 2);
            }

            // 4. Olives on the north terraces, kept apart from the houses and from each other.
            int olives = 0;
            foreach (var (b, r) in new[] { (296f, 0.72f), (312f, 0.77f), (328f, 0.70f), (20f, 0.72f), (36f, 0.78f), (52f, 0.70f), (66f, 0.76f), (305f, 0.64f) })
            {
                if (olives >= MaxOlives) break;
                var at = ctx.At(b, r);
                float h = ctx.Range(0.11f, 0.13f) * s;
                if (Put(at, 0.3f * h * HexScale.PieceScale, () => ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), h, StandIn.Tree, OliveLeaf, 0.5f, Slope))) olives++;
            }

            // 5. Life: a donkey on the market square, on the side toward the camera.
            ctx.PlaceByHeight(Donkey, ctx.At(165f, 0.12f), ctx.Range(0f, 360f), 0.06f * s, StandIn.Figure, null, 0.5f, Slope);
        }

        static Vector3 Dir(float bearingDeg)
        {
            float a = bearingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        static float Planar(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
