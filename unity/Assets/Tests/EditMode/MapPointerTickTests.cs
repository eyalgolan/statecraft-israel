#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

/// <summary>#1661 items 4, 5 and 6: the real <c>MapPointer.Update</c> path, driven through its test seam
/// (<c>MapPointer.Tick</c>, as <c>CameraController.Tick</c> is, #890). The other fixtures reach
/// <c>ProcessPresses</c> and the handlers underneath <c>Update</c>, so nothing drove the frame itself (item 6).
/// <para>One thing to know when reading these: the fixture's <c>BeginTouch</c>, <c>MoveTouch</c> and <c>EndTouch</c>
/// default to <c>queueEventOnly: false</c>, so each call runs its own <c>InputSystem.Update</c>. A gesture written as
/// three plain calls therefore spans three input updates, and the press edge is gone before <c>Tick</c> runs. To put a
/// whole gesture inside one input update, every call but the last passes <c>queueEventOnly: true</c>.</para></summary>
public class MapPointerTickTests : InputTestFixture
{
    static void Send(Object o, string message) =>
        o.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(o, null);

    sealed class Rig : System.IDisposable
    {
        public readonly GameObject Go;
        public readonly CameraRig View;
        public readonly CameraController Controller;
        public readonly MapPointer Pointer;
        /// <summary>Every position the pointer asked the UI about: one per click, order or tap it acted on.</summary>
        public readonly List<Vector2> Asked = new List<Vector2>();
        public Rig()
        {
            Go = new GameObject("MapPointerTick", typeof(UnityEngine.Camera));
            View = Go.AddComponent<CameraRig>();
            View.LookAt = new Vector3(500000, 0, 500000); View.Distance = 100000f;
            Controller = Go.AddComponent<CameraController>();
            Controller.Bounds = new Rect(0, 0, 1000000, 1000000);
            Pointer = Go.AddComponent<MapPointer>();
            var asked = Asked;
            Pointer.IsOverUi = p => { asked.Add(p); return true; };   // true: the decision is recorded, no map needed
            var pointer = Pointer;
            Controller.HoldsMouseDrag = () => pointer.RightTapCandidate || pointer.LeftTapCandidate;   // as GameRoot wires them
            Controller.HoldsTouchDrag = () => pointer.TouchTapCandidate;
            Send(Controller, "Awake"); Send(Controller, "OnEnable"); Send(Pointer, "OnEnable");
        }
        /// <summary>One frame, in the order the attribute now guarantees: the pointer, then the camera.</summary>
        public void Frame() { Pointer.Tick(); Controller.Tick(0.016f); }
        /// <summary>A tap is decided at the end of the pointer's frame and only <em>enqueued</em> (OnPress), while
        /// ProcessPresses already ran at the top of that same frame, so a tap takes effect on the frame after the finger
        /// lifts. A mouse click does not pay this, since it enqueues from the action callback outside Tick.</summary>
        public void SettleFrame() { Frame(); }
        public void Dispose() { Send(Pointer, "OnDisable"); Send(Controller, "OnDisable"); Object.DestroyImmediate(Go); }
    }

    Mouse mouse = null!;

