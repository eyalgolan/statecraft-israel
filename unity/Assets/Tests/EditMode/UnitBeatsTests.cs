#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;

/// <summary>#2202 (plan docs/superpowers/plans/2026-11-02-enemy-move-playback.md, Task 2; owner, 2 November 2026: "Q1-Q7
/// recommended, Q8 yes"): the turn's recorded unit actions become beats in the sim's own order. Presentation only: the
/// planner orders and groups the sim's causes, it never decides what a unit did (R-ENEMY-001 names the enemy's).</summary>
public class UnitBeatsTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);

    static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

    /// <summary>An enemy move line as Movement.Move writes it: the unit before the move, the destination in Args[0].</summary>
    static Cause Move(int id, int fromQ, int fromR, int toQ, int toR, string enemyOrder = Movement.Kind) =>
        new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, "Report text is never read", enemyOrder,
            key: WhyKeys.UnitMoved, units: new[] { new UnitRef(new Unit(id, "infantry", fromQ, fromR, side: "egypt")) },
            args: new[] { Hex(toQ, toR), "1" });

    /// <summary>An engagement as Combat.Engage writes it: attackers then defenders, the hex first, the attacker count fifth.</summary>
    static Cause Engagement(int q, int r, int[] attackers, int[] defenders, string enemyOrder = Combat.Attack)
    {
        var units = attackers.Select(a => new UnitRef(new Unit(a, "infantry", q + 1, r, side: "egypt")))
            .Concat(defenders.Select(d => new UnitRef(new Unit(d, "infantry", q, r, side: Movement.PlayerSide)))).ToList();
        return new Cause("map", Hex(q, r), 0, "Report text is never read", enemyOrder,
            enemyAttackers: enemyOrder == Combat.Attack ? attackers : null, key: WhyKeys.Engagement, units: units,
            args: new[] { Hex(q, r), "150", "100", "0", attackers.Length.ToString(CultureInfo.InvariantCulture) });
    }

    static Cause Retreat(int id, int q, int r, int toQ, int toR) =>
        new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, "Report text is never read", key: WhyKeys.Retreated,
            units: new[] { new UnitRef(new Unit(id, "infantry", q, r, side: Movement.PlayerSide)) }, args: new[] { Hex(toQ, toR) });

    static Cause Destroyed(int id, int q, int r) =>
        new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, "Report text is never read", key: WhyKeys.Destroyed,
            units: new[] { new UnitRef(new Unit(id, "infantry", q, r, side: Movement.PlayerSide)) });

    static Cause Other(string key, string enemyOrder = "") =>
        new Cause("map", "5", 0, "Report text is never read", enemyOrder, key: key,
            units: new[] { new UnitRef(new Unit(5, "infantry", 0, 0, side: "egypt")) });

    static IEnumerable<int> Movers(IEnumerable<UnitBeat> beats) => beats.SelectMany(b => b.Moves.Select(m => m.Unit));

    [Test]
    public void NoEnemyCausesMakeNoBeats()
    {
        var why = new[] { Move(1, 0, 0, 1, 0, enemyOrder: ""), Engagement(4, 4, new[] { 1 }, new[] { 9 }, enemyOrder: ""),
            new Cause("economy", "money", 5, "Tax") };
        Assert.IsEmpty(UnitBeats.From(why, BeatSource.Enemy));
        Assert.IsEmpty(UnitBeats.From(Array.Empty<Cause>(), BeatSource.Enemy));
    }

    [Test]
    public void MovesKeepTheSimsOrder()
    {
        // Far apart, so each is its own beat: the planner never sorts.
        var why = new[] { Move(3, 0, 0, 1, 0), Move(1, 20, 0, 21, 0), Move(2, 40, 0, 41, 0) };
        var beats = UnitBeats.From(why, BeatSource.Enemy);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, Movers(beats).ToArray());
        Assert.AreEqual(3, beats.Count);
    }

    [Test]
    public void AMoveRecordsTheHexItLeftAndTheHexItReached()
    {
        var beat = UnitBeats.From(new[] { Move(7, 2, 3, 3, 3) }, BeatSource.Enemy).Single();
        Assert.AreEqual(BeatKind.Move, beat.Kind);
        Assert.AreEqual(0, beat.Engagements);
        var step = beat.Moves.Single();
        Assert.AreEqual((7, 2, 3, 3, 3), (step.Unit, step.FromQ, step.FromR, step.ToQ, step.ToR));
        Assert.AreEqual((3, 3), (beat.FocusQ, beat.FocusR));
        CollectionAssert.AreEqual(new[] { 7 }, beat.Units);
    }

    [Test]
    public void NearbyMovesShareABeatUpToFour()
    {
        var why = new[]
        {
            Move(1, 0, 0, 1, 0), Move(2, 0, 1, 2, 0), Move(3, 0, 2, 1, 1), Move(4, 0, 3, 3, 0), Move(5, 0, 4, 1, 2),
            Move(6, 20, 0, 11, 0),   // 10 hexes from the first destination
        };
        var beats = UnitBeats.From(why, BeatSource.Enemy);
        CollectionAssert.AreEqual(new[] { 4, 1, 1 }, beats.Select(b => b.Moves.Count).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, beats[0].Moves.Select(m => m.Unit).ToArray());
        CollectionAssert.AreEqual(new[] { 5 }, beats[1].Moves.Select(m => m.Unit).ToArray());
        CollectionAssert.AreEqual(new[] { 6 }, beats[2].Moves.Select(m => m.Unit).ToArray());
        Assert.AreEqual(UnitBeats.Timing.MaxUnitsPerBeat, beats[0].Moves.Count);
    }

    [Test]
    public void AGroupedBeatLooksAtTheMiddleOfItsDestinations()
    {
        var beat = UnitBeats.From(new[] { Move(1, 0, 0, 0, 0), Move(2, 0, 0, 2, 0) }, BeatSource.Enemy).Single();
        Assert.AreEqual((1, 0), (beat.FocusQ, beat.FocusR));
    }

    [Test]
    public void AnEngagementIsItsOwnBeatAndTakesItsRetreatersAndLosses()
    {
        var why = new[]
        {
            Move(1, 0, 0, 1, 0),
            Engagement(4, 4, new[] { 1, 2 }, new[] { 9, 10 }), Retreat(9, 4, 4, 3, 5), Destroyed(10, 4, 4),
            Engagement(8, 8, new[] { 3 }, new[] { 11 }), Retreat(11, 8, 8, 7, 9),
        };
        var beats = UnitBeats.From(why, BeatSource.Enemy);
        CollectionAssert.AreEqual(new[] { BeatKind.Move, BeatKind.Attack, BeatKind.Attack }, beats.Select(b => b.Kind).ToArray());
        var first = beats[1];
        CollectionAssert.AreEqual(new[] { 1, 2 }, first.Attackers);
        Assert.AreEqual((4, 4), (first.FocusQ, first.FocusR));
        Assert.AreEqual((9, 4, 4, 3, 5), (first.Retreats.Single().Unit, first.Retreats[0].FromQ, first.Retreats[0].FromR,
            first.Retreats[0].ToQ, first.Retreats[0].ToR));
        CollectionAssert.AreEqual(new[] { why[1], why[2], why[3] }, first.Causes, "its own line, then its retreat and loss");
        CollectionAssert.AreEqual(new[] { 9 }, first.Units, "only a retreater moves on an attack beat");
        CollectionAssert.AreEqual(new[] { 11 }, beats[2].Retreats.Select(m => m.Unit).ToArray(), "the next engagement's own");
    }

    [Test]
    public void FortifyAndAttackOrdersMakeNoBeat()
    {
        var why = new[] { Other(WhyKeys.Fortifying, Orders.Fortify), Other(WhyKeys.AttackOrdered, Combat.Attack), Other(WhyKeys.Holds) };
        Assert.IsEmpty(UnitBeats.From(why, BeatSource.Enemy));
    }

    [Test]
    public void APlayerEngagementIsNotABeat()
    {
        var why = new[] { Engagement(4, 4, new[] { 9 }, new[] { 1 }, enemyOrder: ""), Retreat(1, 4, 4, 5, 4) };
        Assert.IsEmpty(UnitBeats.From(why, BeatSource.Enemy));
    }

    [Test]
    public void ARetreatAfterAPlayerEngagementIsNotTakenByTheNextEnemyBeat()
    {
        var why = new[]
        {
            Engagement(4, 4, new[] { 9 }, new[] { 1 }, enemyOrder: ""), Retreat(1, 4, 4, 5, 4),
            Engagement(8, 8, new[] { 2 }, new[] { 10 }),
        };
        var beat = UnitBeats.From(why, BeatSource.Enemy).Single();
        Assert.IsEmpty(beat.Retreats);
    }

    [Test]
    public void ALongTurnIsCappedAndFolded()
    {
        var why = Enumerable.Range(1, 20).Select(i => Move(i, i * 10, 0, i * 10 + 1, 0)).ToArray();
        var beats = UnitBeats.From(why, BeatSource.Enemy);
        Assert.AreEqual(UnitBeats.Timing.MaxBeats, beats.Count);
        Assert.IsTrue(beats.Take(beats.Count - 1).All(b => !b.Folded));
        var folded = beats[beats.Count - 1];
        Assert.IsTrue(folded.Folded);
        CollectionAssert.AreEqual(Enumerable.Range(UnitBeats.Timing.MaxBeats, 20 - UnitBeats.Timing.MaxBeats + 1).ToArray(),
            folded.Moves.Select(m => m.Unit).ToArray(), "every remaining unit, in the sim's order");
        Assert.AreEqual(0f, folded.Pan, "a folded beat moves no camera");
        CollectionAssert.AreEqual(Enumerable.Range(1, 20).ToArray(), Movers(beats).ToArray(), "each unit exactly once");
        Assert.LessOrEqual(UnitBeats.Total(beats), UnitBeats.Timing.Cap + 1e-4f);
        foreach (var b in beats)
        {
            if (!b.Folded) Assert.GreaterOrEqual(b.Pan, UnitBeats.Timing.MinPan - 1e-5f);
            Assert.GreaterOrEqual(b.Dwell, UnitBeats.Timing.MinDwell - 1e-5f);
        }
    }

    [Test]
    public void AnOverlongTurnShrinksItsPansAndHoldsNoFurtherThanTheirMinimums()
    {
        var why = Enumerable.Range(0, UnitBeats.Timing.MaxBeats)
            .Select(i => Engagement(i * 30, 0, new[] { i + 1 }, new[] { 100 + i })).ToArray();
        var beats = UnitBeats.From(why, BeatSource.Enemy);
        Assert.AreEqual(UnitBeats.Timing.MaxBeats, beats.Count, "fixture: no fold");
        Assert.Greater(UnitBeats.Timing.MaxBeats * (UnitBeats.Timing.PanMax + UnitBeats.Timing.AttackMax), UnitBeats.Timing.Cap,
            "fixture: at full pacing the turn would pass the cap");
        Assert.AreEqual(UnitBeats.Timing.Cap, UnitBeats.Total(beats), 1e-3f, "shrunk to the cap, no further");
        foreach (var b in beats)
        {
            Assert.That(b.Pan, NUnit.Framework.Is.InRange(UnitBeats.Timing.MinPan, UnitBeats.Timing.PanMax));
            Assert.That(b.Dwell, NUnit.Framework.Is.InRange(UnitBeats.Timing.AttackMin, UnitBeats.Timing.AttackMax));
        }
    }

    [Test]
    public void AShortTurnKeepsTheFullPacing()
    {
        var beats = UnitBeats.From(new[] { Move(1, 0, 0, 1, 0), Engagement(30, 0, new[] { 2 }, new[] { 9 }) }, BeatSource.Enemy);
        Assert.AreEqual(UnitBeats.Timing.PanMax, beats[0].Pan, "the first beat's pan is not known from the camera: the longest");
        Assert.AreEqual(UnitBeats.Timing.MoveDwell, beats[0].Dwell);
        Assert.AreEqual(UnitBeats.Timing.PanMax, beats[1].Pan, 1e-5f, "29 hexes away: the longest pan");
        Assert.AreEqual(UnitBeats.Timing.AttackMax, beats[1].Dwell, "an attack is held for its fire take");
        Assert.AreEqual(beats[0].Pan + UnitTokenMotion.Duration + beats[0].Dwell, beats[0].Length, 1e-5f, "a move beat slides");
        Assert.AreEqual(beats[1].Pan + beats[1].Dwell, beats[1].Length, 1e-5f);
        Assert.AreEqual(beats[0].Length + beats[1].Length, UnitBeats.Total(beats), 1e-5f);
    }

    [Test]
    public void ANearPanIsShorter()
    {
        var beats = UnitBeats.From(new[] { Move(1, 0, 0, 0, 0), Move(2, 9, 0, 8, 0) }, BeatSource.Enemy);
        Assert.AreEqual(2, beats.Count, "fixture: 8 hexes apart, two beats");
        Assert.AreEqual(Math.Max(UnitBeats.Timing.PanMin, Math.Min(UnitBeats.Timing.PanMax, 8 * UnitBeats.Timing.PanPerHex)),
            beats[1].Pan, 1e-5f);
    }

    /// <summary>The seam #2544 PR 2 takes: another source of beats over the same Why list, with the same grouping and pacing.</summary>
    [Test]
    public void AnotherSourceReadsItsOwnLines()
    {
        var mine = new BeatSource(c => c.Key == WhyKeys.UnitMoved && c.EnemyOrder == "", c => c.Key == WhyKeys.Engagement && c.EnemyOrder == "");
        var why = new[] { Move(1, 0, 0, 1, 0, enemyOrder: ""), Move(2, 30, 0, 31, 0), Engagement(4, 4, new[] { 1 }, new[] { 9 }, enemyOrder: "") };
        var beats = UnitBeats.From(why, mine);
        CollectionAssert.AreEqual(new[] { BeatKind.Move, BeatKind.Attack }, beats.Select(b => b.Kind).ToArray());
        CollectionAssert.AreEqual(new[] { 1 }, Movers(beats).ToArray());
    }

    [Test]
    public void TheFirst1948TurnPlaysEveryEnemyMove()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
        var r = Simulation.Step(C, s, Array.Empty<Command>());
        var expected = r.Why.Where(w => w.Key == WhyKeys.UnitMoved && w.EnemyOrder == Movement.Kind).Select(w => w.Units[0].Id).ToArray();
        Assert.IsNotEmpty(expected, "fixture: the first 1948 turn has enemy moves");
        var beats = UnitBeats.From(r.Why, BeatSource.Enemy);
        CollectionAssert.AreEqual(expected, Movers(beats).ToArray(), "every enemy move once, in the sim's order");
        Assert.LessOrEqual(UnitBeats.Total(beats), UnitBeats.Timing.Cap + 1e-4f);
        var attacks = r.Why.Count(w => w.Key == WhyKeys.Engagement && w.EnemyOrder == Combat.Attack);
        Assert.AreEqual(attacks, beats.Count(b => b.Kind == BeatKind.Attack && !b.Folded) + beats.Where(b => b.Folded).Sum(b => b.Engagements),
            "every enemy engagement once");
    }
}
