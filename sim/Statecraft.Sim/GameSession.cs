using System;
using System.Collections.Generic;

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

        GameSession(Content c, GameState s, SaveGame save) { Content = c; State = s; Save = save; }

        public static GameSession New(Content c, ulong seed, Mode mode)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var s = Simulation.NewGame(c, seed, mode);
            return new GameSession(c, s, new SaveGame(c.Hash, seed, mode, c.Ruleset, s.Hash()));
        }

        /// <summary>Runs one turn and records it in the save.</summary>
        public StepResult Step(IReadOnlyList<Command> commands)
        {
            var r = Simulation.Step(Content, State, commands);
            Save.Record(commands, r.State);
            Previous = State; State = r.State; BeforeLast = Last; Last = r;
            return r;
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
                if (sameContent) throw new SaveException(0, "The replay does not match the saved game: the save is damaged or was edited, or the game has a bug.");
                list.Add(ReplayDiffers);
            }
            warnings = list;
            return session;
        }
    }
}
