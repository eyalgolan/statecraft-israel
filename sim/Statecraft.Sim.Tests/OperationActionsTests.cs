using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2544, R-ENEMY-008 and R-ENEMY-009: a military operation card carries out its moves and attacks, through the
/// player's own orders, under both GDD identities; a refused action does not stop the later ones; a player's order to a unit
/// the operation is moving cancels the operation's remaining orders; and the sim lists every action for the UI. A fixture of
/// two brigade rows on MovementTests' 6 x 3 coastal plain (3 movement points, an enemy unit on (1, 1)). Fixture values, not
/// history.</summary>
public class OperationActionsTests
{
    // brig_a starts on (0, 0) and brig_b on (0, 1), both in the card's region. Each row's anchor 1 is a far hex a go-to takes
    // two turns to reach; anchor 2, (1, 1), is the enemy's hex, the sourced target an attack row names.
    const string Rows = "units:\n" + "- id: brig_a\n  service: army\n  side: israel\n  status: draft\n"
        + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
        + "  - { place: Here, lon: 34.29, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
        + "  - { place: There, lon: 34.30, lat: 33.35, from: '1948-05-15', to: '1949-01-01' }\n"
        + "  - { place: Target, lon: 34.31, lat: 33.35, from: '1948-05-15', to: '' }\n"
        + "- id: brig_b\n  service: army\n  side: israel\n  status: draft\n"
        + "  strength: { value: 30, as_of: '1948-05-15' }\n  anchors:\n"
        + "  - { place: Here, lon: 34.29, lat: 33.36, from: '1948-05-15', to: '1949-01-01' }\n"
        + "  - { place: There, lon: 34.30, lat: 33.36, from: '1948-05-15', to: '1949-01-01' }\n"
        + "  - { place: Target, lon: 34.31, lat: 33.36, from: '1948-05-15', to: '' }\n";
    const string Hexes = "anchors:\n"
        + "  - { id: brig_a, anchor: 0, q: 0, r: 0 }\n  - { id: brig_a, anchor: 1, q: 5, r: 0 }\n  - { id: brig_a, anchor: 2, q: 1, r: 1 }\n"
        + "  - { id: brig_b, anchor: 0, q: 0, r: 1 }\n  - { id: brig_b, anchor: 1, q: 5, r: 2 }\n  - { id: brig_b, anchor: 2, q: 1, r: 1 }\n";

    static string Card(string id, string orders, string lines = "") =>
        "id: " + id + "\ntitle: Fixture\ntext: Fixture.\nfrom: 1948-05-15\n" + lines + "options:\n"
        + "  - id: ok\n    text: Accept\n    historical: true\n    requires_unit_regions: [fixture_region]\n"
        + "    unit_orders: [" + orders + "]\n"
        + "sources: [{ url: \"https://example.org/event\", checked: 2026-10-18 }]\n";

    static Content Load(bool gdd03, params (string Id, string Text)[] cards) => LoadIn(gdd03, "{ q: 0, r: 0 }, { q: 0, r: 1 }, { q: 1, r: 0 }", cards);

