#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;

namespace Statecraft.HexTiles
{
    /// <summary>A hex visual owner's borrowed Collection prefabs. Requests only placed aliases, once per owner, through
    /// the game's public stream; pending/failed requests keep their Resources or stand-in fallback. No bundle is owned
    /// here. Dispose on immediate board teardown as well as destruction: a late answer must not revive an old board.</summary>
    internal sealed class HexCollectionFigures : IDisposable
    {
        internal static Action<string, CollectionDetail, Action<GameObject?>>? Source;
        static Action<string, CollectionDetail, Action<GameObject?>>? Requester =>
            Source ?? (Application.isPlaying ? CollectionStream.Request : (Action<string, CollectionDetail, Action<GameObject?>>?)null);
        internal static bool Available => (HexModels.DrawCollectionFigures || HexModels.DrawTownModels) && Requester != null;

        readonly Dictionary<string, GameObject?> prefabs = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> answered = new HashSet<string>(StringComparer.Ordinal);
        Action? changed;
        bool disposed;

        internal HexCollectionFigures(Action changed) { this.changed = changed; }

        internal GameObject? Prefab(string id)
        {
            if (!Available || !HexModels.CollectionAlias(id, out var figure, out var detail)) return HexModels.Prefab(id);
            if (!disposed && !prefabs.ContainsKey(id))
            {
                prefabs[id] = null; // before Request: a source is allowed to answer synchronously
                var weak = new WeakReference<HexCollectionFigures>(this);
                Requester!(figure, detail, prefab =>
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
