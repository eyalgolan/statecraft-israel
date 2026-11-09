using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        /// <summary>R-POP-002: a location's city must be a member location of its region in regions.yaml (AtLocation), and a
        /// place where own people live by R-POP-004's rulings: a census city (cities.yaml, R-CITY-006) or a field host of that
        /// region (field_hexes host; owner, 7 November 2026, #1689: "These two only (Recommended)"). Any other member, a
        /// locality, a sites.yaml id or a map location, is refused (review of #2936, S1-1).</summary>
        internal void ValidateCities(Content c)
        {
            foreach (var l in locations)
            {
                if (l.City == null) continue;
                var region = c.PlayerRegions.AtLocation(l.City);
                if (region?.Id != l.Region)
                    throw new ArgumentException("City " + l.City + " is not in region " + l.Region + " in regions.yaml (R-POP-002).");
                if (!c.Cities.Any(x => x.Id == l.City) && !region.FieldHosts.Any(h => h.Host == l.City))
                    throw new ArgumentException("City " + l.City + " of region " + l.Region
                        + " is neither a census city of cities.yaml nor a field host in regions.yaml (R-POP-002).");
            }
        }
        /// <summary>#1725 PR 7, producer 12 (owner, 8 November 2026: "Start + runtime bind (Rec.)"): these bindings plus
        /// <paramref name="added"/>, a new instance (bindings stay immutable). The constructor's checks apply: a target id
        /// already bound, or a target whose region has no bound location, is refused.</summary>
        internal PopulationBindings WithTargets(IEnumerable<PopulationTarget> added) =>
            new PopulationBindings(locations, targets.Values.Concat(added), captors, qualifications);
        /// <summary>The bound target <paramref name="id"/>, or null.</summary>
        internal PopulationTarget? FindTarget(string id) => targets.TryGetValue(id, out var t) ? t : null;
        /// <summary>The bound assignment targets, in ordinal id order (#1725 PR 7, the live month snapshot reads them).</summary>
        internal IEnumerable<PopulationTarget> Targets => targets.Values;
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
        internal int ReceptionLimit { get { RequireInitialized(); return receptionLimit; } }
        int peoplePerUnit;
        /// <summary>People one record stands for in this ledger (CreateFixture's scale).</summary>
        internal int PeoplePerUnit { get { RequireInitialized(); return peoplePerUnit; } }
        /// <summary>#1725 PR 7, natural increase (R-CITY-008; owner, 8 November 2026: "Carry the fraction (Recommended)"): the
        /// records' worth of natural increase owed but not yet a whole record, carried from turn to turn: a double, as the
        /// aggregate natural increase is, written as its IEEE bits (docs/determinism.md's carry rule).</summary>
        internal double NaturalIncreaseCarry { get; private set; }
        /// <summary>The natural-increase records added so far: their split over the cities is Cities.Apportion of this count.</summary>
        internal int NaturalIncreaseRecords { get; private set; }
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
        /// <summary>Initialized with mode Historical (the R-POP-004 start's scale). Not written to the canonical text: the
        /// scale it implies already is.</summary>
        internal bool IsHistorical { get; private set; }
        /// <summary>The ledger's frozen bindings; an uninitialized ledger is refused.</summary>
        internal PopulationBindings Bindings { get { RequireInitialized(); return bindings!; } }
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
        /// <summary>#1725 PR 5 (review S5-3): Pools.Init and StartingForces.Init read an initialized ledger only in the dark
        /// start's shape, a Historical one (R-POP-004); a synthetic or Open ledger is refused as by <see cref="RefuseAggregate"/>.</summary>
        internal static void RefuseUnlessHistorical(GameState state)
        {
            if (!state.PopulationLedger.IsHistorical) RefuseAggregate(state);
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
        /// <summary>#1710 (#1725 PR 7), R-POW-005, R-POP-002: the active soldiers <paramref name="ids"/> surrender to
        /// <paramref name="captor"/>, all or nothing. Each becomes Captured with that captor, no location and no assignment, its
        /// Home the place it served in (the formation's region; owner, 8 November 2026: "Formation's region (Recommended)"),
        /// and keeps its id, history and issued item (owner: "Stays on record (Recommended)"). Every record is checked and the
        /// candidate validated before any is committed, so a refusal changes nothing. Nobody is lost: no loss is written.</summary>
        internal void Capture(IReadOnlyList<int> ids, string captor)
        {
            RequireInitialized();
            if (captor == null || !bindings!.HasCaptor(captor))
                throw new ArgumentException("Side '" + captor + "' is not a bound captor of this ledger (R-POP-002).");
            var candidate = new SortedDictionary<int, PopulationUnit>(units);
            var seen = new HashSet<int>();
            foreach (var id in ids)
            {
                var p = Get(id);
                if (p.Status != PopulationStatus.ActiveSoldier || !seen.Add(id))
                    throw new ArgumentException("Only an active soldier can surrender, once (R-POW-005).");
                candidate[id] = p.WithPlacement(PopulationStatus.Captured, null, PopulationAssignment.Unassigned, captor, p.Location);
            }
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
        /// <summary>Whether the manifest item <paramref name="source"/>, <paramref name="ordinal"/> has arrived, even if its
        /// population id was lost since.</summary>
        internal bool HasArrived(string source, int ordinal) => sources.ContainsKey(PopulationText.Fields(source, ordinal));
        /// <summary>The population id the manifest item <paramref name="source"/>, <paramref name="ordinal"/> was given, even if
        /// it was lost since; null if the item never arrived.</summary>
        internal int? IdOf(string source, int ordinal) => sources.TryGetValue(PopulationText.Fields(source, ordinal), out var id) ? id : (int?)null;
        /// <summary><see cref="RegisterArrival"/> for several items, in order, all or nothing (#1725 PR 7, Cities.Arrive): every
        /// record is validated in one candidate before any is committed, so a refused item adds none of the others.</summary>
        internal IReadOnlyList<int> RegisterArrivals(IReadOnlyList<PopulationInventoryRow> rows)
        {
            RequireInitialized();
            var candidate = new SortedDictionary<int, PopulationUnit>(units);
            var registered = new SortedDictionary<string, int>(sources, StringComparer.Ordinal);
            var next = NextPopulationId;
            var ids = new List<int>();
            foreach (var row in rows)
            {
                var key = PopulationText.Fields(row.Source, row.Ordinal);
                if (registered.ContainsKey(key)) throw new ArgumentException("Source item already registered.");
                // int.MaxValue is the exhausted next-id sentinel; never overflow after partially inserting a record.
                var id = next; next = checked(next + 1);
                candidate[id] = row.Template.WithId(id); registered.Add(key, id); ids.Add(id);
            }
            Validate(candidate);
            units = candidate; sources = registered; NextPopulationId = next;
            return ids.AsReadOnly();
        }
        /// <summary>#1725 PR 7, natural increase: registers <paramref name="rows"/> as <see cref="RegisterArrivals"/> does, all or
        /// nothing, then counts them in <see cref="NaturalIncreaseRecords"/> and keeps <paramref name="carry"/>, the fraction of a
        /// record still owed (0 up to 1).</summary>
        internal void RegisterNaturalIncrease(IReadOnlyList<PopulationInventoryRow> rows, double carry)
        {
            RequireInitialized();
            if (!(carry >= 0 && carry < 1)) throw new ArgumentOutOfRangeException(nameof(carry), "The carried natural increase is a fraction of one record.");
            var records = checked(NaturalIncreaseRecords + rows.Count);
            RegisterArrivals(rows);
            NaturalIncreaseCarry = carry; NaturalIncreaseRecords = records;
        }
        /// <summary>The bound target <paramref name="id"/>, or null (#1725 PR 7, producer 12).</summary>
        internal PopulationTarget? Target(string id) { RequireInitialized(); return bindings!.FindTarget(id); }
        /// <summary>#1725 PR 7, producer 12. Owner, 8 November 2026, "Start + runtime bind (Rec.)" (#1725 issuecomment-6724941867): the start
        /// facilities' Course targets are bound at the start, plus one all-or-nothing runtime bind for new formations and
        /// built camps. Binds <paramref name="target"/>, refusing an id already bound, with the ledger unchanged.</summary>
        internal void BindTarget(PopulationTarget target) => Commit(Array.Empty<PopulationUnit>(), new[] { target });
        /// <summary>#1725 PR 7, producer 12: several record replacements and new targets, all or nothing. The new bindings and
        /// every replaced record are validated as one candidate before anything is committed, so a refused record or target
        /// changes neither the records nor the bindings. Each record is replaced at most once, and must exist.</summary>
        internal void Commit(IReadOnlyCollection<PopulationUnit> replacements, IReadOnlyCollection<PopulationTarget> bind)
        {
            RequireInitialized();
            var b = bind.Count == 0 ? bindings! : bindings!.WithTargets(bind);
            var candidate = new SortedDictionary<int, PopulationUnit>(units);
            var seen = new HashSet<int>();
            foreach (var r in replacements)
            {
                Get(r.Id);
                if (!seen.Add(r.Id)) throw new ArgumentException("A record is replaced at most once in a commit.");
                candidate[r.Id] = r;
            }
            Validate(candidate, b);
            units = candidate; bindings = b;
        }
        public void RegisterCombatLoss(int id, string source)
        {
            RequireInitialized(); Get(id);
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Explicit combat-loss provenance required.");
            PopulationText.ValidateText(source);
            var candidate = new SortedDictionary<int, PopulationUnit>(units); candidate.Remove(id);
            Validate(candidate); units = candidate; losses.Add(id, source);
        }
        void Validate(SortedDictionary<int, PopulationUnit> candidate) => Validate(candidate, bindings!);
        void Validate(SortedDictionary<int, PopulationUnit> candidate, PopulationBindings bindings)
        {
            var issued = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in candidate.Values)
            {
                if (p.ReceptionTicks > receptionLimit || !bindings.HasQualification(p.Qualification))
                    throw new ArgumentException("Invalid reception progress or qualification.");
                var tierValid = p.Profession switch {
                    CivilianProfession.None => p.Tier == CivilianTier.None && p.WorkXp == 0,
                    CivilianProfession.Farmer or CivilianProfession.Pioneer => p.Tier == CivilianTier.Novice || p.Tier == CivilianTier.Trained || p.Tier == CivilianTier.Veteran,
                    // R-CRAFT-003 (#1698 follow-up): a novice craftsman from the course start. Valid outside a course too,
                    // a maintainer reading of Q55 ("workshop work by a novice craftsman"), not an owner ruling.
                    CivilianProfession.Craftsman => p.Tier == CivilianTier.Novice || p.Tier == CivilianTier.Apprentice || p.Tier == CivilianTier.Expert,
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
        /// <summary>No production caller or content file opts into initialization. Does not invoke old population producers.
        /// The scale: a synthetic fixture (no <paramref name="mode"/>) and an Open one read the gdd-0.3 catalogue's
        /// population.people_per_unit (10,000, Open mode's); a Historical one reads rules.yaml's people_per_unit for the
        /// start day (1,000), as StartingForces and Cities do (R-POP-004; owner, 1 November 2026, D2). Census and units_1948
        /// rows are the Historical start's, so they need mode Historical; the scale has no fallback (R-POP-001).</summary>
        internal static GameState CreateFixture(Content c, PopulationBindings bindings, IEnumerable<PopulationInventoryRow> rows, int nextPopulationId = 0, Mode? mode = null)
        {
            if (!c.UsesPopulationLedger) throw new InvalidOperationException("Population fixtures require gdd-0.3 identity.");
            if (nextPopulationId < 0) throw new ArgumentOutOfRangeException(nameof(nextPopulationId));
            var s = new GameState(c);
            if (mode is Mode m) s.Mode = m;
            s.PopulationLedger.Initialize(c, bindings, rows, nextPopulationId, mode);
            return s;
        }

        /// <summary>#1725 PR 5: <see cref="CreateFixture"/>'s initialization, in place on a state's own ledger, so the dark
        /// start (Simulation.NewGame's internal overload) can initialize the ledger of the game it is building. Same
        /// checks and scale rule as CreateFixture. All or nothing: it builds and validates a candidate ledger and copies it
        /// in only when valid. Refuses a ledger that is not gdd-0.3's or is already initialized.</summary>
        internal void Initialize(Content c, PopulationBindings bindings, IEnumerable<PopulationInventoryRow> rows, int nextPopulationId, Mode? mode)
        {
            if (!c.UsesPopulationLedger || !enabled) throw new InvalidOperationException("Population fixtures require gdd-0.3 identity.");
            if (IsInitialized || units.Count != 0) throw new InvalidOperationException("The population ledger is already initialized.");
            if (nextPopulationId < 0) throw new ArgumentOutOfRangeException(nameof(nextPopulationId));
            var l = new PopulationLedger(true);
            l.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            bindings.ValidateCities(c);
            var ordered = rows.OrderBy(x => x.Source, StringComparer.Ordinal).ThenBy(x => x.Ordinal).ToList();
            if (mode != Mode.Historical && ordered.Any(x => x.Source.StartsWith(HistoricalPopulation.CensusPrefix, StringComparison.Ordinal)
                || x.Source.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal)
                || x.Source.StartsWith(HistoricalPopulation.FarmerPrefix, StringComparison.Ordinal)))
                throw new ArgumentException("Census, units_1948 and field rows are the Historical start's; their fixture needs mode Historical (R-POP-004).");
            l.peoplePerUnit = mode == Mode.Historical ? HistoricalPeoplePerUnit(c) : c.PopulationPeoplePerUnit;
            l.receptionLimit = c.RequirePopulationInteger("reception.turns");
            l.NextPopulationId = nextPopulationId;
            // Build all records before validating references: teachers may occur after their pupils in authored order.
            foreach (var row in ordered)
            {
                var next = checked(l.NextPopulationId + 1);
                l.sources.Add(PopulationText.Fields(row.Source, row.Ordinal), l.NextPopulationId);
                l.units.Add(l.NextPopulationId, row.Template.WithId(l.NextPopulationId)); l.NextPopulationId = next;
            }
            l.Validate(l.units);
            this.bindings = l.bindings; units = l.units; sources = l.sources; losses = l.losses;
            peoplePerUnit = l.peoplePerUnit; receptionLimit = l.receptionLimit; NextPopulationId = l.NextPopulationId;
            IsHistorical = mode == Mode.Historical;
            IsInitialized = true;
        }
        // R-POP-001's no-fallback rule, as for the catalogue value: a missing or nonpositive value is refused, never replaced.
        internal static int HistoricalPeoplePerUnit(Content c) => c.RulesAt(c.Rules.Start).Mvp?.PeoplePerUnit is int ppu && ppu > 0 ? ppu
            : throw new InvalidOperationException("The Historical scale needs a positive mvp.people_per_unit in rules.yaml for the start day; no fallback.");
        internal IEnumerable<string> Canonical()
        {
            yield return PopulationText.Fields("initialized", IsInitialized, "next", NextPopulationId,
                "people-per-unit", peoplePerUnit, "reception-limit", receptionLimit);
            // #1725 PR 7: written only once natural increase has run, so every ledger before it hashes as before. The carry
            // follows docs/determinism.md's carry rule: a nonzero carry is the 16 hex digits of its IEEE bits, -0.0 as 0.0
            // (as GameState's Num and GddState's segment carry write theirs), and a zero carry is omitted.
            if (NaturalIncreaseRecords != 0 || NaturalIncreaseCarry != 0)
                yield return NaturalIncreaseCarry == 0
                    ? PopulationText.Fields("natural-increase", "records", NaturalIncreaseRecords)
                    : PopulationText.Fields("natural-increase", "records", NaturalIncreaseRecords, "carry",
                        BitConverter.DoubleToInt64Bits(NaturalIncreaseCarry).ToString("X16", CultureInfo.InvariantCulture));
            if (bindings != null) foreach (var b in bindings.Canonical()) yield return b;
            foreach (var p in units.Values) yield return PopulationText.Fields("unit", p.Canonical());
            foreach (var source in sources) yield return PopulationText.Fields("source", source.Key, source.Value);
            foreach (var loss in losses) yield return PopulationText.Fields("loss", loss.Key, loss.Value);
        }
    }
}
