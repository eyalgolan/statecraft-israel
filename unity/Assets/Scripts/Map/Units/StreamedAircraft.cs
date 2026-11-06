#nullable enable
using System;
using System.Linq;
using Statecraft.Game.Map.Collection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The 101 Squadron Spitfire's Blender-built model (tools/spitfire_101_model.py; CollectionCatalog.IsraeliSpitfire),
    /// streamed in a bundle of its own after the first frame over the code-built Spitfire (#2734: never under Resources, so not
    /// in the WebGL player's first download). It sits on an aircraft holder (the showcase's Spitfire1948, a 101 Squadron
    /// token's Aircraft) whose code-built airframe and propeller are built first and drawn until the model arrives. Then the
    /// model stands under the holder in the code-built model's frame (real metres, nose +Z, its centre of gravity at the
    /// origin), its renderers cast shadows as the code-built airframe's do, the code-built airframe and propeller stop drawing,
    /// and the holder's flight (<see cref="ShowcaseFlight"/>) turns the model's own <see cref="PropellerName"/>. It fails open:
    /// an answer of null (a missing or damaged bundle), a model with no <see cref="PropellerName"/> child, and edit mode with no
    /// <see cref="Source"/> keep the whole code-built aircraft. The model is asked for once the holder is first active (so a
    /// token built under a hidden layer asks when it is shown), its lease is released when the holder is destroyed, and an
    /// answer after that is ignored. The model's meshes and materials are its bundle's: nothing here owns or frees them
    /// (MapRuntimeAssets owns only what the code builds).</summary>
    [ExecuteAlways]
    public sealed class StreamedAircraft : MonoBehaviour
    {
        public const string Model = CollectionCatalog.IsraeliSpitfire;
        /// <summary>The streamed model's propeller, a direct child of its root (the FBX's top-level node at the hub, turning
        /// about its local +Z), and the name of the model's instance under the holder.</summary>
        public const string PropellerName = "Propeller", ModelName = "StreamedModel";

        /// <summary>How the game asks for the model: the Collection stream's owned lease (not Request, which pins the bundle for good).</summary>
        internal static readonly Func<string, CollectionDetail, Action<GameObject?>, IDisposable> ProductionAcquire = CollectionStream.Acquire;

        /// <summary>A stand-in for the stream; null is the game's (<see cref="ProductionAcquire"/>, in a player or in play mode).
        /// In edit mode nothing is asked for unless a caller sets it, as UnitFigures.Source has it (the stream's coroutine never
        /// runs there): the editor's captures answer from the AssetDatabase at once and the tests answer by hand. Callers put
        /// back null. Setting it forgets the arrived model (<see cref="arrived"/>).</summary>
        public static Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? Source
        {
            get => source;
            set { source = value; arrived = null; }
        }
        static Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? source;

        /// <summary>The model once it has arrived, so a holder built after that (a rebuilt token) draws it at once rather than
        /// the code-built aircraft until its own answer, a frame or more later; it still takes its own lease. Null until then,
        /// and again once the bundle is released (Unity's null for a destroyed prefab), as UnitFigures keeps its arrived models.</summary>
        static GameObject? arrived;

        static Func<string, CollectionDetail, Action<GameObject?>, IDisposable>? Acquirer =>
            Source ?? (Application.isPlaying ? ProductionAcquire : null);

        Renderer[] codeBuilt = Array.Empty<Renderer>();
        IDisposable? lease;
        bool attached, released;

        /// <summary>Whether the model has been asked for.</summary>
        internal bool Asked { get; private set; }
        /// <summary>The streamed model's instance once it stands (null until then, and for good when it failed open).</summary>
        public GameObject? Streamed { get; private set; }

        /// <summary>Puts the swap on <paramref name="plane"/>, the holder of the code-built <paramref name="airframe"/> and
        /// <paramref name="propeller"/>. Call once the holder is placed, scaled and, when it flies, given its ShowcaseFlight: an
        /// answer can come at once (a capture's stand-in).</summary>
        public static StreamedAircraft Attach(GameObject plane, GameObject airframe, Transform propeller)
        {
            var swap = plane.AddComponent<StreamedAircraft>();
            swap.codeBuilt = airframe.GetComponentsInChildren<Renderer>(true).Concat(propeller.GetComponentsInChildren<Renderer>(true)).ToArray();
            swap.attached = true;
            if (swap.isActiveAndEnabled) swap.Ask();
            return swap;
        }

        void OnEnable() { if (attached) Ask(); }

        void Ask()
        {
            if (Asked || released) return;
            var acquire = Acquirer;
            if (acquire == null) return;
            Asked = true;
            var held = acquire(Model, CollectionDetail.Far, Arrived);
            if (released) { held.Dispose(); return; }
            lease = held;
            if (arrived != null) Arrived(arrived);   // already here: no frame of the code-built aircraft; the stream's own answer is then ignored
        }

        void Arrived(GameObject? prefab)
        {
            if (released || this == null) return;   // the holder is gone: a late answer draws nothing
            if (prefab == null) return;              // fail open (the stream has warned once): the code-built aircraft stays
            if (Streamed != null) return;
            if (prefab.transform.Find(PropellerName) == null)
            {
                Debug.LogWarning($"UNITS: the streamed {Model} has no {PropellerName} child to turn; the code-built aircraft stays.");
                return;
            }
            arrived = prefab;
            var go = Instantiate(prefab, transform, false);
            go.name = ModelName;
            go.transform.localPosition = Vector3.zero;
            go.SetActive(true);   // a streamed prefab may be stored inactive
            var shadows = codeBuilt.Length > 0 && codeBuilt[0] != null ? codeBuilt[0].shadowCastingMode : ShadowCastingMode.On;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = shadows;
            foreach (var r in codeBuilt) if (r != null) r.enabled = false;
            Streamed = go;
            if (TryGetComponent<ShowcaseFlight>(out var flight)) flight.TurnPropeller(go.transform.Find(PropellerName));
        }

        void OnDestroy()
        {
            released = true;
            lease?.Dispose(); lease = null;
        }
    }
}
