using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>GDD Phase 4 PR 8, item 1 (#792; plan "PR 8: Unity, the pioneers on the map"): in the GDD loop a route a
/// corridor follows is drawn by its built segments, cut at the route index's segment ranges (RouteSpan.Segments); outside
/// it every route keeps its dates.</summary>
public class RouteLayerCorridorTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);

    static List<RouteSegment> Marks(MapScene map) =>
        RouteTests.RoutesRoot(map).Find(RouteLayer.RibbonsName).GetComponentsInChildren<RouteSegment>(true).ToList();

    /// <summary>The bake's index entry for <paramref name="route"/>, as the map placed it.</summary>
    static RouteSpan BakedSpan(string route)
    {
        var inputs = Statecraft.Game.Boot.GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16z);
        var routes = RoutePaths.ReadRuntimeOrYaml(inputs.RoutesTsv, inputs.RoutesYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), Debug.LogWarning);
        var plans = RouteLayer.Plan(routes);
        var baked = AssetDatabase.LoadAssetAtPath<TextAsset>(RouteRibbonFile.AssetPath);
        var data = new TerrainData { heightmapResolution = meta.width };
        try
        {
            data.size = MapBuilder.TerrainSize(meta);
            Assert.IsTrue(RouteRibbonFile.TryRead(baked.bytes, RouteRibbonFile.Source.Of(heights, inputs.HeightmapJson, inputs.RoutesYaml),
                data.size, Vector3.zero, plans, out var read, out var why), why);
            return read.SelectMany(g => g.Routes).Single(s => s.Id == route);
        }
        finally { Object.DestroyImmediate(data); }
    }

    /// <summary>Review focus 3: with segment 1 of the Burma Road's 4 built, the map draws that segment's index range and
    /// nothing else of the route (its dated ribbon hidden, though the date is past 14 June 1948), and nothing of the other
    /// corridors' routes, none of whose segments is built.</summary>
    [Test]
    public void OnlyBuiltSegmentsDraw()
    {
        var burma = C.Corridors.Single(d => d.Id == "burma_road");
        Assert.AreEqual(4, burma.Segments.Count);
        var state = new GameState();
        state.Corridors[burma.Id] = new CorridorWork(burma.Id, new SegmentWork[] { new SegmentWork(null, 6, new DateTime(1948, 6, 30)), null, null, null });
        var span = BakedSpan(burma.Route);
        Assert.AreEqual(5, span.Segments.Length, "one start per segment and the route's end");

        var map = RouteTests.BuildMap();
        try
        {
            RouteLayer.ShowAt(map, new DateTime(1948, 7, 15), RouteLayer.BuiltSegments(C, state));
            var shown = Marks(map).Where(m => m.gameObject.activeInHierarchy).ToList();
            Assert.AreEqual(1, shown.Count, string.Join(", ", shown.Select(m => m.name)));
            Assert.AreEqual((burma.Route, 0), (shown[0].Route, shown[0].Index));
            var mesh = shown[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual((uint)(span.Segments[1] - span.Segments[0]), mesh.GetIndexCount(0), "segment 1's index range");
            Assert.Greater(mesh.GetIndexCount(0), 0u);
            Assert.Less(mesh.GetIndexCount(0), (uint)span.IndexCount, "not the whole route");
            // The four segment ribbons tile the route: their triangles add up to the route's, and each holds only the
            // cross-sections it draws (a section of Across vertices on each cut, shared by the two segments meeting there).
            var parts = Marks(map).Where(m => m.Route == burma.Route && m.Index >= 0).OrderBy(m => m.Index).ToList();
            Assert.AreEqual(4, parts.Count);
            uint sum = 0;
            foreach (var m in parts)
            {
                var part = m.GetComponent<MeshFilter>().sharedMesh;
                var count = span.Segments[m.Index + 1] - span.Segments[m.Index];
                Assert.AreEqual((uint)count, part.GetIndexCount(0), m.name);
                Assert.AreEqual((count / ((RouteLayer.Across - 1) * 6) + 1) * RouteLayer.Across, part.vertexCount, m.name);
                sum += part.GetIndexCount(0);
            }
            Assert.AreEqual((uint)span.IndexCount, sum, "the segments tile the route");
            // Every corridor route is cut, each into its corridor's segments.
            foreach (var d in C.Corridors)
                Assert.AreEqual(d.Segments.Count, Marks(map).Count(m => m.Route == d.Route && m.Index >= 0), d.Id);
            // A rebuild of the map (MapScene.RouteSegmentsBuilt) draws the corridors as the game left them.
            map.Build();
            shown = Marks(map).Where(m => m.gameObject.activeInHierarchy).ToList();
            Assert.AreEqual(1, shown.Count, "after a rebuild: " + string.Join(", ", shown.Select(m => m.name)));
            Assert.AreEqual((burma.Route, 0), (shown[0].Route, shown[0].Index), "after a rebuild");
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>The owner's review of #1312: in the GDD loop the Burma Road's convoy truck and lorry (RoadVehicles.Plans) wait
    /// until every segment of the road is built, so no vehicle drives on road that is not drawn; once it is, both run.</summary>
    [Test]
    public void BurmaRoadVehiclesWaitForTheWholeRoad()
    {
        var burma = C.Corridors.Single(d => d.Id == "burma_road");
        var convoy = new[] { RoadVehicles.SandwichConvoy, RoadVehicles.CanvasLorry };
        var saved = RoadVehicles.Request;
        var asked = new List<(string id, Action<GameObject> done)>();
        RoadVehicles.Request = (id, detail, done) => asked.Add((id, done));
        var map = RouteTests.BuildMap();
        try
        {
            map.Control = Control1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Control1948.FileName)));
            var vehicles = RouteTests.RoutesRoot(map).Find(RoadVehicles.Name).GetComponent<RoadVehicles>();
            void Answer() { foreach (var q in asked.ToArray()) q.done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(q.id, CollectionDetail.Near))); }
            var date = new DateTime(1948, 7, 15);
            var done = new SegmentWork(null, 6, new DateTime(1948, 6, 30));
            var state = new GameState();
            state.Corridors[burma.Id] = new CorridorWork(burma.Id, new SegmentWork[] { done, done, done, null });
            RouteLayer.ShowAt(map, date, RouteLayer.BuiltSegments(C, state));
            Answer();
            Assert.IsFalse(asked.Any(q => convoy.Contains(q.id)), "three of four segments built: the convoy models are not asked for");
            Assert.IsFalse(vehicles.Placed.Any(p => p.Route == burma.Route), "three of four segments built: no vehicle on the road");
            Assert.IsTrue(vehicles.Placed.Any(), "the other roads' vehicles still run");

            state.Corridors[burma.Id] = new CorridorWork(burma.Id, new SegmentWork[] { done, done, done, done });
            RouteLayer.ShowAt(map, date, RouteLayer.BuiltSegments(C, state));
            Answer();
            CollectionAssert.AreEquivalent(convoy, vehicles.Placed.Where(p => p.Route == burma.Route).Select(p => p.Model),
                "the whole road built: the convoy truck and the lorry run on it");
        }
        finally { RoadVehicles.Request = saved; Object.DestroyImmediate(map.gameObject); }
    }

    /// <summary>Outside the GDD loop (no built segments given) the corridor routes keep their dates: the Burma Road's
    /// dated ribbon from 14 June 1948, and no segment ribbon at any date.</summary>
    [Test]
    public void TheLegacyLoopDrawsByDate()
    {
        var map = RouteTests.BuildMap();
        try
        {
            var dated = Marks(map).Single(m => m.Route == "burma_road" && m.Index < 0);
            foreach (var (date, drawn) in new[] { (new DateTime(1948, 6, 13), false), (new DateTime(1948, 6, 14), true), (new DateTime(1948, 5, 15), false) })
            {
                RouteLayer.ShowAt(map, date);
                Assert.AreEqual(drawn, dated.gameObject.activeInHierarchy, $"{date:yyyy-MM-dd}");
                Assert.IsFalse(Marks(map).Any(m => m.Index >= 0 && m.gameObject.activeInHierarchy), "no segment ribbon outside the GDD loop");
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }
}
