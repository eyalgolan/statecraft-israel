#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#2712, through GameRoot on Map.unity: (1) in the two-row HUD a side screen covers the whole map, so the camera
/// keys do not move the hidden map while one is open; in the one-row desktop layout the screen is a window beside the map
/// and the keys still move it (plan amendment A4). (2) New Game starts from the opening view, as a fresh boot does.</summary>
public class CameraScreenCoverTests : SharedCameraActionsFixture
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-cover-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    IEnumerator Boot(int width, int height)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(width, height, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = UiLanguage.English;
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        for (int i = 0; i < 5; i++) yield return null;
    }

    static void Send(MonoBehaviour c, string message) =>
        c.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(c, null);

    /// <summary>Item 1: with the Budget open, D (pan) and = (zoom in) held for half a second leave the camera where it is in
    /// the two-row HUD (960x1080) and move it in the one-row one (1920x1080). With the screen closed, both layouts pan.</summary>
    [UnityTest]
    public IEnumerator TheCameraKeysStopOnlyUnderAScreenThatCoversTheMap([Values(960, 1920)] int width)
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        yield return Boot(width, 1080);
        bool twoRow = width < 1400;
        Assert.AreEqual(twoRow, ui.Q("root").ClassListContains(HudPresenter.TwoRowClass), "precondition: the HUD tier");
        var cam = Object.FindFirstObjectByType<CameraController>();
        var rig = cam.GetComponent<CameraRig>();
        Send(cam, "Awake"); Send(cam, "OnEnable");
        try
        {
            (Vector3 moved, float zoomed) Hold(Key k)
            {
                var from = rig.LookAt; var distance = rig.Distance;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(k)); InputSystem.Update();
                for (int i = 0; i < 5; i++) cam.Tick(0.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                cam.Tick(0.1f);
                return (rig.LookAt - from, rig.Distance - distance);
            }
            UiTestKit.Press(ui.Q<Button>("nav-budget"));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass), "precondition: the Budget is open");
            var pan = Hold(Key.D).moved.magnitude;
            var zoom = Mathf.Abs(Hold(Key.Equals).zoomed);
            if (twoRow)
            {
                Assert.AreEqual(0f, pan, "two rows, screen open: D does not pan the hidden map");
                Assert.AreEqual(0f, zoom, "two rows, screen open: = does not zoom the hidden map");
            }
            else
            {
                Assert.Greater(pan, 1f, "one row, screen open: D still pans (A4)");
                Assert.Greater(zoom, 1f, "one row, screen open: = still zooms (A4)");
            }
            UiTestKit.Press(ui.Q<Button>("nav-budget"));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsFalse(ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass), "precondition: the Budget is closed");
            Assert.Greater(Hold(Key.D).moved.magnitude, 1f, "no screen open: D pans");
        }
        finally { Send(cam, "OnDisable"); }
    }

    /// <summary>Item 2: the player pans and zooms away, then presses New Game (twice, as the top bar asks): the camera is
    /// back at the opening view the boot gave, its look-at and its zoom (CameraRig.StartDistance).</summary>
    [UnityTest]
    public IEnumerator NewGameStartsFromTheOpeningView()
    {
        yield return Boot(1920, 1080);
        var rig = Object.FindFirstObjectByType<CameraController>().GetComponent<CameraRig>();
        var opening = rig.LookAt;
        Assert.AreEqual(CameraRig.StartDistance, rig.Distance, "precondition: the boot opens at the start zoom");
        rig.LookAt = opening + new Vector3(60000f, 0f, 80000f);
        rig.Distance = 120000f;
        rig.Apply();
        var session = root.Bridge!.Session;
        UiTestKit.Press(ui.Q<Button>("new-game"));
        UiTestKit.Press(ui.Q<Button>("new-game"));
        Assert.AreNotSame(session, root.Bridge!.Session, "precondition: New Game replaced the session");
        Assert.AreEqual(opening.x, rig.LookAt.x, 1f, "New Game: the opening look-at (x)");
        Assert.AreEqual(opening.z, rig.LookAt.z, 1f, "New Game: the opening look-at (z)");
        Assert.AreEqual(CameraRig.StartDistance, rig.Distance, 1f, "New Game: the opening zoom");
    }
}
