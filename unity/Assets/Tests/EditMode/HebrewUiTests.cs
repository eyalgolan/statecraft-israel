#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The Hebrew UI (#193): asked for by hand only ("?lang=he", "-lang he"), English otherwise; when asked, GameRoot
/// loads strings.he.yaml and marks the root, and every text is set in the Hebrew face through the advanced text
/// generator.</summary>
public class HebrewUiTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    string saveDir = "";

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-hebrew-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    // --- The flag ---------------------------------------------------------------------------------------------------

    [Test]
    public void HebrewIsOnlyAskedForByHand()
    {
        var none = new string[0];
        foreach (var q in new[] { "?lang=he", "?golden&lang=he", "?lang=he&smoke=5", "?lang=he#top" })
            Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.Requested("http://localhost:8000/index.html" + q, none, webgl: true), q);
        foreach (var q in new[] { "", "?lang", "?lang=", "?lang=en", "?lang=hex", "?lang=HE", "?slang=he", "?lang=fr", "?golden" })
            Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost:8000/index.html" + q, none, webgl: true), q);
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested(null, none, webgl: true));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.Requested(null, new[] { "-batchmode", "-lang", "he" }, webgl: false));
        foreach (var args in new[] { none, new[] { "-lang" }, new[] { "-lang", "en" }, new[] { "-lang", "-smoke" }, new[] { "he" }, new[] { "-language", "he" } })
            Assert.AreEqual(UiLanguage.English, UiLanguage.Requested(null, args, webgl: false), string.Join(" ", args));
        // A flag on the desktop does not count on the web and the other way round (each instrument's own rule).
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost/index.html", new[] { "-lang", "he" }, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost/index.html?lang=he", none, webgl: false));
    }

    // --- The game ---------------------------------------------------------------------------------------------------

    GameRoot Boot(string? language)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Language = language;
        root.Begin(Inputs, 0);
        return root;
    }

    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    [Test]
    public void TheGameIsEnglishUnlessHebrewIsAskedFor()
    {
        var game = Boot(null);                                         // the test run's command line has no "-lang he"
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(En.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        Assert.IsTrue(Ui.Query<TextElement>().ToList().All(t => t.ClassListContains("start-he") || t.languageDirection == LanguageDirection.Inherit),
            "English sets no paragraph direction");
    }

    [Test]
    public void AskedForHebrewTheGameLoadsTheHebrewStringsAndMarksTheRoot()
    {
        var game = Boot(UiLanguage.Hebrew);
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(He.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        StringAssert.Contains("תור", Ui.Q<Label>("turn").text);
        // New Game in Hebrew disposes the old UiDirection and makes a new one; the start screen's Hebrew lines stay right to
        // left through it (neither takes theirs off nor sets it).
        game.Begin(Inputs, 0);
        TheDeclarationStaysRightToLeft("a Hebrew restart");
        // Starting again in English takes the mark and every other direction off (New Game restarts through Begin).
        game.Language = UiLanguage.English;
        game.Begin(Inputs, 0);
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(En.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        Assert.IsTrue(Ui.Query<TextElement>().ToList().All(t => t.ClassListContains("start-he") || t.languageDirection == LanguageDirection.Inherit));
        TheDeclarationStaysRightToLeft("an English restart");
    }

    static void TheDeclarationStaysRightToLeft(string after)
    {
        var lines = Ui.Query<TextElement>(className: "start-he").ToList();
        Assert.IsNotEmpty(lines, "Game.uxml's Hebrew start lines");
        foreach (var t in lines) Assert.AreEqual(LanguageDirection.RTL, t.languageDirection, $"after {after}: '{t.name}'");
    }

    // --- The direction ----------------------------------------------------------------------------------------------

    [Test]
    public void ATextIsRightToLeftWhenItsFirstLetterIsHebrew()
    {
        foreach (var s in new[] { "תור 1", "5  מלחמה כוללת", "קואליציה: 30 מתוך 37", "‏…אנו", "العربية" })
            Assert.IsTrue(UiDirection.RightToLeft(s), s);
        foreach (var s in new[] { "15 May 1948", "IL 20.0m", "+5%", "-5%", "30%", "", "David Ben-Gurion", "Mapai :מפלגה", "$40.0m", "1948" })
            Assert.IsFalse(UiDirection.RightToLeft(s), s);
        Assert.IsFalse(UiDirection.RightToLeft(null));
    }

    /// <summary>In Hebrew each text takes its paragraph direction from its own words: the Hebrew strings right to left, the
    /// English date and the numbers left to right (so "15 May 1948" is not reordered), and a text that changes follows.</summary>
    [Test]
    public void InHebrewEachTextTakesItsDirectionFromItsOwnWords()
    {
        var game = Boot(UiLanguage.Hebrew);
        Assert.AreEqual(LanguageDirection.RTL, Ui.Q<Label>("turn").languageDirection, "turn: Hebrew");
        Assert.AreEqual(LanguageDirection.LTR, Ui.Q<Label>("date").languageDirection, "the date is still English (#55)");
        Assert.AreEqual(LanguageDirection.LTR, Ui.Q<Label>("year").languageDirection, "digits alone");
        foreach (var t in Ui.Query<TextElement>().ToList().Where(t => !t.ClassListContains("start-he")))
            Assert.AreEqual(UiDirection.RightToLeft(t.text) ? LanguageDirection.RTL : LanguageDirection.LTR, t.languageDirection, $"'{t.name}': {t.text}");
        // A text that changes takes its new direction at once.
        var turn = Ui.Q<Label>("turn");
        turn.text = "Turn 7";
        Assert.AreEqual(LanguageDirection.LTR, turn.languageDirection);
        turn.text = "תור 7";
        Assert.AreEqual(LanguageDirection.RTL, turn.languageDirection);
        // After a turn the texts the turn rewrote keep the direction of their new words.
        game.Bridge!.EndTurn();
        Assert.AreEqual(LanguageDirection.RTL, turn.languageDirection, turn.text);
        Assert.AreEqual(LanguageDirection.LTR, Ui.Q<Label>("date").languageDirection, Ui.Q<Label>("date").text);
    }

    /// <summary>Many texts a presenter builds after boot get their words before they are on the panel (new Label(text)), so
    /// no text change reaches UiDirection: they take their direction when it walks the tree again, on the next frame after
    /// a submit, a click or a turn. Here: a label added before a submit, one added before a click, and the Why rows End
    /// Turn rebuilds (Hebrew headers, English lines).</summary>
    [UnityTest]
    public IEnumerator InHebrewTextsBuiltAfterBootTakeTheirDirectionAfterASubmitAClickOrATurn()
    {
        var restore = OnTexture(1920, 1080);
        try
        {
            var game = Object.FindFirstObjectByType<GameRoot>();
            game.Store = new SaveStore(saveDir);
            game.Language = UiLanguage.Hebrew;
            game.Begin(Inputs, 0);
            for (int i = 0; i < 2; i++) yield return null;

            // A submit and a click: a label added with its words already set has no direction until the next frame.
            var submitted = new Label(He["budget.vacant"]);
            Ui.Q("root").Add(submitted);
            Assert.AreEqual(LanguageDirection.Inherit, submitted.languageDirection);
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = submitted; submitted.SendEvent(e); }
            for (int i = 0; i < 2; i++) yield return null;
            Directed(new TextElement[] { submitted }, "a submit");
            var clicked = new Label("15 May 1948");
            Ui.Q("root").Add(clicked);
            Assert.AreEqual(LanguageDirection.Inherit, clicked.languageDirection);
            using (var e = ClickEvent.GetPooled()) { e.target = clicked; clicked.SendEvent(e); }
            for (int i = 0; i < 2; i++) yield return null;
            Directed(new TextElement[] { clicked }, "a click");

            // A turn: End Turn rebuilds the Why rows.
            game.Bridge!.EndTurn();
            var built = Ui.Q<ScrollView>("why-rows").contentContainer.Query<TextElement>().ToList();
            Assert.IsTrue(built.All(t => t.languageDirection == LanguageDirection.Inherit), "the new rows have no direction before the next frame");
            for (int i = 0; i < 2; i++) yield return null;
            Directed(built, "a turn");
            Assert.IsTrue(built.Any(t => t.languageDirection == LanguageDirection.RTL) && built.Any(t => t.languageDirection == LanguageDirection.LTR),
                "the rows mix Hebrew (the headers) and English (the lines, #55)");
        }
        finally { restore(); }
    }

    static void Directed(IList<TextElement> texts, string after)
    {
        Assert.IsNotEmpty(texts, after);
        foreach (var t in texts)
            Assert.AreEqual(UiDirection.RightToLeft(t.text) ? LanguageDirection.RTL : LanguageDirection.LTR, t.languageDirection, $"after {after}: '{t.name}': {t.text}");
    }

    /// <summary>Opens the map scene with its panel drawn into a texture of this size; the action it returns puts the panel back.</summary>
    static Action OnTexture(int width, int height)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var rt = new RenderTexture(width, height, 0);
        var ps = Object.Instantiate(doc.panelSettings);
        ps.targetTexture = rt;
        doc.panelSettings = ps;
        return () => { doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); };
    }

    // --- The house ---------------------------------------------------------------------------------------------------

    /// <summary>In Hebrew the half circle is the same picture mirrored (#199): the first seat, the coalition's, is at the
    /// right end and the last at the left, at the same height. English keeps the first seat on the left.</summary>
    [Test]
    public void TheHemicycleIsMirroredInHebrewOnly()
    {
        var centre = new Vector2(160, 150);
        const float Scale = 100f;
        var seats = Hemicycle.Layout(120);
        Vector2 first = seats[0], last = seats[seats.Count - 1];
        Assert.Less(Hemicycle.ToBox(first, centre, Scale, mirrored: false).x, centre.x, "English: the first seat on the left");
        Assert.Greater(Hemicycle.ToBox(first, centre, Scale, mirrored: true).x, centre.x, "Hebrew: the first seat on the right");
        Assert.Less(Hemicycle.ToBox(last, centre, Scale, mirrored: true).x, centre.x);
        foreach (var seat in seats)
        {
            Vector2 ltr = Hemicycle.ToBox(seat, centre, Scale, false), rtl = Hemicycle.ToBox(seat, centre, Scale, true);
            Assert.AreEqual(2 * centre.x - ltr.x, rtl.x, 1e-3f, "mirrored about the centre line");
            Assert.AreEqual(ltr.y, rtl.y, "the same height");
        }

        using var ui = new UiTestKit();
        var c = Inputs.Content;
        SimBridge Game(UiText t) => new SimBridge(Statecraft.Sim.GameSession.New(c, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical), t);
        using (new CabinetPresenter(ui.Root, Game(En), En, null))
            Assert.IsFalse(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "English");
        using (new CabinetPresenter(ui.Root, Game(He), He, null))
            Assert.IsTrue(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "Hebrew");
        using (new CabinetPresenter(ui.Root, Game(En), En, null))
            Assert.IsFalse(ui.Q<Hemicycle>("cabinet-hemicycle").Mirrored, "English again: the mark does not stick");
    }

    // --- The Budget rows --------------------------------------------------------------------------------------------

    /// <summary>No Budget row is wider than the screen, in either language, at the widest and the narrowest window: the
    /// office name wraps in its column (in Hebrew a row with a Hebrew phrase inside an English office was wider than the
    /// screen at 1920x1080, and the screen scrolled sideways).</summary>
    [UnityTest]
    public IEnumerator NoBudgetRowIsWiderThanItsScreen(
        [Values(UiLanguage.Hebrew, UiLanguage.English)] string language, [Values(1920, 390)] int width)
    {
        var restore = OnTexture(width, width == 1920 ? 1080 : 844);
        try
        {
            var game = Object.FindFirstObjectByType<GameRoot>();
            game.Store = new SaveStore(saveDir);
            game.Language = language;
            game.Begin(Inputs, 0);
            UiTestKit.Press(Ui.Q<Button>("nav-budget"));
            for (int i = 0; i < 4; i++) yield return null;             // layout needs a few passes
            var list = Ui.Q<ScrollView>("budget-rows");
            var room = list.contentViewport.layout.width;
            Assert.Greater(room, 100f, "the Budget screen is open");
            var rows = list.contentContainer.Children().ToList();
            Assert.Greater(rows.Count, 3);
            foreach (var row in rows)
            {
                Assert.LessOrEqual(row.layout.width, room + 0.5f, $"{language} {width}: '{row[0].name}' is wider than the screen");
                foreach (var part in row.Children())
                {
                    Assert.LessOrEqual(part.layout.xMax, row.layout.width + 0.5f, $"{language} {width}: '{part.name}' runs past its row's end");
                    Assert.GreaterOrEqual(part.layout.xMin, -0.5f, $"{language} {width}: '{part.name}' runs past its row's start");
                }
            }
            Assert.LessOrEqual(list.contentContainer.layout.width, room + 0.5f, $"{language} {width}: the rows scroll sideways");
            // The control: without the wrapping class the long Hebrew row is wider than the screen at 1920 (so this test can fail).
            if (language == UiLanguage.Hebrew && width == 1920)
            {
                foreach (var row in rows) row[0].RemoveFromClassList("budget-office");
                for (int i = 0; i < 4; i++) yield return null;
                Assert.IsTrue(rows.Any(r => r.layout.width > room + 0.5f || r.Children().Any(x => x.layout.xMin < -0.5f || x.layout.xMax > r.layout.width + 0.5f)),
                    "control: without the class a row overflows (in Hebrew the row runs right to left, so it spills past the start)");
            }
        }
        finally { restore(); }
    }

    // --- The face ---------------------------------------------------------------------------------------------------

    /// <summary>The classes a rule of Statecraft.uss sets in EB Garamond (".year", ".why-group"); type selectors are left out.</summary>
    static string[] GaramondClasses()
    {
        var uss = Regex.Replace(File.ReadAllText(UiPaths.Folder + "/Statecraft.uss"), @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Matches(uss, @"([^{}]+)\{([^{}]*)\}").Cast<Match>().Where(m => m.Groups[2].Value.Contains("EBGaramond"))
            .SelectMany(m => Regex.Matches(m.Groups[1].Value, @"\.([\w-]+)").Cast<Match>().Select(c => c.Groups[1].Value)).Distinct().ToArray();
    }

    /// <summary>With .lang-he on the root every text element of Game.uxml resolves Frank Ruhl Libre and the advanced
    /// generator, whatever rule styles it; without it (the control) the same elements resolve EB Garamond. So do labels
    /// with the classes only presenters add (the Why headers, the person cards, the map labels).</summary>
    [UnityTest]
    public IEnumerator InHebrewEveryTextIsSetInTheHebrewFaceThroughTheAdvancedGenerator()
    {
        var rt = new RenderTexture(1920, 1080, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("HebrewUiTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        try
        {
            var hebrew = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew);
            Assert.IsNotNull(hebrew, UiPaths.FontHebrew);
            var root = doc.rootVisualElement.Q("root");
            static Font? FontOf(TextElement e)
            {
                var d = e.resolvedStyle.unityFontDefinition;
                return d.fontAsset != null ? d.fontAsset.sourceFontFile : d.font;
            }
            // A class a rule sets in EB Garamond that no element of Game.uxml carries is added at runtime: a label carries it.
            var classes = GaramondClasses();
            Assert.Contains("why-group", classes, "the rules are read");
            foreach (var c in classes.Where(c => root.Q(className: c) == null))
            {
                var l = new Label("א") { name = "runtime-" + c };
                l.AddToClassList(c);
                root.Add(l);
            }
            // The start screen's Hebrew lines are Hebrew in both languages (the Declaration's own text).
            var texts = root.Query<TextElement>().ToList().Where(e => !e.ClassListContains("start-he")).ToList();
            Assert.Greater(texts.Count, 30, "Game.uxml's labels and buttons");
            for (int i = 0; i < 3; i++) yield return null;             // resolved styles need a style pass
            foreach (var e in texts)
                Assert.AreNotSame(hebrew, FontOf(e), $"English: '{e.name}' ({string.Join(" ", e.GetClasses())})");
            root.AddToClassList(UiLanguage.HebrewClass);
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var e in texts)
            {
                var what = $"'{e.name}' ({string.Join(" ", e.GetClasses())})";
                Assert.AreSame(hebrew, FontOf(e), what);
                Assert.AreEqual("Advanced", e.resolvedStyle.unityTextGenerator.ToString(), what);
            }
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
