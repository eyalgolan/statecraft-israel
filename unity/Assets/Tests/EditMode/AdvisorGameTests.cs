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
        Assert.IsNotNull(game.Advisor);
        Assert.IsNotNull(Ui.Q(AdvisorPresenter.LayerName).userData, "the cover hook is set");
        Ui.Q<Button>("end-turn").Focus();
        game.Bridge!.EndTurn();
        Assert.IsFalse(game.Advisor!.IsOpen, "no start screen: never by itself");
        Assert.IsEmpty(game.AdvisorSession!.Queue.History);
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
