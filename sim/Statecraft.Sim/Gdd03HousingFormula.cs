using System;

namespace Statecraft.Sim
{
    /// <summary>Unwired arithmetic for #1703 synthetic fixtures. No state, resident selection,
    /// initial condition, snapshot timing, Money settlement or indicator commit.</summary>
    internal sealed class Gdd03HousingFormula
    {
        readonly decimal conditionMax, moneyThreshold, moneySlope, cohesionThreshold;
        readonly decimal maxCohesionLoss, overflowPp, unpaidPp, repairPp;
        internal string OverflowBasis { get; }

        Gdd03HousingFormula(Gdd03BalanceCatalogue c)
        {
            // content/gdd03-balance.yaml:226-253; GDD balance.md:100-105 at 605277df.
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
            // content/gdd03-balance.yaml:683-687. No repair cost/work/availability policy here.
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
            if (input.SurchargeResidents < 0 || input.PreArrivalResidents < 0
                || input.CohesionResidents < 0 || input.OwnPopulation < 0 || input.Capacity < 0m
                || input.UpkeepDue < 0m || input.UpkeepPaid < 0m || input.UpkeepPaid > input.UpkeepDue)
                throw new ArgumentOutOfRangeException(nameof(input), "counts/capacity/upkeep must be nonnegative; paid cannot exceed due");
            ValidateCondition(input.ConditionBefore);
            // GDD cities.md:29,33,35; loop-switch.md:29,33 at 605277df.
            // Counts are independently supplied; this helper makes no timing/status decision.
            var surcharge = input.SurchargeResidents * Math.Max(0m, moneyThreshold - input.ConditionBefore) * moneySlope;
            var overflow = input.PreArrivalResidents == 0 ? 0m :
                (overflowPp / 100m) * Math.Max(0m, input.PreArrivalResidents - input.Capacity) / input.PreArrivalResidents;
            var unpaid = input.UpkeepDue == 0m ? 0m :
                (unpaidPp / 100m) * ((input.UpkeepDue - input.UpkeepPaid) / input.UpkeepDue);
            var after = Clamp(input.ConditionBefore - overflow - unpaid);
            // GDD cities.md:31,33. Contribution in displayed points, not indicator storage.
            var cohesion = input.OwnPopulation == 0 ? 0m : maxCohesionLoss
                * ((decimal)input.CohesionResidents / input.OwnPopulation)
                * Math.Max(0m, (cohesionThreshold - after) / cohesionThreshold);
            return new Gdd03HousingResult(surcharge, overflow, unpaid, after, cohesion);
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
        internal int PreArrivalResidents { get; }
        internal int CohesionResidents { get; }
        internal int OwnPopulation { get; }
        internal decimal Capacity { get; }
        internal decimal ConditionBefore { get; }
        internal decimal UpkeepDue { get; }
        internal decimal UpkeepPaid { get; }

        internal Gdd03HousingInput(int surchargeResidents, int preArrivalResidents, int cohesionResidents,
            int ownPopulation, decimal capacity, decimal conditionBefore, decimal upkeepDue, decimal upkeepPaid)
        {
            SurchargeResidents = surchargeResidents; PreArrivalResidents = preArrivalResidents;
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
