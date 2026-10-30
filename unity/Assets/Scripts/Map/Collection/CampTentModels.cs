#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's khaki canvas tent (owner, 27 October 2026, #756: "Sign off all 19"; the prepared role in
    /// docs/generated-buildings-props-mapping.json, "Existing non-white transit-camp ridge tents") in place of a transit camp's
    /// khaki ridge tents, up close. The camp kit builds those tents into a mesh of their own (SettlementKits.MaabarotSplit);
    /// this component sits on that mesh's object. It owns the model the way the single-building models do (#1273's residency,
    /// the special-buildings policy): a game camera close to the camp admits it, and the model is acquired from the collection
    /// stream; when no camera is close any more, and on disable or destroy, its instances are destroyed and the lease is
    /// released, so the bundle can unload. While the model stands, each camera draws one model per tent below the town models'
    /// switch distance (TownModelLayer.CloseEnterM / CloseExitM) with the kit's khaki tents switched off, and the kit's tents
    /// beyond it. It fails open: until the model arrives, and for ever when its bundle is missing or damaged, the kit's tents
    /// stay.</summary>
    [ExecuteAlways]
    public sealed class CampTentModels : MonoBehaviour
    {
        public const string Model = CollectionCatalog.KhakiTent;
        const string Key = "camp";

        static readonly Func<string, CollectionDetail, Action<GameObject?>, IDisposable> DefaultAcquire = CollectionStream.Acquire;
        /// <summary>Test seam: owned acquisition (internal; the tests see it through InternalsVisibleTo).</summary>
        internal static Func<string, CollectionDetail, Action<GameObject?>, IDisposable> Acquire = DefaultAcquire;

        readonly List<Renderer> models = new List<Renderer>();
        readonly List<GameObject> instances = new List<GameObject>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        IReadOnlyList<SettlementKits.KhakiTent> tents = Array.Empty<SettlementKits.KhakiTent>();
        Renderer? kit;
        CollectionResidencyView? view;
        CollectionResidency? residency;
        Vector3 centre;
        float extent;
        bool dirty;

        /// <summary>Whether the model has arrived and its instances stand (false: the kit's tents are the only ones drawn).</summary>
        public bool HasModels => instances.Count > 0;
        /// <summary>The model instances, one per khaki tent (empty until the model arrives, and again after leave).</summary>
        public int ModelCount => instances.Count;
        /// <summary>The model instances (test seam: the tests check they are destroyed before the bundle unloads).</summary>
        internal IReadOnlyList<GameObject> Instances => instances;
        /// <summary>Whether a game camera is close to this camp now, so the model is demanded and its lease held.</summary>
        public bool Demanded => view != null && view.Keys.Count > 0;
        public bool LastClose { get; private set; }
        internal readonly struct CameraDiagnostics
        {
            internal readonly int ObservedCameras;
            internal CameraDiagnostics(int observedCameras) { ObservedCameras = observedCameras; }
        }
        internal CameraDiagnostics DiagnosticsSnapshot => new CameraDiagnostics(closeFor.Count);
        bool missing;

        /// <summary>This camp's counters for the residency diagnostics, in the special-buildings category whose policy it uses.</summary>
        internal CollectionResidencyMetrics.Category ResidencySnapshot()
        {
            var ids = Demanded || instances.Count > 0 ? new[] { Model } : Array.Empty<string>();
            return CollectionResidencyMetrics.Row("special", isActiveAndEnabled && Demanded ? 1 : 0, instances.Count, ids,
                residency?.OwnerCount ?? 0, residency?.PendingCount ?? 0, 0, models.Count, 0, missing ? 1 : 0, ids.Select(id => id + "|Near"));
        }

        /// <summary>Attaches to <paramref name="khakiKit"/>, the camp's khaki tents' kit object. Nothing is asked for until a
        /// game camera comes close to the camp (<paramref name="policy"/>).</summary>
        internal static CampTentModels Attach(GameObject khakiKit, IReadOnlyList<SettlementKits.KhakiTent> tents, CollectionResidencyPolicy policy)
        {
            var layer = khakiKit.AddComponent<CampTentModels>();
            layer.kit = khakiKit.GetComponent<Renderer>();
            layer.tents = tents;
            layer.view = new CollectionResidencyView(policy);
            if (tents.Count > 0)
            {
                // The camp's anchor: its tents' centre, reaching out to its farthest tent's end (the kit's vertices are in world space).
                var c = new Vector2(tents.Average(t => t.C.x), tents.Average(t => t.C.y));
                layer.centre = new Vector3(c.x, 0f, c.y);
                layer.extent = tents.Max(t => (t.C - c).magnitude + 0.5f * t.W);
            }
            return layer;
        }

        void Refresh()
        {
            if (view == null || !isActiveAndEnabled || tents.Count == 0) return;
            residency ??= new CollectionResidency(Acquire, ClearForRetirement, (id, detail, prefab) =>
                {
                    if (prefab == null) { missing = true; Debug.LogWarning($"CAMPS: {Model} did not stream; the kit's khaki tents stay."); return; }
                    missing = false; Place(prefab); CollectionResidencyMetrics.MarkDirty();
                },
                (instances, owners, keys, complete) => CollectionResidencyMetrics.AfterDestroyed("special", instances, owners, complete,
                    Application.isPlaying && Acquire == DefaultAcquire ? () => CollectionStream.Settled(keys) : (Func<bool>?)null, keys));
            residency.Replace(Demanded ? new[] { (Model, CollectionDetail.Near) } : Array.Empty<(string, CollectionDetail)>());
        }

        /// <summary>Leave: the model's instances go, the kit's tents come back, and every reference to the prefab is dropped.</summary>
        IReadOnlyList<UnityEngine.Object> ClearForRetirement()
        {
            var gone = instances.Where(go => go != null).ToArray();
            foreach (var go in gone)
                if (Application.isPlaying)
                {
                    // Reparenting during a parent's deactivation is forbidden; inactive models are already hidden.
                    if (gameObject.activeInHierarchy) go.transform.SetParent(null);
                    Destroy(go);
                }
                else DestroyImmediate(go);
            instances.Clear(); models.Clear(); closeFor.Clear(); missing = false;
            if (kit != null) kit.forceRenderingOff = false;
            CollectionResidencyMetrics.MarkDirty();
            return gone;
        }

        void Place(GameObject prefab)
        {
            foreach (var t in tents)
            {
                var go = Instantiate(prefab, transform, false); go.name = Model; instances.Add(go);
                // A streamed prefab may be stored inactive. Only its instance must be activated: otherwise HasModels is true
                // and the fallback kit is hidden even though the replacement tent cannot draw. It is activated before it is
                // measured because an inactive renderer reports empty bounds; the whole placement runs before the next frame.
                go.SetActive(true);
                // Sized in the model's own frame first, so its size does not depend on the yaw: its long side seen from above
                // becomes the kit tent's length.
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                if (edge > 1e-4f) go.transform.localScale *= t.W / edge;
                // The model's long side lies along the prefab's z once it stands up; the kit's ridge runs along the tent's
                // length at its yaw (counter-clockwise in x-z), so a turn of 90 - yaw about y lays it along the ridge.
                go.transform.localRotation = Quaternion.Euler(0f, 90f - t.Yaw, 0f) * prefab.transform.localRotation;
                b = Bounds(go);
                go.transform.position += new Vector3(t.C.x - b.center.x, t.Y - b.min.y, t.C.y - b.center.z);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.On; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    r.forceRenderingOff = !LastClose; models.Add(r);
                }
            }
            if (kit != null) kit.forceRenderingOff = LastClose;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Choose;
            closeFor.Clear(); LastClose = false;
            view?.Clear();
            residency?.Replace(Array.Empty<(string, CollectionDetail)>());
        }
        void OnDestroy() { residency?.Dispose(); residency = null; }
        void LateUpdate() => AdvanceCameraFrame(Time.frameCount);
        internal void AdvanceCameraFrame(int frame)
        {
            if (view != null && view.Prune(frame)) dirty = true;
            foreach (int id in closeFor.Keys.Where(id => !view!.Observes(id)).ToArray()) closeFor.Remove(id);
            if (dirty) { dirty = false; Refresh(); }
        }

        /// <summary>The per-camera step: the camera's demand for the model (residency), then the switch, the models up close
        /// once they have arrived, the kit's tents otherwise.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection || view == null) return;
            bool before = Demanded;
            view.Observe(cam, new[] { (Key, centre, extent) }, Time.frameCount);
            if (before != Demanded) { if (Application.isPlaying) dirty = true; else Refresh(); }
            int id = cam.GetInstanceID();
            bool close = SpecialBuildingModels.CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            bool show = HasModels && close;
            if (kit != null) kit.forceRenderingOff = show;
            foreach (var r in models) if (r != null) r.forceRenderingOff = !show;
        }
    }
}
