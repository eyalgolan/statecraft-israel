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

        /// <summary>The residency anchors' radius (FillResidencyAnchors, the row's own ClearanceOf(w), from #2274) is the radius
        /// #2351 gave them (ClearanceOf(w.Model)) for every row there is: every sourced placement and a built arms workshop's b21.</summary>
        [Test]
        public void EachRowsResidencyRadiusIsItsModelsClearance()
        {
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var workshops = SpecialBuildingModels.ArmsWorkshops(c, new[] { Workshop(c, 1, "rehovot", Built) });
            Assert.AreEqual(1, workshops.Count, "premise: one built workshop's row");
            var rows = SpecialBuildingModels.EveryPlacement.Concat(workshops).ToList();
            foreach (var w in rows)
            {
                float byRow = SpecialBuildingModels.ClearanceOf(w), byModel = SpecialBuildingModels.ClearanceOf(w.Model);
                TestContext.WriteLine(FormattableString.Invariant($"{w.Key} ({w.Model}): ClearanceOf(w) {byRow:R}, ClearanceOf(w.Model) {byModel:R}"));
                Assert.AreEqual(byModel, byRow, 0f, w.Key);
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
            // sourced spot"): the four props may stand inside a town's kit footprint, but clear of its drawn buildings (its building
            // models; a major city's kit blocks, which it draws at every zoom in their place: #2876), of the unit tokens standing in
            // towns and of every settlement's and locality's name point.
            var towns = map.GetComponentInChildren<Statecraft.Game.Map.Kits.TownModelLayer>(true);
            Assert.IsNotNull(towns, "the town building models");
            var buildings = map.KitFootprints.Where(f => !Statecraft.Game.Map.Kits.TownModels.IsMajorCity(f.id)).SelectMany(f => towns.InstancesOf(f.id))
                .Select(i => (i.C, 0.5f * new Vector2(i.W, i.D).magnitude)).ToList();
            Assert.IsTrue(TownLandmarks.Clear(flat, r, buildings), key + " clear of the towns' buildings");
            foreach (var block in map.MajorCityBlocks)
                Assert.IsTrue(SpecialBuildingModels.ClearOfBox(flat, r, block.C, block.Yaw, block.W, block.D), $"{key} clear of {block.Id}'s kit {block.Kind} at {block.C}");
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

        // ---- #2274 merged with #2281 (one planner, the props off the farm scenes, no rebuild per frame) ----

        /// <summary>The hex props keep clear of each of the four props where it is drawn (HexPropModels.Others reads the same plan
        /// the layer draws from), on the Altalena's turn, 15 June 1948, when all four stand.</summary>
        [Test]
        public void TheHexPropsKeepClearOfEachPropWhereItIsDrawn()
        {
            var map = OpenMap();
            var date = new DateTime(1948, 6, 15);
            var savedProps = HexPropModels.Request;
            HexPropModels.Request = (id, done) => { };
            try
            {
                var layer = CollectionGeometryFixture.Special(map, date)!; AnswerAll();
                var kept = HexPropModels.ShowAt(map, date)!.Others(map, date);
                foreach (var key in PropKeys)
                {
                    var p = layer.Placed.Single(x => x.Key == key); var drawn = new Vector2(p.At.x, p.At.z);
                    Assert.IsTrue(kept.Any(d => (d.centre - drawn).magnitude < 1f && Mathf.Abs(d.radius - SpecialBuildingModels.ClearanceOf(p.Model)) < 0.01f),
                        key + ": the hex props keep clear of it where it is drawn");
                }
            }
            finally { HexPropModels.Request = savedProps; }
        }

        /// <summary>The four props keep off the farm scenes' plan (RuralLife.PlanOf), as a workshop does: with a farm scene planned
        /// right on each prop's spot, every prop the plan still places stands clear of every scene.</summary>
        [Test]
        public void ThePropsKeepOffTheFarmScenesPlan()
        {
            var map = OpenMap();
            var d = new DateTime(1948, 6, 15);
            var rows = SpecialBuildingModels.Placements(d);
            var clear = SpecialBuildingModels.Plan(map, d, rows, _ => true, log: false, Array.Empty<(RuralLife.Want, Vector2)>())
                .Where(x => x.want.TownEdge).ToList();
            Assert.Greater(clear.Count, 0, "premise: the props have spots with no farm scenes");
            var fake = clear.Select(x => (want: new RuralLife.Want("test_" + x.want.Key, RuralLife.Models[0], x.at, 0f), spot: x.at)).ToList();
            var moved = SpecialBuildingModels.Plan(map, d, rows, _ => true, log: false, fake).Where(x => x.want.TownEdge).ToList();
            Assert.Greater(moved.Count, 0, "the props still stand");
            var scenes = fake.Select(f => (f.spot, RuralLife.SceneRadiusM(f.want.Model))).ToList();
            foreach (var (w, at) in moved)
                Assert.IsTrue(TownLandmarks.Clear(at, SpecialBuildingModels.ClearanceOf(w), scenes), w.Key + " keeps off the farm scenes' plan");
        }

        /// <summary>With no workshop built, a change of the farm scenes' plan moves the props on GameRoot's next refresh (the same
        /// date): the props are drawn on the plan shown, a scene put on a prop's ground moves it in the plan (the premise), and
        /// the layer then draws every prop where the new plan puts it.</summary>
        [Test]
        public void ARuralPlanChangeMovesThePropsWithNoWorkshopBuilt()
        {
            var map = OpenMap();
            var d = new DateTime(1948, 6, 15);
            var savedPeople = CivilianPeople.Request; var savedRural = RuralLife.Request;
            CivilianPeople.Request = (id, detail, done) => { }; RuralLife.Request = (id, detail, done) => { };
            try
            {
                CollectionGeometryFixture.People(map, d, Array.Empty<Statecraft.Sim.Placement>(), Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content));
                var rural = CollectionGeometryFixture.Rural(map)!;
                var special = CollectionGeometryFixture.Special(map, d)!; AnswerAll();
                Dictionary<string, Vector2> Planned() => SpecialBuildingModels.Plan(map, d, SpecialBuildingModels.Placements(d), _ => true, false, RuralLife.PlanOf(map))
                    .Where(x => x.want.TownEdge).ToDictionary(x => x.want.Key, x => x.at);
                Dictionary<string, Vector2> Drawn() => special.Placed.Where(p => PropKeys.Contains(p.Key)).ToDictionary(p => p.Key, p => new Vector2(p.At.x, p.At.z));
                bool Same(Dictionary<string, Vector2> a, Dictionary<string, Vector2> b) =>
                    a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var o) && (o - kv.Value).magnitude < 1e-3f);
                var old = Drawn();
                Assert.Greater(old.Count, 0, "premise: the props are drawn");
                Assert.IsTrue(Same(old, Planned()), "premise: the props are drawn on the plan shown");
                var wants = rural.Wanted.ToList(); var others = rural.OthersShown.ToList();
                // A farm scene stands on a 700 m clear disc (RuralLife.ClearanceRadiusM) at its first ring, FirstRingClearances
                // clearances out from its anchor on one of Bearings bearings. So the test's scene (the widest, first in the list:
                // RuralLife.Plan drops a want past MaxAnchorsPerModel) is anchored one first ring away from a target on each
                // bearing in turn: on the bearing it tries first it lands on the target if that ground is clear. The targets are
                // each prop's spot and points 300 m round it, until the plan moves a prop.
                var model = RuralLife.Models.OrderByDescending(RuralLife.SceneRadiusM).First();
                float ring = RuralLife.ClearanceRadiusM * RuralLife.FirstRingClearances;
                bool moved = false;
                foreach (var s in old.Values)
                {
                    foreach (var target in new[] { s, s + new Vector2(300f, 0f), s - new Vector2(300f, 0f), s + new Vector2(0f, 300f), s - new Vector2(0f, 300f) })
                    {
                        for (int b = 0; b < RuralLife.Bearings && !moved; b++)
                        {
                            float angle = b * (360f / RuralLife.Bearings) * Mathf.Deg2Rad;
                            var anchor = target - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring;
                            rural.ShowForTest(wants.Prepend(new RuralLife.Want("test_scene", model, anchor, 0f)).ToList(), others.ToList());
                            moved = !Same(old, Planned());
                        }
                        if (moved) break;
                    }
                    if (moved) break;
                }
                Assert.IsTrue(moved, "premise: a farm scene on a prop's ground moves a prop in the plan");
                CollectionGeometryFixture.Special(map, d); AnswerAll();
                var now = Planned(); var drawn = Drawn();
                CollectionAssert.AreEquivalent(now.Keys, drawn.Keys, "the props the new plan places are the ones drawn");
                foreach (var kv in now) Assert.Less((drawn[kv.Key] - kv.Value).magnitude, 1e-3f, kv.Key + " drawn where the new plan puts it");
            }
            finally { CivilianPeople.Request = savedPeople; RuralLife.Request = savedRural; }
        }

        /// <summary>The layer never rebuilds per frame, only on an event (53 on #2274: "never per frame"). The unit fixture calls
        /// the three collection helpers in production order, then three frames with the same
        /// inputs and the layers' per-frame LateUpdate keep every drawn model and ask for nothing. Then an event, a change of the
        /// farm scenes' plan while the well (which keeps off the farm scenes) stands, rebuilds it: the old fort is destroyed,
        /// which shows the frames' identity check can see a rebuild.</summary>
        [Test]
        public void CollectionHelpersDoNotRebuildWithoutAnInputChange()
        {
            var map = OpenMap();
            var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
            var d = new DateTime(1948, 6, 15);
            var savedPeople = CivilianPeople.Request; var savedRural = RuralLife.Request;
            CivilianPeople.Request = (id, detail, done) => { }; RuralLife.Request = (id, detail, done) => { };
            try
            {
                CollectionGeometryFixture.People(map, d, Array.Empty<Statecraft.Sim.Placement>(), c);
                var rural = CollectionGeometryFixture.Rural(map)!;
                var layer = CollectionGeometryFixture.Special(map, d)!;
                AnswerAll();
                int v = RuralLife.VersionOf(map);
                Assert.Greater(v, 0, "premise: a farm-scene plan is shown, so the stamp path is live");
                Assert.IsNotNull(layer.transform.Find("beersheba_abrahams_well"), "premise: the well is drawn");
                Assert.IsNotNull(layer.transform.Find("latrun_tegart"), "premise: the fort is drawn");
                var before = layer.Placed.Select(p => p.Key).ToDictionary(k => k, k => layer.transform.Find(k)!.gameObject);
                var lateSpecial = typeof(SpecialBuildingModels).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var lateRural = typeof(RuralLife).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                for (int frame = 0; frame < 3; frame++)
                {
                    CollectionGeometryFixture.People(map, d, Array.Empty<Statecraft.Sim.Placement>(), c);
                    CollectionGeometryFixture.Rural(map);
                    CollectionGeometryFixture.Special(map, d);
                    lateSpecial.Invoke(layer, null); lateRural.Invoke(rural, null);
                    Assert.AreEqual(v, RuralLife.VersionOf(map), $"frame {frame}: the farm-scene plan's version did not move");
                    CollectionAssert.IsEmpty(asked, $"frame {frame}: nothing asked for again");
                    AnswerAll();
                    foreach (var kv in before)
                    {
                        var now = layer.transform.Find(kv.Key);
                        Assert.IsTrue(now != null && ReferenceEquals(kv.Value, now.gameObject), $"frame {frame}: {kv.Key} is the same object, not rebuilt");
                    }
                    Assert.AreEqual(before.Count, layer.transform.childCount, $"frame {frame}: no model added or removed");
                }
                // The event: a farm scene by the drawn well changes the plan.
                var well = layer.Placed.Single(p => p.Key == "beersheba_abrahams_well").At;
                rural.ShowForTest(rural.Wanted.Append(new RuralLife.Want("test_scene", RuralLife.Models[0], new Vector2(well.x, well.z), 0f)).ToList(),
                    rural.OthersShown.ToList());
                Assert.AreNotEqual(v, RuralLife.VersionOf(map), "premise: the farm-scene plan's version moved");
                CollectionGeometryFixture.Special(map, d); AnswerAll();
                Assert.IsTrue(before["latrun_tegart"] == null, "the event rebuilt the layer: the old fort object is destroyed");
            }
            finally { CivilianPeople.Request = savedPeople; RuralLife.Request = savedRural; }
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
        /// close zoom, and on the same spot when placed again. A major city's (TownModels.IsMajorCity) stands at the city's edge
        /// instead (owner, 6 November 2026: "make the workshop at the edge of the city"; <see cref="AtTheCitysEdge"/>).</summary>
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
            var streets = Streets(map, c, projects);
            AnswerAll();
            var footprints = map.KitFootprints.Select(f => (f.centre, f.radius)).ToList();
            float r0 = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            // A town whose footprint covers its own hex (Jerusalem and Tel Aviv today: footprints of several hexes,
            // FootprintRadiusM) leaves no ground there clear of it: the first ring of the search already leaves the hex, so its
            // b21 stands between the town's drawn buildings (owner, 2 November 2026: "Among the houses").
            var covered = new List<string>();
            foreach (var project in projects)
            {
                var hex = c.HexAt(project.Q, project.R)!; var w3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                float inside = SpecialBuildingModels.Inside(new Vector2(w3.x, w3.z), footprints);
                TestContext.WriteLine($"{project.City}: hex centre {inside:0} m inside a town's footprint");
                if (inside > 0f && inside + CivilianPeople.LabelClearanceM + 1.5f * r0 + r0 > SpecialBuildingModels.HexInnerRadiusM) covered.Add(project.City);
            }
            CollectionAssert.AreEquivalent(new[] { "jerusalem", "tel_aviv" }, covered, "the cities whose footprint covers their own hex");
            var keys = projects.Select(p => SpecialBuildingModels.WorkshopKey(p.Id)).ToList();
            CollectionAssert.IsSubsetOf(keys, layer.Placed.Select(p => p.Key), "a b21 at every built workshop");
            Assert.AreEqual(keys.Count, layer.Placed.Count(p => p.Model == SpecialBuildingModels.Workshop), "exactly one b21 per built workshop");
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
                // The owner, 6 November 2026 (verbatim): "make the workshop at the edge of the city": a major city's b21 stands at
                // its edge, off its project's hex where no edge spot is on it; every other city's wholly on its hex, as before.
                if (Statecraft.Game.Map.Kits.TownModels.IsMajorCity(project.City))
                {
                    var edge = new List<string>();
                    AtTheCitysEdge(map, project.City, key, flat, r, edge);
                    Assert.IsEmpty(edge, string.Join("\n", edge));
                }
                else
                {
                    Assert.LessOrEqual(off + r, SpecialBuildingModels.HexInnerRadiusM, key + " wholly on its project's hex");
                    if (covered.Contains(project.City))
                        AmongTheHouses(map, key, flat, r, streets);
                    else Assert.IsTrue(TownLandmarks.Clear(flat, r, footprints), key + " clear of the towns (and their labels)");
                }
                Assert.IsFalse(ground.OnRoute(flat, r), key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, After), key + " clear of the unit tokens");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, others), key + " clear of the sourced building models");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, spots), key + " clear of the other workshops");
                Assert.IsTrue(TownLandmarks.Clear(flat, r, props), key + " clear of the civilians, the wildlife and the farm scenes");
                spots.Add((flat, r));
                var go = layer.transform.Find(key)!;
                if (streets.ContainsKey(key))
                {
                    // On a street between a major city's kit blocks (#2876): turned about the vertical only, along the street.
                    var turn = go.localRotation * Quaternion.Inverse(prefab.transform.localRotation);
                    Assert.Greater(Vector3.Dot(turn * Vector3.up, Vector3.up), 0.9999f, key + " upright: turned only about the vertical");
                }
                else Assert.Less(Quaternion.Angle(prefab.transform.localRotation, go.localRotation), 0.01f, key + " keeps the FBX axis conversion");
                var rs = go.GetComponentsInChildren<Renderer>(true);
                var b = rs.Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.AreEqual(p.At.y, b.min.y, 1f, key + " stands on the ground");
                Assert.Less(b.size.y, Mathf.Max(b.size.x, b.size.z), key + " upright: lower than it is long");
                // The size is the model's own: a street workshop's world-axis bounds also take in its turn along the street
                // (#2876), so it is measured with that turn taken off for a moment (the FBX axis conversion alone), then put back.
                var sized = b;
                if (streets.ContainsKey(key))
                {
                    var along = go.localRotation;
                    go.localRotation = prefab.transform.localRotation;
                    sized = rs.Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                    go.localRotation = along;
                }
                Assert.AreEqual(SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), Mathf.Max(sized.size.x, sized.size.z), 1f, key + " at the buildings' one world scale");
                Assert.IsTrue(rs.All(x => x.forceRenderingOff), key + " hidden until the close zoom");
            }
            // Deterministic: a second layer from nothing puts every workshop on the same spot.
            var first = layer.Placed.Where(p => keys.Contains(p.Key)).ToDictionary(p => p.Key, p => p.At);
            UnityEngine.Object.DestroyImmediate(layer.gameObject);
            var again = CollectionGeometryFixture.Special(map, After, c, cities.Select((city, i) => Workshop(c, 100 + i, city, Built)).ToList())!;
            AnswerAll();
            foreach (var key in keys) Assert.AreEqual(first[key], again.Placed.Single(p => p.Key == key).At, key + " on the same spot again");
        }

        /// <summary>The street direction the plan gives each workshop that stands on a street between a major city's kit blocks
        /// (#2876; SpecialBuildingModels.PlanWithRoutes, the plan the layer draws), by key.</summary>
        static IReadOnlyDictionary<string, Vector2> Streets(MapScene map, Statecraft.Sim.Content c, IEnumerable<Statecraft.Sim.CityProject> projects)
        {
            var wants = SpecialBuildingModels.Placements(After).Concat(SpecialBuildingModels.ArmsWorkshops(c, projects)).ToList();
            SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out var along);
            return wants.Where(w => w.Model == SpecialBuildingModels.Workshop && along.ContainsKey(w.Key)).ToDictionary(w => w.Key, w => along[w.Key]);
        }

        /// <summary>Owner, 2 November 2026 ("Among the houses"): a b21 on a hex a town's footprint covers overlaps none of the
        /// drawn buildings, whatever they are (read from the map, not listed here): the town layer's models, and a major city's
        /// kit blocks, which it draws at every zoom in their place (#2876). A b21 on a street between kit blocks
        /// (<paramref name="streets"/>) is checked as its square along the street, SizeOf a side, against the blocks and the kit
        /// landmarks; else as its clearance disc. The names' pills are checked on screen by
        /// MapLabelTests.NoNamePillCoversAWorkshopModelInTelAviv and ...InJerusalem.</summary>
        static void AmongTheHouses(MapScene map, string key, Vector2 flat, float r, IReadOnlyDictionary<string, Vector2> streets)
        {
            var layer = map.GetComponentInChildren<Statecraft.Game.Map.Kits.TownModelLayer>(true);
            Assert.IsNotNull(layer, "the town layer");
            bool street = streets.TryGetValue(key, out var dir);
            float yaw = street ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f, size = SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop);
            bool Clear(Vector2 c, float boxYaw, float w, float d) =>
                street ? BoxesApart(flat, yaw, size, size, c, boxYaw, w, d) : SpecialBuildingModels.ClearOfBox(flat, r, c, boxYaw, w, d);
            int n = 0;
            foreach (var b in map.MajorCityBlocks)
            {
                if ((b.C - flat).magnitude > SpecialBuildingModels.HexInnerRadiusM + 0.5f * (b.W + b.D)) continue;
                n++;
                Assert.IsTrue(Clear(b.C, b.Yaw, b.W, b.D), $"{key} overlaps {b.Id}'s kit {b.Kind} at {b.C}");
            }
            foreach (var f in map.KitFootprints)
            {
                if (Statecraft.Game.Map.Kits.TownModels.IsMajorCity(f.id)) continue;   // its models are never drawn (#2876)
                foreach (var b in layer!.InstancesOf(f.id))
                {
                    n++;
                    // The building's footprint, a W x D rectangle turned by its yaw: its nearest point to the model's centre.
                    var local = Quaternion.Euler(0f, -b.Yaw, 0f) * new Vector3(flat.x - b.C.x, 0f, flat.y - b.C.y);
                    var near = new Vector2(Mathf.Clamp(local.x, -0.5f * b.W, 0.5f * b.W), Mathf.Clamp(local.z, -0.5f * b.D, 0.5f * b.D));
                    Assert.GreaterOrEqual((new Vector2(local.x, local.z) - near).magnitude, r, $"{key} overlaps {f.id}'s {b.Model} at {b.C}");
                }
            }
            Assert.Greater(n, 0, key + ": the town layer has drawn buildings to keep clear of");
            // Review of #2281 (B1): nor any wall, platform, landmark, harbour work or street palm the kits placed, any landmark's
            // close copy, or any replaceable landmark part (all house-free by design, so a gap search is drawn to them).
            var stats = map.KitStats;
            int m = 0;
            foreach (var l in stats.Landmarks)
            {
                if ((l.C - flat).magnitude > SpecialBuildingModels.HexInnerRadiusM + 0.5f * (l.W + l.D)) continue;
                m++;
                Assert.IsTrue(Clear(l.C, l.Yaw, l.W, l.D), $"{key} stands on {l.Id}'s {l.Kind} at {l.C}");
            }
            foreach (var part in stats.LandmarkParts)
                Assert.IsTrue(Clear(new Vector2(part.Near.center.x, part.Near.center.z), 0f, part.Near.size.x, part.Near.size.z),
                    $"{key} stands on {part.Owner}'s {part.Kind}");
            TestContext.WriteLine($"{key}: clear of {n} drawn buildings and {m} kit landmarks near it{(street ? $" (on a street at {yaw:0} deg)" : "")}");
            Assert.Greater(m, 0, key + ": the kits drew landmarks near it to keep clear of");
        }

        /// <summary>A workshop built on Jerusalem's or Tel Aviv's own hex, which the town's footprint covers, gets its b21 at the
        /// city's edge (owner, 6 November 2026, verbatim: "make the workshop at the edge of the city", which replaces "Among the
        /// houses" of 2 November 2026 for the major cities; <see cref="AtTheCitysEdge"/>), outside the Old City's walls, clear of
        /// the routes and the tokens, upright, and on the same spot when placed again; one plan with both stays under 50 ms.</summary>
        [Test]
        public void AWorkshopInJerusalemOrTelAvivStandsAtTheCitysEdge()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var projects = new[] { Workshop(c, 21, "jerusalem", Built), Workshop(c, 22, "tel_aviv", Built) };
            var layer = CollectionGeometryFixture.Special(map, After, c, projects)!;
            AnswerAll();
            var ground = PlacementLayer.CampGround(map);
            var first = new Dictionary<string, Vector3>();
            foreach (var project in projects)
            {
                string key = SpecialBuildingModels.WorkshopKey(project.Id);
                CollectionAssert.Contains(layer.Placed.Select(p => p.Key), key, project.City + " gets its b21");
                var p = layer.Placed.Single(x => x.Key == key); first[key] = p.At;
                var hex = c.HexAt(project.Q, project.R)!; var centre3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                var flat = new Vector2(p.At.x, p.At.z); float r = SpecialBuildingModels.ClearanceOf(p.Model);
                float off = (flat - new Vector2(centre3.x, centre3.z)).magnitude;
                TestContext.WriteLine($"{key} ({project.City}): {off:0} m from its hex centre");
                var edge = new List<string>();
                AtTheCitysEdge(map, project.City, key, flat, r, edge);
                Assert.IsEmpty(edge, string.Join("\n", edge));
                if (project.City == "jerusalem")
                {
                    var (oc, half) = Statecraft.Game.Map.Kits.SettlementKits.OldCityWalls(map.Frame);
                    var d = flat - oc;
                    TestContext.WriteLine($"{key}: {d.x:0} m east and {d.y:0} m north of the Old City's centre (walls' half side {half:0} m)");
                    Assert.IsTrue(Mathf.Abs(d.x) >= half + r || Mathf.Abs(d.y) >= half + r, key + " outside the Old City's walls");
                }
                Assert.IsFalse(ground.OnRoute(flat, r), key + " clear of the routes");
                Assert.IsFalse(ground.OnToken(flat, r, After), key + " clear of the unit tokens");
                var b = layer.transform.Find(key)!.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.Less(b.size.y, Mathf.Max(b.size.x, b.size.z), key + " upright");
            }
            UnityEngine.Object.DestroyImmediate(layer.gameObject);
            var again = CollectionGeometryFixture.Special(map, After, c, projects)!;
            AnswerAll();
            foreach (var kv in first) Assert.AreEqual(kv.Value, again.Placed.Single(p => p.Key == kv.Key).At, kv.Key + " on the same spot again");
            // The load gate's rebuild line: one full plan (the sourced buildings and both workshops at their city's edge), as
            // each Rebuild computes it.
            var wants = SpecialBuildingModels.Placements(After).Concat(SpecialBuildingModels.ArmsWorkshops(c, projects)).ToList();
            SpecialBuildingModels.Plan(map, After, wants, _ => true, log: false);   // warm
            var clock = System.Diagnostics.Stopwatch.StartNew();
            const int Runs = 5;
            for (int i = 0; i < Runs; i++) SpecialBuildingModels.Plan(map, After, wants, _ => true, log: false);
            double ms = clock.Elapsed.TotalMilliseconds / Runs;
            TestContext.WriteLine($"PLAN-TIMING Jerusalem+Tel Aviv workshops: {ms:0.0} ms per plan");
            // A design bound, not a budget: a plan runs on a prefab answer or a change of date or workshops, never per frame.
            Assert.Less(ms, 50.0, "one plan with two workshops at their city's edge");
        }

        /// <summary>#2876 ("Big blocks, every zoom", owner, 6 November 2026): the four major cities (TownModels.IsMajorCity) draw
        /// their kit blocks at every zoom and never their building models, so a b21 among their houses (owner, 2 November 2026:
        /// "Among the houses") and Tal Heuberger's props at their edge keep off those blocks (MapScene.MajorCityBlocks), not off
        /// the models no longer drawn there. A workshop on each major city's own hex gets a planned spot (at the city's edge,
        /// on or off the hex: owner, 6 November 2026, "make the workshop at the edge of the city"), whose ground (its clearance
        /// disc, or on a street its square, SizeOf a side, along the street) overlaps no major city's kit block; the Altalena sandbags (15 June 1948) and the Haifa quay crates, where planned, keep off them too.
        /// The plan is the one the layer draws (every model available), placed twice to the same spots.</summary>
        [Test]
        public void AWorkshopOrPropInAMajorCityKeepsOffItsKitBlocks()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var blocks = map.MajorCityBlocks;
            var majors = c.SettlementHexes.Select(h => h.Id).Where(Statecraft.Game.Map.Kits.TownModels.IsMajorCity).OrderBy(x => x, StringComparer.Ordinal).ToList();
            CollectionAssert.AreEqual(new[] { "haifa", "jaffa", "jerusalem", "tel_aviv" }, majors, "the major cities with a settlement hex");
            foreach (var city in majors) Assert.Greater(blocks.Count(b => b.Id == city), 0, city + " has kit blocks to keep clear of");
            Assert.IsTrue(blocks.All(b => majors.Contains(b.Id) && (b.Kind == "building" || b.Kind == "wing")), "only the major cities' buildings and wings");
            var projects = majors.Select((city, i) => Workshop(c, 700 + i, city, Built)).ToList();
            var wants = SpecialBuildingModels.Placements(After).Concat(SpecialBuildingModels.ArmsWorkshops(c, projects)).ToList();
            var plan = SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out var along);
            var again = SpecialBuildingModels.Plan(map, After, wants, _ => true, log: false);
            CollectionAssert.AreEqual(plan.Select(x => (x.want.Key, x.at)).ToList(), again.Select(x => (x.want.Key, x.at)).ToList(), "the same spots when planned again");
            float size = SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), r = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            foreach (var project in projects)
            {
                string key = SpecialBuildingModels.WorkshopKey(project.Id);
                CollectionAssert.Contains(plan.Select(x => x.want.Key).ToList(), key, project.City + "'s workshop gets a planned spot");
                var at = plan.Single(x => x.want.Key == key).at;
                var hex = c.HexAt(project.Q, project.R)!; var centre3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                float off = (at - new Vector2(centre3.x, centre3.z)).magnitude;
                // Owner, 6 November 2026 (verbatim): "make the workshop at the edge of the city": it may stand off its hex.
                bool street = along.TryGetValue(key, out var dir);
                float yaw = street ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;
                int near = 0;
                foreach (var b in blocks)
                {
                    if ((b.C - at).magnitude > r + 0.5f * (b.W + b.D)) continue;
                    near++;
                    if (street)
                        Assert.IsTrue(BoxesApart(at, yaw, size, size, b.C, b.Yaw, b.W, b.D),
                            $"{key} ({project.City}): its {size:0} m square along the street at {at} overlaps {b.Id}'s {b.Kind} at {b.C} ({b.W:0} x {b.D:0} m, {b.Yaw:0} deg)");
                    else
                        Assert.IsTrue(SpecialBuildingModels.ClearOfBox(at, r, b.C, b.Yaw, b.W, b.D),
                            $"{key} ({project.City}): its {r:0} m disc at {at} overlaps {b.Id}'s {b.Kind} at {b.C} ({b.W:0} x {b.D:0} m, {b.Yaw:0} deg)");
                }
                TestContext.WriteLine($"{key} ({project.City}): {off:0} m from its hex centre{(street ? $", on a street at {yaw:0} deg" : "")}; {near} kit blocks within reach");
            }
            // Tal Heuberger's props at a major city's edge (Want.TownEdge), on the day both stand.
            var date = new DateTime(1948, 6, 15);
            var props = SpecialBuildingModels.Plan(map, date, SpecialBuildingModels.Placements(date), _ => true, log: false);
            foreach (var key in new[] { "tel_aviv_altalena_sandbags", "haifa_quay_crates" })
            {
                var row = props.Where(x => x.want.Key == key).ToList();
                if (row.Count == 0) { TestContext.WriteLine($"{key}: not planned on 15 June 1948"); continue; }
                var (want, at) = row[0]; float pr = SpecialBuildingModels.ClearanceOf(want);
                foreach (var b in blocks)
                    Assert.IsTrue(SpecialBuildingModels.ClearOfBox(at, pr, b.C, b.Yaw, b.W, b.D), $"{key} at {at} overlaps {b.Id}'s {b.Kind} at {b.C}");
                TestContext.WriteLine($"{key}: at {at}, clear of the major cities' kit blocks");
            }
        }

        /// <summary>#2876, coordinator's captures of 6 November 2026: clear of the major cities' kit blocks, the Tel Aviv b21 (on a
        /// street between two tall blocks) and the Jerusalem b21 (in a gap with a tall block south of it) were still hidden. The
        /// camera looks north (CameraRig yaw 0; fixed north under gdd-0.3, the only ruleset with workshops,
        /// CameraHeadingPolicy.IsFixedNorth) and down at CameraRig.Pitch on the screen's centre row, less toward the top of the
        /// screen, to CameraRig.Pitch - CameraRig.Fov / 2 at its top edge: the shallowest ray, the one a block hides most behind.
        /// For each major city's workshop, as the layer draws it, a 5 by 5 grid of points on its top (the drawn model's highest
        /// point, over its square, SizeOf a side, turned along its street if it has one) each sees the camera along that
        /// shallowest ray: the ray toward the camera (south and up) meets no major-city kit block, each taken as its box (the
        /// census footprint, Placed.C, Yaw, W, D) from the ground up to its wall top (the highest of the ground at its corners
        /// and centre, where the kit stands it, plus Placed.H). Also, again: each workshop gets a spot (on or off its hex: owner,
        /// 6 November 2026, "make the workshop at the edge of the city"), clear of the blocks, drawn where it was planned, and the
        /// plan is the same when made twice. Every city's failures are listed together, by city.</summary>
        [Test]
        public void AWorkshopInAMajorCityIsInTheCamerasSightPastItsKitBlocks()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var blocks = map.MajorCityBlocks;
            var majors = c.SettlementHexes.Select(h => h.Id).Where(Statecraft.Game.Map.Kits.TownModels.IsMajorCity).OrderBy(x => x, StringComparer.Ordinal).ToList();
            CollectionAssert.AreEqual(new[] { "haifa", "jaffa", "jerusalem", "tel_aviv" }, majors, "the major cities with a settlement hex");
            var projects = majors.Select((city, i) => Workshop(c, 700 + i, city, Built)).ToList();
            var wants = SpecialBuildingModels.Placements(After).Concat(SpecialBuildingModels.ArmsWorkshops(c, projects)).ToList();
            var plan = SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out var along);
            var again = SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out var alongAgain);
            CollectionAssert.AreEqual(plan.Select(x => (x.want.Key, x.at)).ToList(), again.Select(x => (x.want.Key, x.at)).ToList(), "the same spots when planned again");
            CollectionAssert.AreEquivalent(along.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(), alongAgain.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(),
                "the same streets when planned again");
            var layer = CollectionGeometryFixture.Special(map, After, c, projects)!;
            AnswerAll();
            var ground = PlacementLayer.CampGround(map);
            var toCamera = ToCamera();
            var tops = BlockTops(map, ground);
            float size = SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), r = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            var failures = new List<string>();
            foreach (var project in projects)
            {
                string city = project.City, key = SpecialBuildingModels.WorkshopKey(project.Id);
                var row = plan.Where(x => x.want.Key == key).ToList();
                if (row.Count == 0) { failures.Add($"{city}: {key} gets no planned spot, so it is not drawn"); continue; }
                var at = row[0].at;
                var hex = c.HexAt(project.Q, project.R)!; var centre3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                float off = (at - new Vector2(centre3.x, centre3.z)).magnitude;
                // The owner, 6 November 2026 (verbatim): "make the workshop at the edge of the city": a major city's b21 may
                // stand off its project's hex (AWorkshopInAMajorCityStandsAtTheCitysEdge), so its hex is not asserted here.
                bool street = along.TryGetValue(key, out var dir);
                float yaw = street ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;
                foreach (var b in blocks)
                {
                    if ((b.C - at).magnitude > r + 0.5f * (b.W + b.D)) continue;
                    bool clear = street ? BoxesApart(at, yaw, size, size, b.C, b.Yaw, b.W, b.D) : SpecialBuildingModels.ClearOfBox(at, r, b.C, b.Yaw, b.W, b.D);
                    if (!clear) failures.Add($"{city}: {key} at {at} overlaps {b.Id}'s {b.Kind} at {b.C}");
                }
                var model = layer.transform.Find(key);
                if (model == null) { failures.Add($"{city}: {key} is planned at {at} but not drawn"); continue; }
                var mb = model.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                if ((new Vector2(mb.center.x, mb.center.z) - at).magnitude > 1f) failures.Add($"{city}: {key} is drawn at {mb.center}, not at its planned {at}");
                var (hidden, first) = HiddenOnTop(map, tops, at, yaw, size, mb.max.y, toCamera);
                TestContext.WriteLine(FormattableString.Invariant(
                    $"{key} ({city}): at {at}, {off:0} m from its hex centre{(street ? $", on a street at {yaw:0} deg" : "")}, top {mb.max.y:0} m; {hidden} of 25 points on its top hidden at {Shallowest:0.0} deg"));
                if (hidden > 0) failures.Add($"{city}: {key}: {hidden} of 25 points on its top are hidden from the camera at {Shallowest:0.0} deg; first: {first}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>The view's shallowest ray's depression (degrees): the screen's top edge, CameraRig.Pitch - Fov / 2.</summary>
        static float Shallowest => CameraRig.Pitch - 0.5f * CameraRig.Fov;

        /// <summary>The ray from a point toward the camera: the camera turns Euler(pitch, yaw 0, 0) and stands at LookAt - forward x
        /// Distance (CameraRig.Apply), so back along forward; at the shallowest pitch.</summary>
        static Vector3 ToCamera()
        {
            var toCamera = -(Quaternion.Euler(Shallowest, 0f, 0f) * Vector3.forward);
            Assert.Greater(toCamera.y, 0f, "the camera is above the ground");
            Assert.Less(toCamera.z, 0f, "the camera is south of what it looks at (x east, z north)");
            Assert.AreEqual(0f, toCamera.x, 1e-6f, "the camera looks due north");
            return toCamera;
        }

        /// <summary>Each major-city block's wall top (world y), in MapScene.MajorCityBlocks order: its wall height over the ground
        /// the kit stands it on (SettlementKits.GroundSpan's: the highest of its corners and centre).</summary>
        static List<float> BlockTops(MapScene map, Statecraft.Game.Map.Kits.KitGround ground) => map.MajorCityBlocks.Select(b =>
        {
            float a = b.Yaw * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a), hi = float.MinValue;
            for (int i = 0; i < 5; i++)
            {
                float u = i == 4 ? 0f : ((i & 1) == 0 ? -0.5f : 0.5f) * b.W, v = i == 4 ? 0f : ((i & 2) == 0 ? -0.5f : 0.5f) * b.D;
                hi = Mathf.Max(hi, ground.Y(b.C.x + u * ca - v * sa, b.C.y + u * sa + v * ca));
            }
            return hi + b.H;
        }).ToList();

        /// <summary>How many of 25 points on a b21's top (a 5 by 5 grid over its square, <paramref name="size"/> a side, turned
        /// <paramref name="yaw"/>, inset 2% so a point is on the top, not its edge, at world y <paramref name="top"/>) a major-city
        /// block hides from the camera (<see cref="RayMeetsBox"/> along <paramref name="toCamera"/>), and the first.</summary>
        static (int hidden, string? first) HiddenOnTop(MapScene map, List<float> tops, Vector2 at, float yaw, float size, float top, Vector3 toCamera)
        {
            var blocks = map.MajorCityBlocks;
            float ya = yaw * Mathf.Deg2Rad, half = 0.49f * size;
            var su = new Vector2(Mathf.Cos(ya), Mathf.Sin(ya)); var sv = new Vector2(-su.y, su.x);
            int hidden = 0; string? first = null;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    var p2 = at + su * (half * (i / 2f - 1f)) + sv * (half * (j / 2f - 1f));
                    var q = new Vector3(p2.x, top, p2.y);
                    for (int k = 0; k < blocks.Count; k++)
                    {
                        var b = blocks[k];
                        if (!RayMeetsBox(q, toCamera, b.C, b.Yaw, b.W, b.D, tops[k])) continue;
                        hidden++;
                        first ??= FormattableString.Invariant($"{b.Id}'s {b.Kind} at {b.C} ({b.W:0} x {b.D:0} m, {b.Yaw:0} deg, top {tops[k]:0} m) hides its top at {q}");
                        break;
                    }
                }
            return (hidden, first);
        }

        /// <summary>How far past the outermost kit block of its city a major city's b21 may stand (m), centre to the block's
        /// nearest point: the layer's design bound of 1 km past the block's face to the model's ground, plus the model's clearance
        /// disc (r) and the band r either side of its bearing that the outermost block is taken from.</summary>
        static float EdgeReach(float r) => 1000f + 2f * r;

        /// <summary>The owner, 6 November 2026, on #2876 (verbatim): "make the workshop at the edge of the city". Whether the b21
        /// planned at <paramref name="at"/> (its clearance disc <paramref name="r"/>) stands at the edge of the major city
        /// <paramref name="city"/>, as this test reads it off the map (the kit census, the footprints, the ground), not off the
        /// layer's search; each miss is added to <paramref name="failures"/>, naming the city:
        /// - clear of every major city's kit block (its disc against each census box);
        /// - past the city's blocks: no block of the city that the band <paramref name="r"/> either side of the ray from the city's
        ///   centre through the spot meets reaches farther out along that ray than the disc's near edge;
        /// - near them: within <see cref="EdgeReach"/> of the nearest block of the city;
        /// - the city's ground: deeper in the city's footprint (distance over radius, the kits' partition,
        ///   SettlementKits.OwnsFootprint) than in any other place's, and clear of every other place's plot reach
        ///   (SettlementKits.PlotReach of its radius), where its buildings stand;
        /// - clear of every place's name pill (<see cref="SpecialBuildingModels.ClearOfName"/>);
        /// - on land, off the beach: its centre dry, and no open sea within the drawn full sand (BeachBands.FullSandAt) plus r.</summary>
        static void AtTheCitysEdge(MapScene map, string city, string key, Vector2 at, float r, List<string> failures)
        {
            string who = FormattableString.Invariant($"{city}: {key} at ({at.x:0}, {at.y:0})");
            var fp = map.KitFootprints.Single(f => f.id == city);
            var own = map.MajorCityBlocks.Where(b => b.Id == city).ToList();
            if (own.Count == 0) { failures.Add(who + ": the city has no kit blocks"); return; }
            foreach (var b in map.MajorCityBlocks)
                if (!SpecialBuildingModels.ClearOfBox(at, r, b.C, b.Yaw, b.W, b.D))
                    failures.Add(FormattableString.Invariant($"{who} overlaps {b.Id}'s {b.Kind} at {b.C}"));
            var toward = at - fp.centre; float dist = toward.magnitude;
            var u = toward / dist; var n = new Vector2(-u.y, u.x);
            float nearest = float.MaxValue;
            foreach (var b in own)
            {
                float a = b.Yaw * Mathf.Deg2Rad;
                var bu = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var bv = new Vector2(-bu.y, bu.x);
                float hu = 0.5f * b.W * Mathf.Abs(Vector2.Dot(bu, u)) + 0.5f * b.D * Mathf.Abs(Vector2.Dot(bv, u));
                float hn = 0.5f * b.W * Mathf.Abs(Vector2.Dot(bu, n)) + 0.5f * b.D * Mathf.Abs(Vector2.Dot(bv, n));
                float tc = Vector2.Dot(b.C - fp.centre, u), sc = Vector2.Dot(b.C - fp.centre, n);
                if (Mathf.Abs(sc) - hn < r - 0.5f && tc + hu > 0f && tc + hu > dist - r + 0.5f)
                    failures.Add(FormattableString.Invariant($"{who} is not past {city}'s {b.Kind} at {b.C} on its bearing: the block reaches {tc + hu:0} m out, the disc from {dist - r:0} m"));
                var q = b.C - at; float qu = q.x * bu.x + q.y * bu.y, qv = q.x * bv.x + q.y * bv.y;
                float du = Mathf.Max(0f, Mathf.Abs(qu) - 0.5f * b.W), dv = Mathf.Max(0f, Mathf.Abs(qv) - 0.5f * b.D);
                nearest = Mathf.Min(nearest, Mathf.Sqrt(du * du + dv * dv));
            }
            if (nearest > EdgeReach(r)) failures.Add(FormattableString.Invariant($"{who} is {nearest:0} m from {city}'s nearest kit block, past {EdgeReach(r):0} m"));
            float mine = dist / fp.radius;
            foreach (var f in map.KitFootprints)
            {
                if (!SpecialBuildingModels.ClearOfName(at, r, f.centre, f.radius)) failures.Add($"{who} is under {f.id}'s name pill");
                if (f.id == city) continue;
                float d = (at - f.centre).magnitude;
                if (d / f.radius <= mine) failures.Add(FormattableString.Invariant($"{who} reads as {f.id}'s: {d / f.radius:0.00} of its radius, {mine:0.00} of {city}'s"));
                if (d < f.radius * Statecraft.Game.Map.Kits.SettlementKits.PlotReach + r) failures.Add($"{who} is on {f.id}'s ground (its plot reach)");
            }
            var ground = PlacementLayer.CampGround(map);
            if (ground.Water(at.x, at.y)) failures.Add(who + " is in the water");
            var reaches = Statecraft.Game.Map.Kits.BeachBands.Reaches(map.Settlements.Concat(map.Localities.Select(l => l.Settlement)).ToList(), ground);
            float sand = Statecraft.Game.Map.Kits.BeachBands.FullSandAt(map.Frame, at.x, at.y, reaches);
            if (ground.SeaDistance(at.x, at.y, sand + r) != float.MaxValue) failures.Add(FormattableString.Invariant($"{who} is on the beach (the sea within {sand:0} m of full sand plus its {r:0} m)"));
            TestContext.WriteLine(FormattableString.Invariant($"{key} ({city}): {Offset(at, fp.centre)} from the city's centre ({mine:0.00} of its radius); {nearest:0} m from its nearest kit block"));
        }

        /// <summary>#2876, the owner, 6 November 2026 (verbatim): "make the workshop at the edge of the city". A workshop built on
        /// each major city's hex (haifa, jaffa, jerusalem, tel_aviv: TownModels.IsMajorCity, which draw their tall kit blocks at
        /// every zoom) gets a planned spot (none is a failure naming the city: the layer then draws no b21), at the city's edge
        /// (<see cref="AtTheCitysEdge"/>), drawn where planned, whose top the camera sees past every major city's kit blocks
        /// along the view's shallowest ray (<see cref="HiddenOnTop"/>, the test's own ray test), and the plan is the same when
        /// made twice. It may stand off its project's hex where no edge spot is on it; each spot's offset from its hex is
        /// logged. Every city's failures are listed together, by city.</summary>
        [Test]
        public void AWorkshopInAMajorCityStandsAtTheCitysEdge()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var majors = c.SettlementHexes.Select(h => h.Id).Where(Statecraft.Game.Map.Kits.TownModels.IsMajorCity).OrderBy(x => x, StringComparer.Ordinal).ToList();
            CollectionAssert.AreEqual(new[] { "haifa", "jaffa", "jerusalem", "tel_aviv" }, majors, "the major cities with a settlement hex");
            var projects = majors.Select((city, i) => Workshop(c, 700 + i, city, Built)).ToList();
            var wants = SpecialBuildingModels.Placements(After).Concat(SpecialBuildingModels.ArmsWorkshops(c, projects)).ToList();
            var plan = SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out var along);
            var again = SpecialBuildingModels.PlanWithRoutes(map, After, wants, _ => true, false, null, out _);
            CollectionAssert.AreEqual(plan.Select(x => (x.want.Key, x.at)).ToList(), again.Select(x => (x.want.Key, x.at)).ToList(), "the same spots when planned again");
            var layer = CollectionGeometryFixture.Special(map, After, c, projects)!;
            AnswerAll();
            var ground = PlacementLayer.CampGround(map);
            var toCamera = ToCamera();
            var tops = BlockTops(map, ground);
            float size = SpecialBuildingModels.SizeOf(SpecialBuildingModels.Workshop), r = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            var failures = new List<string>();
            foreach (var project in projects)
            {
                string city = project.City, key = SpecialBuildingModels.WorkshopKey(project.Id);
                var row = plan.Where(x => x.want.Key == key).ToList();
                if (row.Count == 0) { failures.Add($"{city}: {key} gets no planned spot, so it is not drawn"); continue; }
                var at = row[0].at;
                var hex = c.HexAt(project.Q, project.R)!; var centre3 = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
                var hexCentre = new Vector2(centre3.x, centre3.z);
                bool onHex = (at - hexCentre).magnitude + r <= SpecialBuildingModels.HexInnerRadiusM;
                TestContext.WriteLine($"{key} ({city}): {Offset(at, hexCentre)} from its hex centre, {(onHex ? "wholly on" : "off")} its project's hex");
                AtTheCitysEdge(map, city, key, at, r, failures);
                bool street = along.TryGetValue(key, out var dir);
                float yaw = street ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg : 0f;
                var model = layer.transform.Find(key);
                if (model == null) { failures.Add($"{city}: {key} is planned at {at} but not drawn"); continue; }
                var mb = model.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                if ((new Vector2(mb.center.x, mb.center.z) - at).magnitude > 1f) failures.Add($"{city}: {key} is drawn at {mb.center}, not at its planned {at}");
                var (hidden, first) = HiddenOnTop(map, tops, at, yaw, size, mb.max.y, toCamera);
                if (hidden > 0) failures.Add($"{city}: {key}: {hidden} of 25 points on its top are hidden from the camera at {Shallowest:0.0} deg; first: {first}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>A point's offset from a centre: metres, compass bearing (0 north, 90 east) and the world point.</summary>
        static string Offset(Vector2 p, Vector2 centre)
        {
            var d = p - centre;
            float bearing = Mathf.Repeat(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 360f);
            return FormattableString.Invariant($"{d.magnitude:0} m at bearing {bearing:0} deg ({p.x:0}, {p.y:0})");
        }

        /// <summary>Whether the ray from <paramref name="q"/> along <paramref name="dir"/> (rising: dir.y &gt; 0) meets the upright box
        /// centred at <paramref name="c"/> (x, z), turned <paramref name="yawDeg"/> anticlockwise in x-z (KitMesh.Corners), w along its
        /// turn and d across, from below the ground up to <paramref name="top"/> (world y). Slabs: the ray is inside the box for
        /// the parameters t within both side slabs and below the top; touching counts as missing.</summary>
        static bool RayMeetsBox(Vector3 q, Vector3 dir, Vector2 c, float yawDeg, float w, float d, float top)
        {
            if (q.y >= top) return false;   // the ray rises, so from above the top it never comes down into the box
            float a = yawDeg * Mathf.Deg2Rad;
            var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var v = new Vector2(-u.y, u.x);
            var rel = new Vector2(q.x - c.x, q.z - c.y); var flat = new Vector2(dir.x, dir.z);
            float t0 = 0f, t1 = (top - q.y) / dir.y;
            foreach (var (axis, halfSide) in new[] { (u, 0.5f * w), (v, 0.5f * d) })
            {
                float o = Vector2.Dot(rel, axis), s = Vector2.Dot(flat, axis);
                if (Mathf.Abs(s) < 1e-7f) { if (Mathf.Abs(o) >= halfSide) return false; continue; }
                float ta = (-halfSide - o) / s, tb = (halfSide - o) / s;
                t0 = Mathf.Max(t0, Mathf.Min(ta, tb)); t1 = Mathf.Min(t1, Mathf.Max(ta, tb));
                if (t0 >= t1) return false;
            }
            return true;
        }

        /// <summary>Whether two boxes, each centred at c, turned yaw degrees anticlockwise in x-z (KitMesh.Corners), w along its turn
        /// and d across, are apart (touching counts as apart): some side's axis separates them (the separating axis test).</summary>
        static bool BoxesApart(Vector2 c1, float yaw1, float w1, float d1, Vector2 c2, float yaw2, float w2, float d2)
        {
            float a1 = yaw1 * Mathf.Deg2Rad, a2 = yaw2 * Mathf.Deg2Rad;
            var u1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)); var v1 = new Vector2(-u1.y, u1.x);
            var u2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)); var v2 = new Vector2(-u2.y, u2.x);
            foreach (var n in new[] { u1, v1, u2, v2 })
            {
                float reach1 = 0.5f * w1 * Mathf.Abs(Vector2.Dot(u1, n)) + 0.5f * d1 * Mathf.Abs(Vector2.Dot(v1, n));
                float reach2 = 0.5f * w2 * Mathf.Abs(Vector2.Dot(u2, n)) + 0.5f * d2 * Mathf.Abs(Vector2.Dot(v2, n));
                if (Mathf.Abs(Vector2.Dot(c2 - c1, n)) >= reach1 + reach2) return true;
            }
            return false;
        }

        /// <summary>Review of #2281 (N3): every building takes its planned ground whether or not its model has streamed. A
        /// workshop model is wanted 400 m from the Tegart fort's planned spot, after the fort; with the fort's bundle failing
        /// (answered null), every building drawn stands exactly where the plan with every model puts it: the workshop does not
        /// slide onto the fort's ground.</summary>
        [Test]
        public void AFailedModelKeepsItsGroundSoNoOtherBuildingMoves()
        {
            var map = OpenMap();
            var layer = CollectionGeometryFixture.Special(map, After)!;
            var fortAt = SpecialBuildingModels.Plan(map, After, SpecialBuildingModels.Placements(After), _ => true, log: false).Single(x => x.want.Key == "latrun_tegart").at;
            // 400 m off the fort, on the first side (east, north, west, south) where the overlap is forced: planned with the fort
            // taking no ground (the fort's want left out), the workshop stands on the fort's ground; planned with every model, it
            // has ground elsewhere. So a layer that let a failed model's ground go would draw it on the fort's ground.
            float reach = SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Tegart) + SpecialBuildingModels.ClearanceOf(SpecialBuildingModels.Workshop);
            string key41 = SpecialBuildingModels.WorkshopKey(41);
            List<SpecialBuildingModels.Want> wants = null!;
            Dictionary<string, Vector2> full = null!;
            bool forced = false;
            foreach (var side in new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down })
            {
                var near = fortAt + 400f * side;
                var (lon, lat) = map.Frame.ToLonLat(new Vector3(near.x, 0f, near.y));
                wants = SpecialBuildingModels.Placements(After)
                    .Append(new SpecialBuildingModels.Want(key41, SpecialBuildingModels.Workshop, Built, lon, lat, "test: by the fort's spot"))
                    .ToList();
                full = SpecialBuildingModels.Plan(map, After, wants, _ => true, log: false).ToDictionary(x => x.want.Key, x => x.at);
                var withoutFort = SpecialBuildingModels.Plan(map, After, wants.Where(w => w.Key != "latrun_tegart").ToList(), _ => true, log: false)
                    .Where(x => x.want.Key == key41).Select(x => (Vector2?)x.at).FirstOrDefault();
                if (!full.ContainsKey(key41) || withoutFort == null || (withoutFort.Value - fortAt).magnitude >= reach) continue;
                TestContext.WriteLine($"the workshop 400 m {side} of the fort: {(withoutFort.Value - fortAt).magnitude:0} m from it if the fort took no ground, {(full[key41] - fortAt).magnitude:0} m with it");
                forced = true; break;
            }
            Assert.IsTrue(forced, "a side where the workshop would stand on the fort's ground if the fort took none");
            layer.ShowForTest(wants);
            Assert.IsTrue(full.ContainsKey(SpecialBuildingModels.WorkshopKey(41)), "the workshop has ground in the full plan");
            LogAssert.Expect(LogType.Warning, new Regex("^BUILDINGS: " + SpecialBuildingModels.Tegart + " did not stream"));
            var pending = asked.ToList(); asked.Clear();
            foreach (var (id, done) in pending)
                done(id == SpecialBuildingModels.Tegart ? null : AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near)));
            CollectionAssert.DoesNotContain(layer.Placed.Select(p => p.Model), SpecialBuildingModels.Tegart);
            CollectionAssert.Contains(layer.Placed.Select(p => p.Key), SpecialBuildingModels.WorkshopKey(41));
            var drawn = layer.Placed.Single(p => p.Key == key41);
            Assert.GreaterOrEqual((new Vector2(drawn.At.x, drawn.At.z) - fortAt).magnitude, reach, "the workshop is not drawn on the failed fort's ground");
            foreach (var p in layer.Placed)
            {
                Assert.AreEqual(full[p.Key].x, p.At.x, 1e-3f, p.Key + " where the full plan puts it (x)");
                Assert.AreEqual(full[p.Key].y, p.At.z, 1e-3f, p.Key + " where the full plan puts it (z)");
            }
        }

        /// <summary>Review of #2281 (N1, N2): ClearOfName's band, edge by edge: from the footprint's south edge to
        /// TownLabelHeightM past its north edge, TownLabelHalfWidthM either side, widened by the model's clearance.</summary>
        [Test]
        public void ClearOfNameIsTheBandAboveAPlacesKit()
        {
            var c = new Vector2(1000f, 2000f); float fr = 800f, r = 200f;
            float w = SpecialBuildingModels.TownLabelHalfWidthM, h = SpecialBuildingModels.TownLabelHeightM;
            Assert.IsFalse(SpecialBuildingModels.ClearOfName(c, r, c, fr), "on the place");
            Assert.IsFalse(SpecialBuildingModels.ClearOfName(c + new Vector2(0f, fr + h), r, c, fr), "in the pill above it");
            Assert.IsFalse(SpecialBuildingModels.ClearOfName(c + new Vector2(w + r - 1f, 0f), r, c, fr), "just inside the side");
            Assert.IsTrue(SpecialBuildingModels.ClearOfName(c + new Vector2(w + r + 1f, 0f), r, c, fr), "just outside the side");
            Assert.IsTrue(SpecialBuildingModels.ClearOfName(c + new Vector2(-w - r - 1f, 0f), r, c, fr), "just outside the other side");
            Assert.IsFalse(SpecialBuildingModels.ClearOfName(c + new Vector2(0f, fr + h + r - 1f), r, c, fr), "just under the top");
            Assert.IsTrue(SpecialBuildingModels.ClearOfName(c + new Vector2(0f, fr + h + r + 1f), r, c, fr), "just over the top");
            Assert.IsFalse(SpecialBuildingModels.ClearOfName(c + new Vector2(0f, -fr - r + 1f), r, c, fr), "just over the bottom");
            Assert.IsTrue(SpecialBuildingModels.ClearOfName(c + new Vector2(0f, -fr - r - 1f), r, c, fr), "just under the bottom");
        }

        /// <summary>Review of #2281 (M1): the b21 boxes the labels keep clear of are published as a new list on every change and
        /// a list once handed out never changes. A b21 that streams in after the labels read the boxes, and a second workshop
        /// built later, each reach the labels as a new list; the earlier lists keep what they held.</summary>
        [Test]
        public void TheWorkshopBoxesReachTheLabelsAsNewListsAndNeverChangeAfter()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var cam = Camera.main!;
            cam.transform.SetPositionAndRotation(new Vector3(0f, Statecraft.Game.Map.Kits.TownModelLayer.CloseEnterM - 1000f, 0f), Quaternion.Euler(90f, 0f, 0f));
            var one = new[] { Workshop(c, 51, "rehovot", Built) };
            var layer = CollectionGeometryFixture.Special(map, After, c, one)!;   // b21 asked for, not answered yet
            layer.Choose(default, cam);
            var before = layer.KeepClearFor(cam);
            Assert.AreEqual(0, before.Count, "nothing drawn yet");
            AnswerAll();   // b21 streams in after the labels read the boxes
            var first = layer.KeepClearFor(cam);
            Assert.AreNotSame(before, first, "a new list once the b21 is drawn");
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(0, before.Count, "the list handed out before is unchanged");
            CollectionGeometryFixture.Special(map, After, c, new[] { one[0], Workshop(c, 52, "hadera", Built) });   // a second workshop
            AnswerAll();
            layer.Choose(default, cam);
            var second = layer.KeepClearFor(cam);
            Assert.AreNotSame(first, second, "a new list for the second b21");
            Assert.AreEqual(2, second.Count);
            Assert.AreEqual(1, first.Count, "the list handed out before is unchanged");
        }

        /// <summary>Review of #2281 (S2): a workshop's spot reads only planned data (the map's unit spots, the day it was built),
        /// never the game's later date, so it stands on the same spot at a later date (past the dated tokens' last days).</summary>
        [Test]
        public void AWorkshopStandsOnTheSameSpotAsTheGameDatePasses()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var projects = c.Cities.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).Select((city, i) => Workshop(c, 600 + i, city, Built)).ToList();
            var wants = SpecialBuildingModels.ArmsWorkshops(c, projects);
            var tokens = map.UnitTokens.Count(t => t.until > Built && t.until <= After);
            TestContext.WriteLine($"{tokens} dated unit tokens end between the build and the later date");
            Assert.Greater(tokens, 0, "premise: some dated tokens end in between");
            var atBuilt = SpecialBuildingModels.Plan(map, Built, wants, _ => true, log: false).ToDictionary(x => x.want.Key, x => x.at);
            var later = SpecialBuildingModels.Plan(map, After, wants, _ => true, log: false).ToDictionary(x => x.want.Key, x => x.at);
            CollectionAssert.AreEquivalent(atBuilt.Keys, later.Keys, "the same workshops stand");
            foreach (var kv in atBuilt) Assert.AreEqual(kv.Value, later[kv.Key], kv.Key + " on the same spot at the later date");
        }

        /// <summary>Review of #2281 (N1): the ground a unit spot's figures hide (SpecialBuildingModels.Hidden), edge by edge: north
        /// of the spot to FigureShadowM past its disc, the disc's width either side, widened by the model's clearance.</summary>
        [Test]
        public void TheGroundAUnitsFiguresHideIsNorthOfItsSpot()
        {
            var c = new Vector2(1000f, 2000f); float R = 500f, r = 200f, s = SpecialBuildingModels.FigureShadowM;
            Assert.Greater(s, 2000f, "the figures are about 2.5 km tall, seen at 50 degrees");
            Assert.IsTrue(SpecialBuildingModels.Hidden(c + new Vector2(0f, R + s - 1f), 0f, c, R), "just inside the shadow's far end");
            Assert.IsFalse(SpecialBuildingModels.Hidden(c + new Vector2(0f, R + s + r + 1f), r, c, R), "just beyond it");
            Assert.IsTrue(SpecialBuildingModels.Hidden(c + new Vector2(R + r - 1f, R), r, c, R), "just inside its east side");
            Assert.IsFalse(SpecialBuildingModels.Hidden(c + new Vector2(R + r + 1f, R), r, c, R), "just outside its east side");
            Assert.IsFalse(SpecialBuildingModels.Hidden(c + new Vector2(-R - r - 1f, R), r, c, R), "just outside its west side");
            Assert.IsFalse(SpecialBuildingModels.Hidden(c + new Vector2(0f, -R - r - 1f), r, c, R), "south of the spot: in front of the figures");
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

        /// <summary>Review of #2281 (blocking): a workshop's spot does not depend on what streamed first. Workshop models are
        /// wanted right at the farm scenes' planned spots (RuralLife.Plan), the most contested ground. With the farm scenes
        /// streamed first, or with b21 streamed alone first and the other buildings and the farm scenes after it, every b21
        /// stands on the same spot, clear of every farm scene drawn.</summary>
        [Test]
        public void AWorkshopsSpotIsTheSameWhateverStreamsFirst()
        {
            var map = OpenMap();
            var c = Rules(Statecraft.Sim.SaveGame.Gdd03Ruleset);
            var savedPeople = CivilianPeople.Request; var savedRural = RuralLife.Request;
            var rural = new List<(string id, CollectionDetail detail, Action<GameObject?> done)>();
            CivilianPeople.Request = (id, detail, done) => { };
            RuralLife.Request = (id, detail, done) => rural.Add((id, detail, done));
            void StreamRural()
            {
                for (int round = 0; round < 10 && rural.Count > 0; round++)
                {
                    var now = rural.ToList(); rural.Clear();
                    foreach (var (id, detail, done) in now) done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail)));
                }
            }
            try
            {
                CollectionGeometryFixture.People(map, After, Array.Empty<Statecraft.Sim.Placement>(), c);
                var farms = RuralLife.Plan(map, RuralLife.Wants(map), RuralLife.OtherProps(map), _ => true);
                Assert.Greater(farms.Count, 0);
                var wants = SpecialBuildingModels.Placements(After).ToList();
                for (int i = 0; i < farms.Count; i++)
                {
                    var (lon, lat) = map.Frame.ToLonLat(new Vector3(farms[i].spot.x, 0f, farms[i].spot.y));
                    wants.Add(new SpecialBuildingModels.Want(SpecialBuildingModels.WorkshopKey(500 + i), SpecialBuildingModels.Workshop, Built, lon, lat, "test: at a farm scene"));
                }
                SpecialBuildingModels Layer()
                {
                    var l = CollectionGeometryFixture.Special(map, After)!;
                    l.ShowForTest(wants);
                    return l;
                }

                // The farm scenes first, then the buildings.
                var ruralLayer = CollectionGeometryFixture.Rural(map)!;
                StreamRural();
                Assert.Greater(ruralLayer.Placed.Count, 0);
                var first = Layer();
                AnswerAll();
                var spots = first.Placed.Where(p => p.Model == SpecialBuildingModels.Workshop).ToDictionary(p => p.Key, p => p.At);
                Assert.Greater(spots.Count, 0, "workshops stand by the farm scenes");
                UnityEngine.Object.DestroyImmediate(first.gameObject); UnityEngine.Object.DestroyImmediate(ruralLayer.gameObject);
                asked.Clear();

                // b21 alone first, then the other buildings, then the farm scenes.
                ruralLayer = CollectionGeometryFixture.Rural(map)!;
                var second = Layer();
                var b21 = asked.Where(a => a.id == SpecialBuildingModels.Workshop).ToList();
                Assert.AreEqual(1, b21.Count);
                asked.RemoveAll(a => a.id == SpecialBuildingModels.Workshop);
                b21[0].done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(SpecialBuildingModels.Workshop, CollectionDetail.Near)));
                AnswerAll();
                StreamRural();
                Assert.Greater(ruralLayer.Placed.Count, 0);
                var scenes = ruralLayer.Placed.Select(p => (new Vector2(p.At.x, p.At.z), RuralLife.SceneRadiusM(p.Model))).ToList();
                var again = second.Placed.Where(p => p.Model == SpecialBuildingModels.Workshop).ToList();
                CollectionAssert.AreEquivalent(spots.Keys, again.Select(p => p.Key), "the same workshops stand");
                foreach (var p in again)
                {
                    Assert.AreEqual(spots[p.Key], p.At, p.Key + " on the same spot whatever streamed first");
                    Assert.IsTrue(TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), SpecialBuildingModels.ClearanceOf(p.Model), scenes),
                        p.Key + " clear of the farm scenes that streamed in after it");
                }
            }
            finally { CivilianPeople.Request = savedPeople; RuralLife.Request = savedRural; }
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
