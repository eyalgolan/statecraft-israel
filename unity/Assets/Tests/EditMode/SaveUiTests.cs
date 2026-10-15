#nullable enable
using System.Collections.Generic;
using System.IO;
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
        StringAssert.StartsWith("Your saved game could not be loaded (line 1: save schema version 9 is newer", notes[0]);
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
        StringAssert.StartsWith("Your saved game could not be loaded (line 4: mode Open is not supported)", notes[0]);
        Assert.IsTrue(File.Exists(Path.Combine(store.Directory, "autosave.bad.txt")));
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
        StringAssert.StartsWith("Your saved game could not be loaded (Not a Statecraft save", notes[0]);
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
        var session = GameStart.Begin(C, store, () => 1UL, notes, Text);
        Assert.AreEqual(1, notes.Count);
        StringAssert.Contains("different game content", notes[0]);
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
