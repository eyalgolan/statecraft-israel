using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Prisoners (GDD Phase 5 PR 12, #751; impact plan section 4.3 as written, Q-POW-1). Only a surrender in combat takes
/// prisoners; they eat Food in step 7, and a shortfall costs Social cohesion, never prisoners; they are never a resource;
/// each front's exchange card returns them and scores then, per population unit; the player's soldiers held are a count that
/// costs Money while held and comes home on the same card with no score. The fixture's numbers are fixture values: 10 people
/// a population unit, 2 Food per prisoner unit, 0.05 cohesion per unfed Food unit, 7 points per unit returned and IL 0.5m
/// per held soldier unit a turn, chosen so each shows apart from the others.</summary>
public class PrisonersTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n"
        + "  - { id: iraqi, kind: unit, side: iraq, class: infantry, movement_points: 3 }\n"
        + "  - { id: irregular, kind: unit, side: ala, class: infantry, movement_points: 3 }\n"
        + "  - { id: egypt_gun, kind: unit, side: egypt, class: artillery, movement_points: 2, range: 2, arms_per_fire: 3, combat_percent: 200 }\n";

    static string Card(string id, string front, string from) => $@"
id: {id}
title: Prisoners exchanged
text: The prisoners are exchanged.
anchor: hard
prisoners: {front}
from: {from}
to: 1950-01-01
options:
  - id: continue
    text: Continue
    historical: true
";

    static readonly Dictionary<string, string> Cards = new Dictionary<string, string>
    {
        ["pow_egypt.yaml"] = Card("pow_egypt", "egypt", "1948-07-01"),
        ["pow_jordan.yaml"] = Card("pow_jordan", "jordan", "1948-07-01"),
    };

    static Content Load(Dictionary<string, string>? cards = null, string? rules = null)
    {
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
        });
        if (rules != null) files["rules.yaml"] = rules;
        return ContentLoader.Load(Fixture.Write(files, cards ?? Cards));
    }

    static string Rules(string from, string to)
    {
        var rules = Fixture.Gdd()["rules.yaml"];
        Assert.Contains(from, rules);
        return rules.Replace(from, to);
    }

    static Unit Soldier(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength);
    static Unit Enemy(int id, int q, int r, int strength, string type = "enemy", string side = "egypt") =>
        new Unit(id, type, q, r, side: side, strength: strength);
    static Unit Attacking(Unit u, int q, int r) =>
        new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Combat.Attack, FormattableString.Invariant($"{q},{r}"));

    /// <summary>A new fixture game on 15 May 1948, before the cards' window opens, with two soldiers ordered to attack the corner
    /// (0, 0) from both its land neighbours and an enemy unit of strength 3 there: 8 against 3 is 2:1, so it loses 1 and,
    /// with nowhere to go, surrenders with 2 units (CombatTests.ADefenderWithNoHexToRetreatToSurrenders).</summary>
    static GameState Surrounded(Content c, Unit? defender = null) =>
        CombatTests.With(c, Attacking(Soldier(1, 1, 0, 4), 0, 0), Attacking(Soldier(3, 0, 1, 4), 0, 0), defender ?? Enemy(2, 0, 0, 3));

    static List<Cause> Fight(Content c, GameState s)
    {
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        return why;
    }

    static List<Cause> Upkeep(Content c, GameState s)
    {
        var why = new List<Cause>();
        Prisoners.Upkeep(c, s, c.RulesAt(s.Date).Mvp!, why);
        return why;
    }

    // R-POW-001: a surrender adds the unit's population units to its side's prisoners, and the Why line says so without a
    // word of celebration (impact plan section 4.3 item 4).
    [Fact]
    public void ASurrenderTakesPrisoners()
    {
        var c = Load();
        var s = Surrounded(c);
        var why = Fight(c, s);
        Assert.False(s.Units.ContainsKey(2));
        Assert.Equal(new[] { ("egypt", 2) }, s.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
        Assert.Empty(s.SoldiersHeld);
        Assert.Contains(why, x => x.Message == "Unit 2 surrendered with 20 people: it had no hex to retreat to. They are held as prisoners");
    }

    // R-POW-001: a destroyed unit and a unit that retreats take no prisoners.
    [Fact]
    public void ADestroyedOrRetreatingUnitTakesNoPrisoners()
    {
        var c = Load();
        var destroyed = CombatTests.With(c, Attacking(Soldier(1, 3, 0, 1), 2, 0), Enemy(2, 2, 0, 1));
        Fight(c, destroyed);
        Assert.False(destroyed.Units.ContainsKey(2));
        var retreated = CombatTests.With(c, Attacking(Soldier(1, 3, 0, 9), 2, 0), Enemy(2, 2, 0, 3));
        Fight(c, retreated);
        Assert.True(retreated.Units.ContainsKey(2));
        Assert.Empty(destroyed.PrisonersHeld);
        Assert.Empty(retreated.PrisonersHeld);
    }

    // R-POW-001: no command takes prisoners: every command kind (CommandGuardTests.Samples, which fails when a kind is added
    // without a sample), played on a state that holds some and has no engagement, leaves the count as it was.
    [Fact]
    public void OnlySurrenderTakesPrisoners()
    {
        var c = Load();
        foreach (var cmd in CommandGuardTests.Samples.Values)
        {
            var s = Simulation.NewGame(c, 1, Mode.Historical);
            s.PrisonersHeld["egypt"] = 2;
            s.SoldiersHeld["egypt"] = 1;
            var r = Simulation.Step(c, s, new[] { cmd });
            Assert.Equal(new[] { ("egypt", 2) }, r.State.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
            Assert.Equal(new[] { ("egypt", 1) }, r.State.SoldiersHeld.Select(kv => (kv.Key, kv.Value)));
        }
    }

    // R-POW-001, R-POW-003: no event effect reaches the prisoners: the loader refuses any target that would name them.
    [Theory]
    [InlineData("prisoners")]
    [InlineData("prisoners_held")]
    [InlineData("soldiers_held")]
    public void NoEventEffectTargetsThePrisoners(string target)
    {
        Assert.DoesNotContain(target, EventRules.SpecialTargets);
        var card = $"id: deal\ntitle: A deal\ntext: A deal.\nfrom: 1948-05-15\noptions:\n  - {{ id: a, text: A, effects: [{{ target: {target}, delta: 1 }}] }}\n";
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["deal.yaml"] = card }));
        Assert.Equal($"options[a].effects.{target}", ex.Field.Substring(ex.Field.IndexOf("options", StringComparison.Ordinal)));
        Assert.Contains("unknown target", ex.Message);
    }

    // R-POW-003: prisoners are never labour, Money, Arms or housing: for every command kind, a state with prisoners and one
    // without end the turn with the same Money, Arms, cities (housing, jobs, people) and units, and Food that differs by the
    // upkeep alone.
    [Fact]
    public void PrisonersAreNeverAResource()
    {
        var c = Load();
        var m = c.RulesAt(new DateTime(1948, 5, 15)).Mvp!;
        foreach (var cmd in CommandGuardTests.Samples.Values)
        {
            var without = Simulation.NewGame(c, 1, Mode.Historical);
            var with = without.Clone();
            with.PrisonersHeld["egypt"] = 3;
            var a = Simulation.Step(c, without, new[] { cmd }).State;
            var b = Simulation.Step(c, with, new[] { cmd }).State;
            Assert.Equal((a.Treasury, a.Debt, a.Arms), (b.Treasury, b.Debt, b.Arms));
            Assert.Equal(a.Food - 3 * m.FoodPerPrisonerUnit!.Value, b.Food);
            Assert.Equal(string.Join(";", a.Cities.Values.Select(x => x.Canonical(false))), string.Join(";", b.Cities.Values.Select(x => x.Canonical(false))));
            Assert.Equal(string.Join(";", a.Units.Values.Select(x => x.Canonical())), string.Join(";", b.Units.Values.Select(x => x.Canonical())));
        }
    }

    // R-POW-004 (impact plan section 4.3 item 3): the score does not move on the turn of a surrender. The mutation run scores
    // at capture and must fail here.
    [Fact]
    public void NoScoreAtCapture()
    {
        var c = Load();
        var s = Surrounded(c);
        s.Score = 7;
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Equal(2, r.State.PrisonersHeld["egypt"]);
        Assert.Equal(7, r.State.Score);
    }

    /// <summary>Plays empty turns (the fixture's are weekly) until <paramref name="card"/> waits for a decision, checking it
    /// does not fire before its window opens on 1 July 1948.</summary>
    static StepResult UntilCard(Content c, GameState s, string card)
    {
        for (int i = 0; i < 20; i++)
        {
            var r = Simulation.Step(c, s, Array.Empty<Command>());
            if (r.State.PendingEvents.Contains(card))
            {
                Assert.True(r.State.Date >= new DateTime(1948, 7, 1));
                return r;
            }
            s = r.State;
        }
        throw new InvalidOperationException(card + " never fired");
    }

    // R-POW-004 (Q-POW-2, Q-POW-3): the front's card fires on the first turn in its window with prisoners held; resolving
    // it returns them and scores points_per_prisoner_unit_returned per unit. The Jordan card returns the Iraqi prisoners too
    // (Q-OBJ-1: the Jordan agreement takes over the Iraqi sector); nothing returns the ALA's (no front).
    [Fact]
    public void ScoredAtTheReturn()
    {
        var c = Load();
        var points = c.RulesAt(new DateTime(1948, 5, 15)).Mvp!.PointsPerPrisonerUnitReturned!.Value;
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PrisonersHeld["egypt"] = 3;
        s.PrisonersHeld["iraq"] = 2;
        s.PrisonersHeld["ala"] = 4;
        var r = UntilCard(c, s, "pow_egypt");
        Assert.Contains("pow_jordan", r.State.PendingEvents);   // both cards fire on the same turn
        Assert.Equal(0, r.State.Score);
        r = Simulation.Step(c, r.State, new Command[] { new ChooseEventOption("pow_egypt", "continue") });   // pow_jordan is delegated
        Assert.Equal(5 * points, r.State.Score);
        Assert.Equal(new[] { ("ala", 4) }, r.State.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
        Assert.Equal(new[] { ("egypt", 3), ("iraq", 2) }, r.State.PrisonersReturned.Select(kv => (kv.Key, kv.Value)));
        Assert.Contains(r.Why, x => x.Message == "Prisoners exchanged: 30 prisoners returned to Egypt");
    }

    // R-POW-004: a front's card waits for prisoners: with none held either way it does not fire, so a run with no surrender
    // fires no card (and its goldens do not move).
    [Fact]
    public void TheCardWaitsForPrisoners()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PrisonersHeld["ala"] = 1;   // no front's card returns them
        for (int i = 0; i < 4; i++) s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.DoesNotContain("pow_egypt", s.FiredEvents);
        Assert.DoesNotContain("pow_jordan", s.FiredEvents);
    }

    // R-POW-002 (Q-POW-5): prisoners eat food_per_prisoner_unit a unit in step 7, after the nation and the import.
    [Fact]
    public void PrisonersEatFood()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PrisonersHeld["egypt"] = 3;
        s.Food = 100;
        var morale = s.Indicators["morale"];
        var why = Upkeep(c, s);
        Assert.Equal(94, s.Food);
        Assert.Equal(morale, s.Indicators["morale"]);
        Assert.Contains(why, x => x.Message == "The prisoners held ate 6 units of Food");
    }

    // R-POW-002: a shortfall never reduces the prisoners.
    [Fact]
    public void AFoodShortfallNeverReducesPrisoners()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PrisonersHeld["egypt"] = 3;
        s.Food = 1;
        Upkeep(c, s);
        Assert.Equal(0, s.Food);
        Assert.Equal(3, s.PrisonersHeld["egypt"]);
    }

    // R-POW-002 (Q-POW-5): what the stock cannot give costs prisoner_shortfall_cohesion of Social cohesion a Food unit.
    [Fact]
    public void AFoodShortfallCostsCohesion()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.PrisonersHeld["egypt"] = 3;
        s.Food = 1;
        var morale = s.Indicators["morale"];
        var why = Upkeep(c, s);
        Assert.Equal(morale - 5 * 0.05, s.Indicators["morale"], 12);   // 6 needed, 1 given
        Assert.Contains(why, x => x.Message.StartsWith("The prisoners held went short of 5 units of Food: ", StringComparison.Ordinal));
    }

    // R-POW-002: the cost survives the whole turn: the effects web that runs after step 7 keeps the shift.
    [Fact]
    public void AFoodShortfallCostsCohesionOverATurn()
    {
        var c = Load();
        var without = Simulation.NewGame(c, 1, Mode.Historical);
        var with = without.Clone();
        with.PrisonersHeld["egypt"] = 1000;   // more than the stock after the import can feed
        var a = Simulation.Step(c, without, Array.Empty<Command>()).State;
        var b = Simulation.Step(c, with, Array.Empty<Command>()).State;
        Assert.Equal(0, b.Food);
        Assert.True(b.Indicators["morale"] < a.Indicators["morale"]);
        Assert.Equal(1000, b.PrisonersHeld["egypt"]);
    }

    // R-POW-005 (Q-POW-4): a surrendered unit of the player's is counted under the side of the engagement's lowest-id
    // attacker, not as prisoners and not in its units.
    [Fact]
    public void OwnSoldiersAreCountedWhenTheySurrender()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Enemy(4, 1, 0, 4, "iraqi", "iraq"), 0, 0), Attacking(Enemy(2, 0, 1, 4), 0, 0), Soldier(5, 0, 0, 3));
        Fight(c, s);
        Assert.False(s.Units.ContainsKey(5));
        Assert.Empty(s.PrisonersHeld);
        Assert.Equal(new[] { ("egypt", 2) }, s.SoldiersHeld.Select(kv => (kv.Key, kv.Value)));
    }

    // R-POW-005 with R-COMBAT-007: step 2 lists the guns after the other attackers, but the captor is still the lowest-id
    // attacker, here Egypt's gun and not the Iraqi unit beside it.
    [Fact]
    public void OwnSoldiersAreHeldByTheLowestIdAttackerWhenItIsAGun()
    {
        var c = Load();
        var s = CombatTests.With(c, Attacking(Enemy(2, 0, 1, 4, "egypt_gun"), 0, 0), Attacking(Enemy(4, 1, 0, 4, "iraqi", "iraq"), 0, 0), Soldier(5, 0, 0, 3));
        Fight(c, s);
        Assert.False(s.Units.ContainsKey(5));
        Assert.Equal(new[] { ("egypt", 2) }, s.SoldiersHeld.Select(kv => (kv.Key, kv.Value)));
    }

    // R-POW-005: the player's soldiers come home on their front's card, with no score.
    [Fact]
    public void OwnSoldiersReturnWithNoScore()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.SoldiersHeld["egypt"] = 2;
        s.Score = 7;
        s = UntilCard(c, s, "pow_egypt").State;   // the soldiers held alone make the card fire
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Empty(r.State.SoldiersHeld);
        Assert.Equal(7, r.State.Score);
        Assert.Contains(r.Why, x => x.Message == "Prisoners exchanged: 20 of our soldiers came home from Egypt");
    }

    // R-POW-005 (Q-POW-4): each turn they are held, the Treasury pays held_soldier_cost_per_turn a unit, borrowing what it lacks.
    [Fact]
    public void OwnSoldiersCostWhileHeld()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.SoldiersHeld["egypt"] = 2;
        s.Treasury = 0.4;
        var debt = s.Debt;
        Upkeep(c, s);
        Assert.Equal((0.0, debt + 0.6), (s.Treasury, s.Debt));   // IL 1m: 0.4 paid, 0.6 borrowed
    }

    // The counts are in the hash and the clone copies them (docs/determinism.md); a state with none hashes as before.
    [Fact]
    public void TheCountsAreState()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var none = s.Hash();
        var copy = s.Clone();
        copy.PrisonersHeld["egypt"] = 1;
        Assert.Empty(s.PrisonersHeld);
        Assert.NotEqual(none, copy.Hash());
        var back = s.Clone(); back.PrisonersReturned["egypt"] = 1; Assert.NotEqual(none, back.Hash());
        var held = s.Clone(); held.SoldiersHeld["egypt"] = 1; Assert.NotEqual(none, held.Hash());
        Assert.Equal(none, s.Clone().Hash());
    }

    // The card's front is a known front and the card has one option (it resolves without a draw); the values are required.
    [Theory]
    [InlineData("prisoners: egypt", "prisoners: iraq", "prisoners", "unknown front 'iraq'")]
    [InlineData("    historical: true\n", "    historical: true\n  - id: other\n    text: Other\n    alternative: { basis: design }\n", "options", "exactly one option")]
    public void BadCardsFailNamingTheField(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(new Dictionary<string, string> { ["pow_egypt.yaml"] = Card("pow_egypt", "egypt", "1948-07-01").Replace(from, to) }));
        Assert.Equal("events/pow_egypt.yaml", ex.File);
        Assert.Equal("events[pow_egypt]." + field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Theory]
    [InlineData("food_per_prisoner_unit: 2", "", "mvp.food_per_prisoner_unit", "required")]
    [InlineData("food_per_prisoner_unit: 2", "food_per_prisoner_unit: -1", "mvp.food_per_prisoner_unit", "must be >= 0")]
    [InlineData("prisoner_shortfall_cohesion: 0.05", "", "mvp.prisoner_shortfall_cohesion", "required")]
    [InlineData("prisoner_shortfall_cohesion: 0.05", "prisoner_shortfall_cohesion: -0.1", "mvp.prisoner_shortfall_cohesion", "must be a finite number >= 0")]
    [InlineData("points_per_prisoner_unit_returned: 7", "", "mvp.points_per_prisoner_unit_returned", "required")]
    [InlineData("points_per_prisoner_unit_returned: 7", "points_per_prisoner_unit_returned: -1", "mvp.points_per_prisoner_unit_returned", "must be >= 0")]
    [InlineData("held_soldier_cost_per_turn: 0.5", "", "mvp.held_soldier_cost_per_turn", "required")]
    [InlineData("held_soldier_cost_per_turn: 0.5", "held_soldier_cost_per_turn: -1", "mvp.held_soldier_cost_per_turn", "must be a finite number >= 0")]
    public void BadValuesFailNamingTheField(string from, string to, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Load(rules: Rules(from, to)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }
}
