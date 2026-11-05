using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One playthrough: the current state, the turn before it, the last step's report, and the save that
    /// records every turn. The Unity SimBridge wraps this; the CLI and tests can use it directly.</summary>
    public sealed class GameSession
    {
        public Content Content { get; }
        public GameState State { get; private set; }
        /// <summary>The state before the last step (null before the first), for "change since last turn".</summary>
        public GameState? Previous { get; private set; }
        /// <summary>The last step's result (null before the first): its Why and Rejected lists.</summary>
        public StepResult? Last { get; private set; }
        /// <summary>The step before Last (null before the second step), for a change that compares two steps' results (the
        /// top bar's arrivals). A loaded session has it from its replay, like Previous and Last. Not part of the state or
        /// the save.</summary>
        public StepResult? BeforeLast { get; private set; }
        public SaveGame Save { get; }
        readonly List<Command> committed = new List<Command>();
        /// <summary>R-TURN-004: the turn's committed orders, in the order given: final, shown at once, run first at End turn.</summary>
        public IReadOnlyList<Command> Committed => committed;
        /// <summary>The state the screen shows: the turn's start after its committed orders (Simulation.Preview).</summary>
        public PreviewResult Now { get; private set; }

        GameSession(Content c, GameState s, SaveGame save)
        {
            Content = c; State = s; Save = save;
            Now = Simulation.Preview(c, s, Array.Empty<Command>());
        }

        public static GameSession New(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, seed, mode);
            return new GameSession(c, s, new SaveGame(c.Hash, seed, mode, c.Ruleset, s.Hash()));
        }

        /// <summary>R-TURN-004: commits a move or a unit order. Null when taken (it is shown at once and saved in the open
        /// turn, R-SAVE-003), else the sim's reason, with nothing changed. A deferred order is a programmer error.</summary>
        public string? Commit(Command cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (!Simulation.IsInstant(cmd)) throw new ArgumentException($"'{cmd.Kind}' waits for End turn; only a move or a unit order other than Send home is committed.", nameof(cmd));
            var next = Simulation.Preview(Content, State, committed.Concat(new[] { cmd }).ToList());
            var refused = next.Rejected.FirstOrDefault(r => ReferenceEquals(r.Command, cmd));
            if (refused != null) return refused.Reason;
            committed.Add(cmd);
            Now = next;
            Save.SetOpen(committed);
            return null;
        }

        /// <summary>Ends the turn: the committed orders, then <paramref name="deferred"/>, as one batch (R-TURN-004).</summary>
        public StepResult EndTurn(IReadOnlyList<Command> deferred)
        {
            if (deferred == null) throw new ArgumentNullException(nameof(deferred));
            var batch = committed.Concat(deferred).ToList();
            var r = Simulation.Step(Content, State, batch);
            Save.Record(batch, r.State);
            Previous = State; State = r.State; BeforeLast = Last; Last = r;
            committed.Clear();
            Now = Simulation.Preview(Content, State, committed);
            return r;
        }

        /// <summary>Runs one turn with these commands and records it (no orders may be committed).</summary>
        public StepResult Step(IReadOnlyList<Command> commands)
        {
            if (committed.Count > 0) throw new InvalidOperationException("Orders are committed this turn: end it with EndTurn.");
            return EndTurn(commands);
        }

        /// <summary>Load warning: the save was made with other content, so the replayed game may differ (spec section 7).</summary>
        public const string ContentChanged = "content_changed";
        /// <summary>Load warning: made with other content, the replay then ended on a different state than the saved game.</summary>
        public const string ReplayDiffers = "replay_differs";

        /// <summary>Parses and replays a save. Throws SaveException when the text is malformed, or when the content is the
        /// same but the replay ends on a different hash (a determinism bug). Content made with other content loads with a
        /// warning (spec section 7), and a second warning if the replay then differs from the saved game. The warnings are
        /// kinds (<see cref="ContentChanged"/>, <see cref="ReplayDiffers"/>), not display text: the UI says each in the
        /// player's language, as it does SaveException.Code (#637). A finished game whose replay differs only by the ending
        /// turn's objective cards (#2536) is not a difference: it loads with no warning. A save older than schema 5 was played
        /// before the soft-event cap kept a left-out event waiting (#2605), so with the same content a different replay is
        /// the rules' change, not damage: it loads with <see cref="ReplayDiffers"/>.</summary>
        public static GameSession Load(Content c, string saveText, out IReadOnlyList<string> warnings)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            // R-SAVE-001/002: the parser enforces the content's ruleset before any replay.
            var saved = SaveGame.Parse(saveText, c.Ruleset);
            // R-SAVE-004: schema 2 stored player attacks as standing orders, which the current rules no longer replay alike.
            if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset
                && saved.Turns.Any(t => t.Any(x => x is UnitOrder o && o.Order == Combat.Attack)))
                throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);
            var list = new List<string>();
            bool sameContent = saved.ContentHash == c.Hash;
            if (!sameContent) list.Add(ContentChanged);
            // #2605: only a current-schema save with the same content must replay exactly; an older one may differ.
            bool mustMatch = sameContent && saved.Version >= SaveGame.SchemaVersion;
            var session = New(c, saved.Seed, saved.Mode);
            int turnNo = 0;
            try { foreach (var turn in saved.Turns) { session.Step(turn); turnNo++; } }
            catch (SaveException) { throw; }
            catch (Exception e) { throw new SaveException(FormattableString.Invariant($"turn {turnNo + 1}: {e.Message}"), e); }
            // #2536: a game finished before the ending turn fired objective cards replays with those cards added, and only
            // them: the same game, so it loads as saved, with no warning, and shows its ending card(s).
            // Only an older save can be from before #2536: a current-schema save always has its ending cards (#2605 review).
            // #2542: a save made before stayed losses left a marker replays with only those markers added. It is the same
            // game, so accept that exact same-content difference; every other replay mismatch keeps the existing handling.
            // Only an older save can be from before the markers: a current-schema save always has them, so one without them
            // was edited (Codex review of 47da9b207).
            if (session.State.Hash() != saved.FinalHash && !(saved.Version < SaveGame.SchemaVersion && OnlyEndingCardsAdded(c, session.State, saved.FinalHash))
                && !(saved.Version < SaveGame.SchemaVersion && sameContent && OnlyLostMarkersAdded(session.State, saved.FinalHash)))
            {
                if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset)
                    throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);
                if (mustMatch) throw new SaveException(0, "The replay does not match the saved game: the save is damaged or was edited, or the game has a bug.");
                list.Add(ReplayDiffers);
            }
            foreach (var cmd in saved.Open)
            {
                var reason = session.Commit(cmd);
                if (reason == null) continue;
                if (mustMatch) throw new SaveException(0, $"The open turn's order '{cmd.Kind}' was refused: {reason}");
                if (!list.Contains(ReplayDiffers)) list.Add(ReplayDiffers);
                break;
            }
            warnings = list;
            return session;
        }

        /// <summary>#2536: whether <paramref name="s"/>, a replayed game, differs from the saved final state only by the ending
        /// turn's objective cards (EventSystem.EndingCard), which games finished before that change never fired. Before it,
        /// a finished game had nothing pending (Delegate resolves every card before a turn, and an ended game raised no
        /// event), so taking exactly the pending cards out of both the fired and the pending lists must give the saved hash.
        /// Narrow by design: the game is over, every pending event is an ending card, and the hash matches exactly.</summary>
        static bool OnlyEndingCardsAdded(Content c, GameState s, string savedHash)
        {
            if (s.GameOver == null || s.PendingEvents.Count == 0) return false;
            if (!s.PendingEvents.All(id => c.Events.FirstOrDefault(e => e.Id == id) is GameEvent e && EventSystem.EndingCard(s, e)))
                return false;
            var before = s.Clone();
            foreach (var id in s.PendingEvents) before.FiredEvents.Remove(id);
            before.PendingEvents.Clear();
            return before.Hash() == savedHash;
        }

        /// <summary>#2542: whether a replay differs from its saved final state only by ownerless, inhabited, stayed markers
        /// for places captured and later lost. Before this change those records were removed. Narrow by design: remove every
        /// record having exactly the new lost-marker shape, then require the saved hash to match.</summary>
        static bool OnlyLostMarkersAdded(GameState s, string savedHash)
        {
            var ids = s.Settlements.Where(kv => kv.Value.Owner == "" && kv.Value.State == ""
                && kv.Value.Since != default && !kv.Value.Depopulated && !kv.Value.Fled
                && kv.Value.Combatants == 0 && kv.Value.Buildings == "" && kv.Value.Turns == 0)
                .Select(kv => kv.Key).ToList();
            if (ids.Count == 0) return false;
            var before = s.Clone();
            foreach (var id in ids) before.Settlements.Remove(id);
            return before.Hash() == savedHash;
        }
    }
}
