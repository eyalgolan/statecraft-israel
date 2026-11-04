using NUnit.Framework;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

/// <summary>#2374: C05's code-driven idle leans his held pose from the feet and breathes with the chest by small, slow turns,
/// starts each figure at its own deterministic point in the cycle, puts everything back exactly when turned off, and
/// allocates nothing per frame.</summary>
public sealed class UnitFigureIdleTests
{
    GameObject token = null!;

    [SetUp] public void Make() => token = new GameObject("givati");
    [TearDown] public void Free() => Object.DestroyImmediate(token);

    /// <summary>A figure piece as UnitLayer builds one, with the model holder UnitFigures.Swap makes and a rig of the bones the
    /// idle reads (under the FBX axis turn, at rest turns of their own, as Tal Heuberger's held pose has).</summary>
    (Transform piece, Transform model, Transform hips, Transform chest, Transform neck) Figure(string name = "Figure2", bool chestBone = true)
    {
        var piece = new GameObject(name).transform;
        piece.SetParent(token.transform, false);
        piece.localRotation = Quaternion.Euler(0f, 30f, 0f); piece.localScale = Vector3.one * UnitShowcase.FigureScale;
        var holder = new GameObject(UnitFigures.ModelName).transform;
        holder.SetParent(piece, false); holder.localScale = Vector3.one * 0.9f;
        var model = new GameObject("c05").transform;
        model.SetParent(holder, false); model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        Transform Bone(Transform parent, string bone, Quaternion rest)
        {
            var t = new GameObject(bone).transform;
            t.SetParent(parent, false); t.localRotation = rest; t.localPosition = Vector3.up * 0.3f;
            return t;
        }
        var armature = Bone(model, UnitFigureIdle.ArmatureNode, Quaternion.identity);
        var hips = Bone(armature, "Hips", Quaternion.Euler(3f, 170f, -2f));
        var chest = Bone(hips, chestBone ? UnitFigureIdle.ChestBone : "Spine", Quaternion.Euler(-4f, 1f, 2f));
        var neck = Bone(chest, UnitFigureIdle.NeckBone, Quaternion.Euler(6f, -3f, 0f));
        Bone(neck, "Head", Quaternion.Euler(2f, 0f, 0f));
        return (piece, model, hips, chest, neck);
    }

    [Test]
    public void TheOffsetIsDeterministicBoundedAndDiffersBetweenFigures()
    {
        Assert.AreEqual(UnitFigureIdle.Phase("givati/Figure2"), UnitFigureIdle.Phase("givati/Figure2"));
        var keys = new[] { "givati/Figure2", "carmeli/Figure2", "givati/Rifleman2", "alexandroni/Figure2", "etzioni/Figure2" };
        for (int i = 0; i < keys.Length; i++)
        {
            Assert.That(UnitFigureIdle.Phase(keys[i]), Is.InRange(0f, 0.9999f), keys[i]);
            for (int j = 0; j < i; j++)
                Assert.Greater(Mathf.Abs(UnitFigureIdle.Phase(keys[i]) - UnitFigureIdle.Phase(keys[j])), 0.02f, keys[i] + " vs " + keys[j]);
        }
        Assert.AreEqual(0.6592f, UnitFigureIdle.Phase("givati/Figure2"), 1e-4f, "pinned: the same offset on every platform and after every rebuild");

        var (piece, model, _, _, _) = Figure();
        var idle = UnitFigureIdle.Equip(model, UnitFigureIdle.KeyOf(piece));
        Assert.IsNotNull(idle);
        Assert.AreEqual("givati/Figure2", UnitFigureIdle.KeyOf(piece), "keyed by the token and the place on it");
        Assert.AreEqual(UnitFigureIdle.Phase("givati/Figure2"), idle!.Offset);
        Assert.IsFalse(idle.enabled, "off until the zoom switch turns it on");

        // Two figures at the same moment are out of step: at some point in a breath their chests differ clearly.
        float other = UnitFigureIdle.Phase("carmeli/Figure2"), apart = 0f;
        for (float t = 0f; t < UnitFigureIdle.BreathS; t += 0.05f)
        {
            UnitFigureIdle.Evaluate(t, idle.Offset, out float a, out _);
            UnitFigureIdle.Evaluate(t, other, out float b, out _);
            apart = Mathf.Max(apart, Mathf.Abs(a - b));
        }
        Assert.Greater(apart, 0.3f, "degrees apart at the most");
    }

