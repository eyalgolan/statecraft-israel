#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>Pioneer work through the UI's queue (#378): PioneerWork and StopWork queue in the real content (the GDD game since the cutover) for a unit that
/// exists, an id the content or state does not have is refused before End Turn, one order per unit, and the legacy loop's
/// content queues neither.</summary>
public class SimBridgeSiteTests
{
    static readonly Content Slice = ContentLoader.Load(Paths.Content);
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

    /// <summary>#1124: the three other pioneer actions Projects.Start runs pass the id check, each at a site of its kind.</summary>
    [TestCase(Projects.BuildCamp, "camp_80")]
    [TestCase(Projects.PlowFields, "mefalsim_field")]
    [TestCase(Projects.DrySwamp, "hula")]
    public void EachOtherPioneerActionIsQueued(string action, string site)
    {
        var (b, id) = WithAHalutz();
        Assert.IsNull(b.Queue(new PioneerWork(id, action, site)));
        var order = b.QueuedLike(new PioneerWork(id, action, site)) as PioneerWork;
        Assert.AreEqual((id, action, site), (order!.Unit, order.Action, order.Site));
    }

    // Corridors (#786): a road or rail order names a segment of corridors.yaml, "corridor:N".
    [Test]
    public void ACorridorOrderQueuesInTheSlice()
    {
        var (b, id) = WithAHalutz();
        Assert.IsNull(b.Queue(new PioneerWork(id, Corridors.BuildRoad, "burma_road:4")));
        Assert.IsNull(b.Queue(new PioneerWork(id, Corridors.BuildRail, "tlv_haifa_rail:1")));   // the same unit: replaces it
        Assert.AreEqual(1, b.Queued.Count);
    }

    [Test]
    public void AnUnknownIdIsRefusedAndNothingIsQueued()
    {
        var t = Text;
        var (b, id) = WithAHalutz();
        Assert.AreEqual(t.Format("order.unknown", "unit"), b.Queue(new PioneerWork(99, Projects.BuildYishuv, "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "pioneer work"), b.Queue(new PioneerWork(id, "dig_well", "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "corridor segment"), b.Queue(new PioneerWork(id, Corridors.BuildRoad, "burma_road:5")));
        Assert.AreEqual(t.Format("order.unknown", "corridor segment"), b.Queue(new PioneerWork(id, Corridors.BuildRoad, "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "site"), b.Queue(new PioneerWork(id, Projects.BuildYishuv, "yeruham")));
        Assert.AreEqual(t.Format("order.unknown", "unit"), b.Queue(new StopWork(99)));
        Assert.AreEqual(0, b.Queued.Count);
    }

    [Test]
    public void TheLegacyLoopQueuesNeither()
    {
        var t = Text;
        var b = Bridge(Legacy);
        var none = b.State.NextId;   // no unit has it: the starting forces take ids 0 to 11 (R-UNIT-006)
        Assert.AreEqual(t.Format("order.unknown", "unit"), b.Queue(new PioneerWork(none, Projects.BuildYishuv, "mefalsim")));
        Assert.AreEqual(t.Format("order.unknown", "unit"), b.Queue(new StopWork(none)));
        Assert.AreEqual(0, b.Queued.Count);
    }
}
