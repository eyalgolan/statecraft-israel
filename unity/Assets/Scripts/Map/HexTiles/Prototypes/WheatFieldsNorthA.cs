#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, a: green, wetter fields on a grid turned about 40 degrees, smaller square parcels mostly in green crops,
    /// the gaps left as grass, and oak and carob hedgerows down three parcel edges. Caps: at most 39 hedgerow trees x
    /// ~650 + the farmstead's two buildings at most 2,800 = 28,150 of 30,000.</summary>
    [HexPrototype("wheat_fields_north_a", Kind = "farm", Region = "north")]
    public sealed class WheatFieldsNorthA : WheatFields
    {
        public override string Title => "Wheat fields, north (a): green crops, oak hedgerows";
        protected override float YawCentre => 40f;
        protected override float YawSpread => 8f;
        protected override float CellAcross => 0.30f;
        protected override int GridHalf => 3;
        static readonly string[] Mix = { "green_crop", "green_crop", "green_crop", "wheat", "wheat", "ploughed" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 5;
        protected override void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("grass", at, yaw, width, depth, 0.15f, 0.9f);
        static readonly float[] Lines = { -1.5f, 0.5f, 2.5f };
        protected override float[] HedgeLines => Lines;
        protected override float TrackReach => 3.6f;
        static readonly Color OakLeaf = new Color(0.30f, 0.42f, 0.20f);
        protected override (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ctx.Rng.Next(3) == 0 ? ("sandbox:carob_lite", StandIn.Tree, OakLeaf, 0.12f) : ("sandbox:tabor_oak_lite", StandIn.Tree, OakLeaf, 0.13f);
    }
}
