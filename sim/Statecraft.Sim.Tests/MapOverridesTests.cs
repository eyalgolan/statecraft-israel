using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/map/overrides.yaml, the map editor's override layer (#1009 step 1; owner rulings of 18 October 2026).</summary>
public class MapOverridesTests
{
    const string Empty = "terrain: []\ncontrol: []\nlocations: []\n";

    static readonly List<MapHex> Hexes = new List<MapHex>
    {
        new MapHex { Q = 1, R = 2, Land = true, Biome = "hills" },
        new MapHex { Q = 2, R = 2, Land = true, Biome = "desert" },
        new MapHex { Q = 3, R = 2, Land = false, Biome = "sea" },
    };

    static MapOverrides Checked(string yaml)
    {
        var o = MapOverrides.Parse(yaml);
        o.ValidateMap(Hexes, new[] { "town_a", "fort_a" });
        o.ValidateControl(new[] { (1, 2), (2, 2) }, new[] { "opening", "truce1" }, new[] { "I", "E", "c", "-" });
        return o;
    }

    [Fact]
    public void TheShippedFileIsEmptySoNothingMoves()
    {
        var o = MapOverrides.Parse(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), MapOverrides.FileName)));
        Assert.Empty(o.Terrain); Assert.Empty(o.Control); Assert.Empty(o.Locations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AMissingFileHasNoOverrides(string? yaml)
    {
        var o = MapOverrides.Parse(yaml);
        Assert.Empty(o.Terrain); Assert.Empty(o.Control); Assert.Empty(o.Locations);
    }

    [Fact]
    public void AValidFileChecks()
    {
        var o = Checked("terrain:\n  - { hex: { q: 1, r: 2 }, value: desert }\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\n"
            + "  - { hex: { q: 1, r: 2 }, period: opening, value: '-' }\nlocations:\n  - { id: fort_a, hex: { q: 2, r: 2 } }\n");
        Assert.True(o.TryLocation("fort_a", out var q, out var r));
        Assert.Equal((2, 2), (q, r));
        Assert.False(o.TryLocation("town_a", out _, out _));
    }

    [Fact]
    public void SerializeWritesTheShippedFormatInAStableOrderAndParsesBack()
    {
        // #1009 step 2: the editor's only writer. The shipped file comes back byte for byte, so an unchanged save writes nothing.
        var shipped = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), MapOverrides.FileName));
        Assert.Equal(shipped, MapOverrides.Serialize(MapOverrides.Parse(shipped)));
        var text = MapOverrides.Serialize(Checked("terrain:\n  - { hex: { q: 2, r: 2 }, value: desert }\n  - { hex: { q: 1, r: 2 }, value: hills }\n"
            + "control:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\n  - { hex: { q: 1, r: 2 }, period: opening, value: '-' }\n"
            + "locations:\n  - { id: town_a, hex: { q: 1, r: 2 } }\n  - { id: fort_a, hex: { q: 2, r: 2 } }\n"));
        Assert.Equal("# yaml-language-server: $schema=../schema/map-overrides.schema.json\n"
            + "terrain:\n  - { hex: { q: 1, r: 2 }, value: 'hills' }\n  - { hex: { q: 2, r: 2 }, value: 'desert' }\n"
            + "control:\n  - { hex: { q: 1, r: 2 }, period: 'opening', value: '-' }\n  - { hex: { q: 1, r: 2 }, period: 'truce1', value: 'E' }\n"
            + "locations:\n  - { id: 'fort_a', hex: { q: 2, r: 2 } }\n  - { id: 'town_a', hex: { q: 1, r: 2 } }\n", text);
        Assert.Equal(text, MapOverrides.Serialize(Checked(text)));
    }

    [Fact]
    public void AControlOverrideReplacesOnePeriodOfOneHexAndNothingElse()
    {
        var o = Checked("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: truce1, value: E }\nlocations: []\n");
        var periods = new[] { "opening", "truce1" };
        Assert.Equal("IE", o.ApplyControl(1, 2, periods, "II"));
        Assert.Equal("cc", o.ApplyControl(2, 2, periods, "cc"));   // another hex keeps its history
    }

    [Theory]
    [InlineData("control: []\nlocations: []\n", "terrain", "must be a list")]
    [InlineData("terrain: []\ncontrol: []\nlocations: []\nnotes: x\n", "line 4", "")]
    [InlineData("terrain:\n  - { value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "required")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: hills }\n  - { hex: { q: 1, r: 2 }, value: desert }\ncontrol: []\nlocations: []\n", "terrain[1].hex", "a second override")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: opening, value: I }\n  - { hex: { q: 1, r: 2 }, period: opening, value: E }\nlocations: []\n", "control[1].period", "a second override")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 1, r: 2 } }\n  - { id: town_a, hex: { q: 2, r: 2 } }\n", "locations[1].id", "a second override")]
    [InlineData("terrain:\n  - { hex: { q: 9, r: 9 }, value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain:\n  - { hex: { q: 3, r: 2 }, value: hills }\ncontrol: []\nlocations: []\n", "terrain[0].hex", "not a land hex")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: tundra }\ncontrol: []\nlocations: []\n", "terrain[0].value", "unknown terrain value 'tundra'")]
    [InlineData("terrain:\n  - { hex: { q: 1, r: 2 }, value: sea }\ncontrol: []\nlocations: []\n", "terrain[0].value", "unknown terrain value 'sea'")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: nowhere, hex: { q: 1, r: 2 } }\n", "locations[0].id", "unknown location id 'nowhere'")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 9, r: 9 } }\n", "locations[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain: []\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 3, r: 2 } }\n", "locations[0].hex", "not a land hex")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 9, r: 9 }, period: opening, value: I }\nlocations: []\n", "control[0].hex", "unknown hex q=9 r=9")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: later, value: I }\nlocations: []\n", "control[0].period", "unknown period 'later'")]
    [InlineData("terrain: []\ncontrol:\n  - { hex: { q: 1, r: 2 }, period: opening, value: Z }\nlocations: []\n", "control[0].value", "unknown control value 'Z'")]
    public void InvalidInputNamesTheFileAndField(string yaml, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Checked(yaml));
        Assert.Equal(MapOverrides.FileName, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // Through the content loader: the sim applies terrain and location overrides last, so every caller (the CLI, the
    // goldens, the game) sees the same map.

    [Fact]
    public void TheLoaderAppliesTerrainAndLocationOverrides()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            [MapOverrides.FileName] = "terrain:\n  - { hex: { q: 3, r: 1 }, value: hills }\ncontrol: []\nlocations:\n  - { id: town_a, hex: { q: 2, r: 1 } }\n",
        }));
        Assert.Equal("hills", c.HexAt(3, 1)!.Biome);
        Assert.Equal("coastal_plain", c.HexAt(1, 0)!.Biome);   // every other hex as generated
        var s = c.SettlementHexes.Single(h => h.Id == "town_a");
        Assert.Equal((2, 1), (s.Q, s.R));
        var l = c.MapLocations.Single(x => x.Id == "town_a");
        Assert.Equal((2, 1), (l.Q, l.R));
        Assert.Equal((34.29, 33.35), (l.Lon, l.Lat));   // the sourced coordinates stay
    }

    [Fact]
    public void WithoutTheFileTheLoaderMovesNothing()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal("desert", c.HexAt(3, 1)!.Biome);
        var s = c.SettlementHexes.Single(h => h.Id == "town_a");
        Assert.Equal((1, 0), (s.Q, s.R));
    }

    [Fact]
    public void TheLoaderRejectsAnUnknownLocationNamingTheOverrideFile()
    {
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            [MapOverrides.FileName] = "terrain: []\ncontrol: []\nlocations:\n  - { id: nowhere, hex: { q: 2, r: 1 } }\n",
        })));
        Assert.Equal(MapOverrides.FileName, ex.File);
        Assert.Equal("locations[0].id", ex.Field);
    }

    [Fact]
    public void TheEmptyFileIsInTheContentHashAndChangesNothingElse()
    {
        var without = ContentLoader.Load(Fixture.Write());
        var with = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { [MapOverrides.FileName] = Empty }));
        Assert.NotEqual(without.Hash, with.Hash);
        Assert.Equal(without.Hexes.Select(h => h.Biome), with.Hexes.Select(h => h.Biome));
        Assert.Equal(without.SettlementHexes.Select(h => (h.Id, h.Q, h.R)), with.SettlementHexes.Select(h => (h.Id, h.Q, h.R)));
    }
}
