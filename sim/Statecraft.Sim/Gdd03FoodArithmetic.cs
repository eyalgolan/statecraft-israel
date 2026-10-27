using System;
using System.Numerics;

namespace Statecraft.Sim
{
    /// <summary>Exact nonnegative fraction. No floating point or number-to-text conversion.</summary>
    internal sealed class Gdd03FoodFraction
    {
        internal BigInteger Numerator { get; }
        internal BigInteger Denominator { get; }

        internal Gdd03FoodFraction(BigInteger numerator, BigInteger denominator)
        {
            if (numerator < 0) throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0) throw new ArgumentOutOfRangeException(nameof(denominator));
            var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
            Numerator = numerator / divisor;
            Denominator = denominator / divisor;
        }
    }

    /// <summary>Arithmetic only; losses are positive magnitudes, not applied indicator mutations.</summary>
    internal sealed class Gdd03FoodResult
    {
        internal BigInteger Need { get; }
        internal long Consumed { get; }
        internal long Remaining { get; }
        internal Gdd03FoodFraction OwnConsumed { get; }
        internal Gdd03FoodFraction ProvisionConsumed { get; }
        internal Gdd03FoodFraction UnmetShare { get; }
        internal Gdd03FoodFraction CohesionLossPoints { get; }
        internal Gdd03FoodFraction CohesionLossStored { get; }
        internal Gdd03FoodFraction NextOwnStrengthFactor { get; }

        internal Gdd03FoodResult(BigInteger need, long consumed, long remaining,
            Gdd03FoodFraction ownConsumed, Gdd03FoodFraction provisionConsumed,
            Gdd03FoodFraction unmetShare, Gdd03FoodFraction cohesionLossPoints,
            Gdd03FoodFraction cohesionLossStored, Gdd03FoodFraction nextOwnStrengthFactor)
        {
            Need = need;
            Consumed = consumed;
            Remaining = remaining;
            OwnConsumed = ownConsumed;
            ProvisionConsumed = provisionConsumed;
            UnmetShare = unmetShare;
            CohesionLossPoints = cohesionLossPoints;
            CohesionLossStored = cohesionLossStored;
            NextOwnStrengthFactor = nextOwnStrengthFactor;
        }
    }

    /// <summary>
    /// #1704/Q5 synthetic-only foundation. No shipping caller, Content, state, ledger or resolver access.
    /// provisionDemand is already resolved Food demand, NOT a captive count or a population selector.
    /// available is already resolved stock; this function performs no production, purchase or grant.
    /// Rules: 605277dfa511dc512562ececeb273b8393b21d35 docs/gdd/cutover-resource-layer.md:29-35.
    /// </summary>
    internal static class Gdd03FoodArithmetic
    {
        // Balance scalars are explicit plain inputs, with no defaults or runtime catalogue hookup.
        // Synthetic tests supply pinned docs/gdd/balance.md:52,108,109,17,18 values.
        // R-FOOD-001, R-FOOD-002, R-FOOD-003; arithmetic draft only, not a replacement for the shipping rule paths.
        internal static Gdd03FoodResult Settle(long available, long ownUnits, long provisionDemand,
            long ownRation, long cohesionMaximum, long combatSlope,
            long indicatorDisplayMaximum, long indicatorStorageMaximum)
        {
            if (available < 0) throw new ArgumentOutOfRangeException(nameof(available));
            if (ownUnits < 0) throw new ArgumentOutOfRangeException(nameof(ownUnits));
            if (provisionDemand < 0) throw new ArgumentOutOfRangeException(nameof(provisionDemand));

            if (ownRation <= 0) throw new ArgumentOutOfRangeException(nameof(ownRation));
            if (cohesionMaximum < 0) throw new ArgumentOutOfRangeException(nameof(cohesionMaximum));
            if (combatSlope < 0) throw new ArgumentOutOfRangeException(nameof(combatSlope));
            if (indicatorDisplayMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(indicatorDisplayMaximum));
            if (indicatorStorageMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(indicatorStorageMaximum));

            // Widen before multiplication/addition; even two long.MaxValue demands remain exact.
            BigInteger ownDemand = new BigInteger(ownUnits) * ownRation;
            BigInteger need = ownDemand + provisionDemand;
            // Consumed is bounded by the nonnegative long available, so this conversion is exact.
            long consumed = (long)BigInteger.Min(new BigInteger(available), need);
            BigInteger unmet = need - consumed;
            BigInteger denominator = need.IsZero ? BigInteger.One : need;
            var q = new Gdd03FoodFraction(unmet, denominator);
            var ownShare = new Gdd03FoodFraction(consumed * ownDemand, denominator);
            var provisionShare = new Gdd03FoodFraction(new BigInteger(consumed) * provisionDemand, denominator);
            var points = new Gdd03FoodFraction(cohesionMaximum * q.Numerator, q.Denominator);
            var stored = new Gdd03FoodFraction(points.Numerator * indicatorStorageMaximum,
                points.Denominator * indicatorDisplayMaximum);
            var factor = new Gdd03FoodFraction(
                BigInteger.Max(BigInteger.Zero, q.Denominator - combatSlope * q.Numerator), q.Denominator);

            // No old factor is accepted: each result replaces it. No previous loss is refunded.
            return new Gdd03FoodResult(need, consumed, available - consumed, ownShare, provisionShare,
                q, points, stored, factor);
        }
    }
}
