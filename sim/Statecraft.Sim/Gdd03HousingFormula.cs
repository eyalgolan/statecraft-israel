using System;

namespace Statecraft.Sim
{
    /// <summary>#1703's housing arithmetic. <see cref="Evaluate"/> is the decimal form for synthetic fixtures;
    /// <see cref="EvaluateTurn"/> is the live gdd-0.3 step in double. Neither holds state, selects residents, sets the
    /// initial condition, settles Money or commits an indicator: Cities.UpdateCondition does.</summary>
    internal sealed class Gdd03HousingFormula
    {
        readonly decimal conditionMax, moneyThreshold, moneySlope, cohesionThreshold;
        readonly decimal maxCohesionLoss, overflowPp, unpaidPp, repairPp;
        internal string OverflowBasis { get; }

        Gdd03HousingFormula(Gdd03BalanceCatalogue c)
        {
            // content/gdd03-balance.yaml `housing.*`; GDD balance.md:100-105 at 605277df.
            conditionMax = Scalar(c, "housing.condition_max", true);
            moneyThreshold = Scalar(c, "housing.money_threshold", true);
            moneySlope = Scalar(c, "housing.money_slope", false);
            cohesionThreshold = Scalar(c, "housing.cohesion_threshold", true);
            maxCohesionLoss = Scalar(c, "housing.max_cohesion_loss", false);
            overflowPp = Scalar(c, "housing.overflow_deterioration", false);
            unpaidPp = Scalar(c, "housing.max_deterioration", false);
            OverflowBasis = c.Values["housing.overflow_deterioration"].Basis;
            if (conditionMax != 1m || moneyThreshold > conditionMax || cohesionThreshold > conditionMax)
                throw Invalid("housing.condition_max", "condition is a fraction in [0,1]; thresholds must lie within it");
            if (!c.Values.TryGetValue("housing_repair", out var repair) || repair == null
                || repair.Basis != "Agreed" || repair.Parts == null || repair.Values == null
                || repair.Values.Count != 0 || !string.IsNullOrEmpty(repair.Expression)
                || !repair.Parts.TryGetValue("condition_pp", out var effect))
                throw Invalid("housing_repair", "requires agreed repair condition part");
            // content/gdd03-balance.yaml `housing_repair.parts.condition_pp`. No repair cost/work/availability policy here.
            repairPp = Number(effect, "housing_repair.parts.condition_pp", false);
        }

