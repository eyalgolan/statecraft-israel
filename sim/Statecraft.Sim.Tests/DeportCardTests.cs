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

    /// <summary>A hard anchor whose window opens the day after the game's start, so it fires on the first turn's event step.</summary>
    static string Hard(string id) => $"id: {id}\ntitle: {id}\ntext: A hard card.\nanchor: hard\nfrom: 1948-05-16\noptions:\n  - id: ok\n    text: OK\n    historical: true\n"
        + "sources: [{ url: \"https://example.org/hard\", checked: 2026-11-02 }]\nstatus: fact-checked\n";

    // R-EVT-007: the card takes no soft slot, so three hard anchors on the Deport turn, which leave the soft cards none
    // (R-EVT-001), do not push it to a later turn.
    [Fact]
    public void TheCardFiresOnTheDeportTurnEvenWhenTheSoftSlotsAreFull()
    {
        var c = DeportTests.Load(files =>
        {
            files["events/after_town_a.yaml"] = Card;
            foreach (var id in new[] { "hard_one", "hard_two", "hard_three" }) files[$"events/{id}.yaml"] = Hard(id);
        });
        var before = DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0));
        Assert.DoesNotContain(before.FiredEvents, id => id.StartsWith("hard_", StringComparison.Ordinal));
        var stepped = Answer(c, before, SettlementControl.Deport).State;
        Assert.All(new[] { "hard_one", "hard_two", "hard_three" }, id => Assert.Contains(id, stepped.PendingEvents));
        Assert.True(stepped.Settlements["town_a"].Depopulated);
        Assert.Contains("after_town_a", stepped.PendingEvents);
    }

    // R-EVT-007: the card follows its own place only; a Deport of another target leaves it unfired.
    [Fact]
    public void ADeportOfAnotherPlaceFiresNoCard()
    {
        var c = Load();
        var s = CombatTests.With(c, DeportTests.Soldier(1, 4, 0));
        s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        var stepped = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_c", SettlementControl.Deport) }).State;
        Assert.True(stepped.Settlements["town_c"].Depopulated);
        Assert.DoesNotContain("after_town_a", stepped.FiredEvents);
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
    // R-CTRL-018, so it could never fire), that lacks the displacement tag (so the release gate holds it, R-GATE-001), that
    // has more than one option, a `to` (a window that closes before a later Deport), or another card kind's key.
    [Theory]
    [InlineData("deported: town_a", "deported: town_x", "events[after_town_a].deported", "unknown settlement target")]
    [InlineData("tags: [displacement]", "tags: [security]", "events[after_town_a].tags", "carries the displacement tag")]
    [InlineData("    text: Continue\n", "    text: Continue\n  - id: other\n    text: Other\n", "events[after_town_a].options", "exactly one option")]
    [InlineData("from: 1948-05-15\n", "from: 1948-05-15\nto: 1949-01-01\n", "events[after_town_a].to", "has no to")]
    [InlineData("from: 1948-05-15\n", "from: 1948-05-15\nprisoners: egypt\n", "events[after_town_a].prisoners", "not an exchange card")]
    [InlineData("from: 1948-05-15\n", "from: 1948-06-15\n", "events[after_town_a].from", "opens at the game's start")]
    [InlineData("from: 1948-05-15\n", "from: 1948-05-15\nanchor: hard\n", "events[after_town_a].anchor", "is soft")]
    [InlineData("from: 1948-05-15\n", "from: 1948-05-15\nconditions: [{ indicator: morale, max: 0.9 }]\n", "events[after_town_a].conditions", "waits for nothing but its Deport")]
    public void TheLoaderRefusesABadCard(string from, string to, string field, string message)
    {
        var card = to.Contains("anchor: hard") ? Card.Replace("    text: Continue\n", "    text: Continue\n    historical: true\n") : Card;
        var ex = Assert.Throws<ContentException>(() => Load(card.Replace(from, to)));
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-EVT-007: one card per place, so one Deport cannot raise two.
    [Fact]
    public void TheLoaderRefusesASecondCardForThePlace()
    {
        var ex = Assert.Throws<ContentException>(() => DeportTests.Load(files =>
        {
            files["events/after_town_a.yaml"] = Card;
            files["events/after_town_a_again.yaml"] = Card.Replace("id: after_town_a\n", "id: after_town_a_again\n");
        }));
        Assert.Equal("events[after_town_a_again].deported", ex.Field);
        Assert.Contains("already has a card", ex.Message);
    }

    [Fact]
    public void TheLoaderRefusesACardForAPlaceWhoseResidentsFled()
    {
        var ex = Assert.Throws<ContentException>(() => Load(fled: new[] { "town_a" }));
        Assert.Equal("events[after_town_a].deported", ex.Field);
        Assert.Contains("fled when it was captured", ex.Message);
    }
}
