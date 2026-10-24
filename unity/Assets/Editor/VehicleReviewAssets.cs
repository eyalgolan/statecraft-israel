#nullable enable
using System;
using System.IO;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Game.VehicleReview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Lossless import of three documented GLB compatibility exports, exclusively for editor review.</summary>
    public static class VehicleReviewAssets
    {
        public const string Folder = VehicleReviewCatalog.Folder;
        public const string CatalogPath = VehicleReviewCatalog.Path;
        static readonly string[] Labels = { "Egyptian Spitfire candidate", "Syrian Renault R35 candidate", "Egyptian Dakota C-47 candidate" };

        [MenuItem("Statecraft/Vehicle review/Import three original candidates")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<VehicleReviewCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.entries = new VehicleReviewCatalog.Entry[VehicleReviewCatalog.Ids.Length];
            for (int i = 0; i < VehicleReviewCatalog.Ids.Length; i++)
            {
                var id = VehicleReviewCatalog.Ids[i]; var dir = Folder + "/" + id; var file = dir + "/" + id + "-compatibility.fbx";
                var importer = AssetImporter.GetAtPath(file) as ModelImporter ?? throw new FileNotFoundException("Documented compatibility FBX missing", file);
                importer.useFileScale = true; importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Generic;
                importer.animationCompression = ModelImporterAnimationCompression.Off; importer.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
                importer.importCameras = false; importer.importLights = false; importer.importBlendShapes = true; importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import; importer.isReadable = false;
                importer.optimizeMeshPolygons = false; importer.optimizeMeshVertices = false; importer.assetBundleName = "";
                importer.SaveAndReimport();
                var texturePath = dir + "/" + id + "_basecolor.jpg";
                var texture = (TextureImporter)AssetImporter.GetAtPath(texturePath); texture.mipmapEnabled = true; texture.sRGBTexture = true;
                texture.GetSourceTextureWidthAndHeight(out int width, out int height);
                texture.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(width, height));
                texture.textureCompression = TextureImporterCompression.Uncompressed; texture.crunchedCompression = false;
                foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" }) texture.ClearPlatformTextureSettings(platform);
                texture.assetBundleName = ""; texture.SaveAndReimport();
                var material = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + id + ".mat");
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, dir + "/" + id + ".mat"); }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_Cull", (float)CullMode.Off); material.enableInstancing = true; EditorUtility.SetDirty(material);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                GameObject prefab;
                try
                {
                    foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = material;
                    prefab = PrefabUtility.SaveAsPrefabAsset(instance, dir + "/" + id + ".prefab");
                }
                finally { Object.DestroyImmediate(instance); }
                // Existing token presentation envelopes; no assertion of relative physical size.
                catalog.entries[i] = new VehicleReviewCatalog.Entry { id = id, label = Labels[i], model = prefab,
                    longSideM = i == 1 ? UnitFigures.VehicleLengthShare * UnitBadge.FigureHeightM * UnitShowcase.FigureScale : 11.2f * UnitShowcase.AircraftScale };
                Debug.Log("VEHICLE ORIGINAL IMPORT " + id + ": " + AssetDatabase.LoadAllAssetsAtPath(file).OfType<Mesh>().Sum(m => (int)m.GetIndexCount(0) / 3) + " triangles; original " + width + "x" + height + " map; no LOD");
            }
            catalog.Validate(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Editor-only vehicle review entry").AddComponent<VehicleReviewScene>();
            EditorSceneManager.SaveScene(scene, VehicleReviewCatalog.ScenePath);
        }
    }
}
