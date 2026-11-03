using System.Globalization;
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

    // R-POP-001, R-POP-004 with D2 (owner, 1 November 2026, "Approve all six (Recommended)"): a ledger fixture of the
    // Historical start counts its records at rules.yaml's people_per_unit for the start day (1,000), as StartingForces and
    // Cities do, while the gdd-0.3 catalogue's population.people_per_unit still reads Open mode's 10,000.
    [Fact]
    public void HistoricalLedgerFixtureCountsAtTheRulesScale()
    {
        var c = Real(true);
        // Precondition, not the claim: the two scales differ (D2), so the total below tells them apart.
        Assert.Equal((1000, 10000), (c.RulesAt(c.Rules.Start).Mvp!.PeoplePerUnit, c.PopulationPeoplePerUnit));
        var inventory = HistoricalPopulation.Inventory(c);
        Assert.NotEmpty(inventory);
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), LedgerRows(inventory), mode: Mode.Historical);
        Assert.Equal(Mode.Historical, s.Mode);
        Assert.Equal(inventory.Count, s.PopulationLedger.Units.Count);
        Assert.Equal((double)inventory.Count * 1000, s.PopulationLedger.TotalPeople);
        // R-POP-003: Clone keeps the fixture's scale rather than re-reading the catalogue.
        Assert.Equal((double)inventory.Count * 1000, s.Clone().PopulationLedger.TotalPeople);
    }

    // R-POP-001 with D2: the catalogue's 10,000 is Open mode's, so an Open fixture and a synthetic one (no mode) keep it.
    [Fact]
    public void OpenAndSyntheticLedgerFixturesKeepTheCatalogueScale()
    {
        var c = Real(true);
        var row = new PopulationInventoryRow("synthetic", 0, PopulationLedgerTests.Person());
        var open = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] { row }, mode: Mode.Open);
        Assert.Equal(Mode.Open, open.Mode);
        Assert.Equal(10000d, open.PopulationLedger.TotalPeople);
        Assert.Equal(10000d, PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] { row }).PopulationLedger.TotalPeople);
    }

    // R-POP-003 with D2: the canonical writer emits the scale. A synthetic fixture's GameState.Mode is the enum default,
    // Historical, so the same rows with and without mode Historical differ only in their scale, and their hashes differ.
    [Fact]
    public void CanonicalStateTellsTheTwoScalesApart()
    {
        var c = Real(true);
        var row = new PopulationInventoryRow("synthetic", 0, PopulationLedgerTests.Person());
        var historical = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] { row }, mode: Mode.Historical);
        var synthetic = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] { row });
        Assert.Equal((Mode.Historical, Mode.Historical), (historical.Mode, synthetic.Mode));
        Assert.Equal((1000d, 10000d), (historical.PopulationLedger.TotalPeople, synthetic.PopulationLedger.TotalPeople));
        Assert.NotEqual(historical.Hash(), synthetic.Hash());
        Assert.Equal(historical.Hash(), historical.Clone().Hash());
    }

    // R-POP-004 with D2: census and units_1948 rows are the Historical start's, so a fixture of them needs mode Historical;
    // without it they would silently count at Open mode's 10,000.
    [Theory]
    [InlineData(null)]
    [InlineData(Mode.Open)]
    public void HistoricalRowsRefuseAnyOtherScale(Mode? mode)
    {
        var c = Real(true);
        var inventory = HistoricalPopulation.Inventory(c);
        foreach (var prefix in new[] { HistoricalPopulation.CensusPrefix, HistoricalPopulation.FormationPrefix })
        {
            var rows = LedgerRows(inventory.Where(r => r.Source.StartsWith(prefix, StringComparison.Ordinal)).Take(1).ToList());
            Assert.Single(rows);
            var e = Assert.Throws<ArgumentException>(() => PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), rows, mode: mode));
            Assert.Equal("Census and units_1948 rows are the Historical start's; their fixture needs mode Historical (R-POP-004).", e.Message);
        }
    }

    // R-POP-001's no-fallback rule on the Historical scale: a missing, zero or negative rules.yaml people_per_unit for the
    // start day is refused, never replaced by the catalogue's value.
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1000)]
    public void HistoricalScaleHasNoFallback(int? peoplePerUnit)
    {
        var c = Real(true);
        var rules = c.RulesAt(c.Rules.Start);
        if (peoplePerUnit is int ppu) rules.Mvp!.PeoplePerUnit = ppu; else rules.Mvp = null;
        var row = new PopulationInventoryRow("synthetic", 0, PopulationLedgerTests.Person());
        var e = Assert.Throws<InvalidOperationException>(() => PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] { row }, mode: Mode.Historical));
        Assert.Equal("The Historical scale needs a positive mvp.people_per_unit in rules.yaml for the start day; no fallback.", e.Message);
    }

    // The real inventory as ledger rows: a free civilian per census unit and an active soldier per formation unit.
    static IEnumerable<PopulationInventoryRow> LedgerRows(IEnumerable<HistoricalPopulationRow> inventory)
    {
        var civilian = PopulationLedgerTests.Person();
        var civilianTemplate = new PopulationUnit(0, PopulationStatus.FreeCivilian, 0, civilian.Location, PopulationAssignment.Unassigned,
            civilian.Profession, civilian.Tier, 0, "basic", 0, PopulationEquipment.None);
        var soldierTemplate = new PopulationUnit(0, PopulationStatus.ActiveSoldier, 0, civilian.Location,
            new PopulationAssignment(PopulationAssignmentKind.ActiveService, "service"), civilian.Profession, civilian.Tier, 0, "basic", 0,
            PopulationEquipment.None);
        return inventory.Select(r => new PopulationInventoryRow(
            r.Source, r.Ordinal, r.Status == PopulationStatus.ActiveSoldier ? soldierTemplate : civilianTemplate)).ToList();
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

    // R-POP-003, R-POP-004 (#1689 region slice): the region-bound fixture builder adds no NewGame path. gdd-0.1 and legacy
    // games still have no ledger and write no ledger section, so their hashes stay; gdd-0.1 content refuses the builder.
    [Fact]
    public void NewGameStillHasNoLedger()
    {
        foreach (var (content, ruleset) in new[] { (Real(false), SaveGame.GddRuleset), (RealContentTests.Legacy(), SaveGame.LegacyRuleset) })
        {
            Assert.Equal(ruleset, content.Ruleset);
            var s = Simulation.NewGame(content, 1, Mode.Historical);
            Assert.False(s.PopulationLedger.IsInitialized);
            Assert.DoesNotContain("population-ledger", s.Describe());
        }
        Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.LedgerFixture(Real(false)));
    }

    // R-POP-004 (#1689 region slice): the Historical ledger fixture holds the inventory, no more and no less: per source, as
    // many records as Inventory lists, census records free civilians and formation records active soldiers.
    [Fact]
    public void HistoricalFixtureHoldsTheInventoryNoMoreNoLess()
    {
        var c = Real(true);
        var inventory = HistoricalPopulation.Inventory(c);
        var units = HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units;
        Assert.Equal(inventory.Count, units.Count);
        Assert.Equal(inventory.Select(r => r.Status), units.Select(u => u.Status));
        Assert.Equal(inventory.Count(r => r.Status == PopulationStatus.FreeCivilian), units.Count(u => u.Status == PopulationStatus.FreeCivilian));
        Assert.Equal(inventory.Count(r => r.Status == PopulationStatus.ActiveSoldier), units.Count(u => u.Status == PopulationStatus.ActiveSoldier));
    }

    // R-POP-001, R-POP-004, D2: the builder goes through #2299's Historical path, so its records count at 1,000 people.
    [Fact]
    public void HistoricalFixtureCountsAtTheRulesScale()
    {
        var c = Real(true);
        var s = HistoricalPopulation.LedgerFixture(c);
        Assert.Equal(Mode.Historical, s.Mode);
        Assert.Equal((double)s.PopulationLedger.Units.Count * 1000, s.PopulationLedger.TotalPeople);
    }

    // R-POP-002, R-POP-004 (#1691 regions): each census record carries the content/regions.yaml region that lists its city
    // (census/tel_aviv is tel_aviv_jaffa; Jaffa gets no records of its own), and no record has an empty or unknown region.
    [Fact]
    public void EveryCensusRecordCarriesItsCitysRegion()
    {
        var c = Real(true);
        var inventory = HistoricalPopulation.Inventory(c);
        var units = HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units;
        for (int i = 0; i < inventory.Count; i++)
        {
            Assert.Contains(units[i].Location!.Region, c.PlayerRegions.Regions.Keys);
            if (inventory[i].Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal))
                Assert.Equal(c.PlayerRegions.AtLocation(inventory[i].Source.Substring(HistoricalPopulation.CensusPrefix.Length))!.Id,
                    units[i].Location!.Region);
        }
        var telAviv = inventory.Select((r, i) => (r, i)).Where(x => x.r.Source == HistoricalPopulation.CensusPrefix + "tel_aviv").ToList();
        Assert.NotEmpty(telAviv);
        Assert.All(telAviv, x => Assert.Equal("tel_aviv_jaffa", units[x.i].Location!.Region));
        Assert.DoesNotContain(inventory, r => r.Source == HistoricalPopulation.CensusPrefix + "jaffa");
    }

    // R-POP-001, R-POP-004, Q10: ids run from 0 with no gaps in the inventory's order (source key by ordinal string compare,
    // then ordinal), and two builds are identical.
    [Fact]
    public void IdsAscendBySourceKeyThenOrdinal()
    {
        var c = Real(true);
        var s = HistoricalPopulation.LedgerFixture(c);
        var units = s.PopulationLedger.Units;
        Assert.Equal(Enumerable.Range(0, units.Count), units.Select(u => u.Id));
        Assert.Equal(units.Count, s.PopulationLedger.NextPopulationId);
        Assert.Equal(s.Hash(), HistoricalPopulation.LedgerFixture(Real(true)).Hash());
    }

    // R-POP-001: the fixture reconciles; the created ids are exactly the living ones.
    [Fact]
    public void HistoricalFixtureReconciles()
    {
        var l = HistoricalPopulation.LedgerFixture(Real(true)).PopulationLedger;
        Assert.True(l.IsInitialized);
        l.Reconcile();
    }

    // R-POP-004: a record whose city or formation has no region in regions.yaml is refused; no region is invented.
    [Fact]
    public void ARecordWithNoRegionIsRefused()
    {
        var c = Real(true);
        c.Cities.First(x => x.Id == "rosh_pinna").Id = "nowhere";
        var e = Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.LedgerFixture(c));
        Assert.Contains("census/nowhere", e.Message);

        c = Real(true);
        var golani = c.StartingForces.First(f => f.Row == "golani");
        (golani.Q, golani.R) = (1000, 1000);
        e = Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.LedgerFixture(c));
        Assert.Contains("units_1948/golani", e.Message);
    }

    // R-POP-004, 27M-24 (#1717: "The starting formations carry issued equipment, recorded apart from the 12 spare Arms"):
    // each starting soldier record carries one issued military item, id <source>/<ordinal>; census records carry none, and
    // no record carries tools.
    [Fact]
    public void EveryStartingSoldierRecordCarriesOneIssuedItem()
    {
        var c = Real(true);
        var inventory = HistoricalPopulation.Inventory(c);
        var units = HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units;
        Assert.Contains(inventory, r => r.Status == PopulationStatus.ActiveSoldier);
        for (int i = 0; i < inventory.Count; i++)
        {
            Assert.Null(units[i].Equipment.ToolsId);
            if (inventory[i].Source.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal))
                Assert.Equal(inventory[i].Source + "/" + inventory[i].Ordinal.ToString(CultureInfo.InvariantCulture),
                    units[i].Equipment.MilitaryId);
            else
                Assert.Null(units[i].Equipment.MilitaryId);
        }
    }

    // R-POP-004, 27M-24: the issued items are distinct, one per population unit of each own starting formation, so their
    // count is the formations' total strength.
    [Fact]
    public void IssuedItemsAreDistinctAndCountTheFormationStrengths()
    {
        var c = Real(true);
        var items = HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units
            .Select(u => u.Equipment.MilitaryId).Where(id => id != null).ToList();
        var strength = c.StartingForces.Where(f => f.Side == Movement.PlayerSide && f.Arrives == null).Sum(f => f.Strength);
        Assert.True(strength > 0);
        Assert.Equal(strength, items.Count);
        Assert.Equal(items.Count, items.Distinct(StringComparer.Ordinal).Count());
    }

    // R-ARMS-001, 27M-24: issued equipment is apart from the spare Arms. Building the fixture leaves the spare stock as a
    // fresh state has it, and NewGame's arms_start (12) is not reduced by any issue.
    [Fact]
    public void IssuedItemsAreApartFromTheSpareArms()
    {
        var c = Real(true);
        Assert.Equal(new GameState(c).Arms, HistoricalPopulation.LedgerFixture(c).Arms);
        Assert.Equal(12, c.RulesAt(c.Rules.Start).Mvp!.ArmsStart);
        Assert.Equal(12, Simulation.NewGame(c, 1, Mode.Historical).Arms);
    }

    // R-POP-004, 27M-24 (#1717: "Starting professions, tools and buildings are owned: turn 1 charges none of them"): the
    // starting formations' issued items are owned, recorded as paid so they are not paid for again.
    [Fact]
    public void StartingEquipmentIsOwned()
    {
        var soldiers = HistoricalPopulation.LedgerFixture(Real(true)).PopulationLedger.Units
            .Where(u => u.Status == PopulationStatus.ActiveSoldier).ToList();
        Assert.NotEmpty(soldiers);
        Assert.All(soldiers, u => Assert.True(u.Equipment.MilitaryPaid));
    }
}
