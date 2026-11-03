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

    // R-MONEY-003: every turn of the reference player's game (gdd-0.1), with its orders, events, upkeep and borrowing.
    [Fact]
    public void EveryTurnOfTheReferenceRunAddsUp()
    {
        var s = Start();
        for (int t = 1; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(Real, s, GoldenRun.Orders(Real, s));
            AssertAddsUp(s, r, $"turn {t} ({s.Date:yyyy-MM-dd})");
            Gdd03MoneyLedgerTests.AssertReconciles(s, r, $"turn {t} ({s.Date:yyyy-MM-dd})");   // #1702: the Debt lines and the floor too
            s = r.State;
        }
    }

    /// <summary>#2212: the Money causes the turn report and the meter tip show, as HudModel.Causes picks them: the largest
    /// <paramref name="max"/> lines MovesMoney picks (not zero), the larger first, the sim's order breaking a tie.</summary>
    static List<Cause> Largest(IReadOnlyList<Cause> why, int max) =>
        why.Select((w, i) => (w, i)).Where(x => EconomySystem.MovesMoney(x.w) && x.w.Delta != 0)
            .OrderByDescending(x => Math.Abs(x.w.Delta)).ThenBy(x => x.i).Take(max).Select(x => x.w).ToList();

    /// <summary>The two lines shown, then the Other line when there is one (EconomySystem.OtherMoney).</summary>
    static List<Cause> Shown(Content c, IReadOnlyList<Cause> why)
    {
        var shown = Largest(why, 2);
        if (EconomySystem.OtherMoney(c, why, shown) is Cause other) shown.Add(other);
        return shown;
    }

    // #2212: with more Money causes than the two shown, one Other line holds the rest, so the lines shown add up to the
    // Money (net) change. The opening turn has tax and spending, military upkeep and several cities' job income.
    [Fact]
    public void TheTwoLargestAndOneOtherLineAddUpToTheChange()
    {
        var s = Start();
        var r = Simulation.Step(Real, s, Array.Empty<Command>());
        Assert.True(r.Why.Count(w => EconomySystem.MovesMoney(w) && w.Delta != 0) >= 3, "the opening turn has 3 or more Money causes");
        var largest = Largest(r.Why, 2);
        var other = EconomySystem.OtherMoney(Real, r.Why, largest);
        Assert.NotNull(other);
        var rest = r.Why.Where(w => EconomySystem.MovesMoney(w) && !largest.Contains(w)).Sum(w => w.Delta);
        Assert.Equal(("economy", "treasury", CauseKeys.MoneyOther, rest), (other!.System, other.Target, other.Key, other.Delta));
        Assert.Equal($"{PlayerText.Change(Real, "treasury", rest)} (other causes)", other.Message);
        Assert.DoesNotContain(other, r.Why);   // the UI's line, never a line of the turn
        var change = (r.State.Treasury - r.State.Debt) - (s.Treasury - s.Debt);
        Assert.True(Math.Abs(largest.Sum(w => w.Delta) + other.Delta - change) <= Tolerance, "the two lines and Other add up to the change");
    }

    // #2212: no Other line when every Money cause is shown (two or fewer), or when the rest rounds to IL 0.0m as shown.
    [Theory]
    [InlineData(new double[0], false)]
    [InlineData(new[] { -3.0 }, false)]
    [InlineData(new[] { -3.0, 2.0 }, false)]
    [InlineData(new[] { -3.0, 2.0, 0.04 }, false)]    // IL +0.0m as shown: left out
    [InlineData(new[] { -3.0, 2.0, -0.04 }, false)]
    [InlineData(new[] { -3.0, 2.0, 0.03, 0.01 }, false)]
    [InlineData(new[] { -3.0, 2.0, 0.05 }, true)]     // IL +0.1m as shown
    [InlineData(new[] { -3.0, 2.0, 0.03, 0.03 }, true)]
    [InlineData(new[] { -3.0, 2.0, 1.0, -1.0 }, false)]  // the rest cancels out
    public void NoOtherLineWhenNothingIsLeftOver(double[] deltas, bool expected)
    {
        var why = deltas.Select(d => new Cause("economy", "treasury", d, PlayerText.Change(Real, "treasury", d), key: CauseKeys.CivilianUpkeep))
            .Append(new Cause("economy", "debt", 5, "borrowed IL\u00A05.0m to cover the deficit", key: CauseKeys.Borrowed))   // not a Money cause
            .ToList();
        var largest = Largest(why, 2);
        var other = EconomySystem.OtherMoney(Real, why, largest);
        Assert.Equal(expected, other != null);
        if (other != null) Assert.Equal(deltas.Sum() - largest.Sum(w => w.Delta), other.Delta, 12);
    }

    // #2212: on every turn of the reference run, the lines shown add up to the Money (net) change, but for a rest the
    // Other line leaves out because it shows as IL 0.0m (under IL 0.05m). At most 3 lines.
    [Fact]
    public void EveryTurnOfTheReferenceRunShowsLinesThatAddUp()
    {
        var s = Start(); var others = 0;
        for (int t = 1; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(Real, s, GoldenRun.Orders(Real, s));
            var shown = Shown(Real, r.Why);
            Assert.True(shown.Count <= 3, $"turn {t}: {shown.Count} lines");
            var change = (r.State.Treasury - r.State.Debt) - (s.Treasury - s.Debt);
            var gap = Math.Abs(shown.Sum(w => w.Delta) - change);
            Assert.True(shown.Count == 3 ? gap <= Tolerance : gap < 0.05 + Tolerance, $"turn {t}: gap {gap}");
            if (shown.Count == 3) others++;
            s = r.State;
        }
        Assert.True(others > 0, "the reference run shows an Other line");
    }

    // #2212 review, H2: the paying purposes no other test here plays through Simulation.Step (the reference run pays for
    // training, yishuvim, plowing, city projects and an event; APurchase... pays for Arms; the map action, civilian upkeep
    // and Deport tests above). Each is played with the Treasury empty, so it borrows: its cost is exactly one Money line,
    // its caller writes no Treasury line of its own beside it, and the lines add up.
    [Theory]
    [InlineData(CauseKeys.Purpose.Corridor)]
    [InlineData(CauseKeys.Purpose.TrainingCamp)]
    [InlineData(CauseKeys.Purpose.Drain)]
    [InlineData(CauseKeys.Purpose.Prisoners)]
    [InlineData(CauseKeys.Purpose.Occupation)]
    public void EveryPayingPurposeAddsUpThroughStep(string purpose)
    {
        Content c; GameState s; Command[] orders;
        string Site(string id, string kind) => Fixture.GddSites + Fixture.GddSites.Replace("sites:\n", "").Replace("site_a", id).Replace("Site A", id)
            .Replace("q: 2", "q: 3").Replace("kinds: [yishuv]", $"kinds: [{kind}]").Replace("    founded: 1949-06-12\n", kind == "swamp" ? "" : "    city: town_a\n");   // only a camp site names its city
        GameState WithHalutz(Content content, out int id)
        {
            var g = Simulation.NewGame(content, 1, Mode.Historical);
            id = g.NextId++;
            g.Units.Add(id, new Unit(id, "halutz", 1, 0));
            return g;
        }
        Content Gdd(string? sites = null) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(sites == null ? null : new Dictionary<string, string>
        {
            ["cities.yaml"] = "\ncities:\n" + Fixture.City("town_a", 12, 8, 10, 6),
            ["sites.yaml"] = sites,
        })));
        switch (purpose)
        {
            case CauseKeys.Purpose.Corridor:
                c = Gdd(); s = WithHalutz(c, out var a); orders = new Command[] { new PioneerWork(a, Corridors.BuildRoad, "road_a:1") }; break;
            case CauseKeys.Purpose.TrainingCamp:
                c = Gdd(Site("camp_a", "military_camp")); s = WithHalutz(c, out var b); orders = new Command[] { new PioneerWork(b, Projects.BuildCamp, "camp_a") }; break;
            case CauseKeys.Purpose.Drain:
                c = Gdd(Site("marsh", "swamp")); s = WithHalutz(c, out var d); orders = new Command[] { new PioneerWork(d, Projects.DrySwamp, "marsh") }; break;
            case CauseKeys.Purpose.Prisoners:
                c = Gdd(); s = Simulation.NewGame(c, 1, Mode.Historical); s.SoldiersHeld["egypt"] = 2; orders = Array.Empty<Command>(); break;
            default:
                c = DeportTests.Load(); s = DeportTests.Awaiting(c, DeportTests.Soldier(1, 1, 0));
                orders = new Command[] { new ResolveCapture("town_a", SettlementControl.Occupy) }; break;
        }
        s.Treasury = 0;
        var r = Simulation.Step(c, s, orders);
        Assert.Empty(r.Rejected);
        AssertAddsUp(s, r, purpose);
        var paid = Assert.Single(r.Why, w => EconomySystem.MovesMoney(w) && w.Key == CauseKeys.Paid && w.Args[0] == purpose);
        Assert.True(paid.Delta < 0, $"{purpose}: the cost is paid");
        Assert.DoesNotContain(r.Why, w => EconomySystem.MovesMoney(w) && w.System != "economy" && w.System != "city" && w.System != "events");
    }

    // #2212 review, H1 and the suggested test 1: what the reference run pays and borrows for, so a test that "adds up on
    // every turn" is not vacuous. Measured at the #2212 head: 44 turns, none refused; Paid for training 1, yishuv 6,
    // project 5, plow 2; Borrowed for the deficit 41, yishuv 6, project 5, plow 2, an event 1. Every Debt line is a loan
    // (Borrowed or MapBorrowed), which moves the Treasury and the Debt alike; a future interest or relief line fails here.
    [Fact]
    public void TheReferenceRunExercisesTheMoneyPaths()
    {
        var s = Start(); int turns = 0;
        var paid = new HashSet<string>(); var borrowed = new HashSet<string>(); var keys = new HashSet<string>();
        for (int t = 1; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(Real, s, GoldenRun.Orders(Real, s));
            Assert.DoesNotContain(r.Rejected, j => !WarEvents1948Tests.IsTheMalkiyaRefusal(j));   // R-ENEMY-005: Hiram's refused order, pinned there
            foreach (var w in r.Why)
            {
                if (w.Key == CauseKeys.Paid) paid.Add(w.Args[0]);
                if (w.Key == CauseKeys.Borrowed) borrowed.Add(w.Args[0]);
                if (EconomySystem.MovesMoney(w)) keys.Add(w.Key);
                if (w.Target == "debt") Assert.Contains(w.Key, new[] { CauseKeys.Borrowed, CauseKeys.MapBorrowed });
            }
            s = r.State; turns++;
        }
        Assert.True(turns >= 40, $"the run lasts {turns} turns");
        Assert.Subset(paid, new HashSet<string> { CauseKeys.Purpose.Training, CauseKeys.Purpose.Yishuv, CauseKeys.Purpose.Project, CauseKeys.Purpose.Plow });
        Assert.Subset(borrowed, new HashSet<string> { CauseKeys.Purpose.Deficit, CauseKeys.Purpose.Event, CauseKeys.Purpose.Yishuv });
        Assert.Subset(keys, new HashSet<string> { CauseKeys.MoneyFlows, CauseKeys.WagesPaid, CauseKeys.MilitaryUpkeep, CauseKeys.Paid, CauseKeys.EventAmount });
    }

    // #2212 review, H3 and the suggested test 3: costs paid at the debt ceiling (mvp.debt_ceiling, IL 250m). PayOrBorrow
    // has no cap: upkeep borrows past it, and the lines add up. A purchase up to the ceiling is paid; one past it is
    // refused once #2203 lands (Pools.BuyArms, before PayOrBorrow), and then writes no Money line. Either way a Paid
    // line exists only with its Treasury move, and the lines add up.
    [Theory]
    [InlineData(0)]   // no orders: military upkeep and the deficit borrow past the ceiling
    [InlineData(1)]   // borrows IL 1.0m, to exactly the ceiling
    [InlineData(2)]   // borrows past it
    public void CostsPaidAtTheDebtCeilingAddUp(int arms)
    {
        var s = Start();
        var ceiling = Real.RulesAt(s.Date).Mvp!.DebtCeiling!.Value;
        Assert.Equal(1.0, Real.RulesAt(s.Date).Mvp!.ArmsPrice!.Value);
        s.Debt = ceiling - 1; s.Treasury = 0;
        var r = Simulation.Step(Real, s, arms == 0 ? Array.Empty<Command>() : new Command[] { new BuyArms(arms) });
        AssertAddsUp(s, r, $"BuyArms({arms}) at the ceiling");
        Assert.True(r.State.Debt > ceiling, "the turn's upkeep borrows past the ceiling: PayOrBorrow has no cap");
        var bought = r.Rejected.Count == 0 && arms > 0;
        if (arms == 1) Assert.True(bought, "a purchase to exactly the ceiling is paid");
        var lines = r.Why.Where(w => w.Key == CauseKeys.Paid).ToList();
        Assert.Equal(bought ? 1 : 0, lines.Count);
        if (bought) Assert.Equal((-(double)arms, CauseKeys.Purpose.Arms), (lines[0].Delta, lines[0].Args[0]));
        Assert.Equal(s.Arms + (bought ? arms : 0), r.State.Arms);
    }
}