    [Test]
    public void TheIdleLeansFromTheFeetAndBreathesAboutTheFiguresOwnAxesAndPutsEverythingBackExactly()
    {
        var (piece, model, hips, chest, neck) = Figure();
        var rest = model.localRotation; var feet = model.localPosition;
        var hipsRest = hips.localRotation; var chestRest = chest.localRotation; var neckRest = neck.localRotation;
        var headRest = neck.GetChild(0).localRotation;
        var idle = UnitFigureIdle.Equip(model, UnitFigureIdle.KeyOf(piece))!;
        idle.enabled = true;
        float breathMost = 0f, swayMost = 0f;
        for (float t = 0f; t < 2f * UnitFigureIdle.SwayS; t += 0.05f)
        {
            idle.Sample(t);
            float breath = Quaternion.Angle(chestRest, chest.localRotation), sway = Quaternion.Angle(rest, model.localRotation);
            Assert.LessOrEqual(breath, UnitFigureIdle.BreathDeg + 0.01f, "chest at " + t);
            Assert.LessOrEqual(sway, UnitFigureIdle.SwayDeg + 0.01f, "lean at " + t);
            Assert.LessOrEqual(Quaternion.Angle(neckRest, neck.localRotation), UnitFigureIdle.HeadDeg + 0.01f, "neck at " + t);
            breathMost = Mathf.Max(breathMost, breath); swayMost = Mathf.Max(swayMost, sway);
            Assert.AreEqual(feet, model.localPosition, "the feet stay planted: the lean pivots on the model's root");
            Assert.IsTrue(hipsRest.Equals(hips.localRotation), "the hips are not turned (a hip roll would drag the feet)");
            // The chest pitches about the figure's right axis and the figure leans about its forward axis, whatever the bones'
            // frames (read at the larger turns, with the axis normalized: ToAngleAxis is coarse under a quarter of a degree).
            var pitch = chest.localRotation * Quaternion.Inverse(chestRest);
            pitch.ToAngleAxis(out float pitchDeg, out var pitchAxis);
            if (pitchDeg > 0.5f * UnitFigureIdle.BreathDeg) Assert.Greater(Mathf.Abs(Vector3.Dot(chest.parent.TransformDirection(pitchAxis).normalized, piece.right)), 0.999f, "chest axis at " + t);
            var lean = model.localRotation * Quaternion.Inverse(rest);
            lean.ToAngleAxis(out float leanDeg, out var leanAxis);
            if (leanDeg > 0.5f * UnitFigureIdle.SwayDeg) Assert.Greater(Mathf.Abs(Vector3.Dot(model.parent.TransformDirection(leanAxis).normalized, piece.forward)), 0.999f, "lean axis at " + t);
        }
        Assert.Greater(breathMost, 0.9f * UnitFigureIdle.BreathDeg, "it breathes");
        Assert.Greater(swayMost, 0.9f * UnitFigureIdle.SwayDeg, "and shifts its weight");
        Assert.IsTrue(headRest.Equals(neck.GetChild(0).localRotation), "the head's own turn is untouched");
        idle.enabled = false;
        Assert.IsTrue(rest.Equals(model.localRotation), "the figure stands as it did, exactly");
        Assert.IsTrue(chestRest.Equals(chest.localRotation), "chest back at rest, exactly");
        Assert.IsTrue(neckRest.Equals(neck.localRotation), "neck back at rest, exactly");
    }

    [Test]
    public void AModelWithoutTheBonesStandsStill()
    {
        var (piece, model, _, _, _) = Figure(chestBone: false);
        Assert.IsNull(UnitFigureIdle.Equip(model, UnitFigureIdle.KeyOf(piece)), "no chest: fail open");
        Assert.IsFalse(model.TryGetComponent<UnitFigureIdle>(out _));
        Assert.IsTrue(UnitFigureIdle.Wanted(UnitFigures.PioneerVeteran));
        Assert.IsFalse(UnitFigureIdle.Wanted(UnitFigures.SabraPioneer)); Assert.IsFalse(UnitFigureIdle.Wanted(null));
    }

    [Test]
    public void SamplingAFrameAllocatesNothing()
    {
        var (piece, model, _, _, _) = Figure();
        var idle = UnitFigureIdle.Equip(model, UnitFigureIdle.KeyOf(piece))!;
        idle.enabled = true;
        float t = 0f;
        TestDelegate frames = () => { for (int i = 0; i < 100; i++) { t += 1f / 60f; idle.Sample(t); } };
        frames();   // the delegate's first call JIT-compiles it (PR 2351); the measured call below still samples 100 frames
        Assert.That(frames, Is.Not.AllocatingGCMemory());
        Assert.Greater(t, 3f);
    }
}
