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

/// <summary>#2375 and #2376 (owner, 3 November 2026: the project has Tal Heuberger's permission for these adapted versions):
/// the token figures whose FBX has no fire take attack with a take baked beside their model from another of Tal Heuberger's
/// figures (CollectionImport.BorrowedFireTakes, FireTakeRetarget): C04 with a clip resampled per frame from C07's
/// Armature|fire take (tangents rebuilt, near-static bones dropped), the Syrian, Iraqi,
/// Egyptian, Lebanese and ALA infantry with C09's through Humanoid, each on its own rig, so its own bundle carries it.</summary>
public class BorrowedFireTakeTests
{
    static Type Import => Type.GetType("Statecraft.Game.EditorTools.CollectionImport, Assembly-CSharp-Editor")!;
    const BindingFlags Any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static IReadOnlyDictionary<string, string> Borrowed => (IReadOnlyDictionary<string, string>)Import.GetField("BorrowedFireTakes", Any)!.GetValue(null);
    static IReadOnlyCollection<string> TokenFigures => (IReadOnlyCollection<string>)Import.GetField("TokenFigures", Any)!.GetValue(null);
    static string FireTakePath(string id) => (string)Import.GetMethod("FireTakePath", Any)!.Invoke(null, new object[] { id });

    static string ControllerPath(string id) => CollectionCatalog.IsFullQualityGeneratedInfantry(id)
        ? $"Assets/Art/Collection/{id}/{id}.controller" : $"Assets/Art/Collection/{id}/{id}-near.controller";
    static string ModelPath(string id) => $"Assets/Art/Collection/{id}/{id}.fbx";
    static AnimationClip SourceFireTake(string id) => AssetDatabase.LoadAllAssetsAtPath(ModelPath(id)).OfType<AnimationClip>()
        .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) && UnitTokenMotion.CollectionFireTake(c.name));
    static AnimatorState Attack(string id) => AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(id))
        .layers[0].stateMachine.states.Select(s => s.state).SingleOrDefault(s => s.name == UnitFigureMotion.AttackState);
    static Transform Hand(Transform root, bool right) => root.GetComponentsInChildren<Transform>(true)
        .Single(t => t.name == (right ? "Right_Hand" : "Left_Hand") || t.name == (right ? "mixamorig:RightHand" : "mixamorig:LeftHand"));

    static readonly string[] Enemies = { UnitFigures.SyrianInfantry, UnitFigures.IraqiInfantry, UnitFigures.EgyptianInfantry, UnitFigures.AlaInfantry, UnitFigures.LebaneseRegular };

    /// <summary>The two issues' figures, each borrowing from the figure the issue names, and no figure that has its own take.</summary>
    [Test]
    public void TheIssuesFiguresBorrowFromTheFiguresTheIssuesName()
    {
        Assert.AreEqual(UnitFigures.VeteranFighter, Borrowed[UnitFigures.SabraPioneer], "#2375: C04 with C06/C07's take (C07's, the one players see on C07)");
        foreach (var id in Enemies) Assert.AreEqual(UnitFigures.Irregular, Borrowed[id], "#2376: " + id + " with C09's take");
        CollectionAssert.AreEquivalent(Enemies.Append(UnitFigures.SabraPioneer), Borrowed.Keys);
        foreach (var pair in Borrowed)
        {
            Assert.That(TokenFigures, Has.Member(pair.Key), pair.Key + " stands on tokens");
            Assert.IsFalse(AssetDatabase.LoadAllAssetsAtPath(ModelPath(pair.Key)).OfType<AnimationClip>().Any(c => UnitTokenMotion.CollectionFireTake(c.name)),
                pair.Key + " has no fire take of its own");
            Assert.IsNotNull(SourceFireTake(pair.Value), pair.Value + " has one to lend");
        }
    }

    /// <summary>Each borrowing figure's committed controller has an attack state on the clip baked beside its model: a
    /// Generic, non-looping take named so UnitTokenMotion counts it as a fire take, as long as the source's.</summary>
    [Test]
    public void EveryBorrowingFigureAttacksWithTheTakeBakedBesideItsModel()
    {
        foreach (var pair in Borrowed)
        {
            string id = pair.Key;
            var state = Attack(id);
            Assert.IsNotNull(state, id + ": attack state");
            var clip = state.motion as AnimationClip;
            Assert.IsNotNull(clip, id + ": the attack state plays a clip");
            Assert.AreEqual(FireTakePath(id), AssetDatabase.GetAssetPath(clip), id + ": the baked take beside its own model");
            Assert.IsTrue(UnitTokenMotion.CollectionFireTake(clip.name), id + ": named as a fire take, " + clip.name);
            Assert.IsFalse(clip.isLooping, id + ": a one-shot take");
            Assert.IsFalse(clip.isHumanMotion, id + ": plays on the figure's own Generic rig");
            Assert.IsFalse(clip.legacy, id);
            var source = SourceFireTake(pair.Value);
            Assert.AreEqual(source.frameRate, clip.frameRate, id + ": the source's frame rate");
            Assert.AreEqual(source.length, clip.length, 1e-3f, id + ": the source's length");
            Assert.Greater(AnimationUtility.GetCurveBindings(clip).Length, 0, id + ": curves");
        }
    }

    /// <summary>Every curve of a baked take binds a bone of the figure's own game prefab, and the take moves its hips and both
    /// arms (a retarget that mapped no arm would leave the rifle still).</summary>
    [Test]
    public void EveryBakedTakeBindsTheFiguresOwnRigAndMovesItsArms()
    {
        foreach (var id in Borrowed.Keys)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            var root = prefab.GetComponentInChildren<Animator>(true).transform;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FireTakePath(id));
            var paths = AnimationUtility.GetCurveBindings(clip).Select(b => b.path).Distinct().ToArray();
            foreach (var path in paths) Assert.IsNotNull(root.Find(path), id + ": " + path);
            var animated = paths.Select(p => root.Find(p).name).ToArray();
            foreach (var bone in new[] { "Hips", "Left_UpperArm", "Right_UpperArm", "Left_Hand", "Right_Hand" })
                Assert.IsTrue(animated.Contains(bone) || animated.Contains("mixamorig:" + bone.Replace("_UpperArm", "Arm").Replace("_", "")),
                    id + ": the take moves " + bone + " (animated: " + string.Join(", ", animated) + ")");
        }
    }

    /// <summary>A token's attack plays the baked take on the figure as on any figure with a take of its own (UnitTokenMotion):
    /// its Animator enters the attack state, the take moves its hands, and the attack lasts the take's length.</summary>
    [Test]
    public void AnAttackPlaysTheBakedTakeOnTheFigure()
    {
        foreach (var id in Borrowed.Keys)
        {
            var token = new GameObject("token " + id);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
                var figure = (GameObject)PrefabUtility.InstantiatePrefab(prefab, token.transform);
                var animator = figure.GetComponentInChildren<Animator>(true);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0f);
                var right = Hand(figure.transform, true); var left = Hand(figure.transform, false);
                Vector3 rightIdle = right.position, leftIdle = left.position;
                var motion = token.AddComponent<UnitTokenMotion>();
                motion.enabled = false;
                Assert.IsTrue(motion.PlayAttack(), id + ": a figure can play the attack");
                Assert.IsTrue(motion.Attacking, id);
                animator.Update(0f);
                Assert.AreEqual(UnitFigureMotion.Attack, animator.GetCurrentAnimatorStateInfo(0).fullPathHash, id + ": in the attack state");
                float length = UnitTokenMotion.FireLength(animator);
                Assert.Greater(length, 0.5f, id + ": the take's length");
                motion.Tick(length / 2f); animator.Update(length / 2f);
                Assert.IsTrue(motion.Attacking, id + ": half way through");
                Assert.Greater((right.position - rightIdle).magnitude + (left.position - leftIdle).magnitude, 0.02f, id + ": the take moves the hands");
                motion.Tick(length);
                Assert.IsFalse(motion.Attacking, id + ": over after the take's length");
            }
            finally { Object.DestroyImmediate(token); }
        }
    }

    /// <summary>C04's take is resampled per frame from C07's Armature|fire take (tangents rebuilt, near-static bones dropped):
    /// the two share Tal Heuberger's 67-bone Armature, so the source take binds C04's bones by path; every bone, at every
    /// frame, is where C07's take puts it (a bone the bake leaves out is one the take holds at the rig's rest pose).</summary>
    [Test]
    public void TheSabraPioneerPlaysTheVeteranFightersTakePoseForPose()
    {
        var baked = AssetDatabase.LoadAssetAtPath<AnimationClip>(FireTakePath(UnitFigures.SabraPioneer));
        var source = SourceFireTake(UnitFigures.VeteranFighter);
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(UnitFigures.SabraPioneer)));
        try
        {
            var bones = go.GetComponentsInChildren<Transform>(true).Where(t => t != go.transform).ToArray();
            var rest = bones.Select(b => (b.localPosition, b.localRotation)).ToArray();
            int frames = Mathf.RoundToInt(source.length * source.frameRate) + 1;
            Assert.AreEqual(37, frames, "Tal's 37-frame take");
            for (int f = 0; f < frames; f++)
            {
                float t = f / source.frameRate;
                source.SampleAnimation(go, t);
                var expected = bones.Select(b => (b.localPosition, b.localRotation)).ToArray();
                for (int b = 0; b < bones.Length; b++) { bones[b].localPosition = rest[b].localPosition; bones[b].localRotation = rest[b].localRotation; }
                baked.SampleAnimation(go, t);
                for (int b = 0; b < bones.Length; b++)
                {
                    Assert.Less(Quaternion.Angle(expected[b].localRotation, bones[b].localRotation), 0.05f, $"{bones[b].name} at frame {f}: rotation");
                    Assert.Less((expected[b].localPosition - bones[b].localPosition).magnitude, 1e-4f, $"{bones[b].name} at frame {f}: position");
                }
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>A baked take keys every frame of the source take on each bone the take moves and holds a still bone on two
    /// keys spanning the take; the arms of a retargeted one turn over the take (the aim and recoil), not only hold.</summary>
    [Test]
    public void ABakedTakeKeysEverySourceFrameAndARetargetedOneMovesTheArms()
    {
        foreach (var pair in Borrowed)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FireTakePath(pair.Key));
            var source = SourceFireTake(pair.Value);
            int frames = Mathf.RoundToInt(source.length * source.frameRate) + 1;
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, b);
                string what = pair.Key + ": " + b.path + " " + b.propertyName;
                if (curve.length == 2)
                {
                    Assert.AreEqual(curve[0].value, curve[1].value, what + " held still");
                    Assert.AreEqual((frames - 1) / source.frameRate, curve[1].time, 1e-5f, what + " held to the end");
                }
                else Assert.AreEqual(frames, curve.length, what + " keyed every frame");
            }
        }
        foreach (var id in Enemies)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FireTakePath(id));
            int frames = Mathf.RoundToInt(clip.length * clip.frameRate) + 1;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollectionCatalog.PrefabPath(id, CollectionDetail.Near));
            var go = Object.Instantiate(prefab);
            try
            {
                var animator = go.GetComponentInChildren<Animator>(true);
                var right = Hand(go.transform, true);
                var shoulder = right.parent.parent.parent;   // hand, forearm, upper arm, shoulder
                clip.SampleAnimation(animator.gameObject, 0f);
                var start = Quaternion.Inverse(shoulder.rotation) * right.rotation;
                float swing = 0f;
                for (int f = 1; f < frames; f++)
                {
                    clip.SampleAnimation(animator.gameObject, f / clip.frameRate);
                    swing = Mathf.Max(swing, Quaternion.Angle(start, Quaternion.Inverse(shoulder.rotation) * right.rotation));
                }
                Assert.Greater(swing, 0.5f, id + ": the right hand turns against the shoulder over the take (" + swing.ToString("F2") + " degrees)");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
