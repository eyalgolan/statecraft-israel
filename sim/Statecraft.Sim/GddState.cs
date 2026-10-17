using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>A city in the GDD v0.1 loop (docs/GDD.md "Cities and construction"; owner decision D-A2: civilians and
    /// refugees are counts per city). Counts are population units, each a group (D-A1). Immutable, so GameState.Clone can copy
    /// the map shallowly; a change replaces the record. Only the attributes approved decisions define are here: housing
    /// condition and craftsmen arrive with the decisions that define them.</summary>
    public sealed class City
    {
        public string Id { get; }
        /// <summary>How much population the city can house (GDD "Housing capacity").</summary>
        public int HousingCapacity { get; }
        /// <summary>The number of jobs (GDD "Employment slots").</summary>
        public int JobSlots { get; }
        public int Civilians { get; }
        /// <summary>The civilians in jobs: part of <see cref="Civilians"/>, not in addition to them.</summary>
        public int Employed { get; }
        public int Refugees { get; }
        /// <summary>The recruits who finished basic training here (GDD "Progression"; R-TRAIN-003). A count until Phase 5 makes
        /// soldiers units.</summary>
        public int Recruits { get; }

        public City(string id, int housingCapacity, int jobSlots, int civilians, int employed, int refugees, int recruits = 0)
        {
            Id = id; HousingCapacity = housingCapacity; JobSlots = jobSlots; Civilians = civilians; Employed = employed; Refugees = refugees;
            Recruits = recruits;
        }

        /// <summary>The same city with new population counts.</summary>
        public City WithCounts(int civilians, int employed, int refugees) => new City(Id, HousingCapacity, JobSlots, civilians, employed, refugees, Recruits);

        /// <summary>The same city with a new number of recruits.</summary>
        public City WithRecruits(int recruits) => new City(Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees, recruits);

        /// <summary>The record as the state hash writes it. The recruits are written only when there are some, so a city with
        /// none hashes as it did before training existed (R-LOOP-002's rule).</summary>
        public string Canonical() =>
            string.Format(CultureInfo.InvariantCulture, "{0}={1},{2},{3},{4},{5}", Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees)
            + (Recruits == 0 ? "" : "," + Recruits.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>A movable unit in the GDD v0.1 loop (owner decision D-A2: halutzim and soldiers are movable units), a group
    /// (D-A1), on hex (Q, R). Immutable; a change replaces the record.</summary>
    public sealed class Unit
    {
        public int Id { get; }
        public string Type { get; }
        public int Q { get; }
        public int R { get; }
        /// <summary>Turns of work done as a halutz (R-VET-001). Phase 5 Q-UNIT-2 counts soldiers' service in the same field.</summary>
        public int Service { get; }

        public Unit(int id, string type, int q, int r, int service = 0) { Id = id; Type = type; Q = q; R = r; Service = service; }

        /// <summary>The record as the state hash writes it. The service is written only when there is some, so a unit with none
        /// hashes as it did before Phase 4 PR 6 (R-LOOP-002's rule).</summary>
        public string Canonical() => string.Format(CultureInfo.InvariantCulture, "{0}:{1}@{2},{3}", Id, Type, Q, R)
            + (Service == 0 ? "" : "/" + Service.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>A group of a city's people in training (GDD "Progression"; R-TRAIN-001): <see cref="Count"/> population units
    /// becoming <see cref="Type"/> (a unit_types.yaml id with a facility) in <see cref="TurnsLeft"/> more turns. Immutable; a
    /// change replaces the record.</summary>
    public sealed class Cohort
    {
        public int Id { get; }
        public string Type { get; }
        public string City { get; }
        public int Count { get; }
        public int TurnsLeft { get; }

        public Cohort(int id, string type, string city, int count, int turnsLeft) { Id = id; Type = type; City = city; Count = count; TurnsLeft = turnsLeft; }

        /// <summary>The same cohort with <paramref name="turnsLeft"/> turns to go.</summary>
        public Cohort WithTurnsLeft(int turnsLeft) => new Cohort(Id, Type, City, Count, turnsLeft);

        /// <summary>The record as the state hash writes it.</summary>
        public string Canonical() => string.Format(CultureInfo.InvariantCulture, "{0}:{1}@{2}x{3}/{4}", Id, Type, City, Count, TurnsLeft);
    }

    /// <summary>Pioneer work at a designated site (sites.yaml; R-SITE-001 to R-SITE-003): <see cref="Unit"/> building it with
    /// <see cref="TurnsLeft"/> more turns to go, or, once <see cref="BuiltOn"/> is set, built on that date. Only sites being
    /// built or built have a record; an available site has none. A unit is busy while a record names it (D-D6: one project per
    /// unit), so Unit itself holds no order. Immutable; a change replaces the record.</summary>
    public sealed class SiteWork
    {
        public string Site { get; }
        /// <summary>The halutz unit building the site; null once it is built (unit ids start at 0, so 0 is a real unit).</summary>
        public int? Unit { get; }
        public int TurnsLeft { get; }
        /// <summary>The end date of the turn the site was built on; null while it is being built.</summary>
        public DateTime? BuiltOn { get; }

        /// <summary>A site being built (<paramref name="builtOn"/> null) needs its unit; a built site has none.</summary>
        public SiteWork(string site, int? unit, int turnsLeft, DateTime? builtOn)
        {
            if ((builtOn == null) != (unit != null)) throw new ArgumentException("a site being built has a unit, and a built site has none", nameof(unit));
            Site = site; Unit = unit; TurnsLeft = turnsLeft; BuiltOn = builtOn;
        }

        /// <summary>The site built on <paramref name="on"/>, with no unit on it.</summary>
        public static SiteWork Built(string site, DateTime on) => new SiteWork(site, null, 0, on);

        /// <summary>The same work with <paramref name="turnsLeft"/> turns to go.</summary>
        public SiteWork WithTurnsLeft(int turnsLeft) => new SiteWork(Site, Unit, turnsLeft, BuiltOn);

        /// <summary>The record as the state hash writes it: "id=b,UNIT,TURNSLEFT" while building, "id=done,yyyy-MM-dd" once built.</summary>
        public string Canonical() => BuiltOn is DateTime on
            ? Site + "=done," + on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, "{0}=b,{1},{2}", Site, Unit, TurnsLeft);
    }

    /// <summary>Work on one corridor segment (R-COR-001 to R-COR-003): <see cref="Unit"/> building it with
    /// <see cref="Progress"/> turns of work done, or, once <see cref="BuiltOn"/> is set, built on that date. Immutable.</summary>
    public sealed class SegmentWork
    {
        /// <summary>The halutz unit building the segment; null once it is built (unit ids start at 0).</summary>
        public int? Unit { get; }
        /// <summary>Turns of work done (spec section 2.2: "segment progress").</summary>
        public int Progress { get; }
        /// <summary>The end date of the turn the segment was built on; null while it is being built.</summary>
        public DateTime? BuiltOn { get; }

        /// <summary>A segment being built (<paramref name="builtOn"/> null) needs its unit; a built segment has none.</summary>
        public SegmentWork(int? unit, int progress, DateTime? builtOn)
        {
            if ((builtOn == null) != (unit != null)) throw new ArgumentException("a segment being built has a unit, and a built segment has none", nameof(unit));
            Unit = unit; Progress = progress; BuiltOn = builtOn;
        }

        /// <summary>"b,UNIT,PROGRESS" while building, "done,yyyy-MM-dd" once built.</summary>
        public string Canonical() => BuiltOn is DateTime on
            ? "done," + on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, "b,{0},{1}", Unit, Progress);
    }

    /// <summary>A corridor some of whose segments are being built or built (corridors.yaml; R-COR-001 to R-COR-003): one
    /// entry per segment of the corridor, in index order, null for a segment not started. A corridor with no started
    /// segment has no record. Immutable; a change replaces the record.</summary>
    public sealed class CorridorWork
    {
        public string Id { get; }
        public IReadOnlyList<SegmentWork?> Segments { get; }

        public CorridorWork(string id, SegmentWork?[] segments) { Id = id; Segments = (SegmentWork?[])segments.Clone(); }

        /// <summary>The same corridor with segment <paramref name="index"/> (0-based) replaced; null when no segment is left
        /// with a record.</summary>
        public CorridorWork? With(int index, SegmentWork? work)
        {
            var a = Segments.ToArray();
            a[index] = work;
            return a.All(x => x == null) ? null : new CorridorWork(Id, a);
        }

        /// <summary>"id=SEG;SEG;...", each segment "-" when not started.</summary>
        public string Canonical() => Id + "=" + string.Join(";", Segments.Select(x => x?.Canonical() ?? "-"));
    }
}
