#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The advisors in the game as a player boots it: built in every Begin, silent in automated runs, kept across a
/// language switch, and never touching the sim or the save.</summary>
public class AdvisorGameTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    readonly List<string> temp = new List<string>();
    string TempDir() { var d = Path.Combine(Path.GetTempPath(), "statecraft-adv-" + Guid.NewGuid().ToString("N")); temp.Add(d); return d; }

    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (var d in temp) { try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch (Exception) { } }
        temp.Clear();
    }

    internal static GameRoot OpenScene(SaveStore store, SettingsStore? settings)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Settings = settings;
        return root;
    }

    [Test]
    public void EveryBeginBuildsTheAdvisorsAndAutomatedRunsStaySilent()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0);                                           // the tools' Begin: no start screen
        Assert.IsFalse(game.AdvisorSession!.AutoShow, "no start screen: never by itself");
        Assert.IsNotNull(game.Advisor);
        Assert.IsNotNull(Ui.Q(AdvisorPresenter.LayerName).userData, "the cover hook is set");
        Ui.Q<Button>("end-turn").Focus();
        game.Bridge!.EndTurn();
        Assert.IsFalse(game.Advisor!.IsOpen, "no start screen: never by itself");
        Assert.IsEmpty(game.AdvisorSession!.Queue.History);
    }

    /// <summary>With the start screen but no settings file to remember what was seen, they never show by themselves.</summary>
    [Test]
    public void NoSettingsFileMeansSilentEvenWithTheStartScreen()
    {
        var game = OpenScene(new SaveStore(TempDir()), null);
        game.Begin(Inputs, 0, startScreen: true);
        Assert.IsFalse(game.AdvisorSession!.AutoShow);
    }

    /// <summary>The positive control for the two silent cases: the start screen and a settings file, as a player boots it.</summary>
    [Test]
    public void TheStartScreenAndASettingsFileLetThemShowByThemselves()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(game.AdvisorSession!.AutoShow);
    }

    [Test]
    public void TheLanguageSwitchKeepsTheAdvisorsSession()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var session = game.AdvisorSession;
        UiTestKit.Press(Ui.Q<Button>("nav-settings"));
        UiTestKit.Press(Ui.Q<Button>("settings-language-he"));
        Assert.AreSame(session, game.AdvisorSession, "the same game in another language: the same session");
        Assert.IsTrue(game.AdvisorSession!.AutoShow, "and still showing by itself, though the rebuild shows no start screen");
        game.Begin(Inputs, 0, startScreen: true);                        // a reload: a new session
        Assert.AreNotSame(session, game.AdvisorSession);
    }

    [Test]
    public void ChangesNeitherTheSimNorTheSave()
    {
        var store = new SaveStore(TempDir());
        var game = OpenScene(store, new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        var bridge = game.Bridge!;
        bridge.EndTurn();
        if (Ui.Q("root").ClassListContains(TurnReportPresenter.OpenClass)) UiTestKit.Press(Ui.Q<Button>("turn-report-close"));
        var hash = bridge.State.Hash(); var save = store.ReadAutosave(); var queued = bridge.Queued.Count;
        game.Advisor!.Notice(AdvisorSignals.TurnEnded);
        if (game.Advisor.IsOpen) game.Advisor.GotIt();
        Assert.AreEqual(hash, bridge.State.Hash());
        Assert.AreEqual(queued, bridge.Queued.Count);
        Assert.AreEqual(save, store.ReadAutosave(), "the save file");
    }
}


public class AdvisorGameNoAttentionTests
{
    // R-GOV-002: replacement on the same bridge consults the current session ruleset.
    [TestCase(Statecraft.Sim.SaveGame.LegacyRuleset)] [TestCase(Statecraft.Sim.SaveGame.GddRuleset)]
    public void NewSessionRestoresOldTourWithoutLeakingSkippedSeenState(string olderRuleset)
    {
        using var rig = new AdvisorTour1688Fixture.Rig(); rig.Advisor.OpenChain(); rig.Advisor.NextStep();
        Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
        rig.Bridge.Replace(Statecraft.Sim.GameSession.New(AdvisorTour1688Fixture.Content(olderRuleset), 1, Statecraft.Sim.Mode.Historical));
        rig.Advisor.NewGame(); rig.Advisor.OpenChain(); rig.Advisor.NextStep();
        Assert.AreEqual("opening.attention", rig.Advisor.Shown?.Id);
        Assert.IsFalse(rig.Session.Settings.Seen("opening.attention"));
        rig.Advisor.NextStep(); Assert.IsTrue(rig.Session.Settings.Seen("opening.attention"));
        rig.Bridge.Replace(Statecraft.Sim.GameSession.New(AdvisorTour1688Fixture.Content(), 1, Statecraft.Sim.Mode.Historical));
        rig.Advisor.NewGame(); rig.Advisor.OpenChain(); rig.Advisor.NextStep();
        Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
        Assert.IsEmpty(System.Linq.Enumerable.Where(rig.Session.Queue.History, e => e.Id == "opening.attention"));
    }

    // R-GOV-002: load gets a fresh advisor session, no automatic/persisted traversal.
    [Test]
    public void LoadDoesNotResumeTourButManualReplayStillSkipsAttention()
    {
        var c = AdvisorTour1688Fixture.Content(); var game = Statecraft.Sim.GameSession.New(c, 1, Statecraft.Sim.Mode.Historical);
        var save = game.Save.Write();
        using (var first = new AdvisorTour1688Fixture.Rig(game: game))
        {
            first.Advisor.OpenChain(); first.Advisor.NextStep(); Assert.AreEqual("opening.city", first.Advisor.Shown?.Id);
        }
        var loaded = Statecraft.Sim.GameSession.Load(c, save, out var warnings); Assert.IsEmpty(warnings);
        using var rig = new AdvisorTour1688Fixture.Rig(game: loaded);
        rig.Advisor.Resume(); Assert.IsFalse(rig.Advisor.InChain);
        rig.Advisor.OpenChain(); rig.Advisor.NextStep(); Assert.AreEqual("opening.city", rig.Advisor.Shown?.Id);
        rig.Advisor.CloseChain(); Assert.IsFalse(rig.Session.Settings.Seen("opening.attention"));
        Assert.AreEqual(save, loaded.Save.Write()); Assert.IsEmpty(rig.Bridge.Queued);
    }
}
