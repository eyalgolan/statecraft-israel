#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Issue #321 (T52): content/map/units_1948.yaml was parsed twice at boot, once by the map for the unit spots
/// (MapScene.Build) and again by GameRoot for the units layer. The map now keeps its parse and hands it on: the units
/// layer gets the very list the map read (one parse a boot), with the same units as a fresh parse of the file; a layer
/// started from other unit text than the map read parses its own.</summary>
public class UnitDataOnceTests
{
    string saveDir = "";

    [SetUp]
    public void SetUp() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-unitsonce-" + System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameInputs Inputs() => GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    /// <summary>A unit as the layer and the cards read it: its fields, anchors and aircraft, as text to compare.</summary>
    static string Describe(UnitEntry u) =>
        string.Join("|", u.Id, u.Name, u.Number, u.Service, u.Formed, u.FirstCommander, u.Sector, u.WikipediaOnly, u.Status, u.Hidden,
            string.Join(",", u.Headgear ?? new List<string>()), u.Disbanded?.Date,
            string.Join(";", u.Anchors.Select(a => string.Join("/", a.Place, a.Lon.ToString("R"), a.Lat.ToString("R"), a.From, a.Since, a.To, a.EndBasis,
                a.EndNote, a.Front, a.Toward?.Place, a.PlaceHex == null ? "" : string.Join(",", a.PlaceHex), a.Precision, a.Note, a.Sources.Count))),
            string.Join(";", (u.Aircraft ?? new List<UnitAircraft>()).Select(t => t.Type + "/" + t.From + "/" + t.To)),
            u.AllSources().Count());

    [Test]
    public void TheUnitsLayerGetsTheMapsParseOfTheUnits()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0);
        Assert.IsNotNull(root.Units);
        Assert.IsNotEmpty(map.UnitEntries);
        Assert.AreSame(map.UnitEntries, root.Units!.Units, "one parse a boot: the layer's units are the map's list");
        // The same units as a fresh parse of the file, field for field.
        var fresh = UnitData.Parse(inputs.Map.UnitsYaml);
        CollectionAssert.AreEqual(fresh.Select(Describe).ToList(), root.Units.Units.Select(Describe).ToList());
    }

    [Test]
    public void OtherUnitTextIsParsedOnItsOwn()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var warnings = new List<string>();
        Assert.AreSame(map.UnitEntries, map.UnitEntriesFor(inputs.Map.UnitsYaml, warnings.Add));
        Assert.AreSame(map.UnitEntries, map.UnitEntriesFor(new string(inputs.Map.UnitsYaml.ToCharArray()), warnings.Add), "equal text, another string");
        var other = map.UnitEntriesFor(inputs.Map.UnitsYaml + "\n# edited\n", warnings.Add);
        Assert.AreNotSame(map.UnitEntries, other);
        Assert.AreEqual(map.UnitEntries.Count, other.Count);
        CollectionAssert.IsEmpty(map.UnitEntriesFor("", warnings.Add));
        Assert.AreEqual(1, warnings.Count, "the missing file's own warning, once");
    }

    /// <summary>A map that is cleared or rebuilt from other inputs never hands on a stale parse.</summary>
    [Test]
    public void ARebuildReplacesTheParse()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var first = map.UnitEntries;
        inputs.Map.UnitsYaml = "";
        map.Build(inputs.Map);
        CollectionAssert.IsEmpty(map.UnitEntries);
        Assert.AreNotSame(first, map.UnitEntries);
    }
}
