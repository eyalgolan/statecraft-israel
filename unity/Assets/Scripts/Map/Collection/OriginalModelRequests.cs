#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Map placements ask only when a camera first reaches a view that can draw full originals.
    /// Independent UI/painter stream consumers do not depend on the map's zoom.</summary>
    public sealed class OriginalModelRequests : MonoBehaviour
    {
        readonly List<Action> pending = new List<Action>();
        public static void WhenVisible(MonoBehaviour owner, Action ask)
        {
            if (!Application.isPlaying) { ask(); return; }
            var requests = owner.GetComponent<OriginalModelRequests>() ?? owner.gameObject.AddComponent<OriginalModelRequests>();
            requests.pending.Add(ask);
        }
        void OnEnable() => RenderPipelineManager.beginCameraRendering += Begin;
        void OnDisable() => RenderPipelineManager.beginCameraRendering -= Begin;
        void Begin(ScriptableRenderContext _, Camera camera)
        {
            if (pending.Count == 0 || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection
                || !OriginalModelDistance.Visible(camera)) return;
            var asks = pending.ToArray(); pending.Clear();
            foreach (var ask in asks) ask();
        }
    }
}
