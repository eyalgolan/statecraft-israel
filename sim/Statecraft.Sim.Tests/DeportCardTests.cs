using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Deport's follow-up cards (GDD Phase 6 PR 9, #801): an event with <c>deported: &lt;target&gt;</c> fires once the
/// player's Deport has depopulated that place, and at no other time (R-EVT-007). The places, the card and its text are
/// fixture values, not history; DeportTests' fixture turns Deport on (Q-TECH-3), as the real content does not.</summary>
public class DeportCardTests
{
    const string Card = "id: after_town_a\ntitle: Town A\ntext: Its residents have been displaced.\nfrom: 1948-05-15\n"
        + "deported: town_a\ntags: [displacement]\noptions:\n  - id: continue\n    text: Continue\n"
        + "sources: [{ url: \"https://example.org/town_a\", checked: 2026-11-01 }]\nstatus: draft\n";

    static Content Load(string card = Card, string[]? fled = null) =>
        DeportTests.Load(files => files["events/after_town_a.yaml"] = card, fled);

    static StepResult Answer(Content c, GameState s, string outcome) =>
        Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", outcome) });

    // R-EVT-007: the card fires on the turn the player's Deport depopulates its place, at that turn's event step.
    [Fact]
    public void TheCardFiresTheTurnItsPlaceIsDeported()
    {
        var c = Load();
        var s = DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0));
        var waited = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.DoesNotContain("after_town_a", waited.FiredEvents);
        var stepped = Answer(c, waited, SettlementControl.Deport);
        Assert.True(stepped.State.Settlements["town_a"].Depopulated);
        Assert.Contains("after_town_a", stepped.State.PendingEvents);
        Assert.Contains(stepped.Why, w => w.Key == CauseKeys.EventNew && w.Target == "after_town_a");
    }

    // R-EVT-007: Raid and Occupy depopulate nothing, so neither fires the card.
    [Theory]
    [InlineData(SettlementControl.Raid)]
    [InlineData(SettlementControl.Occupy)]
    public void RaidAndOccupyFireNoCard(string outcome)
    {
        var c = Load();
        var s = Answer(c, DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0)), outcome).State;
        Assert.DoesNotContain("after_town_a", s.FiredEvents);
        s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.DoesNotContain("after_town_a", s.FiredEvents);
    }

    // R-EVT-007: a card left unanswered resolves to its one option without drawing from the RNG, as an objective's card does.
    [Fact]
    public void AnUnansweredCardResolvesWithoutADraw()
    {
        var c = Load();
        var s = Answer(c, DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0)), SettlementControl.Deport).State;
        Assert.Contains("after_town_a", s.PendingEvents);
        var rng = s.RngState;
        EventSystem.Delegate(c, s, new List<Cause>());
        Assert.Equal(rng, s.RngState);
        Assert.Equal("continue", s.ChosenOptions["after_town_a"]);
    }

    // R-EVT-007: the loader refuses a card whose place is not a target, whose residents fled (Deport is refused there,
    // R-CTRL-018, so it could never fire), that lacks the displacement tag (so the release gate holds it, R-GATE-001), or
    // that has more than one option.
    [Theory]
    [InlineData("deported: town_a", "deported: town_x", "events[after_town_a].deported")]
    [InlineData("tags: [displacement]", "tags: [security]", "events[after_town_a].tags")]
    [InlineData("    text: Continue\n", "    text: Continue\n  - id: other\n    text: Other\n", "events[after_town_a].options")]
    public void TheLoaderRefusesABadCard(string from, string to, string field)
    {
        var ex = Assert.Throws<ContentException>(() => Load(Card.Replace(from, to)));
        Assert.Equal(field, ex.Field);
    }

    [Fact]
    public void TheLoaderRefusesACardForAPlaceWhoseResidentsFled()
    {
        var ex = Assert.Throws<ContentException>(() => Load(fled: new[] { "town_a" }));
        Assert.Equal("events[after_town_a].deported", ex.Field);
        Assert.Contains("fled", ex.Message);
    }
}
