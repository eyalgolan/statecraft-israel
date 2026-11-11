#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.HexTiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using YamlDotNet.RepresentationModel;

namespace Statecraft.Game.Tests
{
    /// <summary>Tal Heuberger's hex-tile props on the main map (owner, 27 October 2026, "2+3"; 2 November 2026: "make sure all of
    /// tal's new models are used"): only at the places the content names, clear, upright, deterministic, close up only, and
    /// their bundle asked for only at the close zoom (the load gate, owner 15 October 2026).</summary>
    public sealed class HexPropModelsTests
    {
        static readonly DateTime Opening = new DateTime(1948, 5, 15), YarkonLink = new DateTime(1949, 9, 20);

        /// <summary>Every rail route's `via` waypoint in routes_1948.yaml whose name says "station", with the earliest `from` of
        /// the routes through it (a bare year is its 1 January, as RoutePaths.Bound reads it).</summary>
        static Dictionary<string, (double lon, double lat, DateTime from)> NamedStations()
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(File.ReadAllText(Path.Combine(Paths.Content, "map", "routes_1948.yaml"))));
            var routes = (YamlSequenceNode)((YamlMappingNode)yaml.Documents[0].RootNode)["routes"];
            var got = new Dictionary<string, (double, double, DateTime)>(StringComparer.Ordinal);
            string S(YamlNode n, string key) => ((YamlScalarNode)((YamlMappingNode)n)[key]).Value!;
            foreach (var r in routes.Children)
            {
                if (S(r, "kind") != "rail") continue;
                string f = S(r, "from");
                var from = f.Length == 4 ? new DateTime(int.Parse(f, CultureInfo.InvariantCulture), 1, 1)
                    : DateTime.ParseExact(f, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                // A route's `to` (none today) would end its stations' window, as RouteWindow and make_board.rail_stations read it.
                Assert.IsFalse(((YamlMappingNode)r).Children.ContainsKey(new YamlScalarNode("to")), S(r, "id") + " has an end date: give its stations a To");
                foreach (var w in ((YamlSequenceNode)((YamlMappingNode)r)["via"]).Children)
                {
                    string name = S(w, "name");
                    if (name.IndexOf("station", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    double lon = double.Parse(S(w, "lon"), CultureInfo.InvariantCulture), lat = double.Parse(S(w, "lat"), CultureInfo.InvariantCulture);
                    got[name] = got.TryGetValue(name, out var had) ? (lon, lat, had.Item3 < from ? had.Item3 : from) : (lon, lat, from);
                }
            }
            return got;
        }

        [Test]
        public void TheStationsAreExactlyTheRailwaysNamedStationsFromTheirRoutesFirstDay()
        {
            var want = NamedStations();
            Assert.AreEqual(20, want.Count, "routes_1948.yaml names 20 stations on its railways");
            CollectionAssert.AreEquivalent(want.Keys, HexPropModels.Stations.Select(s => s.Name), "no station is invented or left out");
            foreach (var s in HexPropModels.Stations)
            {
                var (lon, lat, from) = want[s.Name];
                Assert.AreEqual(lon, s.Lon, 1e-9, s.Name); Assert.AreEqual(lat, s.Lat, 1e-9, s.Name);
                Assert.AreEqual(from, s.From, s.Name + ": drawn from its route's first day");
            }
        }

        [Test]
        public void AStationIsDrawnOnlyInsideItsRoutesWindow()
        {
            var s = new HexPropModels.RailStation("x", 0, 0, new DateTime(1920, 1, 1), new DateTime(1949, 9, 1));
            Assert.IsFalse(s.DrawnOn(new DateTime(1919, 12, 31)));
            Assert.IsTrue(s.DrawnOn(new DateTime(1948, 5, 15)));
            Assert.IsFalse(s.DrawnOn(new DateTime(1949, 9, 1)), "to is exclusive, as RouteWindow.Covers");
            Assert.IsTrue(HexPropModels.Stations.All(x => x.To == null), "no route through a station has an end date today");
        }

        [Test]
        public void SizesAreTheDesignValuesAtTheOneWorldScale()
        {
            Assert.AreEqual(12f * Statecraft.Game.Map.Kits.SettlementKits.ModelScale, HexPropModels.SizeOf(HexPropModels.Station), 1e-3f);
            Assert.AreEqual(RuralLife.SizeOf(RuralLife.BedouinTent), HexPropModels.SizeOf(HexPropModels.Tent), 1e-3f, "the same tent size as RuralLife's");
            Assert.AreEqual(4f * Statecraft.Game.Map.Kits.SettlementKits.ModelScale, HexPropModels.SizeOf(HexPropModels.PricklyPear), 1e-3f);
            foreach (var id in HexPropModels.Models) Assert.IsTrue(HexProps.Has(id), id + " is one of the bundle's props");
        }

        [Test]
        public void TheSpotIsTheNearestClearRingWithinReachAndDeterministic()
        {
            var anchor = new Vector2(100f, 200f);
            bool Free(Vector2 p, float r) => (p - anchor).magnitude > 1000f;
            var a = HexPropModels.NearestSpot("x", anchor, 100f, Free, 0f, 4400f);
            Assert.AreEqual(a, HexPropModels.NearestSpot("x", anchor, 100f, Free, 0f, 4400f));
            // Rings at 150 m + 50 m steps: 1000 m is not clear, 1050 m is the first that is.
            Assert.AreEqual(1050f, (a!.Value - anchor).magnitude, 0.01f);
            Assert.IsNull(HexPropModels.NearestSpot("x", anchor, 100f, Free, 0f, 900f), "nothing beyond its reach");
            Assert.IsNull(HexPropModels.NearestSpot("x", anchor, 100f, (_, __) => false, 0f, 4400f));
            var outside = HexPropModels.NearestSpot("x", anchor, 100f, (_, __) => true, 500f, 400f);
            Assert.AreEqual(650f, (outside!.Value - anchor).magnitude, 0.01f, "from the start radius out");
        }

        // ---- on the real map, with the real prefabs ----

        Action<string, Action<GameObject?>> saved = null!;
        Action<string, CollectionDetail, Action<GameObject?>> savedPeople = null!, savedRural = null!, savedSpecial = null!;
        readonly List<(string id, Action<GameObject?> done)> asked = new List<(string, Action<GameObject?>)>();

        [SetUp]
        public void Seam()
        {
            saved = HexPropModels.Request; savedPeople = CivilianPeople.Request; savedRural = RuralLife.Request; savedSpecial = SpecialBuildingModels.Request;
            asked.Clear();
            HexPropModels.Request = (id, done) => asked.Add((id, done));
            // The layers the props keep clear of: the civilians' places without their figures (as SpecialBuildingModelsTests),
            // the rural scenes and the special buildings with the editor's prefabs, as the player's stream would answer them.
            CivilianPeople.Request = (id, detail, done) => { };
            RuralLife.Request = SpecialBuildingModels.Request = (id, detail, done) =>
                done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
        }

        [TearDown]
        public void Restore()
        {
            HexPropModels.Request = saved; CivilianPeople.Request = savedPeople; RuralLife.Request = savedRural; SpecialBuildingModels.Request = savedSpecial;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static MapScene OpenMap()
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
            map.Build();
            return map;
        }

        static Camera At(float height)
        {
            var cam = Camera.main!;
            cam.transform.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.Euler(90f, 0f, 0f));
            return cam;
        }

        void AnswerAll(bool found = true)
        {
            var pending = asked.ToList(); asked.Clear();
            foreach (var (id, done) in pending) done(found ? AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(id)) : null);
        }

