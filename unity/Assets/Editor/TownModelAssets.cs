using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Issue #659: makes the towns' model list (Kits.TownModels.SetPath) from MATKOT's building models, bakes each
    /// model's three lighter levels of detail into it as sub-assets, and points the map scene's MapScene at the list, so the
    /// models load with the map (owner, 16 October 2026) and never through a Resources folder. Idempotent: the baked meshes
    /// are made again from the models each run.
    /// The levels come from Unity's Mesh LOD generator (MeshLodUtility) run on a copy of each model's mesh; each level the
    /// map draws is copied out as its own small mesh (only the vertices its triangles use), compressed like the models and
    /// kept without a CPU copy. A WebGL build ignored RenderParams.forceMeshLod and drew the full mesh (issue #659).
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
            set.mid = new Mesh[n]; set.far = new Mesh[n]; set.farthest = new Mesh[n];
            for (int i = 0; i < n; i++)
            {
                string id = TownModels.Ids[i], dir = $"{TownModels.Folder}/{id}";
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath($"{dir}/{id}.fbx")) if (o is Mesh m) set.meshes[i] = m;
                set.materials[i] = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{id}.mat");
                var levels = Object.Instantiate(set.meshes[i]);
                MeshLodUtility.GenerateMeshLods(levels, -1);
                var full = set.meshes[i].bounds;
                set.mid[i] = Bake(levels, Nearest(levels, TownModelSet.MidTriangles, full), id + "_mid", set, full.min.z);
                set.far[i] = Bake(levels, Nearest(levels, TownModelSet.FarTriangles, full), id + "_far", set, full.min.z);
                set.farthest[i] = Bake(levels, Nearest(levels, 0, full), id + "_farthest", set, full.min.z);
                Object.DestroyImmediate(levels);
            }
            set.Validate();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(TownModels.SetPath, ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
            var map = Object.FindFirstObjectByType<MapScene>();
            var so = new SerializedObject(map);
            // Loaded again: opening the scene unloads assets nothing references, the new list among them.
            so.FindProperty("townModels").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TownModelSet>(TownModels.SetPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The generated level whose triangle count is nearest <paramref name="triangles"/> (level 1 or deeper; 0
        /// asks for the lightest) among those that keep the model's shape (<see cref="KeepsShape"/>), so one fitted matrix
        /// places every level on the same ground.</summary>
        static int Nearest(Mesh m, int triangles, Bounds full)
        {
            int best = -1;
            for (int l = 1; l < m.lodCount; l++)
            {
                if (!KeepsShape(LevelBounds(m, l), full)) continue;
                if (best < 0 || Mathf.Abs((int)m.GetIndexCount(0, l) / 3 - triangles) < Mathf.Abs((int)m.GetIndexCount(0, best) / 3 - triangles)) best = l;
            }
            if (best < 0) throw new System.InvalidOperationException($"{m.name}: no level of detail keeps the model's shape");
            return best;
        }

        /// <summary>A level keeps the model's shape when its width, depth and height are within a fifth of the model's (a light
        /// level may lose a roof's small top). Its base is put back on the model's (Bake): a light level often loses the
        /// thin ground slab under a Tripo model, which would leave the building floating.</summary>
        public static bool KeepsShape(Bounds level, Bounds full) =>
            Mathf.Abs(level.size.x - full.size.x) <= 0.2f && Mathf.Abs(level.size.y - full.size.y) <= 0.2f && Mathf.Abs(level.size.z - full.size.z) <= 0.2f;

        static Bounds LevelBounds(Mesh m, int level)
        {
            var pos = m.vertices; var idx = m.GetIndices(0, level, true);
            var b = new Bounds(pos[idx[0]], Vector3.zero);
            foreach (int v in idx) b.Encapsulate(pos[v]);
            return b;
        }

        /// <summary>Level <paramref name="level"/> of <paramref name="src"/> as its own mesh: its triangles and only the
        /// vertices they use (position, normal, uv0, colour where the model has them), in the model's own mesh space with its
        /// lowest point moved to <paramref name="baseZ"/> (the model's base), added to <paramref name="owner"/>'s asset,
        /// mesh-compressed (Medium, as the models) and without a CPU copy.</summary>
        static Mesh Bake(Mesh src, int level, string name, Object owner, float baseZ)
        {
            var idx = src.GetIndices(0, level, true);
            var pos = src.vertices; var nrm = src.normals; var uv = src.uv; var col = src.colors32;
            var map = new Dictionary<int, int>();
            var p = new List<Vector3>(); var nn = new List<Vector3>(); var u = new List<Vector2>(); var c = new List<Color32>();
            var tri = new int[idx.Length];
            for (int k = 0; k < idx.Length; k++)
            {
                int v = idx[k];
                if (!map.TryGetValue(v, out int at))
                {
                    at = p.Count; map[v] = at; p.Add(pos[v]);
                    if (nrm.Length > 0) nn.Add(nrm[v]);
                    if (uv.Length > 0) u.Add(uv[v]);
                    if (col.Length > 0) c.Add(col[v]);
                }
                tri[k] = at;
            }
            float lowest = float.MaxValue;
            foreach (var v in p) lowest = Mathf.Min(lowest, v.z);
            for (int k = 0; k < p.Count; k++) p[k] += new Vector3(0f, 0f, baseZ - lowest);
            var m = new Mesh { name = name, indexFormat = p.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(p);
            if (nn.Count > 0) m.SetNormals(nn);
            if (u.Count > 0) m.SetUVs(0, u);
            if (c.Count > 0) m.SetColors(c);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            MeshUtility.SetMeshCompression(m, ModelImporterMeshCompression.Medium);
            AssetDatabase.AddObjectToAsset(m, owner);
            var so = new SerializedObject(m);
            so.FindProperty("m_IsReadable").boolValue = false;   // no CPU copy in a build: nothing reads the vertices
            so.ApplyModifiedPropertiesWithoutUndo();
            return m;
        }
    }
}
