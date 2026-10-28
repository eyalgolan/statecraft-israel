#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Tal Heuberger's khaki canvas tent (owner, 27 October 2026, #756: "Sign off all 19"; the prepared role in
    /// docs/generated-buildings-props-mapping.json, "Existing non-white transit-camp ridge tents") in place of a transit camp's
    /// khaki ridge tents, up close. The camp kit builds those tents into a mesh of their own (SettlementKits.MaabarotSplit);
    /// this component sits on that mesh's object, asks the collection stream for the model after the first frame, and once it
    /// has arrived, for each camera, draws one model per tent below the town models' switch distance
    /// (TownModelLayer.CloseEnterM / CloseExitM) with the kit's khaki tents switched off, and the kit's tents beyond it.
    /// It fails open: until the model arrives, and for ever when its bundle is missing or damaged, the kit's tents stay.</summary>
    [ExecuteAlways]
    public sealed class CampTentModels : MonoBehaviour
    {
        public const string Model = CollectionCatalog.KhakiTent;

        /// <summary>Test seam (internal; the tests see it through InternalsVisibleTo).</summary>
        internal static Action<string, CollectionDetail, Action<GameObject?>> Request = CollectionStream.Request;

        readonly List<Renderer> models = new List<Renderer>();
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        IReadOnlyList<SettlementKits.KhakiTent> tents = Array.Empty<SettlementKits.KhakiTent>();
        Renderer? kit;
        bool asked;

        /// <summary>Whether the model has arrived and its instances stand (false: the kit's tents are the only ones drawn).</summary>
        public bool HasModels => models.Count > 0;
        /// <summary>The model instances, one per khaki tent (empty until the model arrives).</summary>
        public int ModelCount => models.Count;
        public bool LastClose { get; private set; }

        /// <summary>Attaches to <paramref name="khakiKit"/>, the camp's khaki tents' kit object, and asks for the model.</summary>
        public static CampTentModels Attach(GameObject khakiKit, IReadOnlyList<SettlementKits.KhakiTent> tents)
        {
            var layer = khakiKit.AddComponent<CampTentModels>();
            layer.kit = khakiKit.GetComponent<Renderer>();
            layer.tents = tents;
            layer.Ask();
            return layer;
        }

        void Ask()
        {
            if (asked || tents.Count == 0) return;
            asked = true;
            Request(Model, CollectionDetail.Near, prefab =>
            {
                if (this == null) return;
                if (prefab == null) { Debug.LogWarning($"CAMPS: {Model} did not stream; the kit's khaki tents stay."); return; }
                Place(prefab);
            });
        }

        void Place(GameObject prefab)
        {
            foreach (var t in tents)
            {
                var go = Instantiate(prefab, transform, false); go.name = Model;
                // The model's long side lies along the prefab's z once it stands up; the kit's ridge runs along the tent's
                // length at its yaw (counter-clockwise in x-z), so a turn of 90 - yaw about y lays it along the ridge.
                go.transform.localRotation = Quaternion.Euler(0f, 90f - t.Yaw, 0f) * prefab.transform.localRotation;
                var b = Bounds(go); float edge = Mathf.Max(b.size.x, b.size.z);
                if (edge > 1e-4f) go.transform.localScale *= t.W / edge;
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
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        /// <summary>The per-camera switch: the models up close once they have arrived, the kit's tents otherwise.</summary>
        internal void Choose(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            int id = cam.GetInstanceID();
            bool close = SpecialBuildingModels.CloseAt(TownLandmarks.ViewDistance(cam), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close; LastClose = close;
            bool show = HasModels && close;
            if (kit != null) kit.forceRenderingOff = show;
            foreach (var r in models) if (r != null) r.forceRenderingOff = !show;
        }
    }
}
