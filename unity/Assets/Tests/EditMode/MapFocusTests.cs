#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#41 B1b (#1116): the map's keyboard stop. Map.unity as the player gets it, at 1280x720: Tab reaches the stop
/// after End Turn, a gold ring shows at the camera's look-at point only while the stop has the focus, Enter or Space picks
/// the hex under it through the map's own pick and leaves the focus on the stop, and the info panel's actions follow it in
/// the Tab order. Covered (a wide screen, the start screen) the stop is out of reach; a restart or a language switch leaves
/// one pick per key press. Keys reach the UI here as the UI Toolkit events the Input System's UI provider makes of them
/// (Tab: NavigationMoveEvent Next; Enter: KeyDownEvent then NavigationSubmitEvent; Space: KeyDownEvent only, as only
/// Enter carries the Submit usage): an edit-mode panel gets no device input (a queued Tab moves nothing there), so the
/// browser check drives the real keys (PR evidence). The camera keys go through the Input System itself
/// (MapFocusCameraKeysTests).</summary>
public class MapFocusTests
{
    static GameInputs? inputs;
    internal static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static UiText TextHe => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-mapfocus-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    /// <summary>Map.unity on a 1280x720 panel, the map built and the game started as GameBoot.Ready does (no start screen
    /// unless asked), laid out, the camera over Tel Aviv, the opening card hidden.</summary>
    IEnumerator Boot(bool startScreen = false)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(Inputs, 0, startScreen);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        if (!startScreen) GameSceneTests.HideTheOpeningCard(ui);
        LookAtTelAviv();
        yield return Frames(3);
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static (int q, int r) TelAviv()
    {
        var tlv = Inputs.Content.SettlementHexes.Single(s => s.Id == "tel_aviv");
        return (tlv.Q, tlv.R);
    }

