#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, b: two long citrus blocks in tight rows on a grid turned about 50 degrees, the eastern half in green
    /// crops, and a carob windbreak along the west edge. Caps: at most 20 citrus x ~1,600 = 32,000, 5 carobs x ~650 =
    /// 3,250, truck ~7,000 and the house at most 1,400 = 43,650 of 60,000.</summary>
    [HexPrototype("citrus_orchard_north_b", Kind = "farm", Region = "north")]
    public sealed class CitrusOrchardNorthB : CitrusOrchard
    {
        public override string Title => "Citrus groves, north (b): long blocks, green fields, carobs";
        protected override float GridYaw(HexTileContext ctx) => 50f;
        protected override float Row => 0.11f;
        static readonly Block[] Layout = { new Block(-0.50f, 0.14f, 4, 3), new Block(-0.50f, -0.38f, 4, 2) };
        protected override Block[] Blocks => Layout;
        static readonly Color CarobLeaf = new Color(0.27f, 0.39f, 0.20f);
        protected override void Windbreaks(HexTileContext ctx, float gridYaw)
        {
            for (int k = 0; k < 5; k++)
                ctx.PlaceByHeight("sandbox:carob_lite", Grid(ctx, gridYaw, -0.64f, -0.40f + 0.16f * k), ctx.Range(0f, 360f), ctx.Range(0.13f, 0.15f) * ctx.S, StandIn.Tree, CarobLeaf, 0.45f);
        }
        protected override void Dress(HexTileContext ctx, float gridYaw)
        {
            ctx.PaintGround("green_crop", Grid(ctx, gridYaw, 0.48f, 0.30f), gridYaw, 0.30f * ctx.S, 0.40f * ctx.S, 0.1f, 0.9f);
            ctx.PaintGround("green_crop", Grid(ctx, gridYaw, 0.50f, -0.10f), gridYaw + 90f, 0.26f * ctx.S, 0.22f * ctx.S, 0.1f, 0.9f);
        }
    }
}
