#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The train's steam (owner, 4 November 2026: "Train steam"; TrainSteam): a few pooled flipbook puffs from the
/// locomotive's chimney only while the train runs, each drawn from where the chimney was when it left and drifting back
/// along the track, the same at the same time, nothing per frame allocated, nothing while hidden, and freed with the train.
/// The train here is the moving showcase (RouteTrain.Spawn) on a straight line east over flat ground.</summary>
public class TrainSteamTests
{
    const float LineM = 60000f, TurnM = 50000f;

    readonly List<Object> made = new List<Object>();

    [TearDown]
    public void Restore()
    {
        TrainSteam.Clock = () => Time.time;
        foreach (var o in made) if (o != null) Object.DestroyImmediate(o);
        made.Clear();
    }

    RouteTrain Train(bool still = false)
    {
        var data = new TerrainData { heightmapResolution = 33, size = new Vector3(LineM + 2000f, 100f, 4000f) };
        made.Add(data);
        var root = new GameObject("test map"); made.Add(root);
        var assets = root.AddComponent<MapRuntimeAssets>();
        var line = new List<Vector2> { new Vector2(0f, 0f), new Vector2(LineM, 0f) };
        var origin = new Vector3(-1000f, 0f, -2000f);
        var go = RouteTrain.Spawn(root.transform, assets, line, data, origin, new Vector3(TurnM, 0f, 0f), new Vector3(20000f, 0f, 0f), still);
        return go.GetComponent<RouteTrain>();
    }

    static TrainSteam SteamOf(RouteTrain train)
    {
        var s = train.GetComponentInChildren<TrainSteam>(true);
        Assert.IsNotNull(s, "the moving train carries its steam (the smoke flipbook ships in Resources/Effects, #2490)");
        return s!;
    }

    static int Shown(TrainSteam s) => s.Renderers.Count(r => r.enabled);

    /// <summary>The puffs a train running since long before <paramref name="now"/> shows: the last Puffs, less the steam
    /// puffs already gone (a steam puff thins away by SteamLifeShare of the life).</summary>
    static int Trail(float now)
    {
        long newest = (long)Math.Floor(now / (double)TrainSteam.GapS);
        int n = 0;
        for (long k = newest - TrainSteam.Puffs + 1; k <= newest; k++)
            if (!TrainSteam.IsSteam(k) || now - k * (double)TrainSteam.GapS < TrainSteam.SteamLifeShare * TrainSteam.LifeS) n++;
        return n;
    }

    /// <summary>The first moment of the first stop: the head reaches the turn point (HeadAt's run, from the line's start).</summary>
    static float FirstStop(RouteTrain t) => (t.Stretch.end - t.Stretch.start - t.Length) / RouteTrain.SpeedMps;

    [Test]
    public void ItSteamsOnlyWhileTheTrainRuns()
    {
        var train = Train(); var steam = SteamOf(train);
        Assert.AreEqual(TrainSteam.Puffs, steam.Renderers.Length, "a small fixed pool");
        Assert.AreEqual(0, Shown(steam), "nothing before the first sample");
        Assert.IsTrue(train.MovingAt(10f));
        steam.Sample(10f);
        Assert.AreEqual(Trail(10f), Shown(steam), "running for 10 s: the whole trail");
        Assert.GreaterOrEqual(Shown(steam), TrainSteam.Puffs - 3, "nearly every slot");
        float stop = FirstStop(train);
        Assert.IsFalse(train.MovingAt(stop + 1f)); Assert.IsFalse(train.MovingAt(stop + RouteTrain.DwellSeconds - 0.1f));
        steam.Sample(stop + 1f);
        Assert.AreEqual(0, Shown(steam), "standing at the end: no steam");
        float leave = stop + RouteTrain.DwellSeconds + 0.05f;
        Assert.IsTrue(train.MovingAt(leave));
        steam.Sample(leave);
        Assert.LessOrEqual(Shown(steam), 1, "off again: only a puff left since it moved, none from the stop");
        steam.Sample(leave + TrainSteam.LifeS);
        Assert.AreEqual(Trail(leave + TrainSteam.LifeS), Shown(steam), "and the trail grows back");
    }

    [Test]
    public void AStillTrainHasNoSteam()
    {
        var train = Train(still: true);
        Assert.IsNull(train.GetComponentInChildren<TrainSteam>(true), "a still train (screenshots, editor renders) never runs");
        Assert.IsFalse(train.MovingAt(10f));
    }

