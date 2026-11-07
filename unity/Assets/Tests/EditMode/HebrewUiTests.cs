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

    // #2265: only the query is read; a flag in the fragment (after the first '#') is not a query parameter.
    [Test]
    public void ALanguageFlagAfterTheFragmentIsIgnored()
    {
        var none = new string[0];
        foreach (var q in new[] { "?x=1#&lang=he", "#?lang=he" })
            Assert.AreEqual(UiLanguage.English, UiLanguage.Requested("http://localhost:8000/index.html" + q, none, webgl: true), q);
        // A fragment-only flag is no flag: the stored choice stands, in both directions.
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart(UiLanguage.Hebrew, "http://localhost:8000/index.html#&lang=en", none, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart(null, "http://localhost:8000/index.html?x=1#?lang=he", none, webgl: true));
        // The query still counts when a fragment follows.
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.Requested("http://localhost:8000/index.html?lang=he#x", none, webgl: true));
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart(UiLanguage.Hebrew, "http://localhost:8000/index.html?lang=en#x", none, webgl: true));
    }

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

    /// <summary>#2688: the roomy tier reads physical meter bounds and the gap on the navigation's side, so mirroring the
    /// top-bar rows does not change whether it fits. This boots each language through GameRoot.Begin on the same panel size.
    /// Red-first: UpdateRoomy measured from the first to last meter and always towards nav.xMin, so Hebrew never added it.</summary>
    [UnityTest]
    public IEnumerator TheHebrewTopBarIsRoomyExactlyWhenTheEnglishTopBarIs()
    {
        var roomy = new Dictionary<string, bool>();
        foreach (var language in new[] { UiLanguage.English, UiLanguage.Hebrew })
        {
            var restore = OnTexture(1920, 1080);
            try
            {
                var game = Object.FindFirstObjectByType<GameRoot>();
                game.Store = new SaveStore(saveDir);
                game.Language = language;
                game.Begin(Inputs, 0);
                for (int i = 0; i < 30 && !(Ui.Q("top-bar").worldBound.width > 0); i++) yield return null;
                for (int i = 0; i < 6; i++) yield return null;          // geometry and the roomy class settle
                roomy[language] = Ui.Q("top-bar").ClassListContains(HudPresenter.RoomyClass);
            }
            finally { restore(); }
        }

        Assert.IsTrue(roomy[UiLanguage.English], "1920x1080 English is roomy (PromoHudTests' pinned expectation)");
        Assert.AreEqual(roomy[UiLanguage.English], roomy[UiLanguage.Hebrew], "1920x1080 Hebrew matches English");
    }

    // #2045: every Orders.Check refusal (its Refused* constants) has a string in both files, and the Hebrew one is Hebrew,
    // not the sim's English. A missing key throws in UiText's lookup.
    [Test]
    public void EveryStandingOrderRefusalHasAStringInBothLanguages()
    {
        var refusals = typeof(Statecraft.Sim.Orders).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.Name.StartsWith("Refused")).Select(f => (string)f.GetRawConstantValue()).ToList();
        Assert.That(refusals, Is.Not.Empty);
        foreach (var reason in refusals)
        {
            Assert.AreEqual(reason, SimBridge.ReasonText(En, reason), "the English string is the sim's own");
            var he = SimBridge.ReasonText(He, reason);
            Assert.AreNotEqual(reason, he, "no Hebrew string for: " + reason);
            StringAssert.IsMatch("[\u05D0-\u05EA]", he, reason);
        }
    }

    // #2198, #2590: every localized Arms-purchase refusal reaches the end-of-turn toast (ReasonText) in both languages.
    [Test]
    public void EveryArmsPurchaseRefusalHasAStringInBothLanguages()
    {
        foreach (var reason in new[]
        {
            Statecraft.Sim.Pools.RefusedDebtCeiling,
            Statecraft.Sim.Pools.RefusedStockCapacity,
        })
        {
            Assert.AreEqual(reason, SimBridge.ReasonText(En, reason), "the English string is the sim's own");
            var he = SimBridge.ReasonText(He, reason);
            Assert.AreNotEqual(reason, he, "no Hebrew string for: " + reason);
            StringAssert.IsMatch("[א-ת]", he, reason);
            StringAssert.DoesNotMatch("[A-Za-z]", he, reason);
        }
    }

    // #2072: every Combat.Check and Training.Reinforce refusal (their Refused* constants) has a string in both files. Each
    // format is filled with distinct values as the sim fills it (a multi-word town, a real course, different numbers) and read
    // with content: the English is the sim's own text, the Hebrew is its line with the Hebrew town and course in their slots,
    // its placeholders in the English's order, and no Latin letter left (owner review item 2: "7"s everywhere caught no
    // swapped slot, no capture in the wrong order, no lazy capture splitting a town).
    [TestCase(typeof(Statecraft.Sim.Combat))]
    [TestCase(typeof(Statecraft.Sim.Training))]
    public void EveryAttackAndReinforceRefusalHasAStringInBothLanguages(System.Type check)
    {
        var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
        var namesHe = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.NameHe);
        var place = c.SettlementNames.Single(x => x.Value == "Rishon LeZion");
        var recruit = c.UnitTypes.Single(t => t.Id == "recruit");
        object townSlot = new object(), courseSlot = new object();
        // Each format's values in the sim's order (Combat.Check, Training.Reinforce/Start), and the key it reads as.
        var filled = new Dictionary<string, (string key, object[] values)>
        {
            [Statecraft.Sim.Combat.RefusedArms] = ("unit.attack_refused.arms", new object[] { 12, 3 }),
            [Statecraft.Sim.Training.RefusedNotInCity] = ("unit.reinforce_refused.not_in_city", new object[] { 41, townSlot }),
            [Statecraft.Sim.Training.RefusedNoCourse] = ("unit.reinforce_refused.no_course", new object[] { 41 }),
            [Statecraft.Sim.Training.RefusedNoLosses] = ("unit.reinforce_refused.no_losses", new object[] { 41 }),
            [Statecraft.Sim.Training.RefusedRoomOne] = ("unit.reinforce_refused.room_one", new object[] { 41, 1 }),
            [Statecraft.Sim.Training.RefusedRoomMany] = ("unit.reinforce_refused.room_many", new object[] { 41, 5 }),
            [Statecraft.Sim.Training.RefusedNoCamp] = ("unit.reinforce_refused.no_camp", new object[] { townSlot }),
            [Statecraft.Sim.Training.RefusedNoIndustry] = ("unit.reinforce_refused.no_industry", new object[] { townSlot }),
            [Statecraft.Sim.Training.RefusedNoHakhshara] = ("unit.reinforce_refused.no_hakhshara", new object[] { townSlot }),
            [Statecraft.Sim.Training.RefusedIdle] = ("unit.reinforce_refused.idle", new object[] { townSlot, 6 }),
            [Statecraft.Sim.Training.RefusedArms] = ("unit.reinforce_refused.arms", new object[] { townSlot, courseSlot, 12, 3 }),
        };
        object English(object v) => v == townSlot ? place.Value : v == courseSlot ? Statecraft.Sim.Training.Course(recruit) : v;
        object Hebrew(object v) => v == townSlot ? namesHe[place.Key] : v == courseSlot ? He["why.cause.course." + Statecraft.Sim.Training.CourseId(recruit)] : v;
        string Slots(string line) => string.Join(",", Regex.Matches(line, @"\{\d+\}").Cast<Match>().Select(m => m.Value));
        var formats = check.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.Name.StartsWith("Refused")).Select(f => (string)f.GetRawConstantValue()).ToList();
        Assert.That(formats, Is.Not.Empty, check.Name + " has no Refused* constants");
        foreach (var format in formats)
        {
            var hasValues = Slots(format) != "";
            Assert.AreEqual(hasValues, filled.ContainsKey(format), "a format and only a format is filled here: " + format);
            var reason = hasValues ? string.Format(System.Globalization.CultureInfo.InvariantCulture, format, filled[format].values.Select(English).ToArray()) : format;
            Assert.AreEqual(reason, SimBridge.ReasonText(En, reason, c, namesHe), "the English string is the sim's own");
            var he = SimBridge.ReasonText(He, reason, c, namesHe);
            StringAssert.IsMatch("[\u05D0-\u05EA]", he, "no Hebrew string for: " + reason);
            StringAssert.DoesNotMatch("[A-Za-z]", he, reason);
            if (!hasValues) continue;
            var (key, values) = filled[format];
            Assert.AreEqual(Slots(En[key]), Slots(He[key]), key + ": the Hebrew line takes its values in the English's order");
            Assert.AreEqual(He.Format(key, values.Select(Hebrew).ToArray()), he, "the values in their slots: " + reason);
        }
    }

    // #2707 catch-all: every Refused* constant anywhere in the sim reaches the toast (ReasonText) in Hebrew under Hebrew, and
    // as the sim's own English under English. A format is filled with distinct numbers, so its pattern must take any value;
    // the values themselves are the per-check tests' (above, CityProjectPanelTests). A refusal still a bare literal in the
    // sim is not seen here: naming it a Refused* constant is how it joins this check.
    [Test]
    public void EveryNamedSimRefusalHasAHebrewString()
    {
        var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
        var refusals = typeof(Statecraft.Sim.Orders).Assembly.GetTypes()
            .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("Refused"))
            .Select(f => (name: f.DeclaringType!.Name + "." + f.Name, format: (string)f.GetRawConstantValue())).ToList();
        Assert.That(refusals.Count, Is.GreaterThan(40));
        var missing = new List<string>();
        foreach (var (name, format) in refusals)
        {
            var slots = Regex.Matches(format, @"\{(\d+)").Cast<Match>().Select(m => int.Parse(m.Groups[1].Value) + 1).DefaultIfEmpty(0).Max();
            var reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, format,
                Enumerable.Range(0, slots).Select(i => (object)(i + 2)).ToArray());
            // The capture answers read as the capture dialog's reviewed line in English too (TheCaptureAnswersReadAsTheReviewedLines).
            if (!CaptureAnswers.ContainsKey(format))
                Assert.AreEqual(reason, SimBridge.ReasonText(En, reason), name + ": the English string is the sim's own");
            if (!Regex.IsMatch(SimBridge.ReasonText(He, reason, c), "[א-ת]")) missing.Add(name + " \"" + reason + "\"");
        }
        Assert.IsEmpty(missing, "no Hebrew string for:\n" + string.Join("\n", missing));
    }

    // #2707 batch 3, the owner's option (b): a capture or end-of-occupation answer the end of turn refuses reuses the capture
    // dialog's reviewed capture.refusal.* line in both languages (settlement-control-strings-review.yaml gains nothing), with
    // the place's Hebrew name in its slot. The answers with no reviewed line that fits stay the sim's English.
    static readonly Dictionary<string, string> CaptureAnswers = new Dictionary<string, string>
    {
        [Statecraft.Sim.SettlementControl.RefusedNoOneToDeport] = "capture.refusal.no_one_to_deport",
        [Statecraft.Sim.SettlementControl.RefusedUnknownOutcome] = "capture.refusal.unknown_outcome",
        [Statecraft.Sim.SettlementControl.RefusedOccupyNeedsUnit] = "capture.refusal.needs_unit",
        [Statecraft.Sim.SettlementControl.RefusedAnnexNeedsUnit] = "capture.refusal.needs_unit",
        [Statecraft.Sim.SettlementControl.RefusedWithdrawNeedsUnit] = "capture.refusal.needs_unit",
        [Statecraft.Sim.SettlementControl.RefusedNoFreeHex] = "capture.refusal.no_free_hex",
        [Statecraft.Sim.Cities.RefusedAlreadyCity] = "capture.refusal.already_city",
    };

    [Test]
    public void TheCaptureAnswersReadAsTheReviewedLines()
    {
        var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
        var namesHe = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName))).ToDictionary(t => t.Id, t => t.NameHe);
        var place = c.SettlementNames.Single(x => x.Value == "Rishon LeZion");
        foreach (var (format, key) in CaptureAnswers)
        {
            var reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, place.Value);
            Assert.AreEqual(En.Format(key, place.Value), SimBridge.ReasonText(En, reason, c, namesHe), reason);
            Assert.AreEqual(He.Format(key, namesHe[place.Key]), SimBridge.ReasonText(He, reason, c, namesHe), reason);
            StringAssert.DoesNotMatch("[A-Za-z]", SimBridge.ReasonText(He, reason, c, namesHe), reason);
        }
        foreach (var reason in new[] { "Deport is not built yet", "That place is not awaiting a decision on its capture",
                     "That place is not awaiting the end of its occupation" })
            Assert.AreEqual(reason, SimBridge.ReasonText(He, reason, c, namesHe), "no reviewed line fits, so the sim's English");
    }

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

    /// <summary>#2340: the language switch rebuilds the info panel and the unit layer, and the player's selection comes back
    /// with them: a hex with its outline, a unit with its card and no hex outline, a pioneer group's card. Before, the new
    /// panel showed nothing while the old hex outline stayed on the map. Both ways, English to Hebrew and back.</summary>
    [Test]
    public void TheLanguageSwitchKeepsTheSelection()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        game.Begin(Inputs, 0);
        // The opening cards answered, not hidden: an open card disables the info panel, and the rebuild opens a hidden card
        // again (the other half of that #2340 item), so no pioneer card could be shown over it.
        for (int i = 0; i < 5 && game.Card!.Shown != null; i++) UiTestKit.Press(Ui.Q("event-options").Query<Button>().First());
        Assert.IsNull(game.Card!.Shown, "precondition: no card over the info panel");
        var bridge = game.Bridge!;
        bool Outlined() => map.Terrain.transform.Find(MapScene.SelectionName) is Transform t && t.GetComponent<MeshRenderer>().enabled;
        var hex = bridge.Content.Hexes.First(h => h.Land && game.Units!.UnitToSelectAt(h.Q, h.R) == null
            && !bridge.Content.SettlementHexes.Any(x => x.Q == h.Q && x.R == h.R) && !bridge.Content.Sites.Any(x => x.Q == h.Q && x.R == h.R));
        var unit = game.Units!.Tokens.First(t => t.Token.Unit.Side == Statecraft.Game.Map.Units.UnitData.Israel).Token.Unit.Id;
        var pioneer = PioneerCardTests.Add(bridge.State, hex.Q, hex.R);
        string language = UiLanguage.English;
        void Switch()
        {
            language = language == UiLanguage.English ? UiLanguage.Hebrew : UiLanguage.English;
            UiTestKit.Press(Ui.Q<Button>("nav-settings"));
            UiTestKit.Press(Ui.Q<Button>("settings-language-" + language));
            Assert.AreEqual(language == UiLanguage.Hebrew, Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "precondition: the UI was rebuilt");
            if (Ui.Q("root").ClassListContains(ScreenNav.ScreenOpenClass)) UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        }

        for (int round = 0; round < 2; round++)   // to Hebrew, then back to English
        {
            GameRoot.OnHexClicked(map, game.Units!, game.Info!, hex.Q, hex.R);
            Assert.AreEqual((hex.Q, hex.R), game.Info!.Selected, "precondition: the hex is selected");
            Switch();
            Assert.AreEqual((hex.Q, hex.R), game.Info!.Selected, language + ": the hex is still selected");
            Assert.IsFalse(game.Info.ShowsUnit, language + ": as a hex");
            Assert.IsTrue(Outlined(), language + ": with its outline");

            game.FocusUnit(unit);
            Assert.AreEqual(unit, game.Units!.Selected, "precondition: the unit is selected");
            Switch();
            Assert.AreEqual(unit, game.Units!.Selected, language + ": the unit is still selected");
            Assert.IsTrue(game.Info!.ShowsUnit, language + ": its card is shown");
            Assert.IsNull(game.Info.Pioneer, language + ": the unit's card, not a pioneer's");
            Assert.IsFalse(Outlined(), language + ": and no hex outline");

            game.Info.Show(hex.Q, hex.R);
            UiTestKit.Press(PioneerCardTests.Link(Ui, pioneer));
            Assert.AreEqual(pioneer, game.Info.Pioneer, "precondition: the pioneer group's card");
            Switch();
            Assert.AreEqual(pioneer, game.Info!.Pioneer, language + ": the pioneer group's card is still shown");
            Assert.IsNull(game.Units!.Selected, language + ": with no unit token selected");
            Assert.IsFalse(Outlined(), language + ": and no hex outline");
        }
    }

    /// <summary>#2708: the language switch rebuilds the Budget screen over the same game, and the new screen keeps the
    /// player's unsubmitted draft (and says it is unsubmitted) instead of starting again from the submitted budget.</summary>
    [Test]
    public void TheLanguageSwitchKeepsAnUnsubmittedBudgetDraft()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        game.Begin(Inputs, 0);
        var office = game.Bridge!.State.Budget.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        var expected = BudgetPresenter.Adjust(game.Bridge.State.Budget, office, BudgetPresenter.Step);
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        UiTestKit.Press(Ui.Q<Button>("budget-" + office + "-up"));
        Assert.AreEqual(En["budget.unsaved"], Ui.Q<Label>("budget-status").text, "precondition: an unsubmitted draft");

        UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        UiTestKit.Press(Ui.Q<Button>("settings-language-he"));
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "precondition: the UI was rebuilt");
        UiTestKit.Press(Ui.Q<Button>("nav-budget"));
        Assert.AreEqual(He["budget.unsaved"], Ui.Q<Label>("budget-status").text, "the draft is still unsubmitted");
        Assert.IsEmpty(game.Bridge!.Queued, "nothing was submitted by the switch");
        Assert.IsTrue(game.Advisor!.HoldsBudgetUnsubmitted, "the advisor knows the kept draft is unsubmitted, so a put-off budget warning is not pruned");

        UiTestKit.Press(Ui.Q<Button>("budget-submit"));
        var queued = (Statecraft.Sim.AllocateBudget)game.Bridge.Queued.Single();
        foreach (var kv in expected) Assert.AreEqual(kv.Value, queued.Shares[kv.Key], 1e-9, kv.Key + ": the edited draft is what Submit sends");
    }

    /// <summary>#2708, the other side: the kept draft belongs to the game the switch rebuilt over. A New game or a load right
    /// after the switch starts from that game's budget, with no draft.</summary>
    [Test]
    public void ANewGameOrALoadAfterTheLanguageSwitchStartsWithNoBudgetDraft()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        game.Begin(Inputs, 0);
        var office = game.Bridge!.State.Budget.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        void EditThenSwitch(string language)
        {
            UiTestKit.Press(Ui.Q<Button>("nav-budget"));
            UiTestKit.Press(Ui.Q<Button>("budget-" + office + "-up"));
            Assert.IsNotEmpty(Ui.Q<Label>("budget-status").text, "precondition: an unsubmitted draft");
            UiTestKit.Press(Ui.Q<Button>("nav-settings"));
            UiTestKit.Press(Ui.Q<Button>("settings-language-" + language));
            Assert.IsNotEmpty(Ui.Q<Label>("budget-status").text, "precondition: the switch kept the draft");
        }
        void AssertNoDraft(string what)
        {
            Assert.AreEqual("", Ui.Q<Label>("budget-status").text, what + ": no draft");
            Assert.IsFalse(Ui.Q<Button>("budget-submit").enabledSelf, what + ": nothing to submit");
            Assert.IsFalse(game.Advisor!.HoldsBudgetUnsubmitted, what + ": the advisor holds no budget warning");
        }

        EditThenSwitch(UiLanguage.Hebrew);
        var before = game.Bridge.Session;
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreNotSame(before, game.Bridge.Session, "precondition: New game replaced the session");
        AssertNoDraft("a New game");

        EditThenSwitch(UiLanguage.English);
        Directory.CreateDirectory(saveDir);
        game.Store!.WriteAutosave(Statecraft.Sim.GameSession.New(Inputs.Content, 9, Statecraft.Sim.Mode.Historical).Save.Write());
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        AssertNoDraft("a load");
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

    // --- The start screen's language choice (#2054) ------------------------------------------------------------------

    GameRoot BootWithStartScreen(SettingsStore settings, string? language)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Settings = settings;
        game.Language = language;
        game.Begin(Inputs, 0, startScreen: true);
        return game;
    }

    static bool StartOpen => Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass);
    static bool Marked(string name) => Ui.Q<Button>(name).ClassListContains(SettingsPresenter.SelectedClass);

    /// <summary>The start screen offers the same two languages as Settings, each named in itself, with the one in use
    /// marked, in both languages.</summary>
    [Test]
    public void TheStartScreenOffersBothLanguagesWithTheOneInUseMarked()
    {
        var settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        BootWithStartScreen(settings, null);                           // no file and no flag: English
        Assert.IsTrue(StartOpen, "precondition: the start screen shows");
        Assert.AreEqual("English", Ui.Q<Button>("start-language-en").text);
        Assert.AreEqual("עברית", Ui.Q<Button>("start-language-he").text, "each language is named in itself");
        Assert.IsTrue(Marked("start-language-en"), "English is marked");
        Assert.IsFalse(Marked("start-language-he"));

        BootWithStartScreen(settings, UiLanguage.Hebrew);
        Assert.IsTrue(StartOpen);
        Assert.AreEqual("English", Ui.Q<Button>("start-language-en").text, "not translated");
        Assert.AreEqual("עברית", Ui.Q<Button>("start-language-he").text);
        Assert.IsTrue(Marked("start-language-he"), "Hebrew is marked");
        Assert.IsFalse(Marked("start-language-en"));
    }

    /// <summary>#2552: a save notice belongs to the session as a language-independent key. Rebuilding the open start
    /// screen through its language choice keeps the notice and renders it in the newly selected language.</summary>
    [TestCase(UiLanguage.English, UiLanguage.Hebrew)]
    [TestCase(UiLanguage.Hebrew, UiLanguage.English)]
    public void AStartScreenSaveNoticeSurvivesALanguageSwitchInTheNewLanguage(string from, string to)
    {
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, SaveStore.AutosaveName), "not a save");
        LogAssert.Expect(LogType.Warning, new Regex("Autosave (rejected|could not be loaded)"));   // either path gives save.unreadable
        var game = BootWithStartScreen(new SettingsStore(Path.Combine(saveDir, "settings")), from);
        var session = game.Bridge!.Session;
        var fromText = from == UiLanguage.Hebrew ? He : En;
        var toText = to == UiLanguage.Hebrew ? He : En;
        Assert.AreEqual(fromText.Format("save.unreadable", fromText["save.reason_other"]), Ui.Q<Label>("toast").text,
            "the notice is raised in the starting language");

        UiTestKit.Press(Ui.Q<Button>("start-language-" + to));

        Assert.AreSame(session, game.Bridge!.Session, "the language switch keeps the session that owns the notice");
        Assert.IsTrue(StartOpen, "the language switch rebuilds the open start screen");
        Assert.AreEqual(toText.Format("save.unreadable", toText["save.reason_other"]), Ui.Q<Label>("toast").text,
            "the retained key is rendered in the new language, not kept as already-rendered text");
    }

    /// <summary>#2552: a New game replaces the bridge's session without Begin, so the old session's save notice must not come
    /// back when a language switch then rebuilds the start screen over the new game (Begin with keep, as SwitchLanguage
    /// calls it). The UI can't reach this today (a New game closes the start screen, so a later switch rebuilds without it),
    /// so the test drives the two steps directly to pin the invariant. Red-first: without the session check the old
    /// "could not be loaded" notice is shown again.</summary>
    [Test]
    public void ANewGameDropsTheOldSessionsSaveNoticeOnALanguageSwitch()
    {
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, SaveStore.AutosaveName), "not a save");
        LogAssert.Expect(LogType.Warning, new Regex("Autosave (rejected|could not be loaded)"));
        var game = BootWithStartScreen(new SettingsStore(Path.Combine(saveDir, "settings")), UiLanguage.English);
        var stale = En.Format("save.unreadable", En["save.reason_other"]);
        Assert.AreEqual(stale, Ui.Q<Label>("toast").text, "precondition: the first session raised the notice");

        game.Bridge!.Replace(Statecraft.Sim.GameSession.New(Inputs.Content, 1, Statecraft.Sim.Mode.Historical));   // as NewGamePresenter.Press
        game.Language = UiLanguage.Hebrew;
        game.Begin(Inputs, 0, startScreen: true, keep: game.Bridge);

        var toast = Ui.Q<Label>("toast");
        Assert.AreEqual("", toast.text, "the new game's start screen shows no save notice at all");
        Assert.AreEqual(DisplayStyle.None, toast.style.display.value, "and the toast is hidden");
    }

    /// <summary>#2552, the player's route beside the test above: after New game replaces a warned-about saved session, a
    /// language switch must not bring that session's save notice back. Both start-screen New Game presses, then Settings'
    /// language choice after the start screen closes. It passes with or without the session check (a closed start screen
    /// rebuilds with no notices), so it pins the visible behaviour, and the test above pins the check itself.</summary>
    [Test]
    public void ANewGameFromTheStartScreenShowsNoOldNoticeAfterALanguageSwitch()
    {
        Directory.CreateDirectory(saveDir);
        var saved = Statecraft.Sim.GameSession.New(Inputs.Content, 9, Statecraft.Sim.Mode.Historical).Save.Write()
            .Replace("content " + Inputs.Content.Hash, "content " + new string('0', Inputs.Content.Hash.Length));
        new SaveStore(saveDir).WriteAutosave(saved);
        LogAssert.Expect(LogType.Warning, "Autosave loaded with a warning: " + Statecraft.Sim.GameSession.ContentChanged);
        var game = BootWithStartScreen(new SettingsStore(Path.Combine(saveDir, "settings")), UiLanguage.English);
        var oldSession = game.Bridge!.Session;
        var stale = En["save.warning." + Statecraft.Sim.GameSession.ContentChanged];
        Assert.AreEqual(stale, Ui.Q<Label>("toast").text, "precondition: the first session raised the notice");

        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.AreEqual(En["newgame.confirm"], Ui.Q<Label>("toast").text, "the first press arms New Game");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.AreNotSame(oldSession, game.Bridge!.Session, "the second press started a new game");
        Assert.IsFalse(StartOpen, "NewGamePresenter.Started closes the start screen");

        UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        UiTestKit.Press(Ui.Q<Button>("settings-language-he"));

        var toast = Ui.Q<Label>("toast");
        Assert.AreEqual("", toast.text, "the new game shows no notice from the replaced saved session");
        Assert.AreEqual(DisplayStyle.None, toast.style.display.value, "and the toast is hidden");
    }

    /// <summary>A choice on the start screen goes through Settings' own switch: the UI changes at once over the same game,
    /// the start screen stays up (Continue still only with a save) with the focus on the choice, the choice is kept under
    /// Settings' key, and Settings shows it. The other way round, a choice in Settings is what the next start screen
    /// shows: the two can never disagree.</summary>
    [Test]
    public void TheStartScreenAndSettingsShareOneLanguageChoice()
    {
        var settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        var game = BootWithStartScreen(settings, null);
        var session = game.Bridge!.Session;
        Assert.AreEqual(DisplayStyle.None, Ui.Q<Button>("start-continue").style.display.value, "precondition: no save, no Continue");

        UiTestKit.Press(Ui.Q<Button>("start-language-he"));
        Assert.IsTrue(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "Hebrew at once, no reload");
        Assert.AreEqual(He.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text);
        Assert.AreSame(session, game.Bridge.Session, "the same game goes on");
        Assert.IsTrue(StartOpen, "the start screen stays up");
        Assert.IsFalse(ScreenNav.IsOpen(Ui.Q("screen-settings")), "Settings does not open over it");
        Assert.AreEqual(DisplayStyle.None, Ui.Q<Button>("start-continue").style.display.value, "still no save to continue");
        Assert.AreEqual(He["start.new_game"], Ui.Q<Button>("start-new-game").text);
        Assert.AreSame(Ui.Q<Button>("start-language-he"), Ui.focusController?.focusedElement, "the focus stays on the choice");
        Assert.IsTrue(Marked("start-language-he"), "Hebrew is marked on the start screen");
        Assert.IsFalse(Marked("start-language-en"));
        Assert.AreEqual(UiLanguage.Hebrew, settings.Read(SettingsPresenter.LanguageKey), "kept under Settings' key");
        Assert.IsTrue(Marked("settings-language-he"), "and Settings shows Hebrew");
        Assert.IsFalse(Marked("settings-language-en"));

        // The other way: New game (no save) closes the screen; English in Settings is what the next start screen shows.
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        Assert.IsFalse(StartOpen, "precondition: on the map");
        UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        UiTestKit.Press(Ui.Q<Button>("settings-language-en"));
        Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "English at once");
        Assert.IsFalse(StartOpen, "a switch in Settings brings no start screen back");
        Assert.AreEqual(UiLanguage.English, settings.Read(SettingsPresenter.LanguageKey));
        game.Language = null;                                          // a reload: only the file remains
        game.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(StartOpen);
        Assert.IsTrue(Marked("start-language-en"), "the start screen shows Settings' choice");
        Assert.IsFalse(Marked("start-language-he"));
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
        foreach (var s in new[] { "15 May 1948", "IL\u00A020.0m", "+5%", "-5%", "30%", "", "David Ben-Gurion", "Mapai :מפלגה", "$40.0m", "1948" })
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
    /// Turn rebuilds (localized rows plus an explicit legacy English fallback fixture).</summary>
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
            // #1682 localizes the actual causes. Keep mixed-direction coverage through the supported unkeyed fallback.
            Ui.Q<ScrollView>("why-rows").contentContainer.Add(new Label(CauseText.Line(
                new Statecraft.Sim.Cause("fixture", "fixture", 0, "Legacy fallback"), game.Bridge.Content,
                Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), He)));
            var built = Ui.Q<ScrollView>("why-rows").contentContainer.Query<TextElement>().ToList();
            Assert.IsTrue(built.All(t => t.languageDirection == LanguageDirection.Inherit), "the new rows have no direction before the next frame");
            for (int i = 0; i < 2; i++) yield return null;
            Directed(built, "a turn");
            Assert.IsTrue(built.Any(t => t.languageDirection == LanguageDirection.RTL) && built.Any(t => t.languageDirection == LanguageDirection.LTR),
                "the rebuilt rows mix Hebrew and the explicit legacy English fallback");
        }
        finally { restore(); }
    }

    /// <summary>#1915: a city panel built in a game started in Hebrew reads in the right order from the first frame: its
    /// right-column lines and its project buttons are built where they stand (GddHexPanel's Line and Button set the words
    /// once the element is on the panel), so UiDirection's change callback gives each its own direction. Uses the #1688
    /// tour fixture's city - the one in the issue's capture - whose projects show. Red-first: the lines and buttons were
    /// built with their words before they joined the panel, no text change reached UiDirection, and no trigger walks the
    /// tree before the player reads the panel.</summary>
    [UnityTest]
    public IEnumerator TheCityPanelReadsRightToLeftWhenTheGameStartsInHebrew()
    {
        var restore = OnTexture(1920, 1080);
        try
        {
            var inputs = new GameInputs { Content = AdvisorTour1688Fixture.Content(),
                Map = GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap), Goldens = new GoldenData() };
            var game = Object.FindFirstObjectByType<GameRoot>();
            game.Store = new SaveStore(saveDir);
            game.Language = UiLanguage.Hebrew;
            game.Begin(inputs, 0);
            for (int i = 0; i < 2; i++) yield return null;

            var city = AdvisorTargets.CityHex(game.Bridge!.Content, game.Bridge.State)!.Value;
            game.Info!.Show(city.q, city.r);   // built by the tour or a map click: no UI submit, click or turn follows
            for (int i = 0; i < 2; i++) yield return null;

            var texts = Ui.Q<VisualElement>(GddHexPanel.HostName).Query<TextElement>().ToList()
                .Where(t => !string.IsNullOrEmpty(t.text)).ToList();
            Assert.IsNotEmpty(texts, "the city panel's right column has lines");
            Assert.IsTrue(texts.Any(t => UiDirection.HasRightToLeft(t.text)), "the column mixes Hebrew with numbers");
            Directed(texts, "the city panel's lines in a game started in Hebrew");
            var buttons = Ui.Q<VisualElement>(GddHexPanel.ProjectsHostName).Query<Button>().ToList();
            Assert.IsTrue(buttons.Any(b => b.name.StartsWith("city-project-", StringComparison.Ordinal) && b.name != GddHexPanel.ToggleName), "the city in the #1915 capture shows its project buttons");
            Directed(buttons, "the city panel's project buttons in a game started in Hebrew");
        }
        finally { restore(); }
    }

    static void Directed(IReadOnlyList<TextElement> texts, string after)
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
        ("nav-why", "hud.nav.why"), ("nav-timeline", "nav.timeline"), ("nav-encyclopedia", "hud.nav.encyclopedia"), ("nav-objectives", "hud.nav.objectives"),
        ("new-game", "hud.new_game"), ("end-turn", "hud.end_turn_needs_orders"),
        ("event-draft", "event.draft"),
        ("budget-screen-title", "budget.title"), ("budget-reset", "budget.reset"), ("budget-submit", "budget.submit"),
        ("defence-screen-title", "defence.title"), ("de-escalate", "defence.lower"), ("escalate", "defence.raise"),
        ("why-screen-title", "why.title"),
        ("encyclopedia-title", "encyclopedia.title"), ("objectives-title", "objectives.title"),
        ("nav-settings", "hud.nav.settings"), ("settings-screen-title", "settings.title"), ("settings-tiltshift-label", "settings.tiltshift"),
        ("settings-quality-label", "settings.quality"), ("settings-quality-auto", "settings.quality.auto"),
        ("settings-quality-high", "settings.quality.high"), ("settings-quality-low", "settings.quality.low"),
        ("settings-sound-label", "settings.sound"), ("settings-sound-off", "settings.sound.off"),
        ("settings-sound-low", "settings.sound.low"), ("settings-sound-high", "settings.sound.high"),
        ("settings-music-label", "settings.music"), ("settings-music-off", "settings.music.off"),
        ("settings-music-low", "settings.music.low"), ("settings-music-high", "settings.music.high"),
        ("settings-advisors-label", "settings.advisors"), ("settings-language-label", "settings.language"),
        ("settings-uiscale-label", "settings.uiscale"), ("settings-companion-label", "settings.companion"),   // #1119
        ("settings-community-label", "settings.community"), ("settings-star", "community.star"), ("start-star", "community.star"),
        ("settings-report", "feedback.open"), ("start-report", "feedback.open"), ("feedback-title", "feedback.open"),   // #1169
        ("hud-star", "community.star"), ("hud-report", "feedback.open"), ("hud-share", "share.open"),   // #1236
        ("feedback-intro", "feedback.intro"), ("feedback-bug", "feedback.bug"), ("feedback-feature", "feedback.feature"),
        ("feedback-itch", "feedback.itch"), ("feedback-close", "report.close"),
        ("settings-share", "share.open"), ("turn-report-share", "share.open"), ("share-title", "share.title"),   // #1170
        ("share-attach", "share.attach"), ("share-close", "report.close"),
        ("start-source", "start.source"), ("start-recording-credit", "start.recording_credit"),
        ("start-continue", "start.continue"), ("start-new-game", "start.new_game"),
        ("start-quit", "ui.quit"), ("settings-quit", "ui.quit"),   // #2153
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
        [Values(UiLanguage.Hebrew, UiLanguage.English)] string language, [Values(1920, 390)] int width, [Values(100, 125, 150)] int uiScale)   // #40: at the Settings scale choices
    {
        var restore = OnTexture(width, width == 1920 ? 1080 : 844);
        try
        {
            if (uiScale != 100) Object.FindFirstObjectByType<UIDocument>().panelSettings.referenceResolution = new Vector2Int(Mathf.RoundToInt(1920f * 100f / uiScale), Mathf.RoundToInt(1080f * 100f / uiScale));   // #40: the panel as the Settings scale choice sets it
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
                Assert.LessOrEqual(row.layout.width, room + 0.5f, $"{language} {width} {uiScale}%: '{row[0].name}' is wider than the screen");
                foreach (var part in row.Children())
                {
                    Assert.LessOrEqual(part.layout.xMax, row.layout.width + 0.5f, $"{language} {width} {uiScale}%: '{part.name}' runs past its row's end");
                    Assert.GreaterOrEqual(part.layout.xMin, -0.5f, $"{language} {width} {uiScale}%: '{part.name}' runs past its row's start");
                }
            }
            Assert.LessOrEqual(list.contentContainer.layout.width, room + 0.5f, $"{language} {width} {uiScale}%: the rows scroll sideways");
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
            // button that names Hebrew in Hebrew (#1036), and the start screen's (#2054).
            var texts = root.Query<TextElement>().ToList().Where(e => !e.ClassListContains("start-he") && e.name != "settings-language-he" && e.name != "start-language-he").ToList();
            Assert.Greater(texts.Count, 30, "Game.uxml's labels and buttons");
            for (int i = 0; i < 3; i++) yield return null;             // resolved styles need a style pass
            foreach (var e in texts)
                Assert.AreNotSame(hebrew, FontOf(e), $"English: '{e.name}' ({string.Join(" ", e.GetClasses())})");
            Assert.AreSame(hebrew, FontOf(root.Q<TextElement>("settings-language-he")), "English: 'עברית' has the Hebrew face");
            Assert.AreSame(hebrew, FontOf(root.Q<TextElement>("start-language-he")), "English: the start screen's 'עברית' has the Hebrew face");
            Assert.AreEqual("Advanced", root.Q<TextElement>("start-language-he").resolvedStyle.unityTextGenerator.ToString(),
                "English: the start screen's 'עברית' is laid out right to left");
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

    // --- A language from translations/ (#1606) --------------------------------------------------------------------------

    /// <summary>Romanian (translations/ro, from #1604) is not offered yet (owner, 3 Nov 2026: "Hide until more is
    /// translated"): no button for it on the start screen or in Settings, and a stored "ro" reads as English at the next
    /// start, since only a language the buttons offer is kept. A flag still wins.</summary>
    [Test]
    public void RomanianIsNotOfferedYet()
    {
        CollectionAssert.AreEqual(new[] { UiLanguage.English, UiLanguage.Hebrew }, LanguageButtons.All.Select(l => l.language).ToArray(),
            "English and Hebrew only");
        const string page = "http://localhost:8000/index.html";
        var none = new string[0];
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart("ro", page, none, webgl: true), "a stored ro is not offered");
        Assert.AreEqual(UiLanguage.English, UiLanguage.AtStart("ro", null, none, webgl: false));
        Assert.AreEqual(UiLanguage.Hebrew, UiLanguage.AtStart("ro", page + "?lang=he", none, webgl: true), "a flag wins");

        var settings = new SettingsStore(Path.Combine(saveDir, "settings"));
        BootWithStartScreen(settings, null);
        Assert.IsTrue(StartOpen, "precondition: the start screen shows");
        Assert.IsNull(Ui.Q<Button>("start-language-ro"), "no Romanian on the start screen");
        Assert.IsNull(Ui.Q<Button>("settings-language-ro"), "nor in Settings");
    }

    /// <summary>A new language's strings: each key its translations/&lt;lang&gt;/ui.yaml has reads the translation, every
    /// other key the English (the per-string fallback of #1606).</summary>
    [Test]
    public void ANewLanguageFallsBackToTheEnglishPerString()
    {
        var text = En.Over(new[] { new KeyValuePair<string, string>("ui.lang", "xx"), new KeyValuePair<string, string>("start.new_game", "[xx] start.new_game") }, "translations/xx/ui.yaml");
        Assert.AreEqual("xx", text["ui.lang"]);
        Assert.AreEqual("[xx] start.new_game", text["start.new_game"], "a key the translation has");
        Assert.AreEqual(En["hud.turn"], text["hud.turn"], "a key it lacks: the English");
        Assert.AreEqual("en", En["ui.lang"], "the English strings are left as they were");
    }

    /// <summary>The way in for a language once it is offered: GameRoot builds the UI from its translations/&lt;lang&gt;/ui.yaml
    /// (Content.Translations, #2124) over the English, left to right in the Latin face. A key the translation adds (a marker
    /// here, not Romanian) shows; the rest is English.</summary>
    [Test]
    public void ATranslatedLanguagesStringsFallBackToEnglish()
    {
        var translations = Inputs.Content.Translations;
        var i = translations.FindIndex(t => t.Lang == "ro");
        Assert.GreaterOrEqual(i, 0, "precondition: translations/ro loads with the content");
        var real = translations[i];
        const string marker = "[ro] start.new_game";
        translations[i] = new Statecraft.Sim.TranslationSet
        {
            Lang = real.Lang, Events = real.Events, Content = real.Content,
            Ui = real.Ui.Append(new KeyValuePair<string, string>("start.new_game", marker)).ToList(),
        };
        try
        {
            var game = BootWithStartScreen(new SettingsStore(Path.Combine(saveDir, "settings")), "ro");
            Assert.AreEqual(marker, Ui.Q<Button>("start-new-game").text, "a key translations/ro has");
            Assert.AreEqual(En.Format("hud.turn", game.Bridge!.State.Turn), Ui.Q<Label>("turn").text, "a key it lacks: the English");
            Assert.IsFalse(Ui.Q("root").ClassListContains(UiLanguage.HebrewClass), "left to right, the Latin face");
        }
        finally { translations[i] = real; }
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
