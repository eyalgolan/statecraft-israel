using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Statecraft.Game.Review;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Editor review preparation only. Never updates a player catalog, bundle or baked LOD asset.</summary>
    public static class Generated1948BuildingsPropsAssets
    {
        public static readonly string[] Ids = {
            "generated-1948-01-netanya-tiled-house-original", "generated-1948-02-netanya-two-storey-balcony-house",
            "generated-1948-03-netanya-flat-roof-porch-house", "generated-1948-04-dark-basalt-house",
            "generated-1948-05-plastered-courtyard-house-original", "generated-1948-06-bedouin-b-enclosed-striped-tent",
            "generated-1948-08-khaki-canvas-tent", "generated-1948-09-stone-village-well",
            "generated-1948-10-low-sandbag-position", "generated-1948-11-supply-crate-stack", "generated-1948-12-low-stone-wall-module"
        };

        [MenuItem("Statecraft/Generated 1948/Prepare buildings and props")]
        public static void Prepare()
        {
            if (EditorBuildSettings.scenes.Any(s => s.path.EndsWith("GeneratedBuildingsPropsReview.unity", StringComparison.Ordinal)))
                throw new InvalidOperationException("Editor review must never be build-listed.");
            var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
            var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
            var serialized = new SerializedObject(review);
            string[] names = { "Original tiled house (roof gaps)", "Netanya balcony house", "Original porch house (rear opening)",
                "Tiberias stone house", "Original courtyard house (roof gaps)", "Bedouin enclosed tent", "Khaki canvas tent",
                "Stone well", "Low sandbag position", "Supply crates", "Low stone wall" };
            var prefabs = serialized.FindProperty("prefabs"); var materials = serialized.FindProperty("materials"); var labels = serialized.FindProperty("labels");
            prefabs.arraySize = materials.arraySize = labels.arraySize = Ids.Length;
            for (int i = 0; i < Ids.Length; i++)
            {
                string stem = Generated1948BuildingsPropsReview.AssetFolder + "/" + Ids[i] + "/" + Ids[i];
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(stem + ".fbx");
                var material = AssetDatabase.LoadAssetAtPath<Material>(stem + ".mat");
                var importer = AssetImporter.GetAtPath(stem + ".fbx") as ModelImporter;
                if (source == null || material == null || importer == null) throw new InvalidOperationException("Missing original review asset " + Ids[i]);
                if (importer.meshCompression != ModelImporterMeshCompression.Off || importer.animationCompression != ModelImporterAnimationCompression.Off || !importer.importAnimation)
                    throw new InvalidOperationException("Lossless mesh/animation import required: " + Ids[i]);
                var texture = material.mainTexture;
                var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (texture == null || ti == null || ti.crunchedCompression || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize < 4096)
                    throw new InvalidOperationException("Exact original full-size colour map required: " + Ids[i]);
                var go = UnityEngine.Object.Instantiate(source);
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, stem + ".prefab"); UnityEngine.Object.DestroyImmediate(go);
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                materials.GetArrayElementAtIndex(i).objectReferenceValue = material;
                labels.GetArrayElementAtIndex(i).stringValue = names[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); EditorSceneManager.CloseScene(scene, true);
            Debug.Log("BUILDINGS ORIGINALS:11 editor-only assets prepared; no campaign catalog or LOD write.");
        }

        [MenuItem("Statecraft/Generated 1948/Play buildings and props review")]
        public static void PlayReview()
        {
            EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
            EditorApplication.isPlaying = true;
        }
    }
}
