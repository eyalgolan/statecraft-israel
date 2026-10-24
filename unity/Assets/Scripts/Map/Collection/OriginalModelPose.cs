#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>C05 and the farmer stand at frame zero of their original clip. The source mesh and clip are unchanged.</summary>
    public sealed class OriginalModelPose : MonoBehaviour
    {
        public static bool Required(string id) => id == "figure-c05-pioneer-veteran-v2" || id == "figure-pioneer-farmer-v1-tpose";
        void OnEnable()
        {
            var animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Play(0, 0, 0f);
            animator.Update(0f);
            animator.enabled = false;
        }
    }
}
