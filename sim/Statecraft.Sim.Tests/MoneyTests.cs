using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 2 PR 2: Money is the Treasury (spec §1.2, "Treasury becomes Money"; owner, 15 October 2026,
/// Q-MONEY-4), and debt and foreign currency stay in the sim, internal under Money (D-C3, Q-MONEY-1).</summary>
public class MoneyTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    // R-MONEY-001: the player sees the Treasury as Money; the account keeps its id, and debt and foreign currency keep theirs.
    [Fact]
    public void TheTreasuryIsShownAsMoney()
    {
        Assert.Equal("Money", PlayerText.Target(C, "treasury"));
        Assert.Equal("IL 2.0m", PlayerText.Money(C, "treasury", 2));
        Assert.Equal(new[] { "treasury", "debt", "fx_reserves" }, C.Money.Select(m => m.Id));
        Assert.Equal("Debt", PlayerText.Target(C, "debt"));
        Assert.Equal("Foreign currency", PlayerText.Target(C, "fx_reserves"));
    }

    // R-MONEY-001 (D-C3): a guard, not a change. The real game runs the GDD loop, its debt and foreign currency are in the
    // state hash, and the sourced 1951 crisis still happens: debt is taken on, and reserves run out after 19 October 1951
    // (FRUS 1951 v.V doc 513) and before the turn that covers the end of 1951, as EconomyCalibrationTests anchors.
    [Fact]
    public void DebtAndForeignCurrencyStayInTheSim()
    {
        Assert.True(C.IsGdd);
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.Date <= new DateTime(1952, 1, 1) && s.GameOver == null)
        {
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            history.Add(s);
        }
        Assert.True(history.Last().Debt > 0, "the run never borrowed");
        var i = history.FindIndex(x => x.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.True(i > 0, "foreign currency never ran out");
        Assert.True(history[i].Date > new DateTime(1951, 10, 19) && history[i - 1].Date < new DateTime(1952, 1, 1),
            "reserves ran out in the turn ending " + PlayerText.Date(history[i].Date));

        var start = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var hash = start.Hash();
        var withDebt = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical); withDebt.Debt += 1;
        var withFx = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical); withFx.FxReserves += 1;
        Assert.NotEqual(hash, withDebt.Hash());
        Assert.NotEqual(hash, withFx.Hash());
    }
}
