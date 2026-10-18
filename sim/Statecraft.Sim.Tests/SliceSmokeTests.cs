using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>Issue #13: a headless run of the whole M3 slice (15 May 1948 to the end of the 1948-51 content: GoldenRun.Until1952,
/// the last turn ends 27 February 1952) through the command path the UI uses, checking invariants after every turn.
///
/// The UI's only door into the sim is SimBridge (unity/Assets/Scripts/UI/SimBridge.cs, a Unity assembly .NET cannot
/// reference): it queues Commands, refuses one it cannot afford in attention, and End Turn calls GameSession.Step with the
/// queue; autosave writes GameSession.Save.Write() and a restart loads it with GameSession.Load. This test drives that same
/// GameSession with the same Command objects the presenters queue (EventCardPresenter, BudgetPresenter, CabinetPresenter,
/// DefencePresenter, InfoPanelPresenter), and applies SimBridge.Queue's attention check before each order.
///
/// Each turn: (1) no order that is legal by the sim's own checks (pending event and option, Cabinet.Check, MapActions.Check,
/// a level in range) and affordable at this turn's prices is refused; (2) no content id and no ISO date shows in the text
/// the player sees after the turn (Why lines, refusals, the event cards, the top bar's sim-formatted values, the cabinet
/// screen's reasons, map-action reasons); (3) the save loads back to the same hash with no warning, and the game goes on
/// from the loaded session, ending where an uninterrupted run ends.</summary>
public class SliceSmokeTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>docs/determinism.md: the WebGL smoke (?smoke=5) ends here; its policy is FirstOption.</summary>
    static string WebSmoke5 => "SMOKE OK 5 turns 1948-10-15 " + GoldenManifest.Expected("smoke5");

    static readonly Regex IsoDate = new Regex(@"\d{4}-\d{2}-\d{2}");

    delegate List<Command> Policy(Content c, GameState s, int turn);

    /// <summary>What GameRoot.Smoke does (GoldenRun.SmokeOrders, shared with Unity's golden check).</summary>
    static List<Command> FirstOption(Content c, GameState s, int turn) => GoldenRun.SmokeOrders(c, s);

    /// <summary>The golden runs' player (GoldenRun.Orders): HistoricalAI's orders, then the reference player's GDD orders.</summary>
    static List<Command> Historical(Content c, GameState s, int turn) => GoldenRun.Orders(c, s).ToList();

    /// <summary>A player who uses every UI order: the event card's first options, then (in turn) an appointment, a
    /// budget change, a call-up change, a map build and a security-level change, each only when the sim's own check
    /// says it is legal now.</summary>
    static List<Command> Player(Content c, GameState s, int turn)
    {
        var cmds = FirstOption(c, s, turn);
        if (turn % 8 == 4)
        {
            var pick = Cabinet.Lineup(c, s).SelectMany(seat => Cabinet.Candidates(c, s, seat.Office.Id)
                    .Where(x => x.reason == null).Select(x => (office: seat.Office.Id, person: x.person.Id))).FirstOrDefault();
            if (pick.office != null) cmds.Add(new Appoint(pick.office, pick.person));
        }
        if (turn % 6 == 1)
        {
            // BudgetPresenter: move one line by 5 points and rescale; here the change moves between two lines.
            var keys = s.Budget.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            var (from, to) = turn % 12 == 1 ? (keys[0], keys[keys.Count - 1]) : (keys[keys.Count - 1], keys[0]);
            if (s.Budget[from] >= 0.05)
                cmds.Add(new AllocateBudget(new Dictionary<string, double>(s.Budget) { [from] = s.Budget[from] - 0.05, [to] = s.Budget[to] + 0.05 }));
        }
        if (turn % 10 == 3) cmds.Add(new SetMobilization(Math.Min(1, Math.Round(s.Mobilization + 0.1, 2))));
        if (turn % 10 == 8) cmds.Add(new SetMobilization(SecuritySystem.MinMobilization(c, s.Date, s.Rung)));
        if (turn % 2 == 0)
            foreach (var action in new[] { MapActions.TransitCamp, MapActions.Housing })
            {
                var hex = c.Hexes.FirstOrDefault(h => MapActions.Check(c, s, action, h.Q, h.R) == null);
                if (hex != null) { cmds.Add(new MapAction(action, hex.Q, hex.R)); break; }
            }
        if (turn % 15 == 7 || turn % 15 == 8)   // DefencePresenter: one level up, then back down the next turn
        {
            var delta = SecuritySystem.ClampRung(s.Rung + (turn % 15 == 7 ? 1 : -1)) - s.Rung;
            if (delta != 0) cmds.Add(new SecurityOrder(delta));
        }
        return cmds;
    }

    /// <summary>SimBridge.Queue's own refusal: an order that costs more attention than is left at this turn's prices.
    /// The orders the bridge would accept are returned; the others are reported (the WebGL smoke fails on them).</summary>
    static List<Command> Queue(Content c, GameState s, IEnumerable<Command> wanted, List<string> refusedByQueue)
    {
        var queued = new List<Command>();
        double left = s.Attention;
        foreach (var cmd in wanted)
        {
            var price = AttentionSystem.Cost(c, s, cmd.Kind);
            if (price > left + 1e-9) { refusedByQueue.Add($"{cmd.Kind} needs {price:0.0} attention, {left:0.0} left"); continue; }
            left -= price;
            queued.Add(cmd);
        }
        return queued;
    }

    /// <summary>The text the screens show for the session's current state, as far as the sim produces it. Prose is the
    /// event body text content writes in plain English, where a word such as "rationing" or "housing" is a word, not a
    /// leaked id; every other line is sim wording or a content display string.</summary>
    static IEnumerable<(string text, bool prose)> Shown(Content c, GameSession g)
    {
        var s = g.State;
        if (g.Last != null)
        {
            foreach (var w in g.Last.Why) yield return (w.Message, false);         // WhyPresenter
            foreach (var r in g.Last.Rejected) yield return (r.Reason, false);     // the toast
        }
        foreach (var e in s.PendingEvents.Select(id => c.Events.First(x => x.Id == id)))   // EventCardPresenter
        {
            var date = EventSystem.FillDate(e, s.Date);
            yield return (Cabinet.Fill(c, s, e.Title, date), false);
            yield return (EventSystem.Render(c, s, e), true);
            foreach (var o in e.Options) yield return (Cabinet.Fill(c, s, o.Text, date), false);
            var holder = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office, date);
            if (holder != null) yield return (c.Offices.First(o => o.Id == e.Office).Title + ": " + holder.Name, false);
        }
        // HudModel's sim-formatted values (the strings.en.yaml wrappers around them are Unity-side).
        yield return (PlayerText.Date(s.Date), false);
        yield return (PlayerText.Money(c, "treasury", s.Treasury - s.Debt), false);
        yield return (PlayerText.Money(c, "fx_reserves", s.FxReserves), false);
        foreach (var level in new[] { s.Indicators["security"], s.Indicators["intl_standing"], EffectsSystem.Approval(c, s) })
            yield return (PlayerText.Percent(level), false);
        // CabinetPresenter: every seat, its holder and why each person cannot take it.
        foreach (var seat in Cabinet.Lineup(c, s))
        {
            yield return (seat.Office.Title + ": " + string.Join(", ", seat.Holders.Select(p => p.Name)), false);
            foreach (var reason in Cabinet.Candidates(c, s, seat.Office.Id).Select(x => x.reason?.Text).Where(x => x != null).Distinct())
                yield return (reason!, false);
        }
        // InfoPanelPresenter: why a build is not possible on a hex (a sample of the map).
        foreach (var h in c.Hexes.Where((_, i) => i % 25 == 0))
            foreach (var action in MapActions.Actions)
            {
                var reason = MapActions.Check(c, s, action, h.Q, h.R);
                if (reason != null) yield return (reason, false);
            }
    }

    sealed class Outcome
    {
        public readonly List<string> Hashes = new List<string>();   // after each turn
        public readonly List<string> Failures = new List<string>();
        public readonly List<string> QueueRefusals = new List<string>();
        public readonly HashSet<string> Carried = new HashSet<string>();   // kinds of orders the sim carried out
        public int Orders, Turns;
        public GameState Final = null!;
    }

    /// <summary>Plays from 15 May 1948 until the slice ends (GoldenRun.Until1952), the game ends or <paramref name="maxTurns"/>
    /// pass. With <paramref name="reload"/>, every turn's save is loaded back and play continues from the loaded session.</summary>
    static Outcome Play(Content c, Policy policy, bool reload, Func<IEnumerable<string>, List<string>>? guard, int maxTurns = 1000)
    {
        var o = new Outcome();
        var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        if (guard != null) Check(0, c, guard, Shown(c, g).ToList(), o);
        for (int turn = 0; turn < maxTurns && g.State.Date < GoldenRun.Until1952 && g.State.GameOver == null; turn++)
        {
            var queueRefused = new List<string>();
            var cmds = Queue(c, g.State, policy(c, g.State, turn), queueRefused);
            o.QueueRefusals.AddRange(queueRefused.Select(r => $"turn {turn + 1} ({PlayerText.Date(g.State.Date)}): {r}"));
            o.Orders += cmds.Count;
            var r = g.Step(cmds);
            foreach (var cmd in cmds.Where(x => r.Rejected.All(j => j.Command != x))) o.Carried.Add(cmd.Kind);
            foreach (var x in r.Rejected)
                o.Failures.Add($"turn {turn + 1}: refused {Describe(x.Command)}: {x.Reason}");
            if (guard != null) Check(turn + 1, c, guard, Shown(c, g).ToList(), o);
            if (reload)
            {
                var text = g.Save.Write();
                var loaded = GameSession.Load(c, text, out var warnings);
                if (warnings.Count > 0) o.Failures.Add($"turn {turn + 1}: load warned: {string.Join("; ", warnings)}");
                if (loaded.State.Hash() != g.State.Hash()) o.Failures.Add($"turn {turn + 1}: the loaded game's hash differs");
                if (loaded.Save.Write() != text) o.Failures.Add($"turn {turn + 1}: the loaded game writes a different save");
                if (!loaded.Last!.Why.Select(w => w.Message).SequenceEqual(r.Why.Select(w => w.Message)))
                    o.Failures.Add($"turn {turn + 1}: the loaded game reports different causes for the last turn");
                g = loaded;                                              // play on from the loaded game
            }
            o.Hashes.Add(g.State.Hash());
            o.Turns = turn + 1;
        }
        o.Final = g.State;
        return o;
    }

    /// <summary>Sim wording gets PlayerTextTests' guard (any content id as a whole word outside a display string). Prose
    /// gets the ids that cannot be English words (snake_case, "operation_yoav"). Every line: no ISO date, no unfilled
    /// "{placeholder}".</summary>
    static void Check(int turn, Content c, Func<IEnumerable<string>, List<string>> guard, List<(string text, bool prose)> shown, Outcome o)
    {
        o.Failures.AddRange(guard(shown.Where(x => !x.prose).Select(x => x.text)).Select(l => $"turn {turn}: content id {l}"));
        foreach (var t in shown.Where(x => x.prose).Select(x => x.text).Distinct())
            foreach (var id in SnakeIds(c).Where(id => Regex.IsMatch(t, $@"\b{Regex.Escape(id)}\b")))
                o.Failures.Add($"turn {turn}: content id '{id}' in: {t}");
        foreach (var t in shown.Select(x => x.text).Distinct())
        {
            if (IsoDate.IsMatch(t)) o.Failures.Add($"turn {turn}: ISO date in: {t}");
            if (t.Contains("{") || t.Contains("}")) o.Failures.Add($"turn {turn}: unfilled placeholder in: {t}");
        }
    }

    /// <summary>Every content id with an underscore: these never read as English.</summary>
    static List<string> SnakeIds(Content c) => c.Indicators.Select(x => x.Id).Concat(c.Policies.Select(x => x.Id))
        .Concat(c.Effects.Select(x => x.Id)).Concat(c.Situations.Select(x => x.Id)).Concat(c.Money.Select(x => x.Id))
        .Concat(c.Regions.Select(x => x.Id)).Concat(c.Offices.Select(x => x.Id)).Concat(c.Events.Select(x => x.Id))
        .Concat(c.Events.SelectMany(e => e.Options.Select(x => x.Id))).Concat(c.People.Select(x => x.Id))
        .Concat(c.Parties.Select(x => x.Id)).Concat(c.Governments.Select(x => x.Id)).Concat(c.Rules.MapActions.Keys)
        .Where(id => id.Contains("_")).Distinct().ToList();

    static string Describe(Command cmd) => cmd switch
    {
        ChooseEventOption e => $"choose {e.EventId}/{e.OptionId}",
        Appoint a => $"appoint {a.Person} as {a.Office}",
        MapAction m => $"{m.Action} at ({m.Q}, {m.R})",
        SetMobilization m => $"call-up {m.Level}",
        SecurityOrder x => $"security {x.Delta:+0;-0}",
        _ => cmd.Kind,
    };

    static void AssertClean(string name, Outcome run, Outcome straight)
    {
        Assert.True(run.Failures.Count == 0, $"{name}: {run.Failures.Count} invariant failures:\n" + string.Join("\n", run.Failures.Take(60)));
        Assert.True(run.Final.Date >= GoldenRun.Until1952, $"{name}: the run stopped on {PlayerText.Date(run.Final.Date)} (game over: {run.Final.GameOver})");
        Assert.True(run.Final.GameOver == null || run.Final.GameOver == GameOverCodes.MvpEnded, $"{name}: game over: {run.Final.GameOver}");   // only the game's own end (R-LOOP-004)
        Assert.Equal(straight.Hashes, run.Hashes);                      // reloading every turn changes nothing
    }

    [Fact]
    public void TheFirstOptionSmokeRunPlaysTheWholeSliceCleanly()
    {
        var clock = Stopwatch.StartNew();
        var run = Play(Real, FirstOption, reload: true, PlayerTextTests.LeakGuard(Real));
        var straight = Play(Real, FirstOption, reload: false, guard: null);
        AssertClean("first option", run, straight);
        Assert.True(run.QueueRefusals.Count == 0, "the UI would refuse an event answer (the WebGL smoke fails):\n" + string.Join("\n", run.QueueRefusals));
        Assert.True(run.Orders > 0);
        // The same policy over five turns ends where the WebGL player's ?smoke=5 run ends (docs/determinism.md).
        var five = Play(Real, FirstOption, reload: false, guard: null, maxTurns: 5).Final;
        var expected = WebSmoke5;   // read before an update rewrites the manifest, so a moved value still fails this run
        if (GoldenManifest.UpdateRequested) GoldenManifest.Update("smoke5", five.Hash());
        Assert.Equal(expected, FormattableString.Invariant($"SMOKE OK 5 turns {five.Date:yyyy-MM-dd} {five.Hash()}"));
        // The library's run (Unity's golden check, #394) ends on the same state as the session run above.
        Assert.Equal(five.Hash(), GoldenRun.PlaySmoke(Real, 5).Hash());
        Assert.True(clock.Elapsed < TimeSpan.FromMinutes(1), $"took {clock.Elapsed}");
    }

    [Fact]
    public void TheHistoricalRunPlaysTheWholeSliceCleanlyAndEndsOnTheGolden()
    {
        var clock = Stopwatch.StartNew();
        var run = Play(Real, Historical, reload: true, PlayerTextTests.LeakGuard(Real));
        AssertClean("historical", run, Play(Real, Historical, reload: false, guard: null));
        Assert.True(run.QueueRefusals.Count == 0, "the reference player sent an order the UI would refuse:\n" + string.Join("\n", run.QueueRefusals));
        Assert.Equal(GoldenRun.Hash1952, run.Final.Hash());
        Assert.True(clock.Elapsed < TimeSpan.FromMinutes(1), $"took {clock.Elapsed}");
    }

    [Fact]
    public void APlayerUsingEveryUiOrderPlaysTheWholeSliceCleanly()
    {
        var clock = Stopwatch.StartNew();
        var run = Play(Real, Player, reload: true, PlayerTextTests.LeakGuard(Real));
        AssertClean("player", run, Play(Real, Player, reload: false, guard: null));
        // Attention runs out some turns: the UI refuses those extra orders at queue time, which is not a sim refusal.
        Assert.True(run.Orders > run.Turns, $"{run.Orders} orders in {run.Turns} turns");
        var uiKinds = new[] { "choose_event_option", "appoint", "allocate_budget", "set_mobilization", "map_action", "security_order" };
        Assert.Empty(uiKinds.Except(run.Carried));                     // every order a UI screen can queue was carried out
        Assert.True(clock.Elapsed < TimeSpan.FromMinutes(1), $"took {clock.Elapsed}");
    }
}
