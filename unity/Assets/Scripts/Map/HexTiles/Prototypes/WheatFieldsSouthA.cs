#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, a: dry farming, large square fields of wheat and fresh ploughing with many left fallow as bare sand, no
    /// hedgerows, and a few date palms by the farmstead. Caps: 4 palms x ~280 + the farmstead at most 2,800 = 3,920 of
    /// 30,000.</summary>
    [HexPrototype("wheat_fields_south_a", Kind = "farm", Region = "south")]
    public sealed class WheatFieldsSouthA : WheatFields
    {
        public override string Title => "Wheat fields, south (a): dry wheat and fallow, palms by the farm";
        protected override float YawCentre => 25f;
        protected override float YawSpread => 10f;
        protected override float CellAcross => 0.46f;
        static readonly string[] Mix = { "wheat", "wheat", "wheat", "ploughed", "ploughed" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 3;
        protected override void Pasture(HexTileContext ctx, Vector3 at, float yaw, float width, float depth) =>
            ctx.PaintGround("desert", at, yaw, width, depth, 0.2f, 0.85f);
        static readonly float[] Lines = { };
        protected override float[] HedgeLines => Lines;
        protected override void Dress(HexTileContext ctx, Quaternion turn, float yaw)
        {
            float s = ctx.S;
            var yard = ctx.Centre + turn * new Vector3(0.48f * s, 0f, -0.18f * s);
            for (int k = 0; k < 4; k++)
            {
                var at = yard + turn * new Vector3((-0.12f + 0.08f * k) * s, 0f, -0.16f * s);
                float ty = ctx.Range(0f, 360f), h = ctx.Range(0.13f, 0.16f) * s;
                if (!ctx.InUnitSpot(at, 0.04f * s)) ctx.PlaceByHeight("sandbox:date_palm", at, ty, h, StandIn.Palm, null, 0.3f);
            }
        }
    }
}
