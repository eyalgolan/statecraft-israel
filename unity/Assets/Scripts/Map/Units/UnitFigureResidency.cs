#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Per-consumer ownership: one live token holder, with imported children retired before its original leases.</summary>
    [ExecuteAlways]
    public sealed class UnitFigureResidency : MonoBehaviour
    {
        string id = "";
        CollectionResidency? residency;
        CollectionResidencyView? view;
        bool nearWanted, initialized, draws = true;
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? acquire;
        internal void Initialize(string model, CollectionResidencyPolicy policy,
            Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? source, bool draw = true)
        {
            if (initialized) { if (model != id) throw new InvalidOperationException("A live holder cannot change its original id."); return; }
            initialized = true; draws = draw; id = model; view = new CollectionResidencyView(policy); acquire = source;
            residency = new CollectionResidency(Acquire, Clear, Answer,
                (instances, owners, keys, complete) => CollectionResidencyMetrics.AfterDestroyed("units", instances, owners, complete,
                    Application.isPlaying && acquire == null ? () => CollectionStream.Settled(keys) : (Func<bool>?)null, keys));
            // The already-visible live token is its first demand. Camera observations subsequently bound spatial residency.
            if (isActiveAndEnabled && !Application.isPlaying) Refresh(true);
        }
        IDisposable Acquire(string model, CollectionDetail detail, Action<GameObject?> answer) =>
            UnitFigures.AcquireOwned(model, detail, answer, acquire);
        void Answer(string model, CollectionDetail detail, GameObject? prefab)
        {
            UnitFigures.AnswerOwned(model, detail, prefab);
            if (!draws) return;
            if (detail == CollectionDetail.Far && prefab != null) UnitFigures.Swap(transform, model, prefab);
            if (detail == CollectionDetail.Near) UnitFigureDetail.Of(transform).OwnedArrived(model);
        }
        internal CollectionResidencyMetrics.Category ResidencySnapshot() => CollectionResidencyMetrics.Row("units",
            initialized && isActiveAndEnabled && (!observed || view!.Keys.Contains(id)) ? 1 : 0,
            transform.Find(UnitFigures.ModelName)?.childCount ?? 0, initialized ? new[] { id } : Array.Empty<string>(),
            residency?.OwnerCount ?? 0, residency?.PendingCount ?? 0, 0, 0, 0, 0, nearWanted ? new[] { id + "|Far", id + "|Near" } : new[] { id + "|Far" });
        void Refresh(bool admitted)
        {
            if (residency == null) return;
            var keys = new List<(string, CollectionDetail)>();
            if (admitted) { keys.Add((id, CollectionDetail.Far)); if (nearWanted) keys.Add((id, CollectionDetail.Near)); }
            residency.Replace(keys);
        }
        bool refreshPending, pendingAdmitted;
        void Schedule(bool admitted)
        { if (Application.isPlaying) { refreshPending = true; pendingAdmitted = admitted; } else Refresh(admitted); }
        internal void RequestNear()
        { nearWanted = true; if (isActiveAndEnabled) Schedule(view == null || !observed || view.Keys.Contains(id)); }
        bool observed;
        internal void Choose(ScriptableRenderContext _, Camera camera)
        {
            if (!initialized || view == null || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            bool first = !observed; observed = true; var before = view.Keys;
            view.Observe(camera, new[] { (id, transform.position, 0f) }, Time.frameCount);
            if (first || !before.SetEquals(view.Keys)) Schedule(view.Keys.Contains(id));
        }
        void LateUpdate()
        {
            if (initialized && observed && view != null && view.Prune(Time.frameCount)) Schedule(view.Keys.Contains(id));
            if (refreshPending) { refreshPending = false; Refresh(pendingAdmitted); }
        }
        IReadOnlyList<UnityEngine.Object> Clear()
        {
            var gone = new List<UnityEngine.Object>();
            var holder = transform.Find(UnitFigures.ModelName);
            if (holder != null)
            {
                UnitFigureDetail.Of(transform).Remove(holder);
                var original = holder.gameObject; gone.Add(original);
                if (Application.isPlaying) { holder.SetParent(null); Destroy(original); } else DestroyImmediate(original);
            }
            // The mesh, plate, badge, flag and every other child are owned by the token, never by this source lease.
            if (TryGetComponent<MeshRenderer>(out var code)) code.enabled = true;
            return gone;
        }
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += Choose;
            if (initialized) { observed = false; if (!Application.isPlaying) Refresh(true); }
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Choose;
            refreshPending = false; view?.Clear(); Refresh(false);
        }
        void OnDestroy() { residency?.Dispose(); residency = null; }
    }
}
