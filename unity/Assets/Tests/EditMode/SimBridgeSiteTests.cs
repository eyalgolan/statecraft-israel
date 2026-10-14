#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>Pioneer work through the UI's queue (#378): PioneerWork and StopWork queue in the slice (MvpSlice) for a unit that
/// exists, an id the content or state does not have is refused before End Turn, one order per unit, and the legacy loop's
/// content queues neither.</summary>
public class SimBridgeSiteTests
{
    static readonly Content Slice = MvpSlice.Load(Paths.Content);
    static readonly Content Legacy = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static SimBridge Bridge(Content c) => new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text);

    /// <summary>A slice bridge whose state holds one halutz unit, and its id. The unit is added to the state directly: in the
    /// slice the Treasury is empty by the time a trained group is ready, and the queue only checks ids.</summary>
    static (SimBridge bridge, int unit) WithAHalutz()
    {
        var b = Bridge(Slice);
        var id = b.State.NextId++;
        b.State.Units.Add(id, new Unit(id, "halutz", -3, 19));
        return (b, id);
    }

    [Test]
    public void BothOrdersQueueInTheSliceOnePerUnit()
    {
        var (b, id) = WithAHalutz();
        Assert.IsNull(b.Queue(new PioneerWork(id, Projects.BuildYishuv, "mefalsim")));
        Assert.IsNull(b.Queue(new StopWork(id)));   // the same unit: replaces the build order
        Assert.AreEqual(1, b.Queued.Count);
        Assert.IsInstanceOf<StopWork>(b.Queued[0]);
    }

    [Test]
    public void AnUnknownIdIsRefusedAndNothingIsQueued()
    {
        var t = Text;
        var (b, id) = WithAHalutz();
        Assert.AreEqual(t.Format("order.unknown", "unit", "99"), b.Queue(new PioneerWork(99, Projects.BuildYishuv, "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "pioneer work", "build_road"), b.Queue(new PioneerWork(id, "build_road", "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "site", "yeruham"), b.Queue(new PioneerWork(id, Projects.BuildYishuv, "yeruham")));
        Assert.AreEqual(t.Format("order.unknown", "unit", "99"), b.Queue(new StopWork(99)));
        Assert.AreEqual(0, b.Queued.Count);
    }

    [Test]
    public void TheLegacyLoopQueuesNeither()
    {
        var t = Text;
        var b = Bridge(Legacy);
        Assert.AreEqual(t.Format("order.unknown", "unit", "1"), b.Queue(new PioneerWork(1, Projects.BuildYishuv, "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "unit", "1"), b.Queue(new StopWork(1)));
        Assert.AreEqual(0, b.Queued.Count);
    }
}
