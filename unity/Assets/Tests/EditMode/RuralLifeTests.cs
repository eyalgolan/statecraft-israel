#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Routes;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Issue #667: all six of MATKOT's rural models are sparsely and deterministically placed by settlement type,
/// clear of the map's occupied ground, streamed after the first frame, upright, and absent without harming the map when
/// their bundle cannot be read.</summary>
public class RuralLifeTests
{
    Action<string, CollectionDetail, Action<GameObject?>> saved = null!;
    readonly List<(string id, CollectionDetail detail, Action<GameObject?> done)> asked = new List<(string, CollectionDetail, Action<GameObject?>)>();

    [SetUp]
    public void Seam()
    {
        saved = RuralLife.Request;
        asked.Clear();
        RuralLife.Request = (id, detail, done) => asked.Add((id, detail, done));
    }

    [TearDown]
    public void Reset()
    {
        RuralLife.Request = saved;
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

    void AnswerAll(bool found = true)
    {
        var pending = asked.ToList();
        asked.Clear();
        foreach (var (id, detail, done) in pending) done(found ? Prefab(id, detail) : null);
    }

    [Test]
    public void EveryLaneModelIsPlacedInItsSourcedSettingAtBothDetails()
    {
        var map = OpenMap();
        var layer = RuralLife.Show(map)!;
        CollectionAssert.AreEquivalent(RuralLife.Models, layer.Wanted.Select(w => w.Model).Distinct());
        Assert.AreEqual(layer.Wanted.Count, layer.Wanted.Select(w => (w.Key, w.Model)).Distinct().Count(), "each anchor once per model");
        foreach (var id in RuralLife.Models)
        {
            Assert.Greater(layer.Wanted.Count(w => w.Model == id), RuralLife.MaxAnchorsPerModel, id + " has spare anchors");
            Assert.AreEqual(2, asked.Count(a => a.id == id), id + " asks for far and near once");
            CollectionAssert.AreEquivalent(new[] { CollectionDetail.Far, CollectionDetail.Near }, asked.Where(a => a.id == id).Select(a => a.detail));
        }
        AnswerAll();
        foreach (var id in RuralLife.Models)
            Assert.AreEqual(RuralLife.MaxAnchorsPerModel, layer.Scenes.Count(w => w.Model == id), id + " sparse placement count");
        Assert.AreEqual(RuralLife.Models.Length * RuralLife.MaxAnchorsPerModel, layer.Scenes.Count, "18 sparse rural scenes");
        CollectionAssert.IsEmpty(layer.Missing);
        var types = map.Settlements.Concat(map.Localities.Select(p => p.Settlement))
            .GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().Type);
        foreach (var want in layer.Scenes)
        {
            if (want.Model == RuralLife.Camel) StringAssert.StartsWith("camp:", want.Key, want.Model + " uses a Bedouin-area camp");
            else if (want.Model == RuralLife.Tractor) Assert.AreEqual("kibbutz", types[want.Key], want.Key);
            else if (want.Model == RuralLife.Wagon || want.Model == RuralLife.Horse)
                Assert.IsTrue(types[want.Key] == "moshav" ||
                    (TownLandmarks.ById.TryGetValue(want.Key, out var model) && model == TownLandmarks.Moshava), want.Key);
            else Assert.AreEqual("village", types[want.Key], want.Key);
        }
        Assert.AreEqual(layer.Scenes.Sum(w => RuralLife.MembersOf(w.Model)) * 2, layer.Placed.Count, "one far and one near instance of every scene's models");
        Assert.AreEqual(RuralLife.MaxAnchorsPerModel * RuralLife.FlockSize * 2, layer.Placed.Count(p => p.Model == RuralLife.Sheep), "the sheep stand in flocks");
    }

