using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1701 (GDD 0.3 arms workshops), on the aggregate cities: the gdd-0.3 `arms_workshop` project kind (R-ARMS-004).
/// The fixture's town_a is on (1, 0), with (1, 1) and (2, 0) land next to it; its workshop row has the real row's numbers
/// (owner, 2 November 2026, OQ2: "IL 15m, 3 turns (Recommended)").</summary>
public class ArmsWorkshopTests
{
    internal const string Row = "  - { id: arms_workshop, name: an arms workshop, cost: 15, turns: 3, capacity: 0, condition: 0, job_slots: 0, worker_slots: 1, rulesets: [gdd-0.3] }\n";

    /// <summary>The GDD fixture with <paramref name="projects"/> as projects.yaml, under gdd-0.3 (with the real balance
    /// catalogue, which that ruleset requires) or gdd-0.1.</summary>
    internal static Dictionary<string, string> Files(bool gdd03, string projects = Fixture.GddProjects + Row)
    {
        var f = Fixture.Gdd(new Dictionary<string, string>
        {
            ["projects.yaml"] = projects,
            // An enemy type, so a test can hold town_a (R-CITY-010).
            ["unit_types.yaml"] = Fixture.GddUnitTypes + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
        });
        if (!gdd03) return f;
        f["eras.yaml"] = f["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + SaveGame.Gdd03Ruleset);
        f[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        return f;
    }

    internal static Content Load(bool gdd03, string projects = Fixture.GddProjects + Row) => ContentLoader.Load(Fixture.Write(Files(gdd03, projects)));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    // R-ARMS-004: the row is read under gdd-0.3 only; gdd-0.1 keeps exactly its kinds, in their order, so its Build list,
    // its StartProject and its hash do not change.
    [Fact]
    public void TheWorkshopIsAKindUnderGdd03Only()
    {
        Assert.Equal(new[] { "housing", "transit_camp", "industry" }, Load(false).ProjectKinds.Select(k => k.Id));
        var c = Load(true);
        Assert.Equal(new[] { "housing", "transit_camp", "industry", "arms_workshop" }, c.ProjectKinds.Select(k => k.Id));
        var k = c.ProjectKinds.Single(x => x.Id == "arms_workshop");
        Assert.Equal((15.0, 3, 0, 0.0, 0, 1), (k.Cost, k.Turns, k.Capacity, k.Condition, k.JobSlots, k.WorkerSlots));
        Assert.Equal(new[] { SaveGame.Gdd03Ruleset }, k.Rulesets);
        Assert.Equal(0, c.ProjectKinds.Single(x => x.Id == "industry").WorkerSlots);
    }

    // R-ARMS-004 (OQ2, owner 2 November 2026: "IL 15m, 3 turns (Recommended)"; OQ8: the Hebrew name awaits a native read):
    // the real game (gdd-0.1) has its four kinds, and the real row loads under gdd-0.3 with the ruled numbers.
    [Fact]
    public void TheRealRowIsTheRuledWorkshop()
    {
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(new[] { "housing", "transit_camp", "industry", "winery" }, real.ProjectKinds.Select(k => k.Id));
        var line = File.ReadAllLines(Path.Combine(RealContentTests.ContentRoot(), "projects.yaml")).Single(l => l.Contains("id: arms_workshop"));
        var k = Load(true, Fixture.GddProjects + line + "\n").ProjectKinds.Single(x => x.Id == "arms_workshop");
        Assert.Equal((15.0, 3, 0, 0.0, 0, 1), (k.Cost, k.Turns, k.Capacity, k.Condition, k.JobSlots, k.WorkerSlots));
        Assert.Equal(("סדנת נשק", HebrewReview.AgentChecked), (k.NameHe, k.NameHeReview));
    }

    // R-ARMS-004: under gdd-0.1 the kind does not exist, so StartProject refuses it as unknown and nothing changes.
    [Fact]
    public void Gdd01RefusesTheWorkshopAsAnUnknownKind()
    {
        var c = Load(false);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new StartProject("arms_workshop", "town_a", 1, 1));
        Assert.Equal("There is no such city project", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-ARMS-004 with R-PROJ-001 and R-PROJ-003: under gdd-0.3 the workshop is built like any project: paid in full at the
    // start (borrowing the shortfall), built after its 3 turns, and it adds no housing, condition or job slots.
    [Fact]
    public void Gdd03BuildsTheWorkshopLikeAnyProject()
    {
        var c = Load(true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = 10; s.Debt = 0;
        var one = Step(c, s, new StartProject("arms_workshop", "town_a", 1, 1));
        Assert.Empty(one.Rejected);
        Assert.Equal(-15, one.Why.Single(w => w.Key == CauseKeys.Paid && w.Args[0] == CauseKeys.Purpose.Project).Delta, 9);
        Assert.Equal(5, one.Why.Single(w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Project).Delta, 9);   // IL 15m against IL 10m
        var p = one.State.CityProjects.Values.Single();
        Assert.Equal(("arms_workshop", 2, (DateTime?)null), (p.Kind, p.TurnsLeft, p.BuiltOn));
        Assert.True(p.Builder);   // R-CITY-011 (#1700): town_a has people without a job, so the start takes one as its builder
        var built = Step(c, Step(c, one.State).State).State;
        Assert.NotNull(built.CityProjects[p.Id].BuiltOn);
        var before = s.Cities["town_a"]; var after = built.Cities["town_a"];
        Assert.Equal((before.HousingCapacity, before.JobSlots, before.Condition), (after.HousingCapacity, after.JobSlots, after.Condition));
    }

    // R-ARMS-004: the loader refuses a row that could not be a gdd-0.3 workshop slot, naming the field.
    [Theory]
    [InlineData("rulesets: [gdd-0.3]", "rulesets: [gdd-0.9]", false, "projects[arms_workshop].rulesets")]
    [InlineData("worker_slots: 1", "worker_slots: 2", true, "projects[arms_workshop].worker_slots")]
    [InlineData("worker_slots: 1", "worker_slots: -1", true, "projects[arms_workshop].worker_slots")]
    [InlineData("job_slots: 0, worker_slots", "job_slots: 1, worker_slots", true, "projects[arms_workshop].job_slots")]
    [InlineData("capacity: 0, condition: 0, job_slots: 0, worker", "capacity: 2, condition: 0, job_slots: 0, worker", true, "projects[arms_workshop].capacity")]
    [InlineData("condition: 0, job_slots: 0, worker", "condition: 0.1, job_slots: 0, worker", true, "projects[arms_workshop].condition")]
    [InlineData("rulesets: [gdd-0.3]", "rulesets: [gdd-0.1, gdd-0.3]", true, "projects[arms_workshop].rulesets")]
    [InlineData(", rulesets: [gdd-0.3]", "", false, "projects[arms_workshop].rulesets")]
    [InlineData("worker_slots: 1", "worker_slots: 0", true, "projects[arms_workshop].worker_slots")]   // the catalogue's 1
    public void ABadWorkshopRowIsRefused(string from, string to, bool gdd03, string where)
    {
        Assert.Contains(from, Row);
        var e = Assert.Throws<ContentException>(() => Load(gdd03, Fixture.GddProjects + Row.Replace(from, to)));
        Assert.Contains(where, e.Message);
    }

    // ---- R-ARMS-005: the workshop's worker (slice B) ----

    /// <summary>A gdd-0.3 game whose town_a has <paramref name="jobs"/> job slots, <paramref name="civilians"/> civilians,
    /// <paramref name="employed"/> of them in jobs, and a built arms workshop on (1, 1): its id is the second value.</summary>
    internal static (Content C, GameState S, int Id) Built(int civilians = 10, int employed = 6, int jobs = 8, bool jobsDefault = true)
    {
        var c = Load(true);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, x.HousingCapacity, jobs, civilians, employed, 0, 0, x.Condition, jobsDefault: jobsDefault);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Workshops.ArmsWorkshop, "town_a", 1, 1, 0, s.Date));
        return (c, s, id);
    }

    internal static AssignWorkers Staff(int count, string city = "town_a") => new AssignWorkers(city, Workshops.Role, count);

    // R-ARMS-005 (27M-13, "Any free civilian. The cost tier follows the worker"; #1701 OQ6, owner 2 November 2026: "Accept
    // all four (Recommended)": every aggregate worker is priced as ordinary): AssignWorkers(city, "workshop", n) staffs the
    // city's first n built workshops with people without a job. The order changes no Money, profession or job.
    [Fact]
    public void StaffingTakesOnePersonWithoutAJob()
    {
        var (c, s, id) = Built(jobsDefault: false);
        var r = Step(c, s, Staff(1));
        Assert.Empty(r.Rejected);
        var w = r.State.Workshops[id];
        Assert.Equal((true, SyntheticWorkshopTier.Ordinary), (w.Staffed, w.WorkerTier));
        var line = Assert.Single(r.Why, x => x.Key == CauseKeys.WorkshopWorkers);
        Assert.Equal(("town_a", "1", "Town A: arms workshops staffed: 1"), (line.Target, string.Join("|", line.Args), line.Message));
        var x = r.State.Cities["town_a"]; var before = Step(c, s).State.Cities["town_a"];
        Assert.Equal((before.Civilians, before.Craftsmen, before.Employed, before.Qualified), (x.Civilians, x.Craftsmen, x.Employed, x.Qualified));
    }

    // R-ARMS-005 with R-CITY-001 and R-CITY-009: the worker holds no job, so the city's most workers is one fewer and the job
    // default fills one job fewer; R-CITY-002: the worker earns no job income.
    [Fact]
    public void TheWorkerHoldsNoJobAndEarnsNoWage()
    {
        var (c, s, _) = Built(civilians: 10, employed: 6, jobs: 12);
        var staffed = Step(c, s, Staff(1));
        Assert.Equal(9, Cities.MaxWorkers(staffed.State, staffed.State.Cities["town_a"]));
        Assert.Equal(9, staffed.State.Cities["town_a"].Employed);   // the job default (step 8) fills 9 of the 10
        Assert.Equal(10, Step(c, s).State.Cities["town_a"].Employed);
        double Wages(StepResult r) => r.Why.Single(w => w.Key == CauseKeys.WagesPaid && w.Target == "town_a").Delta;
        var next = Step(c, staffed.State);
        var nine = staffed.State.Clone(); nine.Workshops.Clear();
        nine.Cities["town_a"] = new City("town_a", nine.Cities["town_a"].HousingCapacity, 9, 10, 9, 0, 0, nine.Cities["town_a"].Condition);
        Assert.Equal(Wages(Step(c, nine)), Wages(next), 12);   // the wages of 9 jobs, as a city with 9 jobs and one idle
        var refused = Step(c, staffed.State, new AssignWorkers("town_a", "job", 10));
        Assert.Equal("Town A has 9 civilians and craftsmen", Assert.Single(refused.Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-001 (#2270): Cities.MaxWorkers(GameState, City) stops at 0 when staffed workshops outnumber the
    // city's people, so the job default never sets a negative Employed.
    [Fact]
    public void AStaffedWorkshopCannotMakeTheMaximumOrDefaultJobsNegative()
    {
        var (c, s, id) = Built(civilians: 0, employed: 0, jobs: 10);
        s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary));
        Assert.Equal(0, Cities.MaxWorkers(s, s.Cities["town_a"]));
        var r = Step(c, s);
        Assert.Equal(0, r.State.Cities["town_a"].Employed);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.WorkersAssigned && w.Target == "town_a");
    }

    public static IEnumerable<object[]> StaffRefusals() => new List<object[]>
    {
        new object[] { "gdd-0.1", "There is no such role" },
        new object[] { "unknown city", "There is no such city" },
        new object[] { "negative", "The number of workers cannot be negative" },
        new object[] { "more than built", "Town A has 1 arms workshop" },
        new object[] { "still building", "Town A has 0 arms workshops" },
        new object[] { "nobody without a job", "Town A has 0 civilians and craftsmen without a job" },
        new object[] { "nobody at all", "Town A has 0 civilians and craftsmen" },
    };

    // R-ARMS-005: each refusal leaves the state as if the order had not been given.
    [Theory]
    [MemberData(nameof(StaffRefusals))]
    public void StaffingRefusalsChangeNothing(string why, string reason)
    {
        // "nobody without a job": the player set the jobs (R-CITY-009's default is off), so no job is given up for it.
        var (c, s, id) = Built(civilians: why == "nobody at all" ? 0 : 10, employed: why == "nobody without a job" ? 10 : why == "nobody at all" ? 0 : 6, jobs: 10,
            jobsDefault: why != "nobody without a job");
        if (why == "gdd-0.1") { c = Load(false); s = Simulation.NewGame(c, 1, Mode.Historical); }
        if (why == "still building") s.CityProjects[id] = new CityProject(id, Workshops.ArmsWorkshop, "town_a", 1, 1, 2);
        var order = why switch
        {
            "unknown city" => Staff(1, "atlantis"),
            "negative" => Staff(-1),
            "more than built" => Staff(2),
            _ => Staff(1),
        };
        var r = Step(c, s, order);
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-ARMS-005: lowering the count unstaffs the last workshops; the record stays, so a paid cycle is kept (R-ARMS-007).
    [Fact]
    public void UnstaffingKeepsTheRecord()
    {
        var (c, s, id) = Built(jobsDefault: false);
        var off = Step(c, Step(c, s, Staff(1)).State, Staff(0));
        Assert.Empty(off.Rejected);
        Assert.False(off.State.Workshops[id].Staffed);
        Assert.Equal(new[] { "0" }, off.Why.Single(x => x.Key == CauseKeys.WorkshopWorkers).Args);
    }

    // R-ARMS-005 (#2270): Workshops.Staff applies the count to built workshops in ascending project-id order.
    [Fact]
    public void StaffingCountSelectsTheLowestProjectIdsAndKeepsLaterRecords()
    {
        var (c, s, first) = Built(civilians: 10, employed: 0, jobs: 0, jobsDefault: false);
        var firstProject = s.CityProjects[first];
        s.CityProjects.Remove(first);
        var second = s.NextId++;
        s.CityProjects.Add(second, new CityProject(second, Workshops.ArmsWorkshop, "town_a", 2, 0, 0, s.Date));
        s.CityProjects.Add(first, firstProject);   // inserted after the higher id; SortedList still orders it first

        var one = Step(c, s, Staff(1));
        Assert.True(one.State.Workshops[first].Staffed);
        Assert.False(one.State.Workshops.ContainsKey(second));

        var two = Step(c, one.State, Staff(2));
        Assert.True(two.State.Workshops[first].Staffed);
        Assert.True(two.State.Workshops[second].Staffed);

        var oneAgain = Step(c, two.State, Staff(1));
        Assert.True(oneAgain.State.Workshops[first].Staffed);
        Assert.False(oneAgain.State.Workshops[second].Staffed);
    }

    // R-ARMS-005 (#2270): Workshops.Staff adds no record for a never-staffed workshop that the order leaves off.
    [Fact]
    public void LeavingANeverStaffedWorkshopOffCreatesNoRecord()
    {
        var (c, s, id) = Built(jobsDefault: false);
        var r = Step(c, s, Staff(0));
        Assert.Empty(r.Rejected);
        Assert.False(r.State.Workshops.ContainsKey(id));
        Assert.DoesNotContain("wshop", r.State.Describe());
    }

    // R-ARMS-005 (#1722 slice E): Workshops.Check, which the hex panel asks before it offers a staffing order, gives exactly
    // the refusal the order gets at End turn, and changes nothing.
    [Theory]
    [MemberData(nameof(StaffRefusals))]
    public void CheckGivesTheOrdersOwnRefusalAndChangesNothing(string why, string reason)
    {
        var (c, s, id) = Built(civilians: why == "nobody at all" ? 0 : 10, employed: why == "nobody without a job" ? 10 : why == "nobody at all" ? 0 : 6, jobs: 10,
            jobsDefault: why != "nobody without a job");
        if (why == "gdd-0.1") { c = Load(false); s = Simulation.NewGame(c, 1, Mode.Historical); }
        if (why == "still building") s.CityProjects[id] = new CityProject(id, Workshops.ArmsWorkshop, "town_a", 1, 1, 2);
        var order = why switch { "unknown city" => Staff(1, "atlantis"), "negative" => Staff(-1), "more than built" => Staff(2), _ => Staff(1) };
        var before = s.Hash();
        Assert.Equal(reason, Workshops.Check(c, s, order.City, order.Count, Array.Empty<Command>()));
        Assert.Equal(before, s.Hash());
        Assert.Equal(reason, Assert.Single(Step(c, s, order).Rejected).Reason);
    }

    // R-ARMS-005: an order Check accepts is accepted: staffing one, unstaffing it again (a lower count is always accepted),
    // and, with two built and one staffed, staffing both.
    [Fact]
    public void CheckAcceptsWhatTheTurnAccepts()
    {
        var (c, s, _) = Built(jobsDefault: false);
        Assert.Null(Workshops.Check(c, s, "town_a", 1, Array.Empty<Command>()));
        var staffed = Step(c, s, Staff(1));
        Assert.Empty(staffed.Rejected);
        Assert.Equal(1, Workshops.Staffed(staffed.State, "town_a"));
        Assert.Null(Workshops.Check(c, staffed.State, "town_a", 0, Array.Empty<Command>()));
        var off = Step(c, staffed.State, Staff(0));
        Assert.Empty(off.Rejected);
        Assert.Equal(0, Workshops.Staffed(off.State, "town_a"));
        var two = staffed.State.Clone();
        SecondWorkshop(two);
        Assert.Null(Workshops.Check(c, two, "town_a", 2, Array.Empty<Command>()));
        var both = Step(c, two, Staff(2));
        Assert.Empty(both.Rejected);
        Assert.Equal(2, Workshops.Staffed(both.State, "town_a"));
    }

    /// <summary>A second built arms workshop for town_a, on (2, 0); its id, above the first's.</summary>
    static int SecondWorkshop(GameState s)
    {
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Workshops.ArmsWorkshop, "town_a", 2, 0, 0, s.Date));
        return id;
    }

    // R-ARMS-005 (#1722 review, H1): Check does not count the pending workshop order itself, only the job, Train and
    // reassignment orders beside it, so with two built, two free and one staffing order queued, two is still offered.
    [Fact]
    public void CheckIgnoresThePendingWorkshopOrderItself()
    {
        var (c, s, first) = Built(civilians: 10, employed: 8, jobs: 10, jobsDefault: false);
        var second = SecondWorkshop(s);
        Assert.Null(Workshops.Check(c, s, "town_a", 2, new Command[] { Staff(1) }));
        var r = Step(c, s, Staff(2));
        Assert.Empty(r.Rejected);
        Assert.True(r.State.Workshops[first].Staffed && r.State.Workshops[second].Staffed);
    }

    // R-ARMS-005 (#2270): Workshops.BuiltIn counts only this city's built projects whose kind has a worker slot.
    [Fact]
    public void BuiltCountsOnlyThisCitysProjectsWithWorkerSlots()
    {
        var (c, s, _) = Built(jobsDefault: false);
        var industry = s.NextId++;
        s.CityProjects.Add(industry, new CityProject(industry, "industry", "town_a", 2, 0, 0, s.Date));
        var elsewhere = s.NextId++;
        s.CityProjects.Add(elsewhere, new CityProject(elsewhere, Workshops.ArmsWorkshop, "town_b", 3, 0, 0, s.Date));

        Assert.Equal(1, Workshops.Built(c, s, "town_a"));
        Assert.Equal("Town A has 1 arms workshop", Assert.Single(Step(c, s, Staff(2)).Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-001 (#1722 slice E): with this turn's queue, Check counts a pending "job" order's jobs (which turn
    // the job default off) and every Train or reassignment there, so an order it accepts is still accepted if the job order
    // runs first. Here the job order takes the last free person, and the turn refuses the staffing with Check's words.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckCountsThisTurnsJobOrder(bool jobsDefault)
    {
        var (c, s, _) = Built(civilians: 10, employed: jobsDefault ? 10 : 9, jobs: 10, jobsDefault: jobsDefault);
        Assert.Null(Workshops.Check(c, s, "town_a", 1, Array.Empty<Command>()));
        var job = new AssignWorkers("town_a", "job", 10);
        var reason = Workshops.Check(c, s, "town_a", 1, new Command[] { job, Staff(1) });
        Assert.Equal("Town A has 0 civilians and craftsmen without a job", reason);
        Assert.Equal(reason, Assert.Single(Step(c, s, job, Staff(1)).Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-001 (#2289): Workshops.Check subtracts this turn's queued Train and pioneer-reassignment orders.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckCountsThisTurnsTrainingOrders(bool reassign)
    {
        var (c, s, _) = Built(civilians: 10, employed: 9, jobs: 10, jobsDefault: false);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithQualified(new Qualified(0, 0, new[] { 1 }));
        Command order = reassign
            ? new ReassignPioneers("town_a", "halutz", 1)
            : new Train("town_a", "halutz", 1);
        const string reason = "Town A has 0 civilians and craftsmen without a job";

        Assert.Equal(reason, Workshops.Check(c, s, "town_a", 1, new[] { order, Staff(1) }));
        Assert.Equal(reason, Assert.Single(Step(c, s, order, Staff(1)).Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-001 (#1722 slice E): the jobs stepper's limit counts a pending staffing order's newly staffed
    // workers, as Training.Available does, so a jobs count it allows is still accepted if the staffing runs first.
    [Fact]
    public void MaxWorkersCountsAPendingStaffingOrder()
    {
        var (c, s, _) = Built(civilians: 10, employed: 6, jobs: 10, jobsDefault: false);
        Assert.Equal(10, Cities.MaxWorkers(s, "town_a", Array.Empty<Command>()));
        Assert.Equal(9, Cities.MaxWorkers(s, "town_a", new Command[] { Staff(1) }));
        Assert.Equal("Town A has 9 civilians and craftsmen", Assert.Single(Step(c, s, Staff(1), new AssignWorkers("town_a", "job", 10)).Rejected).Reason);
        Assert.Empty(Step(c, s, Staff(1), new AssignWorkers("town_a", "job", 9)).Rejected);
    }

    // R-ARMS-005 (#1722 slice E): the refusals' English are format constants, so the UI can word them in its language
    // (SimBridge.ReasonText), and the turn's reasons are those formats filled in.
    [Fact]
    public void TheRefusalsAreTheFormatConstants()
    {
        string F(string format, params object[] v) => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, v);
        Assert.Equal("Town A has 1 arms workshop", F(Workshops.RefusedBuiltOne, "Town A"));
        Assert.Equal("Town A has 0 arms workshops", F(Workshops.RefusedBuilt, "Town A", 0));
        Assert.Equal("Town A has 0 civilians and craftsmen", F(Workshops.RefusedPeople, "Town A", 0));
        Assert.Equal("Town A has 0 civilians and craftsmen without a job", F(Workshops.RefusedPeopleNoJob, "Town A", 0));
    }

    // R-CITY-001 with R-ARMS-005 (#1722 review, H2): a jobs order past the city's civilians and craftsmen less its staffed
    // workshops is refused with the workshop order's own format, so the UI words both the same way (workshop.refused_people).
    [Fact]
    public void TheJobsOrdersPeopleRefusalIsTheWorkshopFormat()
    {
        var (c, s, _) = Built(civilians: 10, employed: 6, jobs: 10, jobsDefault: false);
        var staffed = Step(c, s, Staff(1)).State;
        var reason = Assert.Single(Step(c, staffed, new AssignWorkers("town_a", "job", 10)).Rejected).Reason;
        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeople, "Town A", 9), reason);
    }

    // ---- #1722 review (F1, C4): the hex panel's next-cycle price and cycle length come from the sim ----

    // R-ARMS-006: the price NextCyclePrice gives before a turn is the worker's tier price (gdd03-balance workshop.cost 5/4/3,
    // content/gdd03-balance.yaml) and what that turn's production step charges and records, at every worker tier.
    [Theory]
    [InlineData(SyntheticWorkshopTier.Ordinary, 5)]
    [InlineData(SyntheticWorkshopTier.Novice, 5)]
    [InlineData(SyntheticWorkshopTier.Apprentice, 4)]
    [InlineData(SyntheticWorkshopTier.Expert, 3)]
    public void NextCyclePriceIsWhatTheTurnCharges(SyntheticWorkshopTier tier, int price)
    {
        var (c, s, id) = Staffed(treasury: 100, tier: tier);
        var next = Workshops.NextCyclePrice(c, s, id);
        Assert.Equal((decimal?)price, next);
        var r = Step(c, s);
        Assert.Equal(next, r.State.Workshops[id].PaidCost);
        Assert.Equal(-(double)next!.Value, Assert.Single(Charges(r)).Delta, 12);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.Borrowed);
    }

    // R-ARMS-005, R-ARMS-006 (#1701 OQ6: every aggregate worker is priced as ordinary): a built workshop never staffed has no
    // record; its next cycle is priced at the tier staffing it would give, which is what its first cycle then charges.
    [Fact]
    public void NextCyclePriceOfANeverStaffedWorkshopIsTheOrdinaryTier()
    {
        var (c, s, id) = Built(jobsDefault: false);
        s.Treasury = 100;
        Assert.False(s.Workshops.ContainsKey(id));
        var next = Workshops.NextCyclePrice(c, s, id);
        Assert.Equal(5m, next);   // gdd03-balance workshop.cost, ordinary
        Assert.Equal(Rules.Price(SyntheticWorkshopTier.Ordinary), next);
        var r = Step(c, s, Staff(1));
        Assert.Equal(SyntheticWorkshopTier.Ordinary, r.State.Workshops[id].WorkerTier);
        Assert.Equal(next, r.State.Workshops[id].PaidCost);
        Assert.Equal(-(double)next!.Value, Assert.Single(Charges(r)).Delta, 12);
    }

    // NextCyclePrice is null, and changes nothing, where there is no next cycle to price: a cycle under way, a workshop still
    // being built, a project with no worker slot, an unknown id, and under gdd-0.1.
    [Theory]
    [InlineData("paid")]
    [InlineData("building")]
    [InlineData("industry")]
    [InlineData("unknown")]
    [InlineData("gdd-0.1")]
    public void NextCyclePriceIsNullWhileACycleIsUnderwayOrOffWorkshop(string why)
    {
        var (c, s, id) = why == "paid" ? Staffed(paid: 5m, progress: 1) : Built();
        if (why == "building") s.CityProjects[id] = new CityProject(id, Workshops.ArmsWorkshop, "town_a", 1, 1, 2);
        if (why == "industry") s.CityProjects[id] = new CityProject(id, "industry", "town_a", 1, 1, 0, s.Date);
        if (why == "unknown") id = s.NextId + 100;
        if (why == "gdd-0.1") c = Load(false);
        var before = s.Hash();
        Assert.Null(Workshops.NextCyclePrice(c, s, id));
        Assert.Equal(before, s.Hash());
    }

    // R-ARMS-006: a cycle's staffed months are the catalogue's workshop.staffed_turns (3) under gdd-0.3, and there is none
    // under gdd-0.1.
    [Fact]
    public void CycleMonthsIsTheCataloguesStaffedTurns()
    {
        Assert.Equal(3, Workshops.CycleMonths(Load(true)));
        Assert.Null(Workshops.CycleMonths(Load(false)));
    }

    // R-ARMS-005, R-LOOP-002: the workshops are hashed as wshop[...] only when there is one, so a gdd-0.1 state hashes as before.
    [Fact]
    public void TheWorkshopsAreHashedOnlyWhenPresent()
    {
        var (c, s, id) = Built(jobsDefault: false);
        Assert.DoesNotContain("wshop", s.Describe());
        var staffed = Step(c, s, Staff(1)).State;
        Assert.Contains("wshop", staffed.Describe());
        var other = staffed.Clone(); other.Workshops[id] = other.Workshops[id].WithStaffed(false);
        Assert.NotEqual(staffed.Hash(), other.Hash());
    }

    // R-ARMS-006 (#2270): Workshop.Canonical writes no "/COST,PROGRESS" suffix while no cycle is paid.
    [Fact]
    public void AnUnpaidWorkshopCanonicalHasNoCycleSuffix()
    {
        Assert.Equal("7:off,Ordinary", new Workshop(7, false, SyntheticWorkshopTier.Ordinary).Canonical());
    }

    // ---- R-ARMS-006: the monthly cycle at the production step (slice C) ----

    static readonly SyntheticWorkshopRules Rules = SyntheticWorkshopRules.FromCatalogue(Load(true).Gdd03Balance!);

    static IEnumerable<Cause> Charges(StepResult r) => r.Why.Where(w => w.Key == CauseKeys.Paid && w.Args[0] == CauseKeys.Purpose.Workshop);

    /// <summary>A gdd-0.3 game with town_a's workshop already staffed at <paramref name="tier"/>, no jobs (so no wages reach
    /// the Treasury before step 6), and the given Treasury and Debt.</summary>
    static (Content C, GameState S, int Id) Staffed(double treasury = 100, double debt = 0, SyntheticWorkshopTier tier = SyntheticWorkshopTier.Ordinary,
        decimal? paid = null, int progress = 0)
    {
        var (c, s, id) = Built(civilians: 10, employed: 0, jobs: 0);
        s.Workshops.Add(id, new Workshop(id, true, tier, paid, progress));
        s.Treasury = treasury; s.Debt = debt;
        return (c, s, id);
    }

    static (GameState S, List<StepResult> Turns) Run(Content c, GameState s, int turns, Func<int, Command[]>? orders = null)
    {
        var all = new List<StepResult>();
        for (int t = 0; t < turns; t++) { var r = Step(c, s, orders?.Invoke(t) ?? Array.Empty<Command>()); all.Add(r); s = r.State; }
        return (s, all);
    }

    // R-ARMS-006 (27M-11, owner-approved 1 November 2026: "At the monthly production step."): the order pays nothing; the
    // cycle is reserved and paid at step 6, after the city's wages, at the worker's price (IL 5m for an ordinary worker).
    [Fact]
    public void TheCycleIsReservedAtTheProductionStepNotOnTheOrder()
    {
        var (c, s, id) = Built(jobsDefault: false);
        s.Treasury = 100;
        var preview = Simulation.Preview(c, s, new Command[] { Staff(1) });
        Assert.Equal(s.Treasury, preview.State.Treasury);
        Assert.Null(preview.State.Workshops[id].PaidCost);
        var r = Step(c, s, Staff(1));
        var charge = Assert.Single(Charges(r));
        Assert.Equal(-5.0, charge.Delta, 12);
        var why = r.Why.ToList();
        var at = why.IndexOf(charge);
        Assert.True(why.FindIndex(w => w.Key == CauseKeys.WagesPaid) < at);
        Assert.True(why.FindIndex(w => w.Key == CauseKeys.WorkshopWorkers) < at);
        Assert.Equal((5m, 1), (r.State.Workshops[id].PaidCost, r.State.Workshops[id].Progress));   // OQ3: the month it is paid counts
    }

    // R-ARMS-006 (27M-12: "Automatic each month while the workshop is staffed, paid like any GDD cost."; OQ3, owner 2
    // November 2026: "Accept all four (Recommended)": the month a cycle is paid is its first staffed month):
    // workshop.staffed_turns (3) staffed months make 1 Arms, and the next cycle starts by itself the month after.
    [Fact]
    public void AStaffedWorkshopMakesOneArmsEveryThreeStaffedTurnsAndRepeats()
    {
        var (c, s, id) = Staffed();
        var (idle, _) = Run(c, Clear(s), 6);
        var (after, turns) = Run(c, s, 6);
        Assert.Equal(3, Rules.StaffedTurns);
        Assert.Equal(new[] { 0, 0, 1, 1, 1, 2 }, turns.Select(t => t.State.Arms - Run(c, Clear(s), turns.IndexOf(t) + 1).S.Arms));
        Assert.Equal(new[] { 1, 0, 0, 1, 0, 0 }, turns.Select(t => Charges(t).Count()));
        Assert.Equal(2, turns.Sum(t => t.Why.Count(w => w.Key == CauseKeys.WorkshopArms)));
        var made = turns[2].Why.Single(w => w.Key == CauseKeys.WorkshopArms);
        Assert.Equal(("town_a", 1.0, "Town A: an arms workshop made 1 Arms"), (made.Target, made.Delta, made.Message));
        Assert.Equal(idle.Arms + 2, after.Arms);
        Assert.Equal((null, 0), (after.Workshops[id].PaidCost, after.Workshops[id].Progress));
    }

    // R-ARMS-006 (#2270): in Workshops.Produce, a complete paid cycle waits at a full Arms stock, never overflowing or charging again.
    [Fact]
    public void AFullArmsStockKeepsACompletedPaidCycleWaiting()
    {
        var (c, s, id) = Staffed(paid: 5m, progress: 2);
        s.Arms = int.MaxValue;

        var first = Step(c, s);
        Assert.Equal(int.MaxValue, first.State.Arms);
        Assert.Equal((5m, 3), (first.State.Workshops[id].PaidCost, first.State.Workshops[id].Progress));
        Assert.Empty(Charges(first));
        Assert.DoesNotContain(first.Why, w => w.Key == CauseKeys.WorkshopArms);

        var second = Step(c, first.State);
        Assert.Equal(int.MaxValue, second.State.Arms);
        Assert.Equal((5m, 3), (second.State.Workshops[id].PaidCost, second.State.Workshops[id].Progress));
        Assert.Empty(Charges(second));
        Assert.DoesNotContain(second.Why, w => w.Key == CauseKeys.WorkshopArms);
    }

    static GameState Clear(GameState s) { var x = s.Clone(); x.Workshops.Clear(); return x; }

    // R-ARMS-006 (27M-13: "The cost tier follows the worker (novice or ordinary / apprentice / expert)."; gdd03-balance
    // workshop.cost 5/4/3, which the GDD's 1 November 2026 per-unit rates ruling keeps as "Money per Arms"; OQ1, owner 2 November 2026: "Keep as ruled").
    [Theory]
    [InlineData(SyntheticWorkshopTier.Ordinary, 5)]
    [InlineData(SyntheticWorkshopTier.Novice, 5)]
    [InlineData(SyntheticWorkshopTier.Apprentice, 4)]
    [InlineData(SyntheticWorkshopTier.Expert, 3)]
    public void TheCyclePaysTheWorkersTierPrice(SyntheticWorkshopTier tier, int price)
    {
        Assert.Equal(price, Rules.Price(tier));
        var (c, s, id) = Staffed(tier: tier);
        var r = Step(c, s);
        Assert.Equal(-price, Assert.Single(Charges(r)).Delta, 12);
        Assert.Equal((decimal?)price, r.State.Workshops[id].PaidCost);
    }

    // R-ARMS-006 with R-MONEY-003 (27M-12: "paid like any GDD cost"): a Treasury short of the price borrows the rest.
    [Fact]
    public void AShortTreasuryBorrowsTheRest()
    {
        var (c, s, _) = Staffed(treasury: 2, debt: 0);
        var r = Step(c, s);
        Assert.Equal(-5.0, Assert.Single(Charges(r)).Delta, 12);
        Assert.Equal(3.0, r.Why.Single(w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Workshop).Delta, 12);
    }

    // R-ARMS-006 (#1701 OQ4, owner 2 November 2026: "Refuse past the ceiling (Recommended)"): a cycle whose borrowing would
    // take the debt past mvp.debt_ceiling (the fixture's 10) is not paid that month: the workshop pauses, the turn report
    // says why, and nothing is borrowed. Exactly at the ceiling is allowed, and a Treasury that pays in full is never refused.
    [Theory]
    [InlineData(0, 8, false)]    // 8 + 5 = 13 > 10
    [InlineData(0, 5, true)]     // 5 + 5 = 10, at the ceiling
    [InlineData(5, 50, true)]    // paid in full: nothing borrowed
    [InlineData(4, 9.5, false)]  // 9.5 + 1 = 10.5 > 10
    public void PastTheDebtCeilingTheCycleWaits(double treasury, double debt, bool paid)
    {
        var (c, s, id) = Staffed(treasury, debt);
        var r = Step(c, s);
        Assert.Equal(paid ? 1 : 0, Charges(r).Count());
        Assert.Equal(paid ? 1 : 0, r.State.Workshops[id].Progress);
        var waits = r.Why.Where(w => w.Key == CauseKeys.WorkshopPaused).ToList();
        if (paid) { Assert.Empty(waits); return; }
        var line = Assert.Single(waits);
        Assert.Equal((Workshops.DebtCeiling, "Town A: an arms workshop's next cycle waits: paying for it would take the debt past its ceiling"),
            (line.Args.Single(), line.Message));
        Assert.Null(r.State.Workshops[id].PaidCost);
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.Borrowed && w.Args[0] == CauseKeys.Purpose.Workshop);
    }

    // R-ARMS-006 (27M-13): a cycle keeps the price it was paid at when the worker changes; only the next cycle is priced at
    // the new worker's tier, and nothing is refunded or charged twice.
    [Fact]
    public void AWorkerChangeKeepsThePaidCycleAndPricesTheNext()
    {
        var (c, s, id) = Staffed(tier: SyntheticWorkshopTier.Ordinary, paid: 3m, progress: 1);   // paid at the expert's IL 3m
        var (after, turns) = Run(c, s, 3);
        Assert.Equal(new[] { 0, 0, 1 }, turns.Select(t => Charges(t).Count()));
        Assert.Equal(1, turns[1].Why.Count(w => w.Key == CauseKeys.WorkshopArms));
        Assert.Equal(-5.0, Charges(turns[2]).Single().Delta, 12);   // the next cycle: the ordinary worker's IL 5m
        Assert.Equal((5m, 1), (after.Workshops[id].PaidCost, after.Workshops[id].Progress));
    }

    // R-ARMS-006, R-ARMS-007: an unstaffed workshop is paused, not cancelled: it keeps its paid cost and progress, is not
    // charged again, and resumes where it stopped.
    [Fact]
    public void AnUnstaffedWorkshopPausesAndResumes()
    {
        var (c, s, id) = Built(civilians: 10, employed: 0, jobs: 0);
        s.Treasury = 100;
        var (after, turns) = Run(c, s, 5, t => t == 0 ? new Command[] { Staff(1) } : t == 1 ? new Command[] { Staff(0) } : t == 3 ? new Command[] { Staff(1) } : Array.Empty<Command>());
        Assert.Equal(new[] { 1, 0, 0, 0, 0 }, turns.Select(t => Charges(t).Count()));
        Assert.Equal(new[] { 1, 1, 1, 2, 0 }, turns.Select(t => t.State.Workshops[id].Progress));
        Assert.Equal(1, turns[4].Why.Count(w => w.Key == CauseKeys.WorkshopArms));
        Assert.Null(after.Workshops[id].PaidCost);
    }

    // R-ARMS-006 with R-CITY-010 (D6: a held city "pauses"): a workshop in a city the enemy holds is not charged and makes no
    // progress, and resumes once the city is retaken.
    [Fact]
    public void AHeldCityPausesItsWorkshop()
    {
        var (c, s, id) = Staffed(paid: 5m, progress: 1);
        s.Units.Add(s.NextId, new Unit(s.NextId++, "enemy", 1, 0, side: "egypt", strength: 3));
        var held = Step(c, s);
        Assert.Equal((5m, 1), (held.State.Workshops[id].PaidCost, held.State.Workshops[id].Progress));
        Assert.Empty(Charges(held));
        var free = held.State.Clone(); free.Units.Clear();
        Assert.Equal(2, Step(c, free).State.Workshops[id].Progress);
    }

    // R-ARMS-006: the workshop's worker is one of the city's people without a job; when an order has taken them all (a Train
    // order counts only the idle civilians), the workshop pauses that month and says why.
    [Fact]
    public void AWorkshopWithNoOneFreeWaits()
    {
        var (c, s, id) = Staffed(paid: 5m, progress: 1);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, x.HousingCapacity, 10, 10, 10, 0, 0, x.Condition, jobsDefault: false);
        var r = Step(c, s);
        Assert.Equal((5m, 1), (r.State.Workshops[id].PaidCost, r.State.Workshops[id].Progress));
        var line = Assert.Single(r.Why, w => w.Key == CauseKeys.WorkshopPaused);
        Assert.Equal((Workshops.NoWorker, "Town A: an arms workshop has no worker without a job this month"), (line.Args.Single(), line.Message));
    }

    // R-ARMS-006: the cycle runs under gdd-0.3 only. A gdd-0.1 state that somehow holds a staffed workshop one month from its
    // Arms (gdd-0.1 can neither build nor staff one, R-ARMS-004/005) makes no Arms, pays nothing and gives no workshop cause,
    // so the turn report's workshop row (TurnReportPresenter.WorkshopRow, #1701 A1) never shows outside gdd-0.3.
    [Fact]
    public void Gdd01RunsNoWorkshopCycle()
    {
        var c = Load(false);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, "industry", "town_a", 1, 1, 0, s.Date));
        s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary, 5m, 2));
        var (after, turns) = Run(c, s, 3);
        Assert.DoesNotContain(turns.SelectMany(t => t.Why), w => w.Key == CauseKeys.WorkshopArms || w.Key == CauseKeys.WorkshopPaused);
        Assert.Empty(turns.SelectMany(Charges));
        Assert.Equal(Run(c, Clear(s), 3).S.Arms, after.Arms);
        Assert.Equal((5m, 2), (after.Workshops[id].PaidCost, after.Workshops[id].Progress));
    }

    // R-ARMS-006, R-SAVE: a game saved in the middle of a cycle replays to the same state: no charge or Arms is repeated.
    [Fact]
    public void AReplayedGameRepeatsNoChargeOrArms()
    {
        var c = Load(true);
        var orders = new Dictionary<int, Command[]>
        {
            [0] = new Command[] { new StartProject(Workshops.ArmsWorkshop, "town_a", 1, 1) },
            [3] = new Command[] { Staff(1) },
        };
        var whole = GameSession.New(c, 7, Mode.Historical);
        for (int t = 0; t < 8; t++) whole.Step(orders.TryGetValue(t, out var o) ? o : Array.Empty<Command>());
        var half = GameSession.New(c, 7, Mode.Historical);
        for (int t = 0; t < 5; t++) half.Step(orders.TryGetValue(t, out var o) ? o : Array.Empty<Command>());
        var loaded = GameSession.Load(c, half.Save.Write(), out _);
        Assert.Equal(half.State.Hash(), loaded.State.Hash());
        for (int t = 5; t < 8; t++) loaded.Step(Array.Empty<Command>());
        Assert.Equal(whole.State.Hash(), loaded.State.Hash());
        Assert.Contains(":on,Ordinary/5,2", half.State.Describe());   // saved two turns into its first cycle
        Assert.Contains(":on,Ordinary/5,2", whole.State.Describe());   // two turns into the second: the first made its Arms
    }

    // ---- R-ARMS-007: 27M-14 and Q55 (slice D) ----

    // R-ARMS-007 (Q55, owner-approved 1 November 2026: "No. Only the stated apprentice-to-expert progression counts"):
    // months of workshop work change no profession, craftsman count or qualification.
    [Fact]
    public void WorkshopWorkChangesNoQualification()
    {
        var (c, s, _) = Staffed();
        var (worked, turns) = Run(c, s, 6);
        var (idle, _) = Run(c, Clear(s), 6);
        Assert.Equal(2, turns.Sum(t => t.Why.Count(w => w.Key == CauseKeys.WorkshopArms)));
        var a = worked.Cities["town_a"]; var b = idle.Cities["town_a"];
        Assert.Equal((b.Civilians, b.Craftsmen, b.Employed, b.Qualified), (a.Civilians, a.Craftsmen, a.Employed, a.Qualified));
    }

    // R-ARMS-007 (27M-14: "Cycle progress is lost and the Money paid is not refunded."; #1701 OQ5, owner 2 November 2026:
    // "Accept all four (Recommended)": the MVP has no destructive cancellation, destruction or transfer of a workshop):
    // a built workshop cannot be cancelled, so its paid cycle is never refunded or lost by an order.
    [Fact]
    public void ABuiltWorkshopCannotBeCancelledSoItsCycleStays()
    {
        var (c, s, id) = Staffed(paid: 5m, progress: 2);
        var r = Step(c, s, new CancelProject(id));
        Assert.Equal("An arms workshop is already built", Assert.Single(r.Rejected).Reason);
        Assert.Contains(r.Why, w => w.Key == CauseKeys.WorkshopArms);   // the cycle finished as usual
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.Paid && w.Delta > 0);
    }

    // ---- Review fixes (2 November 2026) ----

    // R-ARMS-004 (#1701 OQ2, owner 2 November 2026: "IL 15m, 3 turns (Recommended)"): the real row's cost and turns are
    // the gdd-0.3 balance catalogue's arms_workshop.parts.cost and .work, so a catalogue change cannot drift from it silently.
    [Fact]
    public void TheRealRowMatchesTheCataloguesCostAndWork()
    {
        var line = File.ReadAllLines(Path.Combine(RealContentTests.ContentRoot(), "projects.yaml")).Single(l => l.Contains("id: arms_workshop"));
        var c = Load(true, Fixture.GddProjects + line + "\n");
        var k = c.ProjectKinds.Single(x => x.Id == Workshops.ArmsWorkshop);
        var row = Gdd03ProjectCatalogue.Read(c).Projects.Single(p => p.Key == Workshops.ArmsWorkshop);
        Assert.Equal((15m, 3m), (row.Cost.Amount, row.Work.Amount));
        Assert.Equal((row.Cost.Amount, row.Work.Amount), ((decimal)k.Cost, (decimal)k.Turns));
    }

    // R-ARMS-006: two staffed workshops in one city with one person without a job: the lower project id works, the other
    // waits with no_worker, whatever order the records were added in.
    [Fact]
    public void TwoWorkshopsAndOneFreePersonStaffTheLowerIdOnly()
    {
        var (c, s, first) = Built(civilians: 10, employed: 9, jobs: 9, jobsDefault: false);
        var second = s.NextId++;
        s.CityProjects.Add(second, new CityProject(second, Workshops.ArmsWorkshop, "town_a", 2, 0, 0, s.Date));
        s.Workshops.Add(second, new Workshop(second, true, SyntheticWorkshopTier.Ordinary));
        s.Workshops.Add(first, new Workshop(first, true, SyntheticWorkshopTier.Ordinary));
        s.Treasury = 100;
        var r = Step(c, s);
        Assert.Single(Charges(r));
        Assert.Equal((5m, 1), (r.State.Workshops[first].PaidCost, r.State.Workshops[first].Progress));
        Assert.Equal(((decimal?)null, 0), (r.State.Workshops[second].PaidCost, r.State.Workshops[second].Progress));
        Assert.Equal(Workshops.NoWorker, Assert.Single(r.Why, w => w.Key == CauseKeys.WorkshopPaused).Args.Single());
    }

    // R-TRAIN-001, R-QUAL-005 with R-ARMS-005: a staffed workshop's worker holds no job but is not idle, so neither Train
    // nor a pioneer reassignment (which takes civilians out of the city) can take them.
    [Fact]
    public void TrainCannotTakeAStaffedWorkshopsWorker()
    {
        var (c, s, id) = Built(civilians: 3, employed: 2, jobs: 2, jobsDefault: false);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithQualified(new Qualified(0, 0, new[] { 0 }));
        s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary));
        Assert.Equal(0, Training.Available(s, "town_a", Array.Empty<Command>()));
        foreach (var order in new Command[] { new Train("town_a", "halutz", 1), new ReassignPioneers("town_a", "halutz", 1) })
        {
            var r = Step(c, s, order);
            Assert.Equal("Town A has 0 civilians without work", Assert.Single(r.Rejected).Reason);
            Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.WorkshopPaused);
        }
    }

    // R-TRAIN-001 with R-ARMS-005 (#2270): Training.Idle stops at 0 when a staffed workshop's worker leaves no free civilian,
    // so a Train refusal never names a negative count.
    [Fact]
    public void TrainReportsZeroIdleWhenAStaffedWorkshopExceedsTheFreePopulation()
    {
        var (c, s, id) = Built(civilians: 3, employed: 3, jobs: 3, jobsDefault: false);
        s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary));
        var r = Step(c, s, new Train("town_a", "halutz", 1));
        Assert.Equal("Town A has 0 civilians without work", Assert.Single(r.Rejected).Reason);
    }

    // R-QUAL-005 with R-ARMS-005 (#2270): the same Training.Idle floor keeps a pioneer-reassignment refusal from naming a
    // negative count.
    [Fact]
    public void ReassignPioneersReportsZeroIdleWhenAStaffedWorkshopExceedsTheFreePopulation()
    {
        var (c, s, id) = Built(civilians: 3, employed: 3, jobs: 3, jobsDefault: false);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithQualified(new Qualified(0, 0, new[] { 0 }));
        s.Workshops.Add(id, new Workshop(id, true, SyntheticWorkshopTier.Ordinary));
        var r = Step(c, s, new ReassignPioneers("town_a", "halutz", 1));
        Assert.Equal("Town A has 0 civilians without work", Assert.Single(r.Rejected).Reason);
    }

    // R-ARMS-005 with R-CITY-001 (#2289): Workshops.Check names 0, never a negative number, when this turn's Train orders
    // take more people than are free.
    [Fact]
    public void CheckReportsZeroFreePeopleWhenPendingTrainingExceedsThem()
    {
        var (c, s, _) = Built(civilians: 10, employed: 9, jobs: 10, jobsDefault: false);
        var pending = new Command[] { new Train("town_a", "halutz", 3) };
        Assert.Equal("Town A has 0 civilians and craftsmen without a job", Workshops.Check(c, s, "town_a", 1, pending));
    }

    // R-TRAIN-001: Training.Available reads only a "job" AssignWorkers as a jobs order; a pending "workshop" order takes
    // its newly staffed workers off the idle civilians instead, so a Train up to the limit stays accepted in either order.
    [Fact]
    public void APendingStaffingOrderIsNotAJobsOrder()
    {
        var (c, s, _) = Built(civilians: 10, employed: 4, jobs: 10, jobsDefault: false);
        var pending = new Command[] { Staff(1) };
        Assert.Equal(5, Training.Available(s, "town_a", pending));
        foreach (var order in new[] { new Command[] { Staff(1), new Train("town_a", "halutz", 5) }, new Command[] { new Train("town_a", "halutz", 5), Staff(1) } })
            Assert.Empty(Step(c, s, order).Rejected);
        Assert.Equal(6, Training.Available(s, "town_a", new Command[] { new AssignWorkers("town_a", "job", 4) }));
    }

    // R-ARMS-005 with R-CITY-009: while the city's job default is on, staffing takes its worker from the default's jobs and
    // leaves the default on, so a city whose people are all in work can staff a workshop; the default then fills one job fewer.
    [Fact]
    public void StaffingTakesItsWorkerFromTheJobDefault()
    {
        var (c, s, id) = Built(civilians: 10, employed: 10, jobs: 10);
        s.Treasury = 100;
        var r = Step(c, s, Staff(1));
        Assert.Empty(r.Rejected);
        Assert.True(r.State.Workshops[id].Staffed);
        var x = r.State.Cities["town_a"];
        Assert.Equal((9, true), (x.Employed, x.JobsDefault));
        var jobs = Assert.Single(r.Why, w => w.Key == CauseKeys.WorkersAssigned && w.Target == "town_a");
        Assert.Equal((-1.0, "Town A: 9 groups in work"), (jobs.Delta, jobs.Message));
        Assert.Single(Charges(r));   // the workshop works this month
        Assert.DoesNotContain(r.Why, w => w.Key == CauseKeys.WorkshopPaused);
        Assert.Equal(9, Step(c, r.State).State.Cities["town_a"].Employed);
    }
}
