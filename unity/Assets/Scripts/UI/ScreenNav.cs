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
        /// <summary>The class on the "root" element while a wide screen (class <see cref="WideClass"/>: Cabinet, Defence) is
        /// open: Statecraft.uss hides the info panel it covers, so its buttons take no focus and no clicks.</summary>
        public const string WideOpenClass = "wide-screen-open", WideClass = "screen-wide";
        /// <summary>The class on the "root" element while any screen is open: in the two-row (narrow) HUD every screen takes
        /// the full width, so Statecraft.uss hides the event column and the info panel under it (#210).</summary>
        public const string ScreenOpenClass = "screen-open";

        /// <summary>Asks to be told, synchronously, whenever <paramref name="screen"/> is opened (a presenter that skips
        /// rendering while its screen is hidden catches up then).</summary>
        public static void OnOpened(VisualElement screen, System.Action action) => screen.userData = action;

        /// <summary>True when <paramref name="screen"/> is open (ScreenNav shows it inline; the stylesheet hides it).</summary>
        public static bool IsOpen(VisualElement screen) =>
            screen.style.display.keyword == StyleKeyword.Undefined && screen.style.display.value == DisplayStyle.Flex;
        readonly VisualElement root;
        readonly List<(Button, System.Action)> hooks = new List<(Button, System.Action)>();
        readonly EventCallback<NavigationCancelEvent> onCancel;

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            // Escape (the UI map's Cancel, from any focused element or the panel) closes the open screen. NavigationGuard
            // handles only NavigationMoveEvent, so the two never meet. GameRoot also polls the Escape key for the case where
            // no element has the focus (CloseOnEscape).
            onCancel = e => { if (Close()) e.StopPropagation(); };
            root.RegisterCallback(onCancel, TrickleDown.TrickleDown);
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

        public void Dispose()
        {
            foreach (var (b, h) in hooks) b.clicked -= h;
            hooks.Clear();
            root.UnregisterCallback(onCancel, TrickleDown.TrickleDown);
            var r = root.Q("root") ?? root;
            r.RemoveFromClassList(WideOpenClass); r.RemoveFromClassList(ScreenOpenClass);
        }

        /// <summary>Closes the open screen (Escape); false when none was open.</summary>
        public bool Close()
        {
            if (Current == null) return false;
            Open(null);
            return true;
        }

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
            var wide = false;
            foreach (var s in Screens)
            {
                var element = root.Q<VisualElement>("screen-" + s);
                element.style.display = s == screen ? DisplayStyle.Flex : DisplayStyle.None;
                root.Q<Button>("nav-" + s).EnableInClassList(OpenClass, s == screen);   // the open screen's button is lit
                if (s == screen) wide = element.ClassListContains(WideClass);
            }
            (root.Q("root") ?? root).EnableInClassList(WideOpenClass, wide);
            (root.Q("root") ?? root).EnableInClassList(ScreenOpenClass, screen != null);
            if (screen != null) (root.Q<VisualElement>("screen-" + screen).userData as System.Action)?.Invoke();
        }
    }
}
