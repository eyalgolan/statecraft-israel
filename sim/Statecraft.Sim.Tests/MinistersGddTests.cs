using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>The ministers on the GDD systems (D-H3; #808's first slice: Phase 7 PR 4 of
/// docs/superpowers/plans/2026-10-15-gdd-phase-7.md): R-MIN-001 (each named system reads its office's multiplier,
/// Q-MIN-1), R-MIN-002 (the multiplier scales output and progress per turn, rounded by the one stated rule: floor with
/// carry, so no fraction of a turn or a person enters the state, Q-MIN-2) and R-MIN-003 (an ended office reads its named
/// successor, or is neutral 1.0 with none, Q-MIN-3). Military training stays unmapped until #742.</summary>
public class MinistersGddTests
{
    /// <summary>The base GDD fixture plus the three offices of the slice, each with a budget line, and the
    /// minister_systems map. The budget gives the named office the whole budget (a factor of 1.5), so a competence-10
    /// holder multiplies by 1.5, a competence-4 one by 1.05 and a vacant office by 0.75.</summary>
    const string Offices = @"
offices:
  - id: prime_minister
    title: Prime Minister
  - id: defense_minister
    title: Minister of Defense
  - id: finance_minister
    title: Minister of Finance
  - id: agriculture_minister
    title: Minister of Agriculture
  - id: immigration_minister
    title: Minister of Immigration
";

    static readonly string Able = @"
  - id: able_agri
    name: Able Agri
    living: false
    competence: 10
    loyalty: 9
    offices:
      - office: agriculture_minister
        from: 1948-05-14
";
    static readonly string Lazy = @"
  - id: lazy_agri
    name: Lazy Agri
    living: false
    competence: 4
    loyalty: 5
    offices:
      - office: agriculture_minister
        from: 1948-05-14
";

    static readonly string Map = @"
  minister_systems:
    housing_construction: labour_minister
    industry_construction: labour_minister
    field_work: agriculture_minister
    yishuv_work: agriculture_minister
    absorption: immigration_minister
";

    /// <summary>The GDD fixture with the map and the named office holders; the named office owns a fully funded budget
    /// line (a 1.5 factor), the other line unfunded.</summary>
    static Content Town(string people = "", string offices = Offices, string budget = "agriculture_minister",
        string? sites = null, string? prefix = null)
    {
        var extra = new Dictionary<string, string>
        {
            ["offices.yaml"] = offices + "  - id: labour_minister\n    title: Minister of Labour\n",
            ["people.yaml"] = Fixture.Files["people.yaml"] + people,
            ["rules.yaml"] = Fixture.Files["rules.yaml"]
                .Replace("map_action: 1 }", Fixture.GddCosts)
                .Replace("budget_start: { finance_minister: 1.0 }", $"budget_start: {{ {budget}: 1.0, finance_minister: 0.0 }}")
                + Fixture.GddRules + Map,
        };
        if (sites != null) extra["sites.yaml"] = (prefix ?? "") + sites;
        return ContentLoader.Load(Fixture.Write(Fixture.Gdd(extra)));
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState WithHalutz(Content c, out int id)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        return s;
    }

    // R-MIN-001, R-MIN-002
    [Fact]
    public void AnAbleMinisterChangesTheSystem()
    {
        // Two states that differ only in who holds agriculture_minister. A yishuv takes build_yishuv_turns = 3 turns.
        // The able holder multiplies progress by 1.5: 1.5, then 3.0 accumulated - built in 2 turns, 0.5 carried nowhere
        // once built. The lazy holder multiplies by 1.05: 1.05, 2.10, 3.15 - built in 3 turns. No minister: 3 turns.
        var ableC = Town(Able);
        var s = WithHalutz(ableC, out var id);
        s = Step(ableC, s, new PioneerWork(id, Projects.BuildYishuv, "site_a")).State;
        Assert.Equal(2, s.Sites["site_a"].TurnsLeft);
        Assert.Equal(0.5, s.Sites["site_a"].Progress, 9);
        s = Step(ableC, s).State;
        Assert.NotNull(s.Sites["site_a"].BuiltOn);   // built on the second turn

        var lazyC = Town(Lazy);
        s = WithHalutz(lazyC, out id);
        s = Step(lazyC, s, new PioneerWork(id, Projects.BuildYishuv, "site_a")).State;
        Assert.Equal(2, s.Sites["site_a"].TurnsLeft);
        Assert.Equal(0.05, s.Sites["site_a"].Progress, 9);
        s = Step(lazyC, s).State;
        Assert.Null(s.Sites["site_a"].BuiltOn);   // still building after two turns
        s = Step(lazyC, s).State;
        Assert.NotNull(s.Sites["site_a"].BuiltOn);   // built on the third

        var noneC = Town();
        s = WithHalutz(noneC, out id);
        s = Step(noneC, s, new PioneerWork(id, Projects.BuildYishuv, "site_a")).State;
        Assert.Equal(3, s.Sites["site_a"].TurnsLeft);   // a vacant office multiplies by 0.75: no whole turn yet
        Assert.Equal(0.75, s.Sites["site_a"].Progress, 9);
    }

    // R-MIN-002
    [Fact]
    public void RoundingIsStated()
    {
        // A vacant office multiplies by 0.75 (competence 0, budget 1.5): the yishuv gains 0.75 a turn, and only whole
        // turns leave the accumulator: 3, 2, 1 turns to go with 0.75, 0.5, 0.25 carried, built on the fourth turn. No
        // fraction of a turn is ever a turn to go, and the people below stay whole groups.
        var c = Town();
        var s = WithHalutz(c, out var id);
        s = Step(c, s, new PioneerWork(id, Projects.BuildYishuv, "site_a")).State;
        Assert.Equal((3, 0.75), (s.Sites["site_a"].TurnsLeft, s.Sites["site_a"].Progress));
        s = Step(c, s).State;
        Assert.Equal((2, 0.5), (s.Sites["site_a"].TurnsLeft, s.Sites["site_a"].Progress));
        s = Step(c, s).State;
        Assert.Equal((1, 0.25), (s.Sites["site_a"].TurnsLeft, s.Sites["site_a"].Progress));
        s = Step(c, s).State;
        Assert.NotNull(s.Sites["site_a"].BuiltOn);

        // The same floor with carry houses whole groups: 5 refugees at 0.75 house 3, and 2 wait.
        c = Town(budget: "immigration_minister");
        s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithCounts(x.Civilians, x.Employed, 5);
        var before = x.Civilians + 5;
        var res = Step(c, s);
        s = res.State;
        x = s.Cities["town_a"];
        Assert.Equal(2, x.Refugees);
        Assert.Equal(before, x.Civilians + x.Refugees);
        Assert.Contains(res.Why, w => w.System == "city" && w.Message == "Town A: 3 groups of immigrants housed");
    }

    // R-MIN-003
    [Fact]
    public void AVacantOrEndedOfficeIsAsStated()
    {
        // A vacant office that exists on the date keeps Multiplier's absent-holder rule: 0.5 competence. Here with the
        // 1.5 budget factor, 5 refugees house floor(5 * 0.75) = 3.
        var c = Town(budget: "immigration_minister");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = c.RulesAt(s.Date).Mvp!;
        Assert.Equal(0.75, EffectsSystem.GddSystemMultiplier(c, s, r, EffectsSystem.AbsorptionSystem), 9);

        // The office ended on the state's date (immigration_minister's real until is 8 October 1951): neutral 1.0,
        // not the absent-holder 0.5 - every refugee is housed, as R-CITY-004 says.
        var ended = Town(offices: Offices.Replace("- id: immigration_minister\n    title: Minister of Immigration",
            "- id: immigration_minister\n    title: Minister of Immigration\n    until: 1951-10-08"));
        s = Simulation.NewGame(ended, 1, Mode.Historical);
        s.Date = new DateTime(1951, 11, 15);
        r = c.RulesAt(s.Date).Mvp!;
        Assert.Equal(1.0, EffectsSystem.GddSystemMultiplier(ended, s, ended.RulesAt(s.Date).Mvp!, EffectsSystem.AbsorptionSystem), 9);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithCounts(x.Civilians, x.Employed, 5);
        s = Step(ended, s).State;
        Assert.Equal(0, s.Cities["town_a"].Refugees);

        // An ended office with a named successor reads the successor's multiplier (Q-MIN-3).
        var succeeded = Town(Able.Replace("agriculture_minister", "interior_minister"), budget: "interior_minister",
            offices: Offices.Replace("- id: immigration_minister\n    title: Minister of Immigration",
                "- id: immigration_minister\n    title: Minister of Immigration\n    until: 1951-10-08\n    successor: interior_minister")
            + "  - id: interior_minister\n    title: Minister of Internal Affairs\n");
        s = Simulation.NewGame(succeeded, 1, Mode.Historical);
        s.Date = new DateTime(1951, 11, 15);
        Assert.Equal(1.5, EffectsSystem.GddSystemMultiplier(succeeded, s, succeeded.RulesAt(s.Date).Mvp!, EffectsSystem.AbsorptionSystem), 9);
    }

    // R-MIN-001: a system the map does not name is neutral.
    [Fact]
    public void AnUnmappedSystemIsNeutral()
    {
        // A camp is no mapped system's work in this slice: it takes build_camp_turns = 2 turns, able minister or not.
        var c = Town(Able, sites: Fixture.GddSites.Replace("sites:\n", "") + @"
  - id: site_camp
    name: Site Camp
    name_he: אתר ג
    q: 3
    r: 0
    kinds: [military_camp]
    city: town_a
    sources:
      - { url: ""https://example.org/site_camp"", checked: 2026-10-14 }
    status: fact-checked
", prefix: "sites:\n");
        var s = WithHalutz(c, out var id);
        s = Step(c, s, new PioneerWork(id, Projects.BuildCamp, "site_camp")).State;
        Assert.Equal((1, 0.0), (s.Sites["site_camp"].TurnsLeft, s.Sites["site_camp"].Progress));
        s = Step(c, s).State;
        Assert.NotNull(s.Sites["site_camp"].BuiltOn);
    }

    // R-MIN-001: the loader refuses a map that names an unknown office.
    [Fact]
    public void TheLoaderRefusesAnUnknownOfficeInTheMap()
    {
        var bad = new Dictionary<string, string>
        {
            ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts)
                + Fixture.GddRules + "  minister_systems:\n    absorption: no_such_minister\n",
        };
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(bad))));
        Assert.Contains("unknown office 'no_such_minister'", e.Message);
    }

    // R-MIN-001 (Q-MIN-1): the real content maps the decided systems.
    [Fact]
    public void TheRealContentMapsTheDecidedSystems()
    {
        var m = ContentLoader.Load(RealContentTests.ContentRoot()).Rules.Mvp!.MinisterSystems;
        Assert.Equal(new Dictionary<string, string>
        {
            ["housing_construction"] = "labour_minister",
            ["industry_construction"] = "labour_minister",
            ["field_work"] = "agriculture_minister",
            ["yishuv_work"] = "agriculture_minister",
            ["absorption"] = "immigration_minister",
            ["corridors"] = "transportation_minister",
        }, m);
    }

    // The live site ETA must match the resolver, including saved carry and promotion after this turn's work.
    [Theory]
    [InlineData(false, "halutz", 0)]
    [InlineData(true, "halutz", 0)]
    [InlineData(false, "halutz", 3)]
    [InlineData(false, "veteran_halutz", 0)]
    [InlineData(true, "veteran_halutz", 0)]
    public void AScaledSiteForecastMatchesEveryRemainingTurn(bool able, string type, int service)
    {
        var c = Town(able ? Able : "");
        var s = WithHalutz(c, out var id);
        s.Units[id] = new Unit(id, type, 1, 0, service);
        var site = c.Sites.Single(x => x.Id == "site_a");
        int Forecast(int left, double carry)
        {
            var hash = s.Hash();
            var result = Projects.TurnsToBuild(c, s, id, site, left, carry);
            Assert.Equal(hash, s.Hash()); // a forecast never advances the real state
            return result;
        }
        var forecasts = new List<int> { Forecast(c.Rules.Mvp!.BuildYishuvTurns, 0) };
        s = Step(c, s, new PioneerWork(id, Projects.BuildYishuv, site.Id)).State;
        int actual = 1;
        while (s.Sites[site.Id].BuiltOn == null && actual < 20)
        {
            var work = s.Sites[site.Id];
            forecasts.Add(Forecast(work.TurnsLeft, work.Progress));
            s = Step(c, s).State; actual++;
        }
        Assert.NotNull(s.Sites[site.Id].BuiltOn);
        for (int i = 0; i < forecasts.Count; i++) Assert.Equal(actual - i, forecasts[i]);
    }

    [Fact]
    public void AChangedMinisterUpdatesTheRemainingForecastWithoutLosingCarry()
    {
        var candidate = Lazy.Replace("    offices:\n      - office: agriculture_minister\n        from: 1948-05-14\n", "");
        var c = Town(Able + candidate);
        var s = WithHalutz(c, out var id);
        var site = c.Sites.Single(x => x.Id == "site_a");
        s = Step(c, s, new PioneerWork(id, Projects.BuildYishuv, site.Id)).State;
        var work = s.Sites[site.Id];
        Assert.Equal((2, 0.5), (work.TurnsLeft, work.Progress));
        var before = s.Hash();
        Assert.Equal(1, Projects.TurnsToBuild(c, s, id, site, work.TurnsLeft, work.Progress));
        Assert.Equal(before, s.Hash());
        Assert.Null(Cabinet.Appoint(c, s, "agriculture_minister", "lazy_agri", new List<Cause>()));
        before = s.Hash();
        Assert.Equal(2, Projects.TurnsToBuild(c, s, id, site, work.TurnsLeft, work.Progress));
        Assert.Equal(before, s.Hash());
        s = Step(c, s).State;
        Assert.Null(s.Sites[site.Id].BuiltOn);
        s = Step(c, s).State;
        Assert.NotNull(s.Sites[site.Id].BuiltOn);
    }

}
