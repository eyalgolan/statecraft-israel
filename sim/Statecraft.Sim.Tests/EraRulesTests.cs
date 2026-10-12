using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Sim;

/// <summary>Rules per era (#58): content/rules/&lt;era&gt;.yaml overrides only the keys it names, is read with the strict
/// Rules type, and Content.RulesAt(date) returns the rules of the era the date is in.</summary>
public class EraRulesTests
{
    const string TwoEras = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1952-01-01
  - id: later
    name: A Later Chapter
    from: 1952-01-01
    to: 1957-01-01
";

    static string Write(string? laterRules, string eras = TwoEras)
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = eras });
        if (laterRules != null)
        {
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), laterRules);
        }
        return dir;
    }

    [Fact]
    public void RealContentHasNoOverrideAndRulesAtIsRulesYaml()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Empty(c.EraRules);
        Assert.Same(c.Rules, c.RulesAt(c.Rules.Start));
        Assert.Same(c.Rules, c.RulesAt(new DateTime(1956, 12, 31)));
        Assert.Same(c.Rules, c.RulesAt(new DateTime(1960, 1, 1)));   // outside every era: the base rules
    }

    [Fact]
    public void AnOverrideChangesOnlyItsKeys()
    {
        var c = ContentLoader.Load(Write("attention_per_turn: 5\nattention_cost: { set_policy: 2 }\n"));
        var b = c.Rules; var l = c.RulesAt(new DateTime(1952, 1, 1));
        Assert.NotSame(b, l);
        Assert.Equal(5, l.AttentionPerTurn);
        Assert.Equal(3, b.AttentionPerTurn);
        Assert.Equal(2, l.AttentionCost["set_policy"]);                 // maps merge key by key
        Assert.Equal(b.AttentionCost["appoint"], l.AttentionCost["appoint"]);
        Assert.Equal(b.StartRung, l.StartRung);                          // every other key is rules.yaml's
        Assert.Equal(b.Start, l.Start);
        Assert.Equal(b.AppointStabilityCostOtherParty, l.AppointStabilityCostOtherParty);
        Assert.Same(b, c.RulesAt(new DateTime(1951, 12, 31)));           // the founding era keeps rules.yaml
    }

    [Fact]
    public void AListInAnOverrideReplacesTheWholeList()
    {
        var c = ContentLoader.Load(Write("rung_min_mobilization: [0, 0.1, 0.2, 0.3, 0.4]\n"));
        Assert.Equal(new[] { 0, 0.1, 0.2, 0.3, 0.4 }, c.RulesAt(new DateTime(1953, 1, 1)).RungMinMobilization);
    }

    [Fact]
    public void AnUnknownKeyInAnOverrideFailsNamingTheFile()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("attention_per_turnn: 5\n")));
        Assert.Contains("rules/later.yaml", e.Message);
    }

    [Fact]
    public void AnOverrideIsValidatedLikeRulesYaml()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("ceasefire_weeks: -1\n")));
        Assert.Contains("rules/later.yaml", e.Message);
        Assert.Contains("ceasefire_weeks", e.Message);
    }

    [Fact]
    public void AnOverrideCannotMoveTheStartDate()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("start: 1952-01-01\n")));
        Assert.Contains("rules/later.yaml", e.Message);
    }

    [Fact]
    public void AnEmptyOverrideFileFails()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("")));
        Assert.Contains("rules/later.yaml", e.Message);
    }

    [Fact]
    public void AnOverrideFileCountsInTheContentHash()
    {
        var without = ContentLoader.Load(Write(null)).Hash;
        var with = ContentLoader.Load(Write("attention_per_turn: 5\n")).Hash;
        Assert.NotEqual(without, with);
    }

    [Fact]
    public void AnEraWithoutAFileHasNoEntry()
    {
        var c = ContentLoader.Load(Write(null));
        Assert.Empty(c.EraRules);
        Assert.Same(c.Rules, c.RulesAt(new DateTime(1953, 1, 1)));
    }

    [Fact]
    public void ABaseRulesErrorNamesRulesYamlEvenWithAnEraFile()
    {
        var dir = Write("attention_per_turn: 5\n");
        var rulesPath = Path.Combine(dir, "rules.yaml");
        File.WriteAllText(rulesPath, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(rulesPath), @"(?m)^ceasefire_weeks:.*$", "ceasefire_weeks: -1"));
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Contains("rules.yaml", e.Message);
        Assert.DoesNotContain("rules/later.yaml", e.Message);
    }

    [Fact]
    public void ATypeErrorInAnOverrideNamesTheFileAndLine()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("# note\nattention_per_turn: abc\n")));
        Assert.Contains("rules/later.yaml", e.Message);
        Assert.Contains("line 2", e.Message);
    }

    [Fact]
    public void ANestedUnknownKeyNamesTheFileAndLine()
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write("map_actions:\n  housing:\n    costt: 9\n")));
        Assert.Contains("rules/later.yaml", e.Message);
        Assert.Contains("line 3", e.Message);
    }

    [Theory]
    [InlineData("treasury_start: 999\n")]
    [InlineData("fx_start: 1\n")]
    [InlineData("start_rung: 2\n")]
    [InlineData("initial_population:\n  jews: 1\n")]
    [InlineData("budget_start:\n  defence: 1\n")]
    public void AnOverrideCannotSetAStartValue(string yaml)
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Write(yaml)));
        Assert.Contains("rules/later.yaml", e.Message);
        Assert.Contains("start/<era>.yaml", e.Message);
    }

    [Fact]
    public void AFileThatNamesNoEraIsRefused()
    {
        var dir = Write(null);
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "latr.yaml"), "attention_per_turn: 5\n");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(dir));
        Assert.Contains("rules/latr.yaml", e.Message);
    }

    [Fact]
    public void ARealOverrideFileInContentNamesAnEra()
    {
        var root = RealContentTests.ContentRoot();
        var dir = Path.Combine(root, "rules");
        if (!Directory.Exists(dir)) return;
        var eras = new HashSet<string>(ContentLoader.Load(root).Eras.ConvertAll(e => e.Id));
        foreach (var f in Directory.GetFiles(dir, "*.yaml")) Assert.Contains(Path.GetFileNameWithoutExtension(f), eras);
    }

    [Fact]
    public void AMergeTwoLevelsDeepKeepsTheSiblingKeys()
    {
        var c = ContentLoader.Load(Write("map_actions:\n  housing:\n    cost: 9\n"));
        var later = c.RulesAt(new DateTime(1953, 1, 1)).MapActions;
        var basis = c.Rules.MapActions;
        Assert.Equal(9, later["housing"].Cost);
        Assert.Equal(basis["housing"].Housing, later["housing"].Housing);
        Assert.Equal(basis["housing"].Name, later["housing"].Name);
        Assert.Equal(basis.Count, later.Count);
    }
}
