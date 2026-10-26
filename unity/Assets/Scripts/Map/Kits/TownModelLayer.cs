#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Original building meshes for admitted camera-focus towns, instanced per model/tile. At overview zoom
    /// TownLandmarks owns the admitted whole-town representation; close cameras own individual buildings. Each camera
    /// swaps only complete towns; loading, failed and outside-focus towns retain code-built fallback. Source artwork is
    /// unchanged. Edge admission is off until a diagnostic-backed allowance is explicitly supplied.</summary>
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
        /// <summary>Compatibility input for LodFor's projection helper; original shipping meshes have no generated LODs.</summary>
        public const float FullDetailPx = 512f;

        sealed class Tile { public string town = ""; public TownModel model; public Bounds bounds; public float size; public Matrix4x4[] m = System.Array.Empty<Matrix4x4>(); }

        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<string, List<SettlementKits.ModelInstance>> perPlace = new Dictionary<string, List<SettlementKits.ModelInstance>>();
        readonly List<(Renderer kit, Renderer? close)> swaps = new List<(Renderer, Renderer?)>();
        readonly Dictionary<Renderer, string> swapIds = new Dictionary<Renderer, string>();
        /// <summary>The towns whose kit buildings never draw (<see cref="Suppress"/>), by settlement id.</summary>
        readonly HashSet<string> suppressed = new HashSet<string>(System.StringComparer.Ordinal);
        TownModelSet? set;
        bool streaming;
        // KitLayer installs the approved production admission policy before the first request.
        // Null is deliberately fail-closed: no edge originals until the diagnostic establishes an allowance.
        internal System.Func<IReadOnlyList<string>, string, bool>? EdgeAllowance = null;
        internal bool SpatialPolicyConfigured { get; private set; }
        internal string SpatialPolicyEvidence { get; private set; } = "";
        float admittedEnterM, admittedExitM, admittedEdgeM;
        int admittedEdges;
        internal void ConfigureResidency(float enterM, float exitM, float edgeM, int maxEdges, string diagnosticEvidence)
        {
            if (!float.IsFinite(enterM) || !float.IsFinite(exitM) || !float.IsFinite(edgeM) || enterM < 0 || exitM < enterM || edgeM < 0 || maxEdges < 0 || string.IsNullOrWhiteSpace(diagnosticEvidence))
                throw new System.ArgumentException("Residency requires finite accepted spatial values and diagnostic evidence.");
            admittedEnterM = enterM; admittedExitM = exitM; admittedEdgeM = edgeM; admittedEdges = maxEdges;
            SpatialPolicyEvidence = diagnosticEvidence; SpatialPolicyConfigured = true;
            foreach (var choice in choices.Values) { choice.hadFocus = false; choice.selected = System.Array.Empty<string>(); choice.towns.Clear(); choice.primary = null; }
            ReconcileModels();
        }
        internal System.Func<string, Camera, bool>? WholeTownReady;
        readonly List<(string id, Bounds bounds)> buildingPlaces = new List<(string, Bounds)>();
        internal static System.Func<string, Collection.CollectionDetail, System.Action<GameObject?>, System.IDisposable> ModelAcquire = Collection.CollectionStream.Acquire;
        sealed class Choice
        {
            public Camera camera = null!;
            public int lastFrame;
            public bool close;
            public string? primary;
            public Vector3 focus;
            public bool hadFocus;
            public string[] selected = System.Array.Empty<string>();
            public readonly HashSet<string> towns = new HashSet<string>(System.StringComparer.Ordinal);
        }
        sealed class Model
        {
            public bool answered;
            public Mesh? mesh;
            public Material? material;
            public System.IDisposable? lease;
        }
        readonly List<(string id, Bounds bounds)> places = new List<(string, Bounds)>();
        readonly Dictionary<int, Choice> choices = new Dictionary<int, Choice>();
        readonly Dictionary<TownModel, Model> models = new Dictionary<TownModel, Model>();
        readonly Dictionary<string, TownModel[]> required = new Dictionary<string, TownModel[]>(System.StringComparer.Ordinal);
        readonly List<int> deadCameras = new List<int>();
        readonly List<System.IDisposable> retiring = new List<System.IDisposable>();
        readonly List<(Renderer renderer, bool off)> previousRenderers = new List<(Renderer, bool)>();
        bool reconcileDirty;
        bool retirementScheduled;
        int pendingRetirementLeases, lifetimeGeneration;
        readonly List<(string id, Collection.CollectionDetail detail)> retiringKeys = new List<(string, Collection.CollectionDetail)>();
        internal int PendingRetirementLeases => retiring.Count + pendingRetirementLeases;
        int cameraGraceFrames = Collection.ProductionResidency.Approved().Towns.Grace;
        float? productionViewEnter, productionViewExit;
        internal void ConfigureProduction(Collection.ProductionResidency.TownPolicy policy)
        {
            if (!policy.Enabled)
            {
                SpatialPolicyConfigured = false; SpatialPolicyEvidence = "disabled";
                choices.Clear(); RestoreRenderers(); ReleaseModels(); return;
            }
            cameraGraceFrames = policy.Grace;
            productionViewEnter = policy.ViewEnter; productionViewExit = policy.ViewExit;
            ConfigureResidency(policy.Enter, policy.Exit, policy.Edge, policy.MaxEdges, policy.Evidence);
        }
        public bool UsesFocusedOriginals => places.Count > 0;

        internal Collection.CollectionResidencyMetrics.Category ResidencySnapshot()
        {
            var active = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var c in choices.Values) foreach (var town in c.towns) active.Add(town);
            var ids = new List<string>(); int owners = 0, pending = 0, refs = 0, instances = 0, missing = 0;
            foreach (var pair in models)
            {
                ids.Add(TownModels.Ids[(int)pair.Key]); if (pair.Value.lease != null) owners++;
                if (!pair.Value.answered) pending++; else if (pair.Value.mesh == null || pair.Value.material == null) missing++;
                if (pair.Value.mesh != null) refs++; if (pair.Value.material != null) refs++;
            }
            foreach (var tile in tiles)
                foreach (var c in choices.Values) if (ReadyForTown(tile.town, c.camera)) { instances += tile.m.Length; break; }
            var row = Collection.CollectionResidencyMetrics.Row("town_buildings", active.Count, instances, ids, owners, pending, 0, 0, 0, missing,
                System.Linq.Enumerable.Select(ids, id => id + "|Near"));
            row.strongCacheRefs += refs; row.retiringLeases = retiring.Count; return row;
        }

        static float GroundDistance(Bounds bounds, Vector3 point)
        {
            point.y = bounds.center.y;
            return Mathf.Sqrt(bounds.SqrDistance(point));
        }

        /// <summary>C89: one town near camera focus and at most two immediate edge neighbours; no map-wide Near load.</summary>
        internal static string[] FocusedTowns(IReadOnlyList<(string id, Bounds bounds)> towns, Vector3 focus, string? previous, System.Func<IReadOnlyList<string>, string, bool>? edgeAllowance, float enterM, float exitM, float edgeM, int maxEdges)
        {
            int primary = -1; float best = float.PositiveInfinity, bestCentre = float.PositiveInfinity;
            for (int i = 0; i < towns.Count; i++)
            {
                float distance = GroundDistance(towns[i].bounds, focus);
                if (towns[i].id == previous && distance <= exitM) { primary = i; break; }
                var centreDelta = towns[i].bounds.center - focus; centreDelta.y = 0;
                float centre = centreDelta.sqrMagnitude;
                if (distance <= enterM && (distance < best || (distance == best &&
                    (centre < bestCentre || (centre == bestCentre && (primary < 0 || string.CompareOrdinal(towns[i].id, towns[primary].id) < 0))))))
                { primary = i; best = distance; bestCentre = centre; }
            }
            if (primary < 0) return System.Array.Empty<string>();
            var edge = new List<(string id, float distance)>();
            var box = towns[primary].bounds; box.Expand(new Vector3(edgeM * 2, 0, edgeM * 2));
            for (int i = 0; i < towns.Count; i++)
            {
                if (i == primary) continue;
                var b = towns[i].bounds; b.center = new Vector3(b.center.x, box.center.y, b.center.z);
                if (box.Intersects(b)) edge.Add((towns[i].id, GroundDistance(b, focus)));
            }
            edge.Sort((a, b) => { int c = a.distance.CompareTo(b.distance); return c != 0 ? c : string.CompareOrdinal(a.id, b.id); });
            var result = new List<string> { towns[primary].id };
            for (int i = 0; i < edge.Count && result.Count <= maxEdges; i++)
                if (edgeAllowance != null && edgeAllowance(result, edge[i].id)) result.Add(edge[i].id);
            return result.ToArray();
        }
        public bool Draw { get; set; } = true;
        /// <summary>Whether the layer has a complete model list (else the towns keep their kits at every zoom).</summary>
        public bool HasModels
        {
            get
            {
                if (set != null) return true;
                // This legacy property means the entire catalog, never one admitted town or one arriving prefab.
                foreach (TownModel kind in System.Enum.GetValues(typeof(TownModel)))
                    if (!models.TryGetValue(kind, out var model) || model.mesh == null || model.material == null) return false;
                return true;
            }
        }
        public int Count { get; private set; }
        /// <summary>For the last camera: whether it drew the models (else the kits), and the instances, triangles, draws and
        /// shadow casters it submitted (test and profiling aids).</summary>
        public bool LastClose { get; private set; }
        public int LastDrawn { get; private set; }
        public long LastTriangles { get; private set; }
        public int LastDraws { get; private set; }

        /// <summary>Explicit town ids and renderer identities distinguish individual towns from mixed locality tiles.
        /// A complete editor model set is optional in a player: acquired originals become ready independently per town,
        /// while incomplete/missing replies keep fallback. No tile or unrelated town swaps on a partial global set.</summary>
        public void Init(IReadOnlyList<SettlementKits.ModelInstance> instances, TownModelSet? models, IReadOnlyList<(Renderer kit, Renderer? close)> swaps, IReadOnlyCollection<string>? townIds = null, IReadOnlyDictionary<Renderer, string>? rendererTownIds = null)
        {
            RestoreRenderers(); ReleaseModels(); choices.Clear(); places.Clear(); buildingPlaces.Clear(); required.Clear();
            string? why = models == null ? "the map scene has no model list" : models.Missing();
            streaming = models == null && Application.isPlaying;
            if (why != null && !streaming && !suppressMissingLog) Debug.LogError($"town models: {why}; the towns keep their kits at every zoom (issue #659)");
            set = why == null ? models : null;
            this.swaps.Clear(); this.swaps.AddRange(swaps); swapIds.Clear();
            foreach (var pair in swaps)
            {
                if (pair.kit == null) continue;
                if (rendererTownIds != null) { if (rendererTownIds.TryGetValue(pair.kit, out var id)) swapIds[pair.kit] = id; }
                else swapIds[pair.kit] = pair.kit.name; // compatibility for explicit test/editor renderer fixtures
            }
            var byTile = new Dictionary<(string, TownModel, int, int), List<SettlementKits.ModelInstance>>();
            perPlace.Clear();
            foreach (var mi in instances)
            {
                var key = (mi.Id, mi.Model, Mathf.FloorToInt(mi.C.x / TileM), Mathf.FloorToInt(mi.C.y / TileM));
                if (!byTile.TryGetValue(key, out var list)) byTile[key] = list = new List<SettlementKits.ModelInstance>();
                list.Add(mi);
                if (!perPlace.TryGetValue(mi.Id, out var mine)) perPlace[mi.Id] = mine = new List<SettlementKits.ModelInstance>();
                mine.Add(mi);
            }
            var keys = new List<(string, TownModel, int, int)>(byTile.Keys);
            keys.Sort((a, b) => { int c = string.CompareOrdinal(a.Item1, b.Item1); if (c != 0) return c;
                c = a.Item2.CompareTo(b.Item2); if (c != 0) return c; c = a.Item3.CompareTo(b.Item3); return c != 0 ? c : a.Item4.CompareTo(b.Item4); });
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
                tiles.Add(new Tile { town = key.Item1, model = key.Item2, bounds = b, size = size / list.Count, m = arr });
            }
            var eligible = townIds == null ? new HashSet<string>(System.StringComparer.Ordinal) : new HashSet<string>(townIds, System.StringComparer.Ordinal);
            if (townIds == null) foreach (var pair in swaps) if (pair.kit != null) eligible.Add(pair.kit.name);
            foreach (string id in eligible)
            {
                if (!perPlace.TryGetValue(id, out var list) || list.Count == 0) continue;
                var corners = new List<Vector3>();
                foreach (var item in list) item.Corners(corners);
                var bounds = new Bounds(corners[0], Vector3.zero);
                foreach (var corner in corners) bounds.Encapsulate(corner);
                places.Add((id, bounds));
                var kinds = new HashSet<TownModel>();
                foreach (var item in list) kinds.Add(item.Model);
                var orderedKinds = new List<TownModel>(kinds); orderedKinds.Sort(); required[id] = orderedKinds.ToArray();
            }
            places.Sort((a, b) => string.CompareOrdinal(a.id, b.id)); buildingPlaces.AddRange(places);
            Count = instances.Count;
            foreach (var (kit, close) in this.swaps)
            { if (kit != null) kit.forceRenderingOff = false; if (close != null) close.forceRenderingOff = true; }
        }

        /// <summary>Records instantiated whole-town seats for diagnostics. Rendering decisions use WholeTownReady per
        /// camera; a seat loaded for another camera never suppresses this camera's fallback.</summary>
        public void Suppress(IEnumerable<string> ids)
        {
            suppressed.Clear();
            suppressed.UnionWith(ids);
            // The next camera decides: do not hide fallback buildings while its original is still loading.
        }

        /// <summary>Whether a whole-town seat exists (a test aid; not a camera readiness decision).</summary>
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

        /// <summary>Test seam for streaming in EditMode, with the same caller-owned fallback renderers as a player.</summary>
        internal void InitStreaming(IReadOnlyList<SettlementKits.ModelInstance> instances, IReadOnlyList<(Renderer kit, Renderer? close)> swaps)
        {
            // A temporary complete editor set would load artwork eagerly. Initialise without that route or an error.
            bool was = suppressMissingLog; suppressMissingLog = true;
            try { Init(instances, null, swaps); streaming = true; }
            finally { suppressMissingLog = was; }
        }
        bool suppressMissingLog;

        internal void PrepareCamera(Camera cam)
        {
            RestoreRenderers();
            bool changed = PruneCameras();
            int key = cam.GetInstanceID();
            if (!choices.TryGetValue(key, out var choice))
            { choices[key] = choice = new Choice { camera = cam }; changed = true; }
            choice.lastFrame = Time.frameCount;
            bool wasClose = choice.close;
            choice.close = CloseAt(ViewDistance(cam), choice.close);
            changed |= wasClose != choice.close;
            string[] next = System.Array.Empty<string>();
            bool withinView = productionViewEnter.HasValue
                ? ViewDistance(cam) <= (choice.hadFocus ? productionViewExit!.Value : productionViewEnter.Value)
                : Collection.OriginalModelDistance.Visible(cam);
            if (SpatialPolicyConfigured && withinView)
            {
                if (Collection.CollectionResidencyView.TryGroundFocus(cam, out var focus))
                {
                    next = choice.hadFocus && focus == choice.focus && EdgeAllowance == null ? choice.selected : FocusedTowns(places, focus, choice.primary, EdgeAllowance == null ? null : (local, edge) =>
                    {
                        var admitted = new List<string>();
                        foreach (var c in OrderedChoices()) if (c.primary != null && !admitted.Contains(c.primary)) admitted.Add(c.primary);
                        foreach (string id in local) if (!admitted.Contains(id)) admitted.Add(id);
                        // Include other retained edges so the allowance sees existing residency as well as all primaries.
                        foreach (var c in OrderedChoices()) if (c != choice)
                            foreach (string id in c.selected) if (!admitted.Contains(id)) admitted.Add(id);
                        return EdgeAllowance!(admitted, edge);
                    }, admittedEnterM, admittedExitM, admittedEdgeM, admittedEdges);
                    choice.focus = focus; choice.hadFocus = true;
                }
                else choice.hadFocus = false;
            }
            if (!SpatialPolicyConfigured || !withinView) choice.hadFocus = false;
            choice.selected = next;
            choice.primary = next.Length == 0 ? null : next[0];
            if (!choice.towns.SetEquals(next))
            {
                choice.towns.Clear(); foreach (var id in next) choice.towns.Add(id); changed = true;
            }
            if (changed) { if (Application.isPlaying) reconcileDirty = true; else ReconcileModels(); }
            foreach (var (kit, close) in swaps)
            {
                bool ready = kit != null && swapIds.TryGetValue(kit, out var town) && (ReadyForTown(town, cam) || WholeTownReady?.Invoke(town, cam) == true);
                Remember(kit, ready); Remember(close, !ready);
            }
        }

        void Remember(Renderer? renderer, bool off)
        {
            if (renderer == null) return;
            previousRenderers.Add((renderer, renderer.forceRenderingOff)); renderer.forceRenderingOff = off;
        }
        internal void RestoreRenderers()
        {
            foreach (var (renderer, off) in previousRenderers) if (renderer != null) renderer.forceRenderingOff = off;
            previousRenderers.Clear();
        }

        internal bool WantsTown(string id, Camera cam) => choices.TryGetValue(cam.GetInstanceID(), out var c) && c.close && c.towns.Contains(id);
        internal bool ReadyForTown(string id, Camera cam)
        {
            if (!Draw || !WantsTown(id, cam) || !required.TryGetValue(id, out var kinds) || kinds.Length == 0) return false;
            if (set != null) return true;
            foreach (var kind in kinds)
                if (!models.TryGetValue(kind, out var model) || model.mesh == null || model.material == null) return false;
            return true;
        }
        internal void ConfigureWholeTowns(IReadOnlyList<(string id, Bounds bounds)> towns)
        {
            places.Clear(); places.AddRange(buildingPlaces);
            var ids = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var town in places) ids.Add(town.id);
            foreach (var town in towns) if (ids.Add(town.id)) places.Add(town);
            places.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var choice in choices.Values) choice.hadFocus = false;
        }
        internal bool WantsWholeTown(string id, Camera camera) =>
            Draw && choices.TryGetValue(camera.GetInstanceID(), out var c) && c.towns.Contains(id) && (!c.close || !required.ContainsKey(id) || (!streaming && set == null));
        internal bool WantsWholeTownAny(string id)
        {
            foreach (var c in OrderedChoices()) if (WantsWholeTown(id, c.camera)) return true;
            return false;
        }
        List<Choice> OrderedChoices()
        {
            var keys = new List<int>(choices.Keys); keys.Sort();
            var ordered = new List<Choice>(); foreach (int key in keys) ordered.Add(choices[key]);
            return ordered;
        }
        bool PruneCameras() => PruneCamerasAt(Time.frameCount, Application.isPlaying);
        bool PruneCamerasAt(int frame, bool applyGrace)
        {
            deadCameras.Clear();
            foreach (var pair in choices)
                if (pair.Value.camera == null || !pair.Value.camera.isActiveAndEnabled || (applyGrace && frame - pair.Value.lastFrame > cameraGraceFrames)) deadCameras.Add(pair.Key);
            foreach (int key in deadCameras) choices.Remove(key);
            if (deadCameras.Count > 0) RestoreRenderers();
            return deadCameras.Count != 0;
        }
        internal void AdvanceCameraFrame(int frame)
        {
            if (PruneCamerasAt(frame, true)) ReconcileModels();
        }
        void ReconcileModels()
        {
            if (!streaming || !isActiveAndEnabled) return;
            var needed = new HashSet<TownModel>();
            var ordered = new List<TownModel>();
            void AddTown(string id)
            {
                if (required.TryGetValue(id, out var kinds)) foreach (var kind in kinds) if (needed.Add(kind)) ordered.Add(kind);
            }
            // A focused town loads before any edge town, even when its model's enum value is larger.
            foreach (var c in OrderedChoices()) if (c.close && c.primary != null) AddTown(c.primary);
            foreach (var c in OrderedChoices()) if (c.close) foreach (string id in c.selected) if (id != c.primary) AddTown(id);
            var obsolete = new List<TownModel>();
            foreach (var pair in models) if (!needed.Contains(pair.Key)) obsolete.Add(pair.Key);
            foreach (var id in obsolete)
            {
                var old = models[id]; models.Remove(id); old.mesh = null; old.material = null;
                if (old.lease != null) { retiring.Add(old.lease); retiringKeys.Add((TownModels.Ids[(int)id], Collection.CollectionDetail.Near)); }
                old.lease = null;
            }
            // No next batch is admitted while a previous frame/source is still retiring.
            if (retiring.Count > 0)
            {
                if (Application.isPlaying && !retirementScheduled) { retirementScheduled = true; StartCoroutine(RetireAfterFrame()); }
                else if (!Application.isPlaying) RetireReleasedModels();
                return;
            }
            if (pendingRetirementLeases > 0) return;
            foreach (var id in ordered)
            {
                if (!isActiveAndEnabled) break;
                if (models.ContainsKey(id)) continue;
                var model = new Model(); models[id] = model;
                int generation = lifetimeGeneration;
                var lease = ModelAcquire(TownModels.Ids[(int)id], Collection.CollectionDetail.Near, prefab =>
                {
                    if (this == null || !isActiveAndEnabled || generation != lifetimeGeneration || !models.TryGetValue(id, out var current) || current != model) return;
                    model.answered = true; Collection.CollectionResidencyMetrics.MarkDirty();
                    var filter = prefab == null ? null : prefab.GetComponentInChildren<MeshFilter>(true);
                    var renderer = prefab == null ? null : prefab.GetComponentInChildren<MeshRenderer>(true);
                    if (filter == null || renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null) return;
                    model.mesh = filter.sharedMesh; model.material = renderer.sharedMaterial; Collection.CollectionResidencyMetrics.MarkDirty();
                });
                if (isActiveAndEnabled && generation == lifetimeGeneration && models.TryGetValue(id, out var live) && live == model) model.lease = lease;
                else
                {
                    retiring.Add(lease); retiringKeys.Add((TownModels.Ids[(int)id], Collection.CollectionDetail.Near));
                    RetireReleasedModels();
                }
            }
            Collection.CollectionResidencyMetrics.MarkDirty();
            if (retiring.Count > 0 && !retirementScheduled && Application.isPlaying)
            { retirementScheduled = true; StartCoroutine(RetireAfterFrame()); }
        }
        System.Collections.IEnumerator RetireAfterFrame()
        {
            yield return null;
            retirementScheduled = false; RetireReleasedModels();
        }
        internal void RetireReleasedModels()
        {
            if (retiring.Count == 0) return;
            var batch = retiring.ToArray(); var keys = retiringKeys.ToArray(); retiring.Clear(); retiringKeys.Clear();
            pendingRetirementLeases += batch.Length;
            Collection.CollectionResidencyMetrics.AfterDestroyed("town_buildings", System.Array.Empty<UnityEngine.Object>(), batch, () =>
            {
                pendingRetirementLeases -= batch.Length;
                if (this != null && isActiveAndEnabled) ReconcileModels();
                Collection.CollectionResidencyMetrics.MarkDirty();
            }, Application.isPlaying && ModelAcquire == Collection.CollectionStream.Acquire ? () => Collection.CollectionStream.Settled(keys) : (System.Func<bool>?)null, keys);
        }
        void ReleaseModels()
        {
            lifetimeGeneration++;
            foreach (var pair in models)
            {
                var model = pair.Value; model.mesh = null; model.material = null;
                if (model.lease != null) { retiring.Add(model.lease); retiringKeys.Add((TownModels.Ids[(int)pair.Key], Collection.CollectionDetail.Near)); }
                model.lease = null;
            }
            models.Clear(); RetireReleasedModels(); Collection.CollectionResidencyMetrics.MarkDirty();
        }
        void LateUpdate() { if (PruneCameras()) reconcileDirty = true; if (reconcileDirty) { reconcileDirty = false; ReconcileModels(); } }
        void OnDestroy() { RestoreRenderers(); ReleaseModels(); }
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += Submit;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Submit;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            StopAllCoroutines(); retirementScheduled = false;
            RestoreRenderers(); choices.Clear(); reconcileDirty = false; ReleaseModels();
        }
        void EndCamera(ScriptableRenderContext _, Camera camera) => RestoreRenderers();

        void Submit(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            PrepareCamera(cam);
            bool close = choices.TryGetValue(cam.GetInstanceID(), out var choice) && choice.close;
            LastClose = close;
            int drawn = 0, draws = 0; long tris = 0;
            if (close && Draw)
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                foreach (var t in tiles)
                {
                    if (!ReadyForTown(t.town, cam)) continue;
                    // The shadow pass sees casters outside the view too: cull on a widened box (as the trees).
                    var wide = t.bounds; wide.Expand(new Vector3(3000f, 0f, 3000f));
                    if (!GeometryUtility.TestPlanesAABB(planes, wide)) continue;
                    var mesh = set != null ? set.Mesh(t.model) : models[t.model].mesh!; // always the original mesh
                    var rp = new RenderParams(set != null ? set.Material(t.model) : models[t.model].material!)
                    {
                        worldBounds = t.bounds, camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, mesh, 0, t.m);
                    draws++; drawn += t.m.Length; tris += (long)t.m.Length * mesh.GetIndexCount(0) / 3;
                }
            }
            LastClose = close && drawn > 0;
            LastDrawn = drawn; LastTriangles = tris; LastDraws = draws;
        }
    }
}
