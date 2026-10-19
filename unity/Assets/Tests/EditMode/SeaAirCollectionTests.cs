#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #669: Tal Heuberger's immigrant steamer off Haifa, fishing boats off Jaffa and Acre, the Altalena off Tel Aviv on
/// its card's turn and the C-54 at Lod during Operation Magic Carpet, streamed after the first frame (SeaAirCollection).</summary>
public class SeaAirCollectionTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();
    static readonly Content C = ContentLoader.Load(Statecraft.Game.Paths.Content);

    [SetUp]
    public void Seam()
    {
        saved = SeaAirCollection.Request; asked.Clear();
        SeaAirCollection.Request = (id, d, done) => asked.Add((id, d, done));
    }

    [TearDown]
    public void Reset()
    {
        SeaAirCollection.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build(); return map;
    }

    static GameObject Far(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Far));

    void AnswerAll()
    {
        var pending = asked.ToList(); asked.Clear();
        foreach (var (id, _, done) in pending) done(Far(id));
    }

    static Bounds WorldBounds(GameObject go) =>
        go.GetComponentsInChildren<Renderer>(true).Select(x => x.bounds).Aggregate((a, x) => { a.Encapsulate(x); return a; });

    static readonly string[] Models = { SeaAirCollection.Altalena, SeaAirCollection.FishingBoat, SeaAirCollection.Steamer, SeaAirCollection.C54 };

    /// <summary>The owner (#663): every model is used. All four of this lane's are in the streamed vehicle bundle and each is
    /// wanted on some turn of the game, on the game's own monthly turn dates.</summary>
    [Test]
    public void EveryModelOfTheLaneIsWantedOnSomeTurnFromTheStreamedVehicleBundle()
    {
        foreach (var id in Models) Assert.AreEqual(CollectionCatalog.Vehicles, CollectionCatalog.BundleOf(id), id + " is in the streamed vehicle bundle");
        var turns = new List<DateTime>();
        for (var d = C.Rules.Start; d < C.End; d = d.AddMonths(1)) turns.Add(d);
        Assert.AreEqual(new DateTime(1948, 5, 15), turns[0]);
        foreach (var id in Models)
            Assert.IsTrue(turns.Any(d => SeaAirCollection.Wants(d, C).Any(w => w.Model == id)), id + " is wanted on some monthly turn");
        Assert.AreEqual(1, turns.Count(d => SeaAirCollection.Wants(d, C).Any(w => w.Model == SeaAirCollection.Altalena)), "the Altalena on one turn");
    }

    /// <summary>Dated items only on their sourced dates. The Altalena on its card's window, 15-23 June 1948 (the turn that holds
    /// 20-22 June, the days the card's sources put the ship off Kfar Vitkin and Tel Aviv), off Tel Aviv. The C-54 at Lod from
    /// 1 July 1949 (Magic Carpet's main wave) to 24 September 1950 (its last flights into Lydda). The harbour craft always.</summary>
    [Test]
    public void TheAltalenaAndTheC54AreWantedOnlyOnTheirSourcedDates()
    {
        var card = C.Events.Single(e => e.Id == SeaAirCollection.AltalenaEvent);
        Assert.AreEqual(new DateTime(1948, 6, 15), card.From); Assert.AreEqual(new DateTime(1948, 6, 23), card.To);
        string[] Keys(DateTime d) => SeaAirCollection.Wants(d, C).Select(w => w.Key).ToArray();
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing" }, Keys(new DateTime(1948, 5, 15)));
        CollectionAssert.AreEqual(new[] { "haifa-steamer", "jaffa-fishing", "acre-fishing", "tel-aviv-altalena" }, Keys(new DateTime(1948, 6, 15)));
        Assert.AreEqual("tel_aviv", SeaAirCollection.Wants(new DateTime(1948, 6, 15), C).Single(w => w.Model == SeaAirCollection.Altalena).Place);
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 6, 14)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 6, 23)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1948, 7, 15)), "tel-aviv-altalena");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1949, 6, 30)), "lod-c54");
        CollectionAssert.Contains(Keys(new DateTime(1949, 7, 1)), "lod-c54");
        CollectionAssert.Contains(Keys(new DateTime(1950, 9, 24)), "lod-c54");
        CollectionAssert.DoesNotContain(Keys(new DateTime(1950, 9, 25)), "lod-c54");
    }

    /// <summary>Nothing is drawn until the stream answers, and only the far mesh, once per model. Then every craft is drawn,
    /// each boat on the open sea (the heightmap's 0 m), its hull passing SeaCraft's own clearance (open sea round it, clear of
    /// the harbour works and of SeaCraft's boats and each other) and at least SeaCraft.OffshoreNearM out from its shore point
    /// (past the surf), its waterline on the surface; the C-54 on land by the airport, its wheels on the ground, off every kit
    /// and with no kit tree drawn under it. Every
    /// craft keeps its prefab's rotation under a quarter turn (upright, as #979), lies with its longer side east-west, and is
    /// drawn at its size. Rebuilding gives the same spots.</summary>
    [Test]
    public void TheCraftStreamInAndFloatUprightClearOfTheSurfTheHarbourWorksAndTheOtherBoats()
    {
        var map = OpenMap();
        var ground = new KitGround(map.Terrain, map.Frame, map.Hexes, map.ElevMinM);
        var kitBoats = map.KitStats!.Crafts; var works = map.KitStats.HarbourWorks;
        Assert.Greater(kitBoats.Count, 0, "SeaCraft's boats are there to keep clear of");
        Assert.Greater(works.Count, 0, "the harbour works are there to keep clear of");
        bool first = true;
        foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
        {
            var layer = SeaAirCollection.ShowAt(map, date, Array.Empty<Placement>(), C)!;
            if (first) CollectionAssert.IsEmpty(layer.Placed, "nothing before the stream answers");
            first = false;
            Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far), "the far mesh only");
            Assert.AreEqual(asked.Count, asked.Select(a => a.id).Distinct().Count(), "each model once");
            AnswerAll();
            CollectionAssert.IsEmpty(layer.Missing, "every craft found a spot on " + date.ToString("d MMM yyyy"));
            Assert.AreEqual(4, layer.Placed.Count);
            var spots = layer.Placed.Select(p => p.At).ToArray();
            SeaAirCollection.ShowAt(map, date.AddMonths(1), Array.Empty<Placement>(), C); AnswerAll();
            SeaAirCollection.ShowAt(map, date, Array.Empty<Placement>(), C); AnswerAll();
            CollectionAssert.AreEqual(spots, layer.Placed.Select(p => p.At).ToArray(), "the same spots again");
            var boats = layer.Placed.Where(p => p.Model != SeaAirCollection.C54).ToList();
            foreach (var (key, model, at) in layer.Placed)
            {
                var child = layer.transform.Find(key)!; var prefab = Far(model);
                var turned = new[] { 0f, 90f }.Min(y => Quaternion.Angle(layer.transform.rotation * Quaternion.Euler(0f, y, 0f) * prefab.transform.localRotation, child.rotation));
                Assert.Less(turned, 0.01f, key + " keeps its prefab's rotation (upright) under a yaw of 0 or 90");
                var b = WorldBounds(child.gameObject);
                Assert.GreaterOrEqual(b.size.x, b.size.z, key + " lies east-west, side-on to the camera");
                Assert.AreEqual(SeaAirCollection.SizeOf(model), b.size.x, 1f, key + " is drawn at its size");
                Assert.AreEqual(at.y, b.min.y, 1f, key + " stands on the surface under it");
                Assert.AreEqual(ground.Y(at.x, at.z), at.y, 0.5f);
                var p = new Vector2(at.x, at.z);
                if (model == SeaAirCollection.C54)
                {
                    Assert.IsFalse(ground.Water(p.x, p.y), "the C-54 stands on land");
                    var lod = map.Frame.ToWorld(SeaAirCollection.LodLon, SeaAirCollection.LodLat, 0);
                    Assert.Less((p - new Vector2(lod.x, lod.z)).magnitude, 3f * SeaAirCollection.SizeOf(model), "by Lod Airport");
                    foreach (var f in map.KitFootprints)
                        Assert.GreaterOrEqual((p - f.centre).magnitude, f.radius + 0.5f * SeaAirCollection.SizeOf(model) - 1f, $"the C-54 on {f.id}'s kit");
                    var veg = map.GetComponentInChildren<VegetationLayer>(true);
                    Assert.IsNotNull(veg);
                    foreach (var (kind, m) in veg.Drawable())
                    {
                        var t = (Vector3)m.GetColumn(3);
                        Assert.GreaterOrEqual((new Vector2(t.x, t.z) - p).magnitude, 0.5f * SeaAirCollection.SizeOf(model) + VegetationLayer.CrownRadius(kind, m) - 1f,
                            "no kit tree drawn under the C-54");
                    }
                    continue;
                }
                Assert.LessOrEqual(Mathf.Abs(ground.ElevM(p.x, p.y)), MapBuilder.SeaToleranceM + 0.5f, key + " lies on the open sea");
                float len = SeaAirCollection.SizeOf(model);
                var others = kitBoats.Concat(boats.Where(o => o.Key != key)
                    .Select(o => new SeaCraft.Craft(SeaCraft.Kind.Steamer, new Vector2(o.At.x, o.At.z), 0f, SeaAirCollection.SizeOf(o.Model), ""))).ToList();
                Assert.IsTrue(SeaCraft.Clear(new SeaCraft.Craft(SeaCraft.Kind.Steamer, p, 0f, len, ""), ground, works.ToList(), others, 0.5f * len),
                    key + " passes SeaCraft's clearance: open sea round it, off the harbour works and every other boat");
                var w = SeaAirCollection.Wants(date, C).Single(x => x.Key == key);
                var coast = SeaCraft.Coasts.Single(c => c.place == w.Place);
                var shoreAt = map.Frame.ToWorld(coast.lon, coast.lat, 0);
                var shore = SeaCraft.FirstWater(new Vector2(shoreAt.x, shoreAt.z), coast.seaward, ground, 20000f)!.Value;
                Assert.GreaterOrEqual(Vector2.Dot(p - shore, coast.seaward), SeaCraft.OffshoreNearM * KitScale.Landmarks / 60f - 1f, key + " lies past the surf");
            }
        }
    }

    /// <summary>The C-54 keeps off the player's placements: a hex placed at the airport moves it to the next free ground.</summary>
    [Test]
    public void TheC54KeepsOffAPlacedHex()
    {
        var map = OpenMap();
        var date = new DateTime(1949, 8, 15);
        var layer = SeaAirCollection.ShowAt(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
        var first = layer.Placed.Single(p => p.Model == SeaAirCollection.C54).At;
        var hex = C.Hexes.OrderBy(h => { var w = map.Frame.ToWorld(h.Lon, h.Lat, 0); return (new Vector2(w.x, w.z) - new Vector2(first.x, first.z)).sqrMagnitude; }).First();
        var placed = new[] { new Placement(MapActions.TransitCamp, hex.Q, hex.R, date) };
        SeaAirCollection.ShowAt(map, date, placed, C); AnswerAll();
        var moved = layer.Placed.Single(p => p.Model == SeaAirCollection.C54).At;
        var c = map.Frame.ToWorld(hex.Lon, hex.Lat, 0);
        Assert.GreaterOrEqual((new Vector2(moved.x, moved.z) - new Vector2(c.x, c.z)).magnitude,
            500f * map.Hexes.hex_size_km + 0.5f * SeaAirCollection.SizeOf(SeaAirCollection.C54) - 1f, "off the placed hex");
    }

    /// <summary>The owner (18 October 2026, #1033): "Train scale for all ambient life". Each craft's long side as drawn
    /// (its renderers' world box, seen from above) is inside its band of the steam train's locomotive car (about 204 m:
    /// Tal Heuberger's locomotive and tender at RouteTrain.Scale x ServiceTrain.ModelSize / ServiceTrain.Cars), the band about its
    /// real size at the one world scale (SettlementKits.ModelScale, 24x: an 8.5 m house is one car): the Altalena, a 100 m
    /// LST, about 11.8 cars; the 80 m steamer about 9.4; the 10 m fishing boat about 1.2; the C-54's 35.8 m wingspan about
    /// 4.2. No ship dwarfs its town: each is shorter than its port's kit footprint is across and than a whole-town model
    /// (TownLandmarks.SizeM).</summary>
    [Test]
    public void EveryCraftIsAtTheTrainScaleAndNoShipDwarfsItsTown()
    {
        var map = OpenMap();
        float loco = Statecraft.Game.Map.Routes.RouteTrain.ModelRealLengthM * Statecraft.Game.Map.Routes.RouteTrain.Scale
            * Statecraft.Game.Map.Routes.ServiceTrain.ModelSize / Statecraft.Game.Map.Routes.ServiceTrain.Cars;
        Assert.AreEqual(204f, loco, 1f, "the locomotive's car");
        Assert.AreEqual(24f, SettlementKits.ModelScale, 0.01f, "the one world scale (#1030)");
        var bands = new Dictionary<string, (float lo, float hi)>
        {
            [SeaAirCollection.Altalena] = (11f, 12.5f), [SeaAirCollection.Steamer] = (8f, 11f),
            [SeaAirCollection.FishingBoat] = (0.9f, 1.5f), [SeaAirCollection.C54] = (3.9f, 4.5f),
        };
        var seen = new HashSet<string>();
        var bad = new List<string>();
        foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
        {
            var layer = SeaAirCollection.ShowAt(map, date, Array.Empty<Placement>(), C)!; AnswerAll();
            Assert.AreEqual(layer.Placed.Count, layer.KeepClear.Count, "the labels keep clear of every drawn craft");
            foreach (var (key, model, _) in layer.Placed)
            {
                var b = WorldBounds(layer.transform.Find(key)!.gameObject);
                float side = Mathf.Max(b.size.x, b.size.z), cars = side / loco;
                seen.Add(model);
                TestContext.WriteLine($"{key}: {side:0} m long, {cars:0.00} locomotive cars, {b.size.y:0} m tall");
                var (lo, hi) = bands[model];
                if (cars < lo || cars > hi) bad.Add($"{key}: {side:0} m, {cars:0.00} locomotive cars (band {lo}-{hi})");
                if (model == SeaAirCollection.C54) continue;
                var place = SeaAirCollection.Wants(date, C).Single(w => w.Key == key).Place;
                var town = map.KitFootprints.FirstOrDefault(f => f.id == place);
                Assert.IsNotNull(town.id, place + " has a kit footprint");
                TestContext.WriteLine($"  {place}'s kit footprint {2f * town.radius:0} m across");
                if (side >= 2f * town.radius) bad.Add($"{key}: {side:0} m against {place}'s {2f * town.radius:0} m");
                if (side >= TownLandmarks.SizeM) bad.Add($"{key}: {side:0} m against a {TownLandmarks.SizeM:0} m whole-town model");
            }
        }
        CollectionAssert.AreEquivalent(bands.Keys, seen, "every model measured");
        CollectionAssert.IsEmpty(bad, string.Join("\n", bad));
    }

    /// <summary>The craft keep clear of the other 1948 Collection layers already on main: the whole-town models (#994), the
    /// civilians (#1023), the farm life (#1029), the wildlife (#1022) and the Tegart fort, barracks and factory (#1031). On
    /// both dated turns, every craft's box, seen from above, meets none of their renderers' boxes.</summary>
    [Test]
    public void TheCraftKeepClearOfTheOtherCollectionLayers()
    {
        var map = OpenMap();
        Action<string, CollectionDetail, Action<GameObject?>> now = (id, d, done) => done(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, d)));
        var savedSeams = (TownLandmarks.Request, CivilianPeople.Request, RuralLife.Request, WildlifeLayer.Request, SpecialBuildingModels.Request);
        TownLandmarks.Request = CivilianPeople.Request = RuralLife.Request = WildlifeLayer.Request = SpecialBuildingModels.Request = now;
        try
        {
            int others = 0, checkedCraft = 0;
            foreach (var date in new[] { new DateTime(1948, 6, 15), new DateTime(1949, 8, 15) })
            {
                var none = Array.Empty<Placement>();
                var layers = new MonoBehaviour?[]
                {
                    TownLandmarks.ShowAt(map, date, none, C), CivilianPeople.ShowAt(map, date, none, C), RuralLife.Show(map),
                    WildlifeLayer.Show(map), SpecialBuildingModels.ShowAt(map, date),
                };
                var boxes = layers.Where(l => l != null).SelectMany(l => l!.GetComponentsInChildren<Renderer>(true).Select(r => (l!.GetType().Name, r.bounds))).ToList();
                others = Math.Max(others, boxes.Count);
                var craft = SeaAirCollection.ShowAt(map, date, none, C)!; AnswerAll();
                foreach (Transform t in craft.transform)
                {
                    var b = WorldBounds(t.gameObject); checkedCraft++;
                    foreach (var (owner, o) in boxes)
                        Assert.IsFalse(b.min.x < o.max.x && o.min.x < b.max.x && b.min.z < o.max.z && o.min.z < b.max.z,
                            $"{t.name} meets {owner}'s model at {o.center} on {date:d MMM yyyy}");
                }
            }
            Assert.Greater(others, 0, "the other layers drew");
            Assert.AreEqual(8, checkedCraft, "every craft checked, four on each turn");
        }
        finally
        {
            (TownLandmarks.Request, CivilianPeople.Request, RuralLife.Request, WildlifeLayer.Request, SpecialBuildingModels.Request) = savedSeams;
        }
    }

    /// <summary>Fails open: a model that does not stream warns once and is not drawn; the other craft draw and the map runs on.</summary>
    [Test]
    public void AModelThatDoesNotStreamWarnsAndTheOthersDraw()
    {
        var map = OpenMap(); var layer = SeaAirCollection.ShowAt(map, new DateTime(1949, 1, 15), Array.Empty<Placement>(), C)!;
        var failed = asked.Single(x => x.id == SeaAirCollection.Steamer);
        LogAssert.Expect(LogType.Warning, new Regex("^SEA/AIR: the model vehicle-immigrant-steamer-1948-v2wl did not stream"));
        failed.done(null);
        foreach (var a in asked.Where(x => x.id != SeaAirCollection.Steamer).ToList()) a.done(Far(a.id));
        CollectionAssert.AreEquivalent(new[] { "jaffa-fishing", "acre-fishing" }, layer.Placed.Select(p => p.Key));
        CollectionAssert.AreEqual(new[] { "haifa-steamer" }, layer.Missing);
    }
}
