#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.VehicleReview;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering;

public class VehicleReviewTests
{
    [SetUp] public void EmptyScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [TearDown] public void Cleanup() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [Test]
    public void ReviewIsEditorOnlyAndHasNoPlayerDependencyOrBundle()
    {
        foreach (var id in VehicleReviewCatalog.Ids)
        {
            Assert.IsNull(CollectionCatalog.BundleOf(id));
            Assert.IsFalse(UnitFigures.BySide.Values.Contains(id));
            Assert.IsFalse(UnitFigures.ByVehicle.Values.Contains(id));
        }
        foreach (string file in new[] { "VehicleReviewCatalog.cs", "VehicleReviewStage.cs", "VehicleReviewScene.cs" })
        {
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Scripts/VehicleReview", file)).Trim();
            StringAssert.StartsWith("#if UNITY_EDITOR", source); StringAssert.EndsWith("#endif", source);
            StringAssert.DoesNotContain("RuntimeInitializeOnLoadMethod", source);
            StringAssert.DoesNotContain("Environment.GetCommandLineArgs", source);
            StringAssert.DoesNotContain("Resources.Load", source);
        }
        Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Resources/VehicleReview")));
        Assert.IsTrue(VehicleReviewCatalog.Path.Contains("/Editor/"));
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Assert.IsFalse(scenes.Contains(VehicleReviewCatalog.ScenePath));
        Assert.IsFalse(AssetDatabase.GetDependencies(scenes, true).Any(IsVehicle));
        foreach (string name in AssetDatabase.GetAllAssetBundleNames())
            Assert.IsFalse(AssetDatabase.GetAssetPathsFromAssetBundle(name).Any(IsVehicle), name);
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)))
            Assert.IsFalse(AssetDatabase.GetDependencies(path, true).Any(IsVehicle), path);
    }

    static bool IsVehicle(string path) => path.StartsWith(VehicleReviewCatalog.Folder + "/", StringComparison.Ordinal) ||
        VehicleReviewCatalog.Ids.Any(id => path.Contains(id));

    [Test]
    public void DeliveredSourceArchivesStayOutsideUnityWithVerifiedHashes()
    {
        string root = System.IO.Path.Combine(Paths.RepoRoot, "asset-sources/generated-1948/vehicles");
        var manifest = JsonUtility.FromJson<ArchiveManifest>(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "sources.json")));
        Assert.AreEqual(3, manifest.items.Length); Assert.AreEqual(2, manifest.items.Select(i => i.archive).Distinct().Count());
        foreach (var item in manifest.items)
        {
            string file = System.IO.Path.Combine(root, item.archive);
            Assert.AreEqual(item.archive_sha256, Hash(file));
            Assert.IsFalse(file.StartsWith(Application.dataPath, StringComparison.Ordinal));
        }
    }
    [Serializable] sealed class ArchiveManifest { public ArchiveProof[] items = Array.Empty<ArchiveProof>(); }
    [Serializable] sealed class ArchiveProof { public string archive = "", archive_sha256 = ""; }

    [Test]
    public void CatalogReferencesOnlyEditorModelsAndNoBundleLabels()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(VehicleReviewCatalog.Path);
        Assert.IsNotNull(catalog); catalog!.Validate();
        foreach (var entry in catalog.entries)
        {
            string path = AssetDatabase.GetAssetPath(entry.model);
            Assert.IsTrue(path.StartsWith(VehicleReviewCatalog.Folder + "/" + entry.id + "/", StringComparison.Ordinal));
            var dependencies = AssetDatabase.GetDependencies(path, true);
            Assert.IsTrue(dependencies.Any(p => p.EndsWith("-compatibility.fbx", StringComparison.Ordinal)));
            Assert.IsTrue(dependencies.Any(p => p.EndsWith("_basecolor.jpg", StringComparison.Ordinal)));
            foreach (string dependency in dependencies.Where(IsVehicle))
            {
                Assert.IsTrue(dependency.Contains("/Editor/"), dependency);
                Assert.IsEmpty(AssetImporter.GetAtPath(dependency).assetBundleName, dependency);
            }
        }
    }
    static string AssetFile(string path) => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", path));

    [TestCase(0, 7177)]
    [TestCase(1, 6329)]
    [TestCase(2, 6854)]
    public void SourceGeometryAndOriginalColourMapImportWithoutReduction(int index, int triangles)
    {
        string id = VehicleReviewCatalog.Ids[index], folder = VehicleReviewCatalog.Folder + "/" + id;
        string file = folder + "/" + id + "-compatibility.fbx", map = folder + "/" + id + "_basecolor.jpg";
        var importer = (ModelImporter)AssetImporter.GetAtPath(file);
        Assert.AreEqual(ModelImporterMeshCompression.Off, importer.meshCompression);
        Assert.IsTrue(importer.importAnimation); Assert.AreEqual(ModelImporterAnimationCompression.Off, importer.animationCompression);
        Assert.AreEqual(0, importer.defaultClipAnimations.Length, "The three original GLBs contain no animation clips");
        Assert.AreEqual(0, AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().Count());
        Assert.AreEqual(triangles, AssetDatabase.LoadAllAssetsAtPath(file).OfType<Mesh>().Sum(m => (int)m.GetIndexCount(0) / 3));
        var texture = (TextureImporter)AssetImporter.GetAtPath(map);
        texture.GetSourceTextureWidthAndHeight(out int width, out int height);
        Assert.AreEqual(4096, width); Assert.AreEqual(4096, height);
        Assert.GreaterOrEqual(texture.maxTextureSize, Mathf.Max(width, height));
        Assert.AreEqual(TextureImporterCompression.Uncompressed, texture.textureCompression); Assert.IsFalse(texture.crunchedCompression);
        foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
            Assert.IsFalse(texture.GetPlatformTextureSettings(platform).overridden, platform);
        var image = AssetDatabase.LoadAssetAtPath<Texture2D>(map); Assert.AreEqual(width, image.width); Assert.AreEqual(height, image.height);
        Assert.IsFalse(System.IO.Directory.GetFiles(AssetFile(folder)).Any(p => p.Contains("-near.") || p.Contains("-far.")));
        var provenance = JsonUtility.FromJson<SourceProof>(System.IO.File.ReadAllText(AssetFile(folder + "/provenance.json")));
        Assert.AreEqual(Hash(file), provenance.compatibility_fbx_sha256); Assert.AreEqual(Hash(map), provenance.texture_sha256);
        Assert.AreEqual(0, provenance.source_animation_count); Assert.AreEqual(triangles, provenance.triangles);
        Assert.Less(provenance.geometry_max_vertex_error, 1e-6f); Assert.Less(provenance.uv_max_error, 1e-6f);
    }

    [Serializable] sealed class SourceProof
    {
        public string compatibility_fbx_sha256 = "", texture_sha256 = "";
        public int source_animation_count = 0, triangles = 0;
        public float geometry_max_vertex_error = 0f, uv_max_error = 0f;
    }
    static string Hash(string path)
    {
        using var hash = System.Security.Cryptography.SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(System.IO.File.ReadAllBytes(path.StartsWith("Assets/", StringComparison.Ordinal) ? AssetFile(path) : path))).Replace("-", "").ToLowerInvariant();
    }

    [Test]
    public void ReviewPlacesThreeOriginalModelsWithoutLodSwitches()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(VehicleReviewCatalog.Path);
        Assert.IsNotNull(catalog, "run VehicleReviewAssets.Import before the integration test"); catalog!.Validate();
        var stage = VehicleReviewStage.Create(catalog);
        try
        {
            Assert.AreEqual(3, stage.Placed.Count);
            foreach (var placed in stage.Placed)
            {
                var box = VehicleReviewStage.BoundsOf(placed.Model);
                Assert.AreEqual(0f, box.min.y, .5f, placed.Id + " ground contact");
                Assert.AreEqual(placed.At.x, box.center.x, .5f, placed.Id + " x pivot");
                Assert.AreEqual(placed.At.z, box.center.z, .5f, placed.Id + " z pivot");
                Assert.AreEqual(placed.LongSideM, Mathf.Max(box.size.x, box.size.z), 1f, placed.Id + " presentation scale");
                Assert.AreEqual(1, placed.Model.GetComponentsInChildren<MeshRenderer>(true).Length);
                Assert.AreEqual(0, placed.Model.GetComponentsInChildren<LODGroup>(true).Length);
            }
            foreach (float distance in new[] { 12000f, 60000f })
            {
                stage.Rig.Distance = distance; stage.Rig.Apply(); stage.Choose(stage.Camera);
                Assert.IsTrue(stage.Placed.All(p => p.Model.activeSelf));
            }
            stage.ModelsVisible = false; stage.Choose(stage.Camera); Assert.IsTrue(stage.Placed.All(p => !p.Model.activeSelf));
            Assert.AreEqual(CameraRig.Fov, stage.Camera.fieldOfView);
            Assert.AreEqual(CameraRig.Pitch, stage.Camera.transform.eulerAngles.x, .01f);
            StringAssert.Contains("Non-historical", VehicleReviewStage.ReviewNotice);
        }
        finally { UnityEngine.Object.DestroyImmediate(stage.gameObject); }
    }

    [Test]
    public void ReviewLabelsFitTheActualOverviewAndInspectViewports()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(VehicleReviewCatalog.Path);
        Assert.IsNotNull(catalog);
        var stage = VehicleReviewStage.Create(catalog!);
        try
        {
            // A 16:9 game view must show the entire overview label layout. In particular,
            // fontSize and characterSize multiply: large characterSize values hid the models.
            stage.Camera.aspect = 16f / 9f;
            stage.Overview(); stage.Choose(stage.Camera);
            foreach (var label in stage.GetComponentsInChildren<TextMesh>())
                AssertViewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds, .03f, .97f, label.text);
            for (int i = 0; i < stage.Placed.Count; i++)
            {
                stage.Focus(i); stage.Choose(stage.Camera);
                var label = stage.GetComponentsInChildren<TextMesh>().Single(t => t.text.StartsWith((i + 1) + " · ", StringComparison.Ordinal));
                var box = Viewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds);
                AssertViewport(stage.Camera, label.GetComponent<MeshRenderer>().bounds, .03f, .97f, label.text);
                Assert.Less(box.width, .75f, label.text + " must leave room around the model");
                var model = Viewport(stage.Camera, VehicleReviewStage.BoundsOf(stage.Placed[i].Model));
                Assert.IsFalse(box.Overlaps(model), label.text + " must not cover the inspected model");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(stage.gameObject); }
    }

    static void AssertViewport(Camera camera, Bounds bounds, float min, float max, string message)
    {
        var box = Viewport(camera, bounds);
        Assert.GreaterOrEqual(box.xMin, min, message); Assert.LessOrEqual(box.xMax, max, message);
        Assert.GreaterOrEqual(box.yMin, min, message); Assert.LessOrEqual(box.yMax, max, message);
    }

    static Rect Viewport(Camera camera, Bounds bounds)
    {
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            var p = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    [Test]
    public void ReviewTeardownRestoresLightingAndLeavesExistingCameraUntouched()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(VehicleReviewCatalog.Path);
        Assert.IsNotNull(catalog);
        var original = new GameObject("Existing game camera").AddComponent<Camera>(); original.tag = "MainCamera"; original.fieldOfView = 43f;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientSkyColor = new Color(.2f, .3f, .4f);
        var before = RenderSettings.ambientSkyColor;
        var stage = VehicleReviewStage.Create(catalog!);
        UnityEngine.Object.DestroyImmediate(stage.gameObject);
        Assert.AreEqual(AmbientMode.Flat, RenderSettings.ambientMode);
        Assert.AreEqual(before, RenderSettings.ambientSkyColor);
        Assert.AreEqual(43f, original.fieldOfView);
        Assert.AreSame(original, Camera.main);
    }

    static int Triangles(GameObject root) => root.GetComponentsInChildren<MeshFilter>(true)
        .Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
}
