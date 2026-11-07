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
        static long saveGeneration;

        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        /// <summary>Renders a language-independent save-notice key in the current UI language.</summary>
        internal static string NoticeText(UiText text, string key) =>
            key == "save.unreadable"
                ? text.Format(key, text["save.reason_other"])
                : text[key];

        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, UiText text, bool fresh = false) =>
            Begin(c, store, newSeed, notes, text, out _, fresh);

        /// <summary>Begin, also saying whether the session continues the autosave (<paramref name="resumed"/>): false for a
        /// new game, whether there was no save, the save was set aside, or <paramref name="fresh"/> ignored it. The start
        /// screen offers Continue only when it is true.</summary>
        public static GameSession Begin(Content c, SaveStore store, Func<ulong> newSeed, List<string> notes, UiText text, out bool resumed,
            bool fresh = false, List<string>? noticeKeys = null)
        {
            void Note(string key)
            {
                noticeKeys?.Add(key);
                notes.Add(NoticeText(text, key));
            }

            resumed = false;
            if (!fresh)
            {
                try
                {
                    var saved = store.ReadAutosave();
                    if (saved == null) return GameSession.New(c, newSeed(), Mode.Historical);
                    var loaded = GameSession.Load(c, saved, out var warnings);
                    // The sim returns warning kinds, not text (#637): each is said in the player's language, the kind goes
                    // to the log. A kind with no string is only logged, so a new kind never sets a loadable save aside.
                    foreach (var kind in warnings)
                    {
                        UnityEngine.Debug.LogWarning("Autosave loaded with a warning: " + kind);
                        var key = "save.warning." + kind;
                        if (text.Has(key)) Note(key);
                    }
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
                    var refusedKey = e is SaveException refused && refused.Code != null
                        ? "save.refused." + refused.Code
                        : null;
                    if (refusedKey != null && text.Has(refusedKey))
                        Note(refusedKey);
                    else
                        Note("save.unreadable");
                }
            }
            return GameSession.New(c, newSeed(), Mode.Historical);
        }

        /// <summary>Writes <paramref name="session"/> through the complete autosave path. A failure never stops play: the
        /// reason goes to <paramref name="onError"/> and false is returned. On WebGL <paramref name="persistenceTarget"/>
        /// receives the asynchronous IndexedDB result; elsewhere the write is the save, and <paramref name="onSaved"/> is told.</summary>
        internal static bool WriteAutosave(GameSession session, SaveStore store, UiText text, Action<string>? onError = null,
            string? persistenceTarget = null, Action? onSaved = null)
        {
            try
            {
                store.WriteAutosave(session.Save.Write());
                // Capture this save, not a bridge whose session may be replaced before an asynchronous commit completes.
                var state = session.State;
                var generation = System.Threading.Interlocked.Increment(ref saveGeneration);
                var receipt = FormattableString.Invariant($"{generation} {state.Date:yyyy-MM-dd} turn {state.Turn} hash {state.Hash()}");
                UnityEngine.Debug.Log("AUTOSAVE PENDING " + receipt);
#if UNITY_WEBGL && !UNITY_EDITOR
                StatecraftPersistAutosave("save " + receipt, persistenceTarget ?? "");
#else
                UnityEngine.Debug.Log("AUTOSAVE OK " + receipt);
                onSaved?.Invoke();
#endif
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Autosave could not be written: " + e);
                onError?.Invoke(text["save.write_failed"]);
                return false;
            }
        }

        /// <summary>Writes the save after every committed order and every turn. Dispose the result to stop autosaving.</summary>
        public static IDisposable Autosave(SimBridge bridge, SaveStore store, UiText text, Action<string>? onError = null,
            string? persistenceTarget = null, Action? onSaved = null)
        {
            void Save(IReadOnlyList<Rejection> _) => WriteAutosave(bridge.Session, store, text, onError, persistenceTarget, onSaved);
            Action<IReadOnlyList<Cause>> onCommit = _ => Save(Array.Empty<Rejection>());
            bridge.Committed += onCommit;
            bridge.TurnEnded += Save;
            return new Unsubscribe(() => { bridge.Committed -= onCommit; bridge.TurnEnded -= Save; });
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void StatecraftPersistAutosave(string request, string target);
#endif

        sealed class Unsubscribe : IDisposable
        {
            readonly Action undo;
            public Unsubscribe(Action undo) { this.undo = undo; }
            public void Dispose() => undo();
        }
    }
}
