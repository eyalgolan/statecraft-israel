using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;
using YamlDotNet.RepresentationModel;

/// <summary>Hebrew display names in content (#558): a `*_he` value sits next to the English one, and its own
/// `*_he_sources` list (url and checked date) next to it, as people.yaml's name_he does with biography_sources.</summary>
public class HebrewNameTests
{
    const string Src = "[{ url: 'https://example.org/he', checked: 2026-10-14 }]";

    static Content Load(Dictionary<string, string> files) => ContentLoader.Load(Fixture.Write(files));

    static Dictionary<string, string> Replace(string file, string from, string to) =>
        new Dictionary<string, string> { [file] = Fixture.Files[file].Replace(from, to) };

    [Fact]
    public void HebrewNamesLoadWithTheirSources()
    {
        var files = Replace("offices.yaml", "    title: Prime Minister\n",
            $"    title: Prime Minister\n    title_he: ראש הממשלה\n    title_he_sources: {Src}\n    title_he_f: ראשת הממשלה\n    title_he_f_sources: {Src}\n");
        files["parties.yaml"] = Fixture.Files["parties.yaml"].Replace("    name: Alpha\n", $"    name: Alpha\n    name_he: אלפא\n    name_he_sources: {Src}\n");
        files["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("    body: Council\n", $"    body: Council\n    body_he: מועצה\n    body_he_sources: {Src}\n");
        files["governments.yaml"] = Fixture.Files["governments.yaml"].Replace("    name: Government One\n", $"    name: Government One\n    name_he: ממשלה אחת\n    name_he_sources: {Src}\n");
        files["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"].Replace("name: Town A,", $"name: Town A, name_he: עיר א, name_he_sources: {Src},");
        var c = Load(files);
        var pm = c.Offices.Single(o => o.Id == "prime_minister");
        Assert.Equal(("ראש הממשלה", "ראשת הממשלה"), (pm.TitleHe, pm.TitleHeF));
        Assert.Equal("https://example.org/he", pm.TitleHeSources.Single().Url);
        Assert.Equal(new DateTime(2026, 10, 14), pm.TitleHeFSources.Single().Checked);
        Assert.Equal("אלפא", c.Parties.Single(p => p.Id == "alpha").NameHe);
        Assert.Equal("", c.Parties.Single(p => p.Id == "beta").NameHe);   // absent: the UI keeps the English
        Assert.Equal("מועצה", c.Legislatures.Single(l => l.Id == "council").BodyHe);
        Assert.Equal("ממשלה אחת", c.Governments.Single(g => g.Id == "gov1").NameHe);
        Assert.Equal("Town A", c.SettlementNames["town_a"]);
    }

    [Theory]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he: ראש הממשלה\n", "offices[prime_minister].title_he_sources")]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he_sources: " + Src + "\n", "offices[prime_minister].title_he")]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he_f: ראשת הממשלה\n    title_he_f_sources: " + Src + "\n", "offices[prime_minister].title_he")]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he: ראש הממשלה\n    title_he_sources: " + Src + "\n    title_he_f: ראשת הממשלה\n", "offices[prime_minister].title_he_f_sources")]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he: ראש הממשלה\n    title_he_sources: [{ url: 'https://example.org/he' }]\n", "offices[prime_minister].title_he_sources[0]")]
    [InlineData("offices.yaml", "    title: Prime Minister\n", "    title: Prime Minister\n    title_he: ראש הממשלה\n    title_he_sources: [{ url: 'example.org/he', checked: 2026-10-14 }]\n", "offices[prime_minister].title_he_sources[0]")]
    [InlineData("parties.yaml", "    name: Alpha\n", "    name: Alpha\n    name_he: Alpha\n    name_he_sources: " + Src + "\n", "parties[alpha].name_he")]
    [InlineData("parties.yaml", "    name: Alpha\n", "    name: Alpha\n    name_he: אלפא\n    name_he_sources: []\n", "parties[alpha].name_he_sources")]
    [InlineData("legislatures.yaml", "    body: Council\n", "    body: Council\n    body_he: מועצה\n", "legislatures[council].body_he_sources")]
    [InlineData("governments.yaml", "    name: Government One\n", "    name: Government One\n    name_he: ממשלה\n", "governments[gov1].name_he_sources")]
    [InlineData("map/settlements_1948.yaml", "name: Town A,", "name: Town A, name_he: עיר א,", "settlements[town_a].name_he_sources")]
    [InlineData("map/settlements_1948.yaml", "name: Town A,", "name: Town A, name_he_sources: " + Src + ",", "settlements[town_a].name_he")]
    [InlineData("map/settlements_1948.yaml", "name: Town A,", "name: Town A, name_he: עיר א, name_he_sources: [{ url: 'https://example.org/he' }],", "settlements[town_a].name_he_sources[0]")]
    public void AHebrewNameNeedsItsSourcesAndTheSourcesNeedTheName(string file, string from, string to, string field)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Replace(file, from, to)));
        Assert.Equal(file, ex.File);
        Assert.Equal(field, ex.Field);
    }

    // ---- the real content ------------------------------------------------------------------------------------------

    static readonly string Root = RealContentTests.ContentRoot();
    static readonly Content C = ContentLoader.Load(Root);
    static readonly Regex HebrewKey = new Regex(@"^[a-z_]+_he(_f)?$");

    static IEnumerable<YamlMappingNode> Rows(string file, string list)
    {
        var y = new YamlStream();
        y.Load(new StringReader(File.ReadAllText(Path.Combine(Root, file))));
        var root = (YamlMappingNode)y.Documents[0].RootNode;
        return ((YamlSequenceNode)root.Children[new YamlScalarNode(list)]).Children.Cast<YamlMappingNode>();
    }

    public static IEnumerable<object[]> Files() => new[]
    {
        new object[] { "offices.yaml", "offices" }, new object[] { "parties.yaml", "parties" },
        new object[] { "governments.yaml", "governments" }, new object[] { "legislatures.yaml", "legislatures" },
        new object[] { "map/settlements_1948.yaml", "settlements" },
    };

    /// <summary>Every Hebrew value in these files has its sources beside it, each an https URL with the day it was read.</summary>
    [Theory]
    [MemberData(nameof(Files))]
    public void EveryHebrewNameInTheRealContentHasASource(string file, string list)
    {
        int seen = 0;
        foreach (var row in Rows(file, list))
        {
            var id = ((YamlScalarNode)row.Children[new YamlScalarNode("id")]).Value;
            var keys = row.Children.Keys.Cast<YamlScalarNode>().Select(k => k.Value!).ToList();
            foreach (var key in keys.Where(k => HebrewKey.IsMatch(k)))
            {
                seen++;
                var value = ((YamlScalarNode)row.Children[new YamlScalarNode(key)]).Value ?? "";
                Assert.True(value.Any(ch => ch >= 'א' && ch <= 'ת'), $"{file} {id}.{key} has no Hebrew letter");
                Assert.True(row.Children.TryGetValue(new YamlScalarNode(key + "_sources"), out var node), $"{file} {id}.{key} has no {key}_sources");
                var sources = ((YamlSequenceNode)node).Children.Cast<YamlMappingNode>().ToList();
                Assert.NotEmpty(sources);
                foreach (var s in sources)
                {
                    Assert.StartsWith("https://", ((YamlScalarNode)s.Children[new YamlScalarNode("url")]).Value);
                    var day = DateTime.ParseExact(((YamlScalarNode)s.Children[new YamlScalarNode("checked")]).Value!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    Assert.True(day >= new DateTime(2026, 10, 14), $"{file} {id}.{key}: checked {day:yyyy-MM-dd}, before the #558 research");
                }
            }
            foreach (var key in keys.Where(k => k.EndsWith("_he_sources", StringComparison.Ordinal) || k.EndsWith("_he_f_sources", StringComparison.Ordinal)))
                Assert.Contains(key.Substring(0, key.Length - "_sources".Length), keys);
        }
        Assert.True(seen > 0, $"{file}: no Hebrew names");
    }

    /// <summary>Coverage as researched for #558: only the rows with no sourced Hebrew name are empty.</summary>
    [Fact]
    public void EveryShownNameHasAHebrewFormButTheUnsourcedOnes()
    {
        Assert.Equal(new[] { "rationing_minister" }, C.Offices.Where(o => o.TitleHe == "").Select(o => o.Id));
        Assert.Equal(new[] { "independent" }, C.Parties.Where(p => p.NameHe == "").Select(p => p.Id));
        Assert.All(C.Governments, g => Assert.NotEqual("", g.NameHe));
        Assert.All(C.Legislatures, l => Assert.NotEqual("", l.BodyHe));
        Assert.All(Rows("map/settlements_1948.yaml", "settlements"), r => Assert.True(r.Children.ContainsKey(new YamlScalarNode("name_he"))));
        // Feminine titles only where a woman held the office (owner, 2026-10-14): Golda Meir, Labour and Foreign Affairs.
        Assert.Equal(new[] { "foreign_minister", "labour_minister" }, C.Offices.Where(o => o.TitleHeF != "").Select(o => o.Id).OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>The owner's 2026-10-14 answers: places in their 1948 form, parties by their short common names, the First
    /// Knesset mirroring its English name, a feminine title where a woman held the office.</summary>
    [Fact]
    public void TheOwnerRulingsHold()
    {
        string Settlement(string id) => ((YamlScalarNode)Rows("map/settlements_1948.yaml", "settlements")
            .Single(r => ((YamlScalarNode)r.Children[new YamlScalarNode("id")]).Value == id).Children[new YamlScalarNode("name_he")]).Value!;
        Assert.Equal("תל אביב", Settlement("tel_aviv"));
        Assert.Equal("לוד", Settlement("lod"));
        Assert.Equal("מפא\"י", C.Parties.Single(p => p.Id == "mapai").NameHe);
        Assert.Equal("מק\"י", C.Parties.Single(p => p.Id == "maki").NameHe);
        Assert.Equal("האספה המכוננת (הכנסת הראשונה)", C.Legislatures.Single(l => l.Id == "first_knesset").BodyHe);
        var labour = C.Offices.Single(o => o.Id == "labour_minister");
        Assert.Equal(("שר העבודה", "שרת העבודה"), (labour.TitleHe, labour.TitleHeF));
    }
}
