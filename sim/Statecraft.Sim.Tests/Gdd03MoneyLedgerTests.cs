using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1702 (GDD 0.3, decision D1, owner, 1 November 2026, "Approve all six (Recommended)"): gdd-0.3 keeps main's
/// economy for the MVP, so its Money ledger is the one gdd-0.1 has (R-MONEY-001..003). These run the shipped content with
/// only eras.yaml's ruleset switched to gdd-0.3, as HistoricalPopulationTests does. "Money" here is the Treasury
/// (R-MONEY-001): it stops at 0 and every shortfall is borrowed into Debt (R-MONEY-003), so the top bar's net Money
/// (Treasury minus Debt) goes below 0 and the Treasury never does.</summary>
public class Gdd03MoneyLedgerTests
{
    static readonly Content Real03 = Load();

    static Content Load()
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(root, rel);
            var text = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            return rel == "eras.yaml" ? text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + System.IO.Path.GetFileName(p)));
    }

    /// <summary>MoneyCausesTests' tolerance (IL millions): the sim adds the causes' amounts in a different order.</summary>
    const double Tolerance = 1e-9;

    static string N(double v) => v.ToString("0.000###", CultureInfo.InvariantCulture);

    /// <summary>The ledger of one step from <paramref name="before"/>: (a) the Money causes (EconomySystem.MovesMoney) add up
    /// to the change in Treasury minus Debt; (b) the Debt lines (Target debt) add up to the change in Debt, so no Debt moves
    /// without a line; (c) the Treasury ends at 0 or more.</summary>
    internal static void AssertReconciles(GameState before, StepResult r, string what)
    {
        var money = r.Why.Where(EconomySystem.MovesMoney).Sum(w => w.Delta);
        var net = (r.State.Treasury - r.State.Debt) - (before.Treasury - before.Debt);
        Assert.True(Math.Abs(money - net) <= Tolerance, $"{what}: the Money causes add up to {N(money)}, Treasury minus Debt moved {N(net)}");
        var debt = r.Why.Where(w => w.Target == "debt").Sum(w => w.Delta);
        Assert.True(Math.Abs(debt - (r.State.Debt - before.Debt)) <= Tolerance, $"{what}: the Debt lines add up to {N(debt)}, the Debt moved {N(r.State.Debt - before.Debt)}");
        Assert.True(r.State.Treasury >= 0, $"{what}: the Treasury ends at {N(r.State.Treasury)}");
    }

    /// <summary>The ledger run's orders (a test policy, not game AI: the reference player is gdd-0.1 only): HistoricalAI's
    /// (every event's historical option, so the events' Treasury amounts), one halutz group in training when there is none,
    /// one housing project when none is being built, each idle halutz to the first free yishuv site, 5 Arms every fourth
    /// turn (refused past the debt ceiling, R-ARMS-002) and $1m converted while the war allows it (R-MONEY-004).</summary>
    static List<Command> Orders(Content c, GameState s)
    {
        var list = new List<Command>(HistoricalAI.Commands(c, s));
        if (!s.Units.Values.Any(u => Projects.IsPioneer(u.Type)) && !s.Cohorts.Values.Any(k => k.Type == "halutz"))
        {
            var halutz = c.UnitTypes.First(t => t.Id == "halutz");
            var city = s.Cities.Values.FirstOrDefault(x => Training.Idle(x) >= 1 && Training.HasFacility(c, s, x.Id, halutz.Facility));
            if (city != null) list.Add(new Train(city.Id, halutz.Id, 1));
        }
        if (!s.CityProjects.Values.Any(p => p.BuiltOn == null))
        {
            var start = (from city in s.Cities.Values
                         let home = c.SettlementHexes.First(h => h.Id == city.Id)
                         from q in Enumerable.Range(home.Q - 2, 5)
                         from r in Enumerable.Range(home.R - 2, 5)
                         where Projects.CheckCity(c, s, MapActions.Housing, city.Id, q, r) == null
                         select new StartProject(MapActions.Housing, city.Id, q, r)).FirstOrDefault();
            if (start != null) list.Add(start);
        }
        var taken = new HashSet<string>(s.Sites.Keys, StringComparer.Ordinal);
        foreach (var u in s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)))
        {
            var site = c.Sites.Where(x => x.Kinds.Contains(Projects.YishuvKind) && !taken.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
            if (site == null) break;
            list.Add(new PioneerWork(u.Id, Projects.BuildYishuv, site.Id));
            taken.Add(site.Id);
        }
        if (s.Turn % 4 == 0) list.Add(new BuyArms(5));
        if (EconomySystem.DollarsRefusal(c, s, 1) == null) list.Add(new ConvertDollars(1));
        return list;
    }

    /// <summary>Plays a whole gdd-0.3 game with <see cref="Orders"/>, calling <paramref name="turn"/> with each turn's start,
    /// orders and result; returns the number of turns.</summary>
    static int Play(Action<int, GameState, List<Command>, StepResult> turn)
    {
        var s = Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical);
        int t = 0;
        while (t < 1000 && s.GameOver == null)
        {
            var orders = Orders(Real03, s);
            var r = Simulation.Step(Real03, s, orders);
            turn(++t, s, orders, r);
            s = r.State;
        }
        return t;
    }

    // R-MONEY-001, R-MONEY-002, R-MONEY-003, #1702 Done-when 1: every turn of a whole gdd-0.3 game, with the orders that cost Money, the
    // events, upkeep, the deficit and its borrowing, reconciles: start Money plus the turn's lines is end Money, the Debt
    // lines are the Debt's change, and the Treasury never ends below 0. The only order refused is Arms past the ceiling.
    [Fact]
    public void EveryTurnOfAWholeGdd03GameReconciles()
    {
        Assert.Equal(SaveGame.Gdd03Ruleset, Real03.Ruleset);
        var paid = new HashSet<string>(); var borrowed = new HashSet<string>(); var keys = new HashSet<string>();
        var turns = Play((t, s, orders, r) =>
        {
            foreach (var x in r.Rejected)
                Assert.True(x.Command is BuyArms && x.Reason == Pools.RefusedDebtCeiling, $"turn {t}: {x.Command.Kind} refused: {x.Reason}");
            AssertReconciles(s, r, $"turn {t} ({s.Date:yyyy-MM-dd})");
            foreach (var w in r.Why)
            {
                if (w.Key == CauseKeys.Paid) paid.Add(w.Args[0]);
                if (w.Key == CauseKeys.Borrowed) borrowed.Add(w.Args[0]);
                if (EconomySystem.MovesMoney(w)) keys.Add(w.Key);
            }
        });
        // Not vacuous: the run lasts, pays for each kind of order, borrows for them and the deficit, and converts dollars.
        Assert.True(turns >= 40, $"the run lasts {turns} turns");
        Assert.Subset(paid, new HashSet<string> { CauseKeys.Purpose.Training, CauseKeys.Purpose.Project, CauseKeys.Purpose.Yishuv, CauseKeys.Purpose.Arms });
        Assert.Subset(borrowed, new HashSet<string> { CauseKeys.Purpose.Deficit, CauseKeys.Purpose.Project, CauseKeys.Purpose.Yishuv, CauseKeys.Purpose.Arms, CauseKeys.Purpose.Event });
        Assert.Subset(keys, new HashSet<string> { CauseKeys.MoneyFlows, CauseKeys.WagesPaid, CauseKeys.MilitaryUpkeep, CauseKeys.Paid, CauseKeys.EventAmount, CauseKeys.DollarsConverted });
        // R-MONEY-002: civilian upkeep stays IL 0m under gdd-0.3 (the owner's 30 October 2026 "Keep ruling" on UPKEEP), so the
        // whole game, with its built yishuvim and housing, never charges it.
        Assert.DoesNotContain(CauseKeys.CivilianUpkeep, keys);
    }

    // R-MONEY-003, #1702 Done-when 2: on every turn of the same game, each order given alone with the Treasury empty leaves
    // the Treasury at 0 or more (the cost is borrowed), and an order refused leaves the state as no order would.
    [Fact]
    public void NoGdd03OrderLeavesTheTreasuryBelowZero()
    {
        var kinds = new HashSet<string>();
        Play((t, s, orders, r) =>
        {
            var empty = s.Clone(); empty.Treasury = 0;
            var none = Simulation.Preview(Real03, empty, Array.Empty<Command>()).State.Describe();
            foreach (var order in orders)
            {
                var p = Simulation.Preview(Real03, empty, new[] { order });
                Assert.True(p.State.Treasury >= 0, $"turn {t}: {order.Kind} leaves the Treasury at {N(p.State.Treasury)}");
                if (p.Rejected.Count > 0) Assert.Equal(none, p.State.Describe());
                else kinds.Add(order.Kind);
            }
        });
        Assert.Subset(kinds, new HashSet<string> { Training.TrainKind, Projects.StartProjectKind, Projects.PioneerKind, Pools.BuyArmsKind,
            EconomySystem.ConvertDollarsKind, "choose_event_option" });
    }

    /// <summary>The start of a gdd-0.3 game with the Treasury at <paramref name="treasury"/> and the Debt at <paramref name="debt"/>.</summary>
    static GameState Start(double treasury, double debt = 0)
    {
        var s = Simulation.NewGame(Real03, GoldenRun.Seed, Mode.Historical);
        s.Treasury = treasury; s.Debt = debt;
        return s;
    }

    /// <summary>A refused order of each kind that costs Money, in a game whose debt is at the ceiling (IL 250m).</summary>
    public static TheoryData<string> Refusals() => new() { "train", "project", "pioneer", "arms_none", "arms_ceiling", "dollars", "event" };

    static Command Refused(GameState s, string kind) => kind switch
    {
        "train" => new Train(s.Cities.Keys.First(), "halutz", 0),
        "project" => new StartProject(MapActions.Housing, s.Cities.Keys.First(), -99, -99),
        "pioneer" => new PioneerWork(9999, Projects.BuildYishuv, Real03.Sites.First(x => x.Kinds.Contains(Projects.YishuvKind)).Id),
        "arms_none" => new BuyArms(0),
        "arms_ceiling" => new BuyArms(1),
        "dollars" => new ConvertDollars(0),
        _ => new ChooseEventOption("no_such_event", "ok"),
    };

    // R-MONEY-003, R-ARMS-002, #1702 Done-when 2: a refused order changes nothing, through End turn: the state is the one
    // the same turn with no order gives, byte for byte.
    [Theory]
    [MemberData(nameof(Refusals))]
    public void ARefusedGdd03OrderChangesNothing(string kind)
    {
        var s = Start(0, Real03.Rules.Mvp!.DebtCeiling!.Value);
        var order = Refused(s, kind);
        var r = Simulation.Step(Real03, s, new[] { order });
        Assert.Same(order, Assert.Single(r.Rejected).Command);
        if (kind == "arms_ceiling") Assert.Equal(Pools.RefusedDebtCeiling, r.Rejected[0].Reason);
        Assert.Equal(Simulation.Step(Real03, s, Array.Empty<Command>()).State.Describe(), r.State.Describe());
    }

    /// <summary><see cref="GameState.Describe"/> without the Treasury and Debt lines.</summary>
    static string WithoutMoney(GameState s) =>
        string.Join("\n", s.Describe().Split('\n').Where(l => !l.StartsWith("treasury=", StringComparison.Ordinal) && !l.StartsWith("debt=", StringComparison.Ordinal)));

    // R-MONEY-003, #1702 Done-when 3 (Q21, Q24): a cash shortfall has no penalty. Two games alike but for the Treasury
    // (empty, so every cost and deficit is borrowed, or IL 1,000m, so nothing is) play the same orders for six turns and
    // differ only in the Treasury and the Debt: cities, jobs, units, indicators, Food and Arms are the same.
    [Fact]
    public void AShortfallChangesNothingButTheTreasuryAndTheDebt()
    {
        GameState poor = Start(0), rich = Start(1000);
        var borrowedFor = new HashSet<string>();
        for (int t = 1; t <= 6; t++)
        {
            var orders = Orders(Real03, poor);
            var a = Simulation.Step(Real03, poor, orders);
            var b = Simulation.Step(Real03, rich, orders);
            Assert.Empty(a.Rejected); Assert.Empty(b.Rejected);
            Assert.Equal(WithoutMoney(b.State), WithoutMoney(a.State));
            Assert.DoesNotContain(b.Why, w => w.Key == CauseKeys.Borrowed);
            foreach (var w in a.Why.Where(w => w.Key == CauseKeys.Borrowed)) borrowedFor.Add(w.Args[0]);
            poor = a.State; rich = b.State;
        }
        Assert.True(poor.Debt > 0 && rich.Debt == 0, $"the empty Treasury borrowed IL {N(poor.Debt)}m, the full one IL {N(rich.Debt)}m");
        Assert.Subset(borrowedFor, new HashSet<string> { CauseKeys.Purpose.Deficit, CauseKeys.Purpose.Training, CauseKeys.Purpose.Project, CauseKeys.Purpose.Arms });
    }

    // R-MONEY-003, R-ARMS-002 (#2198; owner, 2 November 2026: "Refuse buys over ceiling (Recommended)"), #1702 Done-when 3
    // (Q22): past the debt ceiling every cost still borrows (training, a city project, the deficit) and only an Arms
    // purchase that would borrow is refused.
    [Fact]
    public void PastTheCeilingEveryCostButArmsBorrows()
    {
        var ceiling = Real03.Rules.Mvp!.DebtCeiling!.Value;
        var s = Start(0, ceiling + 10);
        var orders = Orders(Real03, s);
        orders.RemoveAll(x => x is BuyArms);
        var arms = new BuyArms(1);
        orders.Add(arms);
        Assert.Contains(orders, x => x is Train); Assert.Contains(orders, x => x is StartProject);
        var r = Simulation.Step(Real03, s, orders);
        var refused = Assert.Single(r.Rejected);
        Assert.Equal((arms, Pools.RefusedDebtCeiling), ((BuyArms)refused.Command, refused.Reason));
        Assert.Equal(s.Arms, r.State.Arms);
        Assert.Subset(r.Why.Where(w => w.Key == CauseKeys.Borrowed).Select(w => w.Args[0]).ToHashSet(),
            new HashSet<string> { CauseKeys.Purpose.Training, CauseKeys.Purpose.Project, CauseKeys.Purpose.Deficit });
        Assert.True(r.State.Debt > s.Debt, "the debt grows past the ceiling");
        AssertReconciles(s, r, "past the ceiling");
    }
}
