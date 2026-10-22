#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, b: terraces sown in wheat, a thin row of olives on the middle terrace only and bare corners. Caps: at
    /// most 12 olives x ~1,600 = 19,200, 4 carobs x ~650 = 2,600, 12 limestone x ~700 = 8,400 and the hut ~700 = 30,900
    /// of 50,000.</summary>
    [HexPrototype("olive_terraces_south_b", Kind = "farm", Region = "south")]
    public sealed class OliveTerracesSouthB : OliveTerraces
    {
        public override string Title => "Olive terraces, south (b): wheat terraces, few olives";
        protected override string BandLayer => "wheat";
        protected override int MiddleRow => 9;
        protected override int UpperRow => 5;
        protected override int OuterCorners => 0;
        protected override int MaxOlives => 12;
        protected override int GapRolls => 2;
    }
}
