using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The economic defeat (GDD Phase 7 PR 5, D-B5; #8): Money is exhausted when the debt is at or above
/// mvp.debt_ceiling (Q-DEF-1) and Food when the national stock is 0 (R-END-001); after mvp.defeat_turns consecutive turn
/// ends with both, the game ends with defeat_economy (Q-DEF-2, N = 3), and a turn with either one not exhausted resets the
/// count (R-END-002). The war loss waits for Phase 5's enemy units (#749).</summary>
public class DefeatTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture with a ceiling of IL 10m and N = 3 (fixture values, not the game's).</summary>
    static Content Gdd(string defeat = "  defeat_turns: 3\n  debt_ceiling: 10\n")
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("  defeat_turns: 3\n  debt_ceiling: 10\n", "").Replace("mvp:\n", "mvp:\n" + defeat);
        return ContentLoader.Load(Fixture.Write(files));
    }

    static GameState Step(Content c, GameState s) => Simulation.Step(c, s, Array.Empty<Command>()).State;

    /// <summary>Money exhausted: the debt at the ceiling. Borrowing only adds to the debt, so it stays exhausted.</summary>
    static void Broke(GameState s, double debt = 10) => s.Debt = debt;

    /// <summary>Food exhausted and kept so: an empty stock, and a foreign currency crisis deep enough to last, so nothing
    /// is imported (R-FOOD-002).</summary>
    static void Starving(GameState s)
    {
        s.Food = 0;
        s.FxReserves = -1e9;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
    }

    // R-END-002
    [Fact]
    public void NTurnsExhaustedEndsTheGame()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Broke(s); Starving(s);
        for (int turn = 1; turn <= 2; turn++)
        {
            s = Step(c, s);
            Assert.Equal(turn, s.ExhaustedTurns);
            Assert.Null(s.GameOver);
        }
        var last = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal(3, last.State.ExhaustedTurns);
        Assert.Equal(GameOverCodes.DefeatEconomy, last.State.GameOver);
        Assert.Contains(last.Why, w => w.System == "era" && w.Target == GameOverCodes.DefeatEconomy);
        // The game is over: an order is refused with the reason, and the turn does not advance.
        var after = Simulation.Step(c, last.State, new Command[] { new SetMobilization(0.5) });
        Assert.Single(after.Rejected);
        Assert.Equal(last.State.Date, after.State.Date);
    }

    // R-END-002
    [Fact]
    public void NMinusOneDoesNotAndARecoveryResetsTheCount()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Broke(s); Starving(s);
        s = Step(c, Step(c, s));
        Assert.Equal(2, s.ExhaustedTurns);
        Assert.Null(s.GameOver);

        // The crisis ends and food is imported again: the count goes back to 0, and the next run of empty turns starts at 1.
        s.ActiveSituations.Remove(EconomySystem.FxCrisis);
        s.FxReserves = 1e9;
        s = Step(c, s);
        Assert.True(s.Food > 0);
        Assert.Equal(0, s.ExhaustedTurns);
        Starving(s);
        s = Step(c, s);
        Assert.Equal(1, s.ExhaustedTurns);
        Assert.Null(s.GameOver);
    }

    // R-END-001
    [Fact]
    public void OneResourceAloneNeverEndsIt()
    {
        var c = Gdd();
        // Money exhausted, Food in stock (imports go on).
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Broke(s, 1000);
        for (int i = 0; i < 6; i++) s = Step(c, s);
        Assert.True(s.Food > 0);
        Assert.Equal(0, s.ExhaustedTurns);
        Assert.Null(s.GameOver);

        // Food exhausted, the debt one step below the ceiling.
        var t = Simulation.NewGame(c, 1, Mode.Historical);
        Starving(t);
        t.Treasury = 1e9;   // no shortfall, so nothing is borrowed
        t.Debt = Math.BitDecrement(10.0);
        for (int i = 0; i < 6; i++) t = Step(c, t);
        Assert.Equal(0, t.Food);
        Assert.True(t.Debt < 10);
        Assert.Equal(0, t.ExhaustedTurns);
        Assert.Null(t.GameOver);
    }

    // R-END-002: the count is the rules' N, read from rules.yaml.
    [Fact]
    public void NComesFromTheRules()
    {
        var c = Gdd("  defeat_turns: 1\n  debt_ceiling: 10\n");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Broke(s); Starving(s);
        Assert.Equal(GameOverCodes.DefeatEconomy, Step(c, s).GameOver);
    }

    // R-END-001, R-END-002: the owner's values (Q-DEF-1: IL 250m; Q-DEF-2: 3 turns), labelled design values.
    [Fact]
    public void TheRealRulesCarryTheOwnersValues()
    {
        Assert.Equal(3, Real.Rules.Mvp!.DefeatTurns);
        Assert.Equal(250.0, Real.Rules.Mvp!.DebtCeiling);
    }

    // R-END-001, R-END-002: both values are required in the GDD loop, so a missing key is never read as 0 (a defeat on
    // the first turn with no debt, or no defeat at all).
    [Theory]
    [InlineData("  defeat_turns: 3\n  debt_ceiling: 10\n", "  debt_ceiling: 10\n", "mvp.defeat_turns", "required")]
    [InlineData("  defeat_turns: 3\n  debt_ceiling: 10\n", "  defeat_turns: 3\n", "mvp.debt_ceiling", "required")]
    [InlineData("  defeat_turns: 3\n  debt_ceiling: 10\n", "  defeat_turns: 0\n  debt_ceiling: 10\n", "mvp.defeat_turns", ">= 1")]
    [InlineData("  defeat_turns: 3\n  debt_ceiling: 10\n", "  defeat_turns: 3\n  debt_ceiling: -1\n", "mvp.debt_ceiling", ">= 0")]
    [InlineData("  defeat_turns: 3\n  debt_ceiling: 10\n", "  defeat_turns: 3\n  debt_ceiling: .inf\n", "mvp.debt_ceiling", ">= 0")]
    public void TheDefeatValuesAreRequiredAndInRange(string line, string replacement, string field, string reason)
    {
        var files = Fixture.Gdd();
        Assert.Contains(line, files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace(line, replacement);
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Contains(field, e.Message);
        Assert.Contains(reason, e.Message);
    }

    // R-END-002 (plan risk 3): the reference run and the Historical-mode run with no orders reach 15 January 1952 and
    // are never defeated, so the existing goldens do not move.
    [Fact]
    public void TheHistoricalRunIsNeverDefeated()
    {
        foreach (var (name, orders) in new (string, Func<GameState, IReadOnlyList<Command>>)[]
                 {
                     ("reference", s => GoldenRun.Orders(Real, s)),
                     ("no orders", _ => Array.Empty<Command>()),
                 })
        {
            var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
            while (s.GameOver == null)
            {
                s = Simulation.Step(Real, s, orders(s)).State;
                Assert.True(s.ExhaustedTurns == 0, $"{name}: Money and Food both exhausted on {s.Date:yyyy-MM-dd}");
            }
            Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
            Assert.Equal(Real.End, s.Date);
        }
    }

    // R-END-001, R-END-002: scenario-mvp-defeat overspends and refuses foreign currency until it is defeated.
    [Fact]
    public void TheDefeatScenarioEndsInDefeat()
    {
        var sc = Scenario.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ScenarioTests.Dir(), "mvp-defeat.yaml")));
        var s = sc.Play(Real);
        Assert.Equal(GameOverCodes.DefeatEconomy, s.GameOver);
        Assert.Equal(Real.Rules.Mvp!.DefeatTurns, s.ExhaustedTurns);
        Assert.Equal(0, s.Food);
        Assert.True(s.Debt >= Real.Rules.Mvp!.DebtCeiling);
    }

    // R-END-002, R-LOOP-002: the counter is in the hash only when above 0, so a state that never had both exhausted
    // hashes as before.
    [Fact]
    public void TheCounterIsHashedOnlyWhenSet()
    {
        var s = new GameState();
        var before = s.Hash();
        Assert.DoesNotContain("exhausted=", s.Describe());
        s.ExhaustedTurns = 1;
        Assert.Contains("exhausted=1", s.Describe());
        Assert.NotEqual(before, s.Hash());
        s.ExhaustedTurns = 0;
        Assert.Equal(before, s.Hash());
    }

    // R-END-002: the counter belongs to the GDD loop; a legacy game never counts.
    [Fact]
    public void TheLegacyLoopNeverCounts()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Debt = 1e9; s.Food = 0;
        s = Step(c, s);
        Assert.Equal(0, s.ExhaustedTurns);
        Assert.Null(s.GameOver);
    }
}
