using Statecraft.Sim;

/// <summary>#1725 PR 3, the employed part (Refs #1689, #1692): the census cities' employed units hold Industry work records
/// on the Historical ledger (R-POP-004). Owner, 7 November 2026 (#1689 comment 6704389240), "Keep people x share
/// (Recommended)": a city's employed is its people times its share (R-CITY-007); (#1689 comment 6704428057) "Start in
/// Industry (Recommended)": every employed unit starts in Industry. The #1725 PR 3 bullet: a work kind and a capacity on
/// <c>PopulationTarget</c>, checked in <c>Validate</c>; the capacity is each city's <c>job_slots</c> (R-CITY-007), and
/// <c>start.industry_slots</c> is the 10,000-scale value, never a city's Industry capacity. #1692 (owner, 3 November 2026,
/// "Approve all three (Recommended)"): records gain the Industry assignment in ascending population id; its Q27, "a worker
/// works in their own city". Farmers stay out of employed (#1725 PR 4); the Haifa teacher is taken last (#1689 comment
/// 6710877137, "Takeable, but last (Recommended)").</summary>
public class HistoricalIndustryWorkersTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // A city's census civilians: its free civilians that are not field farmers (a farmer lives in its host, R-POP-004).
    static List<PopulationUnit> Census(GameState s, string city) => s.PopulationLedger.Units
        .Where(u => u.Status == PopulationStatus.FreeCivilian && u.Profession != CivilianProfession.Farmer
            && string.Equals(u.Location?.City, city, StringComparison.Ordinal)).OrderBy(u => u.Id).ToList();

    static bool InIndustry(PopulationUnit u, string city) => u.Assignment.Kind == PopulationAssignmentKind.Work
        && u.Assignment.Target == HistoricalPopulation.IndustryTarget(city);

    // R-POP-004, R-CITY-007: each census city's first `employed` census records, by ascending population id, hold Work on
    // the city's Industry target; the rest have no assignment. So the ledger's Industry count is the aggregate employed.
    [Fact]
    public void DarkStartEmployedHoldIndustryInAscendingId()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.NotEmpty(c.Cities);
        foreach (var city in c.Cities)
        {
            var census = Census(s, city.Id);
            Assert.Equal(city.Civilians, census.Count);
            Assert.True(city.Employed < census.Count, city.Id);
            Assert.Equal(census.Take(city.Employed).Select(u => u.Id), census.Where(u => InIndustry(u, city.Id)).Select(u => u.Id));
            Assert.All(census.Skip(city.Employed), u => Assert.Equal(PopulationAssignmentKind.Unassigned, u.Assignment.Kind));
            Assert.Equal(s.Cities[city.Id].Employed, census.Count(u => InIndustry(u, city.Id)));
        }
    }

    // The PR 3 bullet: the capacity is each city's job_slots (R-CITY-007), never start.industry_slots. One Industry target
    // per census city, in the city's region, naming the city; no other Industry target is bound at the start.
    [Fact]
    public void EachCitysIndustryCapacityIsItsJobSlots()
    {
        var c = Gdd03();
        var ledger = Dark(c).PopulationLedger;
        var industry = ledger.Bindings.Targets.Where(t => t.WorkKind == PopulationWorkKind.Industry).ToList();
        Assert.Equal(c.Cities.Select(x => HistoricalPopulation.IndustryTarget(x.Id)).OrderBy(x => x, StringComparer.Ordinal),
            industry.Select(t => t.Id));
        foreach (var city in c.Cities)
        {
            var t = ledger.Target(HistoricalPopulation.IndustryTarget(city.Id))!;
            Assert.Equal(PopulationAssignmentKind.Work, t.Kind);
            Assert.Equal(city.Id, t.City);
            Assert.Equal(c.PlayerRegions.AtLocation(city.Id)!.Id, t.Region);
            Assert.Equal(city.JobSlots, t.Capacity);
        }
        // start.industry_slots (8, Default) is a 10,000-scale value: Jerusalem's 29, Tel Aviv's 106 and Haifa's 42 are not it.
        Assert.Contains(c.Cities, x => x.JobSlots != 8);
        Assert.All(c.Cities, x => Assert.Equal(x.JobSlots, ledger.Target(HistoricalPopulation.IndustryTarget(x.Id))!.Capacity));
    }

    // Farmers stay out of employed (#1725 PR 4): each keeps its field's Work target. The Haifa teacher, the last census/haifa
    // record, is not among Haifa's employed (R-TRAIN-008: it starts unassigned). The ledger's Industry workers are exactly
    // the census cities' employed, in total too.
    [Fact]
    public void FarmersAndTheTeacherStayOutOfIndustry()
    {
        var c = Gdd03();
        var s = Dark(c);
        var units = s.PopulationLedger.Units;
        var farmers = units.Where(u => u.Profession == CivilianProfession.Farmer).ToList();
        Assert.NotEmpty(farmers);
        Assert.All(farmers, f => Assert.StartsWith(HistoricalPopulation.FarmerPrefix, f.Assignment.Target));
        var teacher = units.Single(u => u.Profession == CivilianProfession.Pioneer);
        Assert.Equal(PopulationAssignmentKind.Unassigned, teacher.Assignment.Kind);
        var industry = units.Where(u => u.Assignment.Kind == PopulationAssignmentKind.Work
            && u.Assignment.Target!.StartsWith(HistoricalPopulation.IndustryPrefix, StringComparison.Ordinal)).ToList();
        Assert.Equal(c.Cities.Sum(x => x.Employed), industry.Count);
        Assert.All(industry, u => Assert.Equal(CivilianProfession.None, u.Profession));
    }

    // Validate (R-POP-002): an Industry target holds at most its capacity. Haifa starts full, so one more Haifa worker is
    // refused, with the ledger unchanged; after one worker is released, the same record is accepted.
    [Fact]
    public void AFullCitysIndustryRefusesOneMoreWorker()
    {
        var c = Gdd03();
        var s = Dark(c);
        var ledger = s.PopulationLedger;
        var haifa = Census(s, "haifa");
        var target = new PopulationAssignment(PopulationAssignmentKind.Work, HistoricalPopulation.IndustryTarget("haifa"));
        var jobless = haifa.First(u => u.Assignment.Kind == PopulationAssignmentKind.Unassigned && u.Profession == CivilianProfession.None);
        var before = string.Join("\n", ledger.Canonical());
        Assert.Throws<ArgumentException>(() => ledger.ChangeAssignment(jobless.Id, target));
        Assert.Equal(before, string.Join("\n", ledger.Canonical()));

        ledger.ChangeAssignment(haifa.First(u => InIndustry(u, "haifa")).Id, PopulationAssignment.Unassigned);
        ledger.ChangeAssignment(jobless.Id, target);
        Assert.Equal(c.Cities.Single(x => x.Id == "haifa").JobSlots, Census(s, "haifa").Count(u => InIndustry(u, "haifa")));
    }

    // #1692 Q27, "a worker works in their own city": a record of another city in the same region is refused on an Industry
    // target, while the target's own city's record is accepted. Several census cities share a region, so the region check
    // alone would not catch it.
    [Fact]
    public void AnotherCitysRecordIsRefusedOnACitysIndustry()
    {
        var c = Gdd03();
        var s = Dark(c);
        var ledger = s.PopulationLedger;
        var pair = c.Cities.GroupBy(x => c.PlayerRegions.AtLocation(x.Id)!.Id).First(g => g.Count() > 1).Take(2).ToList();
        var region = c.PlayerRegions.AtLocation(pair[0].Id)!.Id;
        ledger.BindTarget(new PopulationTarget("test/industry", PopulationAssignmentKind.Work, region,
            workKind: PopulationWorkKind.Industry, city: pair[0].Id, capacity: 1000));
        var work = new PopulationAssignment(PopulationAssignmentKind.Work, "test/industry");
        var other = Census(s, pair[1].Id).First(u => u.Assignment.Kind == PopulationAssignmentKind.Unassigned);
        var own = Census(s, pair[0].Id).First(u => u.Assignment.Kind == PopulationAssignmentKind.Unassigned);
        Assert.Equal(region, other.Location!.Region);
        Assert.Throws<ArgumentException>(() => ledger.ChangeAssignment(other.Id, work));
        ledger.ChangeAssignment(own.Id, work);
        Assert.Equal(work.Target, ledger.Units.Single(u => u.Id == own.Id).Assignment.Target);
    }

    static GameState Fixture(Content c, string region, string city) => PopulationLedger.CreateFixture(c,
        new PopulationBindings(new[] { new PopulationLocation(region) }, new[] { new PopulationTarget("test/industry",
            PopulationAssignmentKind.Work, region, workKind: PopulationWorkKind.Industry, city: city, capacity: 1) },
            Array.Empty<string>(), Array.Empty<string>()),
        Array.Empty<PopulationInventoryRow>(), mode: Mode.Historical);

    // R-POP-002 (ValidateCities): an Industry target's city is a census city (cities.yaml, R-CITY-006) of its own region in
    // regions.yaml. A census city of another region is refused, and so is a member location of the region that is not a
    // census city; the refusal names the target and the region. The region's own census city is accepted.
    [Fact]
    public void AnIndustryTargetNeedsACensusCityOfItsRegion()
    {
        var c = Gdd03();
        var census = new HashSet<string>(c.Cities.Select(x => x.Id), StringComparer.Ordinal);
        var region = c.PlayerRegions.AtLocation("haifa")!;
        var elsewhere = c.Cities.First(x => c.PlayerRegions.AtLocation(x.Id)!.Id != region.Id).Id;
        var notCensus = region.MemberLocations.First(p => !census.Contains(p));
        foreach (var city in new[] { elsewhere, notCensus })
        {
            var e = Assert.Throws<ArgumentException>(() => Fixture(c, region.Id, city));
            Assert.Equal("Industry target test/industry needs a census city of region " + region.Id + " (R-POP-002).", e.Message);
        }
        Assert.True(Fixture(c, region.Id, "haifa").PopulationLedger.IsInitialized);
    }

    // The work kind: an Industry target is a Work target with its city and a nonnegative capacity; any other target has
    // neither. A target without the work kind keeps its canonical text, so no existing state hash moves.
    [Fact]
    public void AnIndustryTargetNeedsWorkItsCityAndACapacity()
    {
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.Work, "r", workKind: PopulationWorkKind.Industry, capacity: 1));
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.Work, "r", workKind: PopulationWorkKind.Industry, city: "x"));
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.Work, "r", workKind: PopulationWorkKind.Industry, city: "x", capacity: -1));
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.ActiveService, "r", workKind: PopulationWorkKind.Industry, city: "x", capacity: 1));
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.Work, "r", city: "x"));
        Assert.Throws<ArgumentException>(() => new PopulationTarget("i", PopulationAssignmentKind.Work, "r", capacity: 1));
        var ok = new PopulationTarget("i", PopulationAssignmentKind.Work, "r", workKind: PopulationWorkKind.Industry, city: "x", capacity: 0);
        Assert.Equal(0, ok.Capacity);

        var plain = new PopulationTarget("field/1,2", PopulationAssignmentKind.Work, "r");
        Assert.Equal(PopulationWorkKind.None, plain.WorkKind);
        Assert.Null(plain.City); Assert.Null(plain.Capacity);
        Assert.Equal(PopulationText.Fields("field/1,2", PopulationText.Assignment(PopulationAssignmentKind.Work), "r", null), plain.Canonical());
        Assert.NotEqual(ok.Canonical(), new PopulationTarget("i", PopulationAssignmentKind.Work, "r", workKind: PopulationWorkKind.Industry, city: "x", capacity: 1).Canonical());
    }

    // #1725 PR 7 (owner, 8 November 2026, "One snapshot per turn (Recommended)": harvest, Industry income and Food all read
    // it): the live month snapshot has one entry per Industry target, available with the target's capacity (an upper
    // bound; "Yes, recheck (Recommended)"), so every Industry worker is work-eligible and the snapshot builds on a dark start.
    [Fact]
    public void TheMonthSnapshotHasAnEntryPerIndustryTarget()
    {
        var c = Gdd03();
        var s = Dark(c);
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        foreach (var city in c.Cities)
        {
            var entry = month.Facilities.Single(f => f.Id == HistoricalPopulation.IndustryTarget(city.Id));
            Assert.True(entry.AvailableAtMonthFreeze);
            Assert.Equal(city.JobSlots, entry.OpeningCapacity);
        }
        var workers = s.PopulationLedger.Units.Where(u => u.Assignment.Kind == PopulationAssignmentKind.Work
            && u.Assignment.Target!.StartsWith(HistoricalPopulation.IndustryPrefix, StringComparison.Ordinal)).Select(u => u.Id).ToList();
        Assert.Equal(c.Cities.Sum(x => x.Employed), workers.Count);
        Assert.Subset(new HashSet<int>(month.WorkEligibleIds), new HashSet<int>(workers));
    }
}
