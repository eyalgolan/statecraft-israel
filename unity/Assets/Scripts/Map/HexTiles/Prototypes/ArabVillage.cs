#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>An Arab stone village on a hill (the Judean and Samarian foothills, 1948). Flat-roofed limestone houses
    /// crowd the hilltop in two tight rings round a small paved square, which stays clear for a unit. The village mosque
    /// (the larger stone courtyard building) faces the square from 0.35 S out, in place of an inner-ring house, and a slim
    /// minaret stands at the square's corner. Below the houses, two rings of dry-stone terrace walls hold a band of tilled
    /// terra-rossa soil planted with olives and a few carobs. Three footpaths run down through gaps in the walls, with
    /// limestone boulders beside them, and prickly-pear hedges and carobs fill the far corners. The soil band and the paths
    /// are painted into the ground (ctx.PaintGround / PaintStroke); the square and the walls stay flats.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing), with measured per-model counts:
    /// 14 houses (kit_jerusalem, _2, _3, kit_stone_village_2) x 700 = 9,800. stone-village-v1 x 8,642. Mosque
    /// kit_stone_village (b17) x 1,400. At most 14 olives x 1602 = 22,428. At most 7 carobs x 648 = 4,536. At most 4
    /// limestone x 700 = 2,800. Minaret and 6 prickly pears as stand-ins, about 320. About 45 flats x 12 = 540. Total about
    /// 50,400 of 60,000 (two house slots now go to the mosque, so a little less).
    ///
    /// The minaret and the prickly pear have no models (tiles:arab_village/...), so they draw stand-ins: Pine, a slim spire,
    /// for the minaret and a low Tree for the prickly pear.</summary>
    [HexPrototype("arab_village", Kind = "village")]
    public sealed class ArabVillage : IHexPrototype
    {
        public string Title => "Arab stone village on a hill (1948)";
        public HexTerrain Terrain => HexTerrain.Hills;
        public string Ground => "hills";
        public int TriangleBudget => 125000;   // the shipped b*-v2 buildings at full detail (Tal, 19 Oct 2026): measured 113k on the israel board and the test hex

        // Explicit house ids, not wildcards: kit_jerusalem* and kit_stone_village* both include b17, kept for the mosque.
        static readonly string[] Houses = { "sandbox:kit_jerusalem", "sandbox:kit_jerusalem_2", "sandbox:kit_jerusalem_3", "sandbox:kit_stone_village_2" };
        const string Mosque = "sandbox:kit_stone_village", Cluster = "proto:stone-village-v1", Minaret = "tiles:arab_village/minaret",
            PricklyPear = "tiles:arab_village/prickly_pear", Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite", Rock = "sandbox:limestone*";

        static readonly Color Limestone = new Color(0.82f, 0.76f, 0.64f), Paving = new Color(0.74f, 0.68f, 0.56f),
            Wall = new Color(0.80f, 0.75f, 0.65f),
            OliveLeaf = new Color(0.42f, 0.50f, 0.34f), CarobLeaf = new Color(0.20f, 0.34f, 0.16f), Cactus = new Color(0.48f, 0.58f, 0.40f),
            RockGrey = new Color(0.72f, 0.69f, 0.62f);

        const float HouseSlope = 38f, WallRise = 140f;
        // The inner-ring slot the mosque takes, and the outer-ring slot beside it that it would overlap.
        const int MosqueInner = 1, MosqueOuter = 2;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, vy = ctx.Range(0f, 360f);
            float[] paths = { vy + 15f, vy + 135f, vy + 255f };
            bool NearPath(float bearing, float clearDeg) { foreach (var p in paths) if (Mathf.Abs(Mathf.DeltaAngle(bearing, p)) < clearDeg) return true; return false; }

            // 1. The square on the hilltop (kept clear for a unit), the mosque facing it from 0.35 S out, on the bearing of
            //    inner-ring slot MosqueInner, and the minaret at the square's corner.
            float mosqueBearing = vy + 30f + 60f * MosqueInner;
            ctx.PlaceFlat(ctx.Centre, vy, 0.30f * s, 0.30f * s, Paving, 150f);
            ctx.PlaceByWidth(Mosque, ctx.At(mosqueBearing, 0.35f), mosqueBearing + 180f, 0.2f * s, StandIn.Box, Limestone, maxSlopeDeg: HouseSlope);
            ctx.PlaceByHeight(Minaret, ctx.At(vy + 45f, 0.165f), vy, 0.2f * s, StandIn.Pine, Limestone, 0.2f, maxSlopeDeg: HouseSlope);

            // 2. Houses in two tight rings, each turned to face the square, with a little jitter so the cluster is organic.
            //    One outer slot is the first-batch stone-village cluster, set a little further out; the mosque takes one
            //    inner slot and the outer slot beside it (their draws are still made, so the sequence holds).
            int h = 0;
            for (int k = 0; k < 6; k++)
            {
                float b = vy + 30f + 60f * k + ctx.Range(-8f, 8f);
                var at = ctx.At(b, 0.27f + ctx.Range(-0.015f, 0.015f));
                float yaw = b + 180f + ctx.Range(-12f, 12f), width = ctx.Range(0.12f, 0.13f) * s;
                if (k == MosqueInner) continue;
                ctx.PlaceByWidth(Houses[h++ % Houses.Length], at, yaw, width, StandIn.Box, Limestone, maxSlopeDeg: HouseSlope);
            }
            for (int k = 0; k < 9; k++)
            {
                float b = vy + 40f * k + ctx.Range(-5f, 5f);
                if (k == 4)
                {
                    ctx.PlaceByWidth(Cluster, ctx.At(b, 0.48f), b + 180f, 0.18f * s, StandIn.Box, Limestone, maxSlopeDeg: HouseSlope);
                    continue;
                }
                var at = ctx.At(b, 0.44f + ctx.Range(-0.015f, 0.015f));
                float yaw = b + 180f + ctx.Range(-12f, 12f), width = ctx.Range(0.12f, 0.13f) * s;
                if (k == MosqueOuter) continue;
                ctx.PlaceByWidth(Houses[h++ % Houses.Length], at, yaw, width, StandIn.Box, Limestone, maxSlopeDeg: HouseSlope);
            }
            float clusterBearing = vy + 160f;

            // 3. Terraces: two rings of thin dry-stone walls along the contour, a band of tilled red soil painted between
            //    them, broken by the three footpaths (and kept clear of the cluster).
            for (int k = 0; k < 22; k++)
            {
                float bb = vy + k * (360f / 22f);
                if (NearPath(bb, 10f) || Mathf.Abs(Mathf.DeltaAngle(bb, clusterBearing)) < 22f) continue;
                ctx.PaintGround("ploughed", ctx.At(bb, 0.60f), bb + 90f, 0.16f * s, 0.03f * s, 0.4f, 0.9f);   // terrace wall line, painted
                ctx.PaintGround("ploughed", ctx.At(bb, 0.67f), bb + 90f, 0.17f * s, 0.11f * s, 0.15f, 0.7f);
            }
            for (int k = 0; k < 28; k++)
            {
                float bb = vy + k * (360f / 28f);
                if (NearPath(bb, 9f)) continue;
                ctx.PaintGround("ploughed", ctx.At(bb, 0.74f), bb + 90f, 0.16f * s, 0.03f * s, 0.4f, 0.9f);
            }

            // 4. The footpaths down the slope, painted as dirt, and limestone boulders where they cut through the upper wall.
            int rocks = 0;
            foreach (var p in paths)
            {
                ctx.PaintStroke("dirt", ctx.At(p, 0.52f), ctx.At(p, 0.86f), 0.045f * s, 0.85f);
                if (rocks < 4 && ctx.PlaceByWidth(Rock, ctx.At(p + 11f, 0.60f), ctx.Range(0f, 360f), ctx.Range(0.05f, 0.07f) * s, StandIn.Rock, RockGrey, maxSlopeDeg: 45f)) rocks++;
            }
            if (rocks < 4) ctx.PlaceByWidth(Rock, ctx.At(paths[1] - 11f, 0.74f), ctx.Range(0f, 360f), 0.06f * s, StandIn.Rock, RockGrey, maxSlopeDeg: 45f);

            // 5. Olives on the soil band, every fifth tree a carob; capped by what is actually placed.
            int olives = 0, carobs = 0, slot = 0;
            for (int k = 0; k < 20; k++)
            {
                float bb = vy + 9f + 18f * k;
                if (NearPath(bb, 10f) || Mathf.Abs(Mathf.DeltaAngle(bb, clusterBearing)) < 22f) continue;
                var at = ctx.At(bb, 0.67f + ctx.Range(-0.01f, 0.01f));
                if (slot++ % 5 == 4)
                {
                    if (carobs < 4 && ctx.PlaceByHeight(Carob, at, ctx.Range(0f, 360f), ctx.Range(0.13f, 0.15f) * s, StandIn.Tree, CarobLeaf, 0.5f, 40f)) carobs++;
                }
                else if (olives < 14 && ctx.PlaceByHeight(Olive, at, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.13f) * s, StandIn.Tree, OliveLeaf, 0.5f, 40f)) olives++;
            }

            // 6. The far corners, past the terraces: carobs in three, prickly-pear hedges (two plants) in the other three.
            for (int c = 0; c < 6; c++)
            {
                float b = 60f * c;
                if (c % 2 == 0)
                    ctx.PlaceByHeight(Carob, ctx.At(b, 0.83f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Tree, CarobLeaf, 0.5f, 40f);
                else
                    foreach (float side in new[] { -4f, 4f })
                        ctx.PlaceByHeight(PricklyPear, ctx.At(b + side, 0.83f), ctx.Range(0f, 360f), 0.06f * s, StandIn.Tree, Cactus, 0.9f, 40f);
            }
        }
    }
}
