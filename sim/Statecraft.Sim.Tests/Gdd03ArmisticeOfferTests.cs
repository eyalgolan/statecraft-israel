using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2478 (#1709 slice B), R-WAR-008, R-EVT-002 and R-ALT-008 under exact gdd-0.3: an armistice card with
/// `front_quiet` may come before its hard-anchor date, and again after a refusal, once its own front may have an early offer
/// (#2318's Armistice.MayOfferEarly: from the 15 December 1948 turn, military.armistice_quiet_turns quiet turns, two full
/// quiet turns after a refusal). On that path it is a soft event that takes the first soft slot, ahead of consequence cards
/// and the other soft events, within the cap of 3. Gdd03MovementFortTests' board and the real catalogue (2 quiet turns).</summary>
public class Gdd03ArmisticeOfferTests
{
    // The real cards' shape (content/events/armistice_*.yaml) with their own front. The dates are the fixture's: Egypt's
    // hard anchor is a year after the turns these tests play, Jordan's comes later still.
    static string Card(string front, string from) => $@"
id: armistice_{front}
title: Armistice with {front}
text: 'Israel decides whether to sign.'
office: prime_minister
anchor: hard
front_quiet: {front}
from: {from}
options:
  - id: sign
    text: Sign the armistice
    historical: true
  - id: hold_out
    text: Refuse and keep fighting
    alternative: {{ basis: design }}
";

    const string Parent = @"
id: parent
title: Parent
text: 'A real decision.'
from: 1948-12-01
to: 1948-12-11
options:
  - id: record
    text: What happened
    historical: true
  - id: road
    text: The road not taken
    alternative:
      basis: proposed
      by: the opposition benches
      date: 1948-12-05
      sources: [{ url: ""https://example.org/minutes"", checked: 2026-10-17 }]
";

    const string Consequence = @"
id: after_road
kind: consequence
title: After the road
text: 'What the record says was feared.'
from: 1948-12-11
to: 1949-02-01
requires: [{ event: parent, option: road }]
options:
  - id: hold
    text: Hold the course
    alternative:
      basis: debated
      by: the cabinet
      date: 1948-12-12
      sources: [{ url: ""https://example.org/cabinet"", checked: 2026-10-17 }]
";

    // An ordinary soft event whose `from` is earlier than every armistice's, so ordering by `from` alone would put it first.
    static string Soft(string id) => $"id: {id}\ntitle: {id}\ntext: 'Ordinary.'\nfrom: 1948-12-01\nto: 1949-02-01\noptions:\n  - id: ok\n    text: Ok\n";

    static Content Load(string ruleset = SaveGame.Gdd03Ruleset, string jordanFrom = "1950-01-01", bool others = true)
    {
        var f = Gdd03MovementFortTests.Files(ruleset);
        f["events/armistice_egypt.yaml"] = Card("egypt", "1949-12-01");
        f["events/armistice_jordan.yaml"] = Card("jordan", jordanFrom);
        if (others)
        {
            f["events/parent.yaml"] = Parent;
            f["events/after_road.yaml"] = Consequence;
            foreach (var id in new[] { "a1", "a2", "a3" }) f[$"events/{id}.yaml"] = Soft(id);
        }
        return ContentLoader.Load(Fixture.Write(f));
    }

    static GameState At(Content c, DateTime date, int egypt, int jordan)
    {
        var s = CombatTests.With(c);
        s.Date = date;
        s.QuietTurns["egypt"] = egypt; s.QuietTurns["jordan"] = jordan;
        s.FiredEvents.Add("parent");   // decided earlier; only after_road's requirement reads it
        s.ChosenOptions["parent"] = "record";
        return s;
    }

    static readonly DateTime December15 = new DateTime(1948, 12, 15);

    static List<string> Fire(Content c, GameState s)
    {
        EventSystem.Trigger(c, s, new List<Cause>(), s.Date.AddDays(-1));
        return s.PendingEvents.ToList();
    }

    // R-WAR-008, R-EVT-002 (owner, 27 October 2026, #1709: "The first offer can come on the 15 December 1948 turn, not
    // earlier"): quiet fronts get no early offer on the turn before; on the 15 December turn the card comes, ahead of the
    // ordinary soft events.
    [Fact]
    public void R_EVT_002_NoEarlyOfferBeforeTheFifteenthOfDecemberTurn()
    {
        var c = Load(others: false);
        Assert.Empty(Fire(c, At(c, new DateTime(1948, 12, 14), 5, 5)));
        Assert.Equal(new[] { "armistice_egypt", "armistice_jordan" }, Fire(c, At(c, December15, 5, 5)));
    }

    // R-WAR-008, R-WAR-005: the card reads its own front's count, so another front's quiet turns never open it.
    [Fact]
    public void R_EVT_002_AnotherFrontsQuietTurnsDoNotOpenTheCard()
    {
        var c = Load(others: false);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, At(c, December15, 2, 1)));
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, At(c, December15, 0, 7)));
    }

    // R-EVT-002, R-ALT-008 (Q-EVT-1, Q-OBJ-1): an early armistice takes the first soft slot, then the consequence card, then
    // the ordinary soft events by `from` and id, three in all.
    [Fact]
    public void R_EVT_002_R_ALT_008_AnEarlyArmisticeComesAheadOfConsequenceAndOrdinarySoftEvents()
    {
        var c = Load();
        var s = At(c, December15, 2, 0);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(new[] { "armistice_egypt", "after_road", "a1" }, Fire(c, s));
    }

    // R-EVT-002, R-ALT-008: two fronts eligible together take the first two slots in a stable order (the cards' `from`, then
    // the id, ordinal) and the cap of 3 still holds; with the same `from` the id decides.
    [Theory]
    [InlineData("1950-01-01", new[] { "armistice_egypt", "armistice_jordan", "after_road" })]
    [InlineData("1949-12-01", new[] { "armistice_egypt", "armistice_jordan", "after_road" })]
    [InlineData("1949-11-01", new[] { "armistice_jordan", "armistice_egypt", "after_road" })]
    public void R_EVT_002_R_ALT_008_TwoEligibleFrontsKeepAStableOrderAndTheCap(string jordanFrom, string[] expected)
    {
        var c = Load(jordanFrom: jordanFrom);
        var s = At(c, December15, 3, 3);
        s.ChosenOptions["parent"] = "road";
        var pending = Fire(c, s);
        Assert.Equal(expected, pending);
        Assert.Equal(EventSystem.MaxSoftEventsPerTurn, pending.Count);
        var again = At(c, December15, 3, 3);
        again.ChosenOptions["parent"] = "road";
        Assert.Equal(pending, Fire(c, again));
    }

    // R-EVT-001 (#2605), R-WAR-008: an early armistice is soft, so when hard anchors take every slot it waits with the other
    // soft events left out. It comes on the next turn its front is still quiet, ahead of them, and leaves the wait. If the
    // front was engaged meanwhile it stays out: the wait keeps a card past its window, not past its other gates.
    [Fact]
    public void R_EVT_001_AnEarlyArmisticeTheCapLeavesOutWaitsForItsQuietFront()
    {
        var c = Load();
        foreach (var id in new[] { "h1", "h2", "h3" })
            c.Events.Add(new GameEvent
            {
                Id = id, Title = id, Anchor = "hard", From = new DateTime(1948, 12, 1),
                Options = new List<EventOption> { new EventOption { Id = "ok", Text = "Ok" } },
            });
        var s = At(c, December15, 2, 0);
        Assert.Equal(new[] { "h1", "h2", "h3" }, Fire(c, s));
        Assert.Equal(new[] { "a1", "a2", "a3", "armistice_egypt" }, s.CappedEvents);

        var engaged = s.Clone();
        engaged.PendingEvents.Clear();
        engaged.Date = December15.AddDays(14);
        engaged.QuietTurns["egypt"] = 0;
        Assert.Equal(new[] { "a1", "a2", "a3" }, Fire(c, engaged));
        Assert.Contains("armistice_egypt", engaged.CappedEvents);

        var quiet = s.Clone();
        quiet.PendingEvents.Clear();
        quiet.Date = December15.AddDays(14);
        quiet.QuietTurns["egypt"] = 3;
        Assert.Equal(new[] { "armistice_egypt", "a1", "a2" }, Fire(c, quiet));
        Assert.Equal(new[] { "a3" }, quiet.CappedEvents);
    }

    // R-EVT-001, R-WAR-008: the sourced hard-anchor date stays the card's ordinary path: on or after it, a front that is not
    // quiet still gets its card, as a hard anchor that takes no soft slot.
    [Fact]
    public void R_EVT_001_TheHardAnchorDateStaysTheOrdinaryPath()
    {
        var c = Load();
        var s = At(c, new DateTime(1949, 12, 15), 0, 0);
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));

        var early = At(c, new DateTime(1949, 1, 15), 2, 0);
        Assert.Equal(new[] { "armistice_egypt", "a1", "a2" }, Fire(c, early));   // early, it is soft: a3 waits
    }

    // R-WAR-006, R-WAR-008 (Q49: "Two full quiet turns must follow the refusal"): a card refused on turn T does not come at
    // the end of T or T+1, and comes again at the end of T+2, once T+1 and T+2 were quiet; then signing ends it.
    [Fact]
    public void R_EVT_002_ARefusedOfferReturnsOnlyAfterTwoFullQuietTurns()
    {
        var c = Load(others: false);
        var s = CombatTests.With(c, new Unit(1, "recruit", 0, 1, side: "israel", strength: 6));
        s.Date = December15;
        s.QuietTurns["jordan"] = 5;
        s.FiredEvents.Add("armistice_egypt");
        s.ChosenOptions["armistice_egypt"] = "sign";   // Egypt is out of the way: a signed front gets no card
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));

        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "hold_out"));
        Assert.Empty(s.PendingEvents);
        s = Turn(c, s);
        Assert.Empty(s.PendingEvents);
        s = Turn(c, s);
        Assert.Equal(new[] { "armistice_jordan" }, s.PendingEvents);
        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "sign"));
        s = Turn(c, s);
        s = Turn(c, s);
        Assert.Empty(s.PendingEvents);
    }

    // R-WAR-006, R-WAR-008: a card refused on or after its date is not brought back by its date while its front is not
    // quiet, so it does not come every turn; it comes back once the front has had two quiet turns again.
    [Fact]
    public void R_EVT_002_ACardRefusedOnItsDateReturnsOnlyThroughTheQuietPath()
    {
        var c = Load(others: false);
        var s = At(c, new DateTime(1950, 2, 15), 2, 1);
        s.FiredEvents.Add("armistice_egypt"); s.ChosenOptions["armistice_egypt"] = "sign";
        s.FiredEvents.Add("armistice_jordan"); s.ChosenOptions["armistice_jordan"] = "hold_out";
        Assert.Empty(Fire(c, s));
        s.QuietTurns["jordan"] = 2;
        Assert.Equal(new[] { "armistice_jordan" }, Fire(c, s));
    }

    // R-WAR-008, R-EVT-003 (owner, 4 November 2026, #2508, option A, "keep early signing"): in Historical mode an early card
    // left unanswered is still a hard anchor for delegation, so it signs on the 15 December 1948 turn, long before its date.
    [Fact]
    public void R_EVT_003_HistoricalModeSignsAnUnansweredEarlyCard()
    {
        var c = Load(others: false);
        var s = CombatTests.With(c, new Unit(1, "recruit", 0, 1, side: "israel", strength: 6));
        s.Mode = Mode.Historical;
        s.Date = December15;
        s.QuietTurns["egypt"] = 2;
        Assert.Equal(new[] { "armistice_egypt" }, Fire(c, s));

        var rng = s.RngState;
        s = Turn(c, s);
        Assert.Equal("sign", s.ChosenOptions["armistice_egypt"]);
        Assert.Equal(rng, s.RngState);   // the hard-anchor path: ordinary delegation would draw
    }

    static GameState Turn(Content c, GameState s, params Command[] orders)
    {
        var r = Simulation.Step(c, s, orders);
        Assert.Empty(r.Rejected);
        return r.State;
    }

    // R-WAR-008 is gdd-0.3 only: under gdd-0.1 the same quiet counts open nothing before the hard-anchor date.
    [Fact]
    public void Gdd01HasNoEarlyOffer()
    {
        var c = Load(SaveGame.GddRuleset);
        var s = At(c, December15, 5, 5);
        s.ChosenOptions["parent"] = "road";
        Assert.Equal(new[] { "after_road", "a1", "a2" }, Fire(c, s));
    }

    // The loader: `front_quiet` names one of Prisoners.Fronts, and only on that front's own card (armistice_<front>), whose
    // refusal resets that front's count (R-WAR-006).
    [Theory]
    [InlineData("iraq", "unknown front 'iraq'")]
    [InlineData("egypt", "armistice_egypt")]
    public void TheLoaderRefusesAFrontQuietThatIsNotTheCardsOwnFront(string front, string message)
    {
        var f = Gdd03MovementFortTests.Files();
        f["events/armistice_jordan.yaml"] = Card("jordan", "1950-01-01").Replace("front_quiet: jordan", "front_quiet: " + front);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f)));
        Assert.Equal("events[armistice_jordan].front_quiet", ex.Field);
        Assert.Contains(message, ex.Message);
    }
}
