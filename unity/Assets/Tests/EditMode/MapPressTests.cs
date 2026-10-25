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
}

/// <summary>#1654 review (blocker): a right press is a right click (an order) or the camera's drag pan, never both. Real
/// mouse events go through the Input System test fixture into MapPointer (its own right-button actions) and the project's
/// Camera actions into CameraController.Tick, wired as GameRoot wires them (HoldsMouseDrag = RightTapCandidate). The UI
/// recorder stands in for the map: HandleRightClick asks it once per right click. Edit mode sends no OnEnable, so the
/// test sends it.</summary>
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
            Controller.HoldsMouseDrag = () => pointer.RightTapCandidate;   // as GameRoot wires it
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
}
