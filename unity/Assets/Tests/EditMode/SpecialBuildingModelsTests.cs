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
                SpecialBuildingModels.SyrianR35, SpecialBuildingModels.Well }, asked.Select(a => a.id), "one request per placed model");
            AnswerAll();
            CollectionAssert.AreEquivalent(new[] { "latrun_tegart", "tzrifin_barracks", "camp_80_barracks", "netanya_factory", "ramat_hahayal_shikun", "degania_r35",
                "beersheba_abrahams_well" }, layer.Placed.Select(p => p.Key));
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
                // Abraham's Well's first ring is not clear (its point is inside Beersheba, ringed by the town's kit and roads): it
                // stands on a widened ring (SpecialBuildingModels.WidenRings), pinned by AbrahamsWellStandsByItsPointAtBeershebaAtEveryDate.
                else if (p.Key != "beersheba_abrahams_well") Assert.Less(off, inside + 4f * r, p.Key + " stands near its sourced point (beyond the town's footprint)");
                // The four props stand at a town's edge right by their points (owner on #2274: "Bigger, closer"), inside a footprint
                // if need be: AssertClearAndUpright checks them against the towns' buildings instead.
                if (!SpecialBuildingModels.EveryPlacement.Single(x => x.Key == p.Key).TownEdge)
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

        // ---- Tal Heuberger's four props (owner, 2 November 2026: "Place all four now (Recommended)") ----

        static readonly Statecraft.Sim.Content C = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);

        static IEnumerable<DateTime> Turns()
        {
            for (var d = C.Rules.Start; d < C.End; d = d.AddMonths(1)) yield return d;
        }

        /// <summary>Each prop on its sourced dates, at its sourced point, its source in the data row marked agent-read with a
        /// person's check pending: the well at every date; the sandbags only on the Altalena card's turn (the card's own window,
        /// as SeaAirCollection draws the ship); the crates and the wall on the 1948 turns (their sources give the year only).</summary>
        [Test]
        public void TalsPropsHaveTheirSourcedDatesPointsAndAgentReadSources()
        {
            var card = C.Events.Single(e => e.Id == SeaAirCollection.AltalenaEvent);
            Assert.AreEqual(card.From, SpecialBuildingModels.SandbagsFrom, "the Altalena card's window");
            Assert.AreEqual(card.To, SpecialBuildingModels.SandbagsTo, "the Altalena card's window");
            DateTime[] On(string key) => Turns().Where(d => SpecialBuildingModels.Placements(d).Any(w => w.Key == key)).ToArray();
            CollectionAssert.AreEqual(new[] { new DateTime(1948, 6, 15) }, On("tel_aviv_altalena_sandbags"), "the sandbags on the Altalena's turn only");
            var y1948 = Turns().Where(d => d.Year == 1948).ToArray();
            CollectionAssert.AreEqual(y1948, On("haifa_quay_crates"), "the crates on the 1948 turns");
            CollectionAssert.AreEqual(y1948, On("abu_ghosh_road_wall"), "the wall on the 1948 turns");
            CollectionAssert.AreEqual(Turns().ToArray(), On("beersheba_abrahams_well"), "Abraham's Well at every date");
            var all = SpecialBuildingModels.EveryPlacement.ToDictionary(w => w.Key);
            var well = all["beersheba_abrahams_well"];
            Assert.AreEqual(34.79307778, well.Lon, 1e-9); Assert.AreEqual(31.237225, well.Lat, 1e-9);
            Assert.AreEqual(SpecialBuildingModels.Siting.Shore, all["tel_aviv_altalena_sandbags"].Site);
            Assert.AreEqual("tel_aviv", all["tel_aviv_altalena_sandbags"].Along);
            Assert.AreEqual(SpecialBuildingModels.Siting.Shore, all["haifa_quay_crates"].Site);
            Assert.AreEqual("haifa", all["haifa_quay_crates"].Along);
            var wall = all["abu_ghosh_road_wall"];
            Assert.AreEqual(SpecialBuildingModels.Siting.Road, wall.Site);
            Assert.AreEqual("jaffa_jerusalem_road", wall.Along);
            Assert.AreEqual(35.11083, wall.Lon, 1e-9); Assert.AreEqual(31.80472, wall.Lat, 1e-9);
            foreach (var key in new[] { "beersheba_abrahams_well", "tel_aviv_altalena_sandbags", "haifa_quay_crates", "abu_ghosh_road_wall" })
            {
                StringAssert.Contains("agent-read 2026-11-02", all[key].Source, key);
                StringAssert.Contains("person check pending", all[key].Source, key);
                StringAssert.Contains("oldid=", all[key].Source, key + ": the revision read");
            }
            Assert.AreEqual(3.75f, SpecialBuildingModels.RealLengthM(SpecialBuildingModels.Well), 1e-6f, "Abraham's Well's diameter (he.wiki)");
        }

        static readonly string[] PropKeys = { "beersheba_abrahams_well", "tel_aviv_altalena_sandbags", "haifa_quay_crates", "abu_ghosh_road_wall" };

        /// <summary>A lease that records each release and every second release of itself (which must never happen).</summary>
        sealed class ProbeLease : IDisposable
        {
            readonly Action done; readonly List<string> twice; readonly string id; bool disposed;
            public ProbeLease(string id, Action done, List<string> twice) { this.id = id; this.done = done; this.twice = twice; }
            public void Dispose() { if (disposed) { twice.Add(id); return; } disposed = true; done(); }
        }

        /// <summary>A special-building layer on <paramref name="map"/> under <paramref name="policy"/>, its acquisitions and releases
        /// counted by model, every answer the real Near prefab (or held back in <paramref name="pending"/> when given).</summary>
        static SpecialBuildingModels CountingLayer(MapScene map, CollectionResidencyPolicy policy, List<string> acquired, List<string> released,
            List<string> twice, List<(string id, Action<GameObject?> done)>? pending = null)
        {
            var go = new GameObject(SpecialBuildingModels.Name); go.transform.SetParent(map.Terrain.transform, false);
            var layer = go.AddComponent<SpecialBuildingModels>();
            typeof(SpecialBuildingModels).GetField("map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(layer, map);
            layer.ConfigureResidency(policy, (id, detail, done) =>
            {
                acquired.Add(id);
                if (pending != null) pending.Add((id, done));
                else done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near)));
                return new ProbeLease(id, () => released.Add(id), twice);
            });
            return layer;
        }

        /// <summary>Looks straight down from <paramref name="height"/> m over a point <paramref name="east"/> m east of
        /// <paramref name="anchor"/> (the residency view measures on the ground plane, round the view's ground focus).</summary>
        static void Look(SpecialBuildingModels layer, Camera cam, Vector3 anchor, float east, float height)
        {
            cam.transform.SetPositionAndRotation(new Vector3(anchor.x + east, height, anchor.z), Quaternion.Euler(90f, 0f, 0f));
            layer.Choose(default, cam);
        }

        /// <summary>The owner, 2 November 2026, on #2274: "Near-only for these props (Recommended)" ("These four small props download
        /// only when the camera comes close to them; the Dakota stays map-wide (craft ruling)"). With the production special-building
        /// policy (ProductionResidency.Approved().Special): at the opening view (90 km) none of the four props' bundles is asked for,
        /// while every other special keeps its map-wide request (owner ruling 29, 30 October 2026, "Far models map-wide"); a close
        /// view over one prop asks for that prop alone, once; leaving releases it once and nothing else; teardown releases every
        /// lease once. (CollectionOtherLeaveTests runs a permissive harness policy; the production distances are covered here and
        /// in ThePropsResidencyHasItsEnterExitBandAndHeightGate.)</summary>
        [Test]
        public void TheFourPropsStreamOnlyNearTheCameraAndTheOtherSpecialsStayMapWide()
        {
            var map = OpenMap();
            var acquired = new List<string>(); var released = new List<string>(); var twice = new List<string>();
            var layer = CountingLayer(map, ProductionResidency.Approved().Special, acquired, released, twice);
            var date = new DateTime(1948, 6, 15);
            SpecialBuildingModels.ShowAt(map, date);
            var cam = new GameObject("TEST camera").AddComponent<Camera>();
            try
            {
                var wants = SpecialBuildingModels.Placements(date);
                var props = wants.Where(w => PropKeys.Contains(w.Key)).ToList();
                Assert.AreEqual(4, props.Count, "all four props are wanted on 15 June 1948");
                Assert.IsTrue(props.All(w => w.NearOnly), "the four props are near-only");
                Assert.IsTrue(wants.Where(w => !PropKeys.Contains(w.Key)).All(w => !w.NearOnly), "no other special is");
                var tel = map.Frame.ToWorld(34.78, 32.08, 0);
                Look(layer, cam, tel, 0f, 90000f);   // the opening distance (90 km)
                var propModels = props.Select(w => w.Model).ToArray();
                Assert.AreEqual(0, acquired.Count(propModels.Contains), "no prop's bundle at the opening view");
                Assert.AreEqual(1, acquired.Count(id => id == SpecialBuildingModels.Tegart), "the other specials stay map-wide, each asked once");
                int before = acquired.Count;
                var well = props.Single(w => w.Key == "beersheba_abrahams_well");
                var at = map.Frame.ToWorld(well.Lon, well.Lat, 0);
                Look(layer, cam, at, 0f, 4000f);   // well inside the 6 km enter distance and the 30 km view gate
                Assert.AreEqual(before + 1, acquired.Count, "one new request");
                Assert.AreEqual(1, acquired.Count(id => id == SpecialBuildingModels.Well), "the well, once, when the camera is close to it");
                Assert.IsTrue(layer.Placed.Any(p => p.Key == "beersheba_abrahams_well"), "and it is drawn");
                Look(layer, cam, tel, 0f, 250000f);   // leave
                CollectionAssert.AreEqual(new[] { SpecialBuildingModels.Well }, released.ToArray(), "leaving releases the well, once, and nothing else");
                Assert.IsFalse(layer.Placed.Any(p => p.Key == "beersheba_abrahams_well"), "and it is no longer drawn");
                Assert.IsTrue(layer.Placed.Any(p => p.Model == SpecialBuildingModels.Tegart), "the map-wide specials stay");
            }
            finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); UnityEngine.Object.DestroyImmediate(layer.gameObject); }
            Assert.AreEqual(acquired.Count, released.Count, "teardown releases every lease");
            CollectionAssert.AreEquivalent(acquired, released, "each lease released once");
            CollectionAssert.IsEmpty(twice, "no lease released twice");
        }

        /// <summary>The production policy's band and gate round a near-only prop: enter within 6 km of its point (plus its clearance),
        /// stay until past 8 km, so 7, 7.9 and 6.1 km neither release nor re-ask; past 8 km releases it once; re-entering asks once
        /// more. The 30 km height gate (40 km to leave it) applies too: right over the prop but higher than 30 km asks nothing;
        /// at 25 km it asks; at 35 km it stays; at 45 km it is released. The camera stays at 4 km, well under both bounds.</summary>
        [Test]
        public void ThePropsResidencyHasItsEnterExitBandAndHeightGate()
        {
            var map = OpenMap();
            var acquired = new List<string>(); var released = new List<string>(); var twice = new List<string>();
            var layer = CountingLayer(map, ProductionResidency.Approved().Special, acquired, released, twice);
            SpecialBuildingModels.ShowAt(map, new DateTime(1952, 4, 15));   // the well alone of the four, with the map-wide specials
            var cam = new GameObject("TEST camera").AddComponent<Camera>();
            try
            {
                var well = SpecialBuildingModels.EveryPlacement.Single(w => w.Key == "beersheba_abrahams_well");
                var at = map.Frame.ToWorld(well.Lon, well.Lat, 0);
                int Asked() => acquired.Count(id => id == SpecialBuildingModels.Well);
                int Released() => released.Count(id => id == SpecialBuildingModels.Well);
                Look(layer, cam, at, 0f, 90000f);
                Assert.AreEqual(0, Asked(), "not at the opening view");
                Look(layer, cam, at, 5000f, 4000f);
                Assert.AreEqual(1, Asked(), "asked within 6 km");
                foreach (var east in new[] { 7000f, 7900f, 6100f, 7900f })
                {
                    Look(layer, cam, at, east, 4000f);
                    Assert.AreEqual(1, Asked(), $"no new ask at {east} m");
                    Assert.AreEqual(0, Released(), $"no release at {east} m (inside 8 km)");
                }
                Look(layer, cam, at, 8500f, 4000f);
                Assert.AreEqual(1, Released(), "released once past 8 km");
                Look(layer, cam, at, 9000f, 4000f);
                Assert.AreEqual(1, Released(), "still once");
                Look(layer, cam, at, 7000f, 4000f);
                Assert.AreEqual(1, Asked(), "7 km, coming back, is not inside 6 km: no ask");
                Look(layer, cam, at, 5000f, 4000f);
                Assert.AreEqual(2, Asked(), "re-entering asks once more");
                Look(layer, cam, at, 0f, 250000f);
                Assert.AreEqual(2, Released(), "and leaving releases it once more");
                // The height gate, right over the well.
                Look(layer, cam, at, 0f, 35000f);
                Assert.AreEqual(2, Asked(), "higher than 30 km: no ask");
                Look(layer, cam, at, 0f, 25000f);
                Assert.AreEqual(3, Asked(), "under 30 km: asked");
                Look(layer, cam, at, 0f, 35000f);
                Assert.AreEqual(2, Released(), "35 km is inside the 40 km gate: kept");
                Look(layer, cam, at, 0f, 45000f);
                Assert.AreEqual(3, Released(), "past 40 km: released");
            }
            finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); UnityEngine.Object.DestroyImmediate(layer.gameObject); }
            CollectionAssert.AreEquivalent(acquired, released, "teardown releases every lease once");
            CollectionAssert.IsEmpty(twice, "no lease released twice");
        }

        /// <summary>A town-edge prop whose nearest clear spot is farther than the cap from its sourced point is not drawn, its bundle
        /// is not asked for even with the camera close (no wasted fetch), and its warning is logged once, not on every redraw. The cap
        /// is forced down to 1 m (a test seam) so every prop on 15 May 1948 is past it.</summary>
        [Test]
        public void APropPastTheCapIsNotDrawnNotFetchedAndWarnsOnce()
        {
            var map = OpenMap();
            var saved = SpecialBuildingModels.MaxPropOffset;
            SpecialBuildingModels.MaxPropOffset = 1f;
            var acquired = new List<string>(); var released = new List<string>(); var twice = new List<string>();
            var cam = new GameObject("TEST camera").AddComponent<Camera>();
            SpecialBuildingModels? layer = null;
            var warnings = new List<string>();
            Application.LogCallback watch = (message, stack, type) => { if (type == LogType.Warning && message.StartsWith("BUILDINGS: the nearest clear spot", StringComparison.Ordinal)) warnings.Add(message); };
            Application.logMessageReceived += watch;
            try
            {
                var date = new DateTime(1948, 5, 15);
                var capped = SpecialBuildingModels.Placements(date).Where(w => w.NearOnly).ToList();
                Assert.AreEqual(3, capped.Count, "the well, the crates and the wall on 15 May 1948");
                foreach (var w in capped)
                    LogAssert.Expect(LogType.Warning, new Regex($"^BUILDINGS: the nearest clear spot for {w.Key} is \\d+ m from its sourced point, past 1 m"));
                layer = CountingLayer(map, new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, 0, "TEST ONLY"), acquired, released, twice);
                SpecialBuildingModels.ShowAt(map, date);
                for (int i = 0; i < 3; i++) Look(layer, cam, map.Frame.ToWorld(34.8, 31.8, 0), 0f, 4000f);
                foreach (var w in capped)
                {
                    Assert.IsFalse(layer.Placed.Any(p => p.Key == w.Key), w.Key + " is not drawn");
                    Assert.AreEqual(0, acquired.Count(id => id == w.Model), w.Key + "'s bundle is not asked for");
                }
                Assert.IsTrue(layer.Placed.Any(p => p.Key == "latrun_tegart"), "the uncapped specials still draw");
                foreach (var w in capped)
                    Assert.AreEqual(1, warnings.Count(m => m.Contains(" for " + w.Key + " is ")), w.Key + "'s warning, once, across three views");
            }
            finally
            {
                Application.logMessageReceived -= watch;
                SpecialBuildingModels.MaxPropOffset = saved;
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
                if (layer != null) UnityEngine.Object.DestroyImmediate(layer.gameObject);
            }
        }

        /// <summary>Residency is keyed by model, so near-only must be one value for every row of a model (else a map-wide row would keep
        /// a near-only prop's bundle resident), as the scale is.</summary>
        [Test]
        public void NearOnlyAndScaleAreUniformPerModel()
        {
            foreach (var g in SpecialBuildingModels.EveryPlacement.GroupBy(w => w.Model))
            {
                Assert.AreEqual(1, g.Select(w => w.NearOnly).Distinct().Count(), g.Key + ": one near-only value for all its rows");
                Assert.AreEqual(1, g.Select(w => w.Scale).Distinct().Count(), g.Key + ": one scale for all its rows");
            }
        }

        /// <summary>Where the specials stand does not depend on the order their bundles arrive (#2266's contract): every spot is
        /// planned from the full wanted list before any model is drawn. Answered in catalogue order and in reverse, the same
        /// keys stand at the same points.</summary>
        [Test]
        public void SpotsDoNotDependOnTheOrderTheBundlesArrive()
        {
            var map = OpenMap();
            var wide = new CollectionResidencyPolicy(1000000, 1000001, 1000000, 1000001, 0, "TEST ONLY");
            var date = new DateTime(1948, 6, 15);
            Dictionary<string, Vector3> Place(bool reverse)
            {
                var acquired = new List<string>(); var released = new List<string>(); var twice = new List<string>();
                var pending = new List<(string id, Action<GameObject?> done)>();
                var layer = CountingLayer(map, wide, acquired, released, twice, pending);
                var cam = new GameObject("TEST camera").AddComponent<Camera>();
                try
                {
                    SpecialBuildingModels.ShowAt(map, date);
                    Look(layer, cam, map.Frame.ToWorld(34.8, 31.8, 0), 0f, 4000f);
                    var order = reverse ? pending.AsEnumerable().Reverse().ToList() : pending.ToList();
                    Assert.Greater(order.Count, 4, "the specials and the props are asked for");
                    foreach (var (id, done) in order) done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near)));
                    return layer.Placed.ToDictionary(p => p.Key, p => p.At);
                }
                finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); UnityEngine.Object.DestroyImmediate(layer.gameObject); }
            }
            var forward = Place(false); var backward = Place(true);
            CollectionAssert.AreEquivalent(forward.Keys, backward.Keys, "the same specials drawn");
            CollectionAssert.IsSubsetOf(PropKeys, forward.Keys.ToList(), "the four props among them");
            foreach (var key in forward.Keys)
                Assert.Less((forward[key] - backward[key]).magnitude, 0.01f, key + " stands at the same point whatever the arrival order");
        }

        /// <summary>The owner, 2 November 2026, on #2274: "Bigger, closer (Recommended)" ("Draw these four at about 3x so they read at
        /// close zoom"). The scale is each prop's own, in its data row (Want.Scale): three times the one world scale for the four
        /// props, which stand at a town's edge (Want.TownEdge); every other row keeps the world scale and the town clearance.</summary>
        [Test]
        public void TalsPropsAreDrawnAtThreeTimesTheWorldScaleFromTheirRows()
        {
            var props = new[] { "beersheba_abrahams_well", "tel_aviv_altalena_sandbags", "haifa_quay_crates", "abu_ghosh_road_wall" };
            foreach (var w in SpecialBuildingModels.EveryPlacement)
            {
                bool prop = props.Contains(w.Key);
                Assert.AreEqual(prop ? 3f : 1f, w.Scale, w.Key + "'s scale");
                Assert.AreEqual(prop, w.TownEdge, w.Key + " stands at a town's edge");
                Assert.AreEqual(SpecialBuildingModels.RealLengthM(w.Model) * SpecialBuildingModels.WorldScale * w.Scale, SpecialBuildingModels.SizeOf(w.Model), 0.01f, w.Key);
            }
            Assert.AreEqual(3.75f * 24f * 3f, SpecialBuildingModels.SizeOf(SpecialBuildingModels.Well), 0.01f, "the well, 270 m across");
            var map = OpenMap();
            var layer = CollectionGeometryFixture.Special(map, new DateTime(1948, 6, 15))!; AnswerAll();
            foreach (var key in props)
            {
                var p = layer.Placed.Single(x => x.Key == key);
                var b = layer.transform.Find(key)!.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                float drawn = Mathf.Max(b.size.x, b.size.z), size = SpecialBuildingModels.SizeOf(p.Model);
                TestContext.WriteLine($"{key}: drawn {drawn:0} m, size {size:0} m");
                Assert.That(drawn, Is.InRange(0.7f * size, 1.5f * size), key + " drawn at its scaled size (a turned box spans up to its diagonal)");
            }
        }

        /// <summary>A placed prop: clear of the towns' footprints, the routes, the unit tokens present that day and the water,
        /// on the ground, upright (its only turn on top of the prefab's FBX axis conversion is about the vertical).</summary>
        void AssertClearAndUpright(MapScene map, SpecialBuildingModels layer, string key, DateTime date, bool alongRoad = false)
        {
            var p = layer.Placed.Single(x => x.Key == key);
            var flat = new Vector2(p.At.x, p.At.z); float r = SpecialBuildingModels.ClearanceOf(p.Model);
            var ground = PlacementLayer.CampGround(map);
            // The owner, 2 November 2026, on #2274: "Bigger, closer (Recommended)" ("let them stand at the town's edge right by the
            // sourced spot"): the four props may stand inside a town's kit footprint, but clear of its building models, of the
            // unit tokens standing in towns and of every settlement's and locality's name point.
            var towns = map.GetComponentInChildren<Statecraft.Game.Map.Kits.TownModelLayer>(true);
            Assert.IsNotNull(towns, "the town building models");
            var buildings = map.KitFootprints.SelectMany(f => towns.InstancesOf(f.id)).Select(i => (i.C, 0.5f * new Vector2(i.W, i.D).magnitude)).ToList();
            Assert.IsTrue(TownLandmarks.Clear(flat, r, buildings), key + " clear of the towns' buildings");
            Assert.IsTrue(TownLandmarks.Clear(flat, r, map.UnitClearings.ToList()), key + " clear of the tokens in the towns");
            foreach (var s in map.Settlements.Concat(map.Localities.Select(l => l.Settlement)))
            {
                var n = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                Assert.Greater((new Vector2(n.x, n.z) - flat).magnitude, r, $"{key} clear of {s.Id}'s name point");
            }
            // A wall along a road is checked as it is placed, along its length (TheWallStandsBesideTheJaffaJerusalemRoadNearAbuGhoshAlongIt).
            if (!alongRoad) Assert.IsFalse(ground.OnRoute(flat, r), key + " clear of the routes");
            Assert.IsFalse(ground.OnToken(flat, r, date), key + " clear of the unit tokens");
            Assert.IsFalse(ground.Water(flat.x, flat.y), key + " on land");
            var go = layer.transform.Find(key)!;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(p.Model, CollectionDetail.Near));
            var yaw = go.localRotation * Quaternion.Inverse(prefab.transform.localRotation);
            Assert.Greater(Vector3.Dot(yaw * Vector3.up, Vector3.up), 0.9999f, key + " upright: turned only about the vertical");
            var b = go.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
            Assert.AreEqual(p.At.y, b.min.y, 1f, key + " stands on the ground");
            TestContext.WriteLine($"{key}: at ({p.At.x:0}, {p.At.z:0}), {Mathf.Max(b.size.x, b.size.z):0} m long");
        }

        /// <summary>The sandbag post on the Tel Aviv shore (he.wiki אלטלנה: the sandbag position at the foot of the Palmach
        /// headquarters fired on the Altalena's boat, the night of 21-22 June 1948), on the Altalena's turn only: on the beach,
        /// just inland of the water's edge, near the coast point the Altalena lies off; gone on the next turn.</summary>
        [Test]
        public void TheSandbagsStandOnTheTelAvivShoreOnlyOnTheAltalenasTurn()
        {
            var map = OpenMap();
            var date = new DateTime(1948, 6, 15);
            var layer = CollectionGeometryFixture.Special(map, date)!; AnswerAll();
            AssertClearAndUpright(map, layer, "tel_aviv_altalena_sandbags", date);
            AssertOnTheBeach(map, layer, "tel_aviv_altalena_sandbags", "tel_aviv");
            CollectionGeometryFixture.Special(map, new DateTime(1948, 7, 15)); AnswerAll();
            Assert.IsFalse(layer.Placed.Any(p => p.Key == "tel_aviv_altalena_sandbags"), "gone after the Altalena's turn");
            Assert.IsNull(layer.transform.Find("tel_aviv_altalena_sandbags"));
            CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 15)); AnswerAll();
            Assert.IsFalse(layer.Placed.Any(p => p.Key == "tel_aviv_altalena_sandbags"), "not before it");
        }

        /// <summary>The crate stack on the Haifa port quay (Nationaal Archief 255-1124: crates on the quay by the immigrant ship
        /// Bulgaria, 1948), on the 1948 turns: on land just inland of the harbour's water, by Haifa's coast point (the
        /// steamer's); gone in 1949.</summary>
        [Test]
        public void TheCratesStandOnTheHaifaQuayOnThe1948Turns()
        {
            var map = OpenMap();
            var date = new DateTime(1948, 5, 15);
            var layer = CollectionGeometryFixture.Special(map, date)!; AnswerAll();
            AssertClearAndUpright(map, layer, "haifa_quay_crates", date);
            AssertOnTheBeach(map, layer, "haifa_quay_crates", "haifa");
            CollectionGeometryFixture.Special(map, new DateTime(1949, 1, 15)); AnswerAll();
            Assert.IsFalse(layer.Placed.Any(p => p.Key == "haifa_quay_crates"), "gone after 1948");
        }

        void AssertOnTheBeach(MapScene map, SpecialBuildingModels layer, string key, string coast)
        {
            var p = layer.Placed.Single(x => x.Key == key); var flat = new Vector2(p.At.x, p.At.z);
            var row = Statecraft.Game.Map.Kits.SeaCraft.Coasts.Single(c => c.place == coast);
            var w = map.Frame.ToWorld(row.lon, row.lat, 0);
            float off = (flat - new Vector2(w.x, w.z)).magnitude;
            TestContext.WriteLine($"{key}: {off:0} m from {coast}'s coast point");
            Assert.Less(off, 1500f, $"{key} right by {coast}'s coast point (owner on #2274: \"Bigger, closer\")");
            var ground = PlacementLayer.CampGround(map);
            float r = SpecialBuildingModels.ClearanceOf(p.Model);
            var sea = flat + row.seaward * (r + SpecialBuildingModels.ShoreInsetM + 10f);
            Assert.IsTrue(ground.Water(sea.x, sea.y), $"{key} on the shore: the water's edge just seaward of it");
        }

        /// <summary>The low wall beside the Jaffa-Jerusalem road near Abu Ghosh (Nationaal Archief 255-0876: a low stone wall
        /// along the road, the village "probably Abu Ghosh", on the way to Jerusalem, 1948), on the 1948 turns: just off the road's
        /// corridor (routes_1948.yaml), near Abu Ghosh's point, its long side along the road.</summary>
        [Test]
        public void TheWallStandsBesideTheJaffaJerusalemRoadNearAbuGhoshAlongIt()
        {
            var map = OpenMap();
            var date = new DateTime(1948, 5, 15);
            var layer = CollectionGeometryFixture.Special(map, date)!; AnswerAll();
            AssertClearAndUpright(map, layer, "abu_ghosh_road_wall", date, alongRoad: true);
            var p = layer.Placed.Single(x => x.Key == "abu_ghosh_road_wall"); var flat = new Vector2(p.At.x, p.At.z);
            var a3 = map.Frame.ToWorld(SpecialBuildingModels.AbuGhoshLon, SpecialBuildingModels.AbuGhoshLat, 0);
            float off = (flat - new Vector2(a3.x, a3.z)).magnitude;
            TestContext.WriteLine($"abu_ghosh_road_wall: {off:0} m from Abu Ghosh's point");
            Assert.Less(off, 1500f, "right by Abu Ghosh's point");
            var (line, half) = RouteLayer.Corridors(map.Frame, map.RouteList.Where(r => r.Id == "jaffa_jerusalem_road")).Single();
            float best = float.MaxValue; Vector2 dir = Vector2.zero;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                float d = Statecraft.Game.Map.Kits.KitGround.SegmentDistance(flat, line[i], line[i + 1]);
                if (d < best) { best = d; dir = (line[i + 1] - line[i]).normalized; }
            }
            var go = layer.transform.Find("abu_ghosh_road_wall")!;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(p.Model, CollectionDetail.Near));
            var probe = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var pb = probe.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                // The wall's own half depth at its drawn size: it stands just off the road's corridor, its long side along it.
                float depthHalf = 0.5f * Mathf.Min(pb.size.x, pb.size.z) * go.localScale.x / prefab.transform.localScale.x;
                Assert.That(best, Is.InRange(half + depthHalf, half + depthHalf + 5f), "beside the road: just off its corridor");
                float lengthHalf = 0.5f * Mathf.Max(pb.size.x, pb.size.z) * go.localScale.x / prefab.transform.localScale.x;
                var ground = PlacementLayer.CampGround(map);
                // Clear of every route and the water along its whole length, sampled every half depth.
                float reachAlong = lengthHalf - depthHalf;
                for (float t = -reachAlong; t <= reachAlong + 0.5f; t += depthHalf)
                {
                    var q = flat + dir * Mathf.Min(t, reachAlong);
                    Assert.IsFalse(ground.OnRoute(q, depthHalf), $"the wall clear of the routes {t:0} m along it");
                    Assert.IsFalse(ground.Water(q.x, q.y), $"the wall on land {t:0} m along it");
                }
                var longLocal = pb.size.x >= pb.size.z ? Vector3.right : Vector3.forward;
                var longWorld = go.localRotation * Quaternion.Inverse(prefab.transform.localRotation) * longLocal;
                Assert.Greater(Mathf.Abs(Vector2.Dot(new Vector2(longWorld.x, longWorld.z).normalized, dir)), 0.999f, "its long side along the road");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        /// <summary>Abraham's Well at Beersheba (he.wiki באר אברהם), at every date: by its sourced point, beyond the town's
        /// footprint as the other single buildings stand.</summary>
        [Test]
        public void AbrahamsWellStandsByItsPointAtBeershebaAtEveryDate()
        {
            var map = OpenMap();
            foreach (var date in new[] { new DateTime(1948, 5, 15), new DateTime(1952, 4, 15) })
            {
                var layer = CollectionGeometryFixture.Special(map, date)!; AnswerAll();
                AssertClearAndUpright(map, layer, "beersheba_abrahams_well", date);
                var p = layer.Placed.Single(x => x.Key == "beersheba_abrahams_well");
                var a3 = map.Frame.ToWorld(34.79307778, 31.237225, 0); var anchor = new Vector2(a3.x, a3.z);
                float off = (new Vector2(p.At.x, p.At.z) - anchor).magnitude, r = SpecialBuildingModels.ClearanceOf(p.Model);
                float inside = SpecialBuildingModels.Inside(anchor, map.KitFootprints.Select(f => (f.centre, f.radius)));
                TestContext.WriteLine($"well on {date:d MMM yyyy}: {off:0} m from its point, {inside:0} m inside Beersheba's footprint");
                // The farthest the widened rings reach (SpecialBuildingModels.SeededSpot, WidenRings).
                float reach = inside + (1.5f + (SpecialBuildingModels.SpotAttempts - 1) / 8 + SpecialBuildingModels.WidenRings * SpecialBuildingModels.WidenShare) * r;
                Assert.Less(off, reach, "by its point, on the nearest clear ring");
                Assert.Less(off, 1500f, "right by Abraham's Well (owner on #2274: \"Bigger, closer\")");
            }
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
                // Abraham's Well, 3.75 m across (he.wiki), drawn at three times the world scale (owner on #2274: "Bigger, closer"):
                // about 270 m, a locomotive car and a third.
                [SpecialBuildingModels.Well] = (1.2f, 1.5f),
            };
            Assert.AreEqual(7, layer.Placed.Count);
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
            // From the first day: the fort, Abraham's Well, and the 1948 crates and wall.
            var opening = new[] { "latrun_tegart", "beersheba_abrahams_well", "haifa_quay_crates", "abu_ghosh_road_wall" };
            CollectionAssert.AreEquivalent(opening, layer.Placed.Select(p => p.Key));
            var fort = layer.transform.Find("latrun_tegart")!.gameObject;
            CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 15));
            Assert.IsTrue(fort != null && fort.transform.parent == layer.transform, "the same date keeps the same models");
            CollectionGeometryFixture.Special(map, After);
            AnswerAll();
            Assert.AreEqual(7, layer.Placed.Count);
            CollectionGeometryFixture.Special(map, new DateTime(1948, 5, 16));
            AnswerAll(); // Earlier-date retirement reacquires its still-needed original through the fake source.
            CollectionAssert.AreEquivalent(opening, layer.Placed.Select(p => p.Key), "an earlier date shows only what stood then");
            Assert.AreEqual(opening.Length, layer.transform.childCount);
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
