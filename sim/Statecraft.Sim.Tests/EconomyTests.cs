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

/// <summary>#1109: the side menu's Budget badge reads EconomySystem.Unsubmitted, true while the Budget screen's draft differs
/// from what End turn will apply (a queued budget's shares, else today's) and Submit can change that this turn.</summary>
public class EconomyUnsubmittedTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static Dictionary<string, double> Moved(IReadOnlyDictionary<string, double> shares)
    {
        var d = new Dictionary<string, double>(shares);
        d["defense_minister"] += 0.05; d["finance_minister"] -= 0.05;
        return d;
    }

    [Theory]
    // case, expected: Attention 4 = a full turn (rules.yaml attention_per_turn), allocate_budget costs 2.
    [InlineData("untouched", false)]
    [InlineData("edited", true)]
    [InlineData("edited_and_submitted", false)]
    [InlineData("edited_after_submitting", true)]       // Submit again: the queued budget is refunded first
    [InlineData("edited_after_submitting_refund", true)]   // Attention 2 = the queued budget's price: only its refund pays
    [InlineData("back_to_today_after_submitting", true)]   // Submit withdraws the queued budget, which costs nothing
    [InlineData("back_to_today_no_attention", true)]
    [InlineData("game_over", false)]
    [InlineData("no_attention", false)]
    [InlineData("attention_queued", false)]             // Attention 2.5 less set_mobilization 1 leaves 1.5 < 2
    public void TrueOnlyWhileADraftDiffersAndSubmitCanApplyIt(string probe, bool expected)
    {
        var s = new GameState { Date = new DateTime(1949, 6, 1), Attention = probe.Contains("no_attention") ? 0 : probe == "attention_queued" ? 2.5 : probe.EndsWith("_refund") ? 2 : 4 };
        EconomySystem.Init(C, s);
        var today = new Dictionary<string, double>(s.Budget);
        var edited = Moved(today);
        if (probe == "game_over") s.GameOver = GameOverCodes.CoalitionCollapse;
        var (draft, pending) = probe switch
        {
            "untouched" => (today, new Command[0]),
            "edited_and_submitted" => (edited, new Command[] { new AllocateBudget(new Dictionary<string, double>(edited)) }),
            "edited_after_submitting" or "edited_after_submitting_refund" => (Moved(edited), new Command[] { new AllocateBudget(new Dictionary<string, double>(edited)) }),
            "back_to_today_after_submitting" or "back_to_today_no_attention" => (today, new Command[] { new AllocateBudget(new Dictionary<string, double>(edited)) }),
            "attention_queued" => (edited, new Command[] { new SetMobilization(0.5) }),
            _ => (edited, new Command[0]),
        };
        Assert.Equal(expected, EconomySystem.Unsubmitted(C, s, draft, pending));
    }
}
