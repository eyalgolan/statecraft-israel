#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>Presentation contract shared by locomotion and action consumers. Missing states leave the authored pose.</summary>
    public static class UnitFigureMotion
    {
        public const string Idle = "idle", Walk = "walk", Attack = "attack";
        static readonly int IdleHash = Animator.StringToHash("Base Layer.idle");
        static readonly int WalkHash = Animator.StringToHash("Base Layer.walk");
        internal static readonly int AttackHash = Animator.StringToHash("Base Layer.attack");
        public static bool Play(Animator animator, string state)
        {
            if (animator == null || animator.runtimeAnimatorController == null || !animator.enabled) return false;
            animator.applyRootMotion = false;
            int hash = state == Idle ? IdleHash : state == Walk ? WalkHash : state == Attack ? AttackHash
                : Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, hash)) return false;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != hash) animator.Play(hash, 0, 0f);
            return true;
        }
        public static void SetMoving(Transform token, bool moving)
        {
            foreach (var animator in token.GetComponentsInChildren<Animator>(true)) Play(animator, moving ? Walk : Idle);
        }
    }
}
