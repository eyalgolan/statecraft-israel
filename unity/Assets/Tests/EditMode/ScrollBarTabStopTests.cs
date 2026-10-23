#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#41 K3, widened: a scroll bar is no use from the keyboard, so none of them should take a Tab stop. #1453
/// took the Cabinet's two out of the order, which is where the audit found the stop, because the seat list is the one
/// long enough to show its bar in a 1280x720 window. Every other view's slider stayed focusable, and a ScrollView hides
/// its scroller only while the content fits, so the stop comes back as soon as a view overflows: a shorter window, or
/// enough content in the list.
///
/// These run in a 1280x420 window, the short window the 14 October audit also used, so the Budget's and Defence's
/// views overflow and their bars are really in the order. Defence is the worst case: it has no control of its own at
/// the opening turn, so its bar is very nearly the only thing a keyboard player can land on.
///
/// `turn-report-rows`, `end-screen-rows` and `capture-scroll` are deliberately not swept: their bar is the only way a
/// keyboard player can scroll them, and that exclusion is pinned below so it cannot be dropped by accident.</summary>
public class ScrollBarTabStopTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-bars-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (ps != null) Object.DestroyImmediate(ps);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    Focusable? Focused => ui.panel?.focusController?.focusedElement;
    VisualElement At => Focused as VisualElement ?? throw new AssertionException("nothing has the focus");
    string FocusedName => (Focused as VisualElement)?.name is string n && n.Length > 0 ? n : Focused == null ? "none" : Focused.GetType().Name;
    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Tab()
    {
        var at = At;
        using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
        yield return null;
    }

    IEnumerator Enter()
    {
        var at = At;
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return null;
    }

    /// <summary>A 1280x420 window: short enough that the Budget's and Defence's views overflow and show their bars.</summary>
    IEnumerator BootShort()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 420, 0);
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
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(3);
    }

    static bool IsBar(VisualElement e) => e is Scroller || e.GetFirstAncestorOfType<Scroller>() != null;

    /// <summary>Opens the screen from its rail button, presses Tab 45 times and returns the scroll views whose bar took
    /// a stop inside that screen. 45 is comfortably more than a lap of the HUD and any one screen today; it is a fixed
    /// count rather than "until the focus comes back", so a screen that grows past it would under-report rather than
    /// loop.</summary>
    IEnumerator BarStops(string screen, List<string> found)
    {
        var nav = ui.Q<Button>("nav-" + screen);
        Assert.IsNotNull(nav, $"precondition: the rail has a button for {screen}");
        nav.Focus();
        yield return null;
        yield return Enter();
        yield return Frames(5);
        var panel = ui.Q<VisualElement>("screen-" + screen);
        // Unique elements, not every hit: 45 presses is more than one lap on a short screen, so the same bar comes
        // round again and counting hits would overstate it (the owner's review of 3923ce06).
        var seen = new HashSet<VisualElement>();
        for (int i = 0; i < 45; i++)
        {
            yield return Tab();
            if (Focused is VisualElement at && IsBar(at) && panel != null && panel.Contains(at) && seen.Add(at))
                found.Add(at.GetFirstAncestorOfType<ScrollView>()?.name ?? "?");
        }
    }

    [UnityTest]
    [TestCase("budget", "budget-rows", ExpectedResult = null)]
    [TestCase("defence", "defence-body", ExpectedResult = null)]
    public IEnumerator AnOverflowingScreensScrollBarIsNotATabStop(string screen, string view)
    {
        yield return BootShort();

        var found = new List<string>();
        yield return BarStops(screen, found);

        // precondition: the view really does overflow here, so its bar is shown and this test means something
        var sv = ui.Q<ScrollView>(view);
        Assert.IsNotNull(sv, $"precondition: {view} is in the tree");
        Assert.Greater(sv.contentContainer.layout.height, sv.contentViewport.layout.height + 0.5f,
            $"precondition: {view} overflows in a 1280x420 window, so its bar is shown");
        Assert.AreEqual(DisplayStyle.Flex, sv.verticalScroller.resolvedStyle.display,
            $"precondition: {view}'s scroller is shown");

        Assert.IsEmpty(found, $"no scroll bar takes a Tab stop in {screen}, but one lap stopped on: {string.Join(", ", found)}");
    }

    /// <summary>The Cabinet, which #1453 already fixed, stays at zero in the short window too.</summary>
    [UnityTest]
    public IEnumerator TheCabinetStaysFreeOfBarStopsInAShortWindow()
    {
        yield return BootShort();
        var found = new List<string>();
        yield return BarStops("cabinet", found);
        Assert.IsEmpty(found, $"the Cabinet's bars stay out of the order, but one lap stopped on: {string.Join(", ", found)}");
    }

    /// <summary>Every ScrollView in the tree has both of its bars out of the order, except the three whose bar is the
    /// only way a keyboard player can scroll them. `KeyScroll` has no target for `turn-report-rows`,
    /// `end-screen-rows` or `capture-scroll`, and `GameRoot` switches PageUp, PageDown, Home and End off entirely
    /// while the report or the end screen is open, so taking their bar out would leave no keyboard route at all.</summary>
    [UnityTest]
    public IEnumerator NoScrollViewInTheTreeKeepsAFocusableBar()
    {
        yield return BootShort();
        var exempt = new[] { "turn-report-rows", "end-screen-rows", "capture-scroll" };
        var bad = new List<string>();
        foreach (var sv in ui.Query<ScrollView>().Build())
        {
            if (System.Array.IndexOf(exempt, sv.name) >= 0) continue;
            // both bars, as the helper itself sets: "every scroll bar" means the horizontal one too
            foreach (var (which, s) in new[] { ("vertical", sv.verticalScroller), ("horizontal", sv.horizontalScroller) })
            {
                if (s == null) continue;
                var at = $"{sv.name}.{which}";
                if (s.focusable) bad.Add(at + ".scroller");
                if (s.slider != null && s.slider.focusable) bad.Add(at + ".slider");
                if (s.lowButton != null && s.lowButton.focusable) bad.Add(at + ".lowButton");
                if (s.highButton != null && s.highButton.focusable) bad.Add(at + ".highButton");
            }
        }
        Assert.IsEmpty(bad, $"these scroll bars can still take the focus: {string.Join(", ", bad)}");
    }

    /// <summary>The exclusion is deliberate, so it is pinned: the three views the keyboard cannot otherwise scroll
    /// keep a focusable bar. If `KeyScroll` is ever given a route to them, this test should be deleted along with the
    /// exclusion in `FocusScroll.OnlyReachableByTheirBar` (#41 K6, the owner's review).</summary>
    [UnityTest]
    public IEnumerator TheViewsTheKeyboardCannotOtherwiseScrollKeepTheirBar()
    {
        yield return BootShort();
        foreach (var name in new[] { "turn-report-rows", "end-screen-rows", "capture-scroll" })
        {
            var sv = ui.Q<ScrollView>(name);
            Assert.IsNotNull(sv, $"precondition: {name} is in the tree");
            Assert.IsTrue(sv.verticalScroller.slider.focusable,
                $"{name} has no keyboard scroll route but its bar, so the bar stays reachable");
        }
    }
}
