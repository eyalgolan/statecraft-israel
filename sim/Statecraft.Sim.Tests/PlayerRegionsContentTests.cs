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
        string? overrideText = null, Action<string>? onRead = null, bool localities = true, string? localityText = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            onRead?.Invoke(rel);
            if (rel == PlayerRegionCatalogue.FileName) return regions;
            if (rel == MapOverrides.FileName) return overrideText;
            if (!localities && rel == PlayerRegionCatalogue.LocalityHexesFileName) return null;
            if (localityText != null && rel == PlayerRegionCatalogue.LocalityHexesFileName) return localityText;
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

    // Owner, 2 November 2026, Q1 "Accept localities", and "Fix all, then ship (Recommended)": "Accept site ids too".
    // Tarshiha is a locality (map/localities_1948.yaml) on (10,6) by map/locality_hexes.yaml; Kiryat Shmona is a
    // designated site (sites.yaml) on (16,2). The row is a test region; its label is not an owner choice.
    internal const string LocalityAndSite = @"
  - id: upper_galilee
    capital_location: tarshiha
    member_hexes: [{ q: 10, r: 6 }, { q: 16, r: 2 }]
    member_locations: [tarshiha, kiryat_shmona]
    design_note: 'Named-place projection for testing; not a historical boundary'
    sources: ['content/map/locality_hexes.yaml', 'content/sites.yaml', 'test fixture: no owner label choice']
    capital_approval: NEEDS-RESEARCH
    binding_status: NEEDS-RESEARCH
    opening_control: { status: NEEDS-RESEARCH, value: null }
