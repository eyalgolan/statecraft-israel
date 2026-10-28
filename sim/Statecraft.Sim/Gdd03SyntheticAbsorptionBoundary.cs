using System;
using System.Numerics;

namespace Statecraft.Sim
{
    /// <summary>Inactive R-OBJ-004 boundary contract for explicit synthetic settled facts only.
    /// No ledger, region, assignment, snapshot, objective award or turn-path integration.
    /// See #1714 and docs/gdd/objective-card.md:19 at 605277dfa511dc512562ececeb273b8393b21d35.</summary>
    public static class Gdd03SyntheticAbsorptionBoundary
    {
        /// <summary>Checks already-settled counts; this method does not produce or classify them.
        /// The caller supplies represented own units, canonical refugee waiting, an agreed ordinary-housing
        /// overflow/demand pair, and the shared employment producer's employed/all-free-civilian pair.
        /// Regional aggregation and monthly-versus-end-state sampling are deliberately outside this contract.
        /// Empty valid pairs satisfy their subcondition. Invalid inputs throw rather than defaulting.
        /// Current catalogue quantities must be single Agreed whole numbers; later fractional design values
        /// require a reviewed contract extension. Calling this does not record completion or choose its date.</summary>
        public static bool HoldsForSyntheticSettledFacts(
            Gdd03BalanceCatalogue catalogue,
            long representedOwnUnits,
            bool hasRefugeeAwaitingLegalBed,
            long ordinaryOverflow,
            long ordinaryDemand,
            long employedFreeCivilians,
            long allFreeCivilians)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset)
                throw new ArgumentException("Synthetic absorption requires the gdd-0.3 catalogue.", nameof(catalogue));

            // content/gdd03-balance.yaml:46-49,512-523 at base 3824eb6f.
            // Read all required entries before evaluating: Deferred/malformed must never act as false or zero.
            var peoplePerUnit = Content.RequirePopulationInteger(catalogue, "population.people_per_unit");
            var populationTarget = RequireAgreedWholeNumber(catalogue, "absorption.population_target", false);
            var maxOverflow = RequireAgreedWholeNumber(catalogue, "absorption.max_overflow", true);
            var minEmployment = RequireAgreedWholeNumber(catalogue, "absorption.min_employment", true);

            if (representedOwnUnits < 0) throw new ArgumentOutOfRangeException(nameof(representedOwnUnits));
            RequireSubset(ordinaryOverflow, ordinaryDemand, nameof(ordinaryOverflow), nameof(ordinaryDemand));
            RequireSubset(employedFreeCivilians, allFreeCivilians, nameof(employedFreeCivilians), nameof(allFreeCivilians));

            // Exact unbounded integer products; 100 is the definition of percent, not a balance default.
            // No rounded shares, binary floating point, text formatting, RNG or state mutation.
            return new BigInteger(representedOwnUnits) * peoplePerUnit > populationTarget
                && !hasRefugeeAwaitingLegalBed
                && new BigInteger(ordinaryOverflow) * 100 <= new BigInteger(ordinaryDemand) * maxOverflow
                && new BigInteger(employedFreeCivilians) * 100 >= new BigInteger(allFreeCivilians) * minEmployment;
        }

        static BigInteger RequireAgreedWholeNumber(Gdd03BalanceCatalogue catalogue, string key, bool percent)
        {
            if (catalogue.Values == null || !catalogue.Values.TryGetValue(key, out var entry) || entry == null
                || entry.Basis != "Agreed" || entry.Values == null || entry.Values.Count != 1
                || !string.IsNullOrEmpty(entry.Expression) || entry.Parts == null || entry.Parts.Count != 0)
                throw new ArgumentException("Required single Agreed catalogue quantity: " + key, nameof(catalogue));
            var value = entry.Values[0];
            if (decimal.Truncate(value) != value || (percent ? value < 0 || value > 100 : value <= 0))
                throw new ArgumentException("Invalid whole-number catalogue quantity: " + key, nameof(catalogue));
            return new BigInteger(value);
        }

        static void RequireSubset(long numerator, long denominator, string numeratorName, string denominatorName)
        {
            if (denominator < 0) throw new ArgumentOutOfRangeException(denominatorName);
            if (numerator < 0 || numerator > denominator) throw new ArgumentOutOfRangeException(numeratorName);
        }
    }
}
