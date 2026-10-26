#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Opens a web page outside the game (#1168: the "Star on GitHub" buttons in Settings and on the start screen).
    /// On WebGL a browser lets a page open a new tab only inside the player's own click or key event, and Unity handles
    /// input on the next frame, outside it, so Application.OpenURL there can be popup-blocked. ExternalLink.jslib instead
    /// arms a one-shot listener on the press, which calls window.open on the release inside that real gesture, and only
    /// when the pointer is released over the same button (playtest B10: a press dragged off it cancels, as on any button).
    /// In the editor and in desktop builds the page opens on `clicked`, which Clickable gives only for a completed click.</summary>
    public static class ExternalLink
    {
        /// <summary>The game's repository, which the Star buttons open.</summary>
        public const string Repository = "https://github.com/eyalgolan/statecraft-israel";

        /// <summary>What Open does with the URL. Tests put a recorder here (and put Default back), so no browser opens.</summary>
        public static Action<string> Opener = Default;
        public static void Open(string url) => Opener(url);

        /// <summary>Pointer-down only arms WebGL. Native/editor and test recorders open on clicked, after Clickable has
        /// accepted the release. Keyboard Submit arms its own Enter/Space key-up on WebGL. Dispose cancels a pending arm.</summary>
        public static Action Bind(Button button, string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int ticket = 0;
#endif
            void Cancel()
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (Opener == (Action<string>)Default && ticket != 0) StatecraftCancelExternalLink(ticket);
#endif
            }
            void Arm(bool keyboard)
            {
                if (!button.enabledInHierarchy) return;
#if UNITY_WEBGL && !UNITY_EDITOR
                if (Opener != (Action<string>)Default) return;
                var frame = button.panel?.visualTree.worldBound ?? default;
                if (!(frame.width > 0f && frame.height > 0f)) return;
                var r = button.worldBound;
                ticket = StatecraftOpenOnRelease(url, (r.xMin - frame.xMin) / frame.width, (r.yMin - frame.yMin) / frame.height,
                    (r.xMax - frame.xMin) / frame.width, (r.yMax - frame.yMin) / frame.height, keyboard ? 1 : 0);
#endif
            }
            EventCallback<PointerDownEvent> onPointer = e => { if (e.button == 0) Arm(false); };
            EventCallback<NavigationSubmitEvent> onSubmit = _ => Arm(true);
            EventCallback<PointerCancelEvent> onCancel = _ => Cancel();
            EventCallback<FocusOutEvent> onBlur = _ => Cancel();
            EventCallback<GeometryChangedEvent> onGeometry = _ => Cancel();
            Action onClick = () =>
            {
                if (!button.enabledInHierarchy) return;
#if UNITY_WEBGL && !UNITY_EDITOR
                if (Opener == (Action<string>)Default) return;   // the real browser release already handled this activation
#endif
                Open(url);
            };
            button.RegisterCallback(onPointer, TrickleDown.TrickleDown);
            button.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
            button.RegisterCallback(onCancel); button.RegisterCallback(onBlur); button.RegisterCallback(onGeometry);
            button.clicked += onClick;
            return () =>
            {
                Cancel();
                button.UnregisterCallback(onPointer, TrickleDown.TrickleDown);
                button.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
                button.UnregisterCallback(onCancel); button.UnregisterCallback(onBlur); button.UnregisterCallback(onGeometry);
                button.clicked -= onClick;
            };
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int StatecraftOpenOnRelease(string url, float left, float top, float right, float bottom, int keyboard);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftCancelExternalLink(int ticket);
        // A caller without a button can only request a keyboard activation, never the next unrelated pointer release.
        public static void Default(string url) { StatecraftOpenOnRelease(url, 0f, 0f, 1f, 1f, 1); }
#else
        public static void Default(string url) => Application.OpenURL(url);
#endif
    }
}
