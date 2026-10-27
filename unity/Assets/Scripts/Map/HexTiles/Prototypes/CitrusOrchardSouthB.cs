#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, b: two small citrus blocks and a block of date palms on a grid turned about -30 degrees, ploughed fields
    /// between, and no windbreak. Caps: at most 12 citrus x ~1,600 = 19,200, 9 palms x ~280 = 2,520, truck ~7,000 and
    /// the house at most 1,400 = 30,120 of 60,000.</summary>
    [HexPrototype("citrus_orchard_south_b", Kind = "farm", Region = "south")]
    public sealed class CitrusOrchardSouthB : CitrusOrchard
    {
        public override string Title => "Citrus groves, south (b): small blocks and date palms";
        protected override float GridYaw(HexTileContext ctx) => -30f;
        static readonly Block[] Layout = { new Block(-0.44f, 0.20f, 3, 2), new Block(0.20f, 0.24f, 3, 2), new Block(-0.50f, -0.40f, 3, 3) };
        protected override Block[] Blocks => Layout;
        protected override (string model, StandIn standIn, Color? tint)? GroveTree(int b, int i, int j) =>
            b == 2 ? ("sandbox:date_palm", StandIn.Palm, null) : base.GroveTree(b, i, j);
        protected override void Windbreaks(HexTileContext ctx, float gridYaw) { }
        protected override void Dress(HexTileContext ctx, float gridYaw)
        {
            ctx.PaintGround("ploughed", Grid(ctx, gridYaw, 0.45f, -0.05f), gridYaw, 0.32f * ctx.S, 0.22f * ctx.S, 0.12f, 0.85f);
            ctx.PaintGround("wheat", Grid(ctx, gridYaw, -0.05f, 0.62f), gridYaw + 90f, 0.22f * ctx.S, 0.50f * ctx.S, 0.12f, 0.85f);
        }
    }
}
