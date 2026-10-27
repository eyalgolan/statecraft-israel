#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Presentation contract shared by locomotion and action consumers. Missing states leave the authored pose.</summary>
    public static class UnitFigureMotion
    {
        public const string Idle = "idle", Walk = "walk";
        public static bool Play(Animator animator, string state)
        {
            if (animator == null || animator.runtimeAnimatorController == null || !animator.enabled) return false;
            animator.applyRootMotion = false;
            int hash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, hash)) return false;
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + state)) animator.Play(hash, 0, 0f);
            return true;
        }
        public static void SetMoving(Transform token, bool moving)
        {
            foreach (var animator in token.GetComponentsInChildren<Animator>(true)) Play(animator, moving ? Walk : Idle);
        }
    }
}
