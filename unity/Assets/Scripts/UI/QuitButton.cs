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
        /// <summary>Whether Quit shows on <paramref name="p"/>: the Windows, macOS and Linux players and their editors.</summary>
        public static bool Shown(RuntimePlatform p) => p switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer => true,
            RuntimePlatform.WindowsEditor or RuntimePlatform.OSXEditor or RuntimePlatform.LinuxEditor => true,
            _ => false,
        };

        /// <summary>Shows <paramref name="b"/> only where <see cref="Shown"/>, and calls <paramref name="quit"/> when it is
        /// pressed. The presenters pass Application.Quit. Returns the unbinding, for the presenter's Dispose (a language
        /// switch builds the screens again over the same buttons).</summary>
        public static Action Bind(Button b, RuntimePlatform p, Action quit)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (quit == null) throw new ArgumentNullException(nameof(quit));
            bool shown = Shown(p);
            b.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) return () => { };
            b.clicked += quit;
            return () => b.clicked -= quit;
        }
    }
}
