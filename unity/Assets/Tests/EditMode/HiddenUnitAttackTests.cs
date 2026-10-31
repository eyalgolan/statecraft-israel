using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Units;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public class HiddenUnitAttackTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void HiddenOrDisabledTokenDoesNotRetainCombatUntilShown(bool disableComponent)
    {
        var token = new GameObject("hidden combat");
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        var idle = new AnimationClip { name = "idle" };
        var fire = new AnimationClip { name = "fire" };
        fire.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 0f));
        try
        {
            var sm = controller.layers[0].stateMachine;
            sm.defaultState = sm.AddState("idle"); sm.defaultState.motion = idle;
            sm.AddState("attack").motion = fire;
            var animator = token.AddComponent<Animator>(); animator.runtimeAnimatorController = controller;
            animator.Rebind(); animator.Update(0f);
            var motion = token.AddComponent<UnitTokenMotion>();
            Assert.IsTrue(motion.PlayAttack(), "compatible visible token can attack");
            if (disableComponent) motion.enabled = false; else token.SetActive(false);
            // EditMode does not guarantee runtime OnDisable dispatch on an unstarted component.
            typeof(UnitTokenMotion).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, null);
            Assert.IsFalse(motion.PlayAttack(), "combat while hidden/disabled is dropped, never queued");
            Assert.AreEqual(0f, typeof(UnitTokenMotion).GetField("attackRemaining", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(motion));
            if (disableComponent) motion.enabled = true; else token.SetActive(true);
            motion.RefreshPose(); animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.idle"), "showing the token cannot replay hidden combat");
            Assert.IsTrue(motion.PlayAttack(), "a fresh visible combat event still works");
            animator.enabled = false;
            Assert.IsFalse(motion.PlayAttack(), "disabled Animator fallback is unchanged");
            animator.enabled = true; animator.runtimeAnimatorController = null;
            Assert.IsFalse(motion.PlayAttack(), "missing controller fallback is unchanged");
        }
        finally { Object.DestroyImmediate(token); Object.DestroyImmediate(controller); Object.DestroyImmediate(idle); Object.DestroyImmediate(fire); }
    }
}
