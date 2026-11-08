using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-MOVE-002, R-MOVE-008, R-MOVE-009 (owner, 23 October 2026: multi-turn go-to "Now"; spec 2026-10-23 section 6).</summary>
public class GoToTests
{
    static Content Board(params (int Q, int R, string Biome)[] biomes) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Fixture.GddUnitTypes
            + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
            + "  - { id: gun, kind: unit, side: israel, class: infantry, movement_points: 2 }\n"
            + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
        ["map/hexes.json"] = MovementTests.Board(biomes), ["corridors.yaml"] = "corridors: []\n"
    })));

    static readonly IReadOnlyDictionary<int, int> None = new Dictionary<int, int>();

    static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 3);

    // R-MOVE-002: the route's hexes carry the turn they are reached on: turn 1 spends the points left, later turns all.
    [Fact]
    public void TheRouteNumbersItsTurns()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3));
        var route = Movement.Route(c, s, 1, 5, 1, new Dictionary<int, int> { [1] = 1 });
        Assert.Equal(new[] { (1, 1, 1), (2, 1, 1), (3, 1, 2), (4, 1, 2), (5, 1, 2) }, route.Select(h => (h.Q, h.R, h.Turn)));
    }

    // R-MOVE-001, R-MOVE-002 (OQ-3): a step that costs more than the unit's full points is a turn of its own.
    [Fact]
    public void AStepCostingMoreThanFullPointsTakesATurn()
    {
        var c = Board((0, 0, "desert"), (0, 2, "desert"), (1, 0, "desert"), (1, 2, "desert"), (2, 0, "desert"), (2, 1, "desert"), (2, 2, "desert"));
        var s = CombatTests.With(c, new Unit(1, "gun", 0, 1, side: "israel", strength: 3));
        // 1,1 costs 1 on turn 1; 2,1 (desert, 3 > the gun's 2) cannot follow on turn 1 and takes all of turn 2; 3,1 is turn 3.
        Assert.Equal(new[] { (1, 1, 1), (2, 1, 2), (3, 1, 3) }, Movement.Route(c, s, 1, 3, 1, None).Select(h => (h.Q, h.R, h.Turn)));
    }

    // R-MOVE-002: Route searches the same land as Move: no water, no enemy unit's hex.
    [Fact]
    public void NoRouteToWaterOrAnEnemysHex()
    {
        var c = Board((5, 1, "sea"));
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3), new Unit(2, "enemy", 4, 1, side: "egypt", strength: 1));
        Assert.Empty(Movement.Route(c, s, 1, 5, 1, None));
        Assert.Empty(Movement.Route(c, s, 1, 4, 1, None));
    }

    // R-MOVE-008: a go-to moves the unit at once as far as its points reach and keeps the order.
    [Fact]
    public void AGoToMovesAtOnceAndStands()
    {
        var c = Board();
        var s = CombatTests.With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) });
        Assert.Empty(now.Rejected);
        Assert.Equal((3, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal((Movement.GoTo, "5,1"), (now.State.Units[1].Order, now.State.Units[1].OrderTarget));
    }

    // R-MOVE-009: the route step moves it on at the next turn's start, and it arrives on the turn the route showed.
    [Fact]
    public void ARouteArrivesOnTheTurnItShowed()
    {
        var c = Board();
        var s = CombatTests.With(c, Soldier(1, 0, 1));
        var shown = Movement.Route(c, s, 1, 5, 1, new Dictionary<int, int>()).Last().Turn;
        Assert.Equal(2, shown);   // three hexes on turn 1, two on turn 2, on the fixture's one-point plain
        var endOfTurn1 = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) }).State;
        Assert.Equal((3, 1), (endOfTurn1.Units[1].Q, endOfTurn1.Units[1].R));
        var startOfTurn2 = Simulation.Preview(c, endOfTurn1, new Command[0]).State;   // the route step has run
        Assert.Equal((5, 1), (startOfTurn2.Units[1].Q, startOfTurn2.Units[1].R));
        Assert.Equal("", startOfTurn2.Units[1].Order);
    }

    // R-MOVE-009, R-MOVE-004 (Q-MOVE-4, no zone of control): a route does not stop because an enemy unit is next to it.
    [Fact]
    public void ARouteGoesOnPastAnEnemy()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 2, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 3, 0, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal((5, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal("", now.State.Units[1].Order);   // arrived
    }

    // R-MOVE-009: an enemy unit on the destination leaves no route; the unit stops and needs orders (Review Focus 5).
    [Fact]
    public void ARouteToAHexAnEnemyTookStops()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 5, 1, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Contains(now.Why, w => w.Key == WhyKeys.StoppedNoRoute);
        Assert.Contains(1, Movement.NeedsOrders(c, now.State, now.Spent));   // it needs orders (Review Focus 5)
    }

    // R-MOVE-008: a destination full on arrival stops the route where the unit stands.
    [Fact]
    public void ARouteWhoseHexIsFullStopsOnArrival()
    {
        var c = Board();   // the fixture's stack limit is 2 a side (MovementTests' summary)
        var s = CombatTests.With(c, new Unit(1, "soldier", 4, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    Soldier(2, 5, 1), Soldier(3, 5, 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Equal((4, 1), (now.State.Units[1].Q, now.State.Units[1].R));
    }

    // R-MOVE-009: the route step moves units on in ascending id, not in the order the go-tos were given. Unit 2's is given
    // first; at the next turn's start unit 1 moves first and takes 5,1's last place, and unit 2 finds it full.
    [Fact]
    public void RoutesContinueInAscendingUnitId()
    {
        var c = Board();   // the fixture's stack limit is 2 a side (MovementTests' summary)
        var s = CombatTests.With(c, Soldier(1, 0, 1), Soldier(2, 0, 1), Soldier(3, 5, 1));
        var endOfTurn1 = Simulation.Step(c, s, new Command[] { new UnitOrder(2, Movement.GoTo, 5, 1), new UnitOrder(1, Movement.GoTo, 5, 1) }).State;
        Assert.Equal(((3, 1), (3, 1)), ((endOfTurn1.Units[1].Q, endOfTurn1.Units[1].R), (endOfTurn1.Units[2].Q, endOfTurn1.Units[2].R)));
        var now = Simulation.Preview(c, endOfTurn1, new Command[0]);   // the route step has run
        Assert.Equal((5, 1, ""), (now.State.Units[1].Q, now.State.Units[1].R, now.State.Units[1].Order));   // arrived
        Assert.Equal((3, 1, ""), (now.State.Units[2].Q, now.State.Units[2].R, now.State.Units[2].Order));   // stopped
        Assert.Contains(now.Why, w => w.Key == WhyKeys.StoppedFull && w.Message.StartsWith("Unit 2 "));
    }

    // R-MOVE-009: when every hex the route reaches this turn is full, the unit waits where it stands and keeps its go-to;
    // it is not stopped, as it is when the destination itself is full.
    [Fact]
    public void ARouteWhoseStopsThisTurnAreAllFullWaits()
    {
        var c = Board();   // the fixture's stack limit is 2 a side (MovementTests' summary)
        var s = CombatTests.With(c, new Unit(1, "gun", 0, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    Soldier(2, 1, 1), Soldier(3, 1, 1), Soldier(4, 2, 1), Soldier(5, 2, 1));
        // The gun's 2 points reach 1,1 and 2,1 this turn, and both hold two soldiers.
        Assert.Equal(new[] { (1, 1), (2, 1) }, Movement.Route(c, s, 1, 5, 1, None).Where(h => h.Turn == 1).Select(h => (h.Q, h.R)));
        var now = Simulation.Preview(c, s, new Command[0]);   // the route step has run
        Assert.Equal((0, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal((Movement.GoTo, "5,1"), (now.State.Units[1].Order, now.State.Units[1].OrderTarget));
        Assert.Equal(0, Movement.PointsLeft(c, s, 1, None) - Movement.PointsLeft(c, now.State, 1, now.Spent));   // no point spent
    }

    // R-MOVE-002, R-MOVE-003 (#2548): a turn of the route that would end on a hex full of the unit's side ends on the last
    // free hex before it, as the route step does, so the route shows the turn the unit really arrives on.
    [Fact]
    public void ARouteShowsTheTurnLostToAFullStackOnTheWay()
    {
        var c = Board();   // the fixture's stack limit is 2 a side (MovementTests' summary)
        var s = CombatTests.With(c, new Unit(1, "gun", 0, 1, side: "israel", strength: 3), Soldier(2, 2, 1), Soldier(3, 2, 1));
        var shown = Movement.Route(c, s, 1, 4, 1, None);
        // The gun's 2 points would end turn 1 on 2,1, which is full: it stops on 1,1, and 4,1 is a turn later.
        Assert.Equal(new[] { (1, 1, 1), (2, 1, 2), (3, 1, 2), (4, 1, 3) }, shown.Select(h => (h.Q, h.R, h.Turn)));
        var state = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Movement.GoTo, 4, 1) }).State;
        var turn = 1;
        while (true)
        {
            turn++;
            var start = Simulation.Preview(c, state, new Command[0]).State;   // the route step has run
            if ((start.Units[1].Q, start.Units[1].R) == (4, 1)) break;
            Assert.True(turn < 5, "the gun never arrived");
            state = Simulation.Step(c, state, new Command[0]).State;
        }
        Assert.Equal(shown.Last().Turn, turn);
    }

    // R-SAVE-003: a go-to is a committed unit order with its hex.
    [Fact]
    public void AGoToIsSavedAsAUnitOrderWithAHex()
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, SaveGame.GddRuleset, new string('b', 64));
        save.SetOpen(new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) });
        Assert.Contains("order 1 go_to 5 1", save.Write());
    }
}
