#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Units
{
    /// <summary>A token's move and attack as the player sees them (#1866, #1867; design by Tal Heuberger, #1900).
    /// Presentation only: the sim has already moved the unit. A token's root stands at the world origin and its pieces
    /// stand at their map places under it, so the root's position is an offset from where the unit now is. A move slides
    /// that offset from the old place to zero over <see cref="Duration"/>; the figures walk meanwhile and stand idle after.
    /// An attack plays the figures' fire take once, then they walk or stand again. The component runs its LateUpdate only
    /// while a move or an attack is under way. Disabling it (the token hidden or rebuilt) puts the token where the unit is.
    /// A figure that turns near mid-move or mid-attack (UnitFigureDetail) or arrives then (UnitFigures) joins in at once.</summary>
    public sealed class UnitTokenMotion : MonoBehaviour
    {
        public const float Duration = 0.65f;
        /// <summary>A shorter move than this (metres) is drawn at once.</summary>
        public const float MinMoveM = 1f;
        /// <summary>How long an attack waits for a figure that can play it. An engagement usually changes strengths, so the
        /// tokens are rebuilt just before it plays, and the new ones turn their near figures on at the next render.</summary>
        public const float AttackWaitS = 0.25f;

        /// <summary>The sim unit this token draws, or -1 for a row with no sim unit (the legacy loop).</summary>
        public int SimId { get; internal set; } = -1;
        /// <summary>Raised when the token's place changes, so UnitLayer redraws its badge and hit boxes there.</summary>
        internal Action? Changed;

        Vector3 from;
        float elapsed = Duration, attackLeft;
        bool firing;   // a figure plays the attack (else it waits for one, up to AttackWaitS)
        bool fired;    // an attack just ended: its figures go back to walking or standing
        float attackLength, resumeAt;   // the playing take's length; where a continued attack picks up
        int posed;     // the walk or idle the figures were last asked for
        readonly List<Animator> animators = new List<Animator>();

        public bool Moving => elapsed < Duration;
        /// <summary>An attack plays, or waits for a figure to play it.</summary>
        public bool Attacking => attackLeft > 0f;

        /// <summary>Starts a slide from <paramref name="offset"/> (the old place less the new) to the unit's place.</summary>
        public void Begin(Vector3 offset)
        {
            if (offset.sqrMagnitude < MinMoveM * MinMoveM) { if (Moving || Attacking) Snap(); return; }
            from = offset; elapsed = 0f;
            transform.position = from;
            enabled = true;
            Pose();
            Changed?.Invoke();
        }

        /// <summary>Plays the figures' attack take once from its start (again, if one is playing). True when a figure
        /// plays it now; else it waits <see cref="AttackWaitS"/> for one to turn near, and then is dropped. A hidden token
        /// does nothing. Nothing is kept to play later.</summary>
        public bool PlayAttack()
        {
            if (!gameObject.activeInHierarchy) return false;
            attackLeft = AttackWaitS; firing = false; resumeAt = 0f;
            enabled = true;
            return Fire();
        }

        /// <summary>How far into its fire take the token's attack is (seconds), or 0 when none plays: a rebuild hands it to
        /// the unit's new token (<see cref="ContinueAttack"/>).</summary>
        internal float AttackPlayed => firing ? Mathf.Max(0f, attackLength - attackLeft) : 0f;

        /// <summary>Goes on with an attack the unit's previous token had played <paramref name="played"/> seconds of, from
        /// there (a rebuild for another unit's order must not cut a take short).</summary>
        internal void ContinueAttack(float played)
        {
            if (played <= 0f || !gameObject.activeInHierarchy) return;
            resumeAt = played; attackLeft = AttackWaitS; firing = false;
            enabled = true;
            Fire();
        }

        /// <summary>Starts the fire take on every live figure that has one; the attack then lasts as long as the longest.</summary>
        bool Fire()
        {
            float longest = 0f;
            animators.Clear(); GetComponentsInChildren(false, animators);
            foreach (var animator in animators)
            {
                if (!UnitFigureMotion.Play(animator, UnitFigureMotion.Attack, restart: true)) continue;   // even mid-attack
                float length = 0f;
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (CollectionFireTake(clip.name)) length = Mathf.Max(length, clip.length);
                if (resumeAt > 0f && length > 0f) animator.Play(UnitFigureMotion.Attack, 0, Mathf.Clamp01(resumeAt / length));
                longest = Mathf.Max(longest, length);
            }
            if (longest <= 0f) return false;
            if (longest <= resumeAt) { attackLeft = 0f; resumeAt = 0f; return false; }   // the take had finished
            attackLength = longest; attackLeft = longest - resumeAt; resumeAt = 0f; firing = true;
            return true;
        }

        /// <summary>The fire take's name as CollectionImport binds it to the attack state.</summary>
        public static bool CollectionFireTake(string name) =>
            name == "fire" || name.EndsWith("|fire", StringComparison.OrdinalIgnoreCase);

        /// <summary>Puts every live figure of the token in the state its move and attack call for now: a figure that
        /// arrives or turns near mid-move walks at once, and one that can play a waiting attack plays it. UnitFigures and
        /// UnitFigureDetail call it. It allocates only when it starts a waiting attack (reading the controller's clips).</summary>
        public void RefreshPose() => Pose();

        void Pose()
        {
            if (Attacking && !firing) Fire();   // a figure that can play the waiting attack turned near
            int state = Moving ? UnitFigureMotion.Walk : UnitFigureMotion.Idle;
            bool changed = state != posed, ended = fired;
            posed = state; fired = false;
            animators.Clear(); GetComponentsInChildren(false, animators);
            foreach (var animator in animators)
            {
                if (firing && UnitFigureMotion.Play(animator, UnitFigureMotion.Attack)) continue;
                // A figure leaves its walk, idle or fire this frame: play the new state even before its Animator reports the old.
                UnitFigureMotion.Play(animator, state,
                    changed || ended && animator.isActiveAndEnabled && animator.HasState(0, UnitFigureMotion.Attack));
            }
        }

        /// <summary>Advances the move and the attack by <paramref name="seconds"/>; LateUpdate passes the frame's time.</summary>
        internal void Tick(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            bool wasMoving = Moving, wasFiring = firing;
            if (Moving)
            {
                elapsed = Mathf.Min(Duration, elapsed + seconds);
                transform.position = Vector3.Lerp(from, Vector3.zero, elapsed / Duration);
                Changed?.Invoke();
            }
            if (Attacking)
            {
                attackLeft = Mathf.Max(0f, attackLeft - seconds);
                if (!Attacking) { fired = firing; firing = false; resumeAt = 0f; }
            }
            if (wasMoving != Moving || wasFiring != firing) Pose();
            if (!Moving && !Attacking) enabled = false;   // an idle token costs nothing per frame
        }

        void LateUpdate() => Tick(Time.deltaTime);

        void OnDisable()
        {
            if (Moving || Attacking) Snap();
        }

        void Snap()
        {
            bool moved = transform.position != Vector3.zero;
            fired = firing;
            elapsed = Duration; attackLeft = 0f; firing = false; resumeAt = 0f; from = Vector3.zero;
            transform.position = Vector3.zero;
            Pose();
            if (moved) Changed?.Invoke();
        }
    }
}
