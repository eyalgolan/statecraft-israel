#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class Generated1948BuildingsPropsTests
{
    [TearDown] public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear(); KitScale.Reset();
    }

    [Test] public void ReviewRequiresItsOwnExplicitFlag()
    {
        Assert.IsFalse(Generated1948BuildingsPropsReview.Requested("https://game/", Array.Empty<string>(), true));
        Assert.IsFalse(Generated1948BuildingsPropsReview.Requested("https://game/?units&building-preview", Array.Empty<string>(), true));
        Assert.IsFalse(Generated1948BuildingsPropsReview.Requested(null, new[] { "-units" }, false));
        Assert.IsTrue(Generated1948BuildingsPropsReview.Requested("https://game/?buildingpropsreview", Array.Empty<string>(), true));
        Assert.IsTrue(Generated1948BuildingsPropsReview.Requested(null, new[] { "-buildingpropsreview" }, false));
    }

    [Test] public void PlayableSceneNamesEveryScopedAssetWithoutVehiclesOrCharacters()
    {
        Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Generated1948BuildingsPropsReview.ScenePath));
        var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
        Assert.AreEqual(11, review.Prefabs.Count);
        Assert.AreEqual(review.Prefabs.Count, review.Labels.Count);
        Assert.AreEqual(review.Prefabs.Count, review.Materials.Count);
        CollectionAssert.AllItemsAreUnique(review.Labels);
        foreach (var label in review.Labels) { Assert.IsNotEmpty(label); Assert.IsFalse(label.Contains("generated-1948")); }
        var numbers = new List<int>();
        for (int i = 0; i < review.Prefabs.Count; i++)
        {
            Assert.IsNotNull(review.Prefabs[i]); Assert.IsNotNull(review.Materials[i]);
            string file = AssetDatabase.GetAssetPath(review.Prefabs[i]);
            StringAssert.StartsWith("Assets/Art/Generated1948Review/generated-1948-", file);
            numbers.Add(int.Parse(System.IO.Path.GetFileName(file).Split('-')[2]));
            Assert.IsNotNull(review.Materials[i].mainTexture);
        }
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 }, numbers);
    }

    [Test] public void ReviewSamplesFitPlatformAndTouchFloor()
    {
        var map = OpenMap();
        var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
        review.Place(map, Camera.main);
        var platform = review.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Review display surface").bounds;
        Assert.AreEqual(11, review.PlacedCount);
        var display = review.transform.Find("Buildings and props — review samples");
        foreach (Transform sample in display)
        {
            if (sample.name == "Review display surface") continue;
            var bounds = Generated1948BuildingsPropsReview.BoundsOf(sample.gameObject);
            Assert.GreaterOrEqual(bounds.min.x, platform.min.x); Assert.LessOrEqual(bounds.max.x, platform.max.x);
            Assert.GreaterOrEqual(bounds.min.z, platform.min.z); Assert.LessOrEqual(bounds.max.z, platform.max.z);
            Assert.AreEqual(platform.max.y, bounds.min.y, .02f, sample.name);
        }
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build(); return map;
    }

    [Test] public void BedouinModelsBelongToExistingEncampmentsAndNeverTownHouses()
    {
        var map = OpenMap();
        var layer = map.GetComponentInChildren<TownModelLayer>();
        Assert.IsNotNull(layer); Assert.IsTrue(layer.HasModels);
        Assert.Greater(layer.Instances().Count(i => i.model == TownModel.BedouinTent2026), 0);
        foreach (var settlement in map.Settlements)
            Assert.IsFalse(layer.InstancesOf(settlement.Id).Any(i => i.Model == TownModel.BedouinTent2026 || i.Model == TownModel.KhakiTent2026), settlement.Id);
        Assert.IsTrue(layer.InstancesOf("netanya").Any(i => i.Model == TownModel.NetanyaTileRepair2026), "approved tiled house actually appears in existing Netanya plots");
        Assert.IsTrue(layer.Instances().Any(i => i.model == TownModel.CourtyardRepair2026), "approved courtyard variation actually appears in existing Arab-town plots");
        Assert.IsNull(Object.FindFirstObjectByType<Generated1948BuildingsPropsReview>(), "ordinary MapScene never loads the review scene");
    }

    [Test] public void CanvasModelsKeepCampCensusAndFarGeometryUnchanged()
    {
        var map = OpenMap(); var s = map.Settlements.Single(x => x.Id == "revivim");
        var at = map.Frame.ToWorld(s.Lon, s.Lat, 0);
        var camps = new List<(string id, Vector2 at, DateTime from)> { ("transit_camp_revivim", new Vector2(at.x, at.z), new DateTime(1950, 6, 15)) };
        var original = SettlementKits.Maabarot(camps, PlacementLayer.CampGround(map), out var before);
        var authored = SettlementKits.MaabarotWithModels(camps, PlacementLayer.CampGround(map), out var after, out var models);
        try
        {
            Assert.IsNotNull(original[0].mesh); Assert.IsNotNull(authored[0].mesh);
            Assert.AreEqual(original[0].tents, authored[0].tents); Assert.AreEqual(original[0].huts, authored[0].huts);
            CollectionAssert.AreEqual(before.Select(p => (p.Id, p.Kind, p.C, p.Yaw, p.W, p.D, p.H)),
                after.Select(p => (p.Id, p.Kind, p.C, p.Yaw, p.W, p.D, p.H)));
            Assert.AreEqual(original[0].mesh!.vertexCount, authored[0].mesh!.vertexCount);
            Assert.AreEqual(original[0].mesh!.GetIndexCount(0), authored[0].mesh!.GetIndexCount(0));
            Assert.Greater(models.Count, 0); Assert.IsTrue(models.All(m => m.Model == TownModel.KhakiTent2026));
            Assert.IsNotNull(authored[0].close);
            foreach (var model in models)
            {
                var plot = after.Single(p => p.Kind == "tent" && (p.C - model.C).sqrMagnitude < .01f);
                Assert.LessOrEqual(model.W, plot.W + .01f, "tent stays inside existing plot width");
                Assert.LessOrEqual(model.D, plot.D + .01f, "tent stays inside existing plot depth");
            }
        }
        finally
        {
            foreach (var camp in original) if (camp.mesh != null) Object.DestroyImmediate(camp.mesh);
            foreach (var camp in authored) { if (camp.mesh != null) Object.DestroyImmediate(camp.mesh); if (camp.close != null) Object.DestroyImmediate(camp.close); }
        }
    }
}
