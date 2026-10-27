#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: a mixed family farm on the plain. A patchwork of painted crop parcels of uneven size (wheat,
    /// green crops, fresh ploughing, a fallow strip) in strips across one grid, a small orchard block on tilled soil, a
    /// hedgerow along the farm track, and a farmhouse with its barn on a yard by the track. Where wheat_fields is one even
    /// grid of large fields, this reads as many small, uneven ones.
    ///
    /// Everything but the trees and the two buildings is painted into the ground. Tall pieces keep out of the unit spot.
    ///
    /// Triangle estimate at the caps, with measured per-model counts: an orchard of at most 9 citrus x ~1,600 = 14,400,
    /// at most 13 hedgerow carobs x ~650 = 8,450, and the two kit_moshava buildings at most 2 x 1,400 = 2,800. Total at
    /// most 25,650 of 30,000. Stand-ins draw fewer.
    ///
    /// The regional variants (MixedFarm&lt;Region&gt;&lt;A|B&gt;) subclass it through the protected virtual members: the
    /// grid's turn, the parcel sizes and crops, the orchard, the hedgerow and the farmstead.</summary>
    [HexPrototype("mixed_farm", Kind = "farm")]
    public class MixedFarm : IHexPrototype
    {
        public virtual string Title => "Mixed farm: crop patchwork, orchard and farmstead";
        public virtual HexTerrain Terrain => HexTerrain.Flat;
        public virtual string Ground => "plains";
        public virtual int TriangleBudget => HexScale.BudgetFields + 25000;   // its farmstead at full detail: 38k measured

        // Models: the EZ-Tree trees and the Tripo kit are local-only; without them the stand-ins draw.
        protected const string Citrus = "sandbox:citrus_lite", Olive = "sandbox:olive_lite", Carob = "sandbox:carob_lite",
            Oak = "sandbox:tabor_oak_lite", Pine = "sandbox:aleppo_pine_lite", Cypress = "sandbox:cypress_lite",
            Palm = "sandbox:date_palm", Acacia = "sandbox:acacia_lite", House = "sandbox:kit_moshava_2", Barn = "sandbox:kit_moshava_3";
        protected static readonly Color CitrusLeaf = new Color(0.16f, 0.34f, 0.14f), OliveLeaf = new Color(0.38f, 0.44f, 0.28f),
            BroadLeaf = new Color(0.27f, 0.39f, 0.20f), PineGreen = new Color(0.20f, 0.33f, 0.18f), DryLeaf = new Color(0.45f, 0.47f, 0.28f),
            Plaster = new Color(0.88f, 0.80f, 0.68f);

        static readonly string[] BaseCrops = { "wheat", "green_crop", "ploughed", "green_crop", "wheat", "plains" };

        /// <summary>The grid's turn is drawn in YawCentre ± YawSpread degrees.</summary>
        protected virtual float YawCentre => 0f;
        protected virtual float YawSpread => 20f;
        /// <summary>Strip widths across the grid, and parcel lengths along a strip, as share-of-S ranges.</summary>
        protected virtual (float min, float max) StripWidth => (0.16f, 0.30f);
        protected virtual (float min, float max) ParcelLength => (0.18f, 0.42f);
        /// <summary>The crop layers a parcel draws from (repeat a layer to weight it).</summary>
        protected virtual string[] Crops => BaseCrops;
        /// <summary>One parcel in this many is left fallow (Fallow paints it, or the ground shows).</summary>
        protected virtual int FallowOneIn => 8;
        protected virtual void Fallow(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) { }

        /// <summary>The orchard block: its centre on the grid (shares of S), cols x rows trees at Spacing.</summary>
        protected virtual Vector2 OrchardAt => new Vector2(-0.42f, 0.30f);
        protected virtual int OrchardCols => 3;
        protected virtual int OrchardRows => 3;
        protected virtual float OrchardSpacing => 0.11f;
        protected virtual (string model, StandIn standIn, Color tint, float height) OrchardTree => (Citrus, StandIn.Tree, CitrusLeaf, 0.12f);

        /// <summary>The farm track runs along the grid at TrackX (share of S); the hedgerow stands HedgeOffset beside it,
        /// a slot every HedgeStep, one slot in HedgeGapOneIn empty, at most MaxHedge trees (0 for none).</summary>
        protected virtual float TrackX => 0.28f;
        protected virtual string TrackLayer => "dirt";
        protected virtual float HedgeOffset => -0.06f;
        protected virtual float HedgeStep => 0.14f;
        protected virtual int HedgeGapOneIn => 3;
        protected virtual int MaxHedge => 13;
        protected virtual (string model, StandIn standIn, Color tint, float height) HedgeTree(HexTileContext ctx) => (Carob, StandIn.Tree, BroadLeaf, 0.12f);

        /// <summary>The yard's centre on the grid (shares of S), out of the unit spot by the track.</summary>
        protected virtual Vector2 YardAt => new Vector2(0.50f, -0.22f);
        protected virtual string HouseModel => House;
        protected virtual string BarnModel => Barn;

        /// <summary>A point on the farm grid, in shares of S.</summary>
        protected static Vector3 Grid(HexTileContext ctx, float yaw, float x, float z) => ctx.Centre + Quaternion.Euler(0f, yaw, 0f) * new Vector3(x * ctx.S, 0f, z * ctx.S);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(YawCentre - YawSpread, YawCentre + YawSpread), gap = 0.03f;
            Vector3 G(float x, float z) => Grid(ctx, yaw, x, z);

            // 1. The patchwork: strips across the grid, each cut into parcels of uneven length, one crop each, a few
            //    left fallow. Parcels whose centre is far outside the hex are not painted (the paint is clipped anyway).
            var (wMin, wMax) = StripWidth; var (lMin, lMax) = ParcelLength;
            string[] crops = Crops;
            for (float x = -0.95f; x < 0.95f;)
            {
                float w = ctx.Range(wMin, wMax);
                for (float z = -0.95f + ctx.Range(0f, 0.12f); z < 0.95f;)
                {
                    float d = ctx.Range(lMin, lMax);
                    var at = G(x + w * 0.5f, z + d * 0.5f);
                    string crop = crops[ctx.Rng.Next(crops.Length)];
                    bool across = ctx.Rng.Next(2) == 0, fallow = ctx.Rng.Next(FallowOneIn) == 0;
                    z += d;
                    if (!ctx.Inside(at, -0.25f * s)) continue;
                    float pw = (w - gap) * s, pd = (d - gap) * s;
                    if (fallow) { Fallow(ctx, at, yaw, pw, pd); continue; }
                    ctx.PaintGround(crop, at, yaw + (across ? 90f : 0f), across ? pd : pw, across ? pw : pd, 0.1f, 0.95f);
                }
                x += w;
            }

            // 2. The orchard block on tilled soil, trees in straight rows; any in the unit spot are left out.
            int cols = OrchardCols, rows = OrchardRows; float sp = OrchardSpacing; var o = OrchardAt; var tree = OrchardTree;
            float x0 = o.x - (cols - 1) * sp * 0.5f, z0 = o.y - (rows - 1) * sp * 0.5f;
            ctx.PaintGround("ploughed", G(o.x, o.y), yaw, ((cols - 1) * sp + 0.10f) * s, ((rows - 1) * sp + 0.10f) * s, 0.2f, 0.8f);
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                {
                    var at = G(x0 + i * sp, z0 + j * sp);
                    float ty = ctx.Range(0f, 360f), h = tree.height * ctx.Range(0.95f, 1.1f) * s;   // drawn even when skipped
                    if (ctx.InUnitSpot(at, 0.04f * s)) continue;
                    ctx.PlaceByHeight(tree.model, at, ty, h, tree.standIn, tree.tint, 0.6f);
                }

            // 3. The farm track along the grid, and the hedgerow beside it.
            ctx.PaintStroke(TrackLayer, G(TrackX, -0.95f), G(TrackX, 0.95f), 0.035f * s, 0.9f);
            int hedge = 0, maxHedge = MaxHedge;
            for (float z = -0.9f; z <= 0.9f && hedge < maxHedge; z += HedgeStep)
            {
                var at = G(TrackX + HedgeOffset, z);
                if (ctx.Rng.Next(HedgeGapOneIn) == 0 || ctx.InUnitSpot(at, 0.05f * s)) continue;
                var t = HedgeTree(ctx);
                if (ctx.PlaceByHeight(t.model, at, ctx.Range(0f, 360f), t.height * s, t.standIn, t.tint, 0.5f)) hedge++;
            }

            // 4. The farmstead: a painted yard by the track, the farmhouse and the barn at right angles.
            var y = YardAt;
            ctx.PaintGround("dirt", G(y.x, y.y), yaw, 0.30f * s, 0.24f * s, 0.3f, 0.9f);
            ctx.PlaceByWidth(HouseModel, G(y.x - 0.06f, y.y + 0.02f), yaw + 180f, 0.13f * s, StandIn.Box, Plaster);
            ctx.PlaceByWidth(BarnModel, G(y.x + 0.08f, y.y - 0.02f), yaw + 90f, 0.12f * s, StandIn.Box, Plaster);
            Dress(ctx, yaw);
        }

        /// <summary>Anything a variant adds last (palms by the yard, scattered trees): nothing here.</summary>
        protected virtual void Dress(HexTileContext ctx, float yaw) { }
    }
}
