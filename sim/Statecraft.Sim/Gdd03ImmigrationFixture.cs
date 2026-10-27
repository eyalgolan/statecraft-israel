using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    /// <summary>Fixture-only manifest content contract. Nothing loads this from ContentLoader or a turn path.
    /// It declares an ordered template, not a ship occurrence, population ledger or production schedule.</summary>
    public sealed class Gdd03ImmigrationFixture
    {
        public const string DiagnosticFile = "(gdd03-immigration-fixture)";
        public string Id { get; }
        public string FixturePort { get; }
        public int IntervalTurns { get; }
        // The catalogue says average. This is deliberately not an executable fixed passenger count.
        public int AverageUnitsPerShip { get; }
        public IReadOnlyList<Gdd03ImmigrationSlot> Slots { get; }
        Gdd03ImmigrationFixture(ImmigrationFixtureDocument doc, int interval, int average,
            List<Gdd03ImmigrationSlot> slots)
        {
            Id = doc.Id!; FixturePort = doc.FixturePort!; IntervalTurns = interval; AverageUnitsPerShip = average;
            Slots = new ReadOnlyCollection<Gdd03ImmigrationSlot>(slots);
        }

        /// <summary>Parse explicitly supplied synthetic YAML. Port ids only check fixture references;
        /// they provide no geography, friendliness, facility capacity or authority to land passengers.</summary>
        public static Gdd03ImmigrationFixture Parse(string yaml, Gdd03BalanceCatalogue catalogue,
            IEnumerable<string> fixturePorts)
        {
            if (yaml == null) throw Error("(file)", "explicit fixture YAML required");
            if (catalogue == null || catalogue.Ruleset != SaveGame.Gdd03Ruleset)
                throw Error("catalogue", "explicit gdd-0.3 catalogue required");
            if (fixturePorts == null) throw Error("fixture_port", "explicit fixture references required");
            ImmigrationFixtureDocument doc;
            try {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<ImmigrationFixtureDocument>(yaml)
                    ?? throw Error("(file)", "nonempty fixture document required");
            } catch (YamlException e) {
                throw Error("line " + e.Start.Line.ToString(CultureInfo.InvariantCulture),
                    "invalid fixture YAML (unknown/duplicate key or malformed value)");
            }
            if (doc.Ruleset != SaveGame.Gdd03Ruleset) throw Error("ruleset", "must be gdd-0.3");
            if (doc.FixtureOnly != true) throw Error("fixture_only", "must explicitly be true; production is excluded");
            RequiredText(doc.Id, "id"); RequiredText(doc.FixturePort, "fixture_port");
            var ports = new HashSet<string>(StringComparer.Ordinal);
            foreach (var port in fixturePorts) { RequiredText(port, "fixture_port"); ports.Add(port); }
            if (!ports.Contains(doc.FixturePort!)) throw Error("fixture_port", "unknown synthetic fixture reference");

            // R-CITY-003: M content/gdd03-balance.yaml:287-298. Preserve Agreed/Default; Deferred is never numeric.
            var interval = Numbers(catalogue, "immigration.ship_interval", "Agreed", 1)[0];
            var average = Numbers(catalogue, "immigration.units_per_ship", "Agreed", 1)[0];
            var mix = Numbers(catalogue, "immigration.mix", "Default", 4);
            if (mix.Any(x => x > 100) || mix.Sum(x => (decimal)x) != 100m)
                throw Error("catalogue.immigration.mix", "requires whole percentages totalling 100");
            if (doc.IntervalTurns != interval) throw Error("interval_turns", "must explicitly match the positive integral catalogue cadence");
            if (doc.AverageUnitsPerShip != average) throw Error("average_units_per_ship", "must explicitly match the catalogue average; no fixed-size policy");
            if (doc.Slots == null || doc.Slots.Count == 0) throw Error("slots", "nonempty ordered cycle required");
            var slots = new List<Gdd03ImmigrationSlot>();
            var counts = new int[4];
            for (int i = 0; i < doc.Slots.Count; i++)
            {
                var at = "slots[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                var s = doc.Slots[i];
                if (s == null) throw Error(at, "explicit passenger slot required");
                // Zero-based contiguous ordinals are a fixture wire convention, not population/occurrence ids.
                if (s.Ordinal != i) throw Error(at + ".ordinal", "must equal its zero-based position; no duplicates or gaps");
                int role;
                switch (s.Role) {
                    case "refugee": role = 0; break;
                    case "free_civilian": role = 1; break;
                    case "apprentice_craftsman": role = 2; break;
                    case "trained_soldier": role = 3; break;
                    default: throw Error(at + ".role", "explicit known passenger role required");
                }
                // D cutover-resource-layer.md:41,45-47; training.md:11-13,23.
                if (s.ReceptionComplete != (role != 0)) throw Error(at + ".reception_complete", "refugees incomplete; all other roles explicitly complete");
                if (s.Profession != (role == 2 ? "craftsman" : "none")) throw Error(at + ".profession", "explicit role-compatible profession required");
                if (s.Tier != (role == 2 ? "apprentice" : "none")) throw Error(at + ".tier", "explicit role-compatible tier required");
                if (s.Qualification != (role == 3 ? "trained" : "none")) throw Error(at + ".qualification", "explicit role-compatible qualification required");
                if (s.Equipment != "none" && !(role == 3 && s.Equipment == "military_package"))
                    throw Error(at + ".equipment", "explicit none or soldier military_package declaration required");
                counts[role]++;
                slots.Add(new Gdd03ImmigrationSlot(i, s.Role!, s.ReceptionComplete!.Value,
                    s.Profession!, s.Tier!, s.Qualification!, s.Equipment!));
            }
            for (int i = 0; i < counts.Length; i++)
                if ((decimal)counts[i] * 100m != (decimal)slots.Count * mix[i])
                    throw Error("slots.mix", "whole passenger counts must exactly match the catalogue mix in refugee/free/apprentice/soldier order");
            return new Gdd03ImmigrationFixture(doc, interval, average, slots);
        }

        static int[] Numbers(Gdd03BalanceCatalogue c, string key, string basis, int length)
        {
            if (c.Values == null || !c.Values.TryGetValue(key, out var row) || row == null
                || row.Basis != basis || row.Values == null || row.Values.Count != length
                || !string.IsNullOrEmpty(row.Expression) || row.Parts == null || row.Parts.Count != 0
                || string.IsNullOrWhiteSpace(row.Note)
                || row.Values.Any(x => x <= 0 || x > int.MaxValue || decimal.Truncate(x) != x))
                throw Error("catalogue." + key, "requires the labelled exact positive integral catalogue row; Deferred is excluded");
            return row.Values.Select(x => (int)x).ToArray();
        }
        static void RequiredText(string? value, string at)
        {
            if (string.IsNullOrWhiteSpace(value)) throw Error(at, "explicit nonempty fixture id required");
            try { PopulationText.ValidateText(value); }
            catch (ArgumentException) { throw Error(at, "well-formed UTF-16 required"); }
        }
        static ContentException Error(string field, string message) => new ContentException(DiagnosticFile, field, message);
    }

    /// <summary>Immutable template slot. Equipment is a declaration, never an issue/refund or spare-Arms mutation.
    /// No military activation status, location, XP, paid flag or population id is inferred.</summary>
    public sealed class Gdd03ImmigrationSlot
    {
        public int Ordinal { get; }
        public string Role { get; }
        public bool ReceptionComplete { get; }
        public string Profession { get; }
        public string Tier { get; }
        public string Qualification { get; }
        public string Equipment { get; }
        internal Gdd03ImmigrationSlot(int ordinal, string role, bool receptionComplete, string profession,
            string tier, string qualification, string equipment)
        {
            Ordinal = ordinal; Role = role; ReceptionComplete = receptionComplete; Profession = profession;
            Tier = tier; Qualification = qualification; Equipment = equipment;
        }
    }

    // Nullable declarations distinguish absence/null from explicit false/zero/none. DTOs never escape Parse.
    internal sealed class ImmigrationFixtureDocument
    {
        public ImmigrationFixtureDocument() { }
        public string? Ruleset { get; set; }
        public bool? FixtureOnly { get; set; }
        public string? Id { get; set; }
        public string? FixturePort { get; set; }
        public int? IntervalTurns { get; set; }
        public int? AverageUnitsPerShip { get; set; }
        public List<ImmigrationFixtureSlotDocument?>? Slots { get; set; }
    }
    internal sealed class ImmigrationFixtureSlotDocument
    {
        public ImmigrationFixtureSlotDocument() { }
        public int? Ordinal { get; set; }
        public string? Role { get; set; }
        public bool? ReceptionComplete { get; set; }
        public string? Profession { get; set; }
        public string? Tier { get; set; }
        public string? Qualification { get; set; }
        public string? Equipment { get; set; }
    }
}
