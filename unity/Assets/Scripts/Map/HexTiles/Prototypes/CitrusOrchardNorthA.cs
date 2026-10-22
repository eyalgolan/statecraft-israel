#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, a: three dense citrus blocks on a grid turned about -25 degrees, a green-crop field beside the packing
    /// house, and an oak and pine windbreak along the north edge. Caps: at most 24 citrus x ~1,600 = 38,400, 6
    /// windbreak trees at most 9,600, truck ~7,000 and the house at most 1,400 = 56,400 of 60,000.</summary>
    [HexPrototype("citrus_orchard_north_a", Kind = "farm", Region = "north")]
    public sealed class CitrusOrchardNorthA : CitrusOrchard
    {
        public override string Title => "Citrus groves, north (a): dense blocks, oak and pine windbreak";
        protected override float GridYaw(HexTileContext ctx) => -25f;
        static readonly Block[] Layout = { new Block(-0.44f, 0.18f, 3, 3), new Block(0.18f, 0.18f, 3, 3), new Block(-0.44f, -0.33f, 3, 2) };
        protected override Block[] Blocks => Layout;
        static readonly Color OakLeaf = new Color(0.30f, 0.42f, 0.20f), PineGreen = new Color(0.20f, 0.33f, 0.18f);
        protected override void Windbreaks(HexTileContext ctx, float gridYaw)
        {
            for (int k = 0; k < 6; k++)
            {
                bool pine = k % 2 == 0;
                ctx.PlaceByHeight(pine ? "sandbox:aleppo_pine_lite" : "sandbox:tabor_oak_lite", Grid(ctx, gridYaw, -0.46f + 0.13f * k, 0.56f), ctx.Range(0f, 360f), ctx.Range(0.15f, 0.18f) * ctx.S, pine ? StandIn.Pine : StandIn.Tree, pine ? PineGreen : OakLeaf, 0.35f);
            }
        }
        protected override void Dress(HexTileContext ctx, float gridYaw) =>
            ctx.PaintGround("green_crop", Grid(ctx, gridYaw, 0.42f, -0.40f), gridYaw + 90f, 0.30f * ctx.S, 0.22f * ctx.S, 0.12f, 0.9f);
    }
}
