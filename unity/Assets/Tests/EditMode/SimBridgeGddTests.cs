#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>The GDD orders through the UI's queue (#380): AssignWorkers, Train and CancelTraining queue in the slice
/// (MvpSlice), an id the content or state does not have is refused before End Turn, an order replaces an earlier one
/// with the same key, and the legacy loop's content queues none of them.</summary>
public class SimBridgeGddTests
{
    static readonly Content Slice = MvpSlice.Load(Paths.Content);
    static readonly Content Legacy = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge Bridge(Content c) => new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text);

    /// <summary>A slice bridge after one turn that opened a halutz cohort, and that cohort's id.</summary>
    static (SimBridge bridge, int cohort) WithACohort()
    {
        var b = Bridge(Slice);
        Assert.IsNull(b.Queue(new Train("tel_aviv", "halutz", 1)));
        b.EndTurn();
        Assert.AreEqual(1, b.State.Cohorts.Count, "the turn opened one cohort");
        return (b, b.State.Cohorts.Keys.First());
    }

    [Test]
    public void EachGddOrderQueuesInTheSlice()
    {
        var b = Bridge(Slice);
        Assert.IsNull(b.Queue(new AssignWorkers("tel_aviv", "job", 90)));
        Assert.IsNull(b.Queue(new Train("tel_aviv", "recruit", 1)));
        Assert.AreEqual(2, b.Queued.Count);

        var (withCohort, id) = WithACohort();
        Assert.IsNull(withCohort.Queue(new CancelTraining(id)));
        Assert.AreEqual(1, withCohort.Queued.Count);
    }

    [Test]
    public void AnUnknownIdIsRefusedAndNothingIsQueued()
    {
        var t = Text;
        var b = Bridge(Slice);
        Assert.AreEqual(t.Format("order.unknown", "city", "haifa"), b.Queue(new AssignWorkers("haifa", "job", 1)));
        Assert.AreEqual(t.Format("order.unknown", "city", "haifa"), b.Queue(new Train("haifa", "halutz", 1)));
        Assert.AreEqual(t.Format("order.unknown", "training", "pilot"), b.Queue(new Train("tel_aviv", "pilot", 1)));
        // civilian is a unit type, but nobody trains to it: it has no facility.
        Assert.AreEqual(t.Format("order.unknown", "training", "civilian"), b.Queue(new Train("tel_aviv", "civilian", 1)));
        Assert.AreEqual(t.Format("order.unknown", "cohort", "7"), b.Queue(new CancelTraining(7)));
        Assert.AreEqual(0, b.Queued.Count);
    }

    [Test]
    public void AnOrderWithTheSameKeyReplacesTheEarlierOne()
    {
        var b = Bridge(Slice);
        b.Queue(new AssignWorkers("tel_aviv", "job", 10));
        b.Queue(new AssignWorkers("tel_aviv", "job", 20));
        Assert.AreEqual(1, b.Queued.Count);
        Assert.AreEqual(20, ((AssignWorkers)b.Queued[0]).Count);

        // A fresh bridge, so the Attention the work order took cannot refuse a training order.
        var t = Bridge(Slice);
        Assert.IsNull(t.Queue(new Train("tel_aviv", "halutz", 1)));
        Assert.IsNull(t.Queue(new Train("tel_aviv", "halutz", 2)));
        Assert.AreEqual(1, t.Queued.Count);
        Assert.AreEqual(2, ((Train)t.Queued[0]).Count);
        Assert.IsNull(t.QueuedLike(new Train("tel_aviv", "recruit", 1)), "another track is its own order");
        Assert.IsNull(t.QueuedLike(new AssignWorkers("tel_aviv", "job", 1)), "work and training orders do not share a key");

        var (withCohort, id) = WithACohort();
        withCohort.Queue(new CancelTraining(id));
        withCohort.Queue(new CancelTraining(id));
        Assert.AreEqual(1, withCohort.Queued.Count);
    }

    [Test]
    public void TheLegacyLoopQueuesNoGddOrder()
    {
        var t = Text;
        var b = Bridge(Legacy);
        Assert.IsFalse(Legacy.IsGdd);
        Assert.AreEqual(t.Format("order.unknown", "city", "tel_aviv"), b.Queue(new AssignWorkers("tel_aviv", "job", 1)));
        Assert.AreEqual(t.Format("order.unknown", "city", "tel_aviv"), b.Queue(new Train("tel_aviv", "halutz", 1)));
        Assert.AreEqual(t.Format("order.unknown", "cohort", "1"), b.Queue(new CancelTraining(1)));
        Assert.AreEqual(0, b.Queued.Count);
    }
}
