using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

public class CultureTests
{
    const string Event = @"
id: levy
title: Emergency levy
text: '{finance_minister} asks for a levy.'
from: 1948-06-01
options:
  - id: pay
    text: Pay it
    effects:
      - target: treasury
        delta: -10.5
";

    static readonly Content C = ContentLoader.Load(Fixture.Write(
        new Dictionary<string, string>
        {
            ["aliyah.yaml"] = Fixture.Files["aliyah.yaml"].Replace("europe: 230", "europe: 23000"),
            ["legislatures.yaml"] = Fixture.Files["legislatures.yaml"].Replace("  - id: house\n", "  - id: house\n    elected: 1949-01-25\n"),
        },
        new Dictionary<string, string> { ["levy.yaml"] = Event }));

    /// <summary>Runs every system that writes numbers or dates into causes or rejection reasons, under one culture.</summary>
    static List<string> Run(CultureInfo culture)
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var why = new List<Cause>();
            var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 1 };
            EffectsSystem.Init(C, s); EconomySystem.Init(C, s); PopulationSystem.Init(C, s);
            SecuritySystem.Init(C, s, why);                                    // call-up floor (percent)
            s.Policies["rationing"] = 1.0;
            EffectsSystem.Apply(C, s, why);                                    // indicator deltas
            s.Indicators["food_supply"] = 0.25; EffectsSystem.UpdateSituations(C, s, why);   // situation value
            PopulationSystem.Apply(C, s, new DateTime(1948, 9, 1), 0.25, why);  // thousands
            s.Mobilization = 1.0; s.Treasury = 1;
            EconomySystem.Apply(C, s, 0.25, why);                              // treasury, debt, fx
            PoliticsSystem.UpdateStability(C, s, 0.25, why);
            PoliticsSystem.ReportChanges(C, new DateTime(1949, 2, 1), new DateTime(1949, 5, 1), why);   // election date
            EventSystem.Trigger(C, s, why);
            EventSystem.Choose(C, s, "levy", "pay", why);                      // event delta
            return why.Select(w => w.Message).ToList();
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("th-TH")]
    public void CauseTextsAreCultureInvariant(string culture)
    {
        var expected = Run(CultureInfo.InvariantCulture);
        Assert.Contains(expected, m => m.Contains("9,160 immigrants"));   // grouping is exercised
        Assert.Contains(expected, m => m.Contains("elected 25 January 1949"));  // calendar is exercised
        Assert.Equal(expected, Run(new CultureInfo(culture)));
    }

    static Content LoadReal(CultureInfo culture)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return ContentLoader.Load(RealContentTests.ContentRoot());
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("he-IL")]
    [InlineData("de-DE")]
    public void RealContentLoadsCultureIndependently(string culture)
    {
        var expected = LoadReal(CultureInfo.InvariantCulture);
        var actual = LoadReal(new CultureInfo(culture));

        Assert.Equal(expected.Hash, actual.Hash);
        // rules.yaml map_actions.transit_camp.cost -- a Money cost with a decimal point (double).
        Assert.Equal(expected.Rules.MapActions["transit_camp"].Cost, actual.Rules.MapActions["transit_camp"].Cost);
        // rules.yaml natural_increase_per_year -- an annual rate with a decimal point (double).
        Assert.Equal(expected.Rules.NaturalIncreasePerYear, actual.Rules.NaturalIncreasePerYear);
        // model.yaml effects[rationing_grates].k -- a negative multiplier with a decimal point (double).
        Assert.Equal(expected.Effects.Single(e => e.Id == "rationing_grates").K,
            actual.Effects.Single(e => e.Id == "rationing_grates").K);
        // model.yaml policies[rationing].from -- a policy start date (DateTime?).
        Assert.Equal(expected.Policies.Single(p => p.Id == "rationing").From,
            actual.Policies.Single(p => p.Id == "rationing").From);
    }
}
