#nullable enable
using System.Collections.Generic;
using Statecraft.Game.Boot;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Under ?navlog (DiagLog.Nav): logs every pointer down/up, click and navigation submit the game panel
    /// receives, with the frame and the target element, before any element handles it (trickle-down). Playtest 2, P2-4.</summary>
    public sealed class NavEventLog : System.IDisposable
    {
        readonly VisualElement root;
        readonly List<Button> buttons;

        public NavEventLog(VisualElement root)
        {
            this.root = root;
            // On the buttons themselves too: a button holds pointer capture, so its up event never trickles through root.
            buttons = root.Query<Button>().ToList();
            foreach (var b in buttons)
            {
                b.RegisterCallback<PointerUpEvent>(OnButtonUp);
                b.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            }
            InputSystem.onEvent += OnRaw;
            root.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
        }

        static string Name(IEventHandler? t) => t is VisualElement v ? (string.IsNullOrEmpty(v.name) ? v.GetType().Name : v.name) : "?";

        /// <summary>The classes on one element, sorted so two lines can be compared by eye.</summary>
        static string Classes(VisualElement v)
        {
            var names = new List<string>(v.GetClasses());
            names.Sort(System.StringComparer.Ordinal);
            return names.Count == 0 ? "" : "." + string.Join(".", names);
        }

        /// <summary>The pressed element and every ancestor above it, each with its classes, innermost first. A press that
        /// lands somewhere nobody expected is usually a full-screen layer in front of the panels, and the rules that show
        /// those layers are descendant selectors (Statecraft.uss, ".event-open .event-scrim"), so the class that displays
        /// the backdrop may sit on any ancestor, not only the HUD root — the whole chain is what settles it. #1728: in
        /// Hebrew a press on the unit panel's Skip reaches `event-scrim` with nothing dimmed, which needs the backdrop to
        /// be both displayed and invisible; this says whether it was displayed, and from where.</summary>
        static string Path(IEventHandler? target)
        {
            if (target is not VisualElement v) return "?";
            var parts = new List<string>();
            for (var e = v; e != null && parts.Count < 12; e = e.parent)
                parts.Add((string.IsNullOrEmpty(e.name) ? e.GetType().Name : e.name) + Classes(e));
            return string.Join(" < ", parts);
        }

        /// <summary>The two modal cards' own state, when either is in the layout. Neither is an ancestor of the backdrop,
        /// so <see cref="Path"/> cannot show them: a press outside a card lands on the scrim, and the chain above the
        /// scrim says nothing about the card the scrim is protecting. What this adds is the card's classes and its
        /// resolved opacity. #1728 needs exactly that pair: a card left in `window-opening` is held at `opacity: 0` and
        /// `scale: 0.8` (Statecraft.uss, ".event-card.window-opening") while staying in the layout and taking presses, so
        /// the player is blocked by a modal they cannot see. `window-opening` with an opacity near 0 is that state;
        /// an opacity near 1 rules it out.</summary>
        string Modals()
        {
            var hud = root.Q("root") ?? root;
            var parts = new List<string>();
            foreach (var name in new[] { "capture-card", "event-card" })
            {
                var card = hud.Q(name);
                if (card == null || card.resolvedStyle.display == DisplayStyle.None) continue;
                var b = card.worldBound;
                parts.Add($"{name}{Classes(card)} opacity={card.resolvedStyle.opacity:0.00} "
                          + $"box=({b.x:0}, {b.y:0}, {b.width:0}x{b.height:0}) enabled={card.enabledInHierarchy}");
            }
            return parts.Count == 0 ? "-" : string.Join(" | ", parts);
        }

        void OnDown(PointerDownEvent e) => DiagLog.NavLine($"ui pointerdown id={e.pointerId} at ({e.position.x:0}, {e.position.y:0}) target={Name(e.target)} path={Path(e.target)} modals={Modals()}");
        void OnUp(PointerUpEvent e) => DiagLog.NavLine($"ui pointerup id={e.pointerId} at ({e.position.x:0}, {e.position.y:0}) target={Name(e.target)}");
        void OnClick(ClickEvent e) => DiagLog.NavLine($"ui click count={e.clickCount} target={Name(e.target)}");
        void OnButtonUp(PointerUpEvent e) => DiagLog.NavLine($"button pointerup at ({e.position.x:0}, {e.position.y:0}) on={Name(e.currentTarget)} inside={((VisualElement)e.currentTarget).worldBound.Contains(e.position)}");
        void OnCaptureOut(PointerCaptureOutEvent e) => DiagLog.NavLine($"button capture out on={Name(e.currentTarget)}");

        /// <summary>Every raw mouse state event the Input System processes (after any merging), with its position and buttons.</summary>
        void OnRaw(InputEventPtr e, InputDevice d)
        {
            if (!(d is Mouse m) || !(e.IsA<StateEvent>() || e.IsA<DeltaStateEvent>())) return;
            var at = m.position.ReadValueFromEvent(e);
            var left = m.leftButton.ReadValueFromEvent(e);
            DiagLog.NavLine($"raw mouse left={left:0} at ({at.x:0}, {at.y:0}) t={e.time:0.000}");
        }

        void OnSubmit(NavigationSubmitEvent e) => DiagLog.NavLine($"ui submit target={Name(e.target)}");

        public void Dispose()
        {
            InputSystem.onEvent -= OnRaw;
            foreach (var b in buttons)
            {
                b.UnregisterCallback<PointerUpEvent>(OnButtonUp);
                b.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            }
            root.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
        }
    }
}
