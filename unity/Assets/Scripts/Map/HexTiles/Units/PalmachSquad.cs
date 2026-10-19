#nullable enable

namespace Statecraft.HexTiles.Units
{
    /// <summary>A Palmach squad, 1948: eight fighters in a wedge (HexUnitTypes.Squad), the 13 October Tripo soldiers with
    /// walk and idle clips. Only the characters: the land under them stays the tile's (Tal's unit rules).</summary>
    [HexUnitType("palmach_squad")]
    public sealed class PalmachSquad : IHexUnitType
    {
        public string Title => "Palmach squad (1948)";
        public HexUnit Create() => HexUnitTypes.Squad(Title,
            "proto:figure-palmach-scout-c12f-1948-v2", "proto:figure-veteran-fighter-c07-1948-v2", "proto:figure-young-recruit-c06-1948-v2");
    }
}
