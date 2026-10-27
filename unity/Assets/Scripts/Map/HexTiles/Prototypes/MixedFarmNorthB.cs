#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, b: larger parcels of green crops and wheat on a grid turned about -40 degrees, a long citrus block, and a
    /// sparse line of Aleppo pines by the track. Caps: 8 citrus x ~1,600 = 12,800, 7 pines x ~1,600 = 11,200 and the
    /// farmstead at most 2,800 = 26,800 of 30,000.</summary>
    [HexPrototype("mixed_farm_north_b", Kind = "farm", Region = "north")]
    public sealed class MixedFarmNorthB : MixedFarm
    {
        public override string Title => "Mixed farm, north (b): large parcels, pine line";
        protected override float YawCentre => -40f;
        protected override float YawSpread => 8f;
        protected override (float min, float max) StripWidth => (0.24f, 0.38f);
        protected override (float min, float max) ParcelLength => (0.26f, 0.50f);
        static readonly string[] Mix = { "green_crop", "wheat", "green_crop", "ploughed", "green_crop" };
        protected override string[] Crops => Mix;
        protected override Vector2 OrchardAt => new Vector2(-0.40f, -0.40f);
        protected override int OrchardCols => 4;
        protected override int OrchardRows => 2;
        protected override int MaxHedge => 7;
        protected override int HedgeGapOneIn => 2;
        protected override (string model, StandIn standIn, Color tint, float height) HedgeTree(HexTileContext ctx) => (Pine, StandIn.Pine, PineGreen, 0.16f);
    }
}
