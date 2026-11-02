using Statecraft.Sim;

/// <summary>#1690 acceptance checks (owner, 1 November 2026): map markers and completed sites create no extra economy or
/// people, and fixture values never become production defaults (R-CITY-006, R-CITY-008, R-POP-001, R-SITE-003).
/// Authored campaign initialization is not on main yet, so each check is pinned to what exists today: the explicit
/// regional fixture (PlayerRegions.CreateFixture) or the exact gdd-0.3 identity's game path with its authored, empty
/// region catalogue. Neither stands in for the later production cutover.</summary>
public class Gdd03RegionalIsolationTests
{
    const string A = "ramla_rehovot";
    const string Marker = "fixture_rural_marker";
    // A kibbutz map marker on Ramla's and Rehovot's land hex (-4,24) of the 4.4 km grid (#1796), with the roster's own
    // flags: no city panel, no founding site.
    const string RuralMarker = @"
- id: fixture_rural_marker
  name: Fixture Kibbutz
  name_he: קיבוץ בדיקה
  name_he_review: agent-checked
  name_he_sources:
  - { url: 'https://example.org/fixture', checked: 2026-11-01 }
  kind: kibbutz
  lon: 34.84
  lat: 31.92
  q: -4
  r: 24
  priority: optional
  marker: true
  managed_city: false
  designated_site: false
  settlement_control_target: false
  sources:
  - { url: 'https://example.org/fixture', checked: 2026-11-01 }
  status: draft
";

