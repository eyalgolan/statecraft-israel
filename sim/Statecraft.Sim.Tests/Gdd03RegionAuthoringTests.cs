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

    // #1691, owner rulings of 2 November 2026: Q1 "Accept localities"; Q3 "All nine drafts as labels"; and "Fix all, then
    // ship (Recommended)", whose option read: "Accept site ids too; author records for the 4 (sourced); for the 14, the
    // member's hex joins its region where it's unclaimed, and where another region holds the hex the member moves to that
    // region. All listed for Amir to review." Q2 (owner, 2 Nov 2026, "Add (10,4) to Galilee (Recommended)"): (10,4), Hanita's hex, joins Galilee.
    // Each of the 14 members off its region's hexes stands on a hex another region holds, so it moves there.
    static readonly (string Member, string From, string To)[] Moved =
    {
        ("kafr_thulth", "haifa_sharon", "northern_hills"), ("azzoun", "haifa_sharon", "northern_hills"),
        ("al_mansi", "haifa_sharon", "northern_hills"), ("kfar_saba_2", "haifa_sharon", "tel_aviv_jaffa"),
        ("al_safiriyya", "tel_aviv_jaffa", "central_plain"), ("majdal_yaba", "central_plain", "haifa_sharon"),
        ("al_tira", "central_plain", "tel_aviv_jaffa"), ("al_muzayria", "central_plain", "tel_aviv_jaffa"),
        ("qula", "central_plain", "tel_aviv_jaffa"), ("tzrifin", "central_plain", "tel_aviv_jaffa"),
        ("beit_liqya", "jerusalem_hills", "central_plain"), ("zirin", "northern_hills", "valleys"),
        ("iraq_al_manshiyya", "southern_coast", "jerusalem_hills"), ("yasur", "southern_coast", "central_plain"),
    };

    [Fact]
    public void ShippedRegionsLoadUnderExactIdentityWithEveryMemberOnAMemberHex()
    {
        var text = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml"));
        var reads = new List<string>();
        var c = PlayerRegionsContentTests.Load(text, onRead: reads.Add);
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Contains("regions.yaml", reads);
        var staged = Rows(Parse(Staged), "player_regions");
        Assert.Equal(staged.Select(r => Text(r, "id")).OrderBy(x => x, StringComparer.Ordinal), c.PlayerRegions.Regions.Keys);
        // Every member's own hex, from the files the loader places it by: settlements, named locations, sites, localities.
        var localities = Rows(Parse(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "map", "locality_hexes.yaml"))), "localities");
        var at = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        foreach (var s in c.SettlementHexes) at[s.Id] = (s.Q, s.R);
        foreach (var l in c.MapLocations) at[l.Id] = (l.Q, l.R);
        foreach (var s in c.Sites) at[s.Id] = (s.Q, s.R);
        foreach (var l in localities) at[Text(l, "id")!] = (Int(l, "q"), Int(l, "r"));
        foreach (var region in c.PlayerRegions.Regions.Values)
        {
            var row = staged.Single(r => Text(r, "id") == region.Id);
            Assert.Equal(Text(row, "capital_location"), region.CapitalLocation);   // Q3: the nine draft labels
            Assert.Contains(region.CapitalLocation, region.MemberLocations);
            Assert.Same(region, c.PlayerRegions.AtCapital(region.CapitalLocation));
            foreach (var m in region.MemberLocations)
            {
                Assert.True(at.ContainsKey(m), m);
                Assert.Contains(at[m], region.MemberHexes);
                Assert.Same(region, c.PlayerRegions.AtLocation(m));
            }
        }
        // 326 approved members, less rosh_haayin_camp (deferred: no sourced position), each in exactly one region.
        Assert.Equal(325, c.PlayerRegions.Regions.Values.Sum(r => r.MemberLocations.Count));
        Assert.Null(c.PlayerRegions.AtLocation("rosh_haayin_camp"));
        foreach (var (member, from, to) in Moved)
        {
            Assert.Contains(member, Strings(staged.Single(r => Text(r, "id") == from), "member_locations"));
            Assert.Equal(to, c.PlayerRegions.AtLocation(member)!.Id);
        }
        Assert.DoesNotContain(staged.SelectMany(r => Assert.IsType<YamlSequenceNode>(Get(r, "member_hexes")).Children.Cast<YamlMappingNode>()),
            h => Int(h, "q") == 10 && Int(h, "r") == 4);   // in no region after the 4.4 km regrid
        Assert.Equal("galilee", c.PlayerRegions.AtHex(10, 4)!.Id);   // Q2, owner 2 Nov 2026: Hanita's own hex
        Assert.Equal("galilee", c.PlayerRegions.AtLocation("hanita")!.Id);
        Assert.Equal(524, c.PlayerRegions.Regions.Values.Sum(r => r.MemberHexes.Count));   // the staged 523 and (10,4)
        // Activation stays off: nothing initializes regions in a new game yet (#1689).
        var state = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(state.PlayerRegions);
        Assert.False(state.PopulationLedger.IsInitialized);
    }

    // The roster claims of the two inputs of map/locality_hexes.yaml, checked against the effective rosters the loader
    // places members by: a locality is "not one of the 25 game settlements" (localities_1948.yaml), and a region place is
    // one "no other roster holds" (region_places_1948.yaml). The sim refuses a member placed on two hexes, but only once it
    // is a member, and never a shared id on one hex, so these are pinned here. Any other shared id must share the hex.
    [Fact]
    public void ShippedLocalityHexIdsKeepTheRosterClaimsOfTheirInputs()
    {
        var map = Path.Combine(RealContentTests.ContentRoot(), "map");
        var c = PlayerRegionsContentTests.Load(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml")));
        var localities = Rows(Parse(File.ReadAllText(Path.Combine(map, "locality_hexes.yaml"))), "localities")
            .ToDictionary(l => Text(l, "id")!, l => (Int(l, "q"), Int(l, "r")), StringComparer.Ordinal);
        var regionPlaces = Rows(Parse(File.ReadAllText(Path.Combine(map, "region_places_1948.yaml"))), "places").Select(p => Text(p, "id")!).ToArray();
        Assert.Equal(3, regionPlaces.Length);
        Assert.All(regionPlaces, p => Assert.True(localities.ContainsKey(p), p));
        var others = c.MapLocations.Select(l => (l.Id, Hex: (l.Q, l.R))).Concat(c.Sites.Select(s => (s.Id, Hex: (s.Q, s.R)))).ToArray();
        Assert.DoesNotContain(c.SettlementHexes, s => localities.ContainsKey(s.Id));
        Assert.Empty(others.Select(o => o.Id).Intersect(regionPlaces, StringComparer.Ordinal));
        Assert.DoesNotContain(others, o => localities.TryGetValue(o.Id, out var hex) && hex != o.Hex);
    }

    [Fact]
    public void SchemaAdaptationRetainsEveryNamedMemberAndEvidenceStatus()
    {
        var evidence = Rows(Parse(Evidence), "regions");
        var staged = Rows(Parse(Staged), "player_regions");
        Assert.Equal(9, staged.Length);
        Assert.Equal(evidence.Select(r => Text(r, "id")), staged.Select(r => Text(r, "id")));
        Assert.Equal(326, staged.Sum(r => Strings(r, "member_locations").Length));
        var hexCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["galilee"] = 29, ["valleys"] = 28, ["haifa_sharon"] = 39, ["tel_aviv_jaffa"] = 7,
            ["central_plain"] = 17, ["jerusalem_hills"] = 91, ["northern_hills"] = 47,
            ["southern_coast"] = 22, ["negev"] = 243,
        };
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
            var hexes = Assert.IsType<YamlSequenceNode>(Get(row, "member_hexes")).Children
                .Select(n => Assert.IsType<YamlMappingNode>(n))
                .Select(h => (Q: int.Parse(Text(h, "q")!, CultureInfo.InvariantCulture),
                    R: int.Parse(Text(h, "r")!, CultureInfo.InvariantCulture))).ToArray();
            Assert.Equal(hexCounts[Text(row, "id")!], hexes.Length);
            Assert.Equal(hexes.OrderBy(h => h.Q).ThenBy(h => h.R), hexes);
            Assert.Equal("design value, not a historical boundary", Text(row, "design_note"));
        }
        Assert.Equal(523, staged.Sum(r => Assert.IsType<YamlSequenceNode>(Get(r, "member_hexes")).Children.Count));
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

    // Owner, 2 November 2026 evening: "Approve all nine as labels (Recommended)". Every capital_location is an approved
    // LABEL; this is still not the regional-capital model the owner declined on 30 October 2026.
    [Fact]
    public void AllNineShippedCapitalsAreApprovedLabels()
    {
        var c = PlayerRegionsContentTests.Load(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml")));
        Assert.Equal(9, c.PlayerRegions.Regions.Count);
        Assert.All(c.PlayerRegions.Regions.Values, r => Assert.Equal("APPROVED", r.CapitalApproval));
        Assert.Equal(0, c.PlayerRegions.Regions.Values.Count(r => r.CapitalApproval == "NEEDS-RESEARCH"));
    }

    // #1691 Q4, owner, 2 November 2026 evening: "Approve as drafted (Recommended)". The 20 opening field hexes, by the
    // per-region split of 27 October 2026 ("Approve as proposed"). Data only: nothing farms them yet. A person's check of
    // each row is pending. The 15 May control check is tools/tests/test_region_field_hexes.py (the sim never reads control).
    static readonly (string Region, int Q, int R)[] ApprovedFields =
    {
        ("galilee", 9, 5), ("galilee", 14, 7),
        ("valleys", 7, 11), ("valleys", 7, 12), ("valleys", 8, 13), ("valleys", 9, 13), ("valleys", 11, 12), ("valleys", 12, 11),
        ("haifa_sharon", 6, 10), ("haifa_sharon", 6, 12), ("haifa_sharon", 3, 13), ("haifa_sharon", 2, 14), ("haifa_sharon", 2, 15), ("haifa_sharon", 1, 15),
        ("tel_aviv_jaffa", -1, 20), ("tel_aviv_jaffa", -2, 21),
        ("central_plain", -5, 25), ("central_plain", -4, 25),
        ("southern_coast", -11, 31),
        ("negev", -10, 33),
    };

    [Fact]
    public void ShippedFieldHexesAreTheOwnerApprovedTwentyOnWorkableLandOfTheirOwnRegion()
    {
        var c = PlayerRegionsContentTests.Load(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml")));
        var split = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["galilee"] = 2, ["valleys"] = 6, ["haifa_sharon"] = 6, ["tel_aviv_jaffa"] = 2, ["central_plain"] = 2,
            ["jerusalem_hills"] = 0, ["northern_hills"] = 0, ["southern_coast"] = 1, ["negev"] = 1,
        };
        Assert.Equal(split.Keys.OrderBy(x => x, StringComparer.Ordinal), c.PlayerRegions.Regions.Keys);
        foreach (var region in c.PlayerRegions.Regions.Values)
        {
            Assert.Equal(split[region.Id], region.FieldHexes.Count);
            Assert.Equal(ApprovedFields.Where(f => f.Region == region.Id).Select(f => (f.Q, f.R)).OrderBy(h => h.Q).ThenBy(h => h.R),
                region.FieldHexes);
            foreach (var h in region.FieldHexes)
            {
                Assert.True(c.HexAt(h.Q, h.R)!.Land);
                Assert.Contains(c.HexAt(h.Q, h.R)!.Biome, new[] { "coastal_plain", "lowland", "valley" });
                Assert.Contains(h, region.MemberHexes);
                Assert.Same(region, c.PlayerRegions.AtHex(h.Q, h.R));
            }
        }
        var all = c.PlayerRegions.Regions.Values.SelectMany(r => r.FieldHexes).ToArray();
        Assert.Equal(20, all.Length);
        Assert.Equal(20, all.Distinct().Count());
        // Data only: activation stays off, so nothing reads the fields in a new game yet (#1689).
        Assert.Empty(Simulation.NewGame(c, 1, Mode.Historical).PlayerRegions);
    }

    // The staged evidence rows, before the 2 November fixes: with localities and sites accepted (and the three new
    // records placed), only the rows the ruling had to fix are still refused, each on a member place. Each refusal is
    // pinned by member and reason, and the refused member is dropped until the row loads, so every refused member is
    // listed: without locality or site placement the 14 moved members would be refused as "unknown place id" instead.
    [Theory]
    [InlineData("galilee", "hanita", "")]   // hanita on (10,4)
    [InlineData("valleys", "", "")]
    [InlineData("haifa_sharon", "kafr_thulth azzoun al_mansi kfar_saba_2", "")]
    [InlineData("tel_aviv_jaffa", "al_safiriyya", "rosh_haayin_camp")]   // the camp: deferred, no sourced position
    [InlineData("central_plain", "majdal_yaba al_tira al_muzayria qula tzrifin", "")]
    [InlineData("jerusalem_hills", "beit_liqya", "")]
    [InlineData("northern_hills", "zirin", "")]
    [InlineData("southern_coast", "iraq_al_manshiyya yasur", "")]
    [InlineData("negev", "", "")]
    public void StagedRowsLoadExceptWhereTheRulingMovedAMemberOrAHex(string id, string outside, string unknown)
    {
        const string Outside = "effective location lies outside member_hexes", Unknown = "unknown place id";
        var row = Rows(Parse(Staged), "player_regions").Single(r => Text(r, "id") == id);
        var members = Assert.IsType<YamlSequenceNode>(Get(row, "member_locations")).Children;
        var refused = new List<(string Member, string Reason)>();
        while (true)
        {
            ContentException? error = null;
            try { Assert.Contains(id, PlayerRegionsContentTests.Load(SingleRow(row)).PlayerRegions.Regions.Keys); }
            catch (ContentException e) { error = e; }
            if (error == null) break;
            Assert.Equal("regions.yaml", error.File);
            var match = System.Text.RegularExpressions.Regex.Match(error.Field, @"^player_regions\[0\]\.member_locations\[(\d+)\]$");
            Assert.True(match.Success, error.Message);
            var i = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var reason = error.Message.Substring(("regions.yaml: " + error.Field + ": ").Length);
            Assert.Contains(reason, new[] { Outside, Unknown });
            refused.Add((Assert.IsType<YamlScalarNode>(members[i]).Value!, reason));
            members.RemoveAt(i);
        }
        static IEnumerable<string> Split(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var expected = Split(outside).Select(m => (m, Outside)).Concat(Split(unknown).Select(m => (m, Unknown)));
        Assert.Equal(expected.OrderBy(x => x.m, StringComparer.Ordinal), refused.OrderBy(x => x.Member, StringComparer.Ordinal));
        // The members refused as off their hexes are this row's moves in the ruling's table (and Hanita, Q2, for Galilee).
        Assert.Equal(Moved.Where(x => x.From == id).Select(x => x.Member).Concat(id == "galilee" ? new[] { "hanita" } : Array.Empty<string>())
            .OrderBy(x => x, StringComparer.Ordinal), Split(outside).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void ApprovedNamedPlaceProjectionsLoadWithoutClaimingFullExtents()
    {
        var c = PlayerRegionsContentTests.Load(Projections);
        var projectionIds = Rows(Parse(Projections), "player_regions").Select(r => Text(r, "id")!).ToArray();
        Assert.Equal(3, projectionIds.Length);
        var evidence = Rows(Parse(Evidence), "regions");
        foreach (var region in projectionIds.Select(id => c.PlayerRegions.Regions[id]))
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

    // #1691 opening allocation, owner ruling "Approve as proposed", 27 October 2026: fields / housing projects
    // (places) / Industry per region, in ledger order. Method and limits: research_notes/gdd03-region-allocation.md.
    static readonly (string Id, int Fields, int Projects, int Places, int Industry)[] OpeningAllocation =
    {
        ("galilee", 2, 1, 3, 0), ("valleys", 6, 1, 3, 0), ("haifa_sharon", 6, 5, 15, 2),
        ("tel_aviv_jaffa", 2, 12, 36, 4), ("central_plain", 2, 1, 3, 0), ("jerusalem_hills", 0, 4, 12, 2),
        ("northern_hills", 0, 0, 0, 0), ("southern_coast", 1, 0, 0, 0), ("negev", 1, 0, 0, 0),
    };
    const string AllocationNote = "research_notes/gdd03-region-allocation.md";
    static YamlMappingNode Entry(YamlMappingNode region, string list, string id) =>
        Rows(region, list).Single(r => Text(r, "id") == id);
    static int Int(YamlMappingNode row, string key) => int.Parse(Text(row, key)!, CultureInfo.InvariantCulture);
    static void PartialEvidenceFromTheNote(YamlMappingNode row)
    {
        Assert.Equal("PARTIAL-EVIDENCE", Text(row, "status"));
        Assert.Contains(Rows(row, "sources"), s => Text(s, "file") == AllocationNote);
    }

    [Fact]
    public void OpeningAllocationRecordsTheApprovedSplitAsPartialEvidence()
    {
        var root = Parse(Evidence);
        var decision = Map(Map(root, "owner_design_decisions"), "opening_allocation");
        Assert.Equal("APPROVED", Text(decision, "status"));
        Assert.Equal("Approve as proposed", Text(decision, "label"));
        var regions = Rows(root, "regions");
        Assert.Equal(OpeningAllocation.Select(a => a.Id), regions.Select(r => Text(r, "id")));
        foreach (var (region, want) in regions.Zip(OpeningAllocation))
        {
            var fields = Entry(region, "fields", want.Id + "_opening_field_allocation");
            PartialEvidenceFromTheNote(fields);
            Assert.Equal(want.Fields, Int(fields, "slots"));
            Assert.Equal("Deferred", Text(fields, "farmer_tiers"));
            Assert.True(Unknown(Get(fields, "hexes")));
            Assert.True(Unknown(Get(fields, "prepared_at_opening")));
            var housing = Entry(region, "facilities", want.Id + "_opening_housing_allocation");
            PartialEvidenceFromTheNote(housing);
            Assert.Equal(want.Projects, Int(housing, "projects"));
            Assert.Equal(want.Places, Int(housing, "places"));
            Assert.Equal(3 * Int(housing, "projects"), Int(housing, "places"));
            var industry = Entry(region, "facilities", want.Id + "_opening_industry_allocation");
            PartialEvidenceFromTheNote(industry);
            Assert.Equal(want.Industry, Int(industry, "slots"));
            // The population basis is scoped to these two rows; no facility inventory is inferred from it.
            var inventory = Entry(region, "facilities", want.Id + "_facility_inventory");
            Assert.True(Unknown(Get(inventory, "value")));
            Assert.Contains("no facility inventory inferred from population", Text(inventory, "needed"));
        }
        Assert.True(File.Exists(Path.Combine(RealContentTests.ContentRoot(), "..", AllocationNote)));
    }

    [Fact]
    public void OpeningAllocationTotalsEqualTheBalanceCatalogue()
    {
        var catalogue = Map(Parse(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "gdd03-balance.yaml"))), "values");
        int Value(string key) => int.Parse(Assert.IsType<YamlScalarNode>(
            Assert.Single(Assert.IsType<YamlSequenceNode>(Get(Map(catalogue, key), "values")).Children)).Value!, CultureInfo.InvariantCulture);
        var regions = Rows(Parse(Evidence), "regions");
        int Sum(string list, string suffix, string key) =>
            regions.Sum(r => Int(Entry(r, list, Text(r, "id") + suffix), key));
        Assert.Equal(20, Value("start.prepared_fields"));
        Assert.Equal(Value("start.prepared_fields"), Sum("fields", "_opening_field_allocation", "slots"));
        Assert.Equal(Value("start.permanent_housing_projects"), Sum("facilities", "_opening_housing_allocation", "projects"));
        Assert.Equal(Value("start.housing_capacity"), Sum("facilities", "_opening_housing_allocation", "places"));
        Assert.Equal(Value("start.industry_slots"), Sum("facilities", "_opening_industry_allocation", "slots"));
        Assert.Equal((24, 72, 8), (Value("start.permanent_housing_projects"), Value("start.housing_capacity"), Value("start.industry_slots")));
    }

    [Fact]
    public void Gdd01ConsumesAuthoredRegionsWhileLegacyStillIgnoresThem()
    {
        var reads = new List<string>();
        var authored = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml"));
        var old = LoadShipped(authored, reads.Add);
        Assert.Contains("regions.yaml", reads);
        Assert.Contains("map/locality_hexes.yaml", reads);
        Assert.NotEmpty(old.PlayerRegions.Regions);
        Assert.Throws<ContentException>(() => LoadShipped(null));
        var files = new Dictionary<string, string>(Fixture.Files) { ["regions.yaml"] = Staged };
        Content Legacy() => ContentLoader.Load(rel => files.TryGetValue(rel, out var text) ? text : null, Array.Empty<string>());
        var legacy = Legacy();
        files.Remove("regions.yaml");
        Assert.Equal(legacy.Hash, Legacy().Hash);
        Assert.Equal(Simulation.NewGame(legacy, 1, Mode.Historical).Describe(),
            Simulation.NewGame(Legacy(), 1, Mode.Historical).Describe());
    }
}
