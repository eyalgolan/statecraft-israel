#nullable enable
using System;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>New Game asks for a second press (no browser confirm dialogs in WebGL), then deletes the autosave and
    /// starts a new Historical game. Ending a turn in between cancels the request. The start screen's New Game goes through
    /// the same two presses (Press), so neither button can wipe the save with one stray press (#175, B6).</summary>
    public sealed class NewGamePresenter : IDisposable
    {
        readonly VisualElement root; readonly SimBridge bridge; readonly SaveStore store; readonly ToastPresenter toast; readonly UiText text; readonly Func<ulong> seed;

        public NewGamePresenter(VisualElement root, SimBridge bridge, SaveStore store, ToastPresenter toast, UiText text, Func<ulong> seed,
            bool slice = false)
        {
            this.slice = slice;
            this.root = root; this.bridge = bridge; this.store = store; this.toast = toast; this.text = text; this.seed = seed;
            button = root.Q<Button>("new-game");
            button.clicked += Press;
            bridge.Changed += Disarm;
        }

        readonly Button button;
        /// <summary>A slice game (?mvp=1): New Game never deletes the player's autosave. GameRoot hides the button and builds
        /// no start screen in a slice game, but builds this presenter with slice: true, so this flag is the guard that stops
        /// the delete.</summary>
        readonly bool slice;

        // Any change (an order, a turn, a new session) means the prompt no longer describes what is on screen.
        void Disarm() => Armed = false;

        /// <summary>Unsubscribes from the bridge and the button (the owner calls this when disabled).</summary>
        public void Dispose() { button.clicked -= Press; bridge.Changed -= Disarm; }

        public bool Armed { get; private set; }

        /// <summary>Raised after the second press has started the new game.</summary>
        public event Action? Started;

        /// <summary>Withdraws an open request (the start screen's Continue and Escape): disarms and takes the prompt off the
        /// toast. Nothing happens when no request is open.</summary>
        public void Cancel()
        {
            if (!Armed) return;
            Armed = false;
            toast.Clear();
        }

        /// <summary>One press of New Game: the first arms and shows the prompt, the second starts the new game.</summary>
        public void Press()
        {
            if (slice) return;
            if (!Armed) { Armed = true; toast.Show(text["newgame.confirm"]); return; }
            Armed = false;
            bool deleted = true;
            // An old autosave that cannot be deleted is overwritten by the next turn's autosave anyway, so the new game still starts.
            try { store.DeleteAutosave(); } catch (Exception e) { deleted = false; UnityEngine.Debug.LogWarning("Autosave could not be deleted: " + e); }
            bridge.Replace(GameSession.New(bridge.Content, seed(), Mode.Historical));
            if (deleted) toast.Clear(); else toast.Show(text["newgame.delete_failed"]);
            Started?.Invoke();
        }
    }
}
