using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-EVT-005: presentation context decorates the existing preview without changing its effects or state.</summary>
public class OptionEffectPreviewTests
{
    static Content Content()
    {
        var model = Fixture.Files["model.yaml"]
            .Replace("indicators:\n", "indicators:\n  - { id: halutz, base: 0.2, name: Test readiness }\n  - { id: retired_test, base: 0, name: Retired test, to: 1948-05-15 }\n")
            .Replace("policies:\n", "policies:\n  - { id: retired_policy, default: 0, office: finance_minister, name: Retired policy, to: 1948-05-15 }\n");
        return ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["model.yaml"] = model,
            ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
            ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
                + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 8, 10, 6, "[military_camp, hakhshara]") + Fixture.City("town_b", 5, 2, 2, 1),
        }), new Dictionary<string, string>
        {
            ["preview_record.yaml"] = "id: preview_record\ntitle: Recorded {prime_minister}\ntext: A fictional fixture record.\nfrom: 1950-01-01\noptions:\n  - { id: keep, text: Keep, historical: true }\n",
        }));
    }

    static EventOption EveryKind() => new EventOption
    {
        Id = "preview",
        Effects = new List<OptionEffect>
        {
            new OptionEffect { Target = "halutz", Delta = 0.041 },
            new OptionEffect { Target = "treasury", Delta = -1.25 },
            // Direct formatting contract: the existing helper rounds whole-stock display half away from zero.
            new OptionEffect { Target = "food", Delta = 2.5 },
            new OptionEffect { Target = "arms", Delta = -100 },
            new OptionEffect { Target = "retired_test", Delta = 1 },
        },
        GrantUnits = new List<UnitGrant>
        {
            new UnitGrant { Type = "halutz", N = 2 },
            new UnitGrant { Type = "halutz", N = 1, At = "town_b" },
            new UnitGrant { Type = "veteran_halutz", N = 3, At = "town_a" },
        },
        CityEffects = new List<CityEffect>
        {
            new CityEffect { City = "town_a", Field = "housing_capacity", Delta = 3 },
            new CityEffect { City = "town_a", Field = "job_slots", Delta = -100 },
            new CityEffect { City = "town_b", Field = "condition", Delta = 0.051 },
        },
        SetPolicy = new Dictionary<string, double> { ["rationing"] = 0.75, ["maabarot"] = 0.2, ["retired_policy"] = 0.9 },
        SetRung = 4,
        Rejoin = "preview_record",
    };

    // R-EVT-005: exact source order/values, duplicates and resolved destinations, without choosing the option.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void ContextKeepsEveryHelperEntryAndItsSimFormattedAmount(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var c = Content();
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            var option = EveryKind();
            var hash = s.Hash();
            var source = EventSystem.OptionEffects(c, s, option);
            var rows = EventSystem.OptionEffectPreviews(c, s, option);
            Assert.Equal(14, rows.Count);
            Assert.Equal(source.Select(x => (x.System, x.Target, x.Delta, x.Message)),
                rows.Select(x => (x.Cause.System, x.Cause.Target, x.Cause.Delta, x.Cause.Message)));
            Assert.Equal(new[] { "halutz", "treasury", "food", "arms", "halutz", "halutz", "veteran_halutz", "town_a.housing_capacity", "town_a.job_slots", "town_b.condition", "maabarot", "rationing", "rung", "rejoin" },
                rows.Select(x => x.Cause.Target));
            Assert.Equal(new[] { "+4.1%", "IL -1.3m", "+3", "-100", "2", "1", "3", "+3", "-100", "+5.1%", "20%", "75%", "4", "" },
                rows.Select(x => x.Amount));
            Assert.Equal(new[] { "halutz", "treasury", "food", "arms", "town_a", "town_b", "town_a", "town_a", "town_a", "town_b", "maabarot", "rationing", "", "preview_record" },
                rows.Select(x => x.ContextId));
            Assert.Equal(new[] { "Test readiness", "Treasury", "Food", "Arms", "Town A", "Town B", "Town A", "Town A", "Town A", "Town B", "Transit camps", "Rationing", "", "Recorded Pm One" },
                rows.Select(x => x.ContextName));
            Assert.Equal("housing_capacity", rows[7].Field);
            Assert.Equal("job_slots", rows[8].Field);
            Assert.Equal("condition", rows[9].Field);
            Assert.Equal(hash, s.Hash());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    // A type and indicator may have the same id; neither category nor destination is guessed in Unity from that id.
    [Fact]
    public void TheSimDistinguishesAnIndicatorFromARepeatedGrantOfTheSameId()
    {
        var c = Content();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var rows = EventSystem.OptionEffectPreviews(c, s, EveryKind());
        Assert.Equal(rows[0].Cause.Target, rows[4].Cause.Target);
        Assert.Equal(OptionEffectPreviewKind.Amount, rows[0].Kind);
        Assert.Equal(OptionEffectPreviewKind.UnitGrant, rows[4].Kind);
        Assert.Equal(OptionEffectPreviewKind.UnitGrant, rows[5].Kind);
        Assert.Equal(("town_a", "town_b"), (rows[4].ContextId, rows[5].ContextId));
        Assert.Equal(OptionEffectPreviewKind.Stock, rows[2].Kind);
        Assert.Equal(OptionEffectPreviewKind.City, rows[7].Kind);
        Assert.Equal(OptionEffectPreviewKind.SetPolicy, rows[10].Kind);
        Assert.Equal(OptionEffectPreviewKind.SetRung, rows[12].Kind);
        Assert.Equal(OptionEffectPreviewKind.Rejoin, rows[13].Kind);
    }

    // One walk builds both lists (#1158 review): each row's own message carries the name and the amount it was built with,
    // for every option in the shipped content and for every kind the fixture can express, a project unlock included.
    [Fact]
    public void EveryRowAgreesWithItsOwnEffectInTheShippedContentAndTheFixture()
    {
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        var fixture = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["projects.yaml"] = Fixture.GddProjects.Replace("job_slots: 3 }", "job_slots: 3, locked: true }"),
        })));
        var unlock = new EventOption { UnlockProjects = new List<string> { "industry" } };
        var cases = new List<(Content c, EventOption o)>(real.Events.SelectMany(e => e.Options).Select(o => (real, o)))
        {
            (fixture, unlock), (Content(), EveryKind()),
        };
        Assert.Contains(cases, x => x.o.Effects.Count > 0);
        var checkedRows = 0;
        foreach (var (c, o) in cases)
        {
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            var effects = EventSystem.OptionEffects(c, s, o);
            var rows = EventSystem.OptionEffectPreviews(c, s, o);
            Assert.Equal(effects.Select(x => (x.System, x.Target, x.Delta, x.Message)),
                rows.Select(x => (x.Cause.System, x.Cause.Target, x.Cause.Delta, x.Cause.Message)));
            foreach (var r in rows)
            {
                Assert.Contains(r.Amount, r.Cause.Message);
                Assert.Contains(r.ContextName, r.Cause.Message);
                checkedRows++;
            }
        }
        Assert.True(checkedRows > cases.Count, $"only {checkedRows} rows over {cases.Count} options");

        var row = Assert.Single(EventSystem.OptionEffectPreviews(fixture, Simulation.NewGame(fixture, 1, Mode.Historical), unlock));
        Assert.Equal((OptionEffectPreviewKind.ProjectUnlock, "industry", "An industry project", ""), (row.Kind, row.ContextId, row.ContextName, row.Amount));
        Assert.Equal("An industry project can now be built", row.Cause.Message);
    }

    [Fact]
    public void AnEmptyOptionHasNoInventedEffects()
    {
        var c = Content();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(EventSystem.OptionEffectPreviews(c, s, new EventOption()));
    }

    [Fact]
    public void CallersCannotReorderThePreview()
    {
        var c = Content();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var rows = EventSystem.OptionEffectPreviews(c, s, EveryKind());
        var list = Assert.IsAssignableFrom<IList<OptionEffectPreview>>(rows);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
    }
}
