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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#41's done-when (owner, 22 October 2026): one GDD turn played with the keyboard alone (answer the event card,
/// change a budget line, appoint a minister, give a training order in a city, end the turn), and the focus order of the
/// event card and of End Turn pinned. Map.unity as the player gets it, at 1280x720. Keys reach the UI as the UI Toolkit
/// events the Input System's UI provider makes of them (Tab and Shift+Tab: NavigationMoveEvent Next and Previous; Enter:
/// KeyDownEvent then NavigationSubmitEvent; Escape: NavigationCancelEvent), as in MapFocusTests: an edit-mode panel gets no
/// device input.</summary>
public class KeyboardTurnTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-kbturn-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    /// <summary>Map.unity on a 1280x720 panel, a new GDD game begun as GameBoot.Ready does with no start screen: the opening
    /// event card is open and has the focus.</summary>
    IEnumerator Boot()
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
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        Assert.IsTrue(Bridge.Content.IsGdd, "precondition: the GDD loop");
    }

    SimBridge Bridge => root.Bridge!;
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    Focusable? Focused => ui.panel.focusController.focusedElement;
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;
    VisualElement At => Focused as VisualElement ?? throw new AssertionException("nothing has the focus");

    IEnumerator Tab(bool back = false)
    {
        var at = At;
        using (var e = NavigationMoveEvent.GetPooled(back ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Enter()
    {
        var at = At;
        using (var e = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { e.target = at; at.SendEvent(e); }
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
        yield return Frames(3);
    }

    IEnumerator Escape()
    {
        var at = At;
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
        yield return Frames(3);
    }

    /// <summary>Tabs until <paramref name="want"/> has the focus, at most <paramref name="max"/> times; fails with the path
    /// taken.</summary>
    IEnumerator TabTo(Func<VisualElement, bool> want, string what, int max = 60)
    {
        var seen = new List<string> { FocusedName };
        for (int i = 0; i < max && !want(At); i++) { yield return Tab(); seen.Add(FocusedName); }
        Assert.IsTrue(want(At), $"Tab reaches {what}: {string.Join(" > ", seen)}");
    }

    IEnumerator TabTo(string name) => TabTo(v => v.name == name, name);

    /// <summary>The names Tab (or Shift+Tab) visits from the focused element, <paramref name="n"/> presses.</summary>
    IEnumerator Order(List<string> into, int n, bool back = false)
    {
        into.Add(FocusedName);
        for (int i = 0; i < n; i++) { yield return Tab(back); into.Add(FocusedName); }
    }

    /// <summary>The side menu, New Game, End Turn: the HUD's own controls, in the order Game.uxml gives them.</summary>
    static readonly string[] Menu = { "nav-cabinet", "nav-budget", "nav-defence", "nav-why", "nav-timeline", "nav-how-to-play", "nav-objectives", "nav-settings", "new-game", "end-turn" };

    /// <summary>The open card takes the focus; Tab runs the card, each option in its order, Hide, then the HUD from the side
    /// menu on (the map stop, the minimap and the info panel are under the card's backdrop and out of reach), and round to
    /// the card again; Shift+Tab runs the same order backwards.</summary>
    [UnityTest]
    public IEnumerator TheEventCardsFocusOrderIsTheCardItsOptionsHideThenTheHud()
    {
        yield return Boot();
        Assert.AreEqual("event-card", FocusedName, "the open card has the focus");
        var options = ui.Q("event-options").Query<Button>().ToList().Select(b => b.name).ToList();
        Assert.GreaterOrEqual(options.Count, 2, "precondition: the opening card offers a choice");
        Assert.AreEqual(DisplayStyle.None, ui.Q("event-illustration").resolvedStyle.display, "precondition: no clip, so no Illustration switch");
        var expected = new[] { "event-card" }.Concat(options).Concat(new[] { "event-hide" }).Concat(Menu)
            .Concat(new[] { "hud-star", "hud-report", "hud-share", "event-card" }).ToList();
        var seen = new List<string>();
        yield return Order(seen, expected.Count - 1);
        CollectionAssert.AreEqual(expected, seen, "Tab from the card: " + string.Join(" > ", seen));
        var back = new List<string>();
        yield return Order(back, expected.Count - 1, back: true);
        expected.Reverse();
        CollectionAssert.AreEqual(expected, back, "Shift+Tab from the card: " + string.Join(" > ", back));
    }

    /// <summary>With no card open (the opening one answered from the keyboard, which hands the focus to End Turn), End Turn
    /// comes after the side menu and New Game; Tab from it goes to the map stop, the minimap's buttons, Star, Report and
    /// Share, and round to the side menu; Shift+Tab from it goes back to New Game. Enter on it ends the turn: the turn report
    /// takes the focus, and closing it with Enter hands the focus on (to the next card if one opens, else End Turn), so
    /// the next turn is played from the keyboard too.</summary>
    [UnityTest]
    public IEnumerator EndTurnsPlaceInTheOrderAndWhereTheFocusGoesAfterIt()
    {
        yield return Boot();
        yield return TabTo(v => v.ClassListContains(EventCardPresenter.OptionClass), "an option");
        yield return Enter();
        Assert.AreEqual("end-turn", FocusedName, "the last card answered from the keyboard hands the focus to End Turn");
        var expected = new[] { "end-turn", "map-focus", "minimap-overview", "minimap-labels", "minimap-borders", "hud-star", "hud-report", "hud-share" }
            .Concat(Menu).ToList();
        var seen = new List<string>();
        yield return Order(seen, expected.Count - 1);
        CollectionAssert.AreEqual(expected, seen, "Tab from End Turn round the HUD: " + string.Join(" > ", seen));
        var back = new List<string>();
        yield return Order(back, 1, back: true);
        CollectionAssert.AreEqual(new[] { "end-turn", "new-game" }, back, "Shift+Tab from End Turn");
        yield return TabTo("end-turn");

        var turn = Bridge.State.Turn;
        yield return Enter();
        yield return Frames(5);
        Assert.AreEqual(turn + 1, Bridge.State.Turn, "Enter on End Turn ends the turn");
        Assert.AreEqual("turn-report-close", FocusedName, "the turn report takes the focus");
        yield return Enter();
        yield return Frames(5);
        var card = ui.Q("event-card");
        var next = card.enabledInHierarchy && card.resolvedStyle.display == DisplayStyle.Flex ? "event-card" : "end-turn";
        Assert.AreEqual(next, FocusedName, "closing the report hands the focus on");
    }

    /// <summary>The done-when's turn, keys only: answer the opening card; open Budget, raise a line with its + and submit;
    /// open the Cabinet, select a seat, appoint a candidate; close it with Escape; Tab to the map stop over Tel Aviv, Enter
    /// to pick it, Tab to Train halutzim in its city panel and press it; Tab to End Turn and end the turn. Each order is
    /// queued, and a press that rebuilds what it is part of keeps the focus there. The opening turn's attention (4) pays for
    /// two of these orders; the test raises it, since what is checked is the keyboard's path, not the attention rule.</summary>
    [UnityTest]
    public IEnumerator AWholeTurnPlaysWithTheKeyboardAlone()
    {
        yield return Boot();
        Bridge.State.Attention = 100;

        // The event card.
        yield return TabTo(v => v.ClassListContains(EventCardPresenter.OptionClass), "an option");
        yield return Enter();
        Assert.IsTrue(Bridge.Queued.OfType<ChooseEventOption>().Any(), "the card is answered");
        Assert.AreEqual("end-turn", FocusedName);

        // A budget line.
        yield return TabTo("nav-budget");
        yield return Enter();
        Assert.AreEqual("budget", ScreenOpen(), "Enter opens Budget");
        yield return TabTo(v => v.name.StartsWith("budget-", StringComparison.Ordinal) && v.name.EndsWith("-up", StringComparison.Ordinal), "a budget line's +");
        var plus = FocusedName;
        yield return Enter();
        Assert.AreEqual(plus, FocusedName, "the + keeps the focus (H2)");
        yield return TabTo("budget-submit");
        yield return Enter();
        Assert.IsTrue(Bridge.Queued.OfType<AllocateBudget>().Any(), "the budget is submitted");

        // A minister.
        yield return TabTo("nav-cabinet");
        yield return Enter();
        Assert.AreEqual("cabinet", ScreenOpen(), "Enter opens the Cabinet");
        yield return TabTo(v => v.name.StartsWith("seat-", StringComparison.Ordinal) && v.name != "seat-prime_minister", "a minister's seat");
        yield return Enter();
        yield return TabTo(v => ui.Q("cabinet-candidates").Contains(v), "a candidate");
        yield return Enter();
        Assert.IsTrue(Bridge.Queued.OfType<Appoint>().Any(), "the appointment is ordered");
        // The candidate list's and the seat list's own scroll bars take Tab after their last row: no error, and Tab goes on.
        yield return TabTo(v => v.name == "seat-chief_of_staff", "the last seat, past every candidate", 120);
        yield return Tab();
        Assert.IsTrue(At is Scroller || At.GetFirstAncestorOfType<Scroller>() != null, $"the Tab after the last seat lands on a scroll bar (K1), not {FocusedName}");
        yield return TabTo("nav-cabinet");
        yield return Escape();
        Assert.IsNull(ScreenOpen(), "Escape closes the Cabinet");
        Assert.AreEqual("nav-cabinet", FocusedName, "and hands the focus to its button (M1)");

        // A training order in a city.
        yield return TabTo("map-focus");
        LookAtTelAviv();
        yield return Frames(3);
        yield return Enter();
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text, "Enter on the map stop picks Tel Aviv");
        yield return TabTo("city-train-halutz");
        yield return Enter();
        Assert.IsTrue(Bridge.Queued.OfType<Train>().Any(t => t.Track == "halutz"), "the training order is queued");
        Assert.AreEqual("city-train-halutz", FocusedName, "the rebuilt panel gives the focus back to Train halutzim");

        // End the turn.
        yield return TabTo("end-turn");
        var turn = Bridge.State.Turn;
        yield return Enter();
        yield return Frames(5);
        Assert.AreEqual(turn + 1, Bridge.State.Turn, "the turn ends");
    }

    /// <summary>The counterpart: Train halutzim pressed with the mouse gives the rebuilt button no focus (PointerBlur's rule,
    /// a click leaves no focus fill behind).</summary>
    [UnityTest]
    public IEnumerator ATrainingOrderByThePointerLeavesNoFocus()
    {
        yield return Boot();
        Bridge.State.Attention = 100;
        GameSceneTests.HideTheOpeningCard(ui);
        LookAtTelAviv();
        var stop = ui.Q("map-focus");
        for (int i = 0; i < 10 && Focused != stop; i++) { yield return null; stop.Focus(); }
        yield return Enter();
        Assert.AreEqual("Tel Aviv", ui.Q<Label>("info-title").text, "precondition: Tel Aviv's city panel");
        DecisionCardTests.PressWithPointer(ui.Q<Button>("city-train-halutz"));
        yield return Frames(3);
        Assert.IsTrue(Bridge.Queued.OfType<Train>().Any(t => t.Track == "halutz"), "the click queues the order");
        Assert.AreNotEqual("city-train-halutz", FocusedName, "and leaves the rebuilt button unlit");
    }

    string? ScreenOpen() => ScreenNav.Screens.FirstOrDefault(s => ui.Q("nav-" + s).ClassListContains(ScreenNav.OpenClass));

    /// <summary>Moves the camera's look-at point onto Tel Aviv's hex, on the ground, as a pan would (MapFocusTests).</summary>
    static void LookAtTelAviv()
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var c = MapFocusTests.Inputs.Content;
        var tlv = c.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var hex = c.HexAt(tlv.Q, tlv.R)!;
        var ground = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        ground.y = map.Terrain.SampleHeight(ground) + map.Terrain.transform.position.y;
        var rig = Camera.main.GetComponent<CameraRig>();
        rig.LookAt = ground; rig.Distance = 30000f;
        rig.Apply();
    }
}
