#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, b: narrow strip fields in alternating wheat, green crops and ploughing on a grid turned about -50
    /// degrees, on sand and grass ground, with carob trees along two strip edges. Caps: two lines of at most 17 carobs
    /// x ~650 = 22,100 + the farmstead at most 2,800 = 24,900 of 30,000.</summary>
    [HexPrototype("wheat_fields_centre_b", Kind = "farm", Region = "centre")]
    public sealed class WheatFieldsCentreB : WheatFields
    {
        public override string Title => "Wheat fields, centre (b): strip fields, carob rows";
        protected override float YawCentre => -50f;
        protected override float YawSpread => 8f;
        protected override float CellAcross => 0.18f;
        protected override float CellAlong => 0.40f;
        protected override int GridHalf => 4;
        static readonly string[] Mix = { "wheat", "green_crop", "ploughed", "wheat", "green_crop", "wheat" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 6;
        protected override void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("beach", at, yaw, width, depth, 0.2f, 0.6f);
        static readonly float[] Lines = { -3.5f, -0.5f };
        protected override float[] HedgeLines => Lines;
        protected override float TrackAcross => 1.5f;
        protected override float TrackReach => 2.3f;
        static readonly Color CarobLeaf = new Color(0.27f, 0.39f, 0.20f);
        protected override (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ("sandbox:carob_lite", StandIn.Tree, CarobLeaf, 0.12f);
    }
}
