#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, b: three broad ploughed terraces with no field hut, the olives close together in full rows. Caps: at
    /// most 26 olives x ~1,600 = 41,600, 4 carobs x ~650 = 2,600 and 12 limestone x ~700 = 8,400 = 52,600; budget
    /// 60,000.</summary>
    [HexPrototype("olive_terraces_centre_b", Kind = "farm", Region = "centre")]
    public sealed class OliveTerracesCentreB : OliveTerraces
    {
        public override string Title => "Olive terraces, centre (b): broad terraces, full rows";
        public override int TriangleBudget => 60000;
        static readonly float[] Broad = { 0.45f, 0.65f, 0.83f };
        static readonly float[] BroadRisers = { 0.34f, 0.55f, 0.75f, 0.92f };
        protected override float[] Bands => Broad;
        protected override float[] Risers => BroadRisers;
        protected override float BandWidth => 0.16f;
        protected override bool HasHut => false;
        protected override int MiddleRow => 18;
        protected override int UpperRow => 12;
        protected override int MaxOlives => 26;
        protected override int GapRolls => 0;
    }
}
