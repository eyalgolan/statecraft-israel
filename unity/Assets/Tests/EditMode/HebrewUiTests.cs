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
        SettingsPresenter.VerifiedShapes.Remove(150);                  // the language switch's stand-in (the game ships none)
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

    /// <summary>The language switch in Settings (#1036): a press rebuilds the UI in the other language at once, over the same
    /// game (this turn's orders kept), with Settings still open on the new choice; the choice is kept in settings.txt, so
    /// the next start (Language unset, as a reload leaves it) comes up in it.</summary>
    [Test]
    public void TheLanguageSwitchInSettingsChangesTheUiAtOnceAndIsKept()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        var settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        game.Settings = settings;
        SettingsScreenTests.VerifyOnEveryShape(150);                   // the game verifies no scale above 100% yet (#1011)
        game.Begin(Inputs, 0);                                         // no file and no flag: English
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.IsTrue(Ui.Q<Button>("settings-language-en").ClassListContains(SettingsPresenter.SelectedClass), "English is marked");
        Assert.AreEqual("English", Ui.Q<Button>("settings-language-en").text);
        Assert.AreEqual("עברית", Ui.Q<Button>("settings-language-he").text, "each language is named in itself");
        var session = game.Bridge!.Session;
        Assert.IsNull(game.Bridge.Queue(new Statecraft.Sim.SetMobilization(1.0)));
        UiTestKit.Press(Ui.Q<Button>("settings-uiscale-150"));        // the UI scale (#40) must survive the rebuild below

        UiTestKit.Press(Ui.Q<Button>("settings-language-he"));
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "Hebrew at once, no reload");
        Assert.AreEqual(He.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        Assert.AreSame(session, game.Bridge.Session, "the same game goes on");
        Assert.AreEqual(1, game.Bridge.Queued.Count, "this turn's order is kept");
        Assert.IsTrue(ScreenNav.IsOpen(Ui.Q("screen-settings")), "Settings stays open");
        Assert.IsTrue(Ui.Q<Button>("settings-language-he").ClassListContains(SettingsPresenter.SelectedClass), "Hebrew is marked");
        Assert.IsFalse(Ui.Q<Button>("settings-language-en").ClassListContains(SettingsPresenter.SelectedClass));
        Assert.AreEqual(UiLanguage.Hebrew, settings.Read(SettingsPresenter.LanguageKey), "kept in settings.txt");
        Assert.AreEqual(new Vector2Int(1280, 720), Object.FindFirstObjectByType<UIDocument>().panelSettings.referenceResolution, "150% survives the switch");
        Assert.IsTrue(Ui.Q<Button>("settings-uiscale-150").ClassListContains(SettingsPresenter.SelectedClass), "and 150% is marked");

        game.Language = null;                                          // a reload: only the file remains
        game.Begin(Inputs, 0);
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "Hebrew after a reload");

        UiTestKit.Press(Ui.Q<Button>("settings-language-en"));
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "and English again at once");
        Assert.AreEqual(En.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        Assert.AreEqual(UiLanguage.English, settings.Read(SettingsPresenter.LanguageKey));
    }

    /// <summary>Codex's review of #1527, finding 4: the language switch keeps this turn's "Skip this turn" choices as it
    /// keeps the queued orders, so End turn does not ask again for a unit the player skipped.</summary>
    [Test]
    public void TheLanguageSwitchKeepsThisTurnsSkips()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        game.Begin(Inputs, 0);
        var skipped = game.Bridge!.NeedsOrders[0];
        game.Bridge.Skip(skipped);

        UiTestKit.Press(Ui.Q<Button>("settings-language-he"));
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "precondition: the UI was rebuilt");
        Assert.IsTrue(game.Bridge!.IsSkipped(skipped), "the skip is kept");
        CollectionAssert.DoesNotContain(game.Bridge.NeedsOrders.ToList(), skipped);
    }

    /// <summary>A language named in the address or on the command line (the store page's "?lang=he" link) holds for that
    /// visit over the stored choice; without one the stored choice holds; with neither, English.</summary>
    [Test]
    public void AFlagWinsOverTheStoredLanguage()
    {
        const string page = "http://localhost:8000/index.html";
        var none = new string[0];
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart(null, page, none, webgl: true));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(UiLanguage.Hebrew, page, none, webgl: true));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(UiLanguage.Hebrew, page + "?golden", none, webgl: true));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(UiLanguage.English, page + "?lang=he", none, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart(UiLanguage.Hebrew, page + "?lang=en", none, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart("fr", page, none, webgl: true), "a value the game does not know");
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(UiLanguage.Hebrew, null, none, webgl: false));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(null, null, new[] { "-lang", "he" }, webgl: false));
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart(UiLanguage.Hebrew, null, new[] { "-lang", "en" }, webgl: false));
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
    /// numbers left to right (so "1948" is not reordered), the date in Hebrew right to left (#687), and a text that changes follows.</summary>
    [Test]
    public void InHebrewEachTextTakesItsDirectionFromItsOwnWords()
    {
        var game = Boot(UiLanguage.Hebrew);
        Assert.AreEqual(LanguageDirection.RTL, Ui.Q<Label>("turn").languageDirection, "turn: Hebrew");
        Assert.AreEqual(LanguageDirection.RTL, Ui.Q<Label>("date").languageDirection, "the date is in Hebrew (#687)");
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
        Assert.AreEqual(LanguageDirection.RTL, Ui.Q<Label>("date").languageDirection, Ui.Q<Label>("date").text);
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

    // --- The static texts ---------------------------------------------------------------------------------------------

    /// <summary>Every text Game.uxml writes itself that a player reads, and its string (#193). "a/b" is the element of class b
    /// inside the element named a. The presenter that owns each element sets it from the UI's strings at bind time.</summary>
    static readonly (string where, string key)[] StaticTexts =
    {
        ("loading", "boot.loading"),
        ("meter-treasury/meter-name", "hud.caption.treasury"), ("meter-immigration/meter-name", "hud.caption.immigration"),
        ("meter-security/meter-name", "hud.caption.security"), ("meter-support/meter-name", "hud.caption.support"),
        ("meter-cohesion/meter-name", "hud.caption.cohesion"), ("meter-standing/meter-name", "hud.caption.standing"), ("meter-fx/meter-name", "hud.caption.fx"),
        ("nav-cabinet", "hud.nav.cabinet"), ("nav-budget", "hud.nav.budget"), ("nav-defence", "hud.nav.defence"),
        ("nav-why", "hud.nav.why"), ("nav-timeline", "nav.timeline"), ("nav-how-to-play", "hud.nav.how_to_play"), ("nav-objectives", "hud.nav.objectives"),
        ("new-game", "hud.new_game"), ("end-turn", "hud.end_turn_needs_orders"),
        ("event-draft", "event.draft"),
        ("budget-screen-title", "budget.title"), ("budget-reset", "budget.reset"), ("budget-submit", "budget.submit"),
        ("defence-screen-title", "defence.title"), ("de-escalate", "defence.lower"), ("escalate", "defence.raise"),
        ("why-screen-title", "why.title"),
        ("how-to-play-title", "how_to_play.title"), ("objectives-title", "objectives.title"),
        ("nav-settings", "hud.nav.settings"), ("settings-screen-title", "settings.title"), ("settings-tiltshift-label", "settings.tiltshift"),
        ("settings-quality-label", "settings.quality"), ("settings-quality-auto", "settings.quality.auto"),
        ("settings-quality-high", "settings.quality.high"), ("settings-quality-low", "settings.quality.low"),
        ("settings-sound-label", "settings.sound"), ("settings-sound-off", "settings.sound.off"),
        ("settings-sound-low", "settings.sound.low"), ("settings-sound-high", "settings.sound.high"),
        ("settings-advisors-label", "settings.advisors"), ("settings-language-label", "settings.language"),
        ("settings-uiscale-label", "settings.uiscale"),
        ("settings-community-label", "settings.community"), ("settings-star", "community.star"), ("start-star", "community.star"),
        ("settings-report", "feedback.open"), ("start-report", "feedback.open"), ("feedback-title", "feedback.open"),   // #1169
        ("hud-star", "community.star"), ("hud-report", "feedback.open"), ("hud-share", "share.open"),   // #1236
        ("feedback-intro", "feedback.intro"), ("feedback-bug", "feedback.bug"), ("feedback-feature", "feedback.feature"),
        ("feedback-itch", "feedback.itch"), ("feedback-close", "report.close"),
        ("settings-share", "share.open"), ("turn-report-share", "share.open"), ("share-title", "share.title"),   // #1170
        ("share-attach", "share.attach"), ("share-close", "report.close"),
        ("start-source", "start.source"), ("start-recording-credit", "start.recording_credit"),
        ("start-continue", "start.continue"), ("start-new-game", "start.new_game"),
    };

    /// <summary>Static texts that stay as Game.uxml writes them in every language: the game's name, and the Declaration's
    /// official English translation under each Hebrew line. ("-5%" and "+5%" have no letters and need no string.)</summary>
    static readonly string[] NotTranslated = { "start-title", "start-en-proclamation", "start-en-date", "start-caption-en" };

    static TextElement Static(VisualElement ui, string where)
    {
        var parts = where.Split('/');
        var e = parts.Length == 1 ? ui.Q<TextElement>(where) : ui.Q(parts[0])?.Q<TextElement>(className: parts[1]);
        Assert.IsNotNull(e, where);
        return e!;
    }

    /// <summary>English shows what it always showed: each English string is the text Game.uxml writes, and every text in
    /// Game.uxml with a letter in it is either one of StaticTexts, deliberately not translated, or a start line in Hebrew
    /// (an English text with the start-he class is not exempt). Game.uxml writes no tooltip: one with a letter would need
    /// a string too. Texts C# writes itself are not read here.</summary>
    [Test]
    public void EachEnglishStaticTextIsTheUxmlsOwnText()
    {
        using var ui = new UiTestKit();
        var listed = new HashSet<TextElement>();
        foreach (var (where, key) in StaticTexts)
        {
            var e = Static(ui.Root, where);
            Assert.AreEqual(e.text, En[key], $"{where}: {key}");
            listed.Add(e);
        }
        foreach (var name in NotTranslated) listed.Add(ui.Q<TextElement>(name));
        var unlisted = ui.Root.Query<TextElement>().ToList()
            .Where(t => !listed.Contains(t) && !(t.ClassListContains("start-he") && UiDirection.RightToLeft(t.text)) && (t.text ?? "").Any(char.IsLetter))
            .Select(t => $"'{t.name}' ({string.Join(" ", t.GetClasses())}): {t.text}").ToList();
        Assert.IsEmpty(unlisted, "a text Game.uxml writes that no string sets");
        var tooltips = ui.Root.Query<VisualElement>().ToList().Where(v => (v.tooltip ?? "").Any(char.IsLetter))
            .Select(v => $"'{v.name}': tooltip {v.tooltip}").ToList();
        Assert.IsEmpty(tooltips, "a tooltip Game.uxml writes that no string sets");
    }

    /// <summary>In Hebrew every static text shows its strings.he.yaml words, with no Latin letter but a listed name, and takes
    /// its direction like any other text; an English restart puts the English back.</summary>
    [Test]
    public void InHebrewEveryStaticTextIsTheHebrewString()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Language = UiLanguage.Hebrew;
        game.Begin(Inputs, 0, startScreen: true);                       // the start screen sets its own texts
        var latin = new Regex("[A-Za-z]");
        foreach (var (where, key) in StaticTexts)
        {
            var e = Static(Ui, where);
            Assert.AreEqual(He[key], e.text, $"{where}: {key}");
            // No Latin letters, names included: the recording's engineer is ראלף הלינגר, as the Israel State Archives' own
            // description spells him (#558).
            Assert.IsFalse(latin.IsMatch(e.text), $"{where}: Latin letters in '{e.text}'");
            Assert.AreEqual(LanguageDirection.RTL, e.languageDirection, $"{where}: {e.text}");
        }
        foreach (var name in NotTranslated) Assert.AreEqual(LanguageDirection.LTR, Ui.Q<TextElement>(name).languageDirection, name);

        game.Language = UiLanguage.English;
        game.Begin(Inputs, 0, startScreen: true);
        foreach (var (where, key) in StaticTexts)
        {
            var e = Static(Ui, where);
            Assert.AreEqual(En[key], e.text, $"after an English restart: {where}");
            Assert.AreEqual(LanguageDirection.Inherit, e.languageDirection, $"after an English restart: {where}");
        }
    }

    /// <summary>#880: in Hebrew every map name pill shows its row's name_he, and its English name where the row has none; an
    /// English restart puts the English names back. Since #1344 every 1948 locality has a name_he (Ghazawiya's is
    /// אל-גזאוויה), so the fallback is checked by clearing one row's name_he and starting again. #1008: a GDD v0.2 map location
    /// (content/map/locations_1948.yaml) is named in both languages too, and the one with a settlement's id (jerusalem)
    /// names that settlement's pill: "מערב ירושלים", "West Jerusalem".</summary>
    [Test]
    public void InHebrewEachMapPillShowsItsHebrewNameWhereTheRowHasOne()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Language = UiLanguage.Hebrew;
        game.Begin(Inputs, 0);
        Assert.AreEqual("נהלל", Ui.Q<Label>("label-nahalal").text);
        Assert.AreEqual("תל אביב", Ui.Q<Label>("label-tel_aviv").text);
        Assert.AreEqual("נשר", Ui.Q<Label>("label-nesher").text, "a 1948 locality with a name_he");
        Assert.AreEqual("נענה", Ui.Q<Label>("label-naana").text, "#1219: Na'ana has a sourced name_he now");
        Assert.AreEqual("סאלחיה", Ui.Q<Label>("label-al_salihiyya").text, "#1313: Salihiya has a sourced name_he now");
        Assert.AreEqual("אל-גזאוויה", Ui.Q<Label>("label-al_ghazzawiyya").text, "#1344: Ghazawiya has a sourced name_he now");
        var locations = Inputs.Content.MapLocations.ToDictionary(l => l.Id);
        Assert.AreEqual("מערב ירושלים", Ui.Q<Label>("label-jerusalem").text, "#1008: Jerusalem's pill is West Jerusalem's");
        foreach (var s in map.Settlements)
            Assert.AreEqual(locations.TryGetValue(s.Id, out var named) ? named.NameHe : string.IsNullOrWhiteSpace(s.NameHe) ? s.Name : s.NameHe, Ui.Q<Label>("label-" + s.Id).text, s.Id);
        foreach (var l in locations.Values) Assert.AreEqual(l.NameHe, Ui.Q<Label>("label-" + l.Id).text, l.Id);
        foreach (var p in map.Localities)
            Assert.AreEqual(string.IsNullOrWhiteSpace(p.NameHe) ? p.Settlement.Name : p.NameHe, Ui.Q<Label>("label-" + p.Settlement.Id).text, p.Settlement.Id);
        // No row lacks a name_he now: clear Ghazawiya's on this build's copy, and the pill falls back to the English name.
        map.Localities.Single(p => p.Settlement.Id == "al_ghazzawiyya").NameHe = null;
        game.Begin(Inputs, 0);
        Assert.AreEqual("Ghazawiya", Ui.Q<Label>("label-al_ghazzawiyya").text, "no name_he: the English name stays");

        game.Language = UiLanguage.English;
        game.Begin(Inputs, 0);
        Assert.AreEqual("Nahalal", Ui.Q<Label>("label-nahalal").text);
        foreach (var s in map.Settlements)
            Assert.AreEqual(locations.TryGetValue(s.Id, out var named) ? named.Name : s.Name, Ui.Q<Label>("label-" + s.Id).text, $"after an English restart: {s.Id}");
        foreach (var l in locations.Values) Assert.AreEqual(l.Name, Ui.Q<Label>("label-" + l.Id).text, $"after an English restart: {l.Id}");
        foreach (var p in map.Localities) Assert.AreEqual(p.Settlement.Name, Ui.Q<Label>("label-" + p.Settlement.Id).text, $"after an English restart: {p.Settlement.Id}");
    }

    /// <summary>The loading line is in the UI's language from the first frame, before Begin, in the Hebrew face and right to
    /// left; a boot failure keeps both. English keeps its line, its face and no direction.</summary>
    [Test]
    public void TheLoadingLineIsInTheUisLanguageBeforeTheGameStarts()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<GameRoot>();
        var line = Ui.Q<Label>("loading");
        game.Language = UiLanguage.Hebrew;
        Assert.IsNotNull(game.ShowLoading());
        Assert.AreEqual(He["boot.loading"], line.text);
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "the Hebrew face before Begin");
        Assert.AreEqual(LanguageDirection.RTL, line.languageDirection);
        LogAssert.Expect(LogType.Error, "BOOT FAIL people.yaml: people[x].name: required");
        Object.FindFirstObjectByType<GameBoot>().Fail("people.yaml: people[x].name: required");
        StringAssert.StartsWith("לא ניתן היה להפעיל", line.text);
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(LanguageDirection.RTL, line.languageDirection);

        game.Language = UiLanguage.English;
        game.ShowLoading();
        Assert.AreEqual(En["boot.loading"], line.text);
        Assert.AreEqual("Loading the map of 1948…", line.text);
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass));
        Assert.AreEqual(LanguageDirection.Inherit, line.languageDirection);
    }

    /// <summary>A boot failure stays on the loading line whichever of GameBoot.Start and GameRoot.Start runs first (neither
    /// sets an execution order). On the desktop the load reads from disk, so its failure lands inside GameBoot.Start
    /// (GameSceneTests.AFailingLoadIsCaughtAndShownWithoutLocalPaths drives the same throw); GameRoot's Start must not
    /// write the loading line over it (M3 final review I5).</summary>
    [Test]
    public void ABootFailureStaysOnTheLoadingLineInEitherStartOrder([Values(UiLanguage.English, UiLanguage.Hebrew)] string language)
    {
        foreach (var failFirst in new[] { true, false })
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var game = Object.FindFirstObjectByType<GameRoot>();
            var boot = Object.FindFirstObjectByType<GameBoot>();
            game.Language = language;
            var order = failFirst ? "GameBoot.Start first" : "GameRoot.Start first";
            if (!failFirst) game.Started();
            LogAssert.Expect(LogType.Error, new Regex("BOOT FAIL Could not find file"));
            LogAssert.Expect(LogType.Exception, new Regex("Could not find file"));
            _ = boot.BootAsync(() => throw new IOException("Could not find file '/Users/someone/Statecraft Israel.app/Contents/Resources/Data/StreamingAssets/map/heightmap.json'."));
            if (failFirst) game.Started();
            var t = language == UiLanguage.Hebrew ? He : En;
            Assert.AreEqual(t.Format("boot.failed", t["boot.reason_other"]), Ui.Q<Label>("loading").text, $"{language}, {order}");   // #637
            Assert.AreEqual(language == UiLanguage.Hebrew, Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), $"{language}, {order}: the face");
        }
    }

    /// <summary>A seat count's unit follows the body (#193, from the #209 review): the Knesset, which was elected
    /// (legislatures.yaml "elected", Legislature.Elected), counts "מנדטים"; the Provisional State Council, which was
    /// appointed, counts its "חברים". English says "seats" for both, as before.</summary>
    [Test]
    public void ASeatCountsUnitIsTheBodysOwnInHebrew()
    {
        var c = Inputs.Content;
        // Only the appointed council may lack its election date: a Knesset row without "elected" would otherwise count
        // its seats as "חברים" with nothing failing.
        CollectionAssert.AreEqual(new[] { "provisional_state_council" }, c.Legislatures.Where(l => !l.Elected.HasValue).Select(l => l.Id),
            "legislatures.yaml: every body but the appointed council has its \"elected\" date");
        Assert.IsTrue(c.Legislatures.Any(l => l.Elected.HasValue), "elected bodies in content");
        foreach (var leg in c.Legislatures)
        {
            Assert.AreEqual(leg.Elected.HasValue ? "מנדטים" : "חברים", CabinetPresenter.SeatUnit(He, leg), leg.Id);
            Assert.AreEqual("seats", CabinetPresenter.SeatUnit(En, leg), leg.Id);
        }
        Assert.AreEqual("seats", CabinetPresenter.SeatUnit(En, null));

        // The game opens under the council (15 May 1948 to 14 February 1949): the house and the budget count its members.
        Boot(UiLanguage.Hebrew);
        Assert.IsFalse(c.Legislatures.First(l => l.Id == "provisional_state_council").Elected.HasValue);
        foreach (var name in new[] { "cabinet-coalition", "cabinet-opposition", "cabinet-majority", "cabinet-largest", "budget-vote" })
        {
            var t = Ui.Q<Label>(name).text;
            StringAssert.Contains("חברים", t, name);
            StringAssert.DoesNotContain("מנדטים", t, name);
        }
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
            // Its own text no longer is since its date turned Hebrew (#701, "10 במרץ 1949" is shorter than "10 March 1949"),
            // so the control triples the longest office name: the class, not one string's width, is what is checked.
            if (language == UiLanguage.Hebrew && width == 1920)
            {
                var longest = (Label)rows.Select(r => r[0]).OrderByDescending(x => ((Label)x).text.Length).First();
                longest.text = string.Join(" ", Enumerable.Repeat(longest.text, 3));
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
            // The start screen's Hebrew lines are Hebrew in both languages (the Declaration's own text), and so is Settings'
            // button that names Hebrew in Hebrew (#1036).
            var texts = root.Query<TextElement>().ToList().Where(e => !e.ClassListContains("start-he") && e.name != "settings-language-he").ToList();
            Assert.Greater(texts.Count, 30, "Game.uxml's labels and buttons");
            for (int i = 0; i < 3; i++) yield return null;             // resolved styles need a style pass
            foreach (var e in texts)
                Assert.AreNotSame(hebrew, FontOf(e), $"English: '{e.name}' ({string.Join(" ", e.GetClasses())})");
            Assert.AreSame(hebrew, FontOf(root.Q<TextElement>("settings-language-he")), "English: 'עברית' has the Hebrew face");
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

    /// <summary>#1210: the one rule the event card and the outcome caption share. In Hebrew a text with a Hebrew form shows
    /// it, a text without one falls back to the English (fail open); in English the English always.</summary>
    [Test]
    public void ContentNamesFillPicksTheHebrewTextOnlyInHebrewAndWhereThereIsOne()
    {
        var c = Inputs.Content;
        var s = Statecraft.Sim.Simulation.NewGame(c, 1, Statecraft.Sim.Mode.Historical);
        var option = c.Events.First(e => e.Id == "maabarot_policy").Options.First(o => o.Id == "maabarot");
        StringAssert.DoesNotContain("{", option.TextHe, "precondition: no placeholder, so the Hebrew shows as written");
        Assert.AreEqual(option.TextHe, ContentNames.Fill(He, c, s, s.Date, option.Text, option.TextHe));
        Assert.AreEqual(option.Text, ContentNames.Fill(He, c, s, s.Date, option.Text, ""), "no text_he: the English");
        Assert.AreEqual(option.Text, ContentNames.Fill(En, c, s, s.Date, option.Text, option.TextHe), "English UI: the English");
    }
}
