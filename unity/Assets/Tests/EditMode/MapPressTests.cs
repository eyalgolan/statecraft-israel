#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

/// <summary>Playtest 2, P2-4: map clicks under same-frame input.</summary>
public class MapPressTests
{
    /// <summary>MapPointer evaluates every press at its own position, in order, even when several land in one frame.</summary>
    [Test]
    public void EveryMapPressIsCheckedAtItsOwnPosition()
    {
        var go = new GameObject("P24Pointer");
        try
        {
            var pointer = go.AddComponent<MapPointer>();
            var asked = new List<Vector2>();
            pointer.IsOverUi = p => { asked.Add(p); return p.y > 1000; };   // the top of the screen is UI
            pointer.OnPress(new Vector2(1908, 1151));
            pointer.OnPress(new Vector2(1630, 317));
            pointer.ProcessPresses();
            CollectionAssert.AreEqual(new[] { new Vector2(1908, 1151), new Vector2(1630, 317) }, asked, "both presses, each where it happened");
            asked.Clear();
            pointer.ProcessPresses();
            Assert.IsEmpty(asked, "a press is handled once");
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>Review of the touchscreen tap rule: a press released inside the slop is a tap, at where the finger landed.</summary>
    [Test]
    public void ATapSelectsWhereTheFingerLanded()
    {
        var tap = new TapPress();
        Assert.IsNull(tap.Step(true, false, new Vector2(1150, 501), false), "nothing while the finger is down");
        Assert.AreEqual((Vector2?)new Vector2(1150, 501), tap.Step(false, true, new Vector2(1152, 500), false),
            "a 2-pixel wiggle is still a tap, and it selects where the finger landed");
    }

    /// <summary>A finger that wanders past the slop drags the camera and never selects - even if it comes back.</summary>
    [Test]
    public void ADragIsNeverATapEvenOneThatComesBack()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(400, 400), false);
        tap.Step(false, false, new Vector2(600, 400), false);   // far past the slop
        Assert.IsNull(tap.Step(false, true, new Vector2(401, 399), false), "back at the start is still a drag");
    }

    /// <summary>A pinch is never a tap: with the thumb held still, lifting it after the other finger spread must not select.</summary>
    [Test]
    public void APinchIsNeverATapEvenWithOneFingerHeldStill()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(1150, 501), false);    // the thumb lands
        tap.Step(false, false, new Vector2(1150, 501), true);    // a second finger goes down; the thumb stays put
        Assert.IsNull(tap.Step(false, true, new Vector2(1150, 501), true), "lifting the thumb after a pinch is not a tap");
    }

    /// <summary>A finger that goes down only after the watched one lifted changes nothing.</summary>
    [Test]
    public void ASecondFingerAfterTheTapChangesNothing()
    {
        var tap = new TapPress();
        tap.Step(true, false, new Vector2(300, 300), false);
        Assert.AreEqual((Vector2?)new Vector2(300, 300), tap.Step(false, true, new Vector2(300, 300), false));
    }

    /// <summary>The travel rule with no devices (#1661 item 4, owner's ruling of 26 October 2026): a press whose
    /// whole journey happens inside one frame is a drag, because <c>Step</c> is now told how far the finger went.
    /// Position and start are the same sample here, so without the travel the slop would see nothing move.</summary>
    [Test]
    public void APressThatTravelsPastTheSlopInOneFrameIsNotATap()
    {
        var tap = new TapPress();
        var at = new Vector2(600, 400);
        Assert.IsNull(tap.Step(true, true, at, false, new Vector2(200, 0)),
            "200 px of travel inside the frame is a drag, even though position equals start");
    }

    /// <summary>The other side of the same rule: travel inside the slop still taps, so the threshold is the slop and
    /// not merely "any travel at all".</summary>
    [Test]
    public void APressThatTravelsInsideTheSlopInOneFrameStillTaps()
    {
        var tap = new TapPress();
        var at = new Vector2(600, 400);
        Assert.AreEqual((Vector2?)at, tap.Step(true, true, at, false, new Vector2(20, 0)),
            "20 px is inside TapPress.SlopPixels (30), so it is still a tap at where the finger landed");
    }
}

/// <summary>#1654 review (blocker): a mouse press is a click or the camera's drag pan, never both. That was first
/// settled for the right button (a click is an order); since #998 the left button drags the same way, and its tests live
/// here beside the right ones rather than in a class of their own. Real mouse events go through the Input System test
/// fixture into MapPointer (its own left- and right-button actions) and the project's Camera actions into
/// CameraController.Tick, wired as GameRoot wires them (HoldsMouseDrag = RightTapCandidate || LeftTapCandidate). The UI
/// recorder stands in for the map: it is asked once per click or order. Edit mode sends no OnEnable, so the test sends
/// it.</summary>
public class RightClickOrPanTests : InputTestFixture
{
    static void Send(Object o, string message) =>
        o.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(o, null);

