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
/// not, and town_e an enemy-held arab_town that is not a target. town_h, a town on (0, 2), is the fixture's one city: the loader
/// holds cities.yaml to the city and town rows (R-CITY-006).</summary>
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

    /// <summary>A target row. Stayed by default, so every Raid, Occupy and Deport test keeps its behaviour (#799 plan, Task 1);
    /// the annexation values are fixture values (people 60, employed_share 0.5), not history.</summary>
    internal static string Target(string id, int q, int r, bool fled = false) => FormattableString.Invariant(
        $"  - {{ id: {id}, q: {q}, r: {r}, operation: fixture_operation, sources: [{{ url: \"https://example.org/{id}\", checked: 2026-10-18 }}], ")
        + (fled ? "residents: fled, " : "residents: stayed, ")
        + FormattableString.Invariant($"residents_sources: [{{ url: \"https://example.org/{id}/residents\", checked: 2026-10-18 }}], ")
        + (fled ? "" : FormattableString.Invariant($"annexation: {{ people: {{ value: 60, status: sourced, date: 1948-11-08, sources: [{{ url: \"https://example.org/{id}/people\", checked: 2026-10-18 }}] }}, employed_share: {{ value: 0.5, status: design }} }}, "))
        + "status: sensitivity-reviewed }\n";

    /// <summary>MovementTests' 6 x 3 coastal plain with the four places; <paramref name="targets"/> lists the targets in
    /// file order.</summary>
    internal static Content Load(params string[] targets) => Load(null, targets);

    /// <summary>As <see cref="Load(string[])"/>, with <paramref name="edit"/> run on the files before they load (DeportTests
    /// turns Deport on with it, Q-TECH-3).</summary>
    internal static Content Load(Action<Dictionary<string, string>>? edit, params string[] targets) => Load(edit, Array.Empty<string>(), targets);

    /// <summary>As <see cref="Load(string[])"/>, with the rows in <paramref name="fled"/> written as <c>residents: fled</c>
    /// and no annexation block (Q-ANNEX-1; OWNER QUESTION 2 (b) on #799).</summary>
    internal static Content LoadFled(string[] fled, params string[] targets) => Load(null, fled, targets);

    internal static Content Load(Action<Dictionary<string, string>>? edit, string[] fled, string[] targets)
    {
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/settlement_hexes.yaml"] = "settlements:\n" + string.Concat(Places.Select(p => FormattableString.Invariant($"  - {{ id: {p.Id}, q: {p.Q}, r: {p.R} }}\n"))) + "  - { id: town_h, q: 0, r: 2 }\n",
            ["map/settlements_1948.yaml"] = "settlements:\n" + string.Concat(Places.Select(p => FormattableString.Invariant(
                $"  - {{ id: {p.Id}, name: {p.Id.Replace("town_", "Town ").ToUpperInvariant()}, lon: 34.29, lat: 33.35, type: {p.Type}, founded: 1900, pre_modern: false }}\n")))
                + "  - { id: town_h, name: Town H, lon: 34.29, lat: 33.15, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_h", 12, 8, 10, 6, "[military_camp, hakhshara]"),
            ["settlement_targets.yaml"] = targets.Length == 0 ? "targets: []\n" : "targets:\n" + string.Concat(targets.Select(id => { var p = Places.Single(x => x.Id == id); return Target(p.Id, p.Q, p.R, fled.Contains(p.Id)); })),
        });
        files["rules.yaml"] = files["rules.yaml"].Replace("enemy_held_localities: []", "enemy_held_localities: [town_a, town_c, town_e]");
        edit?.Invoke(files);
        return ContentLoader.Load(Fixture.Write(files));
    }

    internal static readonly string[] AllTargets = { "town_a", "town_c", "town_d" };

    static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);
    internal static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 2);
    internal static Unit Enemy(int id, int q, int r) => new Unit(id, "enemy", q, r, side: "egypt", strength: 2);

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
    // under enemy control, with the ownerless lost marker retained.
    [Fact]
    public void AnEnemyUnitHoldingACapturedTargetReturnsItToEnemyControl()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        Capture(c, s);
        var lost = Assert.Single(s.Settlements).Value;
        Assert.Equal(("", "", s.Date, false), (lost.Owner, lost.State, lost.Since, lost.Fled));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AStayedLostMarkerCanBeCapturedAgainWithANewDate(bool gdd03)
    {
        var c = OccupyTests.Load(gdd03);
        var s = With(c, Soldier(1, 1, 0));
        var old = s.Date.AddDays(-7);
        s.Settlements["town_a"] = new SettlementControl(since: old);
        var next = s.Date.AddDays(7);
        Capture(c, s);
        var record = s.Settlements["town_a"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.AwaitingChoice, next), (record.Owner, record.State, record.Since));
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

    // R-MOVE-005 in Phase 6 (#1304; the owner's #793 ruling that the targets "can be captured in play"): a player unit may
    // move onto an enemy-held target's hex, Reachable lists it, a path may run through it, and the unit that ends its move
    // there with no enemy unit on it captures the place at turn end (R-CTRL-004), through the whole resolver.
    [Fact]
    public void APlayerUnitMayEnterAnEnemyHeldTargetAndCaptureIt()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 0));
        Assert.Equal(1, Movement.Reachable(c, s, 1)[(1, 0)]);
        Assert.Equal(new[] { (1, 0), (2, 0) }, Movement.Path(c, s, 1, 2, 0));   // through town_a, not round it for 3
        var result = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0) });
        Assert.Empty(result.Rejected);
        Assert.Equal((1, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        var ctrl = Assert.Single(result.State.Settlements);
        Assert.Equal("town_a", ctrl.Key);
        Assert.Equal(SettlementControl.AwaitingChoice, ctrl.Value.State);
    }

    // R-MOVE-005 in Phase 6 (#1304): an enemy-held place that is not a target (Gaza's case, Q-TGT-1 (a)) stays closed:
    // Move is refused, Reachable leaves it out and no path runs through it.
    [Fact]
    public void AnEnemyHeldPlaceThatIsNotATargetStaysClosed()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 3, 2));
        var reach = Movement.Reachable(c, s, 1);
        Assert.False(reach.ContainsKey((4, 2)));
        Assert.Equal(3, reach[(5, 2)]);   // round town_e by (4, 1) and (5, 1), not through it for 2
        Assert.DoesNotContain((4, 2), Movement.Path(c, s, 1, 5, 2));
        var result = Simulation.Step(c, s, new Command[] { new Move(1, 4, 2) });
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible", Assert.Single(result.Rejected).Reason);
        Assert.Equal((3, 2), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.Empty(result.State.Settlements);
    }

    // #1304's done-when on the shipped content, so a change to settlement_targets.yaml cannot quietly reopen Gaza or close Acre:
    // a player unit beside Acre (a target) can reach it, and none beside Gaza (not one) can.
    [Fact]
    public void RealContentGazaStaysClosedAndAcreIsReachable()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var type = Simulation.NewGame(c, 1, Mode.Historical).Units.Values.First(u => u.Side == "israel").Type;
        (int Q, int R) Hex(string id) { var h = c.SettlementHexes.Single(x => x.Id == id); return (h.Q, h.R); }
        bool Reaches(string id)   // from any of the six hexes around it (some are sea or too far on the real map)
        {
            var (q, r) = Hex(id);
            return new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, -1), (-1, 1) }.Any(d =>
            {
                var s = With(c, new Unit(900, type, q + d.Item1, r + d.Item2, side: "israel", strength: 2));
                foreach (var uid in s.Units.Where(x => (x.Value.Q, x.Value.R) == (q, r)).Select(x => x.Key).ToList()) s.Units.Remove(uid);   // an enemy unit's hex is refused first
                return Movement.Reachable(c, s, 900).ContainsKey((q, r));
            });
        }
        Assert.False(Reaches("gaza"));
        Assert.True(Reaches("acre"));
    }

    // R-CTRL-004 (owner, 23 October 2026, OQ-2: "Instant"): a move that ends on an enemy-held target with no enemy unit
    // there captures it at once, before End turn; End turn stamps the turn's end as Since, as step 3 does.
    [Fact]
    public void AMoveEndingOnATargetCapturesItAtOnce()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 1, 0) });
        Assert.Equal(SettlementControl.AwaitingChoice, now.State.Settlements["town_a"].State);
        Assert.Equal(default(DateTime), now.State.Settlements["town_a"].Since);
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0) }).State;
        Assert.Equal(ended.Date, ended.Settlements["town_a"].Since);
    }

    // R-CTRL-004: passing through a target captures nothing (R-MOVE-005 lets a path pass through it): 0,0 to 2,0 crosses 1,0.
    [Fact]
    public void PassingThroughATargetCapturesNothing()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 0));
        Assert.Equal(new[] { (1, 0), (2, 0) }, Movement.Path(c, s, 1, 2, 0));   // precondition: the path crosses town_a
        Assert.Empty(Simulation.Preview(c, s, new Command[] { new Move(1, 2, 0) }).State.Settlements);
    }

    // R-CTRL-004: a unit that captured and left keeps the capture; only enemy units alone on it take it back, in step 3.
    [Fact]
    public void ACaptureStaysWhenTheUnitLeaves()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0), new Move(1, 0, 1) }).State;
        Assert.Equal(SettlementControl.AwaitingChoice, ended.Settlements["town_a"].State);
    }

    // R-CTRL-004: the check draws nothing.
    [Fact]
    public void TheInstantCheckDrawsNothing()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        Assert.Equal(s.RngState, Simulation.Preview(c, s, new Command[] { new Move(1, 1, 0) }).State.RngState);
    }

    // R-CTRL-007: the answer stays deferred: given in the capture's turn, it runs at End turn after the capturing move.
    [Fact]
    public void TheAnswerGivenInTheCapturesTurnRunsAtEndTurn()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0), new ResolveCapture("town_a", SettlementControl.Raid) });
        Assert.Empty(ended.Rejected);
        Assert.Equal("", ended.State.Settlements["town_a"].Owner);
    }

    // R-CTRL-004: the automatic route stop is checked before any order (spec 4.4, 6.3).
    [Fact]
    public void ARouteStopCapturesBeforeAnyPlayerOrder()
    {
        var c = Load(AllTargets);
        var s = With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 2,
            order: Movement.GoTo, orderTarget: "1,0"));
        var now = Simulation.Preview(c, s, Array.Empty<Command>());
        Assert.Equal((1, 0), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal(SettlementControl.AwaitingChoice, now.State.Settlements["town_a"].State);
        Assert.Equal(default(DateTime), now.State.Settlements["town_a"].Since);
    }

    // R-CTRL-004: same-turn answers preserve the capture date, including Deport's ownerless record (#1327).
    [Theory]
    [InlineData(SettlementControl.Occupy)]
    [InlineData(SettlementControl.Deport)]
    public void ANewCaptureAnsweredInTheSameTurnKeepsItsCaptureDate(string outcome)
    {
        var c = DeportTests.Load();
        var s = With(c, Soldier(1, 0, 1));
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0), new ResolveCapture("town_a", outcome) });
        Assert.Empty(ended.Rejected);
        Assert.Equal(ended.State.Date, ended.State.Settlements["town_a"].Since);
    }

    // R-CTRL-006 fixture: town_b, a kibbutz on (3, 1), appended after town_h so file order differs from ordinal id order.
    // Fixture values, not history.
    static Content LoadWithKibbutz() => Load(f =>
    {
        f["map/settlement_hexes.yaml"] += "  - { id: town_b, q: 3, r: 1 }\n";
        f["map/settlements_1948.yaml"] += "  - { id: town_b, name: Town B, lon: 34.29, lat: 33.25, type: kibbutz, founded: 1930, pre_modern: false }\n";
    }, AllTargets);

    static void AssertLost(GameState s, string id, DateTime since)
    {
        var x = s.Settlements[id];
        Assert.Equal(("", "", since, false, false, 0, "", 0), (x.Owner, x.State, x.Since, x.Depopulated, x.Fled, x.Combatants, x.Buildings, x.Turns));
    }

    // R-CTRL-006 (Q-TGT-2 (c), owner, 18 October 2026, #783; #796 default 3, owner, 21 October 2026): an enemy military unit
    // alone on a player's locality (town_h, a town that is not an arab_town and not enemy-held) changes its state with no
    // player choice: an ownerless record dated the turn's end, the same lost marker as R-CTRL-002, and one Why line.
    [Fact]
    public void AnEnemyUnitAloneOnAPlayersLocalityTakesIt()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 0, 2));
        var next = s.Date.AddDays(7);
        var why = Capture(c, s);
        AssertLost(s, "town_h", next);
        Assert.Single(s.Settlements);
        var line = Assert.Single(why);
        Assert.Equal(("town_h", "place_lost"), (line.Target, line.Key));
        Assert.Equal("Town H is lost: enemy units hold it and no Israeli unit is there", line.Message);
    }

    // R-CTRL-006: an Israeli military unit on the hex keeps it (no loss), and so does nobody on it; a halutz is no
    // combatant (R-UNIT-001; #796 default 1), so it does not keep it.
    [Fact]
    public void OnlyEnemyUnitsAloneTakeAPlayersLocality()
    {
        var c = Load(AllTargets);
        var both = With(c, Enemy(1, 0, 2), Soldier(2, 0, 2));
        Assert.Empty(Capture(c, both));
        Assert.Empty(both.Settlements);
        var none = With(c);
        Assert.Empty(Capture(c, none));
        Assert.Empty(none.Settlements);
        var halutz = With(c, Enemy(1, 0, 2), new Unit(2, "halutz", 0, 2));
        Capture(c, halutz);
        AssertLost(halutz, "town_h", halutz.Date.AddDays(7));
    }

    // R-CTRL-006 (#796 default 3): an arab_town is no player's locality, whether a target that is not enemy-held (town_d,
    // Jaffa's case) or an enemy-held place (town_e), so an enemy unit on it records nothing.
    [Fact]
    public void AnArabTownIsNoPlayersLocality()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 1, 2), Enemy(2, 4, 2));
        Assert.Empty(Capture(c, s));
        Assert.Empty(s.Settlements);
    }

    // R-CTRL-006 (#796 default 3: "Israeli units holding it again change it back"): Israeli military units alone on a lost
    // locality remove its record, so it is the player's again as at the start, with one Why line. A lost locality nobody
    // holds stays lost, and so does one both sides are on.
    [Fact]
    public void IsraeliUnitsRetakingAPlayersLocalityChangeItBack()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 0, 2));
        Capture(c, s);
        var lost = s.Settlements["town_h"].Since;
        s.Units.Clear();
        Assert.Empty(Capture(c, s));
        AssertLost(s, "town_h", lost);
        s.Units[1] = Enemy(1, 0, 2);
        s.Units[2] = Soldier(2, 0, 2);
        Assert.Empty(Capture(c, s));
        AssertLost(s, "town_h", lost);
        s.Units.Remove(1);
        var why = Capture(c, s);
        Assert.Empty(s.Settlements);
        var line = Assert.Single(why);
        Assert.Equal(("town_h", "place_retaken"), (line.Target, line.Key));
        Assert.Equal("Town H is retaken: Israeli units hold it again and no enemy unit is there", line.Message);
    }

    // R-CTRL-006, determinism: losses resolve in ordinal settlement id, not file order (town_b is after town_h in the
    // files) nor unit order, and the state is the same either way.
    [Fact]
    public void PlayersLocalitiesResolveInOrdinalOrder()
    {
        var c = LoadWithKibbutz();
        var s1 = With(c, Enemy(1, 0, 2), Enemy(2, 3, 1));
        var s2 = With(c, Enemy(2, 3, 1), Enemy(1, 0, 2));
        var why1 = Capture(c, s1);
        var why2 = Capture(c, s2);
        Assert.Equal(new[] { "town_b", "town_h" }, why1.Select(x => x.Target).ToArray());
        Assert.Equal(why1.Select(x => x.Message), why2.Select(x => x.Message));
        Assert.Equal(s1.Describe(), s2.Describe());
    }

    // R-CTRL-006, determinism: neither a loss nor a retake draws from the RNG.
    [Fact]
    public void ALossAndARetakeDrawNothing()
    {
        var c = LoadWithKibbutz();
        var s = With(c, Enemy(1, 0, 2), Enemy(2, 3, 1));
        var rng = s.RngState;
        Capture(c, s);
        Assert.Equal(2, s.Settlements.Count);
        s.Units.Clear();
        s.Units[3] = Soldier(3, 3, 1);
        Capture(c, s);
        Assert.Single(s.Settlements);
        Assert.Equal(rng, s.RngState);
    }

    // R-CTRL-006 and R-CTRL-005's mirror: a loss changes only the settlement state. Every resource, city, unit, the score
    // and every objective are as before step 3.
    [Fact]
    public void ALossTakesNothingElse()
    {
        var c = LoadWithKibbutz();
        var s = With(c, Enemy(1, 3, 1));
        var before = s.Clone();
        Assert.NotEmpty(Capture(c, s));
        s.Settlements.Clear();
        Assert.Equal(before.Describe(), s.Describe());
    }

    // R-ENEMY-001 with R-CTRL-006: an enemy unit may now end a move on a player's locality (town_h) and its path may pass
    // through one, but not on an arab_town that is not enemy-held (town_d, Jaffa's case) nor on an annexed target, which
    // R-CTRL-006 does not cover. A retreat still never ends on any of them (#2932: NoEntry is unchanged).
    [Fact]
    public void AnEnemyUnitMayMoveOntoAPlayersLocality()
    {
        var c = Load(AllTargets);
        var s = With(c, Enemy(1, 0, 1));
        var spent = new Dictionary<int, int>();
        Assert.Null(Movement.Move(c, s, 1, 0, 2, spent, new List<Cause>(), enemy: true));
        Assert.Equal((0, 2), (s.Units[1].Q, s.Units[1].R));
        var noEntry = Movement.NoEntry(c, s, true);
        Assert.Contains((0, 2), noEntry);
        Assert.Contains((1, 2), noEntry);
        var t = With(c, Enemy(1, 1, 1));
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible",
            Movement.Move(c, t, 1, 1, 2, new Dictionary<int, int>(), new List<Cause>(), enemy: true));
        t.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.Annexed, t.Date);
        Assert.Equal("Units may not enter an enemy-held locality before its capture is possible",
            Movement.Move(c, t, 1, 1, 0, new Dictionary<int, int>(), new List<Cause>(), enemy: true));
    }

    // R-CTRL-006 only as far as the rule needs (R-ENEMY-001): an enemy path goes round a player's locality it is not sent
    // to, as a card's go-to goes round every enemy-held place but its own (the owner's answer 1 on #2087), so a march never
    // takes a place on its way. From (4, 1) to (2, 1) the way through town_b on (3, 1) costs 2; the way round costs 3.
    [Fact]
    public void AnEnemyPathGoesRoundAPlayersLocalityItIsNotSentTo()
    {
        var c = LoadWithKibbutz();
        var s = With(c, Enemy(1, 4, 1));
        var spent = new Dictionary<int, int>();
        Assert.Null(Movement.Move(c, s, 1, 2, 1, spent, new List<Cause>(), enemy: true));
        Assert.Equal(3, spent[1]);
        var t = With(c, Enemy(1, 4, 1));
        var sent = new Dictionary<int, int>();
        Assert.Null(Movement.Move(c, t, 1, 3, 1, sent, new List<Cause>(), enemy: true));
        Assert.Equal(((3, 1), 1), ((t.Units[1].Q, t.Units[1].R), sent[1]));
    }

    // R-CTRL-006 through the whole resolver: an enemy unit alone on a player's locality at End turn takes it in step 3.
    [Fact]
    public void TheResolverRecordsTheLossAtTurnEnd()
    {
        var c = LoadWithKibbutz();
        var s = With(c, Enemy(1, 3, 1));
        var next = Simulation.Step(c, s, Array.Empty<Command>());
        AssertLost(next.State, "town_b", next.State.Date);
        Assert.Contains(next.Why, x => x.Key == "place_lost" && x.Target == "town_b");
    }

    // #1305 done-when: on the shipped content no player's locality is lost on the first turn (Historical mode; Open mode
    // arrives with milestone M4).
    [Fact]
    public void RealContentLosesNoPlayersLocalityOnTheFirstTurn()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        var next = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.DoesNotContain(next.Why, x => x.Key == "place_lost");
        Assert.Empty(next.State.Settlements);
    }
}