        internal static Gdd03HousingFormula FromCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null)
                throw Invalid("housing", "requires explicit gdd-0.3 catalogue");
            return new Gdd03HousingFormula(catalogue);
        }

        static ContentException Invalid(string key, string reason) =>
            new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key, reason);

        static decimal Scalar(Gdd03BalanceCatalogue c, string key, bool positive)
        {
            if (!c.Values.TryGetValue(key, out var row)) throw Invalid(key, "required housing scalar");
            return Number(row, key, positive);
        }

        static decimal Number(Gdd03DesignValue row, string key, bool positive)
        {
            if (row == null || (row.Basis != "Agreed" && row.Basis != "Default")
                || row.Values == null || row.Values.Count != 1 || row.Parts == null
                || row.Parts.Count != 0 || !string.IsNullOrEmpty(row.Expression)
                || string.IsNullOrWhiteSpace(row.Note))
                throw Invalid(key, "requires one labelled Agreed/Default scalar; Deferred is not a number");
            var value = row.Values[0];
            if (positive ? value <= 0m : value < 0m)
                throw Invalid(key, positive ? "must be positive" : "must be nonnegative");
            return value;
        }

        internal Gdd03HousingResult Evaluate(Gdd03HousingInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.SurchargeResidents < 0 || input.Residents < 0
                || input.CohesionResidents < 0 || input.OwnPopulation < 0
                || input.CohesionResidents > input.OwnPopulation || input.Capacity < 0m
                || input.UpkeepDue < 0m || input.UpkeepPaid < 0m || input.UpkeepPaid > input.UpkeepDue)
                throw new ArgumentOutOfRangeException(nameof(input), "counts/capacity/upkeep must be nonnegative; regional residents cannot exceed own population; paid cannot exceed due");
            ValidateCondition(input.ConditionBefore);
            // GDD cities.md:29,33,35 at 605277df. Overflow counts the housed residents after the step's arrivals become
            // civilians (27M-15, owner, 1 November 2026). Counts are independently supplied; this helper selects no one.
            var surcharge = input.SurchargeResidents * Math.Max(0m, moneyThreshold - input.ConditionBefore) * moneySlope;
            var overflow = input.Residents == 0 ? 0m :
                (overflowPp / 100m) * Math.Max(0m, input.Residents - input.Capacity) / input.Residents;
            var unpaid = input.UpkeepDue == 0m ? 0m :
                (unpaidPp / 100m) * ((input.UpkeepDue - input.UpkeepPaid) / input.UpkeepDue);
            var after = Clamp(input.ConditionBefore - overflow - unpaid);
            // GDD cities.md:31,33 at 605277df. Contribution in displayed points, not indicator storage.
            var cohesion = input.OwnPopulation == 0 ? 0m : maxCohesionLoss
                * ((decimal)input.CohesionResidents / input.OwnPopulation)
                * Math.Max(0m, (cohesionThreshold - after) / cohesionThreshold);
            return new Gdd03HousingResult(surcharge, overflow, unpaid, after, cohesion);
        }

        /// <summary>R-HOUSE-005's live step for one city (#1703; Cities.UpdateCondition): Evaluate's surcharge, overflow
        /// and cohesion terms in the same order, with no upkeep (27M-17), in double. A city's condition, the Treasury and the
        /// indicators are doubles, hashed as IEEE bits (docs/determinism.md), so the live step stays in double rather than
        /// round-trip state through decimal; only the catalogue's one- and two-digit scalars are cast, as Workshops casts
        /// its catalogue price.</summary>
        internal (double Surcharge, double ConditionAfter, double Cohesion) EvaluateTurn(int residents, int ownPopulation,
            int capacity, double condition)
        {
            if (residents < 0 || capacity < 0 || residents > ownPopulation)
                throw new ArgumentOutOfRangeException(nameof(residents), "counts must be nonnegative; residents cannot exceed own population");
            if (condition < 0.0 || condition > (double)conditionMax)
                throw new ArgumentOutOfRangeException(nameof(condition), "condition must be within catalogue bounds");
            var surcharge = residents * Math.Max(0.0, (double)moneyThreshold - condition) * (double)moneySlope;
            var overflow = residents == 0 ? 0.0 : ((double)overflowPp / 100.0) * Math.Max(0, residents - capacity) / residents;
            var after = Math.Max(0.0, Math.Min((double)conditionMax, condition - overflow));
            var threshold = (double)cohesionThreshold;
            var cohesion = ownPopulation == 0 ? 0.0 : (double)maxCohesionLoss
                * ((double)residents / ownPopulation) * Math.Max(0.0, (threshold - after) / threshold);
            return (surcharge, after, cohesion);
        }

        // Explicit single project-effect arithmetic; caller owns command/payment/application-once.
        internal decimal ApplyConditionEffect(decimal condition, decimal deltaFraction)
        {
            ValidateCondition(condition);
            // Avoid overflow even for extreme explicit effects before clamping.
            if (deltaFraction >= conditionMax - condition) return conditionMax;
            if (deltaFraction <= -condition) return 0m;
            return condition + deltaFraction;
        }

        internal decimal RepairCondition(decimal condition) => ApplyConditionEffect(condition, repairPp / 100m);
        decimal Clamp(decimal condition) => Math.Max(0m, Math.Min(conditionMax, condition));
        void ValidateCondition(decimal condition)
        {
            if (condition < 0m || condition > conditionMax)
                throw new ArgumentOutOfRangeException(nameof(condition), "explicit condition must be within catalogue bounds");
        }
    }

    /// <summary>Caller-selected boundary counts. No field has a gameplay default.</summary>
    internal sealed class Gdd03HousingInput
    {
        internal int SurchargeResidents { get; }
        /// <summary>The housed residents after the step's arrivals become civilians (R-HOUSE-005, 27M-15).</summary>
        internal int Residents { get; }
        internal int CohesionResidents { get; }
        internal int OwnPopulation { get; }
        internal decimal Capacity { get; }
        internal decimal ConditionBefore { get; }
        internal decimal UpkeepDue { get; }
        internal decimal UpkeepPaid { get; }

        internal Gdd03HousingInput(int surchargeResidents, int residents, int cohesionResidents,
            int ownPopulation, decimal capacity, decimal conditionBefore, decimal upkeepDue, decimal upkeepPaid)
        {
            SurchargeResidents = surchargeResidents; Residents = residents;
            CohesionResidents = cohesionResidents; OwnPopulation = ownPopulation; Capacity = capacity;
            ConditionBefore = conditionBefore; UpkeepDue = upkeepDue; UpkeepPaid = upkeepPaid;
        }
    }

    internal sealed class Gdd03HousingResult
    {
        internal decimal SurchargeMoney { get; }
        internal decimal OverflowConditionLoss { get; }
        internal decimal UnpaidConditionLoss { get; }
        internal decimal TotalConditionLossPp => (OverflowConditionLoss + UnpaidConditionLoss) * 100m;
        internal decimal ConditionAfter { get; }
        internal decimal CohesionLossPoints { get; }

        internal Gdd03HousingResult(decimal surchargeMoney, decimal overflowConditionLoss,
            decimal unpaidConditionLoss, decimal conditionAfter, decimal cohesionLossPoints)
        {
            SurchargeMoney = surchargeMoney; OverflowConditionLoss = overflowConditionLoss;
            UnpaidConditionLoss = unpaidConditionLoss; ConditionAfter = conditionAfter;
            CohesionLossPoints = cohesionLossPoints;
        }
    }
}
