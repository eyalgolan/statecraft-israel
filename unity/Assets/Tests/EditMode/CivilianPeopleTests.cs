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

/// <summary>Issue #666: MATKOT's civilian figures stand in small deterministic groups at the Israeli towns,
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
        CollectionAssert.DoesNotContain(CivilianPeople.SettledTypes, "arab_town", "the Arab towns wait for the Arab villager figure (#708)");
    }

    [Test]
    public void EveryEligibleSettlementGetsOneSmallGroupAndEveryPlacedCampGetsItsTwoPeopleFromItsDate()
    {
        var map = OpenMap();
        var content = ContentLoader.Load(Statecraft.Game.Paths.Content);
        var hex = content.SettlementHexes.Single(s => s.Id == "revivim");
        var camp = new[] { new Placement(MapActions.TransitCamp, hex.Q, hex.R, new DateTime(1950, 8, 1)) };

        var before = CivilianPeople.Wants(map, new DateTime(1950, 7, 31), camp, content);
        int settlements = map.Settlements.Count(s => CivilianPeople.SettledTypes.Contains(s.Type));
        Assert.AreEqual(settlements * CivilianPeople.SettledGroupSize, before.Count);
        CollectionAssert.DoesNotContain(before.Select(w => w.Model), CivilianPeople.ArrivingMan);

        var from = CivilianPeople.Wants(map, new DateTime(1950, 8, 1), camp, content);
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
        Assert.AreEqual(3, asked.Count, "only the three far settled models, once each");
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
            Assert.AreEqual(CivilianPeople.FigureHeightM, WorldBounds(child.gameObject).size.y, CivilianPeople.FigureHeightM * 0.02f, p.Key + " symbolic height");
            Assert.IsTrue(child.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterial.enableInstancing), p.Key + " is GPU-instanced");
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
            Assert.AreEqual(3, asked.Count, "near models asked once on entering close zoom");
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
