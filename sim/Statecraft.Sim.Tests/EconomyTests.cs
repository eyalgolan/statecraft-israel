using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class EconomyTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(C, s);
        s.Population["veterans"] = 1000;
        return s;
    }

    [Fact]
    public void InitCopiesStartValues()
    {
        var s = Fresh();
        Assert.Equal(100, s.Treasury); Assert.Equal(50, s.FxReserves); Assert.Equal(1.0, s.Budget["finance_minister"]);
    }

    [Fact]
    public void QuarterOfTaxSpendingAndTrade()
    {
        var s = Fresh();
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(100 + 0.1 - 0.125, s.Treasury, 9);   // tax 0.0004*1000/4, spend 0.0005*1000/4
        Assert.Equal(50 + 5 - 0.025, s.FxReserves, 9);   // exports 20/4, imports 0.0001*1000/4
    }

    [Fact]
    public void MobilizationCostsTreasuryAndDeficitBecomesDebt()
    {
        var s = Fresh(); s.Treasury = 1; s.Mobilization = 1.0;
        EconomySystem.Apply(C, s, 0.25, new List<Cause>());
        Assert.Equal(0, s.Treasury);
        Assert.Equal(10 + 0.025 - 1, s.Debt, 9);
    }

    [Fact]
    public void FxCrisisHasHysteresis()
    {
        var s = Fresh(); var why = new List<Cause>();
        s.FxReserves = -10; EconomySystem.Apply(C, s, 0.25, why);
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 5; EconomySystem.Apply(C, s, 0.25, why);   // 5 + 4.975 < 12.5 (quarter of start): still on
        Assert.Contains("fx_crisis", s.ActiveSituations);
        s.FxReserves = 20; EconomySystem.Apply(C, s, 0.25, why);
        Assert.DoesNotContain("fx_crisis", s.ActiveSituations);
    }

    [Theory]
    [InlineData("finance_minister", 1.0, null)]
    [InlineData("finance_minister", 0.9, "Budget shares must sum to 1")]
    [InlineData("chief_rabbi", 1.0, "The budget names a ministry that does not exist in this game")]
    [InlineData("finance_minister", -1.0, "Budget shares must sum to 1")]
    public void AllocateValidates(string office, double share, string? error)
    {
        var s = Fresh();
        Assert.Equal(error, EconomySystem.Allocate(C, s, new Dictionary<string, double> { [office] = share }));
    }

    [Fact]
    public void BudgetShareScalesEffects()
    {
        var s = Fresh();
        Assert.Equal(1.0, EffectsSystem.BudgetFactor(C, s, "finance_minister"));   // equal split is neutral
        s.Budget = new Dictionary<string, double> { ["finance_minister"] = 0.0, ["defense_minister"] = 1.0 };
        Assert.Equal(0.5, EffectsSystem.BudgetFactor(C, s, "finance_minister"));
        Assert.Equal(1.5, EffectsSystem.BudgetFactor(s.Budget, "defense_minister"));
    }
}

public class EconomyNegativeShareTests
{
    [Fact]
    public void NegativeShareRejectedEvenIfSumIsOne()
    {
        var c = EconomyCoverageTests.TwoBudgets();
        var s = new GameState();
        EconomySystem.Init(c, s);
        var err = EconomySystem.Allocate(c, s, new System.Collections.Generic.Dictionary<string, double> { ["finance_minister"] = 1.5, ["defense_minister"] = -0.5 });
        Assert.Equal("Budget shares cannot be negative", err);
    }
}

public class EconomyInvalidShareTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    static GameState Fresh() { var s = new GameState(); EconomySystem.Init(C, s); return s; }

    [Fact]
    public void NaNShareRejectedAndBudgetUnchanged()
    {
        var s = Fresh(); var before = new Dictionary<string, double>(s.Budget);
        var err = EconomySystem.Allocate(C, s, new Dictionary<string, double> { ["finance_minister"] = double.NaN });
        Assert.False(string.IsNullOrEmpty(err));
        Assert.Equal(before, s.Budget);
    }

    [Fact]
    public void InfinitePairRejectedAndBudgetUnchanged()
    {
        var s = Fresh(); var before = new Dictionary<string, double>(s.Budget);
        var err = EconomySystem.Allocate(C, s, new Dictionary<string, double> { ["finance_minister"] = double.PositiveInfinity, ["defense_minister"] = double.NegativeInfinity });
        Assert.False(string.IsNullOrEmpty(err));
        Assert.Equal(before, s.Budget);
    }

    [Fact]
    public void NullSharesRejectedAndBudgetUnchanged()
    {
        var s = Fresh(); var before = new Dictionary<string, double>(s.Budget);
        var err = EconomySystem.Allocate(C, s, null!);
        Assert.False(string.IsNullOrEmpty(err));
        Assert.Equal(before, s.Budget);
    }
}

public class EconomyCoverageTests
{
    internal static Content TwoBudgets() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
    {
        ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("budget_start: { finance_minister: 1.0 }", "budget_start: { finance_minister: 0.5, defense_minister: 0.5 }"),
    }));

    [Fact]
    public void PartialMapIsRejectedAndBudgetUnchanged()
    {
        var c = TwoBudgets(); var s = new GameState(); EconomySystem.Init(c, s);
        var before = new Dictionary<string, double>(s.Budget);
        Assert.Equal("The budget must give a share to every ministry, and only to them",
            EconomySystem.Allocate(c, s, new Dictionary<string, double> { ["finance_minister"] = 1.0 }));
        Assert.Equal(before, s.Budget);
    }

    [Fact]
    public void MapWithAnUnbudgetedOfficeIsRejected()
    {
        var c = TwoBudgets(); var s = new GameState(); EconomySystem.Init(c, s);
        Assert.Equal("The budget must give a share to every ministry, and only to them",
            EconomySystem.Allocate(c, s, new Dictionary<string, double> { ["finance_minister"] = 0.4, ["defense_minister"] = 0.4, ["prime_minister"] = 0.2 }));
    }

    [Fact]
    public void FullMapIsAccepted()
    {
        var c = TwoBudgets(); var s = new GameState(); EconomySystem.Init(c, s);
        var shares = new Dictionary<string, double> { ["finance_minister"] = 0.3, ["defense_minister"] = 0.7 };
        Assert.Null(EconomySystem.Allocate(c, s, shares));
        Assert.Equal(shares, s.Budget);
    }
}
