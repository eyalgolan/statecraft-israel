using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1510: military replacements (R-TRAIN-004). A damaged Israeli formation on a city's settlement hex takes trained
/// replacement groups from that city's civilians without work; each group pays its course's Money and Arms (owner, 23 October
/// 2026: "Replacements cost Arms"), and the finished cohort adds to the unit's strength up to the strength it started with.
/// Which formations, veterans and a lost target follow the owner's "Default (Recommended)" ruling of the same day.</summary>
public class ReplacementTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>Tel Aviv has the start's military camp (cities.yaml, D-D5).</summary>
    const string City = "tel_aviv";

    static SettlementHex Hex(string city) => Mvp.SettlementHexes.Single(h => h.Id == city);

    /// <summary>A new game with a player formation of <paramref name="type"/> on Tel Aviv's hex, at
    /// <paramref name="strength"/> of the <paramref name="full"/> it started with (0: never damaged), and Arms to spare.</summary>
    static (GameState s, int id) Game(string type = "recruit", int strength = 2, int full = 5, int service = 0)
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var h = Hex(City);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, type, h.Q, h.R, service, side: "israel", strength: strength, homeCity: City, full: full));
        s.Arms = 100;
        return (s, id);
    }

    static string? Reinforce(GameState s, int unit, int n, string city = City) => Training.Reinforce(Mvp, s, unit, city, n, new List<Cause>());

    static void Turns(GameState s, int n) { for (int i = 0; i < n; i++) Training.Progress(Mvp, s, new List<Cause>()); }

    static int Turns(string track) => Mvp.UnitTypes.Single(t => t.Id == track).TrainingTurns;

    // R-TRAIN-004: the finished cohort adds its count to the unit, makes no new unit, and the unit is at full strength again.
    [Fact]
    public void ARefillAddsStrengthToTheUnit()
    {
        var (s, id) = Game();
        var civilians = s.Cities[City].Civilians;
        Assert.Null(Reinforce(s, id, 3));
        Assert.Equal(civilians - 3, s.Cities[City].Civilians);
        var cohort = Assert.Single(s.Cohorts.Values);
        Assert.Equal((id, "recruit", 3), (cohort.Unit, cohort.Type, cohort.Count));
        var next = s.NextId;
        var units = s.Units.Count;

        Turns(s, Turns("recruit"));

        Assert.Empty(s.Cohorts);
        Assert.Equal(units, s.Units.Count);
        Assert.Equal(next, s.NextId);
        Assert.Equal((5, 0), (s.Units[id].Strength, s.Units[id].Full));
    }

    // R-TRAIN-004 with R-ARMS-003: a recruit group takes arms_per_recruit_group, and a short stock refuses it, state unchanged.
    [Fact]
    public void ARefillTakesArmsAndAShortStockRefusesIt()
    {
        var (s, id) = Game();
        var per = Mvp.RulesAt(s.Date).Mvp!.ArmsPerRecruitGroup!.Value;
        Assert.Null(Reinforce(s, id, 2));
        Assert.Equal(100 - 2 * per, s.Arms);
        Assert.Equal(2 * per, Assert.Single(s.Cohorts.Values).Arms);

        var (t, tid) = Game();
        t.Arms = 2 * per - 1;
        var before = t.Hash();
        Assert.Contains("not enough Arms", Reinforce(t, tid, 2));
        Assert.Equal(before, t.Hash());
    }

    // R-TRAIN-004: a partial refill keeps the unit's Full; in one turn a cancelled cohort and a finishing one both resolve.
    [Fact]
    public void APartialRefillKeepsFullBesideACancelledCohort()
    {
        var (s, id) = Game(strength: 2, full: 5);
        Assert.Null(Reinforce(s, id, 1));
        var h = Hex(City);
        var other = s.NextId++;
        s.Units.Add(other, new Unit(other, "recruit", h.Q, h.R, side: "israel", strength: 1, homeCity: City, full: 3));
        Assert.Null(Reinforce(s, other, 2));
        Turns(s, Turns("recruit") - 1);
        s.Units.Remove(other);
        var why = new List<Cause>();
        Training.Progress(Mvp, s, why);
        Assert.Empty(s.Cohorts);
        Assert.Equal((3, 5), (s.Units[id].Strength, s.Units[id].Full));
        Assert.Equal(100, s.Arms + Mvp.RulesAt(s.Date).Mvp!.ArmsPerRecruitGroup!.Value);   // the cancelled cohort's Arms came back
        Assert.Contains(why, x => x.Message.Contains($"joined unit {id}"));
    }

    // R-TRAIN-004 (owner, 23 October 2026, "Default"): a gun crew pays its own type's course and training_arms (R-UNIT-008).
    [Fact]
    public void ArtilleryReplacementsPayTheGunsCourse()
    {
        var (s, id) = Game("artillery");
        var guns = Mvp.UnitTypes.Single(t => t.Id == "artillery");
        Assert.Null(Reinforce(s, id, 1));
        var k = Assert.Single(s.Cohorts.Values);
        Assert.Equal(("artillery", guns.TrainingArms!.Value, guns.TrainingTurns), (k.Type, k.Arms, k.TurnsLeft));
        Assert.Equal(100 - guns.TrainingArms!.Value, s.Arms);
    }

    // R-TRAIN-004 (owner, 23 October 2026, "Default"): a veteran trains replacements on basic training and stays a veteran
    // with its service (R-UNIT-004).
    [Fact]
    public void AVeteranKeepsItsTypeAndServiceWhenRefilled()
    {
        var (s, id) = Game("veteran_soldier", strength: 3, full: 4, service: 6);
        Assert.Null(Reinforce(s, id, 1));
        Assert.Equal("recruit", Assert.Single(s.Cohorts.Values).Type);
        Turns(s, Turns("recruit"));
        var u = s.Units[id];
        Assert.Equal(("veteran_soldier", 6, 4), (u.Type, u.Service, u.Strength));
    }

    // R-TRAIN-004: the cap is the starting strength, counting the replacements already in training.
    [Fact]
    public void TheOrderIsRefusedPastTheStartingStrength()
    {
        var (s, id) = Game(strength: 2, full: 5);
        Assert.Equal($"Unit {id} can take 3 more groups", Reinforce(s, id, 4));
        Assert.Null(Reinforce(s, id, 2));
        Assert.Equal($"Unit {id} can take 1 more group", Reinforce(s, id, 2));
        Assert.Null(Reinforce(s, id, 1));
        Assert.Equal($"Unit {id} has no losses to replace", Reinforce(s, id, 1));

        var (t, tid) = Game(strength: 4, full: 0);   // never damaged
        Assert.Equal($"Unit {tid} has no losses to replace", Reinforce(t, tid, 1));
    }

    // R-TRAIN-004: only the player's trained military types, on the city's own hex, take replacements.
    [Fact]
    public void TheOrderIsRefusedForTheWrongUnitOrPlace()
    {
        var (s, id) = Game();
        var h = Hex(City);
        var halutz = s.NextId++;
        s.Units.Add(halutz, new Unit(halutz, "halutz", h.Q, h.R));
        var enemy = s.NextId++;
        s.Units.Add(enemy, new Unit(enemy, "egyptian_army", h.Q, h.R, side: "egypt", strength: 1, full: 3));
        var before = s.Hash();
        Assert.Equal("There is no such unit", Reinforce(s, 999, 1));
        Assert.Equal("Only a military unit takes that order", Reinforce(s, halutz, 1));
        Assert.Equal("That unit is not yours", Reinforce(s, enemy, 1));
        Assert.Equal("There is no such city", Reinforce(s, id, 1, "nowhere"));
        Assert.Equal($"Unit {id} is not in Haifa", Reinforce(s, id, 1, "haifa"));
        Assert.Equal(before, s.Hash());
    }

    // R-TRAIN-002 with R-TRAIN-004: cancelling the replacements returns the people and the Arms, the Money stays spent.
    [Fact]
    public void CancellingReplacementsRefundsPeopleAndArms()
    {
        var (s, id) = Game();
        var civilians = s.Cities[City].Civilians;
        Assert.Null(Reinforce(s, id, 2));
        var treasury = s.Treasury;
        Assert.Null(Training.Cancel(Mvp, s, s.Cohorts.Keys.Single(), new List<Cause>()));
        Assert.Equal((civilians, 100, treasury), (s.Cities[City].Civilians, s.Arms, s.Treasury));
        Assert.Equal(2, s.Units[id].Strength);
    }

    // R-TRAIN-004 (owner, 23 October 2026, "Default"): a target destroyed, captured or sent home cancels the cohort as
    // R-TRAIN-002 does.
    [Fact]
    public void ALostTargetCancelsTheCohort()
    {
        var (s, id) = Game();
        var civilians = s.Cities[City].Civilians;
        Assert.Null(Reinforce(s, id, 2));
        var treasury = s.Treasury;
        s.Units.Remove(id);
        var why = new List<Cause>();
        Training.Progress(Mvp, s, why);
        Assert.Empty(s.Cohorts);
        Assert.Equal((civilians, 100, treasury), (s.Cities[City].Civilians, s.Arms, s.Treasury));
        Assert.Contains(why, x => x.Message.Contains($"unit {id} is no longer in Tel Aviv"));
    }

    // R-TRAIN-004: a target that left the city's hex before the course ends cancels it too.
    [Fact]
    public void ATargetThatLeavesTheCityCancelsTheCohort()
    {
        var (s, id) = Game();
        Assert.Null(Reinforce(s, id, 2));
        Turns(s, 1);
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, u.Q + 1, u.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
        Turns(s, 1);
        Assert.Empty(s.Cohorts);
        Assert.Equal(100, s.Arms);
        Assert.Equal(2, s.Units[id].Strength);
    }

    // R-COMBAT-003 with R-TRAIN-004: the first loss stores the strength the unit had; a destroyed unit leaves with it.
    [Fact]
    public void TheFirstLossStoresTheStartingStrength()
    {
        var u = new Unit(1, "recruit", 0, 0, side: "israel", strength: 4);
        Assert.Equal(0, u.Full);
        Assert.DoesNotContain("|full=", u.Canonical());
        var hit = Combat.Lose(u);
        Assert.Equal((3, 4), (hit.Strength, hit.Full));
        Assert.EndsWith("|full=4", hit.Canonical());
        Assert.Equal((2, 4), (Combat.Lose(hit).Strength, Combat.Lose(hit).Full));
        Assert.Equal(0, Combat.Lose(new Unit(2, "egyptian_army", 0, 0, side: "egypt", strength: 4)).Full);   // no rule reads an enemy's
    }

    // R-TRAIN-004, R-LOOP-002: the cohort's target is hashed only when set, so other cohorts hash as before.
    [Fact]
    public void TheCohortsTargetIsHashedOnlyWhenSet()
    {
        Assert.Equal("3:recruit@tel_avivx2/3|arms=2", new Cohort(3, "recruit", "tel_aviv", 2, 3, 2).Canonical());
        Assert.Equal("3:recruit@tel_avivx2/3|arms=2|unit=0", new Cohort(3, "recruit", "tel_aviv", 2, 3, 2, 0).Canonical());
        Assert.Equal(0, new Cohort(3, "recruit", "tel_aviv", 2, 3, 2, 0).WithTurnsLeft(1).Unit);
    }

    // R-TRAIN-004: the order saves, replays and plays from a scenario.
    [Fact]
    public void TheOrderSavesAndReplays()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        // The save keeps the command log, a refused order included (unit 0 is a starting brigade away from Tel Aviv), and the
        // replay must reach the same hash; StepRunsTheOrder covers an accepted one.
        g.Step(new Command[] { new Reinforce(0, City, 1) });
        var text = g.Save.Write();
        Assert.Contains($"\nreinforce 0 {City} 1\n", text);
        Assert.Equal(g.State.Hash(), GameSession.Load(Mvp, text, out _).State.Hash());
        Assert.Equal("reinforce", new Reinforce(0, City, 1).Kind);
        var built = Scenario.Build(new Dictionary<string, object> { ["kind"] = "reinforce", ["unit"] = "3", ["city"] = City, ["count"] = "2" });
        var r = Assert.IsType<Reinforce>(built);
        Assert.Equal((3, City, 2), (r.Unit, r.City, r.Count));
    }

    // R-TRAIN-004: Step takes the order and charges its Attention (rules.yaml attention_cost.reinforce).
    [Fact]
    public void StepRunsTheOrder()
    {
        var (s, id) = Game();
        var r = Simulation.Step(Mvp, s, new Command[] { new Reinforce(id, City, 1) });
        Assert.Empty(r.Rejected);
        Assert.Equal(2, r.State.Units[id].Strength);   // training_turns 3 counts the turn of the order: two turns to go
        var k = Assert.Single(r.State.Cohorts.Values);
        Assert.Equal((id, 2), (k.Unit, k.TurnsLeft));
        Assert.Contains(r.Why, x => x.Message == "Tel Aviv: 1 group began basic training as replacements for unit " + id + ", cost IL 0.5m");
    }

    // R-TRAIN-004 with R-CITY-001: a queued Reinforce takes its city's civilians as a queued Train does.
    [Fact]
    public void AQueuedReinforceCountsAgainstTheCitysFreeCivilians()
    {
        Assert.Equal(5, Training.Ordered(City, new Command[] { new Train(City, "recruit", 2), new Reinforce(0, City, 3), new Reinforce(0, "haifa", 4) }));
    }
}
