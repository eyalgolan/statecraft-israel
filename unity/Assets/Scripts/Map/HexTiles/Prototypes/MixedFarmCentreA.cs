#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Prototypes
{
    /// <summary>Centre, a: narrow vegetable strips of green crops and ploughing on a grid turned about 20 degrees, a citrus
    /// block of four rows, a cypress windbreak by the sandy track. Caps: 12 citrus x ~1,600 = 19,200 (fewer by the unit
    /// spot), 4 cypresses x ~1,600 = 6,400 and the farmstead at most 2,800 = 28,400 of 30,000.</summary>
    [HexPrototype("mixed_farm_centre_a", Kind = "farm", Region = "centre")]
    public sealed class MixedFarmCentreA : MixedFarm
    {
        public override string Title => "Mixed farm, centre (a): vegetable strips, citrus, cypresses";
        protected override float YawCentre => 20f;
        protected override float YawSpread => 6f;
        protected override (float min, float max) StripWidth => (0.09f, 0.15f);
        protected override (float min, float max) ParcelLength => (0.30f, 0.60f);
        static readonly string[] Mix = { "green_crop", "ploughed", "green_crop", "wheat", "green_crop" };
        protected override string[] Crops => Mix;
        protected override int OrchardRows => 4;
        protected override Vector2 OrchardAt => new Vector2(-0.44f, 0.34f);
        protected override string TrackLayer => "beach";
        protected override int MaxHedge => 4;
        protected override int HedgeGapOneIn => 2;
        static readonly Color CypressDark = new Color(0.12f, 0.26f, 0.13f);
        protected override (string model, StandIn standIn, Color tint, float height) HedgeTree(HexTileContext ctx) => (Cypress, StandIn.Pine, CypressDark, 0.17f);
    }
}
