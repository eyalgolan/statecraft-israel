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

/// <summary>Writes the start screen's stills for #2054 (the language choice), in English and Hebrew at 1920x1080 and 390x844,
/// to `.superpowers/start-language-2054/` (local, not committed), or to the folder STATECRAFT_SHOT_DIR names. "after"
/// is the screen as it is; "before" then hides the new row, which leaves the panel as main lays it out. Explicit, so it does not run with
/// the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class StartScreenLanguageShots
{
    string saveDir = "";
    string shotDir = "";
    RenderTexture? rt; PanelSettings? ps;

    [SetUp]
    public void TempSaves()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-start-shots-" + Guid.NewGuid().ToString("N"));
        shotDir = Environment.GetEnvironmentVariable("STATECRAFT_SHOT_DIR") is string d && d.Length > 0
            ? d : Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "start-language-2054");
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

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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
        Debug.Log($"SHOT {name}");
    }

    IEnumerator Shots(bool hebrew, int width, int height)
    {
        // The whole game in Hebrew at 390x844 logs "Layout update is struggling" on main too (31 Oct 2026, before #2054):
        // not this tool's to judge, so the stills are still written.
        LogAssert.ignoreFailingMessages = true;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        rt = new RenderTexture(width, height, 24);
        ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        Object.FindFirstObjectByType<MapScene>().Build(MapFocusTests.Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = hebrew ? UiLanguage.Hebrew : UiLanguage.English;
        root.Begin(MapFocusTests.Inputs, 0, startScreen: true);
        var ui = doc.rootVisualElement;
        var panel = ui.Q("start-panel");
        for (int i = 0; i < 60 && !(panel.worldBound.height > 0); i++) yield return null;
        yield return Frames(10);
        var name = $"{(hebrew ? "he" : "en")}-{width}x{height}";
        Shot($"after-{name}");
        var row = ui.Q("start-language");                              // null on main, which has no row
        if (row != null) row.style.display = DisplayStyle.None;        // main's panel: no language row
        yield return Frames(10);
        Shot($"before-{name}");
    }

    [UnityTest] public IEnumerator English1920x1080() { yield return Shots(false, 1920, 1080); }
    [UnityTest] public IEnumerator Hebrew1920x1080() { yield return Shots(true, 1920, 1080); }
    [UnityTest] public IEnumerator English390x844() { yield return Shots(false, 390, 844); }
    [UnityTest] public IEnumerator Hebrew390x844() { yield return Shots(true, 390, 844); }
}
