#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>#1853, the data side of the body's scroll reset: the card puts its body back to the top only for a new event
/// or a new session, never on a re-render or Hide/"Decision waiting" of the same event. The scroller's range is forced so
/// these tests can set an offset without a layout pass. That makes them lifecycle tests, not geometry or overflow proofs.
/// Each test first asserts that the offset is nonzero, so a reset that has already happened cannot pass by accident.</summary>
public class EventScrollLifecycleReleaseTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text(bool hebrew) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text, hebrew ? "strings.he.yaml" : "strings.en.yaml");

    const string First = "armistice_egypt", Second = "armistice_lebanon";
    const float Offset = 250f;

    /// <summary>A historical session with exactly this event pending, on its own first day (EventCardTests.Showing's setup).</summary>
    static GameSession Pending(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.Single(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return g;
    }

    /// <summary>Forces a vertical range on the body's scroller, scrolls part way down, and checks that the offset took.</summary>
    static ScrollView ScrollDown(UiTestKit ui)
    {
        var scroll = ui.Q<ScrollView>("event-scroll");
        scroll.verticalScroller.lowValue = 0f;
        scroll.verticalScroller.highValue = 1000f;
        scroll.scrollOffset = new Vector2(0f, Offset);
        Assert.AreEqual(Offset, scroll.scrollOffset.y, 0.01f, "precondition: the forced range holds a nonzero offset");
        return scroll;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RenderingTheSameEventAgainKeepsTheScroll(bool hebrew)
    {
        var text = Text(hebrew);
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, new SimBridge(Pending(First), text), text, null);
        Assert.AreEqual(First, card.Shown!.Id);
        var scroll = ScrollDown(ui);
        card.Render();
        Assert.AreEqual(First, card.Shown!.Id);
        Assert.AreEqual(Offset, scroll.scrollOffset.y, 0.01f, "the same event keeps its place");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HideAndDecisionWaitingKeepTheScroll(bool hebrew)
    {
        var text = Text(hebrew);
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, new SimBridge(Pending(First), text), text, null);
        var scroll = ScrollDown(ui);
        card.Collapse();
        Assert.IsTrue(card.Collapsed, "precondition: collapsed");
        card.Reopen();
        Assert.IsFalse(card.Collapsed);
        Assert.AreEqual(First, card.Shown!.Id);
        Assert.AreEqual(Offset, scroll.scrollOffset.y, 0.01f, "the reopened card is where the player left it");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ADifferentEventOpensAtTheTop(bool hebrew)
    {
        var text = Text(hebrew);
        using var ui = new UiTestKit();
        var bridge = new SimBridge(Pending(First), text);
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        var scroll = ScrollDown(ui);
        var s = bridge.Session.State;
        s.Date = C.Events.Single(e => e.Id == Second).From;
        s.PendingEvents.Clear(); s.PendingEvents.Add(Second);
        card.Render();
        Assert.AreEqual(Second, card.Shown!.Id, "precondition: the other event shows");
        Assert.AreEqual(0f, scroll.scrollOffset.y, 0.01f, "a new event opens at its text");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ANewSessionWithTheSameEventOpensAtTheTop(bool hebrew)
    {
        var text = Text(hebrew);
        using var ui = new UiTestKit();
        var bridge = new SimBridge(Pending(First), text);
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        var scroll = ScrollDown(ui);
        var old = bridge.Session;
        bridge.Replace(Pending(First));                       // raises Changed: the card renders
        Assert.AreNotSame(old, bridge.Session, "precondition: a new session");
        Assert.AreEqual(First, card.Shown!.Id, "precondition: the same event id");
        Assert.AreEqual(0f, scroll.scrollOffset.y, 0.01f, "a new session (New Game, a load) forgets the old place");
    }
}