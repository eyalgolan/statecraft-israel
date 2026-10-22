#nullable enable
using System;
using System.Collections.Generic;
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
    /// <summary>Imports only the three new candidates and bakes their own far meshes. Existing art and LODs are untouched.</summary>
    public static class VehicleReviewAssets
    {
        public const string Folder = "Assets/Art/Generated1948Review";
        public const string CatalogPath = "Assets/Resources/VehicleReview/Models.asset";
        static readonly string[] Labels = { "Egyptian Spitfire candidate", "Syrian Renault R35 candidate", "Egyptian Dakota C-47 candidate" };

        [MenuItem("Statecraft/Vehicle review/Import three candidates")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!); AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<VehicleReviewCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.entries = new VehicleReviewCatalog.Entry[VehicleReviewCatalog.Ids.Length];
            for (int i = 0; i < VehicleReviewCatalog.Ids.Length; i++)
            {
                var id = VehicleReviewCatalog.Ids[i]; var dir = Folder + "/" + id; var file = dir + "/" + id + "-near.fbx";
                var importer = AssetImporter.GetAtPath(file) as ModelImporter ?? throw new FileNotFoundException("Vehicle FBX missing", file);
                importer.useFileScale = true; importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
                importer.importCameras = false; importer.importLights = false; importer.importBlendShapes = false; importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.meshCompression = ModelImporterMeshCompression.Medium;
                importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.None; importer.isReadable = false;
                importer.SaveAndReimport();
                var texturePath = dir + "/" + id + "_basecolor.jpg";
                var texture = (TextureImporter)AssetImporter.GetAtPath(texturePath); texture.mipmapEnabled = true; texture.sRGBTexture = true;
                texture.maxTextureSize = 512; texture.textureCompression = TextureImporterCompression.Compressed; texture.crunchedCompression = true; texture.SaveAndReimport();
                var material = AssetDatabase.LoadAssetAtPath<Material>(dir + "/" + id + ".mat");
                if (material == null) throw new FileNotFoundException("Candidate material missing", dir);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetFloat("_Smoothness", 0f); material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                var near = (GameObject)PrefabUtility.InstantiatePrefab(model);
                GameObject nearPrefab;
                try
                {
                    foreach (var renderer in near.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = material;
                    nearPrefab = PrefabUtility.SaveAsPrefabAsset(near, dir + "/" + id + ".prefab");
                }
                finally { Object.DestroyImmediate(near); }
                var meshes = AssetDatabase.LoadAllAssetsAtPath(file).OfType<Mesh>().ToArray();
                if (meshes.Length != 1) throw new InvalidOperationException(id + " needs one static mesh.");
                var farMesh = BakeFar(meshes[0], id);
                string meshPath = dir + "/" + id + "-far.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (old == null) AssetDatabase.CreateAsset(farMesh, meshPath);
                else { EditorUtility.CopySerialized(farMesh, old); Object.DestroyImmediate(farMesh); farMesh = old; }
                var far = new GameObject(id + " far");
                GameObject farPrefab;
                try
                {
                    far.transform.localRotation = model.transform.localRotation; far.transform.localScale = model.transform.localScale;
                    far.AddComponent<MeshFilter>().sharedMesh = farMesh; far.AddComponent<MeshRenderer>().sharedMaterial = material;
                    farPrefab = PrefabUtility.SaveAsPrefabAsset(far, dir + "/" + id + "-far.prefab");
                }
                finally { Object.DestroyImmediate(far); }
                // Presentation envelopes, not assertions of relative physical size: reuse the existing aircraft / vehicle token sizes.
                catalog.entries[i] = new VehicleReviewCatalog.Entry { id = id, label = Labels[i], near = nearPrefab, far = farPrefab,
                    longSideM = i == 1 ? UnitFigures.VehicleLengthShare * UnitBadge.FigureHeightM * UnitShowcase.FigureScale : 11.2f * UnitShowcase.AircraftScale };
                Debug.Log("VEHICLE REVIEW IMPORT " + id + ": " + meshes[0].GetIndexCount(0) / 3 + " near, " + farMesh.GetIndexCount(0) / 3 + " far triangles");
            }
            catalog.Validate(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Vehicle review runtime entry").AddComponent<VehicleReviewScene>();
            EditorSceneManager.SaveScene(scene, "Assets/VehicleReview.unity");
        }

        static Mesh BakeFar(Mesh source, string id)
        {
            var levels = Object.Instantiate(source);
            try
            {
                MeshLodUtility.GenerateMeshLods(levels, -1);
                int selected = -1, best = int.MaxValue; var positions = levels.vertices; var full = source.bounds;
                for (int level = 1; level < levels.lodCount; level++)
                {
                    int triangles = (int)levels.GetIndexCount(0, level) / 3;
                    if (triangles <= 0 || triangles > 4500 || triangles >= source.GetIndexCount(0) / 3) continue;
                    var indices = levels.GetIndices(0, level, true); var box = new Bounds(positions[indices[0]], Vector3.zero);
                    foreach (int v in indices) box.Encapsulate(positions[v]);
                    bool shape = true;
                    for (int axis = 0; axis < 3; axis++) if (Mathf.Abs(box.size[axis] - full.size[axis]) > Mathf.Max(full.size[axis] * .2f, .01f)) shape = false;
                    if (shape && 4500 - triangles < best) { selected = level; best = 4500 - triangles; }
                }
                if (selected < 0) throw new InvalidOperationException(id + ": no far LOD below 4500 triangles preserves its bounds; review reduction manually.");
                var idx = levels.GetIndices(0, selected, true); var map = new Dictionary<int, int>();
                var pos = new List<Vector3>(); var normal = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new int[idx.Length];
                var normals = levels.normals; var uvs = levels.uv;
                for (int k = 0; k < idx.Length; k++)
                {
                    int v = idx[k];
                    if (!map.TryGetValue(v, out int at)) { at = pos.Count; map[v] = at; pos.Add(positions[v]); normal.Add(normals[v]); uv.Add(uvs[v]); }
                    tri[k] = at;
                }
                var far = new Mesh { name = id + " far" }; far.SetVertices(pos); far.SetNormals(normal); far.SetUVs(0, uv); far.SetTriangles(tri, 0); far.RecalculateBounds();
                MeshUtility.SetMeshCompression(far, ModelImporterMeshCompression.Medium);
                return far;
            }
            finally { Object.DestroyImmediate(levels); }
        }
    }
}
