using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Design data for the future gdd-0.3 loop. No legacy/gdd-0.1 system reads these values.
    /// Composite source rows retain separately labelled parts (for example agreed cost, default upkeep).</summary>
    public sealed class Gdd03BalanceCatalogue
    {
        // R-LOOP-005: every numeric row is non-negative unless it is one of these signed values. The reasons are part of
        // the exception table so a new exception cannot be added without documenting why it is signed.
        internal static readonly IReadOnlyDictionary<string, string> NegativeExceptions = new Dictionary<string, string>
        {
            ["values.maabara.parts.condition_pp"] = "A signed completion delta; the row note says condition -10 pp.",
            ["values.permanent_housing.parts.condition_pp"] = "A signed completion delta; the row note says condition +10 pp.",
            ["values.housing_repair.parts.condition_pp"] = "A signed completion delta; the row note says condition +10 pp.",
        };

        // Counts, whole turns, movement points, integer draw bands and whole-percent bonuses (no upper bound).
        internal static readonly IReadOnlyCollection<string> WholeNumberKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "values.campaign.play_minutes", "values.event.max_gap_peace", "values.event.max_gap_war",
            "values.population.people_per_unit", "values.start.population", "values.start.spare_arms",
            "values.start.farmers", "values.start.prepared_fields", "values.start.trained_pioneers",
            "values.start.apprentices", "values.start.experts", "values.start.pioneer_teachers",
            "values.start.military_camps", "values.start.reception_camps", "values.start.training_centres",
            "values.start.industry_slots", "values.start.permanent_housing_projects", "values.start.housing_capacity",
            "values.profession.course_turns", "values.profession.trainees_per_teacher", "values.profession.work_to_veteran",
            "values.craft.course_turns", "values.craft.expert_work_turns", "values.builder.slots_per_project",
            "values.construction.starts_per_turn", "values.field.slots", "values.field.nominal_turns",
            "values.reception.capacity", "values.reception.turns", "values.reception.basic_credit",
            "values.reception.queue_cohesion_loss.parts.cadence_turns", "values.import.delivery_turns",
            "values.immigration.ship_interval", "values.immigration.units_per_ship", "values.workshop.staffed_turns",
            "values.military.camp_capacity", "values.military.basic_turns", "values.military.issue_arms",
            "values.military.draft_wait", "values.military.demobilise_wait", "values.military.demobilise_return",
            "values.military.repeat_issue", "values.military.combat_arms", "values.military.combat_xp_gain",
            "values.military.combat_xp_full", "values.military.fort_bonus", "values.military.fortify_turns",
            "values.military.fortify_bonus", "values.military.stack_limit", "values.military.move_infantry",
            "values.military.move_armoured", "values.military.move_road", "values.military.move_rail",
            "values.military.armistice_quiet_turns", "values.occupation.garrison", "values.occupation.turns",
            "values.combat.draw_count", "values.combat.draw_count.parts.draw_band",
            "values.combat.draw_count.parts.below_0_5", "values.combat.draw_count.parts.from_0_5_below_1",
            "values.combat.draw_count.parts.from_1_below_2", "values.combat.draw_count.parts.at_least_2",
            "values.combat.retreat_hexes", "values.combat.artillery_range", "values.combat.loss_cohorts",
            "values.absorption.population_target", "values.maabara.parts.housing",
            "values.permanent_housing.parts.housing", "values.industry.parts.job_slots",
            "values.arms_workshop.parts.worker_slots", "values.coastal_plain.parts.movement",
            "values.coastal_plain.parts.defence_percent", "values.lowland.parts.movement",
            "values.lowland.parts.defence_percent", "values.valley.parts.movement",
            "values.valley.parts.defence_percent", "values.hills.parts.movement",
            "values.hills.parts.defence_percent", "values.mountains.parts.movement",
            "values.mountains.parts.defence_percent", "values.desert.parts.movement",
            "values.desert.parts.defence_percent",
        };

        internal static readonly IReadOnlyCollection<string> FractionKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "values.start.housing_condition", "values.housing.condition_max", "values.housing.money_threshold",
            "values.housing.cohesion_threshold", "values.puppet.tax_fraction",
        };

        internal static readonly IReadOnlyCollection<string> PercentKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "values.immigration.mix", "values.absorption.max_overflow", "values.absorption.min_employment",
        };

        public const string FileName = "gdd03-balance.yaml";
        public string Ruleset { get; set; } = "";
        public Dictionary<string, Gdd03DesignValue> Values { get; set; } = new Dictionary<string, Gdd03DesignValue>();

        /// <summary>#1706 slices 0/1: explicit design-data read for synthetic gdd-0.3 fixtures.
        /// No loader, combat, preview, AI or shipping caller selects this table.</summary>
        internal static Gdd03CombatCatalogue ReadCombatCatalogue(Content content) => Gdd03Military.ReadCombatCatalogue(content);

        internal void Validate()
        {
            if (Ruleset != SaveGame.Gdd03Ruleset) throw new ContentException(FileName, "ruleset", "must be gdd-0.3 (catalogue only; not a live-loop switch)");
            // R-LOOP-005, #1687/#1717: A is not a balance default. Even relabelling it Agreed cannot approve a guess.
            // A later owner decision must update this contract and validate the complete 65-unit start partition.
            if (!Values.TryGetValue("start.army", out var army) || army == null || army.Basis != "Deferred"
                || army.Expression != "A" || army.Values == null || army.Parts == null || army.Values.Count != 0 || army.Parts.Count != 0)
                throw new ContentException(FileName, "values.start.army", "must remain Deferred symbol A, with no numeric value, until the owner decides #1717");
            foreach (var entry in Values.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(entry.Key)) throw new ContentException(FileName, "values", "a catalogue key is required");
                if (entry.Value == null) throw new ContentException(FileName, "values." + entry.Key, "entry required");
                entry.Value.Validate("values." + entry.Key);
            }
        }

        internal void ValidateNumbers()
        {
            foreach (var entry in Values.OrderBy(x => x.Key, StringComparer.Ordinal))
                ValidateNumbers(entry.Value, "values." + entry.Key);
        }

        static void ValidateNumbers(Gdd03DesignValue row, string at)
        {
            ValidateNumbers(at, row.Values);
            foreach (var part in row.Parts.OrderBy(x => x.Key, StringComparer.Ordinal))
                ValidateNumbers(part.Value, at + ".parts." + part.Key);
        }

        static void ValidateNumbers(string at, IEnumerable<decimal> values)
        {
            if (NegativeExceptions.ContainsKey(at)) return;
            foreach (var value in values)
            {
                if (WholeNumberKeys.Contains(at) && (value < 0 || value != decimal.Truncate(value)))
                    throw new ContentException(FileName, at, "must be a whole number >= 0");
                if (FractionKeys.Contains(at) && (value < 0 || value > 1))
                    throw new ContentException(FileName, at, "must be between 0 and 1");
                if (PercentKeys.Contains(at) && (value < 0 || value > 100))
                    throw new ContentException(FileName, at, "must be between 0 and 100");
                if (value < 0)
                    throw new ContentException(FileName, at, "must be >= 0");
            }
        }
    }

    public sealed class Gdd03DesignValue
    {
        public string Basis { get; set; } = "";
        // Exact integer/decimal source quantities. No binary floating point, formula evaluation or gameplay defaults.
        public List<decimal> Values { get; set; } = new List<decimal>();
        public string Expression { get; set; } = "";
        public string Note { get; set; } = "";
        public Dictionary<string, Gdd03DesignValue> Parts { get; set; } = new Dictionary<string, Gdd03DesignValue>();

        internal void Validate(string at)
        {
            if (Values == null || Parts == null)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "values must be a list and parts a map");
            if (Basis != "Agreed" && Basis != "Default" && Basis != "Deferred")
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at + ".basis", "must be Agreed, Default or Deferred");
            if (string.IsNullOrWhiteSpace(Note)) throw new ContentException(Gdd03BalanceCatalogue.FileName, at + ".note", "design basis/provenance required");
            if (Values.Count == 0 && string.IsNullOrWhiteSpace(Expression) && Parts.Count == 0)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "requires exact values, a symbolic expression or labelled parts");
            if (Values.Count != 0 && !string.IsNullOrEmpty(Expression))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "use exact values or an expression, not both");
            if (Basis == "Deferred" && (Values.Count != 0 || string.IsNullOrWhiteSpace(Expression) || Parts.Count != 0))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, at, "Deferred quantities require a symbolic expression, never a numeric guess");
            foreach (var part in Parts.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(part.Key) || part.Value == null) throw new ContentException(Gdd03BalanceCatalogue.FileName, at + ".parts." + part.Key, "named part required");
                part.Value.Validate(at + ".parts." + part.Key);
            }
        }
    }

    internal enum Gdd03CombatOutcome { AttackerLoss, BothHold, DefenderRetreats, DefenderLoss }

    /// <summary>Inclusive draw interval; no state transition is implemented here.</summary>
    internal sealed class Gdd03CombatBand
    {
        public int First { get; }
        public int Last { get; }
        public Gdd03CombatOutcome Outcome { get; }
        internal Gdd03CombatBand(int first, int last, Gdd03CombatOutcome outcome)
        { First = first; Last = last; Outcome = outcome; }
    }

    /// <summary>Frozen source metadata: Default is never promoted to Agreed by validation.</summary>
    internal sealed class Gdd03CombatSource
    {
        public string Basis { get; }
        public string Note { get; }
        public IReadOnlyList<decimal> Values { get; }
        internal Gdd03CombatSource(Gdd03DesignValue value)
        { Basis = value.Basis; Note = value.Note; Values = Array.AsReadOnly(value.Values.ToArray()); }
    }

    /// <summary>Validated catalogue snapshot only. Accepts an already supplied exact decimal ratio;
    /// it calculates no strength, consumes no RNG and applies no combat outcome.</summary>
    internal sealed class Gdd03CombatCatalogue
    {
        readonly decimal[] thresholds;
        readonly IReadOnlyList<Gdd03CombatBand>[] rows;
        public int DrawCount { get; }
        public decimal ArmouredFactor { get; }
        public decimal ArtilleryFactor { get; }
        public int ArtilleryRange { get; }
        public IReadOnlyDictionary<string, Gdd03CombatSource> Sources { get; }

        internal Gdd03CombatCatalogue(decimal[] thresholds, IReadOnlyList<Gdd03CombatBand>[] rows,
            int draws, decimal armour, decimal artillery, int range, Dictionary<string, Gdd03CombatSource> sources)
        {
            this.thresholds = thresholds; this.rows = rows; DrawCount = draws;
            ArmouredFactor = armour; ArtilleryFactor = artillery; ArtilleryRange = range;
            Sources = new ReadOnlyDictionary<string, Gdd03CombatSource>(sources);
        }

        // Exact comparison of a caller-supplied ratio, with no display-percent rounding.
        public int Row(decimal ratio)
        {
            if (ratio < 0) throw new ArgumentOutOfRangeException(nameof(ratio));
            for (var i = 0; i < thresholds.Length; i++) if (ratio < thresholds[i]) return i;
            return thresholds.Length;
        }

        public IReadOnlyList<Gdd03CombatBand> Bands(int row)
        {
            if (row < 0 || row >= rows.Length) throw new ArgumentOutOfRangeException(nameof(row));
            return rows[row];
        }

        public Gdd03CombatOutcome Outcome(decimal ratio, int draw)
        {
            if (draw < 0 || draw > 99) throw new ArgumentOutOfRangeException(nameof(draw));
            return rows[Row(ratio)].Single(b => draw >= b.First && draw <= b.Last).Outcome;
        }
    }

}
