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

/// <summary>Writes the before and after stills for #41's screen-entry fix (K5), in English and Hebrew, to
/// `.superpowers/a11y-screen-entry-41/` as the 14 October audit does (local, not committed). The "before" state is the
/// old path called directly: the screen is opened through `ScreenNav` and the focus is put back on the rail button,
/// which is where it stayed before `HandFocusTo`. Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class ScreenEntryShots
{
    string saveDir = "";
    string shotDir = "";
    RenderTexture? rt; PanelSettings? ps;
    VisualElement ui = null!;
    GameRoot root = null!;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-entry-" + Guid.NewGuid().ToString("N"));
        shotDir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "a11y-screen-entry-41");
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

    IEnumerator Enter()
    {
        var at = At;
        using (var k = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None)) { k.target = at; at.SendEvent(k); }
        using (var s = NavigationSubmitEvent.GetPooled()) { s.target = at; at.SendEvent(s); }
        yield return null;
    }

    IEnumerator Boot(bool hebrew)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(1280, 720, 24);
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

    IEnumerator Shots(bool hebrew)
    {
        var lang = hebrew ? "he" : "en";
        yield return Boot(hebrew);

        var nav = ui.Q<Button>("nav-cabinet");
        nav.Focus(); yield return null;
        yield return Enter();                       // the fix hands the focus into the screen
        yield return Frames(5);
        Shot($"cabinet-after-{lang}");

        // before: what Open did until now, leave the focus on the rail button that opened the screen
        FocusReturn.To(ui.Q<Button>("nav-cabinet"));
        yield return Frames(3);
        Shot($"cabinet-before-{lang}");
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
