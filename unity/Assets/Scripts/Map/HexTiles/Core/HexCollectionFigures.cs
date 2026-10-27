#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>A hex visual owner's acquired Collection prefabs, and its borrowed hex props (<see cref="HexProps"/>, from
    /// their own bundle's stream, pinned like any Request). Requests only placed ids, once per owner; pending/failed
    /// requests keep Resources or stand-in fallback. Retire generated instances and prefab/mesh/material references before
    /// disposing the acquired leases. A late callback must not revive a disposed board.</summary>
    internal sealed class HexCollectionFigures : IDisposable
    {
        internal static Action<string, CollectionDetail, Action<GameObject?>>? Source, PropSource;
        internal static Func<string, CollectionDetail, Action<GameObject?>, IDisposable> Acquire = CollectionStream.Acquire;
        static Action<string, CollectionDetail, Action<GameObject?>>? Requester =>
            Source ?? (Application.isPlaying ? CollectionStream.Request : (Action<string, CollectionDetail, Action<GameObject?>>?)null);
        static Action<string, CollectionDetail, Action<GameObject?>>? PropRequester =>
            PropSource ?? (Application.isPlaying ? (id, detail, done) => CollectionStream.Request(HexProps.Catalog, id, detail, done)
                : (Action<string, CollectionDetail, Action<GameObject?>>?)null);
        internal static bool Available => (HexModels.DrawCollectionFigures || HexModels.DrawTownModels) && Requester != null;

        /// <summary>Whether <paramref name="id"/> is drawn from a stream here: a hex prop while playing (or with a test
        /// source), a Collection alias while <see cref="Available"/>.</summary>
        internal static bool Streams(string id) =>
            HexProps.Has(id) ? PropRequester != null : Available && HexModels.CollectionAlias(id, out _, out _);

        static readonly List<WeakReference<HexCollectionFigures>> diagnosticOwners = new List<WeakReference<HexCollectionFigures>>();
        readonly HashSet<(string id, CollectionDetail detail)> originalKeys = new HashSet<(string, CollectionDetail)>();
        Func<int>? instanceCounter;
        internal void SetInstanceCounter(Func<int> count) { instanceCounter = count; }
        internal bool IsOriginalCached(string alias) => prefabs.TryGetValue(alias, out var prefab) && prefab != null;
        readonly HashSet<string> originalIds = new HashSet<string>(StringComparer.Ordinal);
        internal static IReadOnlyList<CollectionResidencyMetrics.Category> ResidencySnapshots()
        {
            var rows = new List<CollectionResidencyMetrics.Category>();
            diagnosticOwners.RemoveAll(w => !w.TryGetTarget(out var o) || o.disposed);
            foreach (var weak in diagnosticOwners) if (weak.TryGetTarget(out var owner))
                rows.Add(CollectionResidencyMetrics.Row("hex_board", owner.prefabs.Count, owner.instanceCounter?.Invoke() ?? 0, owner.originalIds, owner.leases.Count,
                    owner.prefabs.Count - owner.answered.Count, owner.prefabs.Values.Count(p => p != null), 0, 0,
                    owner.answered.Count(id => owner.prefabs[id] == null), owner.originalKeys.Select(k => k.id + "|" + k.detail)));
            return rows;
        }
        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> answered = new HashSet<string>(StringComparer.Ordinal);
        readonly List<IDisposable> leases = new List<IDisposable>();
        Action? changed;
        bool disposed;

        internal HexCollectionFigures(Action changed) { this.changed = changed; diagnosticOwners.Add(new WeakReference<HexCollectionFigures>(this)); }

        internal GameObject? Prefab(string id)
        {
            // A prop is asked for by its own id (one prefab, so any detail); a figure alias by its Collection id and level.
            string figure = id; var detail = CollectionDetail.Far;
            bool prop = HexProps.Has(id);
            if (prop ? PropRequester == null : !Available || !HexModels.CollectionAlias(id, out figure, out detail)) return HexModels.Prefab(id);
            if (!disposed && !prefabs.ContainsKey(id))
            {
                // The residency metrics' originals are the Collection's models only; a hex prop is pinned and not one of them.
                if (!prop) { originalIds.Add(figure); originalKeys.Add((figure, detail)); CollectionResidencyMetrics.MarkDirty(); }
                prefabs[id] = null; // before Request: a source is allowed to answer synchronously
                var weak = new WeakReference<HexCollectionFigures>(this);
                Action<GameObject?> answer = prefab =>
                {
                    if (!weak.TryGetTarget(out var owner) || owner.disposed || !owner.answered.Add(id)) return;
                    owner.prefabs[id] = prefab; CollectionResidencyMetrics.MarkDirty();
                    if (prefab != null) owner.changed?.Invoke();
                };
                if (prop) PropRequester!(figure, detail, answer);
                else if (Source != null) Source(figure, detail, answer);
                else leases.Add(Acquire(figure, detail, answer));
            }
            if (prefabs.TryGetValue(id, out var loaded) && loaded != null) return loaded;
            // Bypass HexModels' Editor alias and its permanent miss/proto cache. Stream owns the real prefab.
            return Resources.Load<GameObject>(HexModels.PathOf(id));
        }

        public void Dispose() => DisposeAfterDestroyed(Array.Empty<UnityEngine.Object>());
        internal void DisposeAfterDestroyed(IEnumerable<UnityEngine.Object> generatedConsumers)
        {
            if (disposed) return;
            disposed = true; changed = null; prefabs.Clear(); answered.Clear();
            CollectionResidencyMetrics.AfterDestroyed("hex_board", generatedConsumers, leases, null, null,
                originalKeys.ToArray());
            originalIds.Clear(); originalKeys.Clear(); instanceCounter = null; CollectionResidencyMetrics.MarkDirty();
            leases.Clear();
        }
    }
}
