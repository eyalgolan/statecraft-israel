using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The event rules of the GDD loop (D-H1; GDD Phase 7 PR 1, #805). The owner took the plan's recommendations
/// for Q-EVT-1 to Q-EVT-4 on 17 October 2026 (#784): keep windows, hard anchors, the cap of 3 soft events a turn and
/// delegation, add no delayed effects, and show every effect of every option. These tests pin those rules on the real
/// monthly content (the mvp-end run) and on small fixtures. The armistice's place ahead of other soft events
/// (TheCapNeverDelaysAnArmistice) needs `front_quiet` and lands with Phase 5 PR 13 (#752).</summary>
public class EventRulesGddTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Every state of the mvp-end run (GoldenRun.Orders, seed 1948, Historical mode), the new game first.</summary>
    static readonly List<GameState> Run = Play();

    static List<GameState> Play()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000) { s = Simulation.Step(C, s, GoldenRun.Orders(C, s)).State; run.Add(s); }
        return run;
    }

    static GameEvent Event(string id) => C.Events.First(e => e.Id == id);

    /// <summary>The events that fired on the turn that ended in Run[i] (for i = 0, those raised by the new game).</summary>
    static List<GameEvent> FiredOn(int i) =>
        Run[i].FiredEvents.Where(id => i == 0 || !Run[i - 1].FiredEvents.Contains(id)).Select(Event).ToList();

    // R-EVT-001: every hard anchor fires on the first turn its window overlaps, when nothing else (requires, conditions,
    // an objective) holds it back.
    [Fact]
    public void HardAnchorsAlwaysFire()
    {
        Assert.Equal(GameOverCodes.MvpEnded, Run[Run.Count - 1].GameOver);
        var plain = C.Events.Where(e => e.Anchor == "hard" && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.Objective == "").ToList();
        Assert.NotEmpty(plain);
        foreach (var e in plain)
        {
            // The first state whose turn overlaps the window: [previous date, date) meets [From, To), or the new game is in it.
            var first = Enumerable.Range(0, Run.Count).First(i => e.From <= Run[i].Date && (e.To == null || (i == 0 ? Run[0].Date : Run[i - 1].Date) < e.To.Value));
            Assert.True(FiredOn(first).Contains(e), $"{e.Id} did not fire on {Run[first].Date:yyyy-MM-dd}, the first turn its window overlaps");
        }
    }

    // R-EVT-001: at most MaxSoftEventsPerTurn (3) soft events fire on any turn of the mvp-end run.
    [Fact]
    public void AtMostThreeSoftEventsATurn()
    {
        Assert.Equal(3, EventSystem.MaxSoftEventsPerTurn);
        for (int i = 0; i < Run.Count; i++)
        {
            var soft = FiredOn(i).Where(e => e.Anchor != "hard").Select(e => e.Id).ToList();
            var hard = FiredOn(i).Count(e => e.Anchor == "hard");
            // the soft events take only the slots the hard anchors leave
            Assert.True(soft.Count <= Math.Max(0, EventSystem.MaxSoftEventsPerTurn - hard), $"{Run[i].Date:yyyy-MM-dd}: {hard} hard, soft {string.Join(", ", soft)}");
        }
    }

    static string Soft(string id, string from) =>
        $"id: {id}\ntitle: '{id}'\ntext: Something happens.\nfrom: {from}\noptions:\n  - id: ok\n    text: Accept\n    historical: true\n";

    // R-EVT-001: the soft events take only the slots the hard anchors leave. With 1 hard anchor and 3 soft events
    // eligible on one turn, exactly 2 soft events fire (the mvp-end run has no such turn, so a fixture pins it).
    [Fact]
    public void HardAnchorsUseUpSoftSlots()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["a.yaml"] = Soft("a", "1948-05-15"),
            ["b.yaml"] = Soft("b", "1948-05-15"),
            ["c.yaml"] = Soft("c", "1948-05-15"),
            ["h.yaml"] = Soft("h", "1948-05-15").Replace("from:", "anchor: hard\nfrom:"),
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "a", "b", "h" }, s.PendingEvents);   // h takes one of the 3 slots; c waits
    }

    // R-EVT-002: the soft events take the free slots in order of From, then id; the one left over waits for the next turn.
    [Fact]
    public void SoftEventsTakeTheSlotsInOrderOfFromThenId()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string>
        {
            ["a.yaml"] = Soft("a", "1948-05-15"),
            ["b.yaml"] = Soft("b", "1948-05-15"),
            ["c.yaml"] = Soft("c", "1948-05-15"),
            ["d.yaml"] = Soft("d", "1948-05-01"),
        }));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "d", "a", "b" }, s.PendingEvents);   // d opened first; then a and b by id; c finds no slot
        var next = Simulation.Step(c, s, new List<Command>()).State;
        Assert.Equal(new[] { "c" }, next.PendingEvents);
    }

    const string Choice = @"
