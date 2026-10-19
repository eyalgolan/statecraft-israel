#nullable enable
using UnityEngine;

namespace Statecraft.HexTiles.Units
{
    /// <summary>One-vehicle units (Tal's rule: a vehicle unit is one vehicle, and no road under it). Drawn at
    /// HexUnitScale.VehicleLength; the 13 October Tripo vehicles face +X, hence the -90 degree turn.</summary>
    [HexUnitType("sandwich_truck")]
    public sealed class SandwichTruck : IHexUnitType
    {
        public string Title => "Sandwich armoured truck (1948)";
        public HexUnit Create() => One(Title, "proto:figure-vehicle-sandwich-convoy-1948-v2");

        public static HexUnit One(string title, string model, float lengthShare = HexUnitScale.VehicleLength) => new HexUnit
        {
            Title = title,
            Members = { new HexUnitMember { Model = model, StandIn = StandIn.Box, Offset = Vector2.zero, HeightShare = lengthShare, CycleShare = 0.12f, ByWidth = true, YawOffset = -90f } },
        };
    }

    [HexUnitType("willys_jeep")]
    public sealed class WillysJeep : IHexUnitType
    {
        public string Title => "Willys jeep (1948)";
        public HexUnit Create() => SandwichTruck.One(Title, "proto:figure-vehicle-willys-jeep-1948-v2", 0.36f);
    }
}
