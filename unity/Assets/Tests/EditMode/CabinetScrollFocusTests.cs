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

/// <summary>#41, the keyboard-only turn's "Still open" list (owner, 23 October 2026), item 1: the Cabinet's two scroll
/// bars are each an extra Tab stop. A ScrollView's scroller is always in sight and cannot be operated usefully from the
/// keyboard here, so it should not take a Tab stop at all. Same harness as KeyboardTurnTests: Map.unity at 1280x720, a
/// new GDD game, keys sent as the UI Toolkit events the Input System's UI provider makes of them.</summary>
public class CabinetScrollFocusTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-kbscroll-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    SimBridge Bridge => root.Bridge!;
    Focusable? Focused => ui.panel?.focusController?.focusedElement;
    VisualElement At => Focused as VisualElement ?? throw new AssertionException("nothing has the focus");
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;

    /// <summary>The scroll view an element sits inside, if any: that is what makes a stop a scroll-bar stop rather than a
    /// control the player meant to reach.</summary>
    static string? ScrollViewAbove(Focusable? f)
    {
        for (var p = f as VisualElement; p != null; p = p.parent)
            if (p is ScrollView sv) return sv.name is string n && n.Length > 0 ? n : "(unnamed ScrollView)";
        return null;
    }

    static bool IsScrollBarStop(Focusable? f) =>
        f is Scroller || f is Slider || (f as VisualElement)?.GetType().Name is "ScrollerSlider";

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Tab(bool back = false)
    {
        var at = At;
        using (var e = NavigationMoveEvent.GetPooled(back ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Enter()
    {
        var at = At;
        using (var k = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { k.target = at; at.SendEvent(k); }
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return null;
    }

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

    /// <summary>Answers the opening card and opens the Cabinet from the keyboard, as the keyboard-only turn does.</summary>
    IEnumerator OpenCabinet()
    {
        yield return Boot();
        yield return Tab();
        yield return Enter();
        yield return Frames(3);
        for (int i = 0; i < 30 && FocusedName != "nav-cabinet"; i++) yield return Tab();
        Assert.AreEqual("nav-cabinet", FocusedName, "precondition: reached the Cabinet button");
        yield return Enter();
        yield return Frames(5);
    }

    /// <summary>Tabs a whole lap and returns every stop that sits inside a ScrollView and is a scroll bar.</summary>
    IEnumerator ScrollBarStops(List<string> into, int laps = 60)
    {
        for (int i = 0; i < laps; i++)
        {
            if (IsScrollBarStop(Focused))
                into.Add($"{FocusedName} in {ScrollViewAbove(Focused) ?? "(no ScrollView)"}");
            yield return Tab();
        }
    }

    [UnityTest]
    public IEnumerator TheCabinetsSeatListScrollBarIsNotATabStop()
    {
        yield return OpenCabinet();
        var stops = new List<string>();
        yield return ScrollBarStops(stops);
        CollectionAssert.IsEmpty(stops, "a scroll bar took a Tab stop in the Cabinet: " + string.Join(", ", stops));
    }

    [UnityTest]
    public IEnumerator TheCabinetsCandidateListScrollBarIsNotATabStop()
    {
        yield return OpenCabinet();
        // open a seat so the candidate list renders; its scroll bar is only in the order once it is shown
        for (int i = 0; i < 40 && !FocusedName.StartsWith("seat-"); i++) yield return Tab();
        Assert.IsTrue(FocusedName.StartsWith("seat-"), "precondition: reached a minister's seat");
        yield return Enter();
        yield return Frames(5);
        Assert.IsTrue(ui.Q<ScrollView>("cabinet-candidates")?.resolvedStyle.display == DisplayStyle.Flex,
            "precondition: the candidate list is shown");

        var stops = new List<string>();
        yield return ScrollBarStops(stops);
        CollectionAssert.IsEmpty(stops, "a scroll bar took a Tab stop with the candidate list open: " + string.Join(", ", stops));
    }
}
