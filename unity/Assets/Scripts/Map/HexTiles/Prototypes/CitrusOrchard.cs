#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Jaffa orange groves south-east of Tel Aviv, 1948: an improvement hex like Civ's plantation. Three
    /// rectangular grove blocks of citrus in straight rows on one grid turned 15 degrees, on dark tilled soil; cypress
    /// windbreaks along the west and north edges (the sea wind); dirt tracks between the blocks; a packing house on its
    /// yard by the main track, a citrus truck beside it and a square irrigation pool. Few, large pieces (HexScale).
    ///
    /// Soil, tracks and the yard are painted into the ground (ctx.PaintGround / PaintStroke); only the pool is a flat.
    /// Tall pieces keep out of the unit spot (ctx.InUnitSpot), so the trees there are left out and the packing house
    /// stands south of it.
    ///
    /// Triangle estimate (per-instance counts of the local models): at most 21 citrus x 1600 + 6 cypress x 1600 + truck
    /// 7000 + packing house up to 1400 = 51,600, plus one flat, against 60,000. Stand-ins draw fewer.</summary>
    [HexPrototype("citrus_orchard")]
    public sealed class CitrusOrchard : IHexPrototype
    {
        public string Title => "Jaffa orange groves (1948)";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "grass";
        public int TriangleBudget => HexScale.BudgetForest;

        // Models: the EZ-Tree trees and the Tripo kit and truck are local-only; without them the stand-ins draw.
        const string Citrus = "sandbox:citrus_lite", Cypress = "sandbox:cypress_lite", House = "sandbox:kit_moshava*",
            Truck = "proto:vehicle-citrus-truck-v1";
        static readonly Color Leaf = new Color(0.16f, 0.34f, 0.14f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f),
            Plaster = new Color(0.88f, 0.80f, 0.68f), Lorry = new Color(0.42f, 0.48f, 0.52f), Water = new Color(0.30f, 0.52f, 0.66f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, gridYaw = 15f, row = 0.13f;
            var turn = Quaternion.Euler(0f, gridYaw, 0f);
            // A point on the orchard grid, in shares of S.
            Vector3 Grid(float x, float z) => ctx.Centre + turn * new Vector3(x * s, 0f, z * s);

            // 1. Dirt tracks painted into the ground: the main track (grid north-south) and the cross track, clipped to the hex.
            ctx.PaintStroke("dirt", Grid(0f, -0.95f), Grid(0f, 0.95f), 0.045f * s, 0.9f);
            ctx.PaintStroke("dirt", Grid(-0.95f, 0f), Grid(0.95f, 0f), 0.04f * s, 0.9f);

            // 2. Grove blocks: tilled soil painted under each, then the trees in straight rows (no position jitter, so rows
            //    read). (x0, z0) is the first tree, cols x rows trees at the row spacing; trees in the unit spot are left out.
            Grove(ctx, Grid, gridYaw, row, -0.44f, 0.18f, 3, 3); // north-west, 9
            Grove(ctx, Grid, gridYaw, row, 0.18f, 0.18f, 3, 2);  // north-east, 6
            Grove(ctx, Grid, gridYaw, row, -0.44f, -0.33f, 3, 2); // south-west, 6

            // 3. Cypress windbreaks: tight rows along the north edge of the north-west block and the west edge of the
            //    south-west block.
            for (int k = 0; k < 3; k++)
            {
                ctx.PlaceByHeight(Cypress, Grid(-0.41f + 0.1f * k, 0.56f), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * s, StandIn.Pine, CypressLeaf, 0.3f);
                ctx.PlaceByHeight(Cypress, Grid(-0.57f, -0.365f + 0.1f * k), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * s, StandIn.Pine, CypressLeaf, 0.3f);
            }

            // 4. The packing house on its painted yard, beside the main track in the south-east quarter and south of the
            //    unit spot, and the truck beside it.
            ctx.PaintGround("dirt", Grid(0.12f, -0.44f), gridYaw, 0.17f * s, 0.36f * s, 0.2f, 0.8f);
            ctx.PlaceByWidth(House, Grid(0.11f, -0.36f), gridYaw + 180f * ctx.Rng.Next(2), 0.14f * s, StandIn.Box, Plaster);
            ctx.PlaceByWidth(Truck, Grid(0.115f, -0.54f), gridYaw, 0.14f * s, StandIn.Box, Lorry);

            // 5. The square irrigation pool, on the grass east of the yard.
            ctx.PlaceFlat(Grid(0.38f, -0.40f), gridYaw, 0.12f * s, 0.12f * s, Water);
        }

        static void Grove(HexTileContext ctx, System.Func<float, float, Vector3> grid, float gridYaw, float row,
            float x0, float z0, int cols, int rows)
        {
            float w = (cols - 1) * row + 0.10f, d = (rows - 1) * row + 0.10f;
            ctx.PaintGround("ploughed", grid(x0 + (cols - 1) * row * 0.5f, z0 + (rows - 1) * row * 0.5f), gridYaw, w * ctx.S, d * ctx.S, 0.2f, 0.7f);
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                {
                    var at = grid(x0 + i * row, z0 + j * row);
                    float yaw = ctx.Range(0f, 360f), height = ctx.Range(0.12f, 0.15f) * ctx.S;   // drawn even when skipped, so the sequence holds
                    if (ctx.InUnitSpot(at, 0.04f * ctx.S)) continue;
                    ctx.PlaceByHeight(Citrus, at, yaw, height, StandIn.Tree, Leaf, 0.6f);
                }
        }
    }
}
