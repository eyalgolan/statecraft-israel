#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The top-bar buttons open the Cabinet, Budget, Defence and Why screens, one at a time; pressing the open
    /// screen's button again closes it. Every press toggles, synchronously, with no debounce: two presses in one frame (a
    /// double click, or a click plus Enter) open and close again, so the result depends only on the number of presses
    /// (HudTests.TwoPressesInOneFrameToggleTwice; playtest 1, item 11).</summary>
    public sealed class ScreenNav : System.IDisposable
    {
        public static readonly string[] Screens = { "cabinet", "budget", "defence", "why" };
        /// <summary>The class on the open screen's button (Statecraft.uss lights its ring).</summary>
        public const string OpenClass = "nav-open";
        readonly VisualElement root;
        readonly List<(Button, System.Action)> hooks = new List<(Button, System.Action)>();

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            foreach (var s in Screens)
            {
                var screen = s;
                var b = root.Q<Button>("nav-" + screen);
                System.Action h = () => Toggle(screen);
                b.clicked += h;
                hooks.Add((b, h));
            }
            Open(null);
        }

        public void Dispose() { foreach (var (b, h) in hooks) b.clicked -= h; hooks.Clear(); }

        /// <summary>The open screen's name, or null.</summary>
        public string? Current { get; private set; }

        public void Toggle(string screen)
        {
            Boot.DiagLog.NavLine($"toggle {screen}: {Current ?? "none"} -> {(Current == screen ? "none" : screen)}");
            Open(Current == screen ? null : screen);
        }

        public void Open(string? screen)
        {
            Current = screen;
            foreach (var s in Screens)
            {
                root.Q<VisualElement>("screen-" + s).style.display = s == screen ? DisplayStyle.Flex : DisplayStyle.None;
                root.Q<Button>("nav-" + s).EnableInClassList(OpenClass, s == screen);   // the open screen's button is lit
            }
        }
    }
}