    sealed class Rig : System.IDisposable
    {
        public readonly GameObject Go;
        public readonly CameraRig View;
        public readonly CameraController Controller;
        public readonly MapPointer Pointer;
        public readonly List<Vector2> Clicks = new List<Vector2>();
        public Rig()
        {
            Go = new GameObject("RightClickOrPan", typeof(UnityEngine.Camera));
            View = Go.AddComponent<CameraRig>();
            View.LookAt = new Vector3(500000, 0, 500000); View.Distance = 100000f;
            Controller = Go.AddComponent<CameraController>();
            Controller.Bounds = new Rect(0, 0, 1000000, 1000000);   // far larger than the view: no view clamp here
            Pointer = Go.AddComponent<MapPointer>();
            var clicks = Clicks;
            Pointer.IsOverUi = p => { clicks.Add(p); return true; };   // asked once per right click; true: no map needed
            var pointer = Pointer;
            Controller.HoldsMouseDrag = () => pointer.RightTapCandidate || pointer.LeftTapCandidate;   // as GameRoot wires it
            Send(Controller, "Awake"); Send(Controller, "OnEnable"); Send(Pointer, "OnEnable");
        }
        public void Frame() { Controller.Tick(0.016f); Pointer.ProcessPresses(); }
        public void Dispose() { Send(Pointer, "OnDisable"); Send(Controller, "OnDisable"); Object.DestroyImmediate(Go); }
    }

    Mouse mouse = null!;

