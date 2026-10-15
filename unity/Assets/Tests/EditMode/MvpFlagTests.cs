#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>The ?mvp=1 dev flag (#380): it loads the content through MvpSlice.Overlay (the GDD slice) for playtests. It is
/// off by default, the golden check and the smoke run ignore it, and a slice game never reads, sets aside or writes the
/// player's autosave.</summary>
public class MvpFlagTests
{
    const string Page = "https://example.org/statecraft/index.html";

    [TestCase(Page + "?mvp=1", false, 0, ExpectedResult = true)]
    [TestCase(Page + "?lang=he&mvp=1", false, 0, ExpectedResult = true)]
    [TestCase(Page + "?mvp=1#top", false, 0, ExpectedResult = true)]
    [TestCase(Page, false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=0", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp-x=1", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=2", false, 0, ExpectedResult = false)]                // exactly "1": not any other number
    [TestCase(Page + "?mvp=10", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=01", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=1x", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=1.5", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvp=1&mvp=0", false, 0, ExpectedResult = false)]          // two values: not a clear request
    [TestCase(Page + "?mvp=0&mvp=1", false, 0, ExpectedResult = false)]
    [TestCase(Page + "?mvpx=1&mvp=1", false, 0, ExpectedResult = true)]          // another parameter's name does not count
    [TestCase(Page + "?xmvp=1", false, 0, ExpectedResult = false)]
    [TestCase("", false, 0, ExpectedResult = false)]
    [TestCase(null, false, 0, ExpectedResult = false)]
    [TestCase(Page + "?golden&mvp=1", true, 0, ExpectedResult = false)]          // the golden check runs the real content
    [TestCase(Page + "?mvp=1&smoke=5", false, 5, ExpectedResult = false)]        // and so does the smoke run
    [TestCase(Page + "?golden&smoke=5&mvp=1", true, 5, ExpectedResult = false)]
    public bool WhichUrlsTurnTheSliceOn(string? url, bool golden, int smoke) => GameBoot.SliceRequested(url, golden, smoke);

    [Test]
    public void WithoutTheFlagTheContentIsTheRealContent()
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        Assert.IsFalse(inputs.Slice);
        Assert.IsFalse(inputs.Content.IsGdd);
        Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, inputs.Content.Hash, "the real content hash does not move");
    }