    [Test]
    public void ThePuffsTrailBackFromTheChimney()
    {
        var train = Train(); var steam = SteamOf(train);
        const float now = 20f;
        var (head, dir) = train.HeadAt(now);
        Assert.AreEqual(+1, dir, "heading east, to the turn point");
        var (chimney, back) = train.ChimneyAt(head, dir);
        Assert.AreEqual(-1f, back.x, 1e-4f, "back is west along the line");
        train.Place(head, dir);
        var loco = train.Cars[0];
        Assert.Greater(chimney.x, loco.position.x, "the chimney is ahead of the locomotive's middle");
        Assert.Less(chimney.x, loco.position.x + train.CarOffsets[0], "and on the locomotive, behind the head");
        steam.Sample(now);
        var puffs = steam.Renderers.Where(r => r.enabled).Select(r => r.transform.position).OrderByDescending(p => p.x).ToList();
        Assert.AreEqual(Trail(now), puffs.Count);
        foreach (var p in puffs)
        {
            Assert.LessOrEqual(p.x, chimney.x + 1f, "every puff is behind the chimney");
            Assert.Greater(p.y, chimney.y, "and above it");
            Assert.AreEqual(0f, p.z, 1f, "over the track");
        }
        Assert.Less(chimney.x - puffs[0].x, RouteTrain.SpeedMps * TrainSteam.GapS + TrainSteam.DriftShare * steam.SizeM, "the newest puff is at the chimney");
        Assert.Greater(chimney.x - puffs[puffs.Count - 1].x, RouteTrain.SpeedMps * (TrainSteam.LifeS - 2f * TrainSteam.GapS),
            "the oldest drifted back along the track as far as the train ran since");
    }

    [Test]
    public void TheSameTimeDrawsTheSameTrail()
    {
        var train = Train(); var steam = SteamOf(train);
        steam.Sample(31.7f);
        var first = steam.Renderers.Select(r => (r.enabled, r.transform.position, r.transform.localScale.x, r.GetComponent<MeshFilter>().sharedMesh)).ToList();
        steam.Sample(5f); steam.Sample(44.2f);
        steam.Sample(31.7f);
        var again = steam.Renderers.Select(r => (r.enabled, r.transform.position, r.transform.localScale.x, r.GetComponent<MeshFilter>().sharedMesh)).ToList();
        CollectionAssert.AreEqual(first, again);
        Assert.IsTrue(Enumerable.Range(0, 6).Any(k => TrainSteam.IsSteam(k)) && Enumerable.Range(0, 6).Any(k => !TrainSteam.IsSteam(k)),
            "white steam among the smoke");
    }

    [Test]
    public void AHiddenTrainShowsNoSteamAndKeepsNoneForLater()
    {
        var train = Train(); var steam = SteamOf(train);
        steam.Sample(10f);
        Assert.Greater(Shown(steam), 0);
        train.gameObject.SetActive(false);   // the train's toggle, or a date before its service
        Assert.IsFalse(steam.isActiveAndEnabled, "it goes with the train: no Update runs");
        Assert.AreEqual(0, Shown(steam), "and every puff went");
        train.gameObject.SetActive(true);
        Assert.AreEqual(0, Shown(steam), "none left over when it shows again");
    }

    [Test]
    public void DestroyingTheTrainFreesItsSteam()
    {
        var train = Train(); var steam = SteamOf(train);
        var owned = steam.Owned.ToList();
        Assert.AreEqual(Statecraft.HexTiles.Flipbook.SmokeGrid * Statecraft.HexTiles.Flipbook.SmokeGrid + TrainSteam.Puffs, owned.Count,
            "the flipbook's frames and one material per puff");
        Assert.IsTrue(owned.All(o => o != null));
        Object.DestroyImmediate(train.gameObject);
        Assert.IsTrue(owned.All(o => o == null), "freed with the train");
    }

    /// <summary>Per-frame garbage stutters on WebGL: sampling the steam allocates nothing.</summary>
    [Test]
    public void SamplingAllocatesNothing()
    {
        var train = Train(); var steam = SteamOf(train);
        float stop = FirstStop(train);
        for (int i = 0; i < 10; i++) steam.Sample(10f + i * 0.016f);   // warm up
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) steam.Sample(10f + (i % 200) * 0.016f);
        for (int i = 0; i < 100; i++) steam.Sample(stop + 1f + i * 0.016f);
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "no allocation per frame");
    }
}
