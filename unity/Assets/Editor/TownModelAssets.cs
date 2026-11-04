using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Keeps an Editor/test model list referencing Tal Heuberger's unmodified imported meshes near (no generated level
    /// of detail there, #1273) and, for the far tier, the far-off copies FarModelLevels made beside them (#2378). The map
    /// scene has no reference to it: the town buildings stream after the first frame (TownModelLayer). Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.TownModelAssets.Create</summary>
    public static class TownModelAssets
    {
        public static void Create()
        {
            var set = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<TownModelSet>(); AssetDatabase.CreateAsset(set, TownModels.SetPath); }
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(TownModels.SetPath)) Object.DestroyImmediate(o, true);
            int n = TownModels.Ids.Length;
            set.meshes = new Mesh[n]; set.materials = new Material[n];
            set.mid = new Mesh[n]; set.far = new Mesh[n]; set.farMaterials = new Material[n];
            for (int i = 0; i < n; i++)
            {
                string id = TownModels.Ids[i], dir = $"{TownModels.Folder}/{id}";
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath($"{dir}/{id}.fbx")) if (o is Mesh m) set.meshes[i] = m;
                set.materials[i] = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{id}.mat");
                // Near, the unmodified imported mesh only (#1273); far, the derived copy and its own material (#2378).
                set.mid[i] = set.meshes[i];
                set.far[i] = AssetDatabase.LoadAssetAtPath<Mesh>(TownModels.FarStem(id) + ".asset");
                set.farMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(TownModels.FarStem(id) + ".mat");
            }
            set.Validate();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TownModels.SetPath, ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<MapScene>();
            var so = new SerializedObject(map);
            // Loaded again: opening the scene unloads assets nothing references, the new list among them.
            so.FindProperty("townModels").objectReferenceValue = null; // originals stream after the first visible frame
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
