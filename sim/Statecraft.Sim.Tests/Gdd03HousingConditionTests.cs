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

    // R-HOUSE-002 (#1703 Q1, owner, 2 November 2026: "Yes, 5 pp under GDD 0.3 (Recommended)"): housed 10 over a
    // capacity of 8 loses 5 pp x 2/10 = 0.01 of condition under gdd-0.3; gdd-0.1 keeps overflow_condition_loss 0.15, so 0.15 x 2/10 = 0.03.
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

    // R-HOUSE-005 (#1703, the cohesion term; owner, 2 November 2026: "Keep the 2-point loss (Recommended)"):
    // 2 x sum((R/P) x max(0, (0.25 - c) / 0.25)) points, from the condition after the turn's deterioration, applied once.
    // One city with all the population at 0.125: 2 x 1 x 0.5 = 1 point, so Social cohesion (morale) falls by 0.01.
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

    // R-HOUSE-005 (#2804 item 10): P also counts the strength of every player unit, an Israeli formation and a side-less
    // halutz unit alike, wherever it stands; neither is housed under gdd-0.3. 10 housed at 0.125 with a formation of 4 and a
    // halutz unit of 6 is P = 20, so 2 x 0.5 x 0.5 = 0.5 points. Leaving out the formation (P = 16) or the halutz unit
    // (P = 14) gives a different loss. TEST-ONLY SYNTHETIC strengths.
    [Fact]
    public void Gdd03CohesionCountsIsraeliFormationsAndHalutzUnitsInP()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.125);
        var formation = Real03.UnitTypes.First(u => u.Side == "israel" && u.Id != "halutz").Id;
        s.Units[1] = new Unit(1, formation, 0, 0, side: "israel", strength: 4);
        s.Units[2] = new Unit(2, "halutz", 0, 0, strength: 6);
        Assert.Equal(20, Cities.OwnPopulation(s));
        Assert.Equal(-0.005, Assert.Single(UpdateCondition(Real03, s), w => w.Key == CauseKeys.HousingCohesion).Delta, 12);
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

    // R-HOUSE-005 in double, the sim's state type (docs/determinism.md: doubles are hashed as IEEE bits). The opening
    // surcharge is each city's housed x (0.50 - 0.35) x 0.1, summed in double in ordinal city order. Its exact IEEE value
    // is 7.665000000000002 (bits 0x401EA8F5C28F5C2B), not the double nearest 7.665 (0x401EA8F5C28F5C29), which a decimal
    // round trip of the city's double condition gave. Computed independently with python3 floats over cities.yaml's 12
    // cities (511 housed units).
    [Fact]
    public void Gdd03OpeningSurchargeIsTheExactDoubleSumOfTheCities()
    {
        var r = Simulation.Step(Real03, Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical), Array.Empty<Command>());
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.HousingSurcharge);
        Assert.Equal(0x401EA8F5C28F5C2BL, BitConverter.DoubleToInt64Bits(-line.Delta));
    }

    // R-HOUSE-005 in a whole gdd-0.3 turn: with every city below cohesion_threshold, the cohesion loss settles once, as one
    // housing_cohesion line on morale, however many cities contribute (#1703: "then apply cohesion once").
    [Fact]
    public void Gdd03WholeTurnHasExactlyOneHousingCohesionLine()
    {
        var s = Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical);
        foreach (var id in s.Cities.Keys.ToList()) s.Cities[id] = s.Cities[id].WithCondition(0.10);   // TEST-ONLY SYNTHETIC condition
        Assert.True(s.Cities.Count > 1);
        var r = Simulation.Step(Real03, s, Array.Empty<Command>());
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.HousingCohesion);
        Assert.Equal("morale", line.Target);
        Assert.True(line.Delta < 0, line.Delta.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // R-HOUSE-005 on the live path, where the turn's deterioration crosses cohesion_threshold: 10 housed in no housing at
    // 0.27 lose 5 pp to 0.22. The surcharge is priced at 0.27 (10 x 0.23 x 0.1 = 0.23 Money); the cohesion term reads
    // the 0.22 after deterioration: 2 x (10 / 10) x (0.25 - 0.22) / 0.25 = 0.24 points, so morale falls by 0.0024. Priced
    // from 0.27 instead, there would be no cohesion loss at all.
    [Fact]
    public void Gdd03DeteriorationBelowTheCohesionThresholdCostsCohesionThatTurn()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 0, condition: 0.27);
        s.Treasury = 100;
        var before = s.Indicators["morale"];
        var why = UpdateCondition(Real03, s);
        Assert.Equal(0.22, s.Cities.Values.Single().Condition!.Value, 12);
        Assert.Equal(-0.23, Assert.Single(why, w => w.Key == CauseKeys.HousingSurcharge).Delta, 12);
        Assert.Equal(100 - 0.23, s.Treasury, 12);
        Assert.Equal(-0.0024, Assert.Single(why, w => w.Key == CauseKeys.HousingCohesion).Delta, 12);
        Assert.Equal(before - 0.0024, s.Indicators["morale"], 12);
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

    // R-HOUSE-005, #1722 (the city panel's housing readout): Cities.HousingOutlook reads, without changing the state, what
    // UpdateCondition would settle for one city on the same state: the surcharge priced before deterioration, the condition
    // after the 5 pp overflow and the cohesion shift on morale. 10 housed in no housing at 0.27: 0.23 Money, 0.22, -0.0024.
    [Fact]
    public void HousingOutlookReadsWhatUpdateConditionSettles()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 0, condition: 0.27);
        s.Treasury = 100;
        var id = s.Cities.Keys.Single();
        var hash = s.Hash();
        var o = Cities.HousingOutlook(Real03, s, id)!.Value;
        Assert.Equal(hash, s.Hash());   // a read: no state change
        var why = UpdateCondition(Real03, s);
        Assert.Equal(-Assert.Single(why, w => w.Key == CauseKeys.HousingSurcharge).Delta, o.Surcharge, 12);
        Assert.Equal(s.Cities[id].Condition!.Value, o.ConditionAfter, 12);
        Assert.Equal(Assert.Single(why, w => w.Key == CauseKeys.HousingCohesion).Delta, o.CohesionShift, 12);
        Assert.Equal((0.50, 0.25), (o.MoneyThreshold, o.CohesionThreshold));   // content/gdd03-balance.yaml housing.*
    }

    // R-HOUSE-005: at or above both thresholds with no overflow there is nothing to read but the condition itself.
    [Fact]
    public void HousingOutlookIsZeroForAWellHousedCity()
    {
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.60);
        var o = Cities.HousingOutlook(Real03, s, s.Cities.Keys.Single())!.Value;
        Assert.Equal((0.0, 0.60, 0.0), (o.Surcharge, o.ConditionAfter, o.CohesionShift));
    }

    // gdd-0.1 has no surcharge or cohesion term (R-HOUSE-005 is gdd-0.3 only), and an unknown city or one with no condition
    // has no readout.
    [Fact]
    public void HousingOutlookIsNullOutsideGdd03AndForACityWithNoCondition()
    {
        var legacy = OneCity(Real01, civilians: 10, capacity: 10, condition: 0.10);
        Assert.Null(Cities.HousingOutlook(Real01, legacy, legacy.Cities.Keys.Single()));
        var s = OneCity(Real03, civilians: 10, capacity: 10, condition: 0.10);
        Assert.Null(Cities.HousingOutlook(Real03, s, "no_such_city"));
        var id = s.Cities.Keys.Single();
        s.Cities[id] = new City(id, 10, 0, 10, 0, 0);
        Assert.Null(Cities.HousingOutlook(Real03, s, id));
    }
}
