#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>North, b: four narrower ploughed terraces, the trees in denser rows, with Aleppo pines among the olives. Caps:
    /// at most 24 olives x ~1,600 = 38,400, 4 pines x ~1,600 = 6,400, 12 limestone x ~700 = 8,400 and the hut ~700 =
    /// 53,900; budget 70,000 (the farmhouse is drawn at full detail).</summary>
    [HexPrototype("olive_terraces_north_b", Kind = "farm", Region = "north")]
    public sealed class OliveTerracesNorthB : OliveTerraces
    {
        public override string Title => "Olive terraces, north (b): narrow terraces, pines";
        public override int TriangleBudget => 70000;   // 59k measured with the farmhouse at full detail
        static readonly float[] Narrow = { 0.40f, 0.54f, 0.68f, 0.82f };
        static readonly float[] NarrowRisers = { 0.33f, 0.47f, 0.61f, 0.75f, 0.89f };
        protected override float[] Bands => Narrow;
        protected override float[] Risers => NarrowRisers;
        protected override float BandWidth => 0.085f;
        protected override string BandLayer => "ploughed";
        protected override int MiddleRow => 16;
        protected override int UpperRow => 11;
        protected override int OuterCorners => 12;
        protected override int MaxOlives => 24;
        static readonly Color PineGreen = new Color(0.20f, 0.33f, 0.18f);
        protected override (string model, StandIn standIn, Color tint, float min, float max) SecondTree => ("sandbox:aleppo_pine_lite", StandIn.Pine, PineGreen, 0.16f, 0.19f);
    }
}
