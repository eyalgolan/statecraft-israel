using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2536: an objective completed on the turn that ends the game still shows its card (R-OBJ-003, R-LOOP-004; the
/// owner's choice "Show the card (Recommended)", 4 November 2026). No other event fires on that turn, and the score is the
/// one step 11 records.</summary>
public class LastTurnObjectiveCardTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The mvp-end run's state one turn before the game's end (GoldenRun.Orders, seed 1948, Historical mode).</summary>
    static GameState BeforeTheEnd(Content c, int turnsBefore = 1)
    {
        var run = new List<GameState> { Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical) };
        for (var i = 0; i < 1000; i++)
        {
            var s = run[run.Count - 1];
            var next = Simulation.Step(c, s, GoldenRun.Orders(c, s)).State;
            if (next.GameOver != null) return run[run.Count - turnsBefore];
            run.Add(next);
        }
        throw new InvalidOperationException("the run never ended");
    }

    static GameState BeforeTheEnd() => BeforeTheEnd(Real);

    /// <summary>Every segment of the Haifa to Tel Aviv rail link built, so step 11 records the railway objective (R-OBJ-005)
    /// at the end of the next turn. The mvp-end run builds none of it.</summary>
    static void BuildRailway(GameState s)
    {
        var def = Real.Corridors.Single(x => x.Id == "tlv_haifa_rail");
        s.Corridors["tlv_haifa_rail"] = new CorridorWork(def.Id, def.Segments.Select(_ => new SegmentWork(null, 0, s.Date)).ToArray());
    }

    static int Points(StepResult r, string objective) =>
        (int)r.Why.Single(w => w.Key == CauseKeys.ObjectiveCompleted && w.Target == objective).Delta;

    // R-OBJ-003, R-LOOP-004: the objective completed on the final turn (the one ending 15 January 1952) shows its card on
    // that turn, and only its card: an event otherwise eligible then stays unfired. The score is step 11's.
    [Fact]
    public void AnObjectiveCompletedOnTheLastTurnShowsItsCard()
    {
        var before = BeforeTheEnd();
        Assert.False(before.Objectives.ContainsKey("railway"), "precondition: the mvp-end run does not complete the railway");
        BuildRailway(before);
        // The road done earlier (its card shown then), so the railway is the infrastructure objective's last part and pays
        // its base (R-OBJ-006): the run has already recorded the yishuvim.
        Assert.True(Objectives.Record(Real, before, Real.RulesAt(before.Date).Mvp!, "burma_road", before.Date, new List<Cause>()));
        before.FiredEvents.Add("burma_road_complete");
        // An event open on the last turn that nothing else holds back, made unfired so it would be eligible then.
        var other = Real.Events.First(e => e.Objective == "" && e.Site == "" && e.Prisoners == "" && e.Deported == ""
            && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.From <= before.Date && (e.To == null || e.To.Value > before.Date));
        before.FiredEvents.Remove(other.Id);

        var last = Simulation.Step(Real, before, Array.Empty<Command>());
        var s = last.State;
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Equal(new DateTime(1952, 1, 15), s.Date);
        Assert.Equal(new DateTime(1952, 1, 15), s.Objectives["railway"]);
        Assert.Equal(new[] { "railway_complete" }, s.PendingEvents);
        Assert.Equal(new[] { "railway_complete" }, s.FiredEvents.Except(before.FiredEvents));
        Assert.DoesNotContain(other.Id, s.FiredEvents);
        Assert.Contains(last.Why, w => w.Key == CauseKeys.EventNew && w.Target == "railway_complete");
        Assert.True(Points(last, "railway") > 0, "the objective scores");
        Assert.Equal(before.Score + Points(last, "railway"), s.Score);
        Assert.Equal(s.Score, EndReport.For(Real, s).Score);
    }

    // R-OBJ-003, R-LOOP-004: ending objective cards are not subject to the ordinary three-soft-card cap.
    // Even otherwise eligible hard-anchor and Deport cards are excluded from the ending turn.
    // The content is loaded fresh here (not Real), since this test rewrites objectives and events in memory.
    [Fact]
    public void TheEndingTurnFiresEveryObjectiveCardUncappedAndNoOtherCategory()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var before = BeforeTheEnd(c);
        before.PendingEvents.Clear();

        var cards = new List<GameEvent>();
        foreach (var objective in c.Objectives)
        {
            // Make every real objective newly complete in step 11 of this final turn.
            objective.CompletesWhen = new ObjectiveCondition
            {
                Absorbed = new AbsorbCondition
                {
                    PopulationAtLeast = 0,
                    OverflowAtMost = 1m,
                    EmployedAtLeast = 0m,
                },
            };
            before.Objectives.Remove(objective.Id);

            var card = c.Events.Single(e => e.Id == objective.Card);
            card.From = before.Date;
            card.To = null;
            card.Anchor = "soft";
            card.Requires.Clear();
            card.Conditions.Clear();
            before.FiredEvents.Remove(card.Id);
            cards.Add(card);
        }

        Assert.True(cards.Count >= 4, "precondition: at least four soft objective cards exercise the normal cap");
        Assert.All(cards, card => Assert.Equal("soft", card.Anchor));

        var hard = new GameEvent
        {
            Id = "fixture_ending_hard",
            Title = "Eligible hard fixture",
            Anchor = "hard",
            From = before.Date,
            Options = new List<EventOption>
            {
                new EventOption { Id = "continue", Text = "Continue" },
            },
        };
        var deport = new GameEvent
        {
            Id = "fixture_ending_deport",
            Title = "Eligible Deport fixture",
            From = before.Date,
            Deported = "acre",
            Tags = new List<string> { "displacement" },
            Options = new List<EventOption>
            {
                new EventOption { Id = "continue", Text = "Continue" },
            },
        };
        c.Events.Add(hard);
        c.Events.Add(deport);
        before.Settlements["acre"] = new SettlementControl(since: before.Date, depopulated: true);

        // Prove both excluded cards satisfy the ordinary trigger, rather than merely asserting against ineligible fixtures.
        var ordinary = before.Clone();
        EventSystem.Trigger(c, ordinary, new List<Cause>(), before.Date);
        Assert.Contains(hard.Id, ordinary.FiredEvents);
        Assert.Contains(deport.Id, ordinary.FiredEvents);

        var last = Simulation.Step(c, before, Array.Empty<Command>());
        var s = last.State;
        var expected = cards.Select(card => card.Id).ToArray();

        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.All(c.Objectives, objective => Assert.Equal(s.Date, s.Objectives[objective.Id]));
        Assert.Equal(expected, s.PendingEvents);
        Assert.Equal(
            expected.OrderBy(id => id, StringComparer.Ordinal),
            s.FiredEvents.Except(before.FiredEvents));
        Assert.Equal(
            expected,
            last.Why.Where(w => w.Key == CauseKeys.EventNew).Select(w => w.Target));

        Assert.DoesNotContain(hard.Id, s.FiredEvents);
        Assert.DoesNotContain(hard.Id, s.PendingEvents);
        Assert.DoesNotContain(deport.Id, s.FiredEvents);
        Assert.DoesNotContain(deport.Id, s.PendingEvents);
    }

    // R-OBJ-003, R-EVT-001, R-LOOP-004: an objective recorded on the turn before the end whose card has not fired yet shows
    // on the ending turn: there is no later turn for it to wait for. Only such cards fire then, uncapped. Objective cards take
    // no soft slot (#2605), so the cap cannot hold one back: here the window does (a `from` the day after that turn's
    // trigger date), and the other objective cards fire on that turn although a hard card takes a slot. Fresh content: the
    // test rewrites objectives and events in memory.
    [Fact]
    public void AnObjectiveCardNotYetFiredShowsOnTheEndingTurn()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var twoBefore = BeforeTheEnd(c, turnsBefore: 2);
        twoBefore.PendingEvents.Clear();
        // The turn before the end triggers its events on this date (Simulation.Step moves the date, then triggers).
        var penultimateDate = Simulation.Step(c, twoBefore, Array.Empty<Command>()).State.Date;
        Assert.True(c.Objectives.Count >= 3, "precondition: at least three objectives, so two can be held back and one fire");

        var cards = new List<GameEvent>();
        foreach (var objective in c.Objectives)
        {
            // Every real objective completes at step 11 of the turn before the end.
            objective.CompletesWhen = new ObjectiveCondition
            {
                Absorbed = new AbsorbCondition { PopulationAtLeast = 0, OverflowAtMost = 1m, EmployedAtLeast = 0m },
            };
            twoBefore.Objectives.Remove(objective.Id);
            var card = c.Events.Single(e => e.Id == objective.Card);
            // The first two objectives' cards open the day after the turn before the end triggers; the rest open now.
            card.From = cards.Count < 2 ? penultimateDate.AddDays(1) : twoBefore.Date;
            card.To = null;
            card.Anchor = "soft";
            card.Requires.Clear();
            card.Conditions.Clear();
            twoBefore.FiredEvents.Remove(card.Id);
            cards.Add(card);
        }
        // A hard card on that turn takes one of the three soft slots, as reparations_vote does on the 15 December 1951 turn.
        c.Events.Add(new GameEvent
        {
            Id = "fixture_penultimate_hard",
            Title = "Hard fixture",
            Anchor = "hard",
            From = twoBefore.Date,
            Options = new List<EventOption> { new EventOption { Id = "continue", Text = "Continue" } },
        });

        var penultimate = Simulation.Step(c, twoBefore, Array.Empty<Command>()).State;
        Assert.Null(penultimate.GameOver);
        Assert.Contains("fixture_penultimate_hard", penultimate.FiredEvents);
        Assert.All(c.Objectives, objective => Assert.Equal(penultimate.Date, penultimate.Objectives[objective.Id]));
        var heldBack = cards.Where(card => !penultimate.FiredEvents.Contains(card.Id)).Select(card => card.Id).ToArray();
        Assert.Equal(cards.Take(2).Select(card => card.Id).OrderBy(id => id, StringComparer.Ordinal),
            heldBack.OrderBy(id => id, StringComparer.Ordinal));
        Assert.Empty(penultimate.CappedEvents.Intersect(cards.Select(card => card.Id)));

        // An event open on the last turn that nothing else holds back, made unfired so it would be eligible then.
        var other = c.Events.First(e => e.Objective == "" && e.Site == "" && e.Prisoners == "" && e.Deported == ""
            && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.From <= penultimate.Date
            && (e.To == null || e.To.Value > penultimate.Date));
        penultimate.FiredEvents.Remove(other.Id);

        var last = Simulation.Step(c, penultimate, Array.Empty<Command>());
        var s = last.State;
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.All(c.Objectives, objective => Assert.Equal(penultimate.Date, s.Objectives[objective.Id]));
        Assert.Equal(heldBack, s.PendingEvents);
        Assert.Equal(heldBack.OrderBy(id => id, StringComparer.Ordinal), s.FiredEvents.Except(penultimate.FiredEvents));
        Assert.Equal(heldBack, last.Why.Where(w => w.Key == CauseKeys.EventNew).Select(w => w.Target));
        Assert.DoesNotContain(other.Id, s.FiredEvents);
        Assert.Equal(penultimate.Score, s.Score);
    }

    // R-LOOP-004: a game ending with no objective completed on its last turn still raises no event on that turn.
    [Fact]
    public void TheLastTurnRaisesNoOtherEvent()
    {
        var before = BeforeTheEnd();
        var other = Real.Events.First(e => e.Objective == "" && e.Site == "" && e.Prisoners == "" && e.Deported == ""
            && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.From <= before.Date && (e.To == null || e.To.Value > before.Date));
        before.FiredEvents.Remove(other.Id);
        var s = Simulation.Step(Real, before, Array.Empty<Command>()).State;
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Empty(s.PendingEvents);
        Assert.DoesNotContain(other.Id, s.FiredEvents);
    }

    // R-OBJ-003, R-LOOP-004: the same on a turn the economic defeat ends (R-END-002): the card shows, nothing else fires.
    [Fact]
    public void AnObjectiveCompletedOnAnEconomicDefeatTurnShowsItsCard()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State;
        s.PendingEvents.Clear();
        var r = Real.RulesAt(s.Date).Mvp!;
        s.Debt = r.DebtCeiling!.Value * 100;
        s.Food = 0;
        s.FxReserves = -1e12;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.ExhaustedTurns = r.DefeatTurns!.Value - 1;
        BuildRailway(s);
        var before = s.Clone();

        var last = Simulation.Step(Real, before, Array.Empty<Command>());
        Assert.Equal(GameOverCodes.DefeatEconomy, last.State.GameOver);
        Assert.True(last.State.Objectives.ContainsKey("railway"));
        Assert.Equal(new[] { "railway_complete" }, last.State.PendingEvents);
        Assert.Equal(new[] { "railway_complete" }, last.State.FiredEvents.Except(before.FiredEvents));
        Assert.Equal(before.Score + Points(last, "railway"), last.State.Score);
    }

    /// <summary>#2536: a one-turn game over fresh content that ends on its first turn's boundary with the railway recorded
    /// then, so its card is the ending card. Returns the save as the new rules write it, and the same save with the final
    /// hash the old rules wrote (the ending card neither fired nor pending).</summary>
    static (Content c, string newSave, string oldSave) FinishedWithAnEndingCard()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var probe = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        c.Rules.Mvp!.End = Simulation.TurnEnd(c, probe);
        var objective = c.Objectives.Single(o => o.Id == "railway");
        objective.CompletesWhen = new ObjectiveCondition
        {
            Absorbed = new AbsorbCondition { PopulationAtLeast = 0, OverflowAtMost = 1m, EmployedAtLeast = 0m },
        };
        var card = c.Events.Single(e => e.Id == objective.Card);
        card.From = probe.Date;
        card.To = null;
        card.Requires.Clear();
        card.Conditions.Clear();

        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        g.Step(Array.Empty<Command>());
        Assert.Equal(GameOverCodes.MvpEnded, g.State.GameOver);
        Assert.Equal(new[] { "railway_complete" }, g.State.PendingEvents);
        var old = g.State.Clone();
        old.FiredEvents.Remove("railway_complete");
        old.PendingEvents.Clear();
        var newSave = g.Save.Write();
        var endLine = "end " + g.State.Hash();
        Assert.Contains(endLine + "\n", newSave);
        return (c, newSave, newSave.Replace(endLine, "end " + old.Hash()));
    }

    // #2536: a game finished before the ending turn fired objective cards replays with its ending card added. That is the
    // only difference, so it loads with no warning (no damaged-save error), over, with the card waiting to be shown.
    [Fact]
    public void AFinishedGameSavedBeforeTheChangeLoadsAndShowsItsEndingCard()
    {
        var (c, newSave, oldSave) = FinishedWithAnEndingCard();
        Assert.NotEqual(newSave, oldSave);
        var loaded = GameSession.Load(c, oldSave, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(GameOverCodes.MvpEnded, loaded.State.GameOver);
        Assert.Equal(new[] { "railway_complete" }, loaded.State.PendingEvents);
        Assert.Contains("railway_complete", loaded.State.FiredEvents);

        GameSession.Load(c, newSave, out var current);   // and a save the new rules wrote loads as before
        Assert.Empty(current);
    }

    // #2536: only that difference is accepted. Any other final hash with the same content is still the damaged-save error.
    [Fact]
    public void AnyOtherReplayDifferenceIsStillRefused()
    {
        var (c, newSave, oldSave) = FinishedWithAnEndingCard();
        var wrong = newSave.Replace(newSave.Split('\n').Single(l => l.StartsWith("end ", StringComparison.Ordinal)), "end " + new string('b', 64));
        Assert.NotEqual(newSave, wrong);
        Assert.Throws<SaveException>(() => GameSession.Load(c, wrong, out _));
    }

    // #2536: with other content the same save gives only content_changed, not replay_differs.
    [Fact]
    public void WithOtherContentAFinishedOldSaveGivesOnlyContentChanged()
    {
        var (c, _, oldSave) = FinishedWithAnEndingCard();
        c.Hash = new string('c', 64);
        GameSession.Load(c, oldSave, out var warnings);
        Assert.Equal(new[] { GameSession.ContentChanged }, warnings);
    }
}
