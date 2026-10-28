using Statecraft.Sim;
using System.Globalization;

/// <summary>R-CITY-006, R-POP-003: explicit fixture state only; capacities are arbitrary test quantities.</summary>
public class PlayerRegionsTests
{
    const string A = "tel_aviv_jaffa", B = "central_plain";
    static Content Content() => PlayerRegionsContentTests.Load("player_regions:\n" + PlayerRegionsContentTests.TelAviv + PlayerRegionsContentTests.Central);
    static PlayerRegionPool Pool(string id, int housing = 7, decimal condition = .4m,
        IEnumerable<PlayerRegionFacility>? facilities = null) => new PlayerRegionPool(id, housing, condition, 3, 2,
            facilities ?? Array.Empty<PlayerRegionFacility>());
    static PopulationUnit Person(string region, int q, int r) => new PopulationUnit(0, PopulationStatus.FreeCivilian,
        0, new PopulationLocation(region, q, r), PopulationAssignment.Unassigned,
        CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None);
    static IEnumerable<PopulationInventoryRow> People(bool reverse = false)
    {
        var rows = new[] { new PopulationInventoryRow("fixture", 0, Person(A, -3, 19)),
            new PopulationInventoryRow("fixture", 1, Person(B, -4, 21)) };
        return reverse ? rows.Reverse() : rows;
    }
    static PopulationTarget[] Targets() => new[] { new PopulationTarget("fixture-a-job", PopulationAssignmentKind.Work, A),
        new PopulationTarget("fixture-b-job", PopulationAssignmentKind.Work, B) };
    static GameState State(Content c, IEnumerable<PlayerRegionPool>? pools = null, bool reverse = false) =>
        PlayerRegions.CreateFixture(c, pools ?? new[] { Pool(A), Pool(B) }, People(reverse), Targets(), Array.Empty<string>(), Array.Empty<string>());

    [Fact]
    public void OneRegionBaselineAndTwoRegionPoolsDoNotUseTownAggregates()
    {
        var c = PlayerRegionsContentTests.Load();
        var one = PlayerRegions.CreateFixture(c, new[] { Pool(A) }, People().Take(1), Targets().Take(1), Array.Empty<string>(), Array.Empty<string>());
        Assert.Single(one.PlayerRegions); Assert.Empty(one.Cities);
        Assert.Equal(1, PlayerRegions.PresentPopulation(one, A));
        Assert.Equal(7, one.PlayerRegions[A].OrdinaryHousing);
        var two = State(Content());
        Assert.Equal(new[] { B, A }, two.PlayerRegions.Keys);
        Assert.Equal(1, PlayerRegions.PresentPopulation(two, A)); Assert.Equal(1, PlayerRegions.PresentPopulation(two, B));
        Assert.Equal(2, two.PopulationLedger.Units.Count); two.PopulationLedger.Reconcile();
        Assert.Throws<ArgumentException>(() => PlayerRegions.PresentPopulation(two, "jaffa"));
    }

    [Fact]
    public void IndustryWorkshopAndDedicatedHousingStaySeparateAndContributeOnce()
    {
        var facilities = new List<PlayerRegionFacility> {
            new("fixture-industry", "tel_aviv", PlayerRegionFacilityKind.Industry, 5),
            new("fixture-workshop", "jaffa", PlayerRegionFacilityKind.Workshop, 11),
            new("fixture-reception", "tel_aviv", PlayerRegionFacilityKind.Reception, 13),
            new("fixture-training", "jaffa", PlayerRegionFacilityKind.Training, 17) };
        var c = Content(); var a = Pool(A, facilities: facilities);
        facilities.Clear();
        var s = State(c, new[] { a, Pool(B) });
        Assert.Equal(7, a.OrdinaryHousing); Assert.Equal(8, a.IndustrySlots); Assert.Equal(13, a.WorkshopSlots);
        Assert.Equal(13, a.ReceptionBeds); Assert.Equal(17, a.TrainingBeds); Assert.Equal(4, a.Facilities.Count);
        Assert.Equal(3, s.PlayerRegions[B].IndustrySlots); Assert.Equal(2, s.PlayerRegions[B].WorkshopSlots);
        Assert.Equal(0, s.PlayerRegions[B].ReceptionBeds); Assert.Equal(0, s.PlayerRegions[B].TrainingBeds);
        Assert.Equal(1, PlayerRegions.PresentPopulation(s, A)); // Two places and four facilities add no people.
        var changed = State(c, new[] { Pool(A, housing: 19, facilities: a.Facilities), Pool(B) }); // only housing differs
        Assert.NotEqual(s.Hash(), changed.Hash());
        Assert.Equal(s.PlayerRegions[B].Canonical(), changed.PlayerRegions[B].Canonical());
    }

