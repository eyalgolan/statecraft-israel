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

    [Test] public void EditorReviewNamesAllOriginalsAndMarksKnownSourceDefects()
    {
        var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
        var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
        Assert.AreEqual(11, review.Prefabs.Count); Assert.AreEqual(11, review.Materials.Count); Assert.AreEqual(11, review.Labels.Count);
        CollectionAssert.AllItemsAreUnique(review.Labels);
        for (int i = 0; i < review.Prefabs.Count; i++)
        {
            Assert.IsNotNull(review.Prefabs[i]); Assert.IsNotNull(review.Materials[i]);
            StringAssert.StartsWith(Generated1948BuildingsPropsReview.AssetFolder, AssetDatabase.GetAssetPath(review.Prefabs[i]));
        }
        Assert.AreEqual(2, review.Labels.Count(s => s.Contains("roof gaps")));
        Assert.AreEqual(1, review.Labels.Count(s => s.Contains("rear opening")));
    }

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