    [SetUp]
    public void Devices()
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.AddDevice<Touchscreen>();
    }

    // --- item 6: the frame itself -------------------------------------------------------------------------------

    /// <summary>The frame handles a queued press, at its own position, exactly once.</summary>
    [Test]
    public void TheFrameHandlesAQueuedPressOnceThroughTheRealPath()
    {
        using var rig = new Rig();
        rig.Pointer.OnPress(new Vector2(800, 450));
        rig.Frame();
        CollectionAssert.AreEqual(new[] { new Vector2(800, 450) }, rig.Asked, "the frame handled the press at its own position");
        rig.Frame();
        Assert.AreEqual(1, rig.Asked.Count, "a press is handled once, not again on the next frame");
    }

    /// <summary>A tap spanning frames selects where the finger landed, driven through the frame.</summary>
    [Test]
    public void ATapOverTwoFramesSelectsWhereTheFingerLanded()
    {
        using var rig = new Rig();
        var at = new Vector2(600, 300);
        BeginTouch(1, at);
        rig.Frame();
        Assert.IsEmpty(rig.Asked, "nothing while the finger is down");
        EndTouch(1, at + new Vector2(2, -1));
        rig.Frame();
        Assert.IsEmpty(rig.Asked, "the tap is decided on the release frame but only queued");
        rig.SettleFrame();
        CollectionAssert.AreEqual(new[] { at }, rig.Asked, "a 2 px wiggle is a tap, at where the finger landed");
    }

    // --- item 4: a whole gesture inside one frame --------------------------------------------------------------
    // The mouse half of item 4 ("no pan") is deliberately absent. A test asserting that a right press, a 300 px move
    // and a release inside one frame leaves LookAt alone passes on its own and fails in the full suite, where the
    // camera had panned to (503876.60, 0, 499869.60). So "it does not pan" is not a stable property of the system here,
    // and a test asserting it would be order-dependent. Reported on #1661 instead of pinned; the no-order half is
    // already covered by RightClickOrPanTests.ARightDragOutAndBackInsideOneFrameIsNoClick.

    /// <summary>Item 4's touch half, as the owner ruled it on 26 October 2026: **a very fast one-finger swipe pans
    /// the map and selects nothing.** A flick whose press, travel and release all land in one input update used to
    /// come back as a tap, at the release point rather than where the finger landed, because <c>Tick</c> samples the
    /// touch once per frame and <c>TapPress.Step</c> set <c>start</c> from that same sample on the press edge: the
    /// 200 px of travel was invisible to <c>position - start</c> and <c>moved</c> never latched. The press now carries
    /// the finger's travel within the frame, so the slop sees it.
    /// <para>The warming frame matters and must stay: <c>ButtonControl.wasPressedThisFrame</c> takes an old path on
    /// its first call ever and can miss a fast press (ButtonControl.cs:287-293, and its own remarks). Without it this
    /// test passes for the wrong reason, with the gesture not seen at all. In the game <c>Tick</c> queries every
    /// frame, so the control is warm.</para></summary>
    [Test]
    public void AFlickInsideOneInputUpdateSelectsNothing()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(400, 400);
        rig.Frame();   // warms press.wasPressedThisFrame: its first call ever takes ButtonControl's old path
        BeginTouch(1, from, queueEventOnly: true);
        MoveTouch(1, from + new Vector2(200, 0), queueEventOnly: true);
        EndTouch(1, from + new Vector2(200, 0));
        rig.Frame();
        rig.SettleFrame();
        Assert.IsEmpty(rig.Asked, "a fast swipe selects nothing: the travel is inside the frame, and the slop sees it");
        // The pan is not asserted here, and deliberately. In this extreme the finger is already up when the frame
        // runs, so CameraController's FingersDown sees none and never reads its delta; panning it would mean changing
        // the camera, which is ruling 2's file. A swipe the camera sees a finger during is the next test.
        Assert.AreEqual(start, rig.View.LookAt, "today it also does not pan, because the finger is gone by the frame");
    }

    /// <summary>The same ruling for a swipe the camera can actually see: fast, but with the finger down across a
    /// frame, as every swipe on a real screen is. Nothing is selected and the map pans by the swipe. This is the half
    /// of the owner's ruling 3 that needs no change to the camera.</summary>
    [Test]
    public void AFastSwipeAcrossFramesPansAndSelectsNothing()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(400, 400);
        rig.Frame();   // warms press.wasPressedThisFrame, as above
        BeginTouch(1, from);
        rig.Frame();
        MoveTouch(1, from + new Vector2(200, 0));
        rig.Frame();
        EndTouch(1, from + new Vector2(200, 0));
        rig.Frame();
        rig.SettleFrame();
        Assert.IsEmpty(rig.Asked, "a swipe past the slop selects nothing");
        Assert.AreNotEqual(start, rig.View.LookAt, "and the map pans by the swipe");
    }

    /// <summary>The zero-travel control for the two tests above: the same one-update press and release, with the
    /// finger still, selects where it was pressed. The finger's travel in the frame is zero, so the slop sees nothing
    /// and the press is a tap, as it should be. That is what makes the swipe above a travel rule rather than a
    /// one-update gesture being dropped: the only difference between them is the 200 px.</summary>
    [Test]
    public void AStillPressInsideOneInputUpdateSelectsWhereItWasPressed()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var at = new Vector2(500, 350);
        rig.Frame();   // same warming as above
        BeginTouch(1, at, queueEventOnly: true);
        EndTouch(1, at);
        rig.Frame();
        rig.SettleFrame();
        CollectionAssert.AreEqual(new[] { at }, rig.Asked, "a still press and release in one update selects, correctly");
        Assert.AreEqual(start, rig.View.LookAt, "and a tap never pans");
    }

    // --- item 5: the order, now declared ------------------------------------------------------------------------

    /// <summary>Item 5's fix, pinned at its cause: the pointer's frame must run before the camera's, because the camera
    /// asks it whether a press may still be a tap and only the pointer's frame moves that state.</summary>
    [Test]
    public void ThePointerTicksBeforeTheCamera()
    {
        int Order<T>() => System.Attribute.GetCustomAttribute(typeof(T), typeof(DefaultExecutionOrder)) is DefaultExecutionOrder o ? o.order : 0;
        Assert.Less(Order<MapPointer>(), Order<CameraController>(),
            "MapPointer must tick first, or a drag lifted just after crossing the slop loses its pan (#1661 item 5)");
    }

    /// <summary>Item 5, the behaviour the order protects: a one-finger drag that crosses the slop and is lifted on the
    /// very next frame still pans. In that order the pointer sets moved first, so HoldDrag replays the kept delta
    /// instead of dropping it as a click's.</summary>
    [Test]
    public void APanLiftedRightAfterCrossingTheSlopIsNotLost()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var from = new Vector2(700, 400);
        BeginTouch(1, from);
        rig.Frame();
        MoveTouch(1, from + new Vector2(TapPress.SlopPixels + 40, 0));
        rig.Frame();
        EndTouch(1, from + new Vector2(TapPress.SlopPixels + 40, 0));
        rig.Frame();
        rig.SettleFrame();
        Assert.AreNotEqual(start, rig.View.LookAt, "the pan is not lost when the finger lifts right after the slop");
        Assert.IsEmpty(rig.Asked, "a drag past the slop never taps");
    }

    // --- #1792: a second finger whose whole press lands inside one input update ---------------------------------

    /// <summary>#1792: a pinch whose second finger presses, travels and lifts inside one input update must still cancel
    /// the thumb's tap. This is <c>MapPointer.cs</c>'s own promise - "a releasing pinch's thumb is inside the slop, yet
    /// must not select" - and before the fix the thumb selected at (400, 400).
    /// <para>Why it broke. <c>Tick</c> counted the fingers from level state (<c>if (touch.press.isPressed)</c>), so a
    /// finger already up when the frame sampled was never counted: <c>otherFingerDown</c> stayed false,
    /// <c>TapPress.pinched</c> never latched, and the still thumb ended as a clean tap. The count now asks
    /// <c>CameraController.DownThisUpdate</c>, which also takes the press edge. Same class as #1661's one-update flick
    /// (#1777): a gesture that opens and closes between two samples is invisible to level state.</para></summary>
    [Test]
    public void APinchWhoseSecondFingerLivesInsideOneUpdateRefusesTheThumbsTap()
    {
        using var rig = new Rig();
        var thumb = new Vector2(400, 400);
        rig.Frame();                                   // warms press.wasPressedThisFrame (see the flick test above)
        BeginTouch(1, thumb);
        rig.Frame();
        Assert.IsEmpty(rig.Asked, "nothing while the thumb alone is down");
        // the second finger's whole life - press, 200 px of travel, release - inside one input update
        BeginTouch(2, new Vector2(700, 400), queueEventOnly: true);
        MoveTouch(2, new Vector2(900, 400), queueEventOnly: true);
        EndTouch(2, new Vector2(900, 400));
        rig.Frame();
        EndTouch(1, thumb);
        rig.Frame();
        rig.SettleFrame();
        Assert.IsEmpty(rig.Asked, "the thumb of a pinch must not select, however briefly the other finger was down");
    }

    /// <summary>#1792's other half, the one the issue names: the same invisible second finger, but with the thumb
    /// <em>moving</em>. The camera's own finger count skipped on <c>!t.press.isPressed</c> too, so it saw one finger,
    /// took <c>case 1</c> and panned by the thumb's delta instead of taking <c>case 2</c> and zooming - the map's
    /// centre moved 16 800 world units before the fix. A pinch must not replay as a one-finger pan.</summary>
    [Test]
    public void APinchWhoseSecondFingerLivesInsideOneUpdateDoesNotPan()
    {
        using var rig = new Rig();
        var start = rig.View.LookAt;
        var thumb = new Vector2(400, 400);
        rig.Frame();
        BeginTouch(1, thumb);
        rig.Frame();
        // all four events in ONE input update: every call but the last passes queueEventOnly, or it flushes its own
        // update and the thumb's drag would land in a later one, where there really is only one finger
        BeginTouch(2, new Vector2(700, 400), queueEventOnly: true);
        MoveTouch(2, new Vector2(900, 400), queueEventOnly: true);
        EndTouch(2, new Vector2(900, 400), queueEventOnly: true);
        MoveTouch(1, thumb + new Vector2(0, -TapPress.SlopPixels - 40));   // the thumb drags well past the slop
        rig.Frame();
        Assert.AreEqual(start, rig.View.LookAt, "a pinch must not pan the map, however briefly the other finger was down");
        // ...and it must not arrive a frame late either (#1795 review). Skipping case 1 for one frame is enough today,
        // because the drag is read from the finger's delta and that delta is gone next frame. On the #1661 camera lane
        // the drag comes from travel recorded at the events, so a deferred pan would land here and a single assertion
        // on the pinch frame alone would not notice.
        rig.Frame();
        Assert.AreEqual(start, rig.View.LookAt, "and the pan does not arrive on the frame after the pinch either");
    }

    /// <summary>The control, and the reason the test above is a finding rather than an artefact: the same gesture with
    /// the second finger down long enough to be sampled does latch the pinch, and the thumb selects nothing. So the
    /// difference between the two is the second finger being invisible to the sample, not the thumb being special.</summary>
    [Test]
    public void APinchWhoseSecondFingerSpansFramesCorrectlyRefusesTheThumbsTap()
    {
        using var rig = new Rig();
        var thumb = new Vector2(400, 400);
        rig.Frame();
        BeginTouch(1, thumb);
        rig.Frame();
        BeginTouch(2, new Vector2(700, 400));
        rig.Frame();                                   // sampled with both fingers down: otherFingerDown is true here
        EndTouch(2, new Vector2(900, 400));
        rig.Frame();
        EndTouch(1, thumb);
        rig.Frame();
        rig.SettleFrame();
        Assert.IsEmpty(rig.Asked, "a pinch's thumb selects nothing when the second finger was sampled");
    }

}
