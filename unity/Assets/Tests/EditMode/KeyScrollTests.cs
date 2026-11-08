#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Accessibility audit H3 (#41): PageUp, PageDown, Home and End scroll the open screen's list, or the open decision
/// card's text, from the keyboard (KeyScroll), since those panels hold labels that Tab never reaches and the arrows pan the
/// camera.</summary>
public class KeyScrollTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    /// <summary>A real panel in the playtest window (1568x752) with the game's UXML.</summary>
    sealed class Rig : IDisposable
    {
        public readonly VisualElement Root;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        readonly List<IDisposable> owned = new List<IDisposable>();

        public Rig()
        {
            rt = new RenderTexture(1568, 752, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("KeyScrollTest");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement;
            Root.Q("root")?.RemoveFromClassList("loading");
        }

        public T Own<T>(T d) where T : IDisposable { owned.Add(d); return d; }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    /// <summary>Holds a view to <paramref name="height"/> px, so its content overflows whatever the window.</summary>
    static void Shorten(ScrollView view, float height)
    {
        view.style.height = height; view.style.minHeight = height; view.style.maxHeight = height; view.style.flexGrow = 0;
    }

    /// <summary>PageDown moves one view less a line, End reaches the bottom, PageUp moves back and Home reaches the top; at
    /// the top PageUp does nothing. The focus never moves.</summary>
    static void PagesThrough(VisualElement root, ScrollView view, string? screen, bool cardOpen, string where)
    {
        var focused = root.panel.focusController.focusedElement;
        float high = view.verticalScroller.highValue, page = Mathf.Max(24f, view.contentViewport.layout.height - 24f);
        Assert.Greater(high, page, where + ": precondition, the view overflows by more than a page");
        Assert.AreEqual(0f, view.scrollOffset.y, 0.5f, where + ": precondition, at the top");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageDown), where + ": PageDown scrolls");
        Assert.AreEqual(Mathf.Min(page, high), view.scrollOffset.y, 0.5f, where + ": PageDown moves one view less a line");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.End), where + ": End scrolls");
        Assert.AreEqual(high, view.scrollOffset.y, 0.5f, where + ": End reaches the bottom");
        Assert.IsFalse(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageDown), where + ": PageDown at the bottom");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageUp), where + ": PageUp scrolls");
        Assert.AreEqual(high - page, view.scrollOffset.y, 0.5f, where + ": PageUp moves one view less a line");
        Assert.IsTrue(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.Home), where + ": Home scrolls");
        Assert.AreEqual(0f, view.scrollOffset.y, 0.5f, where + ": Home reaches the top");
        Assert.IsFalse(KeyScroll.Press(root, screen, cardOpen, KeyScroll.Key.PageUp), where + ": PageUp at the top");
        Assert.AreSame(focused, root.panel.focusController.focusedElement, where + ": the keys move no focus");
    }

    [UnityTest]
    public IEnumerator ThePageKeysScrollTheWhyListAndTheDefenceBody()
    {
        using var r = new Rig();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        r.Own(new DefencePresenter(r.Root, bridge, Text));
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn();
        foreach (var (screen, view) in new[] { ("why", "why-rows"), ("defence", "defence-body") })
        {
            var v = r.Root.Q<ScrollView>(view);
            Shorten(v, 80f);
            nav.Open(screen);
            yield return Frames(4);
            PagesThrough(r.Root, v, screen, false, screen);
        }
    }

    /// <summary>An open encyclopedia page hides the list, so the keys scroll the page's body (#2095 review).</summary>
    [UnityTest]
    public IEnumerator ThePageKeysScrollAnOpenEncyclopediaPage()
    {
        using var r = new Rig();
        var nav = r.Own(new ScreenNav(r.Root));
        var pedia = r.Own(new EncyclopediaPresenter(r.Root, Text, AdvisorCatalog.Load(), nav, null));
        pedia.Open("cities");
        var body = r.Root.Q<ScrollView>("encyclopedia-page-body");
        Shorten(body, 40f);
        yield return Frames(4);
        PagesThrough(r.Root, body, "encyclopedia", false, "encyclopedia page");
    }

    [UnityTest]
    public IEnumerator FocusInTheCandidateListScrollsThatListNotTheCards()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 2, 1)) g.Step(HistoricalAI.Commands(C, g.State));
        var nav = r.Own(new ScreenNav(r.Root));
        var cabinet = r.Own(new CabinetPresenter(r.Root, new SimBridge(g, Text), Text, null));
        nav.Open("cabinet");
        cabinet.Select("finance_minister");
        var candidates = r.Root.Q<ScrollView>("cabinet-candidates");
        var seats = r.Root.Q<ScrollView>("cabinet-seats");
        Shorten(candidates, 60f);
        yield return Frames(4);
        Assert.Greater(seats.verticalScroller.highValue, 0f, "precondition: the cards overflow too");
        var first = candidates.contentContainer.Query<Button>().ToList().First(b => b.canGrabFocus);
        first.Focus();
        yield return Frames(2);
        Assert.AreSame(first, r.Root.panel.focusController.focusedElement, "precondition: a candidate has the focus");
        PagesThrough(r.Root, candidates, "cabinet", false, "candidates");
        Assert.AreEqual(0f, seats.scrollOffset.y, 0.5f, "the cards did not move");

        first.Blur();
        yield return Frames(2);
        Assert.IsTrue(KeyScroll.Press(r.Root, "cabinet", false, KeyScroll.Key.PageDown), "no focus in a list: the cards scroll");
        Assert.Greater(seats.scrollOffset.y, 0f, "the cards moved");
    }

    [UnityTest]
    public IEnumerator TheCardsTextScrollsOnlyWhileTheCardIsOpenAndNoScreenCoversIt()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains("armistice_egypt")) g.Step(new Command[0]);
        var bridge = new SimBridge(g, Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        var card = r.Own(new EventCardPresenter(r.Root, bridge, Text, null));
        var text = r.Root.Q<ScrollView>("event-scroll");
        Shorten(text, 16f);
        yield return Frames(4);
        Assert.IsNotNull(card.Shown, "precondition: the card is open");
        Assert.IsFalse(card.Collapsed, "precondition: the card is not hidden");
        Assert.Greater(text.worldBound.height, 0f, "precondition: the card's text is laid out");
        Assert.IsFalse(KeyScroll.Press(r.Root, null, false, KeyScroll.Key.PageDown), "a hidden card's text does not scroll");
        Assert.AreEqual(0f, text.scrollOffset.y, 0.5f, "hidden card");
        nav.Open("why");
        yield return Frames(2);
        KeyScroll.Press(r.Root, "why", true, KeyScroll.Key.PageDown);
        Assert.AreEqual(0f, text.scrollOffset.y, 0.5f, "an open screen takes the keys, not the card under it");
        nav.Open(null);
        yield return Frames(4);
        PagesThrough(r.Root, text, null, true, "event text");
    }

    /// <summary>A game on its last day, ended as the MVP ends (as EndScreenTests.Ended), so the end screen opens.</summary>
    static GameSession Ended()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.End; g.State.GameOver = GameOverCodes.MvpEnded;
        return g;
    }

    /// <summary>H3, K6 (#2479): the turn report's rows page from the keyboard; its rows are labels, so Tab never reaches them.</summary>
    [UnityTest]
    public IEnumerator ThePageKeysScrollTheTurnReportH3K6()
    {
        using var r = new Rig();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = r.Own(new ScreenNav(r.Root));
        var report = r.Own(new TurnReportPresenter(r.Root, bridge, Text, nav));
        bridge.EndTurn();
        var rows = r.Root.Q<ScrollView>("turn-report-rows");
        Shorten(rows, 16f);
        yield return Frames(4);
        Assert.IsTrue(report.IsOpen, "precondition: the report is open");
        PagesThrough(r.Root, rows, null, false, "turn report");
    }

    /// <summary>H3, K6 (#2479): the ending's rows page from the keyboard.</summary>
    [UnityTest]
    public IEnumerator ThePageKeysScrollTheEndingH3K6()
    {
        using var r = new Rig();
        var end = r.Own(new EndScreenPresenter(r.Root, new SimBridge(Ended(), Text), Text, null));
        var rows = r.Root.Q<ScrollView>("end-screen-rows");
        Shorten(rows, 16f);
        yield return Frames(4);
        Assert.IsTrue(end.IsOpen, "precondition: the end screen is open");
        PagesThrough(r.Root, rows, null, false, "ending");
    }

    /// <summary>H3, K6 (#2479): the capture dialog's text and options page from the keyboard while it shows, and the info
    /// panel's facts page while no dialog, card or screen covers it.</summary>
    [UnityTest]
    public IEnumerator ThePageKeysScrollTheCaptureDialogThenTheInfoPanelH3K6()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear();
        g.State.Settlements["lod"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, g.State.Date);
        var bridge = new SimBridge(g, Text);
        r.Own(new ScreenNav(r.Root));
        var info = r.Own(new InfoPanelPresenter(r.Root, bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)));
        var card = r.Own(new EventCardPresenter(r.Root, bridge, Text, null));
        var dialog = r.Own(new CaptureDialogPresenter(r.Root, bridge, Text, card, new Dictionary<string, string>()));
        var text = r.Root.Q<ScrollView>("capture-scroll");
        Shorten(text, 16f);
        yield return Frames(4);
        Assert.AreEqual("lod", dialog.Shown, "precondition: the dialog shows");
        PagesThrough(r.Root, text, null, false, "capture dialog");

        dialog.Collapse(takeFocus: false);
        var hex = C.SettlementHexes.First(x => x.Id == "tel_aviv");
        info.Show(hex.Q, hex.R);
        var panel = r.Root.Q<ScrollView>("info-panel");
        Shorten(panel, 16f);
        yield return Frames(4);
        Assert.IsTrue(panel.enabledInHierarchy, "precondition: nothing covers the info panel");
        PagesThrough(r.Root, panel, null, false, "info panel");
    }

    /// <summary>H3, K6 (#2479): the topmost open view takes the keys: the ending over the turn report, the report over an
    /// open screen, and nothing under them moves.</summary>
    [UnityTest]
    public IEnumerator TheTopmostOpenViewTakesThePageKeysH3K6()
    {
        using var r = new Rig();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var bridge = new SimBridge(g, Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        var report = r.Own(new TurnReportPresenter(r.Root, bridge, Text, nav));
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn();
        nav.Open("why");
        var why = r.Root.Q<ScrollView>("why-rows"); var rows = r.Root.Q<ScrollView>("turn-report-rows");
        Shorten(why, 40f); Shorten(rows, 16f);
        yield return Frames(4);
        Assert.IsTrue(report.IsOpen, "precondition: the report is open over the Why screen");
        Assert.Greater(why.verticalScroller.highValue, 0f, "precondition: the Why list overflows too");
        PagesThrough(r.Root, rows, "why", false, "report over a screen");
        Assert.IsTrue(KeyScroll.Press(r.Root, "why", false, KeyScroll.Key.End), "End under the report");
        Assert.AreEqual(0f, why.scrollOffset.y, 0.5f, "the screen under the report did not move");

        g.State.Date = C.End; g.State.GameOver = GameOverCodes.MvpEnded;
        var end = r.Own(new EndScreenPresenter(r.Root, bridge, Text, null));
        var ending = r.Root.Q<ScrollView>("end-screen-rows");
        Shorten(ending, 16f);
        rows.scrollOffset = Vector2.zero;
        yield return Frames(4);
        Assert.IsTrue(end.IsOpen && report.IsOpen, "precondition: the ending is open over the report");
        PagesThrough(r.Root, ending, "why", false, "ending over the report");
        Assert.AreEqual(0f, rows.scrollOffset.y, 0.5f, "the report under the ending did not move");
    }

    [UnityTest]
    public IEnumerator NothingScrollsUnderTheStartScreen()
    {
        using var r = new Rig();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var nav = r.Own(new ScreenNav(r.Root));
        r.Own(new WhyPresenter(r.Root, bridge, Text));
        bridge.EndTurn();
        var rows = r.Root.Q<ScrollView>("why-rows");
        Shorten(rows, 40f);
        nav.Open("why");
        r.Root.Q("root").AddToClassList(StartScreenPresenter.OpenClass);
        yield return Frames(4);
        Assert.Greater(rows.verticalScroller.highValue, 0f, "precondition: the list overflows");
        foreach (KeyScroll.Key key in Enum.GetValues(typeof(KeyScroll.Key)))
            Assert.IsFalse(KeyScroll.Press(r.Root, "why", false, key), key + " under the start screen");
        Assert.AreEqual(0f, rows.scrollOffset.y, 0.5f, "the list did not move");
    }
}

