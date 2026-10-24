#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Statecraft.Game.Map.Kits;

public class OriginalModelsStreamingTests
{
    [Test]
    public void EveryOriginalHasItsOwnBundle()
    {
        var ids = CollectionCatalog.Ids.ToArray();
        Assert.AreEqual(73, ids.Length, "all 23 buildings and 50 collection models");
        Assert.AreEqual(ids.Length, ids.Select(CollectionCatalog.BundleOf).Distinct().Count(), "one model per bundle");
    }

    [Test]
    public void TheBootSceneDoesNotPackTalsMeshesOrColourMaps()
    {
        var dependencies = AssetDatabase.GetDependencies("Assets/Map.unity", true);
        var originals = dependencies.Where(p => p.StartsWith("Assets/Art/Collection/", StringComparison.Ordinal)
            || p.StartsWith("Assets/Art/Settlements/Prototype/Models/", StringComparison.Ordinal)
            || p == "Assets/Art/Settlements/TownModels.asset").ToArray();
        Assert.IsEmpty(originals, "originals must arrive through their bundles after the first frame");
    }

    [Test]
    public void BothDetailRequestsUseTheSameOriginalPrefab()
    {
        foreach (var id in CollectionCatalog.Ids)
            Assert.AreEqual(CollectionCatalog.PrefabPath(id, CollectionDetail.Near),
                CollectionCatalog.PrefabPath(id, CollectionDetail.Far), id + ": no separate far mesh/prefab");
    }

    [Test]
    public void NothingFetchesUntilTheReadyFrameHasActuallyRendered()
    {
        Assert.IsFalse(CollectionStream.MayFetch(100, -1));
        Assert.IsFalse(CollectionStream.MayFetch(10, 10));
        Assert.IsFalse(CollectionStream.MayFetch(12, 10));
        Assert.IsTrue(CollectionStream.MayFetch(13, 10));
    }

    [Test]
    public void TheStrategicCapKeepsGameplayOriginalsAndSkipsDioramaOriginals()
    {
        Assert.IsTrue(OriginalModelDistance.CloseAt(60000, null));
        Assert.IsFalse(OriginalModelDistance.CloseAt(250000, null));
        Assert.IsTrue(OriginalModelDistance.CloseAt(125000, true));
        Assert.IsFalse(OriginalModelDistance.CloseAt(125000, false));
    }

