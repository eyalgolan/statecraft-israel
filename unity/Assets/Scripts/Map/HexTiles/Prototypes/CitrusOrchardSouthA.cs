#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, a: a sparse young grove, every other tree missing so the blocks show as a chequer on pale soil, an acacia
    /// windbreak, and bare sand around the blocks. Caps: at most 11 citrus x ~1,600 = 17,600, 6 acacias x ~1,160 =
    /// 6,960, truck ~7,000 and the house at most 1,400 = 32,960 of 60,000.</summary>
    [HexPrototype("citrus_orchard_south_a", Kind = "farm", Region = "south")]
    public sealed class CitrusOrchardSouthA : CitrusOrchard
    {
        public override string Title => "Citrus groves, south (a): sparse young grove, acacias";
        protected override float GridYaw(HexTileContext ctx) => 0f;
        protected override string BlockSoil => "dirt";
        protected override (string model, StandIn standIn, Color? tint)? GroveTree(int b, int i, int j) =>
            (i + j) % 2 == 1 ? null : base.GroveTree(b, i, j);
        static readonly Color AcaciaLeaf = new Color(0.45f, 0.47f, 0.28f);
        protected override void Windbreaks(HexTileContext ctx, float gridYaw)
        {
            for (int k = 0; k < 3; k++)
            {
                ctx.PlaceByHeight("sandbox:acacia_lite", Grid(ctx, gridYaw, -0.41f + 0.14f * k, 0.58f), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * ctx.S, StandIn.Tree, AcaciaLeaf, 0.5f);
                ctx.PlaceByHeight("sandbox:acacia_lite", Grid(ctx, gridYaw, -0.60f, -0.38f + 0.14f * k), ctx.Range(0f, 360f), ctx.Range(0.12f, 0.14f) * ctx.S, StandIn.Tree, AcaciaLeaf, 0.5f);
            }
        }
        protected override void Dress(HexTileContext ctx, float gridYaw)
        {
            ctx.PaintGround("desert", Grid(ctx, gridYaw, 0.48f, -0.12f), gridYaw, 0.26f * ctx.S, 0.20f * ctx.S, 0.3f, 0.7f);
            ctx.PaintGround("desert", Grid(ctx, gridYaw, -0.10f, -0.70f), gridYaw, 0.40f * ctx.S, 0.20f * ctx.S, 0.3f, 0.7f);
        }
    }
}
