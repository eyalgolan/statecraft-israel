using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1700 slice A (GDD 0.3 builders on the aggregate cities), under gdd-0.3 only: R-CITY-011 with R-PROJ-001 to
/// R-PROJ-003 (owner, 3 November 2026: "Builders in the MVP (Recommended)", "One builder, today's times (Recommended)",
/// "Yes, if someone is free"). The fixture's town_a is on (1, 0), with (1, 1) and (2, 0) land next to it; its housing takes
/// 2 turns, its transit camp 1 and its industry 2 (Fixture.GddProjects).</summary>
public class CityBuildersTests
{
    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    /// <summary>A game whose town_a has <paramref name="jobs"/> job slots and <paramref name="civilians"/> civilians,
    /// <paramref name="employed"/> of them in jobs.</summary>
    static (Content C, GameState S) Game(int civilians = 10, int employed = 8, int jobs = 8, bool jobsDefault = false, bool gdd03 = true,
        string projects = Fixture.GddProjects + ArmsWorkshopTests.Row)
    {
        var c = ArmsWorkshopTests.Load(gdd03, projects);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, x.HousingCapacity, jobs, civilians, employed, 0, 0, x.Condition, jobsDefault: jobsDefault);
        return (c, s);
    }

    /// <summary>Adds a town_a project of <paramref name="kind"/> being built on hex (q, r), and returns its id.</summary>
    static int Add(GameState s, string kind, int q, int r, int turnsLeft, bool builder)
    {
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, kind, "town_a", q, r, turnsLeft, builder: builder));
        return id;
    }

    static AssignWorkers Build(int count, string city = "town_a") => new AssignWorkers(city, Projects.BuildRole, count);

    static StartProject Housing() => new StartProject("housing", "town_a", 1, 1);

    // R-CITY-011: a start takes one of the city's people without a job as its builder. The builder holds no job, so the
    // employed count is unchanged, but nobody else can take them: the idle civilians and the job limit are 1 lower.
    [Fact]
    public void Gdd03StartTakesAFreePersonAsBuilder()
    {
        var (c, s) = Game();   // 10 civilians, 8 in jobs: 2 without a job
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        var p = r.State.CityProjects.Values.Single();
        Assert.True(p.Builder);
        Assert.Equal(1, Projects.Builders(r.State, "town_a"));
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.ProjectBuilder);
        Assert.Equal(("town_a", "housing", "Town A: a builder started on a housing project"), (line.Target, string.Join("|", line.Args), line.Message));
        var before = Step(c, s).State;
        var x = r.State.Cities["town_a"]; var was = before.Cities["town_a"];
        Assert.Equal(was.Employed, x.Employed);
        Assert.Equal(Training.Idle(before, was) - 1, Training.Idle(r.State, x));
        Assert.Equal(Training.Available(before, "town_a", Array.Empty<Command>()) - 1, Training.Available(r.State, "town_a", Array.Empty<Command>()));
        // The job limit counts down from the city's people, so give it room to show the builder.
        var roomy = new City(x.Id, x.HousingCapacity, 20, x.Civilians, x.Employed, x.Refugees, x.Recruits, x.Condition, x.Craftsmen, x.JobsDefault, x.Qualified);
        var roomyWas = new City(was.Id, was.HousingCapacity, 20, was.Civilians, was.Employed, was.Refugees, was.Recruits, was.Condition, was.Craftsmen, was.JobsDefault, was.Qualified);
        Assert.Equal(Cities.MaxWorkers(before, roomyWas) - 1, Cities.MaxWorkers(r.State, roomy));
    }

    // R-CITY-011, R-PROJ-003: with nobody free at the start, the project has no builder and waits: no progress, nothing
    // added, while the Money was still paid at the start (R-PROJ-001). The check runs once, at the start.
    [Fact]
    public void Gdd03StartWithNobodyFreeWaits()
    {
        var (c, s) = Game(civilians: 10, employed: 10, jobs: 10);
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.Paid && w.Args[0] == CauseKeys.Purpose.Project);
        var id = r.State.CityProjects.Keys.Single();
        var wait = Assert.Single(r.Why, w => w.Key == CauseKeys.ProjectWaiting);
        Assert.Equal(("housing|" + Projects.NoBuilder, "Town A: a housing project waits: no builder"), (string.Join("|", wait.Args), wait.Message));
        var state = r.State;
        for (int i = 1; i < 12; i++) state = Step(c, state).State;
        var p = state.CityProjects[id];
        Assert.False(p.Builder);
        Assert.Equal((2, (DateTime?)null), (p.TurnsLeft, p.BuiltOn));
        var x = state.Cities["town_a"]; var was = s.Cities["town_a"];
        Assert.Equal((was.HousingCapacity, was.JobSlots), (x.HousingCapacity, x.JobSlots));
    }

    // R-CITY-011 with R-CITY-009: the automatic builder never takes anyone out of a job, the default's jobs included.
    [Fact]
    public void Gdd03AutoBuilderNeverTakesADefaultJob()
    {
        var (c, s) = Game(civilians: 8, employed: 8, jobs: 8, jobsDefault: true);
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        Assert.False(r.State.CityProjects.Values.Single().Builder);
        Assert.Equal(8, r.State.Cities["town_a"].Employed);
    }

    // R-CITY-011: the start takes its builder when it runs, in queue order: a Train queued first takes the one free person;
    // queued after the start, the Train finds nobody idle.
    [Fact]
    public void Gdd03AutoBuilderFollowsQueueOrder()
    {
        var (c, s) = Game(civilians: 10, employed: 9, jobs: 9);
        var first = Step(c, s, new Train("town_a", "halutz", 1), Housing());
        Assert.Empty(first.Rejected);
        Assert.False(first.State.CityProjects.Values.Single().Builder);
        var after = Step(c, s, Housing(), new Train("town_a", "halutz", 1));
        Assert.True(after.State.CityProjects.Values.Single().Builder);
        var refused = Assert.Single(after.Rejected);
        Assert.IsType<Train>(refused.Command);
        Assert.Equal("Town A has 0 civilians without work", refused.Reason);
    }

    // R-PROJ-003 ("One builder, today's times"): with a builder from its start, a project is built after exactly its
    // kind's turns, and adds its capacity, condition and job slots once.
    [Theory]
    [InlineData("transit_camp", 1)]
    [InlineData("industry", 2)]
    public void Gdd03BuiltInTodaysTurns(string kind, int turns)
    {
        // town_a's condition at 0.8: the kind's change (the transit camp's -0.1) is never cut at 0 or 1 (R-HOUSE-001), and
        // it stays above the gdd-0.3 surcharge and cohesion thresholds (R-HOUSE-005), so only the condition differs below.
        (City Was, City Built, City Later, ProjectKind K) Run(string projects)
        {
            var (c, s) = Game(projects: projects);
            s.Policies[MapActions.MaabarotPolicy] = 1;
            s.Cities["town_a"] = s.Cities["town_a"].WithCondition(0.8);
            var row = c.ProjectKinds.Single(x => x.Id == kind);
            var state = Step(c, s, new StartProject(kind, "town_a", 1, 1)).State;
            var id = state.CityProjects.Keys.Single();
            Assert.True(state.CityProjects[id].Builder || state.CityProjects[id].BuiltOn != null);
            for (int i = 1; i < turns; i++)
            {
                Assert.Null(state.CityProjects[id].BuiltOn);
                state = Step(c, state).State;
            }
            Assert.NotNull(state.CityProjects[id].BuiltOn);
            return (s.Cities["town_a"], state.Cities["town_a"], Step(c, state).State.Cities["town_a"], row);
        }
        var (was, built, later, k) = Run(Fixture.GddProjects + ArmsWorkshopTests.Row);
        Assert.Equal((was.HousingCapacity + k.Capacity, was.JobSlots + k.JobSlots), (built.HousingCapacity, built.JobSlots));
        Assert.Equal((built.HousingCapacity, built.JobSlots), (later.HousingCapacity, later.JobSlots));
        // The condition, against the same run with the kinds' condition at 0: the gdd-0.3 housing step (R-HOUSE-002) moves
        // both runs alike, so the difference is the kind's condition, on the turn it is built and, added once, after it.
        var (_, plain, plainLater, zeroed) = Run(Fixture.GddProjects.Replace("condition: -0.1", "condition: 0") + ArmsWorkshopTests.Row);
        Assert.Equal(0.0, zeroed.Condition);   // the comparison run's kind has no condition change, so the difference is k's
        Assert.Equal(plain.Condition!.Value + k.Condition, built.Condition!.Value, 9);
        Assert.Equal(plainLater.Condition!.Value + k.Condition, later.Condition!.Value, 9);
    }

    // R-PROJ-003: Money is never an input to progress.
    [Fact]
    public void Gdd03ExtraMoneyDoesNotChangeTheCompletionTurn()
    {
        DateTime? BuiltOn(double treasury)
        {
            var (c, s) = Game();
            s.Treasury = treasury; s.Debt = 0;
            var state = Step(c, s, Housing()).State;
            Assert.True(state.CityProjects.Values.Single().Builder);
            for (int i = 0; i < 4; i++) state = Step(c, state).State;
            return state.CityProjects.Values.Single().BuiltOn;
        }
        var poor = BuiltOn(1);
        Assert.NotNull(poor);
        Assert.Equal(poor, BuiltOn(1000));
    }

    // R-CITY-011, R-PROJ-003: taking the builder off pauses the project with its progress kept; putting one back resumes it,
    // so it is built exactly as many turns later as it waited.
    [Fact]
    public void Gdd03UnstaffingPausesAndKeepsProgress()
    {
        // Housing at the real game's 6 turns, so a pause fits before it is built.
        var (c, s) = Game(projects: Fixture.GddProjects.Replace("cost: 3, turns: 2", "cost: 3, turns: 6") + ArmsWorkshopTests.Row);
        int BuiltAt(bool pause)
        {
            var state = Step(c, s, Housing()).State;   // turn 1
            var id = state.CityProjects.Keys.Single();
            Assert.True(state.CityProjects[id].Builder);
            for (int turn = 2; turn <= 20; turn++)
            {
                var orders = !pause ? Array.Empty<Command>() : turn == 3 ? new Command[] { Build(0) } : turn == 6 ? new Command[] { Build(1) } : Array.Empty<Command>();
                var left = state.CityProjects[id].TurnsLeft;
                var r = Step(c, state, orders);
                Assert.Empty(r.Rejected);
                if (pause && turn >= 3 && turn <= 5) Assert.Equal(left, r.State.CityProjects[id].TurnsLeft);
                state = r.State;
                if (state.CityProjects[id].BuiltOn != null) return turn;
            }
            throw new InvalidOperationException("never built");
        }
        Assert.Equal(6, BuiltAt(false));
        Assert.Equal(9, BuiltAt(true));
    }

    // R-CITY-011: a rise puts builders on the projects that have none, in ascending id; a fall takes them off, in descending id.
    [Fact]
    public void Gdd03AssignBuildFillsAscendingAndRemovesDescending()
    {
        var (c, s) = Game(civilians: 10, employed: 6);
        // 5 turns left, so none is built (and its builder freed) during the two turns.
        var one = Add(s, "housing", 1, 1, 5, false);
        var two = Add(s, "industry", 2, 0, 5, true);
        var three = Add(s, "housing", 0, 1, 5, false);
        var up = Step(c, s, Build(2));
        Assert.Empty(up.Rejected);
        Assert.Equal((true, true, false), (up.State.CityProjects[one].Builder, up.State.CityProjects[two].Builder, up.State.CityProjects[three].Builder));
        var down = Step(c, up.State, Build(1));
        Assert.Empty(down.Rejected);
        Assert.Equal((true, false, false), (down.State.CityProjects[one].Builder, down.State.CityProjects[two].Builder, down.State.CityProjects[three].Builder));
        // The Why screen says which project the fall freed a builder from (slice B); the rise freed none.
        Assert.DoesNotContain(up.Why, w => w.Key == CauseKeys.ProjectBuilderFreed);
        var freed = Assert.Single(down.Why, w => w.Key == CauseKeys.ProjectBuilderFreed);
        Assert.Equal(("town_a", "industry"), (freed.Target, freed.Args.Single()));
    }

    // R-CITY-011: the order's refusals leave the state as it was; under gdd-0.1 the role does not exist.
    [Theory]
    [InlineData("town_a", 4, true, "Town A has 3 city projects being built")]
    [InlineData("town_a", 3, true, "Town A has 1 civilians and craftsmen without a job")]
    [InlineData("town_a", -1, true, "The number of workers cannot be negative")]
    [InlineData("nowhere", 1, true, "There is no such city")]
    [InlineData("town_a", 1, false, "There is no such role")]
    public void Gdd03BuildRefusals(string city, int count, bool gdd03, string reason)
    {
        var (c, s) = Game(civilians: 10, employed: 8, gdd03: gdd03);   // 2 without a job
        Add(s, "housing", 1, 1, 2, gdd03);   // under gdd-0.3 its builder is one of the 2
        Add(s, "industry", 2, 0, 2, false);
        Add(s, "housing", 0, 1, 2, false);
        var r = Step(c, s, Build(count, city));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    [Fact]
    public void Gdd03BuildRefusesOneProjectInTheSingular()
    {
        var (c, s) = Game();
        Add(s, "housing", 1, 1, 2, false);
        Assert.Equal("Town A has 1 city project being built", Assert.Single(Step(c, s, Build(2)).Rejected).Reason);
    }

    // R-CITY-011, R-PROJ-002: a builder is freed when the project is built or cancelled, and does not move to another
    // project by itself; a cancel returns no Money.
    [Fact]
    public void Gdd03BuilderFreedOnCompletionAndCancel()
    {
        var (c, s) = Game();
        var done = Add(s, "housing", 1, 1, 1, true);
        var waiting = Add(s, "industry", 2, 0, 2, false);
        var built = Step(c, s);
        var r = built.State;
        // The Why screen says the builder is free again (slice B): once, for the built project, not the one with no builder.
        var freed = Assert.Single(built.Why, w => w.Key == CauseKeys.ProjectBuilderFreed);
        Assert.Equal(("town_a", "housing"), (freed.Target, freed.Args.Single()));
        Assert.Equal("Town A: a builder left a housing project and is free again", freed.Message);
        Assert.NotNull(r.CityProjects[done].BuiltOn);
        Assert.False(r.CityProjects[done].Builder);
        Assert.False(r.CityProjects[waiting].Builder);
        Assert.Equal(2, r.CityProjects[waiting].TurnsLeft);
        Assert.Equal(0, Projects.Builders(r, "town_a"));

        var (c2, s2) = Game();
        var id = Add(s2, "housing", 1, 1, 2, true);
        var cancel = Step(c2, s2, new CancelProject(id));
        Assert.Empty(cancel.Rejected);
        Assert.Equal("housing", Assert.Single(cancel.Why, w => w.Key == CauseKeys.ProjectBuilderFreed).Args.Single());
        Assert.Equal(0, Projects.Builders(cancel.State, "town_a"));
        var none = Step(c2, s2).State;
        Assert.Equal(Training.Idle(none, none.Cities["town_a"]) + 1, Training.Idle(cancel.State, cancel.State.Cities["town_a"]));
        Assert.Equal(none.Treasury, cancel.State.Treasury, 9);
    }

    // R-CITY-011 with R-ARMS-006: builders and workshop workers share the city's people without a job; step 5 (the
    // projects) comes before step 6 (the workshops), so with one such person the project advances and the workshop pauses.
    [Fact]
    public void Gdd03BuilderAndWorkshopShareTheFreePeople()
    {
        var (c, s, workshop) = ArmsWorkshopTests.Built(civilians: 10, employed: 9, jobs: 9, jobsDefault: false);
        s.Workshops.Add(workshop, new Workshop(workshop, true, SyntheticWorkshopTier.Ordinary));
        var id = Add(s, "housing", 2, 0, 2, true);
        var r = Step(c, s);
        Assert.Equal(1, r.State.CityProjects[id].TurnsLeft);
        var paused = Assert.Single(r.Why, w => w.Key == CauseKeys.WorkshopPaused);
        Assert.Equal(Workshops.NoWorker, paused.Args[0]);
    }

    // R-CITY-011 with R-CITY-010: a held city's project keeps its builder and its turns.
    [Fact]
    public void Gdd03HeldCityKeepsItsBuilder()
    {
        var (c, s) = Game();
        var id = Add(s, "housing", 1, 1, 2, true);
        s.Units.Add(s.NextId, new Unit(s.NextId++, "enemy", 1, 0, side: "egypt", strength: 3));
        var p = Step(c, s).State.CityProjects[id];
        Assert.True(p.Builder);
        Assert.Equal(2, p.TurnsLeft);
    }

    // R-CITY-011 with R-CITY-009: the job default fills jobs from the people without a job who are not builders.
    [Fact]
    public void Gdd03JobDefaultLeavesBuildersOut()
    {
        var (c, s) = Game(civilians: 10, employed: 6, jobs: 10, jobsDefault: true);
        Add(s, "housing", 1, 1, 2, true);
        var x = Step(c, s).State.Cities["town_a"];
        Assert.Equal(9, x.Employed);
        Assert.True(x.Employed <= x.Civilians + x.Craftsmen - 1);
    }

    // R-PROJ-001, R-PROJ-003: gdd-0.1 is unchanged: its projects need no workers, take no builder and write no new field.
    [Fact]
    public void Gdd01ProjectsStillNeedNoWorkers()
    {
        var (c, s) = Game(civilians: 10, employed: 10, jobs: 10, gdd03: false);   // nobody free
        var one = Step(c, s, Housing());
        Assert.Empty(one.Rejected);
        Assert.DoesNotContain(one.Why, w => w.Key == CauseKeys.ProjectBuilder || w.Key == CauseKeys.ProjectWaiting);
        var id = one.State.CityProjects.Keys.Single();
        Assert.False(one.State.CityProjects[id].Builder);
        Assert.Equal(FormattableString.Invariant($"{id}:housing@town_a,1,1/1"), one.State.CityProjects[id].Canonical());
        var two = Step(c, one.State).State;
        Assert.NotNull(two.CityProjects[id].BuiltOn);
        Assert.Equal(10, two.Cities["town_a"].Employed);
    }

    // R-CITY-011: the builder is written into the state hash only when set.
    [Fact]
    public void TheBuilderIsHashedOnlyWhenSet()
    {
        var p = new CityProject(5, "housing", "town_a", 1, 1, 2);
        Assert.Equal("5:housing@town_a,1,1/2", p.Canonical());
        Assert.Equal("5:housing@town_a,1,1/2;builder", p.WithBuilder(true).Canonical());
        Assert.Equal("5:housing@town_a,1,1/done,1948-05-15", p.WithBuilder(true).Built(new DateTime(1948, 5, 15)).Canonical());
        // The hash leaves the flag out once built, so check the record itself: Built frees the builder, and Builders,
        // which counts the flag, never counts a built project (#1700 slice A review NIT).
        Assert.False(p.WithBuilder(true).Built(new DateTime(1948, 5, 15)).Builder);
    }
    // R-SAVE with R-CITY-011 (review MUST-1): a save is the command log, so a game with builders, saved and loaded, replays to
    // the same state, the builders included, and goes on identically.
    [Fact]
    public void Gdd03BuildersSurviveASaveAndLoad()
    {
        // Housing and industry at 6 turns, so both are still being built when the game is saved.
        var c = ArmsWorkshopTests.Load(true, Fixture.GddProjects.Replace("turns: 2,", "turns: 6,") + ArmsWorkshopTests.Row);
        var orders = new Dictionary<int, Command[]>
        {
            [0] = new Command[] { Housing(), new StartProject("industry", "town_a", 2, 0) },
            [1] = new Command[] { Build(1) },
        };
        var game = GameSession.New(c, 7, Mode.Historical);
        for (int t = 0; t < 2; t++) game.Step(orders[t]);
        Assert.Contains(";builder", game.State.Describe());
        var loaded = GameSession.Load(c, game.Save.Write(), out _);
        Assert.Equal(game.State.Hash(), loaded.State.Hash());
        game.Step(Array.Empty<Command>());
        loaded.Step(Array.Empty<Command>());
        Assert.Equal(game.State.Hash(), loaded.State.Hash());
    }

    // R-TRAIN-001, R-ARMS-005, R-CITY-001 with R-CITY-011 (review SHOULD-1): the limits a panel reads with this turn's queue
    // count the builder a queued gdd-0.3 start takes, so a count up to them is accepted whichever order runs first.
    [Fact]
    public void Gdd03QueuedStartCountsInThePanelLimits()
    {
        var (c, s) = Game(civilians: 10, employed: 9, jobs: 20);   // one free person
        var queued = new Command[] { Housing() };
        Assert.Equal(0, Training.Available(s, "town_a", queued));
        var train = Step(c, s, Housing(), new Train("town_a", "halutz", 1));
        Assert.Single(train.Rejected);   // the limit of 0 is what makes this refusal expected
        Assert.Equal(9, Cities.MaxWorkers(s, "town_a", queued));
        Assert.Empty(Step(c, s, Housing(), new AssignWorkers("town_a", "job", 9)).Rejected);
        Assert.Single(Step(c, s, Housing(), new AssignWorkers("town_a", "job", 10)).Rejected);
        var (wc, ws, _) = ArmsWorkshopTests.Built(civilians: 10, employed: 9, jobs: 9, jobsDefault: false);
        Assert.Null(Workshops.Check(wc, ws, "town_a", 1, Array.Empty<Command>()));
        Assert.NotNull(Workshops.Check(wc, ws, "town_a", 1, new Command[] { new StartProject("housing", "town_a", 2, 0) }));
        // Under gdd-0.1 a start takes nobody, so nothing changes.
        var (oc, os) = Game(civilians: 10, employed: 9, jobs: 20, gdd03: false);
        Assert.Equal(1, Training.Available(os, "town_a", queued));
    }

    // R-PROJ-003, R-ARMS-006 (review SHOULD-3): with two builders and one person without a job, the lower id advances, the
    // higher waits with no_worker, and the city's staffed workshop pauses at step 6.
    [Fact]
    public void Gdd03MoreBuildersThanPeopleWaitInAscendingId()
    {
        var (c, s, workshop) = ArmsWorkshopTests.Built(civilians: 10, employed: 9, jobs: 9, jobsDefault: false);
        s.Workshops.Add(workshop, new Workshop(workshop, true, SyntheticWorkshopTier.Ordinary));
        var low = Add(s, "housing", 2, 0, 2, true);
        var high = Add(s, "industry", 0, 1, 2, true);
        var r = Step(c, s);
        Assert.Equal((1, 2), (r.State.CityProjects[low].TurnsLeft, r.State.CityProjects[high].TurnsLeft));
        var wait = Assert.Single(r.Why, w => w.Key == CauseKeys.ProjectWaiting);
        Assert.Equal(("industry", Projects.NoWorker), (wait.Args[0], wait.Args[1]));
        Assert.Equal("Town A: an industry project waits: no person without a job is left for its builder", wait.Message);
        Assert.Equal(Workshops.NoWorker, Assert.Single(r.Why, w => w.Key == CauseKeys.WorkshopPaused).Args[0]);
        Assert.True(r.State.CityProjects[high].Builder);   // a wait keeps the builder
    }

    // R-CITY-011 (review SHOULD-3): a city with nobody starts its project without a builder, and the build order is refused.
    [Fact]
    public void Gdd03ACityWithNobodyTakesNoBuilder()
    {
        var (c, s) = Game(civilians: 0, employed: 0, jobs: 0);
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        Assert.False(r.State.CityProjects.Values.Single().Builder);
        Assert.Equal("Town A has 0 civilians and craftsmen without a job", Assert.Single(Step(c, r.State, Build(1)).Rejected).Reason);
    }

    // R-CITY-011 with R-CITY-009 (review NIT-2): a person the job default has not yet put in an open job holds no job, so a
    // start takes them; the default then fills one job fewer.
    [Fact]
    public void Gdd03AutoBuilderTakesAPersonTheDefaultHadNotPlaced()
    {
        var (c, s) = Game(civilians: 10, employed: 8, jobs: 10, jobsDefault: true);
        var r = Step(c, s, Housing());
        Assert.True(r.State.CityProjects.Values.Single().Builder);
        Assert.Equal(9, r.State.Cities["town_a"].Employed);
        Assert.Equal(10, Step(c, s).State.Cities["town_a"].Employed);
    }
    // R-CITY-011 (second review): a start's free person excludes an existing builder and a staffed workshop's worker.
    [Fact]
    public void Gdd03StartSeesAnExistingBuilder()
    {
        var (c, s) = Game(civilians: 10, employed: 9, jobs: 9);   // one person without a job, already a builder
        Add(s, "industry", 2, 0, 5, true);
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        Assert.False(r.State.CityProjects.Values.Single(p => p.Kind == "housing").Builder);
    }

    [Fact]
    public void Gdd03StartSeesAStaffedWorkshop()
    {
        var (c, s, workshop) = ArmsWorkshopTests.Built(civilians: 10, employed: 9, jobs: 9, jobsDefault: false);
        s.Workshops.Add(workshop, new Workshop(workshop, true, SyntheticWorkshopTier.Ordinary));   // its worker is the one
        var r = Step(c, s, new StartProject("housing", "town_a", 2, 0));
        Assert.Empty(r.Rejected);
        Assert.False(r.State.CityProjects.Values.Single(p => p.Kind == "housing").Builder);
    }

    // R-CITY-011 (second review): the start runs in queue order against AssignWorkers too: a job order queued first takes
    // the free person; queued after the start, it is refused past the people less the builder.
    [Fact]
    public void Gdd03AutoBuilderFollowsQueueOrderWithAJobOrder()
    {
        var (c, s) = Game(civilians: 10, employed: 9, jobs: 10);
        var jobFirst = Step(c, s, new AssignWorkers("town_a", "job", 10), Housing());
        Assert.Empty(jobFirst.Rejected);
        Assert.False(jobFirst.State.CityProjects.Values.Single().Builder);
        var startFirst = Step(c, s, Housing(), new AssignWorkers("town_a", "job", 10));
        Assert.True(startFirst.State.CityProjects.Values.Single().Builder);
        Assert.Equal("Town A has 9 civilians and craftsmen", Assert.Single(startFirst.Rejected).Reason);
    }

    // R-CITY-011 with R-ARMS-005's job-default rule (second review): while the default is on, a build order may take its
    // builder from the default's jobs (the employed fall, with the city's line) and is refused only past the people.
    [Fact]
    public void Gdd03BuildOrderMayTakeFromTheJobDefault()
    {
        var (c, s) = Game(civilians: 10, employed: 10, jobs: 10, jobsDefault: true);
        var id = Add(s, "housing", 1, 1, 2, false);
        var r = Simulation.Preview(c, s, new Command[] { Build(1) });   // the orders only, before the turn's housing step
        Assert.Empty(r.Rejected);
        Assert.True(r.State.CityProjects[id].Builder);
        var x = r.State.Cities["town_a"];
        Assert.Equal((9, true), (x.Employed, x.JobsDefault));
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.WorkersAssigned);
        Assert.Equal("Town A: 9 groups in work", line.Message);

        var (c2, s2) = Game(civilians: 1, employed: 1, jobs: 1, jobsDefault: true);
        Add(s2, "housing", 1, 1, 2, false);
        Add(s2, "industry", 2, 0, 2, false);
        Assert.Equal("Town A has 1 civilians and craftsmen", Assert.Single(Step(c2, s2, Build(2)).Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-011 (second review): staffing a workshop from the job default leaves the builders out of the jobs.
    [Fact]
    public void Gdd03WorkshopStaffingFromTheDefaultLeavesBuildersOut()
    {
        var (c, s, _) = ArmsWorkshopTests.Built(civilians: 10, employed: 9, jobs: 10, jobsDefault: true);
        Add(s, "housing", 2, 0, 2, true);
        var r = Simulation.Preview(c, s, new Command[] { ArmsWorkshopTests.Staff(1) });
        Assert.Empty(r.Rejected);
        Assert.Equal(8, r.State.Cities["town_a"].Employed);   // 10 people less the worker and the builder
    }

    // R-PROJ-001, R-PROJ-003 (plan test 14, second review): under gdd-0.1 a start in a city with people without a job takes
    // no builder, writes no new field or Why line, and changes no count; byte identity with main is the golden manifest's.
    [Fact]
    public void Gdd01StartWithFreePeopleTakesNoBuilder()
    {
        var (c, s) = Game(civilians: 10, employed: 8, jobs: 8, gdd03: false);
        var one = Step(c, s, Housing());
        var none = Step(c, s);
        Assert.Empty(one.Rejected);
        Assert.DoesNotContain(";builder", one.State.Describe());
        Assert.DoesNotContain(one.Why, w => w.Key == CauseKeys.ProjectBuilder || w.Key == CauseKeys.ProjectWaiting);
        var x = one.State.Cities["town_a"]; var was = none.State.Cities["town_a"];
        Assert.Equal((Training.Idle(none.State, was), Cities.MaxWorkers(none.State, was), was.Employed),
            (Training.Idle(one.State, x), Cities.MaxWorkers(one.State, x), x.Employed));
        var id = one.State.CityProjects.Keys.Single();
        Assert.NotNull(Step(c, one.State).State.CityProjects[id].BuiltOn);
    }

    // R-PROJ-001, R-PROJ-003 with R-CITY-011 (#1700 slice A review: a gdd-0.1 hash check, plan test 14): under gdd-0.1 a
    // start takes no builder, so a job order up to all the city's people is accepted before or after it and the whole state
    // hash is the same either way, on the start's turn and the turn it is built. Under gdd-0.3 the same two orders differ
    // (Gdd03AutoBuilderFollowsQueueOrderWithAJobOrder). Compared in the same run: no literal hash outside the golden manifest.
    [Fact]
    public void Gdd01StartOrderLeavesTheHashUnchanged()
    {
        var (c, s) = Game(civilians: 10, employed: 9, jobs: 10, gdd03: false);   // one person without a job
        var jobFirst = Step(c, s, new AssignWorkers("town_a", "job", 10), Housing());
        var startFirst = Step(c, s, Housing(), new AssignWorkers("town_a", "job", 10));
        Assert.Empty(jobFirst.Rejected);
        Assert.Empty(startFirst.Rejected);
        Assert.Equal(jobFirst.State.Hash(), startFirst.State.Hash());
        Assert.Equal(Step(c, jobFirst.State).State.Hash(), Step(c, startFirst.State).State.Hash());
    }
}
