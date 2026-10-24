#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Spec 6: four help levels in Settings, the one set marked, kept in the settings file, applied at once.</summary>
public class AdvisorLevelSettingTests
{
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
    /// the Settings screen.</summary>
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
                var nav = new ScreenNav(root);
                var settings = new SettingsPresenter(root, hebrew ? He : En, null, advisors: new AdvisorSettings(null));
                nav.Open("settings");
                try
                {
                    for (int i = 0; i < 6; i++) yield return null;
                    var screen = root.Q("screen-settings").worldBound;
                    foreach (var level in AdvisorSettings.Levels)
                    {
                        var b = root.Q("settings-advisors-" + level).worldBound;
                        if (!(b.width > 0) || b.xMin < screen.xMin - 0.5f || b.xMax > screen.xMax + 0.5f)
                            problems.Add($"{(hebrew ? "he" : "en")} {w}x{h}: {level} {b} leaves the screen {screen}");
                    }
                }
                finally
                {
                    settings.Dispose(); nav.Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
