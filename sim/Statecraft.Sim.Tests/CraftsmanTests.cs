using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 3 PR 5, the craftsman (D-D4): a civilian trains as one in a city with a completed industry project
/// (Q-CRAFT-1), and an employed craftsman fills an ordinary job slot and earns a multiple of the job income (Q-CRAFT-2). The
/// fixture's values are its own: an income multiple of 2, and 3 turns and IL 0.75m a group for the course.</summary>
public class CraftsmanTests
{
    /// <summary>The GDD fixture's one city, town_a: housing for 12, 8 jobs, 10 civilians, 6 of them employed.</summary>
    static Content Town() => ContentLoader.Load(Fixture.Write(Fixture.Gdd()));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    /// <summary>A copy of <paramref name="s"/> with an industry project in town_a, built when <paramref name="built"/>.</summary>
    static GameState WithIndustry(GameState s, bool built)
    {
        var x = s.Clone();
        var id = x.NextId++;
        x.CityProjects.Add(id, new CityProject(id, Training.Industry, "town_a", 1, 1, built ? 0 : 3, built ? x.Date : (System.DateTime?)null));
        return x;
    }

    // R-CRAFT-002
    [Fact]
    public void ACraftsmanEarnsMoreThanAWorker()
    {
        var c = Town();
        var worker = Simulation.NewGame(c, 1, Mode.Historical);
        worker.Cities["town_a"] = new City("town_a", 12, 8, 10, 1, 0);                  // 1 civilian in work
        var craftsman = worker.Clone();
        craftsman.Cities["town_a"] = new City("town_a", 12, 8, 9, 1, 0, craftsmen: 1);  // the same, but a craftsman in the job

        var difference = Step(c, craftsman).State.Treasury - Step(c, worker).State.Treasury;
        var rate = c.Rules.Mvp!.IncomePerJobPerYear * SimTime.YearFraction(TurnLength.Week);   // the fixture's first turn is a week
        Assert.Equal(2.0, c.Rules.Mvp.CraftsmanIncomeMultiple);
        Assert.Equal(rate * (2.0 - 1), difference, 9);

        // Without a job a craftsman earns nothing, like a civilian (R-CITY-002 pays the employed only).
        worker.Cities["town_a"] = new City("town_a", 12, 8, 10, 0, 0);
        craftsman.Cities["town_a"] = new City("town_a", 12, 8, 9, 0, 0, craftsmen: 1);
        Assert.Equal(Step(c, worker).State.Treasury, Step(c, craftsman).State.Treasury, 12);
    }

    // R-CRAFT-001
    [Fact]
    public void ACraftsmanIsMadeAsStated()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var order = new Train("town_a", Training.Craftsman, 1);

        // No industry project, or one still being built: refused, as if the order had not been given.
        foreach (var none in new[] { s, WithIndustry(s, built: false) })
        {
            var refused = Step(c, none, order);
            Assert.Equal("Town A has no completed industry project", Assert.Single(refused.Rejected).Reason);
            Assert.Equal(Step(c, none).State.Hash(), refused.State.Hash());
        }

        var built = WithIndustry(s, built: true);
        var one = Step(c, built, order);
        Assert.Empty(one.Rejected);
        Assert.Equal(0.75, Step(c, built).State.Treasury - one.State.Treasury, 9);   // training_cost, paid at once
        var cohort = Assert.Single(one.State.Cohorts.Values);
        Assert.Equal((Training.Craftsman, "town_a", 1, 2), (cohort.Type, cohort.City, cohort.Count, cohort.TurnsLeft));   // 3 turns, this one counted
        Assert.Equal(9, one.State.Cities["town_a"].Civilians);   // the trainee left the civilians at once
        Assert.Equal(0, one.State.Cities["town_a"].Craftsmen);

        var two = Step(c, one.State).State;
        Assert.Equal(0, two.Cities["town_a"].Craftsmen);
        var done = Step(c, two);
        var city = done.State.Cities["town_a"];
        Assert.Empty(done.State.Cohorts);
        var never = Step(c, Step(c, Step(c, built).State).State).State.Cities["town_a"];   // the same three turns, no order
        Assert.Equal((never.Civilians - 1, 1), (city.Civilians, city.Craftsmen));
        Assert.Contains(done.Why, x => x.System == "city" && x.Message == "Town A: 1 group finished craft training and are craftsmen");
        Assert.Contains(",craftsmen=1", city.Canonical());
    }

    // R-CRAFT-001 (R-TRAIN-001's refusals): every refusal leaves the state as if the order had not been given.
    [Theory]
    [InlineData("unknown", 1, "There is no such training")]
    [InlineData(Training.Craftsman, 0, "Train at least one group")]
    [InlineData(Training.Craftsman, 5, "Town A has 4 civilians without work")]
    public void ARefusedCraftsmanTrainingLeavesTheStateUnchanged(string track, int count, string reason)
    {
        var c = Town();
        var s = WithIndustry(Simulation.NewGame(c, 1, Mode.Historical), built: true);
        var r = Step(c, s, new Train("town_a", track, count));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-CRAFT-002, R-TRAIN-001: craftsmen take the jobs first, so only the employed beyond them are civilians in work.
    [Fact]
    public void CraftsmenTakeTheJobsFirst()
    {
        var x = new City("town_a", 12, 8, 6, 5, 0, craftsmen: 2);   // 5 in work: the 2 craftsmen and 3 civilians
        Assert.Equal(3, Training.Idle(x));
        Assert.Equal(8, Cities.MaxWorkers(x));                      // min(8 jobs, 6 civilians + 2 craftsmen)
    }
}
