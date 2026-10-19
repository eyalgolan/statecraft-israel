using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInputTests
{
    static CameraRig NewRig()
    {
        var go = new GameObject("RigTest", typeof(Camera));
        var rig = go.AddComponent<CameraRig>();
        rig.LookAt = new Vector3(500000, 0, 500000); rig.Distance = 100000f;
        return rig;
    }

    /// <summary>A map far larger than the view, so the input tests below never meet the view clamp (issue #174), which
    /// the edge tests further down check on a map the size of the real one.</summary>
    static readonly Rect Map = new Rect(0, 0, 1000000, 1000000);
    /// <summary>The real terrain's size (heightmap.json: 161.5 x 431.2 km).</summary>
    static readonly Rect RealMap = new Rect(0, 0, 161529f, 431223f);

    [Test]
    public void TheCameraActionMapIsInTheProjectWideAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var map = asset.FindActionMap("Camera", true);
        foreach (var name in new[] { "Pan", "DragPan", "Zoom", "Orbit" }) Assert.IsNotNull(map.FindAction(name), name);
        Assert.AreEqual(8, map.FindAction("Pan").bindings.Count(b => b.isPartOfComposite && b.path.StartsWith("<Keyboard>/")));
        Assert.AreEqual(3, map.FindAction("DragPan").bindings.Count);
        Assert.AreEqual(1, map.FindAction("Zoom").bindings.Count);
        Assert.AreEqual(3, map.FindAction("Orbit").bindings.Count);
        var added = new System.Collections.Generic.List<InputDevice>();
        if (Keyboard.current == null) added.Add(InputSystem.AddDevice<Keyboard>());
        if (Mouse.current == null) added.Add(InputSystem.AddDevice<Mouse>());
        try
        {
            foreach (var a in map.actions)
                foreach (var b in a.bindings.Where(b => !b.isComposite))
                {
                    using var found = InputSystem.FindControls(b.path);
                    Assert.Greater(found.Count, 0, a.name + " " + b.path + " resolves to no control");
                }
        }
        finally { foreach (var d in added) InputSystem.RemoveDevice(d); }
        Assert.IsNotNull(asset.FindActionMap("UI", true).FindAction("Click"), "UI Toolkit needs the UI map's actions");
    }

    [Test]
    public void APinchZoomsByTheFingersDistanceAtTheTouchpadRate()
    {
        // the fingers spread 100 px in one frame: one notch in (PixelsPerNotch, the touchpad's rate)
        Assert.AreEqual(1f, CameraController.PinchNotches(new Vector2(0, 0), Vector2.zero, new Vector2(200, 0), new Vector2(100, 0)), 1e-4);
        // they close 100 px: one notch out
        Assert.AreEqual(-1f, CameraController.PinchNotches(new Vector2(0, 0), Vector2.zero, new Vector2(100, 0), new Vector2(-100, 0)), 1e-4);
        // both fingers still (or moving together, the distance unchanged): no zoom
        Assert.AreEqual(0f, CameraController.PinchNotches(new Vector2(0, 0), Vector2.zero, new Vector2(100, 0), Vector2.zero), 1e-4);
        Assert.AreEqual(0f, CameraController.PinchNotches(new Vector2(50, 0), new Vector2(10, 0), new Vector2(160, 0), new Vector2(10, 0)), 1e-4);
    }

    [Test]
    public void PanMovesTheLookAtNorthAtYawZero()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up }, 0.5f, Map);
            Assert.AreEqual(500000f, rig.LookAt.x, 1e-2);
            Assert.AreEqual(500000f + 0.4f * 100000f, rig.LookAt.z, 1e-1);   // 0.8 distances per second for half a second
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void ZoomIsClampedAndOrbitTurnsTheView()
    {
        var rig = NewRig();
        try
        {
            for (int i = 0; i < 100; i++) CameraController.Step(rig, new CameraInput { Zoom = 3 }, 0.016f, Map);
            Assert.AreEqual(CameraController.MinDistance, rig.Distance, 1e-3);
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.5f, Map);
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.AreEqual(30f, rig.transform.eulerAngles.y, 1e-3);
            CameraController.Step(rig, new CameraInput { Pan = Vector2.left }, 1000f, Map);
            // Clamped so the view's west edge is the map's (issue #174), not the look-at point.
            Assert.AreEqual(Map.xMin, rig.LookAt.x + CameraController.ViewFootprint(rig).xMin, 1f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void PointerInputIsIgnoredOverTheUi()
    {
        var rig = NewRig();
        try
        {
            var before = (rig.LookAt, rig.Distance, rig.Yaw);
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50, OverUi = true, DragFromUi = true }, 1f, Map);
            Assert.AreEqual(before, (rig.LookAt, rig.Distance, rig.Yaw));
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50 }, 1f, Map);   // control: same input off the UI moves the rig
            Assert.AreNotEqual(before.Distance, rig.Distance);
            Assert.AreNotEqual(before.LookAt, rig.LookAt);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void KeysStillPanAndOrbitWhileThePointerIsOverTheUi()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up, Orbit = 1, Zoom = 1, OverUi = true }, 0.5f, Map);
            Assert.AreEqual(100000f, rig.Distance, 1e-3);                 // the wheel was over the UI
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.Greater(rig.LookAt.z, 500000f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void MaxDistanceClampsAndYawWraps()
    {
        var rig = NewRig();
        try
        {
            for (int i = 0; i < 200; i++) CameraController.Step(rig, new CameraInput { Zoom = -3 }, 0.016f, Map);
            Assert.AreEqual(CameraController.MaxDistance, rig.Distance, 1e-3);
            rig.Yaw = 350f;
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.5f, Map);   // +30
            Assert.AreEqual(20f, rig.Yaw, 1e-3);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void PanFollowsTheYaw()
    {
        var rig = NewRig();
        try
        {
            rig.Yaw = 90f;   // looking east: forward is +x
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up }, 0.5f, Map);
            Assert.AreEqual(500000f + 0.4f * 100000f, rig.LookAt.x, 1f);
            Assert.AreEqual(500000f, rig.LookAt.z, 1f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void TwoHalfStepsEqualOneFullStep()
    {
        var a = NewRig(); var b = NewRig();
        try
        {
            var inp = new CameraInput { Pan = new Vector2(1, 1), Orbit = 1, Zoom = 0 };
            a.Yaw = b.Yaw = 0;
            CameraController.Step(a, inp, 0.1f, Map);
            CameraController.Step(b, inp, 0.05f, Map); CameraController.Step(b, inp, 0.05f, Map);
            Assert.AreEqual(a.Yaw, b.Yaw, 1e-3);
            // the pan direction turns with the yaw between the half steps (3 degrees here: yaw updates before pan), so allow 0.5% of the distance
            Assert.AreEqual(a.LookAt.x, b.LookAt.x, 500f);
            Assert.AreEqual(a.LookAt.z, b.LookAt.z, 500f);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }

    [Test]
    public void PanAloneIsFrameRateIndependent()
    {
        var a = NewRig(); var b = NewRig();
        try
        {
            var inp = new CameraInput { Pan = new Vector2(1, 1) };
            CameraController.Step(a, inp, 0.1f, Map);
            CameraController.Step(b, inp, 0.05f, Map); CameraController.Step(b, inp, 0.05f, Map);
            Assert.AreEqual(a.LookAt.x, b.LookAt.x, 1f);
            Assert.AreEqual(a.LookAt.z, b.LookAt.z, 1f);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }

    [Test]
    public void AStalledFrameMovesNoMoreThanTheCap()
    {
        var a = NewRig(); var b = NewRig();
        try
        {
            var inp = new CameraInput { Pan = Vector2.up, Orbit = 1 };
            CameraController.Step(a, inp, CameraController.ClampDt(5f), Map);
            CameraController.Step(b, inp, CameraController.MaxFrameSeconds, Map);
            Assert.AreEqual(b.LookAt, a.LookAt);
            Assert.AreEqual(b.Yaw, a.Yaw, 1e-4);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }

    /// <summary>The view's ground on the map at the rig's current pose: [look-at + footprint] inside the map, within tol.</summary>
    static void AssertViewOnMap(CameraRig rig, Rect map, string what, float tol = 1f)
    {
        var f = CameraController.ViewFootprint(rig);
        Assert.GreaterOrEqual(rig.LookAt.x + f.xMin, map.xMin - tol, what + ": west");
        Assert.LessOrEqual(rig.LookAt.x + f.xMax, map.xMax + tol, what + ": east");
        Assert.GreaterOrEqual(rig.LookAt.z + f.yMin, map.yMin - tol, what + ": south");
        Assert.LessOrEqual(rig.LookAt.z + f.yMax, map.yMax + tol, what + ": north");
    }

    /// <summary>Issue #174: panning past a corner at gameplay zoom stops where the view's edges meet the map's, at every
    /// yaw and with the top bar's inset, so nothing past the map's edge (since #36 the frame) shows. (The look-at clamp alone left 3/4 of the view on it.)</summary>
    [Test]
    public void PanningPastACornerKeepsTheViewOnTheMap()
    {
        var rig = NewRig();
        try
        {
            var cam = rig.GetComponent<Camera>(); cam.aspect = 16f / 9f; cam.fieldOfView = CameraRig.Fov;   // the game's view (Init is not run here)
            foreach (var d in new[] { CameraController.MinDistance, 30000f, 60000f })
                foreach (var yaw in new[] { 0f, 45f, 90f, 200f })
                    foreach (var inset in new[] { 0f, 0.1f })
                        foreach (var dir in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
                        {
                            rig.LookAt = new Vector3(RealMap.center.x, 0, RealMap.center.y); rig.Distance = d; rig.Yaw = yaw; rig.TopInset = inset;
                            var what = $"{d / 1000f} km, yaw {yaw}, inset {inset}, pan {dir}";
                            for (int i = 0; i < 120; i++) CameraController.Step(rig, new CameraInput { Pan = dir }, 0.5f, RealMap);
                            AssertViewOnMap(rig, RealMap, what);
                            // and it got there: the view touches the map's edge on at least one side
                            var f = CameraController.ViewFootprint(rig);
                            float gap = Mathf.Min(Mathf.Abs(rig.LookAt.x + f.xMin - RealMap.xMin), Mathf.Abs(rig.LookAt.x + f.xMax - RealMap.xMax),
                                Mathf.Abs(rig.LookAt.z + f.yMin - RealMap.yMin), Mathf.Abs(rig.LookAt.z + f.yMax - RealMap.yMax));
                            Assert.Less(gap, 1f, what + ": the pan reached the edge");
                        }
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    /// <summary>Issue #174: where the view is wider than the map (the overview), it is centred on the map on that axis, and
    /// zooming out at the edge moves the view smoothly rather than jumping (the clamp narrows to the centre continuously).</summary>
    [Test]
    public void AViewWiderThanTheMapIsCentredAndZoomingOutNeverJumps()
    {
        var rig = NewRig();
        try
        {
            var cam = rig.GetComponent<Camera>(); cam.aspect = 16f / 9f; cam.fieldOfView = CameraRig.Fov;   // the game's view (Init is not run here)
            rig.LookAt = new Vector3(RealMap.xMax, 0, RealMap.center.y); rig.Distance = CameraController.MinDistance; rig.Yaw = 0;
            CameraController.Step(rig, new CameraInput { Pan = Vector2.right }, 10f, RealMap);
            float worst = 0f;
            while (rig.Distance < CameraController.MaxDistance - 1f)
            {
                var before = rig.LookAt;
                CameraController.Step(rig, new CameraInput { Zoom = -0.2f, Pan = Vector2.right }, 0.016f, RealMap);
                // a frame's pan alone moves 0.8 * 0.016 distances; the clamp may add the view's growth, a few % of it
                worst = Mathf.Max(worst, (rig.LookAt - before).magnitude / rig.Distance);
                if (CameraController.ViewFootprint(rig).width <= RealMap.width)
                    AssertViewOnMap(rig, RealMap, $"{rig.Distance / 1000f:0} km");
            }
            Assert.Less(worst, 0.05f, "no jump while zooming out at the edge");
            var f = CameraController.ViewFootprint(rig);
            Assert.Greater(f.width, RealMap.width, "at the overview the view is wider than the map");
            Assert.AreEqual(RealMap.center.x, rig.LookAt.x + f.center.x, 1f, "so it is centred on the map");
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    /// <summary>Issue #174: over low ground the look-at point stays on y = 0 (the approved framing); over ground high enough
    /// to come within MinClearanceShare of the camera, it is lifted just enough, and dropped again on the way back down.</summary>
    [Test]
    public void TheCameraIsLiftedOnlyOverHighGround()
    {
        var rig = NewRig();
        try
        {
            rig.Distance = CameraController.MinDistance;
            float height = rig.Distance * Mathf.Sin(CameraRig.Pitch * Mathf.Deg2Rad);
            float groundY = 0f;
            System.Func<Vector2, float> ground = _ => groundY;
            foreach (var g in new[] { 0f, 0.5f, 0.79f, 0.9f, 1.3f, 0.3f })
            {
                groundY = g * height;
                CameraController.Step(rig, new CameraInput(), 0f, Map, ground);
                float clearance = rig.transform.position.y - groundY;
                if (g <= 1f - CameraController.MinClearanceShare)
                    Assert.AreEqual(0f, rig.LookAt.y, 1e-3, $"ground at {g} of the camera's height: no lift");
                else
                    Assert.AreEqual(CameraController.MinClearanceShare * height, clearance, 1f, $"ground at {g}: lifted to the least clearance");
            }
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    /// <summary>Review B-I1: with the ground's top given, the near side of the view is bounded by the corner rays' hits on
    /// that plane, not by the point below the camera: at 60 km, pushed to the south edge, the camera may stand behind the
    /// edge, and the strip kept off screen is a few km (it was 19-20 km). With the camera below the top plane (the hero
    /// zoom over high ground) the point below the camera is the bound.</summary>
    [Test]
    public void TheNearSideIsBoundedByTheGroundsTopNotTheCamera()
    {
        var rig = NewRig();
        try
        {
            var cam = rig.GetComponent<Camera>(); cam.aspect = 16f / 9f; cam.fieldOfView = CameraRig.Fov;
            const float top = 8400f;   // about the real terrain's highest point (2367 m + 427 m, at 3x)
            float back = Mathf.Cos(CameraRig.Pitch * Mathf.Deg2Rad);   // the camera is this many distances behind the look-at point
            rig.LookAt = new Vector3(RealMap.center.x, 0, RealMap.center.y); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0;
            for (int i = 0; i < 60; i++) CameraController.Step(rig, new CameraInput { Pan = Vector2.down }, 0.5f, RealMap, _ => 0f, top);
            var f = CameraController.ViewFootprint(rig, top);
            Assert.AreEqual(RealMap.yMin, rig.LookAt.z + f.yMin, 1f, "the view's near side at the south edge");
            Assert.Less(rig.LookAt.z, back * rig.Distance - 10000f, "the camera stands behind the edge: the edge rows stay reachable");
            var flat = CameraController.ViewFootprint(rig);
            Assert.Less(flat.yMin - f.yMin, 5000f, "the high-ground margin at 60 km is a few km");
            Assert.Greater(flat.yMin - f.yMin, 1000f, "and it is there");
            rig.Distance = CameraController.MinDistance; rig.ApplyProjection();   // camera 4.6 km up, below the 8.4 km plane
            Assert.AreEqual(-back * rig.Distance, CameraController.ViewFootprint(rig, top).yMin, 1f, "the point below the camera bounds it");
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    /// <summary>A rig without a camera keeps the old rule: the look-at point itself stays on the map.</summary>
    [Test]
    public void ARigWithoutACameraClampsTheLookAtPoint()
    {
        var go = new GameObject("BareRig");
        try
        {
            var rig = go.AddComponent<CameraRig>(); rig.LookAt = new Vector3(100, 0, 100); rig.Distance = 6000f;
            Assert.AreEqual(Rect.zero, CameraController.ViewFootprint(rig));
            CameraController.Step(rig, new CameraInput { Pan = Vector2.left }, 10f, RealMap);
            Assert.AreEqual(0f, rig.LookAt.x, 1e-3);
        }
        finally { Object.DestroyImmediate(go); }
    }

    static CameraController NewController(out GameObject go)
    {
        go = new GameObject("CtlTest", typeof(Camera));
        var c = go.AddComponent<CameraController>();
        c.IsOverUi = p => p.x < 100;   // the panel is the left strip
        return c;
    }

    [Test]
    public void NoPointerMeansNotOverTheUi()
    {
        var c = NewController(out var go);
        try
        {
            bool probed = false; c.IsOverUi = _ => { probed = true; return true; };
            var inp = c.Gather(Vector2.zero, Vector2.zero, 120f, 0, false, null);
            Assert.IsFalse(inp.OverUi); Assert.IsFalse(probed);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ADragStartedOverTheMapKeepsPanningOverAPanel()
    {
        var c = NewController(out var go);
        try
        {
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(500, 0));
            var inp = c.Gather(Vector2.zero, Vector2.one, 120f, 0, true, new Vector2(50, 0));   // now over the panel
            Assert.IsFalse(inp.DragFromUi); Assert.IsTrue(inp.OverUi);                          // wheel is still the UI's
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ADragStartedOverTheUiNeverPansTheMap()
    {
        var c = NewController(out var go);
        try
        {
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(50, 0));
            var inp = c.Gather(Vector2.zero, Vector2.one, 0, 0, true, new Vector2(500, 0));     // moved onto the map
            Assert.IsTrue(inp.DragFromUi);
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, false, new Vector2(500, 0));             // released
            Assert.IsFalse(c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(500, 0)).DragFromUi);
        }
        finally { Object.DestroyImmediate(go); }
    }

    // Task 24 (carry 3), measured in headed Chrome on the WebGL build: the Input System's default scroll behaviour
    // (UniformAcrossAllPlatforms) already gives 1 per wheel notch (Chrome's 100 px notch arrived as 1.0, a 300 px burst
    // as 3.0), so one notch must zoom by ZoomPerStep (10%), not by a /120 of it.
    [Test]
    public void OneWheelNotchZoomsByOneStep()
    {
        Assert.AreEqual(UnityEngine.InputSystem.InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms,
            UnityEngine.InputSystem.InputSystem.settings.scrollDeltaBehavior, "the scroll range this controller assumes");
        var c = NewController(out var go);
        try
        {
            Assert.AreEqual(1f, c.Gather(Vector2.zero, Vector2.zero, 1f, 0, false, null).Zoom);
            Assert.AreEqual(-1f, c.Gather(Vector2.zero, Vector2.zero, -1f, 0, false, null).Zoom);
            Assert.AreEqual(3f, c.Gather(Vector2.zero, Vector2.zero, 7f, 0, false, null).Zoom, "bursts are clamped");
            var rig = go.GetComponent<CameraRig>();
            rig.Distance = 120000f;
            CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, 1f, 0, false, null), 0.016f, Map);
            Assert.AreEqual(108000f, rig.Distance, 1f);
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Issue #43 (T45): in WebGL the zoom counts the browser's wheel events itself. Unity's player took the smallest
    /// delta seen since load as one notch, so one 0.5 px touchpad event made every later frame zoom the clamped 27%. A
    /// mouse notch is still one step: 100 px, three lines, one page, or a macOS mouse tick (4.000244140625 px).</summary>
    [Test]
    public void AMouseNotchIsOneStepInEveryMode()
    {
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-100f, CameraController.WheelPixels), 1e-5, "100 px up");
        Assert.AreEqual(-1f, CameraController.WheelEventNotches(100f, CameraController.WheelPixels), 1e-5, "100 px down");
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-3f, CameraController.WheelLines), 1e-5, "3 lines");
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-1f, CameraController.WheelPages), 1e-5, "1 page");
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-4.000244140625f, CameraController.WheelPixels), 1e-5, "macOS tick");
        Assert.AreEqual(-1f, CameraController.WheelEventNotches(8.00048828125f, CameraController.WheelPixels), 1e-5,
            "an accelerated macOS tick (two ticks' worth) is still one notch");
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-13 * 4.000244140625f, CameraController.WheelPixels), 1e-5,
            "a fast-spun macOS tick above the threshold is one notch");
        foreach (var mode in new[] { CameraController.WheelPixels, CameraController.WheelLines, CameraController.WheelPages })
            Assert.AreEqual(float.PositiveInfinity, 1f / CameraController.WheelEventNotches(0f, mode), $"0 in mode {mode} is +0, not -0");
    }

    /// <summary>#528 review: one discrete pixel notch is one step whatever its size. Chrome's synthetic 100 px notch
    /// arrives as 200 px, and display scaling or page zoom gives 80, 120, 125 or 150 px; every pixel event of
    /// MouseNotchMinPixels (50) or more is exactly one notch in its direction (DOM deltaY below zero = in = positive).</summary>
    [Test]
    public void APixelNotchIsOneStepWhateverItsSize()
    {
        foreach (var px in new[] { 50f, 80f, 100f, 120f, 125f, 150f, 200f, 360f })
        {
            Assert.AreEqual(1f, CameraController.WheelEventNotches(-px, CameraController.WheelPixels), 1e-6, $"-{px} px is one notch in");
            Assert.AreEqual(-1f, CameraController.WheelEventNotches(px, CameraController.WheelPixels), 1e-6, $"+{px} px is one notch out");
        }
        Assert.AreEqual(CameraController.MouseNotchMinPixels, 50f, "the threshold these cases assume");
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-50f, CameraController.WheelPixels), 1e-6, "at the threshold: a notch");
        Assert.AreEqual(0.4999f, CameraController.WheelEventNotches(-49.99f, CameraController.WheelPixels), 1e-5, "just below: proportional");
        Assert.AreEqual(-0.49f, CameraController.WheelEventNotches(49f, CameraController.WheelPixels), 1e-6);
        Assert.AreEqual(2f, CameraController.WheelEventNotches(-6f, CameraController.WheelLines), 1e-6, "lines stay as they were: 6 are 2 notches");
        var rig = NewRig();
        try
        {
            rig.Distance = 120000f;
            CameraController.Step(rig, new CameraInput { Zoom = CameraController.WheelNotches(new[] { -200f }, new[] { 0 }, 1) }, 0.016f, Map);
            Assert.AreEqual(108000f, rig.Distance, 1f, "Chrome's synthetic 200 px notch zooms 10%");
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    /// <summary>#528 review: a fast trackpad flick can send events of 50 to 100 px, which MouseNotchMinPixels counts as a
    /// whole notch each, up to twice their length. The over-count is bounded per frame by Gather's clamp: one 60 px event
    /// is one notch (proportional would be 0.6), and five in one frame are five notches, clamped to three (27%).</summary>
    [Test]
    public void A60PxFlickEventIsOneNotchAndAFrameOfThemIsClampedToThree()
    {
        Assert.AreEqual(1f, CameraController.WheelEventNotches(-60f, CameraController.WheelPixels), 1e-6, "one 60 px event");
        var frame = new[] { -60f, -60f, -60f, -60f, -60f };
        Assert.AreEqual(5f, CameraController.WheelNotches(frame, new int[frame.Length], frame.Length), 1e-6, "before the clamp");
        var c = NewController(out var go);
        try
        {
            var inp = c.Gather(Vector2.zero, Vector2.zero, CameraController.WheelNotches(frame, new int[frame.Length], frame.Length), 0, false,
                new Vector2(500, 0));
            Assert.AreEqual(3f, inp.Zoom, 1e-6, "clamped to three notches a frame");
            var rig = go.GetComponent<CameraRig>();
            rig.Distance = 120000f;
            CameraController.Step(rig, inp, 0.016f, Map);
            Assert.AreEqual(120000f * 0.729f, rig.Distance, 1f, "27.1% in that frame, no more");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TouchpadPixelsZoomInProportionAndNothingIsLearned()
    {
        Assert.AreEqual(0.005f, CameraController.WheelEventNotches(-0.5f, CameraController.WheelPixels), 1e-6);
        Assert.AreEqual(0.04f, CameraController.WheelEventNotches(-4f, CameraController.WheelPixels), 1e-6, "4 px is not a tick");
        Assert.AreEqual(0.07f, CameraController.WheelEventNotches(-7f, CameraController.WheelPixels), 1e-6);
        // A swipe of many small deltas, then a mouse notch: the swipe zooms by its length, and the notch is still one step.
        var swipe = new float[] { -0.5f, -1, -2, -4, -6, -8, -8, -8, -6.2f, -4.8f, -3.1f, -1.4f, -0.6f };
        var modes = new int[swipe.Length];
        Assert.AreEqual(-swipe.Sum() / 100f, CameraController.WheelNotches(swipe, modes, swipe.Length), 1e-5);
        Assert.AreEqual(1f, CameraController.WheelNotches(new[] { -100f }, new[] { 0 }, 1), 1e-5);
        Assert.AreEqual(1f, CameraController.WheelNotches(new[] { -3f }, new[] { CameraController.WheelLines }, 1), 1e-5);
        Assert.AreEqual(0.4f, CameraController.WheelNotches(new[] { -40f, -100f }, new[] { 0, 0 }, 1), 1e-5, "only count events");
        // The swipe the browser runs use (tools/headed.py pad_swipe: a ramp, a hold, momentum decaying 12% an event).
        var pad = new List<float> { -0.5f, -1, -2, -4, -6, -8, -8, -8, -8, -8 };
        for (int k = 1; 8 * Mathf.Pow(0.88f, k) >= 0.5f; k++) pad.Add(-(float)System.Math.Round(8 * System.Math.Pow(0.88, k), 2));
        Assert.AreEqual(31, pad.Count);
        Assert.AreEqual(1.082f, CameraController.WheelNotches(pad.ToArray(), new int[pad.Count], pad.Count), 1e-3, "108.2 px is 1.08 notches");
    }

    [Test]
    public void ATouchpadSwipeZoomsSmoothlyAndStaysOffPanels()
    {
        var c = NewController(out var go);
        try
        {
            var rig = go.GetComponent<CameraRig>();
            rig.Distance = 120000f;
            foreach (var px in new[] { 0.5f, 2f, 8f, 8f, 4f, 1f })   // one frame's events each
            {
                var before = rig.Distance;
                var notches = CameraController.WheelNotches(new[] { -px }, new[] { 0 }, 1);
                CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, notches, 0, false, new Vector2(500, 0)), 0.016f, Map);
                Assert.AreEqual(before * Mathf.Pow(0.9f, px / 100f), rig.Distance, 1f, $"{px} px");
            }
            var held = rig.Distance;
            var swipe = new[] { -0.5f, -2f, -8f, -8f, -4f, -1f, -100f };   // a swipe and a mouse notch, over the panel
            var overPanel = CameraController.WheelNotches(swipe, new int[swipe.Length], swipe.Length);
            Assert.Greater(overPanel, 1f);
            CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, overPanel, 0, false, new Vector2(50, 0)), 0.016f, Map);
            Assert.AreEqual(held, rig.Distance, 1e-3, "no zoom over a panel");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Task 24 review M3: zooming in then out by the same notches returns to the same distance.</summary>
    [Test]
    public void ZoomingInThenOutReturnsToTheStart()
    {
        var c = NewController(out var go);
        try
        {
            var rig = go.GetComponent<CameraRig>();
            foreach (var notches in new[] { 1f, 3f, 0.5f })
            {
                rig.Distance = 120000f;
                CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, notches, 0, false, null), 0.016f, Map);
                CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, -notches, 0, false, null), 0.016f, Map);
                Assert.AreEqual(120000f, rig.Distance, 1f, $"{notches} notches in, then out");
            }
            rig.Distance = 120000f;
            CameraController.Step(rig, c.Gather(Vector2.zero, Vector2.zero, -1f, 0, false, null), 0.016f, Map);
            Assert.AreEqual(120000f / 0.9f, rig.Distance, 1f, "one notch out undoes one notch in (10%)");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
