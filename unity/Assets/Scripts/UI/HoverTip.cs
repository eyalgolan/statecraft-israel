#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A visible tip for icon-only buttons (owner ruling on #584: "icon-only, with tooltips"). Each button keeps its
    /// name in <c>tooltip</c>, as the top bar's meters do (#523), but UI Toolkit draws <c>tooltip</c> only in Editor UI, not
    /// in a runtime panel, so this shows the same text in Game.uxml's <c>hud-tip</c> label beside the button.
    /// Hover and keyboard focus are tracked apart (review of #584): the tip names the hovered button, else the focused one, so
    /// a pointer leaving does not hide the name the keyboard focus still holds, and a blur (PointerBlur after a click) does
    /// not hide the one under the pointer. Pressing a button hides its tip (the screen or layer it opens may lie under it)
    /// until the pointer or the focus comes to it again. On touch the pointer enters on finger down and leaves on lift, so
    /// the tip shows only while the finger is down, and the lift presses the button. The label takes no clicks; nothing runs
    /// per frame.</summary>
    public sealed class HoverTip : IDisposable
    {
        public const string TipName = "hud-tip";
        static readonly Dictionary<Label, Coordinator> Coordinators = new Dictionary<Label, Coordinator>();
        /// <summary>Which side of its button the tip sits on.</summary>
        public enum Side { Left, Right }

        readonly VisualElement layer; readonly Label tip; readonly Func<Side> side;
        readonly Coordinator coordinator;
        readonly List<VisualElement> targets = new List<VisualElement>();
        readonly EventCallback<PointerEnterEvent> onEnter; readonly EventCallback<PointerLeaveEvent> onLeave;
        readonly EventCallback<FocusInEvent> onFocus; readonly EventCallback<FocusOutEvent> onBlur;
        readonly EventCallback<ClickEvent> onClick; readonly EventCallback<NavigationSubmitEvent> onSubmit;
        VisualElement? hovered, focused, pressed, shownFor;
        long hoveredAt, focusedAt;
        bool disposed;

        /// <param name="root">The UI root holding the <c>hud-tip</c> label.</param>
        /// <param name="side">Where the tip goes, asked each time it shows (a mirrored layout may move the buttons).</param>
        public HoverTip(VisualElement root, IEnumerable<VisualElement> buttons, Func<Side> side)
        {
            tip = root.Q<Label>(TipName); layer = tip.parent; this.side = side;
            if (!Coordinators.TryGetValue(tip, out var shared)) Coordinators.Add(tip, shared = new Coordinator(tip));
            coordinator = shared;
            onEnter = e => { hovered = (VisualElement)e.currentTarget; hoveredAt = coordinator.Next(); pressed = null; coordinator.Refresh(); };
            onLeave = e => { if (hovered == e.currentTarget) { hovered = null; hoveredAt = 0; } coordinator.Refresh(); };
            onFocus = e => { focused = (VisualElement)e.currentTarget; focusedAt = coordinator.Next(); if (pressed == focused && hovered != focused) pressed = null; coordinator.Refresh(); };
            onBlur = e => { if (focused == e.currentTarget) { focused = null; focusedAt = 0; } coordinator.Refresh(); };
            onClick = e => { pressed = (VisualElement)e.currentTarget; coordinator.Refresh(); };
            onSubmit = e => { pressed = (VisualElement)e.currentTarget; coordinator.Refresh(); };
            foreach (var b in buttons)
            {
                targets.Add(b);
                b.RegisterCallback(onEnter); b.RegisterCallback(onLeave); b.RegisterCallback(onFocus); b.RegisterCallback(onBlur);
                b.RegisterCallback(onClick); b.RegisterCallback(onSubmit);
            }
            coordinator.Add(this);
        }

        /// <summary>The button the tip shows for now, or null.</summary>
        public VisualElement? ShownFor => shownFor;

        void Show(VisualElement target)
        {
            var r = layer.WorldToLocal(target.worldBound);
            tip.text = target.tooltip;
            if (side() == Side.Left) { tip.style.left = StyleKeyword.Auto; tip.style.right = layer.layout.width - r.xMin + 8; }
            else { tip.style.right = StyleKeyword.Auto; tip.style.left = r.xMax + 8; }
            tip.style.top = r.center.y;
            tip.style.display = DisplayStyle.Flex;
            shownFor = target;
        }

        sealed class Coordinator
        {
            readonly Label tip;
            readonly List<HoverTip> members = new List<HoverTip>();
            long sequence;

            public Coordinator(Label tip) => this.tip = tip;
            public long Next() => ++sequence;
            public void Add(HoverTip member) => members.Add(member);

            public void Refresh()
            {
                HoverTip? owner = null;
                long latest = 0;
                foreach (var member in members)
                {
                    member.shownFor = null;
                    if (member.hovered != null && member.hoveredAt > latest) { owner = member; latest = member.hoveredAt; }
                }
                if (owner == null)
                {
                    foreach (var member in members)
                        if (member.focused != null && member.focusedAt > latest) { owner = member; latest = member.focusedAt; }
                }
                var target = owner?.hovered ?? owner?.focused;
                if (owner == null || target == null || target == owner.pressed || string.IsNullOrEmpty(target.tooltip) || !(target.worldBound.width > 0))
                {
                    tip.style.display = DisplayStyle.None;
                    return;
                }
                owner.Show(target);
            }

            public void Remove(HoverTip member)
            {
                members.Remove(member);
                member.shownFor = null;
                Refresh();
                if (members.Count == 0) Coordinators.Remove(tip);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var b in targets)
            {
                b.UnregisterCallback(onEnter); b.UnregisterCallback(onLeave); b.UnregisterCallback(onFocus); b.UnregisterCallback(onBlur);
                b.UnregisterCallback(onClick); b.UnregisterCallback(onSubmit);
            }
            hovered = focused = pressed = null;
            hoveredAt = focusedAt = 0;
            targets.Clear();
            coordinator.Remove(this);
        }
    }
}
