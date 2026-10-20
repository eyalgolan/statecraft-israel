#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>A kibbutz of the 1940s as a Civ-style settlement tile (Degania, Ein Harod). A compact fenced compound
    /// stands in the north of the hex, toward one flat side (bearing 30 or 330). The first-batch compound cluster is its core.
    /// The dining hall (the landmark) faces the hex centre on the lawn in front of it. Two rows of low living quarters stand
    /// to the west. The farmyard to the east holds two cowsheds, a hay barn and the water tower. A painted ring path runs
    /// inside the perimeter fence. Large painted fields (wheat, green fodder, fresh ploughing) on one grid fill the rest of
    /// the hex. The access road runs from the gate through the hex centre, where a unit stands, lined with shade trees.
    /// Three rectangular fishponds lie on dykes to one side. An L of cypress windbreaks shelters the other side.
    ///
    /// The compound starts 0.28 S out and its buildings 0.30 S out (centre 0.56 S), so no tall piece enters the unit spot.
    /// A core 0.35 S out would cross into it: a 0.31 S footprint reaches 0.16 S toward the centre.
    ///
    /// Triangle estimate at the maximum counts (FlatSurface refuses nothing), with measured per-model counts. The compound
    /// cluster has 12,432 polygon indices in its FBX: about 4,144 triangles, or about 6,200 if it holds quads. Dining hall kit_kibbutz_2 (b07) 1,400. Six
    /// quarters kit_kibbutz (b09a) x 700 = 4,200. Three barns kit_kibbutz_3 (b24) x 1,400 = 4,200. Water tower
    /// kit_kibbutz_4 (b28) 1,400. Buildings 15,344. 13 cypress x 1,596 = 20,748. 4 tabor oaks x 648 = 2,592. 3 pond flats
    /// x 12 = 36. Total about 38,700 (40,800 with quads) of 60,000.</summary>
    [HexPrototype("kibbutz")]
    public sealed class Kibbutz : IHexPrototype
    {
        public string Title => "Kibbutz compound and its fields (1940s)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "plains";
        public int TriangleBudget => 60000;

        // Explicit kit ids, not "kit_kibbutz*": the wildcard would mix the dining hall, barn and water tower into the rows.
        const string Core = "proto:kibbutz-compound-v2", Quarters = "sandbox:kit_kibbutz", DiningHall = "sandbox:kit_kibbutz_2",
            Barn = "sandbox:kit_kibbutz_3", WaterTower = "sandbox:kit_kibbutz_4", Cypress = "sandbox:cypress_lite",
            ShadeTree = "sandbox:tabor_oak_lite";

        static readonly Color RedRoof = new Color(0.72f, 0.36f, 0.26f), Plaster = new Color(0.86f, 0.82f, 0.72f),
            Timber = new Color(0.52f, 0.38f, 0.26f), Concrete = new Color(0.82f, 0.80f, 0.74f),
            PondWater = new Color(0.27f, 0.45f, 0.53f), Leaf = new Color(0.20f, 0.36f, 0.16f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f);

        // The perimeter in the compound frame (u across, v outward from the hex centre, shares of S), chamfered at the
        // corners so the ring stays inside the inset hexagon.
        static readonly Vector2[] Perimeter =
        {
            new Vector2(-0.42f, 0.205f), new Vector2(0.42f, 0.205f), new Vector2(0.49f, 0.14f), new Vector2(0.49f, -0.20f),
            new Vector2(0.36f, -0.285f), new Vector2(-0.36f, -0.285f), new Vector2(-0.49f, -0.20f), new Vector2(-0.49f, 0.14f),
        };

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            // The compound faces a flat side in the north half, so it stands behind the unit as the camera sees it.
            float bearing = (ctx.Rng.Next(2) == 0 ? -30f : 30f) + ctx.Range(-3f, 3f);
            float pondSide = ctx.Rng.Next(2) == 0 ? -1f : 1f;
            var turn = Quaternion.Euler(0f, bearing, 0f);
            // The field frame (x across, z toward the compound, shares of S, about the hex centre) and the compound frame.
            Vector3 F(float x, float z) => ctx.Centre + turn * new Vector3(x * s, 0f, z * s);
            const float Out = 0.56f;
            Vector3 L(float u, float v) => F(u, Out + v);

            // 1. Fields: a 7 x 7 grid of large parcels painted as crops, clipped to the hex, none under the compound.
            string[] crops = { "wheat", "wheat", "green_crop", "ploughed", "wheat", "green_crop" };
            const float cell = 0.34f;
            for (int i = -3; i <= 3; i++)
                for (int j = -3; j <= 3; j++)
                {
                    float x = i * cell, z = j * cell;
                    var at = F(x, z);
                    if (!ctx.Inside(at, -0.2f * s)) continue;          // wholly outside the hex
                    if (Mathf.Abs(x) < 0.54f && z > 0.2f) continue;     // the compound
                    string crop = crops[ctx.Rng.Next(crops.Length)];
                    bool across = ctx.Rng.Next(2) == 0;
                    if (ctx.Rng.Next(8) == 0) continue;                 // pasture
                    ctx.PaintGround(crop, at, bearing + (across ? 90f : 0f), (cell - 0.03f) * s, (cell - 0.03f) * s, 0.08f, 0.95f);
                }

            // 2. The compound ground: a lawn over the fenced area, the farmyard in dirt, then the ring path inside the fence.
            ctx.PaintGround("grass", L(0f, -0.04f), bearing, 0.98f * s, 0.49f * s, 0.15f, 0.8f);
            ctx.PaintGround("dirt", L(0.31f, -0.07f), bearing, 0.30f * s, 0.36f * s, 0.25f, 0.8f);
            for (int k = 0; k < Perimeter.Length; k++)
            {
                Vector2 a = Perimeter[k], b = Perimeter[(k + 1) % Perimeter.Length];
                ctx.PaintStroke("dirt", L(a.x, a.y), L(b.x, b.y), 0.022f * s, 0.9f);
            }
            // The access road from the gate through the hex centre (the unit stands on it) to the far edge.
            ctx.PaintStroke("dirt", L(0f, -0.285f), ctx.At(bearing + 180f, 0.95f), 0.035f * s, 0.95f);

            // 3. Fishponds: three rectangles in a row on painted dykes, on one side of the road. Water-blue flats, since a
            //    pond is a man-made surface, not ground.
            float px = pondSide * 0.40f;
            ctx.PaintGround("dirt", F(px, -0.475f), bearing, 0.19f * s, 0.42f * s, 0.15f, 0.9f);
            foreach (float z in new[] { -0.34f, -0.475f, -0.61f })
                if (!ctx.InUnitSpot(F(px, z), 0.09f * s))
                    ctx.PlaceFlat(F(px, z), bearing, 0.13f * s, 0.11f * s, PondWater, 80f);

            // 4. The compound. The core cluster faces the hex centre, with the dining hall in front of it on the lawn.
            Place(ctx, L(0f, 0.03f), 0.22f * s, p => ctx.PlaceByWidth(Core, p, bearing + 180f, 0.31f * s, StandIn.Box, RedRoof));
            Place(ctx, L(0f, -0.205f), 0.05f * s, p => ctx.PlaceByWidth(DiningHall, p, bearing + 180f, 0.17f * s, StandIn.Box, Plaster));
            // The living quarters: two rows of three to the west, facing the core across a lane.
            foreach (float u in new[] { -0.245f, -0.385f })
                foreach (float v in new[] { 0.105f, -0.015f, -0.135f })
                    Place(ctx, L(u, v), 0.09f * s, p => ctx.PlaceByWidth(Quarters, p, bearing + 90f, 0.12f * s, StandIn.Box, RedRoof));
            // The farmyard to the east: two cowsheds side by side, a hay barn in front, the water tower behind.
            foreach (float u in new[] { 0.235f, 0.37f })
                Place(ctx, L(u, -0.03f), 0.1f * s, p => ctx.PlaceByWidth(Barn, p, bearing + 180f, 0.16f * s, StandIn.Box, Timber));
            Place(ctx, L(0.30f, -0.205f), 0.09f * s, p => ctx.PlaceByWidth(Barn, p, bearing + 90f, 0.13f * s, StandIn.Box, Timber));
            Place(ctx, L(0.25f, 0.13f), 0.07f * s, p => ctx.PlaceByHeight(WaterTower, p, bearing + 180f, 0.18f * s, StandIn.Palm, Concrete, 0.55f));
            Place(ctx, L(0.405f, 0.135f), 0.07f * s, p => ctx.PlaceByHeight(ShadeTree, p, ctx.Range(0f, 360f), 0.12f * s, StandIn.Tree, Leaf, 0.5f));

            // 5. Shade trees along the road beyond the unit spot (tabor oaks stand in for eucalyptus).
            Place(ctx, F(0.075f, -0.42f), 0.08f * s, p => ctx.PlaceByHeight(ShadeTree, p, ctx.Range(0f, 360f), 0.13f * s, StandIn.Tree, Leaf, 0.5f));
            Place(ctx, F(-0.075f, -0.58f), 0.08f * s, p => ctx.PlaceByHeight(ShadeTree, p, ctx.Range(0f, 360f), 0.13f * s, StandIn.Tree, Leaf, 0.5f));
            Place(ctx, F(0.075f, -0.74f), 0.08f * s, p => ctx.PlaceByHeight(ShadeTree, p, ctx.Range(0f, 360f), 0.13f * s, StandIn.Tree, Leaf, 0.5f));

            // 6. Windbreaks: an L of cypresses along two field edges on the side away from the ponds (8 + 5).
            float wx = -pondSide;
            for (int k = 0; k < 8; k++)
                Place(ctx, F(wx * 0.51f, -0.42f + 0.085f * k), 0.05f * s,
                    p => ctx.PlaceByHeight(Cypress, p, ctx.Range(0f, 360f), ctx.Range(0.15f, 0.17f) * s, StandIn.Pine, CypressLeaf, 0.3f));
            foreach (float x in new[] { 0.18f, 0.265f, 0.35f, 0.435f, 0.51f })
                Place(ctx, F(wx * x, -0.51f), 0.05f * s,
                    p => ctx.PlaceByHeight(Cypress, p, ctx.Range(0f, 360f), ctx.Range(0.15f, 0.17f) * s, StandIn.Pine, CypressLeaf, 0.3f));
        }

        /// <summary>Places a tall piece only when it stays clear of the unit spot by <paramref name="clearM"/> (about half its
        /// footprint); a refused placement is normal.</summary>
        static void Place(HexTileContext ctx, Vector3 at, float clearM, System.Func<Vector3, bool> place)
        {
            if (!ctx.InUnitSpot(at, clearM)) place(at);
        }
    }
}
