#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, b: a dense Aleppo pine wood in tight rows turned -40 to -20 degrees, small clearings, and oaks filling
    /// the gaps. Caps: 26 pines x ~1,600 = 41,600, 10 oaks x ~650 = 6,500, 9 margin trees x ~650 = 5,850 and 3 rocks x
    /// ~700 = 2,100 = 56,050 of 60,000.</summary>
    [HexPrototype("forest_north_b", Kind = "forest", Region = "north")]
    public sealed class ForestNorthB : PineForest
    {
        public override string Title => "Forest, north (b): dense pine wood";
        protected override int MaxPines => 26;
        protected override int MaxFill => 10;
        protected override (float min, float max) RowYaw => (-40f, -20f);
        protected override float RowGap => 0.14f;
        protected override float TreeGap => 0.14f;
        protected override float Jitter => 0.02f;
        protected override (float big, float small) ClearingRadii => (0.11f, 0.08f);
        protected override string ClearingLayer => "grass";
    }
}
