#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, b: a date-palm grove in close rows on green ground, small garden plots in its clearings, acacias round
    /// the edge. Caps: 44 palms x ~280 = 12,320, 6 acacias x ~1,160 = 6,960, 9 margin acacias x ~1,160 = 10,440 and 3
    /// rocks x ~700 = 2,100 = 31,820 of 60,000.</summary>
    [HexPrototype("forest_south_b", Kind = "forest", Region = "south")]
    public sealed class ForestSouthB : PineForest
    {
        public override string Title => "Forest, south (b): date-palm grove";
        public override HexTerrain Terrain => HexTerrain.Flat;
        public override string Ground => "desert";
        const string Acacia = "sandbox:acacia_lite", Palm = "sandbox:date_palm";
        static readonly Color AcaciaLeaf = new Color(0.45f, 0.47f, 0.28f);
        protected override int MaxPines => 44;
        protected override int MaxFill => 6;
        protected override Tree StandTree(HexTileContext ctx) => new Tree(Palm, StandIn.Palm, Color.white, 0.14f, 0.18f, 0.3f);
        protected override Tree FillTree(HexTileContext ctx) => new Tree(Acacia, StandIn.Tree, AcaciaLeaf, 0.12f, 0.14f, 0.5f);
        protected override Tree MarginTree(HexTileContext ctx) => new Tree(Acacia, StandIn.Tree, AcaciaLeaf, 0.12f, 0.14f, 0.5f);
        protected override (float min, float max) RowYaw => (-10f, 10f);
        protected override float RowGap => 0.13f;
        protected override float TreeGap => 0.13f;
        protected override float Jitter => 0.012f;
        protected override (float big, float small) ClearingRadii => (0.13f, 0.10f);
        protected override string ClearingLayer => "green_crop";
        protected override string? OutcropModel => null;
    }
}
