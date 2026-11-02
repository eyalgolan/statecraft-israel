#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Quit (#2153): on the start screen ("start-quit") and in Settings ("settings-quit") for the desktop players
    /// (Steam, the GitHub archives), where a full-screen player otherwise has only Alt+F4 and a Steam Deck has no way out.
    /// Hidden in the browser, where a page cannot close itself (and on any other platform, which the game does not ship to).</summary>
    public static class QuitButton
    {
        /// <summary>The platform the presenters bind for. Tests put their own here (and put <see cref="DefaultPlatform"/> back).</summary>
        internal static Func<RuntimePlatform> Platform = DefaultPlatform;
        /// <summary>What a press does in the presenters, read at the press. Tests put a counter here (and put
        /// <see cref="DefaultQuit"/> back), so no test ever quits.</summary>
        internal static Action Quitter = DefaultQuit;
        internal static RuntimePlatform DefaultPlatform() => Application.platform;
        internal static void DefaultQuit() => Application.Quit();

        /// <summary>Whether Quit shows on <paramref name="p"/>: the Windows, macOS and Linux players and their editors.</summary>
        public static bool Shown(RuntimePlatform p) => p switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer => true,
            RuntimePlatform.WindowsEditor or RuntimePlatform.OSXEditor or RuntimePlatform.LinuxEditor => true,
            _ => false,
        };

        /// <summary>Shows <paramref name="b"/> (or <paramref name="row"/>, the row it alone fills) only where
        /// <see cref="Shown"/>, and calls <paramref name="quit"/> when it is pressed. The start screen binds through
        /// <see cref="BindInGame"/>; Settings binds its own press, which asks first (#2188). Returns the unbinding, for
        /// the presenter's Dispose (a language switch builds the screens again over the same buttons).</summary>
        public static Action Bind(Button b, RuntimePlatform p, Action quit, VisualElement? row = null)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (quit == null) throw new ArgumentNullException(nameof(quit));
            bool shown = Shown(p);
            // Shown: the inline display is cleared, so the stylesheet decides, as for the buttons beside it.
            (row ?? b).style.display = shown ? new StyleEnum<DisplayStyle>(StyleKeyword.Null) : DisplayStyle.None;
            if (!shown) return () => { };
            b.clicked += quit;
            return () => b.clicked -= quit;
        }

        /// <summary><see cref="Bind"/> as the presenters bind: on <see cref="Platform"/>, calling <see cref="Quitter"/>.</summary>
        internal static Action BindInGame(Button b, VisualElement? row = null) => Bind(b, Platform(), () => Quitter(), row);
    }
}
