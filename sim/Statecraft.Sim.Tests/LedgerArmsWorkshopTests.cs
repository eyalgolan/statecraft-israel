using Statecraft.Sim;

/// <summary>#1725 PR 7, producer 07: arms workshops (R-ARMS-005 to R-ARMS-007) staff and produce on a game with an
/// initialized Historical ledger, the dark start (R-POP-002). A workshop's worker is one named free-civilian record with a
/// Work assignment on the workshop's own target; the aggregate city fields are neither read nor written. The owner's
/// rulings, owner, 8 November 2026, each the recommended option of the plan's section 7:
/// Q1 "Work assignment (Rec.)": the worker holds a Work assignment on a target workshop/&lt;project id&gt;, bound at
///    runtime, all or nothing (PopulationLedger.Commit, #2964).
/// Q2 "Keep existing order (Rec.)": AssignWorkers(city, "workshop", n) staffs on a ledger game; the order picks the records.
/// Q3 "Wait for Industry records (Rec.)": a count of the people without a job waits for PR 3's Industry records, so no such
///    count is read here: a workshop works when its own bound worker is there (Q6), and staffing takes only records with no
///    assignment (Q7).
/// Q4 "Only craftsmen discount (Rec.)": only a craftsman apprentice or expert lowers the price; every other worker pays the
///    ordinary IL 5m.
/// Q5 "Not in this change (Rec.)": workshop months give no record work XP.
/// Q6 "Waits, stays staffed (Rec.)": a staffed workshop whose bound worker is gone waits with the no_worker line, stays
///    staffed, and is not given another worker.
/// Q7 "Unassigned, lowest id (Rec.)": the city's unassigned free civilians in ascending population id, one per newly
///    staffed workshop in ascending project id; unstaffing releases the workers of the higher project ids.
/// Q8 "Keep tier field (Rec.)": Workshop.WorkerTier is the priced tier, set from the record at staffing and at each new
///    cycle; no new field.
/// The tests call Workshops and Cities directly: Simulation.Step and TurnResolver.Resolve still refuse a ledger game.</summary>
public class LedgerArmsWorkshopTests
{
    const string Haifa = "haifa";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);
        s.Arms = 10; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    static GameState Aggregate(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Arms = 10; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    // A built arms workshop in the city at its settlement hex, as ArmsWorkshopTests injects one. Returns its project id.
    static int Built(Content c, GameState s, string city)
    {
        var h = c.SettlementHexes.First(x => x.Id == city);
        var id = s.NextId++;
        s.CityProjects.Add(id, new CityProject(id, Workshops.ArmsWorkshop, city, h.Q, h.R, 0, s.Date));
        return id;
    }

    // Q7: the city's free-civilian records with no assignment, ascending id.
    static List<PopulationUnit> Free(GameState s, string city) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned && p.Location?.City == city)
        .OrderBy(p => p.Id).ToList();

    static string Target(int project) => "workshop/" + project;

    static List<PopulationUnit> On(GameState s, int project) => s.PopulationLedger.Units
        .Where(p => p.Assignment.Target == Target(project)).ToList();

    static bool Bound(GameState s, string id) => s.PopulationLedger.Canonical().Any(line =>
        line.StartsWith("6:target|", StringComparison.Ordinal) && line.Contains(":" + id.Length + ":" + id + "|", StringComparison.Ordinal));

    static string CityText(GameState s) => string.Join(";", s.Cities.Values.Select(x => x.Canonical()));

    static IEnumerable<(string, string, double, string, string)> Lines(IEnumerable<Cause> why) => why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key));

    // A record replaced in the ledger with a craftsman's profession and tier (a fixture: no craftsman exists at the dark start).
    static void MakeCraftsman(GameState s, int id, CivilianTier tier)
    {
        var p = s.PopulationLedger.Get(id);
        s.PopulationLedger.Commit(new[] { new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment,
            CivilianProfession.Craftsman, tier, 0, p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home) }, Array.Empty<PopulationTarget>());
    }

    static (GameState S, int Project, int Worker) Staffed(Content c)
    {
        var s = Dark(c);
        var project = Built(c, s, Haifa);
        var worker = Free(s, Haifa)[0].Id;
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));
        return (s, project, worker);
    }

    // Q2 "Keep existing order (Rec.)", Q7 "Unassigned, lowest id (Rec.)", Q1 "Work assignment (Rec.)", Q8 "Keep tier field
    // (Rec.)" (owner, 8 November 2026): AssignWorkers(haifa, "workshop", 1) staffs the built workshop with the lowest-id
    // unassigned free civilian of Haifa, now on a Work assignment to workshop/<project id>, bound in the same commit. The
    // record keeps its place, profession, tier and XP (R-ARMS-005, Q55); the workshop is staffed at the record's tier. No city
    // field is written, no id is made or lost, and the Why line is the aggregate game's staffing line.
    [Fact]
    public void StaffingTakesTheLowestIdUnassignedFreeCivilian()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var project = Built(c, s, Haifa); Assert.Equal(project, Built(c, twin, Haifa));
        var before = Free(s, Haifa)[0];
        var cities = CityText(s); var next = s.PopulationLedger.NextPopulationId; var count = s.PopulationLedger.Units.Count;
        Assert.False(Bound(s, Target(project)));
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, why));
        Assert.Null(Cities.Assign(c, twin, Haifa, Workshops.Role, 1, twinWhy));

        var p = s.PopulationLedger.Get(before.Id);
        Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Work, Target(project)), (p.Status, p.Assignment.Kind, p.Assignment.Target));
        Assert.Equal(before.Location!.Canonical(), p.Location!.Canonical());
        Assert.Equal((before.Profession, before.Tier, before.WorkXp, before.Qualification, before.ReceptionTicks), (p.Profession, p.Tier, p.WorkXp, p.Qualification, p.ReceptionTicks));
        Assert.True(Bound(s, Target(project)));
        Assert.Equal(new[] { before.Id }, On(s, project).Select(x => x.Id));
        Assert.Equal($"{project}:on,Ordinary", s.Workshops[project].Canonical());
        Assert.Equal(twin.Workshops[project].Canonical(), s.Workshops[project].Canonical());
        Assert.Equal(cities, CityText(s));
        Assert.Equal(Lines(twinWhy.Where(x => x.Key == CauseKeys.WorkshopWorkers)), Lines(why));
        Assert.Equal((next, count), (s.PopulationLedger.NextPopulationId, s.PopulationLedger.Units.Count));
        s.PopulationLedger.Reconcile();
    }

    // R-ARMS-005: a staffed workshop's worker is not idle, so Train cannot take them (Training's idle records are the
    // unassigned ones); re-issuing the same order changes nothing.
    [Fact]
    public void TheWorkerIsNotIdleAndTheSameOrderChangesNothing()
    {
        var c = Gdd03();
        var (s, project, worker) = Staffed(c);
        Assert.DoesNotContain(worker, Free(s, Haifa).Select(x => x.Id));
        var ledger = s.PopulationLedger.Canonical().ToList(); var shop = s.Workshops[project].Canonical();
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(shop, s.Workshops[project].Canonical());
    }

    // Q7 "Unassigned, lowest id (Rec.)" (owner, 8 November 2026): two workshops take the two lowest-id records, in ascending
    // project id. Lowering the order to 1 releases the higher project's worker, who is an unassigned free civilian again in
    // place; the workshop keeps its record, paid cost and progress (a pause, R-ARMS-007), and its target stays bound.
    [Fact]
    public void TwoWorkshopsTakeTwoRecordsAndUnstaffingReleasesTheHigherProjectsWorker()
    {
        var c = Gdd03();
        var s = Dark(c);
        var first = Built(c, s, Haifa); var second = Built(c, s, Haifa);
        var free = Free(s, Haifa).Take(2).ToList();
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 2, new List<Cause>()));
        Assert.Equal(new[] { free[0].Id }, On(s, first).Select(x => x.Id));
        Assert.Equal(new[] { free[1].Id }, On(s, second).Select(x => x.Id));
        Workshops.Produce(c, s, new List<Cause>());
        var paid = s.Workshops[second];
        Assert.Equal((5m, 1), (paid.PaidCost, paid.Progress));

        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));

        Assert.Equal(new[] { free[0].Id }, On(s, first).Select(x => x.Id));
        Assert.Empty(On(s, second));
        var back = s.PopulationLedger.Get(free[1].Id);
        Assert.Equal((PopulationAssignmentKind.Unassigned, free[1].Location!.Canonical()), (back.Assignment.Kind, back.Location!.Canonical()));
        Assert.Equal($"{second}:off,Ordinary/5,1", s.Workshops[second].Canonical());
        Assert.True(Bound(s, Target(second)));
        s.PopulationLedger.Reconcile();
    }

    // R-ARMS-005's refusals on the ledger, each with the state, the ledger and the Why list unchanged: no such city, a negative
    // count, more than the city's built workshops (the aggregate game's words), and more newly staffed workshops than the
    // city's unassigned free-civilian records.
    [Fact]
    public void RefusalsChangeNothing()
    {
        var c = Gdd03();
        var s = Dark(c);
        Built(c, s, Haifa);
        var why = new List<Cause>();
        string Refusal(string city, int n)
        {
            var before = s.Describe(); var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList();
            var r = Cities.Assign(c, s, city, Workshops.Role, n, why);
            Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Equal(ledger, s.PopulationLedger.Canonical());
            return r!;
        }
        Assert.Equal("There is no such city", Refusal("missing", 1));
        Assert.Equal(Cities.RefusedNegativeWorkers, Refusal(Haifa, -1));
        Assert.Equal("Haifa has 1 arms workshop", Refusal(Haifa, 2));
        Assert.Empty(why);

        // One workshop staffed, then one more built workshop than Haifa has unassigned records left: the order for all of
        // them is refused with the records' count, and the order for one fewer takes every one of them.
        var free = Free(s, Haifa);
        Built(c, s, Haifa);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, why));
        why.Clear();
        var left = Free(s, Haifa).Count;
        for (int i = 0; i < left; i++) Built(c, s, Haifa);
        Assert.Equal($"Haifa has {left} free civilians without an assignment", Refusal(Haifa, left + 2));
        Assert.Empty(why);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, left + 1, why));
        Assert.Empty(Free(s, Haifa));
        Assert.True(free.Count > left);
        s.PopulationLedger.Reconcile();
    }

    // Owner, 8 November 2026: "Refuse with a reason (Recommended)". R-CITY-011's builders are city counts, not records, so a
    // staffing order in a city with a builder is refused with a shown reason before any write; nothing throws, and the state,
    // the ledger and the hash are unchanged.
    [Fact]
    public void ACityWithABuilderIsRefusedWithAReason()
    {
        var c = Gdd03();
        var s = Dark(c);
        Built(c, s, Haifa);
        var h = c.SettlementHexes.First(x => x.Id == Haifa);
        var site = s.NextId++;
        s.CityProjects.Add(site, new CityProject(site, "housing", Haifa, h.Q, h.R + 1, 2, builder: true));
        Assert.Equal(1, Projects.Builders(s, Haifa));
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList();
        Assert.Equal("Haifa has builders on city projects, who are not on the population ledger yet", Cities.Assign(c, s, Haifa, Workshops.Role, 1, why));
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Empty(why);
        s.PopulationLedger.Reconcile();
    }

    // R-ARMS-006 (27M-11: "At the monthly production step."; Q4 "Only craftsmen discount (Rec.)", owner, 8 November 2026): the
    // dark start's workshop, worked by a census record (profession None), pays IL 5m for its first cycle and counts the month.
    [Fact]
    public void TheDarkStartsWorkshopPaysAndAdvancesACycle()
    {
        var c = Gdd03();
        var (s, project, _) = Staffed(c);
        var why = new List<Cause>();
        Workshops.Produce(c, s, why);
        Assert.Equal((95.0, 0.0, 10), (s.Treasury, s.Debt, s.Arms));
        Assert.Equal($"{project}:on,Ordinary/5,1", s.Workshops[project].Canonical());
        s.PopulationLedger.Reconcile();
    }

    // R-ARMS-006 (workshop.staffed_turns 3) and Q5 "Not in this change (Rec.)" (owner, 8 November 2026): three staffed months
    // make 1 Arms and end the cycle; the next month starts the next one by itself. No record changes (no work XP).
    [Fact]
    public void ThreeStaffedMonthsMakeOneArms()
    {
        var c = Gdd03();
        var (s, project, _) = Staffed(c);
        var ledger = s.PopulationLedger.Canonical().ToList();
        var why = new List<Cause>();
        for (int i = 0; i < 3; i++) Workshops.Produce(c, s, why);
        Assert.Equal((11, 95.0), (s.Arms, s.Treasury));
        Assert.Equal($"{project}:on,Ordinary", s.Workshops[project].Canonical());
        Assert.Single(why, x => x.Key == CauseKeys.WorkshopArms);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Workshops.Produce(c, s, why);
        Assert.Equal((90.0, $"{project}:on,Ordinary/5,1"), (s.Treasury, s.Workshops[project].Canonical()));
    }

    // R-MONEY-003 and #1701 OQ4 (owner, 2 November 2026: "Refuse past the ceiling (Recommended)"): a short Treasury borrows
    // the rest; a cycle whose borrowing would pass mvp.debt_ceiling waits with the debt_ceiling line and nothing borrowed;
    // exactly at the ceiling is allowed.
    [Fact]
    public void AShortTreasuryBorrowsAndThePastCeilingCycleWaits()
    {
        var c = Gdd03();
        var ceiling = c.RulesAt(Dark(c).Date).Mvp!.DebtCeiling!.Value;

        var (s, project, _) = Staffed(c);
        s.Treasury = 2; s.Debt = 0;
        Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal((0.0, 3.0, 1), (s.Treasury, s.Debt, s.Workshops[project].Progress));

        (s, project, _) = Staffed(c);
        s.Treasury = 2; s.Debt = ceiling - 3;
        Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal((0.0, ceiling, 1), (s.Treasury, s.Debt, s.Workshops[project].Progress));

        (s, project, _) = Staffed(c);
        s.Treasury = 2; s.Debt = ceiling - 2;
        var shop = s.Workshops[project].Canonical();
        var why = new List<Cause>();
        Workshops.Produce(c, s, why);
        Assert.Equal((2.0, ceiling - 2, shop), (s.Treasury, s.Debt, s.Workshops[project].Canonical()));
        var line = Assert.Single(why);
        Assert.Equal((CauseKeys.WorkshopPaused, Workshops.DebtCeiling), (line.Key, line.Args![0]));
    }

    // R-CITY-010: a workshop in a city the enemy holds is not charged and makes no progress; its cycle is kept.
    [Fact]
    public void AHeldCityPausesItsWorkshop()
    {
        var c = Gdd03();
        var (s, project, _) = Staffed(c);
        Workshops.Produce(c, s, new List<Cause>());
        var shop = s.Workshops[project].Canonical(); var treasury = s.Treasury;
        var h = c.SettlementHexes.First(x => x.Id == Haifa);
        var enemy = s.NextId++;
        s.Units[enemy] = new Unit(enemy, "enemy", h.Q, h.R, side: "egypt", strength: 1);
        var why = new List<Cause>();
        Workshops.Produce(c, s, why);
        Assert.Equal((treasury, shop), (s.Treasury, s.Workshops[project].Canonical()));
        Assert.Empty(why);
    }

    // Q6 "Waits, stays staffed (Rec.)" and Q3 "Wait for Industry records (Rec.)" (owner, 8 November 2026): a staffed workshop
    // whose bound worker is gone waits with the existing no_worker line, stays staffed, is not charged, and takes no other
    // record, even with free civilians in the city (no count of people without a job is read).
    [Fact]
    public void AWorkshopWhoseWorkerIsGoneWaitsAndStaysStaffed()
    {
        var c = Gdd03();
        var (s, project, worker) = Staffed(c);
        var p = s.PopulationLedger.Get(worker);
        s.PopulationLedger.Commit(new[] { p.WithPlacement(p.Status, p.Location, PopulationAssignment.Unassigned, p.Captor, p.Home) }, Array.Empty<PopulationTarget>());
        var ledger = s.PopulationLedger.Canonical().ToList(); var treasury = s.Treasury;
        Assert.NotEmpty(Free(s, Haifa).Skip(1));
        var why = new List<Cause>();

        Workshops.Produce(c, s, why);

        var line = Assert.Single(why);
        Assert.Equal((CauseKeys.WorkshopPaused, Workshops.NoWorker, project.ToString()), (line.Key, line.Args![0], line.Args![1]));
        Assert.Equal($"{project}:on,Ordinary", s.Workshops[project].Canonical());
        Assert.Equal(treasury, s.Treasury);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // Q4 "Only craftsmen discount (Rec.)" and Q8 "Keep tier field (Rec.)" (owner, 8 November 2026): the worker record's tier
    // prices the cycle: a craftsman apprentice IL 4m, an expert IL 3m; a craftsman novice and every other profession pay the
    // ordinary IL 5m (the Haifa teacher, a veteran pioneer, too).
    [Theory]
    [InlineData(CivilianTier.Apprentice, "Apprentice", 4.0)]
    [InlineData(CivilianTier.Expert, "Expert", 3.0)]
    [InlineData(CivilianTier.Novice, "Ordinary", 5.0)]
    public void ACraftsmanWorkersTierSetsThePrice(CivilianTier tier, string priced, double price)
    {
        var c = Gdd03();
        var s = Dark(c);
        var project = Built(c, s, Haifa);
        MakeCraftsman(s, Free(s, Haifa)[0].Id, tier);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));
        Assert.Equal($"{project}:on,{priced}", s.Workshops[project].Canonical());
        Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal(100 - price, s.Treasury);
    }

    [Fact]
    public void AVeteranPioneerPaysTheOrdinaryPrice()
    {
        var c = Gdd03();
        var s = Dark(c);
        var teacher = Free(s, Haifa).Single(p => p.Profession == CivilianProfession.Pioneer);
        Assert.Equal(CivilianTier.Veteran, teacher.Tier);
        var count = Free(s, Haifa).Count;
        Assert.Equal(teacher.Id, Free(s, Haifa).Last().Id);   // "Takeable, but last": the ascending order reaches it last
        var shops = Enumerable.Range(0, count).Select(_ => Built(c, s, Haifa)).ToList();
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, count, new List<Cause>()));
        Assert.Equal(new[] { teacher.Id }, On(s, shops.Last()).Select(x => x.Id));
        Assert.Equal($"{shops.Last()}:on,Ordinary", s.Workshops[shops.Last()].Canonical());
    }

    // 27M-13 (owner-approved 1 November 2026: "The cost tier follows the worker") with Q8 "Keep tier field (Rec.)": a cycle
    // under way keeps its paid cost when the worker's tier changes; only the next cycle is priced at the new tier.
    [Fact]
    public void ATierChangeKeepsTheCycleUnderWayAndPricesTheNext()
    {
        var c = Gdd03();
        var s = Dark(c);
        var project = Built(c, s, Haifa);
        var worker = Free(s, Haifa)[0].Id;
        MakeCraftsman(s, worker, CivilianTier.Apprentice);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));
        Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal($"{project}:on,Apprentice/4,1", s.Workshops[project].Canonical());
        MakeCraftsman(s, worker, CivilianTier.Expert);
        Workshops.Produce(c, s, new List<Cause>()); Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal((96.0, 11, $"{project}:on,Apprentice"), (s.Treasury, s.Arms, s.Workshops[project].Canonical()));
        Workshops.Produce(c, s, new List<Cause>());
        Assert.Equal((93.0, $"{project}:on,Expert/3,1"), (s.Treasury, s.Workshops[project].Canonical()));
    }

    // Parity: an ordinary worker's workshop moves the Treasury, the Debt, the Arms and the Why lines exactly as the aggregate
    // game's workshop does, month by month, through a cycle and into the next.
    [Fact]
    public void DarkAndAggregateMatchForOrdinaryWorkers()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var project = Built(c, s, Haifa); Built(c, twin, Haifa);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 1, new List<Cause>()));
        Assert.Null(Cities.Assign(c, twin, Haifa, Workshops.Role, 1, new List<Cause>()));
        s.Treasury = twin.Treasury = 7; s.Debt = twin.Debt = 0;
        for (int i = 0; i < 5; i++)
        {
            var why = new List<Cause>(); var twinWhy = new List<Cause>();
            Workshops.Produce(c, s, why); Workshops.Produce(c, twin, twinWhy);
            Assert.Equal(Lines(twinWhy), Lines(why));
            Assert.Equal((twin.Treasury, twin.Debt, twin.Arms), (s.Treasury, s.Debt, s.Arms));
            Assert.Equal(twin.Workshops[project].Canonical(), s.Workshops[project].Canonical());
        }
    }

    // R-ARMS-004 worker_slots 1 (#1725 PR 7 review S2): a corrupted ledger with two worker records on one workshop's target
    // throws before step 6 writes anything. The first workshop in ascending project id is valid, yet it is not paid or
    // advanced: Treasury, Debt, Arms, every workshop and the ledger are unchanged, and no Why line is added.
    [Fact]
    public void ASecondWorkerRecordOnALaterWorkshopThrowsBeforeAnyWrite()
    {
        var c = Gdd03();
        var s = Dark(c);
        var first = Built(c, s, Haifa); var second = Built(c, s, Haifa);
        Assert.True(first < second);
        Assert.Null(Cities.Assign(c, s, Haifa, Workshops.Role, 2, new List<Cause>()));
        var p = Free(s, Haifa)[0];   // a fixture: a second record on the second workshop's (already bound) target
        s.PopulationLedger.Commit(new[] { p.WithPlacement(p.Status, p.Location, new PopulationAssignment(PopulationAssignmentKind.Work, Target(second)), p.Captor, p.Home) }, Array.Empty<PopulationTarget>());
        Assert.Equal((1, 2), (On(s, first).Count, On(s, second).Count));
        var ledger = s.PopulationLedger.Canonical().ToList();
        var shops = s.Workshops.Values.Select(w => w.Canonical()).ToList();
        var money = (s.Treasury, s.Debt, s.Arms);
        var why = new List<Cause>();

        Assert.Contains("more than one worker record", Assert.Throws<InvalidOperationException>(() => Workshops.Produce(c, s, why)).Message);

        Assert.Equal(money, (s.Treasury, s.Debt, s.Arms));
        Assert.Equal(shops, s.Workshops.Values.Select(w => w.Canonical()).ToList());
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Empty(why);
    }

    // R-POP-002: on a Historical ledger only the "workshop" role runs; "job" and "build" are still refused before any write.
    [Theory]
    [InlineData("job")] [InlineData(Projects.BuildRole)]
    public void OtherRolesAreStillRefusedOnTheLedger(string role)
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Cities.Assign(c, s, Haifa, role, 1, new List<Cause>())).Message);
        Assert.Equal(hash, s.Hash());
    }

    // R-POP-002 (#1725 PR 7 review S2): only a ledger initialized with mode Historical is let through. An initialized Open
    // ledger, like a synthetic one (no mode), is still refused by both entry points, with the state unchanged.
    [Theory]
    [InlineData(true, true)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(false, false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open, bool produce)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        s.CityProjects.Add(0, new CityProject(0, Workshops.ArmsWorkshop, "missing", 0, 0, 0, s.Date));
        s.Workshops.Add(0, new Workshop(0, true, SyntheticWorkshopTier.Ordinary));
        s.Treasury = 23; s.Arms = 5;
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        Action call = produce ? () => Workshops.Produce(c, s, why) : () => Cities.Assign(c, s, "missing", Workshops.Role, 1, why);
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(call).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }
}
