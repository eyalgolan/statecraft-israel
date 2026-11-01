#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>The figure states a token's Animators can play while the token moves or attacks (#1866, #1867; design by
    /// Tal Heuberger, #1900): the controllers' <c>idle</c>, <c>walk</c> and <c>attack</c> states, which CollectionImport adds
    /// only where the figure's own FBX has the take. A missing controller or state leaves the figure as it is.</summary>
    public static class UnitFigureMotion
    {
        public const string IdleState = "idle", WalkState = "walk", AttackState = "attack";
        public static readonly int Idle = Animator.StringToHash("Base Layer." + IdleState);
        public static readonly int Walk = Animator.StringToHash("Base Layer." + WalkState);
        public static readonly int Attack = Animator.StringToHash("Base Layer." + AttackState);

        /// <summary>Plays <paramref name="state"/> (one of the hashes above) on <paramref name="animator"/> from its start,
        /// unless it is already playing it and <paramref name="restart"/> is false (the Animator reports a Play only after its
        /// next update, so a caller that changed the state this frame passes true). False when the Animator cannot play it:
        /// none, not active and enabled (a far figure's near Animator, or a token being torn down), no controller, or no
        /// such state. No allocation.</summary>
        public static bool Play(Animator? animator, int state, bool restart = false)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return false;
            if (!animator.HasState(0, state)) return false;
            animator.applyRootMotion = false;   // the token's offset moves the figure; a take's root motion must not add to it
            if (restart || animator.GetCurrentAnimatorStateInfo(0).fullPathHash != state) animator.Play(state, 0, 0f);
            return true;
        }
    }
}
