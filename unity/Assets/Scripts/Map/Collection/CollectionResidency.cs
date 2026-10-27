#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Validated immutable presentation category policy.</summary>
    internal sealed class CollectionResidencyPolicy
    {
        internal readonly float EnterM, ExitM, ViewEnterM, ViewExitM;
        internal readonly int CameraGraceFrames;
        internal readonly string Evidence;
        public CollectionResidencyPolicy(float enterM, float exitM, float viewEnterM, float viewExitM, int cameraGraceFrames, string evidence)
        {
            if (!float.IsFinite(enterM) || !float.IsFinite(exitM) || !float.IsFinite(viewEnterM) || !float.IsFinite(viewExitM)
                || enterM < 0 || exitM < enterM || viewEnterM < 0 || viewExitM < viewEnterM || cameraGraceFrames < 0
                || string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Explicit finite residency policy and evidence required.");
            EnterM = enterM; ExitM = exitM; ViewEnterM = viewEnterM; ViewExitM = viewExitM;
            CameraGraceFrames = cameraGraceFrames; Evidence = evidence;
        }
    }

    /// <summary>Category-owned demand. Leave invalidates callbacks, retires real consumers, then admits latest demand.</summary>
    internal sealed class CollectionResidency : IDisposable
    {
        internal delegate void Retire(IReadOnlyList<UnityEngine.Object> instances, IReadOnlyList<IDisposable> owners,
            IReadOnlyList<(string id, CollectionDetail detail)> keys, Action complete);
        readonly Func<string, CollectionDetail, Action<GameObject?>, IDisposable> acquire;
        readonly Func<IReadOnlyList<UnityEngine.Object>> destroyAndClear;
        readonly Action<string, CollectionDetail, GameObject?> answer;
        readonly Retire retire;
        readonly List<IDisposable> owners = new List<IDisposable>();
        HashSet<(string id, CollectionDetail detail)> active = new HashSet<(string, CollectionDetail)>(),
            latest = new HashSet<(string, CollectionDetail)>();
        int generation;
        bool retiring, disposed;
        readonly HashSet<(string id, CollectionDetail detail)> answered = new HashSet<(string, CollectionDetail)>();
        internal int PendingCount => active.Count(k => !answered.Contains(k));
        internal int OwnerCount => owners.Count;
        internal bool IsRetiring => retiring;
        public CollectionResidency(Func<string, CollectionDetail, Action<GameObject?>, IDisposable> acquire,
            Func<IReadOnlyList<UnityEngine.Object>> destroyAndClear, Action<string, CollectionDetail, GameObject?> answer, Retire retire)
        { this.acquire = acquire; this.destroyAndClear = destroyAndClear; this.answer = answer; this.retire = retire; }

        internal void Replace(IEnumerable<(string id, CollectionDetail detail)> demand)
        {
            if (disposed) return;
            latest = new HashSet<(string, CollectionDetail)>(demand); CollectionResidencyMetrics.MarkDirty();
            if (retiring || active.SetEquals(latest)) return;
            // Adding a near alias does not retire a still-needed far consumer.
            if (active.IsSubsetOf(latest))
            {
                var added = latest.Except(active).ToArray();
                active = new HashSet<(string, CollectionDetail)>(latest);
                AcquireKeys(added); return;
            }
            Leave();
        }
        void Leave()
        {
            generation++; // No old answer may rebuild the departed view.
            var oldKeys = active.ToArray(); active.Clear(); answered.Clear();
            var oldOwners = owners.ToArray(); owners.Clear();
            var instances = destroyAndClear(); // Must restore fallback/destroy and clear all prefab/renderer references first.
            retiring = true;
            retire(instances, oldOwners, oldKeys, () =>
            {
                retiring = false;
                if (!disposed) Admit();
            });
        }
        void Admit()
        {
            active = new HashSet<(string, CollectionDetail)>(latest);
            AcquireKeys(active);
        }
        void AcquireKeys(IEnumerable<(string id, CollectionDetail detail)> keys)
        {
            int version = generation;
            foreach (var key in keys.OrderBy(k => k.id, StringComparer.Ordinal).ThenBy(k => k.detail).ToArray())
            {
                if (disposed || version != generation) return;
                var lease = acquire(key.id, key.detail, prefab =>
                {
                    if (!disposed && version == generation && active.Contains(key)) { answered.Add(key); answer(key.id, key.detail, prefab); CollectionResidencyMetrics.MarkDirty(); }
                });
                // Synchronous fake/editor answers may tear down the owner.
                if (disposed || version != generation) lease.Dispose(); else owners.Add(lease);
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; latest.Clear();
            if (!retiring) Leave(); else generation++;
        }
    }

    /// <summary>Union of game cameras, pruned on frame boundaries. All distances/grace come from explicit input.</summary>
    internal sealed class CollectionResidencyView
    {
        sealed class Choice
        {
            internal Camera Camera = null!;
            internal int Frame;
            internal bool Visible;
            internal HashSet<string> Keys = new HashSet<string>(StringComparer.Ordinal);
        }
        readonly CollectionResidencyPolicy policy;
        readonly Dictionary<int, Choice> cameras = new Dictionary<int, Choice>();
        internal HashSet<string> Keys => new HashSet<string>(cameras.Values.SelectMany(c => c.Keys), StringComparer.Ordinal);
        internal CollectionResidencyView(CollectionResidencyPolicy policy) { this.policy = policy; }
        internal void Clear() => cameras.Clear();
        // Unproject in camera space: subtracting near/far WORLD points at hundreds of kilometres loses
        // direction precision, moving a 100km ground focus kilometres off its actual footprint.
        internal static bool TryGroundFocus(Camera camera, out Vector3 focus)
        {
            var inverse = camera.projectionMatrix.inverse;
            Vector3 near = inverse.MultiplyPoint(new Vector3(0, 0, -1));
            Vector3 far = inverse.MultiplyPoint(new Vector3(0, 0, 1));
            var direction = camera.cameraToWorldMatrix.MultiplyVector(far - near);
            var origin = camera.cameraToWorldMatrix.MultiplyPoint(near);
            focus = Vector3.zero;
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.z)
                || !float.IsFinite(origin.x) || !float.IsFinite(origin.y) || !float.IsFinite(origin.z)
                || direction.sqrMagnitude <= 0) return false;
            var ray = new Ray(origin, direction);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return false;
            focus = ray.GetPoint(distance);
            return float.IsFinite(focus.x) && float.IsFinite(focus.y) && float.IsFinite(focus.z);
        }

        internal bool Prune(int frame)
        {
            var gone = cameras.Where(p => p.Value.Camera == null || !p.Value.Camera.isActiveAndEnabled
                || frame - p.Value.Frame > policy.CameraGraceFrames).Select(p => p.Key).ToArray();
            foreach (int id in gone) cameras.Remove(id);
            return gone.Length != 0;
        }
        internal void Observe(Camera camera, IEnumerable<(string key, Vector3 at, float radius)> anchors, int frame)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            Prune(frame);
            int id = camera.GetInstanceID();
            if (!cameras.TryGetValue(id, out var choice)) cameras[id] = choice = new Choice { Camera = camera };
            choice.Frame = frame;
            float height = Kits.TownModelLayer.ViewDistance(camera);
            choice.Visible = height <= (choice.Visible ? policy.ViewExitM : policy.ViewEnterM);
            var old = choice.Keys; choice.Keys = new HashSet<string>(StringComparer.Ordinal);
            if (!choice.Visible) return;
            if (!TryGroundFocus(camera, out var focus)) return;
            foreach (var (key, at, extent) in anchors)
            {
                float dx = focus.x - at.x, dz = focus.z - at.z;
                float radius = (old.Contains(key) ? policy.ExitM : policy.EnterM) + Mathf.Max(0, extent);
                if (dx * dx + dz * dz <= radius * radius) choice.Keys.Add(key);
            }
        }
    }
}
