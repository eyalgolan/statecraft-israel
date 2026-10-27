#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, b: open Shephelah woodland, carobs and olives widely spaced on dry grass with large clearings, rows
    /// turned -70 to -50 degrees. Caps: 24 carobs x ~650 = 15,600, 8 olives x ~1,600 = 12,800, 9 margin trees x ~650 =
    /// 5,850 and 3 rocks x ~700 = 2,100 = 36,350 of 60,000.</summary>
    [HexPrototype("forest_centre_b", Kind = "forest", Region = "centre")]
    public sealed class ForestCentreB : PineForest
    {
        public override string Title => "Forest, centre (b): open carob woodland";
        protected override int MaxPines => 24;
        protected override int MaxFill => 8;
        static readonly Color OliveLeaf = new Color(0.38f, 0.44f, 0.28f);
        protected override Tree StandTree(HexTileContext ctx) => new Tree(Carob, StandIn.Tree, CarobGreen, 0.13f, 0.16f, 0.45f);
        protected override Tree FillTree(HexTileContext ctx) => new Tree("sandbox:olive_lite", StandIn.Tree, OliveLeaf, 0.12f, 0.14f, 0.5f);
        protected override (float min, float max) RowYaw => (-70f, -50f);
        protected override float RowGap => 0.19f;
        protected override float TreeGap => 0.20f;
        protected override float Jitter => 0.05f;
        protected override (float big, float small) ClearingRadii => (0.20f, 0.15f);
        protected override string ClearingLayer => "plains";
    }
}
