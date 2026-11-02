using System.Collections.Generic;
using Statecraft.Sim;

// R-PROJ-001, R-PROJ-003: #1700 builder rules over the #1963 catalogue, synthetic fixtures only.
// Amir, 28 Oct 2026 (#1668): "Extra Money cannot replace work or speed completion. Housing and improvements also
// require Money for upkeep."
public class Gdd03ProjectBuildFixtureTests
{
    static Gdd03ProjectCatalogue Catalogue()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        // Synthetic identity selection only; no scenario, region or facility binding.
        content.Eras = new() { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = SaveGame.Gdd03Ruleset } } };
        return Gdd03ProjectCatalogue.Read(content);
    }

    static readonly IReadOnlyDictionary<int, Gdd03BuilderTier[]> Nobody = new Dictionary<int, Gdd03BuilderTier[]>();
    static IReadOnlyDictionary<int, Gdd03BuilderTier[]> On(int id, params Gdd03BuilderTier[] tiers)
        => new Dictionary<int, Gdd03BuilderTier[]> { [id] = tiers };

    [Fact]
    public void AFundedProjectWithNoBuildersMakesNoProgress()
    {
        var f = new Gdd03ProjectBuildFixture(Catalogue(), 5m);
        var id = f.Start("maabara"); // 5 Money, 1 work
        Assert.Equal(0m, f.Money); // the full cost up front (R-PROJ-001)
        for (var i = 0; i < 10; i++) f.EndTurn(Nobody);
        var p = Assert.Single(f.Projects);
        Assert.Equal(id, p.Id);
        Assert.Equal(0m, p.WorkDone);
        Assert.Null(p.CompletedTurn);
        Assert.Equal(0m, f.Money); // an unfinished project owes no upkeep
    }

    [Fact]
    public void ExtraMoneyDoesNotChangeTheCompletionTurn()
    {
        int? Completion(decimal money)
        {
            var f = new Gdd03ProjectBuildFixture(Catalogue(), money);
            var id = f.Start("permanent_housing"); // 15 Money, 3 work
            // Builders are released on completion, so they work only while the project is open.
            for (var i = 0; i < 10 && f.Projects[0].CompletedTurn == null; i++) f.EndTurn(On(id, Gdd03BuilderTier.OrdinaryOrNovice));
            return f.Projects[0].CompletedTurn;
        }
        Assert.Equal(3, Completion(15m)); // one ordinary builder: 1 work a turn
        Assert.Equal(3, Completion(1000m));
    }

    [Fact]
    public void ACompletedImprovementChargesItsUpkeepEachTurnEvenVacant()
    {
        var f = new Gdd03ProjectBuildFixture(Catalogue(), 20m);
        var id = f.Start("industry"); // 10 Money, 3 work; the catalogue's 0.25 upkeep is post-MVP
        Assert.Equal(10m, f.Money);
        f.EndTurn(On(id, Gdd03BuilderTier.Expert)); // 3 work: complete on turn 1
        Assert.Equal(1, f.Projects[0].CompletedTurn);
        Assert.Equal(10m, f.Money); // charged the MVP rate, IL 0m (R-MONEY-002), from the completion turn
        f.EndTurn(Nobody);
        f.EndTurn(Nobody);
        Assert.Equal(10m, f.Money); // and every turn after, with no one working there
        Assert.Equal(0m, f.Debt);
        Assert.Equal(3m, f.Projects[0].WorkDone);
    }

    [Fact]
    public void AShortStartBorrowsAndBuildersAreCappedAtTheSlots()
    {
        var f = new Gdd03ProjectBuildFixture(Catalogue(), 14m);
        f.Start("permanent_housing"); // 15 Money: the 1 short is borrowed (D1, R-MONEY-003)
        Assert.Equal(0m, f.Money);
        Assert.Equal(1m, f.Debt);
        var id = f.Start("industry"); // 10 Money, all borrowed
        Assert.Equal(0m, f.Money);
        Assert.Equal(11m, f.Debt);
        Assert.Equal(2, f.Projects.Count);
        var four = On(id, Gdd03BuilderTier.OrdinaryOrNovice, Gdd03BuilderTier.OrdinaryOrNovice,
            Gdd03BuilderTier.OrdinaryOrNovice, Gdd03BuilderTier.OrdinaryOrNovice);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(four));
        Assert.Equal(0m, f.Projects[1].WorkDone);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(On(id, (Gdd03BuilderTier)7))); // no such tier
        Assert.Equal(0m, f.Projects[1].WorkDone);
        Assert.Equal(0, f.Turn);
        f.EndTurn(On(id, Gdd03BuilderTier.Apprentice)); // 1.5 of 3
        Assert.Equal(1.5m, f.Projects[1].WorkDone);
        Assert.Null(f.Projects[1].CompletedTurn);
    }
}
