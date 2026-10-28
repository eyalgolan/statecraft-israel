using System.Globalization;
using Statecraft.Sim;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>R-CITY-006: design approval does not complete geometry or verify history.</summary>
public class Gdd03RegionAuthoringTests
{
    static string Research(string name) => Path.Combine(RealContentTests.ContentRoot(), "..", "research_notes", name);
    static string Evidence => File.ReadAllText(Research("gdd03-regions-capitals-fields.yaml"));
    static string Staged => File.ReadAllText(Research("gdd03-regions.schema-input.yaml"));
    static string Projections => File.ReadAllText(Research("gdd03-regions.projections.yaml"));
    static YamlMappingNode Parse(string text)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        return Assert.IsType<YamlMappingNode>(Assert.Single(stream.Documents).RootNode);
    }
    static YamlNode Get(YamlMappingNode row, string key) => row.Children[new YamlScalarNode(key)];
    static string? Text(YamlMappingNode row, string key) => Assert.IsType<YamlScalarNode>(Get(row, key)).Value;
    static string Approved => File.ReadAllText(Research("gdd03-regions.approved-draft.yaml"));
    /// <summary>A scalar's value with YAML's plain nulls read as null, so a quoted "null" or "" stays text; an absent key is null.</summary>
    static string? Value(YamlMappingNode row, string key)
    {
        if (!row.Children.TryGetValue(new YamlScalarNode(key), out var node)) return null;
        var scalar = Assert.IsType<YamlScalarNode>(node);
        return scalar.Style == ScalarStyle.Plain && scalar.Value is "" or "~" or "null" or "Null" or "NULL" ? null : scalar.Value;
    }
    static YamlMappingNode Map(YamlMappingNode row, string key) => Assert.IsType<YamlMappingNode>(Get(row, key));
    static YamlMappingNode[] Rows(YamlMappingNode root, string key) =>
        Assert.IsType<YamlSequenceNode>(Get(root, key)).Children.Select(n => Assert.IsType<YamlMappingNode>(n)).ToArray();
    static string[] Strings(YamlMappingNode row, string key) =>
        Assert.IsType<YamlSequenceNode>(Get(row, key)).Children.Select(n => Assert.IsType<YamlScalarNode>(n).Value!).ToArray();
    static bool Unknown(YamlNode node) => string.IsNullOrEmpty(Assert.IsType<YamlScalarNode>(node).Value)
        || Assert.IsType<YamlScalarNode>(node).Value == "null";
    /// <summary>Every opening_control under node with its path, in document order; a list item's segment is its id, else
    /// "q,r" for a hex, else its index (as tools/regions_draft_reference.py writes the approved draft's).</summary>
    static IEnumerable<(string Path, YamlMappingNode Control)> PathControls(YamlNode node, string path)
    {
        if (node is YamlMappingNode map)
            foreach (var pair in map.Children)
            {
                var key = Assert.IsType<YamlScalarNode>(pair.Key).Value!;
                if (key == "opening_control") yield return (path, Assert.IsType<YamlMappingNode>(pair.Value));
                foreach (var child in PathControls(pair.Value, path == "" ? key : path + "/" + key)) yield return child;
            }
        else if (node is YamlSequenceNode sequence)
            for (var i = 0; i < sequence.Children.Count; i++)
            {
                var item = sequence.Children[i];
                var segment = i.ToString(CultureInfo.InvariantCulture);
                if (item is YamlMappingNode m)
                {
                    // A string id, as YAML reads it: a plain null or a plain number is not one.
                    if (Value(m, "id") is string id && !(((YamlScalarNode)m.Children[new YamlScalarNode("id")]).Style == ScalarStyle.Plain
                        && long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
                        segment = id;
                    else if (m.Children.ContainsKey(new YamlScalarNode("q")) && m.Children.ContainsKey(new YamlScalarNode("r")))
                        segment = Text(m, "q") + "," + Text(m, "r");
                }
                foreach (var child in PathControls(item, path + "/" + segment)) yield return child;
            }
    }
    static IEnumerable<YamlMappingNode> Controls(YamlNode node)
    {
        if (node is YamlMappingNode map)
            foreach (var pair in map.Children)
            {
                if (pair.Key is YamlScalarNode key && key.Value == "opening_control")
                    yield return Assert.IsType<YamlMappingNode>(pair.Value);
                foreach (var child in Controls(pair.Value)) yield return child;
            }
        else if (node is YamlSequenceNode sequence)
            foreach (var item in sequence.Children)
                foreach (var child in Controls(item)) yield return child;
    }
    static string SingleRow(YamlMappingNode row)
    {
        var root = new YamlMappingNode();
        root.Add("player_regions", new YamlSequenceNode(row));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        new YamlStream(new YamlDocument(root)).Save(output, false);
        return output.ToString();
    }
    static Content LoadShipped(string? regions, Action<string>? onRead = null)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            onRead?.Invoke(rel);
            if (rel == PlayerRegionCatalogue.FileName) return regions;
            var path = Path.Combine(root, rel);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    [Fact]
    public void AuthoredRuntimeFileLoadsUnderExactIdentityWithActivationOff()
    {
        var text = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml"));
        var reads = new List<string>();
        var c = PlayerRegionsContentTests.Load(text, onRead: reads.Add);
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Contains("regions.yaml", reads);
        Assert.Empty(c.PlayerRegions.Regions);
        var state = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(state.PlayerRegions);
        Assert.False(state.PopulationLedger.IsInitialized);
    }

    [Fact]
    public void SchemaAdaptationRetainsEveryNamedMemberAndEvidenceStatus()
    {
        var evidence = Rows(Parse(Evidence), "regions");
        var staged = Rows(Parse(Staged), "player_regions");
        Assert.Equal(9, staged.Length);
        Assert.Equal(evidence.Select(r => Text(r, "id")), staged.Select(r => Text(r, "id")));
        Assert.Equal(326, staged.Sum(r => Strings(r, "member_locations").Length));
        var approved = new[] { "valleys", "tel_aviv_jaffa", "central_plain" };
        foreach (var row in staged)
        {
            var original = evidence.Single(r => Text(r, "id") == Text(row, "id"));
            Assert.Equal(Text(original, "capital_place_id"), Text(row, "capital_location"));
            Assert.Equal(Rows(original, "members").Select(r => Text(r, "id")), Strings(row, "member_locations"));
            Assert.Equal(Text(original, "binding_status"), Text(row, "binding_status"));
            Assert.Equal(Text(Map(original, "opening_control"), "status"), Text(Map(row, "opening_control"), "status"));
            Assert.True(Unknown(Get(Map(row, "opening_control"), "value")));
            Assert.Equal("NEEDS-RESEARCH", Text(original, "capital_status"));
            Assert.Equal(approved.Contains(Text(row, "id")) ? "APPROVED" : "NEEDS-RESEARCH", Text(row, "capital_approval"));
            Assert.True(Unknown(Get(original, "boundary_hexes")));
            Assert.Empty(Assert.IsType<YamlSequenceNode>(Get(row, "member_hexes")).Children);
            Assert.Equal("design value, not a historical boundary", Text(row, "design_note"));
        }
    }

    // #1852 review: the imported evidence keeps the owner-approved draft's membership and opening control, identity by
    // identity (the reference is generated from the approved file by tools/regions_draft_reference.py; its header pins the
    // draft's id, size and SHA-256).
    [Fact]
    public void EvidenceKeepsTheApprovedDraftMembershipAndControlPerIdentity()
    {
        var approved = Parse(Approved);
        var source = Map(approved, "source");
        Assert.Equal("docs/drafts/regions-1691.draft.yaml", Text(source, "file"));
        Assert.Equal("69e0586f6805bf97b49b64c4ee3fd6732afd128594939a9f92e8f5aa3113f125", Text(source, "sha256"));
        var draft = Rows(approved, "regions");
        var evidence = Rows(Parse(Evidence), "regions");
        Assert.Equal(draft.Select(r => Text(r, "id")), evidence.Select(r => Text(r, "id")));
        Assert.Equal(326, draft.Sum(r => Rows(r, "members").Length));
        foreach (var (want, got) in draft.Zip(evidence))
        {
            var id = Text(want, "id");
            var control = Map(got, "opening_control");
            Assert.Equal((id, Value(want, "capital_place_id"), Value(want, "binding_status"), Value(want, "control_status"), Value(want, "control_value")),
                (id, Value(got, "capital_place_id"), Value(got, "binding_status"), Value(control, "status"), Value(control, "value")));
            var members = Rows(got, "members");
            Assert.Equal(Rows(want, "members").Select(m => Text(m, "id")), members.Select(m => Text(m, "id")));
            foreach (var (w, g) in Rows(want, "members").Zip(members))
            {
                var c = Map(g, "opening_control");
                Assert.Equal((Value(w, "id"), Value(w, "region_id"), Value(w, "binding_status"), Value(w, "control_status"), Value(w, "control_value"), Value(w, "control_date")),
                    (Value(g, "id"), Value(g, "region_id"), Value(g, "binding_status"), Value(c, "status"), Value(c, "value"), Value(c, "date")));
            }
        }
        // #1852 review: the reception site, hexes and facilities carry their own opening controls; compare each by path.
        var others = PathControls(Parse(Evidence), "")
            .Where(x => !(x.Path.Split('/') is var p && p[0] == "regions" && (p.Length == 2 || p[2] == "members"))).ToArray();
        var wantOthers = Rows(approved, "ancillary_controls");
        Assert.Equal(50, wantOthers.Length);
        Assert.Equal(wantOthers.Select(r => (Value(r, "path"), Value(r, "status"), Value(r, "value"), Value(r, "date"))),
            others.Select(x => ((string?)x.Path, Value(x.Control, "status"), Value(x.Control, "value"), Value(x.Control, "date"))));
    }

    [Theory]
    [InlineData("galilee")]
    [InlineData("valleys")]
    [InlineData("haifa_sharon")]
    [InlineData("tel_aviv_jaffa")]
    [InlineData("central_plain")]
    [InlineData("jerusalem_hills")]
    [InlineData("northern_hills")]
    [InlineData("southern_coast")]
    [InlineData("negev")]
    public void EveryIncompleteRegionIsRejectedByTheSharedStrictLoader(string id)
    {
        var row = Rows(Parse(Staged), "player_regions").Single(r => Text(r, "id") == id);
        var error = Assert.Throws<ContentException>(() => PlayerRegionsContentTests.Load(SingleRow(row)));
        Assert.Equal("regions.yaml", error.File);
        Assert.Equal("player_regions[0].member_hexes", error.Field);
    }

    [Fact]
    public void ApprovedNamedPlaceProjectionsLoadWithoutClaimingFullExtents()
    {
        var c = PlayerRegionsContentTests.Load(Projections);
        Assert.Equal(3, c.PlayerRegions.Regions.Count);
        var evidence = Rows(Parse(Evidence), "regions");
        foreach (var region in c.PlayerRegions.Regions.Values)
        {
            var original = evidence.Single(r => Text(r, "id") == region.Id);
            Assert.Equal(Text(original, "capital_place_id"), region.CapitalLocation);
            Assert.Equal("APPROVED", region.CapitalApproval);
            Assert.Equal(Text(original, "binding_status"), region.BindingStatus);
            Assert.Null(region.OpeningController);
            foreach (var member in region.MemberLocations)
            {
                var source = Rows(original, "members").Single(r => Text(r, "id") == member);
                var hex = Map(source, "hex");
                Assert.Contains((int.Parse(Text(hex, "q")!, CultureInfo.InvariantCulture),
                    int.Parse(Text(hex, "r")!, CultureInfo.InvariantCulture)), region.MemberHexes);
            }
        }
        Assert.Same(c.PlayerRegions.AtLocation("tel_aviv"), c.PlayerRegions.AtLocation("jaffa"));
        Assert.Equal(new[] { "jaffa", "tel_aviv" }, c.PlayerRegions.Regions["tel_aviv_jaffa"].MemberLocations);
        Assert.Equal(new[] { "rehovot", "rishon_lezion" }, c.PlayerRegions.Regions["central_plain"].MemberLocations);
    }

    [Fact]
    public void OwnerLabelsAreApprovedSeparatelyFromFactualStatus()
    {
        var root = Parse(Evidence);
        Assert.Contains("APPROVED", Text(root, "approval")!);
        var decisions = Map(root, "owner_design_decisions");
        Assert.Equal("2026-10-27", Text(decisions, "date"));
        foreach (var pair in new[] { ("tel_aviv_jaffa", "Tel Aviv"), ("central_plain", "Rishon LeZion"), ("valleys", "Tiberias") })
        {
            var choice = Map(Map(decisions, "capitals"), pair.Item1);
            Assert.Equal("APPROVED", Text(choice, "status"));
            Assert.Equal(pair.Item2, Text(choice, "label"));
        }
        Assert.Equal("Haifa-Sharon", Text(Map(decisions, "reception_region"), "label"));
        foreach (var id in new[] { "haifa_sharon", "jerusalem_hills" })
        {
            var extent = Map(Map(decisions, "retained_extents"), id);
            Assert.Equal("APPROVED", Text(extent, "status"));
            Assert.Equal("Keep as proposed", Text(extent, "label"));
        }
    }

    [Fact]
    public void ReceptionDesignBindingDoesNotPromoteFootprintOrControl()
    {
        var root = Parse(Evidence);
        Assert.Equal("false", Text(root, "runtime_activation"));
        Assert.Equal("NEEDS-HUMAN-CHECK", Text(root, "historical_review"));
        var camp = Map(root, "reception_site");
        Assert.Equal("kiryat_shmuel_camp_haifa", Text(camp, "value"));
        Assert.Equal("haifa_sharon", Text(Map(camp, "region_binding"), "value"));
        Assert.Equal("APPROVED", Text(Map(camp, "region_binding"), "status"));
        Assert.Equal("NEEDS-HUMAN-CHECK", Text(camp, "status"));
        Assert.Equal("NEEDS-HUMAN-CHECK", Text(Map(camp, "mandate_anchor"), "camp_footprint_status"));
        Assert.Equal("PARTIAL-EVIDENCE", Text(Map(camp, "opening_control"), "status"));
        Assert.True(Unknown(Get(Map(camp, "opening_control"), "value")));
        foreach (var key in new[] { "q", "r" }) Assert.True(Unknown(Get(Map(camp, "hex"), key)));
        foreach (var key in new[] { "lon", "lat" }) Assert.True(Unknown(Get(Map(camp, "coordinates"), key)));
        Assert.DoesNotContain("OWNER OPTION", Evidence);
        Assert.DoesNotContain("region binding PENDING", Evidence);
        Assert.DoesNotContain("approval by name pending", Evidence);
    }

    [Fact]
    public void HistoricalControlInventoryRetainsUnknownValuesAndOriginalStatuses()
    {
        var controls = Controls(Parse(Evidence)).ToArray();
        Assert.Equal(385, controls.Length);
        Assert.Equal(33, controls.Count(r => Text(r, "status") == "SOURCED"));
        Assert.Equal(255, controls.Count(r => Text(r, "status") == "PARTIAL-EVIDENCE"));
        Assert.Equal(97, controls.Count(r => Text(r, "status") == "NEEDS-RESEARCH"));
        foreach (var row in controls)
            if (Text(row, "status") != "SOURCED") Assert.True(Unknown(Get(row, "value")));
            else Assert.False(string.IsNullOrWhiteSpace(Text(row, "value")));
    }

    [Fact]
    public void OlderIdentityIgnoresAuthoredAndInvalidRegionContentAndKeepsManifestGoldens()
    {
        var reads = new List<string>();
        var authored = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml"));
        var old = LoadShipped(authored, reads.Add);
        Assert.DoesNotContain("regions.yaml", reads);
        var absent = LoadShipped(null);
        var incomplete = LoadShipped(Staged);
        Assert.Equal(absent.Hash, old.Hash);
        Assert.Equal(absent.Hash, incomplete.Hash);
        Assert.Equal(Simulation.NewGame(absent, 1, Mode.Historical).Describe(),
            Simulation.NewGame(old, 1, Mode.Historical).Describe());
        var run1950 = GoldenRun.Play(old, GoldenRun.Until1950);
        var run1952 = GoldenRun.Play(old, GoldenRun.Until1952);
        Assert.Equal(GoldenManifest.Expected("hash1950"), run1950.Hash());
        Assert.Equal(GoldenManifest.Expected("hash1952"), run1952.Hash());
        Assert.Equal(GoldenSnapshot.Read("hash1950"), run1950.Describe());
        Assert.Equal(GoldenSnapshot.Read("hash1952"), run1952.Describe());
        var files = new Dictionary<string, string>(Fixture.Files) { ["regions.yaml"] = Staged };
        Content Legacy() => ContentLoader.Load(rel => files.TryGetValue(rel, out var text) ? text : null, Array.Empty<string>());
        var legacy = Legacy();
        files.Remove("regions.yaml");
        Assert.Equal(legacy.Hash, Legacy().Hash);
        Assert.Equal(Simulation.NewGame(legacy, 1, Mode.Historical).Describe(),
            Simulation.NewGame(Legacy(), 1, Mode.Historical).Describe());
    }
}
