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
    /// 7000 + packing house up to 1400 = 51,600, plus one flat, against 60,000. Stand-ins draw fewer.
    ///
    /// The regional variants (CitrusOrchard&lt;Region&gt;&lt;A|B&gt;) subclass it through the protected virtual members:
    /// the grid, the blocks, the grove trees, the windbreaks and the yard. Their defaults are this tile's own, so
    /// citrus_orchard itself builds exactly as before (HexVariantTests).</summary>
    [HexPrototype("citrus_orchard", Kind = "farm")]
    public class CitrusOrchard : IHexPrototype
    {
        public virtual string Title => "Jaffa orange groves (1948)";
        public virtual HexTerrain Terrain => HexTerrain.Flat;
        public virtual string Ground => "grass";
        public virtual int TriangleBudget => HexScale.BudgetForest;

        // Models: the EZ-Tree trees and the Tripo kit and truck are local-only; without them the stand-ins draw.
        protected const string Citrus = "sandbox:citrus_lite", Cypress = "sandbox:cypress_lite", House = "sandbox:kit_moshava*",
            Truck = "proto:vehicle-citrus-truck-v1";
        protected static readonly Color Leaf = new Color(0.16f, 0.34f, 0.14f), CypressLeaf = new Color(0.12f, 0.26f, 0.13f),
            Plaster = new Color(0.88f, 0.80f, 0.68f), Lorry = new Color(0.42f, 0.48f, 0.52f), Water = new Color(0.30f, 0.52f, 0.66f);

        /// <summary>A grove block: the first tree at (x0, z0) on the grid, in shares of S, and cols x rows trees.</summary>
        protected readonly struct Block
        {
            public readonly float X0, Z0; public readonly int Cols, Rows;
            public Block(float x0, float z0, int cols, int rows) { X0 = x0; Z0 = z0; Cols = cols; Rows = rows; }
        }

        static readonly Block[] BaseBlocks = { new Block(-0.44f, 0.18f, 3, 3), new Block(0.18f, 0.18f, 3, 2), new Block(-0.44f, -0.33f, 3, 2) };

        /// <summary>The orchard grid's turn in degrees, and the tree spacing along its rows as a share of S.</summary>
        protected virtual float GridYaw(HexTileContext ctx) => 15f;
        protected virtual float Row => 0.13f;
        /// <summary>The grove blocks: north-west 9 trees, north-east 6, south-west 6.</summary>
        protected virtual Block[] Blocks => BaseBlocks;
        /// <summary>The soil painted under a block.</summary>
        protected virtual string BlockSoil => "ploughed";
        /// <summary>The tree at column i, row j of block b (no randomness, so the sequence holds); null leaves a gap.</summary>
        protected virtual (string model, StandIn standIn, Color? tint)? GroveTree(int b, int i, int j) => (Citrus, StandIn.Tree, Leaf);
        /// <summary>A grove tree's height range, as shares of S.</summary>
        protected virtual (float min, float max) TreeHeight => (0.12f, 0.15f);

        /// <summary>A point on the orchard grid, in shares of S.</summary>
        protected static Vector3 Grid(HexTileContext ctx, float yaw, float x, float z) => ctx.Centre + Quaternion.Euler(0f, yaw, 0f) * new Vector3(x * ctx.S, 0f, z * ctx.S);

        public void Build(HexTileContext ctx)
        {
            float gridYaw = GridYaw(ctx), row = Row;
            Vector3 G(float x, float z) => Grid(ctx, gridYaw, x, z);

            // 1. Tracks painted into the ground.
            Tracks(ctx, gridYaw);

            // 2. Grove blocks: tilled soil painted under each, then the trees in straight rows (no position jitter, so rows
            //    read). (x0, z0) is the first tree, cols x rows trees at the row spacing; trees in the unit spot are left out.
            var blocks = Blocks;
            for (int b = 0; b < blocks.Length; b++) Grove(ctx, G, gridYaw, row, b, blocks[b]);

            // 3. Windbreaks, 4. the packing house and its yard, 5. the pool.
            Windbreaks(ctx, gridYaw);
            Yard(ctx, gridYaw);
            Dress(ctx, gridYaw);
        }

        /// <summary>The main track (grid north-south) and the cross track, clipped to the hex.</summary>
        protected virtual void Tracks(HexTileContext ctx, float gridYaw)
        {
            float s = ctx.S;
            ctx.PaintStroke("dirt", Grid(ctx, gridYaw, 0f, -0.95f), Grid(ctx, gridYaw, 0f, 0.95f), 0.045f * s, 0.9f);
            ctx.PaintStroke("dirt", Grid(ctx, gridYaw, -0.95f, 0f), Grid(ctx, gridYaw, 0.95f, 0f), 0.04f * s, 0.9f);
        }

        /// <summary>Cypress windbreaks: tight rows along the north edge of the north-west block and the west edge of the
        /// south-west block.</summary>
        protected virtual void Windbreaks(HexTileContext ctx, float gridYaw)
        {
            float s = ctx.S;
            for (int k = 0; k < 3; k++)
            {
                ctx.PlaceByHeight(Cypress, Grid(ctx, gridYaw, -0.41f + 0.1f * k, 0.56f), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * s, StandIn.Pine, CypressLeaf, 0.3f);
                ctx.PlaceByHeight(Cypress, Grid(ctx, gridYaw, -0.57f, -0.365f + 0.1f * k), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * s, StandIn.Pine, CypressLeaf, 0.3f);
            }
        }

        /// <summary>The packing house on its painted yard, beside the main track in the south-east quarter and south of the
        /// unit spot, the truck beside it, and the square irrigation pool on the grass east of the yard.</summary>
        protected virtual void Yard(HexTileContext ctx, float gridYaw)
        {
            float s = ctx.S;
            ctx.PaintGround("dirt", Grid(ctx, gridYaw, 0.12f, -0.44f), gridYaw, 0.17f * s, 0.36f * s, 0.2f, 0.8f);
            ctx.PlaceByWidth(House, Grid(ctx, gridYaw, 0.11f, -0.36f), gridYaw + 180f * ctx.Rng.Next(2), 0.14f * s, StandIn.Box, Plaster);
            ctx.PlaceByWidth(Truck, Grid(ctx, gridYaw, 0.115f, -0.54f), gridYaw, 0.14f * s, StandIn.Box, Lorry);
            ctx.PlaceFlat(Grid(ctx, gridYaw, 0.38f, -0.40f), gridYaw, 0.12f * s, 0.12f * s, Water);
        }

        /// <summary>Anything a variant adds last: nothing here.</summary>
        protected virtual void Dress(HexTileContext ctx, float gridYaw) { }

        void Grove(HexTileContext ctx, System.Func<float, float, Vector3> grid, float gridYaw, float row, int b, Block k)
        {
            float w = (k.Cols - 1) * row + 0.10f, d = (k.Rows - 1) * row + 0.10f;
            ctx.PaintGround(BlockSoil, grid(k.X0 + (k.Cols - 1) * row * 0.5f, k.Z0 + (k.Rows - 1) * row * 0.5f), gridYaw, w * ctx.S, d * ctx.S, 0.2f, 0.7f);
            var (hMin, hMax) = TreeHeight;
            for (int i = 0; i < k.Cols; i++)
                for (int j = 0; j < k.Rows; j++)
                {
                    var at = grid(k.X0 + i * row, k.Z0 + j * row);
                    float yaw = ctx.Range(0f, 360f), height = ctx.Range(hMin, hMax) * ctx.S;   // drawn even when skipped, so the sequence holds
                    if (ctx.InUnitSpot(at, 0.04f * ctx.S)) continue;
                    var tree = GroveTree(b, i, j);
                    if (tree == null) continue;
                    ctx.PlaceByHeight(tree.Value.model, at, yaw, height, tree.Value.standIn, tree.Value.tint, 0.6f);
                }
        }
    }
}
