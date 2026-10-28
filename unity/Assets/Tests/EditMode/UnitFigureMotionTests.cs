using NUnit.Framework;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;
using Statecraft.Game.Map.Collection;

public class UnitFigureMotionTests
{
    [TestCase(0f)]
    [TestCase(.01f)]
    public void SubthresholdMotionSnapsToItsDestination(float offset)
    {
        var go = new GameObject("small transition");
        try
        {
            var motion = go.AddComponent<UnitTokenMotion>();
            motion.Begin(Vector3.right * offset);
            Assert.IsFalse(motion.Moving);
            Assert.AreEqual(Vector3.zero, go.transform.position);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void DisablingMotionSnapsAndInvalidatesItsCachedBounds()
    {
        var go = new GameObject("disabled transition");
        try
        {
            var motion = go.AddComponent<UnitTokenMotion>();
            int changes = 0;
            motion.Changed = () => changes++;
            motion.Begin(Vector3.right * 100f);
            motion.enabled = false;
            // EditMode does not dispatch this runtime callback for an unstarted component.
            go.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
            Assert.IsFalse(motion.Moving);
            Assert.AreEqual(Vector3.zero, go.transform.position);
            Assert.AreEqual(1, changes, "the camera must refresh cached hit bounds after the snap");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void RuntimeC06PrefabAndControllerBindTheSameOriginalModel()
    {
        const string id = "figure-c06-young-recruit-v2";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
        Assert.IsNotNull(prefab);
        var skin = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var animator = prefab.GetComponentInChildren<Animator>(true);
        var model = AssetDatabase.GetAssetPath(skin.sharedMesh);
        Assert.AreEqual("Assets/Art/Collection/" + id + "/" + id + ".fbx", model);
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            Assert.AreEqual(model, AssetDatabase.GetAssetPath(clip), clip.name + " must match the actual runtime skeleton");
    }
    [Test]
    public void AttackExpiresAndRestoresCurrentLocomotionAndMissingAttackIsSilent()
    {
        var go = new GameObject("attack");
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        var idle = new AnimationClip { name = "idle" };
        var walk = new AnimationClip { name = "walk" };
        var fire = new AnimationClip { name = "fire" };
        fire.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 0f));
        try
        {
            var sm = controller.layers[0].stateMachine;
            sm.defaultState = sm.AddState("idle"); sm.defaultState.motion = idle;
            sm.AddState("walk").motion = walk; sm.AddState("attack").motion = fire;
            var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller;
            animator.Rebind(); animator.Update(0f);
            var motion = go.AddComponent<UnitTokenMotion>(); motion.Begin(Vector3.right);
            Assert.IsTrue(motion.PlayAttack()); animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.attack"));
            motion.AdvanceAttack(.5f); animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.attack"));
            motion.AdvanceAttack(1f); animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.walk"));
            Assert.IsTrue(motion.PlayAttack());
            motion.Advance(10f); motion.AdvanceAttack(10f); animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.idle"));
            animator.runtimeAnimatorController = null;
            Assert.IsFalse(motion.PlayAttack());
        }
        finally
        {
            Object.DestroyImmediate(go); Object.DestroyImmediate(controller);
            Object.DestroyImmediate(idle); Object.DestroyImmediate(walk); Object.DestroyImmediate(fire);
        }
    }
    [Test]
    public void ImportPreparationRetainsSameWalkAndAttackStatesOnSecondPass()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Statecraft.Game.EditorTools.CollectionImport")).First(t => t != null);
        var prepare = type.GetMethod("AddWalkState", BindingFlags.Static | BindingFlags.NonPublic);
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        const string model = "Assets/Art/Collection/figure-c06-young-recruit-v2/figure-c06-young-recruit-v2.fbx";
        try
        {
            prepare.Invoke(null, new object[] { controller, model });
            var first = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            Assert.AreEqual(2, first.Length);
            Assert.IsTrue(first.Any(s => s.name == "walk" && s.motion != null));
            Assert.IsTrue(first.Any(s => s.name == "attack" && s.motion != null));
            prepare.Invoke(null, new object[] { controller, model });
            CollectionAssert.AreEquivalent(first, controller.layers[0].stateMachine.states.Select(s => s.state));
        }
        finally { Object.DestroyImmediate(controller); }
    }
    [Test]
    public void ExistingAnimatorPlaysWalkThenReturnsToIdle()
    {
        var go = new GameObject("states");
        var controller = new AnimatorController();
        controller.AddLayer("Base Layer");
        var idle = new AnimationClip { name = "idle" };
        var walk = new AnimationClip { name = "walk" };
        try
        {
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = machine.AddState("idle");
            machine.defaultState.motion = idle;
            machine.AddState("walk").motion = walk;
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.Rebind(); animator.Update(0f);
            Assert.IsTrue(UnitFigureMotion.Play(animator, UnitFigureMotion.Walk));
            animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.walk"));
            Assert.IsTrue(UnitFigureMotion.Play(animator, UnitFigureMotion.Idle));
            animator.Update(0f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.idle"));
            Assert.IsFalse(animator.applyRootMotion);
            Assert.IsFalse(UnitFigureMotion.Play(animator, "missing"));
        }
        finally
        {
            Object.DestroyImmediate(go); Object.DestroyImmediate(controller);
            Object.DestroyImmediate(idle); Object.DestroyImmediate(walk);
        }
    }
    [Test]
    public void TransitionClampsToDestinationAndStops()
    {
        var go = new GameObject("transition");
        try
        {
            var motion = go.AddComponent<UnitTokenMotion>();
            motion.Begin(Vector3.right * 100f);
            motion.Advance(UnitTokenMotion.Duration / 2f);
            Assert.AreEqual(50f, go.transform.position.x, .001f);
            Assert.IsTrue(motion.Moving);
            motion.Advance(100f);
            Assert.AreEqual(Vector3.zero, go.transform.position);
            Assert.IsFalse(motion.Moving);
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test]
    public void MissingControllerAndDisabledAnimatorFailOpen()
    {
        var go = new GameObject("missing clips");
        try
        {
            var animator = go.AddComponent<Animator>();
            Assert.IsFalse(UnitFigureMotion.Play(animator, UnitFigureMotion.Walk));
            animator.enabled = false;
            Assert.IsFalse(UnitFigureMotion.Play(animator, UnitFigureMotion.Idle));
            UnitFigureMotion.SetMoving(go.transform, true);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
