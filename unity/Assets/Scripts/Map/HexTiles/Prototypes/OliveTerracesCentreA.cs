#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, a: thin pale terraces of bare earth on the Shephelah slopes, olives spaced wider along them, carobs
    /// more common. Caps: at most 22 olives x ~1,600 = 35,200, 6 carobs x ~650 = 3,900, 12 limestone x ~700 = 8,400 and
    /// the hut ~700 = 48,200 of 50,000.</summary>
    [HexPrototype("olive_terraces_centre_a", Kind = "farm", Region = "centre")]
    public sealed class OliveTerracesCentreA : OliveTerraces
    {
        public override string Title => "Olive terraces, centre (a): thin pale terraces, carobs";
        protected override string BandLayer => "dirt";
        protected override float BandWidth => 0.08f;
        protected override int MiddleRow => 11;
        protected override int UpperRow => 7;
        protected override int MaxCarobs => 6;
        protected override int CarobRolls => 3;
    }
}
