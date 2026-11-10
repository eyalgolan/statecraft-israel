using Statecraft.Sim;

/// <summary>#1725: ReturnCraftsmen (R-QUAL-002) and ReassignCraftsmen (R-QUAL-004) on a game with an initialized Historical
/// ledger, the dark start (R-POP-002). The Historical start has no craftsmen and the craftsman course is refused until #2476,
/// so both orders are refused with a plain reason, with no state change and nothing thrown (owner, 9 November 2026: "Refuse
/// for now (Recommended)"). A synthetic or Open ledger is still refused as before.</summary>
public class LedgerCraftsmenOrdersTests
{
    const string TelAviv = "tel_aviv";
    const string Refusal = "Craftsmen are not on the population ledger yet (#2476)";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static string? Order(Content c, GameState s, bool reassign, string city, int count, List<Cause> why) => reassign
        ? Qualification.ReassignCraftsmen(c, s, city, count, why)
        : Qualification.ReturnCraftsmen(c, s, city, count, why);

    // Owner, 9 November 2026, "Refuse for now (Recommended)": each order is refused with a plain reason on the dark start,
    // the state hash, the ledger and the description unchanged, and no Why line.
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EachCraftsmanOrderIsRefusedOnTheDarkStart(bool reassign)
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.True(s.PopulationLedger.IsInitialized);
        var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList(); var before = s.Describe();
        var why = new List<Cause>();
        Assert.Equal(Refusal, Order(c, s, reassign, TelAviv, 1, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(ledger, s.PopulationLedger.Canonical().ToList());
        Assert.Equal(before, s.Describe());
        s.PopulationLedger.Reconcile();
    }

    // The orders' own refusals for an unknown city and a count below 1 come first, as Train's do before its craftsman
    // refusal (R-TRAIN-001), with the state unchanged.
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TheOrdersOwnRefusalsComeFirst(bool reassign)
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash(); var why = new List<Cause>();
        Assert.Equal("There is no such city", Order(c, s, reassign, "nowhere", 1, why));
        Assert.Equal(reassign ? Qualification.RefusedReassignCount : Qualification.RefusedReturnCount, Order(c, s, reassign, TelAviv, 0, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
    }

    // R-POP-002: an initialized Open ledger, like a synthetic one (no mode), is still refused by throwing, state unchanged.
    [Theory]
    [InlineData(true, false)] [InlineData(false, false)] [InlineData(true, true)] [InlineData(false, true)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open, bool reassign)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var hash = s.Hash(); var why = new List<Cause>();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Order(c, s, reassign, TelAviv, 1, why)).Message);
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
    }
}
