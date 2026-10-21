#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Opens a web page outside the game (#1168: the "Star on GitHub" buttons in Settings and on the start screen).
    /// On WebGL a browser lets a page open a new tab only inside the player's own click or key event, and Unity handles
    /// input on the next frame, outside it, so Application.OpenURL there can be popup-blocked. ExternalLink.jslib instead
    /// arms a one-shot listener for the press's release (pointerup on #unity-canvas, or keyup), which calls window.open
    /// inside that real gesture. That is why Bind opens on the press (pointer down, or the keyboard's Submit, which comes on
    /// key down) and not on `clicked`, which comes on the release, too late to arm anything; so a press dragged off the
    /// button still opens the page. In the editor and in desktop builds Open falls back to Application.OpenURL.</summary>
    public static class ExternalLink
    {
        /// <summary>The game's repository, which the Star buttons open.</summary>
        public const string Repository = "https://github.com/eyalgolan/statecraft-israel";

        /// <summary>What Open does with the URL. Tests put a recorder here (and put Default back), so no browser opens.</summary>
        public static Action<string> Opener = Default;

        public static void Open(string url) => Opener(url);

        /// <summary>Opens <paramref name="url"/> when the button is pressed with the primary pointer or the keyboard's Submit.
        /// The returned action unwires it (a presenter's Dispose).</summary>
        public static Action Bind(Button button, string url)
        {
            EventCallback<PointerDownEvent> onPointer = e => { if (e.button == 0) Open(url); };
            EventCallback<NavigationSubmitEvent> onSubmit = _ => Open(url);
            // TrickleDown: runs before the button's Clickable, whatever it does with the event.
            button.RegisterCallback(onPointer, TrickleDown.TrickleDown);
            button.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
            return () =>
            {
                button.UnregisterCallback(onPointer, TrickleDown.TrickleDown);
                button.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
            };
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftOpenOnRelease(string url);
        public static void Default(string url) => StatecraftOpenOnRelease(url);
#else
        public static void Default(string url) => Application.OpenURL(url);
#endif
    }
}
