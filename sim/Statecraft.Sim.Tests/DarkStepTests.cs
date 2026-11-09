using System;
using Statecraft.Sim;

/// <summary>#1689 / #1725 PR 6: Simulation.Step on a dark start. Step lets in a game whose ledger was initialized with
/// mode Historical (owner, 8 November 2026, #1725 Draft 10: "Historical mode alone (Rec.)"), runs its orders on the working
/// copy, and reaches the turn, which still refuses the ledger ("Separate dark-turn PR (Rec.)"). A synthetic ledger is
/// still refused at Step's entry. Nothing here mutates the caller's state.</summary>
public class DarkStepTests
{
    const string DarkTurnHeld = "The dark turn is held";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // R-POP-002: a dark start passes Step's entry and is refused by TurnResolver.Resolve, by its own message, with the
    // input unchanged; a synthetic fixture ledger is still refused at the entry, by the aggregate guard's message.
    [Fact]
    public void StepLetsADarkStartInAndTheTurnRefusesIt()
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash();
        var text = s.Describe();
        var e = Assert.Throws<NotSupportedException>(() => Simulation.Step(c, s, Array.Empty<Command>()));
        Assert.Contains(DarkTurnHeld, e.Message);
        Assert.Contains("integration is held", e.Message);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(text, s.Describe());

        var synthetic = PopulationLedgerTests.State(new PopulationInventoryRow("start", 0, PopulationLedgerTests.Person()));
        var before = synthetic.Hash();
        var refused = Assert.Throws<NotSupportedException>(() => Simulation.Step(PopulationLedgerTests.Content(), synthetic, Array.Empty<Command>()));
        Assert.DoesNotContain(DarkTurnHeld, refused.Message);
        Assert.Equal(before, synthetic.Hash());
    }
}
