#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Issue #321 (T57 (c)): settlements_1948.yaml was parsed at boot by the map (MapScene.Build, the markers, kits
/// and footprints) and again by GameRoot, only for the settlements' names (the info panel's id-to-name). GameRoot now takes
/// the map's list when the text is the one the map read.</summary>
public class SettlementsOnceTests
{
    string saveDir = "";

    [SetUp]
    public void SetUp() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-settlonce-" + System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameInputs Inputs() => GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    /// <summary>One parse a boot: building the map and starting the game parses the file once.</summary>
    [Test]
    public void ABootParsesTheSettlementsOnce()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        int before = SettlementPlacer.Parses;
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0);
        Assert.AreEqual(1, SettlementPlacer.Parses - before, "settlements_1948.yaml parsed once a boot");
    }

    /// <summary>The names GameRoot gets are a fresh parse's, id for id; other text is parsed on its own.</summary>
    [Test]
    public void TheMapsSettlementsGiveTheSameNames()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var shared = map.SettlementsFor(inputs.Map.SettlementsYaml);
        Assert.AreSame(map.Settlements, shared);
        Assert.AreSame(map.Settlements, map.SettlementsFor(new string(inputs.Map.SettlementsYaml.ToCharArray())), "equal text, another string");
        var fresh = SettlementPlacer.Parse(inputs.Map.SettlementsYaml);
        CollectionAssert.AreEqual(fresh.Select(s => (s.Id, s.Name)).ToList(), shared.Select(s => (s.Id, s.Name)).ToList());
        var other = map.SettlementsFor(inputs.Map.SettlementsYaml + "\n# edited\n");
        Assert.AreNotSame(map.Settlements, other);
        CollectionAssert.AreEqual(fresh.Select(s => (s.Id, s.Name)).ToList(), other.Select(s => (s.Id, s.Name)).ToList());
    }
}
