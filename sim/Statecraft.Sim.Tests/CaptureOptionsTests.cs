using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Statecraft.Sim.Reference;

/// <summary>The capture options helper and the automated players (GDD Phase 6 PR 5, #797). Impact plan section 4.1 item 1:
/// Deport is never the default, never first, never pre-selected, and never chosen by the reference player or delegation;
/// Q-REF-1 (a) (owner, 18 October 2026, #783 batch 3): the reference player and HistoricalAI always Raid. The places are
/// CaptureTests' fixture values, not history.</summary>
public class CaptureOptionsTests
{
    static Content Load() => CaptureTests.Load(CaptureTests.AllTargets);

    static GameState Awaiting(Content c, params string[] ids)
    {
        var s = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0), CaptureTests.Soldier(2, 4, 0));
        foreach (var id in ids) s.Settlements[id] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        return s;
    }

    // R-CTRL-008: the options come in one fixed order, Raid, Occupy, Deport, so Deport is never first; and an option
    // carries no default or selected flag, so the helper cannot pre-select one.
    [Fact]
    public void DeportIsNeverFirstOrDefault()
    {
        var c = Load();
        var options = SettlementControl.CaptureOptions(c, Awaiting(c, "town_a"), "town_a");
        Assert.Equal(new[] { SettlementControl.Raid, SettlementControl.Occupy, SettlementControl.Deport }, options.Select(o => o.Outcome).ToArray());
        Assert.NotEqual(SettlementControl.Deport, options[0].Outcome);
        Assert.Equal(new[] { "Outcome", "Preview", "Refusal", "Refused" },
            typeof(CaptureOption).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    // R-CTRL-007, R-CTRL-008: Raid (PR 5) and Occupy (PR 6, #798) are built. Deport (PR 8) carries the "not built yet"
    // refusal and no preview; a place not awaiting the choice offers nothing.
    [Fact]
    public void OnlyRaidAndOccupyAreBuiltYet()
    {
        var c = Load();
        var s = Awaiting(c, "town_a");
        var options = SettlementControl.CaptureOptions(c, s, "town_a");
        Assert.All(options.Take(2), o => { Assert.Null(o.Refusal); Assert.NotNull(o.Preview); });
        Assert.Contains("not built yet", options[2].Refusal);
        Assert.Null(options[2].Preview);
        Assert.Empty(SettlementControl.CaptureOptions(c, s, "town_c"));
        Assert.Empty(SettlementControl.CaptureOptions(c, s, "nowhere"));
    }

    // R-CTRL-008, spec section 2.6: each built option's preview is the change resolving it makes, checked here on a clone
    // the test resolves itself; for Raid every number is 0. CaptureOptions leaves the state as it found it.
    [Fact]
    public void ThePreviewIsTheOutcome()
    {
        var c = Load();
        var s = Awaiting(c, "town_a");
        s.Attention = 5;
        var before = s.Describe();
        foreach (var o in SettlementControl.CaptureOptions(c, s, "town_a").Where(o => o.Preview != null))
        {
            var after = s.Clone();
            Assert.Null(AttentionSystem.TrySpend(c, after, SettlementControl.ResolveKind));
            Assert.Null(SettlementControl.Resolve(c, after, "town_a", o.Outcome, new List<Cause>()));
            var p = o.Preview!;
            Assert.Equal(after.Attention - s.Attention, p.Attention);
            Assert.Equal(after.Treasury - s.Treasury, p.Money);
            Assert.Equal(after.Debt - s.Debt, p.Debt);
            Assert.Equal(after.Food - s.Food, p.Food);
            Assert.Equal(after.Arms - s.Arms, p.Arms);
            Assert.Equal(after.Indicators.GetValueOrDefault("morale") - s.Indicators.GetValueOrDefault("morale"), p.SocialCohesion);
            Assert.Equal(after.Indicators.GetValueOrDefault("intl_standing") - s.Indicators.GetValueOrDefault("intl_standing"), p.InternationalSupport);
            Assert.Equal(after.Score - s.Score, p.Score);
        }
        var raid = SettlementControl.CaptureOptions(c, s, "town_a")[0].Preview!;
        Assert.Equal((0.0, 0.0, 0.0, 0, 0, 0.0, 0.0, 0), (raid.Attention, raid.Money, raid.Debt, raid.Food, raid.Arms, raid.SocialCohesion, raid.InternationalSupport, raid.Score));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-009 (section 4.1 item 1; Q-REF-1 (a)): HistoricalAI, and the reference player that sends its orders, answer
    // every capture awaiting the choice with Raid, in ordinal id order, and never with Deport. Nothing delegates a capture:
    // EventSystem.Delegate answers events only, and an unanswered capture waits (Q-HIST-1 (a)).
    [Fact]
    public void NoAutomatedPlayerDeports()
    {
        var c = Load();
        var s = Awaiting(c, "town_c", "town_a");
        foreach (var orders in new[] { HistoricalAI.Commands(c, s), ReferencePlayer.Commands(c, s) })
        {
            var captures = orders.OfType<ResolveCapture>().ToList();
            Assert.Equal(new[] { "town_a", "town_c" }, captures.Select(x => x.Settlement).ToArray());
            Assert.All(captures, x => Assert.Equal(SettlementControl.Raid, x.Outcome));
            Assert.DoesNotContain(orders, x => x is ResolveCapture r && r.Outcome == SettlementControl.Deport);
        }
        var next = Simulation.Step(c, s, ReferencePlayer.Commands(c, s));
        Assert.DoesNotContain(next.Rejected, x => x.Command is ResolveCapture);
        Assert.Empty(next.State.Settlements);
    }
}
