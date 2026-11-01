using System.Globalization;
using Statecraft.Sim;

// All identities, eligibility flags, progress and work receipts below are test-only synthetic inputs.
public class Gdd03CraftArithmeticTests
{
    static Gdd03BalanceCatalogue Catalogue() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;
    static Gdd03CraftArithmetic Arithmetic() => Gdd03CraftArithmetic.Read(Catalogue());

    // R-CRAFT-001/002: expected values independently transcribed from gdd03-balance.yaml:138-185,190-193. The course is
    // 0.5 Money and 3 turns (owner ruling, 30 October 2026: "Keep the set values (Recommended)"; #2115), not Amir's 5 / 6.
    [Theory]
    [InlineData("he-IL")]
    [InlineData("en-US")]
    public void CatalogueAndThreeCourseTicksUseOnlyCourseIncome(string culture)
    {
        var old = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var a = Arithmetic();
            Assert.Equal(0.5m, a.CourseCost); Assert.Equal(3, a.CourseTurns);
            Assert.Equal(1m, a.CourseIncome); Assert.Equal(36, a.ExpertWorkTurns);
            var progress = 0; decimal income = 0;
            for (var turn = 1; turn <= 3; turn++)
            {
                var r = a.CourseTick(17, progress, 4, eligibleTurn: true);
                Assert.Equal(17, r.UnitId); Assert.Equal(4, r.WorkXp);
                Assert.Equal(turn, r.ProgressTicks); Assert.Equal(turn == 3, r.Completes);
                Assert.Equal(1m, r.CourseIncome); Assert.Equal(0m, r.IndustryIncome);
                progress = r.ProgressTicks; income += r.CourseIncome;
            }
            Assert.Equal(3m, income);
            var idle = a.CourseTick(17, 2, 4, eligibleTurn: false);
            Assert.Equal(2, idle.ProgressTicks); Assert.False(idle.Completes);
            Assert.Equal(0m, idle.CourseIncome); Assert.Equal(0m, idle.IndustryIncome);
            // Complete-course input is invalid: caller must decide follow-on policy elsewhere.
            Assert.Throws<ArgumentOutOfRangeException>(() => a.CourseTick(17, 3, 4, true));
        }
        finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }

    // R-CRAFT-002: one confirmed work receipt, regardless of output/rate; old tier supplies this turn's quotes.
    [Theory]
    [InlineData("Industry")]
    [InlineData("Workshop")]
    [InlineData("Construction")]
    public void ActualWorkCrossesThresholdAfterOldTierOutput(string activityName)
    {
        var activity = Enum.Parse<CraftTickActivity>(activityName);
        var a = Arithmetic();
        var before = a.WorkTick(17, CivilianTier.Apprentice, 34, activity, true);
        Assert.Equal(35, before.WorkXp); Assert.Equal(CivilianTier.Apprentice, before.TierAfter);
        var crossing = a.WorkTick(17, before.TierAfter, before.WorkXp, activity, true);
        Assert.Equal(17, crossing.UnitId); Assert.Equal(36, crossing.WorkXp);
        Assert.Equal(CivilianTier.Apprentice, crossing.OutputTier); Assert.Equal(CivilianTier.Expert, crossing.TierAfter);
        Assert.Equal(activity == CraftTickActivity.Industry ? 2m : 0m, crossing.IndustryIncome);
        Assert.Equal(activity == CraftTickActivity.Construction ? 1.5m : 0m, crossing.BuilderWork);
        var next = a.WorkTick(17, crossing.TierAfter, crossing.WorkXp, activity, true);
        Assert.Equal(37, next.WorkXp); Assert.Equal(CivilianTier.Expert, next.OutputTier);
        Assert.Equal(activity == CraftTickActivity.Industry ? 3m : 0m, next.IndustryIncome);
        Assert.Equal(activity == CraftTickActivity.Construction ? 3m : 0m, next.BuilderWork);
        // Workshop work has no realized output field: confirmed intermediate/zero-yield turns still count.
        Assert.Equal(0m, a.WorkTick(17, CivilianTier.Apprentice, 0, CraftTickActivity.Workshop, true).IndustryIncome);
    }

    // R-CRAFT-002: synthetic 12+12+12 participation sequence, no second persisted XP store.
    [Fact]
    public void MixedWorkSourcesCountOneTickPerExplicitReceipt()
    {
        var a = Arithmetic(); var xp = 0; var tier = CivilianTier.Apprentice;
        foreach (var activity in new[] { CraftTickActivity.Industry, CraftTickActivity.Workshop, CraftTickActivity.Construction })
            for (var i = 0; i < 12; i++)
            {
                var r = a.WorkTick(17, tier, xp, activity, true); xp = r.WorkXp; tier = r.TierAfter;
            }
        Assert.Equal(36, xp); Assert.Equal(CivilianTier.Expert, tier);
    }

    // R-CRAFT-001/002: paused work and all nonwork activities award no output or XP.
    [Theory]
    [InlineData("Idle")] [InlineData("Travel")]
    [InlineData("Course")] [InlineData("Industry")]
    [InlineData("Workshop")] [InlineData("Construction")]
    public void NoActualWorkMeansNoXpOrOutput(string activityName)
    {
        var activity = Enum.Parse<CraftTickActivity>(activityName);
        var r = Arithmetic().WorkTick(17, CivilianTier.Apprentice, 35, activity, false);
        Assert.Equal(35, r.WorkXp); Assert.Equal(CivilianTier.Apprentice, r.TierAfter);
        Assert.Equal(0m, r.IndustryIncome); Assert.Equal(0m, r.BuilderWork);
        if (activity == CraftTickActivity.Idle || activity == CraftTickActivity.Travel || activity == CraftTickActivity.Course)
            Assert.Throws<ArgumentException>(() => Arithmetic().WorkTick(17, CivilianTier.Apprentice, 35, activity, true));
    }

    // R-CRAFT-001/002: malformed catalogue terms remain unavailable; no fallback/default or formula evaluation.
    [Theory]
    [InlineData("craft.course_cost")] [InlineData("craft.course_turns")]
    [InlineData("craft.course_income")] [InlineData("craft.expert_work_turns")]
    [InlineData("industry.income")] [InlineData("builder.work_rate")]
    public void ReaderRejectsMissingDeferredAndWrongShape(string key)
    {
        foreach (var fault in new[] { "missing", "deferred", "default", "expression", "arity", "parts", "negative" })
        {
            var c = Catalogue(); var value = c.Values[key];
            switch (fault)
            {
                case "missing": c.Values.Remove(key); break;
                case "deferred": value.Basis = "Deferred"; value.Values.Clear(); value.Expression = "unresolved"; break;
                case "default": value.Basis = "Default"; break;
                case "expression": value.Expression = "unresolved"; break;
                case "arity": value.Values.Add(9m); break; // test-only malformed synthetic value
                case "parts": value.Parts["synthetic"] = new Gdd03DesignValue(); break;
                case "negative": value.Values[0] = -1m; break; // test-only invalid value
            }
            Assert.Throws<ContentException>(() => Gdd03CraftArithmetic.Read(c));
        }
    }

    [Theory]
    [InlineData("craft.course_turns")] [InlineData("craft.expert_work_turns")]
    public void ReaderRejectsZeroFractionalAndOutOfRangeTicks(string key)
    {
        foreach (var invalid in new[] { 0m, 1.5m, (decimal)int.MaxValue + 1m })
        {
            var c = Catalogue(); c.Values[key].Values[0] = invalid;
            Assert.Throws<ContentException>(() => Gdd03CraftArithmetic.Read(c));
        }
    }

    [Fact]
    public void ReaderSnapshotsValuesAndRequiresExactCatalogueIdentity()
    {
        var c = Catalogue(); var a = Gdd03CraftArithmetic.Read(c);
        c.Values["craft.course_income"].Values[0] = 99m; // test-only mutation after read
        Assert.Equal(1m, a.CourseTick(17, 0, 0, true).CourseIncome);
        foreach (var identity in new[] { "legacy", "gdd-0.1", "future" })
        {
            c.Ruleset = identity;
            Assert.Throws<ContentException>(() => Gdd03CraftArithmetic.Read(c));
        }
    }

    [Fact]
    public void InvalidExplicitInputsAndXpOverflowRefuse()
    {
        var a = Arithmetic();
        Assert.Throws<ArgumentOutOfRangeException>(() => a.CourseTick(-1, 0, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.CourseTick(17, -1, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.CourseTick(17, 0, -1, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.WorkTick(-1, CivilianTier.Apprentice, 0, CraftTickActivity.Industry, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.WorkTick(17, CivilianTier.Apprentice, -1, CraftTickActivity.Industry, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.WorkTick(17, CivilianTier.Trained, 0, CraftTickActivity.Industry, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.WorkTick(17, CivilianTier.Apprentice, 0, (CraftTickActivity)99, true));
        Assert.Throws<OverflowException>(() => a.WorkTick(17, CivilianTier.Expert, int.MaxValue, CraftTickActivity.Industry, true));
    }

    // R-CRAFT-002: a shared builder snapshot needs only its key and cannot expose a mutable backing array.
    [Fact] public void BuilderRateAccessorIsNarrowFrozenAndReadOnly()
    {
        var catalogue = Catalogue(); var row = catalogue.Values["builder.work_rate"];
        catalogue.Values.Clear(); catalogue.Values.Add("builder.work_rate", row);
        var rates = Gdd03CraftArithmetic.ReadBuilderRates(catalogue);
        Assert.Equal(new[] { 1m, 1.5m, 3m }, rates);
        row.Values[0] = 99m; // synthetic source mutation after snapshot
        Assert.Equal(1m, rates[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<decimal>)rates)[0] = 9m);
        Assert.Throws<ContentException>(() => Gdd03CraftArithmetic.Read(catalogue));
    }
}