    [Fact]
    public void LedgerRemainsTheOnlyPersonAndAssignmentAuthority()
    {
        var s = State(Content()); var l = s.PopulationLedger;
        l.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "fixture-a-job"));
        Assert.Equal(1, PlayerRegions.Assigned(s, A, PopulationAssignmentKind.Work));
        Assert.Equal(0, PlayerRegions.Assigned(s, B, PopulationAssignmentKind.Work));
        l.ChangeAssignment(0, PopulationAssignment.Unassigned); // released, so only the cross-region check can refuse the B job
        var before = s.Describe();
        Assert.Throws<ArgumentException>(() => l.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "fixture-b-job")));
        Assert.Equal(before, s.Describe()); Assert.Equal(0, PlayerRegions.Assigned(s, B, PopulationAssignmentKind.Work)); l.Reconcile();
        Assert.Equal(0, l.Get(0).Id); Assert.Equal(2, l.Units.Count);
        // This exercises existing ledger region validation, not a new reservation command or slot-capacity policy.
    }

    [Fact]
    public void ClonesAndInsertionOrderPreserveCanonicalStateAcrossCultures()
    {
        var c = Content(); var a = State(c); var b = State(c, new[] { Pool(B), Pool(A) }, reverse: true);
        Assert.Equal(a.Describe(), b.Describe());
        var clone = a.Clone(); var before = a.Describe();
        clone.PopulationLedger.RegisterCombatLoss(0, "fixture-loss");
        Assert.Equal(before, a.Describe()); Assert.NotEqual(before, clone.Describe());
        Assert.Equal(1, PlayerRegions.PresentPopulation(a, A)); Assert.Equal(0, PlayerRegions.PresentPopulation(clone, A));
        Assert.Same(a.PlayerRegions[A], clone.PlayerRegions[A]); // Immutable records may be shared.
        var culture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var name in new[] { "he-IL", "en-US", "de-DE" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                Assert.Equal(before, a.Describe()); Assert.Equal(before, State(c).Describe());
            }
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Theory]
    [InlineData(-1, 4, 3, 2)]
    [InlineData(7, -1, 3, 2)]
    [InlineData(7, 11, 3, 2)]
    [InlineData(7, 4, -1, 2)]
    [InlineData(7, 4, 3, -1)]
    public void InvalidCapacitiesAndConditionAreRefused(int housing, int conditionTenths, int industry, int workshop) =>
        Assert.Throws<ArgumentException>(() => new PlayerRegionPool(A, housing, conditionTenths / 10m, industry, workshop, Array.Empty<PlayerRegionFacility>()));

    [Fact]
    public void FacilityIdentityLocationAndExplicitStartCoverageAreRequired()
    {
        var c = Content();
        var f = new PlayerRegionFacility("fixture-facility", "tel_aviv", PlayerRegionFacilityKind.Industry, 1);
        Assert.Throws<ArgumentException>(() => Pool(A, facilities: new[] { f, f }));
        // Same id in two pools, each at a valid location of its own pool: only national facility uniqueness can refuse it.
        var twin = new PlayerRegionFacility("fixture-facility", "rishon_lezion", PlayerRegionFacilityKind.Industry, 1);
        Assert.Equal("Facility must have one owning region and a member location.",
            Assert.Throws<ArgumentException>(() => State(c, new[] { Pool(A, facilities: new[] { f }), Pool(B, facilities: new[] { twin }) })).Message);
        Assert.Throws<ArgumentException>(() => State(c, new[] { Pool(A) }));
        Assert.Throws<ArgumentException>(() => State(c, new[] { Pool(A), Pool(B), Pool(B) }));
        var outside = new PlayerRegionFacility("fixture-facility", "tiberias", PlayerRegionFacilityKind.Industry, 1);
        Assert.Throws<ArgumentException>(() => State(c, new[] { Pool(A, facilities: new[] { outside }), Pool(B) }));
        Assert.Throws<ArgumentException>(() => new PlayerRegionFacility("fixture-facility", "tel_aviv", PlayerRegionFacilityKind.Industry, -1));
        Assert.Throws<OverflowException>(() => new PlayerRegionPool(A, 1, 1m, int.MaxValue, 0, new[] { f }));
    }

    [Fact]
    public void GeographyDoesNotActivateAProductionPopulationOrBypassAggregateGuards()
    {
        var c = Content(); var production = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(production.PlayerRegions); Assert.False(production.PopulationLedger.IsInitialized);
        var fixture = State(c); var before = fixture.Describe();
        const string guardMessage = "Population-ledger fixtures cannot run aggregate systems; gdd-0.3 integration is held.";
        Assert.Equal(guardMessage, Assert.Throws<NotSupportedException>(() => Cities.Init(c, fixture)).Message);
        Assert.Equal(guardMessage, Assert.Throws<NotSupportedException>(() => Simulation.Step(c, fixture, Array.Empty<Command>())).Message);
        Assert.Equal(before, fixture.Describe());
        Assert.Equal("Regional fixtures require gdd-0.3.",
            Assert.Throws<InvalidOperationException>(() => State(PlayerRegionsContentTests.Load(modern: false))).Message);
    }
}
