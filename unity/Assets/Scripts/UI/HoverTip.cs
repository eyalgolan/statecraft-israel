#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A visible tip for icon-only buttons (owner ruling on #584: "icon-only, with tooltips"). Each button keeps its
    /// name in <c>tooltip</c>, as the top bar's meters do (#523), but UI Toolkit draws <c>tooltip</c> only in Editor UI, not
    /// in a runtime panel, so this shows the same text in Game.uxml's <c>hud-tip</c> label beside the button: on pointer
    /// hover and on keyboard focus (a keyboard player sees the name too), hidden on leave and on blur. The label takes no
    /// clicks. Nothing runs per frame.</summary>
    public sealed class HoverTip : IDisposable
    {
        public const string TipName = "hud-tip";
        /// <summary>Which side of its button the tip sits on.</summary>
        public enum Side { Left, Right }

        readonly VisualElement layer; readonly Label tip; readonly Func<Side> side;
        readonly List<VisualElement> targets = new List<VisualElement>();
        readonly EventCallback<PointerEnterEvent> onEnter; readonly EventCallback<PointerLeaveEvent> onLeave;
        readonly EventCallback<FocusInEvent> onFocus; readonly EventCallback<FocusOutEvent> onBlur;
        VisualElement? shownFor;

        /// <param name="root">The UI root holding the <c>hud-tip</c> label.</param>
        /// <param name="side">Where the tip goes, asked each time it shows (a mirrored layout may move the buttons).</param>
        public HoverTip(VisualElement root, IEnumerable<VisualElement> buttons, Func<Side> side)
        {
            tip = root.Q<Label>(TipName); layer = tip.parent; this.side = side;
            onEnter = e => Show((VisualElement)e.currentTarget); onLeave = e => Hide((VisualElement)e.currentTarget);
            onFocus = e => Show((VisualElement)e.currentTarget); onBlur = e => Hide((VisualElement)e.currentTarget);
            foreach (var b in buttons)
            {
                targets.Add(b);
                b.RegisterCallback(onEnter); b.RegisterCallback(onLeave); b.RegisterCallback(onFocus); b.RegisterCallback(onBlur);
            }
        }

        /// <summary>The button the tip shows for now, or null.</summary>
        public VisualElement? ShownFor => shownFor;

        public void Show(VisualElement target)
        {
            if (string.IsNullOrEmpty(target.tooltip) || !(target.worldBound.width > 0)) return;
            var r = layer.WorldToLocal(target.worldBound);
            tip.text = target.tooltip;
            if (side() == Side.Left) { tip.style.left = StyleKeyword.Auto; tip.style.right = layer.layout.width - r.xMin + 8; }
            else { tip.style.right = StyleKeyword.Auto; tip.style.left = r.xMax + 8; }
            tip.style.top = r.center.y;
            tip.style.display = DisplayStyle.Flex;
            shownFor = target;
        }

        public void Hide(VisualElement target)
        {
            if (shownFor != target) return;
            tip.style.display = DisplayStyle.None;
            shownFor = null;
        }

        public void Dispose()
        {
            foreach (var b in targets)
            {
                b.UnregisterCallback(onEnter); b.UnregisterCallback(onLeave); b.UnregisterCallback(onFocus); b.UnregisterCallback(onBlur);
            }
            if (shownFor != null) Hide(shownFor);
            targets.Clear();
        }
    }
}
