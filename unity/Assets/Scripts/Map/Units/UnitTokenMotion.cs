#nullable enable
using UnityEngine;
using System.Collections.Generic;
namespace Statecraft.Game.Map.Units
{
    /// <summary>A bounded, presentation-only offset. Destroying/rebuilding a token cancels its transition.</summary>
    public sealed class UnitTokenMotion : MonoBehaviour
    {
        public const float Duration = .65f;
        Vector3 start; float elapsed;
        readonly List<Animator> animators = new List<Animator>();
        float attackRemaining;
        public string Identity = "";
        public System.Action? Changed;
        public bool Moving { get; private set; }
        public void Begin(Vector3 offset)
        {
            elapsed = 0f; Moving = offset.sqrMagnitude > .001f;
            start = Moving ? offset : Vector3.zero;
            // Token roots stay at world origin; map positions are baked into their child geometry.
            // This position is a presentation offset from that geometry, not a map destination.
            transform.position = start;
        }
        public void Advance(float seconds)
        {
            if (!Moving) return;
            elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0f, seconds));
            transform.position = Vector3.Lerp(start, Vector3.zero, elapsed / Duration);
            Moving = elapsed < Duration;
            RefreshPose();
            Changed?.Invoke();
        }
        public void RefreshPose()
        {
            animators.Clear(); transform.GetComponentsInChildren(true, animators);
            foreach (var animator in animators)
                UnitFigureMotion.Play(animator, attackRemaining > 0f ? UnitFigureMotion.Attack : Moving ? UnitFigureMotion.Walk : UnitFigureMotion.Idle);
        }
        public bool PlayAttack()
        {
            animators.Clear(); transform.GetComponentsInChildren(true, animators);
            float duration = 0f;
            foreach (var animator in animators)
            {
                if (!UnitFigureMotion.Play(animator, UnitFigureMotion.Attack)) continue;
                animator.Play(UnitFigureMotion.AttackHash, 0, 0f);
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name.EndsWith("|fire", System.StringComparison.OrdinalIgnoreCase) || clip.name == "fire")
                        duration = Mathf.Max(duration, clip.length);
            }
            attackRemaining = duration;
            return duration > 0f;
        }
        public void AdvanceAttack(float seconds)
        {
            if (attackRemaining <= 0f) return;
            attackRemaining = Mathf.Max(0f, attackRemaining - Mathf.Max(0f, seconds));
            RefreshPose();
        }
        void LateUpdate() { Advance(Time.deltaTime); AdvanceAttack(Time.deltaTime); }
        void OnDisable()
        {
            attackRemaining = 0f; Moving = false; transform.position = Vector3.zero;
            UnitFigureMotion.SetMoving(transform, false);
            Changed?.Invoke();
        }
    }
}
