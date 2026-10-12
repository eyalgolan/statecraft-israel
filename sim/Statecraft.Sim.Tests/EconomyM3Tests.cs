using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>M3 economy rules on the fixture: donations and loans, the food cut during a currency crisis, and the
/// historical AI sending reserves home.</summary>
public class EconomyM3Tests
{
    static Content Load(string rulesExtra) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = Fixture.Files["rules.yaml"] + rulesExtra }));

    static GameState Fresh(Content c)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EffectsSystem.Init(c, s);
        EconomySystem.Init(c, s);
        s.Population["veterans"] = 1000;
        return s;
    }

    [Fact]
    public void DonationsAndLoansAddForeignCurrency()
    {
        var c = Load("capital_inflow_per_year: 8\n");
        var s = Fresh(c);
        var why = new List<Cause>();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Equal(50 + 5 + 2 - 0.025, s.FxReserves, 9);   // exports 20/4, inflow 8/4, imports 0.0001*1000/4
        Assert.Contains(why, w => w.Target == "fx_reserves" && w.Message.Contains("donations and loans $2.0m"));
    }

    [Fact]
    public void ACurrencyCrisisCutsFoodImports()
    {
        var c = Load("fx_crisis_food_per_year: -0.4\n");
        var s = Fresh(c);
        s.FxReserves = -20;                                     // still negative after a quarter of exports (+5)
        var why = new List<Cause>();
        EconomySystem.Apply(c, s, 0.25, why);
        Assert.Contains(EconomySystem.FxCrisis, s.ActiveSituations);
        Assert.Equal(0.4 - 0.1, s.Indicators["food_supply"], 9);
        Assert.Equal(-0.1, s.IndicatorShifts["food_supply"], 9);
        Assert.Contains(why, w => w.Target == "food_supply" && w.Message == "Food supply -10.0% (foreign currency crisis: food imports cut)");
    }

    [Fact]
    public void TheFoodCutStopsWhenTheCrisisEases()
    {
        var c = Load("fx_crisis_food_per_year: -0.4\n");
        var s = Fresh(c);
        s.FxReserves = -20;
        EconomySystem.Apply(c, s, 0.25, new List<Cause>());
        Assert.Contains(EconomySystem.FxCrisis, s.ActiveSituations);
        var afterCut = s.Indicators["food_supply"];
        s.FxReserves = 10 * c.Rules.FxStart;                     // well above the exit threshold, 0.25 * fx_start
        EconomySystem.Apply(c, s, 0.25, new List<Cause>());      // the crisis eases in this step
        Assert.DoesNotContain(EconomySystem.FxCrisis, s.ActiveSituations);
        Assert.Equal(afterCut, s.Indicators["food_supply"], 9);
        EconomySystem.Apply(c, s, 0.25, new List<Cause>());
        Assert.Equal(afterCut, s.Indicators["food_supply"], 9);
    }

    [Fact]
    public void NoCrisisNoFoodCut()
    {
        var c = Load("fx_crisis_food_per_year: -0.4\n");
        var s = Fresh(c);
        EconomySystem.Apply(c, s, 0.25, new List<Cause>());
        Assert.Equal(0.4, s.Indicators["food_supply"], 9);
    }

    [Fact]
    public void APositiveFoodCutIsALoadError()
    {
        var ex = Assert.Throws<ContentException>(() => Load("fx_crisis_food_per_year: 0.1\n"));
        Assert.Equal("fx_crisis_food_per_year", ex.Field);
    }

    [Fact]
    public void TheBudgetFactorRuleWorksOnAnyShares()
    {
        var draft = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.3, ["c"] = 0.2 };
        Assert.Equal(1.25, EffectsSystem.BudgetFactor(draft, "a"), 9);     // 0.5 + 0.5 * 3 / 2
        Assert.Equal(0.8, EffectsSystem.BudgetFactor(draft, "c"), 9);
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(draft, "z"), 9);      // no line: neutral
    }

    [Fact]
    public void HistoricalAiSendsReservesHomeToTheRungMinimum()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Assert.Empty(HistoricalAI.Commands(c, s));                 // at war: the call-up is already the rung-5 minimum
        s.Rung = 2;
        var order = Assert.IsType<SetMobilization>(HistoricalAI.Commands(c, s).Single());
        Assert.Equal(0.05, order.Level);
    }
}
