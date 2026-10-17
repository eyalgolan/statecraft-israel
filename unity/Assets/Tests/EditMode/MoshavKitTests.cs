#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The moshav kit (#32, SettlementKits.Moshav.cs): Nahalal, the map's moshav, is laid out as Kauffmann's ring, the
/// public building at the hub and the farmsteads turned to face the ring street.</summary>
public class MoshavKitTests
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

    /// <summary>Nahalal has one public building, near its hub (within 0.65 of the radius); no other game settlement has one.</summary>
    [Test]
    public void TheMoshavHasOnePublicBuildingAtItsHub()
    {
        var map = OpenMap();
        TownCensusTests.Census(map, out var placed, out var g);
        var bad = new List<string>();
        int moshavim = 0;
        foreach (var s in map.Settlements)
        {
            var halls = placed.Where(p => p.Id == s.Id && p.Kind == "public_building").ToList();
            if (s.Type != "moshav") { if (halls.Count > 0) bad.Add($"{s.Id} ({s.Type}): {halls.Count} public buildings"); continue; }
            moshavim++;
            if (halls.Count != 1) { bad.Add($"{s.Id}: {halls.Count} public buildings"); continue; }
            var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            float t = (halls[0].C - new Vector2(w.x, w.z)).magnitude / SettlementKits.FootprintRadiusM(s, g.HexSizeM);
            TestContext.WriteLine($"{s.Id}: public building at t={t:0.00}");
            if (t > 0.65f) bad.Add($"{s.Id}: the public building stands at {t:0.00} of the radius");
        }
        Assert.AreEqual(1, moshavim, "Nahalal, the one moshav game settlement");
        CollectionAssert.IsEmpty(bad);
    }

    /// <summary>Every Nahalal house stands with its long side along the ring (its own x axis at right angles to the line from
    /// the hub, give or take the 4 degree yaw jitter: |cos| at most sin 4 degrees, 0.070), and there are at least three of them.</summary>
    [Test]
    public void TheFarmsteadsFaceTheRingStreet()
    {
        var map = OpenMap();
        TownCensusTests.Census(map, out var placed, out _);
        var s = map.Settlements.Single(x => x.Type == "moshav");
        var w = map.Frame.ToWorld(s.Lon, s.Lat, 0);
        var centre = new Vector2(w.x, w.z);
        var houses = placed.Where(p => p.Id == s.Id && p.Kind == "building").ToList();
        var bad = new List<string>();
        foreach (var h in houses)
        {
            float a = h.Yaw * Mathf.Deg2Rad;
            float dot = Mathf.Abs(Vector2.Dot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), (h.C - centre).normalized));
            TestContext.WriteLine($"{s.Id}: house at {h.C}, |cos| to the radial {dot:0.000}");
            if (dot > 0.08f) bad.Add($"house at {h.C}: its long side is {Mathf.Acos(dot) * Mathf.Rad2Deg:0} degrees off the radial, not across it");
        }
        Assert.GreaterOrEqual(houses.Count, 3, "Nahalal's farmsteads");
        CollectionAssert.IsEmpty(bad);
    }
}
