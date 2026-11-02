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

        /// <summary>The load gate: the opening view asks for nothing; the first close camera asks once for the three props.</summary>
        [Test]
        public void ThePropsAreAskedForOnlyAtTheCloseZoomAndOnlyOnce()
        {
            var map = OpenMap();
            var layer = HexPropModels.ShowAt(map, Opening)!;
            layer.Choose(default, At(60000f));
            Assert.IsFalse(layer.Requested, "nothing streams at the opening zoom");
            CollectionAssert.IsEmpty(asked);
            layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            Assert.IsTrue(layer.Requested);
            CollectionAssert.AreEquivalent(HexPropModels.Models, asked.Select(a => a.id), "one request per prop");
            AnswerAll();
            layer.Choose(default, At(60000f)); layer.Choose(default, At(Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM));
            CollectionAssert.IsEmpty(asked, "asked once");
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
            Assert.GreaterOrEqual(layer.Placed.Count(p => p.Model == HexPropModels.PricklyPear), (int)(0.9f * villages), "nearly every village has its hedge");

            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var others = RuralLife.OtherProps(map);
            foreach (var p in map.Terrain.transform.Find(RuralLife.Name).GetComponent<RuralLife>().Placed) others.Add((new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)));
            foreach (var p in map.Terrain.transform.Find(SpecialBuildingModels.Name).GetComponent<SpecialBuildingModels>().Placed)
                others.Add((new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model)));
            var spots = new List<(Vector2, float)>();
            foreach (var p in layer.Placed)
            {
                var w = wants[p.Key]; var flat = new Vector2(p.At.x, p.At.z); float r = HexPropModels.ClearanceOf(p.Model);
                float off = (flat - w.Anchor).magnitude;
                if (p.Model == HexPropModels.Station) TestContext.WriteLine($"{p.Key}: {off:0} m from its sourced point");
                Assert.LessOrEqual(off, w.Start + HexPropModels.ReachOf(p.Model, ground.HexSizeM) + 1e-2f, p.Key + " stands beside its place");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, footprints), p.Key + " clear of every kit footprint");
                Assert.IsFalse(ground.OnRoute(flat, r), p.Key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, YarkonLink), p.Key + " clear of the unit tokens");
                Assert.IsFalse(ground.Water(flat.x, flat.y), p.Key + " on land");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, others), p.Key + " clear of the other Collection layers");
                Assert.IsTrue(Statecraft.Game.Map.Collection.TownLandmarks.Clear(flat, r, spots), p.Key + " clear of the other props");
                spots.Add((flat, r));
                var go = layer.transform.Find(p.Key)!;
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
            Assert.IsTrue(layer.GetComponentsInChildren<Renderer>(true).All(x => x.forceRenderingOff), "hidden beyond the close zoom's exit");
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
            Assert.AreEqual(0, layer.transform.childCount);
        }
    }
}
