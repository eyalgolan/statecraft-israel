using Statecraft.Sim;

/// <summary>#1725 PR 7, producer 12: Train, Reinforce, CancelTraining and the training step (R-TRAIN-001 to R-TRAIN-004) run
/// on a game with an initialized Historical ledger, the dark start (R-POP-002). The trainees are the city's free-civilian
/// records without an assignment; the aggregate city fields are not written. The owner's rulings, 8 November 2026, each the
/// recommended option of the plan's section 6:
/// 1. "Recruit+crews+halutz (Rec.)": recruit, crews and halutz run; craftsman is refused with a plain reason until #2476.
/// 2. "Cohorts stay main (Rec.)": State.Cohorts stays canonical; a per-facility Course target (military_camp/&lt;city&gt;,
///    hakhshara/&lt;city&gt;); the cohort gets a sorted member-id list, hashed only when set.
/// 3. "Unskilled first, keep (Rec.)": Profession None first, then the qualified, each ascending id; the profession is kept dormant.
/// 4. "Full minus strength (Rec.)": Reinforce room is Unit.Full - Strength - training; refuse when Strength != the serving records.
/// 5. "Keep costs, pool rest (Rec.)": one issued item per record; training_arms - 1 more from stock, kept in Cohort.Arms for refund.
/// 6. "New unit at graduation (Rec.)": bind a new formation target at graduation, one formation per cohort (R-UNIT-003).
/// 7. "Free pioneer, not housed (Rec.)": a halutz graduate is FreeCivilian Pioneer Novice, Unassigned, in the city's region with no city.
/// 8. "Enforce it here (Rec.)": camp capacity, 50 records per camp city, over all military tracks.
/// 9. "Start + runtime bind (Rec.)": the start facilities' Course targets are bound at Initialize, plus one all-or-nothing
///    runtime BindTarget for new formations and built camps.
/// The tests call Training directly: Simulation.Step and TurnResolver.Resolve still refuse a ledger game (R-POP-002).</summary>
public class LedgerTrainingTests
{
    const string TelAviv = "tel_aviv";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);
        s.Arms = 200; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    static GameState Aggregate(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Arms = 200; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    // Ruling 3, "Unskilled first, keep (Rec.)": the city's free civilians without an assignment, Profession None first, then
    // the qualified, each ascending id.
    static List<PopulationUnit> Idle(GameState s, string city) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned && p.Location?.City == city)
        .OrderBy(p => p.Profession == CivilianProfession.None ? 0 : 1).ThenBy(p => p.Id).ToList();

    static string Region(Content c, string city) => c.PlayerRegions.AtLocation(city)!.Id;

    static (int Q, int R) Hex(Content c, string city) { var h = c.SettlementHexes.First(x => x.Id == city); return (h.Q, h.R); }

    static int OnlyCohort(GameState s) => Assert.Single(s.Cohorts).Key;

    static IEnumerable<(string, string, double, string, string)> Lines(IEnumerable<Cause> why) => why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key));

    // A target binding line of the ledger's canonical text (R-POP-003) whose target id is <paramref name="id"/>.
    static bool Bound(GameState s, string id) => s.PopulationLedger.Canonical().Any(line =>
        line.StartsWith("6:target|", StringComparison.Ordinal) && line.Contains(":" + id.Length + ":" + id + "|", StringComparison.Ordinal));

    static string Cities(GameState s) => string.Join(";", s.Cities.Values.Select(x => x.Canonical()));

    // R-TRAIN-001 on the ledger. Ruling 3: the first n idle records in Profession None then id order become trainees on
    // the facility's course (ruling 2: military_camp/<city>) in the same place (27M-7, no travel), progress 0. Ruling 5: each
    // is issued one military item from the stock. The Treasury pays once and the Why lines are the aggregate game's. No city
    // field is written, no id is made or lost, and the cohort lists its members.
    [Fact]
    public void TrainOnTheDarkStartTurnsFreeCiviliansIntoTrainees()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var picked = Idle(s, TelAviv).Take(2).ToList();
        var cities = Cities(s); var next = s.PopulationLedger.NextPopulationId; var count = s.PopulationLedger.Units.Count;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Training.Start(c, s, TelAviv, "recruit", 2, why));
        Assert.Null(Training.Start(c, twin, TelAviv, "recruit", 2, twinWhy));

        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Equal((198, 99.0, 0.0), (s.Arms, s.Treasury, s.Debt));
        Assert.Equal((twin.Arms, twin.Treasury, twin.Debt), (s.Arms, s.Treasury, s.Debt));
        Assert.Equal(cities, Cities(s));
        var id = OnlyCohort(s);
        Assert.Equal(twin.Cohorts.Keys, s.Cohorts.Keys);
        Assert.EndsWith("|members=" + string.Join(",", picked.Select(p => p.Id)), s.Cohorts[id].Canonical());
        Assert.DoesNotContain("members", twin.Cohorts[id].Canonical());
        foreach (var before in picked)
        {
            var p = s.PopulationLedger.Get(before.Id);
            Assert.Equal(PopulationStatus.Trainee, p.Status);
            Assert.Equal((PopulationAssignmentKind.Course, "military_camp/" + TelAviv, 0, (int?)null),
                (p.Assignment.Kind, p.Assignment.Target, p.Assignment.ProgressTicks, p.Assignment.TeacherId));
            Assert.Equal(before.Location!.Canonical(), p.Location!.Canonical());
            Assert.Equal(("training/" + id + "/" + p.Id, true), (p.Equipment.MilitaryId, p.Equipment.MilitaryPaid));
            Assert.Equal((before.ReceptionTicks, before.Profession, before.Tier, before.Qualification), (p.ReceptionTicks, p.Profession, p.Tier, p.Qualification));
        }
        Assert.Equal((next, count), (s.PopulationLedger.NextPopulationId, s.PopulationLedger.Units.Count));
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-002 on the ledger: the same records go back to free civilians without a job, in place, with their reception;
    // the items this course issued return to the stock (R-ARMS-003), so the ledger's records are as before the order and
    // spare plus issued Arms is unchanged. The Money is not refunded (D-A3). The Why lines are the aggregate game's.
    [Fact]
    public void CancelReturnsTheSameRecordsAndTheirArms()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var ledger = s.PopulationLedger.Canonical().ToList(); var cities = Cities(s);
        Assert.Null(Training.Start(c, s, TelAviv, "recruit", 3, new List<Cause>()));
        Assert.Null(Training.Start(c, twin, TelAviv, "recruit", 3, new List<Cause>()));
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Training.Cancel(c, s, OnlyCohort(s), why));
        Assert.Null(Training.Cancel(c, twin, OnlyCohort(twin), twinWhy));

        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Empty(s.Cohorts);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal((200, 98.5), (s.Arms, s.Treasury));
        Assert.Equal(cities, Cities(s));
        Assert.Equal(Training.RefusedNoCohort, Training.Cancel(c, s, 12345, why));
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-003 and R-UNIT-003 on the ledger. Ruling 6, "New unit at graduation (Rec.)": at the turn the cohort ends, a new
    // formation target is bound for the new map unit, and the records serve it as active soldiers with the type's class as
    // their qualification, keeping their item; the map unit's strength is its serving records. Each earlier turn counts one
    // course tick on each record.
    [Fact]
    public void ProgressGraduatesRecruitsIntoOneNewFormation()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var picked = Idle(s, TelAviv).Take(2).Select(p => p.Id).ToList();
        Assert.Null(Training.Start(c, s, TelAviv, "recruit", 2, new List<Cause>()));
        Assert.Null(Training.Start(c, twin, TelAviv, "recruit", 2, new List<Cause>()));
        var cohort = OnlyCohort(s);
        var turns = c.UnitTypes.First(t => t.Id == "recruit").TrainingTurns;
        Assert.True(turns > 1);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();
        for (int i = 1; i < turns; i++)
        {
            Training.Progress(c, s, why); Training.Progress(c, twin, twinWhy);
            Assert.All(picked, id => Assert.Equal(i, s.PopulationLedger.Get(id).Assignment.ProgressTicks));
            Assert.EndsWith("|members=" + string.Join(",", picked), s.Cohorts[cohort].Canonical());
        }
        var unitId = s.NextId;
        Assert.False(Bound(s, "formation/" + unitId));
        Training.Progress(c, s, why); Training.Progress(c, twin, twinWhy);

        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Empty(s.Cohorts);
        Assert.Equal(twin.Units[unitId].Canonical(), s.Units[unitId].Canonical());
        Assert.Equal(2, s.Units[unitId].Strength);
        Assert.Equal(unitId + 1, s.NextId);
        var target = "formation/" + unitId;
        foreach (var id in picked)
        {
            var p = s.PopulationLedger.Get(id);
            Assert.Equal((PopulationStatus.ActiveSoldier, PopulationAssignmentKind.ActiveService, target, "infantry"),
                (p.Status, p.Assignment.Kind, p.Assignment.Target, p.Qualification));
            Assert.Equal("training/" + cohort + "/" + id, p.Equipment.MilitaryId);
            Assert.Equal((Region(c, TelAviv), (string?)null), (p.Location!.Region, p.Location.City));
        }
        Assert.Equal(s.Units[unitId].Strength, s.PopulationLedger.Units.Count(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == target));
        Assert.True(Bound(s, target));
        Assert.Equal(198, s.Arms);
        s.PopulationLedger.Reconcile();
    }

    // Ruling 5, "Keep costs, pool rest (Rec.)": a gun crew takes its type's training_arms a group (R-ARMS-003), one as the
    // record's issued item and the rest from the stock into Cohort.Arms; cancelling returns them all.
    [Fact]
    public void ACrewTakesOneItemPerRecordAndPoolsTheRest()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var arty = c.UnitTypes.First(t => t.Id == "artillery");
        var per = arty.TrainingArms!.Value;
        Assert.True(per > 1);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();
        Assert.Null(Training.Start(c, s, TelAviv, "artillery", 2, why));
        Assert.Null(Training.Start(c, twin, TelAviv, "artillery", 2, twinWhy));
        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Equal(200 - 2 * per, s.Arms);
        var k = s.Cohorts[OnlyCohort(s)];
        Assert.Equal(2 * (per - 1), k.Arms);
        Assert.Equal(2, s.PopulationLedger.Units.Count(p => p.Equipment.MilitaryId?.StartsWith("training/" + k.Id + "/", StringComparison.Ordinal) == true));
        var back = new List<Cause>(); var twinBack = new List<Cause>();
        Assert.Null(Training.Cancel(c, s, k.Id, back));
        Assert.Null(Training.Cancel(c, twin, k.Id, twinBack));
        Assert.Equal(Lines(twinBack), Lines(back));
        Assert.Equal(200, s.Arms);
        s.PopulationLedger.Reconcile();
    }

    // An own opening infantry formation of the dark start, moved onto Tel Aviv's hex, after one of its records is lost in
    // combat: strength one less, Full the strength it had (R-TRAIN-004).
    static (GameState S, Unit U, string Target) Wounded(Content c)
    {
        var s = Dark(c);
        var i = Enumerable.Range(0, c.StartingForces.Count).First(j =>
        {
            var f = c.StartingForces[j];
            return f.Side == Movement.PlayerSide && f.Arrives == null && f.Strength >= 2
                && c.UnitTypes.First(t => t.Id == f.Type).Class == "infantry";
        });
        var target = HistoricalPopulation.FormationPrefix + c.StartingForces[i].Row;
        var u = s.Units[i];
        var lost = s.PopulationLedger.Units.First(p => p.Assignment.Target == target);
        s.PopulationLedger.RegisterCombatLoss(lost.Id, "fixture-loss");
        var (q, r) = Hex(c, TelAviv);
        s.Units[i] = new Unit(u.Id, u.Type, q, r, u.Service, u.Side, u.Strength - 1, u.Order, u.OrderTarget, u.HomeCity, u.Strength, u.CombatXp);
        return (s, s.Units[i], target);
    }

    // R-TRAIN-004 on the ledger. Ruling 4, "Full minus strength (Rec.)": the room is Unit.Full - Strength - training, and the
    // replacements graduate into the formation's own target as active soldiers, its strength back to Full.
    [Fact]
    public void ReinforceRefillsAStartingFormation()
    {
        var c = Gdd03();
        var (s, u, target) = Wounded(c);
        var why = new List<Cause>();
        Assert.Equal(string.Format(Training.RefusedRoomOne, u.Id, 1), Training.Reinforce(c, s, u.Id, TelAviv, 2, why));
        Assert.Empty(why);
        var picked = Idle(s, TelAviv).First().Id;
        Assert.Null(Training.Reinforce(c, s, u.Id, TelAviv, 1, why));
        Assert.Equal(string.Format(Training.RefusedNoLosses, u.Id), Training.Reinforce(c, s, u.Id, TelAviv, 1, why));
        var turns = c.UnitTypes.First(t => t.Id == "recruit").TrainingTurns;
        for (int t = 0; t < turns; t++) Training.Progress(c, s, why);
        Assert.Empty(s.Cohorts);
        Assert.Equal((u.Full, 0), (s.Units[u.Id].Strength, s.Units[u.Id].Full));
        var p = s.PopulationLedger.Get(picked);
        Assert.Equal((PopulationStatus.ActiveSoldier, target, "infantry"), (p.Status, p.Assignment.Target, p.Qualification));
        Assert.Equal(s.Units[u.Id].Strength, s.PopulationLedger.Units.Count(x => x.Status == PopulationStatus.ActiveSoldier && x.Assignment.Target == target));
        s.PopulationLedger.Reconcile();
    }

    // Ruling 4: "refuse when Strength != the serving records", with the state unchanged.
    [Fact]
    public void ReinforceIsRefusedWhenTheStrengthIsNotTheServingRecords()
    {
        var c = Gdd03();
        var (s, u, _) = Wounded(c);
        s.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength - 1, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp);
        var hash = s.Hash(); var why = new List<Cause>();
        Assert.Equal(string.Format("Unit {0} has {1} soldiers on record but strength {2}", u.Id, u.Strength, u.Strength - 1),
            Training.Reinforce(c, s, u.Id, TelAviv, 1, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
    }

    // R-TRAIN-004 (owner, 23 October 2026, "Default"): replacements whose unit is gone are cancelled as R-TRAIN-002 cancels a
    // cohort, on the ledger too: the records are free civilians again and the item returns to the stock.
    [Fact]
    public void ReplacementsWhoseUnitIsGoneAreCancelledOnTheLedger()
    {
        var c = Gdd03();
        var (s, u, _) = Wounded(c);
        var picked = Idle(s, TelAviv).First().Id;
        var arms = s.Arms;
        Assert.Null(Training.Reinforce(c, s, u.Id, TelAviv, 1, new List<Cause>()));
        s.Units.Remove(u.Id);
        var why = new List<Cause>();
        Training.Progress(c, s, why);
        Assert.Empty(s.Cohorts);
        Assert.Contains(why, x => x.Key == CauseKeys.ReplacementsStopped);
        Assert.Contains(why, x => x.Key == CauseKeys.TrainingCancelled);
        var p = s.PopulationLedger.Get(picked);
        Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned, (string?)null), (p.Status, p.Assignment.Kind, p.Equipment.MilitaryId));
        Assert.Equal(arms, s.Arms);
        s.PopulationLedger.Reconcile();
    }

    // Ruling 7, "Free pioneer, not housed (Rec.)": halutz trainees take the Hakhshara's course (hakhshara/<city>) and no
    // Arms; a graduate is a free civilian pioneer novice without an assignment in the city's region with no city, so the
    // city houses them no longer (27M-18). The map gets one halutz unit per group, as in the aggregate game.
    [Fact]
    public void HalutzTrainingOnTheLedger()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var picked = Idle(s, TelAviv).Take(2).Select(p => p.Id).ToList();
        var housed = Statecraft.Sim.Cities.Housed(s, s.Cities[TelAviv]);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();
        Assert.Null(Training.Start(c, s, TelAviv, "halutz", 2, why));
        Assert.Null(Training.Start(c, twin, TelAviv, "halutz", 2, twinWhy));
        Assert.Equal(200, s.Arms);
        Assert.All(picked, id => Assert.Equal(("hakhshara/" + TelAviv, (string?)null),
            (s.PopulationLedger.Get(id).Assignment.Target, s.PopulationLedger.Get(id).Equipment.MilitaryId)));
        Assert.Equal(housed, Statecraft.Sim.Cities.Housed(s, s.Cities[TelAviv]));
        var turns = c.UnitTypes.First(t => t.Id == "halutz").TrainingTurns;
        for (int t = 0; t < turns; t++) { Training.Progress(c, s, why); Training.Progress(c, twin, twinWhy); }
        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Equal(twin.Units.Values.Select(x => x.Canonical()), s.Units.Values.Select(x => x.Canonical()));
        Assert.Equal(twin.NextId, s.NextId);
        foreach (var id in picked)
        {
            var p = s.PopulationLedger.Get(id);
            Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned, CivilianProfession.Pioneer, CivilianTier.Novice),
                (p.Status, p.Assignment.Kind, p.Profession, p.Tier));
            Assert.Equal((Region(c, TelAviv), (string?)null), (p.Location!.Region, p.Location.City));
        }
        Assert.Equal(housed - 2, Statecraft.Sim.Cities.Housed(s, s.Cities[TelAviv]));
        s.PopulationLedger.Reconcile();
    }

    // Ruling 1, "Recruit+crews+halutz (Rec.)": craftsman is refused with a plain reason until #2476, before any other check of
    // the facility, with the state unchanged.
    [Fact]
    public void CraftsmanTrackIsRefusedOnTheLedger()
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash(); var why = new List<Cause>();
        Assert.Equal("Craft training does not run on the population ledger yet (#2476)", Training.Start(c, s, TelAviv, "craftsman", 1, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
    }

    // Owner, 8 November 2026, Q3, "Refuse, with reason (Recommended)": a military trainee who already carries a military item
    // is refused with a plain reason before any write, the state and the ledger unchanged. It was an error before the ruling.
    [Fact]
    public void ATraineeWhoCarriesAnItemIsRefusedWithAReason()
    {
        var c = Gdd03();
        var s = Dark(c);
        var first = Idle(s, TelAviv).First();
        s.PopulationLedger.Replace(new PopulationUnit(first.Id, first.Status, first.ReceptionTicks, first.Location, first.Assignment,
            first.Profession, first.Tier, first.WorkXp, first.Qualification, first.CombatXp, new PopulationEquipment(null, false, "carried", true),
            first.Captor, first.Home));
        var ledger = s.PopulationLedger.Canonical().ToList(); var hash = s.Hash(); var cities = Cities(s); var why = new List<Cause>();
        Assert.Equal("A civilian without work in Tel Aviv already carries a military item", Training.Start(c, s, TelAviv, "recruit", 1, why));
        Assert.Empty(why);
        Assert.Empty(s.Cohorts);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(hash, s.Hash()); Assert.Equal(cities, Cities(s));
    }

    // Owner, 8 November 2026, Q3, "Refuse, with reason (Recommended)": a farmer or craftsman on the halutz track is refused
    // with a plain reason before any write, the state and the ledger unchanged. It was an error before the ruling.
    [Theory]
    [InlineData(CivilianProfession.Farmer)] [InlineData(CivilianProfession.Craftsman)]
    public void AFarmerOrCraftsmanOnTheHalutzTrackIsRefusedWithAReason(CivilianProfession profession)
    {
        var c = Gdd03();
        var s = Dark(c);
        var first = Idle(s, TelAviv).First();
        s.PopulationLedger.Replace(new PopulationUnit(first.Id, first.Status, first.ReceptionTicks, first.Location, first.Assignment,
            profession, CivilianTier.Novice, 0, first.Qualification, first.CombatXp, first.Equipment, first.Captor, first.Home));
        var count = Idle(s, TelAviv).Count;
        var ledger = s.PopulationLedger.Canonical().ToList(); var hash = s.Hash(); var cities = Cities(s); var why = new List<Cause>();
        Assert.Equal("A farmer or craftsman in Tel Aviv cannot train as a halutz", Training.Start(c, s, TelAviv, "halutz", count, why));
        Assert.Empty(why);
        Assert.Empty(s.Cohorts);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(hash, s.Hash()); Assert.Equal(cities, Cities(s));
    }

    // Ruling 8, "Enforce it here (Rec.)": 50 records per camp city, over all military tracks; the 51st is refused, a crew
    // counting as a recruit, with the state unchanged. The Hakhshara is not a camp.
    [Fact]
    public void TheCampCapRefusesTheFiftyFirstTrainee()
    {
        var c = Gdd03();
        Assert.Equal(50, Gdd03MilitaryCatalogue.ReadCampCapacity(c.Gdd03Balance!));
        var s = Dark(c);
        Assert.True(Idle(s, TelAviv).Count > 60);
        var why = new List<Cause>();
        Assert.Null(Training.Start(c, s, TelAviv, "recruit", 49, why));
        Assert.Equal("Tel Aviv's military camp has room for 1 more trainees (capacity 50)", Training.Start(c, s, TelAviv, "artillery", 2, why));
        Assert.Null(Training.Start(c, s, TelAviv, "armoured_car", 1, why));
        var hash = s.Hash(); var count = why.Count;
        Assert.Equal("Tel Aviv's military camp has room for 0 more trainees (capacity 50)", Training.Start(c, s, TelAviv, "recruit", 1, why));
        Assert.Equal(hash, s.Hash()); Assert.Equal(count, why.Count);
        Assert.Null(Training.Start(c, s, TelAviv, "halutz", 1, why));
        s.PopulationLedger.Reconcile();
    }

    // Ruling 3, "Unskilled first, keep (Rec.)": the training centre's teacher, a Haifa pioneer veteran, is taken last (the
    // ruling on #1689: "Takeable, but last (Recommended)"), and keeps the profession dormant as a trainee. Haifa is given a
    // Hakhshara here, which has no camp cap, so every idle record can be ordered.
    [Fact]
    public void TheHaifaTeacherIsTakenLast()
    {
        var c = Gdd03();
        c.Cities.First(x => x.Id == HistoricalPopulation.TeacherCity).Facilities.Add("hakhshara");
        var s = Dark(c);
        var idle = Idle(s, HistoricalPopulation.TeacherCity);
        var teacher = Assert.Single(idle, p => p.Profession == CivilianProfession.Pioneer);
        Assert.Equal(teacher.Id, idle.Last().Id);
        Assert.True(idle.Count > 1);
        Assert.Null(Training.Start(c, s, HistoricalPopulation.TeacherCity, "halutz", idle.Count - 1, new List<Cause>()));
        Assert.Equal(PopulationStatus.FreeCivilian, s.PopulationLedger.Get(teacher.Id).Status);
        Assert.Null(Training.Start(c, s, HistoricalPopulation.TeacherCity, "halutz", 1, new List<Cause>()));
        var p = s.PopulationLedger.Get(teacher.Id);
        Assert.Equal((PopulationStatus.Trainee, CivilianProfession.Pioneer, CivilianTier.Veteran), (p.Status, p.Profession, p.Tier));
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-001: only free civilians without an assignment are idle. A field farmer (Work) and a refugee are not, so a
    // count past the city's idle records is refused with that count.
    [Fact]
    public void RefugeesAndFarmersAreNotIdle()
    {
        var c = Gdd03();
        var host = HistoricalPopulation.FieldFarmerHosts(c).First(h => c.Cities.Any(x => x.Id == h.Host)).Host;
        c.Cities.First(x => x.Id == host).Facilities.Add("hakhshara");
        var s = Dark(c);
        Assert.Contains(s.PopulationLedger.Units, p => p.Location?.City == host && p.Assignment.Kind == PopulationAssignmentKind.Work);
        var refugee = Idle(s, host).First();
        s.PopulationLedger.Transition(refugee.Id, PopulationStatus.Refugee, refugee.Location, PopulationAssignment.Unassigned);
        var idle = Idle(s, host).Count;
        var name = PlayerText.Settlement(c, host);
        var hash = s.Hash(); var why = new List<Cause>();
        Assert.Equal(string.Format(Training.RefusedIdle, name, idle), Training.Start(c, s, host, "halutz", idle + 1, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
    }

    // Ruling 9, "Start + runtime bind (Rec.)": a camp built at a camp site (R-CAMP-001) has no Course target at Initialize;
    // the first Train there binds it, all or nothing with the trainees.
    [Fact]
    public void ABuiltCampBindsItsCourseTargetAtRuntime()
    {
        var c = Gdd03();
        var site = c.Sites.First(x => x.Kinds.Contains("military_camp") && x.City != "" && c.Cities.Any(k => k.Id == x.City && !k.Facilities.Contains("military_camp")));
        var s = Dark(c);
        var target = "military_camp/" + site.City;
        Assert.True(Bound(s, "military_camp/" + TelAviv));
        Assert.True(Bound(s, "hakhshara/" + TelAviv));
        Assert.True(Bound(s, "military_camp/jerusalem"));
        Assert.False(Bound(s, target));
        s.Sites[site.Id] = new SiteWork(site.Id, null, 0, s.Date);
        Assert.Null(Training.Start(c, s, site.City, "recruit", 1, new List<Cause>()));
        Assert.True(Bound(s, target));
        s.PopulationLedger.Reconcile();
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through. An Open or synthetic ledger is still refused
    // by all four, with the state unchanged.
    [Theory]
    [InlineData("start", true)] [InlineData("start", false)] [InlineData("cancel", true)] [InlineData("cancel", false)]
    [InlineData("progress", true)] [InlineData("progress", false)] [InlineData("reinforce", true)] [InlineData("reinforce", false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(string call, bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        void Invoke()
        {
            switch (call)
            {
                case "start": Training.Start(c, s, "missing", "missing", 1, why); break;
                case "cancel": Training.Cancel(c, s, 0, why); break;
                case "progress": Training.Progress(c, s, why); break;
                default: Training.Reinforce(c, s, 0, "missing", 1, why); break;
            }
        }
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(Invoke).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // R-POP-003: the same orders on the same seed give the same ledger and state hash; Clone keeps the cohort's members, and a
    // clone's run matches the original's.
    [Fact]
    public void SameSeedSameLedgerCanonicalAndStateHash()
    {
        var c = Gdd03();
        GameState Run()
        {
            var s = Dark(c);
            Assert.Null(Training.Start(c, s, TelAviv, "recruit", 3, new List<Cause>()));
            Assert.Null(Training.Start(c, s, TelAviv, "halutz", 2, new List<Cause>()));
            Training.Progress(c, s, new List<Cause>());
            return s;
        }
        var a = Run(); var b = Run();
        Assert.Equal(a.PopulationLedger.Canonical(), b.PopulationLedger.Canonical());
        Assert.Equal(a.Hash(), b.Hash());
        var clone = a.Clone();
        Assert.Equal(a.Hash(), clone.Hash());
        for (int t = 0; t < 4; t++) { Training.Progress(c, a, new List<Cause>()); Training.Progress(c, clone, new List<Cause>()); }
        Assert.Equal(a.PopulationLedger.Canonical(), clone.PopulationLedger.Canonical());
        Assert.Equal(a.Hash(), clone.Hash());
        Assert.NotEqual(b.Hash(), a.Hash());
        a.PopulationLedger.Reconcile();
    }

    // Ruling 6 for a crew, and ruling 5 at graduation (#1725 issuecomment-6724941867): an artillery or armoured car cohort
    // ends as one new formation of its own type (R-UNIT-008), its records active soldiers serving formation/<unit id> with
    // the type's class as their qualification and their issued item kept. The pooled rest of training_arms is used up, so
    // the stock ends at the aggregate cost, as on the twin aggregate game.
    [Theory]
    [InlineData("artillery")] [InlineData("armoured_car")]
    public void ACrewCohortGraduatesIntoOneNewFormationOfItsType(string track)
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var type = c.UnitTypes.First(t => t.Id == track);
        var per = type.TrainingArms!.Value;
        var picked = Idle(s, TelAviv).Take(2).Select(p => p.Id).ToList();
        Assert.Null(Training.Start(c, s, TelAviv, track, 2, new List<Cause>()));
        Assert.Null(Training.Start(c, twin, TelAviv, track, 2, new List<Cause>()));
        var cohort = OnlyCohort(s);
        Assert.Equal(2 * (per - 1), s.Cohorts[cohort].Arms);
        var unitId = s.NextId;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();
        for (int i = 0; i < type.TrainingTurns; i++) { Training.Progress(c, s, why); Training.Progress(c, twin, twinWhy); }

        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.Empty(s.Cohorts);
        var unit = Assert.Single(s.Units.Values, u => u.Id >= unitId);
        Assert.Equal((unitId, track, 2), (unit.Id, unit.Type, unit.Strength));
        Assert.Equal(twin.Units[unitId].Canonical(), unit.Canonical());
        Assert.Equal(unitId + 1, s.NextId);
        var target = "formation/" + unitId;
        Assert.True(Bound(s, target));
        foreach (var id in picked)
        {
            var p = s.PopulationLedger.Get(id);
            Assert.Equal((PopulationStatus.ActiveSoldier, PopulationAssignmentKind.ActiveService, target, type.Class),
                (p.Status, p.Assignment.Kind, p.Assignment.Target, p.Qualification));
            Assert.Equal(("training/" + cohort + "/" + id, true), (p.Equipment.MilitaryId, p.Equipment.MilitaryPaid));
            Assert.Equal((Region(c, TelAviv), (string?)null), (p.Location!.Region, p.Location.City));
        }
        Assert.Equal(2, s.PopulationLedger.Units.Count(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == target));
        Assert.Equal(200 - 2 * per, s.Arms);
        Assert.Equal(twin.Arms, s.Arms);
        s.PopulationLedger.Reconcile();
    }

    // Ruling 9, "Start + runtime bind (Rec.)" (#1725 issuecomment-6724941867): the runtime bind is all or nothing. A target
    // id already bound is refused, and a Commit with one invalid record and one new target changes neither; the ledger's
    // canonical text, the state and its hash are unchanged.
    [Fact]
    public void ARefusedBindTargetLeavesTheLedgerUnchanged()
    {
        var c = Gdd03();
        var s = Dark(c);
        var ledger = s.PopulationLedger.Canonical().ToList(); var before = s.Describe(); var hash = s.Hash();
        var bound = "military_camp/" + TelAviv;
        Assert.True(Bound(s, bound));

        Assert.ThrowsAny<ArgumentException>(() => s.PopulationLedger.BindTarget(
            new PopulationTarget(bound, PopulationAssignmentKind.Course, Region(c, TelAviv), 0)));

        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        s.PopulationLedger.Reconcile();
    }

    [Fact]
    public void ARefusedCommitLeavesTheRecordsAndBindingsUnchanged()
    {
        var c = Gdd03();
        var s = Dark(c);
        var ledger = s.PopulationLedger.Canonical().ToList(); var before = s.Describe(); var hash = s.Hash();
        var p = Idle(s, TelAviv).First();
        var invalid = new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment, p.Profession, p.Tier, p.WorkXp,
            "no_such_class", p.CombatXp, p.Equipment, p.Captor, p.Home);
        var fresh = "formation/999999";
        Assert.False(Bound(s, fresh));

        Assert.ThrowsAny<ArgumentException>(() => s.PopulationLedger.Commit(new[] { invalid },
            new[] { new PopulationTarget(fresh, PopulationAssignmentKind.ActiveService, Region(c, TelAviv)) }));

        Assert.False(Bound(s, fresh));
        Assert.Equal(p.Qualification, s.PopulationLedger.Get(p.Id).Qualification);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        s.PopulationLedger.Reconcile();
    }
}
