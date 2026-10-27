#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>South, a: dry terraces with sandy risers, few olives with wide gaps, and acacias among them. Caps: at most 14
    /// olives x ~1,600 = 22,400, 4 acacias x ~1,160 = 4,640, 12 limestone x ~700 = 8,400 and the hut ~700 = 36,140 of
    /// 50,000.</summary>
    [HexPrototype("olive_terraces_south_a", Kind = "farm", Region = "south")]
    public sealed class OliveTerracesSouthA : OliveTerraces
    {
        public override string Title => "Olive terraces, south (a): dry terraces, acacias";
        protected override string RiserLayer => "desert";
        protected override int MiddleRow => 10;
        protected override int UpperRow => 7;
        protected override int MaxOlives => 14;
        protected override int GapRolls => 3;
        static readonly Color AcaciaLeaf = new Color(0.45f, 0.47f, 0.28f);
        protected override (string model, StandIn standIn, Color tint, float min, float max) SecondTree => ("sandbox:acacia_lite", StandIn.Tree, AcaciaLeaf, 0.12f, 0.14f);
    }
}
