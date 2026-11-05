#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
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

/// <summary>Accessibility audit L4, against integration bug B6: while a screen is open, Up and Down move the focus between
/// its rows; Left and Right never move it, and with no screen open none of the four does. The owner's answer on #2340,
/// Q27: "do the recommended for all" (https://github.com/eyalgolan/statecraft-israel/issues/2340#issuecomment-6623136558).
/// Same harness as KeyboardTurnTests: Map.unity at 1280x720 with GameRoot's own NavigationGuard, a new GDD game, and the
/// moves sent as the NavigationMoveEvent the UI provider raises; one test presses the real arrow keys through GameRoot's
/// frame, on the camera tests' input fixture.</summary>
public class ScreenRowArrowTests : SharedCameraActionsFixture
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;
    int presses;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-rows-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    Focusable? Focused => ui.panel?.focusController?.focusedElement;
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;
    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Move(NavigationMoveEvent.Direction d)
    {
        var at = Focused as VisualElement ?? ui;
        using (var e = NavigationMoveEvent.GetPooled(d)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Focus(string name)
    {
        var b = ui.Q<VisualElement>(name);
        Assert.IsNotNull(b, name);
        b.Focus();
        for (int i = 0; i < 10 && Focused != b; i++) yield return null;
        Assert.AreEqual(name, FocusedName, "precondition: focused");
    }

    /// <summary>Boots the game, answers the opening card from the keyboard (Tab, Enter) and counts every button press
    /// from here on, so a move that presses anything shows. <paramref name="hebrew"/> sets the UI's language before Begin.</summary>
    IEnumerator Boot(bool hebrew = false)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = hebrew ? UiLanguage.Hebrew : UiLanguage.English;
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        yield return Move(NavigationMoveEvent.Direction.Next);
        var at = (VisualElement)Focused!;
        using (var k = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { k.target = at; at.SendEvent(k); }
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return Frames(3);
        presses = 0;
        foreach (var b in ui.Query<Button>().ToList()) b.clicked += () => presses++;
    }

    IEnumerator Open(string screen)
    {
        UiTestKit.Press(ui.Q<Button>("nav-" + screen));
        presses = 0;   // the nav press itself
        yield return Frames(5);
        Assert.IsTrue(ui.Q("screen-" + screen).resolvedStyle.display == DisplayStyle.Flex, "precondition: " + screen + " is open");
    }

    /// <summary>The Settings row (.settings-row) the focus is in, as its index among the screen's rows, or -1.</summary>
    int SettingsRow()
    {
        var rows = ui.Q("screen-settings").Query(className: "settings-row").ToList();
        var at = Focused as VisualElement;
        return rows.FindIndex(r => at != null && r.Contains(at));
    }

    /// <summary>L4 and B6, a multi-row screen: in Settings, Down walks the option rows one at a time, from a choice button to
    /// the choice in the next row nearest below it, and Up walks back; Left and Right leave the focus where it is; the focus
    /// never leaves the screen at either end, nor reaches the header's X, which is no row (#2549 review), and no move
    /// presses anything.</summary>
    [UnityTest] public IEnumerator UpAndDownWalkTheSettingsRowsL4() => SettingsRows(false);

    /// <summary>The same in the Hebrew UI (GameRoot.Language), right to left.</summary>
    [UnityTest] public IEnumerator UpAndDownWalkTheSettingsRowsInHebrewL4() => SettingsRows(true);

    IEnumerator SettingsRows(bool hebrew)
    {
        yield return Boot(hebrew);
        Assert.AreEqual(hebrew, ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "precondition: the UI's language");
        yield return Open("settings");
        yield return Focus("settings-tiltshift");
        Assert.AreEqual(0, SettingsRow());
        // The rows with a control to take the focus: in this editor the companion's row shows none (WebCompanion.Available).
        var reachable = ui.Q("screen-settings").Query(className: "settings-row").ToList()
            .Select((r, i) => (r, i))
            .Where(x => x.r.resolvedStyle.display == DisplayStyle.Flex && x.r.Query<Button>().ToList().Any(b =>
                b.enabledInHierarchy && b.resolvedStyle.display == DisplayStyle.Flex && b.resolvedStyle.visibility == Visibility.Visible))
            .Select(x => x.i).ToList();
        Assert.GreaterOrEqual(reachable.Count, 6, "precondition: the option rows");
        for (int k = 1; k < reachable.Count; k++)
        {
            yield return Move(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(reachable[k], SettingsRow(), $"Down moves to row {reachable[k]}, at {FocusedName}");
        }
        for (int i = 0; i < 5; i++) yield return Move(NavigationMoveEvent.Direction.Down);
        Assert.IsTrue(ui.Q("screen-settings").Contains((VisualElement)Focused!), "Down past the last row stays in the screen, at " + FocusedName);
        yield return Focus("settings-sound-high");
        yield return Move(NavigationMoveEvent.Direction.Up);
        Assert.AreEqual("settings-quality-low", FocusedName, "Up goes to the choice straight above");
        foreach (var d in new[] { NavigationMoveEvent.Direction.Left, NavigationMoveEvent.Direction.Right })
        {
            yield return Move(d);
            Assert.AreEqual("settings-quality-low", FocusedName, d + " leaves the focus where it is");
        }
        for (int i = 0; i < 12; i++) yield return Move(NavigationMoveEvent.Direction.Up);
        Assert.IsTrue(ui.Q("screen-settings").Contains((VisualElement)Focused!), "Up past the first row stays in the screen, at " + FocusedName);
        Assert.AreEqual(reachable[0], SettingsRow(), "Up past the first row stays on it, not the header's X, at " + FocusedName);
        Assert.AreEqual(DisplayStyle.Flex, ui.Q("screen-settings").resolvedStyle.display, "the screen is still open");
        Assert.AreEqual(0, presses, "moving pressed nothing");
    }

    /// <summary>L4 and B6, a list: in the Budget, Down goes from a line's - to the next line's -, skipping a line whose
    /// buttons are disabled and one that is hidden, and Up comes back the same way; nothing is pressed.</summary>
    [UnityTest]
    public IEnumerator UpAndDownWalkTheBudgetLinesSkippingHiddenAndDisabledL4()
    {
        yield return Boot();
        yield return Open("budget");
        var lines = ui.Q<ScrollView>("budget-rows").contentContainer.Children().Where(r => r.Q<Button>(r.name + "-down") != null).ToList();
        Assert.GreaterOrEqual(lines.Count, 5, "precondition: the budget has its lines");
        yield return Focus(lines[0].name + "-down");
        yield return Move(NavigationMoveEvent.Direction.Down);
        Assert.AreEqual(lines[1].name + "-down", FocusedName, "Down: the next line's -");
        lines[2].Q<Button>(lines[2].name + "-down").SetEnabled(false);
        lines[2].Q<Button>(lines[2].name + "-up").SetEnabled(false);
        lines[3].style.display = DisplayStyle.None;
        yield return Frames(3);
        yield return Move(NavigationMoveEvent.Direction.Down);
        Assert.AreEqual(lines[4].name + "-down", FocusedName, "a disabled line and a hidden one are skipped");
        yield return Move(NavigationMoveEvent.Direction.Up);
        Assert.AreEqual(lines[1].name + "-down", FocusedName, "and Up skips them too");
        Assert.AreEqual(0, presses, "moving pressed nothing");
    }

    /// <summary>L4 and B6 through GameRoot (#2549 review): the real Up and Down keys, read by GameRoot's own key poll in
    /// its frame, walk the Budget's lines, and the camera GameRoot wired (ScreenTakesUpDown) does not pan on them while
    /// the screen is open; once it is closed, the same keys pan and leave the focus where it is. In English and in Hebrew
    /// (right to left), so the real key path is proven both ways (#2549 review).</summary>
    [UnityTest]
    public IEnumerator TheArrowKeysThroughGameRootWalkTheRowsAndPanOnlyWithTheScreenClosedL4([Values(false, true)] bool hebrew)
    {
        // The keys as state events: the fixture's Press (Set) does not run in an edit-mode [UnityTest], and Boot needs frames.
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        // Warm both keys: ButtonControl.wasPressedThisFrame takes an old path on its first call ever and can miss a press
        // (as in MapPointerTickTests); in the game GameRoot asks every frame, so they are warm.
        _ = keyboard.upArrowKey.wasPressedThisFrame; _ = keyboard.downArrowKey.wasPressedThisFrame;
        yield return Boot(hebrew);
        var cam = Object.FindFirstObjectByType<CameraController>();
        var rig = cam.GetComponent<CameraRig>();
        Send(cam, "Awake"); Send(cam, "OnEnable");
        try
        {
            yield return Open("budget");
            var lines = ui.Q<ScrollView>("budget-rows").contentContainer.Children().Where(r => r.Q<Button>(r.name + "-down") != null).ToList();
            yield return Focus(lines[0].name + "-down");
            // One frame with the key down: GameRoot's poll (Update) and the camera's (Tick) both read it.
            Vector3 Hold(UnityEngine.InputSystem.Key k)
            {
                var from = rig.LookAt;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(k)); InputSystem.Update();
                Assert.IsTrue(keyboard[k].wasPressedThisFrame, "precondition: " + k + " went down this frame");
                Send(root, "Update");
                for (int i = 0; i < 5; i++) cam.Tick(0.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                cam.Tick(0.1f);
                return rig.LookAt - from;
            }
            Assert.AreEqual(Vector3.zero, Hold(UnityEngine.InputSystem.Key.DownArrow), "screen open: Down does not pan");
            Assert.AreEqual(lines[1].name + "-down", FocusedName, "Down: the next line's -");
            Assert.AreEqual(Vector3.zero, Hold(UnityEngine.InputSystem.Key.UpArrow), "screen open: Up does not pan");
            Assert.AreEqual(lines[0].name + "-down", FocusedName, "Up: back to the first line's -");
            UiTestKit.Press(ui.Q<Button>("nav-budget"));
            yield return Frames(5);
            Assert.AreEqual(DisplayStyle.None, ui.Q("screen-budget").resolvedStyle.display, "precondition: the Budget is closed");
            yield return Focus("nav-budget");
            presses = 0;
            Assert.Greater(Hold(UnityEngine.InputSystem.Key.UpArrow).magnitude + Hold(UnityEngine.InputSystem.Key.DownArrow).magnitude, 1f, "no screen open: Up and Down pan (B6)");
            Assert.AreEqual("nav-budget", FocusedName, "no screen open: the focus stays (B6)");
            Assert.AreEqual(0, presses, "no key pressed a button");
        }
        finally { Send(cam, "OnDisable"); }
    }

    /// <summary>L4 under the turn report (#2651 review): the report covers the screen it opened over, so the real Up and Down
    /// keys, read by GameRoot's poll, move no focus inside that hidden screen; they leave it where the report put it.</summary>
    [UnityTest]
    public IEnumerator UpAndDownMoveNothingUnderTheTurnReportL4()
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        _ = keyboard.upArrowKey.wasPressedThisFrame; _ = keyboard.downArrowKey.wasPressedThisFrame;   // warm, as above
        yield return Boot();
        yield return Open("budget");
        var lines = ui.Q<ScrollView>("budget-rows").contentContainer.Children().Where(r => r.Q<Button>(r.name + "-down") != null).ToList();
        yield return Focus(lines[0].name + "-down");
        root.Bridge!.EndTurn();
        yield return Frames(5);
        Assert.IsTrue(ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass), "precondition: the turn report is open");
        Assert.AreEqual(DisplayStyle.Flex, ui.Q("screen-budget").resolvedStyle.display, "precondition: the Budget is still open under it");
        var at = FocusedName;
        foreach (var k in new[] { UnityEngine.InputSystem.Key.DownArrow, UnityEngine.InputSystem.Key.UpArrow, UnityEngine.InputSystem.Key.DownArrow })
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(k)); InputSystem.Update();
            Assert.IsTrue(keyboard[k].wasPressedThisFrame, "precondition: " + k + " went down this frame");
            Send(root, "Update");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Assert.AreEqual(at, FocusedName, k + " under the report left the focus where it was");
        }
        Assert.AreEqual(0, presses, "no key pressed a button");
    }

    static void Send(MonoBehaviour c, string message) =>
        c.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(c, null);

    /// <summary>B6 with no screen open, through GameRoot's guard: none of the four directions moves the focus off a HUD
    /// button or presses it.</summary>
    [UnityTest]
    public IEnumerator WithNoScreenOpenNoArrowMovesTheFocusB6()
    {
        yield return Boot();
        Assert.IsFalse(ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass), "precondition: no screen is open");
        foreach (var name in new[] { "nav-cabinet", "nav-budget", "end-turn" })
        {
            yield return Focus(name);
            foreach (var d in new[] { NavigationMoveEvent.Direction.Up, NavigationMoveEvent.Direction.Down, NavigationMoveEvent.Direction.Left, NavigationMoveEvent.Direction.Right })
            {
                yield return Move(d);
                Assert.AreEqual(name, FocusedName, $"{d} left the focus on {name}");
            }
        }
        Assert.AreEqual(0, presses, "no arrow pressed a button");
    }
}
