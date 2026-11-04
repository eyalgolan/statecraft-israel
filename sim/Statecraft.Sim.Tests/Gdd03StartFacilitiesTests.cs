using Statecraft.Sim;

// R-CAMP-003: the optional overlay changes only exact gdd-0.3; it names the map location each shipped start camp stands for,
// and only the city's military_camp facility flag is kept.
public class Gdd03StartFacilitiesTests
{
    const string OverlayFile = "gdd03-start.yaml";

    static string ShippedOverlay() => File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), OverlayFile));

    static Content LoadShipped(bool modern, string? overlay, Action<string>? onRead = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            onRead?.Invoke(rel);
            if (rel == OverlayFile) return overlay;
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            return modern && rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    [Fact]
    public void ShippedGdd03StartHasTheCataloguedNumberOfCampCities()
    {
        var path = Path.Combine(RealContentTests.ContentRoot(), OverlayFile);
        Assert.True(File.Exists(path), OverlayFile + " must be shipped");
        var reads = new List<string>();
        var c = LoadShipped(true, ShippedOverlay(), reads.Add);
        Assert.Contains(OverlayFile, reads);
        Assert.Equal(Gdd03Military.Scalar(c, "start.military_camps"),
            c.Cities.Count(x => x.Facilities.Contains("military_camp")));
        Assert.Equal(new[] { "jerusalem", "tel_aviv" },
            c.Cities.Where(x => x.Facilities.Contains("military_camp")).Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(new[] { "military_camp", "hakhshara" }, c.Cities.Single(x => x.Id == "tel_aviv").Facilities);
        var state = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.True(Training.HasFacility(c, state, "tel_aviv", "military_camp"));
        Assert.True(Training.HasFacility(c, state, "jerusalem", "military_camp"));
    }

    // #2484, owner, 4 November 2026: "Keep the Hakhshara". Under gdd-0.3 the Hakhshara stays the way civilians become
    // halutzim, so on the shipped content Tel Aviv's Hakhshara trains a halutz group into a halutz unit (R-TRAIN-001, R-TRAIN-003).
    [Fact]
    public void Gdd03HakhsharaTrainsHalutz()
    {
        var c = LoadShipped(true, ShippedOverlay());
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var turns = c.UnitTypes.Single(t => t.Id == "halutz").TrainingTurns;
        var r = Simulation.Step(c, s, new Command[] { new Train("tel_aviv", "halutz", 1) });
        Assert.Empty(r.Rejected);
        var cohort = Assert.Single(r.State.Cohorts.Values);
        Assert.Equal(("halutz", "tel_aviv", 1), (cohort.Type, cohort.City, cohort.Count));
        for (int turn = 1; turn < turns; turn++)
        {
            Assert.DoesNotContain(r.State.Units.Values, u => u.Type == "halutz");
            r = Simulation.Step(c, r.State, new Command[0]);
        }
        Assert.Empty(r.State.Cohorts);
        var unit = Assert.Single(r.State.Units.Values, u => u.Type == "halutz");   // one unit per group, at the city's hex
        var home = c.SettlementHexes.First(h => h.Id == "tel_aviv");
        Assert.Equal((home.Q, home.R), (unit.Q, unit.R));
    }

    // #2484, owner, 4 November 2026: "Keep the Hakhshara". Under gdd-0.3 the halutz still trains at a Hakhshara (Tel Aviv's
    // own Hakhshara is pinned by ShippedGdd03StartHasTheCataloguedNumberOfCampCities above).
    [Fact]
    public void Gdd03TelAvivStartsWithHakhshara()
    {
        var c = LoadShipped(true, ShippedOverlay());
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Equal("hakhshara", c.UnitTypes.Single(t => t.Id == "halutz").Facility);
        Assert.True(Training.HasFacility(c, Simulation.NewGame(c, 1, Mode.Historical), "tel_aviv", "hakhshara"));
    }

    [Fact]
    public void AbsentOverlayLeavesGdd03CitiesUnchanged()
    {
        var reads = new List<string>();
        var c = LoadShipped(true, null, reads.Add);
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Contains(OverlayFile, reads); // requested under gdd-0.3; the reader returned null, so the absent path ran
        Assert.Equal(new[] { "tel_aviv" }, c.Cities.Where(x => x.Facilities.Contains("military_camp")).Select(x => x.Id));
        Assert.Equal(new[] { "military_camp", "hakhshara" }, c.Cities.Single(x => x.Id == "tel_aviv").Facilities);
        Assert.False(Training.HasFacility(c, Simulation.NewGame(c, 1, Mode.Historical), "jerusalem", "military_camp"));
    }

    [Fact]
    public void SyntheticGdd03FixtureLoadsWithoutTheOptionalOverlay()
    {
        var files = Fixture.Gdd();
        files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
        files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(
            Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        var c = ContentLoader.Load(Fixture.Write(files));
        var city = Assert.Single(c.Cities);
        Assert.Equal("town_a", city.Id);
        Assert.Equal(new[] { "military_camp", "hakhshara" }, city.Facilities);
    }

    [Fact]
    public void Gdd03ReadsOverlayBytesIntoItsHash()
    {
        var text = ShippedOverlay();
        var original = LoadShipped(true, text);
        var changed = LoadShipped(true, text + "# hash input changed\n");
        Assert.NotEqual(original.Hash, changed.Hash);
    }

    [Fact]
    public void Gdd01NeverReadsOverlayAndKeepsManifestGoldens()
    {
        var reads = new List<string>();
        var poison = LoadShipped(false, "this is deliberately invalid YAML: [", reads.Add);
        Assert.DoesNotContain(OverlayFile, reads);
        var absent = LoadShipped(false, null);
        Assert.Equal(absent.Hash, poison.Hash);
        Assert.Equal(Simulation.NewGame(absent, 1, Mode.Historical).Describe(),
            Simulation.NewGame(poison, 1, Mode.Historical).Describe());
        var run1950 = GoldenRun.Play(poison, GoldenRun.Until1950);
        var run1952 = GoldenRun.Play(poison, GoldenRun.Until1952);
        Assert.Equal(GoldenManifest.Expected("hash1950"), run1950.Hash());
        Assert.Equal(GoldenManifest.Expected("hash1952"), run1952.Hash());
        Assert.Equal(GoldenSnapshot.Read("hash1950"), run1950.Describe());
        Assert.Equal(GoldenSnapshot.Read("hash1952"), run1952.Describe());
    }

    public static IEnumerable<object[]> InvalidOverlays()
    {
        yield return Case("ruleset: gdd-0.1\nmilitary_camps: []\n", "ruleset", "must be gdd-0.3");
        yield return Case("ruleset: gdd-0.3\n", "military_camps", "must be a list");
        yield return Case(One("missing", "military_schneller"), "military_camps[0].city", "must name an existing city");
        yield return Case(Two(("tel_aviv", "military_tel_litvinsky"), ("tel_aviv", "military_schneller")),
            "military_camps[1].city", "duplicate city");
        yield return Case(One("jerusalem", "missing"), "military_camps[0].location", "must name an existing map location");
        yield return Case(Two(("tel_aviv", "military_tel_litvinsky"), ("jerusalem", "military_tel_litvinsky")),
            "military_camps[1].location", "duplicate location");
        yield return Case(One("jerusalem", "negba"), "military_camps[0].location", "must name a military_camp map location");
        yield return Case(One("tel_aviv", "military_tel_litvinsky"), "military_camps",
            "effective military-camp city count 1 must equal start.military_camps 2");
    }

    static object[] Case(string yaml, string field, string reason) => new object[] { yaml, field, reason };
    static string One(string city, string location) => Two((city, location));
    static string Two(params (string City, string Location)[] rows) =>
        "ruleset: gdd-0.3\nmilitary_camps:\n" + string.Concat(rows.Select(x => $"  - {{ city: {x.City}, location: {x.Location} }}\n"));

    [Theory]
    [MemberData(nameof(InvalidOverlays))]
    public void InvalidOverlayFailsWithItsExactFieldAndReason(string yaml, string field, string reason)
    {
        var ex = Assert.Throws<ContentException>(() => LoadShipped(true, yaml));
        Assert.Equal(OverlayFile, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Equal($"{OverlayFile}: {field}: {reason}", ex.Message);
    }
}
