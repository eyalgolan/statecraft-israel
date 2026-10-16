#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.TestTools;

public class SaveUiTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static readonly List<string> TempFolders = new List<string>();
    static SaveStore TempStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-saves-" + System.Guid.NewGuid().ToString("N"));
        TempFolders.Add(dir);
        return new SaveStore(dir);
    }

    [TearDown]
    public void RemoveTempFolders()
    {
        foreach (var d in TempFolders) { try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch (System.Exception) { } }
        TempFolders.Clear();
    }

    /// <summary>A store whose reads and set-asides fail the way a broken disk does, deterministically.</summary>
    sealed class FaultyStore : SaveStore
    {
        readonly string? text;
        public bool SetAsideCalled;
        public FaultyStore(string? text) : base(Path.Combine(Path.GetTempPath(), "statecraft-faulty")) { this.text = text; }
        public override string? ReadAutosave() => text ?? throw new IOException("disk gone");
        public override void SetAsideAutosave() { SetAsideCalled = true; throw new System.UnauthorizedAccessException("no permission"); }
    }

    [Test]
    public void EveryTurnIsAutosavedAndTheNextStartContinuesIt()
    {
        var store = TempStore();
        var notes = new List<string>();
        var bridge = new SimBridge(GameStart.Begin(C, store, () => 77UL, notes, Text), Text);
        GameStart.Autosave(bridge, store, Text);
        bridge.Queue(new SetMobilization(1.0));
        bridge.EndTurn(); bridge.EndTurn();
        Assert.IsTrue(File.Exists(store.AutosavePath));
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        var resumed = GameStart.Begin(C, store, () => 1UL, notes, Text);
        Assert.IsEmpty(notes);
        Assert.AreEqual(bridge.State.Hash(), resumed.State.Hash());
        Assert.AreEqual(2, resumed.State.Turn);
    }

    [Test]
    public void AnUnreadableAutosaveIsSetAsideAndANewGameStarts()
    {
        var store = TempStore();
        store.WriteAutosave("statecraft-save 9\ncontent x\nseed 1\nmode Historical\nend x\n");
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 5UL, notes, Text);
        Assert.AreEqual(0, session.State.Turn);
        Assert.AreEqual(Text["save.refused." + SaveException.NewerVersion], notes[0]);   // a newer schema: R-SAVE-002 (N2)
        Assert.IsFalse(File.Exists(store.AutosavePath));
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void ASaveWithAnUnsupportedModeIsSetAsideToo()
    {
        var store = TempStore();
        var good = GameSession.New(C, 9, Mode.Historical).Save.Write();
        store.WriteAutosave(good.Replace("mode Historical", "mode Open"));
        var notes = new List<string>();
        var session = GameStart.Begin(C, store, () => 6UL, notes, Text);
        Assert.AreEqual(0, session.State.Turn);
        Assert.AreEqual(new[] { Unreadable(Text) }, notes.ToArray());
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    /// <summary>The note the player reads for any autosave that cannot be continued: a short reason in their language.</summary>
    static string Unreadable(UiText text) => text.Format("save.unreadable", text["save.reason_other"]);

    /// <summary>#631: every way a save is refused (a damaged line, another ruleset, a replay that ends elsewhere, a replay that
    /// throws) shows the player the same short reason in English and in Hebrew, never the sim's English message; the full
    /// exception, with the replay's own exception inside it, goes to the log.</summary>
    [TestCase("strings.en.yaml", "damaged line")]
    [TestCase("strings.he.yaml", "damaged line")]
    [TestCase("strings.en.yaml", "other ruleset")]
    [TestCase("strings.he.yaml", "other ruleset")]
    [TestCase("strings.en.yaml", "replay differs")]
    [TestCase("strings.he.yaml", "replay differs")]
    [TestCase("strings.en.yaml", "replay throws")]
    [TestCase("strings.he.yaml", "replay throws")]
    public void AnUnreadableSaveShowsOnlyALocalisedReason(string file, string kind)
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Folder + "/" + file).text, file);
        var content = C;
        var played = GameSession.New(C, 9, Mode.Historical);
        played.Step(new Command[] { new SetMobilization(1.0) });
        var save = played.Save.Write();
        string logged;
        var expected = Unreadable(text);
        switch (kind)
        {
            case "damaged line":
                save = save.Replace("seed 9", "seed nine");
                logged = "line 3: bad seed";
                break;
            case "other ruleset":
                var other = C.Ruleset == SaveGame.LegacyRuleset ? SaveGame.GddRuleset : SaveGame.LegacyRuleset;
                save = save.Replace("ruleset " + C.Ruleset, "ruleset " + other);
                logged = "version of the game's rules";
                // A coded refusal since the cutover (R-SAVE-001): a legacy save in the GDD game says so in its own string (N4).
                expected = text["save.refused." + SaveException.OlderRules];
                break;
            case "replay differs":
                save = save.Replace("seed 9", "seed 10");   // the same content, so a replay that ends elsewhere is refused
                logged = "The replay does not match the saved game";
                break;
            default:
                content = ContentLoader.Load(Paths.Content);   // its own copy: removing a price must not touch C
                content.Rules.AttentionCost.Remove("set_mobilization");
                foreach (var r in content.EraRules.Values) r.AttentionCost.Remove("set_mobilization");
                logged = "turn 1: (.|\\n)*KeyNotFoundException";
                break;
        }
        var store = TempStore();
        store.WriteAutosave(save);
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Autosave rejected: (.|\\n)*" + logged));
        var session = GameStart.Begin(content, store, () => 6UL, notes, text, out var resumed);
        Assert.IsFalse(resumed);
        Assert.AreEqual(0, session.State.Turn);
        Assert.AreEqual(new[] { expected }, notes.ToArray());
        StringAssert.DoesNotContain("line", notes[0]);
        StringAssert.DoesNotContain("turn", notes[0]);
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    /// <summary>A store whose autosave is refused with a code the strings do not have (a future SaveException code).</summary>
    sealed class UnknownCodeStore : SaveStore
    {
        public UnknownCodeStore(string dir) : base(dir) { }
        public override string? ReadAutosave() => throw new SaveException(0, "made by a future version", "a_code_with_no_string");
    }

    /// <summary>#637: a load warning kind with no string (a kind added to the sim before its strings) is only logged. It
    /// must not throw inside Begin, which would set a loadable save aside as unreadable.</summary>
    [Test]
    public void ALoadWarningKindWithNoStringIsOnlyLogged()
    {
        var yaml = AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text;
        var key = "save.warning." + GameSession.ContentChanged + ":";
        StringAssert.Contains("\n" + key, yaml);
        var text = UiText.Parse(string.Join("\n", yaml.Split('\n').Where(l => !l.StartsWith(key))));
        Assert.IsFalse(text.Has("save.warning." + GameSession.ContentChanged));
        var store = TempStore();
        store.WriteAutosave(GameSession.New(C, 9, Mode.Historical).Save.Write().Replace("content " + C.Hash, "content " + new string('0', 64)));
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, "Autosave loaded with a warning: " + GameSession.ContentChanged);
        GameStart.Begin(C, store, () => 6UL, notes, text, out var resumed);
        Assert.IsTrue(resumed, "the save is continued, not set aside");
        Assert.IsEmpty(notes);
        Assert.IsTrue(File.Exists(store.AutosavePath));
        Assert.IsFalse(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    /// <summary>The start never blocks: a refusal code with no string falls back to the one short localised reason (#631),
    /// instead of throwing a missing-key error out of GameStart.Begin.</summary>
    [Test]
    public void ARefusalCodeWithNoStringFallsBackToTheShortReason()
    {
        var store = new UnknownCodeStore(TempStore().Directory);
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Autosave rejected"));
        GameSession? session = null;
        Assert.DoesNotThrow(() => session = GameStart.Begin(C, store, () => 6UL, notes, Text));
        Assert.AreEqual(0, session!.State.Turn);
        Assert.AreEqual(new[] { Unreadable(Text) }, notes.ToArray());
    }

    /// <summary>N4 (R-SAVE-001, R-SAVE-002): a save from before the GDD cutover, or from a newer version, is refused with the
    /// code's own string, in English and in Hebrew, and the player reads "a new game has started" once.</summary>
    [TestCase("ruleset legacy", SaveException.OlderRules, "earlier version of the game's rules")]
    [TestCase("ruleset gdd-0.2", SaveException.NewerVersion, "newer version of the game")]
    public void ARefusedSaveIsSaidOnceInThePlayersLanguage(string ruleset, string code, string english)
    {
        var good = GameSession.New(C, 9, Mode.Historical).Save.Write();
        StringAssert.Contains("\nruleset gdd-0.1\n", good);
        foreach (var path in new[] { UiPaths.Strings, UiPaths.StringsHebrew })
        {
            var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text);
            var store = TempStore();
            store.WriteAutosave(good.Replace("ruleset gdd-0.1", ruleset));
            var notes = new List<string>();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Autosave rejected"));
            var session = GameStart.Begin(C, store, () => 6UL, notes, text, out var resumed);
            Assert.IsFalse(resumed);
            Assert.AreEqual(0, session.State.Turn);
            Assert.AreEqual(new[] { text["save.refused." + code] }, notes.ToArray(), path);
        }
        var en = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text)["save.refused." + code];
        StringAssert.Contains(english, en);
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(en, "new game").Count, "said once (N4)");
    }

    /// <summary>#637: a save that loads with warnings (made with other content; and then a replay that ends elsewhere) is
    /// continued, and each warning kind is said in the player's language, English and Hebrew, never the sim's English. The
    /// sim returns the kind; the kind goes to the log.</summary>
    [TestCase("strings.en.yaml", "content_changed")]
    [TestCase("strings.he.yaml", "content_changed")]
    [TestCase("strings.en.yaml", "content_changed", "replay_differs")]
    [TestCase("strings.he.yaml", "content_changed", "replay_differs")]
    public void ALoadWarningIsSaidInThePlayersLanguage(string file, params string[] kinds)
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Folder + "/" + file).text, file);
        var played = GameSession.New(C, 9, Mode.Historical);
        played.Step(new Command[] { new SetMobilization(1.0) });
        var save = played.Save.Write().Replace("content " + C.Hash, "content " + new string('0', 64));
        if (kinds.Length > 1) save = save.Replace("seed 9", "seed 10");   // other content, so a replay that ends elsewhere only warns
        var store = TempStore();
        store.WriteAutosave(save);
        var notes = new List<string>();
        foreach (var kind in kinds) LogAssert.Expect(LogType.Warning, "Autosave loaded with a warning: " + kind);
        var session = GameStart.Begin(C, store, () => 6UL, notes, text, out var resumed);
        Assert.IsTrue(resumed);
        Assert.AreEqual(1, session.State.Turn);
        var expected = new List<string>();
        foreach (var kind in kinds) expected.Add(text.Has("save.warning." + kind) ? text["save.warning." + kind] : "<no string save.warning." + kind + ">");
        Assert.AreEqual(expected.ToArray(), notes.ToArray(), file);
    }

    [Test]
    public void AReadThatFailsAndASetAsideThatFailsNeverStopTheStart()
    {
        var store = new FaultyStore(null);
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("disk gone"));
        GameSession? session = null;
        Assert.DoesNotThrow(() => session = GameStart.Begin(C, store, () => 6UL, notes, Text));
        Assert.AreEqual(0, session!.State.Turn);
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual("Your saved game could not be loaded (the file could not be read or replayed). A new game has started.", notes[0]);
        StringAssert.DoesNotContain("disk gone", notes[0]);          // raw exception text can carry file paths: it is logged, not shown
        Assert.IsTrue(store.SetAsideCalled);
    }

    [Test]
    public void AnUnparseableSaveWhoseSetAsideFailsNeverStopsTheStartEither()
    {
        var store = new FaultyStore("this is not a save\n");
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Autosave rejected"));
        GameSession? session = null;
        Assert.DoesNotThrow(() => session = GameStart.Begin(C, store, () => 6UL, notes, Text));
        Assert.AreEqual(0, session!.State.Turn);
        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(Unreadable(Text), notes[0]);
        Assert.IsTrue(store.SetAsideCalled);
    }

    [Test]
    public void ACrashBetweenDeletingAndMovingLosesNothing()
    {
        // WriteAutosave deletes the old file only after the .tmp is completely written; a crash after the delete leaves just the .tmp.
        var store = TempStore();
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), Text);
        played.EndTurn(); played.EndTurn();
        Directory.CreateDirectory(store.Directory);
        File.WriteAllText(store.AutosavePath + ".tmp", played.Session.Save.Write());
        var notes = new List<string>();
        var resumed = GameStart.Begin(C, store, () => 1UL, notes, Text);
        Assert.IsEmpty(notes);
        Assert.AreEqual(2, resumed.State.Turn);
        Assert.AreEqual(played.State.Hash(), resumed.State.Hash());
        Assert.IsTrue(File.Exists(store.AutosavePath), "the .tmp was promoted");
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
    }

    [Test]
    public void ALeftOverTmpIsIgnoredWhenTheAutosaveExists()
    {
        var store = TempStore();
        var played = new SimBridge(GameSession.New(C, 9, Mode.Historical), Text);
        played.EndTurn();
        store.WriteAutosave(played.Session.Save.Write());
        File.WriteAllText(store.AutosavePath + ".tmp", "half a sav");           // a partial write
        var resumed = GameStart.Begin(C, store, () => 1UL, new List<string>(), Text);
        Assert.AreEqual(1, resumed.State.Turn);
        Assert.AreEqual(played.State.Hash(), resumed.State.Hash());
    }

    [Test]
    public void ADisposedAutosaveStopsWriting()
    {
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var autosave = GameStart.Autosave(bridge, store, Text);
        bridge.EndTurn();
        var before = File.ReadAllText(store.AutosavePath);
        autosave.Dispose();
        bridge.EndTurn();
        Assert.AreEqual(before, File.ReadAllText(store.AutosavePath));
    }

    [Test]
    public void AnOpenPromptIsClearedByAnyChange()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        var session = bridge.Session;
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        bridge.Queue(new SecurityOrder(-1));                         // any change: the prompt no longer applies
        Assert.IsFalse(newGame.Armed);
        ui.Press("new-game");
        Assert.AreSame(session, bridge.Session, "one press after a change only arms again");
        Assert.IsTrue(newGame.Armed);
        Assert.AreEqual("Press New game again to abandon this game and start over.", ui.Q<Label>("toast").text);
        Assert.AreEqual(1, bridge.Queued.Count);
    }

    [Test]
    public void AFailedDeleteStillStartsTheNewGame()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        bridge.EndTurn();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var store = new DeleteFailsStore();
        new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.DoesNotThrow(() => ui.Press("new-game"));
        Assert.AreEqual(0, bridge.State.Turn);
        StringAssert.StartsWith("The old saved game could not be removed", ui.Q<Label>("toast").text);
    }

    sealed class DeleteFailsStore : SaveStore
    {
        public DeleteFailsStore() : base(Path.Combine(Path.GetTempPath(), "statecraft-faulty")) { }
        public override void DeleteAutosave() => throw new IOException("locked");
    }

    [Test]
    public void NewGameNeedsASecondPress()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        GameStart.Autosave(bridge, store, Text);
        bridge.EndTurn();
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        Assert.AreEqual("Press New game again to abandon this game and start over.", ui.Q<UnityEngine.UIElements.Label>("toast").text);
        Assert.AreEqual(1, bridge.State.Turn);
        ui.Press("new-game");
        Assert.AreEqual(0, bridge.State.Turn);
        Assert.IsFalse(File.Exists(store.AutosavePath));
    }

    [Test]
    public void TheWebSaveFolderDoesNotDependOnThePageUrl()
    {
        // On the web Application.persistentDataPath is /idbfs/<hash of the page URL>; the save folder must not be under it.
        var persistent = "/idbfs/0a1b2c3d4e5f60718293a4b5c6d7e8f9";
        var web = SaveStore.DirectoryFor(RuntimePlatform.WebGLPlayer, persistent);
        Assert.AreEqual(SaveStore.WebDirectory, web);
        StringAssert.DoesNotContain(persistent, web);
        Assert.AreEqual(web, SaveStore.DirectoryFor(RuntimePlatform.WebGLPlayer, "/idbfs/ffffffffffffffffffffffffffffffff"));
        Assert.AreEqual(Path.Combine("/home/x/.config/game", "saves"), SaveStore.DirectoryFor(RuntimePlatform.OSXPlayer, "/home/x/.config/game"));
    }

    [Test]
    public void AFailingAutosaveWriteNeverStopsATurn()
    {
        var file = Path.Combine(Path.GetTempPath(), "statecraft-not-a-folder-" + System.Guid.NewGuid().ToString("N"));
        File.WriteAllText(file, "x");                                   // a file where the folder should be
        try
        {
            var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
            var errors = new List<string>();
            GameStart.Autosave(bridge, new SaveStore(file), Text, errors.Add);
            Assert.DoesNotThrow(() => bridge.EndTurn());
            Assert.AreEqual(1, bridge.State.Turn);
            Assert.AreEqual(1, errors.Count);
            StringAssert.StartsWith("Your game could not be saved", errors[0]);
        }
        finally { File.Delete(file); }
    }

    [Test]
    public void ANewGameIgnoresTheAutosaveWhenAsked()
    {
        var store = TempStore();
        var played = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        GameStart.Autosave(played, store, Text);
        played.EndTurn();
        var session = GameStart.Begin(C, store, () => 8UL, new List<string>(), Text, fresh: true);
        Assert.AreEqual(0, session.State.Turn);
        Assert.IsTrue(File.Exists(store.AutosavePath), "an ignored save is left alone");
    }

    [Test]
    public void ASaveMadeWithOtherContentLoadsWithANote()
    {
        var store = TempStore();
        var text = GameSession.New(C, 9, Mode.Historical).Save.Write().Replace("content " + C.Hash, "content " + new string('0', C.Hash.Length));
        store.WriteAutosave(text);
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, "Autosave loaded with a warning: " + GameSession.ContentChanged);
        var session = GameStart.Begin(C, store, () => 1UL, notes, Text);
        Assert.AreEqual(new[] { Text["save.warning." + GameSession.ContentChanged] }, notes.ToArray());   // #637
        Assert.IsTrue(File.Exists(store.AutosavePath), "a warning is not a failure: the file is kept until the next turn's autosave replaces it");
        Assert.AreEqual(0, session.State.Turn);
    }

    [Test]
    public void ArmingIsCancelledByATurnAndADisposedButtonDoesNothing()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("new-game");
        Assert.IsTrue(newGame.Armed);
        bridge.EndTurn();
        Assert.IsFalse(newGame.Armed);
        ui.Press("new-game");
        Assert.AreEqual(1, bridge.State.Turn, "one press after a turn only arms again");
        newGame.Dispose();
        ui.Press("new-game"); ui.Press("new-game");
        Assert.AreEqual(1, bridge.State.Turn);
    }

    [Test]
    public void NewGameRefreshesTheScreens()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        var hud = new HudPresenter(ui.Root, bridge, Text);
        var toast = new ToastPresenter(ui.Root, bridge, Text);
        new NewGamePresenter(ui.Root, bridge, store, toast, Text, () => 4UL);
        ui.Press("end-turn");
        Assert.AreEqual("Turn 1", ui.Q<Label>("turn").text);
        ui.Press("new-game"); ui.Press("new-game");
        Assert.AreEqual("Turn 0", ui.Q<Label>("turn").text);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("toast").style.display.value);
    }

    [Test]
    public void ALingeringTmpIsSetAsideWhenThereIsNoAutosave()
    {
        // Promotion failed and the .tmp cannot be read: autosave.txt is missing, so the .tmp itself must be set aside,
        // or it would be found, rejected and noted on every start.
        var store = TempStore();
        Directory.CreateDirectory(store.Directory);
        File.WriteAllText(store.AutosavePath + ".tmp", "half a sav");
        store.SetAsideAutosave();
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
        var notes = new List<string>();
        Assert.AreEqual(0, GameStart.Begin(C, store, () => 1UL, notes, Text).State.Turn);
        Assert.IsEmpty(notes, "nothing is left to reject on the next start");
    }

    [Test]
    public void ABrokenFirstWriteEndsSafely()
    {
        // A crash in the middle of the very first write leaves a partial .tmp and no autosave.txt.
        var store = TempStore();
        var good = GameSession.New(C, 9, Mode.Historical).Save.Write();
        Directory.CreateDirectory(store.Directory);
        File.WriteAllText(store.AutosavePath + ".tmp", good.Substring(0, good.Length / 2));
        var notes = new List<string>();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Autosave rejected"));
        var session = GameStart.Begin(C, store, () => 1UL, notes, Text);
        Assert.AreEqual(0, session.State.Turn);
        Assert.AreEqual(1, notes.Count);
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        Assert.IsFalse(File.Exists(store.AutosavePath));
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
    }

    [Test]
    public void NewGameAlsoRemovesALingeringTmp()
    {
        using var ui = new UiTestKit();
        var store = TempStore();
        var played = new SimBridge(GameSession.New(C, 3, Mode.Historical), Text);
        played.EndTurn();
        Directory.CreateDirectory(store.Directory);
        File.WriteAllText(store.AutosavePath + ".tmp", played.Session.Save.Write());      // a complete save, only the .tmp
        var toast = new ToastPresenter(ui.Root, played, Text);
        new NewGamePresenter(ui.Root, played, store, toast, Text, () => 4UL);
        ui.Press("new-game"); ui.Press("new-game");
        Assert.IsFalse(File.Exists(store.AutosavePath + ".tmp"));
        var notes = new List<string>();
        Assert.AreEqual(0, GameStart.Begin(C, store, () => 1UL, notes, Text).State.Turn, "the next start does not resurrect the abandoned game");
        Assert.IsEmpty(notes);
    }
}
