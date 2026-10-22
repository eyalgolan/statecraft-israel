#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, a: large square fields of wheat and fresh ploughing on the coastal plain, the grid turned about 10
    /// degrees, a sandy farm track, and a single cypress windbreak. Caps: one line of at most 9 cypresses x ~1,600 =
    /// 14,400 + the farmstead at most 2,800 = 17,200 of 30,000.</summary>
    [HexPrototype("wheat_fields_centre_a", Kind = "farm", Region = "centre")]
    public sealed class WheatFieldsCentreA : WheatFields
    {
        public override string Title => "Wheat fields, centre (a): large fields, cypress windbreak";
        protected override float YawCentre => 10f;
        protected override float YawSpread => 6f;
        protected override float CellAcross => 0.44f;
        static readonly string[] Mix = { "wheat", "wheat", "ploughed", "wheat", "green_crop" };
        protected override string[] Crops => Mix;
        protected override int PastureOneIn => 9;
        static readonly float[] Lines = { -1.0f };
        protected override float[] HedgeLines => Lines;
        protected override int HedgeGapOneIn => 4;
        protected override string TrackLayer => "beach";
        static readonly Color CypressLeaf = new Color(0.12f, 0.26f, 0.13f);
        protected override (string model, StandIn standIn, Color? tint, float height) HedgeTree(HexTileContext ctx) =>
            ("sandbox:cypress_lite", StandIn.Pine, CypressLeaf, 0.17f);
    }
}
