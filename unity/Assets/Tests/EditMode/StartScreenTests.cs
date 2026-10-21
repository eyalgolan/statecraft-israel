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
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The start screen (StartScreenPresenter): the quoted lines are word for word the official texts, Continue only
/// with a save, New Game only through its two presses, Escape, automated runs skipping it, its layout at the four window
/// sizes it must fit, and the Hebrew laid out right to left.</summary>
public class StartScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static string Source(string name) => File.ReadAllText(Path.Combine(Paths.RepoRoot, "docs", "sources", "declaration-1948", name));

    readonly List<string> temp = new List<string>();
    SaveStore TempStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-start-" + Guid.NewGuid().ToString("N"));
        temp.Add(dir);
        return new SaveStore(dir);
    }

    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // after the scene tests
        foreach (var d in temp) { try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch (Exception) { } }
        temp.Clear();
    }

    // --- The text ---------------------------------------------------------------------------------------------------

    /// <summary>The lines exactly as Game.uxml shows them, checked twice: against these literals (a change shows in review)
    /// and as verbatim passages of the sources in docs/sources/declaration-1948 (the Hebrew of Iton Rishmi No. 1, the
    /// English translation). A leading ellipsis marks a sentence quoted from its middle.</summary>
    static readonly (string name, string text, string source)[] Quoted =
    {
        ("start-he-proclamation", "…אנו מכריזים בזאת על הקמת מדינה יהודית בארץ־ישראל, היא מדינת ישראל.", "he.txt"),
        ("start-en-proclamation", "…HEREBY DECLARE THE ESTABLISHMENT OF A JEWISH STATE IN ERETZ-ISRAEL, TO BE KNOWN AS THE STATE OF ISRAEL.", "en.txt"),
        ("start-he-date", "בעיר תל־אביב, היום הזה, ערב שבת, ה׳ אייר תש״ח, 14 במאי 1948.", "he.txt"),
        ("start-en-date", "IN THE CITY OF TEL-AVIV, ON THIS SABBATH EVE, THE 5TH DAY OF IYAR, 5708 (14TH MAY, 1948).", "en.txt"),
        // The recording's captions (DeclarationRecording): the sentence Ben-Gurion is heard reading.
        ("start-caption-he", "…אנו מכריזים בזאת על הקמת מדינה יהודית בארץ־ישראל, היא מדינת ישראל.", "he.txt"),
        ("start-caption-en", "…HEREBY DECLARE THE ESTABLISHMENT OF A JEWISH STATE IN ERETZ-ISRAEL, TO BE KNOWN AS THE STATE OF ISRAEL.", "en.txt"),
    };

    [Test]
    public void TheQuotedLinesAreWordForWordTheOfficialTexts()
    {
        using var ui = new UiTestKit();
        foreach (var (name, text, source) in Quoted)
        {
            var shown = ui.Q<Label>(name).text;
            Assert.AreEqual(text, shown, name);
            var passage = shown.StartsWith("…", StringComparison.Ordinal) ? shown.Substring(1) : shown;
            StringAssert.Contains(passage, Source(source), $"{name} is not a verbatim passage of docs/sources/declaration-1948/{source}");
            Assert.IsFalse(Regex.IsMatch(shown, "[‎‏‪-‮⁦-⁩]"), $"{name}: no direction marks; the text generator orders it");
        }
        // The Hebrew punctuation is the source's own: maqaf (U+05BE), geresh (U+05F3) and gershayim (U+05F4), not a hyphen
        // or quotation marks that would look alike.
        var date = ui.Q<Label>("start-he-date").text;
        StringAssert.Contains("תל־אביב", date);
        StringAssert.Contains("ה׳ אייר תש״ח", date);
        StringAssert.Contains("בארץ־ישראל", ui.Q<Label>("start-he-proclamation").text);
        StringAssert.Contains("Official Gazette No. 1, 14 May 1948", ui.Q<Label>("start-source").text);
        StringAssert.Contains("AI-generated", ui.Q<Label>("start-source").text);
    }

    [UnityTest]
    public IEnumerator TheHebrewIsSetInFrankRuhlLibreThroughTheAdvancedTextGenerator()
    {
        using var ui = new RenderedUi();
        ui.Root.Q("root").AddToClassList(StartScreenPresenter.OpenClass);
        for (int i = 0; i < 3; i++) yield return null;                 // resolved styles need a style pass
        var font = AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew);
        Assert.IsNotNull(font, UiPaths.FontHebrew);
        foreach (var name in new[] { "start-he-proclamation", "start-he-date" })
        {
            var label = ui.Q<Label>(name);
            Assert.AreEqual("Advanced", label.resolvedStyle.unityTextGenerator.ToString(), name);
            Assert.AreEqual(LanguageDirection.RTL, label.languageDirection, $"{name}: a right-to-left paragraph (its full stop goes on the left)");
            var fontAsset = label.resolvedStyle.unityFontDefinition.fontAsset;
            var resolved = fontAsset != null ? fontAsset.sourceFontFile : label.resolvedStyle.unityFontDefinition.font;
            Assert.AreSame(font, resolved, name);
            foreach (var c in label.text.Where(c => c > 0x7f))
                Assert.IsTrue(font.HasCharacter(c), $"{name}: Frank Ruhl Libre has no glyph for U+{(int)c:X4}");
        }
    }

    /// <summary>The advanced text generator is on for the project (Project Settings, UI Toolkit, Enable Advanced Text) and
    /// the panel settings carry its ICU data, so a player build lays the Hebrew out right to left (UiAssets.Create).</summary>
    [Test]
    public void AdvancedTextIsOnAndThePanelCarriesItsIcuData()
    {
        var settings = File.ReadAllText(Path.Combine(Paths.RepoRoot, "unity", "ProjectSettings", "UIToolkitProjectSettings.asset"));
        StringAssert.Contains("m_EnableAdvancedText: 1", settings);
        var icu = new SerializedObject(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)).FindProperty("m_ICUDataAsset");
        Assert.IsNotNull(icu);
        Assert.IsNotNull(icu.objectReferenceValue, "StatecraftPanel.asset has no ICU data (run UiAssets.Create)");
        StringAssert.Contains("m_ICUDataAsset: {fileID: 20204, guid: 0000000000000000f000000000000000, type: 0}",
            File.ReadAllText(Path.Combine(Paths.RepoRoot, "unity", UiPaths.Panel)), "saved in the asset, not only assigned on load");
    }

    // --- Automation ---------------------------------------------------------------------------------------------------

    [Test]
    public void AutomatedRunsSkipTheScreen()
    {
        var none = new string[0];
        foreach (var q in new[] { "?golden", "?golden&smoke=5", "?smoke", "?smoke=5", "?golden&shot", "?shot", "?vprofile", "?profile",
                     "?units", "?borderproof", "?nostart", "?bootlog&golden" })
            Assert.IsTrue(StartScreenPresenter.Skipped("http://localhost:8000/index.html" + q, none, webgl: true), q);
        foreach (var q in new[] { "", "?bootlog", "?navlog", "?camlog", "?smoke=0", "?goldenx", "?nostartx", "?shots" })
            Assert.IsFalse(StartScreenPresenter.Skipped("http://localhost:8000/index.html" + q, none, webgl: true), q);
        foreach (var args in new[] { new[] { "-golden" }, new[] { "-smoke", "5" }, new[] { "-smoke" }, new[] { "-shot" }, new[] { "-vprofile" },
                     new[] { "-profile" }, new[] { "-units" }, new[] { "-" + BorderProof.Flag }, new[] { "-nostart" } })
            Assert.IsTrue(StartScreenPresenter.Skipped(null, args, webgl: false), string.Join(" ", args));
        Assert.IsFalse(StartScreenPresenter.Skipped(null, new[] { "-batchmode" }, webgl: false));
        Assert.IsFalse(StartScreenPresenter.Skipped(null, new[] { "-smoke", "0" }, webgl: false));
        // A flag on the desktop does not count on the web and the other way round (each instrument's own rule).
        Assert.IsFalse(StartScreenPresenter.Skipped("http://localhost/index.html", new[] { "-golden" }, webgl: true));
        Assert.IsFalse(StartScreenPresenter.Skipped("http://localhost/index.html?golden", none, webgl: false));
    }

    // --- Behaviour ------------------------------------------------------------------------------------------------------

    /// <summary>Game.uxml on a panel that renders into a 1920x1080 texture, so layout, style and focus run as in a player
    /// once frames pass (UiTestKit's panel has no target and is not laid out in edit mode).</summary>
    sealed class RenderedUi : IDisposable
    {
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        public readonly UIDocument Doc;
        public VisualElement Root => Doc.rootVisualElement;

        public RenderedUi(int w = 1920, int h = 1080)
        {
            rt = new RenderTexture(w, h, 0);
            ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            go = new GameObject("StartScreenTest");
            go.SetActive(false);
            Doc = go.AddComponent<UIDocument>();
            Doc.panelSettings = ps;
            Doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
        }

        public T Q<T>(string name) where T : VisualElement
        {
            var e = Root.Q<T>(name);
            Assert.IsNotNull(e, name);
            return e;
        }

        public void Press(string name) => UiTestKit.Press(Q<Button>(name));

        public void Dispose() { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    sealed class NoAnswer : DeclarationRecording.IRequest { public ulong Bytes => 0; public void Abort() { } }

    /// <summary>The start the way GameRoot makes it: GameStart.Begin over the store, the toast, New Game and the screen.</summary>
    sealed class Rig : IDisposable
    {
        public readonly RenderedUi Ui = new RenderedUi();
        public readonly SimBridge Bridge;
        public readonly ToastPresenter Toast;
        public readonly NewGamePresenter NewGame;
        public readonly StartScreenPresenter Start;
        public readonly DeclarationRecording Recording;
        public readonly List<bool> Shown = new List<bool>();

        public Rig(SaveStore store)
        {
            var notes = new List<string>();
            Bridge = new SimBridge(GameStart.Begin(C, store, () => 4UL, notes, Text, out var resumed), Text);
            Toast = new ToastPresenter(Ui.Root, Bridge, Text);
            NewGame = new NewGamePresenter(Ui.Root, Bridge, store, Toast, Text, () => 5UL);
            GameStart.Autosave(Bridge, store, Text);
            Ui.Root.Q("root").RemoveFromClassList("loading");
            Recording = new DeclarationRecording(Ui.Root, Text, Toast.Show, (url, done) => new NoAnswer());   // a fetch that never answers
            Start = new StartScreenPresenter(Ui.Root, NewGame, resumed, Shown.Add, Recording);
            Start.Show();
        }

        public bool ContinueShown => Ui.Q<Button>("start-continue").style.display.value == DisplayStyle.Flex;
        public void Dispose() { Start.Dispose(); Recording.Dispose(); NewGame.Dispose(); Toast.Dispose(); Ui.Dispose(); }
    }

    static SaveStore WithSave(SaveStore store, int turns)
    {
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), Text);
        for (int i = 0; i < turns; i++) played.EndTurn();
        store.WriteAutosave(played.Session.Save.Write());
        return store;
    }

    /// <summary>Ben-Gurion's recording (docs/declaration-recording.md) plays only from its own button: never when the screen
    /// opens, never on Continue or New Game; the screen closing stops it.</summary>
    [Test]
    public void TheRecordingPlaysOnlyFromItsButtonAndStopsWhenTheScreenCloses()
    {
        var store = WithSave(TempStore(), 2);
        using (var rig = new Rig(store))
        {
            Assert.IsFalse(rig.Recording.Playing, "nothing plays when the screen opens");
            rig.Ui.Press("start-new-game");                                // arms the confirmation
            Assert.IsFalse(rig.Recording.Playing, "New Game does not play it");
            rig.Ui.Press("start-continue");                                // withdraws and closes
            Assert.IsFalse(rig.Recording.Playing, "Continue does not play it");
        }
        using (var rig = new Rig(WithSave(TempStore(), 2)))
        {
            rig.Ui.Press("start-listen");
            Assert.IsTrue(rig.Recording.Playing, "its button plays it");
            rig.Ui.Press("start-continue");
            Assert.IsFalse(rig.Start.IsOpen);
            Assert.IsFalse(rig.Recording.Playing, "closing the screen stops it");
        }
    }

    [Test]
    public void WithoutASaveThereIsNoContinueAndNewGameSimplyBegins()
    {
        var store = TempStore();
        using var rig = new Rig(store);
        Assert.IsTrue(rig.Start.IsOpen);
        Assert.IsFalse(rig.Start.Resumed);
        Assert.IsFalse(rig.ContinueShown, "no save: no Continue");
        var session = rig.Bridge.Session;
        rig.Ui.Press("start-continue");                                  // hidden, and ignored even if pressed
        Assert.IsTrue(rig.Start.IsOpen);
        rig.Ui.Press("start-new-game");
        Assert.IsFalse(rig.Start.IsOpen, "one press: there is nothing to lose");
        Assert.AreSame(session, rig.Bridge.Session, "the session already is a new game");
        Assert.AreEqual(new[] { true, false }, rig.Shown.ToArray(), "the camera keys are stopped, then given back");
        Assert.IsFalse(Directory.Exists(store.Directory) && Directory.GetFiles(store.Directory).Length > 0);
    }

    /// <summary>#1168: the start screen's "Star on GitHub" asks to open exactly the repository, once per press (mouse or
    /// keyboard), through the injectable opener; the screen stays open.</summary>
    [Test]
    public void TheStarButtonOpensTheRepositoryAndLeavesTheScreenOpen()
    {
        var opened = new List<string>();
        ExternalLink.Opener = opened.Add;
        try
        {
            using var rig = new Rig(TempStore());
            var star = rig.Ui.Q<Button>("start-star");
            Assert.AreEqual("Star on GitHub", star.text);
            SettingsScreenTests.PressWithPointer(star);
            CollectionAssert.AreEqual(new[] { "https://github.com/eyalgolan/statecraft-israel" }, opened, "a click");
            rig.Ui.Press("start-star");
            Assert.AreEqual(2, opened.Count, "Enter or Space");
            Assert.IsTrue(rig.Start.IsOpen, "the screen stays open");
        }
        finally { ExternalLink.Opener = ExternalLink.Default; }
    }

    /// <summary>#1169: the start screen's "Report a bug or idea" opens the report panel over the start screen, which stays
    /// open under it and is out of Tab's reach until the panel closes.</summary>
    [Test]
    public void TheReportButtonOpensThePanelOverTheScreen()
    {
        using var rig = new Rig(TempStore());
        using var feedback = new FeedbackPresenter(rig.Ui.Root, null, "0.1.0 / content ab12");
        Assert.AreEqual("Report a bug or idea", rig.Ui.Q<Button>("start-report").text);
        rig.Ui.Press("start-report");
        Assert.IsTrue(feedback.IsOpen);
        Assert.IsTrue(rig.Start.IsOpen, "the start screen stays under it");
        Assert.IsFalse(rig.Ui.Q<VisualElement>("start-screen").enabledSelf, "out of Tab's reach");
        rig.Ui.Press("feedback-close");
        Assert.IsTrue(rig.Ui.Q<VisualElement>("start-screen").enabledSelf);
        Assert.IsTrue(rig.Start.IsOpen);
    }

    [Test]
    public void ContinueShowsOnlyWithASaveAndLoadsIt()
    {
        var store = WithSave(TempStore(), 3);
        var saved = File.ReadAllText(store.AutosavePath);
        using var rig = new Rig(store);
        Assert.IsTrue(rig.Start.Resumed);
        Assert.IsTrue(rig.ContinueShown);
        Assert.AreEqual(3, rig.Bridge.State.Turn, "the session under the screen is the save, replayed");
        rig.Ui.Press("start-continue");
        Assert.IsFalse(rig.Start.IsOpen);
        Assert.AreEqual(3, rig.Bridge.State.Turn);
        Assert.AreEqual(saved, File.ReadAllText(store.AutosavePath), "Continue never touches the save");
        rig.Bridge.EndTurn();
        Assert.AreEqual(4, rig.Bridge.State.Turn, "and play goes on from it");
    }

    [Test]
    public void AnUnreadableSaveOffersNoContinue()
    {
        var store = TempStore();
        store.WriteAutosave("not a save\n");
        LogAssert.Expect(LogType.Warning, new Regex("Autosave rejected"));
        using var rig = new Rig(store);
        Assert.IsFalse(rig.Start.Resumed);
        Assert.IsFalse(rig.ContinueShown, "the save was set aside: there is nothing to continue");
    }

    [Test]
    public void NewGameOverASaveGoesThroughTheConfirmation()
    {
        var store = WithSave(TempStore(), 2);
        using var rig = new Rig(store);
        rig.Ui.Press("start-new-game");
        Assert.IsTrue(rig.Start.IsOpen, "the first press only asks");
        Assert.IsTrue(rig.NewGame.Armed);
        Assert.AreEqual(Text["newgame.confirm"], rig.Ui.Q<Label>("toast").text);
        Assert.AreEqual(DisplayStyle.Flex, rig.Ui.Q<Label>("toast").style.display.value);
        Assert.IsTrue(File.Exists(store.AutosavePath), "one press never wipes the save");
        Assert.AreEqual(2, rig.Bridge.State.Turn);
        rig.Ui.Press("start-new-game");
        Assert.IsFalse(rig.Start.IsOpen);
        Assert.AreEqual(0, rig.Bridge.State.Turn, "a new game");
        Assert.IsFalse(File.Exists(store.AutosavePath), "the abandoned save is gone");
        Assert.IsFalse(rig.Start.Resumed);
    }

    [Test]
    public void EscapeAndContinueWithdrawTheRequest()
    {
        var store = WithSave(TempStore(), 2);
        using var rig = new Rig(store);
        rig.Ui.Press("start-new-game");
        Assert.IsTrue(rig.NewGame.Armed);
        using (var e = NavigationCancelEvent.GetPooled()) { e.target = rig.Ui.Q<Button>("start-new-game"); rig.Ui.Q<Button>("start-new-game").SendEvent(e); }
        Assert.IsFalse(rig.NewGame.Armed, "Escape withdraws it");
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Label>("toast").style.display.value, "and takes the prompt down");
        Assert.IsTrue(rig.Start.IsOpen, "Escape does not leave the screen");
        Assert.IsFalse(rig.Start.Escape(), "nothing left to withdraw");
        rig.Ui.Press("start-new-game");
        Assert.IsTrue(rig.NewGame.Armed);
        rig.Ui.Press("start-continue");
        Assert.IsFalse(rig.Start.IsOpen);
        Assert.IsFalse(rig.NewGame.Armed, "Continue withdraws it: the top bar's New Game starts again from one press");
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Q<Label>("toast").style.display.value);
        rig.Ui.Press("new-game");
        Assert.AreEqual(2, rig.Bridge.State.Turn, "a single press on the top bar only asks");
        Assert.IsTrue(File.Exists(store.AutosavePath));
    }

    [UnityTest]
    public IEnumerator WhileItShowsTheHudIsHiddenAndOnlyItsButtonsTakeFocus()
    {
        var store = WithSave(TempStore(), 1);
        using var rig = new Rig(store);
        yield return null; yield return null;
        foreach (var hud in new[] { "top-bar", "right-column", "info-panel", "map-labels" })
            Assert.AreEqual(DisplayStyle.None, rig.Ui.Root.Q(hud).resolvedStyle.display, hud);
        foreach (var b in new[] { "end-turn", "new-game", "nav-cabinet", "build-camp" })
        {
            Assert.IsFalse(rig.Ui.Q<Button>(b).canGrabFocus, $"{b} is out of reach under the start screen");
            rig.Ui.Q<Button>(b).Focus();
            Assert.AreNotSame(rig.Ui.Q<Button>(b), rig.Ui.Root.focusController?.focusedElement, $"{b} cannot be focused");
        }
        var cont = rig.Ui.Q<Button>("start-continue");
        cont.Focus();
        Assert.AreSame(cont, rig.Ui.Root.focusController?.focusedElement, "Continue has the focus");
        // Tab walks the screen's buttons only: Continue, New Game, the recording's button, Star on GitHub (#1168) and Report a bug or idea (#1169).
        var seen = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var at = (VisualElement)rig.Ui.Root.focusController!.focusedElement;
            using (var e = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target = at; at.SendEvent(e); }
            yield return null;
            seen.Add((rig.Ui.Root.focusController.focusedElement as VisualElement)?.name ?? "none");
        }
        Assert.AreEqual("start-new-game", seen[0], "Tab moves from Continue to New Game: " + string.Join(", ", seen));
        Assert.IsTrue(seen.All(n => n == "start-continue" || n == "start-new-game" || n == "start-listen" || n == "start-star" || n == "start-report"), "Tab stays on the screen's buttons: " + string.Join(", ", seen));
        CollectionAssert.Contains(seen, "start-listen", "Tab reaches the recording's button: " + string.Join(", ", seen));
        rig.Ui.Press("start-continue");
        yield return null; yield return null;
        Assert.AreEqual(DisplayStyle.Flex, rig.Ui.Root.Q("top-bar").resolvedStyle.display);
        Assert.AreEqual(DisplayStyle.None, rig.Ui.Root.Q("start-screen").resolvedStyle.display);
        Assert.IsTrue(rig.Ui.Q<Button>("end-turn").canGrabFocus, "the HUD is back");
    }

    /// <summary>#41 M1: closed from the keyboard (New Game with no save, or Continue), the screen gives the focus to End Turn
    /// rather than to nothing, so the next Tab or Enter goes on from the top bar.</summary>
    [UnityTest]
    public IEnumerator ClosingItFromTheKeyboardFocusesEndTurn()
    {
        foreach (var save in new[] { false, true })
        {
            using var rig = new Rig(save ? WithSave(TempStore(), 1) : TempStore());
            yield return null; yield return null;
            var name = save ? "start-continue" : "start-new-game";
            var button = rig.Ui.Q<Button>(name);
            button.Focus();
            yield return null;
            Assert.AreSame(button, rig.Ui.Root.focusController?.focusedElement, name + ": precondition, focused");
            rig.Ui.Press(name);
            yield return null; yield return null; yield return null;
            Assert.IsFalse(rig.Start.IsOpen, name + ": closed");
            Assert.AreSame(rig.Ui.Q<Button>("end-turn"), rig.Ui.Root.focusController?.focusedElement, name + ": End Turn has the focus");
        }
    }

    /// <summary>#41 M1: closed with the mouse, the screen hands no focus on (PointerBlur's rule: a click leaves no focus
    /// fill behind), so End Turn is not lit.</summary>
    [UnityTest]
    public IEnumerator ClosingItWithThePointerFocusesNothing()
    {
        using var rig = new Rig(TempStore());
        yield return null; yield return null;
        PressWithPointer(rig.Ui.Q<Button>("start-new-game"));
        yield return null; yield return null; yield return null;
        Assert.IsFalse(rig.Start.IsOpen, "precondition: the click closed the screen");
        Assert.AreNotSame(rig.Ui.Q<Button>("end-turn"), rig.Ui.Root.focusController?.focusedElement, "End Turn is not lit by a mouse click");
    }

    /// <summary>A mouse press as the player makes it: pointer down (which also gives the button the focus), then the
    /// button's Clickable fires `clicked` on pointer up. UiTestKit.Press is the keyboard's Submit instead.</summary>
    static void PressWithPointer(Button b)
    {
        b.Focus();
        using (var down = PointerDownEvent.GetPooled()) { down.target = b; b.SendEvent(down); }
        using var click = ClickEvent.GetPooled();
        click.target = b;
        typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(EventBase) }, null)!.Invoke(b.clickable, new object[] { click });
    }

    /// <summary>#400: a turn-1 save keeps the build buttons disabled anyway, so the test above cannot see them. With a save
    /// from after August 1950, a selected hex and an event waiting, the build buttons and the event's options are live, and
    /// still none of them takes the focus under the start screen.
    /// Built in GameRoot's order: the HUD, ScreenNav, the info panel and the event card, then the screen.</summary>
    [UnityTest]
    public IEnumerator ASaveFrom1950KeepsTheLiveHudOutOfReach()
    {
        var g = GameSession.New(C, 9, Mode.Historical);
        while (g.State.Date < new DateTime(1950, 8, 1) || g.State.PendingEvents.Count == 0) g.Step(HistoricalAI.Commands(C, g.State));
        var store = TempStore();
        store.WriteAutosave(g.Save.Write());
        using var ui = new RenderedUi();
        var bridge = new SimBridge(GameStart.Begin(C, store, () => 4UL, new List<string>(), Text, out var resumed), Text);
        var owned = new List<IDisposable>();
        try
        {
            ui.Root.Q("root").RemoveFromClassList("loading");
            owned.Add(new HudPresenter(ui.Root, bridge, Text));
            owned.Add(new ScreenNav(ui.Root));
            var info = new InfoPanelPresenter(ui.Root, bridge, Text, C.SettlementHexes.ToDictionary(x => x.Id, x => x.Id)); owned.Add(info);
            var card = new EventCardPresenter(ui.Root, bridge, Text, null); owned.Add(card);
            var toast = new ToastPresenter(ui.Root, bridge, Text); owned.Add(toast);
            var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 5UL); owned.Add(newGame);
            var tlv = C.SettlementHexes.Single(x => x.Id == "tel_aviv");
            info.Show(tlv.Q, tlv.R);
            var start = new StartScreenPresenter(ui.Root, newGame, resumed); owned.Add(start);
            start.Show();
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(resumed, "precondition: the 1950 save is continued");
            var build = ui.Q<Button>("build-camp");
            Assert.IsTrue(build.enabledSelf && build.style.display.value == DisplayStyle.Flex, "precondition: build-camp is live: " + ui.Q<Label>("info-reason").text);
            Assert.IsNotNull(ui.Root.Q("event-options").Q<Button>(), "precondition: an event's options are shown");
            void NoneReachable(string where)
            {
                foreach (var region in new[] { "top-bar", "right-column", "info-panel", "event-layer" })
                    foreach (var b in ui.Root.Q(region).Query<Button>().ToList())
                    {
                        Assert.IsFalse(b.canGrabFocus, $"{where}: {region} {b.name} cannot take the focus under the start screen");
                        b.Focus();
                        Assert.AreNotSame(b, ui.Root.focusController?.focusedElement, $"{where}: {region} {b.name} is not focused");
                    }
            }
            NoneReachable("under the start screen");
            ui.Press("start-continue");
            yield return null; yield return null;
            // The waiting event's card opens in the middle (decision cards) and takes the focus (review S1); its options are
            // in reach, the panels under its backdrop are not; Hide gives them back.
            yield return null;
            Assert.AreSame(ui.Root.Q("event-card"), ui.Root.panel.focusController.focusedElement, "the event card has the focus once the screen closes");
            Assert.IsTrue(ui.Root.Q("event-options").Q<Button>().canGrabFocus, "the event's options are in reach once the screen closes");
            Assert.IsFalse(build.canGrabFocus, "the info panel waits under the decision card's backdrop");
            card.Collapse();
            yield return null;
            Assert.IsTrue(build.canGrabFocus, "the build button is back once the card is hidden");
        }
        finally { for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
    }

    // --- Layout ------------------------------------------------------------------------------------------------------

    /// <summary>The painting is a native render of at least 1920x1080 at 16:9 (StartScreenPresenter.ArtAspect), imported at
    /// its own size, so a 1080p window never shows an upscale (#268).</summary>
    [Test]
    public void ThePaintingIsNativeAtLeast1080p()
    {
        const string path = "Assets/UI/StartScreen/declaration-hall.jpg";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out var w, out var h);
        Assert.GreaterOrEqual(w, 1920, "source width");
        Assert.GreaterOrEqual(h, 1080, "source height");
        Assert.AreEqual(StartScreenPresenter.ArtAspect, (float)w / h, 0.002f, "16:9");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Assert.AreEqual((w, h), (tex.width, tex.height), "imported at its own size (maxTextureSize does not shrink it)");
    }

    /// <summary>At each window size the owner named (a 1080p and a 768p screen, a small 16:10 window, a phone held upright):
    /// the screen covers the window; the painting keeps its 16:9 (a full-width band below 16:9, so nothing is cropped) and
    /// starts at the window's top, anchored at its top centre so a crop takes both sides alike; the panel, its lines and
    /// both buttons are inside the window and the panel; no line overlaps another or is squeezed below its font size; the
    /// panel leaves the painting's upper half (the flags and the table) clear.</summary>
    [UnityTest]
    public IEnumerator ItFitsEveryWindowSize()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (1366, 768), (960, 600), (390, 844) })
        {
            var rt = new RenderTexture(w, h, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            ps.targetTexture = rt;
            var go = new GameObject("StartLayout");
            go.SetActive(false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            var store = WithSave(TempStore(), 1);
            var bridge = new SimBridge(GameStart.Begin(C, store, () => 4UL, new List<string>(), Text, out var resumed), Text);
            var toast = new ToastPresenter(root, bridge, Text);
            var newGame = new NewGamePresenter(root, bridge, store, toast, Text, () => 5UL);
            var start = new StartScreenPresenter(root, newGame, resumed);
            try
            {
                root.Q("root").RemoveFromClassList("loading");
                start.Show();
                for (int i = 0; i < 20 && (float.IsNaN(root.Q("start-panel").worldBound.width) || root.Q("start-panel").worldBound.width <= 0); i++) yield return null;
                for (int i = 0; i < 4; i++) yield return null;      // the band height is set from the first layout
                var window = root.panel.visualTree.worldBound;
                Rect R(string name) => root.Q(name).worldBound;
                string at = $"{w}x{h} ({window.width:0}x{window.height:0} logical)";
                bool Inside(Rect inner, Rect outer) => inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

                Assert.IsTrue(Inside(window, R("start-screen")), $"{at}: the screen {R("start-screen")} covers the window");
                var art = R("start-art");
                var panel = R("start-panel");
                Assert.AreEqual(window.width, art.width, 0.5f, $"{at}: the painting is full width");
                Assert.IsTrue(Inside(panel, window), $"{at}: the panel {panel} is inside the window");
                var tall = window.width / window.height < StartScreenPresenter.ArtAspect;
                if (root.Q("start-screen").ClassListContains(StartScreenPresenter.BandClass))
                {
                    Assert.IsTrue(tall, at);
                    Assert.AreEqual(art.width / StartScreenPresenter.ArtAspect, art.height, 2f, $"{at}: the band keeps 16:9 (to pixel snapping): nothing is cropped");
                    Assert.IsTrue(Inside(art, window), $"{at}: the band {art} is inside the window");
                    Assert.GreaterOrEqual(panel.yMin, art.yMax - 0.5f, $"{at}: the panel {panel} is under the band {art}");
                    Assert.AreEqual(art.yMin - window.yMin, window.yMax - panel.yMax, 20f, $"{at}: the band and the panel are centred together");
                }
                else
                {
                    Assert.AreEqual(window.yMin, art.yMin, 0.5f, $"{at}: the painting starts at the top");
                    Assert.AreEqual(window.height, art.height, 0.5f, $"{at}: the painting fills the window");
                    Assert.AreEqual(BackgroundPositionKeyword.Center, root.Q("start-art").resolvedStyle.backgroundPositionX.keyword, $"{at}: a crop takes both sides alike (the aisle stays in the middle)");
                    Assert.AreEqual(BackgroundPositionKeyword.Top, root.Q("start-art").resolvedStyle.backgroundPositionY.keyword, $"{at}: a crop keeps the top (the flags and the table)");
                    // Cover, anchored at the top: the painting's upper half (the flags and the table) stays clear of the panel.
                    var painted = Mathf.Max(window.height, window.width / StartScreenPresenter.ArtAspect);
                    Assert.GreaterOrEqual(panel.yMin, window.yMin + painted * 0.5f - 0.5f, $"{at}: the panel {panel} covers the painting's upper half");
                }
                if (h > w) Assert.IsTrue(root.Q("start-screen").ClassListContains(StartScreenPresenter.BandClass), $"{at}: a phone held upright stacks the painting over the panel");
                var lines = new[] { "start-title", "start-he-proclamation", "start-en-proclamation", "start-he-date", "start-en-date", "start-source", "start-actions", "start-community" };
                foreach (var name in lines)
                {
                    var r = R(name);
                    Assert.IsTrue(Inside(r, panel), $"{at}: {name} {r} leaves the panel {panel}");
                    if (root.Q(name) is Label l) Assert.GreaterOrEqual(r.height + 0.5f, l.resolvedStyle.fontSize, $"{at}: {name} collapsed");
                }
                for (int i = 1; i < lines.Length; i++)
                    Assert.LessOrEqual(R(lines[i - 1]).yMax, R(lines[i]).yMin + 0.5f, $"{at}: {lines[i - 1]} overlaps {lines[i]}");
                foreach (var b in new[] { "start-continue", "start-new-game" })
                {
                    var r = R(b);
                    Assert.AreEqual(DisplayStyle.Flex, root.Q(b).resolvedStyle.display, $"{at}: {b} shows");
                    Assert.IsTrue(Inside(r, R("start-actions")) && Inside(r, panel), $"{at}: {b} {r} is inside the panel");
                    Assert.GreaterOrEqual(r.height, 40f, $"{at}: {b} is a usable target");
                }
                Assert.IsFalse(R("start-continue").Overlaps(R("start-new-game")), at);
                // #1168 review: Star on GitHub is New game's size, beside it or (in a narrow window) wrapped under it, and
                // secondary in colour: no fill and no gold edge.
                var star = R("start-star");
                Assert.IsTrue(Inside(star, R("start-actions")) && Inside(star, panel), $"{at}: the Star button {star} is with New game, inside the panel");
                var onePixel = Mathf.Max(0.5f, window.width / w + 0.01f);   // a scaled panel snaps each edge to a screen pixel
                Assert.AreEqual(R("start-new-game").height, star.height, onePixel, $"{at}: the Star button is New game's height");
                Assert.AreEqual(R("start-new-game").width, star.width, onePixel, $"{at}: the Star button is New game's width");
                Assert.IsFalse(star.Overlaps(R("start-new-game")) || star.Overlaps(R("start-continue")), $"{at}: the Star button overlaps a button");
                var starStyle = root.Q("start-star").resolvedStyle; var newGameStyle = root.Q("start-new-game").resolvedStyle;
                Assert.AreEqual(newGameStyle.fontSize, starStyle.fontSize, $"{at}: the Star button's text is New game's size");
                Assert.Less(starStyle.backgroundColor.a, 0.01f, $"{at}: the Star button has no fill (secondary to New game)");
                Assert.AreNotEqual(newGameStyle.borderTopColor, starStyle.borderTopColor, $"{at}: the Star button's edge is not New game's gold");
                Assert.IsTrue(Inside(R("start-report"), R("start-community")), $"{at}: the Report button {R("start-report")} is in its row");   // #1169
                Assert.IsFalse(R("start-report").Overlaps(star), at);
            }
            finally
            {
                start.Dispose(); newGame.Dispose(); toast.Dispose();
                Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }

    /// <summary>tools/web_check.py clicks the screen's buttons where ?navlog says they are (--startscreen, --persistence):
    /// the NAV line names every shown button (Continue only with a save; the recording's button once its row shows), each
    /// at its own centre to half a pixel as a fraction of the window; the last line logged is the current layout; and
    /// without ?navlog nothing is logged.</summary>
    [UnityTest]
    public IEnumerator UnderNavlogItLogsWhereEachShownButtonIs()
    {
        var wasNav = DiagLog.Nav;
        var logged = new List<string>();
        void Capture(string message, string trace, LogType type) { if (message.Contains(" start buttons ")) logged.Add(message); }
        Application.logMessageReceived += Capture;
        try
        {
            foreach (var save in new[] { false, true })
            {
                DiagLog.Nav = true; logged.Clear();
                using var rig = new Rig(save ? WithSave(TempStore(), 1) : TempStore());
                for (int i = 0; i < 6; i++) yield return null;
                var line = rig.Start.ButtonCentres();
                Assert.IsNotNull(line, $"save={save}: the screen is laid out");
                Assert.IsNotEmpty(logged, $"save={save}: a NAV line is logged");
                StringAssert.IsMatch(@"^NAV f=\d+ t=\d+ start buttons ", logged[^1]);
                Assert.AreEqual(line, logged[^1].Substring(logged[^1].IndexOf("start buttons ", StringComparison.Ordinal)), "the last line is the current layout");
                var window = rig.Ui.Root.panel.visualTree.worldBound;
                var centres = Regex.Matches(line!, @"(\S+)=([0-9.]+),([0-9.]+)").Cast<Match>().ToDictionary(m => m.Groups[1].Value,
                    m => new Vector2(window.xMin + window.width * float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
                                     window.yMin + window.height * float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture)));
                var shown = save ? StartScreenPresenter.Buttons : StartScreenPresenter.Buttons.Where(b => b != "start-continue").ToArray();
                CollectionAssert.AreEqual(shown, centres.Keys, $"save={save}: {line}");
                foreach (var (name, at) in centres.Select(kv => (kv.Key, kv.Value)))
                {
                    var r = rig.Ui.Q<Button>(name).worldBound;
                    Assert.AreEqual(r.center.x, at.x, 0.5f, $"save={save}: {name} x ({r})");
                    Assert.AreEqual(r.center.y, at.y, 0.5f, $"save={save}: {name} y ({r})");
                    Assert.IsTrue(r.Contains(at) && window.Contains(at), $"save={save}: {name}'s centre {at} is on it and in the window");
                }
            }
            DiagLog.Nav = false; logged.Clear();
            using (var quiet = new Rig(TempStore()))
            {
                for (int i = 0; i < 6; i++) yield return null;
                Assert.IsNotNull(quiet.Start.ButtonCentres(), "precondition: laid out");
                Assert.IsEmpty(logged, "without ?navlog nothing is logged");
            }
        }
        finally { Application.logMessageReceived -= Capture; DiagLog.Nav = wasNav; }
    }

    // --- The scene ---------------------------------------------------------------------------------------------------

    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    GameRoot OpenScene(SaveStore store)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store;
        return root;
    }

    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static bool StartOpen => Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass);

    /// <summary>Map.unity as a player boots it: the start screen over the continued save, the camera keys off until it closes.</summary>
    [Test]
    public void ThePlayerBootShowsTheScreenOverTheContinuedSaveAndStopsTheCamera()
    {
        var store = WithSave(TempStore(), 2);
        var root = OpenScene(store);
        var cam = Object.FindFirstObjectByType<CameraController>();
        root.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(StartOpen);
        Assert.IsFalse(Ui.Q("root").ClassListContains("loading"));
        Assert.AreEqual(2, root.Bridge!.State.Turn, "the map's first turn waits behind the screen: the session is the save");
        Assert.IsFalse(cam.enabled, "the camera keys are off");
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        Assert.IsFalse(StartOpen);
        Assert.IsTrue(cam.enabled);
        root.enabled = false;                                             // disposing with the screen open gives the camera back too
        root.enabled = true;
        root.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(StartOpen && !cam.enabled);
        root.enabled = false;
        Assert.IsTrue(cam.enabled);
        Assert.IsFalse(StartOpen);
    }

    /// <summary>Automation never meets the screen: a smoke run (no New Game, so no screen even when asked), the tests'
    /// and tools' Begin(inputs, n), and the Ready path in the editor (not playing).</summary>
    [Test]
    public void AutomatedBootsNeverShowTheScreen()
    {
        var root = OpenScene(TempStore());
        LogAssert.Expect(LogType.Warning, "SMOKE FAIL the smoke run needs play mode or a player");
        root.Begin(Inputs, 5, startScreen: true);
        Assert.IsFalse(StartOpen, "smoke");
        root.Begin(Inputs, 0);
        Assert.IsFalse(StartOpen, "Begin(inputs, n)");
        root.Begin(Inputs);
        Assert.IsFalse(StartOpen, "GameBoot.Ready outside play mode");
        Assert.IsTrue(Object.FindFirstObjectByType<CameraController>().enabled);
    }

    // --- Right to left -----------------------------------------------------------------------------------------------

    /// <summary>The Hebrew is laid out right to left (#193), rendered through the start screen's own style and direction
    /// (Frank Ruhl Libre, the advanced generator, RTL). "שי" is shin then yod in logical order: right to left, the big shin
    /// is drawn on the right and the small yod on its left, so the ink leans right; drawn left to right (a reversed line,
    /// what the standard generator does: the control) it leans left. "ש." checks the paragraph's direction: a right-to-left
    /// paragraph puts the full stop on the left of the shin (ink leans right), a left-to-right one (the second control) on its
    /// right.</summary>
    [UnityTest]
    public IEnumerator HebrewIsLaidOutRightToLeft()
    {
        const int W = 400, H = 320;
        var rt = new RenderTexture(W, H, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        ps.scaleMode = PanelScaleMode.ConstantPixelSize; ps.scale = 1f;
        ps.clearColor = true; ps.colorClearValue = Color.black;
        var go = new GameObject("RtlTest");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        go.SetActive(true);
        doc.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Statecraft.uss"));
        void Make(int row, bool advanced, string text, LanguageDirection direction = LanguageDirection.RTL)
        {
            var l = new Label(text);
            if (advanced) { l.AddToClassList("start-he"); l.languageDirection = direction; }
            else l.style.unityFontDefinition = new StyleFontDefinition(AssetDatabase.LoadAssetAtPath<Font>(UiPaths.FontHebrew));
            l.style.position = Position.Absolute; l.style.left = 20; l.style.top = row * 80; l.style.width = 360; l.style.height = 80;
            l.style.fontSize = 60; l.style.color = Color.white; l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
            l.style.whiteSpace = WhiteSpace.NoWrap;
            doc.rootVisualElement.Add(l);
        }
        Make(0, advanced: true, "\u05E9\u05D9");                        // שי
        Make(1, advanced: false, "\u05E9\u05D9");
        Make(2, advanced: true, "\u05E9.");                             // ש.
        Make(3, advanced: true, "\u05E9.", LanguageDirection.LTR);      // the control: a left-to-right paragraph
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        try
        {
            // The ink's centre of mass relative to the middle of its extent, as a share of the extent: > 0 leans right.
            float Lean(int row)
            {
                var cols = new float[W];
                for (int y = H - 80 * (row + 1); y < H - 80 * row; y++)          // ReadPixels row 0 is the bottom
                    for (int x = 0; x < W; x++) cols[x] += tex.GetPixel(x, y).grayscale;
                float total = cols.Sum();
                if (total < 20f) return float.NaN;
                int first = Array.FindIndex(cols, c => c > 0.5f), last = Array.FindLastIndex(cols, c => c > 0.5f);
                float centre = 0; for (int x = 0; x < W; x++) centre += x * cols[x];
                centre /= total;
                return (centre - (first + last) / 2f) / Mathf.Max(1, last - first);
            }
            float rtl = float.NaN, ltr = float.NaN, stop = float.NaN, ltrStop = float.NaN;
            for (int frame = 0; frame < 30 && (float.IsNaN(rtl) || float.IsNaN(ltr) || float.IsNaN(stop) || float.IsNaN(ltrStop)); frame++)
            {
                yield return null;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                RenderTexture.active = null;
                rtl = Lean(0); ltr = Lean(1); stop = Lean(2); ltrStop = Lean(3);
            }
            Assert.IsFalse(float.IsNaN(rtl) || float.IsNaN(ltr) || float.IsNaN(stop) || float.IsNaN(ltrStop), "the labels were not drawn (no ink in the render)");
            Assert.AreEqual("Advanced", doc.rootVisualElement[0].resolvedStyle.unityTextGenerator.ToString());
            Assert.AreEqual("Standard", doc.rootVisualElement[1].resolvedStyle.unityTextGenerator.ToString());
            Assert.Less(ltr, -0.05f, $"control: drawn left to right, the shin comes first and the ink leans left ({ltr:0.000})");
            Assert.Greater(rtl, 0.05f, $"the start screen's Hebrew is drawn right to left: the ink leans right ({rtl:0.000})");
            Assert.Less(ltrStop, -0.05f, $"control: a left-to-right paragraph puts the full stop on the right ({ltrStop:0.000})");
            Assert.Greater(stop, 0.05f, $"a right-to-left paragraph: the full stop is on the left ({stop:0.000})");
        }
        finally { Object.DestroyImmediate(tex); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
