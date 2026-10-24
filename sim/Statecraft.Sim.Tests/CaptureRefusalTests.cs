using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-CTRL-008, R-CTRL-015 (#802): each refused option carries a stable refusal id and the values its line needs, so
/// the dialog shows a reviewed line in the player's language without reading the sim's English (Q-TECH-5); the English
/// reason stays for the log and the commands. The places are CaptureTests' fixture values, not history.</summary>
public class CaptureRefusalTests
{
    static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);
    static Unit Enemy(int id, int q, int r) => CaptureTests.Enemy(id, q, r);

    static GameState With(Content c, string state, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, state, s.Date);
        return s;
    }

    static CaptureOption Option(IReadOnlyList<CaptureOption> options, string outcome) => options.Single(o => o.Outcome == outcome);

    /// <summary>The refused option's id, its values, and the English reason, which is the option's Refusal.</summary>
    static void Refused(CaptureOption o, string id, string english, params object[] args)
    {
        Assert.NotNull(o.Refused);
        Assert.Equal(id, o.Refused!.Id);
        Assert.Equal(args, o.Refused.Args);
        Assert.Equal(english, o.Refused.Text);
        Assert.Equal(o.Refused.Text, o.Refusal);
        Assert.Null(o.Preview);
    }

    // R-CTRL-008: the ids are a fixed, distinct list, and a refusal with an id outside it cannot be made.
    [Fact]
    public void TheIdsAreAFixedList()
    {
        Assert.Equal(new[] { "attention", "needs_unit", "no_free_hex", "no_one_to_deport", "not_in_play", "already_city", "not_awaiting", "unknown_outcome" },
            CaptureRefusal.Ids);
        Assert.Throws<ArgumentException>(() => new CaptureRefusal("made_up", "English"));
    }

    // R-CTRL-007, R-CTRL-008: with mvp.deport_in_play off, Deport's refusal is not_in_play, which the dialog uses to leave it
    // out until the owner's review (R-CTRL-007).
    [Fact]
    public void DeportOutOfPlayIsNotInPlay()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = With(c, SettlementControl.AwaitingChoice, Soldier(1, 1, 0));
        Refused(Option(SettlementControl.CaptureOptions(c, s, "town_a"), SettlementControl.Deport), CaptureRefusal.NotInPlay, "Deport is not built yet");
        Assert.Null(Option(SettlementControl.CaptureOptions(c, s, "town_a"), SettlementControl.Raid).Refused);
    }

    // R-CTRL-011, R-CTRL-014: Occupy, Annex and Withdraw without an Israeli military unit on the place are needs_unit; the
    // place is the option's own, so the id carries no value for it (the UI names it in its language).
    [Fact]
    public void NoGarrisonIsNeedsUnit()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var capture = With(c, SettlementControl.AwaitingChoice, Soldier(1, 2, 0));
        Refused(Option(SettlementControl.CaptureOptions(c, capture, "town_a"), SettlementControl.Occupy), CaptureRefusal.NeedsUnit, "Occupy needs an Israeli military unit on TOWN A");
        var end = With(c, SettlementControl.AwaitingEnd, Soldier(1, 2, 0));
        var options = SettlementControl.EndOptions(c, end, "town_a");
        Refused(Option(options, SettlementControl.Annex), CaptureRefusal.NeedsUnit, "Annex needs an Israeli military unit on TOWN A");
        Refused(Option(options, SettlementControl.WithdrawOutcome), CaptureRefusal.NeedsUnit, "Withdraw needs an Israeli military unit on TOWN A");
        Assert.Equal(options[0].Refusal, SettlementControl.End(c, end, "town_a", SettlementControl.Annex, new List<Cause>()));
    }

    // R-CTRL-010: a unit with nowhere to withdraw to is no_free_hex (RaidTests.RaidWithNowhereToGoIsRefused's board).
    [Fact]
    public void NowhereToGoIsNoFreeHex()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = With(c, SettlementControl.AwaitingChoice, Soldier(1, 1, 0), Soldier(6, 1, 0), Soldier(7, 2, 0), Enemy(3, 0, 0), Enemy(4, 0, 1), Enemy(5, 1, 1));
        var raid = Option(SettlementControl.CaptureOptions(c, s, "town_a"), SettlementControl.Raid);
        Refused(raid, CaptureRefusal.NoFreeHex, "A unit has no free hex next to TOWN A to withdraw to");
        Assert.Equal(raid.Refusal, SettlementControl.Resolve(c, s, "town_a", SettlementControl.Raid, new List<Cause>()));
    }

    // R-CTRL-018 (OWNER QUESTION 4 (a) on #799): Deport of a fled town is no_one_to_deport.
    [Fact]
    public void AFledTownIsNoOneToDeport()
    {
        var c = DeportTests.Load(fled: new[] { "town_a" });
        var s = With(c, SettlementControl.AwaitingChoice, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date, fled: true);
        Refused(Option(SettlementControl.CaptureOptions(c, s, "town_a"), SettlementControl.Deport), CaptureRefusal.NoOneToDeport,
            "Its residents fled when it was captured, so there is no one to deport");
    }

    // R-CTRL-007, R-CTRL-014: an option the Attention left cannot pay for is attention, with what it needs and what is left.
    [Fact]
    public void TooLittleAttentionIsAttentionWithItsNumbers()
    {
        var c = CaptureTests.Load(files => files["rules.yaml"] = files["rules.yaml"]
            .Replace("resolve_capture: 0,", "resolve_capture: 2,").Replace("end_occupation: 0 }", "end_occupation: 2 }"), CaptureTests.AllTargets);
        var capture = With(c, SettlementControl.AwaitingChoice, Soldier(1, 1, 0));
        capture.Attention = 1;
        var need = AttentionSystem.Cost(c, capture, SettlementControl.ResolveKind);   // 2 at the fixture's rung, raised by its step
        Assert.True(need > 1);
        foreach (var o in SettlementControl.CaptureOptions(c, capture, "town_a"))
            Refused(o, CaptureRefusal.Attention, FormattableString.Invariant($"Not enough attention (need {need:0.0}, have 1.0)"), need, 1.0);
        var end = With(c, SettlementControl.AwaitingEnd, Soldier(1, 1, 0));
        end.Attention = 0.5;
        Assert.Equal(need, AttentionSystem.Cost(c, end, SettlementControl.EndKind));
        foreach (var o in SettlementControl.EndOptions(c, end, "town_a"))
            Refused(o, CaptureRefusal.Attention, FormattableString.Invariant($"Not enough attention (need {need:0.0}, have 0.5)"), need, 0.5);
    }
}
