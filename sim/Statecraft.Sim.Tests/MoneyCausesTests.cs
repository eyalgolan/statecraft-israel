using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2199: the causes of the Money (net) meter (Treasury minus Debt, the top bar, the turn report and the meter
/// tip) are the turn's lines that <see cref="EconomySystem.MovesMoney"/> picks, the filter the HUD uses. Together they add
/// up to the meter's change: the Treasury lines, job income, and each payment's whole cost (one line, borrowed share
/// included). A borrowing line (Target debt) is not one: it moves the Treasury and the Debt by the same amount.</summary>
public class MoneyCausesTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The tolerance in IL millions. The sim adds the same amounts to the Treasury and the Debt as the causes carry,
    /// but in a different order and association (the Treasury starts at IL 20m and takes each term in turn; a payment takes
    /// the whole cost, then moves the shortfall to Debt), so exact equality does not hold: the gap is 2.7e-15 on the opening
    /// turn and 2.3e-13 with a purchase of 999 Arms (measured before this test bought 200 for #2203's debt ceiling), and every test here passes at 1e-12 (measured, #2199). 1e-9 leaves room above
    /// that rounding and is far below the smallest amount a line shows (IL 0.1m), so a missing or doubled line always fails.</summary>
    const double Tolerance = 1e-9;

    static void AssertAddsUp(GameState before, StepResult r, string what)
    {
        var lines = r.Why.Where(EconomySystem.MovesMoney).ToList();
        var sum = lines.Sum(w => w.Delta);
        var change = (r.State.Treasury - r.State.Debt) - (before.Treasury - before.Debt);
        string N(double v) => v.ToString("0.000###", CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(sum - change) <= Tolerance,
            $"{what}: the Money causes add up to {N(sum)}, the change in Treasury minus Debt is {N(change)} (gap {N(change - sum)}); "
            + "unlisted: " + string.Join("; ", r.Why.Where(w => !EconomySystem.MovesMoney(w) && w.Message.Contains("IL")).Select(w => $"[{w.Key}] {w.Message}"))
            + " | listed: " + string.Join("; ", lines.Select(w => $"[{w.Key}] {N(w.Delta)}")));
    }

    static GameState Start() => Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);

    // R-CITY-002, R-MONEY-001: a turn with no orders: tax and spending, military upkeep and the cities' job income.
    [Fact]
    public void ATurnWithNoOrdersAddsUp()
    {
        var s = Start();
        for (int t = 1; t <= 6; t++)
        {
            var r = Simulation.Step(Real, s, Array.Empty<Command>());
            Assert.Empty(r.Rejected);
            AssertAddsUp(s, r, $"turn {t}");
            Assert.Contains(r.Why, w => w.Key == CauseKeys.WagesPaid && EconomySystem.MovesMoney(w));
            s = r.State;
        }
    }

    // R-ARMS-002, R-MONEY-003: Arms bought for more than the Treasury holds: part paid in cash, the rest borrowed. 200 Arms
    // cost IL 200m against the opening IL 20m, so IL 180m is borrowed, under the debt ceiling #2203 adds (IL 250m).
    [Fact]
    public void APurchasePaidPartlyInCashAndPartlyBorrowedAddsUp()
    {
        const int count = 200;
        var s = Start();
        Assert.True(count * Real.RulesAt(s.Date).Mvp!.ArmsPrice!.Value > s.Treasury, "the purchase costs more than the Treasury holds");
        var r = Simulation.Step(Real, s, new Command[] { new BuyArms(count) });
        Assert.Empty(r.Rejected);
        Assert.True(r.State.Debt > s.Debt, "the purchase borrows");
        AssertAddsUp(s, r, $"BuyArms({count})");
        // The whole cost is one Money line; the borrowing lines (the purchase's and the deficit's) are not Money causes.
        var paid = Assert.Single(r.Why, w => w.Key == CauseKeys.Paid);
        Assert.Equal(("treasury", -count * Real.RulesAt(s.Date).Mvp!.ArmsPrice!.Value, CauseKeys.Purpose.Arms), (paid.Target, paid.Delta, paid.Args[0]));
        Assert.Contains(r.Why, w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Arms);
        Assert.DoesNotContain(r.Why.Where(EconomySystem.MovesMoney), w => w.Target == "debt");
    }

    // R-TRAIN-001, R-MONEY-003: a training order paid from the Treasury.
    [Fact]
    public void ATrainingOrderAddsUp()
    {
        var s = Start();
        var r = Simulation.Step(Real, s, new Command[] { new Train("tel_aviv", "halutz", 2) });
        Assert.Empty(r.Rejected);
        AssertAddsUp(s, r, "Train(tel_aviv, halutz, 2)");
    }

    /// <summary>The real content never builds a map action: the GDD loop, which runs in every era, refuses them all
    /// (R-PROJ-004, MapActions.Check's gdd_loop reason). MapActions.Apply still runs in the legacy loop (D-E1), so these use
    /// the legacy fixture on a day its map actions are open, as EraSystemsTests does, and its housing hex.</summary>
    static readonly Content Legacy = ContentLoader.Load(Fixture.Write());

    static GameState MapActionDay()
    {
        var s = Simulation.NewGame(Legacy, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);   // the fixture's map_actions_from
        return s;
    }

    static readonly MapAction Housing = new MapAction(MapActions.Housing, 2, 0);

    // R-MONEY-001: a map action's cost is paid from the Treasury, so it is a Money line, before the build line.
    [Fact]
    public void AMapActionPaidInCashAddsUp()
    {
        Assert.Equal("Map actions are not part of this game: start a city project instead", MapActions.Check(Real, Start(), MapActions.Housing, 2, 0));
        var s = MapActionDay();
        s.Treasury = 100;   // more than the cost, so nothing is borrowed for it
        AssertAddsUp(s, Simulation.Step(Legacy, s, Array.Empty<Command>()), "the same turn with no orders");
        var r = Simulation.Step(Legacy, s, new Command[] { Housing });
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.MapBuilt);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.MapBorrowed);
        AssertAddsUp(s, r, "MapAction(housing) in cash");
        var cost = Legacy.RulesAt(s.Date).MapActions[MapActions.Housing].Cost;
        var paid = Assert.Single(r.Why, w => w.Key == CauseKeys.Paid);
        Assert.Equal(("treasury", -cost, CauseKeys.Purpose.MapAction, MapActions.Housing), (paid.Target, paid.Delta, paid.Args[0], paid.Args[1]));
    }

    // R-MONEY-003: a map action with the Treasury short: its cost is one Money line, and the borrowing (MapBorrowed) is not.
    [Fact]
    public void AMapActionWithTheTreasuryShortAddsUp()
    {
        var s = MapActionDay();
        s.Treasury = 0;
        var r = Simulation.Step(Legacy, s, new Command[] { Housing });
        Assert.Empty(r.Rejected);
        AssertAddsUp(s, r, "MapAction(housing) borrowed");
        var borrowed = Assert.Single(r.Why, w => w.Key == CauseKeys.MapBorrowed);
        Assert.False(EconomySystem.MovesMoney(borrowed));
        var paid = Assert.Single(r.Why, w => w.Key == CauseKeys.Paid);
        var why = r.Why.ToList();
        Assert.True(why.IndexOf(paid) < why.IndexOf(borrowed), "the Money line comes before its borrowing, as PayOrBorrow orders them");
    }

    // R-MONEY-002, R-MONEY-003: civilian upkeep writes its own Money line (EconomySystem.PayCivilianUpkeep), so it borrows
    // through PayNamedOrBorrow and is counted once, paid in cash or borrowed. Every real rate is 0
    // (MoneyTests.TheMvpChargesNoCivilianUpkeep), so this uses the GDD fixture with a city upkeep, as MoneyTests does.
    [Theory]
    [InlineData(1.0, 100.0)]      // paid in cash
    [InlineData(1000.0, 0.0)]     // more than the Treasury holds after the turn's income: borrowed
    public void CivilianUpkeepIsOneMoneyLine(double upkeepPerYear, double treasury)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("city_upkeep_per_year: 0", FormattableString.Invariant($"city_upkeep_per_year: {upkeepPerYear}"));
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = treasury;
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Empty(r.Rejected);
        AssertAddsUp(s, r, $"civilian upkeep {upkeepPerYear} a year, Treasury {treasury}");
        Assert.Single(r.Why, w => w.Key == CauseKeys.CivilianUpkeep);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.Paid);
        Assert.Equal(treasury == 0, r.Why.Any(w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.CivilianUpkeep));
    }

    // R-CTRL-018, R-MONEY-003: a Deport writes its own Money line (CauseKeys.DeportCost), so it is counted once. Deport is not
    // in play in the real content (mvp.deport_in_play: false), so this uses DeportTests' fixture, whose IL 121m cost borrows.
    [Fact]
    public void ADeportIsOneMoneyLine()
    {
        var c = DeportTests.Load();
        var s = DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0));
        var r = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", SettlementControl.Deport) });
        Assert.Empty(r.Rejected);
        AssertAddsUp(s, r, "Deport of town_a");
        Assert.Single(r.Why, w => w.Key == CauseKeys.DeportCost);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.Paid);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Deport);
    }

    // R-MONEY-003: every turn of the reference player's game, with its orders, events, upkeep and borrowing.
    [Fact]
    public void EveryTurnOfTheReferenceRunAddsUp()
    {
        var s = Start();
        for (int t = 1; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(Real, s, GoldenRun.Orders(Real, s));
            AssertAddsUp(s, r, $"turn {t} ({s.Date:yyyy-MM-dd})");
            s = r.State;
        }
    }
}
