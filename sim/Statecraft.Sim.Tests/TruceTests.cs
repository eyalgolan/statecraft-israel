using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The player's side of a truce (GDD Phase 5 PR 11, slice 2; Q-WAR-3; #750): from accepting first_truce until
/// the turn covering 8 July 1948, and from accepting second_truce until the turn covering 15 October 1948, the player's
/// attack orders are refused with "A truce is in force", state unchanged, while movement and the other orders stay
/// allowed; accepting a truce ends and clears a queued attack, as on the enemy side (owner rulings, 22 October 2026,
/// #750). The enemy side's date-driven guard is R-ENEMY-006 (#1263).</summary>
public class TruceTests
{
    static Content Load(Dictionary<string, string>? events = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Fixture.GddUnitTypes
            + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
            + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
        ["map/hexes.json"] = MovementTests.Board(),
        ["corridors.yaml"] = "corridors: []\n",
    }), events));

    /// <summary>A game on <paramref name="date"/> with a soldier on (3, 0), an enemy on (2, 0), and the truce cards
    /// answered as given.</summary>
    static GameState State(Content c, DateTime date, params (string Event, string Option)[] chosen)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Date = date;
        s.Units.Add(1, new Unit(1, "soldier", 3, 0, side: "israel", strength: 5));
        s.Units.Add(2, new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3));
        foreach (var (e, o) in chosen) s.ChosenOptions[e] = o;
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] cmds) => Simulation.Step(c, s, cmds);

    // R-COMBAT-009: an attack order is refused with "A truce is in force" while a truce the player accepted is in
    // force, and the state is untouched.
    [Theory]
    [InlineData("1948-06-15", "first_truce", "accept")]    // inside the first truce
    [InlineData("1948-09-15", "second_truce", "comply")]   // inside the second truce
    public void AnAcceptedTruceRefusesAttacks(string date, string ev, string option)
    {
        var c = Load();
        var s = State(c, DateTime.Parse(date), (ev, option));
        var r = Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0));
        Assert.Equal("A truce is in force", Assert.Single(r.Rejected).Reason);
        Assert.Equal("", r.State.Units[1].Order);
    }

    // R-COMBAT-009: the truce binds from accepting the card, to the turn that covers its end (owner ruling 1, 22 Oct
    // 2026: a complying player attacks again from the 15 October 1948 turn). A refused or defied card binds nothing,
    // and no answer binds nothing.
    [Theory]
    [InlineData("1948-07-15", "first_truce", "accept")]    // the first truce ended on 8 July
    [InlineData("1948-10-15", "second_truce", "comply")]   // the second truce's last day: attacks are back
    [InlineData("1948-06-15", "first_truce", "refuse")]    // a refused card binds nothing
    [InlineData("1948-09-15", "second_truce", "defy")]
    [InlineData("1948-06-15", null, null)]                 // no card answered yet
    public void OutsideAnAcceptedTruceAttacksStand(string date, string? ev, string? option)
    {
        var c = Load();
        var s = ev == null ? State(c, DateTime.Parse(date)) : State(c, DateTime.Parse(date), (ev, option!));
        Assert.Empty(Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected);
    }

    // R-COMBAT-009: movement and the other standing orders stay allowed inside a truce.
    [Fact]
    public void ATruceAllowsMovementAndFortify()
    {
        var c = Load();
        var s = State(c, new DateTime(1948, 6, 15), ("first_truce", "accept"));
        Assert.Empty(Step(c, s, new Move(1, 4, 0)).Rejected);
        Assert.Empty(Step(c, s, new UnitOrder(1, Orders.Fortify)).Rejected);
    }

    /// <summary>R-COMBAT-009: the player's accepted truce binds the player's orders only - an enemy attack order on
    /// the same turn is untouched (R-ENEMY-006's own date-driven guard is the enemy side's).</summary>
    [Fact]
    public void AnAcceptedTruceDoesNotBindTheEnemy()
    {
        var c = Load();
        var s = State(c, new DateTime(1948, 6, 15), ("first_truce", "accept"));
        var why = new List<Cause>();
        Assert.Null(Combat.Give(c, s, 2, 3, 0, why, enemy: true));
        Assert.Equal(Combat.Attack, s.Units[2].Order);
    }

    /// <summary>R-COMBAT-009: the unit card asks <see cref="Combat.Check"/> which hexes to offer (#754) - inside an
    /// accepted truce it answers "A truce is in force" (the card hides Attack), and null for the same order once the
    /// window has ended.</summary>
    [Theory]
    [InlineData("1948-06-15", "A truce is in force")]   // inside the accepted first truce
    [InlineData("1948-07-15", null)]                    // the first truce ended on 8 July
    public void TheUnitCardsCheckReadsTheTruce(string date, string? expected)
    {
        var c = Load();
        var s = State(c, DateTime.Parse(date), ("first_truce", "accept"));
        Assert.Equal(expected, Combat.Check(c, s, 1, 2, 0));
    }

    // R-COMBAT-009 (owner ruling 2): accepting a truce ends and clears the player's standing attacks, with a line;
    // fortify and the enemy's orders are untouched. Choosing to fight on keeps them.
    [Fact]
    public void AcceptingATruceEndsAQueuedAttack()
    {
        var c = Load(TruceEvents);
        var s = State(c, new DateTime(1948, 6, 15));
        s.Units[1] = new Unit(1, "soldier", 3, 0, side: "israel", strength: 5, order: Combat.Attack, orderTarget: "2,0");
        s.Units[2] = new Unit(2, "enemy", 2, 0, side: "egypt", strength: 3, order: Combat.Attack, orderTarget: "3,0");

        s.PendingEvents.Add("first_truce");
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "first_truce", "accept", why));
        Assert.Equal("", s.Units[1].Order);
        Assert.Equal(Combat.Attack, s.Units[2].Order);   // the enemy's guard is R-ENEMY-006's, at resolver step 1
        Assert.Contains(why, w => w.Message.Contains("a truce is on"));

        // And from here the player cannot order a new attack until the truce's end.
        Assert.Equal("A truce is in force", Assert.Single(Step(c, s, new UnitOrder(1, Combat.Attack, 2, 0)).Rejected).Reason);

        // Choosing to fight on keeps a queued attack.
        var t = State(c, new DateTime(1948, 6, 15));
        t.Units[1] = new Unit(1, "soldier", 3, 0, side: "israel", strength: 5, order: Combat.Attack, orderTarget: "2,0");
        t.PendingEvents.Add("first_truce");
        Assert.Null(EventSystem.Choose(c, t, "first_truce", "refuse", new List<Cause>()));
        Assert.Equal(Combat.Attack, t.Units[1].Order);
    }

    /// <summary>The two truce cards as the fixture needs them: the accepting option and one that fights on. Fixture
    /// texts, not the cards'.</summary>
    static readonly Dictionary<string, string> TruceEvents = new Dictionary<string, string>
    {
        ["first_truce.yaml"] = @"
id: first_truce
title: The First Truce
text: The Security Council has called for a cease-fire.
office: prime_minister
from: 1948-06-01
options:
  - id: accept
    text: Accept the truce
    historical: true
    recommended: true
  - id: refuse
    text: Refuse the truce and keep fighting
    alternative: { basis: design }
",
        ["second_truce.yaml"] = @"
id: second_truce
title: The Second Truce
text: The Security Council has ordered a cease-fire.
office: prime_minister
from: 1948-07-15
options:
  - id: comply
    text: Comply with the cease-fire
    historical: true
    recommended: true
  - id: defy
    text: Defy the order and keep fighting
    alternative: { basis: design }
",
    };
}
