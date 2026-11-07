using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>The census city of the training centre's one veteran pioneer teacher (owner, 7 November 2026, #1689
        /// comment 6704565066: "the training centre's one veteran pioneer teacher is a Haifa census unit").</summary>
        internal const string TeacherCity = "haifa";

        /// <summary>The own opening formations, the one predicate for them (review S2-2): the map/units_1948.yaml rows that
        /// give the start inventory soldiers, Israeli, in the start (no dated arrival), with a strength. The inventory, the
        /// loader's formation_combat_xp check and StartingForces.Init's ledger check all read it.</summary>
        internal static IEnumerable<StartingForce> OpeningFormations(Content c) => c.StartingForces
            .Where(f => f.Side == Movement.PlayerSide && f.Arrives == null && f.Strength > 0);

        /// <summary>The row ids of <see cref="OpeningFormations"/>.</summary>
        internal static IEnumerable<string> OpeningFormationRows(Content c) => OpeningFormations(c).Select(f => f.Row);

        internal static IReadOnlyList<HistoricalPopulationRow> Inventory(Content c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (!c.UsesPopulationLedger) throw new InvalidOperationException("The Historical population inventory requires gdd-0.3.");
            var counts = new List<(string Source, int Units, PopulationStatus Status)>();
            foreach (var city in c.Cities) counts.Add((CensusPrefix + city.Id, city.Civilians, PopulationStatus.FreeCivilian));
            foreach (var f in OpeningFormations(c))
                counts.Add((FormationPrefix + f.Row, f.Strength, PopulationStatus.ActiveSoldier));
            if (counts.Select(x => x.Source).Distinct(StringComparer.Ordinal).Count() != counts.Count)
                throw new InvalidOperationException("Duplicate Historical population source.");
            var rows = new List<HistoricalPopulationRow>();
            foreach (var (source, units, status) in counts.OrderBy(x => x.Source, StringComparer.Ordinal))
                for (int i = 0; i < units; i++) rows.Add(new HistoricalPopulationRow(source, i, status));
            return rows.AsReadOnly();
        }

        /// <summary>R-POP-004 (#1689 region slice): the Inventory as an initialized Historical ledger fixture, each record in
        /// its real region of regions.yaml. A census record takes the region that lists its city (AtLocation), and its location names that city. A formation
        /// record takes the region of its starting anchor hex (AtHex): the owner's ruling on #1689's open question 1
        /// (3 November 2026: "Its anchor hex's region (Recommended)"). A source with no region is refused; no region is invented. Records
        /// are free civilians with no assignment and their reception done (reception.turns), and soldiers serving in their own
        /// formation, with no profession, tier or qualification (#1717 and #1692 add those), but for the training centre's veteran pioneer teacher, the last census/haifa record (#1689). Each soldier carries one issued military item, id
        /// <c>units_1948/&lt;row&gt;/&lt;ordinal&gt;</c>, owned and recorded as paid, apart from the spare Arms (#1717, 27M-24).
        /// mode Historical gives #2299's 1,000-a-record scale (D2). Fixture-only: the public NewGame does not call it (R-POP-002).</summary>
        internal static GameState LedgerFixture(Content c)
        {
            var s = new GameState(c) { Mode = Mode.Historical };
            Initialize(c, s);
            return s;
        }

        /// <summary>#1725 PR 5: <see cref="LedgerFixture"/>'s ledger, initialized in place on <paramref name="s"/>, a gdd-0.3
        /// Historical state with no ledger yet; the dark start (Simulation.NewGame's internal overload) calls it before
        /// Pools.Init. All or nothing (PopulationLedger.Initialize): a refusal leaves the state as it was.</summary>
        internal static void Initialize(Content c, GameState s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var inventory = Inventory(c);
            if (s.Mode != Mode.Historical) throw new InvalidOperationException("The Historical ledger needs a Historical game (R-POP-004).");
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
            // A census record is bound to its city in that region; a formation record keeps the region-only location.
            var placed = regions.ToDictionary(x => x.Key, x => x.Key.StartsWith(CensusPrefix, StringComparison.Ordinal)
                ? new PopulationLocation(x.Value, city: x.Key.Substring(CensusPrefix.Length)) : locations[x.Value], StringComparer.Ordinal);
            // A formation is its soldiers' ActiveService target, in the formation's region.
            var targets = regions.Where(x => x.Key.StartsWith(FormationPrefix, StringComparison.Ordinal))
                .Select(x => new PopulationTarget(x.Key, PopulationAssignmentKind.ActiveService, x.Value));
            // #1689 rows 5 to 8. Owner, 7 November 2026 (comment 6704389240): "Veteran for all 8 (Recommended)" and "1 each,
            // Negev 0 (Recommended)", Negev's 0 a labelled stand-in. Maintainer call, eyalgolan-30's coordinator, 7 November
            // 2026: the veteran level lives on the formation (a veteran_soldier, R-UNIT-006, each soldier's ActiveService
            // target), so a soldier's qualification is its formation type's unit_types.yaml class, the source
            // Gdd03BasicTrainingFixture's graduates use; its combat XP is gdd03-start.yaml's formation_combat_xp, no fallback.
            var xp = c.Gdd03FormationCombatXp
                ?? throw new InvalidOperationException("The Historical ledger fixture needs gdd03-start.yaml's formation_combat_xp; no fallback (#1689).");
            var soldier = new Dictionary<string, (string Qualification, int CombatXp)>(StringComparer.Ordinal);
            foreach (var f in c.StartingForces.Where(f => sources.Contains(FormationPrefix + f.Row)))
            {
                var cls = c.UnitTypes.FirstOrDefault(t => string.Equals(t.Id, f.Type, StringComparison.Ordinal))?.Class;
                if (string.IsNullOrWhiteSpace(cls))
                    throw new InvalidOperationException("No unit_types.yaml class for formation " + f.Row + "'s type " + f.Type + " (#1689).");
                if (!xp.TryGetValue(f.Row, out var combatXp))
                    throw new InvalidOperationException("No gdd03-start.yaml formation_combat_xp for formation " + f.Row + "; no fallback (#1689).");
                soldier[FormationPrefix + f.Row] = (cls!, combatXp);
            }
            var bindings = new PopulationBindings(locations.Values.Concat(placed.Values.Where(l => l.City != null)), targets,
                Array.Empty<string>(), soldier.Values.Select(x => x.Qualification).Distinct(StringComparer.Ordinal));
            // reception.turns is the reception before free-civilian status (docs/gdd/balance.md:91), so a census free civilian
            // starts with it done: R-VET-001 gives work XP only to a received free civilian. Soldiers keep 0 (not ruled).
            var received = c.RequirePopulationInteger("reception.turns");
            // #1725 PR 3, the teacher part. Owner, 7 November 2026 (#1689 comment 6704565066), "Yes to all three
            // (Recommended)": "the training centre's one veteran pioneer teacher is a Haifa census unit"; the count is
            // start.pioneer_teachers (1, Agreed, "included in population total"), so no record is added and no other count
            // has a ruling. Maintainer call (the coordinator, eyalgolan-30, 7 November 2026): the last census/haifa ordinal.
            // R-TRAIN-005: a free civilian, a veteran of the matching profession (pioneer). Owner, 7 November 2026, relayed
            // by the coordinator: work XP "0 (Recommended)". R-TRAIN-008 refuses a teaching teacher with no pupil, so the
            // teacher starts unassigned. Owner, 7 November 2026, relayed: "Takeable, but last (Recommended)": no guard; the
            // ascending-id rules (R-POP-004's Q10 "N units", #1692's job default) reach the last ordinal last.
            var teachers = c.RequirePopulationInteger("start.pioneer_teachers");
            if (teachers != 1)
                throw new InvalidOperationException("start.pioneer_teachers is " + teachers.ToString(CultureInfo.InvariantCulture)
                    + "; the ruling places one veteran pioneer teacher in Haifa's census and no other (#1689).");
            var teacherSource = CensusPrefix + TeacherCity;
            var teacher = inventory.Where(r => r.Source == teacherSource).OrderBy(r => r.Ordinal).LastOrDefault()
                ?? throw new InvalidOperationException("No " + teacherSource + " record for the veteran pioneer teacher (#1689).");
            bool IsTeacher(HistoricalPopulationRow r) => ReferenceEquals(r, teacher);
            var rows = inventory.Select(r => new PopulationInventoryRow(r.Source, r.Ordinal, new PopulationUnit(0, r.Status,
                r.Status == PopulationStatus.FreeCivilian ? received : 0,
                placed[r.Source], r.Status == PopulationStatus.ActiveSoldier
                    ? new PopulationAssignment(PopulationAssignmentKind.ActiveService, r.Source) : PopulationAssignment.Unassigned,
                IsTeacher(r) ? CivilianProfession.Pioneer : CivilianProfession.None, IsTeacher(r) ? CivilianTier.Veteran : CivilianTier.None, 0,
                r.Status == PopulationStatus.ActiveSoldier ? soldier[r.Source].Qualification : "",
                r.Status == PopulationStatus.ActiveSoldier ? soldier[r.Source].CombatXp : 0, r.Status == PopulationStatus.ActiveSoldier
                    ? new PopulationEquipment(militaryId: r.Source + "/" + r.Ordinal.ToString(CultureInfo.InvariantCulture), militaryPaid: true)
                    : PopulationEquipment.None)));
            s.PopulationLedger.Initialize(c, bindings, rows, 0, Mode.Historical);
        }
    }
}