    /// <summary>The real content under exact gdd-0.3, optionally with the rural marker appended to the location roster
    /// and an explicit regions.yaml text (null reads the authored file).</summary>
    static Content Load(bool marker = false, string? regions = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            if (rel == PlayerRegionCatalogue.FileName && regions != null) return regions;
            if (rel == "eras.yaml") return text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            if (marker && rel == "map/locations_1948.yaml") return text!.TrimEnd() + "\n" + RuralMarker;
            return text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    static PlayerRegionPool Pool() => new PlayerRegionPool(A, 7, .4m, 3, 2,
        new[] { new PlayerRegionFacility("fixture-workshop", "rehovot", PlayerRegionFacilityKind.Workshop, 11) });

    static GameState Fixture(Content c) => PlayerRegions.CreateFixture(c, new[] { Pool() },
        new[] { new PopulationInventoryRow("fixture", 0, new PopulationUnit(0, PopulationStatus.FreeCivilian, 0,
            new PopulationLocation(A, -4, 24), PopulationAssignment.Unassigned, CivilianProfession.None, CivilianTier.None,
            0, "", 0, PopulationEquipment.None)) },
        new[] { new PopulationTarget("fixture-a-job", PopulationAssignmentKind.Work, A) },
        Array.Empty<string>(), Array.Empty<string>());

    // The readable state minus the geography section's own place lines, which name the marker by design.
    static string WithoutPlace(GameState s, string place) =>
        string.Join("\n", s.Describe().Split('\n').Where(l => !l.Contains(place, StringComparison.Ordinal)));

    // R-CITY-006, R-CITY-008, R-POP-001. GDD 0.3 (docs/gdd/cities.md:11 at 605277dfa): "A map marker does not
    // automatically create another managed city." Pinned to the fixture: a rural marker that is a member location of a
    // region adds no pool, capacity, facility, person or city to it.
    [Fact]
    public void R_CITY_006_ARuralMarkerInARegionAddsNoEconomyOrPersonToTheFixture()
    {
        var region = PlayerRegionsContentTests.RamlaRehovot;
        var plain = Fixture(Load(regions: "player_regions:\n" + region));
        var withMarker = Load(marker: true, regions: "player_regions:\n" + region.Replace("[ramla, rehovot]", "[ramla, rehovot, " + Marker + "]"));
        Assert.Contains(Marker, withMarker.PlayerRegions.Regions[A].MemberLocations);
        var marked = Fixture(withMarker);
        Assert.Equal(plain.PlayerRegions.Keys, marked.PlayerRegions.Keys);
        Assert.Equal(plain.PlayerRegions[A].Canonical(), marked.PlayerRegions[A].Canonical());
        Assert.Equal(PlayerRegions.PresentPopulation(plain, A), PlayerRegions.PresentPopulation(marked, A));
        Assert.Equal(plain.PopulationLedger.Canonical(), marked.PopulationLedger.Canonical());
        Assert.Empty(marked.Cities); Assert.Empty(marked.Population);
        Assert.Equal(WithoutPlace(plain, Marker), WithoutPlace(marked, Marker));
    }

    // R-CITY-006 ("The cities are the `city` and `town` rows of `map/settlements_1948.yaml`"), R-POP-001. Pinned to the
    // exact gdd-0.3 game path with the authored, empty region catalogue (so the empty PlayerRegions check pins that
    // catalogue): adding a rural marker to the location roster changes no start state, and no turn after it, and no
    // marker without managed_city is a city.
    [Fact]
    public void R_CITY_006_ARuralMarkerOnTheMapCreatesNoCityRegionOrPersonInTheGame()
    {
        var plainContent = Load(); var markerContent = Load(marker: true);
        Assert.Contains(markerContent.MapLocations, l => l.Id == Marker);
        var plain = Simulation.NewGame(plainContent, 1, Mode.Historical);
        var marked = Simulation.NewGame(markerContent, 1, Mode.Historical);
        Assert.Equal(plain.Describe(), marked.Describe());
        Assert.Equal(Simulation.Step(plainContent, plain, Array.Empty<Command>()).State.Describe(),
            Simulation.Step(markerContent, marked, Array.Empty<Command>()).State.Describe());
        Assert.DoesNotContain(Marker, marked.Cities.Keys);
        foreach (var l in markerContent.MapLocations.Where(l => !l.ManagedCity)) Assert.DoesNotContain(l.Id, marked.Cities.Keys);
        Assert.Empty(marked.PlayerRegions); Assert.False(marked.PopulationLedger.IsInitialized);
    }

    // R-SITE-003 ("No city, housing, job, population or Social cohesion changes"), R-POP-001. GDD 0.3
    // (docs/gdd/cities.md:72 at 605277dfa) excludes "automatically granting a city economy on founding completion". Pinned to the exact gdd-0.3 game path:
    // regional fixtures refuse turns (R-POP-002), so completion inside an initialized region waits for the campaign initialization.
    [Fact]
    public void R_SITE_003_CompletingAYishuvSiteCreatesNoCityRegionPersonOrResource()
    {
        var c = Load();
        var site = c.Sites.First(x => x.Kinds.Contains("yishuv"));
        GameState Start(bool building)
        {
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            var id = s.NextId++;
            s.Units.Add(id, new Unit(id, "halutz", site.Q, site.R));
            if (building) s.Sites[site.Id] = new SiteWork(site.Id, id, 1, null);
            return s;
        }
        // The office multiplier can be below 1 (R-MIN-002), so step both games together until the site is built.
        GameState idle = Start(false), built = Start(true);
        for (int turn = 0; turn < 6 && built.Sites[site.Id].BuiltOn == null; turn++)
        {
            idle = Simulation.Step(c, idle, Array.Empty<Command>()).State;
            built = Simulation.Step(c, built, Array.Empty<Command>()).State;
        }
        Assert.NotNull(built.Sites[site.Id].BuiltOn);
        Assert.Equal(idle.Cities.Values.Select(x => x.Canonical()), built.Cities.Values.Select(x => x.Canonical()));
        Assert.DoesNotContain(site.Id, built.Cities.Keys);
        Assert.Equal(idle.Population, built.Population); Assert.Equal(idle.TotalPopulation, built.TotalPopulation);
        Assert.Equal(idle.Units.Count, built.Units.Count); Assert.Equal(idle.NextId, built.NextId);
        Assert.Equal((idle.Treasury, idle.Food, idle.Arms), (built.Treasury, built.Food, built.Arms));
        Assert.Equal(idle.Indicators, built.Indicators); // Social cohesion is the "morale" indicator
        Assert.Empty(built.PlayerRegions); Assert.False(built.PopulationLedger.IsInitialized);
    }

    // R-CITY-006 ("no nonempty production regional roster or allocations are supplied"), R-POP-001 (no fallback),
    // R-CITY-008 (one person record). Pinned to the fixture and the authored files: building a regional fixture first
    // leaves the authored catalogue and a new game exactly as they were, and the fixture keeps one ledger only.
    [Fact]
    public void R_CITY_006_FixtureValuesNeverBecomeProductionDefaultsOrASecondLedger()
    {
        var before = Load(); var game = Simulation.NewGame(before, 1, Mode.Historical).Describe();
        var fixture = Fixture(Load(regions: "player_regions:\n" + PlayerRegionsContentTests.RamlaRehovot));
        fixture.PopulationLedger.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "fixture-a-job"));
        Assert.Empty(fixture.Cities); Assert.Empty(fixture.Population);
        Assert.Equal(fixture.PopulationLedger.TotalPeople, fixture.TotalPopulation);

        var after = Load();
        Assert.Equal(before.Hash, after.Hash);
        Assert.Empty(after.PlayerRegions.Regions); Assert.Empty(PlayerRegionCatalogue.Empty.Regions);
        Assert.Equal(before.PopulationPeoplePerUnit, after.PopulationPeoplePerUnit);
        var production = Simulation.NewGame(after, 1, Mode.Historical);
        Assert.Equal(game, production.Describe());
        // This guards static or global leakage from a fixture. "before" may already carry a leak from an earlier fixture
        // in this process (a static default did exactly that when injected), so also look for the fixture's own ids.
        foreach (var id in new[] { "fixture", A, "fixture-a-job", "fixture-workshop" })
            Assert.DoesNotContain(id, production.Describe(), StringComparison.Ordinal);
        Assert.Empty(production.PlayerRegions); Assert.False(production.PopulationLedger.IsInitialized);
        Assert.Equal(production.Population.Values.Sum(), production.TotalPopulation);
    }
}
