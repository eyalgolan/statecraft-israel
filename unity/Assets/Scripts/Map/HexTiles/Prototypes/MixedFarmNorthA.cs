#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, a: small parcels mostly in green crops on a grid turned about 30 degrees, an olive block, and an oak
    /// hedgerow along the track. Caps: 6 olives x ~1,600 = 9,600, 13 oaks x ~650 = 8,450 and the farmstead at most
    /// 2,800 = 20,850 of 30,000.</summary>
    [HexPrototype("mixed_farm_north_a", Kind = "farm", Region = "north")]
    public sealed class MixedFarmNorthA : MixedFarm
    {
        public override string Title => "Mixed farm, north (a): small green parcels, oak hedge";
        protected override float YawCentre => 30f;
        protected override float YawSpread => 8f;
        protected override (float min, float max) StripWidth => (0.12f, 0.22f);
        protected override (float min, float max) ParcelLength => (0.14f, 0.30f);
        static readonly string[] Mix = { "green_crop", "green_crop", "green_crop", "wheat", "ploughed", "grass" };
        protected override string[] Crops => Mix;
        protected override int OrchardRows => 2;
        protected override (string model, StandIn standIn, Color tint, float height) OrchardTree => (Olive, StandIn.Tree, OliveLeaf, 0.12f);
        protected override (string model, StandIn standIn, Color tint, float height) HedgeTree(HexTileContext ctx) => (Oak, StandIn.Tree, BroadLeaf, 0.13f);
    }
}
