using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Q-UNIT-3: military units pay Money in resolver step 7, and never pay ongoing Arms upkeep.</summary>
public class MilitaryUpkeepTests
{
    // A real turn must pay for the military unit; a hash containing a unit alone does not prove that it was charged.
    [Theory]
    [InlineData("recruit")]
    [InlineData("veteran_soldier")]
    [InlineData("artillery")]
    [InlineData("armoured_car")]
    public void MilitaryUnitsPayInStepSeven(string type)
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var empty = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var army = empty.Clone();
        var id = army.NextId++;
        army.Units.Add(id, new Unit(id, type, 0, 0, side: "israel", strength: 3));
        var withoutArmy = Simulation.Step(c, empty, new Command[0]);
        var withArmy = Simulation.Step(c, army, new Command[0]);
        Assert.True(withArmy.State.Treasury - withArmy.State.Debt < withoutArmy.State.Treasury - withoutArmy.State.Debt,
            "The same turn with a military unit must pay its Money upkeep");
        Assert.Equal(withoutArmy.State.Arms, withArmy.State.Arms);
        Assert.Contains(withArmy.Why, x => x.System == "economy" && x.Target == "treasury" && x.Message.Contains("military upkeep"));
    }

    // R-UNIT-010, R-MONEY-003: production can fund upkeep; housing still happens after payment.
    [Theory]
    [InlineData(false, 2, 0)]
    [InlineData(true, 5, 2)]
    public void ProductionFundsUpkeepBeforeHousingAndAShortfallBorrows(bool mappedAbsorption, int refugees, int waiting)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("military_upkeep_per_turn: 0.1", "military_upkeep_per_turn: 2");
        // Isolate step 8's existing residents from step 9's later arrivals.
        files["aliyah.yaml"] = Fixture.Files["aliyah.yaml"].Replace("europe: 230", "europe: 0");
        if (mappedAbsorption)
        {
            // R-MIN-001, R-MIN-002: a vacant Immigration office with the full budget houses floor(5 * 0.75) = 3.
            files["offices.yaml"] = Fixture.Files["offices.yaml"] + "  - id: immigration_minister\n    title: Minister of Immigration\n";
            files["rules.yaml"] = files["rules.yaml"]
                .Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { immigration_minister: 1.0, finance_minister: 0.0 }")
                + "  minister_systems:\n    absorption: immigration_minister\n";
        }
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = 0;
        s.Cities["town_a"] = s.Cities["town_a"].WithCounts(10, 6, refugees);
        s.Units.Add(1, new Unit(1, "recruit", 1, 0, side: "israel", strength: 3));
        var debt = s.Debt;
        var arms = s.Arms;
        var why = new List<Cause>();
        TurnResolver.Resolve(c, s, s.Date.AddMonths(3), 0.25, why);

        Assert.Equal(0, s.Treasury);
        Assert.Equal(debt + 2 - 6 * 1.2 * 0.25, s.Debt, 9);
        Assert.Equal(arms, s.Arms);
        Assert.Equal(3, s.Units[1].Strength); // No silent attrition or disbanding to hide the shortfall.
        Assert.Equal(waiting, s.Cities["town_a"].Refugees);
        Assert.Equal(10 + refugees - waiting, s.Cities["town_a"].Civilians);
        Assert.Equal(refugees - waiting,
            Assert.Single(why, x => x.Message.Contains("immigrants housed")).Delta);
        var production = why.FindIndex(x => x.Message.Contains("jobs paid"));
        var upkeep = why.FindIndex(x => x.Message.EndsWith("(military upkeep)"));
        var housing = why.FindIndex(x => x.Message.Contains("immigrants housed"));
        Assert.True(production >= 0 && upkeep > production && housing > upkeep);
        Assert.Contains(why, x => x.System == "economy" && x.Target == "debt" && x.Message.EndsWith("for military upkeep"));
    }

    // R-UNIT-010: per formation per turn, irrespective of strength, weekly/monthly length or civilian tokens.
    [Theory]
    [InlineData(1, 1 / 52.0)]
    [InlineData(7, 1 / 12.0)]
    public void OnlyIsraeliMilitaryFormationsPay(int strength, double yearFraction)
    {
        var files = Fixture.Gdd();
        files["unit_types.yaml"] += "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Units.Add(1, new Unit(1, "recruit", 1, 0, side: "israel", strength: strength));
        s.Units.Add(2, new Unit(2, "veteran_soldier", 1, 0, side: "israel", strength: strength));
        s.Units.Add(3, new Unit(3, "enemy", 1, 0, side: "egypt", strength: strength));
        s.Units.Add(4, new Unit(4, "halutz", 1, 0));
        s.Units.Add(5, new Unit(5, "veteran_soldier", 1, 0)); // default record without a military side
        var why = new List<Cause>();
        TurnResolver.Resolve(c, s, s.Date.AddDays(7), yearFraction, why);
        Assert.Equal(-0.2, Assert.Single(why, x => x.Message.EndsWith("(military upkeep)")).Delta, 9);
    }

    // R-UNIT-010: graduation in step 4 makes a formation eligible for upkeep in the same turn's step 7.
    [Fact]
    public void ANewlyGraduatedFormationPaysThisTurn()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId++;
        s.Cohorts.Add(id, new Cohort(id, "recruit", "tel_aviv", 3, 1, 3));
        var why = new List<Cause>();
        // The Israeli formations on the board, not every starting row: a brigade dated after the start arrives later (R-UNIT-009).
        var israelBefore = s.Units.Values.Count(u => u.Side == "israel");
        TurnResolver.Resolve(c, s, s.Date.AddDays(7), 7 / 365.0, why);
        Assert.Equal(-(israelBefore + 1) * c.Rules.Mvp!.MilitaryUpkeepPerTurn!.Value,
            Assert.Single(why, x => x.Message.EndsWith("(military upkeep)")).Delta);
        var formed = Assert.Single(s.Units.Values, u => u.Id >= c.StartingForces.Count);
        Assert.Equal(("recruit", 0), (formed.Type, formed.Service));
    }

    // R-UNIT-010: the legacy resolver does not acquire military rules just because a state has a unit.
    [Fact]
    public void TheLegacyLoopDoesNotChargeUpkeepOrAdvanceService()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Units.Add(1, new Unit(1, "recruit", 1, 0, side: "israel", strength: 3));
        var before = s.Hash();
        var why = new List<Cause>();
        TurnResolver.Resolve(c, s, s.Date.AddDays(7), 7 / 365.0, why);
        Assert.Equal(before, s.Hash());
        Assert.Empty(why);
    }

    // R-UNIT-010, R-UNIT-004: values are explicit, finite and in range rather than defaulting missing keys to zero.
    [Theory]
    [InlineData("military_upkeep_per_turn: 0.1", "", "military_upkeep_per_turn", "required")]
    [InlineData("military_upkeep_per_turn: 0.1", "military_upkeep_per_turn: -1", "military_upkeep_per_turn", ">= 0")]
    [InlineData("military_upkeep_per_turn: 0.1", "military_upkeep_per_turn: .nan", "military_upkeep_per_turn", "finite")]
    [InlineData("military_upkeep_per_turn: 0.1", "military_upkeep_per_turn: .inf", "military_upkeep_per_turn", "finite")]
    [InlineData("veteran_soldier_service: 3", "", "veteran_soldier_service", "required")]
    [InlineData("veteran_soldier_service: 3", "veteran_soldier_service: 0", "veteran_soldier_service", ">= 1")]
    public void TheMilitaryRulesAreRequiredAndInRange(string line, string replacement, string field, string reason)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("  " + line + "\n", replacement == "" ? "" : "  " + replacement + "\n");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal("rules.yaml", e.File);
        Assert.Equal("mvp." + field, e.Field);
        Assert.Contains(reason, e.Message);
    }
}
