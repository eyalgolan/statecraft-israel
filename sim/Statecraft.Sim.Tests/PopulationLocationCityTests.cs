using Statecraft.Sim;

/// <summary>#1689 PR 1, city binding (coordinator ruling, 7 November 2026, option (a)): a population location may name its
/// city, a regions.yaml member location of its region. The city is written into the canonical text only when set, as
/// R-UNIT-002's <c>|home=</c> is, so a location without one, and a ledger without one, hashes as before.</summary>
public class PopulationLocationCityTests
{
    // R-POP-003: pinned from bfdfc3222 + the reception commit, before the City field existed.
    [Fact]
    public void ALocationWithNoCityKeepsItsCanonicalTextAndLedgerHash()
    {
        Assert.Equal("5:north|1:0|1:0|4:camp", new PopulationLocation("north", 0, 0, "camp").Canonical());
        Assert.Equal("5:north|-|-|-", new PopulationLocation("north").Canonical());
        Assert.Equal("98a09ec203e20e197141395397c041383454b74b5c7d204b78a239693bd87ec2",
            PopulationLedgerTests.State(new PopulationInventoryRow("start", 0, PopulationLedgerTests.Person())).Hash());
    }
    // R-POP-003: a set city is written after the other fields, length-prefixed, so it reaches the hash.
    [Fact]
    public void ACityIsWrittenOnlyWhenSet()
    {
        Assert.Equal("5:north|-|-|-|city=5:haifa", new PopulationLocation("north", city: "haifa").Canonical());
        Assert.NotEqual(new PopulationLocation("north").Canonical(), new PopulationLocation("north", city: "haifa").Canonical());
        Assert.Throws<ArgumentException>(() => new PopulationLocation("north", city: " "));
    }

    static GameState Fixture(Content c, PopulationLocation location) => PopulationLedger.CreateFixture(c,
        new PopulationBindings(new[] { location }, Array.Empty<PopulationTarget>(), Array.Empty<string>(), Array.Empty<string>()),
        Array.Empty<PopulationInventoryRow>(), mode: Mode.Historical);

    // R-POP-002: a location's city must be in its region by regions.yaml (AtLocation); the refusal names both.
    [Fact]
    public void ACityOutsideItsRegionIsRefusedNamingBoth()
    {
        var c = HistoricalPopulationTests.Real(true);
        var haifa = c.PlayerRegions.AtLocation("haifa")!.Id;
        var other = c.PlayerRegions.Regions.Keys.First(r => r != haifa);
        var e = Assert.Throws<ArgumentException>(() => Fixture(c, new PopulationLocation(other, city: "haifa")));
        Assert.Contains("haifa", e.Message);
        Assert.Contains(other, e.Message);
        var unknown = Assert.Throws<ArgumentException>(() => Fixture(c, new PopulationLocation(haifa, city: "atlantis")));
        Assert.Contains("atlantis", unknown.Message);
        Assert.Contains(haifa, unknown.Message);
        Assert.True(Fixture(c, new PopulationLocation(haifa, city: "haifa")).PopulationLedger.IsInitialized);
    }

    // S1-1 (review of #2936): a location's city is a place where own people live by R-POP-004's rulings, a census city
    // (cities.yaml, R-CITY-006) or a field host of its region (regions.yaml field_hexes host; owner, 7 November 2026, #1689:
    // "These two only (Recommended)", a settlement hosts F19 and F20 only). Any other member location of the region, a
    // locality, a sites.yaml id or a map location, is refused, naming it and the region.
    [Fact]
    public void AMemberLocationThatIsNeitherACensusCityNorAFieldHostIsRefused()
    {
        var c = HistoricalPopulationTests.Real(true);
        var census = new HashSet<string>(c.Cities.Select(x => x.Id), StringComparer.Ordinal);
        var region = c.PlayerRegions.AtLocation("haifa")!;
        var hosts = new HashSet<string>(region.FieldHosts.Select(h => h.Host), StringComparer.Ordinal);
        var place = region.MemberLocations.First(p => !census.Contains(p) && !hosts.Contains(p));
        var e = Assert.Throws<ArgumentException>(() => Fixture(c, new PopulationLocation(region.Id, city: place)));
        Assert.Contains(place, e.Message);
        Assert.Contains(region.Id, e.Message);
        // The two settlement hosts stay accepted in their own region, and no other region.
        foreach (var host in new[] { "nir_am", "nirim" })
        {
            var home = c.PlayerRegions.AtLocation(host)!;
            Assert.Contains(home.FieldHosts, h => h.Host == host);
            Assert.True(Fixture(c, new PopulationLocation(home.Id, city: host)).PopulationLedger.IsInitialized);
        }
    }
}
