using Statecraft.Sim;

/// <summary>#1725 PR 3, the teacher part (Refs #1689): the training centre's one veteran pioneer teacher in the Historical
/// ledger. Owner, 7 November 2026 (#1689 comment 6704565066), "Yes to all three (Recommended)": "the training centre's one
/// veteran pioneer teacher is a Haifa census unit". Owner, 7 November 2026, relayed by the coordinator (no comment link):
/// starting veterans' work XP "0 (Recommended)"; the teacher "Takeable, but last (Recommended)". Maintainer call (the
/// coordinator, eyalgolan-30, 7 November 2026): the teacher is the last census/haifa ordinal.</summary>
public class HistoricalPioneerTeacherTests
{
    const string Haifa = "census/haifa";

    static (Content C, IReadOnlyList<HistoricalPopulationRow> Inventory, IReadOnlyList<PopulationUnit> Units) Start()
    {
        var c = HistoricalPopulationTests.Real(true);
        return (c, HistoricalPopulation.Inventory(c), HistoricalPopulation.LedgerFixture(c).PopulationLedger.Units);
    }

    static List<(HistoricalPopulationRow Row, PopulationUnit Unit)> Census((Content C, IReadOnlyList<HistoricalPopulationRow> Inventory, IReadOnlyList<PopulationUnit> Units) s)
        => s.Inventory.Select((r, i) => (r, s.Units[i])).Where(x => x.r.Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal)).ToList();

    // The ruling: one veteran pioneer teacher, a Haifa census unit, "included in population total" (content/gdd03-balance.yaml
    // start.pioneer_teachers, 1, Agreed; docs/gdd/training.md:48, "The starting centre and veteran pioneer teacher belong to
    // the initial population inventory, not additional people."). So exactly one census record has a profession, it is a
    // census/haifa record, and no record is added: the census still counts each city's R-CITY-007 start units.
    [Fact]
    public void OneCensusRecordIsTheTeacherAndItIsAHaifaCensusUnit()
    {
        var s = Start();
        var census = Census(s);
        Assert.Equal(s.C.Cities.Sum(city => city.Civilians), census.Count);
        Assert.Equal(1, s.C.RequirePopulationInteger("start.pioneer_teachers"));
        var teachers = census.Where(x => x.Unit.Profession != CivilianProfession.None).ToList();
        Assert.Single(teachers);
        Assert.Equal(Haifa, teachers[0].Row.Source);
    }

    // Maintainer call (the coordinator, 7 November 2026): the teacher is the LAST census/haifa ordinal, derived from content as
    // Haifa's start units minus 1, never a hard-coded number.
    [Fact]
    public void TheTeacherIsTheLastHaifaCensusOrdinal()
    {
        var s = Start();
        var haifaUnits = s.C.Cities.Single(city => city.Id == "haifa").Civilians;
        Assert.True(haifaUnits > 1);
        var teacher = Census(s).Single(x => x.Unit.Profession != CivilianProfession.None);
        Assert.Equal(haifaUnits - 1, teacher.Row.Ordinal);
        Assert.Equal(s.Inventory.Where(r => r.Source == Haifa).Max(r => r.Ordinal), teacher.Row.Ordinal);
    }

    // R-TRAIN-005: "The teacher is a free civilian, a veteran of the matching profession" (a pioneer, for the ruling's
    // "veteran pioneer teacher"); the ledger's pioneer tiers are novice, trained or veteran (PopulationLedger.Validate).
    // Owner, 7 November 2026, relayed: starting veterans' work XP "0 (Recommended)". R-TRAIN-008: "a teaching teacher with no
    // pupil (an invalid inventory)" is refused, and the start has no pupil, so the teacher holds no assignment: one of
    // Haifa's jobless. Everything else is as for any census record (R-POP-004): in Haifa, reception done, no military
    // qualification, no combat XP, no equipment.
    [Fact]
    public void TheTeacherIsAnUnassignedVeteranPioneerWithNoWorkXp()
    {
        var s = Start();
        var t = Census(s).Single(x => x.Unit.Profession != CivilianProfession.None).Unit;
        Assert.Equal(CivilianProfession.Pioneer, t.Profession);
        Assert.Equal(CivilianTier.Veteran, t.Tier);
        Assert.Equal(0, t.WorkXp);
        Assert.Equal(PopulationStatus.FreeCivilian, t.Status);
        Assert.Equal(PopulationAssignmentKind.Unassigned, t.Assignment.Kind);
        Assert.Equal("haifa", t.Location!.City);
        Assert.Equal(s.C.PlayerRegions.AtLocation("haifa")!.Id, t.Location.Region);
        Assert.Equal(s.C.RequirePopulationInteger("reception.turns"), t.ReceptionTicks);
        Assert.Equal("", t.Qualification);
        Assert.Equal(0, t.CombatXp);
        Assert.Same(PopulationEquipment.None, t.Equipment);
    }

    // R-TRAIN-008 (a teaching teacher with no pupil is an invalid inventory): no record starts teaching or on a course, and a
    // training centre fixture in Haifa's region accepts the opening ledger as it is. The teacher meets Start's teacher test:
    // a free civilian, a veteran farmer or pioneer (Gdd03TrainingCentreFixture.Start).
    [Fact]
    public void ATrainingCentreInHaifaAcceptsTheOpeningLedger()
    {
        var c = HistoricalPopulationTests.Real(true);
        var state = HistoricalPopulation.LedgerFixture(c);
        Assert.DoesNotContain(state.PopulationLedger.Units, u => u.Assignment.Kind == PopulationAssignmentKind.Teaching
            || u.Assignment.Kind == PopulationAssignmentKind.Course);
        var before = state.Hash();
        _ = new Gdd03TrainingCentreFixture(c, state, new[] {
            new Gdd03FixtureTrainingCentre("centre/haifa", c.PlayerRegions.AtLocation("haifa")!.Id, operating: true) });
        Assert.Equal(before, state.Hash());
        var t = state.PopulationLedger.Units.Single(u => u.Profession != CivilianProfession.None);
        Assert.True(Gdd03ReceptionFixture.CanStartCivilianActivity(c, state.PopulationLedger, t));
    }

    // Owner, 7 November 2026, relayed: the teacher "Takeable, but last (Recommended)". No guard: the teacher is an ordinary
    // jobless free civilian (it passes the same civilian-activity test as the others, above). "Last" comes from the rules
    // that exist: "N units" commands take ascending population ids (R-POP-004, Q10), and the job default gains or loses the
    // Industry assignment in ascending population id (#1692, owner answer of 3 November 2026). The teacher has the highest id
    // of Haifa's census records, so taking Haifa's first k records by ascending id reaches it only when k is all of them.
    // At the start Haifa's employed are fewer than its records less one, so a default up to its employed never reaches it.
    // No ledger producer runs the job default or Train yet (#1725 PR 6 and PR 7); this pins the order they will read.
    [Fact]
    public void TheTeacherIsTakenLastByAscendingId()
    {
        var s = Start();
        var haifa = Census(s).Where(x => x.Row.Source == Haifa).Select(x => x.Unit).OrderBy(u => u.Id).ToList();
        var teacher = haifa.Single(u => u.Profession != CivilianProfession.None);
        Assert.Equal(haifa.Max(u => u.Id), teacher.Id);
        var jobless = haifa.Where(u => u.Status == PopulationStatus.FreeCivilian && u.Assignment.Kind == PopulationAssignmentKind.Unassigned).ToList();
        Assert.Equal(haifa.Count, jobless.Count);
        for (int k = 1; k <= jobless.Count; k++)
            Assert.Equal(k == jobless.Count, jobless.Take(k).Any(u => u.Id == teacher.Id));
        var employed = s.C.Cities.Single(city => city.Id == "haifa").Employed;
        Assert.True(employed < haifa.Count - 1);
        Assert.DoesNotContain(jobless.Take(employed), u => u.Id == teacher.Id);
    }

    // The count is content's start.pioneer_teachers (1, Agreed), which the ruling's "one" matches. No ruling places a second
    // teacher, so any other value is refused, all or nothing, and no value is invented.
    [Fact]
    public void AnotherTeacherCountIsRefused()
    {
        var root = RealContentTests.ContentRoot();
        var c = ContentLoader.Load(rel =>
        {
            var text = File.Exists(Path.Combine(root, rel)) ? File.ReadAllText(Path.Combine(root, rel)) : null;
            if (rel == "eras.yaml") return text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            if (rel == Gdd03BalanceCatalogue.FileName)
            {
                var row = "\"start.pioneer_teachers\":";
                var at = text!.IndexOf(row, StringComparison.Ordinal);
                var values = text.IndexOf("\"values\": [1]", at, StringComparison.Ordinal);
                Assert.True(at >= 0 && values > at);
                return text.Substring(0, values) + "\"values\": [2]" + text.Substring(values + "\"values\": [1]".Length);
            }
            return text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
        Assert.Equal(2, c.RequirePopulationInteger("start.pioneer_teachers"));
        var s = new GameState(c) { Mode = Mode.Historical };
        var ex = Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.Initialize(c, s));
        Assert.Contains("start.pioneer_teachers", ex.Message);
        Assert.False(s.PopulationLedger.IsInitialized);
    }
}
