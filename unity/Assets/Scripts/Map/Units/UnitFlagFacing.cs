#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Owner ruling (2026-10-15): "billboard the flag". Turns a token's flag about its pole's vertical axis to face
    /// the camera about to render, so the cloth never shows edge-on as the camera orbits; the pole stays upright and its foot
    /// stays on the base's edge. It works on the camera's position only: no allocation, and nothing when the camera has not
    /// moved since the last turn. Disabled with its token (a hidden layer turns nothing). It runs in the editor too, so the
    /// editor's promo renders and the tests see the flags as play does.</summary>
    [ExecuteAlways]
    public sealed class UnitFlagFacing : MonoBehaviour
    {
        Vector3 lastCamera = new Vector3(float.NaN, 0f, 0f);
        static readonly List<UnitFlagFacing> Live = new List<UnitFlagFacing>();

        void OnEnable() { RenderPipelineManager.beginCameraRendering += Turn; Live.Add(this); }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= Turn; Live.Remove(this); }

        /// <summary>Turns every live flag to <paramref name="camera"/> now. A turn made as a camera renders shows from its next
        /// frame (unnoticed in play); a still render (the editor's promo captures, the tests) calls this before rendering.</summary>
        public static void FaceAll(Vector3 camera)
        {
            for (int i = 0; i < Live.Count; i++) { Face(Live[i].transform, camera); Live[i].lastCamera = camera; }
        }

        void Turn(ScriptableRenderContext _, Camera cam)
        {
            if (cam == null) return;
            var at = cam.transform.position;
            if (at == lastCamera) return;
            lastCamera = at;
            Face(transform, at);
        }

        /// <summary>Turns <paramref name="flag"/> about the world's vertical so its cloth (local XY plane, its front towards
        /// local -Z) faces <paramref name="camera"/>, seen from above.</summary>
        public static void Face(Transform flag, Vector3 camera)
        {
            var toCam = camera - flag.position; toCam.y = 0f;
            if (toCam.sqrMagnitude < 1f) return;
            flag.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }
}
