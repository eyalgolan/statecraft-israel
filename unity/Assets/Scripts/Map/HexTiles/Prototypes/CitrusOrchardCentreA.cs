#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, a: three citrus blocks on the coastal plain, the north-east one enlarged, on a grid turned about 35
    /// degrees, sandy tracks between them, and cypress windbreaks along the west and north edges. Caps: at most 24
    /// citrus x ~1,600 = 38,400, 6 cypresses x ~1,600 = 9,600, truck ~7,000 and the house at most 1,400 = 56,400 of
    /// 60,000.</summary>
    [HexPrototype("citrus_orchard_centre_a", Kind = "farm", Region = "centre")]
    public sealed class CitrusOrchardCentreA : CitrusOrchard
    {
        public override string Title => "Citrus groves, centre (a): four blocks, sandy tracks";
        protected override float GridYaw(HexTileContext ctx) => 35f;
        static readonly Block[] Layout = { new Block(-0.44f, 0.18f, 3, 3), new Block(0.18f, 0.18f, 3, 3), new Block(-0.44f, -0.33f, 3, 2) };
        protected override Block[] Blocks => Layout;
        protected override void Tracks(HexTileContext ctx, float gridYaw)
        {
            float s = ctx.S;
            ctx.PaintStroke("beach", Grid(ctx, gridYaw, 0f, -0.95f), Grid(ctx, gridYaw, 0f, 0.95f), 0.05f * s, 0.9f);
            ctx.PaintStroke("beach", Grid(ctx, gridYaw, -0.95f, 0f), Grid(ctx, gridYaw, 0.95f, 0f), 0.045f * s, 0.9f);
        }
    }
}
