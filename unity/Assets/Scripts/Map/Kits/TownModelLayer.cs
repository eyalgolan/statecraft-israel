#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Building meshes for admitted towns and villages, instanced per model/tile: Tal Heuberger's originals below the
    /// close switch (<see cref="CloseEnterM"/>, #1273) and their lighter far-off copies beyond it, out to the far band
    /// (<see cref="FarEnterM"/>; owner, 3 November 2026, #2378: "Ask Tal; allow them far off only", then "I have Tal's OK;
    /// go ahead"). The far copies serve <see cref="Init"/>'s farTowns: every settlement with a whole-town model (the
    /// moshavot and towns, the kibbutzim, Nahalal and the Arab towns: Acre, Nazareth, Lod, Gaza, Beersheba), whose
    /// whole-town model gives way to them within the far band (owner, 3 November 2026, "Cities/towns only (Recommended)");
    /// the 271 1948 localities keep their kit blocks. The major cities (Tel Aviv, Jerusalem, Haifa and Jaffa:
    /// <see cref="TownModels.IsMajorCity"/>) are admitted below the close switch, as every other place, and draw Tal
    /// Heuberger's building models there (owner, 7 November 2026, "Tal's models close (Recommended)"); from 40 km out they
    /// take no far-off copies and draw their kits, with their landmarks (owner, 6 November 2026, "Big blocks, every zoom",
    /// which "Tal's models close (Recommended)" supersedes below the switch; it superseded the 2 November 2026 ruling, "Yes,
    /// small buildings at every zoom"). Under the production policy
    /// (owner ruling, 31 Oct 2026, "Load all in view (Recommended)") a close camera admits every town and every merged village
    /// tile in its view (+<see cref="ViewMarginM"/>), nearest first, and they unload above <see cref="CloseExitM"/>. Each camera
    /// swaps only complete towns and complete village tiles; loading and failed places retain code-built fallback. Source
    /// artwork is unchanged. Edge admission is off until a diagnostic-backed allowance is explicitly supplied.</summary>
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
        /// <summary>The far band (m), this layer's alone (the seven other layers on the close switch keep 40/45 km): from
        /// the close switch out to FarEnterM every settlement with a whole-town model draws the far-off
        /// copies of their building models instead of their kits; the kits come back above FarExitM. 100 km holds the 90 km opening and
        /// the 60 km gameplay view and stays under the strategic cap (Collection.OriginalModelDistance, 120 km); a 204 m house is 3.5 px
        /// there, where the kit's 100x houses read better as a mass, and the view's footprint (10,670 km2, about 170 places)
        /// bounds the cost. The band (12%) is wider than one wheel notch.</summary>
        public const float FarEnterM = 100000f, FarExitM = 112000f;
        /// <summary>The web tier's last runtime quality fallback step (QualityFallback.Steps, TownModelsFar): every camera
        /// is past the close switch at every zoom, so the towns draw their far-off copies without shadows instead of the
        /// originals with them, at the 30 km opening too. The fallback reaches it only on a device whose frames stay over the
        /// 30 fps floor after its other steps; it steps back off it as frames recover, and the map's teardown and a fixed
        /// quality choice (GraphicsQuality.Apply) clear it. A change takes effect at the camera's next prepare, unmoved.</summary>
        public static bool LowTier;
        /// <summary>Owner ruling, 31 Oct 2026, "Load all in view (Recommended)": a close camera admits a town or village tile
        /// whose building-model bounds, widened by ViewMarginM on every side, are in its view; an admitted one stays until it
        /// leaves the view widened by ViewKeepM, so a pan does not drop and reload a kind at the margin.</summary>
        public const float ViewMarginM = 3000f, ViewKeepM = 6000f;
        sealed class Tile { public string town = ""; public TownModel model; public Bounds bounds; public float size; public Matrix4x4[] m = System.Array.Empty<Matrix4x4>(); }

        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<string, List<SettlementKits.ModelInstance>> perPlace = new Dictionary<string, List<SettlementKits.ModelInstance>>();
        readonly List<(Renderer kit, Renderer? close)> swaps = new List<(Renderer, Renderer?)>();
        /// <summary>#2266: renderers that draw exactly when their leader (a kit or close renderer of <see cref="swaps"/>) does:
        /// the kit's replaceable landmark parts and Tal Heuberger's models standing in for them (HexPropModels).</summary>
        readonly Dictionary<Renderer, List<Renderer>> followers = new Dictionary<Renderer, List<Renderer>>();
        readonly Dictionary<Renderer, string> swapIds = new Dictionary<Renderer, string>();
        /// <summary>Each merged locality tile renderer's villages with building models: the tile swaps as one, only when every
        /// one of them is ready (<see cref="villageTiles"/>: each village's group).</summary>
        readonly Dictionary<Renderer, string[]> tileIds = new Dictionary<Renderer, string[]>();
        readonly Dictionary<string, string[]> villageTiles = new Dictionary<string, string[]>(System.StringComparer.Ordinal);
        /// <summary>The view-admission units: each detailed town alone, each village tile's villages together.</summary>
        readonly List<(string[] ids, Bounds bounds)> viewPlaces = new List<(string[], Bounds)>();
        readonly Plane[] frustum = new Plane[6];
        readonly List<(int place, float distance, string id)> viewScratch = new List<(int, float, string)>();
        readonly List<string> viewNext = new List<string>();
        readonly Dictionary<string[], bool> villagesReady = new Dictionary<string[], bool>();
        /// <summary>Set by <see cref="ConfigureProduction"/>. Explicit spatial values (<see cref="ConfigureResidency"/>: tests and
        /// diagnostics) keep the camera-focus admission alone.</summary>
        bool viewAdmission;
        /// <summary>The towns whose kit buildings never draw (<see cref="Suppress"/>), by settlement id.</summary>
        readonly HashSet<string> suppressed = new HashSet<string>(System.StringComparer.Ordinal);
        /// <summary>The places that draw the far-off copies beyond the close switch (<see cref="Init"/>); null admits none
        /// there (fail-closed, as the edge allowance).</summary>
        HashSet<string>? farTowns;
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
            SpatialPolicyEvidence = diagnosticEvidence; SpatialPolicyConfigured = true; viewAdmission = false;
            foreach (var choice in choices.Values)
            {
                choice.hadFocus = false; choice.selected = System.Array.Empty<string>(); choice.towns.Clear(); choice.primary = null;
                choice.inView.Clear(); choice.viewed.Clear(); choice.viewValid = false;
            }
            ReconcileModels();
        }
        internal System.Func<string, Camera, bool>? WholeTownReady;
        readonly List<(string id, Bounds bounds)> buildingPlaces = new List<(string, Bounds)>();
        // The full kit partition includes settlements without detailed models: those settlements must still be able to own
        // ground against a detailed neighbour.
        readonly List<(Vector2 centre, float radius, string id)> townFootprints = new List<(Vector2, float, string)>();
        readonly List<(Vector2 centre, float radius, string id)> configuredTownFootprints = new List<(Vector2, float, string)>();
        readonly HashSet<string> ignoredTownFootprintIds = new HashSet<string>(System.StringComparer.Ordinal);
        readonly Dictionary<string, (Vector2 centre, float radius, string id)> admissionFootprintsById =
            new Dictionary<string, (Vector2, float, string)>(System.StringComparer.Ordinal);
        readonly HashSet<string> loggedDuplicateTownFootprintIds = new HashSet<string>(System.StringComparer.Ordinal);
        static readonly IReadOnlyDictionary<string, (Vector2 centre, float radius, string id)> NoFootprintsById =
            new Dictionary<string, (Vector2, float, string)>(System.StringComparer.Ordinal);
        sealed class EdgeDistanceComparer : IComparer<(string id, float distance)>
        {
            public static readonly EdgeDistanceComparer Instance = new EdgeDistanceComparer();
            public int Compare((string id, float distance) a, (string id, float distance) b)
            { int c = a.distance.CompareTo(b.distance); return c != 0 ? c : string.CompareOrdinal(a.id, b.id); }
        }
        /// <summary>Nearest first, then by the place's first id: a shared instance, so a pan allocates no sort delegate.</summary>
        sealed class ViewDistanceComparer : IComparer<(int place, float distance, string id)>
        {
            public static readonly ViewDistanceComparer Instance = new ViewDistanceComparer();
            public int Compare((int place, float distance, string id) a, (int place, float distance, string id) b)
            { int c = a.distance.CompareTo(b.distance); return c != 0 ? c : string.CompareOrdinal(a.id, b.id); }
        }
        internal static System.Func<string, Collection.CollectionDetail, System.Action<GameObject?>, System.IDisposable> ModelAcquire = Collection.CollectionStream.Acquire;
        sealed class Choice
        {
            public Camera camera = null!;
            public int lastFrame;
            /// <summary>Below the close switch (the originals); within the far band (the far copies, where admitted).</summary>
            public bool close, far;
            public string? primary;
            public Vector3 focus;
            public bool hadFocus;
            public string[] selected = System.Array.Empty<string>();
            public readonly HashSet<string> towns = new HashSet<string>(System.StringComparer.Ordinal);
            /// <summary>The towns and villages admitted by view (nearest first), and the same as a set.</summary>
            public readonly List<string> inView = new List<string>();
            public readonly HashSet<string> viewed = new HashSet<string>(System.StringComparer.Ordinal);
            public Matrix4x4 viewMatrix;
            public bool viewValid, viewClose, viewFar;
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
        /// <summary>The streamed originals (Collection.CollectionDetail.Near) and the streamed far copies (Far), by kind.</summary>
        readonly Dictionary<TownModel, Model> models = new Dictionary<TownModel, Model>(), farModels = new Dictionary<TownModel, Model>();
        readonly Dictionary<string, TownModel[]> required = new Dictionary<string, TownModel[]>(System.StringComparer.Ordinal);
        readonly List<int> deadCameras = new List<int>();
        readonly HashSet<string> warnedIds = new HashSet<string>(System.StringComparer.Ordinal);
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
                SpatialPolicyConfigured = false; SpatialPolicyEvidence = "disabled"; viewAdmission = false;
                choices.Clear(); RestoreRenderers(); ReleaseModels(); return;
            }
            cameraGraceFrames = policy.Grace;
            productionViewEnter = policy.ViewEnter; productionViewExit = policy.ViewExit;
            ConfigureResidency(policy.Enter, policy.Exit, policy.Edge, policy.MaxEdges, policy.Evidence);
            viewAdmission = true;
        }
        public bool UsesFocusedOriginals => places.Count > 0;

        internal Collection.CollectionResidencyMetrics.Category ResidencySnapshot()
        {
            var active = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var c in choices.Values) { foreach (var town in c.towns) active.Add(town); foreach (var id in c.viewed) active.Add(id); }
            var ids = new List<string>(); var details = new List<string>(); int owners = 0, pending = 0, refs = 0, instances = 0, missing = 0;
            foreach (var (streamed, detail) in new[] { (models, Collection.CollectionDetail.Near), (farModels, Collection.CollectionDetail.Far) })
                foreach (var pair in streamed)
                {
                    ids.Add(StreamId(pair.Key, detail)); details.Add(StreamId(pair.Key, detail) + "|" + detail); if (pair.Value.lease != null) owners++;
                    if (!pair.Value.answered) pending++; else if (pair.Value.mesh == null || pair.Value.material == null) missing++;
                    if (pair.Value.mesh != null) refs++; if (pair.Value.material != null) refs++;
                }
            foreach (var tile in tiles)
                foreach (var c in choices.Values) if (ReadyForTown(tile.town, c.camera)) { instances += tile.m.Length; break; }
            var row = Collection.CollectionResidencyMetrics.Row("town_buildings", active.Count, instances, ids, owners, pending, 0, 0, 0, missing, details);
            row.strongCacheRefs += refs; row.retiringLeases = retiring.Count; return row;
        }

        static float GroundDistance(Bounds bounds, Vector3 point)
        {
            point.y = bounds.center.y;
            return Mathf.Sqrt(bounds.SqrDistance(point));
        }

        /// <summary>C89: one town near camera focus and at most two immediate edge neighbours; no map-wide Near load.</summary>
        internal static string[] FocusedTowns(IReadOnlyList<(string id, Bounds bounds)> towns, Vector3 focus, string? previous, System.Func<IReadOnlyList<string>, string, bool>? edgeAllowance, float enterM, float exitM, float edgeM, int maxEdges)
            => FocusedTownsWithFootprints(towns, NoFootprintsById, System.Array.Empty<(Vector2, float, string)>(), focus, previous,
                edgeAllowance, enterM, exitM, edgeM, maxEdges);

        /// <summary>As <see cref="FocusedTowns"/>, with detailed towns found in a precomputed id-to-footprint lookup and
        /// ownership decided against the full kit footprint partition, including settlements without detailed models.</summary>
        internal static string[] FocusedTownsWithFootprints(IReadOnlyList<(string id, Bounds bounds)> towns,
            IReadOnlyDictionary<string, (Vector2 centre, float radius, string id)> footprintsByTownId,
            IReadOnlyList<(Vector2 centre, float radius, string id)> allFootprints, Vector3 focus, string? previous,
            System.Func<IReadOnlyList<string>, string, bool>? edgeAllowance, float enterM, float exitM, float edgeM, int maxEdges)
        {
            maxEdges = Mathf.Max(0, maxEdges);
            int primary = -1; float best = float.PositiveInfinity, bestCentre = float.PositiveInfinity;
            var groundFocus = new Vector2(focus.x, focus.z);
            bool insideFootprintPartition = false;
            for (int i = 0; i < allFootprints.Count && !insideFootprintPartition; i++)
                insideFootprintPartition = SettlementKits.NormalizedFootprintDistance(groundFocus,
                    allFootprints[i].centre, allFootprints[i].radius) <= SettlementKits.FootprintBoundary;
            // Seam ownership is subordinate to hysteresis only while the previous town still owns the focus under the same
            // normalized-distance partition as admission, or while the focus remains in its instance-bounds exit band.
            if (previous != null) for (int i = 0; i < towns.Count; i++)
            {
                if (towns[i].id != previous) continue;
                bool ownsFootprint = footprintsByTownId.TryGetValue(previous, out var footprint)
                    && SettlementKits.NormalizedFootprintDistance(groundFocus, footprint.centre, footprint.radius)
                        <= SettlementKits.FootprintBoundary
                    && SettlementKits.OwnsFootprint(groundFocus, footprint.id, footprint.centre, footprint.radius, allFootprints);
                if (ownsFootprint || GroundDistance(towns[i].bounds, focus) <= exitM) primary = i;
                break;
            }
            if (primary < 0) for (int i = 0; i < towns.Count; i++)
            {
                if (!footprintsByTownId.TryGetValue(towns[i].id, out var footprint)) continue;
                float normalized = SettlementKits.NormalizedFootprintDistance(groundFocus, footprint.centre, footprint.radius);
                if (normalized <= SettlementKits.FootprintBoundary && SettlementKits.OwnsFootprint(groundFocus, footprint.id, footprint.centre,
                    footprint.radius, allFootprints) && (normalized < best || (normalized == best &&
                    (primary < 0 || string.CompareOrdinal(towns[i].id, towns[primary].id) < 0))))
                { primary = i; best = normalized; }
            }
            if (primary < 0 && insideFootprintPartition) return System.Array.Empty<string>();
            if (primary < 0) for (int i = 0; i < towns.Count; i++)
            {
                float distance = GroundDistance(towns[i].bounds, focus);
                var centreDelta = towns[i].bounds.center - focus; centreDelta.y = 0;
                float centre = centreDelta.sqrMagnitude;
                if (distance <= enterM && (distance < best || (distance == best &&
                    (centre < bestCentre || (centre == bestCentre && (primary < 0 || string.CompareOrdinal(towns[i].id, towns[primary].id) < 0))))))
                { primary = i; best = distance; bestCentre = centre; }
            }
            if (primary < 0) return System.Array.Empty<string>();
            if (edgeAllowance == null || maxEdges == 0) return new[] { towns[primary].id };
            var edge = new (string id, float distance)[towns.Count - 1]; int edgeCount = 0;
            var box = towns[primary].bounds; box.Expand(new Vector3(edgeM * 2, 0, edgeM * 2));
            for (int i = 0; i < towns.Count; i++)
            {
                if (i == primary) continue;
                var b = towns[i].bounds; b.center = new Vector3(b.center.x, box.center.y, b.center.z);
                if (box.Intersects(b)) edge[edgeCount++] = (towns[i].id, GroundDistance(b, focus));
            }
            System.Array.Sort(edge, 0, edgeCount, EdgeDistanceComparer.Instance);
            int capacity = maxEdges >= towns.Count ? towns.Count : maxEdges + 1;
            var result = new string[capacity]; result[0] = towns[primary].id; int resultCount = 1;
            for (int i = 0; i < edgeCount && resultCount <= maxEdges; i++)
                if (edgeAllowance(new System.ArraySegment<string>(result, 0, resultCount), edge[i].id)) result[resultCount++] = edge[i].id;
            if (resultCount != result.Length) System.Array.Resize(ref result, resultCount);
            return result;
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
        /// <summary>For the last camera: whether it was below the close switch and drew the models (else the kits), and the
        /// instances, triangles and draws it submitted (test and profiling aids).</summary>
        public bool LastClose { get; private set; }
        public int LastDrawn { get; private set; }
        public long LastTriangles { get; private set; }
        public int LastDraws { get; private set; }
        /// <summary>For the last camera: the instances it submitted with shadow casting on (a test and profiling aid).</summary>
        public int LastShadowCasters { get; private set; }

        /// <summary>Explicit town ids and renderer identities distinguish individual towns from mixed locality tiles
        /// (<paramref name="rendererTileIds"/>: each tile renderer's villages). A complete editor model set is optional in a
        /// player: acquired originals become ready independently per town, while incomplete/missing replies keep fallback. A
        /// tile swaps only when every village in it is ready; no tile or unrelated town swaps on a partial global set.
        /// <paramref name="farTowns"/> names the places that draw the far-off copies beyond the close switch (every settlement
        /// with a whole-town model: KitLayer passes them); null, the default of the small test fixtures, admits none there.</summary>
        public void Init(IReadOnlyList<SettlementKits.ModelInstance> instances, TownModelSet? models, IReadOnlyList<(Renderer kit, Renderer? close)> swaps, IReadOnlyCollection<string>? townIds = null, IReadOnlyDictionary<Renderer, string>? rendererTownIds = null,
            IReadOnlyDictionary<Renderer, string[]>? rendererTileIds = null, IReadOnlyCollection<string>? farTowns = null)
        {
            RestoreRenderers(); ReleaseModels(); choices.Clear(); places.Clear(); buildingPlaces.Clear();
            this.farTowns = farTowns == null ? null : new HashSet<string>(farTowns, System.StringComparer.Ordinal);
            townFootprints.Clear(); configuredTownFootprints.Clear(); ignoredTownFootprintIds.Clear();
            admissionFootprintsById.Clear(); loggedDuplicateTownFootprintIds.Clear(); required.Clear();
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
            // A place's building-model bounds, and the model kinds it needs (required); false without building models.
            bool Describe(string id, out Bounds placeBounds)
            {
                placeBounds = default;
                if (!perPlace.TryGetValue(id, out var list) || list.Count == 0) return false;
                var corners = new List<Vector3>();
                foreach (var item in list) item.Corners(corners);
                placeBounds = new Bounds(corners[0], Vector3.zero);
                foreach (var corner in corners) placeBounds.Encapsulate(corner);
                var kinds = new HashSet<TownModel>();
                foreach (var item in list) kinds.Add(item.Model);
                var orderedKinds = new List<TownModel>(kinds); orderedKinds.Sort(); required[id] = orderedKinds.ToArray();
                return true;
            }
            foreach (string id in eligible) if (Describe(id, out var bounds)) places.Add((id, bounds));
            places.Sort((a, b) => string.CompareOrdinal(a.id, b.id)); buildingPlaces.AddRange(places);
            tileIds.Clear(); villageTiles.Clear(); viewPlaces.Clear();
            foreach (var place in places) viewPlaces.Add((new[] { place.id }, place.bounds));
            if (rendererTileIds != null) foreach (var pair in this.swaps)
            {
                if (pair.kit == null || !rendererTileIds.TryGetValue(pair.kit, out var ids)) continue;
                var villages = new List<string>(); var tile = new Bounds();
                foreach (string id in ids)
                {
                    if (eligible.Contains(id) || !Describe(id, out var bounds)) continue;
                    if (villages.Count == 0) tile = bounds; else tile.Encapsulate(bounds);
                    villages.Add(id);
                }
                if (villages.Count == 0) continue;   // camps' tents and model-less places: the close mesh keeps them
                villages.Sort(string.CompareOrdinal);
                var group = villages.ToArray(); tileIds[pair.kit] = group; viewPlaces.Add((group, tile));
                foreach (string id in group) villageTiles[id] = group;
            }
            Count = instances.Count;
            foreach (var (kit, close) in this.swaps)
            { if (kit != null) kit.forceRenderingOff = false; if (close != null) close.forceRenderingOff = true; }
            followers.Clear();
        }

        /// <summary>#2266: <paramref name="follower"/> draws from now on exactly when <paramref name="leader"/> does, camera by
        /// camera; it takes the leader's state between cameras at once.</summary>
        internal void Follow(Renderer leader, Renderer follower)
        {
            if (!followers.TryGetValue(leader, out var list)) followers[leader] = list = new List<Renderer>();
            list.RemoveAll(r => r == null);
            if (!list.Contains(follower)) list.Add(follower);
            bool resting = leader.forceRenderingOff;
            foreach (var (renderer, off) in previousRenderers) if (renderer == leader) { resting = off; break; }
            follower.forceRenderingOff = resting;
        }

        internal void Unfollow(Renderer leader, Renderer follower) { if (followers.TryGetValue(leader, out var list)) list.Remove(follower); }

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

        /// <summary>The models placed for a town or locality, as placed: a test aid, and the buildings the town-edge props keep
        /// clear of (Collection.SpecialBuildingModels).</summary>
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

        /// <summary>Whether a camera at <paramref name="distanceM"/> is within the far band (the far-off copies, beyond the
        /// close switch), given its last choice (<paramref name="wasFar"/>, null for none): the same hysteresis as
        /// <see cref="CloseAt"/>, on <see cref="FarEnterM"/> and <see cref="FarExitM"/>.</summary>
        public static bool FarAt(float distanceM, bool? wasFar) =>
            wasFar == true ? distanceM <= FarExitM : distanceM < FarEnterM;

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

        /// <summary>A direct call (tests and editor tools, outside any render) applies a change of demand at once in edit mode.</summary>
        internal void PrepareCamera(Camera cam) => PrepareCameraCore(cam, inRender: false);
        internal void PrepareCameraCore(Camera cam, bool inRender)
        {
            RestoreRenderers();
            bool changed = PruneCameras();
            int key = cam.GetInstanceID();
            if (!choices.TryGetValue(key, out var choice))
            { choices[key] = choice = new Choice { camera = cam }; changed = true; }
            choice.lastFrame = Time.frameCount;
            bool wasClose = choice.close, wasFar = choice.far;
            choice.close = !LowTier && CloseAt(ViewDistance(cam), choice.close);
            choice.far = FarAt(ViewDistance(cam), choice.far);
            changed |= wasClose != choice.close || wasFar != choice.far;
            string[] next = System.Array.Empty<string>();
            bool withinView = productionViewEnter.HasValue
                ? ViewDistance(cam) <= (choice.hadFocus ? productionViewExit!.Value : productionViewEnter.Value)
                : Collection.OriginalModelDistance.Visible(cam);
            if (SpatialPolicyConfigured && withinView)
            {
                if (Collection.CollectionResidencyView.TryGroundFocus(cam, out var focus))
                {
                    next = choice.hadFocus && focus == choice.focus && EdgeAllowance == null ? choice.selected : FocusedTownsWithFootprints(
                        places, admissionFootprintsById, townFootprints, focus, choice.primary, AllowanceFor(choice),
                        admittedEnterM, admittedExitM, admittedEdgeM, admittedEdges);
                    choice.focus = focus; choice.hadFocus = true;
                }
                else choice.hadFocus = false;
            }
            if (!SpatialPolicyConfigured || !withinView) choice.hadFocus = false;
            choice.selected = next;
            choice.primary = next.Length == 0 ? null : next[0];
            if (!SameTowns(choice.towns, next))
            {
                choice.towns.Clear(); foreach (var id in next) choice.towns.Add(id); changed = true;
            }
            changed |= AdmitInView(choice, cam);
            if (changed)
            {
                if (Application.isPlaying || inRender)
                {
                    reconcileDirty = true;
#if UNITY_EDITOR
                    // Edit mode runs LateUpdate only when something asks for a player-loop update.
                    if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
                }
                else ReconcileModels();
            }
            villagesReady.Clear();
            foreach (var (kit, close) in swaps)
            {
                bool ready = kit != null && (swapIds.TryGetValue(kit, out var town)
                    ? ReadyForTown(town, cam) || WholeTownReady?.Invoke(town, cam) == true
                    : tileIds.TryGetValue(kit, out var villages) && VillagesReady(villages, cam));
                Remember(kit, ready); Remember(close, !ready);
            }
        }

        /// <summary>Whether <paramref name="towns"/> holds exactly the ids of <paramref name="next"/>, as HashSet.SetEquals would
        /// say, without allocating: SetEquals walks an array through IEnumerable, which boxes its enumerator, on every frame a
        /// camera has a ground focus (below OriginalModelDistance.EnterM, 120 km), the far band included. The sets are a few
        /// towns, so the two passes cost nothing.</summary>
        static bool SameTowns(HashSet<string> towns, string[] next)
        {
            foreach (string id in next) if (!towns.Contains(id)) return false;
            foreach (string id in towns) if (System.Array.IndexOf(next, id) < 0) return false;
            return true;
        }
        static bool SameTowns(HashSet<string> towns, List<string> next)
        {
            foreach (string id in next) if (!towns.Contains(id)) return false;
            foreach (string id in towns) if (!next.Contains(id)) return false;
            return true;
        }

        /// <summary>The edge allowance for <paramref name="owner"/>'s focus selection, or null without one. A method of its own,
        /// so PrepareCameraCore holds no closure: a closure there was allocated on every call, far off too, where the far-off
        /// copies draw.</summary>
        System.Func<IReadOnlyList<string>, string, bool>? AllowanceFor(Choice owner) => EdgeAllowance == null ? null : (local, edge) =>
        {
            var admitted = new List<string>();
            foreach (var c in OrderedChoices()) if (c.primary != null && !admitted.Contains(c.primary)) admitted.Add(c.primary);
            foreach (string id in local) if (!admitted.Contains(id)) admitted.Add(id);
            // Include other retained edges so the allowance sees existing residency as well as all primaries.
            foreach (var c in OrderedChoices()) if (c != owner)
                foreach (string id in c.selected) if (!admitted.Contains(id)) admitted.Add(id);
            return EdgeAllowance!(admitted, edge);
        };

        /// <summary>Owner ruling, 31 Oct 2026, "Load all in view (Recommended)": under the production policy a close camera
        /// admits every town and village tile in its view (<see cref="ViewMarginM"/>, kept to <see cref="ViewKeepM"/>), nearest
        /// its ground focus first; a far camera only what its zoom admits (<see cref="AtZoom"/>). Returns whether the
        /// admitted set changed.</summary>
        bool AdmitInView(Choice choice, Camera cam)
        {
            if (!viewAdmission || !SpatialPolicyConfigured)
            {
                choice.viewValid = false;
                if (choice.inView.Count == 0) return false;
                choice.inView.Clear(); choice.viewed.Clear(); return true;
            }
            var viewProjection = cam.projectionMatrix * cam.worldToCameraMatrix;
            if (choice.viewValid && viewProjection == choice.viewMatrix && choice.viewClose == choice.close && choice.viewFar == choice.far) return false;
            choice.viewMatrix = viewProjection; choice.viewValid = true; choice.viewClose = choice.close; choice.viewFar = choice.far;
            GeometryUtility.CalculateFrustumPlanes(viewProjection, frustum);
            var from = choice.hadFocus ? choice.focus : cam.transform.position;
            viewScratch.Clear();
            for (int i = 0; i < viewPlaces.Count; i++)
            {
                var (ids, bounds) = viewPlaces[i];
                if (!AtZoom(choice, ids[0])) continue;   // a village tile's first village: never a major city
                float margin = choice.viewed.Contains(ids[0]) ? ViewKeepM : ViewMarginM;
                var wide = bounds; wide.Expand(new Vector3(2f * margin, 0f, 2f * margin));
                if (GeometryUtility.TestPlanesAABB(frustum, wide)) viewScratch.Add((i, GroundDistance(bounds, from), ids[0]));
            }
            viewScratch.Sort(ViewDistanceComparer.Instance);
            viewNext.Clear();
            foreach (var (i, _, _) in viewScratch) viewNext.AddRange(viewPlaces[i].ids);
            bool changed = !SameTowns(choice.viewed, viewNext);   // not SetEquals: no boxed enumerator while the view moves
            choice.inView.Clear(); choice.inView.AddRange(viewNext);   // the order alone loads nothing new
            if (changed) { choice.viewed.Clear(); choice.viewed.UnionWith(viewNext); }
            return changed;
        }

        /// <summary>Whether every village of a merged tile has its building models ready for this camera (cached per
        /// PrepareCamera: the swap and the draw decide alike).</summary>
        bool VillagesReady(string[] villages, Camera cam)
        {
            if (villagesReady.TryGetValue(villages, out bool ready)) return ready;
            ready = villages.Length > 0;
            foreach (string id in villages) if (!ReadyForTown(id, cam)) { ready = false; break; }
            villagesReady[villages] = ready;
            return ready;
        }

        void Remember(Renderer? renderer, bool off)
        {
            if (renderer == null) return;
            previousRenderers.Add((renderer, renderer.forceRenderingOff)); renderer.forceRenderingOff = off;
            if (followers.TryGetValue(renderer, out var list))
                foreach (var f in list) if (f != null) { previousRenderers.Add((f, f.forceRenderingOff)); f.forceRenderingOff = off; }
        }
        internal void RestoreRenderers()
        {
            foreach (var (renderer, off) in previousRenderers) if (renderer != null) renderer.forceRenderingOff = off;
            previousRenderers.Clear();
        }

        /// <summary>Whether a camera's zoom admits a place's building models: every place below the close switch (the
        /// originals); within the far band the places that take the far-off copies (<see cref="farTowns"/>); beyond it none.
        /// A major city (<see cref="TownModels.IsMajorCity"/>) only below the close switch, with its hysteresis (in below
        /// CloseEnterM, out above CloseExitM, as every place): Tal Heuberger's building models there (owner, 7 November 2026,
        /// "Tal's models close (Recommended)"), and from 40 km out its kit (owner, 6 November 2026, "Big blocks, every zoom",
        /// superseded below the switch by "Tal's models close (Recommended)"); never in the far band.</summary>
        bool AtZoom(Choice c, string id) => c.close || (c.far && !TownModels.IsMajorCity(id) && farTowns != null && farTowns.Contains(id));
        /// <summary>Whether the camera-focus selection holds this town at its zoom (the focused town and its edges).</summary>
        internal bool WantsTown(string id, Camera cam) => choices.TryGetValue(cam.GetInstanceID(), out var c) && AtZoom(c, id) && c.towns.Contains(id);
        /// <summary>Whether this camera admits the place's building models: the focus selection, or (production) its view.</summary>
        internal bool AdmitsTown(string id, Camera cam) =>
            choices.TryGetValue(cam.GetInstanceID(), out var c) && AtZoom(c, id) && (c.towns.Contains(id) || c.viewed.Contains(id));
        /// <summary>Whether this camera's admitted place has every kind it needs at the camera's tier: the originals below the
        /// close switch, the far-off copies beyond it.</summary>
        internal bool ReadyForTown(string id, Camera cam)
        {
            if (!Draw || !AdmitsTown(id, cam) || !required.TryGetValue(id, out var kinds) || kinds.Length == 0) return false;
            if (set != null) return true;
            var streamed = choices[cam.GetInstanceID()].close ? models : farModels;
            foreach (var kind in kinds)
                if (!streamed.TryGetValue(kind, out var model) || model.mesh == null || model.material == null) return false;
            return true;
        }

        /// <summary>The streamed id of a kind at a detail: the original's id near, its far-off copy's far.</summary>
        static string StreamId(TownModel kind, Collection.CollectionDetail detail) =>
            detail == Collection.CollectionDetail.Near ? TownModels.Ids[(int)kind] : Collection.CollectionCatalog.FarCopy(TownModels.Ids[(int)kind]);
        static void BuildAdmissionFootprints(IReadOnlyList<(string id, Bounds bounds)> proposedPlaces,
            IReadOnlyList<(Vector2 centre, float radius, string id)> configured,
            HashSet<string> ignored, List<(Vector2 centre, float radius, string id)> partition,
            Dictionary<string, (Vector2 centre, float radius, string id)> byTownId, HashSet<string> loggedDuplicateIds)
        {
            var detailedIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var town in proposedPlaces) detailedIds.Add(town.id);
            var counts = new Dictionary<string, int>(System.StringComparer.Ordinal);
            foreach (var footprint in configured)
            {
                counts.TryGetValue(footprint.id, out int count);
                counts[footprint.id] = count + 1;
            }
            var invalid = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var town in proposedPlaces)
                if (counts.TryGetValue(town.id, out int count) && count > 1 && invalid.Add(town.id)
                    && loggedDuplicateIds.Add(town.id))
                    Debug.LogError($"Detailed town footprint id '{town.id}' occurs more than once; dropping its footprint and using instance bounds.");
            foreach (var footprint in configured)
            {
                if (invalid.Contains(footprint.id) || ignored.Contains(footprint.id)) continue;
                partition.Add(footprint);
                if (detailedIds.Contains(footprint.id)) byTownId.Add(footprint.id, footprint);
            }
        }

        void CommitAdmissionFootprints(List<(Vector2 centre, float radius, string id)> partition,
            Dictionary<string, (Vector2 centre, float radius, string id)> byTownId)
        {
            townFootprints.Clear(); townFootprints.AddRange(partition);
            admissionFootprintsById.Clear();
            foreach (var pair in byTownId) admissionFootprintsById.Add(pair.Key, pair.Value);
        }

        void ClearFocusChoices()
        {
            foreach (var choice in choices.Values) { choice.hadFocus = false; choice.primary = null; }
        }

        void ClearMissingFocusChoices()
        {
            var present = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var place in places) present.Add(place.id);
            foreach (var choice in choices.Values)
            {
                choice.hadFocus = false;
                if (choice.primary != null && !present.Contains(choice.primary)
                    && !admissionFootprintsById.ContainsKey(choice.primary)) choice.primary = null;
            }
        }

        /// <summary>Stores the game-settlement footprint partition separately from detailed-instance and whole-town bounds.
        /// Camera focus and town labels use a footprint centre, which need not fall among surviving model placements. The
        /// caller excludes the same 1948 locality ids that game-town generation ignores; non-detailed game settlements and
        /// camps still block detailed neighbours. Repeated non-detailed camp ids remain separate discs. A duplicate detailed
        /// id is logged and dropped so that town safely falls back to its instance bounds.</summary>
        internal void ConfigureTownFootprints(IReadOnlyList<(Vector2 centre, float radius, string id)> footprints,
            IReadOnlyCollection<string>? ignoredForGameSettlements = null)
        {
            var proposedConfigured = new List<(Vector2 centre, float radius, string id)>(footprints);
            var proposedIgnored = ignoredForGameSettlements == null
                ? new HashSet<string>(System.StringComparer.Ordinal)
                : new HashSet<string>(ignoredForGameSettlements, System.StringComparer.Ordinal);
            var partition = new List<(Vector2 centre, float radius, string id)>();
            var byTownId = new Dictionary<string, (Vector2 centre, float radius, string id)>(System.StringComparer.Ordinal);
            var proposedLoggedDuplicates = new HashSet<string>(System.StringComparer.Ordinal);
            BuildAdmissionFootprints(places, proposedConfigured, proposedIgnored, partition, byTownId, proposedLoggedDuplicates);
            configuredTownFootprints.Clear(); configuredTownFootprints.AddRange(proposedConfigured);
            ignoredTownFootprintIds.Clear(); ignoredTownFootprintIds.UnionWith(proposedIgnored);
            loggedDuplicateTownFootprintIds.Clear(); loggedDuplicateTownFootprintIds.UnionWith(proposedLoggedDuplicates);
            CommitAdmissionFootprints(partition, byTownId);
            ClearFocusChoices();
        }
        internal void ConfigureWholeTowns(IReadOnlyList<(string id, Bounds bounds)> towns)
        {
            var proposedPlaces = new List<(string id, Bounds bounds)>(buildingPlaces);
            var ids = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var town in proposedPlaces) ids.Add(town.id);
            foreach (var town in towns) if (ids.Add(town.id)) proposedPlaces.Add(town);
            proposedPlaces.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            var partition = new List<(Vector2 centre, float radius, string id)>();
            var byTownId = new Dictionary<string, (Vector2 centre, float radius, string id)>(System.StringComparer.Ordinal);
            var proposedLoggedDuplicates = new HashSet<string>(loggedDuplicateTownFootprintIds, System.StringComparer.Ordinal);
            BuildAdmissionFootprints(proposedPlaces, configuredTownFootprints, ignoredTownFootprintIds, partition, byTownId, proposedLoggedDuplicates);
            places.Clear(); places.AddRange(proposedPlaces);
            loggedDuplicateTownFootprintIds.Clear(); loggedDuplicateTownFootprintIds.UnionWith(proposedLoggedDuplicates);
            CommitAdmissionFootprints(partition, byTownId);
            ClearMissingFocusChoices();
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
            // The kinds each tier needs: a close camera's admitted places need the originals, a far camera's the far copies.
            var needed = new HashSet<TownModel>(); var ordered = new List<TownModel>();
            var neededFar = new HashSet<TownModel>(); var orderedFar = new List<TownModel>();
            void AddTown(Choice c, string id)
            {
                var (wanted, order) = c.close ? (needed, ordered) : (neededFar, orderedFar);
                if (required.TryGetValue(id, out var kinds)) foreach (var kind in kinds) if (wanted.Add(kind)) order.Add(kind);
            }
            // A focused town loads before any edge town, even when its model's enum value is larger; each only at a zoom that
            // admits it (AtZoom).
            foreach (var c in OrderedChoices()) if (c.primary != null && AtZoom(c, c.primary)) AddTown(c, c.primary);
            foreach (var c in OrderedChoices()) foreach (string id in c.selected) if (id != c.primary && AtZoom(c, id)) AddTown(c, id);
            // Then every town and village in view, nearest first (owner ruling, 31 Oct 2026, "Load all in view"); AdmitInView
            // has already left out what this zoom does not admit.
            foreach (var c in OrderedChoices()) foreach (string id in c.inView) AddTown(c, id);
            foreach (var (streamed, kept, detail) in new[] { (models, needed, Collection.CollectionDetail.Near), (farModels, neededFar, Collection.CollectionDetail.Far) })
            {
                var obsolete = new List<TownModel>();
                foreach (var pair in streamed) if (!kept.Contains(pair.Key)) obsolete.Add(pair.Key);
                foreach (var id in obsolete)
                {
                    var old = streamed[id]; streamed.Remove(id); old.mesh = null; old.material = null;
                    if (old.lease != null) { retiring.Add(old.lease); retiringKeys.Add((StreamId(id, detail), detail)); }
                    old.lease = null;
                }
            }
            // No next batch is admitted while a previous frame/source is still retiring.
            if (retiring.Count > 0)
            {
                if (Application.isPlaying && !retirementScheduled) { retirementScheduled = true; StartCoroutine(RetireAfterFrame()); }
                else if (!Application.isPlaying) RetireReleasedModels();
                return;
            }
            if (pendingRetirementLeases > 0) return;
            Acquire(models, ordered, Collection.CollectionDetail.Near);
            Acquire(farModels, orderedFar, Collection.CollectionDetail.Far);
            Collection.CollectionResidencyMetrics.MarkDirty();
            if (retiring.Count > 0 && !retirementScheduled && Application.isPlaying)
            { retirementScheduled = true; StartCoroutine(RetireAfterFrame()); }
        }

        /// <summary>Asks the stream for each kind of <paramref name="ordered"/> that <paramref name="streamed"/> lacks, at
        /// <paramref name="detail"/> (the original near, the far-off copy far).</summary>
        void Acquire(Dictionary<TownModel, Model> streamed, List<TownModel> ordered, Collection.CollectionDetail detail)
        {
            foreach (var id in ordered)
            {
                if (!isActiveAndEnabled) break;
                if (streamed.ContainsKey(id)) continue;
                var model = new Model(); streamed[id] = model;
                int generation = lifetimeGeneration;
                string streamId = StreamId(id, detail);
                var lease = ModelAcquire(streamId, detail, prefab =>
                {
                    if (this == null || !isActiveAndEnabled || generation != lifetimeGeneration || !streamed.TryGetValue(id, out var current) || current != model) return;
                    model.answered = true; Collection.CollectionResidencyMetrics.MarkDirty();
                    var filter = prefab == null ? null : prefab.GetComponentInChildren<MeshFilter>(true);
                    var renderer = prefab == null ? null : prefab.GetComponentInChildren<MeshRenderer>(true);
                    if (filter == null || renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                    {
                        // Once per streamed id for this layer (an original and its far-off copy fail apart): its places keep their
                        // code-built kits.
                        if (warnedIds.Add(streamId)) Debug.LogWarning($"TOWN MODELS: {streamId} did not stream; the places built from it keep their kits.");
                        return;
                    }
                    model.mesh = filter.sharedMesh; model.material = renderer.sharedMaterial; Collection.CollectionResidencyMetrics.MarkDirty();
                });
                if (isActiveAndEnabled && generation == lifetimeGeneration && streamed.TryGetValue(id, out var live) && live == model) model.lease = lease;
                else
                {
                    retiring.Add(lease); retiringKeys.Add((streamId, detail));
                    RetireReleasedModels();
                }
            }
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
            foreach (var (streamed, detail) in new[] { (models, Collection.CollectionDetail.Near), (farModels, Collection.CollectionDetail.Far) })
            {
                foreach (var pair in streamed)
                {
                    var model = pair.Value; model.mesh = null; model.material = null;
                    if (model.lease != null) { retiring.Add(model.lease); retiringKeys.Add((StreamId(pair.Key, detail), detail)); }
                    model.lease = null;
                }
                streamed.Clear();
            }
            RetireReleasedModels(); Collection.CollectionResidencyMetrics.MarkDirty();
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

        /// <summary>The render callback. A change of demand never acquires or retires models while a camera renders, in edit
        /// mode either (a Scene or Game view camera): it waits for LateUpdate, as in play mode.</summary>
        void Submit(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            PrepareCameraCore(cam, inRender: true);
            bool close = choices.TryGetValue(cam.GetInstanceID(), out var choice) && choice.close;
            LastClose = close;
            int drawn = 0, draws = 0, casters = 0; long tris = 0;
            // Owner ruling on #2267, 2 November 2026, "Shadows off far, then measure (Recommended)": beyond the close switch the
            // building models (the far-off copies) cast no shadows; below it every building casts.
            var shadows = close ? ShadowCastingMode.On : ShadowCastingMode.Off;
            // Beyond the close switch the far-off copies draw (#2378): for the places the far band admits (AtZoom);
            // ReadyForTown admits nothing else there.
            var streamed = close ? models : farModels;
            if (Draw)
            {
                // Into the scratch array (no Plane[6] per camera per frame: this runs far off too, the common view).
                GeometryUtility.CalculateFrustumPlanes(cam.projectionMatrix * cam.worldToCameraMatrix, frustum);
                foreach (var t in tiles)
                {
                    if (!ReadyForTown(t.town, cam)) continue;
                    // A village's buildings draw only with its whole tile swapped (else they would stand on its kit houses).
                    if (villageTiles.TryGetValue(t.town, out var villages) && !VillagesReady(villages, cam)) continue;
                    // The shadow pass sees casters outside the view too: cull on a widened box (as the trees).
                    var wide = t.bounds; wide.Expand(new Vector3(3000f, 0f, 3000f));
                    if (!GeometryUtility.TestPlanesAABB(frustum, wide)) continue;
                    var mesh = set != null ? (close ? set.Mesh(t.model) : set.FarMesh(t.model)) : streamed[t.model].mesh!;   // the original below the close switch
                    var rp = new RenderParams(set != null ? (close ? set.Material(t.model) : set.FarMaterial(t.model)) : streamed[t.model].material!)
                    {
                        worldBounds = t.bounds, camera = cam, shadowCastingMode = shadows, receiveShadows = true,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, mesh, 0, t.m);
                    draws++; drawn += t.m.Length; tris += (long)t.m.Length * mesh.GetIndexCount(0) / 3;
                    if (shadows != ShadowCastingMode.Off) casters += t.m.Length;
                }
            }
            LastClose = close && drawn > 0;
            LastDrawn = drawn; LastTriangles = tris; LastDraws = draws; LastShadowCasters = casters;
        }
    }
}