/// <summary>H3, K6 (#2479) through GameRoot's own key poll: PageDown and Home, pressed as real keys, page the open turn report
/// with the focus on its Close button and with no focus at all, and the camera does not move.</summary>
public class KeyScrollThroughGameRootTests : SharedCameraActionsFixture
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-keyscroll-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static void Send(MonoBehaviour c, string message) =>
        c.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(c, null);

    [UnityTest]
    public IEnumerator ThePageKeysPageTheOpenTurnReportAndLeaveTheMapH3K6()
    {
        // The keys as state events: the fixture's Press (Set) does not run in an edit-mode [UnityTest], and the boot needs frames.
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        // Warm the keys: wasPressedThisFrame can miss a press on its first call ever (as in MapPointerTickTests).
        _ = keyboard.pageDownKey.wasPressedThisFrame; _ = keyboard.homeKey.wasPressedThisFrame;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 0);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(MapFocusTests.Inputs, 0);
        var ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        for (int i = 0; i < 5; i++) yield return null;
        GameSceneTests.HideTheOpeningCard(ui);
        root.Bridge!.EndTurn();
        var rows = ui.Q<ScrollView>("turn-report-rows");
        rows.style.height = 16f; rows.style.minHeight = 16f; rows.style.maxHeight = 16f; rows.style.flexGrow = 0;
        for (int i = 0; i < 5; i++) yield return null;
        Assert.IsTrue(ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass), "precondition: the turn report is open");
        Assert.Greater(rows.verticalScroller.highValue, 24f, "precondition: the report's rows overflow by more than a line");
        var cam = Object.FindFirstObjectByType<CameraController>();
        var rig = cam.GetComponent<CameraRig>();
        Send(cam, "Awake"); Send(cam, "OnEnable");
        try
        {
            // One frame with the key down: GameRoot's poll (Update) and the camera's (Tick) both read it.
            void Hold(UnityEngine.InputSystem.Key k)
            {
                var from = rig.LookAt; float distance = rig.Distance, yaw = rig.Yaw;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(k)); InputSystem.Update();
                Assert.IsTrue(keyboard[k].wasPressedThisFrame, "precondition: " + k + " went down this frame");
                Send(root, "Update");
                for (int i = 0; i < 5; i++) cam.Tick(0.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                cam.Tick(0.1f);
                Assert.AreEqual(from, rig.LookAt, k + ": the map does not pan");
                Assert.AreEqual(distance, rig.Distance, k + ": the map does not zoom");
                Assert.AreEqual(yaw, rig.Yaw, k + ": the map does not turn");
            }
            var close = ui.Q<Button>("turn-report-close");
            close.Focus();
            Assert.AreSame(close, ui.panel.focusController.focusedElement, "precondition: the report's Close has the focus");
            Hold(UnityEngine.InputSystem.Key.PageDown);
            Assert.Greater(rows.scrollOffset.y, 0f, "PageDown, focus on Close: the report's rows moved");
            close.Blur();
            Assert.IsNull(ui.panel.focusController.focusedElement, "precondition: nothing has the focus");
            Hold(UnityEngine.InputSystem.Key.Home);
            Assert.AreEqual(0f, rows.scrollOffset.y, 0.5f, "Home, no focus: back at the top");
        }
        finally { Send(cam, "OnDisable"); }
    }
}
