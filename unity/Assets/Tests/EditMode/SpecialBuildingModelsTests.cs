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
    /// <summary>Issue #707: Tal Heuberger's five remaining single-building models use the streamed collection path.</summary>
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
            CollectionAssert.DoesNotContain(SpecialBuildingModels.Placements(new DateTime(1950, 12, 31)).Select(x => x.Model),
                SpecialBuildingModels.Shikun, "Ramat HaHayal starts after the sourced year 1950");
            Assert.AreEqual(1, SpecialBuildingModels.Placements(new DateTime(1951, 1, 1))
                .Count(x => x.Model == SpecialBuildingModels.Shikun), "Ramat HaHayal on the owner-ruled year-level date");
            // Degania's tank from the day of the Syrian attack, 20 May 1948 (owner, 27 October 2026: "Degania landmark").
            CollectionAssert.DoesNotContain(SpecialBuildingModels.Placements(new DateTime(1948, 5, 19)).Select(x => x.Model), SpecialBuildingModels.SyrianR35);
            var tank = SpecialBuildingModels.Placements(new DateTime(1948, 5, 20)).Single(x => x.Model == SpecialBuildingModels.SyrianR35);
            Assert.AreEqual("degania_r35", tank.Key);
            Assert.AreEqual(35.5747, tank.Lon, 1e-9, "Degania Alef's row in settlements_1948.yaml");
            Assert.AreEqual(32.7081, tank.Lat, 1e-9);
            StringAssert.Contains("degania_defence.yaml", tank.Source);
            Assert.AreEqual(4.02f, SpecialBuildingModels.RealLengthM(SpecialBuildingModels.SyrianR35), 1e-6f, "the R35's length, en.wiki Renault R35 (research_notes/Map/degania-r35.md)");
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

        static readonly DateTime After = new DateTime(1951, 1, 1);

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
            try { people = CollectionGeometryFixture.People(map, After, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content))!; }
            finally { CivilianPeople.Request = savedPeople; }
            Assert.Greater(people.Wanted.Count, 0);
            var props = people.Wanted.Select(w => (new Vector2(w.At.x, w.At.z), CivilianPeople.FigureClearanceM))
                .Concat(WildlifeLayer.Wants(map).Select(w => (w.At, 0.5f * WildlifeLayer.SizeM))).ToList();
            var layer = CollectionGeometryFixture.Special(map, After)!;
            CollectionAssert.AreEquivalent(new[] { SpecialBuildingModels.Tegart, SpecialBuildingModels.Barracks, SpecialBuildingModels.Factory, SpecialBuildingModels.Shikun,
                SpecialBuildingModels.SyrianR35 }, asked.Select(a => a.id), "one request per placed model");
            AnswerAll();
            CollectionAssert.AreEquivalent(new[] { "latrun_tegart", "tzrifin_barracks", "camp_80_barracks", "netanya_factory", "ramat_hahayal_shikun", "degania_r35" }, layer.Placed.Select(p => p.Key));
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
                // Degania's R35 (#2046) is not placed on SeededSpot's ring beyond the footprint: RouteFacingEdgeSpot walks Degania
                // Alef's drawn village box widened by its clearance, from the side facing the nearest route, to the first spot clear
                // of the towns' footprints, the routes and the tokens (the edge is pinned by
                // DeganiaTankStandsJustOutsideEveryVillageKitBoundAtTheTransportSide). A point on that box's edge is no farther from
                // the sourced point than the widened box's farthest corner, and the ring's bound does not hold for it: on 4.4 km
                // hexes (#1183) Degania's footprint shrinks with the hex, the spot 75 degrees off the route bearing misses its
                // clearance by about 1 m, and the R35 takes the next, 90 degrees off, 1445 m out (1396 m at 75 degrees on 5 km hexes).
                if (p.Key == "degania_r35")
                {
                    var village = SpecialBuildingModels.KitBounds(map.transform.Find(Statecraft.Game.Map.Kits.KitLayer.RootName)!, "degania_alef")!.Value;
                    float reach = 0f;
                    foreach (var cx in new[] { village.min.x - r, village.max.x + r })
                        foreach (var cz in new[] { village.min.z - r, village.max.z + r })
                            reach = Mathf.Max(reach, (new Vector2(cx, cz) - new Vector2(a3.x, a3.z)).magnitude);
                    TestContext.WriteLine($"{p.Key}: its widened village box reaches {reach:0} m from its sourced point");
                    Assert.Less(off, reach, p.Key + " stands near its sourced point (on the edge of its widened village box)");
                }
                else Assert.Less(off, inside + 4f * r, p.Key + " stands near its sourced point (beyond the town's footprint)");
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

        /// <summary>The R35 stands just outside Degania Alef's drawn kit, on the side facing the nearest mapped route: its centre
        /// is on the kit's box widened by one model-clearance on each side (one clearance out from a side, up to the square root
        /// of two clearances off a corner). The same spot with the "kits" toggle off when it is placed: the box comes from the
        /// kit meshes, not from their (then disabled) renderers.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void DeganiaTankStandsJustOutsideEveryVillageKitBoundAtTheTransportSide(bool kitsShown)
        {
            var map = OpenMap();
            // Placement runs inside the fixture; record what it logs so a seeded fallback cannot pass unnoticed.
            var logged = new List<string>();
            Application.LogCallback watch = (message, stack, type) => logged.Add(message);
            Application.logMessageReceived += watch;
            SpecialBuildingModels layer;
            if (!kitsShown) VisualToggles.Set("kits", false);
            try { layer = CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 20))!; AnswerAll(); }
            finally { Application.logMessageReceived -= watch; if (!kitsShown) VisualToggles.Set("kits", true); }
            Assert.IsFalse(logged.Any(m => m.Contains("route-facing spot unavailable for degania_r35")),
                "the R35 took its route-facing village spot, not the seeded fallback");
            var tank = layer.Placed.Single(p => p.Key == "degania_r35");
            var kits = map.transform.Find(Statecraft.Game.Map.Kits.KitLayer.RootName)!;
            var bounds = kits.Cast<Transform>()
                .Where(t => t.name == "degania_alef" || t.name == "degania_alef" + Statecraft.Game.Map.Kits.KitLayer.CloseSuffix)
                .Select(t => t.GetComponent<Renderer>().bounds).ToArray();
            Assert.IsNotEmpty(bounds, "Degania's far and close kit meshes define its drawn village bounds");
            var at = tank.At; float clearance = SpecialBuildingModels.ClearanceOf(tank.Model);
            // A box's gaps to the R35's centre on the ground plane, x and z (the tank's height is the terrain's, not the kit box's).
            (float x, float z) Gaps(Bounds b) => (Mathf.Max(b.min.x - at.x, at.x - b.max.x, 0f), Mathf.Max(b.min.z - at.z, at.z - b.max.z, 0f));
            foreach (var b in bounds) Assert.Greater(Mathf.Max(Gaps(b).x, Gaps(b).z), 0f, "the R35 centre lies outside " + b);
            var village = bounds[0]; for (int i = 1; i < bounds.Length; i++) village.Encapsulate(bounds[i]);
            const float FloatSlackM = 1e-3f;   // the spot is computed on the widened box's edge; 1 mm absorbs float rounding
            // On the widened box's edge: its larger axis gap to the village box (x or z) is exactly one clearance.
            var (gapX, gapZ) = Gaps(village);
            Assert.AreEqual(clearance, Mathf.Max(gapX, gapZ), FloatSlackM, "the R35's centre is on the village box widened by its clearance");
            Assert.That(Mathf.Sqrt(gapX * gapX + gapZ * gapZ), Is.InRange(clearance - FloatSlackM, Mathf.Sqrt(2f) * clearance + FloatSlackM),
                "one clearance beyond a side of the village, up to the square root of two clearances beyond a corner");
            var footprint = map.KitFootprints.Single(f => f.id == "degania_alef");
            Vector2 nearest = Vector2.zero; float best = float.MaxValue;
            foreach (var (line, _) in map.KitRouteCorridors)
                for (int i = 0; i + 1 < line.Count; i++)
                {
                    var a = line[i]; var ab = line[i + 1] - a;
                    float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(footprint.centre - a, ab) / ab.sqrMagnitude) : 0f;
                    var q = a + ab * t; float d = (q - footprint.centre).sqrMagnitude;
                    // As the placement does: a segment through the centre gives no bearing.
                    if (d < best && d > 1e-4f) { best = d; nearest = q; }
                }
            // The candidates sweep the facing half-plane, its +/-90 degree edges included.
            Assert.GreaterOrEqual(Vector2.Dot((new Vector2(at.x, at.z) - footprint.centre).normalized, (nearest - footprint.centre).normalized), -1e-4f,
                "the chosen village edge is on the side facing the nearest mapped transport segment");
        }

        /// <summary>The owner on v0.1.1 (18 October 2026): Tal Heuberger's buildings were "way bigger then the train". The #707
        /// buildings stand at the #700 buildings' one world scale (an 8.5 m house as long as the steam train's locomotive car,
        /// about 204 m: 24x real): the fort, the factory and the shikun are 3 to 4 locomotive cars long, the barracks 2.5 to 3.25, and
        /// none is longer than the whole service train (about 816 m).</summary>
        [Test]
        public void EveryModelIsSizedAgainstTheLocomotive()
        {
            float train = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize, loco = train / ServiceTrain.Cars;
            Assert.AreEqual(204f, loco, 1f, "the locomotive's car");
            Assert.AreEqual(24f, SpecialBuildingModels.WorldScale, 0.01f, "the #700 buildings' world scale");
            var map = OpenMap();
            var layer = CollectionGeometryFixture.Special(map, After)!;
            AnswerAll();
            var bands = new Dictionary<string, (float lo, float hi)>
            {
                [SpecialBuildingModels.Tegart] = (3f, 4f), [SpecialBuildingModels.Factory] = (3f, 4f), [SpecialBuildingModels.Barracks] = (2.5f, 3.25f),
                [SpecialBuildingModels.Shikun] = (3f, 4f),
                // The R35, 4.02 m at the same world scale: about 96 m, under half a locomotive car.
                [SpecialBuildingModels.SyrianR35] = (0.42f, 0.52f),
            };
            Assert.AreEqual(6, layer.Placed.Count);
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
            var layer = CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 15))!;
            AnswerAll();
            CollectionAssert.AreEqual(new[] { "latrun_tegart" }, layer.Placed.Select(p => p.Key));
            var fort = layer.transform.Find("latrun_tegart")!.gameObject;
            CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 15));
            Assert.IsTrue(fort != null && fort.transform.parent == layer.transform, "the same date keeps the same models");
            CollectionGeometryFixture.Special(map, After);
            AnswerAll();
            Assert.AreEqual(6, layer.Placed.Count);
            CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 16));
            AnswerAll(); // Earlier-date retirement reacquires its still-needed original through the fake source.
            CollectionAssert.AreEqual(new[] { "latrun_tegart" }, layer.Placed.Select(p => p.Key), "an earlier date shows only what stood then");
            Assert.AreEqual(1, layer.transform.childCount);
        }

        // ---- #1701 slice F (owner, 27 October 2026: "Arms workshops (Recommended)"): b21 on each built arms workshop, gdd-0.3 only ----

        /// <summary>The real content, fresh (the shared copy is never changed), with every era under <paramref name="ruleset"/>.</summary>
        static Statecraft.Sim.Content Rules(string ruleset)
        {
            var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
            foreach (var era in c.Eras) { era.Systems.Ruleset = ruleset; era.Systems.Loop = Statecraft.Sim.EraSystems.Gdd; }
            return c;
        }

        static readonly DateTime Built = new DateTime(1949, 3, 1);

        /// <summary>City project <paramref name="id"/>: an arms workshop on <paramref name="city"/>'s own hex, built on
        /// <paramref name="builtOn"/> or (null) still being built.</summary>
        static Statecraft.Sim.CityProject Workshop(Statecraft.Sim.Content c, int id, string city, DateTime? builtOn)
        {
            var h = c.SettlementHexes.Single(x => x.Id == city);
            return new Statecraft.Sim.CityProject(id, Statecraft.Sim.Workshops.ArmsWorkshop, city, h.Q, h.R, builtOn == null ? 2 : 0, builtOn);
        }

        [Test]
        public void ArmsWorkshopsAreWantedUnderGdd03AtBuiltWorkshopsOnly()
        {
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var netanya = c.SettlementHexes.Single(x => x.Id == "netanya");
            var projects = new[]
            {
                Workshop(c, 9, "rehovot", Built), Workshop(c, 8, "hadera", null),
                new Statecraft.Sim.CityProject(7, "housing", "netanya", netanya.Q, netanya.R, 0, Built),
            };
            var wants = SpecialBuildingModels.ArmsWorkshops(c, projects);
            Assert.AreEqual(1, wants.Count, "one model per BUILT arms workshop: none for the one being built, none for housing");
            var w = wants[0]; var hex = c.HexAt(projects[0].Q, projects[0].R)!;
            Assert.AreEqual(("arms_workshop_9", SpecialBuildingModels.Workshop, Built), (w.Key, w.Model, w.From));
            Assert.AreEqual((hex.Lon, hex.Lat), (w.Lon, w.Lat), "anchored on the project's own hex");
            Assert.IsEmpty(SpecialBuildingModels.ArmsWorkshops(Rules(Statecraft.Sim.SaveGame.GddRuleset), projects), "never under gdd-0.1");
        }

        /// <summary>A built arms workshop on each city's own hex (the hardest ground: the town's footprint is there): every one
        /// but those of the two cities whose footprint covers the whole hex gets its b21, wholly on that hex, clear of the towns' footprints (and so their labels), the routes, the unit tokens,
        /// the civilians, the wildlife, the farm scenes and every other building model, upright, on the ground, hidden until the
        /// close zoom, and on the same spot when placed again.</summary>
        [Test]
        public void OnTheMapEachBuiltWorkshopStandsOnItsHexClearAndUpright()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var cities = c.Cities.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.Greater(cities.Count, 5);
            var projects = cities.Select((city, i) => Workshop(c, 100 + i, city, Built)).ToList();
            var savedPeople = CivilianPeople.Request; var savedRural = RuralLife.Request;
            CivilianPeople.Request = (id, detail, done) => { };
            RuralLife.Request = (id, detail, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
            CivilianPeople people; RuralLife rural;
            try
            {
                people = CollectionGeometryFixture.People(map, After, Array.Empty<Statecraft.Sim.Placement>(), c)!;
                rural = CollectionGeometryFixture.Rural(map)!;
            }
            finally { CivilianPeople.Request = savedPeople; RuralLife.Request = savedRural; }
            Assert.Greater(rural.Placed.Count, 0, "the farm scenes stand first, as GameRoot shows them");
            var props = people.Wanted.Select(w => (new Vector2(w.At.x, w.At.z), CivilianPeople.FigureClearanceM))
                .Concat(WildlifeLayer.Wants(map).Select(w => (w.At, 0.5f * WildlifeLayer.SizeM)))
                .Concat(rural.Placed.Select(p => (new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model)))).ToList();
            var layer = CollectionGeometryFixture.Special(map, After, c, projects)!;
            Assert.AreEqual(1, asked.Count(a => a.id == SpecialBuildingModels.Workshop), "b21 is asked for once, whatever the number of workshops");
            AnswerAll();
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            float r0 = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            // A town whose footprint covers its own hex (Jerusalem and Tel Aviv: footprints of several hexes, FootprintRadiusM)
            // leaves no ground there clear of it: the first ring of the search already leaves the hex, so no b21 is drawn
            // (logged), rather than one standing in the town's houses.
            var covered = new List<string>();
            foreach (var project in projects)
            {
                var hex = c.HexAt(project.Q, project.R)!; var w3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                float inside = SpecialBuildingModels.Inside(new Vector2(w3.x, w3.z), footprints);
                TestContext.WriteLine($"{project.City}: hex centre {inside:0} m inside a town's footprint");
                if (inside > 0f && inside + CivilianPeople.LabelClearanceM + 1.5f * r0 + r0 > SpecialBuildingModels.HexInnerRadiusM) covered.Add(project.City);
            }
            CollectionAssert.AreEquivalent(new[] { "jerusalem", "tel_aviv" }, covered, "the cities whose footprint covers their own hex");
            projects = projects.Where(p => !covered.Contains(p.City)).ToList();
            var keys = projects.Select(p => SpecialBuildingModels.WorkshopKey(p.Id)).ToList();
            CollectionAssert.IsSubsetOf(keys, layer.Placed.Select(p => p.Key), "a b21 at every other built workshop");
            Assert.AreEqual(keys.Count, layer.Placed.Count(p => p.Model == SpecialBuildingModels.Workshop), "none for the covered cities");
            var ground = PlacementLayer.CampGround(map);
            var others = layer.Placed.Where(p => !keys.Contains(p.Key)).Select(p => (new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model))).ToList();
            var spots = new List<(Vector2, float)>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(SpecialBuildingModels.Workshop, CollectionDetail.Near));
            foreach (var project in projects)
            {
                var key = SpecialBuildingModels.WorkshopKey(project.Id);
                var p = layer.Placed.Single(x => x.Key == key);
                Assert.AreEqual(SpecialBuildingModels.Workshop, p.Model);
                var hex = c.HexAt(project.Q, project.R)!; var centre3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                var flat = new Vector2(p.At.x, p.At.z); float r = SpecialBuildingModels.ClearanceOf(p.Model);
                float off = (flat - new Vector2(centre3.x, centre3.z)).magnitude;
                TestContext.WriteLine($"{key} ({project.City}): {off:0} m from its hex centre, clearance {r:0} m");
                Assert.LessOrEqual(off + r, SpecialBuildingModels.HexInnerRadiusM, key + " wholly on its project's hex");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, footprints), key + " clear of the towns (and their labels)");
                Assert.IsFalse(ground.OnRoute(flat, r), key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, After), key + " clear of the unit tokens");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, others), key + " clear of the sourced building models");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, spots), key + " clear of the other workshops");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, props), key + " clear of the civilians, the wildlife and the farm scenes");
                spots.Add((flat, r));
                var go = layer.transform.Find(key)!;
                Assert.Less(Quaternion.Angle(prefab.transform.localRotation, go.localRotation), 0.01f, key + " keeps the FBX axis conversion");
                var rs = go.GetComponentsInChildren<Renderer>(true);
                var b = rs.Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.AreEqual(p.At.y, b.min.y, 1f, key + " stands on the ground");
                Assert.Less(b.size.y, Mathf.Max(b.size.x, b.size.z), key + " upright: lower than it is long");
                Assert.AreEqual(SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), Mathf.Max(b.size.x, b.size.z), 1f, key + " at the buildings' one world scale");
                Assert.IsTrue(rs.All(x => x.forceRenderingOff), key + " hidden until the close zoom");
            }
            // Deterministic: a second layer from nothing puts every workshop on the same spot.
            var first = layer.Placed.Where(p => keys.Contains(p.Key)).ToDictionary(p => p.Key, p => p.At);
            UnityEngine.Object.DestroyImmediate(layer.gameObject);
            var again = CollectionGeometryFixture.Special(map, After, c, cities.Select((city, i) => Workshop(c, 100 + i, city, Built)).ToList())!;
            AnswerAll();
            foreach (var key in keys) Assert.AreEqual(first[key], again.Placed.Single(p => p.Key == key).At, key + " on the same spot again");
        }

        /// <summary>The model stands from the turn the build completes (not while it is being built), stays put on GameRoot's
        /// refreshes at one state, leaves with its project, and never stands under gdd-0.1.</summary>
        [Test]
        public void TheWorkshopStandsFromItsCompletionAndLeavesWithItsProject()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            string key = SpecialBuildingModels.WorkshopKey(9);
            var layer = CollectionGeometryFixture.Special(map, Built, c, new[] { Workshop(c, 9, "rehovot", null) })!;
            AnswerAll();
            CollectionAssert.DoesNotContain(layer.Placed.Select(p => p.Key), key, "not while it is being built");
            Assert.IsNull(layer.transform.Find(key));
            // Built on this turn's date: the same date, a different state, so the layer redraws.
            var built = new[] { Workshop(c, 9, "rehovot", Built) };
            CollectionGeometryFixture.Special(map, Built, c, built);
            AnswerAll();
            CollectionAssert.Contains(layer.Placed.Select(p => p.Key), key, "from the turn the build completes");
            var model = layer.transform.Find(key)!.gameObject;
            CollectionGeometryFixture.Special(map, Built, c, new[] { Workshop(c, 9, "rehovot", Built) });
            Assert.IsTrue(model != null && model.transform.parent == layer.transform, "the same state keeps the same model");
            CollectionGeometryFixture.Special(map, Built, c, Array.Empty<Statecraft.Sim.CityProject>());
            AnswerAll();
            CollectionAssert.DoesNotContain(layer.Placed.Select(p => p.Key), key, "gone with its project");
            Assert.IsNull(layer.transform.Find(key));
            CollectionGeometryFixture.Special(map, Built.AddMonths(1), Rules(Statecraft.Sim.SaveGame.GddRuleset), built);
            AnswerAll();
            CollectionAssert.DoesNotContain(layer.Placed.Select(p => p.Model), SpecialBuildingModels.Workshop, "never under gdd-0.1");
        }

        [Test]
        public void ABundleThatDoesNotStreamLeavesTheMapRunning()
        {
            var map = OpenMap();
            var layer = CollectionGeometryFixture.Special(map, After)!;
            int n = asked.Count;
            Assert.Greater(n, 0);
            for (int i = 0; i < n; i++) LogAssert.Expect(LogType.Warning, new Regex("^BUILDINGS: .* did not stream"));
            AnswerAll(found: false);
            Assert.AreEqual(0, layer.Placed.Count);
            Assert.AreEqual(0, layer.transform.childCount);
            Assert.IsTrue(layer.FailedOpen);
            Assert.DoesNotThrow(() => CollectionGeometryFixture.Special(map, new DateTime(1951, 1, 1)));
            Assert.AreEqual(0, asked.Count, "a failed model is not asked for again");
            Assert.IsNotNull(map.Terrain);
        }
    }
}
