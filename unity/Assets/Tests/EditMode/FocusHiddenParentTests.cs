#nullable enable
using System;
using System.Collections;
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

/// <summary>#41, found while reviewing #1489. `FocusReturn.CanTakeFocus` asked only whether the element itself is
/// drawn (`resolvedStyle.display != None`), not whether an ancestor hides it. An element inside a hidden parent keeps
/// its own `display: Flex`, so it passed the test, and then `Focus()` did not take and the focus was left nowhere,
/// which is the very thing the predicate exists to prevent.
///
/// A closed screen is exactly that shape: `screen-budget` is `display: none` while its `screen-close-budget` button
/// is `Flex` and enabled. Measured on main before the fix:
///
///     screen-close-budget: ownDisplay=Flex enabledInHierarchy=True CanTakeFocus=True
///                          after Focus() the focus is on &lt;null&gt; (took=False)
///
/// It does not bite in the shipped paths today, because every caller passes scopes inside an open panel, but a
/// caller that hands `ToOrWithin` a hidden subtree gets a silent focus drop instead of the fallback.</summary>
public class FocusHiddenParentTests
{
    string saveDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-hidden-" + Guid.NewGuid().ToString("N"));

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

    [UnityTest]
    public IEnumerator AScopeInsideAHiddenParentIsSkippedForTheLastStop()
    {
        yield return Boot();

        var hidden = ui.Q<VisualElement>("screen-budget");
        Assert.IsNotNull(hidden, "precondition: the Budget screen is in the tree");
        Assert.AreEqual(DisplayStyle.None, hidden.resolvedStyle.display, "precondition: it is closed, so display:none");
        var inside = ui.Q<Button>("screen-close-budget");
        Assert.IsNotNull(inside, "precondition: it has a button of its own");
        Assert.AreEqual(DisplayStyle.Flex, inside.resolvedStyle.display,
            "precondition: the button's own display is Flex; only its parent hides it");
        Assert.IsTrue(inside.enabledInHierarchy, "precondition: and it is enabled");

        var last = ui.Q<Button>("end-turn");
        Assert.IsNotNull(last, "precondition: End Turn is the visible last stop");

        // nothing focused to start, so a drop is unambiguous
        (Focused as VisualElement)?.Blur();
        yield return null;

        FocusReturn.ToOrWithin(null, last, hidden);
        yield return Frames(3);

        Assert.IsNotNull(Focused,
            $"the focus is not dropped when the only candidates are inside a hidden parent; it is on {FocusedName}");
        Assert.AreEqual("end-turn", FocusedName,
            "the hidden screen's own button is skipped and the last stop takes the focus");
    }

    /// <summary>The ordinary case still works, and this one has to prove it rather than pass on the fallback: a scope
    /// that is drawn gives up its own first control. The owner caught the first version asserting only that something
    /// had the focus, which `end-turn` would have satisfied even if the minimap gave up nothing.</summary>
    [UnityTest]
    public IEnumerator AVisibleScopeStillGivesUpItsFirstControl()
    {
        yield return Boot();

        var minimap = ui.Q<VisualElement>("minimap");
        Assert.IsNotNull(minimap, "precondition: the minimap is in the tree");
        Assert.AreNotEqual(DisplayStyle.None, minimap.resolvedStyle.display, "precondition: and it is drawn");
        var first = ui.Q<Button>("minimap-overview");
        Assert.IsNotNull(first, "precondition: minimap-overview is its first button");

        (Focused as VisualElement)?.Blur();
        yield return null;

        FocusReturn.ToOrWithin(null, ui.Q<Button>("end-turn"), minimap);
        yield return Frames(3);

        Assert.IsTrue(minimap.Contains(Focused as VisualElement),
            $"the focus lands inside the drawn scope, not on the last stop; it is on {FocusedName}");
        Assert.AreEqual("minimap-overview", FocusedName, "and on its first button");
    }
}
