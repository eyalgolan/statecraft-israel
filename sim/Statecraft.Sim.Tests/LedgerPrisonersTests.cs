using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1725 PR 7, #1710: Prisoners.Take and Prisoners.Upkeep on a game with an initialized Historical ledger, the
/// dark start. A surrendering opening formation's serving soldier records become Captured records under the captor's side
/// (R-POW-005, R-POP-002), and SoldiersHeld keeps its count next to them, with an invariant that the count per captor is the
/// Captured records with that captor. The prisoners eat and the held soldiers cost Money as today (R-POW-002, R-POW-005).
/// Owner rulings, 8 November 2026 (#1725): "Yes, as today (Recommended)", "Winning side, set at start (Recommended)",
/// "Formation's region (Recommended)", "Stays on record (Recommended)", "Keep count + check (Recommended)", "No, keep
/// count (Recommended)" and "Prisoners #1710 first (Recommended)". Owner rulings, 9 November 2026: "Go ahead on strength (Recommended)",
/// "Yes, in #1706 (Recommended)" and "Sent home (Recommended)".</summary>
public class LedgerPrisonersTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static string Take(Content c, GameState s, Unit u, Unit captor) => Prisoners.Take(c, s, u, captor);

    // The own opening formations on the board, lowest id first: unit id i is c.StartingForces[i] (StartingForces.Init).
    static List<Unit> Own(Content c, GameState s) => s.Units.Values
        .Where(u => u.Side == Movement.PlayerSide && u.Id < c.StartingForces.Count && u.Strength > 0).OrderBy(u => u.Id).ToList();

    static Unit Enemy(GameState s, string side) => s.Units.Values.Where(u => u.Side == side).OrderBy(u => u.Id).First();

    static string Key(Content c, Unit u) => HistoricalPopulation.FormationPrefix + c.StartingForces[u.Id].Row;

    static List<PopulationUnit> Serving(Content c, GameState s, Unit u) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == Key(c, u)).OrderBy(p => p.Id).ToList();

    // "Keep count + check (Recommended)": per captor, SoldiersHeld equals the Captured records with that captor.
    static void AssertHeldMatchesRecords(GameState s)
    {
        var records = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.Captured)
            .GroupBy(p => p.Captor!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        foreach (var side in records.Keys.Union(s.SoldiersHeld.Keys, StringComparer.Ordinal))
            Assert.Equal(records.TryGetValue(side, out var n) ? n : 0, s.SoldiersHeld.TryGetValue(side, out var m) ? m : 0);
    }

    // R-POW-005, R-POP-002: each serving record of the surrendering opening formation becomes Captured, captor the side of
    // the lowest-id attacker passed in ("Winning side, set at start (Recommended)"), Home the formation's region, region
    // only ("Formation's region (Recommended)"), no location and no assignment, its issued military item kept unchanged
    // ("Stays on record (Recommended)"), and its id, qualification, combat XP and reception kept. Nobody dies.
    [Fact]
    public void AnOwnSurrenderMakesItsRecordsCapturedWithTheirCaptor()
    {
        var c = Gdd03();
        var s = Dark(c);
        var u = Own(c, s)[0]; var captor = Enemy(s, "egypt");
        var before = Serving(c, s, u);
        Assert.Equal(u.Strength, before.Count);
        var next = s.PopulationLedger.NextPopulationId; var people = s.PopulationLedger.TotalPeople;
        var others = s.PopulationLedger.Units.Where(p => !before.Any(b => b.Id == p.Id)).Select(p => p.Canonical()).ToList();
        Assert.Equal("They are held by the enemy", Take(c, s, u, captor));
        foreach (var b in before)
        {
            var p = s.PopulationLedger.Get(b.Id);
            Assert.Equal(PopulationStatus.Captured, p.Status);
            Assert.Equal("egypt", p.Captor);
            Assert.Null(p.Location);
            Assert.Equal(b.Location!.Canonical(), p.Home!.Canonical());
            Assert.Null(p.Home!.City); Assert.Null(p.Home.Q);
            Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
            Assert.Equal(b.Equipment.Canonical(), p.Equipment.Canonical());
            Assert.NotNull(p.Equipment.MilitaryId);
            Assert.Equal((b.Qualification, b.CombatXp, b.ReceptionTicks, b.Profession, b.Tier, b.WorkXp),
                (p.Qualification, p.CombatXp, p.ReceptionTicks, p.Profession, p.Tier, p.WorkXp));
        }
        Assert.Empty(Serving(c, s, u));
        Assert.Equal(others, s.PopulationLedger.Units.Where(p => !before.Any(b => b.Id == p.Id)).Select(p => p.Canonical()));
        Assert.Equal((next, people), (s.PopulationLedger.NextPopulationId, s.PopulationLedger.TotalPeople));
        Assert.DoesNotContain(s.PopulationLedger.Canonical(), l => l.StartsWith("4:loss|", StringComparison.Ordinal));
        s.PopulationLedger.Reconcile();
    }

    // "Winning side, set at start (Recommended)": every enemy side with a military unit type in unit_types.yaml (sides other
    // than israel and "") is bound as a captor when the Historical ledger is initialized.
    [Fact]
    public void EveryEnemySideIsBoundAsACaptorAtTheStart()
    {
        var c = Gdd03();
        var sides = c.UnitTypes.Select(t => t.Side).Where(x => x != "" && x != Movement.PlayerSide).Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Contains("egypt", sides); Assert.Contains("ala", sides);
        // The canonical text writes each field as its length, a colon and its value (PopulationText.Fields).
        var lines = Dark(c).PopulationLedger.Canonical().Where(l => l.StartsWith("6:captor|", StringComparison.Ordinal)).ToList();
        Assert.Equal(sides.Select(x => $"6:captor|{x.Length}:{x}"), lines);
    }

    // "Keep count + check (Recommended)": SoldiersHeld rises by the formation's strength under the captor's side, as in the
    // aggregate game, and the count per captor equals the Captured records with that captor, over two captors.
    [Fact]
    public void SoldiersHeldRisesByTheSameCountAndMatchesTheRecords()
    {
        var c = Gdd03();
        var s = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var own = Own(c, s);
        Assert.True(own.Count >= 3);
        foreach (var (i, side) in new[] { (0, "egypt"), (1, "jordan"), (2, "egypt") })
        {
            Take(c, s, own[i], Enemy(s, side));
            Take(c, pub, pub.Units[own[i].Id], Enemy(pub, side));
            AssertHeldMatchesRecords(s);
        }
        Assert.Equal(own[0].Strength + own[2].Strength, s.SoldiersHeld["egypt"]);
        Assert.Equal(own[1].Strength, s.SoldiersHeld["jordan"]);
        Assert.Equal(pub.SoldiersHeld, s.SoldiersHeld);
        s.PopulationLedger.Reconcile();
    }

    // R-POW-001, "No, keep count (Recommended)": an enemy surrender adds its strength to PrisonersHeld under its side, as
    // today, and writes no ledger record (R-POP-004: enemy formations are not own population).
    [Fact]
    public void AnEnemySurrenderAddsPrisonersAndNoRecord()
    {
        var c = Gdd03();
        var s = Dark(c);
        var e = Enemy(s, "egypt");
        var ledger = s.PopulationLedger.Canonical().ToList();
        Assert.Equal("They are held as prisoners", Take(c, s, e, Own(c, s)[0]));
        Assert.Equal(e.Strength, s.PrisonersHeld["egypt"]);
        Assert.Empty(s.SoldiersHeld);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        s.PopulationLedger.Reconcile();
    }

    // R-POW-002, R-POW-005, "Yes, as today (Recommended)": on the dark start Upkeep feeds the prisoners from Food, a
    // shortfall costs cohesion, and the Treasury pays held_soldier_cost_per_turn a held unit, borrowing what it lacks, all
    // exactly as on the aggregate start with the same counts and stocks. Upkeep changes no ledger record; the captured own
    // records are not fed here (they eat the national ration, mvp.md R-FOOD-001/002 ledger paragraph).
    [Theory]
    [InlineData(1_000_000, 50.0)] [InlineData(3, 0.0)]
    public void UpkeepChargesAsTodayOnALedgerGame(int food, double treasury)
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var own = Own(c, dark)[0]; var e = Enemy(dark, "egypt");
        foreach (var s in new[] { dark, pub })
        {
            Take(c, s, s.Units[e.Id], s.Units[own.Id]);
            Take(c, s, s.Units[own.Id], s.Units[e.Id]);
            s.Food = food; s.Treasury = treasury; s.Debt = 0;
        }
        var r = c.RulesAt(dark.Date).Mvp!;
        var ledger = dark.PopulationLedger.Canonical().ToList();
        var moraleDark = dark.Indicators.GetValueOrDefault("morale"); var moralePub = pub.Indicators.GetValueOrDefault("morale");
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        Prisoners.Upkeep(c, dark, r, darkWhy);
        Prisoners.Upkeep(c, pub, r, pubWhy);
        Assert.Equal(pubWhy.Select(x => x.Message), darkWhy.Select(x => x.Message));
        Assert.Equal(pubWhy.Select(x => x.Delta), darkWhy.Select(x => x.Delta));
        Assert.Equal((pub.Food, pub.Treasury, pub.Debt), (dark.Food, dark.Treasury, dark.Debt));
        Assert.Equal(pub.Indicators.GetValueOrDefault("morale") - moralePub, dark.Indicators.GetValueOrDefault("morale") - moraleDark, 12);
        var need = (long)e.Strength * r.FoodPerPrisonerUnit!.Value;
        Assert.Equal(Math.Max(0, food - need), dark.Food);
        var cost = own.Strength * r.HeldSoldierCostPerTurn!.Value;
        Assert.True(cost > 0);
        Assert.Equal(treasury - cost + dark.Debt, dark.Treasury, 12);
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
        dark.PopulationLedger.Reconcile();
    }

    // R-POP-002 "refused before mutation": each refusal leaves the state, the ledger and the hash unchanged. A captor side
    // the ledger does not bind, and a count that already disagrees with the Captured records ("Keep count + check
    // (Recommended)"). The owner's rulings of 9 November 2026, "Go ahead on strength (Recommended)" and "Yes, in #1706
    // (Recommended)", took out the refusals of a strength its records do not number and of a trained formation: those
    // cases are captured now (the tests below).
    [Theory]
    [InlineData("unbound", "is not a bound captor")] [InlineData("count", "captured records with that captor")]
    public void ARefusedCaptureLeavesStateLedgerAndHashUnchanged(string why, string reason)
    {
        var c = Gdd03();
        var s = Dark(c);
        var u = Own(c, s)[0]; var captor = Enemy(s, "egypt");
        switch (why)
        {
            case "unbound": captor = new Unit(captor.Id, captor.Type, captor.Q, captor.R, side: "local", strength: captor.Strength); break;
            case "count": s.SoldiersHeld["egypt"] = 1; break;
        }
        var before = s.Describe(); var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList();
        var units = s.Units.Values.Select(x => x.Canonical()).ToList();
        Assert.Contains(reason, Assert.ThrowsAny<Exception>(() => Take(c, s, u, captor)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(units, s.Units.Values.Select(x => x.Canonical()));
    }

    // "Go ahead on strength (Recommended)" (owner, 9 November 2026): a formation with fewer serving records than its strength
    // surrenders; every record it has is captured, and SoldiersHeld grows by the records captured, not the strength, so the
    // count still equals the Captured records ("Keep count + check (Recommended)") and the next Take and Upkeep go on.
    [Fact]
    public void AFormationWithFewerRecordsThanStrengthHasAllItsRecordsCaptured()
    {
        var c = Gdd03();
        var s = Dark(c);
        var u = Own(c, s)[0]; var captor = Enemy(s, "egypt");
        var serving = Serving(c, s, u);
        Assert.True(serving.Count >= 3);
        foreach (var p in serving.Skip(serving.Count - 2)) s.PopulationLedger.RegisterCombatLoss(p.Id, "fixture");
        var left = Serving(c, s, u);
        Assert.Equal(u.Strength - 2, left.Count);
        Assert.Equal("They are held by the enemy", Take(c, s, u, captor));
        Assert.Empty(Serving(c, s, u));
        foreach (var p in left)
            Assert.Equal((PopulationStatus.Captured, "egypt"), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Assert.Equal(left.Count, s.SoldiersHeld["egypt"]);
        AssertHeldMatchesRecords(s);
        Take(c, s, Own(c, s)[1], captor);
        Prisoners.Upkeep(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
        AssertHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // "Go ahead on strength (Recommended)": a formation with more serving records than its strength surrenders exactly its
    // strength of them, the lowest ids first (as "Lowest id first (Recommended)" and docs/GDD.md's ascending population
    // ids). SoldiersHeld grows by the strength, as in the aggregate game. "Sent home (Recommended)" (owner, 9 November
    // 2026): the leftover records leave service as Send home's do (R-UNIT-005: "Nearest city, unemployed (Recommended)",
    // "Settled in (Recommended)", "Keep both (Recommended)", "Back to Arms stock (Recommended)"): free civilians without an
    // assignment in the nearest city to the formation's hex, reception ticks reception.turns, qualification and combat XP
    // kept, the issued item back in the Arms stock once. No record is made or lost.
    [Fact]
    public void AFormationWithMoreRecordsThanStrengthHasItsLowestRecordsCapturedAndTheRestSentHome()
    {
        var c = Gdd03();
        var s = Dark(c);
        var o = Own(c, s)[0]; var captor = Enemy(s, "egypt");
        var u = new Unit(o.Id, o.Type, o.Q, o.R, o.Service, o.Side, o.Strength - 2, o.Order, o.OrderTarget, o.HomeCity, o.Full, o.CombatXp);
        s.Units[u.Id] = u;
        var serving = Serving(c, s, u);
        Assert.Equal(u.Strength + 2, serving.Count);
        var leftover = serving.Skip(u.Strength).ToList();
        Assert.Equal(new[] { serving[u.Strength].Id, serving[u.Strength + 1].Id }, leftover.Select(p => p.Id));
        Assert.All(leftover, p => Assert.NotNull(p.Equipment.MilitaryId));
        var city = Demobilization.Nearest(c, u.Q, u.R);
        var region = c.PlayerRegions.AtLocation(city)!.Id;
        var received = c.RequirePopulationInteger("reception.turns");
        var arms = s.Arms; var next = s.PopulationLedger.NextPopulationId; var count = s.PopulationLedger.Units.Count;
        var cities = string.Join(";", s.Cities.Values.Select(x => x.Canonical()));
        Take(c, s, u, captor);
        foreach (var p in serving.Take(u.Strength))
            Assert.Equal((PopulationStatus.Captured, "egypt"), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Assert.Empty(Serving(c, s, u));
        var home = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.FreeCivilian && serving.Any(w => w.Id == p.Id))
            .Select(p => p.Id).OrderBy(x => x).ToList();
        Assert.Equal(leftover.Select(p => p.Id), home);
        foreach (var was in leftover)
        {
            var p = s.PopulationLedger.Get(was.Id);
            Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
            Assert.Equal((region, city), (p.Location!.Region, p.Location.City));
            Assert.Equal(received, p.ReceptionTicks);
            Assert.Null(p.Captor);
            Assert.Null(p.Equipment.MilitaryId);
            Assert.Equal((was.Qualification, was.CombatXp, was.Profession, was.Tier, was.WorkXp, was.Equipment.ToolsId),
                (p.Qualification, p.CombatXp, p.Profession, p.Tier, p.WorkXp, p.Equipment.ToolsId));
        }
        Assert.Equal(arms + leftover.Count, s.Arms);
        Assert.Equal((next, count), (s.PopulationLedger.NextPopulationId, s.PopulationLedger.Units.Count));
        Assert.Equal(cities, string.Join(";", s.Cities.Values.Select(x => x.Canonical())));   // "No, records only (Recommended)"
        Assert.Equal(u.Strength, s.SoldiersHeld["egypt"]);
        AssertHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // "Yes, in #1706 (Recommended)" (owner, 9 November 2026): a trained formation's soldiers are found as battles find them,
    // on Training.FormationTarget (formation/<unit id>, #2964), and become Captured; a player unit with no records on its
    // target captures none and adds nothing to SoldiersHeld ("Go ahead on strength (Recommended)").
    [Fact]
    public void ATrainedFormationsRecordsAreCaptured()
    {
        var c = Gdd03();
        var s = Dark(c);
        s.Arms = 200; s.Treasury = 100; s.Debt = 0;
        Assert.Null(Training.Start(c, s, "tel_aviv", "recruit", 1, new List<Cause>()));
        var unit = s.NextId;
        for (int i = 0; i < c.UnitTypes.First(t => t.Id == "recruit").TrainingTurns; i++) Training.Progress(c, s, new List<Cause>());
        Assert.True(unit >= c.StartingForces.Count);
        var u = s.Units[unit];
        var target = Training.FormationTarget(c, unit);
        var serving = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == target)
            .OrderBy(p => p.Id).ToList();
        Assert.Equal(u.Strength, serving.Count);
        Assert.Equal("They are held by the enemy", Take(c, s, u, Enemy(s, "jordan")));
        foreach (var p in serving)
            Assert.Equal((PopulationStatus.Captured, "jordan"), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Assert.Equal(u.Strength, s.SoldiersHeld["jordan"]);
        AssertHeldMatchesRecords(s);

        var none = new Unit(s.NextId, "recruit", u.Q, u.R, side: Movement.PlayerSide, strength: 1); s.Units.Add(none.Id, none); s.NextId++;
        var ledger = s.PopulationLedger.Canonical().ToList();
        Take(c, s, none, Enemy(s, "jordan"));
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(u.Strength, s.SoldiersHeld["jordan"]);
        AssertHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // "Keep count + check (Recommended)": Upkeep refuses a count that disagrees with the records, with nothing paid or eaten.
    [Fact]
    public void UpkeepRefusesACountThatDisagreesWithTheRecords()
    {
        var c = Gdd03();
        var s = Dark(c);
        s.SoldiersHeld["egypt"] = 1;
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        Assert.Contains("captured records with that captor",
            Assert.Throws<InvalidOperationException>(() => Prisoners.Upkeep(c, s, c.RulesAt(s.Date).Mvp!, why)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through; an Open or synthetic one is still refused
    // before any write, by Take and by Upkeep.
    [Theory]
    [InlineData(true, false)] [InlineData(false, false)] [InlineData(true, true)] [InlineData(false, true)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open, bool upkeep)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        Action call = upkeep ? () => Prisoners.Upkeep(c, s, new MvpRules(), why)
            : () => Take(c, s, new Unit(0, "missing", 0, 0, side: Movement.PlayerSide, strength: 1), new Unit(1, "missing", 0, 0, side: "egypt"));
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(call).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // Where Take's refusal can be reached from combat, and what it leaves. Since #1706 both Combat.Resolve (step 2,
    // TurnResolver.cs:20) and the player's instant attack, Combat.Strike (its one caller is Step, Simulation.cs:268), fight
    // on a dark start and can reach Prisoners.Take (LedgerCombatTests covers Resolve's surrender). Here an
    // enemy unit boxed in by own units surrenders to a Strike while SoldiersHeld disagrees with the Captured records, so Take
    // throws InvalidOperationException before it writes anything. The throw comes after the Strike's own writes (the
    // attacker's order and the engagement's draw and losses), and those stay on the state object a direct call is given. In
    // the game that object is never the caller's: Step works on a clone, so the throw discards the whole
    // command's result and Step's caller is unchanged. A Clone() taken before the call pins that isolation. Step itself
    // cannot be driven there: the refusal needs an initialized Historical ledger, and Step refuses any initialized ledger
    // before it clones, while a game without a ledger skips every ledger check in Take.
    [Fact]
    public void ARefusedCaptureInCombatThrowsAndAPriorCloneIsUnchanged()
    {
        var c = Gdd03();
        var s = Dark(c);

        // The weakest enemy unit (lowest id on a tie), attacked by the strongest own formation (lowest id on a tie).
        var e = s.Units.Values.Where(u => Prisoners.FrontOf.ContainsKey(u.Side) && u.Strength > 0)
            .OrderBy(u => u.Strength).ThenBy(u => u.Id).First();
        var own = Own(c, s).OrderByDescending(u => u.Strength).ThenBy(u => u.Id).ToList();
        var ring = Movement.Neighbours.Select(d => (Q: e.Q + d.Q, R: e.R + d.R))
            .Where(h => c.HexAt(h.Q, h.R) is MapHex hex && hex.Land).ToList();
        Assert.NotEmpty(ring);
        Assert.True(own.Count >= ring.Count);
        for (int i = 0; i < ring.Count; i++)
        {
            var u = own[i];
            s.Units[u.Id] = new Unit(u.Id, u.Type, ring[i].Q, ring[i].R, u.Service, u.Side, u.Strength, "", "", u.HomeCity, u.Full, u.CombatXp);
        }
        var attacker = own[0].Id;
        // A seed whose one draw makes the defender retreat with no hex to go to: with the counts in order, it surrenders.
        string? Strike(GameState g, List<Cause> why) => Combat.Strike(c, g, attacker, e.Q, e.R, new Dictionary<int, int>(),
            new Dictionary<int, AttackRecord>(), why);
        ulong seed = 1;
        for (; seed < 500; seed++)
        {
            var trial = s.Clone(); trial.RngState = seed; var why = new List<Cause>();
            Assert.Null(Strike(trial, why));
            if (why.Any(x => x.Key == WhyKeys.SurrenderedPrisoners && x.Units.Any(u => u.Id == e.Id))) break;
        }
        Assert.True(seed < 500, "no seed gave a surrender");
        s.RngState = seed;
        s.SoldiersHeld["egypt"] = 1;   // the count disagrees with the records: Take refuses (R-POW-005)

        var prior = s.Clone();
        var hash = prior.Hash(); var ledger = prior.PopulationLedger.Canonical().ToList();
        var lines = new List<Cause>();
        Assert.Contains("captured records with that captor",
            Assert.Throws<InvalidOperationException>(() => Strike(s, lines)).Message);
        Assert.Equal(hash, prior.Hash());
        Assert.Equal(ledger, prior.PopulationLedger.Canonical());
        // Take wrote nothing: the ledger and the counts are as they were, and the defender is still on the board.
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(1, s.SoldiersHeld["egypt"]);
        Assert.Equal(prior.PrisonersHeld.TryGetValue(e.Side, out var was) ? was : 0, s.PrisonersHeld.TryGetValue(e.Side, out var held) ? held : 0);
        Assert.True(s.Units.ContainsKey(e.Id));
        // The Strike's own writes before the throw remain on the object passed in: the draw, and the engagement's line.
        Assert.NotEqual(prior.RngState, s.RngState);
        Assert.NotEqual(hash, s.Hash());
        Assert.Contains(lines, x => x.Key == WhyKeys.Engagement);
    }

    // R-POW-005, #2473: Prisoners.Return still refuses a Historical ledger. It removes a front's SoldiersHeld count but moves
    // no Captured record, so on a dark start after a real capture it must throw before any write; lifting that refusal must
    // move the records home in the same change, or the next Take or Upkeep finds the count and the records disagree.
    [Fact]
    public void ReturnStillRefusesAHistoricalLedger()
    {
        var c = Gdd03();
        var s = Dark(c);
        Take(c, s, Own(c, s)[0], Enemy(s, "egypt"));
        Assert.True(s.SoldiersHeld["egypt"] > 0);
        Assert.True(s.PopulationLedger.IsHistorical);
        var before = s.Describe(); var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList(); var why = new List<Cause>();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Prisoners.Return(c, s, "egypt", why)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        Assert.Equal(ledger, s.PopulationLedger.Canonical()); Assert.Empty(why);
        AssertHeldMatchesRecords(s);
    }

    // Determinism: Take and Upkeep draw no RNG; two dark games given the same captures and upkeep end with the same hash and
    // ledger text, and a capture changes the hash.
    [Fact]
    public void TheSameCapturesGiveTheSameHash()
    {
        var c = Gdd03();
        string Run(out string start)
        {
            var s = Dark(c); start = s.Hash();
            var own = Own(c, s); var rng = s.RngState;
            Take(c, s, own[1], Enemy(s, "syria")); Take(c, s, own[0], Enemy(s, "egypt")); Take(c, s, Enemy(s, "iraq"), own[2]);
            Prisoners.Upkeep(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
            Assert.Equal(rng, s.RngState);
            return s.Hash() + "\n" + string.Join("\n", s.PopulationLedger.Canonical());
        }
        var a = Run(out var startA); var b = Run(out var startB);
        Assert.Equal(startA, startB);
        Assert.Equal(a, b);
        Assert.NotEqual(startA, a.Split('\n')[0]);
    }
}
