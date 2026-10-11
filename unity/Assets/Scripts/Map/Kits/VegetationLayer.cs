#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Draws the trees with GPU instancing (Graphics.RenderMeshInstanced, WebGL 2 instanced arrays): one batch list
    /// per tree kind and 30 km tile, each with its own bounds so off-screen tiles are culled. A tile's instances are shuffled
    /// once (seeded), so drawing a prefix thins it evenly: far tiles (the country-wide zoom) draw a share that falls with
    /// distance, where a tree is a pixel or less. Submitted for every camera from RenderPipelineManager.beginCameraRendering,
    /// so editor renders (MapCapture, the render-sample tests) see the trees as the player does.</summary>
    [ExecuteAlways]
    public sealed class VegetationLayer : MonoBehaviour
    {
        public const float TileM = 30000f;
        /// <summary>Tiles nearer than this draw every tree; beyond it the share falls as (ThinStartM / distance)^2.</summary>
        public const float ThinStartM = 110000f;

        sealed class Tile { public TreeKind kind; public Bounds bounds; public Matrix4x4[] m = System.Array.Empty<Matrix4x4>(); }

        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<TreeKind, Mesh> meshes = new Dictionary<TreeKind, Mesh>();
        Material? material;
        public bool Draw { get; set; } = true;
        public int Count { get; private set; }
        /// <summary>Instances submitted for the last camera (a test and profiling aid).</summary>
        public int LastDrawn { get; private set; }

        public void Init(List<(TreeKind kind, Matrix4x4 m)> trees, Material treeMaterial, MapRuntimeAssets assets)
        {
            material = treeMaterial;
            foreach (TreeKind k in System.Enum.GetValues(typeof(TreeKind))) meshes[k] = assets.Own(Vegetation.BuildMesh(k));
            var byTile = new Dictionary<(TreeKind, int, int), List<Matrix4x4>>();
            foreach (var (kind, m) in trees)
            {
                var p = m.GetColumn(3);
                var key = (kind, Mathf.FloorToInt(p.x / TileM), Mathf.FloorToInt(p.z / TileM));
                if (!byTile.TryGetValue(key, out var list)) byTile[key] = list = new List<Matrix4x4>();
                list.Add(m);
            }
            var keys = new List<(TreeKind, int, int)>(byTile.Keys);
            keys.Sort();
            tiles.Clear();
            foreach (var key in keys)
            {
                var list = byTile[key];
                var rnd = new KitRandom($"tile:{key.Item1}:{key.Item2}:{key.Item3}", 19);
                for (int i = list.Count - 1; i > 0; i--) { int j = rnd.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
                var b = new Bounds((Vector3)list[0].GetColumn(3), Vector3.zero);
                foreach (var m in list) b.Encapsulate((Vector3)m.GetColumn(3));
                b.Expand(new Vector3(400f, 800f, 400f) * (KitScale.Trees / KitScale.CountReference));   // the crowns round the trunk points
                tiles.Add(new Tile { kind = key.Item1, bounds = b, m = list.ToArray() });
            }
            Count = trees.Count;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Submit;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Submit;

        void Submit(ScriptableRenderContext context, Camera cam)
        {
            if (!Draw || material == null || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var eye = cam.transform.position;
            int drawn = 0;
            foreach (var t in tiles)
            {
                // The shadow pass sees casters outside the view too, so cull on a widened box (the sun is 30 degrees up: a
                // crown's shadow reaches about twice its height).
                var wide = t.bounds; wide.Expand(new Vector3(3000f, 0f, 3000f));
                if (!GeometryUtility.TestPlanesAABB(planes, wide)) continue;
                float d = Mathf.Sqrt(t.bounds.SqrDistance(eye));
                float share = d <= ThinStartM ? 1f : Mathf.Clamp01((ThinStartM / d) * (ThinStartM / d));
                int n = Mathf.Min(t.m.Length, Mathf.CeilToInt(t.m.Length * share));
                if (n <= 0) continue;
                var rp = new RenderParams(material)
                {
                    worldBounds = t.bounds, camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true,
                    layer = gameObject.layer,
                };
                Graphics.RenderMeshInstanced(rp, meshes[t.kind], 0, t.m, n);
                drawn += n;
            }
            LastDrawn = drawn;
        }
    }
}
