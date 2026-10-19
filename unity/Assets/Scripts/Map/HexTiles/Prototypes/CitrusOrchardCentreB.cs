#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, b: three wide-spaced citrus blocks on a grid turned about -40 degrees, a row of date palms down the main
    /// track, and a cypress windbreak along the north edge only. Caps: at most 15 citrus x ~1,600 = 24,000, 3 cypresses
    /// x ~1,600 = 4,800, 12 palms x ~280 = 3,360, truck ~7,000 and the house at most 1,400 = 40,560 of 60,000.</summary>
    [HexPrototype("citrus_orchard_centre_b", Kind = "farm", Region = "centre")]
    public sealed class CitrusOrchardCentreB : CitrusOrchard
    {
        public override string Title => "Citrus groves, centre (b): wide rows, palm-lined track";
        protected override float GridYaw(HexTileContext ctx) => -40f;
        protected override float Row => 0.15f;
        static readonly Block[] Layout = { new Block(-0.50f, 0.20f, 3, 2), new Block(0.20f, 0.22f, 3, 2), new Block(-0.50f, -0.42f, 2, 2) };
        protected override Block[] Blocks => Layout;
        static readonly Color CypressDark = new Color(0.12f, 0.26f, 0.13f);
        protected override void Windbreaks(HexTileContext ctx, float gridYaw)
        {
            for (int k = 0; k < 3; k++)
                ctx.PlaceByHeight(Cypress, Grid(ctx, gridYaw, -0.47f + 0.15f * k, 0.48f), ctx.Range(0f, 360f), ctx.Range(0.17f, 0.19f) * ctx.S, StandIn.Pine, CypressDark, 0.3f);
        }
        protected override void Dress(HexTileContext ctx, float gridYaw)
        {
            for (int k = 0; k < 12; k++)
            {
                float z = -0.85f + 0.155f * k;
                var at = Grid(ctx, gridYaw, 0.06f, z);
                if (ctx.InUnitSpot(at, 0.04f * ctx.S) || Mathf.Abs(z + 0.45f) < 0.12f) continue;
                ctx.PlaceByHeight("sandbox:date_palm", at, ctx.Range(0f, 360f), ctx.Range(0.14f, 0.17f) * ctx.S, StandIn.Palm, null, 0.3f);
            }
        }
    }
}
