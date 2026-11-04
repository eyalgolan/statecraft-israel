using NUnit.Framework;
using Statecraft.Game.Map.Units;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

/// <summary>#2373: the 65 mm gun fires in code when its token attacks (recoil, flash, smoke), from the same event as the
/// figures' fire takes, and comes back to rest exactly; the muzzle and the recoil axis are read from the model's own
/// barrel node, whichever way it points; a hidden gun drops its shot; sampling a frame allocates nothing.</summary>
public sealed class UnitGunFireTests
{
    GameObject token = null!;

    [SetUp] public void Make() => token = new GameObject("givati");
    [TearDown] public void Free() => Object.DestroyImmediate(token);

    /// <summary>A vehicle piece as UnitLayer builds one (scaled, headed west), with the model holder UnitFigures.Swap makes and
    /// a gun of three cubes under the FBX axis turn: a tube long along the gun's front, a body behind its middle, a trail
    /// behind that. With <paramref name="sideways"/> the gun is built along its own -X instead, to prove the barrel is
    /// found by its shape and not by a hand-pinned axis; with <paramref name="barrelNode"/> false it is one merged node.</summary>
    (Transform piece, Transform holder, Transform barrel) Gun(bool sideways = false, bool barrelNode = true, float elevationDeg = 0f)
    {
        var piece = new GameObject("Vehicle0").transform;
        piece.SetParent(token.transform, false);
        piece.localRotation = Quaternion.Euler(0f, 270f, 0f); piece.localScale = Vector3.one * UnitShowcase.FigureScale;
        var holder = new GameObject(UnitFigures.ModelName).transform;
        holder.SetParent(piece, false); holder.localScale = Vector3.one * 0.5f;
        var model = new GameObject("gun").transform;
        model.SetParent(holder, false); model.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // the FBX axis turn: local -Y is the holder's +Z, local +Z its +Y
        // In the holder's frame: the tube centred at z = 1 (or x = -1), 1 long and 0.1 across, 0.5 up; the body at 0.2; the trail at -0.8.
        Transform Cube(string name, Vector3 atHolder, Vector3 sizeHolder)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            go.transform.SetParent(model, false);
            go.transform.localPosition = new Vector3(atHolder.x, -atHolder.z, atHolder.y);
            go.transform.localScale = new Vector3(sizeHolder.x, sizeHolder.z, sizeHolder.y);
            return go.transform;
        }
        Vector3 Along(float d) => sideways ? new Vector3(-d, 0f, 0f) : new Vector3(0f, 0f, d);
        Vector3 Size(float along, float across, float up) => sideways ? new Vector3(along, up, across) : new Vector3(across, up, along);
        var barrel = Cube(barrelNode ? UnitGunFire.BarrelNode : "gun-merged", Along(1f) + Vector3.up * 0.5f, Size(1f, 0.1f, 0.1f));
        if (elevationDeg != 0f)
        {
            // Elevated about the holder's X through the tube's centre: the muzzle end rises (the tube still points along the front).
            var centre = barrel.position;
            barrel.RotateAround(centre, holder.TransformDirection(Vector3.right), -elevationDeg);
        }
        if (barrelNode)
        {
            Cube(UnitGunFire.BodyNode, Along(0.2f) + Vector3.up * 0.35f, Size(0.5f, 0.5f, 0.7f));
            Cube("wheel_tail", Along(-0.8f) + Vector3.up * 0.1f, Size(0.8f, 0.1f, 0.1f));
        }
        return (piece, holder, barrel);
    }

    [Test]
    public void OnlyTheGunGetsAShot()
    {
        var (piece, _, _) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.WillysJeep);
        Assert.IsFalse(piece.TryGetComponent<UnitGunFire>(out _), "the jeep does not fire");
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        Assert.IsTrue(piece.TryGetComponent<UnitGunFire>(out var gun), "the 65 mm gun does");
        Assert.IsFalse(gun.enabled, "and runs nothing until it fires");
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        Assert.AreEqual(1, piece.GetComponents<UnitGunFire>().Length, "equipped once");
    }

    [Test]
    public void TheMuzzleAndTheRecoilAxisComeFromTheBarrelNodeWhicheverWayItPoints()
    {
        var (piece, _, _) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        Assert.IsTrue(gun.Ready);
        Assert.Less(Vector3.Angle(Vector3.forward, gun.Direction), 0.01f, "the tube's long axis, towards the end farther from the body");
        Assert.That(Vector3.Distance(new Vector3(0f, 0.5f, 1.5f), gun.Muzzle), Is.LessThan(1e-3f), "the muzzle: the tube's far end");

        var (side, _, _) = Gun(sideways: true);
        UnitGunFire.Equip(side, UnitFigures.Napoleonchik);
        var sideGun = side.GetComponent<UnitGunFire>();
        Assert.IsTrue(sideGun.Ready);
        Assert.Less(Vector3.Angle(Vector3.left, sideGun.Direction), 0.01f, "a gun built along -X recoils along +X");
        Assert.That(Vector3.Distance(new Vector3(-1.5f, 0.5f, 0f), sideGun.Muzzle), Is.LessThan(1e-3f));

        var (raised, _, _) = Gun(elevationDeg: 15f);
        UnitGunFire.Equip(raised, UnitFigures.Napoleonchik);
        var raisedGun = raised.GetComponent<UnitGunFire>();
        Assert.IsTrue(raisedGun.Ready);
        Assert.Less(Vector3.Angle(raisedGun.Direction, new Vector3(0f, Mathf.Sin(15f * Mathf.Deg2Rad), Mathf.Cos(15f * Mathf.Deg2Rad))), 2f,
            "an elevated tube recoils up its own incline, read from its box (taller than the tube is wide by the rise)");
        var trueMuzzle = new Vector3(0f, 0.5f + 0.5f * Mathf.Sin(15f * Mathf.Deg2Rad), 1f + 0.5f * Mathf.Cos(15f * Mathf.Deg2Rad));
        Assert.That(Vector3.Distance(trueMuzzle, raisedGun.Muzzle), Is.LessThan(0.03f), "the muzzle at the high end of the tube");

        var (merged, _, _) = Gun(barrelNode: false);
        UnitGunFire.Equip(merged, UnitFigures.Napoleonchik);
        var mergedGun = merged.GetComponent<UnitGunFire>();
        Assert.IsTrue(mergedGun.Ready, "a model with no barrel node still fires");
        Assert.Less(Vector3.Angle(Vector3.forward, mergedGun.Direction), 0.01f, "whole, level, towards its front");
        Assert.That(Vector3.Distance(new Vector3(0f, 0.5f, 1.5f), mergedGun.Muzzle), Is.LessThan(1e-3f), "its front end");
    }

    [Test]
    public void AnAttackFiresTheGunAndItReturnsToRest()
    {
        var (piece, holder, barrel) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        var barrelRest = barrel.localPosition;
        float Along() => Vector3.Dot(holder.InverseTransformPoint(barrel.position) - new Vector3(0f, 0.5f, 1f), gun.Direction);
        float MuzzleHeight() => piece.InverseTransformPoint(holder.TransformPoint(gun.Muzzle)).y;
        float restHeight = MuzzleHeight();
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        Assert.IsTrue(motion.PlayAttack(), "the gun alone plays the token's attack");
        Assert.IsTrue(motion.Attacking); Assert.IsTrue(gun.Playing); Assert.IsTrue(gun.enabled);
        Assert.AreEqual(0f, gun.Time);
        var flash = piece.Find("MuzzleFlash").GetComponent<Renderer>();
        Assert.IsTrue(flash.enabled, "the flash shows from the shot");
        Assert.AreEqual(0f, Along(), 1e-4f, "the tube has not moved yet");

        gun.Tick(0.05f);
        Assert.Less(Along(), -0.1f, "the tube is kicked back along its axis");
        Assert.Less(Vector3.Dot(holder.localPosition, gun.Direction), 0f, "the carriage hops back");
        Assert.Greater(MuzzleHeight(), restHeight, "and noses up");
        Assert.IsTrue(flash.enabled);

        gun.Tick(0.1f);
        Assert.IsFalse(flash.enabled, "the flash is gone after a few frames");
        int shown = 0;
        for (int i = 0; i < UnitGunFire.Puffs; i++) if (piece.Find("MuzzleSmoke" + i).GetComponent<Renderer>().enabled) shown++;
        Assert.AreEqual(UnitGunFire.Puffs, shown, "every puff is in the air");
        var first = piece.Find("MuzzleSmoke0"); var firstAt = first.localPosition; var firstSize = first.localScale.x;
        gun.Tick(0.3f);
        Assert.Greater(Vector3.Dot(first.localPosition - firstAt, Vector3.forward), 0f, "the smoke rolls forward of the muzzle");
        Assert.Greater(first.localPosition.y, firstAt.y, "and rises");
        Assert.Greater(first.localScale.x, firstSize, "and spreads");

        motion.Tick(UnitGunFire.RecoilS - 0.01f);
        Assert.IsTrue(motion.Attacking, "the attack lasts the recoil");
        motion.Tick(0.02f);
        Assert.IsFalse(motion.Attacking); Assert.IsFalse(motion.enabled);
        gun.Tick(UnitGunFire.RecoilS);
        Assert.AreEqual(0f, Along(), 1e-4f, "the tube is back at rest after the recoil");
        Assert.AreEqual(Vector3.zero, holder.localPosition); Assert.AreEqual(Quaternion.identity, holder.localRotation);
        Assert.IsTrue(gun.Playing, "the smoke outlasts the attack");

        gun.Tick(UnitGunFire.TotalS);
        Assert.IsFalse(gun.Playing); Assert.IsFalse(gun.enabled, "the shot is over and the gun runs nothing");
        Assert.AreEqual(barrelRest, barrel.localPosition, "the tube is exactly where it was");
        Assert.AreEqual(Vector3.zero, holder.localPosition); Assert.AreEqual(Quaternion.identity, holder.localRotation);
        Assert.IsFalse(flash.enabled);
        for (int i = 0; i < UnitGunFire.Puffs; i++) Assert.IsFalse(piece.Find("MuzzleSmoke" + i).GetComponent<Renderer>().enabled, "puff " + i);
    }

    /// <summary>The envelopes behind the shot: the tube and the carriage start and end at rest and never leave their bounds;
    /// the carriage settles through a small forward overshoot; the flash fades to nothing by FlashS.</summary>
    [Test]
    public void TheRecoilEnvelopesStartAndEndAtRestAndStayBounded()
    {
        Assert.AreEqual(0f, UnitGunFire.Tube(0f)); Assert.AreEqual(0f, UnitGunFire.Tube(UnitGunFire.RecoilS));
        Assert.AreEqual(0f, UnitGunFire.Hop(0f)); Assert.AreEqual(0f, UnitGunFire.Hop(UnitGunFire.RecoilS));
        Assert.AreEqual(1f, UnitGunFire.Tube(UnitGunFire.TubeBackS), 1e-5f, "fully back at the end of the kick");
        float minHop = 1f;
        for (float t = 0f; t <= UnitGunFire.RecoilS; t += 0.001f)
        {
            Assert.That(UnitGunFire.Tube(t), Is.InRange(0f, 1f), "tube at " + t);
            Assert.That(UnitGunFire.Hop(t), Is.InRange(-0.1f, 1f), "hop at " + t);
            Assert.That(UnitGunFire.Flash(t), Is.InRange(0f, 1f));
            minHop = Mathf.Min(minHop, UnitGunFire.Hop(t));
        }
        Assert.Less(minHop, -0.02f, "the carriage settles through a forward overshoot");
        Assert.AreEqual(1f, UnitGunFire.Flash(0f)); Assert.AreEqual(0f, UnitGunFire.Flash(UnitGunFire.FlashS));
        UnitGunFire.Puff(0.5f, out float forward, out float rise, out float radius, out float alpha);
        Assert.Greater(forward, 0f); Assert.Greater(rise, 0f); Assert.Greater(radius, 0f); Assert.That(alpha, Is.InRange(0f, UnitGunFire.PuffAlpha));
        UnitGunFire.Puff(1f, out _, out _, out _, out float gone);
        Assert.AreEqual(0f, gone, 1e-5f, "a puff has faded by the end of its life");
    }

    /// <summary>A rebuild hands a playing attack to the unit's new token from where it was (UnitTokenMotion.ContinueAttack):
    /// the shot is a pure function of its time, so the new gun shows the same frame the old one did.</summary>
    [Test]
    public void AContinuedShotShowsTheSameFrame()
    {
        var (piece, holder, barrel) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        gun.Fire();
        gun.Sample(0.3f);
        var tube = barrel.localPosition; var hop = holder.localPosition; var smoke = piece.Find("MuzzleSmoke1").localPosition;
        gun.Sample(UnitGunFire.TotalS);
        Assert.IsFalse(gun.Playing);
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        motion.ContinueAttack(0.3f);
        Assert.IsTrue(gun.Playing); Assert.AreEqual(0.3f, gun.Time, 1e-5f);
        Assert.AreEqual(tube, barrel.localPosition); Assert.AreEqual(hop, holder.localPosition);
        Assert.AreEqual(smoke, piece.Find("MuzzleSmoke1").localPosition);
        motion.ContinueAttack(UnitGunFire.RecoilS + 0.1f);
        Assert.IsFalse(motion.Attacking, "past the recoil the attack had finished");
    }

    [Test]
    public void AHiddenGunDropsItsShotAtRest()
    {
        var (piece, holder, barrel) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        var rest = barrel.localPosition;
        gun.Fire(); gun.Sample(0.1f);
        Assert.AreNotEqual(rest, barrel.localPosition);
        token.SetActive(false);   // the units toggled off mid-shot
        Assert.IsFalse(gun.Playing); Assert.IsFalse(gun.enabled);
        Assert.AreEqual(rest, barrel.localPosition); Assert.AreEqual(Vector3.zero, holder.localPosition);
        token.SetActive(true);
        Assert.IsFalse(gun.Playing, "shown again, it does not go on with the shot it dropped");
        Assert.IsFalse(piece.Find("MuzzleFlash").GetComponent<Renderer>().enabled);
    }

    [Test]
    public void AGunWhoseModelHasNotArrivedDoesNotFire()
    {
        var piece = new GameObject("Vehicle0").transform;
        piece.SetParent(token.transform, false);
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        Assert.IsFalse(gun.Ready);
        var motion = token.AddComponent<UnitTokenMotion>();
        motion.enabled = false;
        Assert.IsFalse(motion.PlayAttack(), "nothing on the token can play it");
        Assert.IsFalse(gun.Playing); Assert.IsFalse(gun.enabled);
        Assert.IsNull(piece.Find("MuzzleFlash"), "nothing is built for a shot that never came");
    }

    [Test]
    public void SamplingAFrameAndFacingACameraAllocateNothing()
    {
        var (piece, _, _) = Gun();
        UnitGunFire.Equip(piece, UnitFigures.Napoleonchik);
        var gun = piece.GetComponent<UnitGunFire>();
        var camera = new GameObject("camera").AddComponent<Camera>();
        try
        {
            camera.transform.SetPositionAndRotation(new Vector3(0f, 2000f, -3000f), Quaternion.Euler(40f, 0f, 0f));
            gun.Fire();
            float t = 0f;
            TestDelegate frames = () =>
            {
                for (int i = 0; i < 100; i++) { t += 0.01f; gun.Sample(t); gun.Face(default, camera); }
            };
            // The exact measured delegate runs once first: Mono JIT-compiles it on its first call, and that one-time cost
            // belongs to the test, not to the shot (as CollectionResidencyViewAllocationTests does, PR 2351).
            frames();
            gun.Fire(); t = 0f;
            Assert.That(frames, Is.Not.AllocatingGCMemory());
            Assert.IsTrue(gun.Playing, "the measured frames were real: a second into the shot, the smoke is still fading");
        }
        finally { Object.DestroyImmediate(camera.gameObject); }
    }
}
