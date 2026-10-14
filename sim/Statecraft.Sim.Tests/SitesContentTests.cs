using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.Serialization;

/// <summary>The designated sites (Phase 1 PR 5, Task 5.1): content/sites.yaml holds sourced facts only (the site, its names,
/// hex, founding date and sources); the build turns and cost are placeholders in MvpSlice.Overlay or test fixtures (ruling 2).
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
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Empty(c.Sites);
    }

    /// <summary>Owner decision (#378, 14 October 2026): the Phase 1 site is Mefalsim, founded 12 June 1949 (the ceremony of
    /// going onto the land, question 5), at the hex of its present coordinates (question 3), from the research in #459.</summary>
    [Fact]
    public void TheSliceHasMefalsimFromContent()
    {
        var c = MvpSlice.Load(RealContentTests.ContentRoot());
        var site = Assert.Single(c.Sites);
        Assert.Equal(("mefalsim", "Mefalsim", "מפלסים", -10, 27), (site.Id, site.Name, site.NameHe, site.Q, site.R));
        Assert.Equal(new[] { "yishuv" }, site.Kinds);
        Assert.Equal(new DateTime(1949, 6, 12), site.Founded);
        Assert.Equal("fact-checked", site.Status);
        Assert.Contains(site.Sources, s => s.Url == "https://benyehuda.org/read/14684");
        Assert.Contains(site.Sources, s => s.Url.StartsWith("https://israelalbum.org.il/", StringComparison.Ordinal));
        Assert.All(site.Sources, s => Assert.NotEqual(default, s.Checked));
        var mvp = c.RulesAt(c.Eras[0].From).Mvp!;
        Assert.Equal((6, 2.0), (mvp.BuildYishuvTurns, mvp.BuildYishuvCost));   // PLACEHOLDER: owner sets (MvpSlice.Overlay)
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
        var site = Assert.Single(MvpSlice.Load(root).Sites);
        Assert.Contains((site.Q, site.R), lineHexes);
    }

    [Fact]
    public void NoBalanceValueIsUnderContent()
    {
        var root = RealContentTests.ContentRoot();
        var sites = File.ReadAllText(Path.Combine(root, "sites.yaml"));
        Assert.DoesNotMatch(@"(?m)^[^#\n]*\b\w*(turns|cost)\w*\s*:|build_yishuv", sites);   // no such key outside a comment
        var overrides = Directory.Exists(Path.Combine(root, "rules")) ? Directory.GetFiles(Path.Combine(root, "rules"), "*.yaml") : new string[0];
        foreach (var f in new[] { "rules.yaml" }.Concat(overrides.Select(x => "rules/" + Path.GetFileName(x))))
            Assert.DoesNotMatch(@"build_yishuv|pioneer_work|stop_work", File.ReadAllText(Path.Combine(root, f)));
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