    /// <summary>The fixture with the card's region made of <paramref name="hexes"/>.</summary>
    static Content LoadIn(bool gdd03, string hexes, params (string Id, string Text)[] cards)
    {
        var files = gdd03 ? EnemyAITests.Gdd03Files() : Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = EnemyAITests.Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
        });
        files["map/units_1948.yaml"] = Rows;
        files["map/unit_anchor_hexes.yaml"] = Hexes;
        files["regions.yaml"] = EnemyAITests.OperationRegions.Replace("{ q: 0, r: 0 }, { q: 1, r: 0 }]", hexes + "]");
        files["map/locality_hexes.yaml"] = "localities: []\n";
        return ContentLoader.Load(Fixture.Write(files, cards.ToDictionary(x => x.Id + ".yaml", x => x.Text)));
    }

    static Content Load(bool gdd03, string orders, string lines = "") => Load(gdd03, ("probe", Card("probe", orders, lines)));

    /// <summary>A new game of <paramref name="c"/> with the probe card pending and an enemy unit of strength 5 on (1, 1).</summary>
    static GameState Game(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Units.Add(9, new Unit(9, "enemy", 1, 1, side: "egypt", strength: 5));
        s.NextId = Math.Max(s.NextId, 10);
        if (!s.PendingEvents.Contains("probe")) s.PendingEvents.Add("probe");
        return s;
    }

    static int Id(Content c, string row) => c.StartingForces.FindIndex(f => f.Row == row);

    // R-ENEMY-008: an attack row is { unit, order: attack, q, r }; `order` defaults to go_to. Its hex is one of the row's own
    // anchor or order target hexes, as a go-to's is (the maintainer's decision on #2544: only what a card's sources state),
    // and needs no end date, since an attack is fought when the card is decided. A row may move and then attack, but not
    // attack twice; any other order is refused.
    [Fact]
    public void TheLoaderReadsAttackRowsAndRefusesBadOnes()
    {
        var c = Load(false, "{ unit: brig_a, q: 5, r: 0 }, { unit: brig_a, order: attack, q: 1, r: 1 }");
        var o = c.Events.Single(e => e.Id == "probe").Options.Single();
        Assert.Equal(new[] { (Movement.GoTo, 5, 0), (Combat.Attack, 1, 1) }, o.UnitOrders.Select(x => (x.Order, x.Q, x.R)));

        var bad = Assert.Throws<ContentException>(() => Load(false, "{ unit: brig_a, order: retreat, q: 1, r: 1 }"));
        Assert.Equal("events[probe].options[ok].unit_orders[0].order", bad.Field);
        Assert.Contains("go_to or attack", bad.Message);

        var unsourced = Assert.Throws<ContentException>(() => Load(false, "{ unit: brig_a, order: attack, q: 2, r: 2 }"));
        Assert.Equal("events[probe].options[ok].unit_orders[0]", unsourced.Field);
        Assert.Contains("is not an anchor or an order target", unsourced.Message);

        var twice = Assert.Throws<ContentException>(() => Load(false, "{ unit: brig_a, order: attack, q: 1, r: 1 }, { unit: brig_a, order: attack, q: 1, r: 1 }"));
        Assert.Equal("events[probe].options[ok].unit_orders[1].unit", twice.Field);
        Assert.Contains("duplicate operation unit row", twice.Message);
    }

    // R-ENEMY-009: the operation a unit is in is found from the card's go-to row, so two cards may not send the same row to
    // the same hex: the loader refuses the second.
    [Fact]
    public void TwoCardsMayNotSendOneRowToOneHex()
    {
        var e = Assert.Throws<ContentException>(() => Load(false,
            ("probe", Card("probe", "{ unit: brig_a, q: 5, r: 0 }")), ("probe_two", Card("probe_two", "{ unit: brig_a, q: 5, r: 0 }"))));
        Assert.Equal(("events/probe_two.yaml", "events[probe_two].options[ok].unit_orders[0]"), (e.File, e.Field));
        Assert.Contains("events[probe] already sends 'brig_a' to hex q=5 r=0", e.Message);
    }

    // R-ENEMY-008 (owner, 4 November 2026: "deciding on a military operation makes the units do what the operation describes";
    // "Both rulesets (Recommended)"): the chosen card's go-to and attack run in list order under gdd-0.1 and gdd-0.3. The
    // attack is the player's instant attack (Combat.Strike): one engagement, one draw, the attacker's points spent. Each
    // action is listed, carried out, for the UI to replay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AChosenOperationCarriesOutItsMovesAndAttacks(bool gdd03)
    {
        var c = Load(gdd03, "{ unit: brig_a, q: 5, r: 0 }, { unit: brig_b, order: attack, q: 1, r: 1 }");
        int a = Id(c, "brig_a"), b = Id(c, "brig_b");
        var s = Game(c);
        var r = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") });

        Assert.Empty(r.Rejected);
        // The go-to is the player's own: the unit moves as far as the player's go-to would take it this turn.
        var own = Simulation.Preview(c, s, new Command[] { new UnitOrder(a, Movement.GoTo, 5, 0) }).State.Units[a];
        Assert.NotEqual((0, 0), (own.Q, own.R));
        Assert.Equal((own.Q, own.R, Movement.GoTo, "5,0"), (r.State.Units[a].Q, r.State.Units[a].R, r.State.Units[a].Order, r.State.Units[a].OrderTarget));
        Assert.Contains(r.Why, x => x.Key == WhyKeys.Engagement);
        Assert.NotEqual(s.RngState, r.State.RngState);   // the one engagement drew
        Assert.Equal(Movement.Points(c, r.State.Units[b]), r.Spent[b]);
        Assert.True(r.Attacked.ContainsKey(b));
        Assert.Equal(new[]
        {
            ("probe", "ok", 0, "brig_a", (int?)a, Movement.GoTo, 5, 0, OperationAction.CarriedOut, (string?)null),
            ("probe", "ok", 1, "brig_b", (int?)b, Combat.Attack, 1, 1, OperationAction.CarriedOut, (string?)null),
        }, r.OperationActions.Select(x => (x.EventId, x.OptionId, x.Index, x.Row, x.Unit, x.Order, x.Q, x.R, x.Outcome, x.Reason)));
    }

    // R-ENEMY-008 (owner on #2544: "Continue the rest (Recommended)"): a refused attack changes nothing and draws nothing. It is
    // rejected as a card's refused go-to is (Orders.RefusedCardOrder) and listed with the player's own reason; the actions
    // after it still run.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARefusedAttackDoesNotStopTheLaterActions(bool gdd03)
    {
        var c = Load(gdd03, "{ unit: brig_a, order: attack, q: 1, r: 1 }, { unit: brig_b, q: 5, r: 2 }");
        int a = Id(c, "brig_a"), b = Id(c, "brig_b");
        var s = Game(c);
        var r = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") });

        var refused = Assert.Single(r.Rejected);
        Assert.Equal((a, Combat.Attack, (int?)1, (int?)1, Orders.RefusedCardOrder),
            (((UnitOrder)refused.Command).Unit, ((UnitOrder)refused.Command).Order, ((UnitOrder)refused.Command).Q, ((UnitOrder)refused.Command).R, refused.Reason));
        Assert.Equal((0, 0, ""), (r.State.Units[a].Q, r.State.Units[a].R, r.State.Units[a].Order));
        Assert.Equal(s.RngState, r.State.RngState);
        Assert.Equal(s.Units[9].Strength, r.State.Units[9].Strength);
        Assert.Equal((Movement.GoTo, "5,2"), (r.State.Units[b].Order, r.State.Units[b].OrderTarget));
        Assert.Equal(new[] { (a, OperationAction.Refusal, Combat.RefusedNotAdjacent), (b, OperationAction.CarriedOut, (string?)null) },
            r.OperationActions.Select(x => (x.Unit!.Value, x.Outcome, x.Reason)));
    }

    // R-ENEMY-008: a delegated card's attack draws exactly as the chosen card's does. Red first: Delegate kept its own copy of
    // the RNG and wrote it back at the end, so the engagement's draw was lost and the next draw repeated it.
    [Fact]
    public void ADelegatedAttackKeepsItsDraw()
    {
        var c = Load(false, "{ unit: brig_b, order: attack, q: 1, r: 1 }", "anchor: hard\n");
        var chosen = Game(c);
        var delegated = chosen.Clone();
        var before = chosen.RngState;
        Assert.Null(EventSystem.Choose(c, chosen, "probe", "ok", new List<Cause>()));
        var actions = new List<OperationAction>();
        EventSystem.Delegate(c, delegated, new List<Cause>(), actions: actions);

        Assert.NotEqual(before, chosen.RngState);
        Assert.Equal(chosen.RngState, delegated.RngState);
        Assert.Equal(chosen.Hash(), delegated.Hash());
        Assert.Equal(OperationAction.CarriedOut, Assert.Single(actions).Outcome);
    }

    // R-ENEMY-009 (owner, 4 November 2026: "if a user tries to cancel orders of a unit that is in an operation, a message would
    // tell him that this will cancel the entire operation"; on #2544: "Only remaining orders (Recommended)"): a player's order
    // that takes a unit off its operation order cancels the operation's remaining orders. Every other unit of the operation
    // stops where it stands; what was done stays (the moves made). A refused order cancels nothing.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APlayersOrderToAUnitInTheOperationCancelsItsRemainingOrders(bool gdd03)
    {
        var c = Load(gdd03, "{ unit: brig_a, q: 5, r: 0 }, { unit: brig_b, q: 5, r: 2 }");
        int a = Id(c, "brig_a"), b = Id(c, "brig_b");
        var s = Game(c);
        var chosen = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") }).State;
        Assert.Equal(new[] { a, b }, EventSystem.OperationUnits(c, chosen, "probe"));
        Assert.Equal("probe", EventSystem.OperationOf(c, chosen, a));
        var moved = (chosen.Units[b].Q, chosen.Units[b].R);
        Assert.NotEqual((0, 1), moved);

        // A refused order (brig_a has no points left to attack with) leaves the operation as it was.
        var kept = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok"), new UnitOrder(a, Combat.Attack, 1, 1) });
        Assert.Single(kept.Rejected);
        Assert.Equal(new[] { a, b }, EventSystem.OperationUnits(c, kept.State, "probe"));
        Assert.DoesNotContain(kept.OperationActions, x => x.Outcome == OperationAction.Withdrawn);

        var r = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok"), new UnitOrder(a, Orders.Fortify) });
        Assert.Empty(r.Rejected);
        Assert.Equal(Orders.Fortifying, r.State.Units[a].Order);
        Assert.Equal((moved.Item1, moved.Item2, "", ""), (r.State.Units[b].Q, r.State.Units[b].R, r.State.Units[b].Order, r.State.Units[b].OrderTarget));
        Assert.False(r.State.CardOrders.ContainsKey(b));
        Assert.Empty(EventSystem.OperationUnits(c, r.State, "probe"));
        Assert.Null(EventSystem.OperationOf(c, r.State, a));
        Assert.Contains(r.Why, x => x.Key == WhyKeys.CardOrderEnded && x.Args.SequenceEqual(new[] { "5,2" }));
        Assert.Equal(new[] { ("probe", 0, a, Movement.GoTo, 5, 0), ("probe", 1, b, Movement.GoTo, 5, 2) },
            r.OperationActions.Where(x => x.Outcome == OperationAction.Withdrawn).Select(x => (x.EventId, x.Index, x.Unit!.Value, x.Order, x.Q, x.R)));
        Assert.DoesNotContain(r.Why, x => x.Key == WhyKeys.CardOrderEnded && x.Args.SequenceEqual(new[] { "5,0" }));   // the player's order is brig_a's line
        Assert.Equal(chosen.Units[a].Q, r.State.Units[a].Q);   // the moves made stay
    }
    // R-ENEMY-009 (owner, 8 November 2026, on #2956: "1 + it can effect the success of the first op as it has less forces"):
    // another card's order that takes a unit of an operation moves the unit to that card's operation and cancels nothing; the
    // first operation goes on with one unit fewer. Not red first: this pins the behaviour PR 1 already had when the owner ruled.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnotherCardTakesAUnitAndTheFirstOperationGoesOnWithoutIt(bool gdd03)
    {
        // The region is the whole board, so brig_b, already on its way, is still in place for the second card (R-ENEMY-007).
        var board = string.Join(", ", Enumerable.Range(0, 6).SelectMany(q => Enumerable.Range(0, 3).Select(r => $"{{ q: {q}, r: {r} }}")));
        var c = LoadIn(gdd03, board, ("probe", Card("probe", "{ unit: brig_a, q: 5, r: 0 }, { unit: brig_b, q: 5, r: 2 }")),
            ("other", Card("other", "{ unit: brig_b, q: 0, r: 1 }")));
        int a = Id(c, "brig_a"), b = Id(c, "brig_b");
        var s = Game(c);
        if (!s.PendingEvents.Contains("other")) s.PendingEvents.Add("other");
        s.Attention = 99;   // two cards decided in one turn; the Attention price is not under test
        var first = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok") });
        Assert.Equal(new[] { a, b }, EventSystem.OperationUnits(c, first.State, "probe"));

        var r = Simulation.Preview(c, s, new Command[] { new ChooseEventOption("probe", "ok"), new ChooseEventOption("other", "ok") });
        Assert.Empty(r.Rejected.Select(x => x.Reason));
        Assert.Equal(new[] { a }, EventSystem.OperationUnits(c, r.State, "probe"));
        Assert.NotEqual("5,2", r.State.Units[b].OrderTarget);
        Assert.Contains(r.OperationActions, x => (x.EventId, x.Unit, x.Outcome) == ("other", (int?)b, OperationAction.CarriedOut));
        Assert.Equal(first.State.Units[a].OrderTarget, r.State.Units[a].OrderTarget);   // brig_a still carries the first card's go-to
        Assert.Equal((first.State.Units[a].Q, first.State.Units[a].R), (r.State.Units[a].Q, r.State.Units[a].R));
        Assert.DoesNotContain(r.OperationActions, x => x.Outcome == OperationAction.Withdrawn);
        Assert.DoesNotContain(r.Why, x => x.Key == WhyKeys.CardOrderEnded);
    }
}
