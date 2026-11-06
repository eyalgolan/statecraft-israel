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

        /// <summary>A tile's trees: <c>all</c> as planted (shuffled), <c>m</c> the ones drawn (all, less those under a unit
        /// figure: <see cref="Hide"/>), in the same order, so drawing a prefix still thins evenly; <c>crown</c> is the largest
        /// crown radius (m) among them, <c>top</c> the highest tree top (world y).</summary>
        sealed class Tile { public TreeKind kind; public Bounds bounds; public float crown, top; public Matrix4x4[] all = System.Array.Empty<Matrix4x4>(), m = System.Array.Empty<Matrix4x4>(); }

        readonly List<Tile> tiles = new List<Tile>();
        readonly SortedDictionary<string, List<(Vector2 centre, float radius)>> holes = new SortedDictionary<string, List<(Vector2, float)>>(System.StringComparer.Ordinal);
        readonly SortedDictionary<string, List<(Vector2 at, float half, float baseY, float tan)>> lanes = new SortedDictionary<string, List<(Vector2, float, float, float)>>(System.StringComparer.Ordinal);
        readonly Dictionary<TreeKind, float> topM = new Dictionary<TreeKind, float>();
        static readonly IReadOnlyList<(Vector2 at, float half, float baseY, float tan)> NoLanes = System.Array.Empty<(Vector2, float, float, float)>();
        readonly Dictionary<TreeKind, Mesh> meshes = new Dictionary<TreeKind, Mesh>();
        Material? material;
        public bool Draw { get; set; } = true;
        public int Count { get; private set; }
        /// <summary>Instances submitted for the last camera (a test and profiling aid).</summary>
        public int LastDrawn { get; private set; }
        /// <summary>Trees left out now because they stand under a unit figure or a model, or in a model's sight lane (<see cref="Hide"/>).</summary>
        public int Hidden { get; private set; }
        /// <summary>Of <see cref="Hidden"/>, the trees left out because they stand in a sight lane (#2876).</summary>
        public int SightHidden { get; private set; }

        /// <summary>A tree's top (world y): its kind's mesh top (Vegetation.BuildMesh) at the instance's height scale, over the
        /// ground it stands on.</summary>
        float TopOf(TreeKind kind, Matrix4x4 m) => m.m13 + topM[kind] * ((Vector3)m.GetColumn(1)).magnitude;

        /// <summary>A tree's crown radius (m) as placed: its kind's real crown times the instance's scale, with the planting
        /// margin (Vegetation.Plant keeps a crown this wide off the lots and routes).</summary>
        public static float CrownRadius(TreeKind kind, Matrix4x4 m) => Vegetation.CrownRadiusM(kind) * ((Vector3)m.GetColumn(0)).magnitude * 1.15f;

        public void Init(List<(TreeKind kind, Matrix4x4 m)> trees, Material treeMaterial, MapRuntimeAssets assets)
        {
            material = treeMaterial;
            foreach (TreeKind k in System.Enum.GetValues(typeof(TreeKind))) { meshes[k] = assets.Own(Vegetation.BuildMesh(k)); topM[k] = meshes[k].bounds.max.y; }
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
                float crown = 0f;
                float top = float.MinValue;
                foreach (var m in list) { crown = Mathf.Max(crown, CrownRadius(key.Item1, m)); top = Mathf.Max(top, TopOf(key.Item1, m)); }
                var arr = list.ToArray();
                tiles.Add(new Tile { kind = key.Item1, bounds = b, crown = crown, top = top, all = arr, m = arr });
            }
            Count = trees.Count;
            holes.Clear(); lanes.Clear(); Hidden = 0; SightHidden = 0;
        }

        /// <summary>Issue #249: leaves out, at draw time, every tree whose crown reaches into one of <paramref name="where"/>
        /// (centre x, z; radius m): the ground under the unit figures drawn now. Each caller (<paramref name="owner"/>: the
        /// units in play, the showcase) sets its own holes, and an empty list gives its trees back. The planting itself, and
        /// so the kits' layout, never changes; only the tiles a hole reaches are rebuilt, when a caller's holes change (a
        /// roster change), never per frame.</summary>
        public void Hide(string owner, IReadOnlyList<(Vector2 centre, float radius)> where) => Hide(owner, where, NoLanes);

        /// <summary>As the disc-only Hide, with <paramref name="sight"/>
        /// (#2876): the lanes from a model toward the map's camera, which looks north, so south of it. A lane at <c>at</c> (x, z)
        /// is <c>half</c> either side of it across and from <c>half</c> north of it south to where the ray from its south edge's
        /// foot (world y <c>baseY</c>), rising <c>tan</c> a metre toward the camera, passes over a tree's top
        /// (<see cref="TopOf"/>); a tree whose crown reaches into it would stand in front of the model, and is left out.</summary>
        public void Hide(string owner, IReadOnlyList<(Vector2 centre, float radius)> where, IReadOnlyList<(Vector2 at, float half, float baseY, float tan)> sight)
        {
            if (where.Count == 0) holes.Remove(owner); else holes[owner] = new List<(Vector2, float)>(where);
            if (sight.Count == 0) lanes.Remove(owner); else lanes[owner] = new List<(Vector2, float, float, float)>(sight);
            var all = new List<(Vector2 centre, float radius)>();
            foreach (var h in holes.Values) all.AddRange(h);
            var allLanes = new List<(Vector2 at, float half, float baseY, float tan)>();
            foreach (var l in lanes.Values) allLanes.AddRange(l);
            int hidden = 0, inLanes = 0;
            var keep = new List<Matrix4x4>();
            foreach (var t in tiles)
            {
                bool holesReach = Reaches(t, all), lanesReach = Reaches(t, allLanes);
                if (!holesReach && !lanesReach) { t.m = t.all; continue; }
                keep.Clear();
                foreach (var m in t.all)
                {
                    if (holesReach && Covered(t.kind, m, all)) hidden++;
                    else if (lanesReach && InLane(t.kind, m, allLanes)) { hidden++; inLanes++; }
                    else keep.Add(m);
                }
                t.m = keep.Count == t.all.Length ? t.all : keep.ToArray();
            }
            Hidden = hidden; SightHidden = inLanes;
        }

        static bool Reaches(Tile t, List<(Vector2 at, float half, float baseY, float tan)> sight)
        {
            foreach (var (at, half, baseY, tan) in sight)
            {
                float run = Mathf.Max(0f, t.top - baseY) / tan;
                if (t.bounds.min.x < at.x + half + t.crown && t.bounds.max.x > at.x - half - t.crown
                    && t.bounds.min.z < at.y + half + t.crown && t.bounds.max.z > at.y - half - run - t.crown) return true;
            }
            return false;
        }

        bool InLane(TreeKind kind, Matrix4x4 m, List<(Vector2 at, float half, float baseY, float tan)> sight)
        {
            var p = m.GetColumn(3); float crown = CrownRadius(kind, m), top = TopOf(kind, m);
            foreach (var (at, half, baseY, tan) in sight)
                if (Mathf.Abs(p.x - at.x) < half + crown && p.z - crown < at.y + half && (at.y - half - p.z) - crown < (top - baseY) / tan) return true;
            return false;
        }

        static bool Reaches(Tile t, List<(Vector2 centre, float radius)> where)
        {
            foreach (var (c, r) in where)
            {
                float reach = r + t.crown;
                float dx = Mathf.Max(t.bounds.min.x - c.x, 0f, c.x - t.bounds.max.x), dz = Mathf.Max(t.bounds.min.z - c.y, 0f, c.y - t.bounds.max.z);
                if (dx * dx + dz * dz < reach * reach) return true;
            }
            return false;
        }

        static bool Covered(TreeKind kind, Matrix4x4 m, List<(Vector2 centre, float radius)> where)
        {
            var p = m.GetColumn(3); float crown = CrownRadius(kind, m);
            foreach (var (c, r) in where)
            {
                float dx = p.x - c.x, dz = p.z - c.y, reach = r + crown;
                if (dx * dx + dz * dz < reach * reach) return true;
            }
            return false;
        }

        /// <summary>Every tree as planted, hidden or not (kind, world matrix): a test aid.</summary>
        public IEnumerable<(TreeKind kind, Matrix4x4 m)> Planted()
        {
            foreach (var t in tiles) foreach (var m in t.all) yield return (t.kind, m);
        }

        /// <summary>The trees a camera near enough to draw every tree would submit now (kind, world matrix): a test aid.</summary>
        public IEnumerable<(TreeKind kind, Matrix4x4 m)> Drawable()
        {
            foreach (var t in tiles) foreach (var m in t.m) yield return (t.kind, m);
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
