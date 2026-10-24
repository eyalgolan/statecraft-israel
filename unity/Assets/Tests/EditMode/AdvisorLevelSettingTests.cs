#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Spec 6: four help levels in Settings, the one set marked, kept in the settings file, applied at once.</summary>
public class AdvisorLevelSettingTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [Test]
    public void PickingALevelKeepsItAndMarksIt()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-level-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore(dir);
            var advisors = new AdvisorSettings(store);
            using var ui = new UiTestKit();
            using var settings = new SettingsPresenter(ui.Root, En, store, advisors: advisors);
            Assert.IsTrue(ui.Q<Button>("settings-advisors-new").ClassListContains(SettingsPresenter.SelectedClass), "the default");
            ui.Press("settings-advisors-warnings");
            Assert.AreEqual("warnings", advisors.Level, "the live setting, so the queue filters at once");
            Assert.AreEqual("warnings", store.Read(AdvisorSettings.LevelKey));
            Assert.IsTrue(ui.Q<Button>("settings-advisors-warnings").ClassListContains(SettingsPresenter.SelectedClass));
            Assert.IsFalse(ui.Q<Button>("settings-advisors-new").ClassListContains(SettingsPresenter.SelectedClass));
            Assert.AreEqual(En["settings.advisors.off"], ui.Q<Button>("settings-advisors-off").text);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    /// <summary>"New to strategy games" is the longest choice: at a phone's width, in both languages, every choice stays in
    /// the Settings screen. The HUD sets the screen's tier for the window (HudPresenter), as in the game.</summary>
    [UnityTest]
    public IEnumerator TheRowFitsOnAPhoneInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (390, 844), (844, 390), (1280, 720) })
            {
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
                var go = new GameObject("LevelRow"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement; root.Q("root").RemoveFromClassList("loading");
                if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                var text = hebrew ? He : En;
                var hud = new HudPresenter(root, new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text), text);
                var nav = new ScreenNav(root);
                var settings = new SettingsPresenter(root, text, null, advisors: new AdvisorSettings(null));
                nav.Open("settings");
                try
                {
                    for (int i = 0; i < 30 && !(root.Q("screen-settings").layout.width > 0); i++) yield return null;
                    for (int i = 0; i < 3; i++) yield return null;
                    var screen = root.Q("screen-settings").worldBound;
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var placed = new List<(string name, Rect b)>();
                    // Each choice, and the screen's last control (the row must not push it out), inside the screen on both axes.
                    foreach (var name in new[] { "settings-advisors-new", "settings-advisors-experienced", "settings-advisors-warnings", "settings-advisors-off", "settings-share" })
                    {
                        var b = root.Q(name).worldBound;
                        if (!(b.width > 0) || b.xMin < screen.xMin - 0.5f || b.xMax > screen.xMax + 0.5f || b.yMin < screen.yMin - 0.5f || b.yMax > screen.yMax + 0.5f)
                            problems.Add($"{at}: {name} {b} leaves the screen {screen}");
                        if (name.StartsWith("settings-advisors-", StringComparison.Ordinal)) placed.Add((name, b));
                    }
                    // Wrapped, the choices never overlap.
                    for (int i = 0; i < placed.Count; i++)
                        for (int j = i + 1; j < placed.Count; j++)
                        {
                            var x = placed[i].b; var y = placed[j].b;
                            if (x.xMin < y.xMax - 0.5f && y.xMin < x.xMax - 0.5f && x.yMin < y.yMax - 0.5f && y.yMin < x.yMax - 0.5f)
                                problems.Add($"{at}: {placed[i].name} {x} overlaps {placed[j].name} {y}");
                        }
                }
                finally
                {
                    settings.Dispose(); nav.Dispose(); hud.Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