    [Test]
    public void SeededSpotsAreDeterministicClearAndUpright()
    {
        var map = OpenMap();
        var first = RuralLife.Show(map)!;
        AnswerAll();
        var spots = first.Placed.Select(p => (p.Key, p.Model, p.Detail, p.At, p.Member)).ToList();
        Object.DestroyImmediate(first.gameObject);
        var second = RuralLife.Show(map)!;
        AnswerAll();
        CollectionAssert.AreEqual(spots, second.Placed.Select(p => (p.Key, p.Model, p.Detail, p.At, p.Member)).ToList(), "same map, same seeded spots");

        var ground = PlacementLayer.CampGround(map);
        foreach (var p in second.Placed.Where(p => p.Detail == CollectionDetail.Far))
        {
            var flat = new Vector2(p.At.x, p.At.z);
            Assert.IsFalse(ground.OnRoute(flat, RuralLife.ClearanceRadiusM), p.Key + " route clearance");
            Assert.IsFalse(ground.OnToken(flat, RuralLife.ClearanceRadiusM), p.Key + " token clearance");
            var go = second.transform.Find(p.Name);
            Assert.IsNotNull(go, p.Name);
            var want = second.Scenes.Single(w => w.Key == p.Key && w.Model == p.Model);
            var prefab = Prefab(p.Model, p.Detail);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, RuralLife.YawOf(want, p.Member), 0f) * prefab.transform.localRotation,
                go!.localRotation), 0.01f, p.Name + " preserves the prefab's FBX axis conversion under its yaw");
            var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.AreEqual(p.At.y, bounds.min.y, 1f, p.Name + " rests on the ground");
        }
        // No kit tree stands over a scene: the trees whose crowns reach its ground are left out (#667: kept clear of the other props).
        var veg = map.GetComponentInChildren<Statecraft.Game.Map.Kits.VegetationLayer>(true);
        Assert.IsNotNull(veg);
        var over = new List<string>();
        foreach (var w in second.Scenes)
        {
            var c = second.Placed.First(p => p.Key == w.Key && p.Model == w.Model && p.Member == 0).At;
            float r = RuralLife.SceneRadiusM(w.Model);
            int n = veg.Drawable().Count(t =>
            {
                var q = t.m.GetColumn(3); float reach = r + Statecraft.Game.Map.Kits.VegetationLayer.CrownRadius(t.kind, t.m);
                return (new Vector2(q.x, q.z) - new Vector2(c.x, c.z)).sqrMagnitude < reach * reach;
            });
            if (n > 0) over.Add($"{w.Key}:{w.Model}: {n} trees");
        }
        CollectionAssert.IsEmpty(over, "kit trees over a rural scene");
    }

    [Test]
    public void CloseZoomUsesNearModelsAndOnlyNearAnimalsAnimate()
    {
        var map = OpenMap();
        var layer = RuralLife.Show(map)!;
        AnswerAll();
        Assert.IsFalse(RuralLife.CloseAt(RuralLife.CloseExitM + 1f, true));
        Assert.IsTrue(RuralLife.CloseAt(RuralLife.CloseEnterM, false));
        Assert.IsTrue(RuralLife.CloseAt(RuralLife.CloseExitM, true));

        var cam = Camera.main!;
        cam.transform.SetPositionAndRotation(new Vector3(0f, RuralLife.CloseEnterM, 0f), Quaternion.Euler(90f, 0f, 0f));
        layer.Choose(default, cam);
        Assert.IsTrue(layer.LastClose);
        Assert.IsTrue(layer.transform.Cast<Transform>().Where(t => t.name.EndsWith(":near")).
            SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).All(r => !r.forceRenderingOff));
        Assert.IsTrue(layer.transform.Cast<Transform>().Where(t => t.name.EndsWith(":far")).
            SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).All(r => r.forceRenderingOff));
        var animated = layer.GetComponentsInChildren<Animator>(true).Where(a => a.runtimeAnimatorController != null).ToList();
        Assert.Greater(animated.Count, 0, "the supplied near-detail horse and sheep idle clips are present");
        Assert.IsTrue(animated.All(a => a.enabled));
        cam.transform.position = new Vector3(0f, RuralLife.CloseExitM + 1f, 0f);
        layer.Choose(default, cam);
        Assert.IsFalse(layer.LastClose);
        Assert.IsTrue(layer.transform.Cast<Transform>().Where(t => t.name.EndsWith(":near")).
            SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).All(r => r.forceRenderingOff));
        Assert.IsTrue(layer.transform.Cast<Transform>().Where(t => t.name.EndsWith(":far")).
            SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).All(r => !r.forceRenderingOff));
        Assert.IsTrue(layer.GetComponentsInChildren<Animator>(true).All(a => !a.enabled));
    }

    [Test]
    public void ABundleThatDoesNotStreamLeavesTheMapRunning()
    {
        var map = OpenMap();
        var layer = RuralLife.Show(map)!;
        int requests = asked.Count;
        for (int i = 0; i < requests; i++)
            LogAssert.Expect(LogType.Warning, new Regex("^RURAL: .* did not stream"));
        AnswerAll(found: false);
        Assert.AreEqual(0, layer.transform.childCount);
        CollectionAssert.AreEquivalent(RuralLife.Models, layer.Missing);
        Assert.IsNotNull(map.Terrain);
        Assert.DoesNotThrow(() => RuralLife.Show(map));
        Assert.AreEqual(0, asked.Count, "a failed detail is not requested every frame");
    }

    [Test]
    public void ModelsUseTheCollectionStreamAndNeverResources()
    {
        Assert.AreEqual(typeof(CollectionStream), saved.Method.DeclaringType);
        Assert.AreEqual(nameof(CollectionStream.Request), saved.Method.Name);
        foreach (var id in RuralLife.Models)
        {
            Assert.IsTrue(CollectionCatalog.BundleOf(id) == CollectionCatalog.Vehicles || CollectionCatalog.BundleOf(id) == CollectionCatalog.Animals, id);
            foreach (var detail in new[] { CollectionDetail.Far, CollectionDetail.Near })
            {
                var path = CollectionCatalog.PrefabPath(id, detail);
                Assert.IsNotNull(Prefab(id, detail), path);
                StringAssert.DoesNotContain("/Resources/", path);
                foreach (var dep in AssetDatabase.GetDependencies(path, true)) StringAssert.DoesNotContain("/Resources/", dep);
            }
            Assert.IsNull(Resources.Load<GameObject>(id));
        }
    }

    /// <summary>The owner on v0.1.1 (18 October 2026): MATKOT's buildings were "way bigger then the train". The farm and
    /// village models stand at a believable size beside the steam train the map runs (ServiceTrain): each one's long side,
    /// seen from above, is inside its band of the locomotive's car (about 204 m: MATKOT's locomotive and tender at
    /// RouteTrain.Scale x ServiceTrain.ModelSize / ServiceTrain.Cars), so a tractor or a wagon is shorter than the
    /// locomotive, a sheep is a small fraction of it, and every model is far smaller than a unit token's figure
    /// (UnitBadge.FigureHeightM x UnitShowcase.FigureScale, about 2.5 km). The bands follow the buildings' one world scale
    /// (an 8.5 m house as long as the locomotive's car, 24x real): a tractor about 3 m real is about 0.35 of the car.
    /// The far and near models of a scene are one size.</summary>
    [Test]
    public void EveryModelStandsAtABelievableSizeBesideTheTrain()
    {
        var map = OpenMap();
        var layer = RuralLife.Show(map)!;
        AnswerAll();
        float loco = RouteTrain.ModelRealLengthM * RouteTrain.Scale * ServiceTrain.ModelSize / ServiceTrain.Cars;
        float figure = UnitBadge.FigureHeightM * UnitShowcase.FigureScale;
        Assert.AreEqual(204f, loco, 1f, "the locomotive's car");
        var bands = new Dictionary<string, (float lo, float hi)>
        {
            [RuralLife.Tractor] = (0.25f, 0.5f), [RuralLife.Wagon] = (0.3f, 0.85f), [RuralLife.Horse] = (0.2f, 0.4f),
            [RuralLife.Sheep] = (0.1f, 0.2f), [RuralLife.Donkey] = (0.13f, 0.3f), [RuralLife.Camel] = (0.25f, 0.5f),
        };
        var bad = new List<string>();
        var sizes = new Dictionary<(string, string, CollectionDetail), float>();
        foreach (var p in layer.Placed)
        {
            var go = layer.transform.Find(p.Name)!;
            var b = go.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            float side = Mathf.Max(b.size.x, b.size.z), share = side / loco;
            sizes[(p.Name, p.Model, p.Detail)] = side;
            var (lo, hi) = bands[p.Model];
            if (share < lo || share > hi) bad.Add($"{p.Name}: {side:0.0} m long, {share:0.00} of the locomotive's car (band {lo}-{hi})");
            if (b.size.y > figure / 10f) bad.Add($"{p.Name}: {b.size.y:0} m tall against a {figure:0} m unit figure");
        }
        foreach (var g in sizes.GroupBy(kv => kv.Key.Item2))
            TestContext.WriteLine($"{g.Key}: {string.Join(", ", g.Select(kv => $"{kv.Key.Item3} {kv.Value:0.0} m").Distinct().Take(4))}");
        foreach (var p in layer.Placed.Where(p => p.Detail == CollectionDetail.Far))
        {
            string near = p.Name.Substring(0, p.Name.Length - "far".Length) + "near";
            var n = sizes.Where(kv => kv.Key.Item1 == near).Select(kv => kv.Value).ToList();
            if (n.Count == 1 && Mathf.Abs(n[0] - sizes[(p.Name, p.Model, p.Detail)]) > 0.1f * n[0]) bad.Add($"{p.Name}: far and near differ ({sizes[(p.Name, p.Model, p.Detail)]:0} / {n[0]:0} m)");
        }
        CollectionAssert.IsEmpty(bad, $"{bad.Count} rural models off the train's scale");
        foreach (var id in RuralLife.Models) Assert.Greater(layer.Placed.Count(p => p.Model == id), 0, id + " drew");
    }

    /// <summary>#667: kept clear of the other props. No rural scene stands under a civilian figure (#666, shown first by
    /// GameRoot) or a wild animal (#670).</summary>
    [Test]
    public void ScenesKeepClearOfTheCiviliansAndTheWildlife()
    {
        var map = OpenMap();
        var savedPeople = CivilianPeople.Request;
        CivilianPeople.Request = (id, detail, done) => { };
        try
        {
            var content = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
            var people = CivilianPeople.ShowAt(map, new DateTime(1948, 5, 15), Array.Empty<Statecraft.Sim.Placement>(), content)!;
            Assert.Greater(people.Wanted.Count, 0, "the civilians are wanted");
            var layer = RuralLife.Show(map)!;
            AnswerAll();
            var others = RuralLife.OtherProps(map);
            Assert.Greater(others.Count, people.Wanted.Count, "the civilians and the wildlife");
            Assert.AreEqual(RuralLife.Models.Length * RuralLife.MaxAnchorsPerModel, layer.Scenes.Count);
            foreach (var p in layer.Placed.Where(p => p.Member == 0 && p.Detail == CollectionDetail.Far))
                Assert.IsTrue(TownLandmarks.Clear(new Vector2(p.At.x, p.At.z), RuralLife.ClearanceRadiusM, others), p.Name + " clear of the civilians and the wildlife");
        }
        finally { CivilianPeople.Request = savedPeople; }
    }
}
