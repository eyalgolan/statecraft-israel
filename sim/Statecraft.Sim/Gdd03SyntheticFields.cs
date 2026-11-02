using System;

namespace Statecraft.Sim
{
    /// <summary>Detached typed catalogue snapshot for synthetic field evaluation only.
    /// Construction does not enable farming in any ruleset or validate unrelated catalogue rows.</summary>
    public sealed class Gdd03FieldRates
    {
        readonly int noviceFood;
        readonly int trainedFood;
        readonly int veteranFood;
        public int SlotsPerField { get; }

        Gdd03FieldRates(int novice, int trained, int veteran, int slots)
        {
            noviceFood = novice; trainedFood = trained; veteranFood = veteran; SlotsPerField = slots;
        }

        // R-FIELD-002: content/gdd03-balance.yaml:142-145,198-201.
        public static Gdd03FieldRates FromCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "ruleset", "requires exact gdd-0.3 catalogue identity");
            var food = ReadIntegers(catalogue, "farmer.output", 3);
            var slots = ReadIntegers(catalogue, "field.slots", 1);
            // This single-field contract represents the catalogued one farmer per field, not multi-worker scaling.
            if (slots[0] != 1)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.field.slots", "requires one farmer per field");
            return new Gdd03FieldRates(food[0], food[1], food[2], slots[0]);
        }

        static int[] ReadIntegers(Gdd03BalanceCatalogue catalogue, string key, int count)
        {
            var at = "values." + key;
            if (catalogue.Values == null || !catalogue.Values.TryGetValue(key, out var row) || row == null
                || (row.Basis != "Agreed" && row.Basis != "Default") || string.IsNullOrWhiteSpace(row.Note)
                || row.Values == null || row.Values.Count != count || !string.IsNullOrEmpty(row.Expression)
                || row.Parts == null || row.Parts.Count != 0)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "requires settled exact integer values with provenance; no Deferred or fallback values");
            var values = new int[count];
            for (int i = 0; i < count; i++)
            {
                var value = row.Values[i];
                if (value < 0 || value > int.MaxValue || decimal.Truncate(value) != value)
                    throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "requires nonnegative Food/slot integers within Int32 range");
                values[i] = checked((int)value);
            }
            return values;
        }

        // The catalogue tier, a multiple of field_food_per_turn (#2112); equal to Food only while that base is 1.
        internal int FoodFor(CivilianTier tier) => tier switch {
            CivilianTier.Novice => noviceFood,
            CivilianTier.Trained => trainedFood,
            CivilianTier.Veteran => veteranFood,
            _ => throw new ArgumentException("Requires a frozen farmer tier.", nameof(tier))
        };

        /// <summary>#2112 (owner, 1 November 2026, "Main's base x 0.3's tiers (Recommended)"): the catalogue's farmer tier
        /// is a multiple of main's field_food_per_turn (R-FIELD-002), so a staffed field yields <see cref="FoodFor"/> times
        /// that base. A quote only; no Food moves.</summary>
        internal int FoodPerTurn(CivilianTier tier, MvpRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (rules.FieldFoodPerTurn == null)
                throw new ContentException("rules.yaml", "mvp.field_food_per_turn", "required for the gdd-0.3 farmer tiers");
            return checked(FoodFor(tier) * rules.FieldFoodPerTurn.Value);
        }
    }

    /// <summary>Explicit fixture facts, not assignment authority or inferred terrain/control.
    /// Eligible is supplied by the fixture; no reception, tools, profession or exclusivity policy is decided here.
    /// A null frozen tier means unstaffed. The caller supplies the current tier without promotion.</summary>
    public sealed class Gdd03SyntheticFieldInput
    {
        public string SyntheticFieldId { get; }
        public bool Prepared { get; }
        public bool Workable { get; }
        public bool Friendly { get; }
        public bool Eligible { get; }
        public CivilianTier? FrozenTier { get; }

        public Gdd03SyntheticFieldInput(string syntheticFieldId, bool prepared, bool workable,
            bool friendly, bool eligible, CivilianTier? frozenTier)
        {
            if (string.IsNullOrWhiteSpace(syntheticFieldId))
                throw new ArgumentException("An explicit synthetic field id is required.", nameof(syntheticFieldId));
            if (frozenTier.HasValue && frozenTier != CivilianTier.Novice
                && frozenTier != CivilianTier.Trained && frozenTier != CivilianTier.Veteran)
                throw new ArgumentException("Use a farmer tier or null for an unstaffed fixture.", nameof(frozenTier));
            SyntheticFieldId = syntheticFieldId; Prepared = prepared; Workable = workable;
            Friendly = friendly; Eligible = eligible; FrozenTier = frozenTier;
        }
    }

    /// <summary>Arithmetic result only. HasSyntheticWork is not a work receipt, XP credit or deduplication key.</summary>
    public sealed class Gdd03SyntheticFieldOutput
    {
        public int Food { get; }
        public int Money => 0; // GDD 605277df docs/gdd/cities.md:13: farming creates no Money.
        public bool HasSyntheticWork { get; }
        internal Gdd03SyntheticFieldOutput(int food, bool hasSyntheticWork)
        {
            Food = food; HasSyntheticWork = hasSyntheticWork;
        }
    }

    /// <summary>Pure single-field fixture evaluator. No state mutation, population table, batch occupancy,
    /// control transition, work receipt or Food settlement. No shipping system calls this API.</summary>
    public static class Gdd03SyntheticFieldEvaluator
    {
        // R-FIELD-002: synthetic arithmetic subset of GDD 605277df docs/gdd/cities.md:13.
        public static Gdd03SyntheticFieldOutput Evaluate(Gdd03FieldRates rates, Gdd03SyntheticFieldInput input)
        {
            if (rates == null) throw new ArgumentNullException(nameof(rates));
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.Prepared || !input.Workable || !input.Friendly || !input.Eligible || !input.FrozenTier.HasValue)
                return new Gdd03SyntheticFieldOutput(0, false);
            return new Gdd03SyntheticFieldOutput(rates.FoodFor(input.FrozenTier.Value), true);
        }
    }
}
