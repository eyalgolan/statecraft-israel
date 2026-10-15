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
        /// <summary>Which side of its button the tip sits on.</summary>
        public enum Side { Left, Right }

        readonly VisualElement layer; readonly Label tip; readonly Func<Side> side;
        readonly List<VisualElement> targets = new List<VisualElement>();
        readonly EventCallback<PointerEnterEvent> onEnter; readonly EventCallback<PointerLeaveEvent> onLeave;
        readonly EventCallback<FocusInEvent> onFocus; readonly EventCallback<FocusOutEvent> onBlur;
        readonly EventCallback<ClickEvent> onClick; readonly EventCallback<NavigationSubmitEvent> onSubmit;
        VisualElement? hovered, focused, pressed, shownFor;

        /// <param name="root">The UI root holding the <c>hud-tip</c> label.</param>
        /// <param name="side">Where the tip goes, asked each time it shows (a mirrored layout may move the buttons).</param>
        public HoverTip(VisualElement root, IEnumerable<VisualElement> buttons, Func<Side> side)
        {
            tip = root.Q<Label>(TipName); layer = tip.parent; this.side = side;
            onEnter = e => { hovered = (VisualElement)e.currentTarget; pressed = null; Refresh(); };
            onLeave = e => { if (hovered == e.currentTarget) hovered = null; Refresh(); };
            onFocus = e => { focused = (VisualElement)e.currentTarget; if (pressed == focused && hovered != focused) pressed = null; Refresh(); };
            onBlur = e => { if (focused == e.currentTarget) focused = null; Refresh(); };
            onClick = e => { pressed = (VisualElement)e.currentTarget; Refresh(); };
            onSubmit = e => { pressed = (VisualElement)e.currentTarget; Refresh(); };
            foreach (var b in buttons)
            {
                targets.Add(b);
                b.RegisterCallback(onEnter); b.RegisterCallback(onLeave); b.RegisterCallback(onFocus); b.RegisterCallback(onBlur);
                b.RegisterCallback(onClick); b.RegisterCallback(onSubmit);
            }
        }

        /// <summary>The button the tip shows for now, or null.</summary>
        public VisualElement? ShownFor => shownFor;

        void Refresh()
        {
            var target = hovered ?? focused;
            if (target != null && target == pressed) target = null;
            if (target == null || string.IsNullOrEmpty(target.tooltip) || !(target.worldBound.width > 0)) { Hide(); return; }
            var r = layer.WorldToLocal(target.worldBound);
            tip.text = target.tooltip;
            if (side() == Side.Left) { tip.style.left = StyleKeyword.Auto; tip.style.right = layer.layout.width - r.xMin + 8; }
            else { tip.style.right = StyleKeyword.Auto; tip.style.left = r.xMax + 8; }
            tip.style.top = r.center.y;
            tip.style.display = DisplayStyle.Flex;
            shownFor = target;
        }

        void Hide()
        {
            if (shownFor == null) return;
            tip.style.display = DisplayStyle.None;
            shownFor = null;
        }

        public void Dispose()
        {
            foreach (var b in targets)
            {
                b.UnregisterCallback(onEnter); b.UnregisterCallback(onLeave); b.UnregisterCallback(onFocus); b.UnregisterCallback(onBlur);
                b.UnregisterCallback(onClick); b.UnregisterCallback(onSubmit);
            }
            hovered = focused = pressed = null;
            Hide();
            targets.Clear();
        }
    }
}
