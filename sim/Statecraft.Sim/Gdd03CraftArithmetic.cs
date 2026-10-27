using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    // Fixture-only input labels; not assignments, persisted receipts or an eligibility decision.
    internal enum CraftTickActivity { Idle, Travel, Course, Industry, Workshop, Construction }

    /// <summary>Detached #1698 arithmetic on explicit synthetic inputs. No GameState/ledger mutation,
    /// reservation, payment, receipt deduplication or live promotion. Future owners supply those contracts.</summary>
    internal sealed class Gdd03CraftArithmetic
    {
        internal decimal CourseCost { get; }
        internal int CourseTurns { get; }
        internal decimal CourseIncome { get; }
        internal int ExpertWorkTurns { get; }
        readonly decimal apprenticeIncome, expertIncome, apprenticeWork, expertWork;

        Gdd03CraftArithmetic(decimal cost, int turns, decimal income, int expertTurns,
            decimal apprenticeIncome, decimal expertIncome, decimal apprenticeWork, decimal expertWork)
        {
            CourseCost = cost; CourseTurns = turns; CourseIncome = income; ExpertWorkTurns = expertTurns;
            this.apprenticeIncome = apprenticeIncome; this.expertIncome = expertIncome;
            this.apprenticeWork = apprenticeWork; this.expertWork = expertWork;
        }

        // R-CRAFT-001/002: exact agreed catalogue quantities only. This does not activate any ruleset.
        internal static Gdd03CraftArithmetic Read(Gdd03BalanceCatalogue catalogue)
        {
            RequireCatalogue(catalogue);
            var cost = Terms(catalogue, "craft.course_cost", 1)[0];
            var turns = Whole(Terms(catalogue, "craft.course_turns", 1)[0], "craft.course_turns");
            var income = Terms(catalogue, "craft.course_income", 1)[0];
            var expertTurns = Whole(Terms(catalogue, "craft.expert_work_turns", 1)[0], "craft.expert_work_turns");
            var industry = Gdd03IndustryRates.Read(catalogue);
            var builder = ReadBuilderRates(catalogue);
            return new Gdd03CraftArithmetic(cost, turns, income, expertTurns,
                industry.For(CivilianTier.Apprentice), industry.For(CivilianTier.Expert), builder[1], builder[2]);
        }

        // Frozen ordinary/apprentice/expert vector for catalogue-only consumers; no course terms required.
        // Consumers may layer stricter project rules on these validated nonnegative rates.
        internal static IReadOnlyList<decimal> ReadBuilderRates(Gdd03BalanceCatalogue catalogue)
        {
            RequireCatalogue(catalogue);
            return Array.AsReadOnly(Terms(catalogue, "builder.work_rate", 3));
        }

        static void RequireCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "ruleset", "craft arithmetic requires the gdd-0.3 catalogue");
        }

        static decimal[] Terms(Gdd03BalanceCatalogue catalogue, string key, int count)
        {
            if (catalogue.Values == null || !catalogue.Values.TryGetValue(key, out var v) || v == null
                || v.Basis != "Agreed" || v.Values == null || v.Values.Count != count
                || !string.IsNullOrEmpty(v.Expression) || v.Parts == null || v.Parts.Count != 0)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, "requires exact agreed scalar or tier values; no Deferred/default substitution");
            var result = v.Values.ToArray();
            foreach (var value in result)
                if (value < 0) throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, "requires nonnegative quantities");
            return result;
        }

        static int Whole(decimal value, string key)
        {
            if (value <= 0 || value > int.MaxValue || decimal.Truncate(value) != value)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, "requires a positive whole Int32 tick count");
            return (int)value;
        }

        // Caller confirms one eligible course turn. Completion is a quote, not a profession/assignment mutation.
        // R-CRAFT-001: final course turn still has only course income, never Industry income or work XP.
        internal CraftCourseTick CourseTick(int unitId, int progressTicks, int workXp, bool eligibleTurn)
        {
            Nonnegative(unitId, nameof(unitId)); Nonnegative(workXp, nameof(workXp));
            if (progressTicks < 0 || progressTicks >= CourseTurns) throw new ArgumentOutOfRangeException(nameof(progressTicks));
            var progress = eligibleTurn ? checked(progressTicks + 1) : progressTicks;
            return new CraftCourseTick(unitId, progress, workXp, eligibleTurn ? CourseIncome : 0m, progress == CourseTurns);
        }

        // Exactly ONE caller-confirmed participation receipt. Common owner must deduplicate by unit/turn
        // before invoking and apply a result at most once. Realized output is deliberately not an XP input.
        // R-CRAFT-002: tier before this tick quotes output; threshold crossing affects only TierAfter.
        internal CraftWorkTick WorkTick(int unitId, CivilianTier tier, int workXp, CraftTickActivity activity, bool actualWork)
        {
            Nonnegative(unitId, nameof(unitId)); Nonnegative(workXp, nameof(workXp));
            if (tier != CivilianTier.Apprentice && tier != CivilianTier.Expert) throw new ArgumentOutOfRangeException(nameof(tier));
            if (!Enum.IsDefined(typeof(CraftTickActivity), activity)) throw new ArgumentOutOfRangeException(nameof(activity));
            var working = activity == CraftTickActivity.Industry || activity == CraftTickActivity.Workshop || activity == CraftTickActivity.Construction;
            if (actualWork && !working) throw new ArgumentException("A nonwork activity cannot carry an actual-work receipt.", nameof(actualWork));
            var xp = actualWork ? checked(workXp + 1) : workXp;
            var after = actualWork && tier == CivilianTier.Apprentice && xp >= ExpertWorkTurns ? CivilianTier.Expert : tier;
            var income = actualWork && activity == CraftTickActivity.Industry ? (tier == CivilianTier.Apprentice ? apprenticeIncome : expertIncome) : 0m;
            var work = actualWork && activity == CraftTickActivity.Construction ? (tier == CivilianTier.Apprentice ? apprenticeWork : expertWork) : 0m;
            return new CraftWorkTick(unitId, tier, after, xp, income, work);
        }

        static void Nonnegative(int value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name);
        }
    }

    // Ephemeral arithmetic results, never a second population/course/XP ledger or save format.
    internal sealed class CraftCourseTick
    {
        internal int UnitId { get; }
        internal int ProgressTicks { get; }
        internal int WorkXp { get; }
        internal decimal CourseIncome { get; }
        internal decimal IndustryIncome => 0m;
        internal bool Completes { get; }
        internal CraftCourseTick(int unitId, int progress, int xp, decimal income, bool completes)
        { UnitId = unitId; ProgressTicks = progress; WorkXp = xp; CourseIncome = income; Completes = completes; }
    }

    internal sealed class CraftWorkTick
    {
        internal int UnitId { get; }
        internal CivilianTier OutputTier { get; }
        internal CivilianTier TierAfter { get; }
        internal int WorkXp { get; }
        internal decimal IndustryIncome { get; }
        internal decimal BuilderWork { get; }
        internal CraftWorkTick(int unitId, CivilianTier before, CivilianTier after, int xp, decimal income, decimal work)
        { UnitId = unitId; OutputTier = before; TierAfter = after; WorkXp = xp; IndustryIncome = income; BuilderWork = work; }
    }
}
