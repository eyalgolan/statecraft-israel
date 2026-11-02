using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1703 (GDD 0.3, the MVP's per-city housing condition), gdd-0.3 only: the surcharge, the cohesion term and the
/// 5 pp overflow (R-HOUSE-002, R-HOUSE-005), housed without formations (R-HOUSE-003, 27M-18), and gdd-0.1 unchanged. The
/// real-content tests run the shipped content with only eras.yaml's ruleset switched to gdd-0.3 (Gdd03MoneyLedgerTests'
/// pattern); the synthetic ones put their own counts and condition on a real city id.</summary>
public class Gdd03HousingConditionTests
{
    static readonly Content Real03 = Load(true);
    static readonly Content Real01 = Load(false);

    static Content Load(bool gdd03)
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(root, rel);
            var text = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            return gdd03 && rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + System.IO.Path.GetFileName(p)));
    }

    const double Tolerance = 1e-9;

    /// <summary>A new game of <paramref name="c"/> with one city only (the first city's id), with no training groups and no
    /// units: <paramref name="civilians"/> civilians, <paramref name="refugees"/> refugees, <paramref name="capacity"/> housing
    /// and <paramref name="condition"/>. TEST-ONLY SYNTHETIC counts.</summary>
    static GameState OneCity(Content c, int civilians, int capacity, double condition, int refugees = 0)
    {
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var id = s.Cities.Keys.First();
        s.Cities.Clear(); s.Cohorts.Clear(); s.Units.Clear();
        s.Cities[id] = new City(id, capacity, 0, civilians, 0, refugees, condition: condition);
        return s;
    }

    static List<Cause> UpdateCondition(Content c, GameState s)
    {
        var why = new List<Cause>();
        Cities.UpdateCondition(c, s, c.RulesAt(s.Date).Mvp!, why);
        return why;
    }

    // R-HOUSE-002 (#1703 Q1 default: the issue's 5 pp overflow, gdd-0.3 only): housed 10 over a capacity of 8 loses
    // 5 pp x 2/10 = 0.01 of condition under gdd-0.3; gdd-0.1 keeps overflow_condition_loss 0.15, so 0.15 x 2/10 = 0.03.
    [Fact]
    public void Gdd03OverflowLosesFivePpTimesTheOverflowShare()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 8, condition: 0.60);
        UpdateCondition(Real03, s);
        Assert.Equal(0.59, s.Cities.Values.Single().Condition!.Value, 12);

        var legacy = OneCity(Real01, civilians: 10, capacity: 8, condition: 0.60);
        UpdateCondition(Real01, legacy);
        Assert.Equal(0.57, legacy.Cities.Values.Single().Condition!.Value, 12);
    }

    // R-HOUSE-002: zero housed has no overflow share, so it loses nothing.
    [Fact]
    public void Gdd03ZeroHousedLosesNothing()
    {
        var s = OneCity(Real03, civilians: 0, capacity: 0, condition: 0.60, refugees: 3);
        UpdateCondition(Real03, s);
        Assert.Equal(0.60, s.Cities.Values.Single().Condition!.Value);
    }

    // R-HOUSE-005 (owner, 2 November 2026, "Scale it to unit size (Recommended)": 0.1 Money per unit): the surcharge is
    // housed x max(0, 0.50 - condition) x 0.1 a turn, one Money line (Target treasury) and borrowed when the Treasury is short.
    // 10 housed at 0.35: 10 x 0.15 x 0.1 = 0.15.
    [Fact]
    public void Gdd03SurchargeIsATenthOfMoneyPerUnitBelowHalfCondition()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.35);
        s.Treasury = 0; s.Debt = 0;
        var why = UpdateCondition(Real03, s);
        var line = Assert.Single(why, w => w.Key == CauseKeys.HousingSurcharge);
        Assert.Equal("treasury", line.Target);
        Assert.Equal(-0.15, line.Delta, 12);
        Assert.True(EconomySystem.MovesMoney(line));
        var borrowed = Assert.Single(why, w => w.Key == CauseKeys.Borrowed);
        Assert.Equal(CauseKeys.Purpose.HousingSurcharge, borrowed.Args[0]);
        Assert.Equal(0.15, s.Debt, 12);
        Assert.Equal(0.0, s.Treasury);
    }

    // R-HOUSE-005, #1703 ("Price the surcharge from pre-deterioration condition, then deteriorate once"): 10 housed in no
    // housing at 0.49 lose 5 pp (to 0.44); the surcharge is priced at 0.49: 10 x 0.01 x 0.1 = 0.01, not 10 x 0.06 x 0.1.
    [Fact]
    public void Gdd03SurchargeIsPricedBeforeTheTurnsDeterioration()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 0, condition: 0.49);
        s.Treasury = 100;
        var why = UpdateCondition(Real03, s);
        Assert.Equal(0.44, s.Cities.Values.Single().Condition!.Value, 12);
        Assert.Equal(-0.01, Assert.Single(why, w => w.Key == CauseKeys.HousingSurcharge).Delta, 12);
        Assert.Equal(100 - 0.01, s.Treasury, 12);
    }

    // R-HOUSE-005: at or above 0.50 there is no surcharge and no line.
    [Fact]
    public void Gdd03NoSurchargeAtHalfConditionOrAbove()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.50);
        var t = s.Treasury;
        Assert.DoesNotContain(UpdateCondition(Real03, s), w => w.Key == CauseKeys.HousingSurcharge);
        Assert.Equal(t, s.Treasury);
    }

    // R-HOUSE-005 (#1703 Q1 default: the issue's cohesion term): 2 x sum((R/P) x max(0, (0.25 - c) / 0.25)) points, from the
    // condition after the turn's deterioration, applied once. One city with all the population at 0.125: 2 x 1 x 0.5 = 1
    // point, so Social cohesion (morale) falls by 0.01.
    [Fact]
    public void Gdd03CohesionLosesTwoPointsAtMostWeightedByResidents()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.125);
        var before = s.Indicators["morale"];
        var why = UpdateCondition(Real03, s);
        var line = Assert.Single(why, w => w.Key == CauseKeys.HousingCohesion);
        Assert.Equal("morale", line.Target);
        Assert.Equal(-0.01, line.Delta, 12);
        Assert.Equal(before - 0.01, s.Indicators["morale"], 12);
    }

    // R-HOUSE-005: P is all own population, so refugees (not housed, 27M-18) weigh the city's residents down: 10 housed of
    // 20 at 0.125 is 2 x 0.5 x 0.5 = 0.5 points. At or above 0.25 there is no loss and no line.
    [Fact]
    public void Gdd03CohesionWeighsResidentsOverAllOwnPopulation()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.125, refugees: 10);
        Assert.Equal(-0.005, Assert.Single(UpdateCondition(Real03, s), w => w.Key == CauseKeys.HousingCohesion).Delta, 12);
        var fine = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.25);
        Assert.DoesNotContain(UpdateCondition(Real03, fine), w => w.Key == CauseKeys.HousingCohesion);
    }

    // R-HOUSE-005, #1703 Done-when "the legacy homes link is off (#1715)": under gdd-0.3 homes_lift_morale adds nothing to
    // Social cohesion, so the cohesion term is the only housing penalty; gdd-0.1 keeps the link.
    [Fact]
    public void Gdd03TurnsTheLegacyHomesLinkOff()
    {
        var s = Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < 3; t++) s = Simulation.Step(Real03, s, Array.Empty<Command>()).State;
        Assert.Equal(0.0, s.EffectContributions["homes_lift_morale"]);

        var legacy = Simulation.NewGame(Real01, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < 3; t++) legacy = Simulation.Step(Real01, legacy, Array.Empty<Command>()).State;
        Assert.NotEqual(0.0, legacy.EffectContributions["homes_lift_morale"]);
    }

    // R-HOUSE-003 (27M-18, owner-approved 1 November 2026: "Housed = civilians, craftsmen, recruits and groups in training.
    // Refugees and pioneer units are not housed."): under gdd-0.3 a city's formations are not housed; gdd-0.1 still counts
    // them (Q-HOUSE-5).
    [Fact]
    public void Gdd03FormationsAreNotHousedButGdd01StillCountsThem()
    {
        foreach (var (c, counted) in new[] { (Real03, false), (Real01, true) })
        {
            var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
            var x = s.Cities.Values.First();
            var type = c.UnitTypes.First(u => u.Side == "israel" && u.Id != "halutz").Id;
            var home = new Unit(9999, type, 0, 0, side: "israel", strength: 3, homeCity: x.Id);
            s.Units[home.Id] = home;
            var people = x.Civilians + x.Craftsmen + x.Recruits + s.Cohorts.Values.Where(k => k.City == x.Id).Sum(k => k.Count);
            Assert.True(Cities.Soldiers(s, x) >= 3, c.Ruleset);
            Assert.Equal(counted ? people + Cities.Soldiers(s, x) : people, Cities.Housed(s, x));
        }
    }

    // R-HOUSE-005 (owner, 2 November 2026, "Scale it to unit size (Recommended)"; the option read: "0.1 per unit (as #2112
    // did for Industry), about 7.6 Money a turn at start."): the opening turn of the real gdd-0.3 game charges every city's
    // housed x (0.50 - 0.35) x 0.1, about 7.6 Money.
    [Fact]
    public void Gdd03OpeningTurnSurchargeIsAboutSevenPointSixMoney()
    {
        var s = Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical);
        Assert.All(s.Cities.Values, x => Assert.Equal(0.35, x.Condition!.Value));
        var r = Simulation.Step(Real03, s, Array.Empty<Command>());
        // Nothing after step 8 changes who is housed under gdd-0.3 (step 9's arrivals are refugees, and formations do not count).
        var housed = r.State.Cities.Values.Sum(x => Cities.Housed(r.State, x));
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.HousingSurcharge);
        Assert.Equal(-housed * 0.15 * 0.1, line.Delta, 9);
        // The shipped census start: 511 housed units x 0.15 x 0.1 = 7.665 Money (the ruling's "about 7.6").
        Assert.Equal(511, housed);
        Assert.Equal(-7.665, line.Delta, 9);
    }

    // gdd-0.1 unchanged: a whole turn has no surcharge and no cohesion line.
    [Fact]
    public void Gdd01HasNoSurchargeAndNoCohesionLine()
    {
        var s = Simulation.NewGame(Real01, GoldenRun.Seed, Mode.Historical);
        var r = Simulation.Step(Real01, s, Array.Empty<Command>());
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.HousingSurcharge || w.Key == CauseKeys.HousingCohesion);
        var low = OneCity(Real01, civilians: 10, capacity: 10, condition: 0.10);
        var t = low.Treasury; var m = low.Indicators["morale"];
        Assert.Empty(UpdateCondition(Real01, low));
        Assert.Equal((t, m), (low.Treasury, low.Indicators["morale"]));
    }
}
