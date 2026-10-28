using Statecraft.Sim;

// Synthetic fixtures only. These tests do not exercise production farming, projects, payments or replay.
public class CivilianProfessionRulesTests
{
    static Content Catalogue()
    {
        var c = PopulationLedgerTests.Content();
        c.Gdd03Balance = ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance;
        return c;
    }
    static PopulationUnit Person(CivilianProfession profession, CivilianTier tier, int xp,
        PopulationAssignmentKind assignment = PopulationAssignmentKind.Work)
        => new PopulationUnit(0, assignment == PopulationAssignmentKind.Course ? PopulationStatus.Trainee : PopulationStatus.FreeCivilian,
            6, new PopulationLocation("fixture"),
            assignment == PopulationAssignmentKind.Unassigned ? PopulationAssignment.Unassigned : new PopulationAssignment(assignment, "fixture-target"),
            profession, tier, xp, "", 7, new PopulationEquipment("fixture-tools", true, "fixture-arms", true));
    static GameState State(Content c, PopulationUnit p) => PopulationLedger.CreateFixture(c,
        new PopulationBindings(new[] { new PopulationLocation("fixture") },
            new[] { new PopulationTarget("fixture-target", p.Assignment.Kind == PopulationAssignmentKind.Unassigned
                ? PopulationAssignmentKind.Work : p.Assignment.Kind, "fixture",
                p.Assignment.Kind == PopulationAssignmentKind.Course ? 3 : (int?)null) },
            Array.Empty<string>(), Array.Empty<string>()),
        new[] { new PopulationInventoryRow("synthetic", 0, p) });
    static CivilianWorkEvidence Work(CivilianProfession p, decimal amount = 0.75m)
        => new CivilianWorkEvidence(0, "fixture-target", p == CivilianProfession.Farmer
            ? CivilianWorkKind.StaffedFarming : CivilianWorkKind.PioneerAdvancement, amount);

