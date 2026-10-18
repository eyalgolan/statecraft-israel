using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>content/map/hexes.json and settlement_hexes.yaml as the sim loads them.</summary>
public class MapContentTests
{
    [Fact]
    public void TheFixtureMapLoads()
    {
        var c = ContentLoader.Load(Fixture.Write());
        Assert.Equal(9, c.Hexes.Count);
        Assert.False(c.HexAt(0, 0)!.Land);
        Assert.Equal("hills", c.HexAt(2, 1)!.Biome);
        Assert.Null(c.HexAt(9, 9));
        Assert.Equal(("town_a", 1, 0), (c.SettlementHexes[0].Id, c.SettlementHexes[0].Q, c.SettlementHexes[0].R));
        Assert.Equal(("town_a", "city", "must_have"), (c.MapLocations[0].Id, c.MapLocations[0].Kind, c.MapLocations[0].Priority));
    }

    [Theory]
    [InlineData("kind: city", "kind: citty", "locations[town_a].kind", "unknown kind")]
    [InlineData("priority: must_have", "priority: urgent", "locations[town_a].priority", "unknown priority")]
    [InlineData("    name_he: עיר א\n", "", "locations[town_a].name_he", "required")]
    [InlineData("    name_he_sources: [{ url: 'https://example.org/town-a-he', checked: 2026-10-18 }]\n", "", "locations[town_a].name_he_sources", "needs a source")]
    [InlineData("    name_he: עיר א\n", "    name_he: עיר א\n    name_he_review: checked\n", "locations[town_a].name_he_review", "not agent-checked")]
    [InlineData("q: 1", "q: 9", "locations[town_a].q", "not a land hex")]
    [InlineData("    sources: [{ url: 'https://example.org/town-a', checked: 2026-10-18 }]\n", "", "locations[town_a].sources", "required")]
    [InlineData("    status: draft", "    starting_control: arab_forces\n    status: draft", "locations[town_a].control_date", "required with starting_control")]
    public void AMapLocationRecordIsValidated(string before, string after, string field, string message)
    {
        var bad = Fixture.Files["map/locations_1948.yaml"].Replace(before, after);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/locations_1948.yaml"] = bad })));
        Assert.Equal("map/locations_1948.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void ASettlementOnASeaHexIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 0, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Equal("map/settlement_hexes.yaml", ex.File);
        Assert.Equal("settlements[town_a]", ex.Field);
        Assert.Contains("hex q=0 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ASettlementOffTheMapIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"].Replace("q: 1, r: 0", "q: 40, r: 0");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Contains("hex q=40 r=0 is not a land hex", ex.Message);
    }

    [Fact]
    public void ADuplicateHexIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"{""q"":4,""r"":0,", @"{""q"":3,""r"":0,");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
        Assert.Equal("hexes[q=3,r=0]", ex.Field);
    }

    [Fact]
    public void AnUnknownKeyInHexesJsonIsALoadError()
    {
        var bad = Fixture.Files["map/hexes.json"].Replace(@"""layout""", @"""layuot""");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = bad })));
        Assert.Equal("map/hexes.json", ex.File);
    }

    [Fact]
    public void ASettlementHexRowForAnUnknownSettlementIsALoadError()
    {
        var bad = Fixture.Files["map/settlement_hexes.yaml"] + "  - { id: town_b, q: 2, r: 0 }\n";
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = bad })));
        Assert.Equal("map/settlement_hexes.yaml", ex.File);
        Assert.Equal("settlements[town_b]", ex.Field);
        Assert.Contains("is not in map/settlements_1948.yaml", ex.Message);
    }

    [Fact]
    public void ASettlementWithoutAHexRowIsALoadError()
    {
        var bad = Fixture.Files["map/settlements_1948.yaml"] + "  - { id: town_c, name: Town C, lon: 34.4, lat: 33.3, type: town, founded: 1900, pre_modern: false }\n";
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlements_1948.yaml"] = bad })));
        Assert.Equal("map/settlements_1948.yaml", ex.File);
        Assert.Equal("settlements[town_c]", ex.Field);
        Assert.Contains("has no hex in map/settlement_hexes.yaml", ex.Message);
    }

    [Fact]
    public void AMissingSettlementHexesFileIsALoadError()
    {
        var dir = Fixture.Write();
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(dir, rel);
            return rel == "map/settlement_hexes.yaml" || !System.IO.File.Exists(path) ? null : System.IO.File.ReadAllText(path);
        }, new string[0]));
        Assert.Equal("map/settlement_hexes.yaml", ex.File);
        Assert.Contains("missing file", ex.Message);
    }

    [Fact]
    public void TheRealMapHas1102HexesAndEvery1948Settlement()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(1102, c.Hexes.Count);
        Assert.Equal(870, c.Hexes.Count(h => h.Land));
        Assert.Equal(25, c.SettlementHexes.Count);
        Assert.Equal(10, c.MapLocations.Count);
        Assert.Contains(c.SettlementHexes, s => s.Id == "tiberias");
        Assert.Contains(c.MapLocations, x => x.Id == "jerusalem" && x.Name == "West Jerusalem" && x.StartingControl == "israeli");
        Assert.Contains(c.MapLocations, x => x.Id == "east_jerusalem_old_city" && x.StartingControl == "contested");
    }
}
