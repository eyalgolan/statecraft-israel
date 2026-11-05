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
    /// An attack plays the figures' fire take once, then they walk or stand again; a 65 mm gun on the token fires with
    /// them (UnitGunFire, #2373). A hit reaction composes a short shake with that slide. The component runs its LateUpdate
    /// only while a move, attack or reaction is under way. Disabling it (the token hidden or rebuilt) clears every motion.
    /// A figure that turns near mid-move or mid-attack (UnitFigureDetail) or arrives then (UnitFigures) joins in at once.</summary>
    public sealed class UnitTokenMotion : MonoBehaviour
    {
        public const float Duration = 0.65f;
        /// <summary>A shorter move than this (metres) is drawn at once.</summary>
        public const float MinMoveM = 1f;
        /// <summary>How long an attack waits for a figure that can play it. An engagement usually changes strengths, so the
        /// tokens are rebuilt just before it plays, and the new ones turn their near figures on at the next render.</summary>
        public const float AttackWaitS = 0.25f;
        public const float HitDurationS = 0.28f, HitFlashS = 0.12f, HitShakeM = 160f;
        public const int MoveDustPuffs = 3;

        /// <summary>The sim unit this token draws, or -1 for a row with no sim unit (the legacy loop).</summary>
        public int SimId { get; internal set; } = -1;
        /// <summary>Raised when the token's place changes, so UnitLayer redraws its badge and hit boxes there.</summary>
        internal Action? Changed;
        internal CombatBursts? Effects;
        internal Vector3 GroundHome;
        internal Renderer? HitFlash;

        Vector3 from, moveOffset;

        /// <summary>The slide under way (world offset from the token's place), without the hit's shake: where the token is,
        /// as the hit's burst and a destroyed unit's dust read it (UnitLayer.DrawnAt).</summary>
        internal Vector3 SlideOffset => moveOffset;
        float elapsed = Duration, attackLeft, hitLeft, hitDelay;
        int nextMoveDust;
        bool firing;   // a figure plays the attack (else it waits for one, up to AttackWaitS)
        bool fired;    // an attack just ended: its figures go back to walking or standing
        float attackLength, resumeAt;   // the playing take's length; where a continued attack picks up
        int posed;     // the walk or idle the figures were last asked for
        readonly List<Animator> animators = new List<Animator>();
        readonly List<UnitFigurePointFire> aims = new List<UnitFigurePointFire>();
        readonly List<UnitGunFire> guns = new List<UnitGunFire>();
        Action? attackStarted, attackMissed;

        public bool Moving => elapsed < Duration;
        /// <summary>An attack plays, or waits for a figure to play it.</summary>
        public bool Attacking => attackLeft > 0f;
        public bool Reacting => hitLeft > 0f;
        /// <summary>A shake and flash wait to start (<see cref="PlayHit"/> with a delay).</summary>
        public bool HitPending => hitDelay > 0f;

        /// <summary>Starts a slide from <paramref name="offset"/> (the old place less the new) to the unit's place.</summary>
        public void Begin(Vector3 offset)
        {
            if (offset.sqrMagnitude < MinMoveM * MinMoveM) { if (Moving || Attacking || Reacting || HitPending) Snap(); return; }
            from = moveOffset = offset; elapsed = 0f; nextMoveDust = 0;
            ApplyPosition();
            enabled = true;
            Pose();
            Changed?.Invoke();
        }

        /// <summary>Plays the figures' attack take once from its start (again, if one is playing). True when a figure
        /// plays it now; else it waits <see cref="AttackWaitS"/> for one to turn near, and then is dropped. A hidden token
        /// does nothing. Nothing is kept to play later.</summary>
        public bool PlayAttack() => PlayAttack(null, null);

        /// <summary>As <see cref="PlayAttack()"/>, reporting when a visible take actually starts, or when its short wait
        /// ends without one. The callbacks let the defender react to the take rather than merely to its request.</summary>
        internal bool PlayAttack(Action? started, Action? missed)
        {
            if (!gameObject.activeInHierarchy) { missed?.Invoke(); return false; }
            DropAttackCue();
            attackStarted = started; attackMissed = missed;
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

        /// <summary>Starts the fire take on every live figure that has one (even mid-attack), from its start or, for a
        /// continued attack, from where it was; the attack then lasts as long as the longest take has left. A take that had
        /// finished plays nothing, so the figures keep their walk or idle (review of #2176, item 8). A C06 figure holds Tal
        /// Heuberger's point-fire aim instead, near or far, for the whole attack; its own fire take counts toward the attack's
        /// length (#2084, UnitFigurePointFire). A 65 mm gun whose model has arrived fires too, near or far, from the same
        /// point in its shot; its recoil counts toward the attack's length (#2373, UnitGunFire).</summary>
        bool Fire()
        {
            float longest = 0f;
            animators.Clear(); GetComponentsInChildren(false, animators);
            aims.Clear(); GetComponentsInChildren(false, aims);
            guns.Clear(); GetComponentsInChildren(false, guns);
            foreach (var animator in animators)
                if (UnitFigureMotion.CanPlay(animator, UnitFigureMotion.Attack)) longest = Mathf.Max(longest, FireLength(animator));
            foreach (var aim in aims)
                if (aim.Ready) longest = Mathf.Max(longest, aim.FireLength);
            foreach (var gun in guns)
                if (gun.Ready) longest = Mathf.Max(longest, UnitGunFire.RecoilS);
            if (longest <= 0f) return false;   // no live figure has a fire take yet: keep waiting
            if (longest <= resumeAt) { attackLeft = 0f; resumeAt = 0f; return false; }   // the take had finished
            float from = resumeAt;
            foreach (var animator in animators)
            {
                if (!UnitFigureMotion.CanPlay(animator, UnitFigureMotion.Attack)) continue;
                float length = FireLength(animator);
                animator.applyRootMotion = false;   // as UnitFigureMotion.Play
                animator.Play(UnitFigureMotion.Attack, 0, resumeAt > 0f && length > 0f ? Mathf.Clamp01(resumeAt / length) : 0f);
            }
            attackLength = longest; attackLeft = longest - resumeAt; resumeAt = 0f; firing = true;
            foreach (var aim in aims) aim.Hold();
            foreach (var gun in guns) if (gun.Ready) gun.Fire(from);
            var started = attackStarted; attackStarted = null; attackMissed = null;
            started?.Invoke();
            return true;
        }

        /// <summary>Starts the defender's short presentation-only shake and badge flash, <paramref name="delayS"/> from now
        /// (when the hit lands on it, CombatBursts.ImpactS), or at once.</summary>
        public bool PlayHit(float delayS = 0f)
        {
            if (!gameObject.activeInHierarchy) return false;
            enabled = true;
            if (delayS > 0f) { hitDelay = delayS; return true; }
            StartHit(0f);
            return true;
        }

        void StartHit(float late)
        {
            hitDelay = 0f;
            hitLeft = Mathf.Max(0f, HitDurationS - late);
            ApplyPosition();
            Changed?.Invoke();
        }

        void EmitMoveDustThrough(float share)
        {
            while (nextMoveDust < MoveDustPuffs)
            {
                float mark = (nextMoveDust + 1f) / (MoveDustPuffs + 1f);
                if (share < mark) break;
                Effects?.March(GroundHome + Vector3.Lerp(from, Vector3.zero, mark));
                nextMoveDust++;
            }
        }

        void ApplyPosition()
        {
            float age = HitDurationS - hitLeft;
            float falloff = Reacting ? hitLeft / HitDurationS : 0f;
            float wave = Reacting ? Mathf.Sin(age * 4f * Mathf.PI / HitDurationS) : 0f;
            transform.position = moveOffset + new Vector3(wave, 0f, -wave * 0.35f) * (HitShakeM * falloff);
            if (HitFlash != null) HitFlash.enabled = hitLeft > HitDurationS - HitFlashS;
        }

        void DropAttackCue()
        {
            var missed = attackMissed; attackStarted = null; attackMissed = null;
            missed?.Invoke();
        }

        /// <summary>The longest fire take on <paramref name="animator"/>'s controller (reading its clips allocates).</summary>
        internal static float FireLength(Animator animator)
        {
            float length = 0f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (CollectionFireTake(clip.name)) length = Mathf.Max(length, clip.length);
            return length;
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
            bool started = Attacking && !firing && Fire();   // a figure that can play the waiting attack turned near
            int state = Moving ? UnitFigureMotion.Walk : UnitFigureMotion.Idle;
            bool changed = state != posed, ended = fired;
            posed = state; fired = false;
            // A held point-fire aim lets go before the Animators are told to walk or stand (it wakes a near one), and is
            // held again after them (it pauses them).
            aims.Clear(); GetComponentsInChildren(false, aims);
            if (!firing) foreach (var aim in aims) aim.Release();
            animators.Clear(); GetComponentsInChildren(false, animators);
            foreach (var animator in animators)
            {
                // Fire has just set a starting figure's take and time; Play would read the Animator's old state and restart
                // the take (review of #2176, item 3).
                if (firing && (started ? UnitFigureMotion.CanPlay(animator, UnitFigureMotion.Attack)
                    : UnitFigureMotion.Play(animator, UnitFigureMotion.Attack))) continue;
                // A figure leaves its walk, idle or fire this frame: play the new state even before its Animator reports the old.
                UnitFigureMotion.Play(animator, state,
                    changed || ended && animator.isActiveAndEnabled && animator.HasState(0, UnitFigureMotion.Attack));
            }
            if (firing) foreach (var aim in aims) aim.Hold();
        }

        /// <summary>Advances the move and the attack by <paramref name="seconds"/>; LateUpdate passes the frame's time.</summary>
        internal void Tick(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            bool wasMoving = Moving, wasFiring = firing, wasReacting = Reacting;
            if (Moving)
            {
                elapsed = Mathf.Min(Duration, elapsed + seconds);
                float share = elapsed / Duration;
                moveOffset = Vector3.Lerp(from, Vector3.zero, share);
                EmitMoveDustThrough(share);
            }
            if (Reacting) hitLeft = Mathf.Max(0f, hitLeft - seconds);
            if (HitPending)
            {
                // The hit lands within this step: the shake starts then, already as old as the rest of the step.
                if (seconds < hitDelay) hitDelay -= seconds; else { StartHit(seconds - hitDelay); wasReacting = false; }
            }
            if (wasMoving || wasReacting) { ApplyPosition(); Changed?.Invoke(); }
            if (Attacking)
            {
                attackLeft = Mathf.Max(0f, attackLeft - seconds);
                if (!Attacking)
                {
                    fired = firing; firing = false; resumeAt = 0f;
                    if (!fired) DropAttackCue();
                }
            }
            if (wasMoving != Moving || wasFiring != firing) Pose();
            if (!Moving && !Attacking && !Reacting && !HitPending) enabled = false;   // an idle token costs nothing per frame
        }

        void LateUpdate() => Tick(Time.deltaTime);

        void OnDisable()
        {
            if (Moving || Attacking || Reacting || HitPending) Snap();
        }

        void Snap()
        {
            bool moved = transform.position != Vector3.zero;
            fired = firing;
            elapsed = Duration; attackLeft = hitLeft = hitDelay = 0f; firing = false; resumeAt = 0f;
            from = moveOffset = Vector3.zero; nextMoveDust = 0;
            DropAttackCue();
            transform.position = Vector3.zero;
            if (HitFlash != null) HitFlash.enabled = false;
            Pose();
            if (moved) Changed?.Invoke();
        }
    }
}
