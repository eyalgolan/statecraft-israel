#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The short How to play screen (#1041): side-menu navigation, both languages, and its two required layouts.</summary>
public class HowToPlayScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [Test]
    public void TheSideMenuOpensAndClosesTheScreenInBothLanguages([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var guide = new HowToPlayPresenter(ui.Root, text);
        var screen = ui.Q<VisualElement>("screen-how-to-play");

        Assert.IsFalse(ScreenNav.IsOpen(screen), "hidden at start");
        ui.Press("nav-how-to-play");
        Assert.IsTrue(ScreenNav.IsOpen(screen), "the side-menu button opens it");
        Assert.AreEqual("how-to-play", nav.Current);
        Assert.AreEqual(text["how_to_play.title"], ui.Q<Label>("how-to-play-title").text);
        ui.Press("nav-how-to-play");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "pressing the same button closes it");
        Assert.IsNull(nav.Current);
    }

    [UnityTest]
    public IEnumerator TheGuideFitsLaptopAndPhoneInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
                yield return Rendered(size.x, size.y, hebrew, root =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {size.x}x{size.y}";
                    var screen = root.Q("screen-how-to-play").worldBound;
                    var window = root.worldBound;
                    if (!(screen.width > 0 && screen.height > 0)) problems.Add($"{at}: screen is not visible");
                    if (screen.xMin < window.xMin || screen.yMin < window.yMin || screen.xMax > window.xMax || screen.yMax > window.yMax)
                        problems.Add($"{at}: screen {screen} leaves window {window}");
                    var body = root.Q<ScrollView>("how-to-play-body").worldBound;
                    foreach (var line in root.Q<ScrollView>("how-to-play-body").Query<Label>().ToList())
                    {
                        if (string.IsNullOrWhiteSpace(line.text)) problems.Add($"{at}: {line.name} is empty");
                        if (line.worldBound.xMin < body.xMin - 1 || line.worldBound.xMax > body.xMax + 1)
                            problems.Add($"{at}: {line.name} {line.worldBound} overflows body {body}");
                    }
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    // R-GOV-002: help is selected by explicit ruleset, never by language or a zero Attention price.
    [Test]
    public void Gdd03HelpContainsNoAttentionInEitherLanguage([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var guide = new HowToPlayPresenter(ui.Root, text, SaveGame.Gdd03Ruleset);
        var attention = ui.Q<Label>("how-to-play-attention");
        Assert.IsEmpty(attention.text, "hidden legacy text must not remain accessible");
        Assert.AreEqual(DisplayStyle.None, attention.style.display.value);
        foreach (var key in new[] { "events", "top_bar", "end_turn" })
            Assert.AreEqual(text["how_to_play.gdd03." + key], ui.Q<Label>("how-to-play-" + key.Replace('_', '-')).text);
        AssertNoAttention(ui.Root);
    }

    [Test]
    public void Gdd03EndTurnHelpKeepsNeedsOrdersGuidance([Values(false, true)] bool hebrew)
    {
        using var ui = new UiTestKit();
        using var guide = new HowToPlayPresenter(ui.Root, hebrew ? He : En, SaveGame.Gdd03Ruleset);
        var line = ui.Q<Label>("how-to-play-end-turn").text;
        foreach (var phrase in hebrew ? new[] { "ממתינה להוראות", "בוחר בה במקום", "ביצור", "לדלג" }
                                    : new[] { "needs orders", "selects it instead", "fortify", "skip it" })
            StringAssert.Contains(phrase, line, "Removing Attention must not erase the needs-orders behavior");
    }

    [Test]
    public void OldRulesetsKeepEveryOriginalHelpLine([Values(false, true)] bool hebrew,
        [Values(SaveGame.LegacyRuleset, SaveGame.GddRuleset)] string ruleset)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var guide = new HowToPlayPresenter(ui.Root, text, ruleset);
        foreach (var key in new[] { "turn", "attention", "city", "training", "events", "top_bar", "end_turn", "why" })
            Assert.AreEqual(text["how_to_play." + key], ui.Q<Label>("how-to-play-" + key.Replace('_', '-')).text);
        Assert.AreNotEqual(DisplayStyle.None, ui.Q<Label>("how-to-play-attention").style.display.value);
    }

    [Test]
    public void RebuildingTheSameTreeRestoresTheCorrectModeAndLanguage()
    {
        using var ui = new UiTestKit();
        using (var guide = new HowToPlayPresenter(ui.Root, En, SaveGame.GddRuleset)) { }
        using (var guide = new HowToPlayPresenter(ui.Root, He, SaveGame.Gdd03Ruleset))
            AssertNoAttention(ui.Root);
        using (var guide = new HowToPlayPresenter(ui.Root, He, SaveGame.LegacyRuleset))
        {
            Assert.AreEqual(He["how_to_play.attention"], ui.Q<Label>("how-to-play-attention").text);
            Assert.AreNotEqual(DisplayStyle.None, ui.Q<Label>("how-to-play-attention").style.display.value);
            Assert.AreEqual(He["how_to_play.events"], ui.Q<Label>("how-to-play-events").text);
        }
        using (var guide = new HowToPlayPresenter(ui.Root, En, SaveGame.Gdd03Ruleset))
            AssertNoAttention(ui.Root);
    }

    static void AssertNoAttention(VisualElement root)
    {
        var lines = root.Q<ScrollView>("how-to-play-body").Query<Label>().ToList();
        var all = string.Join("\n", lines.Select(x => x.text));
        StringAssert.DoesNotContain("Attention", all);
        StringAssert.DoesNotContain("תשומת לב", all);
        StringAssert.DoesNotContain("תשומת הלב", all);
    }

    [UnityTest]
    public IEnumerator Gdd03GuideRendersWithoutAttentionAtLaptopAndPhoneSizesInBothLanguages()
    {
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
                yield return Rendered(size.x, size.y, hebrew, root =>
                {
                    AssertNoAttention(root);
                    var attention = root.Q<Label>("how-to-play-attention");
                    Assert.AreEqual(DisplayStyle.None, attention.resolvedStyle.display);
                    var screen = root.Q("screen-how-to-play").worldBound;
                    Assert.Greater(screen.width, 0);
                    Assert.Greater(screen.height, 0);
                    var body = root.Q<ScrollView>("how-to-play-body").worldBound;
                    foreach (var line in root.Q<ScrollView>("how-to-play-body").Query<Label>().ToList())
                    {
                        if (line == attention) continue;
                        Assert.IsNotEmpty(line.text);
                        Assert.Greater(line.worldBound.height, 0, line.name);
                        Assert.GreaterOrEqual(line.worldBound.xMin, body.xMin - 1, line.name);
                        Assert.LessOrEqual(line.worldBound.xMax, body.xMax + 1, line.name);
                    }
                }, SaveGame.Gdd03Ruleset);
    }

    static IEnumerator Rendered(int width, int height, bool hebrew, System.Action<VisualElement> check, string ruleset = SaveGame.GddRuleset)
    {
        var rt = new RenderTexture(width, height, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("HowToPlayLayoutTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var text = hebrew ? He : En;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        var guide = new HowToPlayPresenter(root, text, ruleset);
        var nav = new ScreenNav(root);
        nav.Open("how-to-play");
        try
        {
            for (int i = 0; i < 30 && !(root.Q("screen-how-to-play").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            check(root);
        }
        finally
        {
            nav.Dispose(); guide.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
