#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Each top-bar number explains itself (#1037): while the pointer is over a meter, or after a tap on it (a
    /// touchscreen has no hover), a tip under the bar gives the meter in words (its tooltip, HudModel.Spoken: the value
    /// and this turn's change) and the turn's main causes, which are the sim's own messages (HudModel.Causes). A second
    /// tap on the same meter closes it, a tap on another moves it, and a tap anywhere else or End Turn closes it; a mouse
    /// click pins nothing, as the mouse hovers (#1108). It shows only while the meter can take the pointer, so a report or
    /// an outcome clip over the bar hides it. The tip is centred under the meter and kept inside the window. Nothing is
    /// computed here; the tip takes no clicks. Its two labels are the meter's words in the UI's language and the causes,
    /// which the sim gives in English (#55), so each takes its own direction.</summary>
    public sealed class MeterTip : IDisposable
    {
        public const string TipName = "meter-tip";
        /// <summary>The gap between the tip and the window's edge, and between the meter and the tip.</summary>
        const float Margin = 8f;

        readonly VisualElement layer, tip; readonly Label words, causesLine; readonly SimBridge bridge; readonly UiText text;
        readonly List<VisualElement> meters = new List<VisualElement>();
        readonly EventCallback<PointerEnterEvent> onEnter; readonly EventCallback<PointerLeaveEvent> onLeave;
        readonly EventCallback<ClickEvent> onClick; readonly EventCallback<GeometryChangedEvent> onTipGeometry;
        readonly EventCallback<PointerDownEvent> onPress; readonly VisualElement pressScope;
        readonly Action<IReadOnlyList<Statecraft.Sim.Rejection>> onTurn;
        VisualElement? hovered, pinned, shownFor;

        public MeterTip(VisualElement root, SimBridge bridge, UiText text, IEnumerable<VisualElement> meters)
        {
            tip = root.Q<VisualElement>(TipName); layer = tip.parent; this.bridge = bridge; this.text = text;
            words = tip.Q<Label>("meter-tip-words"); causesLine = tip.Q<Label>("meter-tip-causes");
            onEnter = e => { hovered = (VisualElement)e.currentTarget; Refresh(); };
            onLeave = e => { if (hovered == e.currentTarget) hovered = null; Refresh(); };
            // #1108: a mouse click pinned the tip, so it outlived the pointer's leave; the mouse has hover, only a tap pins. A
            // click still drops a tap's pin (a touchscreen laptop), as the press handler leaves presses on meters alone.
            // (PointerType is spelled out: UnityEngine.PointerType has the same name.)
            onClick = e =>
            {
                var m = (VisualElement)e.currentTarget;
                pinned = e.pointerType == UnityEngine.UIElements.PointerType.mouse ? null : pinned == m ? null : m;
                Refresh();
            };
            // A tapped tip closes on the next tap anywhere: the panel sends a press with nothing under it (the map) to its root,
            // so the press is heard there before any control takes it. A press on a meter is left to its click (move, close).
            onPress = e => { if (pinned == null || OnAMeter(e.target as VisualElement)) return; pinned = null; Refresh(); };
            onTipGeometry = _ => Place();
            onTurn = _ => { pinned = null; Refresh(); };
            foreach (var m in meters)
            {
                m.RegisterCallback(onEnter); m.RegisterCallback(onLeave); m.RegisterCallback(onClick);
                this.meters.Add(m);
            }
            tip.RegisterCallback(onTipGeometry);
            pressScope = root.panel?.visualTree ?? root;   // a HUD built off a panel (some tests) has no panel root
            pressScope.RegisterCallback(onPress, TrickleDown.TrickleDown);
            bridge.TurnEnded += onTurn;
            Hide();
        }

        bool OnAMeter(VisualElement? e)
        {
            foreach (var m in meters) if (e != null && (m == e || m.Contains(e))) return true;
            return false;
        }

        /// <summary>The meter the tip is shown for, or null.</summary>
        public VisualElement? ShownFor => shownFor;

        /// <summary>The tip's two lines for <paramref name="meter"/> (an element named "meter-" + a HudModel.MeterIds id): its
        /// words, with the causes' heading when there are causes, and the causes ("" when none).</summary>
        public (string words, string causes) Lines(VisualElement meter)
        {
            var causes = HudModel.Causes(bridge.Session.Last, meter.name.Substring("meter-".Length));
            return causes.Count == 0 ? (meter.tooltip, "") : (meter.tooltip + "\n" + text["hud.causes"], string.Join("\n", causes));
        }

        /// <summary>Shows the tip for the hovered meter, else the tapped one, with today's words (HudPresenter.Render calls
        /// this after it sets the tooltips), or hides it.</summary>
        public void Refresh()
        {
            var target = hovered ?? pinned;
            if (target == null || !target.enabledInHierarchy || string.IsNullOrEmpty(target.tooltip) || !(target.worldBound.width > 0)) { Hide(); return; }
            var (w, c) = Lines(target);
            words.text = w; causesLine.text = c;
            causesLine.style.display = c.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            shownFor = target;
            tip.style.display = DisplayStyle.Flex;
            Place();
        }

        /// <summary>Under the meter, centred on it, inside the window: the tip's width is known once it is laid out, so this
        /// runs again on its geometry change.</summary>
        void Place()
        {
            if (shownFor == null) return;
            var r = layer.WorldToLocal(shownFor.worldBound);
            float w = tip.layout.width > 0 ? tip.layout.width : 0, room = layer.layout.width;
            float left = r.center.x - w / 2;
            left = Mathf.Max(Margin, Mathf.Min(left, room - w - Margin));
            if (Mathf.Abs(tip.style.left.value.value - left) > 0.5f || tip.style.left.keyword != StyleKeyword.Undefined) tip.style.left = left;
            var top = r.yMax + Margin;
            if (Mathf.Abs(tip.style.top.value.value - top) > 0.5f || tip.style.top.keyword != StyleKeyword.Undefined) tip.style.top = top;
        }

        void Hide()
        {
            tip.style.display = DisplayStyle.None;
            shownFor = null;
        }

        public void Dispose()
        {
            foreach (var m in meters) { m.UnregisterCallback(onEnter); m.UnregisterCallback(onLeave); m.UnregisterCallback(onClick); }
            meters.Clear();
            tip.UnregisterCallback(onTipGeometry);
            pressScope.UnregisterCallback(onPress, TrickleDown.TrickleDown);
            bridge.TurnEnded -= onTurn;
            hovered = pinned = null;
            Hide();
        }
    }
}
