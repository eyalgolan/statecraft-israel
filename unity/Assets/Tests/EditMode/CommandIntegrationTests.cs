#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.Prototype;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Actual Map.unity startup, save isolation and restoration for the opt-in experiment.</summary>
public sealed class CommandIntegrationTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    string saveDir = "";
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    sealed class GuardedStore : SaveStore
    {
        public int Reads, Deletes, SetAsides;
        public GuardedStore(string path) : base(path) { }
        public override string? ReadAutosave() { Reads++; throw new InvalidOperationException("Command must never read a campaign save."); }
        public override void DeleteAutosave() { Deletes++; throw new InvalidOperationException("Command must never delete a campaign save."); }
        public override void SetAsideAutosave() { SetAsides++; throw new InvalidOperationException("Command must never move a campaign save."); }
    }

    [SetUp] public void OpenMap()
    {
        saveDir = Path.Combine(Path.GetTempPath(), "statecraft-command-" + Guid.NewGuid().ToString("N"));
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
    }
    [TearDown] public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    [Test] public void CommandNeverReadsWritesMovesOrDeletesTheCampaignSave()
    {
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, SaveStore.AutosaveName);
        File.WriteAllText(path, "player save sentinel"); File.WriteAllText(path + ".tmp", "pending save sentinel");
        var store = new GuardedStore(saveDir);
        var root = Object.FindFirstObjectByType<GameRoot>(); root.Store = store;
        root.Begin(Inputs, 0, startScreen: true, commandMode: true);
        var prototype = root.GetComponent<CommandPrototype>();
        Assert.IsNotNull(prototype); Assert.IsTrue(prototype.Model.Paused);
        Assert.IsNotNull(Ui.Q("command-prototype")); Assert.IsNull(root.Units);
        Assert.IsFalse(Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass));
        Assert.AreEqual(GameSession.New(Inputs.Content, GoldenRun.Seed, Mode.Historical).State.Hash(), root.Bridge!.State.Hash());
        var session = root.Bridge.Session;
        UiTestKit.Press(Ui.Q<Button>("new-game")); UiTestKit.Press(Ui.Q<Button>("new-game"));
        Assert.AreSame(session, root.Bridge.Session);
        root.Bridge.EndTurn(); prototype.Restart(); root.enabled = false;
        Assert.AreEqual(0, store.Reads); Assert.AreEqual(0, store.Deletes); Assert.AreEqual(0, store.SetAsides);
        Assert.AreEqual("player save sentinel", File.ReadAllText(path));
        Assert.AreEqual("pending save sentinel", File.ReadAllText(path + ".tmp"));
        Assert.AreEqual(2, Directory.GetFiles(saveDir).Length);
    }

    [Test] public void DefaultStartScreenAndCampaignRemainAvailableWithoutTheFlag()
    {
        var root = Object.FindFirstObjectByType<GameRoot>(); root.Store = new SaveStore(saveDir);
        root.Begin(Inputs, 0, startScreen: true);
        Assert.IsNull(root.GetComponent<CommandPrototype>()); Assert.IsNull(Ui.Q("command-prototype"));
        Assert.IsNotNull(root.Units); Assert.IsTrue(Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass));
        root.Bridge!.EndTurn(); Assert.IsTrue(File.Exists(Path.Combine(saveDir, SaveStore.AutosaveName)));
    }

    [Test] public void SwitchingFromCampaignHidesSymbolsAndTeardownRestoresInputCameraAndLayers()
    {
        var root = Object.FindFirstObjectByType<GameRoot>(); root.Store = new SaveStore(saveDir);
        root.Begin(Inputs, 0);
        var map = Object.FindFirstObjectByType<MapScene>();
        var unitObject = map.Terrain.transform.Find(UnitLayer.RootName).gameObject;
        var placements = Object.FindFirstObjectByType<PlacementLayer>().gameObject;
        var pointer = Object.FindFirstObjectByType<MapPointer>(); var controller = Camera.main.GetComponent<CameraController>();
        var rig = controller.GetComponent<CameraRig>(); var oldLook = rig.LookAt; var oldDistance = rig.Distance; var oldYaw = rig.Yaw;
        Assert.IsTrue(unitObject.activeSelf); Assert.IsTrue(placements.activeSelf);
        root.Begin(Inputs, 0, commandMode: true);
        Assert.IsFalse(unitObject.activeSelf); Assert.IsFalse(placements.activeSelf);
        Assert.IsFalse(pointer.enabled); Assert.IsFalse(controller.enabled);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("root").style.display.value);
        var prototype = root.GetComponent<CommandPrototype>(); prototype.Restart();
        Assert.AreEqual(1, root.GetComponents<CommandPrototype>().Length);
        Assert.AreEqual(1, Ui.Query<VisualElement>("command-prototype").ToList().Count);
        root.enabled = false;
        Assert.IsTrue(pointer.enabled); Assert.IsTrue(controller.enabled);
        Assert.IsTrue(unitObject.activeSelf); Assert.IsTrue(placements.activeSelf);
        Assert.AreNotEqual(DisplayStyle.None, Ui.Q("root").style.display.value);
        Assert.IsNull(Ui.Q("command-prototype")); Assert.IsNull(root.GetComponent<CommandPrototype>());
        Assert.AreEqual(oldLook, rig.LookAt); Assert.AreEqual(oldDistance, rig.Distance); Assert.AreEqual(oldYaw, rig.Yaw);
        root.enabled = true; root.Begin(Inputs, 0);
        Assert.IsNotNull(root.Units); Assert.IsTrue(root.Units!.gameObject.activeSelf);
        Assert.AreSame(map, pointer.Map); Assert.IsNotNull(pointer.IsOverUi); Assert.IsNotNull(controller.IsOverUi);
    }

    [Test] public void RestartResetsOnlyTheToyMissionAndDisposalRestoresInitiallyDisabledControls()
    {
        var root = Object.FindFirstObjectByType<GameRoot>(); root.Store = new GuardedStore(saveDir);
        var pointer = Object.FindFirstObjectByType<MapPointer>(); var controller = Camera.main.GetComponent<CameraController>();
        pointer.enabled = false; controller.enabled = false;
        root.Begin(Inputs, 0, commandMode: true);
        var prototype = root.GetComponent<CommandPrototype>(); var first = prototype.Model;
        var campaignHash = root.Bridge!.State.Hash();
        Assert.IsTrue(first.Build(3, CommandKind.Camp)); first.Paused = false; first.Advance(10);
        Assert.IsTrue(first.CampReady);
        prototype.Restart(); Assert.AreNotSame(first, prototype.Model);
        Assert.IsTrue(prototype.Model.Paused); Assert.AreEqual(0, prototype.Model.Elapsed); Assert.AreEqual(140, prototype.Model.Money);
        Assert.AreEqual(campaignHash, root.Bridge.State.Hash()); Assert.IsFalse(Directory.Exists(saveDir));
        root.enabled = false;
        Assert.IsFalse(pointer.enabled); Assert.IsFalse(controller.enabled);
        Assert.IsNull(GameObject.Find("CommandPrototype_LocalPlaytest"));
    }
}
