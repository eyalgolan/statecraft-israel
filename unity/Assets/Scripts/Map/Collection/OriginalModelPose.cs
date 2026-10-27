#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>A far placement stands at frame zero of its original clip without running an Animator every frame.
    /// C05 and the farmer also keep this still pose at close zoom. Source meshes and clips are unchanged.</summary>
    public sealed class OriginalModelPose : MonoBehaviour
    {
        public static bool Required(string id) => id == "figure-c05-pioneer-veteran-v2" || id == "figure-pioneer-farmer-v1-tpose";
        void OnEnable() => Sample(gameObject);

        /// <summary>Called before a far original is measured or drawn (#1273, owner 24 Oct 2026).
        /// Only instances are posed; their original controllers and clip data stay intact.</summary>
        public static void Sample(GameObject instance)
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null) continue;
                var culling = animator.cullingMode;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Play(0, 0, 0f);
                animator.Update(0f);
                animator.enabled = false;
                animator.cullingMode = culling;
            }
        }
    }
}
