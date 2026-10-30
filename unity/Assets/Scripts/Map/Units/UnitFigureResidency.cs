#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Per-consumer ownership: one live token holder, with imported children retired before its original leases.
    /// A roster holder keeps Far once acquired while its token object lives, including while an ancestor is inactive. Destroying the token
    /// releases Far in <see cref="OnDestroy"/>; disabling this component explicitly releases it in <see cref="OnDisable"/>. Disabling the
    /// component while its GameObject is already inactive does not invoke OnDisable, so that Far lease remains until destruction.</summary>
    [ExecuteAlways]
    public sealed class UnitFigureResidency : MonoBehaviour
    {
        string id = "";
        CollectionResidency? residency, retainedFarResidency, retainedNearResidency;
        CollectionResidencyView? view;
        bool nearWanted, initialized, draws = true, retainFar;
        Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? acquire;
        internal void Initialize(string model, CollectionResidencyPolicy policy,
            Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? source, bool draw = true, bool retainFar = false,
            CollectionResidency.Retire? retirement = null)
        {
            if (initialized) { if (model != id) throw new InvalidOperationException("A live holder cannot change its original id."); return; }
            initialized = true; draws = draw; this.retainFar = retainFar; id = model;
            view = new CollectionResidencyView(policy); acquire = source;
            CollectionResidency Make(Func<IReadOnlyList<UnityEngine.Object>> clear) => new CollectionResidency(Acquire, clear, Answer,
                retirement ?? ((instances, owners, keys, complete) => CollectionResidencyMetrics.AfterDestroyed("units", instances, owners, complete,
                    Application.isPlaying && acquire == null ? () => CollectionStream.Settled(keys) : (Func<bool>?)null, keys)));
            if (retainFar)
            {
                retainedFarResidency = Make(Clear);
                retainedNearResidency = Make(ClearNear);
            }
            else residency = Make(Clear);
            // The already-visible holder is its first demand. Roster holders keep Far for their whole lifetime; camera
            // observations subsequently bound only Near. Other consumers retain the original spatial residency behaviour.
            if (isActiveAndEnabled)
            {
                if (retainFar) Refresh(false);
                else if (!Application.isPlaying) Refresh(true);
            }
        }
        IDisposable Acquire(string model, CollectionDetail detail, Action<GameObject?> answer) =>
            UnitFigures.AcquireOwned(model, detail, answer, acquire);
        void Answer(string model, CollectionDetail detail, GameObject? prefab)
        {
            UnitFigures.AnswerOwned(model, detail, prefab);
            // A gate answer can synchronously rebuild the layer and destroy this draw:false prefetch holder.
            if (!draws) return;
            if (detail == CollectionDetail.Far && prefab != null) UnitFigures.Swap(transform, model, prefab);
            if (detail == CollectionDetail.Near) UnitFigureDetail.Of(transform).OwnedArrived(model);
            // A holder dressed below an inactive layer acquires its cached Far only when the layer is reactivated. Mark
            // that handoff for UnitLayer.LateUpdate: never destroy the gate holder inside this answer/lifecycle stack.
            if (detail == CollectionDetail.Far && ActiveAnsweredFar)
                GetComponentInParent<UnitLayer>()?.ScheduleSatisfiedPrefetchRetirement(model);
        }
        IEnumerable<(string id, CollectionDetail detail)> ActiveKeys() =>
            (residency?.ActiveKeys ?? Array.Empty<(string, CollectionDetail)>())
                .Concat(retainedFarResidency?.ActiveKeys ?? Array.Empty<(string, CollectionDetail)>())
                .Concat(retainedNearResidency?.ActiveKeys ?? Array.Empty<(string, CollectionDetail)>());
        /// <summary>The ownership snapshot. Its demanded anchor is any active retained/detail key, including a retained Far
        /// that is not currently camera-demanded; <see cref="CameraResident"/> reports camera demand separately.</summary>
        internal CollectionResidencyMetrics.Category ResidencySnapshot()
        {
            var keys = ActiveKeys().Distinct().ToArray();
            return CollectionResidencyMetrics.Row("units", keys.Length == 0 ? 0 : 1,
                transform.Find(UnitFigures.ModelName)?.childCount ?? 0, keys.Select(key => key.id).Distinct(StringComparer.Ordinal),
                (residency?.OwnerCount ?? 0) + (retainedFarResidency?.OwnerCount ?? 0) + (retainedNearResidency?.OwnerCount ?? 0),
                (residency?.PendingCount ?? 0) + (retainedFarResidency?.PendingCount ?? 0) + (retainedNearResidency?.PendingCount ?? 0),
                0, 0, 0, 0, keys.Select(key => key.id + "|" + key.detail));
        }
        internal bool CameraResident => observed && view != null && view.Keys.Contains(id);
        internal string ModelId => id;
        /// <summary>True only after this live holder has acquired and answered its retained Far key. An inactive holder cannot
        /// complete a roster-gate handoff; deactivation also excludes an object during its deferred-destroy frame.</summary>
        internal bool ActiveAnsweredFar => isActiveAndEnabled && UnitFigures.Arrived(id)
            && retainedFarResidency?.ActiveAndAnswered((id, CollectionDetail.Far)) == true;
        void Refresh(bool nearAdmitted, bool releaseFar = false)
        {
            if (retainFar)
            {
                retainedNearResidency?.Replace(isActiveAndEnabled && nearAdmitted && nearWanted
                    ? new[] { (id, CollectionDetail.Near) } : Array.Empty<(string, CollectionDetail)>());
                retainedFarResidency?.Replace(releaseFar ? Array.Empty<(string, CollectionDetail)>() : new[] { (id, CollectionDetail.Far) });
                return;
            }
            if (residency == null) return;
            var keys = new List<(string, CollectionDetail)>();
            if (isActiveAndEnabled && nearAdmitted) keys.Add((id, CollectionDetail.Far));
            if (isActiveAndEnabled && nearAdmitted && nearWanted) keys.Add((id, CollectionDetail.Near));
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
            if (this == null || !initialized || view == null || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
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
                UnitFigures.GeometryChanged();
            }
            // The mesh, plate, badge, flag and every other child are owned by the token, never by this source lease.
            if (TryGetComponent<MeshRenderer>(out var code)) code.enabled = true;
            return gone;
        }
        IReadOnlyList<UnityEngine.Object> ClearNear() => UnitFigureDetail.Of(transform).RetireNear(transform);
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += Choose;
            if (initialized)
            {
                observed = false;
                if (retainFar) Refresh(false);
                else if (!Application.isPlaying) Refresh(true);
            }
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Choose;
            // GameObject deactivation keeps a roster holder's Far lease until the object is destroyed, so a hidden or
            // deferred-destroy token cannot open a source-ownership gap. Disabling the component is an explicit release.
            refreshPending = false; view?.Clear(); Refresh(false, releaseFar: !enabled);
        }
        void OnDestroy()
        {
            residency?.Dispose(); retainedNearResidency?.Dispose(); retainedFarResidency?.Dispose();
            residency = retainedNearResidency = retainedFarResidency = null;
        }
    }
}
