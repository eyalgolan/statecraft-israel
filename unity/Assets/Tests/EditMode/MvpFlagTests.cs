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

    [Test]
    public void ASliceGameWritesNoAutosave()
    {
        var root = BootSlice();
        root.Bridge!.EndTurn();
        Assert.IsFalse(File.Exists(Path.Combine(saveDir, SaveStore.AutosaveName)));
    }
}