id: choice
title: A choice
text: '{finance_minister} asks for a decision.'
office: finance_minister
anchor: {ANCHOR}
from: 1948-05-15
options:
  - id: first
    text: The first way
    recommended: true
    effects:
      - target: fx_reserves
        delta: -10
  - id: history
    text: The way it went
    historical: true
    effects:
      - target: food_supply
        delta: 0.1
    set_policy:
      rationing: 0.4
    set_rung: 3
";

    static Content ChoiceContent(string anchor) =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["choice.yaml"] = Choice.Replace("{ANCHOR}", anchor) }));

    // R-EVT-003: an event the player leaves unanswered at End Turn is delegated to its office's holder; in Historical
    // mode a hard anchor takes its historical option with no RNG draw.
    [Fact]
    public void AnUnansweredEventIsDelegated()
    {
        var c = ChoiceContent("soft");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(new[] { "choice" }, s.PendingEvents);
        var r = Simulation.Step(c, s, new List<Command>());
        Assert.Empty(r.State.PendingEvents);
        Assert.True(r.State.ChosenOptions.ContainsKey("choice"));
        Assert.Contains(r.Why, w => w.System == "events" && w.Target == "choice" && w.Message.StartsWith("Delegated to "));

        var hard = ChoiceContent("hard");
        var h = Simulation.NewGame(hard, 1, Mode.Historical);
        var rng = h.RngState;
        EventSystem.Delegate(hard, h, new List<Cause>());
        Assert.Equal("history", h.ChosenOptions["choice"]);   // not the recommended one: the historical option, always
        Assert.Equal(rng, h.RngState);                       // and no draw
    }

    // R-EVT-004: an option's effects apply on the turn it is chosen, never later; no option can carry a delay.
    [Fact]
    public void NoEffectArrivesLater()
    {
        var c = ChoiceContent("soft");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var first = Simulation.Step(c, s, new List<Command> { new ChooseEventOption("choice", "first") });
        Assert.Contains(first.Why, w => w.System == "events" && w.Target == "fx_reserves" && w.Delta == -10);
        var later = first.State;
        for (int turn = 0; turn < 3; turn++)
        {
            var r = Simulation.Step(c, later, new List<Command>());
            // Nothing of the event comes back on a later turn. An effect is applied only in Resolve, which writes an
            // "events" cause for each change, so the causes are the record to check.
            Assert.DoesNotContain(r.Why, w => w.System == "events");
            later = r.State;
        }
        var delayed = Choice.Replace("{ANCHOR}", "soft").Replace("        delta: -10\n", "        delta: -10\n    delay: 2\n");
        Assert.Contains("delay", delayed);
        var refused = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["choice.yaml"] = delayed })));
        Assert.Contains("delay", refused.Message);
    }

    // R-EVT-005: one sim helper lists every effect of an option, in the order they apply: the numeric effects, the
    // policies it sets, then the security level.
    [Fact]
    public void ThePreviewListsEveryEffectOfAnOption()
    {
        var c = ChoiceContent("soft");
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var e = c.Events.First(x => x.Id == "choice");
        var lines = EventSystem.OptionEffects(c, s, e.Options[1]);
        Assert.Equal(new[] { "food_supply", "rationing", "rung" }, lines.Select(l => l.Target));
        Assert.Equal(new[] { 0.1, 0.4, 3.0 }, lines.Select(l => l.Delta));
        Assert.Equal(PlayerText.Capitalised(PlayerText.Change(c, "food_supply", 0.1)), lines[0].Message);
        Assert.All(lines, l => Assert.Equal("events", l.System));
        var money = EventSystem.OptionEffects(c, s, e.Options[0]).Single();
        Assert.Equal(("fx_reserves", -10.0), (money.Target, money.Delta));

        // What the helper lists is what choosing the option applies.
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "choice", "history", why));
        Assert.Equal(new[] { "food_supply", "rationing" }, why.Where(w => w.System == "events" && w.Target != "choice").Select(w => w.Target));
        Assert.Equal(3, s.Rung);
    }

    // R-EVT-005: on the real content, for every option of every event on the turn its card reaches the player, the
    // helper lists exactly the option's effects, policies and security level.
    [Fact]
    public void ThePreviewListsEveryEffectOnTheRealContent()
    {
        var checkedOptions = 0;
        foreach (var e in C.Events)
        {
            var s = Run.First(x => x.FiredEvents.Contains(e.Id));   // every event fires in the run (EventRetimingTests)
            foreach (var o in e.Options)
            {
                var expected = o.Effects.Select(fx => (fx.Target, fx.Delta))
                    .Concat(o.SetPolicy.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => (k.Key, k.Value)))
                    .Concat(o.SetRung.HasValue ? new[] { ("rung", (double)o.SetRung.Value) } : new (string, double)[0]);
                Assert.Equal(expected, EventSystem.OptionEffects(C, s, o).Select(l => (l.Target, l.Delta)));
                checkedOptions++;
            }
        }
        Assert.True(checkedOptions > 60, $"only {checkedOptions} options");
    }
}
