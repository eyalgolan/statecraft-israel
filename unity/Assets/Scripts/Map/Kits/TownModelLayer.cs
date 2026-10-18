#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Issue #659, owner ruling on #700: "Models up close only". Up close the towns' buildings are MATKOT's building
    /// models, drawn with GPU instancing (Graphics.RenderMeshInstanced: one batch list per model and <see cref="TileM"/> tile,
    /// culled per tile, as <see cref="VegetationLayer"/> draws the trees); far off they are the kits' own buildings, as on
    /// main. Each camera picks by its view distance (the camera rig's zoom: <see cref="ViewDistance"/>) with hysteresis:
    /// models from below <see cref="CloseEnterM"/>, kits again from above <see cref="CloseExitM"/>. With the models the
    /// layer swaps every kit mesh (its buildings) for its close mesh (its landmarks, palms and the models' plinths) through
    /// Renderer.forceRenderingOff, so the "kits" toggle (Renderer.enabled) still hides both. Each tile draws one of its
    /// model's levels of detail (TownModelSet: the full mesh and three lighter meshes baked by the editor), by how large its
    /// buildings look. Submitted for every camera from RenderPipelineManager.beginCameraRendering, before that camera culls,
    /// so editor renders (MapCapture, the render tests) see the towns as the player does.</summary>
    [ExecuteAlways]
    public sealed class TownModelLayer : MonoBehaviour
    {
        public const float TileM = VegetationLayer.TileM;
        /// <summary>The view distances (m) of the switch. The models come in below CloseEnterM and the kits come back above
        /// CloseExitM. The band holds the geometric middle of tel_aviv30 (30 km, a close view: models) and gameplay03 (60 km,
        /// the gameplay view: kits), sqrt(30 x 60) = 42.4 km, and is wider than one wheel notch (CameraController.ZoomPerStep,
        /// 10% of the distance), so a notch out and back never flips the towns twice. diorama02 (250 km) is far beyond it, the
        /// close checks (hero06, 6 km: CameraController.MinDistance) well inside.</summary>
        public const float CloseEnterM = 40000f, CloseExitM = 45000f;
        /// <summary>Level 0 of detail is a building this many pixels across or more; each halving of the size is one level
        /// more (LodFor; TownModelSet.Mesh picks the mesh; the full mesh from about 256 px): at tel_aviv30 a Tel Aviv block
        /// (about 56 px) and at jaffa12 a village house (about 77 px) draw the mid mesh (about 800 triangles), at the 6 km
        /// closest zoom a block (about 280 px) the full mesh. At 256 jaffa12 drew 2.4 million triangles of full meshes.</summary>
        public const float FullDetailPx = 512f;

        sealed class Tile { public TownModel model; public Bounds bounds; public float size; public Matrix4x4[] m = System.Array.Empty<Matrix4x4>(); }

        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<string, List<SettlementKits.ModelInstance>> perPlace = new Dictionary<string, List<SettlementKits.ModelInstance>>();
        readonly List<(Renderer kit, Renderer? close)> swaps = new List<(Renderer, Renderer?)>();
        /// <summary>Each camera's last choice (by instance id), for the hysteresis.</summary>
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        /// <summary>The towns whose kit buildings never draw (<see cref="Suppress"/>), by settlement id.</summary>
        readonly HashSet<string> suppressed = new HashSet<string>(System.StringComparer.Ordinal);
        TownModelSet? set;
        public bool Draw { get; set; } = true;
        /// <summary>Whether the layer has a complete model list (else the towns keep their kits at every zoom).</summary>
        public bool HasModels => set != null;
        public int Count { get; private set; }
        /// <summary>For the last camera: whether it drew the models (else the kits), and the instances, triangles, draws and
        /// shadow casters it submitted (test and profiling aids).</summary>
        public bool LastClose { get; private set; }
        public int LastDrawn { get; private set; }
        public long LastTriangles { get; private set; }
        public int LastDraws { get; private set; }

        /// <summary><paramref name="swaps"/>: each kit renderer (a town's or a tile's buildings) and its close renderer (null
        /// where nothing is left without the buildings). Without a complete <paramref name="models"/> list (none, or one
        /// lacking a model or a level: TownModelSet.Missing) the layer logs an error and never swaps: every town keeps its kit
        /// buildings at every zoom, so a missing model never leaves a plot empty (the fail-open check on #700).</summary>
        public void Init(IReadOnlyList<SettlementKits.ModelInstance> instances, TownModelSet? models, IReadOnlyList<(Renderer kit, Renderer? close)> swaps)
        {
            string? why = models == null ? "the map scene has no model list" : models.Missing();
            if (why != null) Debug.LogError($"town models: {why}; the towns keep their kits at every zoom (issue #659)");
            set = why == null ? models : null;
            this.swaps.Clear(); this.swaps.AddRange(swaps);
            closeFor.Clear();
            var byTile = new Dictionary<(TownModel, int, int), List<SettlementKits.ModelInstance>>();
            perPlace.Clear();
            foreach (var mi in instances)
            {
                var key = (mi.Model, Mathf.FloorToInt(mi.C.x / TileM), Mathf.FloorToInt(mi.C.y / TileM));
                if (!byTile.TryGetValue(key, out var list)) byTile[key] = list = new List<SettlementKits.ModelInstance>();
                list.Add(mi);
                if (!perPlace.TryGetValue(mi.Id, out var mine)) perPlace[mi.Id] = mine = new List<SettlementKits.ModelInstance>();
                mine.Add(mi);
            }
            var keys = new List<(TownModel, int, int)>(byTile.Keys);
            keys.Sort();
            tiles.Clear();
            foreach (var key in keys)
            {
                var list = byTile[key];
                var corners = new List<Vector3>();
                float size = 0f;
                var arr = new Matrix4x4[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Corners(corners);
                    arr[i] = list[i].M;
                    size += Mathf.Max(list[i].W, list[i].D, list[i].H);
                }
                var b = new Bounds(corners[0], Vector3.zero);
                foreach (var c in corners) b.Encapsulate(c);
                tiles.Add(new Tile { model = key.Item1, bounds = b, size = size / list.Count, m = arr });
            }
            Count = instances.Count;
            Show(false);
        }

        /// <summary>Owner ruling of 18 October 2026 ("Whole model far, buildings near"; no procedural houses in a town with a
        /// whole-town model, at any zoom): the towns <paramref name="ids"/> (the settlement ids TownLandmarks stood a
        /// whole-town model on) show their close mesh at every zoom, their kit mesh at none. Far off their whole-town model
        /// stands on them, up close these models do. Replaces the last list; an empty list gives every town #700's swap back.
        /// It holds without a complete model list too (the whole-town model then draws at every zoom: TownLandmarks).</summary>
        public void Suppress(IEnumerable<string> ids)
        {
            suppressed.Clear();
            suppressed.UnionWith(ids);
            Show(set != null && LastClose);
        }

        /// <summary>Whether town <paramref name="id"/>'s kit buildings never draw (a test aid).</summary>
        public bool Suppressed(string id) => suppressed.Contains(id);

        /// <summary>The models placed for a town or locality (a test aid).</summary>
        public int CountFor(string id) => perPlace.TryGetValue(id, out var l) ? l.Count : 0;

        /// <summary>The models placed for a town or locality, as placed (a test aid).</summary>
        public IReadOnlyList<SettlementKits.ModelInstance> InstancesOf(string id) =>
            perPlace.TryGetValue(id, out var l) ? l : (IReadOnlyList<SettlementKits.ModelInstance>)System.Array.Empty<SettlementKits.ModelInstance>();

        /// <summary>The camera's view distance (m): along its view ray to the map's ground plane (y = 0, where the camera rig's
        /// look-at stands), so it is the rig's zoom (CameraRig.Distance). A camera looking up or level sees no ground: far.</summary>
        public static float ViewDistance(Camera cam)
        {
            var t = cam.transform;
            return t.forward.y < -1e-4f ? t.position.y / -t.forward.y : float.PositiveInfinity;
        }

        /// <summary>Whether a camera at <paramref name="distanceM"/> draws the models, given its last choice
        /// (<paramref name="wasClose"/>, null for none).</summary>
        public static bool CloseAt(float distanceM, bool? wasClose) =>
            wasClose == true ? distanceM <= CloseExitM : distanceM < CloseEnterM;

        /// <summary>The level of detail for a building <paramref name="sizeM"/> across seen from <paramref name="distanceM"/>
        /// with a vertical field of view <paramref name="fovDeg"/> on a screen <paramref name="screenPx"/> tall: level 0 at
        /// <see cref="FullDetailPx"/> or more, one level more per halving, up to <paramref name="levels"/> - 1.</summary>
        public static int LodFor(float sizeM, float distanceM, float fovDeg, float screenPx, int levels)
        {
            if (levels <= 1) return 0;
            float px = sizeM / (Mathf.Max(distanceM, 1f) * 2f * Mathf.Tan(0.5f * fovDeg * Mathf.Deg2Rad)) * screenPx;
            if (px >= FullDetailPx) return 0;
            int lod = Mathf.FloorToInt(Mathf.Log(FullDetailPx / Mathf.Max(px, 1e-3f), 2f));
            return Mathf.Clamp(lod, 0, levels - 1);
        }

        /// <summary>Every instance (model, world matrix): a test aid.</summary>
        public IEnumerable<(TownModel model, Matrix4x4 m)> Instances()
        {
            foreach (var t in tiles) foreach (var m in t.m) yield return (t.model, m);
        }

        /// <summary>Kit buildings (false) or close meshes (true) for the camera about to render; a suppressed town's close mesh
        /// at either.</summary>
        void Show(bool close)
        {
            foreach (var (kit, closeR) in swaps)
            {
                bool c = close || (kit != null && suppressed.Contains(kit.name));
                if (kit != null) kit.forceRenderingOff = c;
                if (closeR != null) closeR.forceRenderingOff = !c;
            }
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Submit;
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= Submit; Show(false); }

        void Submit(ScriptableRenderContext context, Camera cam)
        {
            if (set == null || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool close = CloseAt(ViewDistance(cam), closeFor.TryGetValue(id, out bool was) ? was : (bool?)null);
            closeFor[id] = close;
            Show(close);
            LastClose = close;
            int drawn = 0, draws = 0; long tris = 0;
            if (close && Draw)
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                var eye = cam.transform.position;
                foreach (var t in tiles)
                {
                    // The shadow pass sees casters outside the view too: cull on a widened box (as the trees).
                    var wide = t.bounds; wide.Expand(new Vector3(3000f, 0f, 3000f));
                    if (!GeometryUtility.TestPlanesAABB(planes, wide)) continue;
                    int level = LodFor(t.size, Mathf.Sqrt(t.bounds.SqrDistance(eye)), cam.fieldOfView, cam.pixelHeight, TownModelSet.Levels + 1);
                    var mesh = set.Mesh(t.model, level);
                    var rp = new RenderParams(set.Material(t.model))
                    {
                        worldBounds = t.bounds, camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, mesh, 0, t.m);
                    draws++; drawn += t.m.Length; tris += (long)t.m.Length * mesh.GetIndexCount(0) / 3;
                }
            }
            LastDrawn = drawn; LastTriangles = tris; LastDraws = draws;
        }
    }
}
