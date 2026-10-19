#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, a: a planted wood of Aleppo pines and cypresses mixed, rows turned 50 to 70 degrees, carobs filling the
    /// canopy, grassy clearings. Caps: 20 conifers x ~1,600 = 32,000, 26 carobs x ~650 = 16,900, 9 margin trees x ~650
    /// = 5,850 and 3 rocks x ~700 = 2,100 = 56,850 of 60,000.</summary>
    [HexPrototype("forest_centre_a", Kind = "forest", Region = "centre")]
    public sealed class ForestCentreA : PineForest
    {
        public override string Title => "Forest, centre (a): pine and cypress";
        protected override int MaxPines => 20;
        static readonly Color CypressDark = new Color(0.12f, 0.26f, 0.13f);
        protected override Tree StandTree(HexTileContext ctx) =>
            ctx.Rng.Next(3) == 0 ? new Tree("sandbox:cypress_lite", StandIn.Pine, CypressDark, 0.17f, 0.20f, 0.3f) : base.StandTree(ctx);
        protected override Tree FillTree(HexTileContext ctx) => new Tree(Carob, StandIn.Tree, CarobGreen, 0.13f, 0.15f, 0.45f);
        protected override (float min, float max) RowYaw => (50f, 70f);
        protected override float TrackBearing => 20f;
        protected override string ClearingLayer => "grass";
    }
}
