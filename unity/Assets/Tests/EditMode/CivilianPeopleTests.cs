#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #666: Tal Heuberger's civilian figures stand in small deterministic groups at the Israeli towns,
/// kibbutzim, moshavim and placed transit camps. They are ambience, never combat units, and stream after the first frame.</summary>
public sealed class CivilianPeopleTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();

    [SetUp]
    public void Seam()
    {
        saved = CivilianPeople.Request;
        asked.Clear();
        CivilianPeople.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        CivilianPeople.Request = saved;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        return map;
    }

    static GameObject Prefab(string id, CollectionDetail detail) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, detail));

    void Answer(CollectionDetail detail)
    {
        foreach (var request in asked.Where(a => a.detail == detail).ToList())
        {
            asked.Remove(request);
            request.done(Prefab(request.id, detail));
        }
    }

    [Test]
    public void EveryCivilianModelIsUsedAtTheRequestedPlacesAndStreamsFromThePeopleBundle()
    {
        Assert.AreEqual(typeof(CollectionStream), saved.Method.DeclaringType, "the game's seam is CollectionStream.Request");
        Assert.AreEqual(nameof(CollectionStream.Request), saved.Method.Name);
        CollectionAssert.AreEqual(new[]
        {
            "figure-c02-townsman-v2", "figure-c03-smith-carpenter-v2", "figure-c13-nurse-v2",
        }, CivilianPeople.SettledModels);
        CollectionAssert.AreEqual(new[]
        {
            "figure-c01-arriving-man-v2", "figure-c10-woman-blue-dress-v2",
        }, CivilianPeople.CampModels);
        foreach (var id in CivilianPeople.SettledModels.Concat(CivilianPeople.CampModels))
        {
            Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.BundleOf(id), id);
            foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, detail);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                StringAssert.DoesNotContain("/Resources/", path);
            }
            Assert.IsNull(Resources.Load<GameObject>(id), id + " is never in the first-frame Resources load");
        }
        CollectionAssert.AreEquivalent(new[] { "city", "town", "kibbutz", "moshav" }, CivilianPeople.SettledTypes);
        CollectionAssert.DoesNotContain(CivilianPeople.SettledTypes, "arab_town", "the Arab towns get the Arab villager, not the C-series set (#708)");
    }

    [Test]
    public void EveryEligibleSettlementGetsOneSmallGroupAndEveryPlacedCampGetsItsTwoPeopleFromItsDate()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = content.SettlementHexes.Single(s => s.Id == "revivim");
        var camp = new[] { new Placement(MapActions.TransitCamp, hex.Q, hex.R, new DateTime(1950, 8, 1)) };

        bool CSeries(CivilianPeople.Want w) => CivilianPeople.SettledModels.Contains(w.Model) || CivilianPeople.CampModels.Contains(w.Model);
        var before = CivilianPeople.Wants(map, new DateTime(1950, 7, 31), camp, content).Where(CSeries).ToList();
        int settlements = map.Settlements.Count(s => CivilianPeople.SettledTypes.Contains(s.Type));
        Assert.AreEqual(settlements * CivilianPeople.SettledGroupSize, before.Count);
        CollectionAssert.DoesNotContain(before.Select(w => w.Model), CivilianPeople.ArrivingMan);

        var from = CivilianPeople.Wants(map, new DateTime(1950, 8, 1), camp, content).Where(CSeries).ToList();
        Assert.AreEqual(settlements * CivilianPeople.SettledGroupSize + CivilianPeople.CampGroupSize, from.Count);
        CollectionAssert.AreEqual(CivilianPeople.CampModels,
            from.Where(w => w.Key.StartsWith("transit_camp_", StringComparison.Ordinal)).Select(w => w.Model));
        Assert.AreEqual(3, CivilianPeople.SettledGroupSize, "design value: a small settled group");
        Assert.AreEqual(2, CivilianPeople.CampGroupSize, "design value: a small camp group");
    }

    [Test]
    public void SeededSpotsAreStableAndFiguresStandClearOfTownsTokensRoutesAndEachOther()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var first = CivilianPeople.Wants(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content);
        var second = CivilianPeople.Wants(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content);
        CollectionAssert.AreEqual(first, second, "same ids and seed give bit-for-bit equal plans");

        var layer = CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        Assert.AreEqual(first.Select(w => w.Model).Distinct().Count(), asked.Count, "each wanted far model, once");
        Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Far));
        CollectionAssert.IsEmpty(layer.Placed, "the stream never answers in the requesting frame");
        Answer(CollectionDetail.Far);

        Assert.AreEqual(first.Count, layer.Placed.Count, "placement count");
        CollectionAssert.IsEmpty(layer.Missing);
        var ground = PlacementLayer.CampGround(map);
        foreach (var p in layer.Placed)
        {
            var at = new Vector2(p.At.x, p.At.z);
            foreach (var f in map.KitFootprints)
                Assert.GreaterOrEqual((at - f.centre).magnitude, f.radius + CivilianPeople.FigureClearanceM - 1f, $"{p.Key} clear of {f.id}");
            Assert.IsFalse(ground.OnRoute(at, CivilianPeople.FigureClearanceM), p.Key + " clear of routes");
            Assert.IsFalse(ground.OnToken(at, CivilianPeople.FigureClearanceM), p.Key + " clear of unit tokens");
        }
        for (int i = 0; i < layer.Placed.Count; i++)
            for (int j = i + 1; j < layer.Placed.Count; j++)
                Assert.GreaterOrEqual(Vector3.Distance(layer.Placed[i].At, layer.Placed[j].At), CivilianPeople.FigureSpacingM - 1f,
                    $"{layer.Placed[i].Key} clear of {layer.Placed[j].Key}");
    }

    [Test]
    public void EveryFigureKeepsThePrefabAxisConversionUnderItsSeededYaw()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var layer = CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        Answer(CollectionDetail.Far);
        Assert.Greater(layer.Placed.Count, 0);
        foreach (var p in layer.Placed)
        {
            var child = layer.transform.Find(p.Key + "/Far");
            Assert.IsNotNull(child, p.Key);
            var prefab = Prefab(p.Model, CollectionDetail.Far);
            var expected = layer.transform.rotation * Quaternion.Euler(0f, p.Yaw, 0f) * prefab.transform.localRotation;
            Assert.Less(Quaternion.Angle(expected, child!.rotation), 0.01f, p.Key + " upright");
            float tall = CivilianPeople.HeightOf(p.Model);
            Assert.AreEqual(tall, WorldBounds(child.gameObject).size.y, tall * 0.02f, p.Key + " height at the world scale");
            Assert.IsTrue(child.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterial.enableInstancing), p.Key + " is GPU-instanced");
        }
    }

    /// <summary>Issue #1033 (the owner, 18 October 2026: "Train scale for all ambient life"): a townsperson is drawn at
    /// the building models' world scale (SettlementKits.ModelScale), a 1.78 m man about 43 m tall, so he is smaller than
    /// the reference house and a fifth of the steam train's locomotive car (RoadVehicles.LocomotiveCarM, about 204 m),
    /// where before he was about 2.5 km tall, twelve locomotive cars.</summary>
    [Test]
    public void EveryFigureIsSmallerThanAHouseAndMuchSmallerThanTheLocomotiveCar()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var layer = CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        Answer(CollectionDetail.Far);
        Assert.Greater(layer.Placed.Count, 0);
        float house = Statecraft.Game.Map.Kits.SettlementKits.ReferenceHouseM * Statecraft.Game.Map.Kits.SettlementKits.ModelScale;
        float car = Statecraft.Game.Map.Routes.RoadVehicles.LocomotiveCarM;
        foreach (var p in layer.Placed)
        {
            var size = WorldBounds(layer.transform.Find(p.Key + "/Far").gameObject).size;
            float tallest = Mathf.Max(size.x, size.y, size.z);
            Assert.Less(tallest, house, $"{p.Key}: {tallest:0} m against the {house:0} m reference house");
            Assert.That(tallest / car, Is.InRange(0.1f, 0.25f), $"{p.Key}: {tallest:0} m against the {car:0} m locomotive car");
        }
    }

    [Test]
    public void CloseZoomStreamsTheNearIdleFiguresAndKeepsFarFiguresUntilTheyArrive()
    {
        Assert.IsTrue(CivilianPeople.CloseAt(40000f, false), "near enters at the 40 km design value");
        Assert.IsTrue(CivilianPeople.CloseAt(45000f, true), "near stays through the 45 km hysteresis band");
        Assert.IsFalse(CivilianPeople.CloseAt(45001f, true));
        Assert.IsFalse(CivilianPeople.CloseAt(40001f, false));
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var layer = CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        Answer(CollectionDetail.Far);
        var camera = CameraAt(30000f);
        try
        {
            layer.Choose(default, camera);
            Assert.AreEqual(layer.Wanted.Select(w => w.Model).Distinct().Count(), asked.Count, "near models asked once on entering close zoom");
            Assert.IsTrue(asked.All(a => a.detail == CollectionDetail.Near));
            Assert.IsTrue(layer.GetComponentsInChildren<Renderer>(true).Any(r => !r.forceRenderingOff), "far figures remain while near streams");
            Answer(CollectionDetail.Near);
            Assert.AreEqual(layer.Placed.Count, layer.GetComponentsInChildren<Animator>(true).Length, "the near idle prefab for every figure");
            Assert.IsTrue(layer.Placed.All(p => layer.transform.Find(p.Key + "/Far").GetComponentInChildren<Renderer>().forceRenderingOff));
            Assert.IsTrue(layer.Placed.All(p => !layer.transform.Find(p.Key + "/Near").GetComponentInChildren<Renderer>().forceRenderingOff));
        }
        finally { Object.DestroyImmediate(camera.gameObject); }
    }

    [Test]
    public void AModelThatDoesNotStreamFailsOpenWithoutDroppingTheOtherPeople()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var layer = CivilianPeople.ShowAt(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content)!;
        var failed = asked.Single(a => a.id == CivilianPeople.Townsman);
        asked.Remove(failed);
        LogAssert.Expect(LogType.Warning, new Regex("^PEOPLE: figure-c02-townsman-v2 far did not stream"));
        failed.done(null);
        Answer(CollectionDetail.Far);
        Assert.Greater(layer.Placed.Count, 0, "the other models are drawn");
        CollectionAssert.DoesNotContain(layer.Placed.Select(p => p.Model), CivilianPeople.Townsman);
        Assert.AreEqual(layer.Wanted.Count(w => w.Model == CivilianPeople.Townsman), layer.Missing.Count(k => k.EndsWith("/0", StringComparison.Ordinal)));
    }

    // ---- #708 and #886: Tal Heuberger's figures offered in #336 ----

    static string Group(CivilianPeople.Want w) => w.Key.Substring(0, w.Key.LastIndexOf('/'));
    static string Place(CivilianPeople.Want w) => Group(w).Split(':')[0];
    static List<string> ModelsAt(IEnumerable<CivilianPeople.Want> wants, string group) =>
        wants.Where(w => Group(w) == group).Select(w => w.Model).ToList();

    /// <summary>Every place type the map knows by id: the game settlements and the 1948 localities.</summary>
    static Dictionary<string, string> Types(MapScene map)
    {
        var types = map.Settlements.ToDictionary(s => s.Id, s => s.Type, StringComparer.Ordinal);
        foreach (var p in map.Localities) if (!types.ContainsKey(p.Settlement.Id)) types[p.Settlement.Id] = p.Settlement.Type;
        return types;
    }

    /// <summary>Each figure of #336 has a placement table here and its places resolve on the map: the Arab villager at the seven
    /// Arab towns and the villages whose flocks RuralLife draws, the kibbutz woman at every kibbutz, the pioneer farmer at the
    /// moshav and the kibbutzim whose tractors RuralLife draws. Nobody else stands at an Arab town, and no figure stands at
    /// a place of another kind.</summary>
    [Test]
    public void TheVillagerKibbutzWomanAndFarmerStandAtTheirPlacesInNormalPlay()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var types = Types(map);
        var plan = CivilianPeople.Wants(map, new DateTime(1949, 1, 1), Array.Empty<Placement>(), content);
        var rural = RuralLife.Wants(map);

        var arabTowns = map.Settlements.Where(s => s.Type == "arab_town").Select(s => s.Id).ToList();
        CollectionAssert.AreEquivalent(new[] { "jaffa", "acre", "nazareth", "lod", "ramla", "gaza", "beersheba" }, arabTowns,
            "the seven Arab towns, each with the stone village (TownLandmarks.ByType)");
        var villages = CivilianPeople.Served(rural, RuralLife.Sheep, RuralLife.Donkey).Select(w => w.Key).ToList();
        Assert.That(villages.Count, Is.InRange(RuralLife.MaxAnchorsPerModel, 2 * RuralLife.MaxAnchorsPerModel));
        Assert.IsTrue(villages.All(v => types[v] == "village"), "the flocks' villages");
        // On 1 January 1949, after the four sourced dates (TheVillagerLeavesFourTownsAfterTheirSourcedDates), three towns keep him.
        var shown = arabTowns.Where(id => !CivilianPeople.VillagerUntil.ContainsKey(id)).ToList();
        CollectionAssert.AreEquivalent(new[] { "acre", "nazareth", "gaza" }, shown);
        foreach (var id in shown.Concat(villages))
            CollectionAssert.AreEqual(new[] { CivilianPeople.ArabVillager }, ModelsAt(plan, id + ":villager"), id + ": one villager beside it");
        var villagers = plan.Where(w => w.Model == CivilianPeople.ArabVillager).ToList();
        Assert.AreEqual(shown.Count + villages.Count, villagers.Count);
        Assert.IsTrue(villagers.All(w => types[Place(w)] == "arab_town" || types[Place(w)] == "village"), "only at the Arab towns and villages");
        Assert.IsTrue(plan.Where(w => arabTowns.Contains(Place(w))).All(w => w.Model == CivilianPeople.ArabVillager),
            "nobody else at the Arab towns (#666's ruling: they wait for the Arab villager)");

        var kibbutzim = map.Settlements.Where(s => s.Type == "kibbutz").Select(s => s.Id).ToList();
        Assert.Greater(kibbutzim.Count, 0);
        foreach (var id in kibbutzim)
            CollectionAssert.AreEqual(new[] { CivilianPeople.KibbutzWoman }, ModelsAt(plan, id + ":kibbutz-woman"), id);
        Assert.AreEqual(kibbutzim.Count, plan.Count(w => w.Model == CivilianPeople.KibbutzWoman));

        var fields = map.Settlements.Where(s => s.Type == "moshav").Select(s => s.Id)
            .Concat(CivilianPeople.Served(rural, RuralLife.Tractor).Select(w => w.Key)).Distinct().ToList();
        Assert.Greater(fields.Count, RuralLife.MaxAnchorsPerModel - 1, "the moshav and the tractors' kibbutzim");
        foreach (var id in fields)
        {
            Assert.That(types[id], Is.EqualTo("moshav").Or.EqualTo("kibbutz"), id);
            CollectionAssert.AreEqual(new[] { CivilianPeople.PioneerFarmer }, ModelsAt(plan, id + ":farmer"), id);
        }
        Assert.AreEqual(fields.Count, plan.Count(w => w.Model == CivilianPeople.PioneerFarmer));

        foreach (var id in CivilianPeople.ImmigrantModels.Append(CivilianPeople.YemeniteImmigrant).Append(CivilianPeople.Builder))
            CollectionAssert.DoesNotContain(plan.Select(w => w.Model), id, id + ": no camp or project built yet");
    }

    /// <summary>The villager stands only where the sim has residents (docs/gdd/settlement-control.md: residents are a state,
    /// inhabited by default): a captured town still awaiting its outcome keeps him; a place the sim records as depopulated
    /// shows none, a town or a village alike, and the rest stay.</summary>
    [Test]
    public void TheVillagerIsNotShownWhereTheResidentsFled()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var date = new DateTime(1948, 8, 15);
        var village = CivilianPeople.Served(RuralLife.Wants(map), RuralLife.Sheep)[0].Key;
        var control = new Dictionary<string, SettlementControl>(StringComparer.Ordinal)
        {
            ["nazareth"] = new SettlementControl("israel", "", new DateTime(1948, 7, 15), depopulated: true),
            ["acre"] = new SettlementControl("israel", SettlementControl.AwaitingChoice, new DateTime(1948, 7, 15)),
            [village] = new SettlementControl("israel", "", new DateTime(1948, 7, 15), depopulated: true),
        };
        var all = CivilianPeople.Wants(map, date, Array.Empty<Placement>(), content);
        var fled = CivilianPeople.Wants(map, date, Array.Empty<Placement>(), content, control);
        Assert.IsNotEmpty(ModelsAt(all, "nazareth:villager"), "inhabited without a record");
        Assert.IsNotEmpty(ModelsAt(all, village + ":villager"));
        CollectionAssert.IsEmpty(ModelsAt(fled, "nazareth:villager"), "a place the sim records as depopulated");
        CollectionAssert.IsEmpty(ModelsAt(fled, village + ":villager"), "a village's residents fled");
        CollectionAssert.AreEqual(new[] { CivilianPeople.ArabVillager }, ModelsAt(fled, "acre:villager"), "a capture awaiting its outcome changes no residents");
        Assert.AreEqual(all.Count(w => w.Model == CivilianPeople.ArabVillager) - 2, fled.Count(w => w.Model == CivilianPeople.ArabVillager));
        Assert.IsTrue(fled.Where(w => w.Model != CivilianPeople.ArabVillager).All(w => !Place(w).Equals("nazareth", StringComparison.Ordinal)),
            "nobody stands in for them");
    }

    /// <summary>The owner, 21 October 2026, on #708: "hide each town's villager after its sourced date", from the #793
    /// packet's sources only (CivilianPeople.VillagerUntil quotes each). Jaffa's source puts its surrender in May, in a section
    /// that ends on 14 May 1948, so he never stands there in the game; Lod's is the last day of Operation Dani's range in the
    /// Knesset table, 17 July 1948; Ramle's 12 July 1948 (IDF Danny account); Beersheba's 21 October 1948 (IDF Yoav account,
    /// Knesset). He stands up to and on each date, and not after; no other town or village has a date.</summary>
    [Test]
    public void TheVillagerLeavesFourTownsAfterTheirSourcedDates()
    {
        CollectionAssert.AreEquivalent(new Dictionary<string, DateTime>
        {
            ["jaffa"] = new DateTime(1948, 5, 14), ["lod"] = new DateTime(1948, 7, 17),
            ["ramla"] = new DateTime(1948, 7, 12), ["beersheba"] = new DateTime(1948, 10, 21),
        }, CivilianPeople.VillagerUntil);
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        Assert.Greater(content.Rules.Start, CivilianPeople.VillagerUntil["jaffa"], "Jaffa's date is before the game starts");
        CollectionAssert.IsEmpty(ModelsAt(CivilianPeople.Wants(map, content.Rules.Start, Array.Empty<Placement>(), content), "jaffa:villager"),
            "no villager at Jaffa from the start");
        foreach (var id in new[] { "lod", "ramla", "beersheba" })
        {
            var until = CivilianPeople.VillagerUntil[id];
            CollectionAssert.AreEqual(new[] { CivilianPeople.ArabVillager }, ModelsAt(CivilianPeople.Wants(map, until, Array.Empty<Placement>(), content), id + ":villager"),
                $"{id} on {until:d MMM yyyy}");
            CollectionAssert.IsEmpty(ModelsAt(CivilianPeople.Wants(map, until.AddDays(1), Array.Empty<Placement>(), content), id + ":villager"),
                $"{id} after {until:d MMM yyyy}");
        }
        var late = CivilianPeople.Wants(map, new DateTime(1951, 12, 15), Array.Empty<Placement>(), content);
        foreach (var id in new[] { "acre", "nazareth", "gaza" })
            CollectionAssert.AreEqual(new[] { CivilianPeople.ArabVillager }, ModelsAt(late, id + ":villager"), id + " has no date");
        Assert.IsTrue(CivilianPeople.VillagerUntil.Keys.All(id => map.Settlements.Single(s => s.Id == id).Type == "arab_town"));
    }

    /// <summary>The elder immigrant, the child and the immigrant woman stand beside C01 and C10 at every built transit camp, from
    /// the camp's date, whether the legacy loop placed it or the GDD loop built it as a city project (#1123); the Yemenite
    /// immigrant joins them only inside Operation Magic Carpet's window (1 July 1949 to 24 September 1950, as the C-54 at
    /// Lod, SeaAirCollection).</summary>
    [Test]
    public void TheImmigrantsStandWithC01AndC10AtEveryCampAndTheYemeniteOnlyInHisWindow()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        Assert.AreEqual(new DateTime(1949, 7, 1), CivilianPeople.YemeniteFrom, "Magic Carpet's main wave (SeaAirCollection.C54From)");
        Assert.AreEqual(new DateTime(1950, 9, 24), CivilianPeople.YemeniteTo, "the last two aircraft reach Lydda (SeaAirCollection.C54To)");
        var revivim = content.SettlementHexes.Single(s => s.Id == "revivim");
        var tel = content.SettlementHexes.Single(s => s.Id == "tel_aviv");
        string camp = $"{MapActions.TransitCamp}_{revivim.Q}_{revivim.R}", built = $"{MapActions.TransitCamp}_{tel.Q}_{tel.R}";
        var legacy = new[] { new Placement(MapActions.TransitCamp, revivim.Q, revivim.R, new DateTime(1949, 6, 15)) };
        var projects = new[] { new CityProject(4, MapActions.TransitCamp, "tel_aviv", tel.Q, tel.R, 0, new DateTime(1950, 2, 15)) };
        List<CivilianPeople.Want> At(DateTime d) => CivilianPeople.Wants(map, d, legacy, content, null, projects);

        var june = At(new DateTime(1949, 6, 30));
        CollectionAssert.AreEqual(CivilianPeople.CampModels, ModelsAt(june, camp));
        CollectionAssert.AreEqual(CivilianPeople.ImmigrantModels, ModelsAt(june, camp + ":immigrants"), "before Magic Carpet's window");
        CollectionAssert.IsEmpty(ModelsAt(june, built), "the project is not built yet");
        var july = At(CivilianPeople.YemeniteFrom);
        CollectionAssert.AreEqual(CivilianPeople.ImmigrantModels.Append(CivilianPeople.YemeniteImmigrant), ModelsAt(july, camp + ":immigrants"));
        var march = At(new DateTime(1950, 3, 15));
        CollectionAssert.AreEqual(CivilianPeople.CampModels, ModelsAt(march, built), "C01 and C10 at the GDD loop's built camp");
        CollectionAssert.AreEqual(CivilianPeople.ImmigrantModels.Append(CivilianPeople.YemeniteImmigrant), ModelsAt(march, built + ":immigrants"));
        CollectionAssert.AreEqual(CivilianPeople.ImmigrantModels.Append(CivilianPeople.YemeniteImmigrant), ModelsAt(At(CivilianPeople.YemeniteTo), camp + ":immigrants"));
        var october = At(new DateTime(1950, 10, 15));
        CollectionAssert.AreEqual(CivilianPeople.ImmigrantModels, ModelsAt(october, camp + ":immigrants"), "after the last flight");
        CollectionAssert.DoesNotContain(october.Select(w => w.Model), CivilianPeople.YemeniteImmigrant);
        CollectionAssert.DoesNotContain(At(new DateTime(1949, 6, 30)).Select(w => w.Model), CivilianPeople.YemeniteImmigrant);

        // Beside C01 and C10: both groups take the camp's hex as their anchor, within the bounded search of each other.
        float reach = 2f * (map.Hexes!.hex_size_km * 1000f + CivilianPeople.LabelClearanceM + CivilianPeople.SearchRings * CivilianPeople.SearchStepM
            + 2f * CivilianPeople.FigureSpacingM);
        Vector3 Centre(IEnumerable<CivilianPeople.Want> ws, string g) => ws.Where(w => Group(w) == g).Aggregate(Vector3.zero, (a, w) => a + w.At) / ws.Count(w => Group(w) == g);
        Assert.Less(Vector3.Distance(Centre(march, built), Centre(march, built + ":immigrants")), reach);
        // Four in a group stand a full spacing apart.
        var four = march.Where(w => Group(w) == built + ":immigrants").ToList();
        for (int i = 0; i < four.Count; i++)
            for (int j = i + 1; j < four.Count; j++)
                Assert.GreaterOrEqual(Vector3.Distance(four[i].At, four[j].At), CivilianPeople.FigureSpacingM - 1f);
    }

    /// <summary>The builder stands at each of the player's city projects (a housing project, industry, a transit camp) while
    /// it is being built (CityProject.BuiltOn null), and leaves when it is built.</summary>
    [Test]
    public void TheBuilderWorksAtEachCityProjectOnlyWhileItIsBeingBuilt()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var tel = content.SettlementHexes.Single(s => s.Id == "tel_aviv");
        var haifa = content.SettlementHexes.Single(s => s.Id == "haifa");
        var date = new DateTime(1950, 3, 15);
        var building = new[]
        {
            new CityProject(7, MapActions.Housing, "tel_aviv", tel.Q, tel.R, 3),
            new CityProject(8, MapActions.TransitCamp, "haifa", haifa.Q, haifa.R, 1),
        };
        var plan = CivilianPeople.Wants(map, date, Array.Empty<Placement>(), content, null, building);
        CollectionAssert.AreEqual(new[] { CivilianPeople.Builder }, ModelsAt(plan, "project_7:builder"));
        CollectionAssert.AreEqual(new[] { CivilianPeople.Builder }, ModelsAt(plan, "project_8:builder"));
        CollectionAssert.DoesNotContain(plan.Select(w => w.Model), CivilianPeople.ArrivingMan, "a camp being built has no arrivals yet");
        var done = CivilianPeople.Wants(map, date, Array.Empty<Placement>(), content, null, building.Select(p => p.Built(new DateTime(1950, 2, 15))));
        CollectionAssert.DoesNotContain(done.Select(w => w.Model), CivilianPeople.Builder, "the builders leave once it is built");
        CollectionAssert.IsNotEmpty(ModelsAt(done, $"{MapActions.TransitCamp}_{haifa.Q}_{haifa.R}"), "and the built camp takes its arrivals");
    }

    /// <summary>#336's figures stream from the people bundle like the C series, never from Resources.</summary>
    [Test]
    public void TheNewFiguresStreamFromThePeopleBundle()
    {
        foreach (var id in CivilianPeople.Models)
        {
            Assert.AreEqual(CollectionCatalog.People, CollectionCatalog.BundleOf(id), id);
            foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                StringAssert.DoesNotContain("/Resources/", CollectionCatalog.PrefabPath(id, detail));
                Assert.IsNotNull(Prefab(id, detail), id + " " + detail);
            }
            Assert.IsNull(Resources.Load<GameObject>(id), id + " is never in the first-frame Resources load");
            Assert.IsTrue(CollectionCatalog.Animated.Contains(id), id + " has an idle clip up close");
        }
        Assert.Less(CivilianPeople.ChildHeightM, CivilianPeople.FigureHeightM, "the child is shorter than the adults");
    }

    /// <summary>The owner on #336: "we must make sure all of his models are in and fully used". Every model the catalog
    /// streams is in some layer's placement table, but two, each by an owner ruling: b21, the artisan workshop, which waits
    /// for a sourced location (owner: wait), and #507's first Jerusalem, which "stays in the repository, unused on the map"
    /// now that #336's second Jerusalem is the city's only far-zoom model (owner, 21 October 2026, #708).</summary>
    [Test]
    public void EveryCatalogModelIsPlacedButTheTwoTheOwnerHeldBack()
    {
        var placed = new HashSet<string>(StringComparer.Ordinal);
        placed.UnionWith(CivilianPeople.Models);
        placed.UnionWith(RuralLife.Models);
        placed.UnionWith(new[] { WildlifeLayer.Ibex, WildlifeLayer.Hyena });
        placed.UnionWith(new[] { SeaAirCollection.Altalena, SeaAirCollection.FishingBoat, SeaAirCollection.Steamer, SeaAirCollection.C54 });
        placed.UnionWith(new[] { Statecraft.Game.Map.Routes.ServiceTrain.ModelId, Statecraft.Game.Map.Routes.RoadVehicles.CitrusTruck,
            Statecraft.Game.Map.Routes.RoadVehicles.CanvasLorry, Statecraft.Game.Map.Routes.RoadVehicles.SandwichConvoy,
            Statecraft.Game.Map.Routes.RoadVehicles.IntercityBus });
        placed.UnionWith(new[] { SpecialBuildingModels.Shikun, SpecialBuildingModels.Tegart, SpecialBuildingModels.Barracks, SpecialBuildingModels.Factory });
        placed.UnionWith(TownLandmarks.ById.Values.Concat(TownLandmarks.ByType.Values).Concat(TownLandmarks.ByAction.Values.Select(v => v.Model))
);
        var units = Statecraft.Game.Map.Units.UnitFigures.ByHeadgear.Values.Concat(Statecraft.Game.Map.Units.UnitFigures.ThirdFigure.Values)
            .Concat(Statecraft.Game.Map.Units.UnitFigures.BySide.Values).Concat(Statecraft.Game.Map.Units.UnitFigures.ByVehicle.Values)
            .Append(Statecraft.Game.Map.Units.UnitFigures.PioneerVeteran);
        placed.UnionWith(units);
        CollectionAssert.AreEquivalent(new[] { SpecialBuildingModels.Workshop, TownLandmarks.Jerusalem }, CollectionCatalog.Ids.Where(id => !placed.Contains(id)).ToList(),
            "every catalogued model has a place but b21 and the first Jerusalem, each held back by an owner ruling");
        CollectionAssert.IsSubsetOf(placed.ToList(), CollectionCatalog.Ids.ToList(), "every placed id is a catalogued model");
    }

    static Camera CameraAt(float distanceM)
    {
        var camera = new GameObject("People camera").AddComponent<Camera>();
        camera.transform.rotation = Quaternion.Euler(CameraRig.Pitch, 0f, 0f);
        camera.transform.position = new Vector3(50000f, 0f, 50000f) - camera.transform.forward * distanceM;
        return camera;
    }

    static Bounds WorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
