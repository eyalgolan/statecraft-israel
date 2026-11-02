#nullable enable
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class Generated1948BuildingsPropsTests
{
    [TearDown] public void Clean() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    [Test] public void EditorReviewIsNeverBuildListedAndHasNoRuntimeLoader()
    {
        Assert.IsTrue(Generated1948BuildingsPropsReview.ScenePath.StartsWith("Assets/Editor/"));
        Assert.IsFalse(EditorBuildSettings.scenes.Any(s => s.path.EndsWith("GeneratedBuildingsPropsReview.unity")));
        Assert.IsFalse(typeof(Generated1948BuildingsPropsReview).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length != 0));
    }

    [Test] public void EditorReviewNamesAllElevenModelsWithNoSourceDefectMarks()
    {
        var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
        Assert.AreEqual(11, review.Prefabs.Count); Assert.AreEqual(11, review.Materials.Count); Assert.AreEqual(11, review.Labels.Count);
        CollectionAssert.AllItemsAreUnique(review.Labels);
        // Each slot holds its own model's prefab and material, wherever the model lives now (review folder, or the collection's once placed).
        var ids = (string[])System.Type.GetType("Statecraft.Game.EditorTools.Generated1948BuildingsPropsAssets, Assembly-CSharp-Editor")!
            .GetField("Ids")!.GetValue(null)!;
        Assert.AreEqual(review.Prefabs.Count, ids.Length);
        for (int i = 0; i < review.Prefabs.Count; i++)
        {
            Assert.IsNotNull(review.Prefabs[i]); Assert.IsNotNull(review.Materials[i]);
            string stem = Generated1948BuildingsPropsReview.DirOf(ids[i]) + "/" + ids[i];
            Assert.AreEqual(Generated1948BuildingsPropsReview.PrefabOf(ids[i]), AssetDatabase.GetAssetPath(review.Prefabs[i]), ids[i] + " prefab");
            Assert.AreEqual(stem + ".mat", AssetDatabase.GetAssetPath(review.Materials[i]), ids[i] + " material");
        }
        // 01, 03 and 05 were regenerated on #1379; no label may still mark a roof gap or rear opening.
        Assert.IsFalse(review.Labels.Any(s => s.Contains("roof gaps") || s.Contains("rear opening")));
    }

    /// <summary>The buildings-props review shows a placed model's shipped prefab (the five houses' streamed prefabs, the
    /// Bedouin tent and the khaki tent in the 1948 Collection) and must never re-save it, as VehicleReviewAssets does not for a
    /// placed vehicle (#2014). Each shipped prefab gets a marker child first: a re-save from the FBX would drop it. Every file
    /// the run writes is put back afterwards.</summary>
    [Test] public void PrepareLeavesThePlacedModelsShippedPrefabsUntouched()
    {
        var ids = (string[])System.Type.GetType("Statecraft.Game.EditorTools.Generated1948BuildingsPropsAssets, Assembly-CSharp-Editor")!
            .GetField("Ids")!.GetValue(null)!;
        var placed = ids.Where(id => System.Array.IndexOf(Statecraft.Game.Map.Kits.TownModels.Ids, id) >= 0
            || Statecraft.Game.Map.Collection.CollectionCatalog.IsGeneratedProp(id)).ToArray();
        Assert.AreEqual(7, placed.Length, "five houses, the Bedouin tent and the khaki tent are placed");
        var shipped = placed.Select(Generated1948BuildingsPropsReview.PrefabOf).ToArray();
        var before = new[] { Generated1948BuildingsPropsReview.AssetFolder }.Concat(ids.Select(Generated1948BuildingsPropsReview.DirOf))
            .Concat(shipped.Select(p => System.IO.Path.GetDirectoryName(p)!)).Distinct()
            .SelectMany(d => System.IO.Directory.GetFiles(AssetFile(d), "*", System.IO.SearchOption.AllDirectories))
            .Distinct().ToDictionary(f => f, System.IO.File.ReadAllBytes);
        const string Marker = "prepare-must-not-resave";
        string[] resaved;
        try
        {
            foreach (string path in shipped)
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try { new GameObject(Marker).transform.SetParent(contents.transform, false); PrefabUtility.SaveAsPrefabAsset(contents, path); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            System.Type.GetType("Statecraft.Game.EditorTools.Generated1948BuildingsPropsAssets, Assembly-CSharp-Editor")!.GetMethod("Prepare")!.Invoke(null, null);
            resaved = shipped.Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find(Marker) == null).ToArray();
        }
        finally
        {
            foreach (var file in before.Keys.Where(f => !System.IO.File.ReadAllBytes(f).SequenceEqual(before[f]))) System.IO.File.WriteAllBytes(file, before[file]);
            AssetDatabase.Refresh();
        }
        CollectionAssert.IsEmpty(resaved, "Prepare re-saved the shipped prefab of a placed model");
    }

    static string AssetFile(string path) => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", path));

    [Test] public void EditorSamplesFitPlatformAndTouchFloor()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
        review.Place(map, Camera.main);
        var platform = review.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Review display surface").bounds;
        foreach (Transform sample in review.transform.Find("Buildings and props — review samples"))
        {
            if (sample.name == "Review display surface") continue;
            var b = Generated1948BuildingsPropsReview.BoundsOf(sample.gameObject);
            Assert.GreaterOrEqual(b.min.x, platform.min.x); Assert.LessOrEqual(b.max.x, platform.max.x);
            Assert.GreaterOrEqual(b.min.z, platform.min.z); Assert.LessOrEqual(b.max.z, platform.max.z);
            Assert.AreEqual(platform.max.y, b.min.y, .02f, sample.name);
        }
    }
}
