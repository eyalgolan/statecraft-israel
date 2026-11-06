using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-MONEY-004, "Spend the dollars" (#813; owner, 2 November 2026: "Spend the dollars (Recommended)",
/// https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564883926): during the war (rung 4 or 5) the
/// player may convert up to mvp.dollars_per_turn of foreign currency a turn into Money, mvp.dollars_total in all, never
/// taking the reserves below 0, at the rate of mvp.il_per_dollar in force on the turn's date. The fixture converts at IL 0.5
/// per $ (IL 0.25 from 15 August 1948), $4m a turn and $10m in all.</summary>
public class DollarsTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    static Content Gdd() => ContentLoader.Load(Fixture.Write(Fixture.Gdd()));

    /// <summary>A new fixture game at the war's rung 5, with the Treasury and the reserves set for round sums.</summary>
    static GameState War(Content c, double fx = 50, int converted = 0)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(5, s.Rung);
        s.Treasury = 1; s.Debt = 0; s.FxReserves = fx; s.DollarsConverted = converted;
        return s;
    }

    static PreviewResult Orders(Content c, GameState s, params Command[] orders) => Simulation.Preview(c, s, orders);

    // R-MONEY-004: the smallest conversion is EconomySystem.DollarsMinimum (US$1m). The sim refuses below it, the UI words
    // the refusal from the same constant (SimBridge.ReasonText; DollarConversionTests pins that), and the sim's own English,
    // a constant tests name in attributes, says the same amount, so none of the three can drift from the others (#2704).
    [Fact]
    public void TheConversionMinimumIsOneConstantTheRefusalAndItsWordingShare()
    {
        var c = Gdd();
        var s = War(c);
        Assert.Equal(EconomySystem.RefusedDollarsMinimum, EconomySystem.DollarsRefusal(c, s, EconomySystem.DollarsMinimum - 1));
        Assert.NotEqual(EconomySystem.RefusedDollarsMinimum, EconomySystem.DollarsRefusal(c, s, EconomySystem.DollarsMinimum));
        Assert.Equal(EconomySystem.RefusedDollarsMinimum,
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Convert at least ${0}m", EconomySystem.DollarsMinimum));
        Assert.True(EconomySystem.RefusedDollarsMinimum == "Convert at least $1m", "the sim's English stays byte for byte as it was");
    }

    // R-MONEY-004: $3m at IL 0.5 per $ moves IL 1.5m into the Treasury and takes $3m from the reserves, with a cause line.
    [Fact]
    public void ConvertingMovesTheDollarsTimesTheRateIntoMoney()
    {
        var c = Gdd();
        Assert.Equal((4, 10, 0.5), (c.Rules.Mvp!.DollarsPerTurn, c.Rules.Mvp.DollarsTotal, c.Rules.Mvp.IlPerDollarOn(c.Rules.Start)));
        var s = War(c);
        var r = Orders(c, s, new ConvertDollars(3));
        Assert.Empty(r.Rejected);
        Assert.Equal((2.5, 0.0, 47.0, 3), (r.State.Treasury, r.State.Debt, r.State.FxReserves, r.State.DollarsConverted));
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.DollarsConverted && w.Target == "treasury");
        Assert.Equal(("economy", "treasury", 1.5), (line.System, line.Target, line.Delta));
        Assert.Equal(new[] { "3" }, line.Args);
        Assert.Contains(CauseKeys.DollarsConverted, CauseKeys.All);   // so the UI words it (CauseText)
        Assert.Equal("Converted $3.0m of foreign currency into Money: " + PlayerText.MoneyChange(c, "treasury", 1.5), line.Message);
    }

    // R-MONEY-004, R-ARMS-002 under gdd-0.3 (#2733 item 13): a conversion queued after an Arms purchase still runs before
    // it, so the purchase spends the converted Money and borrows nothing. The gdd-0.3 fixture's Arms cost IL 0.25 and $1
    // converts at IL 0.5, so 8 Arms cost exactly the IL 2m that $4m brings. Run in queue order, the purchase would borrow.
    [Fact]
    public void UnderGdd03AConversionQueuedAfterAnArmsPurchasePaysForIt()
    {
        var c = Gdd03MovementFortTests.Load();
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        Assert.Equal((0.25, 0.5), (c.Rules.Mvp!.ArmsPrice, c.Rules.Mvp.IlPerDollarOn(c.Rules.Start)));
        var s = War(c);
        s.Treasury = 0;
        var r = Orders(c, s, new BuyArms(8), new ConvertDollars(4));
        Assert.Empty(r.Rejected);
        Assert.Equal((s.Arms + 8, 0.0, 0.0, 46.0, 4), (r.State.Arms, r.State.Treasury, r.State.Debt, r.State.FxReserves, r.State.DollarsConverted));
        var keys = r.Why.Select(w => w.Key).ToList();
        Assert.True(keys.IndexOf(CauseKeys.DollarsConverted) >= 0 && keys.IndexOf(CauseKeys.DollarsConverted) < keys.IndexOf(CauseKeys.ArmsBought),
            "the conversion's cause comes before the purchase's: " + string.Join(", ", keys));
    }

    // #2527: the same conversion also explains the reserves' drop, so the HUD's foreign-currency meter (which lists the
    // causes aimed at fx_reserves, HudModel.cs) shows its reason.
    [Fact]
    public void TheConversionAlsoExplainsTheReservesDrop()
    {
        var c = Gdd();
        var s = War(c);
        var r = Orders(c, s, new ConvertDollars(3));
        Assert.Empty(r.Rejected);
        var fx = Assert.Single(r.Why, w => w.Key == CauseKeys.DollarsConvertedFx);
        Assert.Equal(("economy", -3.0), (fx.System, fx.Delta));
        Assert.Equal(new[] { "3" }, fx.Args);
        Assert.Equal("Foreign currency converted into Money: " + PlayerText.MoneyChange(c, "fx_reserves", -3.0), fx.Message);
    }

    // R-MONEY-004: the rate is the one in force on the turn's date (mvp.il_per_dollar, each rate from its day on).
    [Theory]
    [InlineData("1948-08-14", 1.0)] [InlineData("1948-08-15", 0.5)] [InlineData("1949-01-15", 0.5)]
    public void TheRateIsTheOneInForceOnTheTurnsDate(string date, double money)
    {
        var c = Gdd();
        var s = War(c);
        s.Date = DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture);
        var r = Orders(c, s, new ConvertDollars(2));
        Assert.Empty(r.Rejected);
        Assert.Equal(1 + money, r.State.Treasury, 12);
        // End turn converts at the same rate: the turn's start date, not its end (the 14 August turn ends after the 15th).
        var none = Simulation.Step(c, s, Array.Empty<Command>()).State;
        var sold = Simulation.Step(c, s, new Command[] { new ConvertDollars(2) });
        Assert.Empty(sold.Rejected);
        Assert.Equal(none.Treasury - none.Debt + money, sold.State.Treasury - sold.State.Debt, 9);
    }

    // R-MONEY-004: the IL goes into the Treasury before the turn's economy, so the turn's own deficit borrows less
    // (R-MONEY-003): the net position (Treasury minus Debt) ends exactly the conversion higher. The fixture's turn borrows
    // about IL 0.48m, so $1m (IL 0.5m) covers it and $4m (IL 2m) also leaves Money in the Treasury.
    [Theory]
    [InlineData(1)] [InlineData(4)]
    public void TheConversionLowersTheTurnsBorrowing(int amount)
    {
        var c = Gdd();
        var s = War(c);
        s.Treasury = 0;
        var none = Simulation.Step(c, s, Array.Empty<Command>()).State;
        var sold = Simulation.Step(c, s, new Command[] { new ConvertDollars(amount) }).State;
        Assert.True(none.Debt > 0 && none.Debt < 0.5, $"the fixture's turn borrows under IL\u00A00.5m ({none.Debt})");
        Assert.Equal((0.0, 0.0), (none.Treasury, sold.Debt));
        Assert.Equal(none.Treasury - none.Debt + amount * 0.5, sold.Treasury - sold.Debt, 9);
        Assert.Equal(none.FxReserves - amount, sold.FxReserves, 9);
        Assert.Equal(amount, sold.DollarsConverted);
    }

    // R-MONEY-004: a turn converts at most dollars_per_turn, over all of its orders together.
    [Fact]
    public void ATurnConvertsAtMostItsMonthlyLimit()
    {
        var c = Gdd();
        var s = War(c);
        Assert.Equal(EconomySystem.RefusedDollarsMonthly, Assert.Single(Orders(c, s, new ConvertDollars(5)).Rejected).Reason);
        var two = Orders(c, s, new ConvertDollars(2), new ConvertDollars(2));
        Assert.Empty(two.Rejected);
        Assert.Equal(4, two.State.DollarsConverted);
        var over = Orders(c, s, new ConvertDollars(3), new ConvertDollars(2));
        Assert.Equal(EconomySystem.RefusedDollarsMonthly, Assert.Single(over.Rejected).Reason);
        Assert.Equal(3, over.State.DollarsConverted);
        // A refused order uses none of the limit: 3, then 2 refused, then 1 makes 4.
        var after = Orders(c, s, new ConvertDollars(3), new ConvertDollars(2), new ConvertDollars(1));
        Assert.Equal(EconomySystem.RefusedDollarsMonthly, Assert.Single(after.Rejected).Reason);
        Assert.Equal(2, ((ConvertDollars)after.Rejected[0].Command).Amount);
        Assert.Equal(4, after.State.DollarsConverted);
        // The limit is per turn: the next turn may convert its own dollars_per_turn again.
        var next = Simulation.Step(c, s, new Command[] { new ConvertDollars(4) }).State;
        Assert.Equal(5, next.Rung);
        Assert.Empty(Orders(c, next, new ConvertDollars(4)).Rejected);
        // A limit of 0 refuses every amount.
        foreach (var r in c.EraRules.Values.Append(c.Rules)) r.Mvp!.DollarsPerTurn = 0;
        Assert.Equal(EconomySystem.RefusedDollarsMonthly, Assert.Single(Orders(c, s, new ConvertDollars(1)).Rejected).Reason);
    }

    // R-MONEY-004: the game converts at most dollars_total; an amount that would pass it is refused whole.
    [Fact]
    public void TheGameConvertsAtMostItsTotal()
    {
        var c = Gdd();
        var s = War(c, converted: 8);
        Assert.Equal(EconomySystem.RefusedDollarsTotal, Assert.Single(Orders(c, s, new ConvertDollars(3)).Rejected).Reason);
        var two = Orders(c, s, new ConvertDollars(2));
        Assert.Empty(two.Rejected);
        Assert.Equal(10, two.State.DollarsConverted);
        Assert.Equal(EconomySystem.RefusedDollarsTotal, Assert.Single(Orders(c, two.State, new ConvertDollars(1)).Rejected).Reason);
    }

    // R-MONEY-004: a conversion may take the reserves to 0, never below.
    [Fact]
    public void TheReservesNeverGoBelowZero()
    {
        var c = Gdd();
        Assert.Equal(EconomySystem.RefusedDollarsReserves, Assert.Single(Orders(c, War(c, fx: 2.5), new ConvertDollars(3)).Rejected).Reason);
        var exact = Orders(c, War(c, fx: 3), new ConvertDollars(3));
        Assert.Empty(exact.Rejected);
        Assert.Equal(0.0, exact.State.FxReserves);
        Assert.Equal(EconomySystem.RefusedDollarsReserves, Assert.Single(Orders(c, War(c, fx: -1), new ConvertDollars(1)).Rejected).Reason);
    }

    // R-MONEY-004: only during the war, rung 4 or 5.
    [Theory]
    [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, false)] [InlineData(4, true)] [InlineData(5, true)]
    public void OnlyTheWarRungsMayConvert(int rung, bool allowed)
    {
        var c = Gdd();
        var s = War(c);
        s.Rung = rung;
        var r = Orders(c, s, new ConvertDollars(1));
        if (allowed) Assert.Empty(r.Rejected);
        else Assert.Equal(EconomySystem.RefusedDollarsNotAtWar, Assert.Single(r.Rejected).Reason);
    }

    const string RungEvent = @"
