#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>The tap rule, with no devices: a finger becomes a tap only when it lifts within
    /// <see cref="SlopPixels"/> of where it landed and no second finger was down while it was held. A moved finger
    /// drags the camera and two fingers pinch (CameraController), so neither may also select a hex.</summary>
    public struct TapPress
    {
        /// <summary>How far the finger may wander, in screen pixels, before the press stops being a tap (about
        /// Android's 8 dp touch slop at 3x).</summary>
        public const float SlopPixels = 30f;

        Vector2 start;
        bool down, moved, pinched;

        /// <summary>One frame of the watched finger: its pressed and released edges, where it is, and whether any
        /// other finger is down. The tap's position on a release frame that still qualifies, otherwise null.</summary>
        public Vector2? Step(bool pressed, bool released, Vector2 position, bool otherFingerDown)
        {
            if (pressed) { start = position; down = true; moved = false; pinched = false; }
            if (!down) return null;
            if (otherFingerDown) pinched = true;                       // a pinch is never a tap, even a still thumb
            if ((position - start).sqrMagnitude > SlopPixels * SlopPixels) moved = true;   // latched: coming back helps not
            if (!released) return null;
            down = false;
            return moved || pinched ? (Vector2?)null : start;
        }
    }

    /// <summary>Left click on the map (not on the UI) selects the hex under the pointer: a ray from the camera hits the
    /// terrain's collider, the hit becomes lon/lat (GeoFrame) and then a hex (HexPicker). On a touchscreen a tap (press and
    /// release inside TapPress.SlopPixels, with no second finger down) does the same, so a one-finger drag pans and a pinch zooms without selecting.</summary>
    public sealed class MapPointer : MonoBehaviour
    {
        public MapScene? Map;
        public Func<Vector2, bool>? IsOverUi;
        public event Action<int, int>? HexClicked;
        /// <summary>The hex a point goes to before the terrain is asked, or null: a city's name pill sends its click to the
        /// city's hex (#1063), wherever the land under the pill is.</summary>
        public Func<Vector2, (int q, int r)?>? PillHex;

        // Every press, at the position of its own input event (InputConfig keeps a release from merging into the next move),
        // queued and evaluated in order in Update: several presses in one slow frame are each checked where they happened,
        // never all at the frame's last pointer position (playtest 2, P2-4).
        readonly Queue<Vector2> presses = new Queue<Vector2>();
        InputAction? press;

        TapPress tap;

        void OnEnable()
        {
            press = new InputAction("map-press", InputActionType.Button, "<Mouse>/leftButton");
            press.performed += OnPressed;
            press.Enable();
        }

        void OnDisable()
        {
            if (press != null) { press.performed -= OnPressed; press.Dispose(); press = null; }
            presses.Clear();
        }

        void OnPressed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) OnPress(p.position.ReadValue());   // read during the event: its own position
        }

        /// <summary>Queues a press at <paramref name="at"/> (screen pixels, y up).</summary>
        internal void OnPress(Vector2 at) => presses.Enqueue(at);

        /// <summary>Handles every queued press, in order, each at its own position.</summary>
        internal void ProcessPresses()
        {
            while (presses.Count > 0) HandleClick(presses.Dequeue());
        }

        void Update()
        {
            ProcessPresses();
            var ts = Touchscreen.current;
            if (ts == null) return;
            var t = ts.primaryTouch;
            var pressed = 0;
            foreach (var touch in ts.touches)
                if (touch.press.isPressed) pressed++;
            // any other finger down cancels the tap (TapPress): a releasing pinch's thumb is inside the slop, yet must not select
            var tapAt = tap.Step(t.press.wasPressedThisFrame, t.press.wasReleasedThisFrame,
                t.position.ReadValue(), pressed > (t.press.isPressed ? 1 : 0));
            if (tapAt.HasValue) OnPress(tapAt.Value);   // a tap selects where the finger landed, like a click
        }

        /// <summary>A left click at <paramref name="pos"/> (screen pixels): ignored over the UI (the same IsOverUi the camera
        /// uses), otherwise the hex under the pointer is raised as HexClicked. Returns whether a hex was picked.</summary>
        public bool HandleClick(Vector2 pos)
        {
            var overUi = IsOverUi != null && IsOverUi(pos);
            Boot.DiagLog.NavLine(FormattableString.Invariant($"map click at ({pos.x:0}, {pos.y:0}) overUi={overUi}"));
            if (overUi || HexAt(pos, false, true) is not (int q, int r)) return false;   // the UI asked once per press (MapPressTests)
            Map!.Select(q, r);                                          // the map marks the hex the info panel describes
            HexClicked?.Invoke(q, r);
            return true;
        }

        /// <summary>The pick without the UI check: the hex under <paramref name="pos"/> (screen pixels) is outlined and raised
        /// as HexClicked, as for a click. The keyboard's map stop (MapFocusPresenter, #41 B1b) picks here at the projected
        /// look-at point: that is not a pointer, and on a narrow window the info panel may sit over the centre. Returns
        /// whether a hex was picked.</summary>
        public bool PickAt(Vector2 pos)
        {
            // terrain only: the stop's centre ring marks the land, not a city's name pill
            if (HexAt(pos, false, false) is not (int q, int r)) return false;
            Map!.Select(q, r);                                          // the map marks the hex the info panel describes
            HexClicked?.Invoke(q, r);
            return true;
        }

        /// <summary>The hex a click at <paramref name="pos"/> (screen pixels) would pick, or null over the UI or off the map:
        /// a pill's city hex (PillHex), else the hex of the unit token drawn under the pointer (UnitLayer.TokenAtScreen), else
        /// the hex of the terrain under the pointer. GameRoot also asks it for the hover.</summary>
        public (int q, int r)? HexAt(Vector2 pos) => HexAt(pos, IsOverUi != null && IsOverUi(pos), true);

        /// <summary>The hex a click at <paramref name="pos"/> would pick were no UI there (HexAt without the UI check).
        /// GameRoot asks it for a click on the unit card over a lit hex (#1617).</summary>
        public (int q, int r)? HexUnder(Vector2 pos) => HexAt(pos, false, true);

        (int q, int r)? HexAt(Vector2 pos, bool overUi, bool pills)
        {
            if (overUi) return null;                                    // the UI is asked first, whatever else is missing
            if (Map == null || Map.Terrain == null) return null;
            if (pills && PillHex?.Invoke(pos) is (int, int) pill) return pill;
            var cam = Camera.main;
            // A unit's badge and figures stand above the ground: a click on them is a click on the unit's hex, not on the
            // terrain behind them (owner's report on v0.2.0). Not for the keyboard's look-at point (pills false).
            var units = pills && cam != null ? Map.Terrain.transform.Find(Units.UnitLayer.RootName) : null;
            if (units != null && units.TryGetComponent<Units.UnitLayer>(out var layer) && layer.TokenAtScreen(cam!, pos) is Units.PlacedToken token)
                return (token.Q, token.R);
            if (cam == null || !TryGroundHit(Map, cam.ScreenPointToRay(pos), out var hit)) return null;
            return Pick(Map, hit.point);
        }

        // The opt-in board draws its own terrain above the lowered real map. Use the nearest displayed ground,
        // falling through its sea/outside holes to the real map. Props and markers never decide the clicked hex.
        static bool TryGroundHit(MapScene map, Ray ray, out RaycastHit hit)
        {
            hit = default;
            bool found = false;
            foreach (var terrain in map.GetComponentsInChildren<Terrain>())
            {
                if (!terrain.enabled || !terrain.drawHeightmap) continue;
                var collider = terrain.GetComponent<TerrainCollider>();
                if (collider == null || !collider.enabled ||
                    !collider.Raycast(ray, out var candidate, found ? hit.distance : 1_000_000f)) continue;
                hit = candidate;
                found = true;
            }
            return found;
        }

        public static (int q, int r) Pick(MapScene map, Vector3 world)
        {
            var (lon, lat) = map.Frame.ToLonLat(world);
            return HexPicker.FromLonLat(lon, lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        }
    }
}
