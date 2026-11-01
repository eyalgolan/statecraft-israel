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
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.DollarsConverted);
        Assert.Equal(("economy", "treasury", 1.5), (line.System, line.Target, line.Delta));
        Assert.Equal(new[] { "3" }, line.Args);
        Assert.Contains(CauseKeys.DollarsConverted, CauseKeys.All);   // so the UI words it (CauseText)
        Assert.Equal("Converted $3.0m of foreign currency into Money: " + PlayerText.MoneyChange(c, "treasury", 1.5), line.Message);
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
        Assert.True(none.Debt > 0 && none.Debt < 0.5, $"the fixture's turn borrows under IL 0.5m ({none.Debt})");
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
        // The limit is per turn: the next turn may convert its own dollars_per_turn again.
        var next = Simulation.Step(c, s, new Command[] { new ConvertDollars(4) }).State;
        Assert.Equal(5, next.Rung);
        Assert.Empty(Orders(c, next, new ConvertDollars(4)).Rejected);
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
    [Theory]
    [InlineData(0, 50, 0, 5)]    // below the minimum
    [InlineData(1, 50, 0, 3)]    // outside the war
    [InlineData(5, 50, 0, 5)]    // past the month's limit
    [InlineData(3, 50, 8, 5)]    // past the total
    [InlineData(3, 2.5, 0, 5)]   // past the reserves
    public void ARefusalLeavesTheStateUntouched(int amount, double fx, int converted, int rung)
    {
        var c = Gdd();
        var s = War(c, fx, converted);
        s.Rung = rung;
        var refused = Simulation.Step(c, s, new Command[] { new ConvertDollars(amount) });
        Assert.Single(refused.Rejected);
        Assert.Equal(Simulation.Step(c, s, Array.Empty<Command>()).State.Hash(), refused.State.Hash());
        Assert.DoesNotContain(refused.Why, w => w.Key == CauseKeys.DollarsConverted);
        var preview = Orders(c, s, new ConvertDollars(amount));
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
            Assert.Empty(r.Rejected);
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
    // IL converted is an IL not borrowed: the debt falls by exactly 44 * 0.333 = IL 14.652m, to IL 25.94m on 15 April 1949
    // and IL 165.79m at the end. The foreign currency is the prototype's, so the crisis still comes on 15 March 1951.
    [Fact]
    public void TheWarOnlyRunReproducesThePrototypeAtTheSourcedRate()
    {
        var none = Prototype(0);
        Assert.Equal((40.59, 180.44), (Math.Round(none.April1949, 2), Math.Round(none.End.Debt, 2)));
        Assert.Equal(new DateTime(1951, 12, 15), none.Crisis);
        var sold = Prototype(4);
        Assert.Equal(44, sold.Converted);
        var saved = 44 * 0.333;
        Assert.Equal(none.April1949 - saved, sold.April1949, 9);
        Assert.Equal(none.End.Debt - saved, sold.End.Debt, 9);
        Assert.Equal((25.94, 165.79), (Math.Round(sold.April1949, 2), Math.Round(sold.End.Debt, 2)));
        Assert.Equal(new DateTime(1951, 3, 15), sold.Crisis);
    }
}
