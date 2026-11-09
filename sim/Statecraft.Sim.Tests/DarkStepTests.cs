using System;
using System.Collections.Generic;
using System.Linq;
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

    // R-POP-002, R-ENEMY-005 ("Only if none pending (Rec.)"): Step skips EventSystem.Delegate on a ledger game only when no
    // card is waiting; a waiting card still reaches Delegate's own guard, which refuses before the turn, input unchanged.
    [Fact]
    public void ADarkStartWithAWaitingCardIsStillRefusedAtDelegate()
    {
        var c = Gdd03();
        var s = Dark(c).Clone();
        s.PendingEvents.Add(c.Events[0].Id);
        var hash = s.Hash();
        var e = Assert.Throws<NotSupportedException>(() => Simulation.Step(c, s, Array.Empty<Command>()));
        Assert.DoesNotContain(DarkTurnHeld, e.Message);
        Assert.Contains("integration is held", e.Message);
        Assert.Equal(hash, s.Hash());
    }

    public static IEnumerable<object[]> CommandKinds() =>
        CommandGuardTests.Samples.Keys.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t => new object[] { t.Name });

    // R-POP-002 ("Yes, unready refuses (Rec.)"): every order kind runs on the working copy of a dark start; one whose
    // system is not ledger-ready refuses there with its guard's NotSupportedException, and the turn refuses the rest.
    // No other exception type, and the caller's state is unchanged.
    [Theory]
    [MemberData(nameof(CommandKinds))]
    public void EveryOrderOnADarkStartRefusesWithTheGameUnchanged(string kind)
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash();
        var cmd = CommandGuardTests.Samples.Single(x => x.Key.Name == kind).Value;
        var e = Record.Exception(() => Simulation.Step(c, s, new[] { cmd }));
        Assert.NotNull(e);
        Assert.IsType<NotSupportedException>(e);
        Assert.Equal(hash, s.Hash());
    }

    // R-POP-003, docs/determinism.md ("Not yet, PR 8 (Rec.)": PR 6 checks only that two runs of the same dark start give
    // the same result): two fresh dark starts given the same orders end the same way and keep the same hash.
    [Fact]
    public void TwoRunsOfTheSameDarkStartGiveTheSameResult()
    {
        foreach (var orders in new[] { Array.Empty<Command>(), new Command[] { new UnitOrder(1, "fortify") } })
        {
            var c1 = Gdd03(); var a = Dark(c1);
            var c2 = Gdd03(); var b = Dark(c2);
            Assert.Equal(a.Hash(), b.Hash());
            var ea = Assert.Throws<NotSupportedException>(() => Simulation.Step(c1, a, orders));
            var eb = Assert.Throws<NotSupportedException>(() => Simulation.Step(c2, b, orders));
            Assert.Equal(ea.Message, eb.Message);
            Assert.Equal(a.Hash(), b.Hash());
            Assert.Equal(a.Describe(), b.Describe());
        }
    }

    // R-POP-002 ("Separate dark-turn PR (Rec.)"): Preview and DelegatedTo still refuse any initialized ledger at their entry,
    // a dark start included, with the state unchanged.
    [Fact]
    public void PreviewAndDelegatedToStillRefuseADarkStart()
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash();
        var p = Assert.Throws<NotSupportedException>(() => Simulation.Preview(c, s, Array.Empty<Command>()));
        var d = Assert.Throws<NotSupportedException>(() => Simulation.DelegatedTo(c, s, Array.Empty<Command>(), c.Events[0].Id));
        Assert.DoesNotContain(DarkTurnHeld, p.Message);
        Assert.DoesNotContain(DarkTurnHeld, d.Message);
        Assert.Equal(hash, s.Hash());
    }
}