    [Test]
    public void TheStrategicCapRestoresOtherLayersRendererFlags()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cameraGo = new GameObject("camera", typeof(Camera));
        try
        {
            var renderer = go.GetComponent<Renderer>();
            var cap = go.AddComponent<OriginalModelDistance>();
            // EditMode does not run play-only OnEnable. Exercise the same lifecycle explicitly.
            typeof(OriginalModelDistance).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!.Invoke(cap, null);
            var camera = cameraGo.GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 250000, 0), Quaternion.Euler(90, 0, 0));
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var begin = typeof(OriginalModelDistance).GetMethod("Begin", flags)!;
            var end = typeof(OriginalModelDistance).GetMethod("End", flags)!;
            foreach (bool previouslyOff in new[] { false, true })
            {
                renderer.forceRenderingOff = previouslyOff;
                begin.Invoke(cap, new object[] { default(ScriptableRenderContext), camera });
                Assert.IsTrue(renderer.forceRenderingOff, "diorama has zero full-original renderers");
                end.Invoke(cap, new object[] { default(ScriptableRenderContext), camera });
                Assert.AreEqual(previouslyOff, renderer.forceRenderingOff, "preserve the placement layer's choice");
            }
        }
        finally
        {
            typeof(OriginalModelDistance).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance)!.Invoke(go.GetComponent<OriginalModelDistance>(), null);
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(cameraGo);
        }
    }

    [Test]
    public void EveryTownBuildingStreamsTheOriginalMeshAndMaterial()
    {
        foreach (var id in TownModels.Ids)
        {
            var path = CollectionCatalog.PrefabPath(id, CollectionDetail.Near);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, id);
            var mesh = prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            Assert.AreEqual($"{CollectionCatalog.BuildingsFolder}/{id}/{id}.fbx", AssetDatabase.GetAssetPath(mesh), id);
            Assert.IsNotNull(prefab.GetComponent<OriginalModelDistance>(), id);
        }
    }

    [Test]
    public void C05AndTheFarmerStandOnAnOriginalClipWithoutBakingAMesh()
    {
        foreach (var id in new[] { "figure-c05-pioneer-veteran-v2", "figure-pioneer-farmer-v1-tpose" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            Assert.IsNotNull(prefab.GetComponent<OriginalModelPose>(), id);
            var go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                typeof(OriginalModelPose).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance)!.Invoke(go.GetComponent<OriginalModelPose>(), null);
                var animator = go.GetComponent<Animator>();
                Assert.IsFalse(animator.enabled, "pose sampled once, then held");
                Assert.IsNotNull(animator.runtimeAnimatorController);
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    Assert.AreEqual($"{CollectionCatalog.Folder}/{id}/{id}.fbx", AssetDatabase.GetAssetPath(r.sharedMesh));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    [Test]
    public void AllOriginalImportsKeepSourceDimensionsAndEveryClipWithoutReduction()
    {
        foreach (var id in CollectionCatalog.Ids)
        {
            string dir = CollectionCatalog.IsBuilding(id) ? CollectionCatalog.BuildingsFolder : CollectionCatalog.Folder;
            string path = $"{dir}/{id}/{id}.fbx";
            var model = (ModelImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(ModelImporterMeshCompression.Off, model.meshCompression, id);
            Assert.AreEqual(ModelImporterAnimationCompression.Off, model.animationCompression, id);
            Assert.IsFalse(model.resampleCurves, id);
            int clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Count(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            Assert.AreEqual(model.defaultClipAnimations.Length, clips, id);
            var serialized = new SerializedObject(model);
            var lod = serialized.FindProperty("m_GenerateMeshLods") ?? serialized.FindProperty("generateMeshLods");
            Assert.IsNotNull(lod, "Unity's LOD setting must be verifiable");
            Assert.IsFalse(lod.boolValue, id);
            var texture = (TextureImporter)AssetImporter.GetAtPath($"{dir}/{id}/{id}_basecolor.jpg");
            texture.GetSourceTextureWidthAndHeight(out int width, out int height);
            Assert.GreaterOrEqual(texture.maxTextureSize, Math.Max(width, height), id);
            Assert.IsFalse(texture.crunchedCompression, id);
            var web = texture.GetPlatformTextureSettings("WebGL");
            Assert.GreaterOrEqual(web.maxTextureSize, Math.Max(width, height), id);
            Assert.IsFalse(web.crunchedCompression, id);
        }
    }

    [Test]
    public void ALateOriginalUsesTheCamerasExistingZoomChoice()
    {
        var first = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var late = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cameraGo = new GameObject("late-arrival camera", typeof(Camera));
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var type = typeof(OriginalModelDistance);
        var begin = type.GetMethod("Begin", flags)!;
        var end = type.GetMethod("End", flags)!;
        var enable = type.GetMethod("OnEnable", flags)!;
        var disable = type.GetMethod("OnDisable", flags)!;
        OriginalModelDistance? oldCap = null, newCap = null;
        try
        {
            var camera = cameraGo.GetComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 60000, 0), Quaternion.Euler(90, 0, 0));
            oldCap = first.AddComponent<OriginalModelDistance>(); enable.Invoke(oldCap, null);
            begin.Invoke(oldCap, new object[] { default(ScriptableRenderContext), camera });
            end.Invoke(oldCap, new object[] { default(ScriptableRenderContext), camera });
            camera.transform.position = new Vector3(0, 125000, 0);
            begin.Invoke(oldCap, new object[] { default(ScriptableRenderContext), camera });
            Assert.IsFalse(first.GetComponent<Renderer>().forceRenderingOff, "the camera retained the close choice");
            newCap = late.AddComponent<OriginalModelDistance>(); enable.Invoke(newCap, null);
            begin.Invoke(newCap, new object[] { default(ScriptableRenderContext), camera });
            Assert.IsFalse(late.GetComponent<Renderer>().forceRenderingOff,
                "a late stream must use the same camera choice, or its kit can disappear before the model draws");
        }
        finally
        {
            if (oldCap != null) disable.Invoke(oldCap, null);
            if (newCap != null) disable.Invoke(newCap, null);
            UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(late); UnityEngine.Object.DestroyImmediate(cameraGo);
        }
    }
}