";

    [Fact]
    public void LocalityAndSiteMembersAreAcceptedOnTheirOwnHexes()
    {
        var c = Load("player_regions:\n" + LocalityAndSite);
        var r = Assert.Single(c.PlayerRegions.Regions).Value;
        Assert.Equal(new[] { "kiryat_shmona", "tarshiha" }, r.MemberLocations);
        Assert.Same(r, c.PlayerRegions.AtLocation("tarshiha"));
        Assert.Same(r, c.PlayerRegions.AtLocation("kiryat_shmona"));
        Assert.Same(r, c.PlayerRegions.AtCapital("tarshiha"));
    }

    // Each case asserts the reason as well as the field: without locality and site placement, every member here
    // would fail earlier as an "unknown place id", so a field-only assert could pass for the wrong reason.
    [Theory]
    // A locality or a site off the region's hexes is placed, and refused there.
    [InlineData("[{ q: 10, r: 6 }, { q: 16, r: 2 }]", "[{ q: 10, r: 6 }, { q: 16, r: 3 }]", "member_locations[1]", "effective location lies outside member_hexes")]
    [InlineData("[{ q: 10, r: 6 }, { q: 16, r: 2 }]", "[{ q: 10, r: 5 }, { q: 16, r: 2 }]", "member_locations[0]", "effective location lies outside member_hexes")]
    // An id in no placement file stays unknown.
    [InlineData("[tarshiha, kiryat_shmona]", "[tarshiha, kiryat_shmona, nowhere_1948]", "member_locations[2]", "unknown place id")]
    // The label still has to be a member (Q3: a label, but the loader's requirement stays).
    [InlineData("capital_location: tarshiha", "capital_location: hebron", "capital_location", "must name a member location on a member hex")]
    public void LocalityAndSiteMembersKeepEveryPlacementCheck(string from, string to, string field, string reason)
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + LocalityAndSite.Replace(from, to)));
        Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0]." + field, e.Field);
        Assert.Equal("regions.yaml: player_regions[0]." + field + ": " + reason, e.Message);
    }

    // #1691 Q4 (owner, 2 November 2026: "Approve as drafted (Recommended)"): optional field_hexes, data only. A field hex
    // is a land hex of the region's own member_hexes, listed once. (-4,24) is coastal_plain; (-4,23) is Rishon LeZion's.
    [Fact]
    public void FieldHexesAreOptionalAndLoadOnTheRegionsOwnHexes()
    {
        Assert.Empty(Assert.Single(Load().PlayerRegions.Regions).Value.FieldHexes);   // absent: none
        var c = Load("player_regions:\n" + RamlaRehovot.Replace("    member_locations:", "    field_hexes: [{ q: -4, r: 24 }]\n    member_locations:"));
        Assert.Equal(new[] { (-4, 24) }, Assert.Single(c.PlayerRegions.Regions).Value.FieldHexes);
    }

    [Theory]
    [InlineData("[{ q: -4 }]", "player_regions[0].field_hexes[0]", "q and r are required integers")]
    [InlineData("[{ r: 24 }]", "player_regions[0].field_hexes[0]", "q and r are required integers")]
    [InlineData("[{ q: 0, r: 0 }]", "player_regions[0].field_hexes[0]", "must name an existing land hex")]
    [InlineData("[{ q: 99, r: 99 }]", "player_regions[0].field_hexes[0]", "must name an existing land hex")]
    [InlineData("[{ q: -4, r: 23 }]", "player_regions[0].field_hexes[0]", "must be one of the region's member_hexes")]
    [InlineData("[{ q: -4, r: 24 }, { q: -4, r: 24 }]", "player_regions[0].field_hexes[1]", "duplicate field hex")]
    [InlineData("null", "player_regions[ramla_rehovot].field_hexes", null)]   // CheckNulls, as for every list
    public void MalformedFieldHexRowsAreRefused(string list, string field, string? reason)
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n"
            + RamlaRehovot.Replace("    member_locations:", "    field_hexes: " + list + "\n    member_locations:")));
        Assert.Equal("regions.yaml", e.File); Assert.Equal(field, e.Field);
        if (reason != null) Assert.Equal("regions.yaml: " + field + ": " + reason, e.Message);
    }

    // Another region's member hex is refused as a field: ownership is exclusive, so no field hex can be in two regions.
    [Fact]
    public void AFieldHexOfAnotherRegionIsRefused()
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n" + RamlaRehovot
            + Central.Replace("    member_locations:", "    field_hexes: [{ q: -4, r: 24 }]\n    member_locations:")));
        Assert.Equal("regions.yaml: player_regions[1].field_hexes[0]: must be one of the region's member_hexes", e.Message);
    }

    // The site is listed first, so it must resolve (sites.yaml is always placed) before the locality is refused.
    [Fact]
    public void WithoutTheLocalityHexesALocalityIsUnknown()
    {
        var e = Assert.Throws<ContentException>(() => Load("player_regions:\n"
            + LocalityAndSite.Replace("[tarshiha, kiryat_shmona]", "[kiryat_shmona, tarshiha]"), localities: false));
        Assert.Equal("regions.yaml", e.File); Assert.Equal("player_regions[0].member_locations[1]", e.Field);
        Assert.Equal("regions.yaml: player_regions[0].member_locations[1]: unknown place id", e.Message);
    }

    // map/locality_hexes.yaml is generated, but the sim still refuses a malformed row instead of placing it: the
    // settlement_hexes.yaml checks (unique ids, land hexes), plus q and r required so a missing one is never a silent 0.
    // hebron is in no region of the default fixture, so without these checks each row would load silently.
    [Theory]
    [InlineData("localities:\n", "localities:\n  - { id: hebron, q: -4, r: 29 }\n", "localities[hebron].id", "duplicate id")]
    [InlineData("{ id: hebron, q: -4, r: 30 }", "{ q: -4, r: 30 }", "localities[].id", ContentLoader.IdRule)]
    [InlineData("{ id: hebron, q: -4, r: 30 }", "{ id: hebron, r: 30 }", "localities[hebron]", "q and r are required integers")]
    [InlineData("{ id: hebron, q: -4, r: 30 }", "{ id: hebron, q: -4 }", "localities[hebron]", "q and r are required integers")]
    [InlineData("{ id: hebron, q: -4, r: 30 }", "{ id: hebron, q: 0, r: 0 }", "localities[hebron]", "hex q=0 r=0 is not a land hex in map/hexes.json")]
    [InlineData("{ id: hebron, q: -4, r: 30 }", "{ id: hebron, q: 99, r: 99 }", "localities[hebron]", "hex q=99 r=99 is not a land hex in map/hexes.json")]
    public void MalformedLocalityHexRowsAreRefused(string from, string to, string field, string reason)
    {
        var real = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), PlayerRegionCatalogue.LocalityHexesFileName));
        Assert.Contains(from, real);
        var e = Assert.Throws<ContentException>(() => Load(localityText: real.Replace(from, to)));
        Assert.Equal(PlayerRegionCatalogue.LocalityHexesFileName, e.File); Assert.Equal(field, e.Field);
        Assert.Equal(PlayerRegionCatalogue.LocalityHexesFileName + ": " + field + ": " + reason, e.Message);
    }

    [Fact]
    public void OnlyExactNewIdentityConsumesAndHashesGeography()
    {
        var reads = new List<string>();
        var old = Load(modern: false, onRead: reads.Add);
        Assert.DoesNotContain("regions.yaml", reads); Assert.Empty(old.PlayerRegions.Regions);
        Assert.DoesNotContain(PlayerRegionCatalogue.LocalityHexesFileName, reads);
        Assert.Equal(old.Hash, Load("malformed: [", modern: false, localities: false).Hash);
        reads.Clear();
        Load(onRead: reads.Add);
        Assert.Contains(PlayerRegionCatalogue.LocalityHexesFileName, reads);
        Assert.Equal(old.Hash, Load("malformed: [", modern: false).Hash);
        Assert.NotEqual(Load().Hash, Load("player_regions:\n" + RamlaRehovot + "\n# consumed provenance\n").Hash);
        Assert.Empty(Load(null).PlayerRegions.Regions);
        // Legacy also has no read, even when an unrelated file is present and malformed.
        var files = new Dictionary<string, string>(Fixture.Files) { ["regions.yaml"] = "malformed: [" };
        reads.Clear();
        var legacy = ContentLoader.Load(rel => { reads.Add(rel); return files.TryGetValue(rel, out var text) ? text : null; }, Array.Empty<string>());
        Assert.DoesNotContain("regions.yaml", reads); Assert.Empty(legacy.PlayerRegions.Regions);
        Assert.DoesNotContain(PlayerRegionCatalogue.LocalityHexesFileName, reads);
        files.Remove("regions.yaml");
        Assert.Equal(legacy.Hash, ContentLoader.Load(rel => files.TryGetValue(rel, out var text) ? text : null, Array.Empty<string>()).Hash);
    }
}
