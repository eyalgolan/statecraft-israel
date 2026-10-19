#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A moshava, a Jewish farming colony of the coastal plain (Rishon LeZion, Rehovot, 1948). One main street
    /// runs through the hex on a grid turned up to 20 degrees, with a cypress gateway at each end. Two rows of red-roofed
    /// houses face the street, each with its garden behind (citrus or a date palm). The water tower stands on a small
    /// plaza mid-street, with the first-batch moshava cluster across from it. A barn, a farmhouse and the stone wine
    /// cellar stand in the back row. Citrus groves on dark soil fill the two flanks. The street, gardens, yards and grove
    /// soil are painted into the ground (ctx.PaintGround / PaintStroke); only the paved plaza is a flat.
    ///
    /// Triangle estimate, at the maximum counts (FlatSurface refuses nothing), with measured per-model counts:
    /// 4 kit_moshava x 700 + 4 kit_moshava_2 x 1399 (houses) + moshava-town-v1 x 3555 + barn kit_moshava_3 x 1400 +
    /// farmhouse kit_moshava x 700 + winery kit_moshava_2 x 1399 = 15,450. 14 citrus x 1602 = 22,428. 4 cypress x 1596 =
    /// 6,384. 6 date palms x 284 = 1,704. Water tower (kit_kibbutz_4) as in the kibbutz tile. 1 flat x 12. Total about 46,000 of 60,000.
    ///
    /// The water tower is the kibbutz tile's b28 tower (sandbox:kit_kibbutz_4); where it is missing, the Palm stand-in
    /// draws instead, its silhouette (a thin shaft under a wide crown) the nearest to a tank on a column.</summary>
    [HexPrototype("moshava_village", Kind = "town")]
    public sealed class MoshavaVillage : IHexPrototype
    {
        public string Title => "Moshava farming colony (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 120000;   // the shipped b*-v2 buildings at full detail (Tal, 19 Oct 2026): measured 106k on the israel board and the test hex

        // Explicit kit ids, not "kit_moshava*": the wildcard would also pick kit_moshava_3, which is the barn.
        const string Farmhouse = "sandbox:kit_moshava", TwoStorey = "sandbox:kit_moshava_2", Barn = "sandbox:kit_moshava_3",
            Cluster = "proto:moshava-town-v1", WaterTower = "sandbox:kit_kibbutz_4",
            Citrus = "sandbox:citrus_lite", Cypress = "sandbox:cypress_lite", Palm = "sandbox:date_palm";

        static readonly Color RedRoof = new Color(0.72f, 0.36f, 0.26f), Stone = new Color(0.78f, 0.72f, 0.60f),
            Timber = new Color(0.52f, 0.38f, 0.26f), Concrete = new Color(0.82f, 0.80f, 0.74f),
            Plaza = new Color(0.84f, 0.78f, 0.64f), Leaf = new Color(0.16f, 0.34f, 0.14f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(-20f, 20f);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            // A point in the street's frame, in shares of S: x across the street, z along it.
            Vector3 L(float x, float z) => ctx.Centre + turn * new Vector3(x * s, 0f, z * s);

            // 1. The main street, a dirt stroke right through the hex (clipped to it).
            ctx.PaintStroke("dirt", L(0f, -0.95f), L(0f, 0.95f), 0.055f * s, 0.95f);

            // 2. Two rows of houses facing the street, five slots a side. The middle west slot is the water-tower plaza,
            //    the middle east slot the first-batch cluster. Every house has its garden behind it.
            float[] slots = { -0.48f, -0.24f, 0f, 0.24f, 0.48f };
            int house = 0;
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i < slots.Length; i++)
                {
                    float z = slots[i];
                    if (i == 2 && side < 0f)
                    {
                        ctx.PlaceFlat(L(-0.17f, 0f), yaw, 0.16f * s, 0.17f * s, Plaza);
                        ctx.PlaceByHeight(WaterTower, L(-0.19f, 0f), yaw, 0.17f * s, StandIn.Palm, Concrete, 0.35f);
                        ctx.PlaceByHeight(Palm, L(-0.11f, -0.065f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Palm, null, 0.3f);
                        ctx.PlaceByHeight(Palm, L(-0.11f, 0.065f), ctx.Range(0f, 360f), 0.12f * s, StandIn.Palm, null, 0.3f);
                        continue;
                    }
                    if (i == 2)
                    {
                        ctx.PlaceByWidth(Cluster, L(0.18f, 0f), yaw - 90f, 0.2f * s, StandIn.Box, RedRoof);
                        continue;
                    }
                    string model = house++ % 2 == 0 ? Farmhouse : TwoStorey;
                    ctx.PlaceByWidth(model, L(side * 0.155f, z), yaw + side * 90f, ctx.Range(0.13f, 0.15f) * s, StandIn.Box, RedRoof);
                    if (!Fits(ctx, L(side * 0.30f, z), yaw, 0.11f * s, 0.17f * s)) continue;
                    ctx.PaintGround("grass", L(side * 0.30f, z), yaw, 0.11f * s, 0.17f * s, 0.25f, 0.75f);
                    if ((i + (side > 0f ? 1 : 0)) % 2 == 0)
                        ctx.PlaceByHeight(Citrus, L(side * 0.30f, z), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, Leaf, 0.6f);
                    else
                        ctx.PlaceByHeight(Palm, L(side * 0.30f, z), ctx.Range(0f, 360f), 0.12f * s, StandIn.Palm, null, 0.3f);
                }

            // 3. The back row: the barn on its yard and a farmhouse to the west, the stone wine cellar on its yard to the east.
            if (Fits(ctx, L(-0.45f, 0.30f), yaw, 0.18f * s, 0.24f * s))
            {
                ctx.PaintGround("dirt", L(-0.45f, 0.30f), yaw, 0.18f * s, 0.24f * s, 0.25f, 0.8f);
                ctx.PlaceByWidth(Barn, L(-0.45f, 0.30f), yaw + 90f, 0.16f * s, StandIn.Box, Timber);
            }
            ctx.PlaceByWidth(Farmhouse, L(-0.45f, -0.30f), yaw + 90f, 0.14f * s, StandIn.Box, RedRoof);
            if (Fits(ctx, L(0.47f, -0.30f), yaw, 0.20f * s, 0.24f * s))
            {
                ctx.PaintGround("dirt", L(0.47f, -0.30f), yaw, 0.20f * s, 0.24f * s, 0.25f, 0.8f);
                ctx.PlaceByWidth(TwoStorey, L(0.47f, -0.30f), yaw - 90f, 0.19f * s, StandIn.Box, Stone);
            }

            // 4. Citrus groves on the flanks, in straight rows on painted tilled soil: 3 x 2 to the east, 2 x 2 to the west.
            ctx.PaintGround("ploughed", L(0.62f, 0.11f), yaw, 0.20f * s, 0.34f * s, 0.2f, 0.7f);
            foreach (float x in new[] { 0.56f, 0.68f })
                foreach (float z in new[] { -0.02f, 0.11f, 0.24f })
                    ctx.PlaceByHeight(Citrus, L(x, z), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, Leaf, 0.6f);
            ctx.PaintGround("ploughed", L(-0.64f, 0.025f), yaw, 0.20f * s, 0.21f * s, 0.2f, 0.7f);
            foreach (float x in new[] { -0.58f, -0.70f })
                foreach (float z in new[] { -0.04f, 0.09f })
                    ctx.PlaceByHeight(Citrus, L(x, z), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * s, StandIn.Tree, Leaf, 0.6f);

            // 5. A cypress gateway at each end of the street.
            foreach (float z in new[] { -0.70f, 0.70f })
                foreach (float x in new[] { -0.075f, 0.075f })
                    ctx.PlaceByHeight(Cypress, L(x, z), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * s, StandIn.Pine, CypressLeaf, 0.3f);
        }

        /// <summary>The test the flats these paints replaced made (every corner inside the hex, on dry ground, within the
        /// default rise), so the pieces gated on them place exactly as before.</summary>
        static bool Fits(HexTileContext ctx, Vector3 at, float yawDeg, float widthM, float depthM, float maxRiseM = 60f)
        {
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var c in new[] { Vector3.zero, new Vector3(widthM, 0f, depthM) * 0.5f, new Vector3(-widthM, 0f, depthM) * 0.5f,
                new Vector3(widthM, 0f, -depthM) * 0.5f, new Vector3(-widthM, 0f, -depthM) * 0.5f })
            {
                var q = at + rot * c;
                if (!ctx.Inside(q) || ctx.IsWater(q)) return false;
                float y = ctx.GroundY(q); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            return hi - lo <= maxRiseM;
        }
    }
}
