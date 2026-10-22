#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, a: an oak woodland, Tabor oaks closing the canopy with a few Aleppo pines and carobs at the margins, its
    /// rows turned 60 to 80 degrees. Caps: 30 oaks x ~650 = 19,500, 6 pines x ~1,600 = 9,600, 9 margin trees x ~650 =
    /// 5,850 and 3 rocks x ~700 = 2,100 = 37,050 of 60,000.</summary>
    [HexPrototype("forest_north_a", Kind = "forest", Region = "north")]
    public sealed class ForestNorthA : PineForest
    {
        public override string Title => "Forest, north (a): oak woodland";
        protected override int MaxPines => 30;
        protected override int MaxFill => 6;
        protected override Tree StandTree(HexTileContext ctx) => new Tree(Oak, StandIn.Tree, OakGreen, 0.14f, 0.18f, 0.45f);
        protected override Tree FillTree(HexTileContext ctx) => new Tree(Pine, StandIn.Pine, PineGreen, 0.17f, 0.20f, 0.3f);
        protected override Tree MarginTree(HexTileContext ctx) => new Tree(Carob, StandIn.Tree, CarobGreen, 0.13f, 0.16f, 0.45f);
        protected override (float min, float max) RowYaw => (60f, 80f);
        protected override float RowGap => 0.15f;
        protected override float TreeGap => 0.15f;
        protected override float Jitter => 0.045f;
        protected override string ClearingLayer => "grass";
    }
}
