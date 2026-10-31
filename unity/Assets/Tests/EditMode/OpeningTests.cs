#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#530 / phase 7 PR10: New Game can skip the opening to the map; replay returns to the start screen.
/// The presentation must not replace the session, advance the simulation, queue an order or alter the autosave.</summary>
public class OpeningTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    string? saveDirectory;
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static bool StartOpen => Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass);

    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (saveDirectory != null && Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
        saveDirectory = null;
    }

    [UnityTest]
    public IEnumerator CanBeSkippedAndReplayed()
    {
        saveDirectory = Path.Combine(Path.GetTempPath(), "statecraft-opening-" + Guid.NewGuid().ToString("N"));
        var store = new SaveStore(saveDirectory);
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store;
        root.Language = "en";
        root.OpeningInEditMode = true;
        var camera = Object.FindFirstObjectByType<CameraController>();
        root.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(StartOpen, "precondition: the real start screen is open");
        var bridge = root.Bridge!;
        var session = bridge.Session;
        var hash = bridge.State.Hash();
        var save = session.Save.Write();
        var orders = bridge.Queued.ToArray();

        // This is the existing no-save New Game path, not a direct call into a future opening presenter.
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        var opening = Ui.Q("opening-layer");
        Assert.IsNotNull(opening, "New Game must show the opening before the first turn (#530)");
        Assert.AreEqual(DisplayStyle.Flex, opening.resolvedStyle.display, "the opening is displayed");
        Assert.IsFalse(camera.enabled, "the map cannot receive camera input behind the opening");
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        yield return null;
        Assert.AreEqual(DisplayStyle.None, opening.resolvedStyle.display, "Skip closes the opening");
        Assert.IsFalse(StartOpen, "skipping a new game's opening returns to the map");
        Assert.IsTrue(camera.enabled, "the map's camera is returned after Skip");
        Assert.AreSame(session, bridge.Session, "the opening does not start another session");
        Assert.AreEqual(hash, bridge.State.Hash(), "the opening does not advance the state or RNG");
        Assert.AreEqual(save, bridge.Session.Save.Write(), "the opening does not alter the replay");
        CollectionAssert.AreEqual(orders, bridge.Queued, "the opening queues no orders");
        Assert.IsFalse(File.Exists(store.AutosavePath), "presentation alone does not create an autosave");

        store.WriteAutosave(save);
        root.enabled = false;
        root.enabled = true;
        root.Begin(Inputs, 0, startScreen: true);
        Assert.IsTrue(StartOpen, "precondition: a saved session is behind the start screen");
        UiTestKit.Press(Ui.Q<Button>("start-continue"));
        yield return null;
        Assert.IsFalse(StartOpen);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("opening-layer").resolvedStyle.display, "Continue does not replay the opening");
        Assert.AreEqual(hash, root.Bridge!.State.Hash());

        root.enabled = false;
        root.enabled = true;
        root.Begin(Inputs, 0, startScreen: true);
        bridge = root.Bridge!;
        session = bridge.Session;
        orders = bridge.Queued.ToArray();
        UiTestKit.Press(Ui.Q<Button>("start-opening-replay"));
        yield return null;
        opening = Ui.Q("opening-layer");
        Assert.IsNotNull(opening);
        Assert.AreEqual(DisplayStyle.Flex, opening.resolvedStyle.display, "the start screen can replay the opening");
        Assert.IsFalse(camera.enabled);
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        yield return null;
        Assert.AreEqual(DisplayStyle.None, opening.resolvedStyle.display);
        Assert.IsTrue(StartOpen, "skipping a replay returns to the start screen");
        Assert.IsFalse(camera.enabled, "the start screen still shields the map");
        Assert.AreSame(session, bridge.Session, "replay keeps the loaded session");
        Assert.AreEqual(hash, bridge.State.Hash(), "replay leaves state and RNG unchanged");
        CollectionAssert.AreEqual(orders, bridge.Queued, "replay queues no orders");
        Assert.AreEqual(save, bridge.Session.Save.Write());
        Assert.AreEqual(save, File.ReadAllText(store.AutosavePath), "replay does not rewrite the autosave");
    }
}
