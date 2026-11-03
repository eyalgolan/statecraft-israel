using Statecraft.Sim;

// R-CAMP-003: the optional overlay changes only exact gdd-0.3 and binds its shipped start camps to existing map locations.
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