    /// <summary>Moves the camera's look-at point onto Tel Aviv's hex, on the ground, as a pan would.</summary>
    static void LookAtTelAviv()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var (q, r) = TelAviv();
        var hex = Inputs.Content.HexAt(q, r)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = 30000f;
        rig.Apply();
    }

    VisualElement Stop => ui.Q("map-focus") ?? throw new AssertionException("Game.uxml has no map-focus stop");
    VisualElement Mark => ui.Q("map-focus-mark") ?? throw new AssertionException("the stop has no map-focus-mark");
    Focusable? Focused => ui.panel.focusController.focusedElement;
    string FocusedName => (Focused as VisualElement)?.name ?? "none";

    IEnumerator FocusOn(VisualElement e)
    {
        e.Focus();
        for (int i = 0; i < 10 && Focused != e; i++) { yield return null; e.Focus(); }
        Assert.AreSame(e, Focused, e.name + ": precondition, focused");
    }

    /// <summary>Tab, as the UI provider sends it: NavigationMoveEvent Next to the focused element.</summary>
    IEnumerator Tab()
    {
        var at = (VisualElement)Focused!;
        using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    /// <summary>A key press on the focused element as the UI provider delivers it: the key, then for Enter (the only key
    /// with the Submit usage) the submit.</summary>
    IEnumerator Key(KeyCode key)
    {
        var at = (VisualElement)Focused!;
        using (var e = KeyDownEvent.GetPooled(key == KeyCode.Space ? ' ' : '\n', key, EventModifiers.None)) { e.target = at; at.SendEvent(e); }
        if (key == KeyCode.Return)
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    bool MarkShown => Mark.resolvedStyle.display == DisplayStyle.Flex && Mark.worldBound.width > 0;

    /// <summary>Where the look-at point is on the panel: the camera's viewport scaled to the panel, which the view fills.</summary>
    Vector2 LookAtOnPanel()
    {
        var cam = Camera.main;
        var v = cam.WorldToViewportPoint(cam.GetComponent<CameraRig>().LookAt);
        var size = ui.panel.visualTree.layout.size;
        return new Vector2(v.x * size.x, (1f - v.y) * size.y);
    }

    /// <summary>Tab from End Turn reaches the stop; the ring shows at the look-at point; Enter picks Tel Aviv through the
    /// map pointer (outline and info panel), the focus stays; the next Tabs reach the city's actions in the info panel
    /// before the order starts again.</summary>
    [UnityTest]
    public IEnumerator TabReachesTheStopAfterEndTurnAndEnterPicksTheCentreHex()
    {
        yield return Boot();
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var picked = new List<(int, int)>();
        Action<int, int> onPick = (q, r) => picked.Add((q, r));
        pointer.HexClicked += onPick;
        try
        {
            Assert.IsFalse(MarkShown, "no ring before the stop has the focus");
            yield return FocusOn(ui.Q<Button>("end-turn"));
            yield return Tab();
            Assert.AreEqual("map-focus", FocusedName, "Tab from End Turn reaches the map stop");
            yield return Frames(2);
            Assert.IsTrue(MarkShown, "the ring shows while the stop has the focus");
            var at = LookAtOnPanel();
            Assert.AreEqual(at.x, Mark.worldBound.center.x, 2f, $"the ring is on the look-at point ({at}), not {Mark.worldBound}");
            Assert.AreEqual(at.y, Mark.worldBound.center.y, 2f, $"the ring is on the look-at point ({at}), not {Mark.worldBound}");

            yield return Key(KeyCode.Return);
            CollectionAssert.AreEqual(new[] { TelAviv() }, picked, "Enter picks the hex under the ring, once");
            Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text, "the info panel shows it");
            var sel = Object.FindFirstObjectByType<MapScene>().Terrain.transform.Find(MapScene.SelectionName);
            Assert.IsTrue(sel != null && sel.GetComponent<MeshRenderer>().enabled, "the map outlines it, as for a click");
            Assert.AreEqual("map-focus", FocusedName, "the focus stays on the stop, so the player can pan and pick again");

            var seen = new List<string>();
            for (int i = 0; i < 12; i++)
            {
                yield return Tab();
                seen.Add(FocusedName);
                if (ui.Q("info-panel").Contains((VisualElement)Focused!)) break;
            }
            Assert.IsTrue(ui.Q("info-panel").Contains((VisualElement)Focused!), "Tab goes on to the info panel's actions: " + string.Join(", ", seen));
            Assert.IsFalse(seen.Any(n => n.StartsWith("nav-", StringComparison.Ordinal) || n == "end-turn"),
                "the info actions come before the order starts again: " + string.Join(", ", seen));
            Assert.IsFalse(MarkShown, "the ring goes with the focus");
        }
        finally { pointer.HexClicked -= onPick; }
    }

    /// <summary>Space picks too; a key that is not Enter or Space (a camera key) picks nothing; a pan under the ring picks
    /// the new centre hex.</summary>
    [UnityTest]
    public IEnumerator SpacePicksAndTheCameraKeysDoNot()
    {
        yield return Boot();
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var picked = new List<(int, int)>();
        Action<int, int> onPick = (q, r) => picked.Add((q, r));
        pointer.HexClicked += onPick;
        try
        {
            yield return FocusOn(Stop);
            foreach (var k in new[] { KeyCode.W, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.Q, KeyCode.E })
                yield return Key(k);
            Assert.IsEmpty(picked, "the camera keys pick nothing");
            Assert.AreEqual("map-focus", FocusedName, "and leave the focus on the stop");
            yield return Key(KeyCode.Space);
            CollectionAssert.AreEqual(new[] { TelAviv() }, picked, "Space picks the centre hex");
            var rig = Camera.main.GetComponent<CameraRig>();
            rig.LookAt += new Vector3(20000f, 0, 0); rig.Apply();   // a pan of four hexes east
            yield return Frames(2);
            var at = LookAtOnPanel();
            Assert.AreEqual(at.x, Mark.worldBound.center.x, 2f, "the ring follows the look-at point");
            yield return Key(KeyCode.Space);
            Assert.AreEqual(2, picked.Count);
            Assert.AreNotEqual(TelAviv(), picked[1], "a pan moves the hex under the ring");
        }
        finally { pointer.HexClicked -= onPick; }
    }

    /// <summary>The stop takes no pointer: the panel picks nothing at the ring, and a click at the centre of the map still
    /// reaches the map and picks the hex there (mouse and touch go through MapPointer's press queue).</summary>
    [UnityTest]
    public IEnumerator MouseAndTouchStillClickThroughTheStop()
    {
        yield return Boot();
        yield return FocusOn(Stop);
        yield return Frames(2);
        Assert.IsTrue(MarkShown, "precondition: the ring shows");
        Assert.IsNull(ui.panel.Pick(Mark.worldBound.center), "neither the stop nor its ring takes the pointer");
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var picked = new List<(int, int)>();
        Action<int, int> onPick = (q, r) => picked.Add((q, r));
        pointer.HexClicked += onPick;
        try
        {
            var cam = Camera.main;
            var s = cam.WorldToScreenPoint(cam.GetComponent<CameraRig>().LookAt);
            pointer.OnPress(new Vector2(s.x, s.y));   // a click or a tap, as MapPointer queues them
            pointer.ProcessPresses();
            CollectionAssert.AreEqual(new[] { TelAviv() }, picked, "the click under the ring picks the hex there");
        }
        finally { pointer.HexClicked -= onPick; }
    }

    /// <summary>Under a wide screen (Cabinet) or the decision card's backdrop the stop is disabled: out of Tab's reach, no
    /// ring, no pick; closing the screen brings it back. Under the start screen it is out of reach too.</summary>
    [UnityTest]
    public IEnumerator CoveringScreensTakeTheStopOutOfReach()
    {
        yield return Boot();
        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var picked = 0;
        Action<int, int> onPick = (q, r) => picked++;
        pointer.HexClicked += onPick;
        try
        {
            yield return FocusOn(Stop);
            UiTestKit.Press(ui.Q<Button>("nav-cabinet"));
            yield return Frames(2);
            Assert.IsFalse(Stop.canGrabFocus, "under the Cabinet the stop is out of reach");
            Assert.AreNotEqual("map-focus", FocusedName, "and loses the focus (Escape's nothing-focused case stays reachable)");
            Assert.IsFalse(MarkShown, "and shows no ring");
            yield return FocusOn(ui.Q<Button>("end-turn"));
            yield return Tab();
            Assert.AreNotEqual("map-focus", FocusedName, "Tab skips it");
            Assert.AreEqual(0, picked);
            UiTestKit.Press(ui.Q<Button>("nav-cabinet"));
            yield return Frames(2);
            Assert.IsTrue(Stop.canGrabFocus, "closing the screen brings it back");

            UiTestKit.Press(ui.Q<Button>("event-reopen"));   // the decision card again: its backdrop covers the map
            yield return Frames(2);
            Assert.IsFalse(Stop.canGrabFocus, "under the open decision card the stop is out of reach");
        }
        finally { pointer.HexClicked -= onPick; }
    }

    [UnityTest]
    public IEnumerator TheStartScreenTakesTheStopOutOfReach()
    {
        yield return Boot(startScreen: true);
        Assert.IsTrue((ui.Q("root") ?? ui).ClassListContains(StartScreenPresenter.OpenClass), "precondition: the start screen shows");
        Assert.IsFalse(Stop.canGrabFocus, "the stop is out of reach under the start screen");
        Stop.Focus();
        yield return Frames(2);
        Assert.AreNotEqual("map-focus", FocusedName);
        Assert.IsFalse(MarkShown);
    }

    /// <summary>A restart (New Game's Begin) and the language switch build every presenter again over the same document:
    /// one key press is still one pick, and no ring is left over from before.</summary>
    [UnityTest]
    public IEnumerator ARestartOrALanguageSwitchLeavesOnePickAndNoStaleRing()
    {
        yield return Boot();
        yield return FocusOn(Stop);
        root.Begin(Inputs, 0);                                     // a restart over the same document
        GameSceneTests.HideTheOpeningCard(ui);
        LookAtTelAviv();
        yield return Frames(3);
        if (Focused != Stop) Assert.IsFalse(MarkShown, "no ring left without the focus");

        root.Language = UiLanguage.Hebrew;                        // the language switch's Begin, over this game
        root.Begin(Inputs, 0);
        GameSceneTests.HideTheOpeningCard(ui);
        LookAtTelAviv();
        yield return Frames(3);
        Assert.AreEqual(TextHe["info.hint"], ui.Q<Label>("info-title").text, "precondition: the Hebrew build");
        Assert.IsFalse(Focused == Stop && !Stop.canGrabFocus, "the focus is not left on a disabled stop");

        var pointer = Object.FindFirstObjectByType<MapPointer>();
        var picked = 0;
        Action<int, int> onPick = (q, r) => picked++;
        pointer.HexClicked += onPick;
        try
        {
            yield return FocusOn(Stop);
            yield return Frames(2);
            Assert.IsTrue(MarkShown, "the ring shows again");
            Assert.AreEqual(LookAtOnPanel().x, Mark.worldBound.center.x, 2f, "at the look-at point");
            yield return Key(KeyCode.Return);
            Assert.AreEqual(1, picked, "one Enter, one pick: no handler is left from the earlier builds");
            yield return Key(KeyCode.Space);
            Assert.AreEqual(2, picked, "one Space, one pick");
        }
        finally { pointer.HexClicked -= onPick; root.Language = null; }
    }

    /// <summary>The hint names the keyboard way in both languages and keeps the mouse's right-drag.</summary>
    [Test]
    public void TheHintNamesTabAndEnterAndKeepsRightDrag()
    {
        StringAssert.Contains("Tab", Text["info.hint"]);
        StringAssert.Contains("Enter", Text["info.hint"]);
        StringAssert.Contains("right-drag", Text["info.hint"]);
        StringAssert.Contains("Tab", TextHe["info.hint"]);
        StringAssert.Contains("Enter", TextHe["info.hint"]);
        StringAssert.Contains("גרירה", TextHe["info.hint"]);
    }

    static float Luminance(Color c) => 0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;
    static float Contrast(float x, float y) => (Mathf.Max(x, y) + 0.05f) / (Mathf.Min(x, y) + 0.05f);

    /// <summary>WCAG 1.4.11: the ring is gold-light between two dark outlines, so against any map colour one of its two
    /// colours keeps at least 3:1 (the worst background, a mid grey of luminance 0.14, still gets 3.4:1); the map's own sea,
    /// sand and ground colours are checked by name, and the gold against its outline. Colours as the stylesheet resolves
    /// them on the focused stop.</summary>
    [UnityTest]
    public IEnumerator TheRingKeepsThreeToOneOnAnyMapColour()
    {
        using var kit = new UiTestKit();
        kit.Q<VisualElement>("root").RemoveFromClassList("loading");
        var stop = kit.Q<VisualElement>("map-focus");
        stop.Focus();
        for (int i = 0; i < 10; i++) yield return null;
        var outer = kit.Q<VisualElement>("map-focus-mark").resolvedStyle;
        var ring = kit.Q<VisualElement>("map-focus-mark").Q(className: "map-focus-ring").resolvedStyle;
        Assert.Greater(outer.borderTopWidth, 0f, "a dark outline");
        Assert.Greater(ring.borderTopWidth, 0f, "a gold ring");
        float gold = Luminance(ring.borderTopColor), dark = Luminance(outer.borderTopColor);
        Assert.GreaterOrEqual(Contrast(gold, dark), 3f, "the gold against its own outline");
        var worst = float.MaxValue;
        for (int i = 0; i <= 1000; i++)
        {
            var l = i / 1000f;
            worst = Mathf.Min(worst, Mathf.Max(Contrast(gold, l), Contrast(dark, l)));
        }
        Assert.GreaterOrEqual(worst, 3f, $"the worst map colour leaves {worst:0.00}:1");
        var named = new Dictionary<string, Color>
        {
            ["sea (the clear colour)"] = MapBuilder.BiomeColours["sea"],
            ["sand (desert ground)"] = MapPalette.Ground["desert"],
            ["coastal plain ground"] = MapPalette.Ground["coastal_plain"],
            ["hills ground"] = MapPalette.Ground["hills"],
        };
        foreach (var (name, c) in named)
        {
            var best = Mathf.Max(Contrast(gold, Luminance(c)), Contrast(dark, Luminance(c)));
            Assert.GreaterOrEqual(best, 3f, $"{name}: {best:0.00}:1");
            Debug.Log(FormattableString.Invariant($"MAPFOCUS contrast {name}: gold {Contrast(gold, Luminance(c)):0.00}, outline {Contrast(dark, Luminance(c)):0.00}"));
        }
        Debug.Log(FormattableString.Invariant($"MAPFOCUS contrast worst case {worst:0.00}, gold vs outline {Contrast(gold, dark):0.00}"));
    }
}

