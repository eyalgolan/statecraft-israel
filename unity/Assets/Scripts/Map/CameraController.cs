#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>One frame of camera input, in the units the actions give: Pan in [-1, 1] per axis (keys), DragPan in
    /// screen pixels (right-button drag), Zoom in wheel notches (positive = in), Orbit in [-1, 1] (Q/E).</summary>
    public struct CameraInput
    {
        public Vector2 Pan, DragPan; public float Zoom, Orbit;
        /// <summary>True while the pointer is over the UI: the camera then ignores pointer input (wheel zoom, drag).</summary>
        public bool OverUi;
        /// <summary>True when the current right-button drag started over the UI (latched on button-down): that drag never pans the map.</summary>
        public bool DragFromUi;
    }

    /// <summary>Pans, zooms and orbits the diorama camera from the project-wide "Camera" action map (Input System only).
    /// While the pointer is over the UI (IsOverUi, wired by the scene) the wheel and the drag belong to the UI; keyboard pan
    /// and orbit still move the camera (plan amendment A4). Pan speed scales with
    /// distance, zoom is clamped to [MinDistance, MaxDistance], and the view stays on the map (issue #174): the look-at
    /// point is held far enough inside the map that the visible ground does not leave it, so the border skirt never fills
    /// the view at gameplay zoom. Where the view is wider than the map (the overview), it is centred on the map instead.
    /// Given the ground's height (Ground), the camera also never sinks into the terrain (MinClearanceShare).</summary>
    [RequireComponent(typeof(CameraRig))]
    public sealed class CameraController : MonoBehaviour
    {
        /// <summary>6 km is the hero zoom (owner ruling 2026-10-11: symbolic-scale settlements need a closer view); 250 km
        /// shows the country.</summary>
        public const float MinDistance = 6000f, MaxDistance = 250000f;
        public const float PanPerSecond = 0.8f;        // distances per second at full key press
        public const float DragPanPerPixel = 0.0012f;  // distances per pixel of drag
        public const float ZoomPerStep = 0.1f;         // fraction of the distance per wheel notch
        public const float OrbitDegreesPerSecond = 60f;
        public const float MaxFrameSeconds = 0.1f;     // a stalled frame (tab in the background) must not fling the camera
        /// <summary>The least height above the ground beneath it the camera keeps, as a share of its height above the
        /// look-at plane (y = 0). Issue #174: the look-at point lies on y = 0, the terrain's lowest point, but high ground is
        /// several km above it at 3x relief (the Golan edge about 6 km), so at the hero zoom (camera 4.6 km up) the camera
        /// sat inside the hills and showed only haze. Below this share the look-at point is lifted just enough; above it
        /// nothing changes. The promo and close-check poses all keep more (hero06judea, the least, 24%).</summary>
        public const float MinClearanceShare = 0.2f;

        /// <summary>Answers "is the pointer over the UI?" for a screen position (y up). Null = never.</summary>
        public Func<Vector2, bool>? IsOverUi;
        /// <summary>The map rectangle on the ground (x, z), in world metres; the view is kept inside it (Step).</summary>
        public Rect Bounds = new Rect(0, 0, float.MaxValue, float.MaxValue);
        /// <summary>The ground's world height at a point (x, z), or null for none (the camera then keeps to y = 0).</summary>
        public Func<Vector2, float>? Ground;
        /// <summary>The highest the ground (and the border skirt, which holds the edge's height) reaches in world y: with it,
        /// ViewFootprint bounds the near side of the view tightly. ShadowFit.GroundY (the default) means flat ground.</summary>
        public float GroundTop = ShadowFit.GroundY;

        CameraRig rig = null!;
        InputAction? pan, dragPan, zoom, orbit;

        void Awake() => rig = GetComponent<CameraRig>();

        void OnEnable()
        {
            var map = InputSystem.actions?.FindActionMap("Camera", throwIfNotFound: true);
            if (map == null) return;
            pan = map.FindAction("Pan", true); dragPan = map.FindAction("DragPan", true);
            zoom = map.FindAction("Zoom", true); orbit = map.FindAction("Orbit", true);
            map.Enable();
        }

        void OnDisable() => InputSystem.actions?.FindActionMap("Camera")?.Disable();

        bool dragFromUi, dragWasDown;

        void Update()
        {
            if (pan == null || dragPan == null || zoom == null || orbit == null) return;
            Vector2? pointer = Pointer.current != null ? Pointer.current.position.ReadValue() : (Vector2?)null;
            var down = Mouse.current != null && Mouse.current.rightButton.isPressed;
            Step(rig, Gather(pan.ReadValue<Vector2>(), dragPan.ReadValue<Vector2>(), zoom.ReadValue<float>(),
                orbit.ReadValue<float>(), down, pointer), ClampDt(Time.unscaledDeltaTime), Bounds, Ground, GroundTop);
        }

        /// <summary>Fits the rig's current pose with no input (review B-m3: a minimap jump goes through the same view fit and
        /// ground clearance as panning, so no frame shows the skirt or the inside of a hill).</summary>
        public void Settle() => Step(GetComponent<CameraRig>(), default, 0f, Bounds, Ground, GroundTop);

        public static float ClampDt(float dt) => Mathf.Min(dt, MaxFrameSeconds);

        /// <summary>Builds one frame of input from raw action values. The wheel is checked against the UI at the pointer every frame;
        /// the drag is checked once, when the button goes down, and that answer holds until it is released. No pointer means not over the UI.</summary>
        public CameraInput Gather(Vector2 panValue, Vector2 dragValue, float zoomRaw, float orbitValue, bool dragDown, Vector2? pointer)
        {
            var over = pointer.HasValue && IsOverUi != null && IsOverUi(pointer.Value);
            if (dragDown && !dragWasDown) dragFromUi = over;
            if (!dragDown) dragFromUi = false;
            dragWasDown = dragDown;
            return new CameraInput
            {
                Pan = panValue, DragPan = dragValue,
                // The Input System's default scroll behaviour (UniformAcrossAllPlatforms) gives 1 per notch on every platform
                // (measured in headed Chrome on WebGL, Task 24: a 100 px notch -> 1.0); clamp bursts.
                Zoom = Mathf.Clamp(zoomRaw, -3f, 3f),
                Orbit = orbitValue, OverUi = over, DragFromUi = dragFromUi,
            };
        }

        /// <summary>Applies one frame of input to the rig and calls rig.Apply(). Pure apart from the rig (and the ground's
        /// height, when given), so it is testable. <paramref name="groundTop"/>: see GroundTop.</summary>
        public static void Step(CameraRig rig, CameraInput input, float dt, Rect bounds, Func<Vector2, float>? ground = null,
            float groundTop = ShadowFit.GroundY)
        {
            var zoom = input.OverUi ? 0f : input.Zoom;
            var drag = input.DragFromUi ? Vector2.zero : input.DragPan;
            rig.Yaw = Mathf.Repeat(rig.Yaw + input.Orbit * OrbitDegreesPerSecond * dt, 360f);
            // (1 - step)^zoom: one notch in is exactly ZoomPerStep closer, and n notches out undo n notches in (Task 24 review M3).
            rig.Distance = Mathf.Clamp(rig.Distance * Mathf.Pow(1f - ZoomPerStep, zoom), MinDistance, MaxDistance);
            var yaw = Quaternion.Euler(0, rig.Yaw, 0);
            var move = input.Pan * (PanPerSecond * dt) - drag * DragPanPerPixel;
            var delta = yaw * new Vector3(move.x, 0, move.y) * rig.Distance;
            var p = rig.LookAt + delta;
            // Issue #174: clamping only the look-at point let up to three quarters of the view (at a corner) show the border
            // skirt, whose land is a flat slab with a hard seam against the terrain. The view's ground footprint around the
            // look-at point depends on the distance, yaw, aspect and top inset, not on where the point is, so it is measured
            // once at this frame's pose and the point is held where the whole footprint stays on the map.
            // With the ground given, the look-at point's height is the lift (Lift) over y = 0, measured afresh each frame; the
            // footprint is measured at the lifted pose (a higher camera sees farther on y = 0), so the fit runs twice. Both
            // passes are pose maths on the projection alone; the transform, tilt-shift and shadow fit are applied once.
            float y = ground == null ? p.y : 0f;
            rig.ApplyProjection();
            for (int pass = 0; pass < (ground == null ? 1 : 2); pass++)
            {
                rig.LookAt = new Vector3(p.x, y, p.z);
                var view = ViewFootprint(rig, groundTop);
                var fitted = new Vector3(Fit(p.x, bounds.xMin, bounds.xMax, view.xMin, view.xMax), 0f,
                    Fit(p.z, bounds.yMin, bounds.yMax, view.yMin, view.yMax));
                if (ground != null) y = Lift(rig, fitted, ground);
                rig.LookAt = new Vector3(fitted.x, y, fitted.z);
            }
            rig.Apply();
        }

        /// <summary>The terrain's world height at (x, z), clamped to its edge outside it (the border skirt holds the edge's
        /// height near the map, so the camera keeps clear of it too).</summary>
        public static Func<Vector2, float> GroundOf(Terrain terrain)
        {
            var origin = terrain.transform.position; var size = terrain.terrainData.size;
            return p => origin.y + terrain.SampleHeight(new Vector3(Mathf.Clamp(p.x, origin.x, origin.x + size.x), 0f,
                Mathf.Clamp(p.y, origin.z, origin.z + size.z)));
        }

        /// <summary>How far to lift a look-at point on y = 0 so the camera keeps MinClearanceShare of its height above the
        /// ground beneath it (0 where it already does).</summary>
        public static float Lift(CameraRig rig, Vector3 lookAt, Func<Vector2, float> ground)
        {
            var rot = Quaternion.Euler(CameraRig.Pitch, rig.Yaw, 0);
            var cam = new Vector3(lookAt.x, 0f, lookAt.z) - rot * Vector3.forward * rig.Distance;
            float height = cam.y, below = ground(new Vector2(cam.x, cam.z));
            return Mathf.Max(0f, below + MinClearanceShare * height - height);
        }

        /// <summary>Corners of the visible map area in viewport units: the top inset's band is covered by the UI.</summary>
        static readonly Vector2[] FootprintCorners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

        /// <summary>The terrain's highest world y (TerrainData.bounds holds the heightmap's actual range); the border skirt
        /// never rises above its edge, so this bounds it too.</summary>
        public static float GroundTopOf(Terrain terrain) => terrain.transform.position.y + terrain.terrainData.bounds.max.y;

        /// <summary>The ground the rig's camera shows, as offsets (x, z) from the look-at point. Every ground point the view
        /// shows lies between y = ShadowFit.GroundY (the terrain's lowest point) and y = <paramref name="groundTop"/> (its
        /// highest), so each corner ray of the visible area (below the top inset) meets it on the segment between its hits on
        /// those two planes (from the camera itself where the camera is below the top plane); seen from above, everything
        /// visible lies in the bounding box of those eight points (the frustum meets each plane in a convex quad). Bounding
        /// only by the y = 0 hits missed high ground on the near side (just behind the south edge the whole 6 km view was
        /// skirt); bounding by the point below the camera instead hid the map's southern 19-20 km at 60 km (review B-I1).
        /// A rig without a camera shows a point (Rect.zero); a corner that does not look down gives an infinite box. Worked
        /// from the rig's pose relative to the look-at point and the camera's projection matrix (aspect, FOV, the inset's
        /// shift; set by ApplyProjection), not Camera.ViewportPointToRay: that unprojects through the 1000 km far plane in
        /// world coordinates, and its rays strayed by up to kilometres (measured: 0.25-4 km).</summary>
        public static Rect ViewFootprint(CameraRig rig, float groundTop = ShadowFit.GroundY)
        {
            var cam = rig.GetComponent<Camera>();
            if (cam == null) return Rect.zero;
            var rot = Quaternion.Euler(CameraRig.Pitch, rig.Yaw, 0);
            var eye = -(rot * Vector3.forward) * rig.Distance;          // the camera, relative to the look-at point
            float camY = rig.LookAt.y + eye.y;
            // NDC x = m00 x / -z - m02, NDC y = m11 y / -z - m12 (CameraRig.ApplyProjection): one unit ahead, the ray through (x, y).
            var m = cam.projectionMatrix;
            float top = 1f - Mathf.Clamp(rig.TopInset, 0f, CameraRig.MaxTopInset);
            float x0 = float.PositiveInfinity, x1 = float.NegativeInfinity, z0 = float.PositiveInfinity, z1 = float.NegativeInfinity;
            foreach (var c in FootprintCorners)
            {
                float nx = 2f * c.x - 1f, ny = 2f * c.y * top - 1f;
                var dir = rot * new Vector3((nx + m[0, 2]) / m[0, 0], (ny + m[1, 2]) / m[1, 1], 1f);
                if (dir.y > -1e-4f)
                    return Rect.MinMaxRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Add(ShadowFit.GroundY); Add(Mathf.Max(groundTop, ShadowFit.GroundY));

                void Add(float planeY)
                {
                    var hit = eye + dir * Mathf.Max(0f, (planeY - camY) / dir.y);
                    x0 = Mathf.Min(x0, hit.x); x1 = Mathf.Max(x1, hit.x); z0 = Mathf.Min(z0, hit.z); z1 = Mathf.Max(z1, hit.z);
                }
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        /// <summary>One axis of the look-at point: within [lo, hi] such that [v + viewMin, v + viewMax] stays in it. A view
        /// wider than the map is centred on it (continuous as the view grows, so zooming out never jumps); an unbounded
        /// view falls back to keeping the point itself on the map.</summary>
        static float Fit(float v, float lo, float hi, float viewMin, float viewMax)
        {
            float a = lo - viewMin, b = hi - viewMax;
            if (a <= b) return Mathf.Clamp(v, a, b);
            return float.IsInfinity(a) || float.IsInfinity(b) || float.IsNaN(a) || float.IsNaN(b) ? Mathf.Clamp(v, lo, hi) : 0.5f * (a + b);
        }
    }
}
