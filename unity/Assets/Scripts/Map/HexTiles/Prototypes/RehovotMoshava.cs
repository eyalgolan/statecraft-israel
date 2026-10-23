#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Rehovot in 1948: the moshava founded in 1890, deep in its citrus groves (the first grove was planted in
    /// 1904; https://en.wikipedia.org/wiki/Rehovot, checked 2026-10-19).
    /// - The moshava core: one main street through the hex, painted dirt, with a small plaza at the centre kept open for a
    ///   unit. Moshava houses (kit_moshava farmhouses and kit_moshava_2 two-storey houses) face it from both sides, three
    ///   a side north and south of the plaza, each with a garden behind.
    /// - Citrus groves all round, in straight rows of citrus_lite on tilled soil.
    /// - At the south-east edge, low-key, the Daniel Sieff Research Institute building: built in 1934 to Benjamin Chaikin's
    ///   design as the first building of the institute that became the Weizmann Institute, a two-storey block in a
    ///   classic, eclectic style with a symmetrical facade (https://www.weizmann.ac.il/ConstructionEngineering/en/node/94,
    ///   checked 2026-10-19). Drawn as a pale stone block in its garden, flanked symmetrically by two cypresses and two
    ///   date palms.
    /// - At the north-west edge, a citrus packing-house shed on its yard (generic).
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing):
    /// - At most 12 houses (kit_moshava, kit_moshava_2, up to 1,400 each) = 16,800.
    /// - At most 30 citrus (citrus_lite, 1,602 each) = 48,060.
    /// - The institute garden: 2 cypresses x 1,602 + 2 date palms x 284 = 3,772.
    /// - One townsperson (up to 4,000). The institute and the packing house: 2 Box stand-ins, about 24.
    /// - Total about 72,700 of 80,000.
    ///
    /// The institute and the packing house have no models (tiles:rehovot_moshava/...), so they draw stand-ins.</summary>
    [HexPrototype("rehovot_moshava", Kind = "town", Region = "centre")]
    public sealed class RehovotMoshava : IHexPrototype
    {
        public string Title => "Rehovot, moshava in its citrus groves (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        // Measured 126,852 triangles worst case with the buildings drawn as the shipped b*-v2 town models at full
        // level (about 6,000 each, HexModels.TownAliases); the budget is that plus 10%. The doc comment's estimate predates them.
        public int TriangleBudget => 140000;

        // Explicit ids: the wildcard kit_moshava* would also pick the barn.
        const string Farmhouse = "sandbox:kit_moshava", TwoStorey = "sandbox:kit_moshava_2", Citrus = "sandbox:citrus_lite",
            Cypress = "sandbox:cypress_lite", Palm = "sandbox:date_palm", Institute = "tiles:rehovot_moshava/sieff_institute",
            PackingHouse = "tiles:rehovot_moshava/packing_house", Townsman = "proto:figure-townsman-c02-1948-v2";

        static readonly Color RedRoof = new Color(0.72f, 0.36f, 0.26f), PaleStone = new Color(0.90f, 0.87f, 0.80f),
            Whitewash = new Color(0.86f, 0.83f, 0.76f), CitrusLeaf = new Color(0.16f, 0.34f, 0.14f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f);

        const int MaxHouses = 12, MaxCitrus = 30;
        /// <summary>The institute and the packing house, in the street frame (x across, z along; shares of S).</summary>
        const float InstX = 0.48f, InstZ = -0.36f, PackX = -0.48f, PackZ = 0.36f;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-15f, 15f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 L(float x, float z) => ctx.Centre + rot * new Vector3(x * s, 0f, z * s);

            // 1. Ground: tilled grove soil on both flanks and at the street ends, the street, the plaza.
            foreach (float x in new[] { -0.62f, 0.62f })
                ctx.PaintGround("ploughed", L(x, 0f), yaw, 0.40f * s, 1.7f * s, 0.2f, 0.65f);
            foreach (float z in new[] { -0.82f, 0.82f })
                ctx.PaintGround("ploughed", L(0f, z), yaw, 0.70f * s, 0.20f * s, 0.25f, 0.65f);
            ctx.PaintStroke("dirt", L(0f, -0.95f), L(0f, 0.95f), 0.055f * s, 0.95f);
            ctx.PaintGround("dirt", ctx.Centre, yaw + 45f, 0.22f * s, 0.22f * s, 0.3f, 0.8f);

            // 2. The houses: three a side north and south of the plaza, facing the street, a garden behind each.
            int houses = 0;
            foreach (float side in new[] { -1f, 1f })
                foreach (float z in new[] { -0.61f, -0.41f, -0.21f, 0.21f, 0.41f, 0.61f })
                {
                    float w = ctx.Range(0.12f, 0.135f);
                    if (houses >= MaxHouses || !ctx.PlaceByWidth(houses % 2 == 0 ? Farmhouse : TwoStorey, L(side * 0.15f, z), yaw + side * 90f, w * s, StandIn.Box, RedRoof)) continue;
                    houses++;
                    ctx.PaintGround("grass", L(side * 0.29f, z), yaw, 0.11f * s, 0.16f * s, 0.25f, 0.7f);
                }

            // 3. The Sieff institute in its garden at the south-east edge, its front to the street; two cypresses before it
            //    and a date palm at each side, set symmetrically.
            var inst = L(InstX, InstZ);
            ctx.PaintGround("grass", inst, yaw, 0.40f * s, 0.34f * s, 0.3f, 0.85f);
            ctx.PlaceByWidth(Institute, inst, yaw - 90f, 0.16f * s, StandIn.Box, PaleStone);
            foreach (float d in new[] { -0.09f, 0.09f })
                ctx.PlaceByHeight(Cypress, L(InstX - 0.17f, InstZ + d), 0f, 0.15f * s, StandIn.Pine, CypressLeaf, 0.3f);
            foreach (float d in new[] { -0.17f, 0.17f })
                ctx.PlaceByHeight(Palm, L(InstX, InstZ + d), 0f, 0.12f * s, StandIn.Palm, null, 0.3f);

            // 4. The packing house on its yard at the north-west edge.
            ctx.PaintGround("dirt", L(PackX, PackZ), yaw, 0.26f * s, 0.26f * s, 0.25f, 0.85f);
            ctx.PlaceByWidth(PackingHouse, L(PackX, PackZ), yaw + 90f, 0.15f * s, StandIn.Box, Whitewash);

            // 5. Citrus groves: rows 0.14 S apart beyond the gardens and past the street ends, off the institute garden and
            //    the packing yard. Two passes (alternate trees first), so the cap thins the groves evenly instead of
            //    leaving one side bare.
            int citrus = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = -6; i <= 6; i++)
                    for (int j = -6; j <= 6; j++)
                    {
                        if (((i + j) & 1) != pass) continue;
                        float x = 0.14f * i, z = 0.14f * j;
                        if (Mathf.Abs(x) < 0.37f && Mathf.Abs(z) < 0.74f) continue;            // the street, houses and gardens
                        if (Mathf.Abs(x) < 0.07f) continue;                                     // the street ends
                        if (Mathf.Abs(x - InstX) < 0.27f && Mathf.Abs(z - InstZ) < 0.26f) continue;
                        if (Mathf.Abs(x - PackX) < 0.20f && Mathf.Abs(z - PackZ) < 0.20f) continue;
                        if (citrus < MaxCitrus && ctx.PlaceByHeight(Citrus, L(x, z), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, CitrusLeaf, 0.5f))
                            citrus++;
                    }

            // 6. Life: a townsman at the plaza's edge.
            ctx.PlaceByHeight(Townsman, L(0.05f, -0.08f), yaw + 200f, 0.075f * s, StandIn.Figure, null, 0.4f);
        }
    }
}