        /// <summary>The map as GameRoot shows it: the layers the props keep clear of first, then the props, then a close camera.</summary>
        HexPropModels Show(MapScene map, DateTime date)
        {
            CollectionGeometryFixture.People(map, date, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Paths.Content));
            CollectionGeometryFixture.Rural(map);
            CollectionGeometryFixture.Special(map, date);
            var layer = HexPropModels.ShowAt(map, date)!;
            Assert.IsNotNull(layer);
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            return layer;
        }

        /// <summary>The load gate. The kit's minarets, belfries and clock tower draw at every zoom, so the first camera to render
        /// asks once for the props (after the first frame); the standalone props still draw only up close.</summary>
        [Test]
        public void ThePropsAreAskedForOnceByTheFirstCameraAndTheStandaloneOnesDrawOnlyUpClose()
        {
            var map = OpenMap();
            var layer = HexPropModels.ShowAt(map, Opening)!;
            Assert.IsFalse(layer.Requested, "nothing is asked for before a camera renders");
            layer.Choose(default, At(60000f));
            Assert.IsTrue(layer.Requested, "the kit has landmark parts to replace at this zoom");
            CollectionAssert.AreEquivalent(HexPropModels.Models, asked.Select(a => a.id), "one request per prop");
            AnswerAll();
            Assert.Greater(layer.Replaced.Count, 0, "the landmark models stand from the first camera");
            Assert.AreEqual(0, layer.Placed.Count, "the standalone props wait for the first close camera (#2266 review: none draws further out)");
            layer.Choose(default, At(60000f));
            Assert.AreEqual(0, layer.Placed.Count);
            HexPropModels.ShowAt(map, Opening.AddDays(1));
            Assert.AreEqual(0, layer.Placed.Count, "a date change before any close camera builds nothing either");
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            Assert.Greater(layer.Placed.Count, 0, "built at the first close camera");
            layer.Choose(default, At(60000f));
            Assert.IsTrue(layer.Placed.All(p => layer.transform.Find(p.Name).GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff)),
                "no standalone prop draws beyond the close zoom");
            CollectionAssert.IsEmpty(asked, "asked once");
        }

        /// <summary>#2266 review of the tamarisk round: the main map's props, from the first camera to the first close one, ask
        /// for the hex-props bundle alone. Never the tamarisk's (only the boards draw it; no tamarisk on the main map, #1951),
        /// and never the papyrus's (HulaPapyrusLayer asks for that one at the Hula).</summary>
        [Test]
        public void TheMainMapsPropsAskOnlyForTheHexPropsBundle()
        {
            var map = OpenMap();
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(60000f));
            var ids = asked.Select(a => a.id).ToList();
            AnswerAll();
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            ids.AddRange(asked.Select(a => a.id));
            Assert.That(ids, Is.Not.Empty, "the first camera asked for the props");
            CollectionAssert.AreEquivalent(new[] { HexProps.Bundle }, ids.Select(HexProps.Catalog.BundleOf).Distinct(),
                "the bundles the main map's props ask for: " + string.Join(", ", ids));
        }

        // ---- #2266 rulings (owner, 2 November 2026) ----

        static Transform Kits(MapScene map) => map.transform.Find(Statecraft.Game.Map.Kits.KitLayer.RootName)!;

        /// <summary>"tal's high quality models replace the built ones everytwhere": every minaret, belfry and clock tower the kit
        /// builds (the mosques of the localities and towns, the churches, Jaffa's clock tower) is a replaceable part, and once the
        /// props stream each has Tal Heuberger's model far off and up close, standing where the part stood, as tall as it, while
        /// the kit's own part is hidden.</summary>
        [Test]
        public void TalsModelsReplaceEveryKitMinaretBelfryAndClockTower()
        {
            var map = OpenMap();
            var parts = map.KitStats.LandmarkParts;
            int minarets = parts.Count(p => p.Kind == "minaret"), belfries = parts.Count(p => p.Kind == "belfry");
            TestContext.WriteLine($"parts: {minarets} minarets, {belfries} belfries, {parts.Count(p => p.Kind == "clock_tower")} clock towers, {parts.Select(p => p.Kit).Distinct().Count()} kit meshes");
            Assert.Greater(minarets, 200, "the mosques of the 1948 localities and the Arab towns");
            Assert.GreaterOrEqual(belfries, 1, "Jaffa's St Peter's and the Christian localities' churches");
            Assert.AreEqual("jaffa", parts.Single(p => p.Kind == "clock_tower").Owner, "Jaffa's clock tower");
            Assert.IsTrue(parts.Any(p => p.Kind == "belfry" && p.Owner == "jaffa"), "St Peter's belfry");
            var layer = Show(map, Opening);
            Assert.AreEqual(parts.Count, layer.Replaced.Count(r => !r.Close), "one far model per part");
            Assert.AreEqual(parts.Count(p => p.Close != null), layer.Replaced.Count(r => r.Close), "one close model per part with a close mesh");
            var kits = Kits(map);
            foreach (var kit in parts.Select(p => p.Kit).Distinct())
            {
                Assert.IsFalse(Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(kit).GetComponent<Renderer>()), kit + ": the kit's own parts are hidden far off");
                var close = kits.Find(kit + Statecraft.Game.Map.Kits.KitLayer.CloseSuffix);
                if (close != null) Assert.IsFalse(Statecraft.Game.Map.Kits.KitLayer.PartsShown(close.GetComponent<Renderer>()), kit + ": and up close");
            }
            var holder = layer.transform.Find(HexPropModels.LandmarksName);
            var ground = PlacementLayer.CampGround(map);
            foreach (var kit in parts.Select(p => p.Kit).Distinct())
                Assert.AreEqual(1, kits.Cast<Transform>().Count(t => t.name == kit), kit + ": one kit renderer by that name (they are found by name)");
            int n = 0;
            foreach (var group in parts.GroupBy(p => p.Kit))
            {
                int i = 0;
                foreach (var part in group)
                {
                    var go = holder.Find($"{part.Owner}:{part.Kind}:{i}:far"); Assert.IsNotNull(go, part.Owner + " " + part.Kind);
                    var b = go!.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                    Assert.AreEqual(part.Far.max.y, b.max.y, 0.02f * part.Far.size.y + 0.5f, part.Owner + ": as tall as the kit's part");
                    Assert.AreEqual(part.Far.center.x, b.center.x, 0.5f, part.Owner + ": where the part stood");
                    Assert.AreEqual(part.Far.center.z, b.center.z, 0.5f);
                    // On the part's own base: the roof for a village minaret or a belfry, the ground for a part with foundations.
                    Assert.AreEqual(Mathf.Max(part.Far.min.y, ground.Y(part.Far.center.x, part.Far.center.z)), b.min.y, 0.5f, part.Owner + " " + part.Kind + ": on the part's base");
                    Assert.Less(Vector3.Angle(go.up, Vector3.up), 0.01f, part.Owner + ": upright");
                    i++; n++;
                }
            }
            Assert.AreEqual(parts.Count, n);
        }

        /// <summary>Each landmark model draws exactly when its kit part would: the far one with the kit mesh, the close one with
        /// the close mesh, camera by camera (TownModelLayer's swap).</summary>
        [Test]
        public void TheLandmarkModelsFollowTheirKitRenderersSwap()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            var kits = Kits(map);
            var towns = kits.GetComponentInChildren<Statecraft.Game.Map.Kits.TownModelLayer>(true);
            var holder = layer.transform.Find(HexPropModels.LandmarksName);
            foreach (var height in new[] { 60000f, (float)CameraController.MinDistance })
            {
                var cam = At(height);
                towns.PrepareCamera(cam);
                try
                {
                    foreach (var part in map.KitStats.LandmarkParts.GroupBy(p => p.Kit).Select(g => g.First()))
                    {
                        var kit = kits.Find(part.Kit).GetComponent<Renderer>();
                        foreach (var r in holder.Find($"{part.Owner}:{part.Kind}:0:far").GetComponentsInChildren<Renderer>(true))
                            Assert.AreEqual(kit.forceRenderingOff, r.forceRenderingOff, part.Kit + " far, at " + height);
                        if (part.Close == null) continue;
                        var close = kits.Find(part.Close).GetComponent<Renderer>();
                        foreach (var r in holder.Find($"{part.Owner}:{part.Kind}:0:close").GetComponentsInChildren<Renderer>(true))
                            Assert.AreEqual(close.forceRenderingOff, r.forceRenderingOff, part.Kit + " close, at " + height);
                    }
                }
                finally { towns.RestoreRenderers(); }
            }
        }

        /// <summary>Never an empty spot: until the props answer, and for good when the bundle fails, the kit draws its own parts.</summary>
        [Test]
        public void TheKitKeepsItsOwnPartsUntilAndUnlessTalsModelsStream()
        {
            var map = OpenMap();
            var kits = Kits(map);
            var parts = map.KitStats.LandmarkParts;
            Assert.Greater(parts.Count, 0);
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(60000f));
            foreach (var kit in parts.Select(p => p.Kit).Distinct())
                Assert.IsTrue(Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(kit).GetComponent<Renderer>()), kit + ": drawn while streaming");
            foreach (var m in HexPropModels.Models) LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"HEXPROPS: {m} did not stream")));
            AnswerAll(found: false);
            Assert.AreEqual(0, layer.Replaced.Count);
            foreach (var kit in parts.Select(p => p.Kit).Distinct())
                Assert.IsTrue(Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(kit).GetComponent<Renderer>()), kit + ": drawn when the bundle fails");
            // A layer that replaced them gives them back when it goes.
            UnityEngine.Object.DestroyImmediate(layer.gameObject);
            var again = Show(map, Opening);
            Assert.Greater(again.Hidden.Count, 0);
            UnityEngine.Object.DestroyImmediate(again.gameObject);
            Assert.IsTrue(parts.Select(p => p.Kit).Distinct().All(k => Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(k).GetComponent<Renderer>())), "the kit draws its parts again");
        }

        /// <summary>The kit draws no part twice: the replaced parts are gone from the kit and close meshes and drawn only by their
        /// fallbacks; off the main map (SettlementKits.Build without replaceParts) the landmarks are whole, as before.</summary>
        [Test]
        public void ThePartsLeaveTheKitMeshesOnlyOnTheMainMap()
        {
            var map = OpenMap();
            var kits = Kits(map);
            var part = map.KitStats.LandmarkParts.First(p => p.Kind == "minaret");
            var kitMesh = kits.Find(part.Kit).GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(2, kitMesh.subMeshCount, "the parts are the kit mesh's submesh 1");
            var sub = kitMesh.GetSubMesh(1);
            Assert.Greater(sub.indexCount, 0);
            Assert.IsTrue(sub.bounds.Contains(part.Far.center), "submesh 1 holds the part");
            Assert.Less(kitMesh.GetSubMesh(0).bounds.max.y, part.Far.max.y + 1f, "submesh 0 does not reach above the part's top through it");
            var ground = PlacementLayer.CampGround(map);
            var plain = Statecraft.Game.Map.Kits.SettlementKits.Build(map.Settlements, new List<Settlement>(), new List<Statecraft.Game.Map.Kits.Localities.Area>(), ground, out _);
            CollectionAssert.IsEmpty(plain.Parts, "no replaceable parts off the main map");
        }

        /// <summary>"Hedge row of several clumps": each village's hedge is 3 to 5 clumps in a row along its edge, clear of the
        /// kit footprints, one and a quarter clumps apart.</summary>
        [Test]
        public void EachVillagesHedgeIsARowOfThreeToFiveClumpsAlongItsEdge()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var rows = layer.Placed.Where(p => p.Model == HexPropModels.PricklyPear).GroupBy(p => p.Key).ToList();
            Assert.Greater(rows.Count, 200);
            foreach (var row in rows)
            {
                var pts = row.Select(p => new Vector2(p.At.x, p.At.z)).ToList();
                Assert.AreEqual(HexPropModels.HedgeClumps(row.Key), pts.Count, row.Key);
                Assert.That(pts.Count, Is.InRange(3, 5), row.Key);
                for (int i = 1; i < pts.Count; i++)
                    Assert.AreEqual(HexPropModels.HedgeSpacingSizes * HexPropModels.SizeOf(HexPropModels.PricklyPear), (pts[i] - pts[i - 1]).magnitude, 1f, row.Key + ": evenly spaced");
                var dir = (pts[pts.Count - 1] - pts[0]).normalized;
                for (int i = 1; i < pts.Count - 1; i++) Assert.Less(Mathf.Abs(Vector3.Cross(dir, (pts[i] - pts[0]).normalized).z), 0.01f, row.Key + ": in a row");
                foreach (var p in pts) Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(p, HexPropModels.ClearanceOf(HexPropModels.PricklyPear), footprints), row.Key);
            }
            Assert.Greater(rows.Sum(r => r.Count()), 3 * 200);
        }

        /// <summary>"Sparse, close zoom only": one garrigue and one scrub per land hex of biome hills or mountains, near the
        /// point FNV-1a picks in it, on clear ground, drawn only up close; none on other ground.</summary>
        [Test]
        public void GarrigueAndScrubAreALightScatterOnOpenHillGroundOnlyUpClose()
        {
            var map = OpenMap();
            var hill = map.Hexes.hexes.Where(h => h.land && (h.biome == "hills" || h.biome == "mountains")).ToList();
            var wants = HexPropModels.Wants(map, Opening);
            Assert.AreEqual(hill.Count, wants.Count(w => w.Model == HexPropModels.Garrigue));
            Assert.AreEqual(hill.Count, wants.Count(w => w.Model == HexPropModels.Scrub));
            var hexM = map.Hexes.hex_size_km * 1000f;
            var layer = Show(map, Opening);
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var byKey = wants.ToDictionary(w => w.Key);
            var scatter = layer.Placed.Where(p => p.Model == HexPropModels.Garrigue || p.Model == HexPropModels.Scrub).ToList();
            TestContext.WriteLine($"scatter: {scatter.Count} placed of {2 * hill.Count} wanted");
            Assert.Greater(scatter.Count, hill.Count, "most hill hexes have their two plants");
            foreach (var p in scatter)
            {
                var flat = new Vector2(p.At.x, p.At.z); float r = HexPropModels.ClearanceOf(p.Model);
                var w = byKey[p.Key];
                var q = w.Key.Split(':')[1].Split(','); var h = hill.Single(x => x.q == int.Parse(q[0]) && x.r == int.Parse(q[1]));
                var c = map.Frame.ToWorld(h.lon, h.lat, 0);
                Assert.LessOrEqual((flat - new Vector2(c.x, c.z)).magnitude, HexPropModels.ScatterReachHex * hexM + HexPropModels.ReachOf(p.Model, hexM) + 1f, p.Key + ": in its hill hex");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, footprints), p.Key + " clear of the towns");
                Assert.IsFalse(ground.OnRoute(flat, r), p.Key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, Opening), p.Key + " clear of the tokens");
            }
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM + 1f));
            Assert.IsTrue(scatter.All(p => layer.transform.Find(p.Name).GetComponentsInChildren<Renderer>(true).All(x => x.forceRenderingOff)), "close zoom only");
        }

        /// <summary>Each prop stands just outside its sourced place, clear of the towns' footprints, the routes, the unit tokens,
        /// water, the other Collection layers and each other, upright on the prefab's own axes, on the ground, with no kit tree
        /// on it, and hidden beyond the close zoom.</summary>
        [Test]
        public void OnTheMapEachPropStandsBesideItsPlaceClearUprightAndOnlyUpClose()
        {
            var map = OpenMap();
            var layer = Show(map, YarkonLink);
            var wants = layer.Wanted.ToDictionary(w => w.Key);
            int camps = map.KitFootprints.Count(f => f.id.StartsWith("camp:", StringComparison.Ordinal));
            int villages = map.Localities.Select(p => p.Settlement).Where(s => s.Type == "village").Select(s => s.Id).Distinct()
                .Count(id => map.KitFootprints.Any(f => f.id == id));
            Assert.AreEqual(20, wants.Values.Count(w => w.Model == HexPropModels.Station));
            Assert.AreEqual(camps, wants.Values.Count(w => w.Model == HexPropModels.Tent), "one tent per Bedouin camp");
            Assert.AreEqual(camps, wants.Values.Select(w => w.Key).Where(k => k.StartsWith("camp:", StringComparison.Ordinal)).Distinct().Count(), "each camp keyed once");
            Assert.AreEqual(villages, wants.Values.Count(w => w.Model == HexPropModels.PricklyPear), "one hedge per Arab village");
            Assert.Greater(camps, 0); Assert.Greater(villages, 100);
            foreach (var m in HexPropModels.Models)
                TestContext.WriteLine($"{m}: {layer.Placed.Count(p => p.Model == m)} placed of {wants.Values.Count(w => w.Model == m)}");
            // A station is left out only when no clear ground lies within one hex of its point, which happens only for a station
            // inside a city's kit footprint (Jaffa, Jerusalem's Khan and Haifa East on this map).
            var footprintsAll = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var unplaced = wants.Values.Where(w => w.Model == HexPropModels.Station && layer.Placed.All(p => p.Key != w.Key)).ToList();
            foreach (var w in unplaced)
            {
                TestContext.WriteLine($"{w.Key}: not drawn, {SpecialBuildingModels.Inside(w.Anchor, footprintsAll):0} m inside a kit footprint");
                Assert.Greater(SpecialBuildingModels.Inside(w.Anchor, footprintsAll), 0f, w.Key + " is left out only inside a town's footprint");
            }
            Assert.GreaterOrEqual(20 - unplaced.Count, 17, "the stations outside the three cities all stand");
            Assert.GreaterOrEqual(layer.Placed.Count(p => p.Model == HexPropModels.Tent), (int)(0.9f * camps), "nearly every camp has its tent");
            Assert.GreaterOrEqual(layer.Placed.Where(p => p.Model == HexPropModels.PricklyPear).Select(p => p.Key).Distinct().Count(), (int)(0.9f * villages), "nearly every village has its hedge");

            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var others = RuralLife.OtherProps(map);
            foreach (var p in map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>().Placed) others.Add((new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)));
            foreach (var p in map.Terrain.transform.Find(SpecialBuildingModels.Name).GetComponent<SpecialBuildingModels>().Placed)
                others.Add((new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)));
            var spots = new List<(Vector2, float, string)>();
            foreach (var p in layer.Placed)
            {
                var w = wants[p.Key]; var flat = new Vector2(p.At.x, p.At.z); float r = HexPropModels.ClearanceOf(p.Model);
                float off = (flat - w.Anchor).magnitude;
                if (p.Model == HexPropModels.Station) TestContext.WriteLine($"{p.Key}: {off:0} m from its sourced point");
                // A hedge's clumps stand in a row about its spot (EachVillagesHedgeIsARowOfThreeToFiveClumpsAlongItsEdge).
                float row = p.Model == HexPropModels.PricklyPear ? 0.5f * 4f * HexPropModels.HedgeSpacingSizes * HexPropModels.SizeOf(p.Model) : 0f;
                Assert.LessOrEqual(off, w.Start + HexPropModels.ReachOf(p.Model, ground.HexSizeM) + row + 1e-2f, p.Key + " stands beside its place");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, footprints), p.Key + " clear of every kit footprint");
                Assert.IsFalse(ground.OnRoute(flat, r), p.Key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, YarkonLink), p.Key + " clear of the unit tokens");
                Assert.IsFalse(ground.Water(flat.x, flat.y), p.Key + " on land");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, others), p.Key + " clear of the other Collection layers");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, spots.Where(x => x.Item3 != p.Key).Select(x => (x.Item1, x.Item2)).ToList()), p.Key + " clear of the other props");
                spots.Add((flat, r, p.Key));
                var go = layer.transform.Find(p.Name)!;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(p.Model));
                Assert.Less(Vector3.Angle(go.up, prefab.transform.up), 0.01f, p.Key + " upright: turned only about the vertical");
                var rs = go.GetComponentsInChildren<Renderer>(true);
                var b = rs.Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.AreEqual(p.At.y, b.min.y, 1f, p.Key + " stands on the ground");
                // Its long side seen from above, in its own axes (the box of a turned model is wider than the model).
                float side = go.GetComponentsInChildren<MeshFilter>(true).Max(f =>
                { var s = Vector3.Scale(f.sharedMesh.bounds.size, f.transform.lossyScale); return Mathf.Max(s.x, s.z); });
                Assert.AreEqual(HexPropModels.SizeOf(p.Model), side, 0.02f * HexPropModels.SizeOf(p.Model), p.Key + " at its world size");
                Assert.IsTrue(rs.All(x => !x.forceRenderingOff), p.Key + " drawn at the close zoom");
            }
            var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
            foreach (var p in layer.Placed)
            {
                float r = HexPropModels.ClearanceOf(p.Model);
                int n = veg.Drawable().Count(t =>
                {
                    var q = t.m.GetColumn(3); float reach = r + Statecraft.Game.Map.Kits.VegetationLayer.CrownRadius(t.kind, t.m);
                    return (new Vector2(q.x, q.z) - new Vector2(p.At.x, p.At.z)).sqrMagnitude < reach * reach;
                });
                Assert.AreEqual(0, n, p.Key + ": kit trees on the prop");
            }
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM + 1f));
            Assert.IsTrue(layer.Placed.All(p => layer.transform.Find(p.Name).GetComponentsInChildren<Renderer>(true).All(x => x.forceRenderingOff)), "hidden beyond the close zoom's exit");
        }

        /// <summary>The same map and date place the same props at the same spots.</summary>
        [Test]
        public void PlacementIsDeterministic()
        {
            var map = OpenMap();
            var first = Show(map, Opening).Placed.ToList();
            UnityEngine.Object.DestroyImmediate(map.Terrain.transform.Find(HexPropModels.Name).gameObject);
            var second = Show(map, Opening).Placed.ToList();
            Assert.Greater(first.Count, 0);
            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>Absent where the content gives no place: the Yarkon link's two stations before its first day, a prickly pear
        /// at any place that is not an Arab village, a tent anywhere but a Bedouin camp; and nothing at all when the bundle
        /// fails (the map goes on).</summary>
        [Test]
        public void NothingStandsWhereTheContentGivesNoPlace()
        {
            var map = OpenMap();
            var opening = HexPropModels.Wants(map, Opening);
            CollectionAssert.DoesNotContain(opening.Select(w => w.Key), "station:Petah Tikva railway station (1921)");
            CollectionAssert.DoesNotContain(opening.Select(w => w.Key), "station:Tel Aviv North railway station");
            Assert.AreEqual(18, opening.Count(w => w.Model == HexPropModels.Station));
            Assert.AreEqual(20, HexPropModels.Wants(map, YarkonLink).Count(w => w.Model == HexPropModels.Station));
            var villages = new HashSet<string>(map.Localities.Select(p => p.Settlement).Where(s => s.Type == "village").Select(s => s.Id));
            Assert.IsTrue(opening.Where(w => w.Model == HexPropModels.PricklyPear).All(w => villages.Contains(w.Key)), "prickly pears at Arab villages only");
            foreach (var s in map.Settlements) CollectionAssert.DoesNotContain(opening.Select(w => w.Key), s.Id, s.Id + ": a game town takes no village prop");
            Assert.IsTrue(opening.Where(w => w.Model == HexPropModels.Tent).All(w => w.Key.StartsWith("camp:", StringComparison.Ordinal)), "tents at Bedouin camps only");
            Assert.IsTrue(opening.All(w => w.Model != HexPropModels.Station || w.Key.StartsWith("station:", StringComparison.Ordinal)));

            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            foreach (var m in HexPropModels.Models) LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape($"HEXPROPS: {m} did not stream")));
            AnswerAll(found: false);
            Assert.AreEqual(0, layer.Placed.Count, "a failed bundle leaves the props off the map");
            Assert.AreEqual(0, layer.transform.childCount - (layer.transform.Find(HexPropModels.LandmarksName) != null ? 1 : 0));
        }

        /// <summary>Review of #2266 (blocking): the props keep clear of where the special buildings and rural scenes WILL stand
        /// (their plans), not of what has streamed so far. With the props answered first and those layers streaming in after,
        /// nothing overlaps, and the props stand exactly where they stand when everything streamed first.</summary>
        [Test]
        public void PropsStandInTheSamePlacesWhateverStreamedFirstAndNothingLandsOnThem()
        {
            var map = OpenMap();
            var waiting = new List<(string id, CollectionDetail detail, Action<GameObject?> done)>();
            RuralLife.Request = SpecialBuildingModels.Request = (id, detail, done) => waiting.Add((id, detail, done));
            CollectionGeometryFixture.People(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Paths.Content));
            var rural = CollectionGeometryFixture.Rural(map)!;
            var special = CollectionGeometryFixture.Special(map, Opening)!;
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            Assert.AreEqual(0, rural.Placed.Count + special.Placed.Count, "the props stood before the rural scenes and the buildings streamed");
            var propsFirst = layer.Placed.Select(p => (p.Name, p.At)).ToList();
            // Others is the layer's keep-clear ground (static before #2266's review fixes, per layer after): by reflection, so
            // this test runs red against either.
            var keptClearBefore = (List<(Vector2 centre, float radius)>)typeof(HexPropModels).GetMethod("Others",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)!
                .Invoke(layer, new object[] { map, Opening })!;
            for (int round = 0; round < 10 && waiting.Count > 0; round++)
            {
                var now = waiting.ToList(); waiting.Clear();
                foreach (var (id, detail, done) in now) done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
            }
            Assert.Greater(rural.Placed.Count, 0); Assert.Greater(special.Placed.Count, 0);
            // What the props kept clear of before anything streamed already held every spot those layers then took.
            foreach (var p in rural.Placed.Where(x => x.Member == 0).Select(x => (x.Key, x.At)).Concat(special.Placed.Select(x => (x.Key, x.At))))
                Assert.IsTrue(keptClearBefore.Any(d => (d.centre - new Vector2(p.At.x, p.At.z)).magnitude < 1f),
                    p.Key + ": its planned spot was kept clear before it streamed");
            var theirs = rural.Placed.Select(p => (new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)))
                .Concat(special.Placed.Select(p => (new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)))).ToList();
            foreach (var p in layer.Placed)
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model), theirs),
                    p.Name + ": no rural scene or building streamed in on it");
            UnityEngine.Object.DestroyImmediate(layer.gameObject);
            var after = HexPropModels.ShowAt(map, Opening)!;
            after.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            CollectionAssert.AreEqual(propsFirst, after.Placed.Select(p => (p.Name, p.At)).ToList(), "the same places in either order");
        }

        /// <summary>#1701 slice F with #2266: the props keep clear of the b21 planned at each built arms workshop (gdd-0.3), as of
        /// the other special buildings' plan. Workshops stand on the hex nearest each prop's anchor, the props' own ground.</summary>
        [Test]
        public void PropsKeepClearOfTheBuiltArmsWorkshops()
        {
            var map = OpenMap();
            var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
            foreach (var era in c.Eras) { era.Systems.Ruleset = Statecraft.Sim.SaveGame.Gdd03Ruleset; era.Systems.Loop = Statecraft.Sim.EraSystems.Gdd; }
            var land = c.Hexes.Where(h => h.Land).Select(h => (h, at: map.Frame.ToWorld(h.Lon, h.Lat, 0))).ToList();
            var hexes = HexPropModels.Wants(map, Opening)
                .Select(w => land.OrderBy(x => (new Vector2(x.at.x, x.at.z) - w.Anchor).sqrMagnitude).First().h).Distinct().ToList();
            var projects = hexes.Select((h, i) => new Statecraft.Sim.CityProject(300 + i, Statecraft.Sim.Workshops.ArmsWorkshop, "rehovot", h.Q, h.R, 0, Opening)).ToList();
            CollectionGeometryFixture.People(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), c);
            CollectionGeometryFixture.Rural(map);
            var special = CollectionGeometryFixture.Special(map, Opening, c, projects)!;
            var layer = HexPropModels.ShowAt(map, Opening, c, projects)!;
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            var b21 = special.Placed.Where(p => p.Model == SpecialBuildingModels.Workshop)
                .Select(p => (new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model))).ToList();
            Assert.Greater(b21.Count, 0, "workshops stand by the props' places");
            Assert.Greater(layer.Placed.Count, 0);
            var kept = layer.Others(map, Opening);
            foreach (var (at, _) in b21)
                Assert.IsTrue(kept.Any(d => (d.centre - at).magnitude < 1f), $"the workshop at {at} is in the props' planned keep-clear ground");
            foreach (var p in layer.Placed)
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model), b21),
                    p.Name + ": no prop stands on a workshop");
        }

        /// <summary>Review of #2281's merge with #2266 (C3): the hex props and the workshops' b21 stand on ONE rural plan, also
        /// after it changes. All three layers are shown as GameRoot shows them, with a b21 wanted at each farm scene; then the
        /// ground the rural scenes keep clear of
        /// changes so that a farm scene moves and a b21 with it (both asserted as the premise), the rural layer is shown again,
        /// and GameRoot's next refresh (the same date and workshops) rebuilds both consumers: every b21 is drawn where the new
        /// plan puts it, and no prop stands on one.</summary>
        [Test]
        public void ARuralPlanChangeRebuildsTheWorkshopsAndThePropsOntoTheNewPlan()
        {
            var map = OpenMap();
            var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
            foreach (var era in c.Eras) { era.Systems.Ruleset = Statecraft.Sim.SaveGame.Gdd03Ruleset; era.Systems.Loop = Statecraft.Sim.EraSystems.Gdd; }
            CollectionGeometryFixture.People(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), c);
            var rural = CollectionGeometryFixture.Rural(map)!;
            var before = RuralLife.PlanOf(map);
            int version = RuralLife.VersionOf(map);
            Assert.Greater(before.Count, 0);
            Assert.AreSame(before, RuralLife.PlanOf(map), "the plan is computed once for what was shown");
            // A workshop's b21 wanted right at each farm scene's planned spot (the contested ground), passed as GameRoot passes
            // the state's workshops (ShowAt(map, date, workshops)).
            var workshops = before.Select((f, i) =>
            {
                var (lon, lat) = map.Frame.ToLonLat(new Vector3(f.spot.x, 0f, f.spot.y));
                // At b21's drawn scale, as ArmsWorkshops' rows (#2876), so its ground is the one ClearanceOf(Workshop) reads.
                return new SpecialBuildingModels.Want(SpecialBuildingModels.WorkshopKey(400 + i), SpecialBuildingModels.Workshop, Opening, null, lon, lat, "test: at a farm scene",
                    SpecialBuildingModels.Siting.Seeded, "", SpecialBuildingModels.WorkshopScale);
            }).ToList();
            var buildings = SpecialBuildingModels.Placements(Opening).Concat(workshops).ToList();
            Dictionary<string, Vector2> B21(IReadOnlyList<(RuralLife.Want want, Vector2 spot)> plan) =>
                SpecialBuildingModels.Plan(map, Opening, buildings, _ => true, log: false, plan)
                    .Where(x => x.want.Model == SpecialBuildingModels.Workshop).ToDictionary(x => x.want.Key, x => x.at);
            var oldB21 = B21(before);
            // The change: other ground on one farm scene's planned spot, the first that moves a b21 when its scene moves.
            List<(Vector2 centre, float radius)>? nextOthers = null; List<(RuralLife.Want want, Vector2 spot)>? after = null;
            foreach (var (_, spot) in before)
            {
                var others = new List<(Vector2 centre, float radius)>(rural.OthersShown) { (spot, 50f) };
                var plan = RuralLife.Plan(map, rural.Wanted, others, _ => true);
                var moved = B21(plan);
                if (moved.Count == oldB21.Count && moved.All(kv => oldB21.TryGetValue(kv.Key, out var o) && (o - kv.Value).sqrMagnitude < 1e-6f)) continue;
                nextOthers = others; after = plan; break;
            }
            Assert.IsNotNull(nextOthers, "premise: some change of the farm scenes' ground moves a b21");
            var special = CollectionGeometryFixture.Special(map, Opening, workshops)!;
            var props = HexPropModels.ShowAt(map, Opening, workshops)!;
            props.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            Dictionary<string, Vector2> Drawn() => special.Placed.Where(p => p.Model == SpecialBuildingModels.Workshop)
                .ToDictionary(p => p.Key, p => new Vector2(p.At.x, p.At.z));
            foreach (var kv in Drawn()) Assert.Less((oldB21[kv.Key] - kv.Value).magnitude, 1e-3f, kv.Key + " drawn on the shown plan");

            // The ground changes; the rural layer is shown again (GameRoot shows it on every refresh), then the next refresh.
            rural.ShowForTest(rural.Wanted.ToList(), nextOthers!);
            Assert.AreNotEqual(version, RuralLife.VersionOf(map), "the plan's version moved");
            var now = RuralLife.PlanOf(map);
            Assert.IsFalse(now.SequenceEqual(before), "premise: the shown plan changed");
            Assert.IsTrue(now.SequenceEqual(after!), "the new plan is the one the changed ground gives");
            CollectionGeometryFixture.Special(map, Opening, workshops);
            HexPropModels.ShowAt(map, Opening, workshops);
            AnswerAll();
            var newB21 = B21(now);
            var drawn = Drawn();
            CollectionAssert.AreEquivalent(newB21.Keys, drawn.Keys, "the workshops drawn are the new plan's");
            foreach (var kv in drawn) Assert.Less((newB21[kv.Key] - kv.Value).magnitude, 1e-3f, kv.Key + " rebuilt onto the new plan");
            var discs = drawn.Values.Select(at => (at, SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop))).ToList();
            foreach (var p in props.Placed)
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model), discs),
                    p.Name + ": no prop stands on a b21 drawn on the new plan");
            var kept = props.Others(map, Opening);
            foreach (var at in drawn.Values)
                Assert.IsTrue(kept.Any(d => (d.centre - at).magnitude < 1f), $"the b21 at {at} is in the props' keep-clear ground");
        }

        /// <summary>#2274's cap (SpecialBuildingModels.MaxPropOffset): a prop whose nearest clear spot is past it is not drawn, so the
        /// hex props keep no ground for it. With the cap lowered to 1 m, none of the props of 15 May 1948 keeps a disc, and the
        /// plan places none of them.</summary>
        [Test]
        public void HexPropsKeepNoGroundForAPropPastTheCap()
        {
            var map = OpenMap();
            var layer = HexPropModels.ShowAt(map, Opening)!;
            var uncapped = SpecialBuildingModels.Plan(map, Opening, SpecialBuildingModels.Placements(Opening), _ => true, log: false)
                .Where(x => x.want.TownEdge).ToList();
            Assert.AreEqual(3, uncapped.Count, "premise: the well, the crates and the wall on 15 May 1948");
            var saved = SpecialBuildingModels.MaxPropOffset;
            SpecialBuildingModels.MaxPropOffset = 1f;
            try
            {
                var kept = layer.Others(map, Opening);
                foreach (var (w, at) in uncapped)
                    Assert.IsFalse(kept.Any(d => (d.centre - at).magnitude < 1f && Mathf.Abs(d.radius - SpecialBuildingModels.ClearanceOf(w)) < 0.01f),
                        w.Key + ": no ground kept for a prop the cap leaves undrawn");
                CollectionAssert.IsEmpty(SpecialBuildingModels.Plan(map, Opening, SpecialBuildingModels.Placements(Opening), _ => true, log: false)
                    .Where(x => x.want.TownEdge).ToList(), "the plan places no prop past the cap");
            }
            finally { SpecialBuildingModels.MaxPropOffset = saved; }
        }

        sealed class Lease : IDisposable { public void Dispose() { } }

        /// <summary>Review of #2266's delta, C1, on today's map: with one model withheld and the rest streamed in later, every
        /// rural scene and special building stands at its spot in the plan with every model available, and none lands on a hex
        /// prop. On today's map no single model's absence moves another scene or building, so this test cannot fail on its own
        /// for C1 (it passes on the old code too): it guards the real map's ordering and overlap. C1 itself is covered by the
        /// constructed case, AStreamedModelStandsAtItsFullPlanSpotWhenAnEarlierOneHasNotStreamed (the farm scenes; #2281 fixes
        /// SpecialBuildingModels.Plan the same way).</summary>
        [Test]
        public void ScenesAndBuildingsStandAtTheirFullPlanSpotsWhateverHasStreamed()
        {
            var map = OpenMap();
            CollectionGeometryFixture.People(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Paths.Content));
            var ruralWants = RuralLife.Wants(map); var ruralOthers = RuralLife.OtherProps(map);
            var full = RuralLife.Plan(map, ruralWants, ruralOthers, _ => true).ToDictionary(x => x.want.Key + "|" + x.want.Model, x => x.spot);
            // On today's map no single rural model's absence moves another scene (the constructed case shows the flaw); the
            // first model is withheld here.
            string withheld = RuralLife.Models[0];
            var waiting = new List<(string id, CollectionDetail detail, Action<GameObject?> done)>();
            RuralLife.Request = SpecialBuildingModels.Request = (id, detail, done) => waiting.Add((id, detail, done));
            var rural = CollectionGeometryFixture.Rural(map)!;
            var special = CollectionGeometryFixture.Special(map, Opening)!;
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            AnswerAll();
            var props = layer.Placed.Select(p => (new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model))).ToList();
            void Answer(Func<string, bool> which)
            {
                for (int round = 0; round < 10; round++)
                {
                    var now = waiting.Where(x => which(x.id)).ToList(); if (now.Count == 0) return;
                    foreach (var x in now) { waiting.Remove(x); x.done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(x.id, x.detail))); }
                }
            }
            void Check(string stage)
            {
                foreach (var p in rural.Placed.Where(x => x.Member == 0))
                {
                    Assert.IsTrue(full.TryGetValue(p.Key + "|" + p.Model, out var f), stage + ": " + p.Key + " " + p.Model + " is in the full plan");
                    Assert.Less((f - new Vector2(p.At.x, p.At.z)).magnitude, 1f, stage + ": " + p.Key + " " + p.Model + " at its full-plan spot");
                }
                var plan = SpecialBuildingModels.Plan(map, Opening, SpecialBuildingModels.Placements(Opening), _ => true, log: false).ToDictionary(x => x.want.Key, x => x.at);
                foreach (var p in special.Placed) Assert.Less((plan[p.Key] - new Vector2(p.At.x, p.At.z)).magnitude, 1f, stage + ": " + p.Key + " at its full-plan spot");
                var theirs = rural.Placed.Select(p => (new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)))
                    .Concat(special.Placed.Select(p => (new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)))).ToList();
                foreach (var (c, r) in props) Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(c, r, theirs), stage + ": a prop at " + c + " is landed on");
            }
            // Everything but the withheld model (and, for the buildings, the barracks) streams; then the rest, in reverse order.
            Answer(id => id != withheld && id != SpecialBuildingModels.Barracks);
            Assert.Greater(rural.Placed.Count, 0);
            Check("a subset streamed");
            Answer(id => true);
            Check("everything streamed");
        }

        /// <summary>Review of #2266's delta, C1 (the farm scenes; #2281 fixes SpecialBuildingModels.Plan the same way), constructed:
        /// two wants on one anchor, keyed so that the second stands
        /// elsewhere in the plan with the first model than in the plan without it. Only the second model streams; it must stand
        /// at its full-plan spot (where the hex props keep clear), not where the first one's absence would let it.</summary>
        [Test]
        public void AStreamedModelStandsAtItsFullPlanSpotWhenAnEarlierOneHasNotStreamed()
        {
            var map = OpenMap();
            CollectionGeometryFixture.People(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Paths.Content));
            var waiting = new List<(string id, CollectionDetail detail, Action<GameObject?> done)>();
            RuralLife.Request = SpecialBuildingModels.Request = (id, detail, done) => waiting.Add((id, detail, done));
            void Answer(string model)
            {
                for (int round = 0; round < 10; round++)
                {
                    var now = waiting.Where(x => x.id == model).ToList(); if (now.Count == 0) return;
                    foreach (var x in now) { waiting.Remove(x); x.done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(x.id, x.detail))); }
                }
            }
            // Rural: a sheep on a planned scene's anchor at its first ring, and a donkey on the same anchor keyed so that its
            // first candidate (RuralLife.Spot: FNV-1a of model and key picks the bearing) is the sheep's spot: in the plan with
            // the sheep it must go elsewhere, alone (the old greedy subset plan) it would stand there.
            var others = RuralLife.OtherProps(map);
            var real = RuralLife.Plan(map, RuralLife.Wants(map), others, _ => true);
            List<RuralLife.Want>? pair = null; Vector2 fullSpot = default, subSpot = default;
            foreach (var (seed, _) in real)
            {
                for (int k = 0; k < 64 && pair == null; k++)
                {
                    var a = new RuralLife.Want("c1:" + k, RuralLife.Sheep, seed.Anchor, seed.AnchorRadius);
                    var alone = RuralLife.Plan(map, new[] { a }, others, _ => true);
                    if (alone.Count != 1) continue;
                    var off = alone[0].spot - seed.Anchor;
                    if (Mathf.Abs(off.magnitude - (seed.AnchorRadius + RuralLife.ClearanceRadiusM * RuralLife.FirstRingClearances)) > 1f) continue;
                    int bearing = ((int)Mathf.Round(Mathf.Atan2(off.y, off.x) * Mathf.Rad2Deg / (360f / RuralLife.Bearings)) % RuralLife.Bearings + RuralLife.Bearings) % RuralLife.Bearings;
                    for (int j = 0; j < 256 && pair == null; j++)
                    {
                        var b = new RuralLife.Want("c1b:" + k + ":" + j, RuralLife.Donkey, seed.Anchor, seed.AnchorRadius);
                        if (RuralLife.StableHash(b.Model + ":" + b.Key) % (uint)RuralLife.Bearings != bearing) continue;
                        var full = RuralLife.Plan(map, new[] { a, b }, others, _ => true);
                        var sub = RuralLife.Plan(map, new[] { b }, others, _ => true);
                        if (full.Count == 2 && sub.Count == 1 && (full[1].spot - sub[0].spot).sqrMagnitude > 1f)
                        { pair = new List<RuralLife.Want> { a, b }; fullSpot = full[1].spot; subSpot = sub[0].spot; }
                    }
                }
                if (pair != null) break;
            }
            var rural = CollectionGeometryFixture.Rural(map)!;
            typeof(RuralLife).GetMethod("Show", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(rural, new object[] { pair!, others });
            Answer(RuralLife.Donkey);
            var donkey = rural.Placed.Where(p => p.Model == RuralLife.Donkey && p.Key == pair![1].Key).ToList();
            Assert.IsNotEmpty(donkey, "the donkey streamed and stands");
            Assert.Less((new Vector2(donkey[0].At.x, donkey[0].At.z) - fullSpot).magnitude, 1f,
                $"the donkey at its full-plan spot {fullSpot}, not {subSpot} where the absent sheep's ground would put it");
        }

        /// <summary>Review of #2266 at f9802bed6, C1: a standalone prop stands in the same place whichever props have streamed,
        /// in whatever order, and whichever failed: every want takes its ground, and only the streamed ones are drawn.</summary>
        [Test]
        public void StandalonePropsStandInTheSamePlacesWhateverStreamedOrFailed()
        {
            var map = OpenMap();
            List<(string Name, string Model, Vector3 At)> Run(Func<string, bool> answered, bool reverse)
            {
                var old = map.Terrain.transform.Find(HexPropModels.Name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                asked.Clear();
                var layer = HexPropModels.ShowAt(map, Opening)!;
                layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
                var pending = asked.ToList(); asked.Clear();
                if (reverse) pending.Reverse();
                foreach (var (id, done) in pending) done(answered(id) ? AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(id)) : null);
                return layer.Placed.Select(p => (p.Name, p.Model, p.At)).ToList();
            }
            var all = Run(_ => true, reverse: false);
            Assert.Greater(all.Count, 1000);
            int moved = 0;
            foreach (var missing in HexPropModels.Standalone)
            {
                var some = Run(id => id != missing, reverse: true);
                var expected = all.Where(p => p.Model != missing).ToList();
                CollectionAssert.AreEquivalent(expected.Select(p => p.Name), some.Select(p => p.Name), missing + " failed: the same props stand");
                var at = expected.ToDictionary(p => p.Name, p => p.At);
                foreach (var p in some) if ((at[p.Name] - p.At).magnitude >= 1f) moved++;
                TestContext.WriteLine($"{missing} failed: {some.Count} props, {some.Count(p => (at[p.Name] - p.At).magnitude >= 1f)} moved");
            }
            Assert.AreEqual(0, moved, "no prop moves when another model is missing or the answers come in another order");
        }

        /// <summary>Review of #2266 at f9802bed6, C2: the ChooseCore render-mode branch does not build an immediate answer;
        /// the request waits for LateUpdate.</summary>
        [Test]
        public void RenderModeDefersHexPropsUntilLateUpdate()
        {
            var map = OpenMap();
            HexPropModels.Request = (id, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(id)));   // answers at once
            var layer = HexPropModels.ShowAt(map, Opening)!;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(HexPropModels).GetMethod("ChooseCore", flags)!.Invoke(layer, new object[] { At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM), true });
            Assert.AreEqual(0, layer.Replaced.Count, "no landmark model instantiated inside the render");
            Assert.AreEqual(0, layer.Placed.Count, "no standalone prop instantiated inside the render");
            typeof(HexPropModels).GetMethod("LateUpdate", flags)!.Invoke(layer, null);
            Assert.Greater(layer.Replaced.Count, 0, "built after the render");
            Assert.Greater(layer.Placed.Count, 0);
        }

        /// <summary>Review of #2266 at f9802bed6, C3: the close switch writes the standalone renderers only when it changes,
        /// not for every camera every frame.</summary>
        [Test]
        public void TheCloseSwitchWritesOnlyAChange()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            var r = layer.transform.Find(layer.Placed[0].Name).GetComponentInChildren<Renderer>();
            Assert.IsFalse(r.forceRenderingOff, "drawn up close");
            r.forceRenderingOff = true;   // a marker: an unchanged switch must not touch it
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            Assert.IsTrue(r.forceRenderingOff, "still close: nothing written");
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM + 1f));
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            Assert.IsFalse(r.forceRenderingOff, "a change writes every renderer");
        }

        /// <summary>Review of #2266 at f9802bed6, C6: the kits are built only by MapScene.Build, which first destroys the map's
        /// children, this layer and Tal Heuberger's models with them. After a rebuild no model is left over, and the new kit
        /// draws its own parts until a new layer replaces them.</summary>
        [Test]
        public void AMapRebuildLeavesNoModelBehindAndTheKitWhole()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            Assert.Greater(layer.Replaced.Count, 0);
            map.Build();
            Assert.IsTrue(layer == null, "the layer went with the old map");
            Assert.AreEqual(0, UnityEngine.Object.FindObjectsByType<HexPropModels>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.AreEqual(0, UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(t => t.name.EndsWith(":far", StringComparison.Ordinal) || t.name.EndsWith(":close", StringComparison.Ordinal)), "no landmark model left over");
            var kits = Kits(map);
            Assert.IsTrue(map.KitStats.LandmarkParts.Select(p => p.Kit).Distinct()
                .All(k => Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(k).GetComponent<Renderer>())), "the new kit draws its own parts");
        }

        /// <summary>Review of #2266's delta, C2: a disabled layer hides Tal Heuberger's models under it, so the kit draws its own
        /// parts again; enabled again, the models stand in for them. Never an empty spot.</summary>
        [Test]
        public void ADisabledLayerGivesTheKitItsPartsBack()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            var kits = Kits(map);
            var kitNames = map.KitStats.LandmarkParts.Select(p => p.Kit).Distinct().ToList();
            Assert.IsTrue(kitNames.All(k => !Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(k).GetComponent<Renderer>())));
            layer.gameObject.SetActive(false);
            Assert.IsTrue(kitNames.All(k => Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(k).GetComponent<Renderer>())), "the kit's own parts while the models are off");
            layer.gameObject.SetActive(true);
            Assert.IsTrue(kitNames.All(k => !Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(k).GetComponent<Renderer>())), "the models stand in again");
        }

        /// <summary>Review of #2266's delta, C3: an answer that never comes holds back only what needs it. With the belfry and
        /// the garrigue never answered, every kit mesh without a belfry has Tal Heuberger's models and the other standalone
        /// props stand; the kit meshes with a belfry keep their own parts.</summary>
        [Test]
        public void AnAnswerThatNeverComesHoldsBackOnlyWhatNeedsIt()
        {
            var map = OpenMap();
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            var pending = asked.ToList(); asked.Clear();
            foreach (var (id, done) in pending)
                if (id != HexPropModels.Belfry && id != HexPropModels.Garrigue) done(AssetDatabase.LoadAssetAtPath<GameObject>(HexProps.PrefabPath(id)));
            var parts = map.KitStats.LandmarkParts;
            var withBelfry = new HashSet<string>(parts.Where(p => p.Kind == "belfry").Select(p => p.Kit));
            var kits = Kits(map);
            foreach (var kit in parts.Select(p => p.Kit).Distinct())
                Assert.AreEqual(withBelfry.Contains(kit), Statecraft.Game.Map.Kits.KitLayer.PartsShown(kits.Find(kit).GetComponent<Renderer>()),
                    kit + (withBelfry.Contains(kit) ? ": keeps its own parts (its belfry never came)" : ": Tal's models stand"));
            Assert.Greater(layer.Placed.Count(p => p.Model == HexPropModels.Station), 0, "the stations stand");
            Assert.Greater(layer.Placed.Count(p => p.Model == HexPropModels.PricklyPear), 0, "the hedges stand");
            Assert.AreEqual(0, layer.Placed.Count(p => p.Model == HexPropModels.Garrigue));
        }

        /// <summary>Review of #2266 with #2271: no standalone prop stands under a whole-town model (TownLandmarks), whose square
        /// reaches its core's radius times the square root of two from the core, past the kit footprint's edge for a core off
        /// the centre. The models are answered at once here; the props plan the seats, so the order does not matter.</summary>
        [Test]
        public void NoPropStandsUnderAWholeTownModel()
        {
            var map = OpenMap();
            var savedR = TownLandmarks.Request; var savedA = TownLandmarks.Acquire;
            try
            {
                GameObject? Prefab(string id, CollectionDetail d) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, d));
                TownLandmarks.Request = (id, d, done) => done(Prefab(id, d));
                TownLandmarks.Acquire = (id, d, done) => { done(Prefab(id, d)); return new Lease(); };
                var towns = TownLandmarks.ShowAt(map, Opening, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Paths.Content))!;
                towns.Choose(default, At(60000f));
                var layer = Show(map, Opening);
                Assert.Greater(towns.Seats.Count, 10, "the whole towns stand");
                int checkedProps = 0;
                foreach (var kv in towns.Seats)
                {
                    float reach = 0.5f * kv.Value.Edge * Mathf.Sqrt(2f);
                    foreach (var p in layer.Placed)
                    {
                        float d = (new Vector2(p.At.x, p.At.z) - kv.Value.Centre).magnitude;
                        Assert.GreaterOrEqual(d + 1f, reach + HexPropModels.ClearanceOf(p.Model), p.Name + " under " + kv.Key + "'s whole-town model");
                        checkedProps++;
                    }
                }
                Assert.Greater(checkedProps, 0);
            }
            finally { TownLandmarks.Request = savedR; TownLandmarks.Acquire = savedA; }
        }

        /// <summary>Review of #2266: the load each zoom band adds, capped (the design budget the PR measured; see its load
        /// table). Far bands: no standalone prop, and the landmark models in view under the cap. Close views: the standalone
        /// props and landmark models in view under the cap. Counted with each camera's kit swap applied.</summary>
        [Test]
        public void EachZoomBandStaysInsideItsPropBudget()
        {
            var map = OpenMap();
            var layer = Show(map, Opening);
            var towns = Kits(map).GetComponentInChildren<Statecraft.Game.Map.Kits.TownModelLayer>(true);
            var rig = Camera.main!.GetComponent<CameraRig>();
            var planes = new Plane[6];
            int Tris(Renderer r) => r.GetComponent<MeshFilter>() is MeshFilter f && f.sharedMesh != null ? (int)(f.sharedMesh.triangles.Length / 3) : 0;
            var views = new List<PromoPose>(PromoPose.All); views.AddRange(PromoPose.CloseUps); views.AddRange(PromoPose.TownChecks);
            foreach (var pose in views)
            {
                pose.Apply(rig, map.Frame);
                var cam = Camera.main!;
                layer.Choose(default, cam); towns.PrepareCamera(cam);
                try
                {
                    GeometryUtility.CalculateFrustumPlanes(cam, planes);
                    bool Seen(Renderer r) => r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, r.bounds);
                    var standalone = layer.Placed.SelectMany(p => layer.transform.Find(p.Name).GetComponentsInChildren<Renderer>(true)).Where(Seen).ToList();
                    var holder = layer.transform.Find(HexPropModels.LandmarksName);
                    Assert.IsNotNull(holder, "the landmark models stand");
                    var landmarks = holder.GetComponentsInChildren<Renderer>(true).Where(Seen).ToList();
                    bool close = pose.Distance <= Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM;
                    TestContext.WriteLine($"BUDGET {pose.Name} ({pose.Distance:0} m): standalone {standalone.Count} / {standalone.Sum(Tris)} tris, landmarks {landmarks.Count} / {landmarks.Sum(Tris)} tris");
                    if (!close) Assert.AreEqual(0, standalone.Count, pose.Name + ": no standalone prop beyond the close zoom");
                    Assert.LessOrEqual(standalone.Count, MaxStandaloneInView, pose.Name);
                    Assert.LessOrEqual(standalone.Sum(Tris), MaxStandaloneTrisInView, pose.Name);
                    Assert.LessOrEqual(landmarks.Sum(Tris), MaxLandmarkTrisInView, pose.Name);
                }
                finally { towns.RestoreRenderers(); }
            }
        }

        // The budget (design values, set from the PR's measurements with headroom; a delta that needs more must say so).
        const int MaxStandaloneInView = 400, MaxStandaloneTrisInView = 250000, MaxLandmarkTrisInView = 900000;

        // ---- #2905 (suspect 2): ShowAt's early-out keys on what the build reads, not on the raw date (as #3068 for the
        // special buildings) ----

        static int Tokens(MapScene map, DateTime date) => map.UnitTokens.Count(t => t.until > date);

        static Dictionary<string, GameObject> DrawnObjects(HexPropModels layer) =>
            layer.Placed.Select(p => p.Name).ToDictionary(n => n, n => layer.transform.Find(n)!.gameObject);

        /// <summary>RED on main (#2905): a new game date that changes nothing the build reads (the same stations, the same
        /// special-building rows, the same dated tokens, the same civilians' list, the same rural plan) does not re-plan the
        /// props. 15 August to 15 September 1949: after the 1948 rows leave and the last dated token's window ends (20 July
        /// 1949), before the Yarkon link's stations (20 September 1949); the civilians' list is the same object (as
        /// SpecialBuildingModelsTests' #3056 case). Each premise is asserted.</summary>
        [TestCase("he-IL")] [TestCase("en-US")]
        public void ADateChangeThatChangesNothingTheBuildReadsSkipsTheRebuild(string culture)
        {
            var savedCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var map = OpenMap();
                var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
                var none = Array.Empty<Statecraft.Sim.Placement>();
                var d1 = new DateTime(1949, 8, 15); var d2 = new DateTime(1949, 9, 15);
                Assert.IsTrue(SpecialBuildingModels.Same(SpecialBuildingModels.Placements(d1), SpecialBuildingModels.Placements(d2)), "premise: the same rows");
                Assert.AreEqual(Tokens(map, d1), Tokens(map, d2), "premise: the same dated tokens");
                Assert.IsTrue(HexPropModels.Wants(map, d1).SequenceEqual(HexPropModels.Wants(map, d2)), "premise: the same stations");
                var layer = Show(map, d1);
                var people = map.Terrain.transform.Find(CivilianPeople.Name)!.GetComponent<CivilianPeople>();
                var list = people.Wanted; int rural = RuralLife.VersionOf(map);
                Assert.IsNull(map.Terrain.transform.Find(TownLandmarks.Name), "premise: no whole-town plan is shown");
                var before = DrawnObjects(layer);
                Assert.Greater(before.Count, 0, "premise: props are drawn");
                int replans = layer.Replans;

                CollectionGeometryFixture.People(map, d2, none, c);
                Assert.IsTrue(ReferenceEquals(list, people.Wanted), "premise: the civilians' list is the same (CivilianPeople swaps it only on a change)");
                CollectionGeometryFixture.Rural(map);
                Assert.AreEqual(rural, RuralLife.VersionOf(map), "premise: the same rural plan");
                CollectionGeometryFixture.Special(map, d2);
                HexPropModels.ShowAt(map, d2);
                Assert.AreEqual(replans, layer.Replans, "the new date changes nothing the build reads: no re-plan");
                foreach (var kv in before)
                    Assert.IsTrue(ReferenceEquals(kv.Value, layer.transform.Find(kv.Key)?.gameObject), kv.Key + " is the same object");
                Assert.AreEqual(before.Count, layer.Placed.Count, "no prop added or removed");
            }
            finally { CultureInfo.CurrentCulture = savedCulture; }
        }

        /// <summary>RED on main (#2905): the civilians' list changing at the same date re-plans the props, with no rural plan
        /// shown (RuralLife.VersionOf stays 0 and cannot carry the change): two housing projects being built add their builders
        /// (as SpecialBuildingModelsTests' #3056 case), whose ground the props keep clear of (RuralLife.OtherProps) and the
        /// special buildings' plan does too. Then no prop stands on a figure's ground.</summary>
        [TestCase("he-IL")] [TestCase("en-US")]
        public void TheCiviliansListChangingAtTheSameDateRebuildsWithNoRuralLifeShown(string culture)
        {
            var savedCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var map = OpenMap();
                var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
                var none = Array.Empty<Statecraft.Sim.Placement>();
                var d = new DateTime(1948, 6, 15);
                var people = CollectionGeometryFixture.People(map, d, none, c)!;
                CollectionGeometryFixture.Special(map, d);
                var layer = HexPropModels.ShowAt(map, d)!;
                layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
                AnswerAll();
                Assert.AreEqual(0, RuralLife.VersionOf(map), "premise: no rural plan is shown, so its version cannot carry the change");
                Assert.Greater(layer.Placed.Count, 0, "premise: props are drawn");
                var old = people.Wanted.ToList();
                int replans = layer.Replans;

                var projects = new[] { "hadera", "rehovot" }.Select((city, i) =>
                {
                    var h = c.SettlementHexes.Single(x => x.Id == city);
                    return new Statecraft.Sim.CityProject(i + 1, "housing", city, h.Q, h.R, 2, null);
                }).ToList();
                CollectionGeometryFixture.People(map, d, none, c, null, projects);
                Assert.IsFalse(old.SequenceEqual(people.Wanted), "premise: the civilians' list changed (the builders)");
                CollectionGeometryFixture.Special(map, d);
                HexPropModels.ShowAt(map, d);
                Assert.AreEqual(replans + 1, layer.Replans, "the civilians' list changed at the same date: one re-plan");
                var figures = people.Wanted.Select(p => (new Vector2(p.At.x, p.At.z), CivilianPeople.FigureClearanceM)).ToList();
                foreach (var p in layer.Placed)
                    Assert.IsTrue(TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model), figures),
                        p.Name + ": no prop stands on a figure's ground");
            }
            finally { CultureInfo.CurrentCulture = savedCulture; }
        }

        /// <summary>A station's first day re-plans with its station (the dated stations follow their RouteWindow): 19 to 20
        /// September 1949, the Yarkon link's stations, with the same special-building rows and dated tokens.</summary>
        [Test]
        public void AStationsFirstDayRebuildsWithItsStation()
        {
            var map = OpenMap();
            var d2 = YarkonLink; var d1 = d2.AddDays(-1);
            Assert.IsTrue(SpecialBuildingModels.Same(SpecialBuildingModels.Placements(d1), SpecialBuildingModels.Placements(d2)), "premise: the same rows");
            Assert.AreEqual(Tokens(map, d1), Tokens(map, d2), "premise: the same dated tokens");
            Assert.Greater(HexPropModels.Wants(map, d2).Count, HexPropModels.Wants(map, d1).Count, "premise: a station opens");
            var layer = Show(map, d1);
            var people = map.Terrain.transform.Find(CivilianPeople.Name)!.GetComponent<CivilianPeople>();
            var list = people.Wanted;
            int replans = layer.Replans;
            var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
            CollectionGeometryFixture.People(map, d2, Array.Empty<Statecraft.Sim.Placement>(), c);
            Assert.IsTrue(ReferenceEquals(list, people.Wanted), "premise: the civilians' list is the same (CivilianPeople swaps it only on a change)");
            CollectionGeometryFixture.Rural(map);
            CollectionGeometryFixture.Special(map, d2);
            HexPropModels.ShowAt(map, d2);
            Assert.AreEqual(replans + 1, layer.Replans, "a station opened: one re-plan");
            Assert.IsTrue(layer.Wanted.SequenceEqual(HexPropModels.Wants(map, d2)), "the props wanted are the new date's");
        }

        /// <summary>A dated token's window ending re-plans the props (they keep clear of the tokens still present, KitGround.OnToken,
        /// and so does the special buildings' plan they keep clear of): the day before the first window that ends after the 1948
        /// rows leave, then that day, with the same stations. Then no prop stands on a token still present.</summary>
        [Test]
        public void ATokenWindowEndingRebuildsThePropsOntoTheNewGround()
        {
            var map = OpenMap();
            var ends = map.UnitTokens.Select(t => t.until).Where(u => u > SpecialBuildingModels.Year1948To && u < YarkonLink).OrderBy(u => u).ToList();
            Assert.Greater(ends.Count, 0, "premise: a dated token's window ends after the 1948 rows and before the Yarkon link");
            var d2 = ends[0]; var d1 = d2.AddDays(-1);
            Assert.IsTrue(SpecialBuildingModels.Same(SpecialBuildingModels.Placements(d1), SpecialBuildingModels.Placements(d2)), "premise: the same rows");
            Assert.IsTrue(HexPropModels.Wants(map, d1).SequenceEqual(HexPropModels.Wants(map, d2)), "premise: the same stations");
            Assert.AreNotEqual(Tokens(map, d1), Tokens(map, d2), "premise: a dated token leaves");
            var layer = Show(map, d1);
            var people = map.Terrain.transform.Find(CivilianPeople.Name)!.GetComponent<CivilianPeople>();
            var list = people.Wanted;
            int replans = layer.Replans;
            var c = Statecraft.Sim.ContentLoader.Load(Paths.Content);
            CollectionGeometryFixture.People(map, d2, Array.Empty<Statecraft.Sim.Placement>(), c);
            Assert.IsTrue(ReferenceEquals(list, people.Wanted), "premise: the civilians' list is the same (CivilianPeople swaps it only on a change)");
            CollectionGeometryFixture.Rural(map);
            CollectionGeometryFixture.Special(map, d2);
            HexPropModels.ShowAt(map, d2);
            Assert.AreEqual(replans + 1, layer.Replans, "a token's window ended: one re-plan");
            var ground = PlacementLayer.CampGround(map);
            foreach (var p in layer.Placed)
                Assert.IsFalse(ground.OnToken(new Vector2(p.At.x, p.At.z), HexPropModels.ClearanceOf(p.Model), d2), p.Name + ": on no token still present");
        }
    }
}
