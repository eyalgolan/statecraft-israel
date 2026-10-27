#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>The tap rule, with no devices: a finger becomes a tap only when it lifts within
    /// <see cref="SlopPixels"/> of where it landed, travelled no further than that inside any one frame, and no second
    /// finger was down while it was held. A moved finger drags the camera and two fingers pinch (CameraController), so
    /// neither may also select a hex.</summary>
    public struct TapPress
    {
        /// <summary>How far the finger may wander, in screen pixels, before the press stops being a tap (about
        /// Android's 8 dp touch slop at 3x).</summary>
        public const float SlopPixels = 30f;

        Vector2 start;
        bool down, moved, pinched;

        /// <summary>True while the press could still end as a tap: it is down, has stayed inside the slop and no second
        /// finger came. The camera does not pan on such a press (CameraController.HoldsDrag), so a press is either a tap or a
        /// pan, never both (#1654 review).</summary>
        public bool TapCandidate => down && !moved && !pinched;

        /// <summary>One frame of the watched finger: its pressed and released edges, where it is, whether any other
        /// finger is down, and how far it travelled within this frame. The tap's position on a release frame that
        /// still qualifies, otherwise null.
        /// <para><paramref name="travel"/> is what makes a very fast swipe a pan rather than a tap (owner's ruling of
        /// 26 October 2026, #1661 item 4). A finger sampled once per frame can press, cross the screen and lift inside
        /// one input update, and then <c>position</c> and <c>start</c> are the same sample: the distance test below
        /// sees nothing moved however far it went. The frame's own travel does see it. Callers with no travel to
        /// report (the mouse's right button, which is stepped from its action callbacks) pass none, so their rule is
        /// unchanged.</para></summary>
        public Vector2? Step(bool pressed, bool released, Vector2 position, bool otherFingerDown, Vector2 travel = default)
        {
            if (pressed) { start = position; down = true; moved = false; pinched = false; }
            if (!down) return null;
            if (otherFingerDown) pinched = true;                       // a pinch is never a tap, even a still thumb
            if ((position - start).sqrMagnitude > SlopPixels * SlopPixels) moved = true;   // latched: coming back helps not
            if (travel.sqrMagnitude > SlopPixels * SlopPixels) moved = true;               // a whole flick inside one frame
            if (!released) return null;
            down = false;
            return moved || pinched ? (Vector2?)null : start;
        }
    }

    /// <summary>Left click on the map (not on the UI) selects the hex under the pointer: a ray from the camera hits the
    /// terrain's collider, the hit becomes lon/lat (GeoFrame) and then a hex (HexPicker). On a touchscreen a tap (press and
    /// release inside TapPress.SlopPixels, with no second finger down) does the same, so a one-finger drag pans and a pinch zooms without selecting.
    /// A right click (press and release inside the same slop) raises HexOrdered instead, selecting nothing. Since #998 a
    /// left press behaves the same way: inside the slop it selects on release, and past it the camera pans and nothing is
    /// selected. One TapPress per gesture decides both a click and the camera's pan (RightTapCandidate, LeftTapCandidate,
    /// TouchTapCandidate): the camera pans only once the press has left the slop, so a press is a click or a pan, never
    /// both (#1654 review).</summary>
    /// <remarks>Runs before <see cref="CameraController"/> (#1661 item 5). The camera asks this pointer whether a press
    /// may still be a tap. For a finger that answer is <see cref="TouchTapCandidate"/>, which only this component's own
    /// frame moves; <see cref="RightTapCandidate"/> is moved by the right button's action callbacks
    /// (<see cref="RightStep"/>) instead, so it does not depend on the order. With no order declared, Unity was free to
    /// tick the camera first, which then read the touch answer from the frame before: a one-finger drag lifted just
    /// after crossing the slop had its delta kept by CameraController.HoldDrag and then dropped, so the pan was lost.
    /// The order is the cause, so the order is what is fixed; HoldDrag's contract (a press is a click or a pan, never
    /// both, #1654) is left alone.</remarks>
    [DefaultExecutionOrder(-100)]
    public sealed class MapPointer : MonoBehaviour
    {
        public MapScene? Map;
        public Func<Vector2, bool>? IsOverUi;
        /// <summary>A picked hex and whether its pointer hit was a city's name pill. Keyboard picks always pass false.</summary>
        public event Action<int, int, bool>? HexClicked;
        /// <summary>A right click on a hex (owner, 25 Oct 2026, "Civ: select, then click": it moves the selected unit there).</summary>
        public event Action<int, int>? HexOrdered;
        /// <summary>The hex a point goes to before the terrain is asked, or null: a city's name pill sends its click to the
        /// city's hex (#1063), wherever the land under the pill is.</summary>
        public Func<Vector2, (int q, int r)?>? PillHex;

        // Every press, at the position of its own input event (InputConfig keeps a release from merging into the next move),
        // queued and evaluated in order in Update: several presses in one slow frame are each checked where they happened,
        // never all at the frame's last pointer position (playtest 2, P2-4).
        readonly Queue<Vector2> presses = new Queue<Vector2>();
        // Right clicks, as presses are: the right button's press, every pointer move while it is held and its release are
        // each stepped at their own event's position, so a drag out and back inside one slow frame is still a drag.
        readonly Queue<Vector2> rightClicks = new Queue<Vector2>();
        InputAction? press, leftMove, rightPress, rightMove;

        TapPress tap, rightTap, leftTap;

        /// <summary>True while the right button's press could still end as a right click (TapPress.TapCandidate): the
        /// camera holds its right-drag pan until it is false.</summary>
        public bool RightTapCandidate => rightTap.TapCandidate;
        /// <summary>True while the primary finger's press could still end as a tap: the camera holds its one-finger pan.</summary>
        public bool TouchTapCandidate => tap.TapCandidate;
        /// <summary>True while the left button's press could still end as a click (#998): the camera holds its drag
        /// until the press leaves the slop, so a click never nudges the map and a drag past it pans.</summary>
        public bool LeftTapCandidate => leftTap.TapCandidate;

        void OnEnable()
        {
            press = new InputAction("map-press", InputActionType.Button, "<Mouse>/leftButton");
            press.performed += OnPressed;
            press.canceled += OnReleased;
            press.Enable();
            leftMove = new InputAction("map-left-move", InputActionType.PassThrough, "<Mouse>/position");
            leftMove.performed += OnLeftMoved;
            leftMove.Enable();
            rightPress = new InputAction("map-right-press", InputActionType.Button, "<Mouse>/rightButton");
            rightPress.performed += OnRightPressed;
            rightPress.canceled += OnRightReleased;
            rightPress.Enable();
            rightMove = new InputAction("map-right-move", InputActionType.PassThrough, "<Mouse>/position");
            rightMove.performed += OnRightMoved;
            rightMove.Enable();
        }

        void OnDisable()
        {
            if (press != null) { press.performed -= OnPressed; press.canceled -= OnReleased; press.Dispose(); press = null; }
            if (leftMove != null) { leftMove.performed -= OnLeftMoved; leftMove.Dispose(); leftMove = null; }
            if (rightPress != null) { rightPress.performed -= OnRightPressed; rightPress.canceled -= OnRightReleased; rightPress.Dispose(); rightPress = null; }
            if (rightMove != null) { rightMove.performed -= OnRightMoved; rightMove.Dispose(); rightMove = null; }
            presses.Clear(); rightClicks.Clear();
            rightTap = default; leftTap = default;
        }

        void OnRightPressed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) RightStep(true, false, p.position.ReadValue());
        }

        void OnRightMoved(InputAction.CallbackContext ctx) => RightStep(false, false, ctx.ReadValue<Vector2>());

        void OnRightReleased(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) RightStep(false, true, p.position.ReadValue());
        }

        void OnPressed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) LeftStep(true, false, p.position.ReadValue());   // read during the event: its own position
        }

        void OnLeftMoved(InputAction.CallbackContext ctx) => LeftStep(false, false, ctx.ReadValue<Vector2>());

        void OnReleased(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) LeftStep(false, true, p.position.ReadValue());
        }

        /// <summary>One step of the left button's press (#998, owner's ruling of 22 October 2026: a left drag pans the
        /// map). The click is decided like the right button's: it is queued on the release, at the position the press
        /// went down, and only while it never left the slop. Queuing on the release and not the press is what lets a
        /// drag pan without selecting — a press cannot be known to be a drag until it moves — and the queue still holds
        /// one entry per physical press, so each click acts exactly once (docs/m3-rulings.md, #1617).</summary>
        internal void LeftStep(bool pressed, bool released, Vector2 position)
        {
            if (leftTap.Step(pressed, released, position, false) is Vector2 at) OnPress(at);
        }

        /// <summary>Queues a press at <paramref name="at"/> (screen pixels, y up).</summary>
        internal void OnPress(Vector2 at) => presses.Enqueue(at);

        /// <summary>Handles every queued press, in order, each at its own position.</summary>
        internal void ProcessPresses()
        {
            while (presses.Count > 0) HandleClick(presses.Dequeue());
            while (rightClicks.Count > 0) HandleRightClick(rightClicks.Dequeue());
        }

        void Update() => Tick();

        /// <summary>One frame of this pointer: the queued presses, then the primary finger's tap rule. Test seam (internal;
        /// the tests see it through InternalsVisibleTo), as CameraController.Tick is (#890): an EditMode test has no player
        /// loop, so it drives the real Update path instead of the handlers underneath it (#1661 item 6).</summary>
        internal void Tick()
        {
            ProcessPresses();
            var ts = Touchscreen.current;
            if (ts == null) return;
            var t = ts.primaryTouch;
            var pressed = 0;
            foreach (var touch in ts.touches)
                if (CameraController.DownThisUpdate(touch)) pressed++;   // counts a finger that already lifted this update (#1792)
            // any other finger down cancels the tap (TapPress): a releasing pinch's thumb is inside the slop, yet must not select
            var tapAt = tap.Step(t.press.wasPressedThisFrame, t.press.wasReleasedThisFrame,
                t.position.ReadValue(), pressed > (CameraController.DownThisUpdate(t) ? 1 : 0), t.delta.ReadValue());
            if (tapAt.HasValue) OnPress(tapAt.Value);   // a tap selects where the finger landed, like a click
        }

        /// <summary>A left click at <paramref name="pos"/> (screen pixels): ignored over the UI (the same IsOverUi the camera
        /// uses), otherwise the hex under the pointer is raised as HexClicked. Returns whether a hex was picked.</summary>
        public bool HandleClick(Vector2 pos)
        {
            var overUi = IsOverUi != null && IsOverUi(pos);
            Boot.DiagLog.NavLine(FormattableString.Invariant($"map click at ({pos.x:0}, {pos.y:0}) overUi={overUi}"));
            if (overUi || HexAt(pos, false, true, out var cityPill) is not (int q, int r)) return false;   // the UI asked once per press (MapPressTests)
            Map!.Select(q, r);                                          // the map marks the hex the info panel describes
            HexClicked?.Invoke(q, r, cityPill);
            return true;
        }

        /// <summary>One right-button event: its press, a pointer move or its release, at the event's own position. A release
        /// with the press never past TapPress.SlopPixels queues a right click where it was pressed (handled in Update, as
        /// presses are); a press that went farther was the camera's drag pan.</summary>
        internal void RightStep(bool pressed, bool released, Vector2 position)
        {
            if (rightTap.Step(pressed, released, position, false) is Vector2 at) rightClicks.Enqueue(at);
        }

        /// <summary>A right click at <paramref name="pos"/> (screen pixels): ignored over the UI, otherwise the hex under the
        /// pointer is raised as HexOrdered (no outline: it selects nothing). Returns whether a hex was raised.</summary>
        public bool HandleRightClick(Vector2 pos)
        {
            var overUi = IsOverUi != null && IsOverUi(pos);
            Boot.DiagLog.NavLine(FormattableString.Invariant($"map right click at ({pos.x:0}, {pos.y:0}) overUi={overUi}"));
            if (overUi || HexAt(pos, false, true) is not (int q, int r)) return false;
            HexOrdered?.Invoke(q, r);
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
            HexClicked?.Invoke(q, r, false);
            return true;
        }

        /// <summary>The hex a click at <paramref name="pos"/> (screen pixels) would pick, or null over the UI or off the map:
        /// a pill's city hex (PillHex), else the hex of the unit token drawn under the pointer (UnitLayer.TokenAtScreen), else
        /// the hex of the terrain under the pointer. GameRoot also asks it for the hover.</summary>
        public (int q, int r)? HexAt(Vector2 pos) => HexAt(pos, IsOverUi != null && IsOverUi(pos), true);

        /// <summary>The hex a click at <paramref name="pos"/> would pick were no UI there (HexAt without the UI check).
        /// GameRoot asks it for a click on the unit card over a lit hex (#1617).</summary>
        public (int q, int r)? HexUnder(Vector2 pos) => HexAt(pos, false, true);

        (int q, int r)? HexAt(Vector2 pos, bool overUi, bool pills) => HexAt(pos, overUi, pills, out _);

        (int q, int r)? HexAt(Vector2 pos, bool overUi, bool pills, out bool cityPill)
        {
            cityPill = false;
            if (overUi) return null;                                    // the UI is asked first, whatever else is missing
            if (Map == null || Map.Terrain == null) return null;
            if (pills && PillHex?.Invoke(pos) is (int, int) pill) { cityPill = true; return pill; }
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
