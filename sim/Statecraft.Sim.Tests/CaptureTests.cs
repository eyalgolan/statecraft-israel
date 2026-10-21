using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Capture, resolver step 3 (GDD Phase 6 PR 4, #796). The owner's rulings of 18 October 2026 (#783, batch 2):
/// Q-CAP-1 (a), a settlement counts as captured when no enemy combatant is in the hex and a player unit holds it at turn end;
/// Q-CAP-3 (a), only enemy military units on the hex are its combatants. The owner's ruling of 21 October 2026 (#796): a
/// halutz holds nothing, "enemy-held" means mvp.enemy_held_localities, and enemy units alone on an awaiting_choice target
/// take it back. The fixture's places are fixture values, not
/// history: town_a and town_c are arab_town targets the fixture lists as enemy-held, town_d an arab_town target that is
/// not, and town_e an enemy-held arab_town that is not a target.</summary>
public class CaptureTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    static readonly (string Id, string Type, int Q, int R)[] Places =
    {
        ("town_a", "arab_town", 1, 0), ("town_c", "arab_town", 4, 0),
        ("town_d", "arab_town", 1, 2), ("town_e", "arab_town", 4, 2),
    };

    static string Target(string id, int q, int r) => FormattableString.Invariant(
        $"  - {{ id: {id}, q: {q}, r: {r}, operation: fixture_operation, sources: [{{ url: \"https://example.org/{id}\", checked: 2026-10-18 }}], status: sensitivity-reviewed }}\n");

    /// <summary>MovementTests' 6 x 3 coastal plain with the four places; <paramref name="targets"/> lists the targets in
    /// file order.</summary>
    static Content Load(params string[] targets)
    {
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/settlement_hexes.yaml"] = "settlements:\n" + string.Concat(Places.Select(p => FormattableString.Invariant($"  - {{ id: {p.Id}, q: {p.Q}, r: {p.R} }}\n"))),
            ["map/settlements_1948.yaml"] = "settlements:\n" + string.Concat(Places.Select(p => FormattableString.Invariant(
                $"  - {{ id: {p.Id}, name: {p.Id.Replace("town_", "Town ").ToUpperInvariant()}, lon: 34.29, lat: 33.35, type: {p.Type}, founded: 1900, pre_modern: false }}\n"))),
            ["settlement_targets.yaml"] = targets.Length == 0 ? "targets: []\n" : "targets:\n" + string.Concat(targets.Select(id => { var p = Places.Single(x => x.Id == id); return Target(p.Id, p.Q, p.R); })),
        });
        files["rules.yaml"] = files["rules.yaml"].Replace("enemy_held_localities: []", "enemy_held_localities: [town_a, town_c, town_e]");
        return ContentLoader.Load(Fixture.Write(files));
    }

    static readonly string[] AllTargets = { "town_a", "town_c", "town_d" };

    static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);
    static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 2);
    static Unit Enemy(int id, int q, int r) => new Unit(id, "enemy", q, r, side: "egypt", strength: 2);

    static GameState Step(Content c, GameState s) => Simulation.Step(c, s, Array.Empty<Command>()).State;

    static List<Cause> Capture(Content c, GameState s)
    {
        var why = new List<Cause>();
        SettlementControl.Capture(c, s, s.Date.AddDays(7), why);
        return why;
    }

    // R-CTRL-004 (Q-CAP-1 (a)): a target is captured at turn end when a player unit holds its hex and no enemy combatant
    // is there. The capture writes the player as owner, the turn's end as Since, and marks it as awaiting the choice.
    [Fact]
    public void ATargetIsCapturedWhenAPlayerUnitHoldsItWithNoEnemyCombatant()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 1, 0));
        var start = s.Date;
        var next = Step(c, s);
        var ctrl = Assert.Single(next.Settlements);
        Assert.Equal("town_a", ctrl.Key);
        Assert.Equal(Movement.PlayerSide, ctrl.Value.Owner);
        Assert.Equal(SettlementControl.AwaitingChoice, ctrl.Value.State);
        Assert.Equal(next.Date, ctrl.Value.Since);
        Assert.True(ctrl.Value.Since > start);
        Assert.Contains("ctrl{\n  town_a=owner=israel,state=awaiting_choice,since=", next.Describe());
    }

    // R-CTRL-004 (Q-CAP-1 (a), Q-CAP-3 (a)): an enemy military unit on the hex is a combatant, so no capture; and with
    // both sides on a captured target, neither holds it, so its record stays.
    [Fact]
    public void AnEnemyCombatantOnTheHexPreventsTheCapture()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 1, 0), Enemy(2, 1, 0));
        Assert.Empty(Step(c, s).Settlements);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        Assert.Empty(Capture(c, s));
        Assert.Equal(SettlementControl.AwaitingChoice, s.Settlements["town_a"].State);
    }

    // R-CTRL-004 (Q-CAP-1 (a); #796, 21 October 2026): no player unit, or only a halutz (no combatant, R-UNIT-001), holds
    // nothing.
    [Fact]
    public void NoPlayerCombatantNoCapture()
    {
        var c = Load(AllTargets);
        Assert.Empty(Step(c, With(c)).Settlements);
        Assert.Empty(Step(c, With(c, new Unit(1, "halutz", 1, 0))).Settlements);
        Assert.Empty(Step(c, With(c, Soldier(1, 2, 0))).Settlements);   // next to the target is not on it
    }

    // R-CTRL-004 (#796, 21 October 2026): a halutz beside a soldier is not an enemy unit, so the soldier still captures.
    [Fact]
    public void AHalutzBesideASoldierDoesNotBlockTheCapture()
    {
        var c = Load(AllTargets);
        var next = Step(c, With(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0)));
        Assert.Equal("town_a", Assert.Single(next.Settlements).Key);
    }

    // R-CTRL-004 (#793, "Only in a what-if"): a target that is not enemy-held at the start (Jaffa's case) offers no capture.
    [Fact]
    public void ATargetThatIsNotEnemyHeldIsNotCaptured()
    {
        var c = Load(AllTargets);
        Assert.Empty(Step(c, With(c, Soldier(1, 1, 2))).Settlements);
    }

    // R-CTRL-004 (#796, 21 October 2026): a captured target the enemy holds again, with no player combatant there, is back
    // under enemy control.
    [Fact]
    public void AnEnemyUnitHoldingACapturedTargetReturnsItToEnemyControl()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        Capture(c, s);
        Assert.Empty(s.Settlements);
    }

    // R-CTRL-001: a player unit on a locality that is not on the list changes nothing.
    [Fact]
    public void ANonTargetIsNeverCaptured()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 4, 2));   // town_e: an enemy-held arab_town, not a target
        Assert.Empty(Step(c, s).Settlements);
        var none = Load();   // an empty list
        Assert.Empty(Step(none, With(none, Soldier(1, 1, 0))).Settlements);
    }

    // R-CTRL-005: capture changes no resident field and grants nothing. Every resource, parameter, city, the score and
    // every objective are the same before and after step 3 on a capture turn; only the settlement state differs.
    [Fact]
    public void CaptureGrantsNothing()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 1, 0), Soldier(2, 4, 0));
        var before = s.Clone();
        Assert.NotEmpty(Capture(c, s));
        Assert.Equal(2, s.Settlements.Count);
        Assert.All(s.Settlements.Values, x => Assert.False(x.Depopulated));
        Assert.All(s.Settlements.Values, x => Assert.Equal(0, x.Combatants));
        s.Settlements.Clear();
        Assert.Equal(before.Describe(), s.Describe());
    }

    // R-CTRL-004, determinism: two captures in one turn give the same state whatever order the targets and the units
    // were added in.
    [Fact]
    public void SettlementsResolveInOrdinalOrder()
    {
        var c1 = Load("town_a", "town_c");
        var c2 = Load("town_c", "town_a");
        var s1 = With(c1, Soldier(1, 1, 0), Soldier(2, 4, 0));
        var s2 = With(c2, Soldier(2, 4, 0), Soldier(1, 1, 0));
        var why1 = Capture(c1, s1);
        var why2 = Capture(c2, s2);
        Assert.Equal(new[] { "town_a", "town_c" }, why1.Select(x => x.Target).ToArray());   // the Why lines, in ordinal id order
        Assert.Equal(why1.Select(x => x.Message), why2.Select(x => x.Message));
        Assert.Equal(s1.Describe(), s2.Describe());
    }

    // R-CTRL-004, determinism: step 3 draws nothing from the RNG.
    [Fact]
    public void NoRngDraw()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 1, 0), Soldier(2, 4, 0));
        var rng = s.RngState;
        Capture(c, s);
        Assert.Equal(2, s.Settlements.Count);
        Assert.Equal(rng, s.RngState);
    }
}
