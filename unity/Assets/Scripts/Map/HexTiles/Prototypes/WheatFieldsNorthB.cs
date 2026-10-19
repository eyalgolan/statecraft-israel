#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, b: long narrow strips of green crops and wheat on a grid turned about -35 degrees, with one Aleppo pine
    /// windbreak along a strip edge. Caps: one line of at most 13 pines x ~1,600 = 20,800 + the farmstead at most 2,800
    /// = 23,600 of 30,000.</summary>
    [HexPrototype("wheat_fields_north_b", Kind = "farm", Region = "north")]
    public sealed class WheatFieldsNorthB : WheatFields
    {
        public override string Title => "Wheat fields, north (b): green strips, pine windbreak";
        protected override float YawCentre => -35f;
        protected override float YawSpread => 8f;
        protected override float CellAcross => 0.22f;
        protected override float CellAlong => 0.50f;
        protected override int GridHalf => 3;
        static readonly string[] Mix = { "green_crop", "wheat", "green_crop", "ploughed", "green_crop" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 6;
        protected override void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("grass", at, yaw, width, depth, 0.15f, 0.9f);
        static readonly float[] Lines = { -1.5f };
        protected override float[] HedgeLines => Lines;
        protected override int HedgeGapOneIn => 4;
        protected override float TrackAcross => 1.5f;
        protected override float TrackReach => 2.2f;
        static readonly Color PineGreen = new Color(0.20f, 0.33f, 0.18f);
        protected override (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ("sandbox:aleppo_pine_lite", StandIn.Pine, PineGreen, 0.16f);
    }
}
