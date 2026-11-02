using Statecraft.Sim;

/// <summary>R-CITY-006: approved named-place projections, not a production boundary roster.</summary>
public class PlayerRegionsContentTests
{
    // Geography from the approved #1691 research on the 4.4 km grid (#1183): Ramla and Rehovot share (-4,24),
    // Rishon LeZion is (-4,23). The shared-hex row is a test region; its capital is not an owner choice.
    // Approval of capitals does not establish opening control.
    internal const string RamlaRehovot = @"
  - id: ramla_rehovot
    capital_location: ramla
    member_hexes: [{ q: -4, r: 24 }]
    member_locations: [ramla, rehovot]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:13,20', 'test fixture: no owner capital choice']
    capital_approval: NEEDS-RESEARCH
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
    internal const string Central = @"
  - id: central_plain
    capital_location: rishon_lezion
    member_hexes: [{ q: -4, r: 23 }]
    member_locations: [rishon_lezion]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:17', 'owner 27 October 2026: approved capital Rishon LeZion']
    capital_approval: APPROVED
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
    internal static Content Load(string? regions = "player_regions:\n" + RamlaRehovot, bool modern = true,
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
        Assert.Equal("ramla_rehovot", r.Id); Assert.Equal("NEEDS-RESEARCH", r.CapitalApproval);
        Assert.Equal("NEEDS-RESEARCH", r.BindingStatus); Assert.Null(r.OpeningController);
        Assert.Equal(new[] { "ramla", "rehovot" }, r.MemberLocations);
        Assert.Single(r.MemberHexes);
        Assert.Same(r, c.PlayerRegions.AtLocation("rehovot"));
        Assert.Same(r, c.PlayerRegions.AtCapital("ramla"));
        Assert.Null(c.PlayerRegions.AtCapital("rehovot"));
        Assert.Same(r, c.PlayerRegions.AtHex(-4, 24));
        Assert.Null(c.PlayerRegions.AtLocation("tiberias"));
        Assert.NotEmpty(c.Regions); // Immigrant origins retain their own catalogue.
    }

