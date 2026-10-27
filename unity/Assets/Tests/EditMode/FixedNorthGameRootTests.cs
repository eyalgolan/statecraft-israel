#nullable enable
using System;
using System.Collections;
using System.IO;
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

public class FixedNorthGameRootTests
{
    string saveDir = "";
    GameRoot game = null!;
    CameraController controller = null!;
    CameraRig rig = null!;
    VisualElement ui = null!;
    GameInputs inputs = null!;

    [SetUp]
    public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-heading-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void Close()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
        VisualToggles.Clear();
    }

    void Open(string ruleset, bool oldPolicy = false)
    {
        // Fresh inputs: never change the shared MapFocusTests content or any file on disk.
        inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        foreach (var era in inputs.Content.Eras)
        {
            era.Systems.Ruleset = ruleset;
            era.Systems.Loop = ruleset == SaveGame.LegacyRuleset ? EraSystems.Legacy : EraSystems.Gdd;
        }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(inputs.Map);
        game = Object.FindFirstObjectByType<GameRoot>();
        game.Store = new SaveStore(saveDir);
        game.Language = UiLanguage.English;
        controller = Camera.main.GetComponent<CameraController>();
        rig = controller.GetComponent<CameraRig>();
        controller.SetFixedNorth(oldPolicy);
        rig.Yaw = 37f; rig.Apply();
        ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        Assert.IsNotNull(ui.panel, "GameRoot integration needs a real panel");
    }

    void Heading(bool north)
    {
        Assert.AreEqual(north, controller.FixedNorth);
        if (north)
        {
            Assert.AreEqual(0f, rig.Yaw);
            Assert.AreEqual(0f, rig.transform.forward.x, 0.00001f);
            Assert.Greater(rig.transform.forward.z, 0f, "north-up minimap and camera agree");
            Assert.Less(rig.transform.forward.y, 0f, "fixed pitch looks down");
        }
    }

    [TestCase("legacy", false)]
    [TestCase("legacy", true)]
    [TestCase("gdd-0.1", false)]
    [TestCase("gdd-0.1", true)]
    [TestCase("gdd-0.3", false)]
    [TestCase("gdd-0.3", true)]
    public void BeginReentryAndDisableRestoreTheOwnedPolicy(string ruleset, bool oldPolicy)
    {
        Open(ruleset, oldPolicy);
        bool north = ruleset == SaveGame.Gdd03Ruleset;
        game.Begin(inputs, 0);
        Heading(north);
        if (!north) Assert.AreEqual(37f, rig.Yaw, "Begin must not normalize legacy yaw");
        rig.Yaw = 83f; rig.Apply();
        game.Begin(inputs, 0); // Executes Shutdown and its Undo before registering the next owner.
        Heading(north);
        if (!north && !oldPolicy) Assert.AreEqual(83f, rig.Yaw);
        rig.Yaw = 71f; rig.Apply();
        game.enabled = false; // OnDisable -> Shutdown -> owned Undo.
        Heading(oldPolicy);
        if (!oldPolicy) Assert.AreEqual(71f, rig.Yaw, "release does not apply or normalize yaw");
        game.enabled = true;
        game.Begin(inputs, 0);
        Heading(north);
        Object.DestroyImmediate(game); // Scene teardown uses the same OnDisable path.
        Heading(oldPolicy);
    }

    [TestCase("legacy")]
    [TestCase("gdd-0.1")]
    [TestCase("gdd-0.3")]
    public void NewGameLoadAndLanguageRebuildKeepTheSessionPolicy(string ruleset)
    {
        Open(ruleset);
        var saved = GameSession.New(inputs.Content, 123, Mode.Historical);
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, SaveStore.AutosaveName), saved.Save.Write());
        game.Begin(inputs, 0);
        Assert.AreEqual(saved.State.Hash(), game.Bridge!.State.Hash(), "Begin resumes the isolated autosave");
        bool north = ruleset == SaveGame.Gdd03Ruleset;
        Heading(north);
        var loaded = game.Bridge.Session;
        UiTestKit.Press(ui.Q<Button>("new-game"));
        UiTestKit.Press(ui.Q<Button>("new-game"));
        Assert.AreNotSame(loaded, game.Bridge.Session, "the actual New Game presenter replaced the session");
        Heading(north);
        var keep = game.Bridge;
        typeof(GameRoot).GetMethod("SwitchLanguage", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.Invoke(game, new object[] { inputs, UiLanguage.Hebrew });
        Assert.AreSame(keep.Session, game.Bridge!.Session, "language rebuild retains the session");
        Heading(north);
        game.enabled = false;
        Assert.IsFalse(controller.FixedNorth);
    }

    [TestCase("legacy")]
    [TestCase("gdd-0.1")]
    public void BeginSwitchesRulesetsOnTheSameController(string olderRuleset)
    {
        Open(SaveGame.Gdd03Ruleset);
        game.Begin(inputs, 0);
        Heading(true);
        var older = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        foreach (var era in older.Content.Eras)
        {
            era.Systems.Ruleset = olderRuleset;
            era.Systems.Loop = olderRuleset == SaveGame.LegacyRuleset ? EraSystems.Legacy : EraSystems.Gdd;
        }
        rig.Yaw = 29f; rig.Apply();
        game.Begin(older, 0);
        Heading(false);
        Assert.AreEqual(29f, rig.Yaw, "releasing the old session does not normalize the next one");
        game.Begin(inputs, 0);
        Heading(true);
    }

    [UnityTest]
    public IEnumerator Gdd03MinimapClickAndOverviewSettleBeforeSyncAndKeepNorth()
    {
        Open(SaveGame.Gdd03Ruleset);
        game.Begin(inputs, 0);
        GameSceneTests.HideTheOpeningCard(ui);
        var image = ui.Q("minimap-image");
        for (int frame = 0; frame < 60 && !(image.layout.width > 0 && image.layout.height > 0); frame++) yield return null;
        Assert.Greater(image.layout.width, 0f, "minimap geometry not ready after 60 frames");
        Assert.Greater(image.layout.height, 0f);
        var view = ui.Q("minimap-view");
        rig.Yaw = 47f; rig.Apply();
        view.style.left = -999f;
        using (var e = PointerDownEvent.GetPooled(new Event
            { type = EventType.MouseDown, button = 0,
                mousePosition = image.worldBound.position + Vector2.Scale(image.worldBound.size, new Vector2(0.1f, 0.1f)) }))
        {
            e.target = image;
            image.SendEvent(e); // The presenter's callback performs Settle then SyncMinimap synchronously.
        }
        Heading(true);
        Assert.AreNotEqual(-999f, view.style.left.value.value, "click synchronized the minimap before returning");
        Assert.That(view.style.left.value.value, Is.InRange(0f, 100f));
        AssertViewMatchesSettledRig(view);
        rig.Yaw = 91f; rig.Apply();
        view.style.left = -999f;
        UiTestKit.Press(ui.Q<Button>("minimap-overview"));
        Heading(true);
        Assert.AreEqual(CameraController.MaxDistance, rig.Distance);
        Assert.AreNotEqual(-999f, view.style.left.value.value, "overview synchronized after settling");
        AssertViewMatchesSettledRig(view);
    }

    void AssertViewMatchesSettledRig(VisualElement view)
    {
        var map = Object.FindFirstObjectByType<MapScene>();
        var b = map.Hexes.bounds;
        var (lon, lat) = map.Frame.ToLonLat(rig.LookAt);
        double widthKm = (b.lon1 - b.lon0) * HexPicker.KmPerDegLon(b.lat0, b.lat1);
        double heightKm = (b.lat1 - b.lat0) * HexPicker.KmPerDegLat;
        float w = Mathf.Min(1f, (float)(1.5 * rig.Distance / 1000 / widthKm));
        float h = Mathf.Min(1f, (float)(rig.Distance / 1000 / heightKm));
        float u = (float)((lon - b.lon0) / (b.lon1 - b.lon0));
        float v = (float)((b.lat1 - lat) / (b.lat1 - b.lat0));
        Assert.AreEqual(Mathf.Clamp(u - w / 2, 0, 1 - w) * 100, view.style.left.value.value, 0.001f);
        Assert.AreEqual(Mathf.Clamp(v - h / 2, 0, 1 - h) * 100, view.style.top.value.value, 0.001f);
        Assert.AreEqual(w * 100, view.style.width.value.value, 0.001f);
        Assert.AreEqual(h * 100, view.style.height.value.value, 0.001f);
    }
}
