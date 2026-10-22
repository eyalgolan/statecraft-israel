#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, b: broad parcels of wheat and green crops on sand and grass, a grid turned about -45 degrees, a citrus
    /// block and a row of date palms by the farmstead, no hedgerow. Caps: 6 citrus x ~1,600 = 9,600, 5 palms x ~280 =
    /// 1,400 and the farmstead at most 2,800 = 13,800 of 30,000.</summary>
    [HexPrototype("mixed_farm_centre_b", Kind = "farm", Region = "centre")]
    public sealed class MixedFarmCentreB : MixedFarm
    {
        public override string Title => "Mixed farm, centre (b): broad parcels, palms by the yard";
        protected override float YawCentre => -45f;
        protected override float YawSpread => 6f;
        protected override (float min, float max) StripWidth => (0.28f, 0.42f);
        protected override (float min, float max) ParcelLength => (0.22f, 0.40f);
        static readonly string[] Mix = { "wheat", "green_crop", "wheat", "ploughed", "beach" };
        protected override string[] Crops => Mix;
        protected override int OrchardRows => 2;
        protected override Vector2 OrchardAt => new Vector2(-0.30f, -0.48f);
        protected override int MaxHedge => 0;
        protected override void Dress(HexTileContext ctx, float yaw)
        {
            var y = YardAt;
            for (int k = 0; k < 5; k++)
            {
                var at = Grid(ctx, yaw, y.x - 0.16f + 0.08f * k, y.y + 0.17f);
                float ty = ctx.Range(0f, 360f), h = ctx.Range(0.14f, 0.17f) * ctx.S;
                if (!ctx.InUnitSpot(at, 0.04f * ctx.S)) ctx.PlaceByHeight(Palm, at, ty, h, StandIn.Palm, null, 0.3f);
            }
        }
    }
}
