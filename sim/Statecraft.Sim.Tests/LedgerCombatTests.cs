using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1706, #1725 PR 7: Combat.Resolve and Combat.Outcomes on a game with an initialized Historical ledger, the dark
/// start. Each population unit an own formation loses in an engagement removes one of its serving soldier records from the
/// ledger as a combat loss (R-COMBAT-003, R-POP-001), the player's instant attack and step 2's alike; the units, the draw
/// and the Why lines are the aggregate game's. Owner rulings, 8 November 2026 (#1725): "Lowest id first (Recommended)",
/// "No, losses are deaths (Recommended)", "No, formation only (Recommended)", "Go ahead on strength", "Date + battle hex
/// (Recommended)", "Prisoners #1710 first (Recommended)", "No, keep region (Recommended)" and "Yes, after #2964
/// (Recommended)". Owner rulings, 9 November 2026: "Go ahead on strength (Recommended)", "Yes, in #1706 (Recommended)",
/// "Sent home (Recommended)" and "Records captured (Recommended)" for a surrender (R-POW-005); on a ledger game the
/// surrender Why line states the records captured, and a formation's leftover records add Send home's Arms line.</summary>
public class LedgerCombatTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static GameState Aggregate(Content c) => Simulation.NewGame(c, 1, Mode.Historical);

    // "Yes, after #2964 (Recommended)": a formation's soldiers are the active-soldier records on its target (#2964's link).
    static List<PopulationUnit> Serving(Content c, GameState s, int unit) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == Training.FormationTarget(c, unit))
        .OrderBy(p => p.Id).ToList();

    // "Date + battle hex (Recommended)": combat/<turn date>/<q>,<r> of the engaged hex.
    static string Source(GameState s, int q, int r) =>
        string.Format(CultureInfo.InvariantCulture, "combat/{0:yyyy-MM-dd}/{1},{2}", s.Date, q, r);

    static string LossLine(int id, string source) => PopulationText.Fields("loss", id, source);

    static List<string> LossLines(GameState s) =>
        s.PopulationLedger.Canonical().Where(l => l.StartsWith("4:loss|", StringComparison.Ordinal)).ToList();

    // The own opening formations, strongest first (lowest id on a tie).
    static List<Unit> Own(Content c, GameState s) => s.Units.Values
        .Where(u => u.Side == Movement.PlayerSide && u.Id < c.StartingForces.Count && u.Strength > 0)
        .OrderByDescending(u => u.Strength).ThenBy(u => u.Id).ToList();

    static Unit Enemy(GameState s, int nth = 0) => s.Units.Values.Where(u => Prisoners.FrontOf.ContainsKey(u.Side))
        .OrderBy(u => u.Id).Skip(nth).First();

    static List<(int Q, int R)> Ring(Content c, GameState s, Unit u) => Movement.Neighbours
        .Select(d => (Q: u.Q + d.Q, R: u.R + d.R)).Where(h => c.HexAt(h.Q, h.R) is MapHex hex && hex.Land).ToList();

    static void Place(GameState s, Unit u, int q, int r, int? strength = null, string order = "", string target = "") =>
        s.Units[u.Id] = new Unit(u.Id, u.Type, q, r, u.Service, u.Side, strength ?? u.Strength, order, target, u.HomeCity, u.Full, u.CombatXp);

    // Step 2 on its own: no attack order is left standing, then one enemy unit, set next to own unit
    // <paramref name="defender"/> on a free land hex, attacks it with <paramref name="strength"/>.
    static void Besiege(Content c, GameState s, int defender, int strength)
    {
        foreach (var u in s.Units.Values.ToList()) if (u.Order == Combat.Attack) Place(s, u, u.Q, u.R);
        var d = s.Units[defender];
        var free = Ring(c, s, d).First(h => !s.Units.Values.Any(x => x.Q == h.Q && x.R == h.R));
        Place(s, Enemy(s), free.Q, free.R, strength, Combat.Attack, FormattableString.Invariant($"{d.Q},{d.R}"));
    }

    static string Units(GameState s) => string.Join(";", s.Units.Values.Select(x => x.Canonical()));

    // No attack order is left standing; one enemy unit goes on each land hex around own unit <paramref name="unit"/> and every
    // other unit leaves that ring, so it has no hex to retreat to. The lowest-id one attacks it with
    // <paramref name="strength"/>, the rest have strength 1. Returns the attacker's side, the captor of a surrender.
    static string Surround(Content c, GameState g, int unit, int strength)
    {
        foreach (var u in g.Units.Values.ToList()) if (u.Order == Combat.Attack) Place(g, u, u.Q, u.R);
        var at = g.Units[unit];
        var ring = Ring(c, g, at);
        var enemies = g.Units.Values.Where(u => u.Side != Movement.PlayerSide && u.Side != "").OrderBy(u => u.Id).Take(ring.Count).ToList();
        Assert.Equal(ring.Count, enemies.Count);
        foreach (var x in g.Units.Values.ToList())
            if (x.Id != unit && !enemies.Any(e => e.Id == x.Id) && ring.Contains((x.Q, x.R))) g.Units.Remove(x.Id);
        for (int i = 0; i < ring.Count; i++)
            Place(g, enemies[i], ring[i].Q, ring[i].R, i == 0 ? strength : 1, i == 0 ? Combat.Attack : "", i == 0 ? FormattableString.Invariant($"{at.Q},{at.R}") : "");
        return enemies[0].Side;
    }

    // The first seed for which the aggregate twin, set up by <paramref name="setUp"/> and fought by
    // <paramref name="fight"/>, passes <paramref name="wanted"/>.
    static ulong Seed(Content c, Action<GameState> setUp, Action<GameState, List<Cause>> fight, Func<GameState, GameState, List<Cause>, bool> wanted)
    {
        for (ulong seed = 1; seed < 2000; seed++)
        {
            var g = Aggregate(c); setUp(g); var before = g.Clone(); g.RngState = seed; before.RngState = seed;
            var why = new List<Cause>();
            fight(g, why);
            if (wanted(before, g, why)) return seed;
        }
        throw new Xunit.Sdk.XunitException("no seed gave the wanted engagement");
    }

    static int Lost(GameState before, GameState after, int unit) =>
        before.Units[unit].Strength - (after.Units.TryGetValue(unit, out var u) ? u.Strength : 0);

    // R-COMBAT-002, R-COMBAT-003, R-POP-001: an enemy attack in step 2 on an own opening formation. Each population unit it
    // loses removes one of its serving records as a combat loss, the lowest id first ("Lowest id first (Recommended)"), with
    // the provenance combat/<date>/<q>,<r> of the engaged hex ("Date + battle hex (Recommended)"); every loss is a death and
    // nothing else is written ("No, losses are deaths (Recommended)"). The units, the draw and the Why lines are the aggregate
    // twin's, and every other record is unchanged, the survivors' combat XP included ("No, formation only (Recommended)").
    [Theory, InlineData("en-US"), InlineData("he-IL")]
    public void AnEnemyAttackRemovesTheLowestIdServingRecordsAsCombatLosses(string culture)
    {
        Gdd03CombatCatalogueTests.InCulture(culture, () =>
        {
            var c = Gdd03();
            var d = Own(c, Dark(c))[0];
            Assert.True(d.Strength >= 2);
            void SetUp(GameState g) => Besiege(c, g, d.Id, 3 * d.Strength);
            var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why),
                (b, g, _) => Lost(b, g, d.Id) >= 2 && g.Units.ContainsKey(d.Id));
            var s = Dark(c); SetUp(s); s.RngState = seed;
            var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
            var serving = Serving(c, s, d.Id);
            Assert.Equal(d.Strength, serving.Count);
            var count = s.PopulationLedger.Units.Count;
            var why = new List<Cause>(); var twinWhy = new List<Cause>();

            Combat.Resolve(c, s, why);
            Combat.Resolve(c, twin, twinWhy);

            Assert.Equal(Units(twin), Units(s));
            Assert.Equal(twin.RngState, s.RngState);
            Assert.Equal(twinWhy.Select(x => x.Message), why.Select(x => x.Message));
            var lost = d.Strength - s.Units[d.Id].Strength;
            Assert.True(lost >= 2);
            var gone = serving.Take(lost).Select(p => p.Id).ToList();
            Assert.Equal(serving.Skip(lost).Select(p => p.Id), Serving(c, s, d.Id).Select(p => p.Id));
            Assert.Equal(s.Units[d.Id].Strength, Serving(c, s, d.Id).Count);
            Assert.Equal(gone.Select(id => LossLine(id, Source(s, d.Q, d.R))), LossLines(s));
            Assert.Equal(count - lost, s.PopulationLedger.Units.Count);
            Assert.All(gone, id => Assert.DoesNotContain(s.PopulationLedger.Units, p => p.Id == id));
            var kept = Dark(c).PopulationLedger.Units.Where(p => !gone.Contains(p.Id)).Select(p => p.Canonical());
            Assert.Equal(kept, s.PopulationLedger.Units.Select(p => p.Canonical()));
            s.PopulationLedger.Reconcile();
        });
    }

    // R-COMBAT-003, "No, keep region (Recommended)": a defender that loses and retreats moves on the map only; its remaining
    // records keep their formation's region-only location.
    [Fact]
    public void ARetreatMovesTheUnitAndNoRecord()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        void SetUp(GameState g) => Besiege(c, g, d.Id, 3 * d.Strength);
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why),
            (b, g, _) => g.Units.TryGetValue(d.Id, out var u) && (u.Q, u.R) != (d.Q, d.R) && Lost(b, g, d.Id) >= 1);
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var before = Serving(c, s, d.Id);
        Combat.Resolve(c, s, new List<Cause>());
        var u = s.Units[d.Id];
        Assert.NotEqual((d.Q, d.R), (u.Q, u.R));
        var after = Serving(c, s, d.Id);
        Assert.Equal(u.Strength, after.Count);
        Assert.Equal(before.Skip(before.Count - after.Count).Select(p => p.Canonical()), after.Select(p => p.Canonical()));
        s.PopulationLedger.Reconcile();
    }

    // R-COMBAT-001, R-COMBAT-005: the player's instant attack (Combat.Strike) fights through the same engagement: the own
    // attacker's losses remove its lowest-id serving records with the attacked hex in the provenance; the enemy defenders'
    // losses touch no record (R-POP-004: enemy formations are not own population), and the units match the aggregate twin.
    [Fact]
    public void ThePlayersStrikeLosesTheAttackersRecordsAndEnemyLossesTouchNone()
    {
        var c = Gdd03();
        var a = Own(c, Dark(c))[0];
        (int Q, int R) hex = default;
        void SetUp(GameState g)
        {
            foreach (var u in g.Units.Values.ToList()) if (u.Order == Combat.Attack) Place(g, u, u.Q, u.R);
            var at = g.Units[a.Id];
            hex = Ring(c, g, at).First(h => !g.Units.Values.Any(x => x.Q == h.Q && x.R == h.R));
            Place(g, Enemy(g), hex.Q, hex.R, 2 * a.Strength);
        }
        string? Strike(GameState g, List<Cause> why) => Combat.Strike(c, g, a.Id, hex.Q, hex.R, new Dictionary<int, int>(),
            new Dictionary<int, AttackRecord>(), why);
        var seed = Seed(c, SetUp, (g, why) => Assert.Null(Strike(g, why)), (b, g, _) => Lost(b, g, a.Id) >= 1);
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
        var serving = Serving(c, s, a.Id);
        Assert.Null(Strike(s, new List<Cause>()));
        Assert.Null(Strike(twin, new List<Cause>()));
        Assert.Equal(Units(twin), Units(s));
        var lost = a.Strength - (s.Units.TryGetValue(a.Id, out var after) ? after.Strength : 0);
        Assert.True(lost >= 1);
        Assert.Equal(serving.Take(lost).Select(p => LossLine(p.Id, Source(s, hex.Q, hex.R))), LossLines(s));
        s.PopulationLedger.Reconcile();
    }

    // "Yes, after #2964 (Recommended)", R-COMBAT-003: a formation trained during the game (#2964: its records serve
    // formation/<unit id>) is covered too. A strength-1 trained formation destroyed in step 2 leaves the map and its one
    // record leaves the ledger as a combat loss.
    [Fact]
    public void ADestroyedTrainedFormationLeavesNoServingRecord()
    {
        var c = Gdd03();
        int unit = -1;
        void Train(GameState g)
        {
            g.Arms = 200; g.Treasury = 100; g.Debt = 0;
            Assert.Null(Training.Start(c, g, "tel_aviv", "recruit", 1, new List<Cause>()));
            unit = g.NextId;
            for (int i = 0; i < c.UnitTypes.First(t => t.Id == "recruit").TrainingTurns; i++) Training.Progress(c, g, new List<Cause>());
        }
        var probe = Dark(c); Train(probe);
        Assert.True(unit >= c.StartingForces.Count);
        Assert.Equal(1, probe.Units[unit].Strength);
        // Alone on its hex (losses fall on the strongest unit there first), on a free land hex beside the last enemy unit.
        void SetUp(GameState g)
        {
            Train(g);
            var e = g.Units.Values.Where(u => Prisoners.FrontOf.ContainsKey(u.Side)).OrderBy(u => u.Id).Last();
            var free = Ring(c, g, e).First(h => !g.Units.Values.Any(x => x.Q == h.Q && x.R == h.R));
            Place(g, g.Units[unit], free.Q, free.R);
            Besiege(c, g, unit, 6);
        }
        var s = Dark(c); SetUp(s);
        // The draw is found on the dark game itself here: the aggregate start trains no records, so its twin is not one.
        ulong seed = 1;
        for (; seed < 2000; seed++)
        {
            var t = s.Clone(); t.RngState = seed;
            Combat.Resolve(c, t, new List<Cause>());
            if (!t.Units.ContainsKey(unit)) break;
        }
        Assert.True(seed < 2000, "no seed destroyed the formation");
        s.RngState = seed;
        var u = s.Units[unit];
        var record = Assert.Single(Serving(c, s, unit));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.False(s.Units.ContainsKey(unit));
        Assert.Contains(why, x => x.Key == WhyKeys.Destroyed);
        Assert.Empty(Serving(c, s, unit));
        Assert.Equal(new[] { LossLine(record.Id, Source(s, u.Q, u.R)) }, LossLines(s));
        s.PopulationLedger.Reconcile();
    }

    // "Go ahead on strength": a formation whose strength differs from its serving records still fights on its strength,
    // with the aggregate twin's units and draw, and loses records only while any remain: here one record is left for a
    // loss of at least two.
    [Fact]
    public void AMismatchedFormationFightsOnItsStrengthAndLosesOnlyTheRecordsLeft()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        void SetUp(GameState g) => Besiege(c, g, d.Id, 3 * d.Strength);
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why), (b, g, _) => Lost(b, g, d.Id) >= 2);
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
        var serving = Serving(c, s, d.Id);
        foreach (var p in serving.Skip(1)) s.PopulationLedger.RegisterCombatLoss(p.Id, "fixture");
        var last = Assert.Single(Serving(c, s, d.Id));
        var prior = LossLines(s);

        Combat.Resolve(c, s, new List<Cause>());
        Combat.Resolve(c, twin, new List<Cause>());

        Assert.Equal(Units(twin), Units(s));
        Assert.Equal(twin.RngState, s.RngState);
        Assert.Empty(Serving(c, s, d.Id));
        Assert.Equal(prior.Append(LossLine(last.Id, Source(s, d.Q, d.R))).OrderBy(x => x, StringComparer.Ordinal),
            LossLines(s).OrderBy(x => x, StringComparer.Ordinal));
        s.PopulationLedger.Reconcile();
    }

    // "Prisoners #1710 first (Recommended)", R-COMBAT-003, R-POW-005: an own formation with nowhere to retreat surrenders on a
    // ledger game. Its losses are written first, as combat losses on its lowest ids, so Prisoners.Take finds its records
    // numbering its strength; the rest become Captured under the attacker's side, and SoldiersHeld matches them.
    [Fact]
    public void AnOwnSurrenderLosesItsLowestRecordsThenTheRestAreCaptured()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        string side = "";
        void SetUp(GameState g) => side = Surround(c, g, d.Id, 3 * d.Strength);
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why),
            (b, g, why) => Lost(b, g, d.Id) >= 1 && why.Any(x => x.Key == WhyKeys.SurrenderedHeld));
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
        var serving = Serving(c, s, d.Id);

        Combat.Resolve(c, s, new List<Cause>());
        Combat.Resolve(c, twin, new List<Cause>());

        Assert.False(s.Units.ContainsKey(d.Id));
        Assert.Equal(Units(twin), Units(s));
        var held = twin.SoldiersHeld[side];
        var lost = d.Strength - held;
        Assert.True(lost >= 1);
        Assert.Equal(held, s.SoldiersHeld[side]);
        Assert.Equal(serving.Take(lost).Select(p => LossLine(p.Id, Source(s, d.Q, d.R))), LossLines(s));
        foreach (var p in serving.Skip(lost))
            Assert.Equal((PopulationStatus.Captured, side), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Prisoners.RequireHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // "Go ahead on strength (Recommended)" (owner, 9 November 2026), R-POW-005: an own formation with fewer serving records
    // than its strength surrenders in step 2 with no refusal. The units and the draw are the aggregate twin's; every record
    // left after its losses is Captured, SoldiersHeld grows by those records (one fewer than the twin's count, for the one
    // record taken out first), and the count still matches the records, so the next Upkeep goes on. "Records captured
    // (Recommended)" (owner, 9 November 2026): the surrender Why line states the records captured, in people (captured x
    // people_per_unit), not the strength; the aggregate twin's line still states its strength in people.
    [Fact]
    public void AFormationWithFewerRecordsThanStrengthSurrendersAndAllItsRecordsAreCaptured()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        string side = "";
        void SetUp(GameState g) => side = Surround(c, g, d.Id, 3 * d.Strength);
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why),
            (b, g, why) => why.Any(x => x.Key == WhyKeys.SurrenderedHeld) && g.SoldiersHeld.Values.Sum() >= 2);
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
        s.PopulationLedger.RegisterCombatLoss(Serving(c, s, d.Id).Last().Id, "fixture");
        var serving = Serving(c, s, d.Id);
        Assert.Equal(d.Strength - 1, serving.Count);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Combat.Resolve(c, s, why);
        Combat.Resolve(c, twin, twinWhy);

        Assert.False(s.Units.ContainsKey(d.Id));
        Assert.Equal(Units(twin), Units(s));
        Assert.Equal(twin.RngState, s.RngState);
        var ppu = c.RulesAt(s.Date).Mvp!.PeoplePerUnit;
        var captured = s.SoldiersHeld[side];
        var line = Assert.Single(why, x => x.Key == WhyKeys.SurrenderedHeld);
        Assert.Equal(new[] { (captured * ppu).ToString(CultureInfo.InvariantCulture) }, line.Args);
        Assert.Equal(FormattableString.Invariant($"Unit {d.Id} surrendered with {captured * ppu:N0} people: it had no hex to retreat to. They are held by the enemy"), line.Message);
        var twinLine = Assert.Single(twinWhy, x => x.Key == WhyKeys.SurrenderedHeld);   // the aggregate game: its strength, unchanged
        Assert.Equal(new[] { (twin.SoldiersHeld[side] * ppu).ToString(CultureInfo.InvariantCulture) }, twinLine.Args);
        Assert.Equal(FormattableString.Invariant($"Unit {d.Id} surrendered with {twin.SoldiersHeld[side] * ppu:N0} people: it had no hex to retreat to. They are held by the enemy"), twinLine.Message);
        Assert.DoesNotContain(why, x => x.Key == CauseKeys.SentHomeArmsReturned);   // no leftover record, so none goes home
        var lost = d.Strength - twin.SoldiersHeld[side];
        Assert.Empty(Serving(c, s, d.Id));
        foreach (var p in serving.Skip(lost))
            Assert.Equal((PopulationStatus.Captured, side), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Assert.Equal(serving.Count - lost, s.SoldiersHeld[side]);
        Assert.Equal(twin.SoldiersHeld[side] - 1, s.SoldiersHeld[side]);
        Prisoners.RequireHeldMatchesRecords(s);
        Prisoners.Upkeep(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
        s.PopulationLedger.Reconcile();
    }

    // "Go ahead on strength (Recommended)": an own formation with more serving records than its strength surrenders in step
    // 2. Its losses take its lowest records first; of the rest exactly its strength are Captured, the lowest ids first.
    // SoldiersHeld is the aggregate twin's. "Sent home (Recommended)" (owner, 9 November 2026): the leftover records leave
    // service as Send home's do (R-UNIT-005): settled-in free civilians without an assignment in the nearest city to the
    // formation's hex, the issued items back in the Arms stock, with Send home's one Arms Why line after the surrender line.
    // The aggregate twin writes no such line and its stock is unchanged.
    [Fact]
    public void AFormationWithMoreRecordsThanStrengthSurrendersItsLowestRecordsAndSendsTheRestHome()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        string side = "";
        void SetUp(GameState g)
        {
            side = Surround(c, g, d.Id, 3 * d.Strength);
            var u = g.Units[d.Id]; Place(g, u, u.Q, u.R, u.Strength - 2);
        }
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why),
            (b, g, why) => why.Any(x => x.Key == WhyKeys.SurrenderedHeld) && g.SoldiersHeld.Values.Sum() >= 1);
        var s = Dark(c); SetUp(s); s.RngState = seed;
        var twin = Aggregate(c); SetUp(twin); twin.RngState = seed;
        var serving = Serving(c, s, d.Id);
        Assert.Equal(s.Units[d.Id].Strength + 2, serving.Count);
        var at = s.Units[d.Id];
        var city = Demobilization.Nearest(c, at.Q, at.R);
        var region = c.PlayerRegions.AtLocation(city)!.Id;
        var arms = s.Arms; var twinArms = twin.Arms;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Combat.Resolve(c, s, why);
        Combat.Resolve(c, twin, twinWhy);

        Assert.Equal(Units(twin), Units(s));
        var held = twin.SoldiersHeld[side];
        var lost = d.Strength - 2 - held;
        Assert.Equal(held, s.SoldiersHeld[side]);
        Assert.Equal(serving.Take(lost).Select(p => LossLine(p.Id, Source(s, d.Q, d.R))), LossLines(s));
        foreach (var p in serving.Skip(lost).Take(held))
            Assert.Equal((PopulationStatus.Captured, side), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Assert.Empty(Serving(c, s, d.Id));
        var leftover = serving.Skip(lost + held).ToList();
        Assert.Equal(new[] { serving[serving.Count - 2].Id, serving[serving.Count - 1].Id }, leftover.Select(p => p.Id));
        var received = c.RequirePopulationInteger("reception.turns");
        foreach (var was in leftover)
        {
            var p = s.PopulationLedger.Get(was.Id);
            Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned), (p.Status, p.Assignment.Kind));
            Assert.Equal((region, city), (p.Location!.Region, p.Location.City));
            Assert.Equal(received, p.ReceptionTicks);
            Assert.Null(p.Equipment.MilitaryId);
            Assert.Equal((was.Qualification, was.CombatXp), (p.Qualification, p.CombatXp));
        }
        var items = leftover.Count(p => p.Equipment.MilitaryId != null);
        Assert.Equal(2, items);
        Assert.Equal(arms + items, s.Arms);
        Assert.Equal(twinArms, twin.Arms);
        var ppu = c.RulesAt(s.Date).Mvp!.PeoplePerUnit;
        var surrender = Assert.Single(why, x => x.Key == WhyKeys.SurrenderedHeld);
        Assert.Equal(new[] { (held * ppu).ToString(CultureInfo.InvariantCulture) }, surrender.Args);
        var back = Assert.Single(why, x => x.Key == CauseKeys.SentHomeArmsReturned);
        Assert.Equal(why.IndexOf(surrender) + 1, why.IndexOf(back));
        Assert.Equal(("city", "arms", (double)items, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: unit {d.Id} returned {items} Arms")),
            (back.System, back.Target, back.Delta, back.Message));
        Assert.Equal(new[] { city, d.Id.ToString(CultureInfo.InvariantCulture) }, back.Args);
        Assert.DoesNotContain(twinWhy, x => x.Key == CauseKeys.SentHomeArmsReturned);
        Prisoners.RequireHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // "Yes, in #1706 (Recommended)" (owner, 9 November 2026): a trained formation (#2964's formation/<unit id> target)
    // surrenders in step 2 and its records left after its losses become Captured under the attacker's side, found as its
    // losses are (Training.FormationTarget); SoldiersHeld matches them.
    [Fact]
    public void ATrainedFormationSurrendersAndItsRecordsAreCaptured()
    {
        var c = Gdd03();
        int unit = -1;
        string side = "";
        void SetUp(GameState g)
        {
            g.Arms = 200; g.Treasury = 100; g.Debt = 0;
            // Three groups: every face that makes the defender retreat also costs it at least one unit (combat_table).
            Assert.Null(Training.Start(c, g, "tel_aviv", "recruit", 3, new List<Cause>()));
            unit = g.NextId;
            for (int i = 0; i < c.UnitTypes.First(t => t.Id == "recruit").TrainingTurns; i++) Training.Progress(c, g, new List<Cause>());
            // Alone on the strongest opening formation's hex (whose ring the enemy units can fill: the other units there
            // change places with it), then surrounded.
            var trained = g.Units[unit]; var at = g.Units[Own(c, g)[0].Id];
            foreach (var x in g.Units.Values.Where(x => x.Q == at.Q && x.R == at.R).ToList()) Place(g, x, trained.Q, trained.R);
            Place(g, trained, at.Q, at.R);
            side = Surround(c, g, unit, 9);
        }
        var s = Dark(c); SetUp(s);
        Assert.True(unit >= c.StartingForces.Count);
        // The draw is found on the dark game itself: the aggregate start trains no records, so its twin is not one.
        ulong seed = 1;
        for (; seed < 2000; seed++)
        {
            var t = s.Clone(); t.RngState = seed; var w = new List<Cause>();
            Combat.Resolve(c, t, w);
            if (w.Any(x => x.Key == WhyKeys.SurrenderedHeld && x.Units.Any(u => u.Id == unit))) break;
        }
        Assert.True(seed < 2000, "no seed made the trained formation surrender");
        s.RngState = seed;
        var serving = Serving(c, s, unit);
        Assert.Equal(s.Units[unit].Strength, serving.Count);
        Assert.All(serving, p => Assert.Equal(Training.FormationPrefix + unit.ToString(CultureInfo.InvariantCulture), p.Assignment.Target));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.False(s.Units.ContainsKey(unit));
        Assert.Empty(Serving(c, s, unit));
        var held = s.SoldiersHeld[side];
        Assert.True(held >= 1);
        foreach (var p in serving.Skip(serving.Count - held))
            Assert.Equal((PopulationStatus.Captured, side), (s.PopulationLedger.Get(p.Id).Status, s.PopulationLedger.Get(p.Id).Captor));
        Prisoners.RequireHeldMatchesRecords(s);
        s.PopulationLedger.Reconcile();
    }

    // Q-UI-1, #2553: Combat.Outcomes runs on a ledger game and gives the aggregate twin's bands for every row, for a unit and
    // hex too, so the card's bands are what Fight takes (the formation's strength).
    [Fact]
    public void OutcomesOnALedgerGameAreTheAggregateGames()
    {
        var c = Gdd03();
        var a = Own(c, Dark(c))[0];
        var s = Dark(c); var twin = Aggregate(c);
        var e = Enemy(s);
        var hex = Ring(c, s, s.Units[a.Id]).First(h => !s.Units.Values.Any(x => x.Q == h.Q && x.R == h.R));
        Place(s, e, hex.Q, hex.R); Place(twin, twin.Units[e.Id], hex.Q, hex.R);
        var before = s.Hash();
        for (int row = 0; row < c.RulesAt(s.Date).Mvp!.CombatTable.Count; row++)
        {
            Assert.Equal(Combat.Outcomes(c, twin, row), Combat.Outcomes(c, s, row));
            Assert.Equal(Combat.Outcomes(c, twin, row, a.Id, hex.Q, hex.R), Combat.Outcomes(c, s, row, a.Id, hex.Q, hex.R));
        }
        Assert.Equal(before, s.Hash());
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through; an Open or synthetic one is still refused by
    // Resolve and Outcomes before any write.
    [Theory]
    [InlineData(true, false)] [InlineData(false, false)] [InlineData(true, true)] [InlineData(false, true)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open, bool outcomes)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        Action call = outcomes ? () => Combat.Outcomes(c, s, 0) : () => Combat.Resolve(c, s, why);
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(call).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // Determinism: the ledger losses draw nothing; two dark games fought from the same seed end with the same hash and
    // ledger text, and the losses change the hash.
    [Fact]
    public void TheSameEngagementGivesTheSameHashAndLedger()
    {
        var c = Gdd03();
        var d = Own(c, Dark(c))[0];
        void SetUp(GameState g) => Besiege(c, g, d.Id, 3 * d.Strength);
        var seed = Seed(c, SetUp, (g, why) => Combat.Resolve(c, g, why), (b, g, _) => Lost(b, g, d.Id) >= 1);
        string Run(out string start)
        {
            var s = Dark(c); SetUp(s); s.RngState = seed; start = s.Hash();
            Combat.Resolve(c, s, new List<Cause>());
            return s.Hash() + "\n" + string.Join("\n", s.PopulationLedger.Canonical());
        }
        var a = Run(out var startA); var b = Run(out var startB);
        Assert.Equal(startA, startB);
        Assert.Equal(a, b);
        Assert.NotEqual(startA, a.Split('\n')[0]);
    }
}
