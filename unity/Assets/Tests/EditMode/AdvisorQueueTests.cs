#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;

/// <summary>Spec 4.2's queue: critical first, then priority, then firing order; one teaching and one warning a turn by
/// themselves, never capping a critical one; the help level (spec 6); once and every; requires; Not now; Hebrew only
/// approved (OQ-4).</summary>
public class AdvisorQueueTests
{
    static readonly DateTime Day = new DateTime(1948, 5, 15);
    const string Head = "advisors:\n  - { id: ben_gurion, person: ben_gurion }\n  - { id: meyerson, person: golda_meir }\n  - { id: dayan, person: dayan }\nmessages:\n";
    static AdvisorCatalog Catalog(params string[] rows) => AdvisorCatalog.Parse(Head + string.Join("", rows.Select(r => "  - " + r + "\n")));
    static string Row(string id, string level, int priority = 1, string extra = "") =>
        $"{{ id: {id}, advisor: dayan, signal: s, level: {level}, priority: {priority}{extra} }}";
    static readonly string[] Fired = { "s" };
    static bool Always(System.Collections.Generic.IReadOnlyDictionary<string, double> _) => true;

    [Test]
    public void CriticalFirstThenPriorityThenFiringOrder()
    {
        var q = new AdvisorQueue(Catalog(Row("t1", "new"), Row("w1", "warnings"), Row("w2", "warnings", 3), Row("c1", "critical")), new AdvisorSettings(null));
        q.Offer(Fired, Always, 1);
        Assert.AreEqual("c1", q.Head(1, false)!.Id);
        q.Done(q.Head(1, false)!, 1, Day);
        Assert.AreEqual("w2", q.Head(1, false)!.Id, "priority 3 before the earlier priority 1");
        q.Done(q.Head(1, false)!, 1, Day);
        Assert.AreEqual("t1", q.Head(1, false)!.Id, "the warning's slot is used this turn; the teaching one is next");
    }

    [Test]
    public void OneTeachingAndOneWarningShowByThemselvesATurn()
    {
        var q = new AdvisorQueue(Catalog(Row("t1", "new"), Row("t2", "new"), Row("w1", "warnings"), Row("w2", "warnings")), new AdvisorSettings(null));
        q.Offer(Fired, Always, 1);
        q.Done(q.Head(1, false)!, 1, Day); q.Done(q.Head(1, false)!, 1, Day);
        Assert.IsNull(q.Head(1, false), "the rest wait for the next turn");
        Assert.AreEqual((1, 2), q.Position(2, false), "next turn: two more, one of each kind");
        Assert.IsNotNull(q.Head(2, false));
    }

    [Test]
    public void CriticalMessagesAreNeverCapped()
    {
        var q = new AdvisorQueue(Catalog(Row("c1", "critical"), Row("c2", "critical"), Row("w1", "warnings")), new AdvisorSettings(null));
        q.Offer(Fired, Always, 1);
        q.Done(q.Head(1, false)!, 1, Day);
        Assert.AreEqual("c2", q.Head(1, false)!.Id);
        q.Done(q.Head(1, false)!, 1, Day);
        Assert.AreEqual("w1", q.Head(1, false)!.Id, "two critical ones used no warning slot");
    }

    [TestCase("new", "t1,e1,w1,c1")]
    [TestCase("experienced", "e1,w1,c1")]
    [TestCase("warnings", "w1,c1")]
    [TestCase("off", "c1")]
    public void TheHelpLevelFilters(string level, string expected)
    {
        var settings = new AdvisorSettings(null); settings.SetLevel(level);
        var q = new AdvisorQueue(Catalog(Row("t1", "new"), Row("e1", "experienced"), Row("w1", "warnings"), Row("c1", "critical")), settings);
        q.Offer(Fired, Always, 1);
        var shown = new System.Collections.Generic.List<string>();
        for (int turn = 1; turn <= 4; turn++)
            while (q.Head(turn, false) is AdvisorCatalog.Message m) { shown.Add(m.Id); q.Done(m, turn, Day); }
        CollectionAssert.AreEquivalent(expected.Split(','), shown);
    }

