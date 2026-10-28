#nullable enable
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// R-CAMERA-001: owner ruling, 27 October 2026, #1720.
public class FixedNorthCameraTests : InputTestFixture
{
    GameObject go = null!;
    CameraRig rig = null!;
    CameraController controller = null!;
    InputActionAsset actions = null!;

    static void Send(CameraController c, string method) => typeof(CameraController).GetMethod(method,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(c, null);

    void Open(string ruleset)
    {
        // The persisted shipped asset, as CameraInputTests uses it; InputTestFixture restores the input system.
        actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        Assert.IsNotNull(actions, "the shipped input asset must exist");
        InputSystem.actions = actions;
        go = new GameObject("FixedNorthCamera", typeof(Camera), typeof(CameraRig));
        rig = go.GetComponent<CameraRig>();
        rig.LookAt = new Vector3(500000, 0, 500000); rig.Distance = 100000f; rig.Yaw = 37f;
        controller = go.AddComponent<CameraController>();
        controller.Bounds = new Rect(0, 0, 1000000, 1000000);
        GameRoot.ConfigureCameraHeading(controller, ruleset);
        Send(controller, "Awake"); Send(controller, "OnEnable");
    }

    void Close()
    {
        try
        {
            if (controller != null) Send(controller, "OnDisable");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
            go = null!; controller = null!;
            // Leave every map of the shared asset off (the UI map too, not only Camera): a later input test must not
            // inherit this fixture's enabled asset state.
            if (actions != null) actions.Disable();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Gdd03KeyboardOrbitCannotTurnNorth(bool clockwise)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Open(SaveGame.Gdd03Ruleset);
        try
        {
            Assert.AreEqual(0f, rig.Yaw, "entering gameplay resets an inherited heading immediately");
            Press(clockwise ? keyboard.eKey : keyboard.qKey);
            for (int i = 0; i < 10; i++) controller.Tick(0.1f);
            Assert.AreEqual(0f, rig.Yaw);
        }
        finally { Close(); }
    }

    [TestCase("legacy")]
    [TestCase("gdd-0.1")]
    public void OlderRulesetsKeepTheExistingOrbit(string ruleset)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Open(ruleset);
        try
        {
            Assert.AreEqual(37f, rig.Yaw);
            var orbit = actions.FindAction("Camera/Orbit", true);
            Press(keyboard.eKey);
            Assert.AreEqual(1f, orbit.ReadValue<float>(), "E reaches the shipped positive orbit binding");
            controller.Tick(0.1f);
            Assert.AreEqual(43f, rig.Yaw, 0.001f);
            Release(keyboard.eKey); Press(keyboard.qKey);
            Assert.AreEqual(-1f, orbit.ReadValue<float>(), "Q reaches the shipped negative orbit binding");
            controller.Tick(0.1f);
            Assert.AreEqual(37f, rig.Yaw, 0.001f);
        }
        finally { Close(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Gdd03MouseDragPansWithoutOrbit(bool left)
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Open(SaveGame.Gdd03Ruleset);
        try
        {
            controller.HoldsMouseDrag = () => false;
            Set(mouse.position, new Vector2(400, 400));
            Press(left ? mouse.leftButton : mouse.rightButton); controller.Tick(0.016f);
            Move(mouse.position, new Vector2(450, 400), new Vector2(50, 0)); controller.Tick(0.016f);
            Assert.AreEqual(0f, rig.Yaw);
            Assert.AreEqual(497600f, rig.LookAt.x, 0.1f, "only the 20 px beyond the 30 px onset pan");
        }
        finally { Close(); }
    }

    [Test]
    public void Gdd03TouchDragAndTwistCannotOrbitAndPinchStillZooms()
    {
        InputSystem.AddDevice<Touchscreen>();
        Open(SaveGame.Gdd03Ruleset);
        try
        {
            BeginTouch(1, new Vector2(400, 400)); controller.Tick(0.016f);
            MoveTouch(1, new Vector2(410, 400)); controller.Tick(0.016f);
            Assert.Less(rig.LookAt.x, 500000f, "one finger still pans");
            BeginTouch(2, new Vector2(510, 400)); controller.Tick(0.016f);
            MoveTouch(1, new Vector2(460, 350)); MoveTouch(2, new Vector2(460, 450));
            controller.Tick(0.016f);
            Assert.AreEqual(0f, rig.Yaw, "a two-finger twist has no orbit binding");
            var before = rig.Distance;
            MoveTouch(2, new Vector2(460, 470)); controller.Tick(0.016f);
            Assert.AreEqual(before * Mathf.Pow(0.9f, 0.2f), rig.Distance, 1f);
            Assert.AreEqual(0f, rig.Yaw);
        }
        finally { Close(); }
    }

    [Test]
    public void Gdd03WheelKeysAndTrackpadPinchKeepTheirExistingRates()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Open(SaveGame.Gdd03Ruleset);
        try
        {
            Set(mouse.position, new Vector2(400, 400)); controller.Tick(0.016f);
            Set(mouse.scroll, new Vector2(0, 1)); controller.Tick(0.016f);
            Assert.AreEqual(90000f, rig.Distance, 1f);
            Set(mouse.scroll, Vector2.zero);
            Press(keyboard.equalsKey); controller.Tick(0.1f); Release(keyboard.equalsKey);
            var expected = 90000f * Mathf.Pow(0.9f, 0.6f);
            Assert.AreEqual(expected, rig.Distance, 1f);
            float pending = 0.05f;
            controller.TrackpadMagnification = () => { var value = pending; pending = 0; return value; };
            controller.Tick(0.016f); controller.Tick(0.016f);
            Assert.AreEqual(expected / 1.05f, rig.Distance, 1f, "magnification is consumed once");
            Assert.AreEqual(0f, rig.Yaw);
        }
        finally { Close(); }
    }

    [Test]
    public void FixedNorthStepAndSettleRepairYawAndPolicyCanBeReleased()
    {
        Open(SaveGame.Gdd03Ruleset);
        try
        {
            rig.Yaw = 91f; controller.Settle(); Assert.AreEqual(0f, rig.Yaw);
            rig.Yaw = 123f;
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.1f, controller.Bounds, fixedNorth: true);
            Assert.AreEqual(0f, rig.Yaw, "Step defends against input bypassing Gather");
            GameRoot.ConfigureCameraHeading(controller, SaveGame.LegacyRuleset);
            var input = controller.Gather(Vector2.zero, Vector2.zero, 0, 1, false, null);
            CameraController.Step(rig, input, 0.1f, controller.Bounds);
            Assert.AreEqual(6f, rig.Yaw, 0.001f, "policy release restores orbit");
        }
        finally { Close(); }
    }

}
