#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, b: wide ploughed strips with wheat between, running across the hex on a grid turned about -20 degrees,
    /// bare fallow sand between some, and a broken line of acacias along one strip. Caps: one line of at most 9 acacias
    /// x ~1,160 = 10,440 + the farmstead at most 2,800 = 13,240 of 30,000.</summary>
    [HexPrototype("wheat_fields_south_b", Kind = "farm", Region = "south")]
    public sealed class WheatFieldsSouthB : WheatFields
    {
        public override string Title => "Wheat fields, south (b): ploughed strips, acacias";
        protected override float YawCentre => -20f;
        protected override float YawSpread => 10f;
        protected override float CellAcross => 0.50f;
        protected override float CellAlong => 0.26f;
        static readonly string[] Mix = { "ploughed", "ploughed", "wheat", "ploughed", "wheat" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 4;
        protected override void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("desert", at, yaw, width, depth, 0.2f, 0.85f);
        static readonly float[] Lines = { -0.5f };
        protected override float[] HedgeLines => Lines;
        protected override int HedgeGapOneIn => 2;
        static readonly Color AcaciaLeaf = new Color(0.45f, 0.47f, 0.28f);
        protected override (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ("sandbox:acacia_lite", StandIn.Tree, AcaciaLeaf, 0.13f);
    }
}
