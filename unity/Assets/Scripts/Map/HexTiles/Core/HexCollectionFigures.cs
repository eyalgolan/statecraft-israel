#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>A hex visual owner's borrowed streamed prefabs: the Collection figures its aliases draw, and the hex props
    /// (<see cref="HexProps"/>, from their own bundle). Requests only placed ids, once per owner, through the game's public
    /// streams; pending/failed requests keep their Resources or stand-in fallback. No bundle is owned here. Dispose on
    /// immediate board teardown as well as destruction: a late answer must not revive an old board.</summary>
    internal sealed class HexCollectionFigures : IDisposable
    {
        internal static Action<string, CollectionDetail, Action<GameObject?>>? Source, PropSource;
        static Action<string, CollectionDetail, Action<GameObject?>>? Requester =>
            Source ?? (Application.isPlaying ? CollectionStream.Request : (Action<string, CollectionDetail, Action<GameObject?>>?)null);
        static Action<string, CollectionDetail, Action<GameObject?>>? PropRequester =>
            PropSource ?? (Application.isPlaying ? (id, detail, done) => CollectionStream.Request(HexProps.Catalog, id, detail, done)
                : (Action<string, CollectionDetail, Action<GameObject?>>?)null);
        internal static bool Available => HexModels.DrawCollectionFigures && Requester != null;

        /// <summary>Whether <paramref name="id"/> is drawn from a stream here: a hex prop while playing (or with a test
        /// source), a Collection alias while <see cref="Available"/>.</summary>
        internal static bool Streams(string id) =>
            HexProps.Has(id) ? PropRequester != null : Available && HexModels.CollectionAlias(id, out _, out _);

        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> answered = new HashSet<string>(StringComparer.Ordinal);
        Action? changed;
        bool disposed;

        internal HexCollectionFigures(Action changed) { this.changed = changed; }

        internal GameObject? Prefab(string id)
        {
            // A prop is asked for by its own id (one prefab, so any detail); a figure alias by its Collection id and level.
            string figure = id; var detail = CollectionDetail.Far;
            var requester = HexProps.Has(id) ? PropRequester
                : Available && HexModels.CollectionAlias(id, out figure, out detail) ? Requester : null;
            if (requester == null) return HexModels.Prefab(id);
            if (!disposed && !prefabs.ContainsKey(id))
            {
                prefabs[id] = null; // before Request: a source is allowed to answer synchronously
                var weak = new WeakReference<HexCollectionFigures>(this);
                requester(figure, detail, prefab =>
                {
                    if (!weak.TryGetTarget(out var owner) || owner.disposed || !owner.answered.Add(id)) return;
                    owner.prefabs[id] = prefab;
                    if (prefab != null) owner.changed?.Invoke();
                });
            }
            if (prefabs.TryGetValue(id, out var loaded) && loaded != null) return loaded;
            // Bypass HexModels' Editor alias and its permanent miss/proto cache. Stream owns the real prefab.
            return Resources.Load<GameObject>(HexModels.PathOf(id));
        }

        public void Dispose() { disposed = true; changed = null; prefabs.Clear(); answered.Clear(); }
    }
}
