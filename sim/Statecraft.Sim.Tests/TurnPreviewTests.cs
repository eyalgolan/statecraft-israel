using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>Spec 2026-10-23 section 3: the screen shows Simulation.Preview(turn start, committed orders), the same RunOrders
/// Step runs (R-MOVE-001, R-MOVE-002).</summary>
public class TurnPreviewTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    internal static Content Plain() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    internal static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);
    internal static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 3);

    // Spec 2026-10-23 section 3: the preview's Why lines are the start of End turn's: what the player saw is what End turn ran.
    [Fact]
    public void ThePreviewsLinesStartEndTurnsLines()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var orders = new Command[] { new Move(1, 1, 1), new Move(1, 2, 1) };
        var now = Simulation.Preview(c, s, orders);
        var ended = Simulation.Step(c, s, orders);
        Assert.Equal(now.Why.Select(w => w.Message), ended.Why.Take(now.Why.Count).Select(w => w.Message));
        Assert.Equal(2, now.Spent[1]);
        Assert.Empty(now.Rejected);
    }

    // Spec 2026-10-23 section 3: a refused order is in Rejected with the sim's reason, and the state is as it was.
    [Fact]
    public void ARefusedOrderIsRejectedAndChangesNothing()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 5, 1) });
        Assert.Equal("That hex is out of reach this turn", Assert.Single(now.Rejected).Reason);
        Assert.Equal(s.Hash(), now.State.Hash());
    }

    // A game that is over takes no order, as Step (Simulation.cs:42-46).
    [Fact]
    public void AGameThatIsOverTakesNoOrder()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        s.GameOver = GameOverCodes.MvpEnded;
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 1, 1) });
        Assert.StartsWith("The game is over", Assert.Single(now.Rejected).Reason);
    }

    // Spec 2026-10-23 section 3.2: moves, go-to, attacks, fortify and hold are instant; Send home and the rest wait for End turn.
    [Fact]
    public void WhichOrdersAreInstant()
    {
        Assert.True(Simulation.IsInstant(new Move(1, 1, 1)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Orders.Fortify)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Orders.Hold)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Combat.Attack, 2, 2)));
        Assert.False(Simulation.IsInstant(new UnitOrder(1, Demobilization.SendHome)));
        Assert.False(Simulation.IsInstant(new BuyArms(1)));
        Assert.False(Simulation.IsInstant(new ResolveCapture("lod", SettlementControl.Raid)));
        Assert.False(Simulation.IsInstant(new SetPolicy("rationing", 0.5)));
    }

    // R-MOVE-001: the points left are the type's less what the unit spent this turn.
    [Fact]
    public void PointsLeftCountTheTurnsMoves()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 2, 1) });
        Assert.Equal(1, Movement.PointsLeft(c, now.State, 1, now.Spent));
        Assert.Equal(3, Movement.PointsLeft(c, s, 1, new Dictionary<int, int>()));
    }

    // R-MOVE-010: the player's military units with points that have neither moved nor attacked this turn and have no order.
    [Fact]
    public void NeedsOrdersListsIdleUnitsThatHaveNotMoved()
    {
        var c = Plain();
        var fortified = new Unit(3, "soldier", 4, 1, side: "israel", strength: 3, order: Orders.Fortify, orderTarget: "4,1");
        var held = new Unit(4, "soldier", 5, 1, side: "israel", strength: 3, order: Orders.Hold);
        var s = With(c, Soldier(1, 0, 1), Soldier(2, 0, 2), fortified, held,
                     new Unit(5, "enemy", 5, 2, side: "egypt", strength: 3), new Unit(6, "halutz", 3, 2));
        var now = Simulation.Preview(c, s, new Command[] { new Move(2, 1, 2) });
        Assert.Equal(new[] { 1 }, Movement.NeedsOrders(c, now.State, now.Spent));
    }
}
