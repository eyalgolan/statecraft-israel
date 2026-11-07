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
}
