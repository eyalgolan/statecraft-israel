using System;
using System.Globalization;

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

        public Unit(int id, string type, int q, int r) { Id = id; Type = type; Q = q; R = r; }

        /// <summary>The record as the state hash writes it.</summary>
        public string Canonical() => string.Format(CultureInfo.InvariantCulture, "{0}:{1}@{2},{3}", Id, Type, Q, R);
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
        /// <summary>The halutz unit building the site; 0 once it is built.</summary>
        public int Unit { get; }
        public int TurnsLeft { get; }
        /// <summary>The end date of the turn the site was built on; null while it is being built.</summary>
        public DateTime? BuiltOn { get; }

        public SiteWork(string site, int unit, int turnsLeft, DateTime? builtOn) { Site = site; Unit = unit; TurnsLeft = turnsLeft; BuiltOn = builtOn; }

        /// <summary>The site built on <paramref name="on"/>, with no unit on it.</summary>
        public static SiteWork Built(string site, DateTime on) => new SiteWork(site, 0, 0, on);

        /// <summary>The same work with <paramref name="turnsLeft"/> turns to go.</summary>
        public SiteWork WithTurnsLeft(int turnsLeft) => new SiteWork(Site, Unit, turnsLeft, BuiltOn);

        /// <summary>The record as the state hash writes it: "id=b,UNIT,TURNSLEFT" while building, "id=done,yyyy-MM-dd" once built.</summary>
        public string Canonical() => BuiltOn is DateTime on
            ? Site + "=done," + on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Format(CultureInfo.InvariantCulture, "{0}=b,{1},{2}", Site, Unit, TurnsLeft);
    }
}
