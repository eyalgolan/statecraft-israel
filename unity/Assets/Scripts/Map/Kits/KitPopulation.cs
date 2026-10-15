#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Population of each settlement, which sizes its kit's footprint (owner ruling: towns spread over hexes in
    /// proportion to population). The figures and their sources live in content/map/settlement_population_1945.yaml
    /// (Village Statistics, 1945; owner-verify), read with the other map inputs (MapInputs.PopulationYaml) and set here by
    /// MapScene.Build before the kits are built. Presentation only: the sim never reads the file.</summary>
    public static class KitPopulation
    {
        public const string FileName = "settlement_population_1945.yaml";
        static readonly HashSet<string> Statuses = new HashSet<string> { "owner-verify", "fact-checked", "verified" };
        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");

        public sealed class Row
        {
            public string Id { get; set; } = "";
            public int? Population { get; set; }
            public string ListedAs { get; set; } = "";
            public string Note { get; set; } = "";
            public string Status { get; set; } = "";
            /// <summary>A person's sign-off beside the status (#375, "humans only"): both or neither, a GitHub handle and a day;
            /// Statecraft.Sim.ReviewRecord checks them. A key that is present but blank counts as present.</summary>
            public string? ReviewedBy { get => reviewedBy; set { reviewedBy = value; ReviewedByGiven = true; } }
            [YamlIgnore] public bool ReviewedByGiven { get; private set; }
            string? reviewedBy;
            public string? ReviewedOn { get => reviewedOn; set { reviewedOn = value; ReviewedOnGiven = true; } }
            [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
            string? reviewedOn;
            public List<SourceRef> Sources { get; set; } = new List<SourceRef>();
        }

        sealed class Doc { public List<Row> Settlements { get; set; } = new List<Row>(); }

        static Dictionary<string, int> current = new Dictionary<string, int>();

        /// <summary>Parses the file. Every malformed input throws InvalidDataException naming <paramref name="file"/> (and the
        /// id): YAML errors, unknown keys, an empty or non-snake_case or duplicate id, a negative population, a status other
        /// than owner-verify / fact-checked / verified, a bad sign-off (reviewed_by / reviewed_on), no sources, a source with an empty url or checked. A null population
        /// (a place the source does not list) is kept out of the table.</summary>
        public static Dictionary<string, int> Parse(string yaml, string file = FileName)
        {
            Doc doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking().Build().Deserialize<Doc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            if (doc?.Settlements == null || doc.Settlements.Count == 0) throw new InvalidDataException($"{file}: no settlements");
            var table = new Dictionary<string, int>(); var seen = new HashSet<string>();
            foreach (var r in doc.Settlements)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.Id)) throw new InvalidDataException($"{file}: a row has an empty id");
                string at = $"{file}: '{r.Id}'";
                if (!IdRule.IsMatch(r.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!seen.Add(r.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (r.Population < 0) throw new InvalidDataException($"{at} population {r.Population} is negative");
                if (!Statuses.Contains(r.Status ?? "")) throw new InvalidDataException($"{at} status '{r.Status}' is not owner-verify, fact-checked or verified");
                var signOff = Statecraft.Sim.ReviewRecord.Problem(r.ReviewedByGiven, r.ReviewedBy, r.ReviewedOnGiven, r.ReviewedOn);
                if (signOff != null) throw new InvalidDataException($"{at} {signOff.Value.field}: {signOff.Value.message}");
                if (r.Sources == null || r.Sources.Count == 0) throw new InvalidDataException($"{at} sources is empty");
                for (int i = 0; i < r.Sources.Count; i++)
                    if (string.IsNullOrWhiteSpace(r.Sources[i]?.Url) || string.IsNullOrWhiteSpace(r.Sources[i].Checked))
                        throw new InvalidDataException($"{at} sources[{i}] needs a url and a checked date");
                if (r.Population.HasValue) table[r.Id] = r.Population.Value;
            }
            return table;
        }

        /// <summary>The table the next kit build reads (MapScene.Build sets it from the map inputs; empty: every place takes
        /// the smallest footprint).</summary>
        public static void Use(Dictionary<string, int> table) => current = table ?? new Dictionary<string, int>();

        /// <summary>The 1945 population, or 0 when the source has none (the kit then takes the smallest footprint).</summary>
        public static int Of(string id) => current.TryGetValue(id, out var n) ? n : 0;
    }
}
