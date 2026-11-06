using Statecraft.Sim;

// R-PROJ-001, R-PROJ-003: #1700 builder rules over the #1963 catalogue, synthetic fixtures only.
// Amir, 28 Oct 2026 (#1668): "Extra Money cannot replace work or speed completion. Housing and improvements also
// require Money for upkeep."
// Owner, 3 Nov 2026 (#1700): "One builder, today's times (Recommended)": one builder a project, and a turn with its
// builder counts as one turn of the kind's existing `turns` (content/projects.yaml: industry 6).
public class Gdd03ProjectBuildFixtureTests
{
    // The national limit comes from the loaded rules (map_actions_per_turn); a test may override it to prove that.
    static Gdd03ProjectBuildFixture New(decimal money, int? mapActionsPerTurn = null)
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        // Synthetic identity selection only; no scenario, region or facility binding.
        content.Eras = new() { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = SaveGame.Gdd03Ruleset } } };
        if (mapActionsPerTurn != null) content.Rules.MapActionsPerTurn = mapActionsPerTurn.Value;
        return new Gdd03ProjectBuildFixture(Gdd03ProjectCatalogue.Read(content), content.Rules, content.ProjectKinds, money);
    }

    static readonly int[] Nobody = new int[0];
    static int[] On(params int[] ids) => ids;

    [Fact]
    public void AFundedProjectWithNoBuilderMakesNoProgress()
    {
        var f = New(10m);
        var id = f.Start("industry", "town_a"); // 10 Money, 6 turns
        Assert.Equal(0m, f.Money); // the full cost up front (R-PROJ-001)
        for (var i = 0; i < 10; i++) f.EndTurn(Nobody);
        var p = Assert.Single(f.Projects);
        Assert.Equal(id, p.Id);
        Assert.Equal(0, p.TurnsDone);
        Assert.Null(p.CompletedTurn);
        Assert.Equal(0m, f.Money); // an unfinished project owes no upkeep
    }

    [Fact]
    public void ExtraMoneyDoesNotChangeTheCompletionTurn()
    {
        int? Completion(decimal money)
        {
            var f = New(money);
            var id = f.Start("industry", "town_a"); // 10 Money, 6 turns
            // The builder is released on completion, so it works only while the project is open.
            for (var i = 0; i < 10 && f.Projects[0].CompletedTurn == null; i++) f.EndTurn(On(id));
            return f.Projects[0].CompletedTurn;
        }
        Assert.Equal(6, Completion(10m)); // one builder: one of the kind's 6 turns a turn
        Assert.Equal(6, Completion(1000m));
    }

    [Fact]
    public void ACompletedImprovementChargesItsUpkeepEachTurnEvenVacant()
    {
        var f = New(20m);
        var id = f.Start("industry", "town_a"); // 10 Money, 6 turns; the catalogue's 0.25 upkeep is post-MVP
        Assert.Equal(10m, f.Money);
        for (var i = 0; i < 6; i++) f.EndTurn(On(id));
        Assert.Equal(6, f.Projects[0].CompletedTurn);
        Assert.Equal(10m, f.Money); // charged the MVP rate, IL 0m (R-MONEY-002), from the completion turn
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(On(id))); // the builder was released
        f.EndTurn(Nobody);
        f.EndTurn(Nobody);
        Assert.Equal(10m, f.Money); // and every turn after, with no one working there
        Assert.Equal(0m, f.Debt);
        Assert.Equal(6, f.Projects[0].TurnsDone);
    }

    [Fact]
    public void AShortStartBorrowsAndAProjectHasOneBuilder()
    {
        var f = New(4m);
        f.Start("industry", "town_a"); // 10 Money: the 6 short is borrowed (D1, R-MONEY-003)
        Assert.Equal(0m, f.Money);
        Assert.Equal(6m, f.Debt);
        f.EndTurn(Nobody); // one start a city a turn (R-PROJ-001 under gdd-0.3)
        var id = f.Start("industry", "town_a"); // 10 Money, all borrowed
        Assert.Equal(0m, f.Money);
        Assert.Equal(16m, f.Debt);
        Assert.Equal(2, f.Projects.Count);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(On(id, id))); // a second builder on one project
        Assert.Equal(0, f.Projects[1].TurnsDone);
        Assert.Throws<System.InvalidOperationException>(() => f.EndTurn(On(id, 99))); // no such project
        Assert.Equal(0, f.Projects[1].TurnsDone);
        Assert.Equal(1, f.Turn); // a refused turn changes nothing
        f.EndTurn(On(id)); // 1 of 6
        Assert.Equal(1, f.Projects[1].TurnsDone);
        Assert.Null(f.Projects[1].CompletedTurn);
    }

    // The catalogue kinds with no row in content/projects.yaml have no `turns` the ruling could count, so the fixture
    // refuses them before anything is paid rather than invent a length.
    [Fact]
    public void AKindWithNoTurnsInProjectsYamlIsRefusedBeforeAnythingIsPaid()
    {
        var f = New(100m);
        var e = Assert.Throws<System.InvalidOperationException>(() => f.Start("maabara", "town_a"));
        Assert.Equal("No turns in projects.yaml for project kind: maabara", e.Message);
        Assert.Equal((100m, 0m, 0), (f.Money, f.Debt, f.Projects.Count));
        f.Start("industry", "town_a"); // the refusal used no start
        Assert.Single(f.Projects);
    }

    [Fact]
    public void OneStartACityATurnAndRunningProjectsCarryOn()
    {
        // #1700 slice B (owner, 3 November 2026: "One per city, plus 2 nationally"), the gdd-0.3 note under R-PROJ-001.
        var f = New(30m);
        var first = f.Start("industry", "town_a"); // the city's one start this turn
        Assert.Equal(20m, f.Money);
        Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_a")); // a second start in the city
        Assert.Equal(20m, f.Money); // refused with Money and state unchanged
        Assert.Equal(0m, f.Debt);
        Assert.Single(f.Projects);
        f.EndTurn(On(first));
        var second = f.Start("industry", "town_a"); // a new turn, a new start
        Assert.Equal(10m, f.Money);
        Assert.Throws<System.InvalidOperationException>(() => f.Start("industry", "town_a"));
        Assert.Equal(10m, f.Money);
        Assert.Equal(2, f.Projects.Count);
        // The refused start does not stop projects already running.
        f.EndTurn(On(first, second));
        Assert.Equal(2, f.Projects[0].TurnsDone);
        Assert.Equal(1, f.Projects[1].TurnsDone);
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
