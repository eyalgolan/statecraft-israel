using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-MONEY-003: the Treasury never goes below 0; every payment it cannot cover is borrowed (T71, #681).</summary>
public class TreasuryFloorTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    // R-MONEY-003: the golden run (the reference player on the real GDD loop) never leaves a negative Treasury, though it
    // borrows from the fourth monthly turn on. What goes below 0 is the net position (Treasury minus Debt) the top bar shows.
    [Fact]
    public void TheReferenceRunNeverLeavesANegativeTreasury()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        int? firstDebt = null;
        while (s.GameOver == null && s.Turn < 1000)
        {
            s = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State;
            Assert.True(s.Treasury >= 0, $"turn {s.Turn} ({s.Date:yyyy-MM-dd}): Treasury {s.Treasury}");
            if (firstDebt == null && s.Debt > 0) firstDebt = s.Turn;
        }
        Assert.Equal(4, firstDebt);   // 15 September 1948: the HUD's "-IL 1.5m" is 0 cash and IL 1.5m debt
    }

    // R-MONEY-003: an event's cost is a payment like any other. With IL 0.4m in the Treasury, the IL 1m claim on Germany
    // pays 0.4 and borrows 0.6, named for the event; before T71 it left the Treasury at -0.6 until the deficit step.
    [Fact]
    public void AnEventCostTheTreasuryCannotCoverIsBorrowed()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.PendingEvents.Clear();
        s.PendingEvents.Add("reparations_claim");
        s.Treasury = 0.4;
        s.Debt = 3;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(Real, s, "reparations_claim", "claim", why));
        Assert.Equal(0, s.Treasury);
        Assert.Equal(3.6, s.Debt, 9);
        Assert.Contains(why, x => x.System == "economy" && x.Target == "debt" && x.Message == "Borrowed IL 0.6m for A Claim on Germany");
    }

    // R-MONEY-003: the Why list reads the event's cost first, then what was borrowed for it.
    [Fact]
    public void AnEventCostIsReportedBeforeTheBorrowing()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.PendingEvents.Clear();
        s.PendingEvents.Add("reparations_claim");
        s.Treasury = 0.4;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(Real, s, "reparations_claim", "claim", why));
        var money = why.Where(x => x.Target == "treasury" || x.Target == "debt").Select(x => x.Message).ToList();
        Assert.Equal(new[] { "Money -IL 1.0m (A Claim on Germany)", "Borrowed IL 0.6m for A Claim on Germany" }, money);
    }

    // R-MONEY-003: an event that brings money in adds it to the Treasury and borrows nothing, debt or no debt.
    [Fact]
    public void AnEventGainAddsToTheTreasuryAndBorrowsNothing()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.PendingEvents.Clear();
        s.PendingEvents.Add("maabarot_policy");
        s.Treasury = 0.4;
        s.Debt = 3;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(Real, s, "maabarot_policy", "maabarot", why));
        Assert.Equal(2.4, s.Treasury, 9);
        Assert.Equal(3, s.Debt);
        Assert.DoesNotContain(why, x => x.Target == "debt");
    }

    // R-MONEY-003: an event cost the Treasury holds is paid from it, and nothing is borrowed.
    [Fact]
    public void AnEventCostTheTreasuryCoversBorrowsNothing()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.PendingEvents.Clear();
        s.PendingEvents.Add("reparations_claim");
        s.Treasury = 5;
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(Real, s, "reparations_claim", "claim", why));
        Assert.Equal(4, s.Treasury, 9);
        Assert.Equal(0, s.Debt);
        Assert.DoesNotContain(why, x => x.Target == "debt");
    }
}