    [SetUp]
    public void Devices()
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        mouse = InputSystem.AddDevice<Mouse>();
    }

    /// <summary>Drags the right button from <paramref name="from"/> through <paramref name="path"/> (one frame a step) and
    /// releases it at the last point. Returns where the look-at point was while the button was still held.</summary>
    Vector3 Drag(Rig rig, Vector2 from, params Vector2[] path)
    {
        Set(mouse.position, from);
        rig.Frame();
        Press(mouse.rightButton);
        rig.Frame();
        var at = from;
        foreach (var p in path) { Move(mouse.position, p, p - at); at = p; rig.Frame(); }
        var held = rig.View.LookAt;
        Release(mouse.rightButton);
        rig.Frame();
        return held;
    }

    [Test]
    public void AShortRightDragIsAClickAndNeverPans()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(700, 400);
        Drag(rig, from, from + new Vector2(10, 0), from + new Vector2(20, 0));
        Assert.AreEqual(start, rig.View.LookAt, "a 20 px right drag does not pan");
        CollectionAssert.AreEqual(new[] { from }, rig.Clicks, "it is one right click, where it was pressed");
    }

    [Test]
    public void ARightDragAtTheSlopIsStillAClickAndOnePastItOnlyPans()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(700, 400);
        Drag(rig, from, from + new Vector2(TapPress.SlopPixels, 0));
        Assert.AreEqual(start, rig.View.LookAt, "at the slop: no pan");
        CollectionAssert.AreEqual(new[] { from }, rig.Clicks, "at the slop: a click");
        rig.Clicks.Clear();
        var held = Drag(rig, from, from + new Vector2(TapPress.SlopPixels + 1, 0));
        Assert.AreNotEqual(start, held, "past the slop: the camera pans");
        Assert.IsEmpty(rig.Clicks, "past the slop: no click");
    }

    [Test]
    public void ARightDragOutAndBackPansAndIsNoClick()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(700, 400);
        var held = Drag(rig, from, from + new Vector2(100, 0), from + new Vector2(1, 0));
        Assert.AreNotEqual(start, held, "the drag panned");
        Assert.IsEmpty(rig.Clicks, "coming back does not make it a click");
    }

    [Test]
    public void ARightDragOutAndBackInsideOneFrameIsNoClick()
    {
        using var rig = new Rig();
        var from = new Vector2(700, 400);
        Set(mouse.position, from);
        rig.Frame();
        // one slow frame: press, out past the slop, back and release, all before the next Update
        Press(mouse.rightButton);
        Move(mouse.position, from + new Vector2(200, 0));
        Move(mouse.position, from);
        Release(mouse.rightButton);
        rig.Frame();
        Assert.IsEmpty(rig.Clicks, "each event is stepped at its own position: the excursion makes it a drag");
    }

    /// <summary>Drags the left button, the same shape as <see cref="Drag"/> does the right one.</summary>
    Vector3 LeftDrag(Rig rig, Vector2 from, params Vector2[] path)
    {
        Set(mouse.position, from);
        rig.Frame();
        Press(mouse.leftButton);
        rig.Frame();
        var at = from;
        foreach (var p in path) { Move(mouse.position, p, p - at); at = p; rig.Frame(); }
        var held = rig.View.LookAt;
        Release(mouse.leftButton);
        rig.Frame();
        return held;
    }

    /// <summary>#998, owner's ruling of 22 October 2026: dragging the map with the left button pans it once the
    /// pointer has moved more than a few pixels, and the drag selects nothing. Today the left button selects on its
    /// press, so a drag both selects the hex it started on and leaves the map still.</summary>
    [Test]
    public void ALeftDragPastTheSlopPansAndSelectsNothing()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(600, 400);
        LeftDrag(rig, from, from + new Vector2(120, 0), from + new Vector2(240, 0));
        Assert.IsEmpty(rig.Clicks, "a left drag selects nothing");
        Assert.AreNotEqual(start, rig.View.LookAt, "and the map pans with it");
    }

    /// <summary>The other half of the same ruling: a plain left click still selects, and does not nudge the map. It
    /// selects on the release now rather than the press, because a press cannot be known to be a drag until it moves;
    /// the click is still queued per press, so each physical click acts exactly once (docs/m3-rulings.md, #1617).</summary>
    [Test]
    public void ALeftClickInsideTheSlopStillSelectsAndDoesNotPan()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var at = new Vector2(600, 400);
        LeftDrag(rig, at, at + new Vector2(4, -3));
        CollectionAssert.AreEqual(new[] { at }, rig.Clicks, "a click inside the slop selects where it went down");
        Assert.AreEqual(start, rig.View.LookAt, "and the map does not move");
    }

    /// <summary>#1791 review: docs/m3-rulings.md:74's per-press guarantee, restated on the new code path. Two physical
    /// left clicks inside one frame - press, release, press, release before a single <c>Frame()</c> - are two clicks,
    /// each at its own position. The left click is queued from the release callback now, so the guarantee rests on the
    /// queue rather than on one sampled position per frame.</summary>
    [Test]
    public void TwoLeftClicksInsideOneFrameAreTwoClicksAtTheirOwnPositions()
    {
        using var rig = new Rig();
        var first = new Vector2(600, 400);
        var second = new Vector2(300, 200);
        Set(mouse.position, first);
        rig.Frame();
        Press(mouse.leftButton);
        Release(mouse.leftButton);
        Move(mouse.position, second);
        Press(mouse.leftButton);
        Release(mouse.leftButton);
        rig.Frame();
        CollectionAssert.AreEqual(new[] { first, second }, rig.Clicks,
            "each physical click acts exactly once, at its own position (docs/m3-rulings.md:74)");
    }

    /// <summary>The left twin of <see cref="ARightDragOutAndBackInsideOneFrameIsNoClick"/>: a press that goes out past
    /// the slop and comes back inside one frame is a drag, not a click, because every move is stepped at its own
    /// position rather than only the last one being sampled.</summary>
    [Test]
    public void ALeftDragOutAndBackInsideOneFrameIsNoClick()
    {
        using var rig = new Rig();
        var from = new Vector2(700, 400);
        Set(mouse.position, from);
        rig.Frame();
        // one slow frame: press, out past the slop, back and release, all before the next frame
        Press(mouse.leftButton);
        Move(mouse.position, from + new Vector2(200, 0));
        Move(mouse.position, from);
        Release(mouse.leftButton);
        rig.Frame();
        Assert.IsEmpty(rig.Clicks, "each event is stepped at its own position: the excursion makes it a drag");
    }

    /// <summary>#1791 review: until #998 a left drag could never pan, so dragging a panel's scrollbar was safe by
    /// construction. Now it relies on the <c>dragFromUi</c> latch (<c>Gather</c>, which answers the UI question once on
    /// the button's way down, and <c>Step</c>, which zeroes the drag when it is set), and nothing pinned that for the
    /// left button.
    /// <para>The second half is what keeps this from passing vacuously: the identical drag begun on the map does pan,
    /// so the first half is the latch holding, not the left button failing to pan at all.</para></summary>
    [Test]
    public void ALeftDragStartingOverTheUiNeverPans()
    {
        using var rig = new Rig();
        rig.Controller.IsOverUi = p => p.y > 1000;   // the top of the screen is a panel
        var start = rig.View.LookAt;
        var onUi = new Vector2(600, 1200);
        LeftDrag(rig, onUi, onUi + new Vector2(120, 0), onUi + new Vector2(240, 0));
        Assert.AreEqual(start, rig.View.LookAt, "a left drag that began on a panel does not pan the map");
        var onMap = new Vector2(600, 400);
        LeftDrag(rig, onMap, onMap + new Vector2(120, 0), onMap + new Vector2(240, 0));
        Assert.AreNotEqual(start, rig.View.LookAt, "the identical drag begun on the map does pan");
    }

    // Extends Ella's #1791 left-drag coverage to the camera-owned press observer (#1808).
    [TestCase(30)]
    [TestCase(31)]
    public void ALeftDragUsesThePressOriginAcrossInputUpdates(int travel)
    {
        using var rig = new Rig();
        rig.View.Yaw = 0;
        var start = rig.View.LookAt;
        var from = new Vector2(600, 400);
        Set(mouse.position, from); rig.Frame();
        Press(mouse.leftButton);
        Move(mouse.position, from + new Vector2(10, 0), new Vector2(10, 0));
        Move(mouse.position, from + new Vector2(travel, 0), new Vector2(travel - 10, 0));
        rig.Frame(); // two input updates, no camera frame since the press
        var expected = start.x - (travel > 30 ? 120f : 0f);
        Assert.That(rig.View.LookAt.x, Is.EqualTo(expected).Within(0.1f), "only travel past 30 px pans");
        Assert.AreEqual(start.z, rig.View.LookAt.z);
        rig.Frame();
        Assert.That(rig.View.LookAt.x, Is.EqualTo(expected).Within(0.1f), "travel is consumed once");
        Release(mouse.leftButton); rig.Frame();
        Assert.That(rig.View.LookAt.x, Is.EqualTo(expected).Within(0.1f), "no release catch-up");
        if (travel == 30) CollectionAssert.AreEqual(new[] { from }, rig.Clicks);
        else Assert.IsEmpty(rig.Clicks);
    }

    [TestCase(10)]
    [TestCase(31)]
    public void ALeftReleaseAndRepressBeforeTheCameraFrameStartsANewGesture(int travel)
    {
        using var rig = new Rig();
        rig.View.Yaw = 0;
        var start = rig.View.LookAt;
        var a = new Vector2(400, 400);
        Set(mouse.position, a); rig.Frame();
        Press(mouse.leftButton);
        Move(mouse.position, a + new Vector2(50, 0), new Vector2(50, 0)); rig.Frame();
        Assert.That(rig.View.LookAt.x, Is.EqualTo(start.x - 20 * 120).Within(0.1f), "A pans its excess");
        var afterA = rig.View.LookAt;
        Release(mouse.leftButton);
        var b = new Vector2(700, 400);
        Set(mouse.position, b); Press(mouse.leftButton);
        Move(mouse.position, b + new Vector2(travel, 0), new Vector2(travel, 0)); rig.Frame();
        var expected = afterA.x - (travel > 30 ? 120f : 0f);
        Assert.That(rig.View.LookAt.x, Is.EqualTo(expected).Within(0.1f), "B has its own origin and onset");
        Assert.AreEqual(afterA.z, rig.View.LookAt.z);
        Release(mouse.leftButton); rig.Frame();
        Assert.That(rig.View.LookAt.x, Is.EqualTo(expected).Within(0.1f));
        if (travel == 10) CollectionAssert.AreEqual(new[] { b }, rig.Clicks);
        else Assert.IsEmpty(rig.Clicks);
    }

    [Test]
    public void ALeftUiOriginRemainsBlockedAfterLeavingBeforeTheCameraFrame()
    {
        using var rig = new Rig();
        rig.View.Yaw = 0;
        rig.Controller.IsOverUi = p => p.x < 500;
        var start = rig.View.LookAt;
        Set(mouse.position, new Vector2(400, 400)); rig.Frame();
        Press(mouse.leftButton);
        Move(mouse.position, new Vector2(600, 400), new Vector2(200, 0)); rig.Frame();
        Assert.AreEqual(start, rig.View.LookAt, "the press began on UI, even though the frame ends on the map");
        Release(mouse.leftButton); rig.Frame();
        Press(mouse.leftButton);
        Move(mouse.position, new Vector2(631, 400), new Vector2(31, 0)); rig.Frame();
        Assert.That(rig.View.LookAt.x, Is.EqualTo(start.x - 120f).Within(0.1f), "a new map press clears the latch and pans one pixel");
        Assert.AreEqual(start.z, rig.View.LookAt.z);
        Release(mouse.leftButton); rig.Frame();
        Assert.IsEmpty(rig.Clicks);
    }
}
