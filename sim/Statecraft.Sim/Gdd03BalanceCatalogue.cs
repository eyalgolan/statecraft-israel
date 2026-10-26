using System;
using System.Collections.Generic;
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
}
