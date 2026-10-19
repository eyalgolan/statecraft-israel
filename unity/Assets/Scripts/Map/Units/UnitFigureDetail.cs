#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Kits;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The close-zoom detail of the tokens' Collection figures (issue #665, #663's rule: "full detail and the idle
    /// clip only at close zoom"). Each camera picks with the town models' switch and its hysteresis (TownModelLayer.CloseAt:
    /// close below CloseEnterM, far again beyond CloseExitM). Up close, each figure whose model has an idle clip
    /// (CollectionCatalog.Animated) shows its skinned near model, playing the clip, under the same holder as its far model, so
    /// the camera's yaw (#644) and the far model's height and feet carry over; far off, the far mesh draws and the clip stops.
    /// The near prefabs are asked for only once a camera first comes close, through the same stream, after the first frame.
    /// It fails open: until a near prefab arrives, and for good when it is missing or damaged, the far model stays. C05, the
    /// pioneer veteran, has no idle clip (walk and run only), so he keeps his still far model close up (owner, 18 October
    /// 2026, #665: "Keep its still model"). One per unit layer or showcase; nothing is allocated while the zoom band holds.</summary>
    [ExecuteAlways]
    public sealed class UnitFigureDetail : MonoBehaviour
    {
        /// <summary>The name of the near model's object, beside the far model under the figure's holder.</summary>
        public const string NearName = "Near";

        sealed class Figure
        {
            public Transform Holder = null!, Far = null!;
            public string Id = "";
            public Renderer[] FarRenderers = Array.Empty<Renderer>(), NearRenderers = Array.Empty<Renderer>();
            public Animator[] Animators = Array.Empty<Animator>();
        }

        readonly List<Figure> figures = new List<Figure>();
        readonly HashSet<string> nearAsked = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Each camera's last choice (by instance id), for the hysteresis.</summary>
        readonly Dictionary<int, bool> closeFor = new Dictionary<int, bool>();
        static readonly Predicate<Figure> Gone = f => f.Holder == null;
        static bool logged;

        /// <summary>True when the last camera to render was close (the near models show where they have arrived).</summary>
        public bool LastClose { get; private set; }

        /// <summary>The switch of the unit layer or showcase that <paramref name="piece"/> stands in (the root that owns its
        /// runtime assets, so it goes with the layer), made on first use.</summary>
        internal static UnitFigureDetail Of(Transform piece)
        {
            var assets = piece.GetComponentInParent<MapRuntimeAssets>(true);
            var root = assets != null ? assets.gameObject : piece.root.gameObject;
            return root.TryGetComponent<UnitFigureDetail>(out var detail) ? detail : root.AddComponent<UnitFigureDetail>();
        }

        /// <summary>A figure whose far model <paramref name="holder"/> now holds (its first child), with an idle clip.</summary>
        internal void Add(Transform holder, string id)
        {
            var far = holder.GetChild(0);
            var f = new Figure { Holder = holder, Far = far, Id = id, FarRenderers = far.GetComponentsInChildren<Renderer>(true) };
            figures.Add(f);
            if (LastClose) { Ask(id); Attach(f); }
            Show(f);
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Choose;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Choose;

        internal void Choose(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            int id = camera.GetInstanceID();
            bool close = TownModelLayer.CloseAt(TownModelLayer.ViewDistance(camera), closeFor.TryGetValue(id, out var was) ? was : (bool?)null);
            closeFor[id] = close;
            if (close == LastClose) return;
            LastClose = close;
            figures.RemoveAll(Gone);
            foreach (var f in figures) { if (close) { Ask(f.Id); Attach(f); } Show(f); }
        }

        void Ask(string id)
        {
            if (nearAsked.Add(id)) UnitFigures.RequestNear(id, () => Arrived(id));
        }

        void Arrived(string id)
        {
            if (this == null) return;   // the layer was rebuilt meanwhile
            figures.RemoveAll(Gone);
            foreach (var f in figures)
                if (f.Id == id) { Attach(f); Show(f); }
        }

        /// <summary>Puts the near model beside the far one, once it has arrived: as tall as the far model, centred over its
        /// feet, casting shadows as it does, with the probes off as on the far model.</summary>
        void Attach(Figure f)
        {
            if (f.NearRenderers.Length > 0 || f.Holder == null) return;
            var prefab = UnitFigures.Near(f.Id);
            if (prefab == null) return;
            var near = Instantiate(prefab, f.Holder, false).transform;   // the prefab's own root rotation (the FBX axis turn) kept
            near.name = NearName;
            if (UnitFigures.BoxIn(f.Holder, f.Far) is Bounds far && UnitFigures.BoxIn(f.Holder, near) is Bounds box && box.size.y > 1e-6f)
            {
                near.localScale *= far.size.y / box.size.y;
                if (UnitFigures.BoxIn(f.Holder, near) is Bounds fit)
                    near.localPosition += new Vector3(far.center.x - fit.center.x, far.min.y - fit.min.y, far.center.z - fit.center.z);
            }
            var like = f.FarRenderers.Length > 0 ? f.FarRenderers[0] : null;
            f.NearRenderers = near.GetComponentsInChildren<Renderer>(true);
            foreach (var r in f.NearRenderers)
            {
                if (like != null) { r.shadowCastingMode = like.shadowCastingMode; r.receiveShadows = like.receiveShadows; }
                r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            f.Animators = near.GetComponentsInChildren<Animator>(true);
            if (!logged && Application.isPlaying) { logged = true; Debug.Log("UNITS figures: the skinned near figures streamed in (close zoom)"); }
        }

        void Show(Figure f)
        {
            bool near = LastClose && f.NearRenderers.Length > 0;
            foreach (var r in f.FarRenderers) if (r != null) r.forceRenderingOff = near;
            foreach (var r in f.NearRenderers) if (r != null) r.forceRenderingOff = !near;
            foreach (var a in f.Animators) if (a != null) a.enabled = near;
        }
    }
}
