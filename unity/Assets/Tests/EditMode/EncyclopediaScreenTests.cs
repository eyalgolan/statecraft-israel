#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

/// <summary>The advisors' encyclopedia (spec 10), which replaces How to play (#1041): side-menu navigation, the pages grouped
/// by advisor, both languages, gdd-0.3's help without Attention (R-GOV-002), and its two required layouts.</summary>
public class EncyclopediaScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    // Approval is simulated ONLY in the test's in-memory copy. Shipped Hebrew stays pending.
    static AdvisorCatalog Approved() => AdvisorCatalog.Parse(File.ReadAllText(AdvisorCatalog.Path).Replace("he_review: pending", "he_review: approved"));

    [Test]
    public void TheSideMenuOpensAndClosesTheScreenInBothLanguages([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var pedia = new EncyclopediaPresenter(ui.Root, FixtureText(text), Pages(), nav, null);
        var screen = ui.Q<VisualElement>("screen-encyclopedia");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "hidden at start");
        ui.Press("nav-encyclopedia");
        Assert.IsTrue(ScreenNav.IsOpen(screen), "the side-menu button opens it");
        Assert.AreEqual("encyclopedia", nav.Current);
        Assert.AreEqual(text["encyclopedia.title"], ui.Q<Label>("encyclopedia-title").text);
        ui.Press("nav-encyclopedia");
        Assert.IsFalse(ScreenNav.IsOpen(screen), "pressing the same button closes it");
        Assert.IsNull(nav.Current);
    }

    /// <summary>Spec 10: grouped by advisor; a page shows its title and text; Back returns to the list; in Hebrew a pending
    /// page is left out of the list (OQ-4), and an advisor with no page shown has no heading.</summary>
    [Test]
    public void PagesAreGroupedByAdvisorAndPendingOnesHideInHebrew()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var pedia = new EncyclopediaPresenter(ui.Root, FixtureText(En), Pages(), nav, null);
        ui.Press("nav-encyclopedia");
        var list = ui.Q<ScrollView>("encyclopedia-list");
        CollectionAssert.AreEqual(new[] { "encyclopedia-advisor-ben_gurion", "encyclopedia-page-p.bg", "encyclopedia-advisor-dayan", "encyclopedia-page-p.dayan" },
            list.contentContainer.Children().Select(c => c.name).ToArray());
        ui.Press("encyclopedia-page-p.bg");
        Assert.AreEqual("p.bg", pedia.ShowingPage);
        Assert.AreEqual("Title BG.", ui.Q<Label>("encyclopedia-page-title").text);
        Assert.AreEqual("Text BG.", ui.Q<Label>("encyclopedia-page-text").text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("encyclopedia-show-me").style.display.value, "no advisor to show it");
        ui.Press("encyclopedia-back");
        Assert.IsNull(pedia.ShowingPage);
        Assert.AreEqual(DisplayStyle.Flex, list.style.display.value, "Back shows the list again");

        using var ui2 = new UiTestKit();
        using var nav2 = new ScreenNav(ui2.Root);
        using var he = new EncyclopediaPresenter(ui2.Root, FixtureText(He), Pages(), nav2, null);
        ui2.Press("nav-encyclopedia");
        CollectionAssert.AreEqual(new[] { "encyclopedia-advisor-ben_gurion", "encyclopedia-page-p.bg" },
            ui2.Q<ScrollView>("encyclopedia-list").contentContainer.Children().Select(c => c.name).ToArray(), "p.dayan is pending");
    }

    /// <summary>Open(page), an advisor's Tell me more: the screen opens on that page; on the list for a page not shown.</summary>
    [Test]
    public void OpenShowsThePageAndFallsBackToTheList()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var pedia = new EncyclopediaPresenter(ui.Root, FixtureText(He), Pages(), nav, null);
        pedia.Open("p.bg");
        Assert.AreEqual("encyclopedia", nav.Current);
        Assert.AreEqual("p.bg", pedia.ShowingPage);
        pedia.Open("p.dayan");
        Assert.AreEqual("encyclopedia", nav.Current);
        Assert.IsNull(pedia.ShowingPage, "pending in Hebrew: the list instead");
    }

    /// <summary>Spec 10: the mechanics the guide and How to play taught, the 14 nothing taught, immigration and the national
    /// meters: 25 pages, grouped by advisor as spec 3.1 splits them.</summary>
    [Test]
    public void TheShippedPagesCoverEveryMechanic()
    {
        var pages = AdvisorCatalog.Load().Pages.ToDictionary(p => p.Id, p => p.Advisor);
        var expected = new Dictionary<string, string>
        {
            ["turns"] = "ben_gurion", ["attention"] = "ben_gurion", ["events"] = "ben_gurion", ["objectives"] = "ben_gurion",
            ["defeat"] = "ben_gurion", ["top_bar"] = "ben_gurion", ["why"] = "ben_gurion",
            ["cities"] = "meyerson", ["immigration"] = "meyerson", ["training"] = "meyerson", ["projects"] = "meyerson",
            ["pioneer_work"] = "meyerson", ["corridors"] = "meyerson", ["money_debt"] = "meyerson", ["budget"] = "meyerson",
            ["cabinet"] = "meyerson", ["food"] = "meyerson", ["national_meters"] = "meyerson",
            ["units_movement"] = "dayan", ["standing_orders"] = "dayan", ["combat"] = "dayan", ["arms"] = "dayan",
            ["truces"] = "dayan", ["capture"] = "dayan", ["enemy_phase"] = "dayan",
        };
        CollectionAssert.AreEquivalent(expected, pages);
    }

    // R-GOV-002: help is selected by explicit ruleset, never by language or a zero Attention price.
    [Test]
    public void Gdd03HelpContainsNoAttentionInEitherLanguage([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var pedia = new EncyclopediaPresenter(ui.Root, text, Approved(), nav, null, SaveGame.Gdd03Ruleset);
        ui.Press("nav-encyclopedia");
        var entries = ui.Q<ScrollView>("encyclopedia-list").contentContainer.Children().Select(c => c.name).ToList();
        CollectionAssert.DoesNotContain(entries, "encyclopedia-page-attention");
        var all = new List<string>(ui.Q<ScrollView>("encyclopedia-list").Query<TextElement>().ToList().Select(t => t.text));
        foreach (var page in Approved().Pages.Where(p => p.Id != "attention"))
        {
            pedia.Open(page.Id);
            Assert.AreEqual(page.Id, pedia.ShowingPage);
            all.Add(ui.Q<Label>("encyclopedia-page-title").text);
            all.Add(ui.Q<Label>("encyclopedia-page-text").text);
        }
        AssertNoAttention(string.Join("\n", all));
    }

    [Test]
    public void OldRulesetsKeepTheAttentionPage([Values(SaveGame.LegacyRuleset, SaveGame.GddRuleset)] string ruleset)
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var pedia = new EncyclopediaPresenter(ui.Root, En, AdvisorCatalog.Load(), nav, null, ruleset);
        pedia.Open("attention");
        Assert.AreEqual("attention", pedia.ShowingPage);
        Assert.AreEqual(En["advisor.page.attention.text"], ui.Q<Label>("encyclopedia-page-text").text);
    }

    static void AssertNoAttention(string all)
    {
        StringAssert.DoesNotContain("Attention", all);
        StringAssert.DoesNotContain("תשומת לב", all);
        StringAssert.DoesNotContain("תשומת הלב", all);
    }

    [UnityTest]
    public IEnumerator TheEncyclopediaFitsLaptopAndPhoneInBothLanguages()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
                yield return Rendered(size.x, size.y, hebrew, (root, pedia) => Fits(root, problems, $"{(hebrew ? "he" : "en")} {size.x}x{size.y}", pedia));
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    static IEnumerable Fits(VisualElement root, List<string> problems, string at, EncyclopediaPresenter pedia)
    {
        var window = root.worldBound;
        void Inside(string what, Rect r, Rect outer)
        {
            if (!(r.width > 0 && r.height > 0)) problems.Add($"{at}: {what} is not visible");
            else if (r.xMin < outer.xMin - 1 || r.yMin < outer.yMin - 1 || r.xMax > outer.xMax + 1 || r.yMax > outer.yMax + 1)
                problems.Add($"{at}: {what} {r} leaves {outer}");
        }
        var screen = root.Q("screen-encyclopedia").worldBound;
        Inside("screen", screen, window);
        var list = root.Q<ScrollView>("encyclopedia-list");
        if (list.contentContainer.childCount == 0) problems.Add($"{at}: the list is empty");
        foreach (var entry in list.contentContainer.Children())
            if (entry.worldBound.xMin < list.worldBound.xMin - 1 || entry.worldBound.xMax > list.worldBound.xMax + 1)
                problems.Add($"{at}: {entry.name} {entry.worldBound} overflows the list {list.worldBound}");
        pedia.Open("attention");
        for (int i = 0; i < 3; i++) yield return null;
        var body = root.Q<ScrollView>("encyclopedia-page-body").worldBound;
        var words = root.Q<Label>("encyclopedia-page-text").worldBound;   // it may scroll down, never sideways
        if (!(words.width > 0) || words.xMin < body.xMin - 1 || words.xMax > body.xMax + 1) problems.Add($"{at}: page text {words} overflows {body}");
        foreach (var name in new[] { "encyclopedia-page-title", "encyclopedia-back", "encyclopedia-show-me" })
            Inside(name, root.Q(name).worldBound, screen);
    }

    static AdvisorCatalog Pages() => AdvisorCatalog.Parse("advisors:\n  - { id: ben_gurion, person: ben_gurion }\n  - { id: meyerson, person: golda_meir }\n  - { id: dayan, person: dayan }\n" +
        "messages: []\npages:\n  - { id: p.bg, advisor: ben_gurion, show_me: attention, he_review: approved }\n  - { id: p.dayan, advisor: dayan }\n");

    static UiText FixtureText(UiText baseText) => UiText.Parse(
        AssetDatabase.LoadAssetAtPath<TextAsset>(baseText["ui.lang"] == UiLanguage.Hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text +
        "\nadvisor.page.p.bg.title: \"Title BG.\"\nadvisor.page.p.bg.text: \"Text BG.\"\nadvisor.page.p.dayan.title: \"Title D.\"\nadvisor.page.p.dayan.text: \"Text D.\"\n");

    /// <summary>The whole HUD as the game lays it out, the encyclopedia open on its list, with an advisor so Show me shows.
    /// The Hebrew is the test's approved copy (the shipped pages wait for their native review).</summary>
    static IEnumerator Rendered(int width, int height, bool hebrew, System.Func<VisualElement, EncyclopediaPresenter, IEnumerable> check)
    {
        var rt = new RenderTexture(width, height, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("EncyclopediaLayoutTest");
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
        var nav = new ScreenNav(root);
        var advisor = new AdvisorPresenter(root, bridge, text, new AdvisorSession(Approved(), new AdvisorSettings(null), false));
        var pedia = new EncyclopediaPresenter(root, text, Approved(), nav, advisor);
        nav.Open("encyclopedia");
        try
        {
            for (int i = 0; i < 30 && !(root.Q("screen-encyclopedia").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var step in check(root, pedia)) yield return step;
        }
        finally
        {
            pedia.Dispose(); advisor.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
