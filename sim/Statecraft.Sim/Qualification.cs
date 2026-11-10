using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The civilians of one city who keep a qualification (R-QUAL-001; #1511, owner ruling "Two-way, keep training",
    /// 23 October 2026): craftsmen, halutzim and veteran pioneers who went back to civilian life. Immutable; a change replaces
    /// the record. They are part of <see cref="City.Civilians"/>, so jobs, housing and training treat them as ordinary civilians
    /// (owner ruling 2). A halutz keeps the turns of service it had (owner ruling 3), one entry per group.</summary>
    public sealed class Qualified
    {
        public static readonly Qualified None = new Qualified(0, 0, Array.Empty<int>());

        public int Craftsmen { get; }
        public int Veterans { get; }
        /// <summary>The service of each qualified halutz group, ascending (so the record is the same whatever order they returned in).</summary>
        public IReadOnlyList<int> Halutzim { get; }

        public Qualified(int craftsmen, int veterans, IReadOnlyList<int> halutzim)
        {
            Craftsmen = craftsmen; Veterans = veterans; Halutzim = Array.AsReadOnly(halutzim.OrderBy(x => x).ToArray());
        }

        public int Total => Craftsmen + Veterans + Halutzim.Count;

        public Qualified WithCraftsmen(int n) => new Qualified(n, Veterans, Halutzim);
        public Qualified WithVeterans(int n) => new Qualified(Craftsmen, n, Halutzim);

        /// <summary>One more halutz group with <paramref name="service"/> turns of service.</summary>
        public Qualified AddHalutz(int service) => new Qualified(Craftsmen, Veterans, Halutzim.Append(service).OrderBy(x => x).ToArray());

        /// <summary>Without the <paramref name="n"/> halutz groups with the most service (R-QUAL-005's pick).</summary>
        public Qualified TakeHalutzim(int n) => new Qualified(Craftsmen, Veterans, Halutzim.Take(Halutzim.Count - n).ToArray());

        /// <summary>Without <paramref name="n"/> qualified people, taken in the order R-QUAL-006 sets: craftsmen, then halutz
        /// groups with the least service, then veterans.</summary>
        public Qualified Lose(int n)
        {
            var c = Math.Min(n, Craftsmen); n -= c;
            var h = Math.Min(n, Halutzim.Count); n -= h;
            return new Qualified(Craftsmen - c, Veterans - n, Halutzim.Skip(h).ToArray());
        }

        /// <summary>Without <paramref name="n"/> qualified pioneers, in R-QUAL-006's order without the craftsmen: halutz groups
        /// with the least service, then veterans (#1725 PR 7, a ledger Train order; owner, 9 November 2026, "Yes, lower it
        /// (Recommended)").</summary>
        public Qualified LosePioneers(int n)
        {
            var h = Math.Min(n, Halutzim.Count);
            return new Qualified(Craftsmen, Veterans - Math.Min(n - h, Veterans), Halutzim.Skip(h).ToArray());
        }

        /// <summary>The state hash's text: nothing at all when nobody is qualified, so a city without them hashes as before
        /// (R-LOOP-002's rule).</summary>
        public string Canonical()
        {
            var text = "";
            if (Craftsmen != 0) text += ",qcraftsmen=" + Craftsmen.ToString(CultureInfo.InvariantCulture);
            if (Halutzim.Count != 0) text += ",qhalutz=" + string.Join(":", Halutzim.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            if (Veterans != 0) text += ",qveterans=" + Veterans.ToString(CultureInfo.InvariantCulture);
            return text;
        }
    }

    /// <summary>Return to civilian life and reassignment (docs/rules/mvp.md R-QUAL-001 to R-QUAL-007; #1511). Instant, no Money:
    /// a craftsman's or a halutz's qualification is kept in the city, and a qualified civilian without a job goes back into
    /// the trained role without R-TRAIN-001's training.</summary>
    public static class Qualification
    {
        /// <summary>The refusals of the return and reassign orders and of the pioneer's return (#2707); the unit and city ones are
        /// Orders.RefusedNoUnit and Training.RefusedNoCity. The UI maps each to its own string (SimBridge.ReasonText); the English
        /// is what a Rejected list and a save carry, so it does not change.</summary>
        public const string RefusedReturnCount = "Return at least one group", RefusedReassignCount = "Reassign at least one group",
            RefusedNotPioneer = "Only a pioneer unit takes that order", RefusedPioneerBusy = "That unit is at work; stop the work first",
            RefusedNoQualification = "There is no such qualification";

        // #1725: the refusal only a Historical ledger game gives. Public for the UI's strings (#3020: SimBridge.ReasonText
        // words it in the UI's language), and not named Refused*, as only a ledger game gives it.
        /// <summary>Owner, 9 November 2026, "Refuse for now (Recommended)": ReturnCraftsmen and ReassignCraftsmen are refused
        /// with a plain reason until #2476 makes craftsmen exist on the ledger (the Historical start has none, and the craftsman
        /// course is refused until then, Training.LedgerRefusedCraftsman).</summary>
        public const string LedgerRefusedCraftsmen = "Craftsmen are not on the population ledger yet (#2476)";

        /// <summary>#1725 PR 7 (owner, 9 November 2026, "Own heading (Recommended)"): the source heading of a person record made
        /// when a pioneer unit with no linked records returns (<see cref="LedgerReturn"/>): this prefix and the city
        /// (<c>returned_pioneer_unit/&lt;city&gt;</c>, ordinals from 0), kept apart from the census, the natural increase
        /// (PopulationSystem.IncreasePrefix) and the arrivals (Cities.ArrivalPrefix), as they are a short heading and the city.</summary>
        internal const string ReturnedUnitPrefix = "returned_pioneer_unit/";

        public const string ReturnCraftsmenKind = "return_craftsmen";
        public const string ReassignCraftsmenKind = "reassign_craftsmen";
        public const string ReassignPioneersKind = "reassign_pioneers";
        /// <summary>The unit order of the pioneer's return (R-QUAL-003).</summary>
        public const string ReturnOrder = "return_civilian";

        static string Groups(int n) => n == 1 ? "1 group" : FormattableString.Invariant($"{n} groups");

        /// <summary>R-QUAL-002: <paramref name="count"/> of the city's craftsmen become qualified civilians. Refused, state
        /// untouched, for an unknown city, a count below 1 or more than the city's craftsmen.</summary>
        public static string? ReturnCraftsmen(Content c, GameState s, string city, int count, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725: on a Historical ledger the order is refused with a reason
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (count < 1) return RefusedReturnCount;
            if (s.PopulationLedger.IsHistorical) return LedgerRefusedCraftsmen;   // owner, 9 November 2026: "Refuse for now (Recommended)"
            var name = PlayerText.Settlement(c, city);
            if (count > x.Craftsmen) return FormattableString.Invariant($"{name} has {x.Craftsmen} craftsmen");
            s.Cities[city] = x.WithCraftsmen(x.Craftsmen - count).WithCounts(x.Civilians + count, x.Employed, x.Refugees)
                .WithQualified(x.Qualified.WithCraftsmen(x.Qualified.Craftsmen + count));
            why.Add(new Cause("city", city, count, $"{name}: {Groups(count)} of craftsmen returned to civilian life and keep their training", key: CauseKeys.CraftsmenReturned, args: new[] { CauseKeys.Number(count) }));
            return null;
        }

        /// <summary>R-QUAL-004: <paramref name="count"/> of the city's qualified craftsmen, without a job, become craftsmen
        /// again. Refused, state untouched, for an unknown city, a count below 1, more than the qualified craftsmen, or more than
        /// the civilians without work (<see cref="Training.Idle(GameState, City)"/>: a staffed workshop's worker or a builder is
        /// not idle, R-ARMS-005, R-CITY-011; #2730).</summary>
        public static string? ReassignCraftsmen(Content c, GameState s, string city, int count, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725: on a Historical ledger the order is refused with a reason
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (count < 1) return RefusedReassignCount;
            if (s.PopulationLedger.IsHistorical) return LedgerRefusedCraftsmen;   // owner, 9 November 2026: "Refuse for now (Recommended)"
            var name = PlayerText.Settlement(c, city);
            if (count > x.Qualified.Craftsmen) return FormattableString.Invariant($"{name} has {x.Qualified.Craftsmen} qualified craftsmen");
            var idle = Training.Idle(s, x);   // R-ARMS-005, R-CITY-011: as Train and ReassignPioneers count it
            if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees).WithCraftsmen(x.Craftsmen + count)
                .WithQualified(x.Qualified.WithCraftsmen(x.Qualified.Craftsmen - count));
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} of qualified craftsmen are craftsmen again", key: CauseKeys.CraftsmenReassigned, args: new[] { CauseKeys.Number(count) }));
            return null;
        }

        /// <summary>R-QUAL-003: an idle halutz or veteran pioneer unit leaves the map and joins the nearest city's civilians,
        /// qualified (its service kept). Null when it takes the order, else the reason; changes nothing, so the unit card can
        /// ask before it offers the order.</summary>
        public static string? CheckReturn(GameState s, int unitId)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (!Projects.IsPioneer(u.Type)) return RefusedNotPioneer;
            if (Projects.IsBusy(s, unitId)) return RefusedPioneerBusy;
            return null;
        }

        /// <summary>R-QUAL-003's order (<see cref="CheckReturn"/>'s refusals). On a Historical ledger game (#1725 PR 7) the unit's
        /// own record returns too (<see cref="LedgerReturn"/>), and the city's civilian count is the records', so only
        /// <see cref="City.Qualified"/> is written (owner, 9 November 2026, "City counts, as today (Recommended)").</summary>
        internal static string? Return(Content c, GameState s, int unitId, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the unit's record returns
            var refused = CheckReturn(s, unitId);
            if (refused != null) return refused;
            var u = s.Units[unitId];
            var city = Demobilization.Nearest(c, u.Q, u.R);
            var ledger = s.PopulationLedger.IsHistorical;
            if (ledger) LedgerReturn(c, s, u, city);
            s.Units.Remove(unitId);
            var x = s.Cities[city];
            var q = u.Type == Projects.VeteranType ? x.Qualified.WithVeterans(x.Qualified.Veterans + 1) : x.Qualified.AddHalutz(u.Service);
            s.Cities[city] = (ledger ? x : x.WithCounts(x.Civilians + 1, x.Employed, x.Refugees)).WithQualified(q);
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), 1,
                $"Unit {unitId} returned to civilian life in {PlayerText.Settlement(c, city)} and keeps its training", key: CauseKeys.PioneerReturned, args: new[] { city }));
            return null;
        }

        /// <summary>#1725 PR 7 (owner, 9 November 2026, "Add the link first (Recommended)"): R-QUAL-003 on a Historical ledger
        /// game moves exactly the record of <paramref name="u"/> (<see cref="Unit.Records"/>, one per group). "Nearest city,
        /// settled (Recommended)": it becomes a free civilian without an assignment located in <paramref name="city"/> (its
        /// region plus the city), with reception ticks = reception.turns, as Send home does; its profession, tier, work XP,
        /// qualification, combat XP and items stay. "City counts, as today (Recommended)": the service is the caller's, in
        /// <see cref="City.Qualified"/>. One validated commit; a linked unit without its one free pioneer record is a broken
        /// invariant, not a refusal. A unit with no records (one an event grants) makes a new one: see <see cref="NewRecord"/>.</summary>
        static void LedgerReturn(Content c, GameState s, Unit u, string city)
        {
            if (u.Records == null) { NewRecord(c, s, u, city); return; }
            var ids = u.Records;
            if (ids.Count != 1)
                throw new InvalidOperationException("Unit " + u.Id.ToString(CultureInfo.InvariantCulture) + " is one group but has " + ids.Count.ToString(CultureInfo.InvariantCulture) + " records.");
            var region = c.PlayerRegions.AtLocation(city)?.Id
                ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");
            var location = new PopulationLocation(region, city: city);
            var received = c.RequirePopulationInteger("reception.turns");
            // Following "Yes, keep the link (Recommended)" (a returned veteran record counts for the veteran track): a veteran
            // pioneer unit's record returns a Veteran pioneer, as a new record from one does ("Beginner; veteran stays
            // (Recommended)"), so ReassignPioneers finds it on the veteran track.
            var veteran = u.Type == Projects.VeteranType;
            var records = ids.Select(id =>
            {
                var p = s.PopulationLedger.Get(id);
                if (!IsFreePioneer(p))
                    throw new InvalidOperationException("Unit " + u.Id.ToString(CultureInfo.InvariantCulture) + "'s record " + id.ToString(CultureInfo.InvariantCulture) + " is not a free pioneer.");
                return new PopulationUnit(p.Id, PopulationStatus.FreeCivilian, received, location, PopulationAssignment.Unassigned,
                    p.Profession, veteran ? CivilianTier.Veteran : p.Tier, p.WorkXp, p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home);
            }).ToList();
            s.PopulationLedger.Commit(records, Array.Empty<PopulationTarget>());
        }

        /// <summary>Owner, 9 November 2026, "Make new records": a returned pioneer unit with no linked records makes "One per unit
        /// (Recommended)" new person record, registered as an arrival is under its "Own heading (Recommended)",
        /// <see cref="ReturnedUnitPrefix"/> and the city, at the heading's next unused ordinal, so it counts as a new person
        /// ("Yes, new people (Recommended)") and Reconcile holds. "Beginner; veteran stays (Recommended)": a halutz gives a
        /// Novice pioneer with 0 work XP, a veteran pioneer a Veteran pioneer. It lands as other returned pioneers do ("Nearest
        /// city, settled (Recommended)"): a free civilian without an assignment in <paramref name="city"/>, reception ticks =
        /// reception.turns; like a census civilian a halutz graduate came from, it has no qualification, combat XP or item.</summary>
        static void NewRecord(Content c, GameState s, Unit u, string city)
        {
            var ledger = s.PopulationLedger;
            var source = ReturnedUnitPrefix + city;
            var ordinal = 0;
            while (ledger.HasArrived(source, ordinal)) ordinal = checked(ordinal + 1);
            var region = c.PlayerRegions.AtLocation(city)?.Id
                ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");
            ledger.RegisterArrival(source, ordinal, new PopulationUnit(0, PopulationStatus.FreeCivilian, c.RequirePopulationInteger("reception.turns"),
                new PopulationLocation(region, city: city), PopulationAssignment.Unassigned, CivilianProfession.Pioneer,
                u.Type == Projects.VeteranType ? CivilianTier.Veteran : CivilianTier.Novice, 0, "", 0, PopulationEquipment.None));
        }

        /// <summary>A free civilian pioneer without an assignment: a halutz graduate ("Free pioneer, not housed (Rec.)"), or one
        /// returned to a city.</summary>
        static bool IsFreePioneer(PopulationUnit p) => p.Status == PopulationStatus.FreeCivilian
            && p.Assignment.Kind == PopulationAssignmentKind.Unassigned && p.Profession == CivilianProfession.Pioneer;

        /// <summary>#1725 PR 7: R-QUAL-005's refusal on a Historical ledger game when the city has fewer free pioneer records
        /// than the groups asked for. Public for the UI's strings, as Training's ledger refusals are (#3020).</summary>
        public const string LedgerRefusedFreePioneers = "{0} has {1} free pioneers without work";

        /// <summary>R-QUAL-005: <paramref name="count"/> of the city's qualified halutz groups (<paramref name="track"/>
        /// `halutz`) or veteran pioneers (`veteran_halutz`), without a job, reappear as units at its settlement hex. Refused,
        /// state untouched, for an unknown city, another track, a count below 1, more than the city's qualified ones, or more
        /// than its civilians without work. On a Historical ledger game (#1725 PR 7) the groups are the city's free pioneer records
        /// (refused with <see cref="LedgerRefusedFreePioneers"/> when it has fewer), and only <see cref="City.Qualified"/> is written.</summary>
        public static string? ReassignPioneers(Content c, GameState s, string city, string track, int count, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: on a Historical ledger the groups are pioneer records
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            var veteran = track == Projects.VeteranType;
            if (!veteran && track != "halutz") return RefusedNoQualification;
            if (count < 1) return RefusedReassignCount;
            var name = PlayerText.Settlement(c, city);
            var have = veteran ? x.Qualified.Veterans : x.Qualified.Halutzim.Count;
            if (count > have) return FormattableString.Invariant($"{name} has {have} qualified {(veteran ? "veteran pioneers" : "halutzim")}");
            var ledger = s.PopulationLedger.IsHistorical;
            List<int>? picked = null;
            if (ledger)
            {
                // #1725 PR 7 (owner, 9 November 2026, "Free pioneers by service (Recommended)"): the city's free, unassigned
                // pioneer records, lowest id first, are the groups that come back; the city's counts are not the ledger's.
                // Each track prefers its own records, following "Yes, keep the link (Recommended)": Veteran ones for
                // veteran_halutz, the others for halutz, then the other tier's when it has too few. The training centre's
                // census teacher, while untouched, comes after every other record of either tier ("Takeable, but last
                // (Recommended)", #1689), so a track that falls back takes the other tier's records before the teacher.
                var teacher = HistoricalPopulation.UntouchedTeacherId(s.PopulationLedger);
                var free = s.PopulationLedger.Units.Where(p => IsFreePioneer(p) && string.Equals(p.Location?.City, city, StringComparison.Ordinal))
                    .OrderBy(p => p.Id == teacher ? 1 : 0).ThenBy(p => (p.Tier == CivilianTier.Veteran) == veteran ? 0 : 1).ThenBy(p => p.Id)
                    .Select(p => p.Id).ToList();
                if (count > free.Count) return string.Format(CultureInfo.InvariantCulture, LedgerRefusedFreePioneers, name, free.Count);
                picked = free.Take(count).ToList();
            }
            else
            {
                var idle = Training.Idle(s, x);   // R-ARMS-005: a staffed workshop's worker is not idle
                if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            }
            var hex = c.SettlementHexes.First(h => h.Id == city);   // every settlement has one (ValidateMap)
            var q = x.Qualified;
            // R-QUAL-005: the most service first (owner, 24 October 2026: "Accept all as built", https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6375597377). A veteran comes back with the service
            // that made it one (R-VET-001). On a ledger the i-th most served group is the i-th lowest record id ("Free
            // pioneers by service (Recommended)"), and its unit is linked to that record ("Add the link first (Recommended)").
            var served = veteran ? new int[count] : q.Halutzim.Skip(q.Halutzim.Count - count).Reverse().ToArray();
            if (picked != null)
            {
                // "Free pioneers by service (Recommended)": the records go back to region-only with no city, as at graduation.
                var region = c.PlayerRegions.AtLocation(city)?.Id
                    ?? throw new InvalidOperationException("No region in regions.yaml for " + city + " (R-POP-004).");
                s.PopulationLedger.Commit(picked.Select(id =>
                {
                    var p = s.PopulationLedger.Get(id);
                    return p.WithPlacement(p.Status, new PopulationLocation(region), p.Assignment, p.Captor, p.Home);
                }).ToList(), Array.Empty<PopulationTarget>());
                HistoricalPopulation.NoteLinked(s.PopulationLedger, picked);
            }
            for (int i = 0; i < count; i++)
            {
                var id = s.NextId++;
                s.Units.Add(id, new Unit(id, track, hex.Q, hex.R, veteran ? c.RulesAt(s.Date).Mvp!.VeteranHalutzService!.Value : served[i],
                    records: picked == null ? null : new[] { picked[i] }));
            }
            var left = veteran ? q.WithVeterans(q.Veterans - count) : q.TakeHalutzim(count);
            s.Cities[city] = (ledger ? x : x.WithCounts(x.Civilians - count, x.Employed, x.Refugees)).WithQualified(left);
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} of qualified {(veteran ? "veteran pioneers" : "halutzim")} are units again", key: CauseKeys.PioneersReassigned, args: new[] { CauseKeys.Number(count), veteran ? "veteran" : "halutz" }));
            return null;
        }
    }
}
