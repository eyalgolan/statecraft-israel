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
