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

/// <summary>Writes the before and after stills for #41 K6, in English and Hebrew, to `.superpowers/a11y-bars-41/`
/// (local, not committed). A 1280x420 window, so the Budget's rows overflow and its bar is shown and in the order.
/// The "before" state puts the bar's slider back in the Tab order, which is what it was before the change, and Tabs
/// from the Budget's rail button until the bar takes the focus; the "after" state walks the same number of presses
/// from the same start. Explicit, so it does not run with the
/// checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class ScrollBarShots
{
    string saveDir = "";
    string shotDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-bars-" + Guid.NewGuid().ToString("N"));
        shotDir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "a11y-bars-41");
        Directory.CreateDirectory(shotDir);
    }

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

    IEnumerator Boot(bool hebrew)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 420, 24);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        if (hebrew) root.Language = UiLanguage.Hebrew;
        root.Begin(MapFocusTests.Inputs, 0);
        ui = doc.rootVisualElement;
        for (int i = 0; i < 30 && !(ui.Q("top-bar").worldBound.height > 0); i++) yield return null;
        yield return Frames(5);
        GameSceneTests.HideTheOpeningCard(ui);
        yield return Frames(3);
    }

    void Shot(string name)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt!.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"SHOT {name}: focus is on {FocusedName}");
    }

    static void PutTheBarBackInTheOrder(ScrollView v)
    {
        var s = v.verticalScroller;
        s.focusable = true;
        if (s.slider != null) s.slider.focusable = true;
    }

    /// <summary>Tabs up to <paramref name="max"/> times, stopping early on a scroll bar. Returns the number of
    /// presses made, so the other state can be walked the same number from the same start: a fair pair.</summary>
    IEnumerator TabToTheBar(int[] steps, int max = 60)
    {
        steps[0] = 0;
        for (int i = 0; i < max; i++)
        {
            yield return Tab();
            steps[0]++;
            if (Focused is VisualElement at && (at is Scroller || at.GetFirstAncestorOfType<Scroller>() != null)) yield break;
        }
    }

    IEnumerator TabExactly(int n)
    {
        for (int i = 0; i < n; i++) yield return Tab();
    }

    IEnumerator Shots(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew);

        ui.Q<Button>("nav-budget").Focus();
        yield return null;
        yield return Enter();
        yield return Frames(5);

        var rows = ui.Q<ScrollView>("budget-rows");
        Debug.Log($"BARSHOT {lang}: budget-rows content={rows.contentContainer.layout.height:F0} viewport={rows.contentViewport.layout.height:F0}");

        // before: the slider back in the order, exactly as it was before this change; Tab until the bar takes the focus
        PutTheBarBackInTheOrder(rows);
        ui.Q<Button>("nav-budget").Focus(); yield return null;
        var steps = new int[1];
        yield return TabToTheBar(steps);
        Debug.Log($"BARSHOT {lang}: the bar takes the focus after {steps[0]} presses, on {FocusedName}");
        Shot($"budget-before-{lang}");

        // after: the bar out of the order again, the same number of presses from the same start
        FocusScroll.BarsAreNotTabStops(rows);
        ui.Q<Button>("nav-budget").Focus(); yield return null;
        yield return TabExactly(steps[0]);
        Shot($"budget-after-{lang}");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
