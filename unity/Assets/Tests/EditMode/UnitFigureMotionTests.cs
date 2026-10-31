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
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

public class UnitFigureMotionTests
{
    static Action<AnimatorController, AnimationClip, string> ConfigureGeneratedController()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Statecraft.Game.EditorTools.Generated1948Characters")).First(t => t != null);
        return (Action<AnimatorController, AnimationClip, string>)Delegate.CreateDelegate(typeof(Action<AnimatorController, AnimationClip, string>),
            type.GetMethod("ConfigureController", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [TestCase("empty")]
    [TestCase("duplicate-idle")]
    [TestCase("duplicate-walk")]
    [TestCase("duplicate-attack")]
    [TestCase("missing-idle")]
    [TestCase("walk-default")]
    [TestCase("unknown-state")]
    public void InvalidGeneratedControllerIsRejectedBeforeMutation(string layout)
    {
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        var before = new AnimationClip { name = "before" }; var idle = new AnimationClip { name = "idle" };
        AnimatorState foreign = null;
        try
        {
            var sm = controller.layers[0].stateMachine;
            AnimatorState Add(string name) { var state = sm.AddState(name); state.name = name; state.motion = before; return state; }
            if (layout != "empty")
            {
                if (layout == "missing-idle") sm.defaultState = Add("walk");
                else sm.defaultState = Add("idle");
                if (layout.StartsWith("duplicate-"))
                {
                    string name = layout.Substring("duplicate-".Length);
                    if (name != "idle") Add(name);
                    Add(name);
                }
                if (layout == "missing-default") sm.defaultState = null;
                if (layout == "foreign-default")
                {
                    foreign = new AnimatorState(); foreign.name = "idle"; foreign.motion = before; sm.defaultState = foreign;
                }
                if (layout == "walk-default") sm.defaultState = Add("walk");
                if (layout == "unknown-state") Add("unexpected");
            }
            var states = sm.states.Select(s => s.state).ToArray();
            var defaultState = sm.defaultState;
            Exception error = null;
            try { ConfigureGeneratedController()(controller, idle, "Assets/Art/Collection/figure-c06-young-recruit-v2/figure-c06-young-recruit-v2.fbx"); }
            catch (Exception caught) { error = caught; }
            CollectionAssert.AreEqual(states, sm.states.Select(s => s.state), "reject before adding states");
            Assert.AreSame(defaultState, sm.defaultState, "reject before changing the default");
            foreach (var state in states) Assert.AreSame(before, state.motion, "reject before overwriting any authored motion");
            if (foreign != null) Assert.AreSame(before, foreign.motion);
            Assert.IsInstanceOf<InvalidOperationException>(error);
        }
        finally
        {
            Object.DestroyImmediate(controller); Object.DestroyImmediate(before); Object.DestroyImmediate(idle);
            if (foreign != null) Object.DestroyImmediate(foreign);
        }
    }

    [Test]
    public void IdleOnlyGeneratedControllerMigratesIdempotently()
    {
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        var idle = new AnimationClip { name = "idle" };
        try
        {
            var sm = controller.layers[0].stateMachine; var original = sm.AddState("idle"); sm.defaultState = original;
            var configure = ConfigureGeneratedController();
            const string model = "Assets/Art/Collection/figure-c06-young-recruit-v2/figure-c06-young-recruit-v2.fbx";
            configure(controller, idle, model);
            var states = sm.states.Select(s => s.state).ToArray();
            CollectionAssert.AreEquivalent(new[] { "idle", "walk", "attack" }, states.Select(s => s.name));
            Assert.AreSame(original, sm.defaultState); Assert.AreSame(idle, original.motion);
            configure(controller, idle, model);
            CollectionAssert.AreEqual(states, sm.states.Select(s => s.state));
        }
        finally { Object.DestroyImmediate(controller); Object.DestroyImmediate(idle); }
    }

    [TestCase("idle")]
    [TestCase("walk")]
    [TestCase("attack")]
    public void BuiltInPlaybackDoesNotAllocateAndCustomStatesStillWork(string state)
    {
        var go = new GameObject("cached states");
        var controller = new AnimatorController(); controller.AddLayer("Base Layer");
        var clip = new AnimationClip { name = "pose" };
        try
        {
            var sm = controller.layers[0].stateMachine;
            foreach (string name in new[] { "idle", "walk", "attack", "custom" }) sm.AddState(name).motion = clip;
            var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.Rebind(); animator.Update(0f);
            for (int i = 0; i < 20; i++) { UnitFigureMotion.Play(animator, state); animator.Update(0f); }
            Assert.That(() =>
            {
                UnitFigureMotion.Play(animator, state);
            }, Is.Not.AllocatingGCMemory());
            Assert.IsTrue(UnitFigureMotion.Play(animator, "custom")); animator.Update(0f);
            Assert.AreEqual(Animator.StringToHash("Base Layer.custom"), animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
            Assert.IsFalse(UnitFigureMotion.Play(animator, "missing-custom"));
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(controller); Object.DestroyImmediate(clip); }
    }

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
            typeof(UnitTokenMotion).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motion, null);
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
