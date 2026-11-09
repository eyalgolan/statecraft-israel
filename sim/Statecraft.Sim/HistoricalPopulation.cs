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

    /// <summary>#1725 PR 4, the ruled part (#1689): one host's opening field farmers, before any farmer record exists.
    /// <see cref="Fields"/> are the Gdd03FieldFixture ids (field/q,r) of the fields it hosts, in field-label order; one farmer
    /// each (owner, 3 November 2026, #1694: "One farmer per field (Recommended)"), split 1:2:1 per host by
    /// <see cref="Gdd03FarmerTierSplit"/> (owner, 7 November 2026, #1689: "Per host, 1/18/1 (Recommended)"), with one housing
    /// place per farmer (owner, 7 November 2026, #1689: "Yes to all three (Recommended)").</summary>
    internal sealed class HistoricalFieldFarmerHost
    {
        public string Host { get; }
        public string Region { get; }
        public IReadOnlyList<string> Fields { get; }
        /// <summary>The fields' regions.yaml labels (F01 to F20), in the same order as <see cref="Fields"/>: label order.</summary>
        public IReadOnlyList<string> Labels { get; }
        public int Novice { get; }
        public int Trained { get; }
        public int Veteran { get; }
        public int Farmers => Fields.Count;
        public int HousingPlaces => Farmers;
        internal HistoricalFieldFarmerHost(string host, string region, IReadOnlyList<string> fields, IReadOnlyList<string> labels,
            (int Novice, int Trained, int Veteran) split)
        {
            Host = host; Region = region; Fields = fields; Labels = labels; (Novice, Trained, Veteran) = split;
        }
        /// <summary>The tier of the farmer on <see cref="Labels"/>[i]: in label order, the novices first, then the trained,
        /// then the veterans. Only Tiberias has more than one tier (owner, 7 November 2026, relayed by the coordinator:
        /// "F08 veteran, F03 novice (Recommended)").</summary>
        public CivilianTier TierAt(int i) => i < Novice ? CivilianTier.Novice : i < Novice + Trained ? CivilianTier.Trained : CivilianTier.Veteran;
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
        /// <summary>#1725 PR 4, maintainer call (the coordinator, 7 November 2026): an opening field farmer's source key is
        /// this prefix plus the field's regions.yaml label (F01 to F20), ordinal 0, so it stays stable if a host changes.</summary>
        internal const string FarmerPrefix = "field/";
        /// <summary>The census city of the training centre's one veteran pioneer teacher (owner, 7 November 2026, #1689
        /// comment 6704565066: "the training centre's one veteran pioneer teacher is a Haifa census unit").</summary>
        internal const string TeacherCity = "haifa";

        /// <summary>The own opening formations, the one predicate for them (review S2-2): the map/units_1948.yaml rows that
        /// give the start inventory soldiers, Israeli, in the start (no dated arrival), with a strength. The inventory, the
        /// loader's formation_combat_xp check and StartingForces.Init's ledger check all read it.</summary>
        internal static IEnumerable<StartingForce> OpeningFormations(Content c) => c.StartingForces
            .Where(f => f.Side == Movement.PlayerSide && f.Arrives == null && f.Strength > 0);

        /// <summary>#1725 PR 7, producer 08: the own brigades dated after the start (R-UNIT-009), Israeli, with a strength, in
        /// <c>Content.StartingForces</c>' order (today the 7th Brigade). Not in the start inventory (R-POP-004): each arrives
        /// on its day as new soldier records (owner, 8 November 2026: "Soldiers of brigade (Recommended)").</summary>
        internal static IEnumerable<StartingForce> DatedFormations(Content c) => c.StartingForces
            .Where(f => f.Side == Movement.PlayerSide && f.Arrives != null && f.Strength > 0);

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
            // #1725 PR 4: one free civilian a field, on top of the census (owner, 7 November 2026, #1689: "On top of census
            // (Recommended)").
            foreach (var h in FieldFarmerHosts(c))
                foreach (var label in h.Labels) counts.Add((FarmerPrefix + label, 1, PopulationStatus.FreeCivilian));
            if (counts.Select(x => x.Source).Distinct(StringComparer.Ordinal).Count() != counts.Count)
                throw new InvalidOperationException("Duplicate Historical population source.");
            var rows = new List<HistoricalPopulationRow>();
            foreach (var (source, units, status) in counts.OrderBy(x => x.Source, StringComparer.Ordinal))
                for (int i = 0; i < units; i++) rows.Add(new HistoricalPopulationRow(source, i, status));
            return rows.AsReadOnly();
        }

        /// <summary>#1725 PR 4: the opening field farmers counted per host, ordered by host id (ordinal).
        /// Owner, 7 November 2026, #1689: "On top of census (Recommended)", so these are added to the census, not taken
        /// from it. Each field_hexes row's host (regions.yaml, #1717) hosts its field's one farmer; the loader has already
        /// checked that the host is a member location of the field's region (Q27). A field with no host, or a total other
        /// than the Agreed start.prepared_fields, is refused: no host or farmer count is invented. Nothing reads it yet:
        /// <see cref="Inventory"/> and <see cref="Initialize"/> build the farmer records from it, and
        /// <see cref="AddFarmerHousing"/> the dark start's housing places.</summary>
        internal static IReadOnlyList<HistoricalFieldFarmerHost> FieldFarmerHosts(Content c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (!c.UsesPopulationLedger) throw new InvalidOperationException("The Historical field farmers require gdd-0.3.");
            var hostOf = new Dictionary<(int Q, int R), string>();
            var labelOf = new Dictionary<(int Q, int R), string>();
            foreach (var region in c.PlayerRegions.Regions.Values)
                foreach (var h in region.FieldHosts) { hostOf.Add((h.Q, h.R), h.Host); labelOf.Add((h.Q, h.R), h.Field); }
            var fields = Gdd03FieldFixture.Fields(c);
            var unhosted = fields.Where(f => !hostOf.ContainsKey((f.Q, f.R))).Select(f => f.Id).ToList();
            if (unhosted.Count > 0)
                throw new InvalidOperationException("No field_hexes host in regions.yaml for " + string.Join(", ", unhosted) + " (R-POP-004, #1717).");
            var prepared = c.RequirePopulationInteger("start.prepared_fields");
            if (fields.Count != prepared)
                throw new InvalidOperationException("The opening fields are " + fields.Count.ToString(CultureInfo.InvariantCulture)
                    + " but start.prepared_fields is " + prepared.ToString(CultureInfo.InvariantCulture) + "; one farmer per field (#1694, #1689).");
            return fields.GroupBy(f => hostOf[(f.Q, f.R)], StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g =>
                {
                    var hosted = g.OrderBy(f => labelOf[(f.Q, f.R)], StringComparer.Ordinal).ToArray();
                    var region = g.Select(f => f.Region).Distinct(StringComparer.Ordinal).Single();
                    return new HistoricalFieldFarmerHost(g.Key, region, Array.AsReadOnly(hosted.Select(f => f.Id).ToArray()),
                        Array.AsReadOnly(hosted.Select(f => labelOf[(f.Q, f.R)]).ToArray()), Gdd03FarmerTierSplit.Split(hosted.Length));
                }).ToList().AsReadOnly();
        }

        /// <summary>#1725 PR 4: the dark start's housing places for the opening farmers. Owner, 7 November 2026 (#1689 comment
        /// 6704565066), "Yes to all three (Recommended)": one added housing place per farmer in their host, so each census
        /// host's city gains its farmers' count in HousingCapacity. Nir Am and Nirim have no cities.yaml record and get none
        /// (owner, relayed by the coordinator: "None for those two for now (Recommended)"): 18 of the 20 places, until the
        /// housing producer (#1725 PR 7). Refuses, unchanged, a state whose census host city is missing.</summary>
        internal static void AddFarmerHousing(Content c, GameState s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var census = c.Cities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            var add = FieldFarmerHosts(c).Where(h => census.Contains(h.Host)).ToList();
            var missing = add.Where(h => !s.Cities.ContainsKey(h.Host)).Select(h => h.Host).ToList();
            if (missing.Count > 0) throw new InvalidOperationException("No city record for farmer host " + string.Join(", ", missing) + " (#1689).");
            foreach (var h in add)
            {
                var x = s.Cities[h.Host];
                s.Cities[h.Host] = new City(x.Id, checked(x.HousingCapacity + h.HousingPlaces), x.JobSlots, x.Civilians, x.Employed,
                    x.Refugees, x.Recruits, x.Condition, x.Craftsmen, x.JobsDefault, x.Qualified);
            }
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
            // #1725 PR 7, producer 08. Coordinator call, 8 November 2026 (Q5 (a)): each own dated brigade (R-UNIT-009) is bound
            // at the start, its service target in its arrival region and its soldiers' qualification, with no record, as its
            // unit id is reserved at the start; StartingForces.Arrive then only registers its records.
            var dated = new HashSet<string>(DatedFormations(c).Select(f => FormationPrefix + f.Row), StringComparer.Ordinal);
            bool Bound(StartingForce f) => sources.Contains(FormationPrefix + f.Row) || dated.Contains(FormationPrefix + f.Row);
            var regions = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var city in c.Cities)
                if (sources.Contains(CensusPrefix + city.Id)) regions[CensusPrefix + city.Id] = c.PlayerRegions.AtLocation(city.Id)?.Id ?? "";
            foreach (var f in c.StartingForces)
                if (Bound(f)) regions[FormationPrefix + f.Row] = c.PlayerRegions.AtHex(f.Q, f.R)?.Id ?? "";
            // #1725 PR 4: a farmer lives in its field's host, a member location of the field's region (Q27).
            var farmer = new Dictionary<string, (string Host, string Field, CivilianTier Tier)>(StringComparer.Ordinal);
            foreach (var h in FieldFarmerHosts(c))
                for (int i = 0; i < h.Labels.Count; i++)
                {
                    farmer[FarmerPrefix + h.Labels[i]] = (h.Host, h.Fields[i], h.TierAt(i));
                    regions[FarmerPrefix + h.Labels[i]] = h.Region;
                }
            var missing = regions.Where(x => x.Value == "").Select(x => x.Key).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("No region in regions.yaml for " + string.Join(", ", missing) + " (R-POP-004).");
            var locations = regions.Values.Distinct(StringComparer.Ordinal).ToDictionary(r => r, r => new PopulationLocation(r), StringComparer.Ordinal);
            // A census record is bound to its city in that region; a formation record keeps the region-only location.
            var placed = regions.ToDictionary(x => x.Key, x => x.Key.StartsWith(CensusPrefix, StringComparison.Ordinal)
                ? new PopulationLocation(x.Value, city: x.Key.Substring(CensusPrefix.Length))
                : farmer.TryGetValue(x.Key, out var fm) ? new PopulationLocation(x.Value, city: fm.Host) : locations[x.Value], StringComparer.Ordinal);
            // A formation is its soldiers' ActiveService target, in the formation's region.
            var targets = regions.Where(x => x.Key.StartsWith(FormationPrefix, StringComparison.Ordinal))
                .Select(x => new PopulationTarget(x.Key, PopulationAssignmentKind.ActiveService, x.Value))
                // A field is its farmer's Work target in the field's region (Gdd03FieldFixture.Targets' ids, field/q,r).
                .Concat(farmer.Select(x => new PopulationTarget(x.Value.Field, PopulationAssignmentKind.Work, regions[x.Key])))
                // #1725 PR 7, producer 12. Owner, 8 November 2026 (#1725 issuecomment-6724941867), "Start + runtime bind (Rec.)": the start
                // facilities' Course targets are bound at the start; and "Cohorts stay main (Rec.)": each trainee's Course
                // target is per facility (military_camp/<city>, hakhshara/<city>), in the city's region (27M-7, no travel).
                .Concat(c.Cities.Where(x => regions.ContainsKey(CensusPrefix + x.Id))
                    .SelectMany(x => Training.LedgerFacilities(c, x.Facilities).Select(f => Training.CourseTargetOf(c, f, x.Id, regions[CensusPrefix + x.Id]))));
            // #1689 rows 5 to 8 and the 7th Brigade's combat XP: see SoldierValues.
            var soldier = new Dictionary<string, (string Qualification, int CombatXp)>(StringComparer.Ordinal);
            foreach (var f in c.StartingForces.Where(Bound)) soldier[FormationPrefix + f.Row] = SoldierValues(c, f);
            var bindings = new PopulationBindings(locations.Values.Concat(placed.Values.Where(l => l.City != null)
                .GroupBy(l => l.Canonical(), StringComparer.Ordinal).Select(g => g.First())), targets,
                Array.Empty<string>(), soldier.Values.Select(x => x.Qualification).Concat(Training.TrainedClasses(c)).Distinct(StringComparer.Ordinal));
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
            // #1725 PR 4. Owner, 7 November 2026 (#1689 comment 6704565066), "Yes to all three (Recommended)": every farmer
            // is staffed on their field from turn 1, so each holds its field's Work assignment. Tier: the per-host 1:2:1 split
            // in label order (FieldFarmerHosts; owner, relayed by the coordinator: "F08 veteran, F03 novice (Recommended)").
            // Work XP 0 (owner, relayed: "0 for all (Recommended)"; veterans "0 (Recommended)", #1689 comment 6710877137).
            // Reception done (reception.turns), as for the census (maintainer call, the coordinator, 7 November 2026). No
            // tools: post-MVP (#1693 answer 1, "Tools in the MVP: no.").
            PopulationAssignment Assignment(HistoricalPopulationRow r) => r.Status == PopulationStatus.ActiveSoldier
                ? new PopulationAssignment(PopulationAssignmentKind.ActiveService, r.Source)
                : farmer.TryGetValue(r.Source, out var f) ? new PopulationAssignment(PopulationAssignmentKind.Work, f.Field) : PopulationAssignment.Unassigned;
            CivilianProfession Profession(HistoricalPopulationRow r) => IsTeacher(r) ? CivilianProfession.Pioneer
                : farmer.ContainsKey(r.Source) ? CivilianProfession.Farmer : CivilianProfession.None;
            CivilianTier Tier(HistoricalPopulationRow r) => IsTeacher(r) ? CivilianTier.Veteran
                : farmer.TryGetValue(r.Source, out var f) ? f.Tier : CivilianTier.None;
            var rows = inventory.Select(r => new PopulationInventoryRow(r.Source, r.Ordinal, new PopulationUnit(0, r.Status,
                r.Status == PopulationStatus.FreeCivilian ? received : 0,
                placed[r.Source], Assignment(r), Profession(r), Tier(r), 0,
                r.Status == PopulationStatus.ActiveSoldier ? soldier[r.Source].Qualification : "",
                r.Status == PopulationStatus.ActiveSoldier ? soldier[r.Source].CombatXp : 0, r.Status == PopulationStatus.ActiveSoldier
                    ? new PopulationEquipment(militaryId: r.Source + "/" + r.Ordinal.ToString(CultureInfo.InvariantCulture), militaryPaid: true)
                    : PopulationEquipment.None)));
            s.PopulationLedger.Initialize(c, bindings, rows, 0, Mode.Historical);
        }

        /// <summary>#1689 rows 5 to 8. Owner, 7 November 2026 (comment 6704389240): "Veteran for all 8 (Recommended)" and "1
        /// each, Negev 0 (Recommended)", Negev's 0 a labelled stand-in; for the 7th Brigade, which arrives later (R-UNIT-009),
        /// owner, 8 November 2026: "1, like the others". Maintainer call, eyalgolan-30's coordinator, 7 November 2026: the
        /// veteran level lives on the formation (a veteran_soldier, R-UNIT-006, each soldier's ActiveService target), so a
        /// soldier's qualification is its formation type's unit_types.yaml class, the source Gdd03BasicTrainingFixture's
        /// graduates use; its combat XP is gdd03-start.yaml's formation_combat_xp, no fallback.</summary>
        static (string Qualification, int CombatXp) SoldierValues(Content c, StartingForce f)
        {
            var xp = c.Gdd03FormationCombatXp
                ?? throw new InvalidOperationException("The Historical ledger fixture needs gdd03-start.yaml's formation_combat_xp; no fallback (#1689).");
            var cls = c.UnitTypes.FirstOrDefault(t => string.Equals(t.Id, f.Type, StringComparison.Ordinal))?.Class;
            if (string.IsNullOrWhiteSpace(cls))
                throw new InvalidOperationException("No unit_types.yaml class for formation " + f.Row + "'s type " + f.Type + " (#1689).");
            if (!xp.TryGetValue(f.Row, out var combatXp))
                throw new InvalidOperationException("No gdd03-start.yaml formation_combat_xp for formation " + f.Row + "; no fallback (#1689).");
            return (cls!, combatXp);
        }

        /// <summary>#1725 PR 7, producer 08 (R-UNIT-009 on the ledger): the records an own dated brigade brings when it arrives,
        /// one per strength unit, as a starting formation's soldiers are (R-POP-004). Owner, 8 November 2026: "Soldiers of
        /// brigade (Recommended)": each is an active soldier serving in the brigade (source key <see cref="FormationPrefix"/>
        /// plus its row, ordinals from 0), with its type's class and its formation_combat_xp ("1, like the others"); "New
        /// people (Recommended)": new records, none taken from the civilians; "Apart from Arms (Recommended)": one issued
        /// military item each, <c>units_1948/&lt;row&gt;/&lt;ordinal&gt;</c>, owned and recorded as paid, as at the start, with
        /// the Arms stock untouched. Coordinator call, 8 November 2026 (Q6 (a)): located in the region of its arrival anchor's
        /// hex, region-only, as a starting formation's records are. Reception 0 and no profession, as a starting soldier.</summary>
        internal static IReadOnlyList<PopulationInventoryRow> ArrivalRows(Content c, StartingForce f)
        {
            if (!(f.Side == Movement.PlayerSide && f.Arrives != null && f.Strength > 0))
                throw new InvalidOperationException("Formation " + f.Row + " is not an own brigade dated after the start (R-UNIT-009).");
            var region = c.PlayerRegions.AtHex(f.Q, f.R)?.Id
                ?? throw new InvalidOperationException("No region in regions.yaml for " + FormationPrefix + f.Row + " (R-POP-004).");
            var v = SoldierValues(c, f);
            var source = FormationPrefix + f.Row;
            var rows = new List<PopulationInventoryRow>();
            for (int i = 0; i < f.Strength; i++)
                rows.Add(new PopulationInventoryRow(source, i, new PopulationUnit(0, PopulationStatus.ActiveSoldier, 0,
                    new PopulationLocation(region), new PopulationAssignment(PopulationAssignmentKind.ActiveService, source),
                    CivilianProfession.None, CivilianTier.None, 0, v.Qualification, v.CombatXp,
                    new PopulationEquipment(militaryId: source + "/" + i.ToString(CultureInfo.InvariantCulture), militaryPaid: true))));
            return rows.AsReadOnly();
        }
    }
}
