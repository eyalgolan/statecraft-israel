using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.Map.Units;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>#1866 and #1867 (design by Tal Heuberger, #1900): a token's slide, its figures' walk and idle, and the
/// one-shot attack, on real Animators with throwaway controllers.</summary>
public class UnitTokenMotionTests
{
    GameObject token = null!;
    AnimatorController controller = null!;
    AnimationClip idle = null!, walk = null!, fire = null!;

    [SetUp]
    public void Make()
    {
        token = new GameObject("token");
        controller = new AnimatorController(); controller.AddLayer("Base Layer");
        idle = new AnimationClip { name = "idle" }; walk = new AnimationClip { name = "walk" };
        fire = new AnimationClip { name = "Armature|fire" };
        fire.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 0.2f, 1f));   // a 0.2 s take
    }

    [TearDown]
    public void Free()
    {
        Object.DestroyImmediate(token); Object.DestroyImmediate(controller);
        Object.DestroyImmediate(idle); Object.DestroyImmediate(walk); Object.DestroyImmediate(fire);
    }

    /// <summary>A figure under the token with the idle default and, as asked, a walk and an attack state.</summary>
    Animator Figure(bool walkState = true, bool attackState = true)
    {
        var machine = controller.layers[0].stateMachine;
        machine.defaultState = machine.AddState("idle"); machine.defaultState.motion = idle;
        if (walkState) machine.AddState("walk").motion = walk;
        if (attackState) machine.AddState("attack").motion = fire;
        var figure = new GameObject("figure"); figure.transform.SetParent(token.transform, false);
        var animator = figure.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = true;
        animator.Rebind(); animator.Update(0f);
        return animator;
    }

    static bool In(Animator a, int state) { a.Update(0f); return a.GetCurrentAnimatorStateInfo(0).fullPathHash == state; }

    [Test]
    public void PlaySwitchesStatesAndFailsOpenWithoutOne()
    {
        var a = Figure(attackState: false);
        Assert.IsTrue(UnitFigureMotion.Play(a, UnitFigureMotion.Walk));
        Assert.IsTrue(In(a, UnitFigureMotion.Walk));
        Assert.IsFalse(a.applyRootMotion, "the token's offset moves the figure, not the take's root");
        Assert.IsTrue(UnitFigureMotion.Play(a, UnitFigureMotion.Idle));
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
        Assert.IsFalse(UnitFigureMotion.Play(a, UnitFigureMotion.Attack), "no attack state: nothing plays");
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
        Assert.IsFalse(UnitFigureMotion.Play(null, UnitFigureMotion.Walk));
    }

    [Test]
    public void AnAnimatorThatCannotPlayIsLeftAlone()
    {
        var a = Figure();
        a.enabled = false;
        Assert.IsFalse(UnitFigureMotion.Play(a, UnitFigureMotion.Walk), "a disabled Animator (a far figure's near one)");
        a.enabled = true; a.gameObject.SetActive(false);
        Assert.IsFalse(UnitFigureMotion.Play(a, UnitFigureMotion.Walk), "an Animator on an inactive object (a token torn down)");
        a.gameObject.SetActive(true); a.runtimeAnimatorController = null;
        Assert.IsFalse(UnitFigureMotion.Play(a, UnitFigureMotion.Walk), "no controller");
    }

    [Test]
    public void ASlideWalksToTheUnitsPlaceThenStandsIdleAndStops()
    {
        var a = Figure();
        var motion = token.AddComponent<UnitTokenMotion>();
        int changes = 0; motion.Changed = () => changes++;
        motion.Begin(new Vector3(1000f, 0f, -500f));
        Assert.IsTrue(motion.Moving); Assert.IsTrue(motion.enabled);
        Assert.AreEqual(new Vector3(1000f, 0f, -500f), token.transform.position, "it starts where it was drawn");
        Assert.IsTrue(In(a, UnitFigureMotion.Walk));
        motion.Tick(UnitTokenMotion.Duration / 2f);
        Assert.That(Vector3.Distance(new Vector3(500f, 0f, -250f), token.transform.position), Is.LessThan(1e-3f));
        Assert.IsTrue(motion.enabled, "still sliding");
        motion.Tick(UnitTokenMotion.Duration);
        Assert.AreEqual(Vector3.zero, token.transform.position, "it ends exactly at the unit's place");
        Assert.IsFalse(motion.Moving);
        Assert.IsFalse(motion.enabled, "an idle token runs no LateUpdate");
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
        Assert.AreEqual(3, changes, "Begin and each step tell the layer to redraw the badge and boxes");
    }

    [Test]
    public void AMoveTooShortToSeeIsDrawnAtOnce()
    {
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        motion.Begin(Vector3.right * (UnitTokenMotion.MinMoveM / 2f));
        Assert.IsFalse(motion.Moving);
        Assert.IsFalse(motion.enabled);
        Assert.AreEqual(Vector3.zero, token.transform.position);
    }

    /// <summary>Every rebuild calls Begin on every token, most with no move (review of #2176, H5): a token that does not
    /// move and was not moving leaves its figures as they are.</summary>
    [Test]
    public void ANoMoveBeginLeavesAnIdleTokensFiguresAlone()
    {
        var a = Figure();
        UnitFigureMotion.Play(a, UnitFigureMotion.Walk);
        Assert.IsTrue(In(a, UnitFigureMotion.Walk), "fixture: the figure plays something else than idle");
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        int changes = 0; motion.Changed = () => changes++;
        motion.Begin(Vector3.zero);
        Assert.IsTrue(In(a, UnitFigureMotion.Walk), "no pose was forced on it");
        Assert.AreEqual(0, changes);
        Assert.IsFalse(motion.enabled);
    }

    [Test]
    public void DisablingMidSlidePutsTheTokenAtItsPlace()
    {
        var a = Figure();
        var motion = token.AddComponent<UnitTokenMotion>();
        int changes = 0;
        motion.Begin(Vector3.right * 1000f);
        motion.Changed = () => changes++;
        // EditMode sends no OnDisable to a script without ExecuteAlways: call it as the player would.
        typeof(UnitTokenMotion).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(motion, null);
        Assert.IsFalse(motion.Moving);
        Assert.AreEqual(Vector3.zero, token.transform.position);
        Assert.AreEqual(1, changes, "the layer moves the badge and boxes back with it");
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
    }

    [Test]
    public void AnAttackPlaysOnceThenTheFiguresWalkOnAndStandIdle()
    {
        var a = Figure();
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        motion.Begin(Vector3.right * 1000f);
        Assert.IsTrue(motion.PlayAttack());
        Assert.IsTrue(motion.Attacking); Assert.IsTrue(motion.enabled);
        Assert.IsTrue(In(a, UnitFigureMotion.Attack));
        motion.Tick(0.1f);
        Assert.IsTrue(In(a, UnitFigureMotion.Attack), "the 0.2 s take is still playing");
        motion.Tick(0.15f);
        Assert.IsFalse(motion.Attacking);
        Assert.IsTrue(In(a, UnitFigureMotion.Walk), "the slide is still under way, so the figure walks on");
        motion.Tick(1f);
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
        Assert.IsFalse(motion.enabled);
        Assert.IsTrue(motion.PlayAttack(), "a standing token attacks too");
        motion.Tick(0.05f);
        Assert.IsTrue(motion.PlayAttack(), "a second attack restarts the take");
        motion.Tick(0.15f);
        Assert.IsTrue(motion.Attacking, "the restarted take runs its full length");
        motion.Tick(0.1f);
        Assert.IsFalse(motion.Attacking); Assert.IsFalse(motion.enabled);
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
    }

    /// <summary>A rebuild for another unit's order hands a playing attack to the unit's new token (review of #2177, 3): it
    /// goes on from where it was, for what is left of the take, not from the start and not cut short.</summary>
    [Test]
    public void AContinuedAttackPlaysOnFromWhereItWas()
    {
        var a = Figure();
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        Assert.IsTrue(motion.PlayAttack());
        motion.Tick(0.05f);
        Assert.AreEqual(0.05f, motion.AttackPlayed, 1e-4f);
        var next = new GameObject("rebuilt token");
        try
        {
            var figure = new GameObject("figure"); figure.transform.SetParent(next.transform, false);
            var b = figure.AddComponent<Animator>(); b.runtimeAnimatorController = controller; b.Rebind(); b.Update(0f);
            var successor = next.AddComponent<UnitTokenMotion>();
            successor.enabled = false;
            successor.ContinueAttack(motion.AttackPlayed);
            Assert.IsTrue(successor.Attacking); Assert.IsTrue(successor.enabled);
            b.Update(0f);
            Assert.AreEqual(UnitFigureMotion.Attack, b.GetCurrentAnimatorStateInfo(0).fullPathHash);
            Assert.AreEqual(0.25f, b.GetCurrentAnimatorStateInfo(0).normalizedTime, 0.02f, "from 0.05 s into the 0.2 s take");
            successor.Tick(0.14f);
            Assert.IsTrue(successor.Attacking, "0.15 s of the take is left");
            successor.Tick(0.02f);
            Assert.IsFalse(successor.Attacking);
        }
        finally { Object.DestroyImmediate(next); }
    }

    [Test]
    public void AFigureWithoutAFireTakeDoesNotAttack()
    {
        var a = Figure(attackState: false);
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        Assert.IsFalse(motion.PlayAttack());
        motion.Tick(UnitTokenMotion.AttackWaitS);
        Assert.IsFalse(motion.Attacking); Assert.IsFalse(motion.enabled, "after the short wait nothing runs");
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));
    }

    /// <summary>An engagement rebuilds the tokens just before it plays, and the new ones turn their near figures on at the
    /// next render: the attack waits that long for them, and no longer.</summary>
    [Test]
    public void AnAttackWaitsBrieflyForAFigureThatTurnsNear()
    {
        var a = Figure();
        a.enabled = false;   // far: the near Animator is off
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        Assert.IsFalse(motion.PlayAttack(), "no figure can play it yet");
        Assert.IsTrue(motion.Attacking); Assert.IsTrue(motion.enabled);
        motion.Tick(0.05f);
        a.enabled = true; motion.RefreshPose();   // as UnitFigureDetail does when the figure turns near
        Assert.IsTrue(In(a, UnitFigureMotion.Attack));
        motion.Tick(0.15f);
        Assert.IsTrue(motion.Attacking, "the take's own 0.2 s, from when it started");
        motion.Tick(0.1f);
        Assert.IsFalse(motion.Attacking);
        Assert.IsTrue(In(a, UnitFigureMotion.Idle));

        a.enabled = false;
        Assert.IsFalse(motion.PlayAttack());
        motion.Tick(UnitTokenMotion.AttackWaitS);
        Assert.IsFalse(motion.Attacking); Assert.IsFalse(motion.enabled);
        a.enabled = true; motion.RefreshPose();
        Assert.IsTrue(In(a, UnitFigureMotion.Idle), "a figure that turns near later does not play an old attack");
    }

    [Test]
    public void AHiddenTokensAttackIsDroppedNotKept()
    {
        var a = Figure();
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        token.SetActive(false);
        Assert.IsFalse(motion.PlayAttack());
        Assert.IsFalse(motion.Attacking);
        token.SetActive(true);
        motion.RefreshPose();
        Assert.IsTrue(In(a, UnitFigureMotion.Idle), "shown again, it does not play the attack it missed");
    }
}
