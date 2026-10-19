#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, a: an acacia grove on sand, the trees wide apart and loosely set, a few date palms among them, desert
    /// rock in bare clearings. Caps: 18 acacias x ~1,160 = 20,880, 10 palms x ~280 = 2,800, 9 margin acacias x ~1,160 =
    /// 10,440 and 3 rocks x ~700 = 2,100 = 36,220 of 60,000.</summary>
    [HexPrototype("forest_south_a", Kind = "forest", Region = "south")]
    public sealed class ForestSouthA : PineForest
    {
        public override string Title => "Forest, south (a): acacia grove";
        public override HexTerrain Terrain => HexTerrain.Flat;
        public override string Ground => "desert";
        const string Acacia = "sandbox:acacia_lite", Palm = "sandbox:date_palm";
        static readonly Color AcaciaLeaf = new Color(0.45f, 0.47f, 0.28f);
        protected override int MaxPines => 18;
        protected override int MaxFill => 10;
        protected override Tree StandTree(HexTileContext ctx) => new Tree(Acacia, StandIn.Tree, AcaciaLeaf, 0.12f, 0.15f, 0.5f);
        protected override Tree FillTree(HexTileContext ctx) => new Tree(Palm, StandIn.Palm, Color.white, 0.13f, 0.16f, 0.3f);
        protected override Tree MarginTree(HexTileContext ctx) => new Tree(Acacia, StandIn.Tree, AcaciaLeaf, 0.11f, 0.13f, 0.5f);
        protected override (float min, float max) RowYaw => (0f, 90f);
        protected override float RowGap => 0.21f;
        protected override float TreeGap => 0.22f;
        protected override float Jitter => 0.06f;
        protected override string ClearingLayer => "desert";
        protected override string? OutcropModel => "sandbox:desert_rock*";
    }
}
