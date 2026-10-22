#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, a: terraces sown in green crops between the olive rows, more trees to a ring, and Tabor oaks among the
    /// olives. Caps: at most 22 olives x ~1,600 = 35,200, 6 oaks x ~650 = 3,900, 12 limestone x ~700 = 8,400 and the
    /// hut ~700 = 48,200 of 50,000.</summary>
    [HexPrototype("olive_terraces_north_a", Kind = "farm", Region = "north")]
    public sealed class OliveTerracesNorthA : OliveTerraces
    {
        public override string Title => "Olive terraces, north (a): green terraces, oaks";
        protected override string BandLayer => "green_crop";
        protected override int MiddleRow => 15;
        protected override int UpperRow => 10;
        protected override int MaxCarobs => 6;
        protected override int CarobRolls => 2;
        static readonly Color OakLeaf = new Color(0.30f, 0.42f, 0.20f);
        protected override (string model, StandIn standIn, Color tint, float min, float max) SecondTree => ("sandbox:tabor_oak_lite", StandIn.Tree, OakLeaf, 0.14f, 0.17f);
    }
}