id: rung
title: A choice
text: '{finance_minister} asks for a decision.'
office: finance_minister
anchor: hard
from: 1948-05-15
options:
  - id: history
    text: The way it went
    historical: true
    set_rung: RUNG
sources: [{ url: https://example.org/event, checked: 2026-10-18 }]
";

    // R-MONEY-004: the war is the rung the turn started at. An earlier order of the same turn that moves the rung (an
    // event's set_rung) changes neither way whether the conversion is allowed, so End turn agrees with the Defence screen,
    // which checks the turn's start.
    [Theory]
    [InlineData(5, 3, true)] [InlineData(4, 2, true)] [InlineData(3, 5, false)]
    public void TheWarIsTheRungTheTurnStartedAt(int start, int set, bool allowed)
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["events/rung.yaml"] = RungEvent.Replace("RUNG", set.ToString(System.Globalization.CultureInfo.InvariantCulture)) })));
        var s = War(c);
        s.Rung = start;
        Assert.Contains("rung", s.PendingEvents);
        var orders = new Command[] { new ChooseEventOption("rung", "history"), new ConvertDollars(1) };
        var preview = Orders(c, s, orders);
        Check(preview.State, preview.Rejected);
        var step = Simulation.Step(c, s, orders);
        Check(step.State, step.Rejected);
        void Check(GameState state, IEnumerable<Rejection> rejected)
        {
            Assert.Equal(set, state.Rung);
            if (allowed) { Assert.Empty(rejected); Assert.Equal(1, state.DollarsConverted); }
            else { Assert.Equal(EconomySystem.RefusedDollarsNotAtWar, Assert.Single(rejected).Reason); Assert.Equal(0, state.DollarsConverted); }
        }
        Assert.Equal(allowed ? null : EconomySystem.RefusedDollarsNotAtWar, EconomySystem.DollarsRefusal(c, s, 1));   // the screen's check
    }

    // R-MONEY-004: at least $1m.
    [Theory]
    [InlineData(0)] [InlineData(-3)]
    public void AnAmountBelowOneIsRefused(int amount)
    {
        var c = Gdd();
        Assert.Equal(EconomySystem.RefusedDollarsMinimum, Assert.Single(Orders(c, War(c), new ConvertDollars(amount)).Rejected).Reason);
    }

    // R-MONEY-004: every refusal leaves the state as the turn with no order, Attention included, and says nothing in Why.
    // The order is priced at 1 here (content prices it at 0), so a refusal that kept the Attention would show.
    [Theory]
    [InlineData(0, 50, 0, 5, EconomySystem.RefusedDollarsMinimum)]
    [InlineData(1, 50, 0, 3, EconomySystem.RefusedDollarsNotAtWar)]
    [InlineData(5, 50, 0, 5, EconomySystem.RefusedDollarsMonthly)]
    [InlineData(3, 50, 8, 5, EconomySystem.RefusedDollarsTotal)]
    [InlineData(3, 2.5, 0, 5, EconomySystem.RefusedDollarsReserves)]
    public void ARefusalLeavesTheStateUntouched(int amount, double fx, int converted, int rung, string reason)
    {
        var c = Gdd();
        foreach (var r in c.EraRules.Values.Append(c.Rules)) r.AttentionCost[EconomySystem.ConvertDollarsKind] = 1;
        var s = War(c, fx, converted);
        s.Rung = rung;
        Assert.True(AttentionSystem.Cost(c, s, EconomySystem.ConvertDollarsKind) > 0 && s.Attention >= AttentionSystem.Cost(c, s, EconomySystem.ConvertDollarsKind));
        var refused = Simulation.Step(c, s, new Command[] { new ConvertDollars(amount) });
        Assert.Equal(reason, Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Simulation.Step(c, s, Array.Empty<Command>()).State.Hash(), refused.State.Hash());
        Assert.DoesNotContain(refused.Why, w => w.Key == CauseKeys.DollarsConverted);
        // Step refills Attention at the turn's end, so only the dry run shows the refund.
        var preview = Orders(c, s, new ConvertDollars(amount));
        Assert.Equal(reason, Assert.Single(preview.Rejected).Reason);
        Assert.Equal(s.Attention, preview.State.Attention);
        Assert.Equal(Orders(c, s).State.Hash(), preview.State.Hash());
    }

    // R-MONEY-004: a GDD order; the legacy loop refuses it before charging Attention.
    [Fact]
    public void TheLegacyLoopRefusesIt()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Simulation.Step(c, s, new Command[] { new ConvertDollars(1) });
        Assert.Equal("That order is not available in this game", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Simulation.Step(c, s, Array.Empty<Command>()).State.Hash(), r.State.Hash());
    }

    // R-MONEY-004: the total is in the state hash only once something is converted, so a run without the order hashes as before.
    [Fact]
    public void TheTotalIsHashedOnlyWhenSet()
    {
        var c = Gdd();
        var s = War(c);
        Assert.DoesNotContain("dollars=", s.Describe());
        var after = Orders(c, s, new ConvertDollars(2)).State;
        Assert.Contains("dollars=2", after.Describe());
        Assert.NotEqual(s.Hash(), after.Hash());
    }

    // R-MONEY-004: the order is saved as `dollars N`, replayed on load, and the total comes back with the same hash.
    [Fact]
    public void TheTotalRoundTripsThroughTheSave()
    {
        var c = Gdd();
        var g = GameSession.New(c, 7, Mode.Historical);
        g.Step(new Command[] { new ConvertDollars(3) });
        g.Step(new Command[] { new ConvertDollars(4) });
        Assert.Equal(7, g.State.DollarsConverted);
        var text = g.Save.Write();
        Assert.Contains("\ndollars 3\n", text);
        var back = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(7, back.State.DollarsConverted);
        Assert.Equal(g.State.Hash(), back.State.Hash());
        var parsed = Assert.IsType<ConvertDollars>(SaveGame.Parse(text).Turns[1].Single());
        Assert.Equal(4, parsed.Amount);
        var built = Assert.IsType<ConvertDollars>(Scenario.Build(new Dictionary<string, object> { ["kind"] = "convert_dollars", ["amount"] = 2 }));
        Assert.Equal(2, built.Amount);
    }

    // R-MONEY-004: the real content's values, as ruled: $4m a turn and $100m in all, at the sourced rates (#813 comment
    // 6565039439): the effective dollar rate IL 0.333 per $ ($3.00) to 18 September 1949, IL 0.357 ($2.80) from the 19th.
    [Fact]
    public void TheRealContentConvertsFourMillionAMonthAndAHundredInAll()
    {
        var m = Real.Rules.Mvp!;
        Assert.Equal((4, 100), (m.DollarsPerTurn, m.DollarsTotal));
        Assert.Equal((0.333, 0.333, 0.333, 0.357, 0.357), (m.IlPerDollarOn(new DateTime(1948, 5, 15)), m.IlPerDollarOn(new DateTime(1949, 3, 15)),
            m.IlPerDollarOn(new DateTime(1949, 9, 18)), m.IlPerDollarOn(new DateTime(1949, 9, 19)), m.IlPerDollarOn(new DateTime(1952, 1, 15))));
        Assert.Equal(0.0, Real.Rules.AttentionCost["convert_dollars"]);
    }

    static (string, string, double, string)[] Lines(StepResult r) => r.Why.Select(w => (w.System, w.Target, w.Delta, w.Message)).ToArray();

    static int FirstArmsPastDebtCeiling(Content c, GameState s, double moneyBeforePurchase = 0)
    {
        var m = c.RulesAt(s.Date).Mvp!;
        var capacity = s.Treasury + moneyBeforePurchase + m.DebtCeiling!.Value - s.Debt;
        // The smallest n with n * price > capacity (exactly at the ceiling is not past it, Pools.BorrowsPastDebtCeiling).
        // Near an exact multiple, float rounding here and in Pools could disagree by one Arms, so fail loudly there.
        var arms = capacity / m.ArmsPrice!.Value;
        Assert.True(Math.Abs(arms - Math.Round(arms)) > 1e-6,
            FormattableString.Invariant($"capacity {capacity} is an exact multiple of the Arms price {m.ArmsPrice.Value}; the boundary is ambiguous"));
        return checked((int)Math.Floor(arms) + 1);
    }

    // R-MONEY-004, R-ARMS-002 (#2206; owner, 2 November 2026: "Conversion counts first (Recommended)"): a turn's dollar
    // conversions run before its Arms purchases, so the purchase borrows only what the conversion leaves it short, whatever
    // the queue order. The accepted and refused boundary is derived from the real content's Treasury, debt ceiling, Arms price,
    // monthly conversion allowance and dated exchange rate.
    [Fact]
    public void AConversionCountsForAnArmsPurchaseWhateverTheQueueOrder()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var m = Real.RulesAt(s.Date).Mvp!;
        Assert.True(s.Rung >= EconomySystem.WarRung);   // the first turn may convert (R-MONEY-004)
        var converted = m.DollarsPerTurn!.Value;
        var firstRefusedAfterConversion = FirstArmsPastDebtCeiling(Real, s, converted * m.IlPerDollarOn(s.Date));
        var accepted = firstRefusedAfterConversion - 1;
        // The amount the conversion makes affordable is refused without it, so the queue order matters.
        Assert.Equal(Pools.RefusedDebtCeiling, Assert.Single(Simulation.Step(Real, s,
            new Command[] { new BuyArms(accepted) }).Rejected).Reason);
        var first = Simulation.Step(Real, s, new Command[] { new ConvertDollars(converted), new BuyArms(accepted) });
        var last = Simulation.Step(Real, s, new Command[] { new BuyArms(accepted), new ConvertDollars(converted) });
        Assert.Empty(first.Rejected);
        Assert.Empty(last.Rejected);
        Assert.Equal(first.State.Hash(), last.State.Hash());
        Assert.Equal(Lines(first), Lines(last));
        Assert.Equal(s.Arms + accepted, last.State.Arms);
        // The money adds up: the orders leave the Treasury at 0 and the debt at purchase cost minus starting cash and conversion, and the turn ends the
        // purchase less and the conversion more than the turn with no order.
        var orders = Simulation.Preview(Real, s, new Command[] { new BuyArms(accepted), new ConvertDollars(converted) }).State;
        Assert.Equal(0.0, orders.Treasury, 9);
        Assert.Equal(s.Debt + accepted * m.ArmsPrice!.Value - s.Treasury - converted * m.IlPerDollarOn(s.Date), orders.Debt, 9);
        var none = Simulation.Step(Real, s, Array.Empty<Command>()).State;
        Assert.Equal(none.Treasury - none.Debt - accepted * m.ArmsPrice.Value + converted * m.IlPerDollarOn(s.Date), last.State.Treasury - last.State.Debt, 9);
        // One Arms more passes the ceiling in either order, refused with the same reason, and the conversion stands.
        var over = Simulation.Step(Real, s, new Command[] { new BuyArms(firstRefusedAfterConversion), new ConvertDollars(converted) });
        var overFirst = Simulation.Step(Real, s, new Command[] { new ConvertDollars(converted), new BuyArms(firstRefusedAfterConversion) });
        Assert.Equal(Pools.RefusedDebtCeiling, Assert.Single(over.Rejected).Reason);
        Assert.Equal(Pools.RefusedDebtCeiling, Assert.Single(overFirst.Rejected).Reason);
        Assert.Equal(overFirst.State.Hash(), over.State.Hash());
        Assert.Equal(converted, over.State.DollarsConverted);
    }

    // R-ARMS-002, R-MONEY-004 (#2206): the Defence screen's ceiling warning counts the conversions queued this turn, as End
    // turn does (Pools.BorrowsPastDebtCeiling with the queue). A conversion End turn refuses for passing the monthly allowance
    // counts for nothing.
    [Fact]
    public void TheCeilingCheckCountsTheQueuedConversionsAsEndTurnDoes()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var hash = s.Hash();
        var m = Real.RulesAt(s.Date).Mvp!;
        var converted = m.DollarsPerTurn!.Value;
        var afterConversion = FirstArmsPastDebtCeiling(Real, s, converted * m.IlPerDollarOn(s.Date));
        // BuyArms(1) is the purchase already queued; the amount checked replaces it in its slot, as Buy does (SimBridge.Queue).
        var queued = new Command[] { new BuyArms(1), new ConvertDollars(converted) };
        Assert.True(Pools.BorrowsPastDebtCeiling(Real, s, Array.Empty<Command>(), afterConversion - 1));
        Assert.False(Pools.BorrowsPastDebtCeiling(Real, s, queued, afterConversion - 1));
        Assert.True(Pools.BorrowsPastDebtCeiling(Real, s, queued, afterConversion));
        Assert.True(Pools.BorrowsPastDebtCeiling(Real, s, new Command[] { new ConvertDollars(converted + 1) }, afterConversion - 1));
        foreach (var count in new[] { afterConversion - 1, afterConversion })
            Assert.Equal(Pools.BorrowsPastDebtCeiling(Real, s, queued, count),
                Simulation.Step(Real, s, new Command[] { new BuyArms(count), new ConvertDollars(converted) }).Rejected.Any(x => x.Reason == Pools.RefusedDebtCeiling));
        Assert.Equal(hash, s.Hash());   // the queue is judged on a copy
    }

    // R-ARMS-002 (#2206): the warning runs the whole queue as End turn will, so spending queued before the purchase counts
    // too. The boundary is derived from the starting Treasury and debt, the ceiling, the Arms price, the halutz training cost,
    // and the monthly conversion at the dated rate.
    [Fact]
    public void TheCeilingCheckCountsTheSpendingQueuedBeforeThePurchase()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var m = Real.RulesAt(s.Date).Mvp!;
        var converted = m.DollarsPerTurn!.Value;
        var trainingCost = Real.UnitTypes.Single(t => t.Id == "halutz").TrainingCost;
        var convertedMoney = converted * m.IlPerDollarOn(s.Date);
        var firstRefused = FirstArmsPastDebtCeiling(Real, s, convertedMoney - trainingCost);
        var queued = new Command[] { new Train("tel_aviv", "halutz", 1), new ConvertDollars(converted) };
        Assert.True(Pools.BorrowsPastDebtCeiling(Real, s, queued, firstRefused));
        Assert.Equal(Pools.RefusedDebtCeiling, Assert.Single(Simulation.Step(Real, s, queued.Append(new BuyArms(firstRefused)).ToList()).Rejected).Reason);
        Assert.False(Pools.BorrowsPastDebtCeiling(Real, s, queued, firstRefused - 1));
        Assert.Empty(Simulation.Step(Real, s, queued.Append(new BuyArms(firstRefused - 1)).ToList()).Rejected);
        Assert.False(Pools.BorrowsPastDebtCeiling(Real, s, new Command[] { new ConvertDollars(converted) }, firstRefused));   // the conversion alone lets them pass
    }

    // #2206: for each queue on the Defence screen, the warning says "refused" for exactly the amounts End turn refuses for the
    // ceiling. Each row gives the queue and the batch End turn runs after Buy queues n Arms: the purchase takes a queued
    // purchase's slot, else goes last (SimBridge.Queue). The first refused amount is 271 - (IL 0.5m a halutz group, IL 2m
    // for industry, paid before the purchase) + 1 for the $4m (IL 1.332m), rounded down.
    [Fact]
    public void TheCeilingWarningAgreesWithEndTurnOnEveryQueue()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var hash = s.Hash();
        Command One() => new Train("tel_aviv", "halutz", 1);
        Command Ten() => new Train("tel_aviv", "halutz", 10);
        Command Four() => new ConvertDollars(4);
        // A hex the game accepts for Tel Aviv's industry, taken from the map rather than written in, so a regrid (#1796's
        // 4.4 km grid made the old (-4, 20) sea) does not turn this row into a refused order that spends nothing.
        var site = Real.Hexes.OrderBy(h => h.Q).ThenBy(h => h.R)
            .First(h => Projects.CheckCityReason(Real, s, "industry", "tel_aviv", h.Q, h.R) == null);
        Command Industry() => new StartProject("industry", "tel_aviv", site.Q, site.R);
        Command Option() => new ChooseEventOption("idf_founding", "one_army");
        var rows = new (Command[] Queued, Func<int, Command[]> EndTurn, int FirstRefused)[]
        {
            (new Command[0], n => new Command[] { new BuyArms(n) }, 271),
            (new[] { Four() }, n => new[] { Four(), new BuyArms(n) }, 272),
            (new[] { One() }, n => new[] { One(), new BuyArms(n) }, 270),
            (new[] { One(), Four() }, n => new[] { One(), Four(), new BuyArms(n) }, 271),
            (new[] { Four(), One() }, n => new[] { Four(), One(), new BuyArms(n) }, 271),
            (new[] { Ten(), Four() }, n => new[] { Ten(), Four(), new BuyArms(n) }, 267),
            (new[] { Industry(), Four() }, n => new[] { Industry(), Four(), new BuyArms(n) }, 270),
            (new[] { Option(), Four() }, n => new[] { Option(), Four(), new BuyArms(n) }, 272),
            // the option and the training use up the turn's Attention: End turn refuses the purchase for that, never for the
            // ceiling, so no amount warns here (the screen's Attention line says why)
            (new[] { Option(), One(), Four() }, n => new[] { Option(), One(), Four(), new BuyArms(n) }, int.MaxValue),
            // the queued purchase runs before the training, which then counts for nothing; the conversion still runs first
            (new[] { new BuyArms(1), One(), Four() }, n => new[] { new BuyArms(n), One(), Four() }, 272),
            (new[] { One(), new BuyArms(1), Four() }, n => new[] { One(), new BuyArms(n), Four() }, 271),
        };
        foreach (var (queued, endTurn, firstRefused) in rows)
            for (int n = 265; n <= 274; n++)
            {
                var turn = Simulation.Step(Real, s, endTurn(n));
                Assert.DoesNotContain(turn.Rejected, x => x.Command is not BuyArms);
                var refused = turn.Rejected.Any(x => x.Command is BuyArms && x.Reason == Pools.RefusedDebtCeiling);
                Assert.Equal(n >= firstRefused, refused);
                Assert.Equal(refused, Pools.BorrowsPastDebtCeiling(Real, s, queued, n));
            }
        Assert.Equal(hash, s.Hash());   // every check runs on a copy
    }

    // #2206: only the conversions queued after the turn's first Arms purchase move, to just before it, in their queue order;
    // every other order keeps its place (here, the order of the rejections).
    [Fact]
    public void OnlyTheConversionsMoveAheadOfTheFirstArmsPurchase()
    {
        var c = Gdd();
        var s = War(c);
        var r = Orders(c, s, new BuyArms(0), new ConvertDollars(0), new BuyArms(-1), new ConvertDollars(5));
        Assert.Equal(new[] { "convert_dollars 0", "convert_dollars 5", "buy_arms 0", "buy_arms -1" },
            r.Rejected.Select(x => x.Command.Kind + " " + (x.Command is ConvertDollars d ? d.Amount : ((BuyArms)x.Command).Count)
                .ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var none = Orders(c, s, new ConvertDollars(5), new ConvertDollars(0));
        Assert.Equal(new[] { 5, 0 }, none.Rejected.Select(x => ((ConvertDollars)x.Command).Amount));
    }

    const string Rates = "  il_per_dollar: [{ from: 1948-05-15, rate: 0.5, status: sourced }, { from: 1948-08-15, rate: 0.25, status: sourced }]\n";

    // R-MONEY-004: missing or out-of-range values are load errors, never a default.
    [Theory]
    [InlineData("  dollars_per_turn: 4\n", "", "mvp.dollars_per_turn", "required")]
    [InlineData("  dollars_per_turn: 4\n", "  dollars_per_turn: -1\n", "mvp.dollars_per_turn", ">= 0")]
    [InlineData("  dollars_total: 10\n", "", "mvp.dollars_total", "required")]
    [InlineData("  dollars_total: 10\n", "  dollars_total: -1\n", "mvp.dollars_total", ">= 0")]
    [InlineData(Rates, "", "mvp.il_per_dollar", "required")]
    [InlineData(Rates, "  il_per_dollar: []\n", "mvp.il_per_dollar", "required")]
    [InlineData(Rates, "  il_per_dollar: [{ from: 1948-05-15, rate: -0.1, status: sourced }]\n", "mvp.il_per_dollar[0].rate", "finite number >= 0")]
    [InlineData(Rates, "  il_per_dollar: [{ from: 1948-05-15, rate: .inf, status: sourced }]\n", "mvp.il_per_dollar[0].rate", "finite number >= 0")]
    [InlineData(Rates, "  il_per_dollar: [{ from: 1948-05-16, rate: 0.5, status: sourced }]\n", "mvp.il_per_dollar[0].from", "on or before the start")]
    [InlineData(Rates, "  il_per_dollar: [{ from: 1948-05-15, rate: 0.5, status: sourced }, { from: 1948-05-15, rate: 0.4, status: sourced }]\n", "mvp.il_per_dollar[1].from", "after the previous")]
    public void MissingOrBadValuesAreLoadErrors(string line, string replacement, string field, string message)
    {
        var files = Fixture.Gdd();
        Assert.Contains(line, files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace(line, replacement);
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Contains(field, e.Message);
        Assert.Contains(message, e.Message);
    }

    /// <summary>The #813 prototype's run: real content, seed 1948, Historical, every card delegated (no orders but the
    /// conversion), converting <paramref name="perTurn"/> each turn that starts at rung 4 or 5. Returns the debt at the end
    /// of the turn ending 15 April 1949, the end state, the first turn end with the currency crisis on and the dollars converted.</summary>
    static (double April1949, GameState End, DateTime? Crisis, int Converted) Prototype(int perTurn)
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        double? april = null; DateTime? crisis = null;
        for (int turn = 0; turn < 1000 && s.GameOver == null; turn++)
        {
            var orders = perTurn > 0 && s.Rung >= 4 ? new Command[] { new ConvertDollars(perTurn) } : Array.Empty<Command>();
            var r = Simulation.Step(Real, s, orders);
            Assert.DoesNotContain(r.Rejected, j => !WarEvents1948Tests.IsTheMalkiyaRefusal(j));   // R-ENEMY-005: Hiram's refused order, pinned there
            s = r.State;
            if (s.Date == new DateTime(1949, 4, 15)) april = s.Debt;
            if (crisis == null && s.ActiveSituations.Contains(EconomySystem.FxCrisis)) crisis = s.Date;
        }
        return (april!.Value, s, crisis, s.DollarsConverted);
    }

    // R-MONEY-004, #813's measurement (https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6564855158):
    // with no lever the debt is IL 40.59m on 15 April 1949 and IL 180.44m at the end, the first crisis on 15 December 1951.
    // The prototype converted $4m a month in the war (11 turns, $44m) at IL 0.36 per $: IL 15.84m less debt (24.75 and
    // 164.60) and the crisis on 15 March 1951. The game converts at the sourced war-time rate, IL 0.333 per $ (#813
    // comment 6565039439). Every war turn ends with the Treasury at 0 and a deficit larger than the conversion, so each
    // IL converted is an IL not borrowed: the debt falls by exactly 44 * 0.333 = IL 14.652m. The foreign currency is the
    // prototype's, so the crisis still comes on 15 March 1951.
    // #813 measured on the 8.7 km hexes. On the 4.4 km hexes (#1183) the war's engagements fall elsewhere, so the formations
    // lost and the turns they are lost on change, and with them mvp.military_upkeep_per_turn (IL 0.05m a formation a turn).
    // R-ENEMY-007 changes this deliberately again: this no-orders run leaves required formations out of place, so its
    // operations postpone and lapse. It now ends at IL 30.69m on 15 April 1949 and IL 170.54m; the conversion ends at
    // IL 16.04m and IL 155.89m. The crisis dates and the IL 14.652m saved are unchanged.
    [Fact]
    public void TheWarOnlyRunReproducesThePrototypeAtTheSourcedRate()
    {
        var none = Prototype(0);
        Assert.Equal((30.69, 170.54), (Math.Round(none.April1949, 2), Math.Round(none.End.Debt, 2)));
        Assert.Equal(new DateTime(1951, 12, 15), none.Crisis);
        var sold = Prototype(4);
        Assert.Equal(44, sold.Converted);
        var saved = 44 * 0.333;
        Assert.Equal(none.April1949 - saved, sold.April1949, 9);
        Assert.Equal(none.End.Debt - saved, sold.End.Debt, 9);
        Assert.Equal((16.04, 155.89), (Math.Round(sold.April1949, 2), Math.Round(sold.End.Debt, 2)));
        Assert.Equal(new DateTime(1951, 3, 15), sold.Crisis);
    }
}