    [Test]
    public void OnceIsNeverAgainAndEveryWaitsItsCooldown()
    {
        var settings = new AdvisorSettings(null);
        var q = new AdvisorQueue(Catalog(Row("o", "warnings"), Row("e", "critical", 1, ", repeat: every, cooldown_turns: 2")), settings);
        q.Offer(Fired, Always, 1);
        q.Done(q.Head(1, false)!, 1, Day); q.Done(q.Head(1, false)!, 1, Day);
        Assert.IsTrue(settings.Seen("o")); Assert.IsFalse(settings.Seen("e"), "an every message is never marked seen");
        q.Offer(Fired, Always, 2);
        Assert.IsNull(q.Head(2, false), "o is seen; e cools down until turn 3");
        q.Offer(Fired, Always, 3);
        Assert.AreEqual("e", q.Head(3, false)!.Id);
    }

    [Test]
    public void RequiresWaitsForTheEarlierMessageAndWhenMustHold()
    {
        var q = new AdvisorQueue(Catalog(Row("a", "new"), Row("b", "new", 1, ", requires: [a]"),
            Row("c", "warnings", 1, ", when: { debt_ratio_at_least: 0.8 }")), new AdvisorSettings(null));
        q.Offer(Fired, w => !w.ContainsKey("debt_ratio_at_least"), 1);
        Assert.AreEqual("a", q.Head(1, false)!.Id);
        q.Done(q.Head(1, false)!, 1, Day);
        Assert.IsNull(q.Head(1, false), "b waits for the next turn's teaching slot; c's condition did not hold");
        q.Offer(Fired, Always, 2);
        CollectionAssert.AreEquivalent(new[] { "b", "c" }, new[] { q.Head(2, false)!.Id, Next(q, 2) });
    }

    static string Next(AdvisorQueue q, int turn) { q.Done(q.Head(turn, false)!, turn, Day); return q.Head(turn, false)!.Id; }

    [Test]
    public void NotNowPutsItOffToTheNextTurnAndIntoTheHistory()
    {
        var q = new AdvisorQueue(Catalog(Row("w", "warnings")), new AdvisorSettings(null));
        q.Offer(Fired, Always, 1);
        q.PutOff(q.Head(1, false)!, 1, Day);
        Assert.IsNull(q.Head(1, false));
        CollectionAssert.AreEqual(new[] { "w" }, q.PutOffIds.ToArray());
        Assert.AreEqual(AdvisorQueue.Outcome.PutOff, q.History.Single().Outcome);
        Assert.AreEqual("w", q.Head(2, false)!.Id, "back next turn");
        Assert.Throws<InvalidOperationException>(() => new AdvisorQueue(Catalog(Row("c", "critical")), new AdvisorSettings(null)).PutOff(
            AdvisorCatalog.Parse(Head + "  - " + Row("c", "critical") + "\n").Find("c")!, 1, Day), "a critical message has no Not now");
    }

    [Test]
    public void InHebrewOnlyApprovedMessagesShow()
    {
        var q = new AdvisorQueue(Catalog(Row("p", "new"), Row("a", "warnings", 1, ", he_review: approved")), new AdvisorSettings(null));
        q.Offer(Fired, Always, 1);
        Assert.AreEqual("a", q.Head(1, hebrew: true)!.Id);
        Assert.AreEqual("p", q.Head(1, hebrew: false)!.Id, "the same queue in English: p fired first");
    }

    [Test]
    public void ResetForgetsTheGameButNotTheSeenMarks()
    {
        var settings = new AdvisorSettings(null);
        var q = new AdvisorQueue(Catalog(Row("a", "new"), Row("b", "warnings")), settings);
        q.Offer(Fired, Always, 1);
        q.Done(q.Head(1, false)!, 1, Day);
        q.Reset();
        Assert.IsEmpty(q.History); Assert.IsNull(q.Head(1, false));
        Assert.IsTrue(settings.Seen("a"), "seen marks are the player's (spec 15)");
    }
}
