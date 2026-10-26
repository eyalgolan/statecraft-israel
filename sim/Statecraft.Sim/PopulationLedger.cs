using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    public sealed class PopulationTarget
    {
        public string Id { get; }
        public PopulationAssignmentKind Kind { get; }
        public string Region { get; }
        public int? MaxProgressTicks { get; }
        public PopulationTarget(string id, PopulationAssignmentKind kind, string region, int? maxProgressTicks = null)
        {
            PopulationText.Assignment(kind);
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(region) || kind == PopulationAssignmentKind.Unassigned
                || maxProgressTicks < 0 || (kind == PopulationAssignmentKind.Course) != maxProgressTicks.HasValue)
                throw new ArgumentException("Targets need explicit kind/region; courses need an explicit progress limit.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(region);
            Id = id; Kind = kind; Region = region; MaxProgressTicks = maxProgressTicks;
        }
        internal string Canonical() => PopulationText.Fields(Id, PopulationText.Assignment(Kind), Region, MaxProgressTicks);
    }

    /// <summary>Frozen explicit reference inventory. No guesses about regional hex ownership, facilities or qualifications.
    /// Foundation fixtures supply these bindings; production authoring remains held.</summary>
    public sealed class PopulationBindings
    {
        readonly PopulationLocation[] locations;
        readonly SortedDictionary<string, PopulationTarget> targets;
        readonly SortedSet<string> captors, qualifications;
        public PopulationBindings(IEnumerable<PopulationLocation> locations, IEnumerable<PopulationTarget> targets,
            IEnumerable<string> captors, IEnumerable<string> qualifications)
        {
            this.locations = locations.OrderBy(x => x.Canonical(), StringComparer.Ordinal).ToArray();
            if (this.locations.Select(x => x.Canonical()).Distinct(StringComparer.Ordinal).Count() != this.locations.Length)
                throw new ArgumentException("Duplicate location binding.");
            // A hex or facility cannot silently refer to two different regions.
            if (this.locations.Where(x => x.Q != null).GroupBy(x => PopulationText.Fields(x.Q, x.R))
                .Any(g => g.Select(x => x.Region).Distinct(StringComparer.Ordinal).Count() != 1)
                || this.locations.Where(x => x.FacilityOrPort != null).GroupBy(x => x.FacilityOrPort, StringComparer.Ordinal)
                .Any(g => g.Select(x => x.Canonical()).Distinct(StringComparer.Ordinal).Count() != 1))
                throw new ArgumentException("Inconsistent region/hex/facility bindings.");
            this.targets = new SortedDictionary<string, PopulationTarget>(StringComparer.Ordinal);
            foreach (var t in targets)
            {
                if (!this.locations.Any(l => l.Region == t.Region)) throw new ArgumentException("Unknown target region.");
                this.targets.Add(t.Id, t);
            }
            this.captors = Names(captors); this.qualifications = Names(qualifications);
        }
        static SortedSet<string> Names(IEnumerable<string> values)
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                PopulationText.ValidateText(value);
                if (string.IsNullOrWhiteSpace(value) || !result.Add(value)) throw new ArgumentException("Duplicate or empty reference.");
            }
            return result;
        }
        internal void ValidateLocation(PopulationLocation location)
        {
            if (!locations.Any(x => x.Canonical() == location.Canonical())) throw new ArgumentException("Unknown or inconsistent location.");
        }
        internal PopulationTarget Target(PopulationAssignment assignment)
        {
            if (assignment.Target == null || !targets.TryGetValue(assignment.Target, out var t)
                || (t.Kind != assignment.Kind && !(t.Kind == PopulationAssignmentKind.Course && assignment.Kind == PopulationAssignmentKind.Teaching)))
                throw new ArgumentException("Unknown or incompatible assignment target.");
            return t;
        }
        internal bool HasCaptor(string captor) => captors.Contains(captor);
        internal bool HasQualification(string qualification) => qualification == "" || qualifications.Contains(qualification);
        internal IEnumerable<string> Canonical()
        {
            foreach (var l in locations) yield return PopulationText.Fields("location", l.Canonical());
            foreach (var t in targets.Values) yield return PopulationText.Fields("target", t.Canonical());
            foreach (var c in captors) yield return PopulationText.Fields("captor", c);
            foreach (var q in qualifications) yield return PopulationText.Fields("qualification", q);
        }
    }

    /// <summary>Visibly synthetic fixture input. Template ids are ignored; source key plus within-row ordinal determines order.</summary>
    internal sealed class PopulationInventoryRow
    {
        internal string Source { get; }
        internal int Ordinal { get; }
        internal PopulationUnit Template { get; }
        internal PopulationInventoryRow(string source, int ordinal, PopulationUnit template)
        {
            if (string.IsNullOrWhiteSpace(source) || ordinal < 0) throw new ArgumentException("Explicit source and nonnegative ordinal required.");
            PopulationText.ValidateText(source);
            Source = source; Ordinal = ordinal; Template = template ?? throw new ArgumentNullException(nameof(template));
        }
    }

    /// <summary>R-POP-001/002/003. Sole own-person authority for initialized gdd-0.3 fixtures.
    /// Mutations validate a candidate map before committing. No resources, causes or RNG are touched here.</summary>
    public sealed class PopulationLedger
    {
        readonly bool enabled;
        SortedDictionary<int, PopulationUnit> units = new SortedDictionary<int, PopulationUnit>();
        SortedDictionary<string, int> sources = new SortedDictionary<string, int>(StringComparer.Ordinal);
        SortedDictionary<int, string> losses = new SortedDictionary<int, string>();
        PopulationBindings? bindings;
        int receptionLimit;
        int peoplePerUnit;
        public bool IsInitialized { get; private set; }
        public int NextPopulationId { get; private set; }
        public double TotalPeople => (double)units.Count * peoplePerUnit;
        /// <summary>Reconcile exact ids, not just totals that could hide substitution. No mutation.</summary>
        public void Reconcile()
        {
            RequireInitialized();
            var created = new HashSet<int>(sources.Values);
            if (created.Count != sources.Count || losses.Keys.Any(id => !created.Contains(id))
                || !new HashSet<int>(created.Except(losses.Keys)).SetEquals(units.Keys)
                || created.Any(id => id < 0 || id >= NextPopulationId))
                throw new InvalidOperationException("Population id conservation failed.");
            Validate(units);
        }
        // A snapshot wrapper prevents casts back to the writable container; records are immutable.
        public IReadOnlyList<PopulationUnit> Units => new ReadOnlyCollection<PopulationUnit>(units.Values.ToList());
        internal PopulationLedger(bool enabled) { this.enabled = enabled; }
        internal PopulationLedger Clone()
        {
            var c = (PopulationLedger)MemberwiseClone();
            c.units = new SortedDictionary<int, PopulationUnit>(units);
            c.sources = new SortedDictionary<string, int>(sources, StringComparer.Ordinal);
            c.losses = new SortedDictionary<int, string>(losses);
            return c;
        }
        // R-POP-002: refuse before old producers/consumers can mutate resources, causes or a second population.
        internal static void RefuseAggregate(GameState state)
        {
            if (state.PopulationLedger.IsInitialized)
                throw new NotSupportedException("Population-ledger fixtures cannot run aggregate systems; gdd-0.3 integration is held.");
        }
        void RequireInitialized()
        {
            if (!enabled || !IsInitialized) throw new InvalidOperationException("Population ledger mutations require an initialized gdd-0.3 fixture.");
        }
        public PopulationUnit Get(int id) => units.TryGetValue(id, out var p) ? p : throw new ArgumentException("Unknown population id.", nameof(id));
        public int Count(PopulationStatus status) => Count(status, null);
        public int Count(PopulationStatus status, string? region)
        {
            PopulationText.Status(status);
            // Captives have no invented location; retained home region is their regional accounting context.
            return units.Values.Count(p => p.Status == status && (region == null || (p.Location ?? p.Home)?.Region == region));
        }
        public void Move(int id, PopulationLocation location)
        {
            RequireInitialized(); var p = Get(id);
            Replace(p.WithPlacement(p.Status, location, p.Assignment, p.Captor, p.Home));
        }
        /// <summary>A second nonempty assignment must first be released explicitly. Transition is the atomic status/location path.</summary>
        public void ChangeAssignment(int id, PopulationAssignment assignment)
        {
            RequireInitialized(); var p = Get(id);
            if (p.Assignment.Kind != PopulationAssignmentKind.Unassigned && assignment.Kind != PopulationAssignmentKind.Unassigned)
                throw new ArgumentException("Release the current assignment first.");
            Replace(p.WithPlacement(p.Status, p.Location, assignment, p.Captor, p.Home));
        }
        public void Transition(int id, PopulationStatus status, PopulationLocation? location, PopulationAssignment assignment,
            string? captor = null, PopulationLocation? home = null)
        {
            RequireInitialized(); var p = Get(id);
            Replace(p.WithPlacement(status, location, assignment, captor, home ?? p.Home));
        }
        /// <summary>Explicit history/progress/equipment update contract. No implicit payments, refunds or XP are awarded.</summary>
        public void Replace(PopulationUnit replacement)
        {
            RequireInitialized(); Get(replacement.Id);
            var candidate = new SortedDictionary<int, PopulationUnit>(units) { [replacement.Id] = replacement };
            Validate(candidate); units = candidate;
        }
        /// <summary>A manifest item can arrive only once, including after its population id is lost.</summary>
        public int RegisterArrival(string source, int ordinal, PopulationUnit template)
        {
            RequireInitialized();
            if (string.IsNullOrWhiteSpace(source) || ordinal < 0) throw new ArgumentException("Explicit source and nonnegative ordinal required.");
            PopulationText.ValidateText(source);
            var key = PopulationText.Fields(source, ordinal);
            if (sources.ContainsKey(key)) throw new ArgumentException("Source item already registered.");
            // int.MaxValue is the exhausted next-id sentinel; never overflow after partially inserting a record.
            var next = checked(NextPopulationId + 1);
            var id = NextPopulationId;
            var candidate = new SortedDictionary<int, PopulationUnit>(units) { [id] = template.WithId(id) };
            Validate(candidate);
            units = candidate; sources.Add(key, id); NextPopulationId = next;
            return id;
        }
        public void RegisterCombatLoss(int id, string source)
        {
            RequireInitialized(); Get(id);
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Explicit combat-loss provenance required.");
            PopulationText.ValidateText(source);
            var candidate = new SortedDictionary<int, PopulationUnit>(units); candidate.Remove(id);
            Validate(candidate); units = candidate; losses.Add(id, source);
        }
        void Validate(SortedDictionary<int, PopulationUnit> candidate)
        {
            var issued = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in candidate.Values)
            {
                if (p.ReceptionTicks > receptionLimit || !bindings!.HasQualification(p.Qualification))
                    throw new ArgumentException("Invalid reception progress or qualification.");
                var tierValid = p.Profession switch {
                    CivilianProfession.None => p.Tier == CivilianTier.None && p.WorkXp == 0,
                    CivilianProfession.Farmer or CivilianProfession.Pioneer => p.Tier == CivilianTier.Novice || p.Tier == CivilianTier.Trained || p.Tier == CivilianTier.Veteran,
                    CivilianProfession.Craftsman => p.Tier == CivilianTier.Apprentice || p.Tier == CivilianTier.Expert,
                    _ => false };
                if (!tierValid) throw new ArgumentException("Tier does not belong to the civilian profession.");
                if (p.Home != null) bindings.ValidateLocation(p.Home);
                if (p.Status == PopulationStatus.Captured)
                {
                    if (p.Location != null || p.Home == null || p.Captor == null || !bindings.HasCaptor(p.Captor)
                        || p.Assignment.Kind != PopulationAssignmentKind.Unassigned)
                        throw new ArgumentException("Captives need captor/home context and no local assignment/location.");
                }
                else
                {
                    if (p.Location == null || p.Captor != null) throw new ArgumentException("Noncaptives need a location and no captor.");
                    bindings.ValidateLocation(p.Location);
                }
                var kind = p.Assignment.Kind;
                var compatible = p.Status switch {
                    PopulationStatus.Refugee => kind == PopulationAssignmentKind.Unassigned || kind == PopulationAssignmentKind.Reception,
                    PopulationStatus.FreeCivilian => kind == PopulationAssignmentKind.Unassigned || kind == PopulationAssignmentKind.Work || kind == PopulationAssignmentKind.Teaching || kind == PopulationAssignmentKind.Reception,
                    PopulationStatus.CivilianInTransit => kind == PopulationAssignmentKind.Travel,
                    PopulationStatus.Trainee => kind == PopulationAssignmentKind.Course,
                    PopulationStatus.ActiveSoldier => kind == PopulationAssignmentKind.ActiveService,
                    PopulationStatus.Captured => kind == PopulationAssignmentKind.Unassigned,
                    _ => false };
                if (!compatible) throw new ArgumentException("Status and assignment are incompatible.");
                if (kind != PopulationAssignmentKind.Unassigned)
                {
                    var target = bindings.Target(p.Assignment);
                    if (target.Region != p.Location!.Region || (kind == PopulationAssignmentKind.Course && p.Assignment.ProgressTicks > target.MaxProgressTicks))
                        throw new ArgumentException("Assignment region/progress is inconsistent.");
                    if (p.Assignment.TeacherId is int teacherId && (teacherId == p.Id || !candidate.TryGetValue(teacherId, out var teacher)
                        || teacher.Assignment.Kind != PopulationAssignmentKind.Teaching || teacher.Assignment.Target != p.Assignment.Target))
                        throw new ArgumentException("Course teacher reference must resolve to a teacher of the same course.");
                }
                foreach (var item in new[] { p.Equipment.ToolsId, p.Equipment.MilitaryId })
                    if (item != null && !issued.Add(item)) throw new ArgumentException("Issued equipment cannot have two owners.");
            }
        }
        /// <summary>No production caller or content file opts into initialization. Does not invoke old population producers.</summary>
        internal static GameState CreateFixture(Content c, PopulationBindings bindings, IEnumerable<PopulationInventoryRow> rows, int nextPopulationId = 0)
        {
            if (!c.UsesPopulationLedger) throw new InvalidOperationException("Population fixtures require gdd-0.3 identity.");
            if (nextPopulationId < 0) throw new ArgumentOutOfRangeException(nameof(nextPopulationId));
            var s = new GameState(c); var l = s.PopulationLedger;
            l.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            l.peoplePerUnit = c.PopulationPeoplePerUnit;
            l.receptionLimit = c.RequirePopulationInteger("reception.turns");
            l.NextPopulationId = nextPopulationId;
            // Build all records before validating references: teachers may occur after their pupils in authored order.
            foreach (var row in rows.OrderBy(x => x.Source, StringComparer.Ordinal).ThenBy(x => x.Ordinal))
            {
                var next = checked(l.NextPopulationId + 1);
                l.sources.Add(PopulationText.Fields(row.Source, row.Ordinal), l.NextPopulationId);
                l.units.Add(l.NextPopulationId, row.Template.WithId(l.NextPopulationId)); l.NextPopulationId = next;
            }
            l.Validate(l.units); l.IsInitialized = true;
            return s;
        }
        internal IEnumerable<string> Canonical()
        {
            yield return PopulationText.Fields("initialized", IsInitialized, "next", NextPopulationId,
                "people-per-unit", peoplePerUnit, "reception-limit", receptionLimit);
            if (bindings != null) foreach (var b in bindings.Canonical()) yield return b;
            foreach (var p in units.Values) yield return PopulationText.Fields("unit", p.Canonical());
            foreach (var source in sources) yield return PopulationText.Fields("source", source.Key, source.Value);
            foreach (var loss in losses) yield return PopulationText.Fields("loss", loss.Key, loss.Value);
        }
    }
}
