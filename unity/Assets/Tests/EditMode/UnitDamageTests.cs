using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>#2432 PR 1: what the map and the unit card read to show that a formation is damaged and how badly. Presentation
/// only: <see cref="UnitDamage"/> reads <see cref="Unit.Strength"/> and <see cref="Unit.Full"/> (R-TRAIN-004's "the strength
/// it had before its first loss", cleared by a refill to full) and the starting force's strength for an enemy unit, which
/// stores no Full (R-COMBAT-003); it writes nothing to the sim or a save. The card states the strength for every military unit
/// on both sides, in English and Hebrew, so the keyboard path and a screen reader get the number too.</summary>
public class UnitDamageTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Units => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static GameState NewGame() => Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Clone();
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    static Unit WithStrength(Unit u, int strength, int full) =>
        new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, strength, u.Order, u.OrderTarget, u.HomeCity, full, u.CombatXp);

    /// <summary>A new game: every military unit, on both sides, stands at its full strength, which is its strength.</summary>
    [Test]
    public void EveryUnitStartsAtFullStrength()
    {
        var s = NewGame();
        var military = s.Units.Values.Where(u => u.Side != "").ToList();
        Assert.That(military.Any(u => u.Side == Movement.PlayerSide) && military.Any(u => u.Side != Movement.PlayerSide), "precondition: both sides");
        foreach (var u in military)
        {
            Assert.AreEqual(u.Strength, UnitDamage.FullStrength(C, u), $"unit {u.Id} ({u.Side})");
            Assert.IsFalse(UnitDamage.Damaged(C, u), $"unit {u.Id} ({u.Side})");
        }
    }

    /// <summary>R-COMBAT-003 with R-TRAIN-004: a player's unit that lost strength keeps the strength it had as its Full; the
    /// look reads that as its full strength. An enemy's stores none, so its starting force's strength is its full.</summary>
    [Test]
    public void ALossReadsAsDamagedOnBothSides()
    {
        var s = NewGame();
        var own = s.Units.Values.First(u => u.Side == Movement.PlayerSide && u.Strength >= 2);
        var lost = WithStrength(own, own.Strength - 1, own.Strength);
        Assert.AreEqual((own.Strength - 1, own.Strength), (lost.Strength, UnitDamage.FullStrength(C, lost)));
        Assert.IsTrue(UnitDamage.Damaged(C, lost));

        var enemy = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide && u.Strength >= 2);
        var hit = WithStrength(enemy, enemy.Strength - 1, 0);   // Combat.Lose stores no Full for an enemy
        Assert.AreEqual(C.StartingForces[enemy.Id].Strength, UnitDamage.FullStrength(C, hit));
        Assert.IsTrue(UnitDamage.Damaged(C, hit));
    }

    /// <summary>A halutz group is not a military unit (R-UNIT-001): never damaged. A unit raised in play is at its full
    /// strength until its first loss (its Full is 0 until then, as Training.Room reads it).</summary>
    [Test]
    public void AHalutzAndAFreshRaisedUnitAreNotDamaged()
    {
        Assert.IsFalse(UnitDamage.Damaged(C, new Unit(999, "halutz", 0, 0)));
        var raised = new Unit(998, "recruit", 0, 0, side: Movement.PlayerSide, strength: 1, homeCity: "tel_aviv");
        Assert.AreEqual(1, UnitDamage.FullStrength(C, raised));
        Assert.IsFalse(UnitDamage.Damaged(C, raised));
    }

    /// <summary>The look follows the sim through a reinforce (#2432, "clear when the unit is reinforced"): the sim's own
    /// Reinforce and training progress refill the unit, and it reads as full again.</summary>
    [Test]
    public void AReinforceClearsTheDamage()
    {
        var s = NewGame();
        var city = C.SettlementHexes.Single(h => h.Id == "tel_aviv");
        var u = s.Units.Values.First(x => x.Side == Movement.PlayerSide && x.Strength >= 3);
        s.Units[u.Id] = new Unit(u.Id, u.Type, city.Q, city.R, u.Service, u.Side, u.Strength - 2, "", "", u.HomeCity, u.Strength, u.CombatXp);
        s.Arms = 100;
        Assert.IsTrue(UnitDamage.Damaged(C, s.Units[u.Id]));
        Assert.IsNull(Training.Reinforce(C, s, u.Id, city.Id, 2, new List<Cause>()), "the sim takes the reinforce");
        for (int turn = 0; turn < 20 && s.Units[u.Id].Full != 0; turn++) Training.Progress(C, s, new List<Cause>());
        Assert.AreEqual((u.Strength, 0), (s.Units[u.Id].Strength, s.Units[u.Id].Full), "precondition: refilled to full");
        Assert.IsFalse(UnitDamage.Damaged(C, s.Units[u.Id]));
    }

    /// <summary>The guards in <see cref="UnitDamage.FullStrength"/>: a Full of 0 or below is unset; a player's unit with no
    /// Full reads its own strength (Training.Room's reading), never its starting force's; an enemy's unit reads its starting
    /// force's strength only for a known content, an id inside StartingForces and a row of its own side, and never below its
    /// strength now (the Max). Each assert goes red if its guard is removed.</summary>
    [Test]
    public void TheFullStrengthGuardsHold()
    {
        var s = NewGame();
        var own = s.Units.Values.First(u => u.Side == Movement.PlayerSide && u.Strength >= 2);
        Assert.AreEqual(own.Strength - 1, UnitDamage.FullStrength(C, WithStrength(own, own.Strength - 1, 0)),
            "a player's unit with Full unset reads its strength, not its starting force's");
        Assert.AreEqual(own.Strength - 1, UnitDamage.FullStrength(C, WithStrength(own, own.Strength - 1, -1)),
            "a negative Full is unset");
        Assert.IsFalse(UnitDamage.Damaged(C, WithStrength(own, own.Strength - 1, 0)));

        var enemy = s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide && u.Strength >= 2);
        int start = C.StartingForces[enemy.Id].Strength;
        var over = WithStrength(enemy, start + 1, 0);
        Assert.AreEqual(start + 1, UnitDamage.FullStrength(C, over), "an enemy above its start reads its strength (the Max)");
        Assert.IsFalse(UnitDamage.Damaged(C, over));

        var hit = WithStrength(enemy, enemy.Strength - 1, 0);
        Assert.AreEqual(enemy.Strength - 1, UnitDamage.FullStrength(null, hit), "no content: an enemy reads as full");
        Assert.IsFalse(UnitDamage.Damaged(null, hit));

        var past = new Unit(C.StartingForces.Count, enemy.Type, 0, 0, side: enemy.Side, strength: 1);
        Assert.AreEqual(1, UnitDamage.FullStrength(C, past), "an id past StartingForces reads its strength");
        var negative = new Unit(-1, enemy.Type, 0, 0, side: enemy.Side, strength: 1);
        Assert.AreEqual(1, UnitDamage.FullStrength(C, negative), "a negative id reads its strength");

        int ownRow = Enumerable.Range(0, C.StartingForces.Count)
            .First(i => C.StartingForces[i].Side == Movement.PlayerSide && C.StartingForces[i].Strength >= 2);
        var mismatch = new Unit(ownRow, enemy.Type, 0, 0, side: enemy.Side, strength: 1);
        Assert.AreEqual(1, UnitDamage.FullStrength(C, mismatch), "an enemy unit on a player's row reads its own strength");
        Assert.IsFalse(UnitDamage.Damaged(C, mismatch));
    }

    /// <summary>The card states the strength for every military unit, from the live state (not the token's copy): the plain
    /// line at full strength, "of" its full strength once damaged, in English and Hebrew.</summary>
    [Test]
    public void TheCardStatesTheStrengthOnBothSides()
    {
        foreach (var t in new[] { Text, He })
        {
            var s = NewGame();
            foreach (var side in new[] { true, false })
            {
                var token = UnitRoster.FromState(Units, C, s, _ => true, (row, a, q, r) => a)
                    .First(x => x.StateUnit is Unit d && (d.Side == Movement.PlayerSide) == side && d.Strength >= 2);
                var u = s.Units[token.SimId!.Value];
                string Lines() => UnitCard.For(new PlacedToken(token, default), s, t, true, C).Body;
                Assert.AreEqual(t.Format("unit.strength", u.Strength), Lines().Split('\n')[0], $"{u.Side} at full strength");
                s.Units[u.Id] = WithStrength(u, u.Strength - 1, side ? u.Strength : 0);
                Assert.AreEqual(t.Format("unit.strength_of", u.Strength - 1, UnitDamage.FullStrength(C, u)), Lines().Split('\n')[0],
                    $"{u.Side} damaged: the card reads the state, the token is the one drawn before the loss");
                s.Units[u.Id] = u;
            }
        }
    }

    /// <summary>The squadron and the ships are tokens only (Q-UNIT-5): no sim unit, so no strength line.</summary>
    [Test]
    public void ATokenOnlyRowHasNoStrengthLine()
    {
        var s = NewGame(); s.Date = new System.DateTime(1948, 7, 1);
        var air = UnitRoster.FromState(Units, C, s, _ => true, (row, a, q, r) => a).First(t => t.Unit.Service == UnitData.Air);
        var body = UnitCard.For(new PlacedToken(air, default), s, Text, true, C).Body;
        StringAssert.DoesNotContain("Strength", body);
    }
}
