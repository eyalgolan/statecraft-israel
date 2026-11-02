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

/// <summary>#1867 (design by Tal Heuberger, #2078): each engagement the player sees resolve plays its attackers' fire
/// take once: an attack order at once, a fresh End Turn engagement after the map is redrawn; never the defenders, never
/// an engagement End Turn only replays, never history on a load.</summary>
public class UnitAttackPresenterTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static GameSession NewSession() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);

    static Cause Line(string key, string attackers) => new Cause("map", "9,9", 0, "Report text is never read",
        key: key, units: new[] { new UnitRef(new Unit(71, "infantry", 0, 0)), new UnitRef(new Unit(72, "guns", 0, 0)),
            new UnitRef(new Unit(73, "infantry", 0, 0)) }, args: new[] { "9,9", "150", "0", "0", attackers });

    /// <summary>The first player unit's attack on an enemy next to it at the start of the game.</summary>
    static UnitOrder AttackNextTo(GameSession session, out int attacker)
    {
        foreach (var u in session.Now.State.Units.Values.Where(u => u.Side == Movement.PlayerSide))
        {
            var enemy = session.Now.State.Units.Values.FirstOrDefault(x => x.Side != Movement.PlayerSide && x.Side != ""
                && MapActions.Distance(x.Q, x.R, u.Q, u.R) == 1);
            if (enemy == null) continue;
            attacker = u.Id;
            return new UnitOrder(u.Id, Combat.Attack, enemy.Q, enemy.R);
        }
        throw new InvalidOperationException("fixture: no player unit stands next to an enemy at the start");
    }

    [Test]
    public void TheAttackersIncludingGunsPlayAndTheDefendersDoNot()
    {
        CollectionAssert.AreEqual(new[] { 71, 72 }, UnitAttackPresenter.Attackers(Line(WhyKeys.Engagement, "2")));
        CollectionAssert.AreEqual(new[] { 71, 72, 73 }, UnitAttackPresenter.Attackers(Line(WhyKeys.Engagement, "3")));
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("4")]
    [TestCase("two")]
    [TestCase("")]
    public void AMalformedAttackerCountPlaysNothing(string count)
    {
        Assert.IsEmpty(UnitAttackPresenter.Attackers(Line(WhyKeys.Engagement, count)));
    }

    [Test]
    public void OnlyAnEngagementPlays()
    {
        Assert.IsEmpty(UnitAttackPresenter.Attackers(Line(WhyKeys.AttackOrdered, "2")));
    }

    /// <summary>An attack order plays at once, after the map is redrawn for it; End Turn runs it again inside the sim (its
    /// first causes are the open turn's), and that replay does not play it twice.</summary>
    [Test]
    public void AnAttackOrderPlaysOnceAfterTheMapIsRedrawn()
    {
        var session = NewSession();
        var bridge = new SimBridge(session, Text);
        var played = new List<int>();
        bool redrawn = false;
        bridge.Changed += () => redrawn = true;
        using var presenter = new UnitAttackPresenter(bridge, id => { Assert.IsTrue(redrawn, "after Changed"); played.Add(id); return true; });
        Assert.IsNull(bridge.Queue(AttackNextTo(session, out int attacker)));
        CollectionAssert.AreEqual(new[] { attacker }, played);
        redrawn = false;
        var r = bridge.EndTurn();
        Assert.IsTrue(r.Why.Any(x => x.Key == WhyKeys.Engagement && UnitAttackPresenter.Attackers(x).Contains(attacker)),
            "the sim replays the committed attack at End Turn");
        Assert.AreEqual(1, played.Count(id => id == attacker), "and the replay is not played again");
        Assert.IsTrue(redrawn, "End Turn redraws the map");
    }

    /// <summary>The real sim, an attack order then a move in the open turn, then End Turn, where an enemy attacks (review of
    /// #2078): the fresh enemy engagement plays, after the map is redrawn; the committed attack, which End Turn replays
    /// first, does not. If the replayed prefix ever stopped matching the open turn's causes, this fails.</summary>
    [Test]
    public void AFreshEndTurnEngagementPlaysBesideAReplayedOne()
    {
        var session = NewSession();
        var bridge = new SimBridge(session, Text);
        var played = new List<int>();
        bool redrawn = false;
        bridge.Changed += () => redrawn = true;
        using var presenter = new UnitAttackPresenter(bridge, id => { Assert.IsTrue(redrawn, "after Changed"); played.Add(id); return true; });
        // Found by running the sim (1948 seed, Historical, first turn): unit 5 attacks enemy unit 9, unit 6 steps next
        // to unit 9, and at End Turn unit 9 attacks.
        var nine = session.Now.State.Units[9];
        Assert.AreNotEqual(Movement.PlayerSide, nine.Side, "fixture: unit 9 is an enemy");
        Assert.IsNull(bridge.Queue(new UnitOrder(5, Combat.Attack, nine.Q, nine.R)), "fixture: unit 5 can attack unit 9");
        Assert.IsNull(bridge.Queue(new Move(6, nine.Q - 2, nine.R)), "fixture: unit 6 can step next to unit 9");
        CollectionAssert.AreEqual(new[] { 5 }, played, "the attack order played at once");
        int openTurn = session.Now.Why.Count;
        redrawn = false;
        var r = bridge.EndTurn();
        var replayed = r.Why.Take(openTurn).Where(x => x.Key == WhyKeys.Engagement).ToList();
        var fresh = r.Why.Skip(openTurn).Where(x => x.Key == WhyKeys.Engagement).ToList();
        Assert.AreEqual(1, replayed.Count, "End Turn replays the committed attack first");
        CollectionAssert.AreEqual(new[] { 5 }, UnitAttackPresenter.Attackers(replayed[0]));
        Assert.IsTrue(fresh.Any(x => x.EnemyOrder == Combat.Attack && UnitAttackPresenter.Attackers(x).Contains(9)),
            "fixture: unit 9's attack is a fresh engagement of this End Turn");
        CollectionAssert.AreEqual(new[] { 5 }.Concat(fresh.SelectMany(UnitAttackPresenter.Attackers)), played,
            "the fresh engagement plays; the replayed attack does not play again");
        CollectionAssert.Contains(played.Skip(1), 9);
    }

    /// <summary>A mid-turn save loads with its attack order already committed (R-SAVE-003), so the bridge never queued it
    /// (ported from Tal Heuberger's #2078): End Turn runs it again first, and it is not played; it played when it was given.</summary>
    [Test]
    public void ALoadedOpenTurnsAttackIsNotPlayedAtEndTurn()
    {
        var session = NewSession();
        Assert.IsNull(session.Commit(AttackNextTo(session, out int attacker)));
        var bridge = new SimBridge(session, Text);
        var played = new List<int>();
        using var presenter = new UnitAttackPresenter(bridge, id => { played.Add(id); return true; });
        var r = bridge.EndTurn();
        Assert.IsTrue(r.Why.Any(x => x.Key == WhyKeys.Engagement && UnitAttackPresenter.Attackers(x).Contains(attacker)),
            "the sim replays the restored attack at End Turn");
        CollectionAssert.DoesNotContain(played, attacker);
    }

    [Test]
    public void ALoadPlaysNoHistoryAndAMissingTokenIsNotRetried()
    {
        var calls = new List<int>();
        var engagement = Line(WhyKeys.Engagement, "2");
        var bridge = new SimBridge(NewSession(), Text,
            (session, orders) => new StepResult(session.State, session.Now.Why.Append(engagement).ToArray(), Array.Empty<Rejection>()));
        using var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return false; });
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { 71, 72 }, calls, "false (no token can play) is not retried");
        bridge.Replace(NewSession());
        CollectionAssert.AreEqual(new[] { 71, 72 }, calls, "a load plays nothing");
    }

    [Test]
    public void ARefusedTurnPlaysNothing()
    {
        var session = NewSession();
        Assert.IsNull(session.Commit(AttackNextTo(session, out _)));
        var bridge = new SimBridge(session, Text, (s, orders) => throw new ArgumentException("refused for the test"));
        var calls = new List<int>();
        using var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return true; });
        LogAssert.Expect(LogType.Warning, "End Turn refused: refused for the test");
        bridge.EndTurn();
        Assert.IsEmpty(calls);
    }

    [Test]
    public void DisposingStopsPlaying()
    {
        var session = NewSession();
        var bridge = new SimBridge(session, Text);
        var calls = new List<int>();
        var presenter = new UnitAttackPresenter(bridge, id => { calls.Add(id); return true; });
        presenter.Dispose(); presenter.Dispose();
        Assert.IsNull(bridge.Queue(AttackNextTo(session, out _)));
        Assert.IsEmpty(calls);
    }
}
