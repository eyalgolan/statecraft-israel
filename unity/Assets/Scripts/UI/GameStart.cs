#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Starting a session: continue the autosave when there is one (replayed to the same state hash), else a new
    /// Historical game. Anything the player should know (a content-change warning, an unreadable save) goes to notes.
    /// No failure while reading or replaying the autosave stops the game from starting: the file is set aside.
    /// <paramref name="fresh"/> ignores the autosave (neither read nor set aside): GameRoot's smoke run uses it, so a smoke
    /// session never touches the player's save, and tests use it. New Game goes through SimBridge.Replace instead.</summary>
    public static class GameStart
    {
        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, UiText text, bool fresh = false) =>
            Begin(c, store, newSeed, notes, text, out _, fresh);

        /// <summary>Begin, also saying whether the session continues the autosave (<paramref name="resumed"/>): false for a
        /// new game, whether there was no save, the save was set aside, or <paramref name="fresh"/> ignored it. The start
        /// screen offers Continue only when it is true.</summary>
        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, UiText text, out bool resumed,
            bool fresh = false)
        {
            resumed = false;
            if (!fresh)
            {
                try
                {
                    var saved = store.ReadAutosave();
                    if (saved == null) return GameSession.New(c, newSeed(), Mode.Historical);
                    var loaded = GameSession.Load(c, saved, out var warnings);
                    notes.AddRange(warnings);
                    resumed = true;
                    return loaded;
                }
                catch (Exception e)   // any failure (a bad save, a replay that throws): never block the start
                {
                    try { store.SetAsideAutosave(); } catch (Exception) { /* even this must not stop the start */ }
                    // The sim's reasons are English and technical (line numbers, a replay's own exception), and anything else
                    // can carry file paths: the full exception (with a replay's inner exception) goes to the log only (#631).
                    UnityEngine.Debug.LogWarning((e is SaveException ? "Autosave rejected: " : "Autosave could not be loaded: ") + e);
                    // A refusal with a code (a save from before the GDD cutover, or from a newer version: R-SAVE-001, R-SAVE-002)
                    // is said once, in the player's language (N4); anything else, and a code with no string, gets one short
                    // localised reason (#631): this path never throws, so the start is never blocked.
                    if (e is SaveException refused && refused.Code != null && text.Has("save.refused." + refused.Code))
                        notes.Add(text["save.refused." + refused.Code]);
                    else notes.Add(text.Format("save.unreadable", text["save.reason_other"]));
                }
            }
            return GameSession.New(c, newSeed(), Mode.Historical);
        }

        /// <summary>Writes the save after every turn. A write that fails (a full or read-only disk) never stops the game:
        /// the reason goes to <paramref name="onError"/>. Dispose the result to stop autosaving.</summary>
        public static IDisposable Autosave(SimBridge bridge, SaveStore store, UiText text, Action<string>? onError = null)
        {
            void Save(IReadOnlyList<Rejection> _)
            {
                try { store.WriteAutosave(bridge.Session.Save.Write()); }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("Autosave could not be written: " + e);
                    onError?.Invoke(text["save.write_failed"]);
                }
            }
            bridge.TurnEnded += Save;
            return new Unsubscribe(() => bridge.TurnEnded -= Save);
        }

        sealed class Unsubscribe : IDisposable
        {
            readonly Action undo;
            public Unsubscribe(Action undo) { this.undo = undo; }
            public void Dispose() => undo();
        }
    }
}
