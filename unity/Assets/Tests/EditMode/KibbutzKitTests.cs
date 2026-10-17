#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The kibbutz kit (#32, SettlementKits.Kibbutz.cs): a kibbutz is told apart from a moshav or a moshava by its dining
/// hall at the heart and the farm sheds of its farm yard; the moshavim and moshavot keep their houses.</summary>
public class KibbutzKitTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    /// <summary>A kibbutz game settlement has at most one dining hall, near its heart (within 0.65 of the radius), and at least
    /// four of the five have one (a hall that finds no ground near the heart is left out, not pushed to the edge); no other game
    /// settlement has one: Nahalal (the moshav) and the moshavot have none.</summary>
    [Test]
    public void EveryKibbutzHasOneDiningHallAtItsHeart()
    {
        var map = OpenMap();
        TownCensusTests.Census(map, out var placed, out var g);
        var bad = new List<string>();
        int kibbutzim = 0, withHall = 0;
        foreach (var s in map.Settlements)
        {
            var halls = placed.Where(p => p.Id == s.Id && p.Kind == "dining_hall").ToList();
            if (s.Type != "kibbutz") { if (halls.Count > 0) bad.Add($"{s.Id} ({s.Type}): {halls.Count} dining halls"); continue; }
            kibbutzim++;
            if (halls.Count > 1) bad.Add($"{s.Id}: {halls.Count} dining halls");
            if (halls.Count != 1) { TestContext.WriteLine($"{s.Id}: no dining hall"); continue; }
            withHall++;
            var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            float t = (halls[0].C - new Vector2(w.x, w.z)).magnitude / SettlementKits.FootprintRadiusM(s, g.HexSizeM);
            TestContext.WriteLine($"{s.Id}: dining hall at t={t:0.00}");
            if (t > 0.65f) bad.Add($"{s.Id}: the dining hall stands at {t:0.00} of the radius");
        }
        Assert.AreEqual(5, kibbutzim, "the five kibbutz game settlements");
        Assert.GreaterOrEqual(withHall, 4, "kibbutzim with a dining hall");
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>Over the whole map (the game settlements and the 1948 localities): nearly every kibbutz builds its dining hall,
    /// and the farm yards hold sheds. Logs one KIBBUTZ line.</summary>
    [Test]
    public void TheKibbutzimBuildTheirHallsAndFarmSheds()
    {
        var map = OpenMap();
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
        foreach (var c in map.RouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        var localities = map.Localities.Select(p => p.Settlement).ToList();
        var built = SettlementKits.Build(map.Settlements, localities, new List<Localities.Area>(), g);
        foreach (var k in built.Towns) Object.DestroyImmediate(k.mesh);
        foreach (var t in built.Tiles) Object.DestroyImmediate(t.mesh);
        int kibbutzim = map.Settlements.Concat(localities).Count(s => s.Type == "kibbutz" && !Localities.IsMixed(s.Id));
        var line = $"KIBBUTZ kibbutzim={kibbutzim} halls={built.KibbutzHalls} sheds={built.FarmSheds}";
        TestContext.WriteLine(line); Debug.Log(line);
        Assert.LessOrEqual(built.KibbutzHalls, kibbutzim, "one dining hall per kibbutz at most");
        Assert.GreaterOrEqual(built.KibbutzHalls, kibbutzim - 2, "nearly every kibbutz builds its dining hall (6 of 8 measured)");
        Assert.GreaterOrEqual(built.FarmSheds, kibbutzim, "the farm yards hold sheds (9 measured for the 8 kibbutzim)");
    }
}
