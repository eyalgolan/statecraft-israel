using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One population unit of the Historical start inventory (R-POP-004): its source key, its ordinal within that
    /// source and its starting status. The ledger's id order is source key, then ordinal (R-POP-001).</summary>
    internal sealed class HistoricalPopulationRow
    {
        /// <summary><see cref="HistoricalPopulation.CensusPrefix"/> plus a city id, or <see cref="HistoricalPopulation.FormationPrefix"/>
        /// plus a map/units_1948.yaml row id.</summary>
        public string Source { get; }
        public int Ordinal { get; }
        public PopulationStatus Status { get; }
        internal HistoricalPopulationRow(string source, int ordinal, PopulationStatus status)
        {
            Source = source; Ordinal = ordinal; Status = status;
        }
    }

    /// <summary>R-POP-004 (#1689; owner, 1 November 2026, D2 and D4): the own population a gdd-0.3 Historical game starts with,
    /// read from the content the aggregate start reads. Each census city gives its R-CITY-007 start units as free civilians;
    /// each Israeli starting formation of map/units_1948.yaml (R-UNIT-006) gives its strength as active soldiers. Enemy
    /// formations are not own population, and a dated arrival (R-UNIT-009) is not in the start. Counts use rules.yaml's
    /// people_per_unit (1,000, D2), as Cities and StartingForces already do. No RNG, no state, no culture formatting.
    /// Nothing in the game calls it yet: an initialized ledger refuses every aggregate system (R-POP-002), so NewGame keeps
    /// the aggregate start until the producers are integrated.</summary>
    internal static class HistoricalPopulation
    {
        internal const string CensusPrefix = "census/";
        internal const string FormationPrefix = "units_1948/";

        internal static IReadOnlyList<HistoricalPopulationRow> Inventory(Content c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (!c.UsesPopulationLedger) throw new InvalidOperationException("The Historical population inventory requires gdd-0.3.");
            var counts = new List<(string Source, int Units, PopulationStatus Status)>();
            foreach (var city in c.Cities) counts.Add((CensusPrefix + city.Id, city.Civilians, PopulationStatus.FreeCivilian));
            foreach (var f in c.StartingForces)
                if (f.Side == Movement.PlayerSide && f.Arrives == null)
                    counts.Add((FormationPrefix + f.Row, f.Strength, PopulationStatus.ActiveSoldier));
            if (counts.Select(x => x.Source).Distinct(StringComparer.Ordinal).Count() != counts.Count)
                throw new InvalidOperationException("Duplicate Historical population source.");
            var rows = new List<HistoricalPopulationRow>();
            foreach (var (source, units, status) in counts.OrderBy(x => x.Source, StringComparer.Ordinal))
                for (int i = 0; i < units; i++) rows.Add(new HistoricalPopulationRow(source, i, status));
            return rows.AsReadOnly();
        }

        /// <summary>R-POP-004 (#1689 region slice): the Inventory as an initialized Historical ledger fixture, each record in
        /// its real region of regions.yaml. A census record takes the region that lists its city (AtLocation). A formation
        /// record takes the region of its starting anchor hex (AtHex): the owner's ruling on #1689's open question 1
        /// (3 November 2026: "Its anchor hex's region (Recommended)"). A source with no region is refused; no region is invented. Records
        /// are free civilians with no assignment and soldiers serving in their own formation, with no profession, tier or
        /// qualification (#1717 and #1692 add those). mode Historical gives #2299's 1,000-a-record scale (D2). Fixture-only:
        /// NewGame does not call it (R-POP-002).</summary>
        internal static GameState LedgerFixture(Content c)
        {
            var inventory = Inventory(c);
            var sources = new HashSet<string>(inventory.Select(r => r.Source), StringComparer.Ordinal);   // only sources with records
            var regions = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var city in c.Cities)
                if (sources.Contains(CensusPrefix + city.Id)) regions[CensusPrefix + city.Id] = c.PlayerRegions.AtLocation(city.Id)?.Id ?? "";
            foreach (var f in c.StartingForces)
                if (sources.Contains(FormationPrefix + f.Row)) regions[FormationPrefix + f.Row] = c.PlayerRegions.AtHex(f.Q, f.R)?.Id ?? "";
            var missing = regions.Where(x => x.Value == "").Select(x => x.Key).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("No region in regions.yaml for " + string.Join(", ", missing) + " (R-POP-004).");
            var locations = regions.Values.Distinct(StringComparer.Ordinal).ToDictionary(r => r, r => new PopulationLocation(r), StringComparer.Ordinal);
            // A formation is its soldiers' ActiveService target, in the formation's region.
            var targets = regions.Where(x => x.Key.StartsWith(FormationPrefix, StringComparison.Ordinal))
                .Select(x => new PopulationTarget(x.Key, PopulationAssignmentKind.ActiveService, x.Value));
            var bindings = new PopulationBindings(locations.Values, targets, Array.Empty<string>(), Array.Empty<string>());
            var rows = inventory.Select(r => new PopulationInventoryRow(r.Source, r.Ordinal, new PopulationUnit(0, r.Status, 0,
                locations[regions[r.Source]], r.Status == PopulationStatus.ActiveSoldier
                    ? new PopulationAssignment(PopulationAssignmentKind.ActiveService, r.Source) : PopulationAssignment.Unassigned,
                CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None)));
            return PopulationLedger.CreateFixture(c, bindings, rows, mode: Mode.Historical);
        }
    }
}