/// <summary>#41 B1b: the camera keys through the Input System's own path (a virtual keyboard from its test fixture and the
/// project-wide Camera actions), with the map stop focused and its presenter listening: the same keys reach the stop as
/// UI key events, the camera still pans, the focus stays on the stop and nothing is picked. Synchronous, as InputTestFixture allows in edit mode.</summary>
public class MapFocusCameraKeysTests : InputTestFixture
{
    static void Send(MonoBehaviour c, string message) =>
        c.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(c, null);

    [Test]
    public void TheCameraKeysStillPanWithTheStopFocused()
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var kit = new UiTestKit();
        kit.Q<VisualElement>("root").RemoveFromClassList("loading");
        var stop = kit.Q<VisualElement>("map-focus");
        stop.Focus();
        Assert.AreSame(stop, kit.Root.panel.focusController.focusedElement, "precondition: the stop has the focus");
        var go = new GameObject("MapFocusKeys", typeof(Camera));
        var rig = go.AddComponent<CameraRig>();
        rig.LookAt = new Vector3(500000, 0, 500000); rig.Distance = 100000f;
        var c = go.AddComponent<CameraController>();
        c.Bounds = new Rect(0, 0, 1000000, 1000000);
        var picked = 0;
        var presenter = new MapFocusPresenter(kit.Root, go.GetComponent<Camera>(), rig, _ => picked++);
        Send(c, "Awake"); Send(c, "OnEnable");
        try
        {
            var from = rig.LookAt;
            var pan = InputSystem.actions.FindAction("Camera/Pan", true);
            // CameraController's frame with a fixed step: edit mode's Time.unscaledDeltaTime is not a frame's
            void Frame() => CameraController.Step(rig, c.Gather(pan.ReadValue<Vector2>(), Vector2.zero, 0, 0, false, null), 0.1f, c.Bounds);
            // the UI gets the same keys as KeyDownEvents on the focused stop
            void UiKey(KeyCode k) { using (var e = KeyDownEvent.GetPooled('\0', k, EventModifiers.None)) { e.target = stop; stop.SendEvent(e); } }
            Press(keyboard.dKey); UiKey(KeyCode.D);
            for (int i = 0; i < 5; i++) Frame();
            Release(keyboard.dKey);
            Press(keyboard.upArrowKey); UiKey(KeyCode.UpArrow);
            for (int i = 0; i < 5; i++) Frame();
            Release(keyboard.upArrowKey);
            Assert.AreNotEqual(from, rig.LookAt, "D and Up still pan the camera");
            Assert.AreSame(stop, kit.Root.panel.focusController.focusedElement, "the focus stays on the stop");
            Assert.AreEqual(0, picked, "a pan picks nothing");
        }
        finally { presenter.Dispose(); Send(c, "OnDisable"); Object.DestroyImmediate(go); }
    }
}
