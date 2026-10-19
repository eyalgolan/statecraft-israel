#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The map's keyboard stop (#41 B1b, #1116; docs/superpowers/plans/2026-10-14-a11y-keyboard-map.md): Game.uxml's
    /// "map-focus" takes a place in the Tab order after End Turn. While it has the focus the stylesheet shows its ring, which
    /// this places on the camera's look-at point; the camera keys keep panning (NavigationGuard), so the player moves the map
    /// under the ring. Enter (the UI's Submit) or Space picks the hex under it through <paramref name="pick"/>, the map's own
    /// pick (MapPointer.PickAt), and the focus stays on the stop. Its own handlers only: disposing (a restart, a language
    /// switch) leaves the element as Game.uxml made it.</summary>
    public sealed class MapFocusPresenter : IDisposable
    {
        readonly VisualElement stop, mark;
        readonly Camera camera; readonly CameraRig rig; readonly Action<Vector2> pick;
        readonly IVisualElementScheduledItem follow;

        /// <param name="pick">Picks the hex under a screen point (pixels, y up), as a click there would.</param>
        public MapFocusPresenter(VisualElement root, Camera camera, CameraRig rig, Action<Vector2> pick)
        {
            stop = root.Q("map-focus"); mark = root.Q("map-focus-mark");
            this.camera = camera; this.rig = rig; this.pick = pick;
            stop.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            stop.RegisterCallback<KeyDownEvent>(OnKey);
            stop.RegisterCallback<FocusInEvent>(OnFocus);
            // every panel update, which runs after the camera's (CameraController.Update): the ring never trails a pan
            follow = stop.schedule.Execute(Place).Every(0);
        }

        void OnFocus(FocusInEvent e) => Place();

        // Enter arrives as Submit (the only key with the Submit usage); Space only as a key, so it is caught here.
        void OnSubmit(NavigationSubmitEvent e) { Pick(); e.StopPropagation(); }

        void OnKey(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Space) return;
            Pick();
            e.StopPropagation();
        }

        void Pick()
        {
            if (camera == null || rig == null) return;   // the camera went first (scene teardown)
            var s = camera.WorldToScreenPoint(rig.LookAt);
            pick(new Vector2(s.x, s.y));   // off the terrain nothing is picked, as for a click there
        }

        /// <summary>Puts the ring's centre on the look-at point (Statecraft.uss centres the ring on its left/top), while the
        /// stop has the focus. The look-at point sits in the middle of the map area below the top bar (CameraRig.TopInset).</summary>
        void Place()
        {
            var panel = stop.panel;
            if (panel == null || camera == null || rig == null || panel.focusController.focusedElement != stop) return;
            var p = MapLabelsPresenter.ToPanel(camera.WorldToViewportPoint(rig.LookAt), camera.rect, panel.visualTree.layout.size,
                stop.worldBound.position);
            mark.style.left = p.x;
            mark.style.top = p.y;
        }

        public void Dispose()
        {
            follow.Pause();
            stop.UnregisterCallback<NavigationSubmitEvent>(OnSubmit);
            stop.UnregisterCallback<KeyDownEvent>(OnKey);
            stop.UnregisterCallback<FocusInEvent>(OnFocus);
        }
    }
}
