using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class UnitAttackPresenterTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static GameSession Session() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);
    static UnitOrder Attack(GameSession session)
    {
        var u = session.Now.State.Units[0];
        var enemy = session.Now.State.Units.Values.First(x => x.Side == "syria" && MapActions.Distance(x.Q, x.R, u.Q, u.R) == 1);
        return new UnitOrder(0, Combat.Attack, enemy.Q, enemy.R);
    }
    static Cause Engagement() => new Cause("map", "9,9", 0, "Do not parse me", key: WhyKeys.Engagement,
        units: new[] { new UnitRef(new Unit(71, "infantry", 0, 0)), new UnitRef(new Unit(72, "guns", 0, 0)),
            new UnitRef(new Unit(73, "defender", 0, 0)) }, args: new[] { "9,9", "100", "0", "0", "2" });

    [Test]
    public void InstantAttackRunsAfterChangedAndIsNotReplayedAtEndTurn()
    {
        var session = Session(); var bridge = new SimBridge(session, Text);
        var calls = new List<int>(); bool rebuilt = false;
        bridge.Changed += () => rebuilt = true;
        using var presenter = new UnitAttackPresenter(bridge, id => { Assert.IsTrue(rebuilt); calls.Add(id); return true; });
        Assert.IsNull(bridge.Queue(Attack(session)));
        CollectionAssert.AreEqual(new[] { 0 }, calls);
        int count = calls.Count;
        bridge.EndTurn();
        Assert.AreEqual(count, calls.Count(id => id == 0), "instant order is replayed by the sim, not the presentation");
    }

    [Test]
    public void AttachingToRestoredOpenOrdersDoesNotReplayThem()
    {
        var session = Session(); Assert.IsNull(session.Commit(Attack(session)));
        var bridge = new SimBridge(session, Text); var calls = new List<int>();
        using var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return true; });
        Assert.IsEmpty(calls);
        bridge.EndTurn();
        CollectionAssert.DoesNotContain(calls, 0);
    }

    [Test]
    public void FreshTurnEngagementIncludesGunsSkipsMissingAndDoesNotQueue()
    {
        var calls = new List<int>();
        var bridge = new SimBridge(Session(), Text, (session, orders) =>
            new StepResult(session.State, session.Now.Why.Concat(new[] { Engagement() }).ToArray(), Array.Empty<Rejection>()));
        using var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return false; });
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { 71, 72 }, calls, "false means missing/incompatible, not retry");
        bridge.Replace(Session());
        CollectionAssert.AreEqual(new[] { 71, 72 }, calls, "load does not replay or retry missing visuals");
    }

    [Test]
    public void RefusalAndFailedTurnNeverReplayStaleCombat()
    {
        var calls = new List<int>();
        var prior = Session();
        Assert.IsNull(prior.Commit(Attack(prior)));
        prior.EndTurn(Array.Empty<Command>());
        Assert.IsTrue(prior.Last.Why.Any(x => x.Key == WhyKeys.Engagement), "stale combat exists");
        var bridge = new SimBridge(prior, Text, (session, orders) => throw new ArgumentException("test refusal"));
        using var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return true; });
        Assert.IsNotNull(bridge.Queue(new UnitOrder(-1, Combat.Attack, 0, 0)));
        LogAssert.Expect(LogType.Warning, "End Turn refused: test refusal");
        bridge.EndTurn(); Assert.IsEmpty(calls);
    }

    [Test]
    public void ReplaceAndDisposeDoNotReplayHistoryOrLeakListeners()
    {
        var bridge = new SimBridge(Session(), Text); var calls = new List<int>();
        var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return true; });
        bridge.Replace(Session()); Assert.IsEmpty(calls);
        presenter.Dispose(); presenter.Dispose();
        Assert.IsNull(bridge.Queue(Attack(bridge.Session)));
        Assert.IsEmpty(calls);
    }
}