    [Theory]
    [InlineData("id: ramla_rehovot", "id: ''", "id")]
    [InlineData("capital_location: ramla", "capital_location: tiberias", "capital_location")]
    [InlineData("capital_location: ramla", "capital_location: ''", "capital_location")]
    [InlineData("[ramla, rehovot]", "[]", "member_locations")]
    [InlineData("[ramla, rehovot]", "[ramla, ramla]", "member_locations[1]")]
    [InlineData("[ramla, rehovot]", "[ramla, missing]", "member_locations[1]")]
    [InlineData("[{ q: -4, r: 24 }]", "[]", "member_hexes")]
    [InlineData("[{ q: -4, r: 24 }]", "[{ q: -4 }]", "member_hexes[0]")]
    [InlineData("[{ q: -4, r: 24 }]", "[{ q: -4, r: 24 }, { q: -4, r: 24 }]", "member_hexes[1]")]
    [InlineData("[{ q: -4, r: 24 }]", "[{ q: -4, r: 23 }]", "member_locations[0]")]
    [InlineData("value: null", "value: inferred", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "PARTIAL-EVIDENCE, value: inferred", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "SOURCED, value: null", "opening_control.value")]
    [InlineData("NEEDS-RESEARCH, value: null", "SOURCED, value: [inferred]", "opening_control.value")]
    [InlineData("capital_approval: NEEDS-RESEARCH", "capital_approval: inferred", "capital_approval")]
    [InlineData("binding_status: NEEDS-RESEARCH", "binding_status: inferred", "binding_status")]
    [InlineData("    sources: ['content/map/settlement_hexes.yaml:13,20', 'test fixture: no owner capital choice']", "    sources: []", "sources")]
    public void InvalidRowIsRejectedBeforeContentIsExposed(string from, string to, string field)
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot.Replace(from, to)));
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
        var duplicate = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot + RamlaRehovot));
        Assert.Equal("player_regions[1].id", duplicate.Field);
        var overlap = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot + RamlaRehovot.Replace("id: ramla_rehovot", "id: central_plain")));
        Assert.Equal("player_regions[1].member_hexes[0]", overlap.Field);
        var duplicatePlace = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot + Central.Replace("[rishon_lezion]", "[rishon_lezion, ramla]")));
        Assert.Equal("player_regions[1].member_locations[1]", duplicatePlace.Field);
    }

    [Fact]
    public void SeaAndAbsentHexesAreRefused()
    {
        var sea = Load(null).Hexes.First(h => !h.Land);
        foreach (var pair in new[] { (sea.Q, sea.R), (int.MaxValue, int.MaxValue) })
        {
            var replacement = FormattableString.Invariant($"{{ q: {pair.Item1}, r: {pair.Item2} }}");
            var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot.Replace("{ q: -4, r: 24 }", replacement)));
            Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0].member_hexes[0]", e.Field);
        }
    }

    [Fact]
    public void FinalPlacementAndAmbiguousDualIdentityAreChecked()
    {
        var e = Assert.Throws<ContentException>(() => Load(overrideText: "terrain: []\ncontrol: []\nlocations: [{ id: ramla, hex: { q: -4, r: 23 } }]\n"));
        Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0].member_locations[0]", e.Field);
        var c = Load(null);
        c.MapLocations.Add(new MapLocation { Id = "ramla", Q = -4, R = 23 });
        e = Assert.Throws<ContentException>(() => PlayerRegionCatalogue.Validate(c, new[] { Definition() }));
        Assert.Equal("player_regions[0].member_locations[0]", e.Field);
    }

    internal static PlayerRegionDefinition Definition() => new PlayerRegionDefinition {
        Id = "ramla_rehovot", CapitalLocation = "ramla", MemberHexes = new() { new() { Q = -4, R = 24 } },
        MemberLocations = new() { "ramla", "rehovot" }, DesignNote = "Named-place projection for testing",
        Sources = new() { "content/map/settlement_hexes.yaml:13,20" }, CapitalApproval = "NEEDS-RESEARCH", BindingStatus = "NEEDS-RESEARCH",
        OpeningControl = new() { Status = "NEEDS-RESEARCH", Value = null }
    };

    [Fact]
    public void ValidatedGeographyDoesNotAliasMutableInput()
    {
        var row = Definition(); var catalogue = PlayerRegionCatalogue.Validate(Load(null), new[] { row });
        row.MemberHexes[0].Q = -3; row.MemberLocations.Clear(); row.Sources.Clear();
        Assert.Equal(-4, catalogue.Regions["ramla_rehovot"].MemberHexes[0].Q);
        Assert.Equal(2, catalogue.Regions["ramla_rehovot"].MemberLocations.Count);
        Assert.Single(catalogue.Regions["ramla_rehovot"].Sources);
    }

    [Fact]
    public void PartialEvidenceStaysNullAndTiberiasKeepsItsApprovedDesignation()
    {
        const string valleys = @"
  - id: valleys
    capital_location: tiberias
    member_hexes: [{ q: 11, r: 10 }]
    member_locations: [tiberias]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/settlement_hexes.yaml:9', 'owner 27 October 2026: approved capital Tiberias']
    capital_approval: APPROVED
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";
        var c = Load("player_regions:\n" + RamlaRehovot + Central + valleys);
        Assert.Equal("tiberias", c.PlayerRegions.Regions["valleys"].CapitalLocation);
        Assert.Equal("APPROVED", c.PlayerRegions.Regions["valleys"].CapitalApproval);
        Assert.Equal("rishon_lezion", c.PlayerRegions.Regions["central_plain"].CapitalLocation);
        Assert.Equal("APPROVED", c.PlayerRegions.Regions["central_plain"].CapitalApproval);
        Assert.All(c.PlayerRegions.Regions.Values, r => Assert.Null(r.OpeningController));
        var partial = Load("player_regions:\n" + RamlaRehovot.Replace("NEEDS-RESEARCH, value: null", "PARTIAL-EVIDENCE, value: null"));
        Assert.Equal("PARTIAL-EVIDENCE", partial.PlayerRegions.Regions["ramla_rehovot"].ControlStatus);
        Assert.Null(partial.PlayerRegions.Regions["ramla_rehovot"].OpeningController);
    }

    [Fact]
    public void OnlyExactNewIdentityConsumesAndHashesGeography()
    {
        var reads = new List<string>();
        var old = Load(modern: false, onRead: reads.Add);
        Assert.DoesNotContain("regions.yaml", reads); Assert.Empty(old.PlayerRegions.Regions);
        Assert.Equal(old.Hash, Load("malformed: [", modern: false).Hash);
        Assert.NotEqual(Load().Hash, Load("player_regions:\n" + RamlaRehovot + "\n# consumed provenance\n").Hash);
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