    // R-VET-001/002: a staffed farming turn or fractional pioneer advancement counts once; ninth output retains the old tier.
    [Theory]
    [InlineData(CivilianProfession.Farmer, CivilianTier.Novice)]
    [InlineData(CivilianProfession.Farmer, CivilianTier.Trained)]
    [InlineData(CivilianProfession.Pioneer, CivilianTier.Novice)]
    [InlineData(CivilianProfession.Pioneer, CivilianTier.Trained)]
    public void NinthWorkingTurnPromotesOnlyTheReturnedNextTurnRecord(CivilianProfession profession, CivilianTier tier)
    {
        var c = Catalogue(); var s = State(c, Person(profession, tier, 8)); var before = s.Describe();
        var p = s.PopulationLedger.Get(0);
        var expectedRate = profession == CivilianProfession.Farmer ? (tier == CivilianTier.Novice ? 2m : 3m)
            : (tier == CivilianTier.Novice ? 0.75m : 1m);
        Assert.Equal(expectedRate, CivilianProfessionRules.OutputAtTurnStart(c, s, 0));
        var next = CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(profession, 0.75m), Work(profession) });
        Assert.Equal(9, next.WorkXp); Assert.Equal(CivilianTier.Veteran, next.Tier);
        Assert.Equal(before, s.Describe()); Assert.Equal(tier, p.Tier);
        Assert.Equal(p.Id, next.Id); Assert.Same(p.Location, next.Location); Assert.Same(p.Assignment, next.Assignment);
        Assert.Same(p.Equipment, next.Equipment); Assert.Equal(p.CombatXp, next.CombatXp);
        Assert.Equal(p.ReceptionTicks, next.ReceptionTicks); Assert.Equal(p.Qualification, next.Qualification);
        s.PopulationLedger.Replace(next);
        Assert.Equal(profession == CivilianProfession.Farmer ? 5m : 1.5m, CivilianProfessionRules.OutputAtTurnStart(c, s, 0));
    }

    // R-VET-001: completed staffed farming counts even with zero output; the source snapshot stays unchanged.
    [Fact] public void CompletedStaffedFarmingTurnWithZeroOutputEarnsOneXp()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Novice, 0));
        var before = s.Describe(); var p = s.PopulationLedger.Get(0);
        var next = CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(CivilianProfession.Farmer, 0) });
        Assert.Equal(1, next.WorkXp); Assert.Equal(CivilianTier.Novice, next.Tier);
        Assert.Equal(0, p.WorkXp); Assert.Same(p, s.PopulationLedger.Get(0)); Assert.Equal(before, s.Describe());
    }

    // R-VET-001, R-POP-002: matching work requires completed reception before XP or promotion.
    [Theory]
    [InlineData(CivilianProfession.Farmer, 0)]
    [InlineData(CivilianProfession.Farmer, 3)]
    [InlineData(CivilianProfession.Farmer, 4)]
    [InlineData(CivilianProfession.Farmer, 5)]
    [InlineData(CivilianProfession.Farmer, 6)]
    [InlineData(CivilianProfession.Pioneer, 0)]
    [InlineData(CivilianProfession.Pioneer, 3)]
    [InlineData(CivilianProfession.Pioneer, 4)]
    [InlineData(CivilianProfession.Pioneer, 5)]
    [InlineData(CivilianProfession.Pioneer, 6)]
    public void ReceptionMustFinishBeforeMatchingWorkEarnsXp(CivilianProfession profession, int receptionTicks)
    {
        var c = Catalogue(); var s = State(c, Person(profession, CivilianTier.Novice, 8).WithReception(receptionTicks));
        var before = s.Describe(); var p = s.PopulationLedger.Get(0);
        var next = CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(profession) });
        if (receptionTicks < 6)
        {
            Assert.Same(p, next); Assert.Equal(8, next.WorkXp); Assert.Equal(CivilianTier.Novice, next.Tier);
        }
        else
        {
            Assert.NotSame(p, next); Assert.Equal(9, next.WorkXp); Assert.Equal(CivilianTier.Veteran, next.Tier);
        }
        Assert.Same(p, s.PopulationLedger.Get(0)); Assert.Equal(8, p.WorkXp); Assert.Equal(CivilianTier.Novice, p.Tier);
        Assert.Equal(before, s.Describe());
    }

    // R-VET-001: no service shortcut; pioneers need matching positive project-advancement evidence.
    [Theory]
    [InlineData(PopulationAssignmentKind.Unassigned)]
    [InlineData(PopulationAssignmentKind.Teaching)]
    [InlineData(PopulationAssignmentKind.Course)]
    [InlineData(PopulationAssignmentKind.Work)]
    public void IneligibleOrUnproductiveTurnsDoNotAdvance(PopulationAssignmentKind assignment)
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Novice, 6, assignment));
        var before = s.Describe();
        var evidence = assignment == PopulationAssignmentKind.Work
            ? new[] { Work(CivilianProfession.Pioneer, 0), Work(CivilianProfession.Farmer),
                new CivilianWorkEvidence(0, "fixture-target", CivilianWorkKind.GenericWork, 1),
                new CivilianWorkEvidence(0, "other-target", CivilianWorkKind.PioneerAdvancement, 1) }
            : new[] { Work(CivilianProfession.Pioneer) };
        var next = CivilianProfessionRules.AfterWork(c, s, 0, evidence);
        Assert.Same(s.PopulationLedger.Get(0), next); Assert.Equal(before, s.Describe());
        Assert.Same(next, CivilianProfessionRules.AfterWork(c, s, 0, Array.Empty<CivilianWorkEvidence>()));
    }

    // R-VET-001: interruption does not erase history; craftsman progression is owned by #1698.
    [Fact] public void InterruptedWorkAndCraftsmenNeverUseServiceOrCourseTicksAsXp()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Novice, 6));
        var next = CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(CivilianProfession.Pioneer) });
        Assert.Equal(7, next.WorkXp); Assert.Equal(CivilianTier.Novice, next.Tier);
        s.PopulationLedger.Replace(next); s.PopulationLedger.ChangeAssignment(0, PopulationAssignment.Unassigned);
        Assert.Equal(7, CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(CivilianProfession.Pioneer) }).WorkXp);
        s.PopulationLedger.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "fixture-target"));
        Assert.Equal(8, CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(CivilianProfession.Pioneer) }).WorkXp);
        var craft = State(c, Person(CivilianProfession.Craftsman, CivilianTier.Apprentice, 8));
        Assert.Same(craft.PopulationLedger.Get(0), CivilianProfessionRules.AfterWork(c, craft, 0, new[] { Work(CivilianProfession.Pioneer) }));
    }

    // R-QUAL-008: matching qualification preserves work/equipment; switching is refused without mutation.
    [Theory] [InlineData(CivilianTier.Novice)] [InlineData(CivilianTier.Trained)] [InlineData(CivilianTier.Veteran)]
    public void MatchingCoursePreservesHistoryAndNeverDowngrades(CivilianTier tier)
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, tier, 8, PopulationAssignmentKind.Course));
        var before = s.Describe(); var p = s.PopulationLedger.Get(0);
        var next = CivilianProfessionRules.AfterMatchingCourse(c, s, 0, CivilianProfession.Pioneer);
        Assert.Equal(tier == CivilianTier.Veteran ? tier : CivilianTier.Trained, next.Tier);
        Assert.Equal(8, next.WorkXp); Assert.Equal(p.Id, next.Id); Assert.Same(p.Equipment, next.Equipment);
        Assert.Same(p.Assignment, next.Assignment); Assert.Equal(p.CombatXp, next.CombatXp);
        Assert.Throws<ArgumentException>(() => CivilianProfessionRules.AfterMatchingCourse(c, s, 0, CivilianProfession.Farmer));
        Assert.Equal(before, s.Describe());
    }

    // R-QUAL-008: this is a catalogue quote, not an affordability/credit decision or a payment.
    [Fact] public void ToolsQuotesReadOnlyAgreedCatalogueAndNeverTouchMoney()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Novice, 0));
        var before = s.Describe();
        Assert.Equal(3, CivilianProfessionRules.ToolsPrice(c, s, CivilianProfession.Farmer));
        Assert.Equal(5, CivilianProfessionRules.ToolsPrice(c, s, CivilianProfession.Pioneer));
        Assert.Throws<ArgumentException>(() => CivilianProfessionRules.ToolsPrice(c, s, CivilianProfession.Craftsman));
        Assert.Equal(before, s.Describe());
    }

    // R-VET-001/002, R-QUAL-008: missing/Deferred/invalid data never falls back to old rules.
    [Theory] [InlineData("profession.work_to_veteran")] [InlineData("profession.pioneer_tools")]
    [InlineData("pioneer.work_rate")] [InlineData("farmer.output")]
    public void InvalidCatalogueFailsClosed(string key)
    {
        foreach (var fault in new[] { "missing", "Deferred", "zero", "shape", "expression", "ruleset" })
        {
            var c = Catalogue(); var profession = key == "farmer.output" ? CivilianProfession.Farmer : CivilianProfession.Pioneer;
            var s = State(c, Person(profession, CivilianTier.Novice, 8)); var before = s.Describe();
            if (fault == "missing") c.Gdd03Balance!.Values.Remove(key);
            else if (fault == "Deferred") c.Gdd03Balance!.Values[key].Basis = "Deferred";
            else if (fault == "zero") c.Gdd03Balance!.Values[key].Values[0] = 0;
            else if (fault == "shape") c.Gdd03Balance!.Values[key].Values.Add(1);
            else if (fault == "expression") c.Gdd03Balance!.Values[key].Expression = "Deferred";
            else c.Gdd03Balance!.Ruleset = SaveGame.GddRuleset;
            Assert.Throws<ContentException>(() => {
                if (key == "profession.work_to_veteran") CivilianProfessionRules.AfterWork(c, s, 0, new[] { Work(profession) });
                else if (key == "profession.pioneer_tools") CivilianProfessionRules.ToolsPrice(c, s, profession);
                else CivilianProfessionRules.OutputAtTurnStart(c, s, 0);
            });
            Assert.Equal(before, s.Describe());
        }
    }

    // R-POP-001/002, R-QUAL-008: internal helpers require an initialized exact-identity fixture.
    [Theory] [InlineData("legacy")] [InlineData("gdd-0.1")] [InlineData("gdd-0.3")]
    public void UninitializedGameCannotUseHelpers(string identity)
    {
        var c = PopulationLedgerTests.Content(identity); var s = new GameState(c); var before = s.Describe();
        Assert.Throws<InvalidOperationException>(() => CivilianProfessionRules.ToolsPrice(c, s, CivilianProfession.Farmer));
        Assert.Throws<InvalidOperationException>(() => CivilianProfessionRules.AfterWork(c, s, 0, Array.Empty<CivilianWorkEvidence>()));
        Assert.Throws<InvalidOperationException>(() => CivilianProfessionRules.OutputAtTurnStart(c, s, 0));
        Assert.Throws<InvalidOperationException>(() => CivilianProfessionRules.AfterMatchingCourse(c, s, 0, CivilianProfession.Pioneer));
        Assert.Equal(before, s.Describe());
    }

    // R-VET-001, R-POP-002: evidence validation is a helper contract; malformed evidence is refused atomically before a proposal.
    [Fact] public void InvalidEvidenceAndUnknownIdsRefuseWithoutMutation()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Novice, 8));
        var before = s.Describe();
        Assert.Equal("populationId", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CivilianWorkEvidence(-1, "fixture-target", CivilianWorkKind.PioneerAdvancement, 1)).ParamName);
        Assert.Equal("advancement", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CivilianWorkEvidence(0, "fixture-target", CivilianWorkKind.PioneerAdvancement, -1)).ParamName);
        Assert.Throws<ArgumentException>(() => new CivilianWorkEvidence(0, "fixture-target", (CivilianWorkKind)99, 1));
        Assert.Throws<ArgumentException>(() => CivilianProfessionRules.AfterWork(c, s, 99, new[] { Work(CivilianProfession.Pioneer) }));
        Assert.Throws<ArgumentException>(() => CivilianProfessionRules.AfterWork(c, s, 0,
            new[] { Work(CivilianProfession.Pioneer), new CivilianWorkEvidence(1, "fixture-target", CivilianWorkKind.PioneerAdvancement, 1) }));
        Assert.Equal(before, s.Describe());
    }
    // R-VET-001/002, R-POP-002/003: stable ordering, atomic refusal and clone guarantees; checked overflow is helper validation.
    [Fact] public void EvaluationIsOrderIndependentAndOverflowCannotPartiallyCommit()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Novice, 8));
        var evidence = new[] { Work(CivilianProfession.Pioneer), Work(CivilianProfession.Pioneer, 0), Work(CivilianProfession.Pioneer) };
        var before = s.Hash(); var clone = s.Clone();
        var a = CivilianProfessionRules.AfterWork(c, s, 0, evidence);
        var b = CivilianProfessionRules.AfterWork(c, clone, 0, evidence.Reverse());
        Assert.Equal(a.WorkXp, b.WorkXp); Assert.Equal(a.Tier, b.Tier);
        s.PopulationLedger.Replace(a); clone.PopulationLedger.Replace(b);
        Assert.Equal(s.Hash(), clone.Hash()); Assert.NotEqual(before, s.Hash());
        var full = State(c, Person(CivilianProfession.Pioneer, CivilianTier.Veteran, int.MaxValue));
        var fullBefore = full.Describe();
        Assert.Throws<OverflowException>(() => CivilianProfessionRules.AfterWork(c, full, 0, evidence));
        Assert.Equal(fullBefore, full.Describe());
    }

}
