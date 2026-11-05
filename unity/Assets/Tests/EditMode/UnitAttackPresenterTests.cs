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

    static Cause Line(string key, string attackers, string defenderLoss = "0") => new Cause("map", "9,9", 0, "Report text is never read",
        key: key, units: new[] { new UnitRef(new Unit(71, "infantry", 0, 0)), new UnitRef(new Unit(72, "guns", 0, 0)),
            new UnitRef(new Unit(73, "infantry", 0, 0)) }, args: new[] { "9,9", "150", "0", defenderLoss, attackers });

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

    /// <summary>The production presenter path gives every attacker one shared defender cue. It reacts with the first fire
    /// take that really starts, or, if none starts, only when the last attack has finished waiting.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public void SeveralAttackersShareOneTimedDefenderReaction(bool aTakeStarts)
    {
        var engagement = Line(WhyKeys.Engagement, "3", defenderLoss: "2");
        var bridge = new SimBridge(NewSession(), Text,
            (session, orders) => new StepResult(session.State, session.Now.Why.Append(engagement).ToArray(), Array.Empty<Rejection>()));
        var immediate = new List<int>();
        var cues = new Dictionary<int, (Action Started, Action Missed)>();
        var reactions = new List<Statecraft.Game.Map.Units.CombatHit>();
        using var presenter = new UnitAttackPresenter(bridge,
            id => { immediate.Add(id); return true; }, _ => true, _ => true,
            hit => { reactions.Add(hit); return true; },
            (id, started, missed) => { cues.Add(id, (started, missed)); return true; });

        bridge.EndTurn();
        CollectionAssert.AreEquivalent(new[] { 71, 72, 73 }, cues.Keys);
        Assert.IsEmpty(immediate, "positive loss uses the production timed attack path");
        Assert.IsEmpty(reactions, "the reaction waits for an actual fire take or for every miss");

        cues[71].Missed();
        Assert.IsEmpty(reactions, "the first miss is not the fallback while attackers remain");
        if (aTakeStarts)
        {
            cues[72].Started();
            Assert.AreEqual(1, reactions.Count, "the first actual fire take starts the shared reaction");
            cues[73].Started();
            Assert.AreEqual(1, reactions.Count, "another actual fire take does not repeat the shared reaction");
        }
        else
        {
            cues[72].Missed();
            Assert.IsEmpty(reactions, "the next-to-last miss is still not the fallback");
            cues[73].Missed();
            Assert.AreEqual(1, reactions.Count, "only the last miss starts the fallback reaction");
        }
        Assert.AreEqual(2, reactions[0].DefenderLoss);
    }

    /// <summary>#2614 review: on the production path the hit on the hex waits for the attackers' fire as the defenders'
    /// reaction does (a shell or dust that lands before the shot reads wrong), with or without a defender loss; if no take
    /// can start, the last miss plays it. Red at bcff7b8e5: the hit played at once, on resolution.</summary>
    [TestCase("0", true)]
    [TestCase("0", false)]
    [TestCase("2", true)]
    [TestCase("2", false)]
    public void TheHitOnTheHexWaitsForTheAttackersFire(string defenderLoss, bool aTakeStarts)
    {
        var engagement = Line(WhyKeys.Engagement, "2", defenderLoss);
        var bridge = new SimBridge(NewSession(), Text,
            (session, orders) => new StepResult(session.State, session.Now.Why.Append(engagement).ToArray(), Array.Empty<Rejection>()));
        var immediate = new List<int>();
        var cues = new Dictionary<int, (Action Started, Action Missed)>();
        var hits = new List<Statecraft.Game.Map.Units.CombatHit>();
        var reactions = new List<Statecraft.Game.Map.Units.CombatHit>();
        using var presenter = new UnitAttackPresenter(bridge,
            id => { immediate.Add(id); return true; }, h => { hits.Add(h); return true; }, _ => true,
            h => { reactions.Add(h); return true; },
            (id, started, missed) => { cues.Add(id, (started, missed)); return true; });

        bridge.EndTurn();
        CollectionAssert.AreEquivalent(new[] { 71, 72 }, cues.Keys);
        Assert.IsEmpty(immediate, "the production path times every attack");
        Assert.IsEmpty(hits, "no hit before an attacker's fire");

        if (aTakeStarts) cues[72].Started();
        else { cues[71].Missed(); Assert.IsEmpty(hits, "not while an attacker may still fire"); cues[72].Missed(); }
        Assert.AreEqual(1, hits.Count, aTakeStarts ? "the hit lands with the first fire take" : "the last miss plays the hit");
        Assert.AreEqual((9, 9), (hits[0].Q, hits[0].R));
        Assert.AreEqual(defenderLoss == "2" ? 1 : 0, reactions.Count, "the defenders react only to a loss, at the same moment");
        cues[71].Started();
        Assert.AreEqual(1, hits.Count, "a later take does not repeat the hit");
    }

    /// <summary>#2614: the dust of a unit the engagement destroyed (its Destroyed line right after it, as Combat.Engage adds
    /// it) waits for the attackers' fire too; a take that starts at once plays it at once. Red at 917ebf2e9: the dust played
    /// on resolution.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ADestroyedUnitsDustWaitsForTheAttackersFire(bool takeStartsAtOnce)
    {
        var engagement = Line(WhyKeys.Engagement, "2", defenderLoss: "2");
        var dead = new Cause("map", "9,9", 0, "Report text is never read", key: WhyKeys.Destroyed,
            units: new[] { new UnitRef(new Unit(73, "infantry", 0, 0)) });
        var bridge = new SimBridge(NewSession(), Text,
            (session, orders) => new StepResult(session.State, session.Now.Why.Append(engagement).Append(dead).ToArray(), Array.Empty<Rejection>()));
        var cues = new Dictionary<int, (Action Started, Action Missed)>();
        var order = new List<string>();
        using var presenter = new UnitAttackPresenter(bridge,
            _ => true, h => { order.Add("hit"); return true; }, id => { order.Add("dust " + id); return true; }, _ => true,
            (id, started, missed) =>
            {
                cues.Add(id, (started, missed));
                if (takeStartsAtOnce && id == 71) started();
                return true;
            });

        bridge.EndTurn();
        if (!takeStartsAtOnce)
        {
            Assert.IsEmpty(order, "no hit and no dust before an attacker's fire");
            cues[72].Started();
        }
        CollectionAssert.AreEqual(new[] { "hit", "dust 73" }, order, "the dust lands with the hit, after the fire");
        cues[71].Started(); cues[72].Missed();
        CollectionAssert.AreEqual(new[] { "hit", "dust 73" }, order, "and only once");
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
        // Found by running the sim (1948 seed, Historical, first turn): unit 5 attacks enemy unit 9, unit 6 takes the hex
        // unit 9 marches on (R-ENEMY-002), so no path leads there, and at End Turn unit 9 attacks instead (R-ENEMY-003).
        var nine = session.Now.State.Units[9];
        Assert.AreNotEqual(Movement.PlayerSide, nine.Side, "fixture: unit 9 is an enemy");
        Assert.IsNull(bridge.Queue(new UnitOrder(5, Combat.Attack, nine.Q, nine.R)), "fixture: unit 5 can attack unit 9");
        var march = C.StartingForces[9].Script[0];
        Assert.IsNull(bridge.Queue(new Move(6, march.Q, march.R)), "fixture: unit 6 can take unit 9's march hex");
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
