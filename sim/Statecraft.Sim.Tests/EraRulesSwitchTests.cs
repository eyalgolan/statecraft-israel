using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>#58, second piece: the systems read RulesAt(the turn's starting date) instead of Rules. Each test gives the
/// "later" era (1952-01-01 to 1957-01-01) an override and checks the value changes with the state's date, in the founding era
/// and in the later one.</summary>
public class EraRulesSwitchTests
{
    const string Eras = @"
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
    static readonly DateTime Early = new DateTime(1950, 6, 1), Late = new DateTime(1953, 6, 1);

    static Content Load(string laterRules)
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = Eras });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), laterRules);
        return ContentLoader.Load(dir);
    }

    static GameState At(Content c, DateTime date) { var s = Simulation.NewGame(c, 1, Mode.Historical); s.Date = date; return s; }

    [Fact]
    public void AttentionRefillsFromTheTurnsEra()
    {
        var c = Load("attention_per_turn: 7.5\n");
        var s = At(c, Early); AttentionSystem.Refill(c, s); Assert.Equal(c.Rules.AttentionPerTurn, s.Attention);
        s = At(c, Late); AttentionSystem.Refill(c, s); Assert.Equal(7.5, s.Attention);
    }

    [Fact]
    public void AttentionCostComesFromTheTurnsEra()
    {
        var c = Load("attention_cost:\n  set_policy: 9\n");
        Assert.NotEqual(9, c.Rules.AttentionCost["set_policy"]);
        Assert.Equal(c.Rules.AttentionCost["set_policy"] * (1 + c.Rules.RungAttentionStep * (At(c, Early).Rung - 1)),
            AttentionSystem.Cost(c, At(c, Early), "set_policy"));
        var late = At(c, Late);
        Assert.Equal(9 * (1 + c.Rules.RungAttentionStep * (late.Rung - 1)), AttentionSystem.Cost(c, late, "set_policy"));
    }

    [Fact]
    public void TreasuryFlowsAndMobilizationCostComeFromTheTurnsEra()
    {
        var c = Load("tax_per_capita_per_year: 2\nspending_per_capita_per_year: 3\nmobilization_cost_per_year: 5\n");
        var early = At(c, Early); var late = At(c, Late); early.Mobilization = late.Mobilization = 0.5;
        var fe = EconomySystem.Flows(c, early, 1.0); var fl = EconomySystem.Flows(c, late, 1.0);
        Assert.Equal(c.Rules.TaxPerCapitaPerYear * early.TotalPopulation, fe.Tax, 9);
        Assert.Equal(2 * late.TotalPopulation, fl.Tax, 9);
        Assert.Equal(3 * late.TotalPopulation, fl.Spending, 9);
        Assert.Equal(c.Rules.MobilizationCostPerYear * 0.5, fe.Mobilization, 9);
        Assert.Equal(5 * 0.5, fl.Mobilization, 9);
        Assert.Equal(fl.Mobilization, EconomySystem.MobilizationCost(c, Late, 0.5), 12);
    }

    [Fact]
    public void SecurityFloorAndCeasefireComeFromTheTurnsEra()
    {
        var c = Load("rung_min_mobilization: [0.1, 0.2, 0.3, 0.4, 0.5]\nceasefire_weeks: 9\n");
        Assert.Equal(c.Rules.RungMinMobilization[4], SecuritySystem.MinMobilization(c, Early, 5));
        Assert.Equal(0.5, SecuritySystem.MinMobilization(c, Late, 5));
        Assert.Equal(0.5, SecuritySystem.EffectiveMobilization(c, Late, 5, 0.1));
        Assert.Equal(c.Rules.CeasefireWeeks, SecuritySystem.CeasefireWeeksAfter(c, Early, 3, 0, 5));
        Assert.Equal(9, SecuritySystem.CeasefireWeeksAfter(c, Late, 3, 0, 5));
        var s = At(c, Late); s.Mobilization = 0; s.Rung = 3; s.CeasefireWeeksLeft = 0;
        SecuritySystem.SetRung(c, s, 5, new List<Cause>());
        Assert.Equal(9, s.CeasefireWeeksLeft);
        Assert.True(s.Mobilization >= 0.5);   // raised to the later era's floor
    }

    [Fact]
    public void MapActionAvailabilityComesFromTheTurnsEra()
    {
        var c = Load("map_actions_from: 1960-01-01\n");
        var early = MapActions.Check(c, At(c, Early), MapActions.Housing, 0, 0);
        var late = MapActions.Check(c, At(c, Late), MapActions.Housing, 0, 0);
        Assert.DoesNotContain("1960", early ?? "");
        Assert.Contains("1960", late);
    }

    [Fact]
    public void ThePopulationGrowsByTheTurnsEraRate()
    {
        var c = Load("natural_increase_per_year: 0.5\n");
        double Grow(DateTime d)
        {
            var s = At(c, d); var before = s.TotalPopulation;
            PopulationSystem.Apply(c, s, d.AddDays(7), 0.1, new List<Cause>());
            return s.TotalPopulation / before;
        }
        Assert.True(Grow(Late) > Grow(Early));
    }

    [Fact]
    public void AppointmentStabilityCostComesFromTheTurnsEra()
    {
        var dir = Fixture.Write(new Dictionary<string, string>
        {
            ["eras.yaml"] = Eras,
            ["people.yaml"] = Fixture.Files["people.yaml"] + @"
  - id: loyalist
    name: Loy Alist
    living: false
    party: alpha
    competence: 2
    loyalty: 9
    offices: []
",
        });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "appoint_stability_cost_other_party: 0.4\n");
        var c = ContentLoader.Load(dir);
        double Cost(DateTime d)
        {
            var s = At(c, d); var before = s.CoalitionStability;
            Assert.Null(Cabinet.Appoint(c, s, "finance_minister", "loyalist", new List<Cause>()));
            return before - s.CoalitionStability;
        }
        Assert.Equal(c.Rules.AppointStabilityCostOtherParty, Cost(Early), 9);
        Assert.Equal(0.4, Cost(Late), 9);
    }

    /// <summary>Through <c>Simulation.Step</c>, across a boundary that falls inside a turn (1948-06-01; weekly war turns run
    /// 05-29 to 06-05): the turn that starts before the boundary taxes at the founding rate for its whole length, the first
    /// turn that starts after it at the later era's rate, and attention is refilled at the later era's value from the end
    /// of the crossing turn on (the refill is for the turn that starts on the new date).</summary>
    [Fact]
    public void ARunPicksUpTheOverrideFromTheFirstTurnThatStartsInTheNewEra()
    {
        var dir = Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = @"
eras:
  - id: founding
    name: The Founding
    from: 1948-05-15
    to: 1948-06-05
  - id: later
    name: A Later Chapter
    from: 1948-06-05
    to: 1957-01-01
" });
        Directory.CreateDirectory(Path.Combine(dir, "rules"));
        File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "attention_per_turn: 7.5\ntax_per_capita_per_year: 1.0\n");
        var c = ContentLoader.Load(dir);
        var boundary = new DateTime(1948, 6, 5);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(c.Rules.AttentionPerTurn, s.Attention);
        var lengths = new List<int>();
        while (s.Date < boundary.AddDays(7))   // through the first whole turn of the new era (the fixture's ceasefire ends the weekly turns after it)
        {
            var start = s.Date;
            var step = Simulation.Step(c, s, new List<Command>());
            var end = step.State.Date;
            var days = (end - start).Days;
            lengths.Add(days);
            var tax = start < boundary ? c.Rules.TaxPerCapitaPerYear : 1.0;
            var expected = (tax - c.Rules.SpendingPerCapitaPerYear) * step.State.TotalPopulation * (days / 365.0)
                - c.Rules.MobilizationCostPerYear * s.Mobilization * (days / 365.0);
            var treasury = step.Why.Single(w => w.System == "economy" && w.Target == "treasury").Delta;
            Assert.True(Math.Abs(expected - treasury) < 1e-9, $"{start:yyyy-MM-dd}: treasury {treasury}, expected {expected}");
            Assert.Equal(end < boundary ? c.Rules.AttentionPerTurn : 7.5, step.State.Attention);
            s = step.State;
        }
        // 22 May + 7 would leave 7 days, under 14, so that turn runs 14 days to the boundary (spec 4.1); the rest are weeks.
        Assert.Equal(new[] { 7, 14, 7 }, lengths);
    }

    /// <summary>The only places the sim reads the base rules directly are the values <c>NewGame</c> reads once (an era
    /// override cannot set them) and a map action's display name (an override cannot remove an action). Everything else
    /// reads <c>RulesAt(date)</c>, so a new <c>c.Rules.X</c> read in a system fails here until it is deliberate.</summary>
    [Fact]
    public void OnlyStartValuesAndMapActionNamesReadTheBaseRulesDirectly()
    {
        var simDir = Path.Combine(Directory.GetParent(RealContentTests.ContentRoot())!.FullName, "sim", "Statecraft.Sim");
        var allowed = new HashSet<string> { "Start", "TreasuryStart", "FxStart", "BudgetStart", "InitialPopulation", "StartRung", "MapActions", "BudgetOwners" };
        var bad = new List<string>();
        foreach (var f in Directory.GetFiles(simDir, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(f);
            if (name == "ContentLoader.cs" || name == "Content.cs") continue;
            var lines = File.ReadAllLines(f);
            for (int i = 0; i < lines.Length; i++)
                // Any receiver (c, content, ...), and a bare `x.Rules` with no member after it (`var r = c.Rules;`), which
                // would hand every later read the base rules.
                foreach (Match m in Regex.Matches(lines[i], @"\b\w+\.Rules\b(?!\s*\()(?:\.(\w+))?"))
                    if (!m.Groups[1].Success || !allowed.Contains(m.Groups[1].Value)) bad.Add($"{name}:{i + 1} {m.Value}");
        }
        Assert.True(bad.Count == 0, "read RulesAt(date) instead: " + string.Join(", ", bad));
    }
}
