using Statecraft.Sim;

/// <summary>#1689, the Historical start inventory under exact gdd-0.3 (R-POP-004): the real census cities and the real
/// map/units_1948.yaml starting formations, read from the shipped content with only eras.yaml's ruleset switched.
/// The inventory is not yet wired into Simulation.NewGame (see R-POP-004 and docs/population-ledger.md).</summary>
public class HistoricalPopulationTests
{
    static Content Real(bool modern)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            return modern && rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    static int Count(IReadOnlyList<HistoricalPopulationRow> rows, string source) => rows.Count(r => r.Source == source);

    // R-POP-004, R-CITY-007, R-UNIT-006, R-CITY-008: one record per census population unit in each city and per population
    // unit of each Israeli starting formation, no more and no less, checked against the aggregate start of the same content.
    [Fact]
    public void RealInventoryHoldsTheCensusCitiesAndTheStartingFormationsNoMoreNoLess()
    {
        var c = Real(true);
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        var rows = HistoricalPopulation.Inventory(c);
        var start = Simulation.NewGame(c, 1, Mode.Historical);   // the aggregate start; the ledger stays uninitialized

        Assert.NotEmpty(start.Cities);
        foreach (var city in start.Cities.Values)
        {
            Assert.Equal(city.Civilians, Count(rows, HistoricalPopulation.CensusPrefix + city.Id));
            Assert.Equal(0, city.Refugees);   // R-CITY-007: a city starts with no refugees, so every census record is a civilian
        }
        var own = start.Units.Values.Where(u => u.Side == Movement.PlayerSide).ToList();
        Assert.NotEmpty(own);
        foreach (var u in own)
            Assert.Equal(u.Strength, Count(rows, HistoricalPopulation.FormationPrefix + c.StartingForces[u.Id].Row));

        var expected = start.Cities.Values.Sum(x => x.Civilians) + own.Sum(u => u.Strength);
        Assert.Equal(expected, rows.Count);
        Assert.All(rows.Where(r => r.Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal)),
            r => Assert.Equal(PopulationStatus.FreeCivilian, r.Status));
        Assert.All(rows.Where(r => r.Source.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal)),
            r => Assert.Equal(PopulationStatus.ActiveSoldier, r.Status));
        Assert.All(rows, r => Assert.True(r.Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal)
            || r.Source.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal)));
    }

    // R-POP-004, R-CITY-006, R-CITY-007: an anchor independent of the loader. The census sources are exactly the twelve
    // managed Jewish cities of cities.yaml (no Arab or mixed town is own population), and named cities' counts are their
    // sourced people (cities.yaml) at 1,000 a unit, to the nearest unit with a half up and at least 1, written out here.
    [Fact]
    public void CensusSourcesAreTheTwelveCitiesAtTheirCensusUnits()
    {
        var rows = HistoricalPopulation.Inventory(Real(true));
        var cities = rows.Select(r => r.Source).Where(s => s.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).Select(s => s.Substring(HistoricalPopulation.CensusPrefix.Length)).OrderBy(s => s, StringComparer.Ordinal);
        Assert.Equal(new[] { "hadera", "haifa", "jerusalem", "netanya", "petah_tikva", "rehovot", "rishon_lezion", "rosh_pinna",
            "safed", "tel_aviv", "tiberias", "zikhron_yaakov" }, cities);
        Assert.Equal(248, Count(rows, HistoricalPopulation.CensusPrefix + "tel_aviv"));    // 248,459 people
        Assert.Equal(99, Count(rows, HistoricalPopulation.CensusPrefix + "haifa"));        // 98,618: rounds up
        Assert.Equal(84, Count(rows, HistoricalPopulation.CensusPrefix + "jerusalem"));    // 83,984: rounds up
        Assert.Equal(6, Count(rows, HistoricalPopulation.CensusPrefix + "tiberias"));      // 5,555: rounds up
        Assert.Equal(1, Count(rows, HistoricalPopulation.CensusPrefix + "rosh_pinna"));    // 350: at least 1
    }

    // R-POP-004, R-UNIT-006: the formation sources are exactly the Israeli starting rows, compared by row id.
    [Fact]
    public void FormationSourcesAreExactlyTheOwnStartingRows()
    {
        var c = Real(true);
        var rows = HistoricalPopulation.Inventory(c);
        var formations = rows.Select(r => r.Source).Where(s => s.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal);
        Assert.Equal(c.StartingForces.Where(f => f.Side == Movement.PlayerSide && f.Arrives == null)
            .Select(f => HistoricalPopulation.FormationPrefix + f.Row).OrderBy(s => s, StringComparer.Ordinal), formations);
    }

    // R-POP-004, R-UNIT-006, R-UNIT-009: enemy formations are not own population, and a dated arrival is not in the start
    // inventory (it takes new ids when it arrives, Q11).
    [Fact]
    public void EnemyFormationsAndDatedArrivalsAreNotInTheStartInventory()
    {
        var c = Real(true);
        var rows = HistoricalPopulation.Inventory(c);
        var enemy = c.StartingForces.Where(f => f.Side != Movement.PlayerSide).ToList();
        var arrivals = c.StartingForces.Where(f => f.Side == Movement.PlayerSide && f.Arrives != null).ToList();
        Assert.NotEmpty(enemy);
        Assert.Contains(arrivals, f => f.Row == "brigade_7");   // R-UNIT-009: the 7th Brigade arrives on 24 May 1948
        foreach (var f in enemy.Concat(arrivals))
            Assert.Equal(0, Count(rows, HistoricalPopulation.FormationPrefix + f.Row));
    }

    // R-POP-004 with D2 (owner, 1 November 2026): the Historical start counts at rules.yaml's people_per_unit, 1,000 a unit,
    // not the gdd-0.3 catalogue's 10,000 (Open mode's). R-UNIT-006's own examples: Golani's 4,095 is 4, Kiryati's 2,504 is 3.
    [Fact]
    public void TheHistoricalStartCountsAtOneThousandPeopleAUnit()
    {
        var c = Real(true);
        Assert.Equal(1000, c.RulesAt(c.Rules.Start).Mvp!.PeoplePerUnit);
        Assert.Equal(10000, c.PopulationPeoplePerUnit);
        var rows = HistoricalPopulation.Inventory(c);
        Assert.Equal(4, Count(rows, HistoricalPopulation.FormationPrefix + "golani"));
        Assert.Equal(3, Count(rows, HistoricalPopulation.FormationPrefix + "kiryati"));
    }

    // R-POP-001, R-POP-004: inventory order is source key by ordinal string comparison, then a contiguous within-source
    // ordinal from 0, so ascending population ids follow it (Q10). Repeated reads are identical (docs/determinism.md).
    [Fact]
    public void InventoryOrderIsSourceKeyThenOrdinalAndRepeatable()
    {
        var c = Real(true);
        var rows = HistoricalPopulation.Inventory(c);
        var sorted = rows.OrderBy(r => r.Source, StringComparer.Ordinal).ThenBy(r => r.Ordinal).ToList();
        Assert.Equal(sorted, rows);
        foreach (var g in rows.GroupBy(r => r.Source, StringComparer.Ordinal))
            Assert.Equal(Enumerable.Range(0, g.Count()), g.Select(r => r.Ordinal));
        var again = HistoricalPopulation.Inventory(Real(true));
        Assert.Equal(rows.Select(r => (r.Source, r.Ordinal, r.Status)), again.Select(r => (r.Source, r.Ordinal, r.Status)));
    }

    // R-POP-001, R-POP-004: only exact gdd-0.3 has a population ledger; the shipped gdd-0.1 content refuses the inventory.
    [Fact]
    public void Gdd01ContentRefusesTheInventory()
    {
        var c = Real(false);
        Assert.Equal(SaveGame.GddRuleset, c.Ruleset);
        Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.Inventory(c));
    }

    // R-POP-002, R-POP-004: a new gdd-0.3 game still starts on the aggregate records with an uninitialized ledger;
    // initializing it at NewGame waits on the producer integration (R-POP-002 refusal).
    [Fact]
    public void NewGameDoesNotInitializeTheLedgerYet()
    {
        Assert.False(Simulation.NewGame(Real(true), 1, Mode.Historical).PopulationLedger.IsInitialized);
    }
}
