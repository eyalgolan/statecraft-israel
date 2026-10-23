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
        /// player's language, as it does SaveException.Code (#637).</summary>
        public static GameSession Load(Content c, string saveText, out IReadOnlyList<string> warnings)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var saved = SaveGame.Parse(saveText);
            // R-SAVE-001: a save replays only under the rules it was played with (spec 2026-10-12 section 2.4, decision D-E4).
            // The refusal carries a code the UI translates; its own message tells the player a new game started (N4).
            if (saved.Ruleset != c.Ruleset)
                throw saved.Ruleset == SaveGame.LegacyRuleset
                    ? new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules)
                    : new SaveException(0, "This save was made with a newer version of the game's rules.", SaveException.NewerVersion);
            // R-SAVE-004: schema 2 stored player attacks as standing orders, which the current rules no longer replay alike.
            if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset
                && saved.Turns.Any(t => t.Any(x => x is UnitOrder o && o.Order == Combat.Attack)))
                throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);
            var list = new List<string>();
            bool sameContent = saved.ContentHash == c.Hash;
            if (!sameContent) list.Add(ContentChanged);
            var session = New(c, saved.Seed, saved.Mode);
            int turnNo = 0;
            try { foreach (var turn in saved.Turns) { session.Step(turn); turnNo++; } }
            catch (SaveException) { throw; }
            catch (Exception e) { throw new SaveException(FormattableString.Invariant($"turn {turnNo + 1}: {e.Message}"), e); }
            if (session.State.Hash() != saved.FinalHash)
            {
                if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset)
                    throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);
                if (sameContent) throw new SaveException(0, "The replay does not match the saved game: the save is damaged or was edited, or the game has a bug.");
                list.Add(ReplayDiffers);
            }
            foreach (var cmd in saved.Open)
            {
                var reason = session.Commit(cmd);
                if (reason == null) continue;
                if (sameContent) throw new SaveException(0, $"The open turn's order '{cmd.Kind}' was refused: {reason}");
                if (!list.Contains(ReplayDiffers)) list.Add(ReplayDiffers);
                break;
            }
            warnings = list;
            return session;
        }
    }
}
