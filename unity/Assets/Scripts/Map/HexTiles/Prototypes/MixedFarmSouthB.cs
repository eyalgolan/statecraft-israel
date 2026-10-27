#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, b: long narrow ploughed and wheat strips on a grid turned about -25 degrees, a small olive block, sand
    /// along the strip ends, and a few acacias by the track. Caps: 4 olives x ~1,600 = 6,400, 5 acacias x ~1,160 =
    /// 5,800 and the farmstead at most 2,800 = 15,000 of 30,000.</summary>
    [HexPrototype("mixed_farm_south_b", Kind = "farm", Region = "south")]
    public sealed class MixedFarmSouthB : MixedFarm
    {
        public override string Title => "Mixed farm, south (b): long dry strips, acacias";
        protected override float YawCentre => -25f;
        protected override float YawSpread => 10f;
        protected override (float min, float max) StripWidth => (0.13f, 0.20f);
        protected override (float min, float max) ParcelLength => (0.50f, 0.80f);
        static readonly string[] Mix = { "ploughed", "wheat", "ploughed", "ploughed", "wheat" };
        protected override string[] Crops => Mix;
        protected override int FallowOneIn => 4;
        protected override void Fallow(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("desert", at, yaw, width, depth, 0.2f, 0.85f);
        protected override int OrchardCols => 2;
        protected override int OrchardRows => 2;
        protected override (string model, StandIn standIn, Color tint, float height) OrchardTree => (Olive, StandIn.Tree, OliveLeaf, 0.12f);
        protected override int MaxHedge => 5;
        protected override int HedgeGapOneIn => 2;
        protected override (string model, StandIn standIn, Color tint, float height) HedgeTree(HexTileContext ctx) => (Acacia, StandIn.Tree, DryLeaf, 0.13f);
    }
}
