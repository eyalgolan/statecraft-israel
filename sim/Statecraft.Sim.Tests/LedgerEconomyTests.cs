using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 01: the money flows (EconomySystem.Flows and Apply, R-MONEY-001, R-MONEY-003) run on
/// a game with an initialized Historical ledger, the dark start. The ledger game does what an aggregate game with the same
/// head count does, bit for bit, and changes no ledger record (R-POP-002). The head count is every living record times
/// people_per_unit, for the tax, the spending and the foreign-currency imports (owner, 8 November 2026: "Everyone alive
/// (Recommended)", "Same as tax (Recommended)", "Everyone alive (Recommended)"), and the start's 552 records are accepted
/// as they are, with no rate retuned ("Accept ledger count (Recommended)").</summary>
public class LedgerEconomyTests
{
    const double Month = 1 / 12.0;

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // The public gdd-0.3 game (no ledger), given the dark start's head count and money, so the two differ only in where
    // GameState.TotalPopulation reads the people from.
    static GameState Aggregate(Content c, GameState dark)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.False(s.PopulationLedger.IsInitialized);
        s.Population = new Dictionary<string, double> { ["veterans"] = dark.TotalPopulation };
        (s.Treasury, s.Debt, s.FxReserves, s.Mobilization, s.Date) = (dark.Treasury, dark.Debt, dark.FxReserves, dark.Mobilization, dark.Date);
        s.ActiveSituations.Remove(EconomySystem.FxCrisis);
        if (dark.ActiveSituations.Contains(EconomySystem.FxCrisis)) s.ActiveSituations.Add(EconomySystem.FxCrisis);
        Assert.Equal(dark.TotalPopulation, s.TotalPopulation);
        return s;
    }

    static List<(string, string, double, string, string, string)> Lines(IEnumerable<Cause> why) =>
        why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key, string.Join("|", x.Args))).ToList();

    // Applies the month's flows to the dark start and to its aggregate twin, and checks that the money, the crisis and the
    // causes agree exactly and that the ledger is the same records afterwards.
    static List<Cause> ApplyBoth(Content c, GameState dark, GameState pub)
    {
        var ledger = dark.PopulationLedger.Canonical();
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        EconomySystem.Apply(c, dark, Month, darkWhy);
        EconomySystem.Apply(c, pub, Month, pubWhy);
        Assert.Equal((pub.Treasury, pub.Debt, pub.FxReserves), (dark.Treasury, dark.Debt, dark.FxReserves));
        Assert.Equal(pub.ActiveSituations.Contains(EconomySystem.FxCrisis), dark.ActiveSituations.Contains(EconomySystem.FxCrisis));
        Assert.Equal(Lines(pubWhy), Lines(darkWhy));
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
        return darkWhy;
    }

    // R-POP-002 (#1725 PR 7 review S2, as LedgerCivilianUpkeepTests): only a ledger initialized with mode Historical is let
    // through. An initialized Open ledger, like a synthetic one (no mode), is still refused by Flows and by Apply before
    // anything is read, with the state and its hash unchanged.
    [Theory]
    [InlineData(true, false)] [InlineData(false, false)] [InlineData(true, true)] [InlineData(false, true)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open, bool apply)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        Action call = apply ? () => EconomySystem.Apply(c, s, Month, why) : () => EconomySystem.Flows(c, s, Month);
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(call).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // R-MONEY-001, R-POP-002: the dark start's flows (the Budget screen's lines) are the aggregate arithmetic on whatever
    // TotalPopulation returns, the same doubles as an aggregate game with that head count. No per-head total is asserted.
    [Fact]
    public void TheDarkStartFlowsAreTheAggregateArithmeticOnItsHeadCount()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Aggregate(c, dark);
        Assert.True(dark.PopulationLedger.IsHistorical);
        var ledger = dark.PopulationLedger.Canonical(); var hash = dark.Hash();
        var d = EconomySystem.Flows(c, dark, Month); var p = EconomySystem.Flows(c, pub, Month);
        Assert.Equal((p.Tax, p.Spending, p.Mobilization), (d.Tax, d.Spending, d.Mobilization));
        Assert.True(d.Tax > 0 && d.Spending > 0);
        Assert.Equal(ledger, dark.PopulationLedger.Canonical()); Assert.Equal(hash, dark.Hash());   // Flows is a query
    }

    // R-MONEY-001: one month of tax, spending, the reserve call-up and the foreign-currency flows moves the dark start's
    // Treasury and reserves exactly as it moves the aggregate twin's, with the same causes, and changes no ledger record.
    [Fact]
    public void TheDarkStartAppliesTheMonthsFlowsAsTheAggregateGameDoes()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Aggregate(c, dark);
        var (treasury, fx) = (dark.Treasury, dark.FxReserves);
        var why = ApplyBoth(c, dark, pub);
        Assert.NotEqual(treasury, dark.Treasury); Assert.NotEqual(fx, dark.FxReserves);
        Assert.Contains(why, x => x.Key == CauseKeys.MoneyFlows);
        Assert.Contains(why, x => x.Key == CauseKeys.FxFlows);
    }

    // R-MONEY-003: with no Money, the month's deficit is borrowed: the Treasury stops at 0 and Debt takes the shortfall,
    // with the deficit's borrowing cause, as in the aggregate game.
    [Fact]
    public void TheDarkStartBorrowsItsDeficitAsTheAggregateGameDoes()
    {
        var c = Gdd03();
        var dark = Dark(c);
        dark.Treasury = 0; dark.Debt = 2;
        var pub = Aggregate(c, dark);
        var flows = EconomySystem.Flows(c, dark, Month);
        Assert.True(flows.Tax - flows.Spending - flows.Mobilization < 0);   // the shipped spending rate is above the tax rate
        var why = ApplyBoth(c, dark, pub);
        Assert.Equal(0, dark.Treasury);
        Assert.True(dark.Debt > 2);
        Assert.Contains(why, x => x.Key == CauseKeys.Borrowed && x.Target == "debt" && x.Args.SequenceEqual(new[] { CauseKeys.Purpose.Deficit }));
    }

    // The foreign-currency crisis: exhausted reserves turn fx_crisis on, and reserves back above a quarter of fx_start
    // turn it off, on the dark start as in the aggregate game.
    [Fact]
    public void TheDarkStartsCurrencyCrisisBeginsAndEasesAsInTheAggregateGame()
    {
        var c = Gdd03();
        var dark = Dark(c);
        Assert.DoesNotContain(EconomySystem.FxCrisis, dark.ActiveSituations);
        dark.FxReserves = -1000;
        var began = ApplyBoth(c, dark, Aggregate(c, dark));
        Assert.Contains(EconomySystem.FxCrisis, dark.ActiveSituations);
        Assert.Contains(began, x => x.Key == CauseKeys.FxCrisisBegan);

        dark.FxReserves = 0.25 * c.RulesAt(dark.Date).FxStart + 1000;
        var eased = ApplyBoth(c, dark, Aggregate(c, dark));
        Assert.DoesNotContain(EconomySystem.FxCrisis, dark.ActiveSituations);
        Assert.Contains(eased, x => x.Key == CauseKeys.FxCrisisEased);
    }


    // Step's own year fraction for the turn starting on s's date (Simulation.Step: the turn length, or the real days of a
    // turn cut at an era start or the game's end).
    static double FirstTurn(Content c, GameState s)
    {
        var len = SecuritySystem.TurnLengthFor(c, s.Date, s.Rung);
        var next = Simulation.TurnEnd(c, s);
        return next != SimTime.Advance(s.Date, len) ? (next - s.Date).Days / 365.0 : SimTime.YearFraction(len);
    }

    // R-MONEY-001 (owner, 8 November 2026: tax "Everyone alive (Recommended)", spending "Same as tax (Recommended)",
    // imports "Everyone alive (Recommended)"): each per-head line is its rate times every living record times
    // people_per_unit times the year fraction, whatever the records' statuses. The dark start has active soldiers; one
    // census record is made a waiting refugee here too (the Historical start has no captor binding, so no record can be
    // captured in it). GameState.TotalPopulation already returns that count (PopulationLedger.TotalPeople), so the
    // economy needs no base of its own.
    [Fact]
    public void TaxSpendingAndImportsCountEveryLivingRecord()
    {
        var c = Gdd03();
        var dark = Dark(c); var ledger = dark.PopulationLedger;
        var civilian = ledger.Units.First(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned);
        ledger.Transition(civilian.Id, PopulationStatus.Refugee, civilian.Location, PopulationAssignment.Unassigned);
        Assert.True(ledger.Count(PopulationStatus.ActiveSoldier) > 0);
        Assert.Equal(1, ledger.Count(PopulationStatus.Refugee));
        Assert.Equal(ledger.Units.Count, Enum.GetValues<PopulationStatus>().Sum(x => ledger.Count(x)));
        var people = (double)ledger.Units.Count * ledger.PeoplePerUnit;
        Assert.Equal(people, dark.TotalPopulation);

        var r = c.RulesAt(dark.Date);
        var flows = EconomySystem.Flows(c, dark, Month);
        Assert.Equal(r.TaxPerCapitaPerYear * people * Month, flows.Tax);
        Assert.Equal(r.SpendingPerCapitaPerYear * people * Month, flows.Spending);
        var fx = dark.FxReserves; var why = new List<Cause>();
        EconomySystem.Apply(c, dark, Month, why);
        Assert.Equal(fx + (r.ExportsPerYear * Month + r.CapitalInflowPerYear * Month - r.ImportsPerCapitaPerYear * people * Month), dark.FxReserves);
        Assert.Equal(CauseKeys.Number(r.ImportsPerCapitaPerYear * people * Month), why.Single(x => x.Key == CauseKeys.FxFlows).Args[2]);
        ledger.Reconcile();
    }

    // The start (owner, 8 November 2026: "Accept ledger count (Recommended)"): the dark start's first turn taxes, spends
    // and imports for its 552 records of 1,000 people, 552,000, not the aggregate start's 649,400, at the unchanged rates.
    [Fact]
    public void TheDarkStartsFirstTurnCounts552000People()
    {
        var c = Gdd03();
        var dark = Dark(c);
        Assert.Equal((552, 1000), (dark.PopulationLedger.Units.Count, dark.PopulationLedger.PeoplePerUnit));
        Assert.Equal(552000d, dark.TotalPopulation);
        Assert.Equal(649400d, dark.Population.Values.Sum());   // the aggregate head count, which the ledger game no longer reads
        Assert.Equal((0.0000545, 0.0001036, 0.00025), (c.RulesAt(dark.Date).TaxPerCapitaPerYear, c.RulesAt(dark.Date).SpendingPerCapitaPerYear,
            c.RulesAt(dark.Date).ImportsPerCapitaPerYear));
        var yf = FirstTurn(c, dark);
        var flows = EconomySystem.Flows(c, dark, yf);
        Assert.Equal(0.0000545 * 552000d * yf, flows.Tax);
        Assert.Equal(0.0001036 * 552000d * yf, flows.Spending);
        Assert.NotEqual(0.0000545 * 649400d * yf, flows.Tax);
    }

    // Parity: a legacy and a gdd-0.1 game have no ledger and keep main's flows. Apply moves only the Treasury, the Debt
    // and the reserves, by the aggregate arithmetic in its own order on GameState.Population's sum, so the state after it
    // describes and hashes as its start with those three numbers set (a same-run comparison, no literal hash).
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void ALegacyOrGdd01GameKeepsItsFlowsAndState(bool legacy)
    {
        var c = legacy ? RealContentTests.Legacy() : HistoricalPopulationTests.Real(false);
        Assert.Equal(legacy ? SaveGame.LegacyRuleset : SaveGame.GddRuleset, c.Ruleset);
        var start = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.False(start.PopulationLedger.IsInitialized);
        Assert.DoesNotContain(EconomySystem.FxCrisis, start.ActiveSituations);
        var r = c.RulesAt(start.Date); var pop = start.Population.OrderBy(kv => kv.Key, StringComparer.Ordinal).Sum(kv => kv.Value);
        Assert.Equal(pop, start.TotalPopulation);
        var tax = r.TaxPerCapitaPerYear * pop * Month; var spending = r.SpendingPerCapitaPerYear * pop * Month;
        var mobilization = r.MobilizationCostPerYear * start.Mobilization * Month;
        var flows = EconomySystem.Flows(c, start, Month);
        Assert.Equal((tax, spending, mobilization), (flows.Tax, flows.Spending, flows.Mobilization));

        var expected = start.Clone();
        expected.Treasury += tax - spending - mobilization;
        if (expected.Treasury < 0) { expected.Debt += -expected.Treasury; expected.Treasury = 0; }
        expected.FxReserves += r.ExportsPerYear * Month + r.CapitalInflowPerYear * Month - r.ImportsPerCapitaPerYear * pop * Month;
        Assert.True(expected.FxReserves > 0);   // no crisis begins, so the situations stay as they were

        var s = start.Clone(); var why = new List<Cause>();
        EconomySystem.Apply(c, s, Month, why);
        Assert.Equal((expected.Treasury, expected.Debt, expected.FxReserves), (s.Treasury, s.Debt, s.FxReserves));
        Assert.Equal(expected.Describe(), s.Describe());
        Assert.Equal(expected.Hash(), s.Hash());
        Assert.Equal(new[] { CauseKeys.MoneyFlows, CauseKeys.FxFlows }, why.Select(x => x.Key));
    }
}
