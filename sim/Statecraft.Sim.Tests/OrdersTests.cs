using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Standing orders (GDD Phase 5 PR 7, #746): UnitOrder(unit, fortify / hold). Q-HOLD-1 is the owner's "(a) 1 turn,
/// free, lost on move" and Q-HOLD-2 his "(a) Own hex, no interception" (15 October 2026, #375). The fixture's defence
/// percents are fixture values (fortify 150, hold 120), chosen apart from 100 so a test sees which one applied.</summary>
public class OrdersTests
{
    /// <summary>An Israeli soldier, an Egyptian unit (an enemy side) and the fixture's halutz types.</summary>
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    static Content Load() =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["unit_types.yaml"] = Types })));

    static GameState With(Content c, params Unit[] units)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        foreach (var u in units) s.Units.Add(u.Id, u);
        return s;
    }

    static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel");
    static Unit Enemy(int id, int q, int r) => new Unit(id, "enemy", q, r, side: "egypt");

    static int Defence(Content c, GameState s, int id) => Orders.DefencePercent(c.RulesAt(s.Date).Mvp!, s.Units[id]);

    // R-ORDER-001: an order stands, turn after turn, until the player gives another.
    [Fact]
    public void AnOrderStandsUntilChanged()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1));
        var r = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Orders.Hold) });
        Assert.Empty(r.Rejected);
        for (int i = 0; i < 3; i++) r = Simulation.Step(c, r.State, new Command[0]);
        Assert.Equal((Orders.Hold, ""), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
        r = Simulation.Step(c, r.State, new Command[] { new UnitOrder(1, Orders.Fortify) });
        Assert.Empty(r.Rejected);
        for (int i = 0; i < 3; i++) r = Simulation.Step(c, r.State, new Command[0]);
        Assert.Equal((Orders.Fortify, "2,1"), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
        r = Simulation.Step(c, r.State, new Command[] { new UnitOrder(1, Orders.Hold) });
        Assert.Equal((Orders.Hold, ""), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
    }

    // R-ORDER-002: fortify is not set during the turn it is given (combat, resolver step 2, sees no bonus), is set by the end
    // of that turn, and costs nothing: no Attention, no Money, nothing else in the state but the unit's order.
    [Fact]
    public void FortifyTakesOneTurnAndCostsNothing()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1));
        var given = s.Clone();
        Assert.Null(Orders.Give(given, 1, Orders.Fortify, new List<Cause>()));
        Assert.Equal(Orders.Fortifying, given.Units[1].Order);
        Assert.Equal(100, Defence(c, given, 1));   // the turn it is given

        var with = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Orders.Fortify) });
        var without = Simulation.Step(c, s, new Command[0]);
        Assert.Empty(with.Rejected);
        Assert.Equal(150, Defence(c, with.State, 1));   // from the next turn: the fixture's fortify percent
        Assert.Equal(without.State.Attention, with.State.Attention);
        Assert.Equal(without.State.Describe().Replace("1:soldier@2,1|side=israel", "1:soldier@2,1|side=israel|order=fortify|target=2,1"),
                     with.State.Describe());
    }

    // R-ORDER-001, R-ORDER-002: a fortified unit that leaves its hex loses fortify by the end of the turn, and has no bonus
    // from the moment it has left. This is the Settle backstop for a changed unit record that still carries its old order.
    [Fact]
    public void MovingClearsFortify()
    {
        var c = Load();
        var s = Simulation.Step(c, With(c, Soldier(1, 2, 1)), new Command[] { new UnitOrder(1, Orders.Fortify) }).State;
        var u = s.Units[1];
        s.Units[1] = new Unit(u.Id, u.Type, 3, 1, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget);   // a changed record still carrying its old order
        Assert.Equal(100, Defence(c, s, 1));
        var r = Simulation.Step(c, s, new Command[0]);
        Assert.Equal(("", ""), (r.State.Units[1].Order, r.State.Units[1].OrderTarget));
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Unit 1 left hex 2,1 and is no longer fortified");

        // Fortify given and then left in the same turn is never set.
        var moved = s.Clone();
        moved.Units[1] = new Unit(1, "soldier", 3, 1, side: "israel", order: Orders.Fortifying, orderTarget: "2,1");
        var left = Simulation.Step(c, moved, new Command[0]).State.Units[1];
        Assert.Equal(("", ""), (left.Order, left.OrderTarget));
    }

    // R-ORDER-003: hold acts on the unit itself and nothing around it: an enemy unit next to it and one past it keep their
    // hexes through the turn, and neither they nor a player unit beside it change their defence. Enemy moves come with the
    // enemy AI (Phase 5 PR 10); nothing in hold stops one.
    [Fact]
    public void HoldNeverIntercepts()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1), Soldier(2, 3, 1), Enemy(3, 1, 1), Enemy(4, 0, 1));
        var r = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Orders.Hold) });
        Assert.Empty(r.Rejected);
        Assert.Equal(120, Defence(c, r.State, 1));   // the fixture's hold percent, on its own hex
        foreach (var id in new[] { 2, 3, 4 })
        {
            Assert.Equal(100, Defence(c, r.State, id));
            Assert.Equal((s.Units[id].Q, s.Units[id].R, ""), (r.State.Units[id].Q, r.State.Units[id].R, r.State.Units[id].Order));
        }
    }

    // R-ORDER-001: refusals leave the state as it was.
    [Theory]
    [InlineData(9, "fortify", "There is no such unit")]
    [InlineData(1, "attack", "That order is not known")]
    [InlineData(5, "hold", "Only a military unit takes that order")]
    [InlineData(3, "hold", "That unit is not yours")]
    [InlineData(2, "hold", "That unit already has that order")]
    public void BadOrdersAreRefused(int unit, string order, string reason)
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1), new Unit(2, "soldier", 3, 1, side: "israel", order: Orders.Hold), Enemy(3, 0, 1), new Unit(5, "halutz", 4, 1));
        var r = Simulation.Step(c, s, new Command[] { new UnitOrder(unit, order) });
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Simulation.Step(c, s, new Command[0]).State.Describe(), r.State.Describe());
    }

    // R-ORDER-001: fortify again on the hex it is fortifying is refused; the legacy loop refuses the order before charging.
    [Fact]
    public void FortifyTwiceIsRefusedAndTheLegacyLoopRefusesTheOrder()
    {
        var c = Load();
        var s = Simulation.Step(c, With(c, Soldier(1, 2, 1)), new Command[] { new UnitOrder(1, Orders.Fortify) }).State;
        Assert.Equal("That unit already has that order", Assert.Single(Simulation.Step(c, s, new Command[] { new UnitOrder(1, Orders.Fortify) }).Rejected).Reason);

        var legacy = ContentLoader.Load(Fixture.Write());
        var g = Simulation.NewGame(legacy, 1, Mode.Historical);
        Assert.Equal("That order is not available in this game", Assert.Single(Simulation.Step(legacy, g, new Command[] { new UnitOrder(1, Orders.Hold) }).Rejected).Reason);
    }

    // R-ORDER-002, R-ORDER-003: the real game's values, design values; fortify 125 is owner-decided (19 Oct 2026, #1071), hold 100 is
    // this PR's proposal: fortify a quarter more in defence, hold nothing; the order is free (D-A6).
    [Fact]
    public void TheRealGameSetsFortifyAtOneQuarterMoreAndHoldNothing()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var m = c.Rules.Mvp!;
        Assert.Equal((125, 100), (m.FortifyDefencePercent, m.HoldDefencePercent));
        Assert.Equal(0, c.Rules.AttentionCost[Orders.Kind]);
    }

    // R-ORDER-002: both percents are required in the GDD loop and at least 1.
    [Theory]
    [InlineData("  fortify_defence_percent: 150\n", "", "mvp.fortify_defence_percent", "required when eras.yaml systems.loop is gdd")]
    [InlineData("  hold_defence_percent: 120\n", "", "mvp.hold_defence_percent", "required when eras.yaml systems.loop is gdd")]
    [InlineData("  hold_defence_percent: 120\n", "  hold_defence_percent: 0\n", "mvp.hold_defence_percent", "must be >= 1")]
    [InlineData("  fortify_defence_percent: 150\n", "  fortify_defence_percent: 0\n", "mvp.fortify_defence_percent", "must be >= 1")]
    public void ThePercentsAreRequired(string from, string to, string field, string message)
    {
        var files = Fixture.Gdd(new Dictionary<string, string> { ["unit_types.yaml"] = Types });
        files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }
}
