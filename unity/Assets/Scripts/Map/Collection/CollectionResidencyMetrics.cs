#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Opt-in development telemetry. Counts are ownership/object receipts, never inferred allocated bytes.</summary>
    public sealed class CollectionResidencyMetrics : MonoBehaviour
    {
        [Serializable] public sealed class Category
        {
            public string category = "";
            public int uniqueDetails;
            public string[] detailIds = Array.Empty<string>();
            public int demandedAnchors, liveInstances, ownedLeases, pendingRequests, retiringLeases, pendingDestroy;
            public int prefabCacheRefs, rendererCacheRefs, waiterCacheRefs, strongCacheRefs, missing, uniqueIds, bundleOwners, pins;
            public string[] originalIds = Array.Empty<string>(), bundleScopeIds = Array.Empty<string>();
        }
        [Serializable] public sealed class Bundle
        {
            public string id = "", state = "";
            public int owners, cachedPrefabs, cachedPaths, pendingRequests;
            public bool pinned, inFlight, evictionQueued;
        }
        [Serializable] public sealed class Snapshot
        {
            public int schema = 1;
            public long sequence;
            // Oversize diagnostics retain all counters but explicitly omit variable ID/bundle arrays, never silently truncate.
            public bool payloadOmitted;
            public string payloadSha256 = "";
            public int omittedBundleCount;
            public string reason = "";
            public Category[] categories = Array.Empty<Category>();
            public Bundle[] bundles = Array.Empty<Bundle>();
            public long unityAllocatedBytes, unityReservedBytes, unityUnusedReservedBytes;
        }
        sealed class Batch
        {
            internal string Category = "";
            internal UnityEngine.Object[] Instances = Array.Empty<UnityEngine.Object>();
            internal int Leases;
            internal string[] Ids = Array.Empty<string>(), Details = Array.Empty<string>();
        }
        static readonly Dictionary<string, HashSet<string>> observedIds = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        static readonly List<Batch> batches = new List<Batch>();
        static bool diagnosticsEnabled, dirty;
        static string lastState = "";
        static long sequence;
        static CollectionResidencyMetrics? host;
        internal static bool HasPendingRetirements => batches.Count != 0;
        internal static void Stop()
        {
            diagnosticsEnabled = false; dirty = false; lastState = ""; observedIds.Clear();
            if (host != null) { if (Application.isPlaying) Destroy(host.gameObject); else DestroyImmediate(host.gameObject); }
            host = null; // pending ownership receipts are removed by their actual completion, never erased here
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetDiagnostics() => Stop();
        internal static bool Enabled => diagnosticsEnabled;
        internal static void Enable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            diagnosticsEnabled = true; dirty = true; lastState = ""; sequence = 0; observedIds.Clear();
            if (Application.isPlaying && host == null)
            {
                var go = new GameObject("Collection residency diagnostics") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go); host = go.AddComponent<CollectionResidencyMetrics>();
            }
#else
            throw new InvalidOperationException("Residency diagnostics require an editor or development build.");
#endif
        }
        internal static void MarkDirty() { if (diagnosticsEnabled) dirty = true; }
        internal static void ResetForTests()
        {
            if (batches.Count != 0) throw new InvalidOperationException("Cannot erase pending destruction receipts.");
            diagnosticsEnabled = false; dirty = false; lastState = ""; observedIds.Clear();
            if (host != null) DestroyImmediate(host.gameObject); host = null;
        }
        internal static void AfterDestroyed(string category, IEnumerable<UnityEngine.Object> instances, IEnumerable<IDisposable> owners,
            Action? complete, Func<bool>? settled, IReadOnlyList<(string id, CollectionDetail detail)>? keys = null)
        {
            if (!diagnosticsEnabled) { CollectionRetirement.AfterDestroyed(instances, owners, complete, settled); return; }
            var objects = instances.ToArray(); var leases = owners.ToArray();
            var batch = new Batch { Category = category, Instances = objects, Leases = leases.Length,
                Ids = keys == null ? Array.Empty<string>() : keys.Select(k => k.id).Distinct(StringComparer.Ordinal).ToArray(),
                Details = keys == null ? Array.Empty<string>() : keys.Select(k => k.id + "|" + k.detail).Distinct(StringComparer.Ordinal).ToArray() };
            if (diagnosticsEnabled) { batches.Add(batch); MarkDirty(); }
            // Receipt outlives a disabled/destroyed category. Retain until actual destruction + source retirement settles.
            CollectionRetirement.AfterDestroyed(objects, leases, () =>
            {
                batches.Remove(batch); batch.Instances = Array.Empty<UnityEngine.Object>(); batch.Ids = batch.Details = Array.Empty<string>(); batch.Leases = 0;
                try { complete?.Invoke(); } finally { MarkDirty(); }
            }, settled);
        }
        internal static Category Row(string category, int demand, int instances, IEnumerable<string> ids, int owners,
            int pending, int prefabs, int renderers, int waiters, int missing, IEnumerable<string>? details = null)
        {
            var names = ids.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            return new Category { category = category, demandedAnchors = demand, liveInstances = instances, originalIds = names,
                detailIds = details == null ? Array.Empty<string>() : details.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                ownedLeases = owners, pendingRequests = pending, prefabCacheRefs = prefabs, rendererCacheRefs = renderers,
                waiterCacheRefs = waiters, strongCacheRefs = prefabs + renderers + waiters, missing = missing, uniqueIds = names.Length };
        }
        internal static Snapshot Capture()
        {
            var categories = new SortedDictionary<string, Category>(StringComparer.Ordinal);
            foreach (var name in new[] { "people", "rural", "craft", "special", "units", "town_buildings", "whole_towns", "shared_road", "retained_road", "train", "wildlife", "hex_board" }) categories[name] = Row(name, 0, 0, Array.Empty<string>(), 0, 0, 0, 0, 0, 0);
            void Add(Category source)
            {
                var row = categories[source.category];
                row.demandedAnchors += source.demandedAnchors; row.liveInstances += source.liveInstances; row.ownedLeases += source.ownedLeases;
                row.pendingRequests += source.pendingRequests; row.prefabCacheRefs += source.prefabCacheRefs; row.rendererCacheRefs += source.rendererCacheRefs;
                row.waiterCacheRefs += source.waiterCacheRefs; row.strongCacheRefs += source.strongCacheRefs; row.missing += source.missing;
                row.detailIds = row.detailIds.Concat(source.detailIds).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                row.originalIds = row.originalIds.Concat(source.originalIds).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            }
            foreach (var x in FindObjectsByType<CivilianPeople>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<RuralLife>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<SeaAirCollection>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<SpecialBuildingModels>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<Units.UnitFigureResidency>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            Add(Units.UnitFigures.ResidencyCaches());
            foreach (var x in FindObjectsByType<Units.UnitFigureDetail>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<Kits.TownModelLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<TownLandmarks>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<Routes.RoadVehicles>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { Add(x.SharedSnapshot()); Add(x.RetainedSnapshot()); }
            foreach (var x in FindObjectsByType<Routes.ServiceTrain>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in FindObjectsByType<WildlifeLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Add(x.ResidencySnapshot());
            foreach (var x in Statecraft.HexTiles.HexCollectionFigures.ResidencySnapshots()) Add(x);
            foreach (var batch in batches)
            {
                var row = categories[batch.Category]; row.retiringLeases += batch.Leases;
                row.pendingDestroy += batch.Instances.Count(x => x != null && x is GameObject);
                row.detailIds = row.detailIds.Concat(batch.Details).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                row.originalIds = row.originalIds.Concat(batch.Ids).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            }
            var bundles = CollectionStream.SharedDiagnostics().Select(b => new Bundle { id = b.Bundle, state = b.State, owners = b.Owners,
                cachedPrefabs = b.CachedPrefabs, cachedPaths = b.CachedPaths, pendingRequests = b.PendingRequests, pinned = b.Pinned,
                inFlight = b.InFlight, evictionQueued = b.EvictionQueued }).ToArray();
            foreach (var row in categories.Values)
            {
                row.uniqueIds = row.originalIds.Length; row.uniqueDetails = row.detailIds.Length;
                // String-only provenance survives owner destruction; no source, prefab, component or closure is retained.
                if (!observedIds.TryGetValue(row.category, out var history)) observedIds[row.category] = history = new HashSet<string>(StringComparer.Ordinal);
                history.UnionWith(row.originalIds);
                row.bundleScopeIds = history.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                var scope = new HashSet<string>(row.bundleScopeIds.Select(id => CollectionCatalog.BundleOf(id) ?? ""), StringComparer.Ordinal);
                foreach (var bundle in bundles) if (scope.Contains(bundle.id)) { row.bundleOwners += bundle.owners; if (bundle.pinned) row.pins++; }
            }
            return new Snapshot { categories = categories.Values.ToArray(), bundles = bundles };
        }
        // Memory fields and pose reason are deliberately excluded from state identity: changing allocator counters alone
        // must not turn an otherwise idle camera into an unbounded frame-by-frame log.
        internal static bool EmitSnapshot(Snapshot snapshot, string reason, bool force, Action<string> log)
        {
            if (!diagnosticsEnabled) return false;
            snapshot.sequence = 0; snapshot.reason = ""; snapshot.unityAllocatedBytes = snapshot.unityReservedBytes = snapshot.unityUnusedReservedBytes = 0;
            string state = JsonUtility.ToJson(snapshot);
            if (!force && state == lastState) return false;
            lastState = state; snapshot.reason = reason;
            snapshot.unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(); snapshot.unityReservedBytes = Profiler.GetTotalReservedMemoryLong();
            snapshot.unityUnusedReservedBytes = Profiler.GetTotalUnusedReservedMemoryLong();
            long current = ++sequence;
            foreach (var category in snapshot.categories)
            {
                var record = new Snapshot { schema = snapshot.schema, sequence = current, reason = reason,
                    categories = new[] { category }, bundles = snapshot.bundles,
                    unityAllocatedBytes = snapshot.unityAllocatedBytes, unityReservedBytes = snapshot.unityReservedBytes,
                    unityUnusedReservedBytes = snapshot.unityUnusedReservedBytes };
                string line = "RESIDENCY " + JsonUtility.ToJson(record);
                if (line.Length >= 4000)
                {
                    using (var hash = SHA256.Create())
                        record.payloadSha256 = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(line))).Replace("-", "").ToLowerInvariant();
                    // Copy only the telemetry DTO. Owner snapshots and aggregate counts remain untouched.
                    var compact = JsonUtility.FromJson<Category>(JsonUtility.ToJson(category));
                    compact.originalIds = compact.detailIds = compact.bundleScopeIds = Array.Empty<string>();
                    record.categories = new[] { compact }; record.omittedBundleCount = record.bundles.Length;
                    record.bundles = Array.Empty<Bundle>(); record.payloadOmitted = true;
                    line = "RESIDENCY " + JsonUtility.ToJson(record);
                }
                if (line.Length >= 4000) throw new InvalidOperationException("RESIDENCY category/reason exceeds diagnostic line budget.");
                log(line);
            }
            return true;
        }
        internal static void PoseSettled(string pose)
        { if (diagnosticsEnabled) EmitSnapshot(Capture(), "pose:" + pose, true, Debug.Log); }
        void LateUpdate()
        { if (!dirty) return; dirty = false; EmitSnapshot(Capture(), "state", false, Debug.Log); }
    }
}
