using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Statecraft.Game.Map.Kits;
using Statecraft.Game.Review;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Category-only preparation. Existing baked town meshes and GUIDs are retained.</summary>
    public static class Generated1948BuildingsPropsAssets
    {
        [MenuItem("Statecraft/Generated 1948/Prepare buildings and props")]
        public static void Prepare()
        {
            string path = TownModels.SetPath;
            string before = File.ReadAllText(path);
            var oldMeshes = Regex.Matches(before, @"(?ms)^--- !u!43 &[^\n]+\n.*?(?=^--- |\z)")
                .Cast<Match>().Select(m => m.Value).ToArray();
            TownModelAssets.AppendMissing();
            string after = File.ReadAllText(path);
            // Saving a Mesh can round-trip compressed normal-range floats. Keep every pre-existing serialized
            // mesh block byte-for-byte, including those floats; only the new groups and catalog refs may be added.
            var originals = oldMeshes.ToDictionary(m => Regex.Match(m, @"^--- !u!43 &([^\n]+)").Groups[1].Value);
            after = Regex.Replace(after, @"(?ms)^--- !u!43 &([^\n]+)\n.*?(?=^--- |\z)",
                m => originals.TryGetValue(m.Groups[1].Value, out var original) ? original : m.Value);
            File.WriteAllText(path, after);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            foreach (string mesh in oldMeshes)
                if (!File.ReadAllText(path).Contains(mesh)) throw new InvalidOperationException("An existing baked town mesh changed; stop and review the asset diff.");
            if (!EditorBuildSettings.scenes.Any(s => s.path == Generated1948BuildingsPropsReview.ScenePath && s.enabled))
                throw new InvalidOperationException("The playable buildings/props scene must be enabled in build settings.");

            var scene = EditorSceneManager.OpenScene(Generated1948BuildingsPropsReview.ScenePath, OpenSceneMode.Additive);
            var review = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Generated1948BuildingsPropsReview>()).Single();
            var serialized = new SerializedObject(review);
            string[] ids = { "generated-1948-01-textured-hip-roof-repair-v2", "generated-1948-02-netanya-two-storey-balcony-house", "generated-1948-03-netanya-flat-roof-porch-house", "generated-1948-04-dark-basalt-house", "generated-1948-05-textured-courtyard-roof-repair-v2", "generated-1948-06-bedouin-b-enclosed-striped-tent", "generated-1948-08-khaki-canvas-tent", "generated-1948-09-stone-village-well", "generated-1948-10-low-sandbag-position", "generated-1948-11-supply-crate-stack", "generated-1948-12-low-stone-wall-module" };
            string[] names = { "Repaired tiled-roof house", "Netanya balcony house", "Netanya porch house", "Tiberias stone house", "Repaired courtyard house", "Bedouin enclosed tent", "Khaki canvas tent", "Stone well", "Low sandbag position", "Supply crates", "Low stone wall" };
            var prefabs = serialized.FindProperty("prefabs"); var materials = serialized.FindProperty("materials"); var labels = serialized.FindProperty("labels");
            prefabs.arraySize = materials.arraySize = labels.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                string stem = "Assets/Art/Generated1948Review/" + ids[i] + "/" + ids[i];
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(stem + "-near.fbx");
                var material = AssetDatabase.LoadAssetAtPath<Material>(stem + ".mat");
                if (source == null || material == null) throw new InvalidOperationException("Missing review asset " + ids[i]);
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = source;
                materials.GetArrayElementAtIndex(i).objectReferenceValue = material;
                labels.GetArrayElementAtIndex(i).stringValue = names[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); EditorSceneManager.CloseScene(scene, true);
            UnityEngine.Debug.Log("Buildings/props prepared: existing " + oldMeshes.Length + " baked mesh blocks retained unchanged.");
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
