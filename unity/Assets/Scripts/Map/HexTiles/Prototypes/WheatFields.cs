#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Context tile: a farmed plain, Civ's farm improvement. A patchwork of large fields on one grid (wheat,
    /// green fodder, fresh ploughing) with tracks between, olive hedgerows along a few edges, and one farmhouse with its
    /// barn (13 October Tripo modules) by the main track. Light, so many hexes can wear it.
    ///
    /// The regional variants (WheatFields&lt;Region&gt;&lt;A|B&gt;) subclass it: the grid's turn, the parcel size, the crop
    /// mix, the hedgerows and the farmstead are the protected virtual members below. Their defaults are this tile's own
    /// values, so wheat_fields itself builds exactly as before (HexVariantTests).</summary>
    [HexPrototype("wheat_fields", Kind = "farm")]
    public class WheatFields : IHexPrototype
    {
        public virtual string Title => "Wheat fields and a farmstead";
        public virtual HexTerrain Terrain => HexTerrain.Flat;
        public virtual string Ground => "plains";
        public virtual int TriangleBudget => HexScale.BudgetFields + 10000;

        static readonly string[] BaseCrops = { "wheat", "wheat", "green_crop", "ploughed", "wheat", "green_crop" };
        static readonly float[] BaseHedges = { -0.5f, 0.5f };

        /// <summary>The grid's turn is drawn in YawCentre ± YawSpread degrees.</summary>
        protected virtual float YawCentre => 0f;
        protected virtual float YawSpread => 25f;
        /// <summary>A parcel's side across (x) and along (z) the grid, as shares of S.</summary>
        protected virtual float CellAcross => 0.36f;
        protected virtual float CellAlong => CellAcross;
        /// <summary>The grid runs from -GridHalf to GridHalf parcels each way.</summary>
        protected virtual int GridHalf => 2;
        /// <summary>The crop layers a parcel draws from, one per parcel (repeat a layer to weight it).</summary>
        protected virtual string[] Crops => BaseCrops;
        /// <summary>One parcel in this many is left as the hex's own ground (pasture).</summary>
        protected virtual int PastureOneIn => 7;
        /// <summary>A parcel left as pasture, at its spot and size: nothing by default (the ground shows).</summary>
        protected virtual void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) { }
        /// <summary>The farm track runs along the grid at this many parcels across, and reaches this far each way.</summary>
        protected virtual float TrackAcross => 0.5f;
        protected virtual float TrackReach => 2.6f;
        protected virtual string TrackLayer => "dirt";
        /// <summary>Hedgerow lines, in parcels across the grid; a tree every half parcel along them, one slot in
        /// HedgeGapOneIn left empty.</summary>
        protected virtual float[] HedgeLines => BaseHedges;
        protected virtual int HedgeGapOneIn => 3;
        /// <summary>One hedgerow tree: model, stand-in, tint and height as a share of S. Drawn after the gap roll.</summary>
        protected virtual (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ("sandbox:olive_lite", StandIn.Tree, null, 0.12f);

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S, yaw = ctx.Range(YawCentre - YawSpread, YawCentre + YawSpread), cx = CellAcross * s, cz = CellAlong * s, gap = 0.035f * s;
            var turn = Quaternion.Euler(0f, yaw, 0f);
            // Fields: a grid of parcels painted into the ground as crop rows (wheat, green crops, fresh ploughing),
            // a few left as pasture, with soft edges so they read as fields, not boards, from far away.
            string[] crops = Crops;
            int n = GridHalf;
            for (int i = -n; i <= n; i++)
                for (int j = -n; j <= n; j++)
                {
                    var at = ctx.Centre + turn * new Vector3(i * cx, 0f, j * cz);
                    string crop = crops[ctx.Rng.Next(crops.Length)];
                    bool across = ctx.Rng.Next(2) == 0;
                    float w = cx - gap, d = cz - gap;
                    if (ctx.Rng.Next(PastureOneIn) == 0) { Pasture(ctx, at, yaw, w, d); continue; }
                    ctx.PaintGround(crop, at, yaw + (across ? 90f : 0f), across ? d : w, across ? w : d, 0.08f, 0.95f);
                }
            // The farm track: one straight track along the grid through the hex, the farmstead on it.
            ctx.PaintStroke(TrackLayer, ctx.Centre + turn * new Vector3(TrackAcross * cx, 0f, -TrackReach * cz), ctx.Centre + turn * new Vector3(TrackAcross * cx, 0f, TrackReach * cz), 0.035f * s);
            // Hedgerows: trees down a few parcel edges.
            for (int k = -2 * n; k <= 2 * n; k++)
                foreach (float x in HedgeLines)
                {
                    var at = ctx.Centre + turn * new Vector3(x * cx, 0f, k * cz * 0.5f);
                    if (ctx.Rng.Next(HedgeGapOneIn) != 0 && !ctx.InUnitSpot(at, 0.05f * s))
                    {
                        var t = HedgeTree(ctx);
                        ctx.PlaceByHeight(t.model, at, ctx.Range(0f, 360f), t.height * s, t.standIn, t.tint, 0.5f);
                    }
                }
            Farmstead(ctx, turn, yaw);
            Dress(ctx, turn, yaw);
        }

        /// <summary>The farmstead on a yard by the centre track: a two-storey farmhouse and a barn.</summary>
        protected virtual void Farmstead(HexTileContext ctx, Quaternion turn, float yaw)
        {
            float s = ctx.S;
            var yard = ctx.Centre + turn * new Vector3(0.48f * s, 0f, -0.18f * s);   // out of the unit spot
            ctx.PaintGround("dirt", yard, yaw, 0.3f * s, 0.24f * s, 0.3f, 0.9f);
            ctx.PlaceByWidth("sandbox:kit_moshava", yard + turn * new Vector3(-0.06f * s, 0f, 0f), yaw + 180f, 0.13f * s, StandIn.Box);
            ctx.PlaceByWidth("sandbox:kit_moshava_3", yard + turn * new Vector3(0.08f * s, 0f, 0.02f * s), yaw + 90f, 0.12f * s, StandIn.Box);
        }

        /// <summary>Anything a variant adds last (scattered trees, a palm by the yard): nothing here.</summary>
        protected virtual void Dress(HexTileContext ctx, Quaternion turn, float yaw) { }
    }

    /// <summary>Context tile: open grazing land in the coastal plain and foothills. Scattered olive and carob trees, a few
    /// rock outcrops, and a flock of sheep with a donkey and their shepherd's track (13 October Tripo animals).</summary>
    [HexPrototype("grazing_land", Kind = "farm")]
    public sealed class GrazingLand : IHexPrototype
    {
        public string Title => "Grazing land with a flock";
        public HexTerrain Terrain => HexTerrain.Flat;
        public string Ground => "grass";
        public int TriangleBudget => 60000;

        public void Build(HexTileContext ctx)
        {
            float s = ctx.S;
            int trees = 0;
            for (int k = 0; k < 60 && trees < 12; k++)
            {
                bool carob = ctx.Rng.Next(3) == 0;
                var spot = ctx.RandomPoint(0.04f * s);
                if (ctx.InUnitSpot(spot, 0.05f * s)) continue;
                if (ctx.PlaceByHeight(carob ? "sandbox:carob_lite" : "sandbox:olive_lite", spot, ctx.Range(0f, 360f), ctx.Range(0.12f, 0.15f) * s, StandIn.Tree, null, 0.5f)) trees++;
            }
            for (int k = 0; k < 3; k++)
                ctx.PlaceByWidth("sandbox:limestone*", ctx.At(ctx.Range(0f, 360f), ctx.Range(0.4f, 0.6f)), ctx.Range(0f, 360f), ctx.Range(0.07f, 0.1f) * s, StandIn.Rock);
            // The flock: one compact group, all grazing the same way, the donkey at its edge.
            var flock = ctx.At(ctx.Range(0f, 360f), ctx.Range(0.45f, 0.55f));   // grazing beside the unit spot, not in it
            float heading = ctx.Range(0f, 360f);
            for (int k = 0; k < 6; k++)
            {
                var p = flock + new Vector3(ctx.Range(-0.1f, 0.1f) * s, 0f, ctx.Range(-0.1f, 0.1f) * s);
                ctx.PlaceByHeight("proto:figure-sheep-lite-1948", p, heading + ctx.Range(-30f, 30f), 0.06f * s, StandIn.Figure, null, 1.2f);
            }
            ctx.PlaceByHeight("proto:figure-donkey-lite-1948", flock + new Vector3(0.13f * s, 0f, 0f), heading, 0.08f * s, StandIn.Figure, null, 1.0f);
        }
    }
}
