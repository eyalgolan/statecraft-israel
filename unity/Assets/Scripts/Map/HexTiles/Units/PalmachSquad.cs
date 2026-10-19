#nullable enable

namespace Statecraft.HexTiles.Units
{
    /// <summary>A Palmach squad, 1948: eight Palmach scouts (c12f) in a wedge (HexUnitTypes.Squad; one character type per
    /// unit, Tal, 19 October 2026). Drawn in the editor as the Collection's whole c12f figure (HexModels.FigureAliases).
    /// Only the characters: the land under them stays the tile's (Tal's unit rules).</summary>
    [HexUnitType("palmach_squad")]
    public sealed class PalmachSquad : IHexUnitType
    {
        public string Title => "Palmach squad (1948)";
        public HexUnit Create() => HexUnitTypes.Squad(Title, "proto:figure-palmach-scout-c12f-1948-v2");
    }
}
