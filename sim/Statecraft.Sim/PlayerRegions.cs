using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Authored geography only. No capacities, residents, control predicate or production defaults.</summary>
    public sealed class PlayerRegionDefinition
    {
        public string Id { get; set; } = "";
        public string CapitalLocation { get; set; } = "";
        public List<PlayerRegionHex> MemberHexes { get; set; } = new List<PlayerRegionHex>();
        public List<string> MemberLocations { get; set; } = new List<string>();
        public List<PlayerRegionFieldHex> FieldHexes { get; set; } = new List<PlayerRegionFieldHex>();
        public string DesignNote { get; set; } = "";
        public List<string> Sources { get; set; } = new List<string>();
        public string CapitalApproval { get; set; } = "";
        public string BindingStatus { get; set; } = "";
        public PlayerRegionControl OpeningControl { get; set; } = new PlayerRegionControl();
    }

    public sealed class PlayerRegionHex
    {
        public int? Q { get; set; }
        public int? R { get; set; }
    }

    /// <summary>A `field_hexes` row: the hex and, optionally, the place that hosts its farmer (R-POP-004, #1717).</summary>
    public sealed class PlayerRegionFieldHex
    {
        public int? Q { get; set; }
        public int? R { get; set; }
        public string Field { get; set; } = "";
        public string Host { get; set; } = "";
        public string HostBasis { get; set; } = "";
        public string HostSource { get; set; } = "";
    }

    /// <summary>A field's approved host. Data only: no farmer record exists and NewGame does not read it.</summary>
    public sealed class PlayerRegionFieldHost
    {
        public string Field { get; }
        public int Q { get; }
        public int R { get; }
        public string Host { get; }
        public string Basis { get; }
        public string Source { get; }
        internal PlayerRegionFieldHost(PlayerRegionFieldHex row)
        {
            Field = row.Field; Q = row.Q!.Value; R = row.R!.Value; Host = row.Host; Basis = row.HostBasis; Source = row.HostSource;
        }
    }

    /// <summary>Evidence metadata, never an operational permission or inherited member control.</summary>
    public sealed class PlayerRegionControl
    {
        public string Status { get; set; } = "";
        // Object preserves YAML null through the loader's string normalization. Validation accepts only null or string.
        public object? Value { get; set; }
    }

    /// <summary>Frozen validated row. Consumers cannot mutate the authored DTO behind an index.</summary>
    public sealed class PlayerRegionGeography
    {
        public string Id { get; }
        public string CapitalLocation { get; }
        /// <summary>The hex of <see cref="CapitalLocation"/>, the region's label place, as the place files put it (#2473: a
        /// returned captive goes to the nearest census city to it). Not in <see cref="Canonical"/>.</summary>
        public (int Q, int R) CapitalHex { get; }
        public IReadOnlyList<(int Q, int R)> MemberHexes { get; }
        public IReadOnlyList<string> MemberLocations { get; }
        public IReadOnlyList<(int Q, int R)> FieldHexes { get; }
        public IReadOnlyList<PlayerRegionFieldHost> FieldHosts { get; }
        public string DesignNote { get; }
        public IReadOnlyList<string> Sources { get; }
        public string CapitalApproval { get; }
        public string BindingStatus { get; }
        public string ControlStatus { get; }
        public string? OpeningController { get; }
        internal PlayerRegionGeography(PlayerRegionDefinition row, bool hosts, (int Q, int R) capitalHex)
        {
            Id = row.Id; CapitalLocation = row.CapitalLocation; CapitalHex = capitalHex;
            MemberHexes = Array.AsReadOnly(row.MemberHexes.Select(h => (h.Q!.Value, h.R!.Value))
                .OrderBy(h => h.Item1).ThenBy(h => h.Item2).ToArray());
            MemberLocations = Array.AsReadOnly(row.MemberLocations.OrderBy(x => x, StringComparer.Ordinal).ToArray());
            FieldHexes = Array.AsReadOnly(row.FieldHexes.Select(h => (h.Q!.Value, h.R!.Value))
                .OrderBy(h => h.Item1).ThenBy(h => h.Item2).ToArray());
            FieldHosts = Array.AsReadOnly(row.FieldHexes.Where(h => hosts && h.Field.Length > 0).Select(h => new PlayerRegionFieldHost(h))
                .OrderBy(h => h.Field, StringComparer.Ordinal).ToArray());
            DesignNote = row.DesignNote; Sources = Array.AsReadOnly(row.Sources.OrderBy(x => x, StringComparer.Ordinal).ToArray());
            CapitalApproval = row.CapitalApproval; BindingStatus = row.BindingStatus;
            ControlStatus = row.OpeningControl.Status;
            OpeningController = row.OpeningControl.Value as string;
        }
        internal IEnumerable<string> Canonical()
        {
            yield return PopulationText.Fields("region", Id, CapitalLocation);
            foreach (var h in MemberHexes) yield return PopulationText.Fields("hex", h.Q, h.R);
            foreach (var p in MemberLocations) yield return PopulationText.Fields("place", p);
        }
    }

    /// <summary>R-CITY-006, R-ENEMY-007. One validator and one immutable GDD geography index for all consumers.
    /// A member place is a settlement, a named map location, a designated site (sites.yaml) or a locality placed by
    /// <see cref="LocalityHexesFileName"/> (owner, 2 November 2026: Q1 "Accept localities"; "Fix all, then ship
    /// (Recommended)": "Accept site ids too"). capital_location is the region's label place (Q3 "All nine drafts as
    /// labels"), required to be a member; it is NOT the regional-capital model the owner declined on 30 October 2026
    /// (docs/GDD.md, "Regional-capital model"), and nothing gives it capital mechanics.</summary>
    public sealed class PlayerRegionCatalogue
    {
        public const string FileName = "regions.yaml";
        /// <summary>Each locality's hex, written by tools/locality_hexes.py with the map's projection and rounding, so the
        /// sim needs no trigonometry (docs/determinism.md). Read under either GDD identity, with regions.yaml.</summary>
        public const string LocalityHexesFileName = "map/locality_hexes.yaml";
        /// <summary>Owner, 7 November 2026, #1689: "These two only (Recommended)". The only fields a settlement may host.</summary>
        static readonly string[] SettlementHostFields = { "F19", "F20" };
        static readonly string[] HostBases = { "sourced", "administrative", "forced", "inference", "design_choice", "decision_a" };
        readonly SortedDictionary<string, PlayerRegionGeography> regions = new SortedDictionary<string, PlayerRegionGeography>(StringComparer.Ordinal);
        readonly Dictionary<string, string> places = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> capitals = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<(int, int), string> hexes = new Dictionary<(int, int), string>();
        public IReadOnlyDictionary<string, PlayerRegionGeography> Regions { get; }
        public static PlayerRegionCatalogue Empty { get; } = new PlayerRegionCatalogue();
        PlayerRegionCatalogue() { Regions = new ReadOnlyDictionary<string, PlayerRegionGeography>(regions); }
        public PlayerRegionGeography? AtLocation(string id) => places.TryGetValue(id, out var r) ? regions[r] : null;
        public PlayerRegionGeography? AtCapital(string id) => capitals.TryGetValue(id, out var r) ? regions[r] : null;
        public PlayerRegionGeography? AtHex(int q, int r) => hexes.TryGetValue((q, r), out var id) ? regions[id] : null;

        internal static PlayerRegionCatalogue Validate(Content c, IEnumerable<PlayerRegionDefinition> rows,
            IEnumerable<SettlementHex>? localities = null)
        {
            var result = new PlayerRegionCatalogue();
            var placements = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
            var conflicts = new HashSet<string>(StringComparer.Ordinal);
            void Place(string id, int q, int r)
            {
                if (placements.TryGetValue(id, out var prior) && prior != (q, r)) conflicts.Add(id);
                placements[id] = (q, r);
            }
            foreach (var p in c.SettlementHexes) Place(p.Id, p.Q, p.R);
            foreach (var p in c.MapLocations) Place(p.Id, p.Q, p.R);
            foreach (var p in c.Sites) Place(p.Id, p.Q, p.R);
            foreach (var p in localities ?? Enumerable.Empty<SettlementHex>()) Place(p.Id, p.Q, p.R);
            var census = new HashSet<string>(c.Cities.Select(city => city.Id), StringComparer.Ordinal);
            int index = 0, fieldNumber = 0;
            foreach (var row in rows)
            {
                var at = FormattableString.Invariant($"player_regions[{index++}]");
                void Fail(string field, string reason) => throw new ContentException(FileName, at + "." + field, reason);
                if (string.IsNullOrWhiteSpace(row.Id) || !Offices.OfficeId.IsMatch(row.Id)) Fail("id", "requires a stable id");
                if (result.regions.ContainsKey(row.Id)) Fail("id", "duplicate region id");
                if (string.IsNullOrWhiteSpace(row.DesignNote)) Fail("design_note", "required; geography is a design choice");
                if (row.Sources.Count == 0 || row.Sources.Any(string.IsNullOrWhiteSpace)) Fail("sources", "explicit provenance required");
                if (row.CapitalApproval != "APPROVED" && row.CapitalApproval != "NEEDS-RESEARCH") Fail("capital_approval", "must be APPROVED or NEEDS-RESEARCH");
                if (row.BindingStatus != "SOURCED" && row.BindingStatus != "PARTIAL-EVIDENCE" && row.BindingStatus != "NEEDS-RESEARCH") Fail("binding_status", "unknown evidence status");
                if (row.OpeningControl == null) Fail("opening_control", "required");
                var control = row.OpeningControl!;
                if (control.Status != "SOURCED" && control.Status != "PARTIAL-EVIDENCE" && control.Status != "NEEDS-RESEARCH") Fail("opening_control.status", "unknown evidence status");
                if (control.Status == "SOURCED" ? !(control.Value is string value) || string.IsNullOrWhiteSpace(value) : control.Value != null)
                    Fail("opening_control.value", "SOURCED needs a string value; unconfirmed control must stay null");
                if (control.Value is string controller)
                {
                    try { PopulationText.ValidateText(controller); }
                    catch (ArgumentException) { Fail("opening_control.value", "must be well-formed UTF-16"); }
                }
                if (row.MemberHexes.Count == 0) Fail("member_hexes", "must not be empty");
                var ownHexes = new HashSet<(int, int)>();
                for (int i = 0; i < row.MemberHexes.Count; i++)
                {
                    var h = row.MemberHexes[i]; var field = FormattableString.Invariant($"member_hexes[{i}]");
                    if (!h.Q.HasValue || !h.R.HasValue) Fail(field, "q and r are required integers");
                    var key = (h.Q!.Value, h.R!.Value);
                    if (!ownHexes.Add(key)) Fail(field, "duplicate member hex");
                    if (c.HexAt(key.Item1, key.Item2)?.Land != true) Fail(field, "must name an existing land hex");
                    if (result.hexes.ContainsKey(key)) Fail(field, "hex already belongs to another region");
                }
                // #1691 Q4 (owner, 2 November 2026: "Approve as drafted (Recommended)"): optional, data only; nothing reads
                // them in play yet. A field hex is a land hex of this region's own member_hexes, so (hex ownership being
                // exclusive) no field hex is in two regions. Control on 15 May is checked by tools/tests (the sim never
                // reads the control layer).
                var ownFields = new HashSet<(int, int)>();
                for (int i = 0; i < row.FieldHexes.Count; i++)
                {
                    var h = row.FieldHexes[i]; var field = FormattableString.Invariant($"field_hexes[{i}]");
                    if (!h.Q.HasValue || !h.R.HasValue) Fail(field, "q and r are required integers");
                    var key = (h.Q!.Value, h.R!.Value);
                    if (c.HexAt(key.Item1, key.Item2)?.Land != true) Fail(field, "must name an existing land hex");
                    if (!ownHexes.Contains(key)) Fail(field, "must be one of the region's member_hexes");
                    if (!ownFields.Add(key)) Fail(field, "duplicate field hex");
                }
                if (row.MemberLocations.Count == 0) Fail("member_locations", "must not be empty");
                var ownPlaces = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < row.MemberLocations.Count; i++)
                {
                    var id = row.MemberLocations[i]; var field = FormattableString.Invariant($"member_locations[{i}]");
                    if (string.IsNullOrWhiteSpace(id) || !placements.TryGetValue(id, out var hex)) { Fail(field, "unknown place id"); continue; }
                    if (conflicts.Contains(id)) Fail(field, "one place id has inconsistent placements across the place files");
                    if (!ownPlaces.Add(id)) Fail(field, "duplicate member location");
                    if (result.places.ContainsKey(id)) Fail(field, "location already belongs to another region");
                    if (!ownHexes.Contains(hex)) Fail(field, "effective location lies outside member_hexes");
                }
                // R-POP-004 (#1717): a field's host is data only, under exact gdd-0.3: since R-ENEMY-007 (#2543, "Both rulesets
                // (Recommended)") gdd-0.1 reads this file for its geography, but it reads no host (mvp.md: "the legacy and
                // gdd-0.1 identities read none of this"). F01 to F20 are the fields' places in the file's order, so a typed label
                // cannot move the permission below. A row with no host keys has no host (synthetic regions).
                var readsHosts = c.Ruleset == SaveGame.Gdd03Ruleset;
                for (int i = 0; i < row.FieldHexes.Count && readsHosts; i++)
                {
                    var h = row.FieldHexes[i]; var field = FormattableString.Invariant($"field_hexes[{i}]");
                    var label = FormattableString.Invariant($"F{++fieldNumber:00}");
                    if (h.Field.Length + h.Host.Length + h.HostBasis.Length + h.HostSource.Length == 0) continue;
                    if (h.Field != label) Fail(field + ".field", $"must be {label}, this field's place in the file's order");
                    if (!HostBases.Contains(h.HostBasis)) Fail(field + ".host_basis", $"{label}: unknown basis {h.HostBasis}");
                    if (string.IsNullOrWhiteSpace(h.HostSource)) Fail(field + ".host_source", $"{label}: explicit provenance required");
                    if (census.Contains(h.Host))
                    {
                        if (!ownPlaces.Contains(h.Host)) Fail(field + ".host", $"{label}: census city {h.Host} is outside this field's region {row.Id}");
                    }
                    else if (!SettlementHostFields.Contains(label)) Fail(field + ".host", $"{label}: a settlement may host only for F19 and F20, not {h.Host}");
                    else if (!ownPlaces.Contains(h.Host)) Fail(field + ".host", $"{label}: {h.Host} is not a member location of {row.Id}");
                }
                if (string.IsNullOrWhiteSpace(row.CapitalLocation) || !ownPlaces.Contains(row.CapitalLocation)) Fail("capital_location", "must name a member location on a member hex");
                var frozen = new PlayerRegionGeography(row, readsHosts, placements[row.CapitalLocation]);
                result.regions.Add(row.Id, frozen); result.capitals.Add(row.CapitalLocation, row.Id);
                foreach (var h in ownHexes) result.hexes.Add(h, row.Id);
                foreach (var p in ownPlaces) result.places.Add(p, row.Id);
            }
            return result;
        }
    }

    /// <summary>Shared fixture boundary; production initialization, relocation and job commands have separate owners.</summary>
    public static class PlayerRegions
    {
        internal static GameState CreateFixture(Content c, IEnumerable<PlayerRegionPool> starts,
            IEnumerable<PopulationInventoryRow> people, IEnumerable<PopulationTarget> targets,
            IEnumerable<string> captors, IEnumerable<string> qualifications)
        {
            if (c.Ruleset != SaveGame.Gdd03Ruleset) throw new InvalidOperationException("Regional fixtures require gdd-0.3.");
            var pools = new SortedDictionary<string, PlayerRegionPool>(StringComparer.Ordinal);
            var facilities = new HashSet<string>(StringComparer.Ordinal);
            var locations = new List<PopulationLocation>();
            foreach (var r in c.PlayerRegions.Regions.Values)
                foreach (var h in r.MemberHexes) locations.Add(new PopulationLocation(r.Id, h.Q, h.R));
            foreach (var pool in starts)
            {
                if (!c.PlayerRegions.Regions.TryGetValue(pool.Id, out var region)) throw new ArgumentException("Unknown starting region.");
                pools.Add(pool.Id, pool);
                foreach (var f in pool.Facilities)
                {
                    if (!facilities.Add(f.Id) || !region.MemberLocations.Contains(f.Location))
                        throw new ArgumentException("Facility must have one owning region and a member location.");
                    // A dedicated facility is an explicit ledger address, never an extra population pool.
                    locations.Add(new PopulationLocation(region.Id, facilityOrPort: f.Id));
                }
            }
            if (!pools.Keys.SequenceEqual(c.PlayerRegions.Regions.Keys)) throw new ArgumentException("Every fixture region needs an explicit start.");
            var bindings = new PopulationBindings(locations, targets, captors, qualifications);
            var state = PopulationLedger.CreateFixture(c, bindings, people);
            state.InitializePlayerRegions(c.PlayerRegions, pools);
            return state;
        }

        /// <summary>All own people currently located here, not ordinary-housing occupancy or civilian eligibility.</summary>
        public static int PresentPopulation(GameState state, string region)
        {
            if (!state.PlayerRegions.ContainsKey(region)) throw new ArgumentException("Unknown initialized region.");
            return state.PopulationLedger.Units.Count(p => p.Location?.Region == region);
        }

        public static int Assigned(GameState state, string region, PopulationAssignmentKind kind)
        {
            if (!state.PlayerRegions.ContainsKey(region)) throw new ArgumentException("Unknown initialized region.");
            return state.PopulationLedger.Units.Count(p => p.Location?.Region == region && p.Assignment.Kind == kind);
        }
    }
}
