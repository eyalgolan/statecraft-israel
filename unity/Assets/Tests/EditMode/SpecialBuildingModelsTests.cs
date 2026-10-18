#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Statecraft.Game.Tests
{
    /// <summary>Issue #707: MATKOT's five remaining single-building models use the streamed collection path.</summary>
    public sealed class SpecialBuildingModelsTests
    {
        [Test]
        public void SourcedPlacementsHaveTheExpectedCountsAndDates()
        {
            Assert.AreEqual(0, SpecialBuildingModels.Placements(new DateTime(1948, 5, 14)).Count);
            var opening = SpecialBuildingModels.Placements(new DateTime(1948, 5, 15));
            Assert.AreEqual(1, opening.Count(x => x.Model == SpecialBuildingModels.Tegart));
            Assert.AreEqual(0, opening.Count(x => x.Model == SpecialBuildingModels.Barracks));

            var camps = SpecialBuildingModels.Placements(new DateTime(1948, 7, 13));
            Assert.AreEqual(2, camps.Count(x => x.Model == SpecialBuildingModels.Barracks), "Tzrifin and Camp 80");
            var industry = SpecialBuildingModels.Placements(new DateTime(1950, 7, 28));
            Assert.AreEqual(1, industry.Count(x => x.Model == SpecialBuildingModels.Factory), "Netanya's sourced industrial zone");
            CollectionAssert.DoesNotContain(industry.Select(x => x.Model), SpecialBuildingModels.Workshop, "no sourced workshop placement");
            CollectionAssert.DoesNotContain(industry.Select(x => x.Model), SpecialBuildingModels.Shikun, "no sourced named estate");
        }

        [Test]
        public void SeededSpotsAreDeterministicAndRespectClearance()
        {
            var anchor = new Vector2(1200f, 3400f);
            bool Free(Vector2 p, float r) => p.x > anchor.x;
            var a = SpecialBuildingModels.SeededSpot("latrun", anchor, 100f, Free);
            var b = SpecialBuildingModels.SeededSpot("latrun", anchor, 100f, Free);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a.HasValue);
            Assert.IsTrue(Free(a!.Value, 100f));
            Assert.IsNull(SpecialBuildingModels.SeededSpot("blocked", anchor, 100f, (_, __) => false));
        }

        [Test]
        public void MapYawIsComposedOnTopOfThePrefabsAxisConversion()
        {
            var own = Quaternion.Euler(-90f, 17f, 0f);
            var got = SpecialBuildingModels.Rotation(own, 35f);
            var up = got * Vector3.up;
            var want = Quaternion.AngleAxis(35f, Vector3.up) * (own * Vector3.up);
            Assert.Less(Vector3.Angle(want, up), 0.001f);
            Assert.Greater(Mathf.Abs(got.x), 0.1f, "the FBX root's -90 degree X conversion was not discarded");
        }

        [Test]
        public void ModelsDrawOnlyAtTheCloseZoomWithHysteresis()
        {
            Assert.IsTrue(SpecialBuildingModels.CloseAt(39000f, null));
            Assert.IsFalse(SpecialBuildingModels.CloseAt(41000f, null));
            Assert.IsTrue(SpecialBuildingModels.CloseAt(43000f, true));
            Assert.IsFalse(SpecialBuildingModels.CloseAt(43000f, false));
            Assert.IsFalse(SpecialBuildingModels.CloseAt(46000f, true));
        }

        // ---- on the real map, with the real prefabs ----

        Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
        readonly List<(string id, Action<GameObject?> done)> asked = new List<(string, Action<GameObject?>)>();

        [SetUp]
        public void Seam()
        {
            saved = SpecialBuildingModels.Request;
            asked.Clear();
            SpecialBuildingModels.Request = (id, detail, done) => asked.Add((id, done));
        }

        [TearDown]
        public void Restore()
        {
            SpecialBuildingModels.Request = saved;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static MapScene OpenMap()
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = UnityEngine.Object.FindFirstObjectByType<MapScene>();
            map.Build();
            return map;
        }

        void AnswerAll(bool found = true)
        {
            var pending = asked.ToList(); asked.Clear();
            foreach (var (id, done) in pending)
                done(found ? AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near)) : null);
        }

        static readonly DateTime After = new DateTime(1950, 8, 1);

        /// <summary>Each sourced model stands near its sourced point, clear of the towns' footprints (and so their labels),
        /// the routes, the unit tokens present from that day and each other, upright on the prefab's own axis conversion,
        /// on the ground, and hidden until the close zoom.</summary>
        [Test]
        public void OnTheMapEachSourcedModelStandsNearItsPlaceClearAndUpright()
        {
            var map = OpenMap();
            // The civilians first, as GameRoot shows them (#666): the buildings keep off their ground.
            var savedPeople = CivilianPeople.Request;
            CivilianPeople.Request = (id, detail, done) => { };
            CivilianPeople people;
            try { people = CivilianPeople.ShowAt(map, After, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content))!; }
            finally { CivilianPeople.Request = savedPeople; }
            Assert.Greater(people.Wanted.Count, 0);
            var props = people.Wanted.Select(w => (new Vector2(w.At.x, w.At.z), CivilianPeople.FigureClearanceM))
                .Concat(WildlifeLayer.Wants(map).Select(w => (w.At, 0.5f * WildlifeLayer.SizeM))).ToList();
            var layer = SpecialBuildingModels.ShowAt(map, After)!;
            CollectionAssert.AreEquivalent(new[] { SpecialBuildingModels.Tegart, SpecialBuildingModels.Barracks, SpecialBuildingModels.Factory },
                asked.Select(a => a.id), "one request per placed model");
            AnswerAll();
            CollectionAssert.AreEquivalent(new[] { "latrun_tegart", "tzrifin_barracks", "camp_80_barracks", "netanya_factory" }, layer.Placed.Select(p => p.Key));
            var ground = PlacementLayer.CampGround(map);
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            var wants = SpecialBuildingModels.Placements(After).ToDictionary(w => w.Key);
            var spots = new List<(Vector2, float)>();
            foreach (var p in layer.Placed)
            {
                var w = wants[p.Key]; var flat = new Vector2(p.At.x, p.At.z); float r = SpecialBuildingModels.ClearanceOf(p.Model);
                var a3 = map.Frame.ToWorld(w.Lon, w.Lat, 0);
                float off = (flat - new Vector2(a3.x, a3.z)).magnitude;
                TestContext.WriteLine($"{p.Key}: {off:0} m from its sourced point");
                float inside = SpecialBuildingModels.Inside(new Vector2(a3.x, a3.z), footprints);
                TestContext.WriteLine($"{p.Key}: {inside:0} m inside a town's footprint");
                Assert.Less(off, inside + 4f * r, p.Key + " stands near its sourced point (beyond the town's footprint)");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, footprints), p.Key + " clear of the towns");
                Assert.IsFalse(ground.OnRoute(flat, r), p.Key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, After), p.Key + " clear of the unit tokens");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, spots), p.Key + " clear of the others");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, props), p.Key + " clear of the civilians and the wildlife");
                spots.Add((flat, r));
                var go = layer.transform.Find(p.Key)!;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(p.Model, CollectionDetail.Near));
                Assert.Less(Quaternion.Angle(prefab.transform.localRotation, go.localRotation), 0.01f, p.Key + " keeps the FBX axis conversion");
                var rs = go.GetComponentsInChildren<Renderer>(true);
                var b = rs.Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.AreEqual(p.At.y, b.min.y, 1f, p.Key + " stands on the ground");
                Assert.Less(b.size.y, Mathf.Max(b.size.x, b.size.z), p.Key + " upright: lower than it is long");
                Assert.IsTrue(rs.All(x => x.forceRenderingOff), p.Key + " hidden until the close zoom");
            }
            var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
            Assert.IsNotNull(veg);
            foreach (var p in layer.Placed)
            {
                float r = SpecialBuildingModels.ClearanceOf(p.Model);
                int n = veg.Drawable().Count(t =>
                {
                    var q = t.m.GetColumn(3); float reach = r + Statecraft.Game.Map.Kits.VegetationLayer.CrownRadius(t.kind, t.m);
                    return (new Vector2(q.x, q.z) - new Vector2(p.At.x, p.At.z)).sqrMagnitude < reach * reach;
                });
                Assert.AreEqual(0, n, p.Key + ": kit trees on the building");
            }

            var cam = Camera.main!;
            cam.transform.SetPositionAndRotation(new Vector3(0f, Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM, 0f), Quaternion.Euler(90f, 0f, 0f));
            layer.Choose(default, cam);
            Assert.IsTrue(layer.GetComponentsInChildren<Renderer>(true).All(x => !x.forceRenderingOff), "drawn at the close zoom");
            cam.transform.position = new Vector3(0f, Statecraft.Game.Map.Kits.TownModelLayer.CloseExitM + 1f, 0f);
            layer.Choose(default, cam);
            Assert.IsTrue(layer.GetComponentsInChildren<Renderer>(true).All(x => x.forceRenderingOff), "hidden beyond the close zoom's exit");
        }

        /// <summary>The owner on v0.1.1 (18 October 2026): MATKOT's buildings were "way bigger then the train". The #707
        /// buildings stand at the #700 buildings' one world scale (an 8.5 m house as long as the steam train's locomotive car,
        /// about 204 m: 24x real): the fort and the factory are 3 to 4 locomotive cars long, the barracks 2.5 to 3.25, and
        /// none is longer than the whole service train (about 816 m).</summary>
        [Test]
        public void EveryModelIsSizedAgainstTheLocomotive()
        {
            float train = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize, loco = train / ServiceTrain.Cars;
            Assert.AreEqual(204f, loco, 1f, "the locomotive's car");
            Assert.AreEqual(24f, SpecialBuildingModels.WorldScale, 0.01f, "the #700 buildings' world scale");
            var map = OpenMap();
            var layer = SpecialBuildingModels.ShowAt(map, After)!;
            AnswerAll();
            var bands = new Dictionary<string, (float lo, float hi)>
            {
                [SpecialBuildingModels.Tegart] = (3f, 4f), [SpecialBuildingModels.Factory] = (3f, 4f), [SpecialBuildingModels.Barracks] = (2.5f, 3.25f),
            };
            Assert.AreEqual(4, layer.Placed.Count);
            foreach (var p in layer.Placed)
            {
                var b = layer.transform.Find(p.Key)!.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                float side = Mathf.Max(b.size.x, b.size.z), cars = side / loco;
                TestContext.WriteLine($"{p.Key} ({p.Model}): {side:0} m long, {cars:0.00} locomotive cars, {b.size.y:0} m tall");
                var (lo, hi) = bands[p.Model];
                Assert.That(cars, Is.InRange(lo, hi), p.Key + " in locomotive cars");
                Assert.Less(side, train, p.Key + " shorter than the whole train");
                Assert.Less(b.size.y, train, p.Key + " lower than the train is long");
            }
        }

        /// <summary>The models follow the game date (each from its sourced day; an earlier save shows fewer), and the layer
        /// does not rebuild on GameRoot's every refresh at one date.</summary>
        [Test]
        public void TheModelsFollowTheGameDateAndDoNotRebuildOnEveryRefresh()
        {
            var map = OpenMap();
            var layer = SpecialBuildingModels.ShowAt(map, new DateTime(1948, 5, 15))!;
            AnswerAll();
            CollectionAssert.AreEqual(new[] { "latrun_tegart" }, layer.Placed.Select(p => p.Key));
            var fort = layer.transform.Find("latrun_tegart")!.gameObject;
            SpecialBuildingModels.ShowAt(map, new DateTime(1948, 5, 15));
            Assert.IsTrue(fort != null && fort.transform.parent == layer.transform, "the same date keeps the same models");
            SpecialBuildingModels.ShowAt(map, After);
            AnswerAll();
            Assert.AreEqual(4, layer.Placed.Count);
            SpecialBuildingModels.ShowAt(map, new DateTime(1948, 5, 16));
            CollectionAssert.AreEqual(new[] { "latrun_tegart" }, layer.Placed.Select(p => p.Key), "an earlier date shows only what stood then");
            Assert.AreEqual(1, layer.transform.childCount);
        }

        [Test]
        public void ABundleThatDoesNotStreamLeavesTheMapRunning()
        {
            var map = OpenMap();
            var layer = SpecialBuildingModels.ShowAt(map, After)!;
            int n = asked.Count;
            Assert.Greater(n, 0);
            for (int i = 0; i < n; i++) LogAssert.Expect(LogType.Warning, new Regex("^BUILDINGS: .* did not stream"));
            AnswerAll(found: false);
            Assert.AreEqual(0, layer.Placed.Count);
            Assert.AreEqual(0, layer.transform.childCount);
            Assert.IsTrue(layer.FailedOpen);
            Assert.DoesNotThrow(() => SpecialBuildingModels.ShowAt(map, new DateTime(1951, 1, 1)));
            Assert.AreEqual(0, asked.Count, "a failed model is not asked for again");
            Assert.IsNotNull(map.Terrain);
        }
    }
}
