using System;

namespace Statecraft.Sim
{
    /// <summary>#2465: the Historical starting farmer split, a quarter novice, half trained and a quarter veteran
    /// (R-POP-004, docs/GDD.md "Historical starting farmer tiers"). Pure integer arithmetic on a count: no city,
    /// population id, census record or field is chosen here, and nothing in the game calls it yet.</summary>
    internal static class Gdd03FarmerTierSplit
    {
        // Largest remainder over quarters (shares 1/4, 2/4, 1/4). Ties go to trained (#1717 rounding ruling). The 2 mod 4
        // case is the owner's ruling of 6 November 2026 on #2465, "Trained: 1/4/1 (Recommended)": the leftover unit is
        // tied between novice and veteran with trained at remainder 0, and it goes to trained, so 6 farmers are 1 / 4 / 1.
        internal static (int Novice, int Trained, int Veteran) Split(int farmers)
        {
            if (farmers < 0) throw new ArgumentOutOfRangeException(nameof(farmers), "Requires a nonnegative count.");
            // Quarters as longs: farmers * 2 would overflow an int at the top of the range.
            long n = farmers;
            long novice = n / 4, trained = n * 2 / 4, veteran = n / 4;
            long leftover = n - novice - trained - veteran;
            long novRem = n % 4, traRem = n * 2 % 4, vetRem = n % 4;
            // Hand out the leftover units one at a time to the largest remaining remainder; trained wins every tie.
            // A single leftover that novice and veteran tie for (2 mod 4, trained at remainder 0) cannot be split between
            // them, so the owner's ruling gives it to trained.
            while (leftover > 0)
            {
                var oneUnitTie = leftover == 1 && novRem == vetRem && novRem > traRem;
                if (oneUnitTie || (traRem >= novRem && traRem >= vetRem)) { trained++; traRem = -1; }
                else if (novRem >= vetRem) { novice++; novRem = -1; }
                else { veteran++; vetRem = -1; }
                leftover--;
            }
            return ((int)novice, (int)trained, (int)veteran);
        }
    }
}
