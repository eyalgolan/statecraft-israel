using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

/// <summary>The designated sites (Phase 1 PR 5, Task 5.1): content/sites.yaml holds sourced facts only (the site, its names,
/// hex, founding date and sources); the build turns and cost are labelled design values in rules.yaml mvp: (Q-CUT-1, R-LOOP-005).
/// The loader reads sites.yaml only when systems.loop is gdd, so the legacy content hash does not move.</summary>
public class SitesContentTests
{
    static Content Load(Dictionary<string, string>? extra = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));

    [Fact]
    public void TheGddFixtureLoadsItsSiteAndBuildRules()
    {
        var c = Load();
        var site = Assert.Single(c.Sites);
        Assert.Equal(("site_a", "Site A", "אתר א", 2, 0), (site.Id, site.Name, site.NameHe, site.Q, site.R));
        Assert.Equal(new[] { "yishuv" }, site.Kinds);
        Assert.Equal(new DateTime(1949, 6, 12), site.Founded);
        Assert.Equal("fact-checked", site.Status);
        Assert.Equal(new DateTime(2026, 10, 14), Assert.Single(site.Sources).Checked);
        var mvp = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((3, 1.5), (mvp.BuildYishuvTurns, mvp.BuildYishuvCost));
    }

    [Fact]
    public void TheLegacyLoopReadsNoSites()
    {
        // The legacy loop (a fixture since the cutover), with a sites file next to it that it must not read.
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["sites.yaml"] = Fixture.GddSites }));
        Assert.Empty(c.Sites);
    }

    /// <summary>Owner decision (#378, 14 October 2026): the Phase 1 site is Mefalsim, founded 12 June 1949 (the ceremony of
    /// going onto the land, question 5), at the hex of its present coordinates (question 3), from the research in #459.</summary>
    [Fact]
    public void TheSliceHasMefalsimFromContent()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var site = Assert.Single(c.Sites, x => x.Id == "mefalsim");
        Assert.Equal(("mefalsim", "Mefalsim", "מפלסים", -10, 27), (site.Id, site.Name, site.NameHe, site.Q, site.R));
        Assert.Equal(new[] { "yishuv" }, site.Kinds);
        Assert.Equal(new DateTime(1949, 6, 12), site.Founded);
        Assert.Equal("fact-checked", site.Status);
        Assert.Contains(site.Sources, s => s.Url == "https://benyehuda.org/read/14684");
        Assert.Contains(site.Sources, s => s.Url.StartsWith("https://israelalbum.org.il/", StringComparison.Ordinal));
        Assert.All(site.Sources, s => Assert.NotEqual(default, s.Checked));
        var mvp = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((6, 2.0), (mvp.BuildYishuvTurns, mvp.BuildYishuvCost));   // design values (rules.yaml mvp:, R-LOOP-005)
        Assert.Equal(0, c.Rules.AttentionCost["pioneer_work"]);   // D-A6: unit orders are free
        Assert.Equal(0, c.Rules.AttentionCost["stop_work"]);
    }

    /// <summary>Question 2 (#378, the recommendation the owner took): a designated site may sit on an armistice line hex.
    /// Mefalsim's hex is an Egypt-Israel line hex in armistice_hexes_1949.yaml, a display-only file the sim never reads, and the
    /// slice still loads.</summary>
    [Fact]
    public void ASiteOnAnArmisticeLineHexIsAllowed()
    {
        var root = RealContentTests.ContentRoot();
        var map = new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(root, "map", "armistice_hexes_1949.yaml")));
        var lineHexes = ((List<object>)map["line_hexes"]).Cast<List<object>>().Select(x => (int.Parse((string)x[0], CultureInfo.InvariantCulture), int.Parse((string)x[1], CultureInfo.InvariantCulture)));
        var site = Assert.Single(ContentLoader.Load(root).Sites, x => x.Id == "mefalsim");
        Assert.Contains((site.Q, site.R), lineHexes);
    }

    /// <summary>R-SITE-004, Q-SITE-1 and Q-SITE-2 (owner, 17 October 2026, #782: "take all of the plan's recommendations"): the
    /// MVP's yishuv sites are the four of the settlements note whose founding day two sources agree on (criteria 1, 2 and 4 of
    /// Task 5.0). Each is founded inside the MVP's years and cites at least two sources with a checked date. Kiryat Shmona and
    /// Beit Shemesh join them (owner, 17 October 2026, #782 "Sign off now"); their days are the owner's (#785,
    /// issuecomment-6237624501): 18 July 1949, and December 1950 under the monthly turn, 15 December 1950.</summary>
    [Fact]
    public void TheMvpYishuvSitesAreTheSourcedList()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var yishuvim = c.Sites.Where(x => x.Kinds.Contains("yishuv")).ToList();
        Assert.Equal(new[] { "beit_kama", "beit_shemesh", "gilat", "kiryat_shmona", "mefalsim", "reim" }, yishuvim.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal));
        var founded = yishuvim.ToDictionary(x => x.Id, x => x.Founded);
        Assert.Equal(new DateTime(1949, 4, 18), founded["beit_kama"]);
        Assert.Equal(new DateTime(1950, 12, 15), founded["beit_shemesh"]);
        Assert.Equal(new DateTime(1949, 12, 4), founded["gilat"]);
        Assert.Equal(new DateTime(1949, 7, 18), founded["kiryat_shmona"]);
        Assert.Equal(new DateTime(1949, 6, 12), founded["mefalsim"]);
        Assert.Equal(new DateTime(1949, 10, 20), founded["reim"]);
        // Each hex is the one tools/hexgrid.py gives for the row's sourced point (the CBS point for the two that joined, rows 7
        // and 16 of the settlements note), so moving a row to another hex is a content change a test sees.
        var hexes = yishuvim.ToDictionary(x => x.Id, x => (x.Q, x.R));
        Assert.Equal((-8, 28), hexes["beit_kama"]);
        Assert.Equal((-3, 24), hexes["beit_shemesh"]);
        Assert.Equal((-10, 30), hexes["gilat"]);
        Assert.Equal((14, 2), hexes["kiryat_shmona"]);
        Assert.Equal((-10, 27), hexes["mefalsim"]);
        Assert.Equal((-12, 29), hexes["reim"]);
        Assert.All(yishuvim, x => Assert.True(x.Sources.Count(s => s.Checked != default) >= 2, x.Id));
    }

    /// <summary>Q-CAMP-2 (owner, 17 October 2026, #785: "Both"): the camp sites are Camp 80 near Pardes Hanna and Tzrifin, both
    /// military camps (owner, "Both military", 17 October 2026, #927), each for its nearest MVP city (Q-CAMP-1, R-CAMP-002).</summary>
    [Fact]
    public void TheMvpCampSitesAreCamp80AndTzrifin()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var camps = c.Sites.Where(Projects.IsCamp).Select(x => (x.Id, x.Name, string.Join(",", x.Kinds), x.City));
        Assert.Equal(new[] { ("camp_80", "Camp 80", "military_camp", "hadera"), ("tzrifin", "Tzrifin", "military_camp", "rishon_lezion") }, camps);
    }

    /// <summary>R-CAMP-002 (owner, 17 October 2026, #927): a camp site's city is the MVP city nearest by hex distance, and a tie
    /// goes to the nearest by km. Camp 80 has one city a hex away, Hadera. Tzrifin has four, and Rishon LeZion, about 3.9 km
    /// away, is the nearest of them.</summary>
    // R-CAMP-002
    [Fact]
    public void ACampSiteServesTheNearestCityByHexThenByKm()
    {
        var root = RealContentTests.ContentRoot();
        var c = ContentLoader.Load(root);
        // The point each camp row's hex comes from, as the row's comment in sites.yaml gives it (Hebrew Wikipedia).
        var sitePoints = new Dictionary<string, (double Lat, double Lon)> { ["camp_80"] = (32.4628, 34.9866), ["tzrifin"] = (31.9587, 34.8395) };
        var yaml = new YamlStream();
        using (var r = new StringReader(File.ReadAllText(Path.Combine(root, "map", "settlements_1948.yaml")))) yaml.Load(r);
        var rows = (YamlSequenceNode)((YamlMappingNode)yaml.Documents[0].RootNode)[new YamlScalarNode("settlements")];
        string Scalar(YamlMappingNode m, string key) => ((YamlScalarNode)m[new YamlScalarNode(key)]).Value!;
        var cityPoints = rows.Cast<YamlMappingNode>().ToDictionary(m => Scalar(m, "id"),
            m => (Lat: double.Parse(Scalar(m, "lat"), CultureInfo.InvariantCulture), Lon: double.Parse(Scalar(m, "lon"), CultureInfo.InvariantCulture)));
        // Straight-line km on a local flat projection; at a few km the curvature is far below the gaps between the cities.
        static double Km((double Lat, double Lon) a, (double Lat, double Lon) b) =>
            111.32 * Math.Sqrt(Math.Pow(a.Lat - b.Lat, 2) + Math.Pow((a.Lon - b.Lon) * Math.Cos((a.Lat + b.Lat) / 2 * Math.PI / 180), 2));
        var hexes = c.SettlementHexes.ToDictionary(x => x.Id);
        var nearest = new Dictionary<string, string[]>();
        foreach (var site in c.Sites.Where(Projects.IsCamp))
        {
            var byHex = c.Cities.Select(x => (x.Id, Hexes: MapActions.Distance(site.Q, site.R, hexes[x.Id].Q, hexes[x.Id].R))).ToList();
            var tied = byHex.Where(x => x.Hexes == byHex.Min(y => y.Hexes)).Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            nearest[site.Id] = tied;
            Assert.Equal(tied.OrderBy(x => Km(sitePoints[site.Id], cityPoints[x])).First(), site.City);
        }
        Assert.Equal(new[] { "hadera" }, nearest["camp_80"]);
        Assert.Equal(new[] { "petah_tikva", "rehovot", "rishon_lezion", "tel_aviv" }, nearest["tzrifin"]);
    }

    /// <summary>Q-FIELD-1 and Q-SITE-3 (#782): each yishuv site has one field site in its own hex, and no other site shares a
    /// yishuv's hex (R-SITE-005 allows it only for another kind).</summary>
    [Fact]
    public void EachYishuvSiteHasOneFieldSiteInItsHex()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        foreach (var y in c.Sites.Where(x => x.Kinds.Contains("yishuv")))
        {
            var others = c.Sites.Where(x => x != y && x.Q == y.Q && x.R == y.R).ToList();
            var field = Assert.Single(others);
            Assert.Equal(new[] { "field" }, field.Kinds);
        }
    }

    [Fact]
    public void TheSitesFileHoldsNoBalanceValue()
    {
        // sites.yaml is sourced facts; the build turns and cost are design values in rules.yaml mvp: since the cutover
        // (Q-CUT-1), listed and tagged under DesignValuesTests (R-LOOP-005).
        var sites = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "sites.yaml"));
        Assert.DoesNotMatch(@"(?m)^[^#\n]*\b\w*(turns|cost)\w*\s*:|build_yishuv", sites);   // no such key outside a comment
    }

    static string Site(string replace, string with) => Fixture.GddSites.Replace(replace, with);
    static string Rules(string mvp) => Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts) + mvp;

    public static IEnumerable<object[]> BadRows() => new List<object[]>
    {
        new object[] { "sites.yaml", Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", ""), "sites[site_a].id", "duplicate id" },
        new object[] { "sites.yaml", Site("name: Site A", "name: ' '"), "sites[site_a].name", "required" },
        new object[] { "sites.yaml", Site("name_he: אתר א", "name_he: ''"), "sites[site_a].name_he", "required" },
        new object[] { "sites.yaml", Site("q: 2", "q: 0"), "sites[site_a].q", "hex q=0 r=0 is not a land hex" },
        new object[] { "sites.yaml", Site("q: 2", "q: 9"), "sites[site_a].q", "hex q=9 r=0 is not a land hex" },
        new object[] { "sites.yaml", Site("q: 2", "q: 1"), "sites[site_a].q", "hex q=1 r=0 holds the 1948 settlement town_a" },
        new object[] { "sites.yaml", Site("kinds: [yishuv]", "kinds: [yishuv, airfield]"), "sites[site_a].kinds", "unknown kind 'airfield'" },
        new object[] { "sites.yaml", Site("founded: 1949-06-12", "founded: 1948-05-14"), "sites[site_a].founded", "between 15 May 1948 and 31 December 1951" },
        new object[] { "sites.yaml", Site("founded: 1949-06-12", "founded: 1952-01-01"), "sites[site_a].founded", "between 15 May 1948 and 31 December 1951" },
        new object[] { "sites.yaml", Site("    founded: 1949-06-12\n", ""), "sites[site_a].founded", "between 15 May 1948 and 31 December 1951" },
        new object[] { "sites.yaml", Site("    sources:\n      - { url: \"https://example.org/site_a\", checked: 2026-10-14 }\n", ""), "sites[site_a].sources", "required" },
        new object[] { "sites.yaml", Site(", checked: 2026-10-14", ""), "sites[site_a].sources[0]", "url and checked date" },
        new object[] { "sites.yaml", Site("url: \"https://example.org/site_a\"", "url: ''"), "sites[site_a].sources[0]", "url and checked date" },
        new object[] { "sites.yaml", Site("status: fact-checked", "status: done"), "sites[site_a].status", "unknown status 'done'" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("build_yishuv_turns: 3", "build_yishuv_turns: 0")), "mvp.build_yishuv_turns", "must be >= 1" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("build_yishuv_cost: 1.5", "build_yishuv_cost: -1")), "mvp.build_yishuv_cost", "must be a finite number >= 0" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules.Replace("build_yishuv_cost: 1.5", "build_yishuv_cost: .nan")), "mvp.build_yishuv_cost", "must be a finite number >= 0" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules).Replace(", pioneer_work: 0", ""), "attention_cost.pioneer_work", "missing cost" },
        new object[] { "rules.yaml", Rules(Fixture.GddRules).Replace(", stop_work: 0", ""), "attention_cost.stop_work", "missing cost" },
    };

    /// <summary>A second fixture site, site_b, at site_a's hex (2, 0); <paramref name="row"/> replaces its founding line.</summary>
    static string Second(string kinds, string row) => Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", "")
        .Replace("site_a", "site_b").Replace("Site A", "Site B").Replace("kinds: [yishuv]", "kinds: " + kinds)
        .Replace("    founded: 1949-06-12\n", row);

    // R-SITE-004, R-SITE-005
    public static IEnumerable<object[]> BadKinds() => new List<object[]>
    {
        new object[] { Site("kinds: [yishuv]", "kinds: [airfield]"), "sites[site_a].kinds", "unknown kind 'airfield'" },
        new object[] { Site("kinds: [yishuv]", "kinds: []"), "sites[site_a].kinds", "required" },
        new object[] { Second("[yishuv]", "    founded: 1949-06-12\n"), "sites[site_b].q", "hex q=2 r=0 already holds the yishuv site site_a" },
        new object[] { Second("[military_camp]", ""), "sites[site_b].city", "required for a camp site" },
        new object[] { Second("[hakhshara]", "    city: nowhere\n"), "sites[site_b].city", "unknown city 'nowhere'" },
        new object[] { Second("[field]", "    city: town_a\n"), "sites[site_b].city", "only a camp site has a city" },
        new object[] { Second("[field]", "    founded: 1949-06-12\n"), "sites[site_b].founded", "only a yishuv site has a founding date" },
    };

    [Theory]
    [MemberData(nameof(BadKinds))]
    public void BadKindsFailLoad(string text, string field, string message)
        => ABadValueFailsNamingTheFileAndField("sites.yaml", text, field, message);

    /// <summary>Sites of other kinds may share a yishuv's hex (Q-SITE-3), and a camp names a city of cities.yaml (Q-CAMP-1).</summary>
    [Fact]
    public void AFieldAndACampMayShareAYishuvsHex()
    {
        var text = Second("[field]", "") + Second("[military_camp]", "    city: town_a\n").Replace(Fixture.GddSites, "").Replace("site_b", "site_c").Replace("Site B", "Site C");
        var c = Load(new Dictionary<string, string> { ["sites.yaml"] = text });
        Assert.Equal(new[] { "site_a", "site_b", "site_c" }, c.Sites.Select(x => x.Id));
        Assert.Equal("town_a", c.Sites[2].City);
        Assert.Equal(default, c.Sites[1].Founded);
    }

    [Theory]
    [MemberData(nameof(BadRows))]
    public void ABadValueFailsNamingTheFileAndField(string file, string text, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { [file] = text }));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void TheGddLoopNeedsTheSitesFile()
    {
        var o = Fixture.Gdd();
        o.Remove("sites.yaml");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(o)));
        Assert.Equal("sites.yaml", ex.File);
        Assert.Contains("missing file", ex.Message);
    }
}
