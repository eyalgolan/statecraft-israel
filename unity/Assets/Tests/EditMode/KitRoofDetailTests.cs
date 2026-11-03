#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The kit buildings' roof detail (owner, 3 November 2026: the kit buildings get real geometry, nearer promo 03's
/// model quality; SettlementKits.RoofDetail): roof parapets, Tel Aviv's set-back top storeys and the stair heads' water
/// tanks. The detail moves no building, stays inside each building's close-mesh cut, and keeps the far map's vertices
/// under <see cref="MaxGrowth"/> times the map without it.</summary>
public class KitRoofDetailTests
{
    [TearDown]
    public void Restore()
    {
        SettlementKits.RoofDetail = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
        KitScale.Reset();
    }

    /// <summary>The most the roof detail may multiply the far kit's vertices by (towns and locality tiles together).</summary>
    const float MaxGrowth = 2f;

    /// <summary>The towns the PR reports (the White City twice, the old towns twice, and Lod's village-style lattice), each of
    /// which must gain detail.</summary>
    static readonly string[] Reported = { "tel_aviv", "haifa", "jaffa", "jerusalem", "lod" };

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    /// <summary>The kits as TownCensusTests.Census builds them (the routes' and units' corridors, the 1948 localities).</summary>
    static SettlementKits.Result Build(MapScene map, out List<SettlementKits.Placed> placed)
    {
        var g = new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);
        foreach (var c in map.KitRouteCorridors) g.AddCorridor(c.line, c.halfWidth);
        foreach (var c in map.UnitClearings) g.AddCorridor(new List<Vector2> { c.centre, c.centre + Vector2.right }, c.radius);
        return SettlementKits.Build(map.Settlements, map.Localities.Select(p => p.Settlement).ToList(), new List<Localities.Area>(), g, out placed);
    }

    static void Destroy(SettlementKits.Result r)
    {
        foreach (var t in r.Towns) Object.DestroyImmediate(t.mesh);
        foreach (var t in r.Tiles) Object.DestroyImmediate(t.mesh);
        foreach (var m in r.Close.Values) Object.DestroyImmediate(m);
    }

    [Test]
    public void RoofDetailMovesNothingStaysInsideItsBuildingsAndKeepsTheBudget()
    {
        var map = OpenMap();
        SettlementKits.RoofDetail = false;
        SettlementKits.Result off; List<SettlementKits.Placed> offPlaced;
        try { off = Build(map, out offPlaced); }
        finally { SettlementKits.RoofDetail = true; }
        var on = Build(map, out var onPlaced);
        try
        {
            // It draws from each building's own stream: every building, landmark and palm is where, as large and as coloured
            // as without it.
            Assert.AreEqual(offPlaced.Count, onPlaced.Count, "census entries");
            for (int i = 0; i < offPlaced.Count; i++)
            {
                SettlementKits.Placed a = offPlaced[i], b = onPlaced[i];
                Assert.IsTrue(a.Id == b.Id && a.Kind == b.Kind && a.C == b.C && a.Yaw == b.Yaw && a.W == b.W && a.D == b.D && a.H == b.H &&
                    a.Wall.Equals(b.Wall), $"census entry {i} ({a.Id} {a.Kind}) moved");
            }
            // It lies inside each building's CutStart/CutEnd span: the close meshes (the kit without its buildings) are unchanged.
            CollectionAssert.AreEquivalent(off.Close.Keys, on.Close.Keys, "close meshes");
            foreach (var kv in off.Close) Assert.AreEqual(kv.Value.vertexCount, on.Close[kv.Key].vertexCount, kv.Key + "'s close mesh");

            var offTowns = off.Towns.ToDictionary(t => t.s.Id, t => t.mesh.vertexCount);
            var onTowns = on.Towns.ToDictionary(t => t.s.Id, t => t.mesh.vertexCount);
            foreach (var kv in offTowns) Assert.GreaterOrEqual(onTowns[kv.Key], kv.Value, kv.Key);
            foreach (var id in Reported)
            {
                TestContext.WriteLine($"ROOF_DETAIL {id} vertices {offTowns[id]} -> {onTowns[id]} (x{onTowns[id] / (float)offTowns[id]:0.00})");
                Assert.Greater(onTowns[id], offTowns[id], id + " gains roof detail");
            }
            int offTiles = off.Tiles.Sum(t => t.mesh.vertexCount), onTiles = on.Tiles.Sum(t => t.mesh.vertexCount);
            int offAll = offTowns.Values.Sum() + offTiles, onAll = onTowns.Values.Sum() + onTiles;
            TestContext.WriteLine($"ROOF_DETAIL localities (tiles) vertices {offTiles} -> {onTiles} (x{onTiles / (float)offTiles:0.00})");
            TestContext.WriteLine($"ROOF_DETAIL far map vertices {offAll} -> {onAll} (x{onAll / (float)offAll:0.00})");
            Assert.Greater(onTiles, offTiles, "the villages gain roof detail");
            Assert.LessOrEqual(onAll, MaxGrowth * offAll, "the far map's vertices");
        }
        finally { Destroy(off); Destroy(on); }
    }
}
