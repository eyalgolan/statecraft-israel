#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>New game's raid flight (#3000, PR 2 of #2968): after the opening and before the raid card, the Egyptian Dakotas
    /// fly in over Tel Aviv, bomb it and fly away (Map.Collection.DakotaRaidFlight), with the HUD hidden as under the card, and
    /// then the card shows. Any click on the map, or Escape, skips the rest of the flight; the card still follows. No bridge,
    /// commands, RNG or save access. GameRoot plays it only where it plays the card, and never in the automated runs.</summary>
    public sealed class RaidFlightPresenter : IDisposable
    {
        public const string OpenClass = "raid-flight-open";

        readonly VisualElement root, layer;
        readonly Func<Action, Action?> fly;
        readonly Action<bool> shownChanged;
        readonly Action finished;
        readonly EventCallback<PointerDownEvent> onPointer;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        Action? stop;
        int run;
        bool disposed;
        int closedFrame = -1;

        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        /// <summary>The click that skips the flight must not also reach the map underneath (GameRoot's overUi).</summary>
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;

        /// <param name="fly">Starts the flight and is told when it ends of itself; returns how to stop it, or null when there
        /// is nothing to fly (no Dakota in the scene): then the card follows at once.</param>
        /// <param name="shownChanged">Told when the flight covers the HUD and when it no longer does.</param>
        /// <param name="finished">Called once the flight has ended or been skipped (not on Dispose): GameRoot shows the card.</param>
        public RaidFlightPresenter(VisualElement root, Func<Action, Action?> fly, Action<bool> shownChanged, Action finished)
        {
            this.root = root.Q("root") ?? root; this.fly = fly; this.shownChanged = shownChanged; this.finished = finished;
            layer = root.Q("raid-flight-layer");
            onPointer = e => { if (IsOpen) { Close(finish: true); e.StopPropagation(); } };
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            layer.RegisterCallback(onPointer);
            layer.RegisterCallback(onCancel);
            layer.style.display = DisplayStyle.None;
        }

        /// <summary>Flies the raid; nothing while it flies (the start screen's close and New game's Started can both report one
        /// new game) or after Dispose. With nothing to fly, it finishes at once.</summary>
        public void Show()
        {
            if (disposed || IsOpen) return;
            int mine = ++run;
            IsOpen = true;
            root.AddToClassList(OpenClass); layer.style.display = DisplayStyle.Flex;
            ScreenNav.DisableCovered(root);
            shownChanged(true);
            stop = fly(() => { if (mine == run) Close(finish: true); });
            if (stop == null && IsOpen && mine == run) Close(finish: true);
            // The layer takes the keyboard focus, so Escape reaches it as a NavigationCancelEvent.
            if (IsOpen) { layer.focusable = true; layer.Focus(); }
        }

        /// <summary>Skips the rest of the flight (Escape); false when it is not flying.</summary>
        public bool Escape()
        {
            if (!IsOpen) return false;
            Close(finish: true);
            return true;
        }

        void Close(bool finish)
        {
            if (!IsOpen) return;
            IsOpen = false; closedFrame = Time.frameCount; run++;
            var halt = stop; stop = null;
            halt?.Invoke();
            layer.style.display = DisplayStyle.None; layer.focusable = false; root.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(root); shownChanged(false);
            if (finish) finished();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Close(finish: false);
            layer.UnregisterCallback(onPointer);
            layer.UnregisterCallback(onCancel);
        }
    }
}
