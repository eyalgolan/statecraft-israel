#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#41, the keyboard-only turn's last "Still open" item: from a screen's rail button it took about 16 Tabs to
/// reach the screen's first control. `ScreenNav.Open` showed the screen but never handed the focus to it, so the focus
/// stayed on the rail button and Tab walked the rest of the HUD, the rail, New Game, End Turn, the minimap's three
/// buttons and star/report/share, before wrapping into the screen. `Close` already hands the focus back to the rail
/// button (`ScreenNav`, `FocusReturn.To(button)`), so open was the asymmetric half. Same harness as
/// `KeyboardTurnTests`.</summary>
public class ScreenEntryTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-screenentry-" + Guid.NewGuid().ToString("N"));

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

    IEnumerator Enter()
    {
        var at = At;
        using (var k = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { k.target = at; at.SendEvent(k); }
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return null;
    }

    IEnumerator Escape()
    {
        var at = At;
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = at; at.SendEvent(e); }
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
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(3);
    }

    /// <summary>Opens a screen the way a keyboard player does: focus its rail button, press Enter.</summary>
    IEnumerator OpenFromTheRail(string screen)
    {
        var nav = ui.Q<Button>("nav-" + screen);
        Assert.IsNotNull(nav, $"precondition: the rail has a button for {screen}");
        nav.Focus();
        yield return null;
        Assert.AreEqual("nav-" + screen, FocusedName, "precondition: the rail button has the focus before Enter");
        yield return Enter();
        yield return Frames(5);
    }

    [UnityTest]
    [TestCase("cabinet", "seat-prime_minister", ExpectedResult = null)]
    [TestCase("budget", "budget-defense_minister-down", ExpectedResult = null)]
    public IEnumerator OpeningAScreenFromTheKeyboardPutsTheFocusOnItsFirstControl(string screen, string expected)
    {
        yield return Boot();
        yield return OpenFromTheRail(screen);

        var panel = ui.Q<VisualElement>("screen-" + screen);
        Assert.IsNotNull(panel, $"precondition: screen-{screen} is in the tree");
        Assert.IsTrue(panel.Contains(At),
            $"the focus should be inside the screen the moment it opens, but it is on {FocusedName}");
        // and on the content, not the X: the header's close button is first in the screen's own order, so without
        // skipping it the first thing a keyboard player could do with a screen they just opened is close it.
        Assert.AreEqual(expected, FocusedName, "and on the screen's first content control");
        Assert.AreNotEqual("screen-close-" + screen, FocusedName, "not the close X");
    }

    /// <summary>Defence is the other case: at the opening turn it has no control of its own that can take the focus
    /// (its ladder and call-up buttons are in the tree but not usable yet, and its scroll bar is no use from the
    /// keyboard, #41 K3), so the screen's own X takes it. That is still better than leaving the focus on the rail
    /// outside the screen the player just opened. The precondition is checked rather than assumed, so if Defence later
    /// gains a usable control (#755 adds the call-up) this test says so instead of quietly passing.</summary>
    [UnityTest]
    public IEnumerator AScreenWithNoUsableControlOfItsOwnTakesItsCloseButton()
    {
        yield return Boot();
        yield return OpenFromTheRail("defence");

        var panel = ui.Q<VisualElement>("screen-defence");
        var x = ui.Q<Button>("screen-close-defence");
        // Measured at the opening turn: de-escalate, escalate, mob-down and mob-up are all display:none, and the
        // defence-body scroll bar is no use from the keyboard, so nothing in the screen but the X can hold the focus.
        var usable = panel.Query<VisualElement>().Build()
            .Where(e => e != panel && e != x && FocusReturn.CanTakeFocus(e)
                        && !(e is Scroller) && e.GetFirstAncestorOfType<Scroller>() == null)
            .ToList();
        Assert.IsEmpty(usable, $"precondition: Defence has no control of its own that can take the focus at the opening turn");
        Assert.IsTrue(panel.Contains(At), $"the focus should be inside the screen, but it is on {FocusedName}");
        Assert.AreEqual("screen-close-defence", FocusedName, "and on its X, the only thing in it that can hold the focus");
    }

    /// <summary>The pointer must not move the focus (PointerBlur's rule), as Close already respects.</summary>
    [UnityTest]
    public IEnumerator APointerPressDoesNotHandTheFocusToTheScreen()
    {
        yield return Boot();
        var endTurn = ui.Q<Button>("end-turn");
        endTurn.Focus();
        yield return null;

        DecisionCardTests.PressWithPointer(ui.Q<Button>("nav-cabinet"));
        yield return Frames(5);

        Assert.AreEqual("cabinet", ScreenOpen(), "precondition: the press opened the Cabinet");
        var panel = ui.Q<VisualElement>("screen-cabinet");
        Assert.IsFalse(panel.Contains(At), $"a pointer press moves no focus, but it is on {FocusedName}");
    }

    /// <summary>Escape still hands the focus back to the rail button it came from.</summary>
    [UnityTest]
    public IEnumerator EscapeStillReturnsTheFocusToTheRailButton()
    {
        yield return Boot();
        yield return OpenFromTheRail("cabinet");
        yield return Escape();
        yield return Frames(3);
        Assert.IsNull(ScreenOpen(), "Escape closes the screen");
        Assert.AreEqual("nav-cabinet", FocusedName, "and the focus is back on the rail button");
    }

    string? ScreenOpen() => ScreenNav.Screens.FirstOrDefault(s => ui.Q("nav-" + s).ClassListContains(ScreenNav.OpenClass));
}
