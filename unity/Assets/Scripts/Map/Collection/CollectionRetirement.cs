#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Retires acquired originals after their generated consumers are actually destroyed. This temporary host
    /// outlives a disabled/destroyed layer; reparenting or clearing a dictionary is not a destruction acknowledgement.</summary>
    public sealed class CollectionRetirement : MonoBehaviour
    {
        public static void AfterDestroyed(IEnumerable<UnityEngine.Object> instances, IEnumerable<IDisposable> owners) =>
            AfterDestroyed(instances, owners, null, null);

        public static void AfterDestroyed(IEnumerable<UnityEngine.Object> instances, IEnumerable<IDisposable> owners, Action? completed, Func<bool>? settled)
        {
            var leases = new List<IDisposable>(owners);
            // Preserve the original two-argument contract for consumers owning no imported source.
            if (leases.Count == 0 && completed == null) return;
            var pending = new List<UnityEngine.Object>(instances);
            if (leases.Count == 0 && pending.Count == 0) { completed?.Invoke(); return; }
            if (!Application.isPlaying)
            {
                // EditMode callers use DestroyImmediate. Fail closed rather than unloading assets of a live consumer.
                foreach (var instance in pending)
                    if (instance != null) throw new InvalidOperationException("Original consumer still exists during EditMode retirement.");
                foreach (var lease in leases) lease.Dispose();
                if (settled != null && !settled()) throw new InvalidOperationException("Retirement is not settled in EditMode.");
                completed?.Invoke(); return;
            }
            var go = new GameObject("Collection original retirement") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<CollectionRetirement>().StartCoroutine(Retire(pending, leases, go, completed, settled));
        }

        static IEnumerator Retire(List<UnityEngine.Object> pending, List<IDisposable> leases, GameObject host, Action? completed, Func<bool>? settled)
        {
            // Even an instanced-mesh consumer needs its last submitted frame to finish before source unload.
            yield return null;
            while (pending.Exists(instance => instance != null)) yield return null;
            pending.Clear();
            foreach (var lease in leases) lease.Dispose();
            leases.Clear();
            // Let the stream drain deferred eviction and cancelled/in-flight work before re-admission.
            yield return null;
            while (settled != null && !settled()) yield return null;
            try { completed?.Invoke(); } finally { Destroy(host); }
        }
    }
}