    [Test]
    public void TheFlagLoadsTheSliceFromDisk()
    {
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap, slice: true);
        Assert.IsTrue(inputs.Slice);
        Assert.IsTrue(inputs.Content.IsGdd);
        Assert.AreEqual(new[] { "tel_aviv" }, inputs.Content.Cities.Select(c => c.Id).ToArray());
        Assert.AreEqual(MvpSlice.Load(Paths.Content).Hash, inputs.Content.Hash);
    }

    /// <summary>The WebGL path: the fetched texts (keys are manifest paths), with and without the flag.</summary>
    [Test]
    public void TheFlagLoadsTheSliceFromFetchedTexts()
    {
        var manifest = ContentManifest.Collect(Paths.Content);
        var texts = manifest.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(Paths.Content, f)));
        var meta = File.ReadAllText(Path.Combine(Paths.StreamingMap, "heightmap.json"));
        var r16z = File.ReadAllBytes(Path.Combine(Paths.StreamingMap, HeightmapCodec.FileName));

        var real = GameInputs.FromTexts(texts, manifest, meta, r16z);
        Assert.IsFalse(real.Slice);
        Assert.IsFalse(real.Content.IsGdd);
        Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, real.Content.Hash);

        var slice = GameInputs.FromTexts(texts, manifest, meta, r16z, slice: true);
        Assert.IsTrue(slice.Slice);
        Assert.IsTrue(slice.Content.IsGdd);
        Assert.AreEqual(MvpSlice.Load(Paths.Content).Hash, slice.Content.Hash);
    }

    // ---- A slice game and the player's save ----

    string saveDir = "";

    [SetUp] public void TempSaves() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-mvp-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void DiscardScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameInputs? sliceInputs;
    static GameInputs SliceInputs => sliceInputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap, slice: true);

    GameRoot BootSlice()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(SliceInputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(SliceInputs, 0);
        return root;
    }

    [Test]
    public void ASliceGameLeavesThePlayersAutosaveAlone()
    {
        // The player's own game, saved by the real content.
        var legacy = GameSession.New(ContentLoader.Load(Paths.Content), GoldenRun.Seed, Mode.Historical);
        legacy.Step(Array.Empty<Command>());
        var saved = legacy.Save.Write();
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, SaveStore.AutosaveName);
        File.WriteAllText(path, saved);

        var root = BootSlice();
        Assert.IsTrue(root.Bridge!.Content.IsGdd, "the slice is running");
        Assert.AreEqual(0, root.Bridge.State.Turn, "a fresh slice game, not the player's save");
        root.Bridge.EndTurn();
        root.Bridge.EndTurn();

        Assert.AreEqual(saved, File.ReadAllText(path), "the player's autosave is unchanged");
        Assert.AreEqual(new[] { SaveStore.AutosaveName }, Directory.GetFiles(saveDir).Select(Path.GetFileName).ToArray(),
            "nothing set aside, nothing added");
    }

    static GameInputs? realInputs;
    static GameInputs RealInputs => realInputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    static UnityEngine.UIElements.Button NewGameButton() =>
        Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>().rootVisualElement.Q<UnityEngine.UIElements.Button>("new-game");

    /// <summary>Defence in depth: even if a slice game ever got a New Game handler, pressing it twice (ask, confirm) would not
    /// delete the player's autosave.</summary>
    [Test]
    public void NewGamePressedTwiceInASliceKeepsTheAutosave()
    {
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, SaveStore.AutosaveName);
        File.WriteAllText(path, "the player's save");
        using var ui = new UiTestKit();
        var text = UiText.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(UiPaths.Strings).text);
        var bridge = new SimBridge(GameSession.New(SliceInputs.Content, GoldenRun.Seed, Mode.Historical), text);
        using var toast = new ToastPresenter(ui.Root, bridge, text);
        using var newGame = new NewGamePresenter(ui.Root, bridge, new SaveStore(saveDir), toast, text, () => 7UL, slice: true);
        var session = bridge.Session;
        ui.Press("new-game");
        ui.Press("new-game");
        Assert.AreEqual("the player's save", File.ReadAllText(path), "the autosave is untouched");
        Assert.AreSame(session, bridge.Session, "no new game started");
    }

    /// <summary>A slice game booted through GameRoot with the start screen asked for: no start screen, and New Game pressed
    /// twice deletes nothing and starts nothing, even with its button forced back on screen (a regression that showed it).
    /// The guard is the presenter's slice flag, which GameRoot must pass.</summary>
    [Test]
    public void ASliceGamesNewGameThroughGameRootDeletesNothing()
    {
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, SaveStore.AutosaveName);
        File.WriteAllText(path, "the player's save");
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(SliceInputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(SliceInputs, 0, startScreen: true);
        var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        Assert.IsFalse(ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass), "no start screen in a slice game");
        var session = root.Bridge!.Session;
        var button = ui.Q<Button>("new-game");
        button.style.display = DisplayStyle.Flex;   // as if a regression drew it again
        UiTestKit.Press(button);
        UiTestKit.Press(button);
        Assert.AreEqual("the player's save", File.ReadAllText(path), "the autosave is untouched");
        Assert.AreSame(session, root.Bridge.Session, "no new game started");
    }

    /// <summary>A slice game has no New Game (it would delete the player's autosave): the button is not drawn and is out of
    /// the Tab order, rather than drawn and dead. A normal game keeps it, also after a slice game in the same scene.</summary>
    [Test]
    public void ASliceGameHasNoNewGameButton()
    {
        BootSlice();
        var button = NewGameButton();
        Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, button.style.display.value, "not drawn");
        Assert.IsFalse(button.focusable, "out of the Tab order");
        Assert.IsFalse(button.canGrabFocus, "out of the Tab order");

        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Begin(RealInputs, 0);   // the same scene, now a normal game
        Assert.AreNotEqual(UnityEngine.UIElements.DisplayStyle.None, button.style.display.value, "a normal game draws it");
        Assert.IsTrue(button.focusable, "a normal game keeps it in the Tab order");
    }

    [Test]
    public void ANormalGameHasItsNewGameButton()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build(RealInputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(RealInputs, 0);
        var button = NewGameButton();
        Assert.AreNotEqual(UnityEngine.UIElements.DisplayStyle.None, button.style.display.value);
        Assert.IsTrue(button.focusable);
    }

    [Test]
    public void ASliceGameWritesNoAutosave()
    {
        var root = BootSlice();
        root.Bridge!.EndTurn();
        Assert.IsFalse(File.Exists(Path.Combine(saveDir, SaveStore.AutosaveName)));
    }
}
