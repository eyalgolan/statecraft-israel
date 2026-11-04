using System.Collections.Generic;
using Statecraft.Sim;

// R-PROJ-001, R-PROJ-003: #1700 builder rules over the #1963 catalogue, synthetic fixtures only.
// Amir, 28 Oct 2026 (#1668): "Extra Money cannot replace work or speed completion. Housing and improvements also
// require Money for upkeep."
public class Gdd03ProjectBuildFixtureTests
{
    // The national limit comes from the loaded rules (map_actions_per_turn); a test may override it to prove that.
    static Gdd03ProjectBuildFixture New(decimal money, int? mapActionsPerTurn = null)
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        // Synthetic identity selection only; no scenario, region or facility binding.
        content.Eras = new() { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = SaveGame.Gdd03Ruleset } } };
        if (mapActionsPerTurn != null) content.Rules.MapActionsPerTurn = mapActionsPerTurn.Value;
        return new Gdd03ProjectBuildFixture(Gdd03ProjectCatalogue.Read(content), content.Rules, money);
    }

    static readonly IReadOnlyDictionary<int, Gdd03BuilderTier[]> Nobody = new Dictionary<int, Gdd03BuilderTier[]>();
    static IReadOnlyDictionary<int, Gdd03BuilderTier[]> On(int id, params Gdd03BuilderTier[] tiers)
        => new Dictionary<int, Gdd03BuilderTier[]> { [id] = tiers };

    [Fact]
    public void AFundedProjectWithNoBuildersMakesNoProgress()
    {
        var f = New(5m);
        var id = f.Start("maabara", "town_a"); // 5 Money, 1 work
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
            var f = New(money);
            var id = f.Start("permanent_housing", "town_a"); // 15 Money, 3 work
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
        var f = New(20m);
        var id = f.Start("industry", "town_a"); // 10 Money, 3 work; the catalogue's 0.25 upkeep is post-MVP
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
        var f = New(14m);
        f.Start("permanent_housing", "town_a"); // 15 Money: the 1 short is borrowed (D1, R-MONEY-003)
        Assert.Equal(0m, f.Money);
        Assert.Equal(1m, f.Debt);
        f.EndTurn(Nobody); // one start a city a turn (R-PROJ-001 under gdd-0.3)
        var id = f.Start("industry", "town_a"); // 10 Money, all borrowed
        Assert.Equal(0m, f.Money);
        Assert.Equal(11m, f.Debt);
        Assert.Equal(2, f.Projects.Count);
        var four = On(id, Gdd03BuilderTier.OrdinaryOrNovice, Gdd03BuilderTier.OrdinaryOrNovice,
            Gdd03BuilderTier.OrdinaryOrNovice, Gdd03BuilderTier.OrdinaryOrNovice);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(four));
        Assert.Equal(0m, f.Projects[1].WorkDone);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(On(id, (Gdd03BuilderTier)7))); // no such tier
        Assert.Equal(0m, f.Projects[1].WorkDone);
        Assert.Equal(1, f.Turn);
        f.EndTurn(On(id, Gdd03BuilderTier.Apprentice)); // 1.5 of 3
        Assert.Equal(1.5m, f.Projects[1].WorkDone);
        Assert.Null(f.Projects[1].CompletedTurn);
    }

    [Fact]
    public void OneStartACityATurnAndRunningProjectsCarryOn()
    {
        // #1700 slice B (owner, 3 November 2026: "One per city, plus 2 nationally"), the gdd-0.3 note under R-PROJ-001.
        var f = New(30m);
        var housing = f.Start("permanent_housing", "town_a"); // the city's one start this turn
        Assert.Equal(15m, f.Money);
        Assert.Throws<System.InvalidOperationException>(() => f.Start("maabara", "town_a")); // a second start in the city
        Assert.Equal(15m, f.Money); // refused with Money and state unchanged
        Assert.Equal(0m, f.Debt);
        Assert.Single(f.Projects);
        f.EndTurn(On(housing, Gdd03BuilderTier.OrdinaryOrNovice));
        var industry = f.Start("industry", "town_a"); // a new turn, a new start
        Assert.Equal(5m, f.Money);
        Assert.Throws<System.InvalidOperationException>(() => f.Start("maabara", "town_a"));
        Assert.Equal(5m, f.Money);
        Assert.Equal(2, f.Projects.Count);
        // The refused start does not stop projects already running.
        f.EndTurn(new Dictionary<int, Gdd03BuilderTier[]>
        {
            [housing] = new[] { Gdd03BuilderTier.OrdinaryOrNovice },
            [industry] = new[] { Gdd03BuilderTier.Expert }
        });
        Assert.Equal(2m, f.Projects[0].WorkDone);
        Assert.Equal(2, f.Projects[1].CompletedTurn);
    }

    [Fact]
    public void TwoStartsInOneCityTheSecondIsRefused()
    {
        var f = New(100m);
        f.Start("industry", "town_a");
        var e = Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_a"));
        Assert.Equal("town_a has started a city project this turn", e.Message);
        Assert.Equal((90m, 0m, 1), (f.Money, f.Debt, f.Projects.Count)); // refused before anything is paid
    }

    [Fact]
    public void OneStartInEachOfTwoCitiesBothAreAccepted()
    {
        var f = New(100m);
        f.Start("industry", "town_a");
        f.Start("industry", "town_b");
        Assert.Equal((80m, 0m, 2), (f.Money, f.Debt, f.Projects.Count));
    }

    [Fact]
    public void StartsInThreeCitiesTheThirdIsRefusedByTheNationalTwo()
    {
        var f = New(100m);
        f.Start("industry", "town_a");
        f.Start("industry", "town_b");
        var e = Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_c"));
        Assert.Equal("No more than 2 city starts are allowed a turn", e.Message);
        Assert.Equal((80m, 0m, 2), (f.Money, f.Debt, f.Projects.Count));
        f.EndTurn(Nobody);
        f.Start("industry", "town_c"); // both counts reset when the turn ends
        f.Start("industry", "town_a");
        Assert.Equal(4, f.Projects.Count);
    }

    // A start the city's limit refuses uses neither the city's slot nor a national one (#1700 slice B review): after A,
    // a refused second A, a start in a second city B is still accepted, and only then is the national 2 used up.
    [Fact]
    public void ARefusedSecondStartInOneCityLeavesTheNationalSlot()
    {
        var f = New(100m);
        f.Start("industry", "town_a");
        var e = Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_a"));
        Assert.Equal("town_a has started a city project this turn", e.Message);
        f.Start("industry", "town_b");
        Assert.Equal((80m, 0m, 2), (f.Money, f.Debt, f.Projects.Count));
        var national = Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_c"));
        Assert.Equal("No more than 2 city starts are allowed a turn", national.Message);
    }

    // The fixture's national 2 is the live game's map_actions_per_turn (content/rules.yaml), which the ruling keeps.
    [Fact]
    public void TheNationalTwoIsTheGamesMapActionsPerTurn()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(2, content.Rules.MapActionsPerTurn); // "plus 2 nationally"
        Assert.Equal(content.Rules.MapActionsPerTurn, New(0m).NationalStarts);
    }

    // The limit is read from the rules, not a literal: with map_actions_per_turn 3 a third city may start.
    [Fact]
    public void TheNationalLimitFollowsTheRulesMapActionsPerTurn()
    {
        var f = New(100m, mapActionsPerTurn: 3);
        Assert.Equal(3, f.NationalStarts);
        f.Start("industry", "town_a");
        f.Start("industry", "town_b");
        f.Start("industry", "town_c");
        var e = Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_d"));
        Assert.Equal("No more than 3 city starts are allowed a turn", e.Message);
        Assert.Equal((70m, 0m, 3), (f.Money, f.Debt, f.Projects.Count));
    }
}
