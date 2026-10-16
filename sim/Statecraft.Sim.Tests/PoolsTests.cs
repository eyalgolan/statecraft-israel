using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The national Food pool (GDD Phase 2 PR 3; D-C2 "National pools"): the stock, the turn's eating and imports in
/// step 7, and food_supply derived from it (maintainer decision of 16 October 2026, option (b), #382: a stock term from
/// months of cover, rationing and events on top, the unmet share lowering it further; the stock stops at 0).</summary>
public class PoolsTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture: 1000 people in units of 10 eat 100 units a month; k_food 0.1, cover_months 3.</summary>
    static Content Gdd(string mvpExtra = "  k_food: 0.1\n  cover_months: 3\n", string? model = null, string rulesExtra = "")
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("  k_food: 0.1\n  cover_months: 3\n", "").Replace("mvp:\n", "mvp:\n" + mvpExtra) + rulesExtra;
        if (model != null) files["model.yaml"] = model;
        return ContentLoader.Load(Fixture.Write(files));
    }

    static GameState Step(Content c, GameState s) => Simulation.Step(c, s, Array.Empty<Command>()).State;
    static StepResult StepWithWhy(Content c, GameState s) => Simulation.Step(c, s, Array.Empty<Command>());

    // R-FOOD-001
    [Fact]
    public void TheStockStartsAtItsMonthsOfCover()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(100, s.FoodNeed);              // 1000 people / 10 per unit, one unit of Food per unit a month
        Assert.Equal(3 * 100, s.Food);              // cover_months of the start's need
        Assert.Equal(0, s.FoodUnmet);

        var r = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var m = Real.Rules.Mvp!;
        Assert.Equal((int)Math.Round(r.TotalPopulation / m.PeoplePerUnit, MidpointRounding.AwayFromZero), r.FoodNeed);
        Assert.Equal(m.CoverMonths!.Value * r.FoodNeed, r.Food);
    }

    // R-FOOD-003
    [Fact]
    public void AFullStockLeavesFoodSupplyWhereItWas()
    {
        // The stock term is exactly 0 at cover_months of cover, so the start's food_supply is the indicator's base.
        var s = Simulation.NewGame(Gdd(), 1, Mode.Historical);
        Assert.Equal(0.4, s.Indicators["food_supply"]);
        Assert.Equal(0.0, Pools.FoodTerm(Gdd(), s));
    }

    // R-FOOD-002
    [Fact]
    public void ImportsKeepTheStockAtItsCoverInANormalTurn()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var next = Step(c, s);
        Assert.Equal(next.FoodNeed * 3, next.Food);
        Assert.Equal(0, next.FoodUnmet);
        Assert.Equal(0.4, next.Indicators["food_supply"]);
    }

    // R-FOOD-002 (Q-POOL-4: the fx crisis cuts the imports, not food_supply directly)
    [Fact]
    public void ACurrencyCrisisCutsTheImportsSoTheStockFalls()
    {
        var c = Gdd(rulesExtra: "fx_crisis_food_per_year: -0.4\n");   // the legacy loop's direct cut, set so its absence shows
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.FxReserves = -1000;                       // stays exhausted through the step
        var r = StepWithWhy(c, s);
        Assert.Equal(300 - 100, r.State.Food);      // ate a month's need, imported nothing
        Assert.Equal(0, r.State.FoodUnmet);
        Assert.Contains(r.Why, w => w.System == "economy" && w.Target == "food" && w.Message.Contains("foreign currency crisis"));
        // The legacy direct cut does not also act in the GDD loop: the only change is the stock term.
        Assert.Equal(0.0, r.State.IndicatorShifts["food_supply"]);
    }

    // R-FOOD-002 (Q-POOL-5: the stock stops at 0, and the unmet share is counted)
    [Fact]
    public void TheStockStopsAtZero()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.FxReserves = -1000;
        s.Food = 40;                                // less than the 100 the nation eats this turn
        var r = StepWithWhy(c, s);
        Assert.Equal(0, r.State.Food);
        Assert.Equal(100 - 40, r.State.FoodUnmet);
        Assert.Contains(r.Why, w => w.System == "economy" && w.Message.Contains("60 of 100"));
        var again = Step(c, r.State);               // still no imports: nothing to eat, never below 0
        Assert.Equal(0, again.Food);
        Assert.Equal(again.FoodNeed, again.FoodUnmet);
    }

    // R-FOOD-003
    [Fact]
    public void FoodSupplyFollowsTheStock()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        double Level(int food, int unmet)
        {
            var x = s.Clone(); x.Food = food; x.FoodUnmet = unmet;
            EffectsSystem.Apply(c, x, new List<Cause>());
            return x.Indicators["food_supply"];
        }
        Assert.Equal(0.4, Level(300, 0), 12);                       // three months: no term
        Assert.Equal(0.4 - 0.1 / 3, Level(200, 0), 12);             // two months: a third of k_food off
        Assert.Equal(0.4 - 0.1, Level(0, 0), 12);                   // empty: all of k_food off
        Assert.Equal(0.4 - 0.1 - 0.2, Level(0, 20), 12);            // 20% of the need unmet: 0.2 further
        Assert.Equal(0.0, Level(0, 100), 12);                       // nothing eaten: clamped at 0, never below
        Assert.True(Level(400, 0) > 0.4);                           // more than the cover lifts it
    }

    // R-FOOD-003: rationing and events still act on top of the stock term.
    [Fact]
    public void RationingAndEventsStillActOnFoodSupply()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Food = 0;                                                 // the term at its lowest, -k_food
        var rationed = s.Clone();
        rationed.Policies["rationing"] = 1.0;
        EffectsSystem.Apply(c, s, new List<Cause>());
        EffectsSystem.Apply(c, rationed, new List<Cause>());
        var feeds = rationed.EffectContributions["rationing_food"];
        Assert.True(feeds > 0);
        Assert.Equal(0.4 - 0.1, s.Indicators["food_supply"], 12);
        Assert.Equal(0.4 - 0.1 + feeds, rationed.Indicators["food_supply"], 12);   // rationing on top of the stock term
        var moved = EffectsSystem.Shift(c, s, "food_supply", -0.05); // an event's shift
        Assert.Equal(-0.05, moved, 12);
        EffectsSystem.Apply(c, s, new List<Cause>());
        Assert.Equal(0.4 - 0.1 - 0.05, s.Indicators["food_supply"], 12);
    }

    // R-FOOD-002: step 7 reads the need of the people at the turn's start (the legacy head count grows after the resolver).
    [Fact]
    public void TheTurnEatsWhatTheStartOfTheTurnNeeds()
    {
        var c = Gdd();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.FxReserves = -1000;
        s.Population["veterans"] = 2000;                            // 200 units now; the state's stored need is still 100
        var next = Step(c, s);
        Assert.Equal(300 - 200, next.Food);                         // the need is re-read at step 7, before arrivals
        Assert.True(next.TotalPopulation > 2000);                   // natural increase came after it
        Assert.Equal(200, next.FoodNeed);
    }

    // R-FOOD-002 (Q-POOL-4) and R-FOOD-003: the sourced 1951 currency crisis still cuts food in the real game.
    [Fact]
    public void The1951CurrencyCrisisStillCutsFood()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var history = new List<GameState> { s };
        while (s.GameOver == null) { s = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State; history.Add(s); }
        var first = history.FindIndex(x => x.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.True(first > 0 && first < history.Count - 1, "the crisis must begin before the last turn");
        var before = history[first];                                // the crisis began in this turn's economy
        var cut = history[first + 1];                               // the next turn's imports are cut
        Assert.True(history.Take(first + 1).All(x => x.Food == Real.Rules.Mvp!.CoverMonths!.Value * x.FoodNeed), "before the crisis the stock holds its cover");
        Assert.True(cut.Food < cut.FoodNeed * Real.Rules.Mvp!.CoverMonths!.Value, "the crisis cut the imports");
        Assert.True(cut.Indicators["food_supply"] < before.Indicators["food_supply"], "food_supply fell with the stock");
        Assert.All(history, x => Assert.True(x.Food >= 0));
    }

    // R-FOOD-001, R-LOOP-002: the pool is hashed in the GDD loop, and the legacy loop's text is unchanged.
    [Fact]
    public void ThePoolIsHashedOnlyWhenSet()
    {
        var legacy = Simulation.NewGame(ContentLoader.Load(Fixture.Write()), 1, Mode.Historical);
        Assert.DoesNotContain("\nfood=", legacy.Describe());
        var s = Simulation.NewGame(Gdd(), 1, Mode.Historical);
        Assert.Contains("\nfood=300,100,0", s.Describe());
        var other = s.Clone(); other.FoodUnmet = 1;
        Assert.NotEqual(s.Hash(), other.Hash());
        Assert.Equal(300, s.Food);                                   // a clone does not share the pool
    }

    // R-FOOD-001: missing or out-of-range values are load errors, never a default.
    [Theory]
    [InlineData("  cover_months: 3\n", "mvp.k_food", "required")]
    [InlineData("  k_food: 0.1\n", "mvp.cover_months", "required")]
    [InlineData("  k_food: -0.1\n  cover_months: 3\n", "mvp.k_food", "between 0 and 1")]
    [InlineData("  k_food: 1.5\n  cover_months: 3\n", "mvp.k_food", "between 0 and 1")]
    [InlineData("  k_food: .nan\n  cover_months: 3\n", "mvp.k_food", "between 0 and 1")]
    [InlineData("  k_food: 0.1\n  cover_months: 0\n", "mvp.cover_months", ">= 1")]
    public void TheFoodValuesAreRequired(string extra, string field, string message)
    {
        var ex = Assert.Throws<ContentException>(() => Gdd(extra));
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    // R-FOOD-003: the GDD loop needs the food_supply indicator the pool feeds.
    [Fact]
    public void TheGddLoopNeedsAFoodSupplyIndicator()
    {
        var model = Fixture.Files["model.yaml"]
            .Replace("  - id: food_supply\n    base: 0.4\n    name: Food supply\n", "")
            .Replace("    target: food_supply\n", "    target: morale\n")
            .Replace("    indicator: food_supply\n", "    indicator: morale\n");
        var ex = Assert.Throws<ContentException>(() => Gdd(model: model));
        Assert.Equal("mvp.k_food", ex.Field);
        Assert.Contains("food_supply", ex.Message);
    }
}
