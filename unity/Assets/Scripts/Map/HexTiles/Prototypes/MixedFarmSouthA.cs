#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, a: broad parcels of wheat and fresh ploughing with fallow sand between, a grid turned about 15 degrees, a
    /// block of date palms, and no hedgerow. Caps: 9 palms x ~280 = 2,520 and the farmstead at most 2,800 = 5,320 of
    /// 30,000.</summary>
    [HexPrototype("mixed_farm_south_a", Kind = "farm", Region = "south")]
    public sealed class MixedFarmSouthA : MixedFarm
    {
        public override string Title => "Mixed farm, south (a): dry parcels, date-palm block";
        protected override float YawCentre => 15f;
        protected override float YawSpread => 10f;
        protected override (float min, float max) StripWidth => (0.30f, 0.45f);
        protected override (float min, float max) ParcelLength => (0.30f, 0.50f);
        static readonly string[] Mix = { "wheat", "ploughed", "wheat", "ploughed", "plains" };
        protected override string[] Crops => Mix;
        protected override int FallowOneIn => 3;
        protected override void Fallow(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("desert", at, yaw, width, depth, 0.2f, 0.85f);
        protected override float OrchardSpacing => 0.12f;
        protected override (string model, StandIn standIn, Color tint, float height) OrchardTree => (Palm, StandIn.Palm, Color.white, 0.15f);
        protected override int MaxHedge => 0;
    }
}
