#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Full originals only below the strategic distance. Strategic view uses the map's
    /// code-built kits and unit plates. No decimated geometry is made. Per-camera flags are restored after rendering.</summary>
    public sealed class OriginalModelDistance : MonoBehaviour
    {
        // Between measured gameplay03 (60 km) and diorama02 (250 km): geometric midpoint 122.5 km.
        // The 120/135 km hysteresis spans more than one 10% wheel step.
        public const float EnterM = 120000f, ExitM = 135000f;
        public static bool CloseAt(float distance, bool? was) => was == true ? distance <= ExitM : distance < EnterM;
        Renderer[] renderers = System.Array.Empty<Renderer>();
        bool[] previous = System.Array.Empty<bool>();
        static readonly Dictionary<int, bool> choices = new Dictionary<int, bool>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetChoices() => choices.Clear();
        // One choice per camera, including models that arrive inside the hysteresis band.
        public static bool Visible(Camera camera)
        {
            int id = camera.GetInstanceID();
            bool close = CloseAt(Kits.TownModelLayer.ViewDistance(camera),
                choices.TryGetValue(id, out var was) ? was : (bool?)null);
            choices[id] = close;
            return close;
        }
        bool pending;
        void OnEnable()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            previous = new bool[renderers.Length];
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }
        void OnDisable()
        {
            Restore();
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
        }
        void Begin(ScriptableRenderContext _, Camera camera)
        {
            Restore();
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;
            if (Visible(camera)) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) { previous[i] = renderers[i].forceRenderingOff; renderers[i].forceRenderingOff = true; }
            pending = true;
        }
        void End(ScriptableRenderContext _, Camera camera) => Restore();
        void Restore()
        {
            if (!pending) return;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].forceRenderingOff = previous[i];
            pending = false;
        }
    }
}
