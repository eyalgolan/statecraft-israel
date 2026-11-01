using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#1866 and #1867 (design by Tal Heuberger, #1900): the token figures' controllers get walk and attack states
/// from takes already in each figure's own FBX (CollectionImport.AddMotionStates); no model, clip or prefab changes.</summary>
public class UnitFigureControllerTests
{
    static Type Import => Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor")!;
    static Type Generated => Type.GetType("Statecraft.Game.EditorTools.Generated1948Characters, Assembly-CSharp-Editor")!;
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static IReadOnlyCollection<string> TokenFigures => (IReadOnlyCollection<string>)Import.GetField("TokenFigures", Any)!.GetValue(null);
    static void AddMotionStates(AnimatorController c, string model) => Import.GetMethod("AddMotionStates", Any)!.Invoke(null, new object[] { c, model });
    static AnimationClip WalkTake(IReadOnlyList<AnimationClip> clips) => (AnimationClip)Import.GetMethod("WalkTake", Any)!.Invoke(null, new object[] { clips });
    static void Configure(AnimatorController c, AnimationClip idle, string model, bool token)
    {
        try { Generated.GetMethod("ConfigureController", Any)!.Invoke(null, new object[] { c, idle, model, token }); }
        catch (TargetInvocationException e) { throw e.InnerException!; }
    }

    const string C06 = "Assets/Art/Collection/figure-c06-young-recruit-v2/figure-c06-young-recruit-v2.fbx";

    static string ControllerPath(string id) => CollectionCatalog.IsFullQualityGeneratedInfantry(id)
        ? $"Assets/Art/Collection/{id}/{id}.controller" : $"Assets/Art/Collection/{id}/{id}-near.controller";
    static string ModelPath(string id) => $"Assets/Art/Collection/{id}/{id}.fbx";
    static AnimationClip[] Takes(string model) => AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>()
        .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
    static AnimatorState State(AnimatorController c, string name) =>
        c.layers[0].stateMachine.states.Select(s => s.state).SingleOrDefault(s => s.name == name);

    /// <summary>Every rigged model a unit token can stand (UnitFigures) is a token figure, so none is left without a walk.</summary>
    [Test]
    public void EveryRiggedFigureOnATokenIsATokenFigure()
    {
        var onTokens = UnitFigures.ByHeadgear.Values.Concat(UnitFigures.ThirdFigure.Values).Concat(UnitFigures.BySide.Values)
            .Concat(UnitFigures.ThirdBySide.Values).Append(UnitFigures.PioneerVeteran).Distinct();
        foreach (var id in onTokens.Where(id => CollectionCatalog.Animated.Contains(id) || id == UnitFigures.PioneerVeteran))
            Assert.That(TokenFigures, Has.Member(id), id);
        Assert.AreEqual(12, TokenFigures.Count);
    }

