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
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #321 (T57): the units layer placed every anchor again (UnitGround.Place, about 40 ms at cpu 4) on the
/// ground the map had just placed them on. It now takes the map's spots, but only when its ground is the map's: the same
/// units (the map's own list, #560), the same footprints and route corridors (the map's), and the same hexes, q, r, lon,
/// lat and land in order. These tests first show the ground is the same on the real map, then that the spots the layer
/// takes are exactly a fresh placement on its own ground (every spot and every skip), and that a rebuilt map never hands
/// on spots for other units.</summary>
public class UnitSpotsOnceTests
{
    string saveDir = "";

    [SetUp]
    public void SetUp() => saveDir = Path.Combine(Path.GetTempPath(), "statecraft-spotsonce-" + System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
    }

    static GameInputs Inputs() => GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);

    static (MapScene map, GameRoot root) Boot(GameInputs inputs, string saveDir)
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0);
        return (map, root);
    }

    /// <summary>The ground the layer places on (UnitLayer.Ground: the map's footprints and corridors, the sim content's
    /// hexes) is the ground the map placed on (its own hexes from hexes.json): the same hexes, bit for bit, in order.</summary>
    [Test]
    public void TheLayersGroundIsTheMapsGround()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var mine = inputs.Content.Hexes; var theirs = map.UnitHexes;
        Assert.AreEqual(theirs.Count, mine.Count);
        for (int i = 0; i < mine.Count; i++)
        {
            Assert.AreEqual((theirs[i].Q, theirs[i].R, theirs[i].Land), (mine[i].Q, mine[i].R, mine[i].Land), $"hex {i}");
            Assert.AreEqual(System.BitConverter.DoubleToInt64Bits(theirs[i].Lon), System.BitConverter.DoubleToInt64Bits(mine[i].Lon), $"hex {i} lon");
            Assert.AreEqual(System.BitConverter.DoubleToInt64Bits(theirs[i].Lat), System.BitConverter.DoubleToInt64Bits(mine[i].Lat), $"hex {i} lat");
        }
        Assert.IsNotNull(map.UnitSpotsFor(map.UnitEntries, mine), "the map hands its spots to the layer's ground");
    }

    /// <summary>The booted layer takes the map's spots (no second placement) and they are a fresh placement on the layer's
    /// own ground, spot for spot and skip for skip.</summary>
    [Test]
    public void TheLayerTakesTheMapsSpotsWhichAreAFreshPlacement()
    {
        var inputs = Inputs();
        var (map, root) = Boot(inputs, saveDir);
        var layer = root.Units!;
        Assert.IsNotEmpty(map.UnitSpots);
        Assert.AreSame(map.UnitSpots, layer.Spots, "the layer took the map's spots instead of placing again");
        var skipped = new List<string>();
        var fresh = UnitLayer.Ground(map, inputs.Content.Hexes)
            .Place(layer.Units, (lon, lat) => { var w = map.Frame.ToWorld(lon, lat, 0); return new Vector2(w.x, w.z); }, skipped.Add);
        Assert.AreEqual(fresh.Count, layer.Spots.Count);
        foreach (var kv in fresh)
        {
            Assert.IsTrue(layer.Spots.TryGetValue(kv.Key, out var spot), kv.Key.Place);
            Assert.AreEqual(kv.Value, spot, kv.Key.Place);
        }
        CollectionAssert.AreEqual(skipped, UnitPlacer.SkipMessages(layer.Units, layer.Spots), "the same anchors skipped, with the same messages");
    }

    /// <summary>Review of #563, nit 3: an anchor with no clear spot is still reported when the layer takes the map's spots
    /// (it logs the map's skips as its own placement did). Golani's first anchor is moved out to sea, where no army token
    /// can stand within one hex.</summary>
    [Test]
    public void TheLayerReportsTheMapsSkippedAnchors()
    {
        var inputs = Inputs();
        const string yavneel = "    lon: 35.49944\n    lat: 32.70944\n";
        StringAssert.Contains(yavneel, inputs.Map.UnitsYaml, "Golani's first anchor, Yavne'el");
        inputs.Map.UnitsYaml = inputs.Map.UnitsYaml.Replace(yavneel, "    lon: 34.30000\n    lat: 31.90000\n");
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var skips = UnitPlacer.SkipMessages(map.UnitEntries, map.UnitSpots);
        Assert.IsNotEmpty(skips, "the map could not place the anchor at sea");
        foreach (var skip in skips) LogAssert.Expect(LogType.Warning, skip);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = new SaveStore(saveDir);
        root.Begin(inputs, 0);
        Assert.AreSame(map.UnitSpots, root.Units!.Spots, "taken from the map, so the warnings are the re-logged skips");
    }

    /// <summary>Other hexes, other units or a rebuilt map: no spots handed on, so the layer places its own.</summary>
    [Test]
    public void SpotsAreHandedOnOnlyForTheSameUnitsAndHexes()
    {
        var inputs = Inputs();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build(inputs.Map);
        var units = map.UnitEntries; var hexes = inputs.Content.Hexes;
        Assert.IsNotNull(map.UnitSpotsFor(units, hexes));
        Assert.IsNull(map.UnitSpotsFor(UnitData.Parse(inputs.Map.UnitsYaml), hexes), "a list the map did not place");
        // Each field the guard compares, changed on one hex alone (review of #563: lat, land, q, r, and the last hex, so a
        // guard that dropped a field or stopped a hex short would hand the spots on).
        List<MapHex> With(int i, System.Func<MapHex, MapHex> change)
        {
            var copy = hexes.Select(h => new MapHex { Q = h.Q, R = h.R, Lon = h.Lon, Lat = h.Lat, ElevM = h.ElevM, Land = h.Land, Biome = h.Biome }).ToList();
            copy[i] = change(copy[i]);
            return copy;
        }
        MapHex Copy(MapHex h) => new MapHex { Q = h.Q, R = h.R, Lon = h.Lon, Lat = h.Lat, ElevM = h.ElevM, Land = h.Land, Biome = h.Biome };
        int last = hexes.Count - 1;
        Assert.IsNotNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.ElevM += 10f; return c; })), "elevation is not the placer's input");
        Assert.IsNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.Lon += 1e-9; return c; })), "a hex's lon moved");
        Assert.IsNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.Lat += 1e-9; return c; })), "a hex's lat moved");
        Assert.IsNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.Land = !c.Land; return c; })), "a hex's land flipped");
        Assert.IsNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.Q += 1; return c; })), "a hex's q changed");
        Assert.IsNull(map.UnitSpotsFor(units, With(100, h => { var c = Copy(h); c.R += 1; return c; })), "a hex's r changed");
        Assert.IsNull(map.UnitSpotsFor(units, With(last, h => { var c = Copy(h); c.Lat += 1e-9; return c; })), "the last hex moved");
        Assert.IsNull(map.UnitSpotsFor(units, hexes.Take(hexes.Count - 1).ToList()), "a hex missing");
        // A rebuild from other unit text: the old list is no longer the map's, and the new spots are for the new units.
        inputs.Map.UnitsYaml = "";
        map.Build(inputs.Map);
        Assert.IsNull(map.UnitSpotsFor(units, hexes), "stale units after a rebuild");
        CollectionAssert.IsEmpty(map.UnitSpots);
    }
}
