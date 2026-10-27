using Statecraft.Sim;

/// <summary>R-CITY-006: approved named-place projections, not a production boundary roster.</summary>
public class PlayerRegionsContentTests
{
    // Geography from the approved #1691 research: Tel Aviv and Jaffa share (-3,19),
    // Rishon LeZion is (-4,21). Approval of capitals does not establish opening control.
    internal const string TelAviv = @"
  - id: tel_aviv_jaffa
    capital_location: tel_aviv
    member_hexes: [{ q: -3, r: 19 }]
    member_locations: [tel_aviv, jaffa]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:5-6', 'owner 27 October 2026: approved capital Tel Aviv']
    capital_approval: APPROVED
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
    internal const string Central = @"
  - id: central_plain
    capital_location: rishon_lezion
    member_hexes: [{ q: -4, r: 21 }]
    member_locations: [rishon_lezion]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:17', 'owner 27 October 2026: approved capital Rishon LeZion']
    capital_approval: APPROVED
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
    internal static Content Load(string? regions = "player_regions:\n" + TelAviv, bool modern = true,
        string? overrideText = null, Action<string>? onRead = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            onRead?.Invoke(rel);
            if (rel == PlayerRegionCatalogue.FileName) return regions;
            if (rel == MapOverrides.FileName) return overrideText;
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            return modern && rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    [Fact]
    public void SharedHexRetainsTwoPlaceIdentitiesAndOneCapital()
    {
        var c = Load(); var r = Assert.Single(c.PlayerRegions.Regions).Value;
        Assert.Equal("tel_aviv_jaffa", r.Id); Assert.Equal("APPROVED", r.CapitalApproval);
        Assert.Equal("NEEDS-RESEARCH", r.BindingStatus); Assert.Null(r.OpeningController);
        Assert.Equal(new[] { "jaffa", "tel_aviv" }, r.MemberLocations);
        Assert.Single(r.MemberHexes);
        Assert.Same(r, c.PlayerRegions.AtLocation("jaffa"));
        Assert.Same(r, c.PlayerRegions.AtCapital("tel_aviv"));
        Assert.Null(c.PlayerRegions.AtCapital("jaffa"));
        Assert.Same(r, c.PlayerRegions.AtHex(-3, 19));
        Assert.Null(c.PlayerRegions.AtLocation("tiberias"));
        Assert.NotEmpty(c.Regions); // Immigrant origins retain their own catalogue.
    }

    [Theory]
    [InlineData("id: tel_aviv_jaffa", "id: ''", "id")]
    [InlineData("capital_location: tel_aviv", "capital_location: tiberias", "capital_location")]
    [InlineData("capital_location: tel_aviv", "capital_location: ''", "capital_location")]
    [InlineData("[tel_aviv, jaffa]", "[]", "member_locations")]
    [InlineData("[tel_aviv, jaffa]", "[tel_aviv, tel_aviv]", "member_locations[1]")]
    [InlineData("[tel_aviv, jaffa]", "[tel_aviv, missing]", "member_locations[1]")]
    [InlineData("[{ q: -3, r: 19 }]", "[]", "member_hexes")]
    [InlineData("[{ q: -3, r: 19 }]", "[{ q: -3 }]", "member_hexes[0]")]
    [InlineData("[{ q: -3, r: 19 }]", "[{ q: -3, r: 19 }, { q: -3, r: 19 }]", "member_hexes[1]")]
    [InlineData("[{ q: -3, r: 19 }]", "[{ q: -4, r: 21 }]", "member_locations[0]")]
    [InlineData("value: null", "value: inferred", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "PARTIAL-EVIDENCE, value: inferred", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "SOURCED, value: null", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "SOURCED, value: [inferred]", "opening_control.value")]
    [InlineData("capital_approval: APPROVED", "capital_approval: inferred", "capital_approval")]
    [InlineData("binding_status: NEEDS-RESEARCH", "binding_status: inferred", "binding_status")]
    [InlineData("    sources: ['content/map/settlement_hexes.yaml:5-6', 'owner 27 October 2026: approved capital Tel Aviv']", "    sources: []", "sources")]
    public void InvalidRowIsRejectedBeforeContentIsExposed(string from, string to, string field)
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + TelAviv.Replace(from, to)));
        Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0]." + field, e.Field);
    }

    [Theory]
    [InlineData("{}", "player_regions")]
    [InlineData("player_regions: null", "player_regions")]
    [InlineData("player_regions: [null]", "player_regions[0]")]
    public void MissingOrNullCollectionsAreRefused(string text, string field)
    {
        var e = Assert.Throws<ContentException>(() => Load(text));
        Assert.Equal("regions.yaml", e.File); Assert.Equal(field, e.Field);
    }

    [Fact]
    public void DuplicateRegionAndOverlappingGeographyAreRefused()
    {
        var duplicate = Assert.Throws<ContentException>(() => Load("player_regions:\n" + TelAviv + TelAviv));
        Assert.Equal("player_regions[1].id", duplicate.Field);
        var overlap = Assert.Throws<ContentException>(() => Load("player_regions:\n" + TelAviv + TelAviv.Replace("id: tel_aviv_jaffa", "id: central_plain")));
        Assert.Equal("player_regions[1].member_hexes[0]", overlap.Field);
        var duplicatePlace = Assert.Throws<ContentException>(() => Load("player_regions:\n" + TelAviv + Central.Replace("[rishon_lezion]", "[rishon_lezion, tel_aviv]")));
        Assert.Equal("player_regions[1].member_locations[1]", duplicatePlace.Field);
    }

    [Fact]
    public void SeaAndAbsentHexesAreRefused()
    {
        var sea = Load(null).Hexes.First(h => !h.Land);
        foreach (var pair in new[] { (sea.Q, sea.R), (int.MaxValue, int.MaxValue) })
        {
            var replacement = FormattableString.Invariant($"{{ q: {pair.Item1}, r: {pair.Item2} }}");
            var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + TelAviv.Replace("{ q: -3, r: 19 }", replacement)));
            Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0].member_hexes[0]", e.Field);
        }
    }

    [Fact]
    public void FinalPlacementAndAmbiguousDualIdentityAreChecked()
    {
        var e = Assert.Throws<ContentException>(() => Load(overrideText: "terrain: []\ncontrol: []\nlocations: [{ id: tel_aviv, hex: { q: -4, r: 21 } }]\n"));
        Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0].member_locations[0]", e.Field);
        var c = Load(null);
        c.MapLocations.Add(new MapLocation { Id = "tel_aviv", Q = -4, R = 21 });
        e = Assert.Throws<ContentException>(() => PlayerRegionCatalogue.Validate(c, new[] { Definition() }));
        Assert.Equal("player_regions[0].member_locations[0]", e.Field);
    }

    internal static PlayerRegionDefinition Definition() => new PlayerRegionDefinition {
        Id = "tel_aviv_jaffa", CapitalLocation = "tel_aviv", MemberHexes = new() { new() { Q = -3, R = 19 } },
        MemberLocations = new() { "tel_aviv", "jaffa" }, DesignNote = "Named-place projection for testing",
        Sources = new() { "content/map/settlement_hexes.yaml:5-6" }, CapitalApproval = "APPROVED", BindingStatus = "NEEDS-RESEARCH",
        OpeningControl = new() { Status = "NEEDS-RESEARCH", Value = null }
    };

    [Fact]
    public void ValidatedGeographyDoesNotAliasMutableInput()
    {
        var row = Definition(); var catalogue = PlayerRegionCatalogue.Validate(Load(null), new[] { row });
        row.MemberHexes[0].Q = -4; row.MemberLocations.Clear(); row.Sources.Clear();
        Assert.Equal(-3, catalogue.Regions["tel_aviv_jaffa"].MemberHexes[0].Q);
        Assert.Equal(2, catalogue.Regions["tel_aviv_jaffa"].MemberLocations.Count);
        Assert.Single(catalogue.Regions["tel_aviv_jaffa"].Sources);
    }

    [Fact]
    public void PartialEvidenceStaysNullAndTiberiasKeepsItsApprovedDesignation()
    {
        const string valleys = @"
  - id: valleys
    capital_location: tiberias
    member_hexes: [{ q: 10, r: 8 }]
    member_locations: [tiberias]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:9', 'owner 27 October 2026: approved capital Tiberias']
    capital_approval: APPROVED
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
        var c = Load("player_regions:\n" + TelAviv + Central + valleys);
        Assert.Equal("tiberias", c.PlayerRegions.Regions["valleys"].CapitalLocation);
        Assert.Equal("APPROVED", c.PlayerRegions.Regions["valleys"].CapitalApproval);
        Assert.Equal("rishon_lezion", c.PlayerRegions.Regions["central_plain"].CapitalLocation);
        Assert.Equal("APPROVED", c.PlayerRegions.Regions["central_plain"].CapitalApproval);
        Assert.All(c.PlayerRegions.Regions.Values, r => Assert.Null(r.OpeningController));
        var partial = Load("player_regions:\n" + TelAviv.Replace("NEEDS-RESEARCH, value: null", "PARTIAL-EVIDENCE, value: null"));
        Assert.Equal("PARTIAL-EVIDENCE", partial.PlayerRegions.Regions["tel_aviv_jaffa"].ControlStatus);
        Assert.Null(partial.PlayerRegions.Regions["tel_aviv_jaffa"].OpeningController);
    }

    [Fact]
    public void OnlyExactNewIdentityConsumesAndHashesGeography()
    {
        var reads = new List<string>();
        var old = Load(modern: false, onRead: reads.Add);
        Assert.DoesNotContain("regions.yaml", reads); Assert.Empty(old.PlayerRegions.Regions);
        Assert.Equal(old.Hash, Load("malformed: [", modern: false).Hash);
        Assert.NotEqual(Load().Hash, Load("player_regions:\n" + TelAviv + "\n# consumed provenance\n").Hash);
        Assert.Empty(Load(null).PlayerRegions.Regions);
        // Legacy also has no read, even when an unrelated file is present and malformed.
        var files = new Dictionary<string, string>(Fixture.Files) { ["regions.yaml"] = "malformed: [" };
        reads.Clear();
        var legacy = ContentLoader.Load(rel => { reads.Add(rel); return files.TryGetValue(rel, out var text) ? text : null; }, Array.Empty<string>());
        Assert.DoesNotContain("regions.yaml", reads); Assert.Empty(legacy.PlayerRegions.Regions);
        files.Remove("regions.yaml");
        Assert.Equal(legacy.Hash, ContentLoader.Load(rel => files.TryGetValue(rel, out var text) ? text : null, Array.Empty<string>()).Hash);
    }
}