    /// <summary>The committed controllers are what PrepareTokenFigureControllers makes: the idle default untouched, a walk
    /// state on the figure's own walk take, and an attack state on its own fire take exactly where the FBX has one.</summary>
    [Test]
    public void EachTokenFigureControllerWalksAndFiresWithItsOwnTakes()
    {
        foreach (var id in TokenFigures)
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(id));
            Assert.IsNotNull(c, id);
            var takes = Takes(ModelPath(id));
            var machine = c.layers[0].stateMachine;
            Assert.AreEqual(id == UnitFigures.PioneerVeteran ? "Armature|idle_walk" : id.EndsWith("-v1", StringComparison.Ordinal) ? "idle" : "Armature|idle",
                machine.defaultState.motion.name, id + ": the default is still the idle the figure stood in");
            var walk = State(c, "walk");
            Assert.IsNotNull(walk, id + ": walk");
            Assert.AreSame(WalkTake(takes), walk.motion, id + ": its own walk take");
            var fire = takes.FirstOrDefault(t => UnitTokenMotion.CollectionFireTake(t.name));
            if (fire == null) Assert.IsNull(State(c, "attack"), id + ": no fire take, no attack state");
            else Assert.AreSame(fire, State(c, "attack")?.motion, id + ": its own fire take");
            Assert.AreEqual(fire == null ? 2 : 3, machine.states.Length, id);
            foreach (var clip in c.animationClips)
                Assert.AreEqual(ModelPath(id), AssetDatabase.GetAssetPath(clip), id + ": every take from the figure's own FBX");
        }
    }

    [Test]
    public void AddingTheStatesTwiceChangesNothing()
    {
        var c = new AnimatorController(); c.AddLayer("Base Layer");
        try
        {
            AddMotionStates(c, C06);
            var first = c.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            CollectionAssert.AreEquivalent(new[] { "walk", "attack" }, first.Select(s => s.name));
            Assert.AreEqual("Armature|walk", State(c, "walk").motion.name);
            Assert.AreEqual("Armature|fire", State(c, "attack").motion.name);
            AddMotionStates(c, C06);
            CollectionAssert.AreEqual(first, c.layers[0].stateMachine.states.Select(s => s.state));
        }
        finally { Object.DestroyImmediate(c); }
    }

    /// <summary>A take named walk wins over C05's idle_walk whichever comes first; idle_walk is used only alone.</summary>
    [Test]
    public void TheWalkTakeIsChosenByNameNotByOrder()
    {
        var walk = new AnimationClip { name = "Armature|walk" };
        var idleWalk = new AnimationClip { name = "Armature|idle_walk" };
        var run = new AnimationClip { name = "Armature|run" };
        try
        {
            Assert.AreSame(walk, WalkTake(new[] { idleWalk, run, walk }));
            Assert.AreSame(walk, WalkTake(new[] { walk, idleWalk }));
            Assert.AreSame(idleWalk, WalkTake(new[] { run, idleWalk }));
            Assert.IsNull(WalkTake(new[] { run }));
        }
        finally { Object.DestroyImmediate(walk); Object.DestroyImmediate(idleWalk); Object.DestroyImmediate(run); }
    }

    /// <summary>A generated figure's controller is checked whole before anything in it changes.</summary>
    [TestCase("empty")]
    [TestCase("two-layers")]
    [TestCase("duplicate-idle")]
    [TestCase("duplicate-walk")]
    [TestCase("unknown-state")]
    [TestCase("walk-default")]
    [TestCase("missing-idle")]
    public void ABadGeneratedControllerIsRefusedUntouched(string layout)
    {
        var c = new AnimatorController(); c.AddLayer("Base Layer");
        var before = new AnimationClip { name = "before" };
        var idle = new AnimationClip { name = "idle" };
        try
        {
            var machine = c.layers[0].stateMachine;
            AnimatorState Add(string name) { var s = machine.AddState(name); s.motion = before; return s; }
            switch (layout)
            {
                case "empty": break;
                case "two-layers": machine.defaultState = Add("idle"); c.AddLayer("Second"); break;
                case "duplicate-idle": machine.defaultState = Add("idle"); machine.AddState("x").name = "idle"; break;
                case "duplicate-walk": machine.defaultState = Add("idle"); Add("walk"); machine.AddState("x").name = "walk"; break;
                case "unknown-state": machine.defaultState = Add("idle"); Add("run"); break;
                case "walk-default": Add("idle"); machine.defaultState = Add("walk"); break;
                case "missing-idle": machine.defaultState = Add("walk"); break;
            }
            foreach (var s in machine.states) s.state.motion = before;
            var states = machine.states.Select(s => s.state).ToArray();
            var defaultState = machine.defaultState;
            int layers = c.layers.Length;
            Assert.Throws<InvalidOperationException>(() => Configure(c, idle, C06, token: true), layout);
            CollectionAssert.AreEqual(states, c.layers[0].stateMachine.states.Select(s => s.state), "no state added");
            Assert.AreSame(defaultState, c.layers[0].stateMachine.defaultState, "the default unchanged");
            Assert.AreEqual(layers, c.layers.Length);
            foreach (var s in states) Assert.AreSame(before, s.motion, "no take changed");
            if (defaultState != null) Assert.AreSame(before, defaultState.motion);
        }
        finally { Object.DestroyImmediate(c); Object.DestroyImmediate(before); Object.DestroyImmediate(idle); }
    }

    /// <summary>A default that is none or another machine's state (review of #1900). Unity's state machine does not keep
    /// such a default: the full EditMode run of this re-implementation found the importer's check passing both, so Unity
    /// reports a default of the machine's own. Either way the importer must end with the owned idle as the default, its
    /// take the figure's idle, or refuse with nothing changed.</summary>
    [TestCase("missing-default")]
    [TestCase("foreign-default")]
    public void ADefaultOutsideTheMachineEndsOnTheOwnedIdleOrIsRefused(string layout)
    {
        var c = new AnimatorController(); c.AddLayer("Base Layer");
        var before = new AnimationClip { name = "before" };
        var idle = new AnimationClip { name = "idle" };
        var foreign = new AnimatorStateMachine();
        try
        {
            var machine = c.layers[0].stateMachine;
            var own = machine.AddState("idle"); own.motion = before;
            if (layout == "missing-default") machine.defaultState = null;
            else { var other = foreign.AddState("idle"); other.motion = before; machine.defaultState = other; }
            var seen = machine.defaultState;
            bool normalised = seen == own;
            Debug.Log($"{layout}: Unity reports the default as {(seen == null ? "none" : seen == own ? "the machine's own idle" : "another machine's state")}");
            if (normalised)
            {
                Configure(c, idle, C06, token: false);
                Assert.AreSame(own, machine.defaultState);
                Assert.AreSame(idle, own.motion, "configured as any owned idle");
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Configure(c, idle, C06, token: false));
                Assert.AreSame(seen, machine.defaultState, "the default unchanged");
                Assert.AreSame(before, own.motion, "no take changed");
                CollectionAssert.AreEqual(new[] { own }, machine.states.Select(s => s.state), "no state added");
            }
        }
        finally { Object.DestroyImmediate(c); Object.DestroyImmediate(before); Object.DestroyImmediate(idle); Object.DestroyImmediate(foreign); }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AnIdleOnlyGeneratedControllerGainsTheStatesOnlyForATokenFigure(bool token)
    {
        var c = new AnimatorController(); c.AddLayer("Base Layer");
        var idle = new AnimationClip { name = "idle" };
        try
        {
            var machine = c.layers[0].stateMachine;
            var original = machine.AddState("idle"); machine.defaultState = original;
            Configure(c, idle, C06, token);
            CollectionAssert.AreEquivalent(token ? new[] { "idle", "walk", "attack" } : new[] { "idle" },
                machine.states.Select(s => s.state.name));
            Assert.AreSame(original, machine.defaultState);
            Assert.AreSame(idle, original.motion);
            var states = machine.states.Select(s => s.state).ToArray();
            Configure(c, idle, C06, token);
            CollectionAssert.AreEqual(states, machine.states.Select(s => s.state), "a second run adds nothing");
        }
        finally { Object.DestroyImmediate(c); Object.DestroyImmediate(idle); }
    }
}
