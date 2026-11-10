using Statecraft.Sim;

/// <summary>#1725 PR 7, producer 11: formation service (R-UNIT-004) on a game with an initialized Historical ledger, the
/// dark start. Under exact gdd-0.3 service is a statistic only: a recruit formation gains its service turn and never
/// promotes (R-UNIT-011, Q16), and no ledger record changes (R-POP-002). Maintainer calls by coordinator eyalgolan-30,
/// 8 November 2026, not owner rulings: the guard is RefuseUnlessHistorical (Q1 a); service stays on the formation, with
/// no ledger field (Q2 a); Serve checks no strength (Q3 a); the dark start has no recruit, so these tests hand-place one
/// (Q4 a); Serve only, PayUpkeep keeps its own row (Q5 a).</summary>
public class LedgerMilitaryServiceTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // Q4 (a): a hand-placed Israeli recruit formation, on the hex of the first Israeli formation, with a fresh id.
    static int AddRecruit(GameState s, int service)
    {
        var at = s.Units.Values.First(u => u.Side == "israel");
        var id = s.Units.Keys.Max() + 1;
        s.Units[id] = new Unit(id, "recruit", at.Q, at.R, service, side: "israel", strength: 3);
        return id;
    }

    // R-UNIT-004, R-POP-002: the dark start's recruit gains one service turn and stays a recruit; every other unit and
    // the ledger (its records and its hash section, Canonical()) are unchanged, and the ledger still reconciles.
    [Fact]
    public void TheDarkStartServesARecruitFormation()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.True(s.PopulationLedger.IsHistorical);
        var id = AddRecruit(s, 2);
        var others = s.Units.Values.Where(u => u.Id != id).Select(u => u.Canonical()).ToList();
        var ledger = s.PopulationLedger.Canonical().ToList();
        var why = new List<Cause>();
        Military.Serve(c, s, c.RulesAt(s.Date).Mvp!, why);
        var after = s.Units[id];
        Assert.Equal(("recruit", 3, "israel", 3), (after.Type, after.Service, after.Side, after.Strength));
        Assert.Equal(others, s.Units.Values.Where(u => u.Id != id).Select(u => u.Canonical()).ToList());
        Assert.Empty(why);
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical().ToList());
    }

    // R-UNIT-004 re-pointed (Q16), R-UNIT-011: under gdd-0.3 a ledger game's recruit far beyond veteran_soldier_service
    // stays a recruit, with no RecruitsVeteran cause (as Gdd03CombatXpTests.Gdd03ServiceNeverPromotes on the aggregate game).
    [Fact]
    public void ServiceNeverPromotesOnALedgerGame()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var id = AddRecruit(s, r.VeteranSoldierService!.Value + 14);
        var ledger = s.PopulationLedger.Canonical().ToList();
        var why = new List<Cause>();
        Military.Serve(c, s, r, why);
        Assert.Equal(("recruit", r.VeteranSoldierService!.Value + 15), (s.Units[id].Type, s.Units[id].Service));
        Assert.DoesNotContain(why, w => w.Key == CauseKeys.RecruitsVeteran);
        Assert.Equal(ledger, s.PopulationLedger.Canonical().ToList());
    }

    // R-POP-002 (#1725 PR 7 review S2): only a ledger initialized with mode Historical is let through. An initialized Open
    // ledger, like a synthetic one (no mode), is still refused by Serve, with the state unchanged.
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        s.Units[1] = new Unit(1, "recruit", 0, 1, 2, side: "israel", strength: 3);
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Military.Serve(c, s, new MvpRules(), why)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }
}
