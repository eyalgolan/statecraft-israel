#nullable enable
using System.Collections.Generic;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Tells the camera what share of the window the top bar covers (CameraRig.TopInset), so the map is framed in
    /// the area below the bar and labels near the top of the view are not hidden under it (playtest 1, item 3). The share
    /// runs from the window's top to the bar's bottom edge, so a phone's safe area above the bar counts too (#2572). Follows
    /// the bar and the window as they change size; disposing gives the camera the whole screen back.</summary>
    public sealed class TopBarInset : System.IDisposable
    {
        readonly VisualElement root, bar; readonly CameraRig rig;
        /// <summary>The bar and every element up to <see cref="root"/>: SafeAreaFit pads the root, which moves #root (and the
        /// bar inside it) without changing the root's or the bar's own layout, so their events alone would miss it (#2572).</summary>
        readonly List<VisualElement> watched = new List<VisualElement>();

        public TopBarInset(VisualElement root, CameraRig rig)
        {
            this.root = root; this.rig = rig;
            bar = root.Q<VisualElement>("top-bar");
            for (var e = bar; e != null; e = e.parent)
            {
                watched.Add(e);
                if (e == root) break;
            }
            foreach (var e in watched) e.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            Update();
        }

        void OnGeometry(GeometryChangedEvent e) => Update();

        /// <summary>Sets the inset from the current layout (0 until the panel has one).</summary>
        public void Update()
        {
            if (rig == null) return;   // the camera went first (scene teardown)
            var panel = root.panel?.visualTree;
            var window = panel?.worldBound.height ?? float.NaN;
            var height = bar.worldBound.height;
            var covered = bar.worldBound.yMax - (panel?.worldBound.yMin ?? float.NaN);
            rig.TopInset = window > 0 && !float.IsNaN(height) && height > 0 && !float.IsNaN(covered) && covered > 0
                ? Mathf.Clamp01(covered / window) : 0f;
            rig.ApplyProjection();
        }

        public void Dispose()
        {
            foreach (var e in watched) e.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            watched.Clear();
            if (rig == null) return;
            rig.TopInset = 0f;
            rig.ApplyProjection();
        }
    }
}
