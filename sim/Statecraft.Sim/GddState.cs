using System;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>A city in the GDD v0.1 loop (docs/GDD.md "Cities and construction"; owner decision D-A2: civilians and
    /// refugees are counts per city). Counts are population units, each a group (D-A1). Immutable, so GameState.Clone can copy
    /// the map shallowly; a change replaces the record. Only the attributes approved decisions define are here.</summary>
    public sealed class City
    {
        public string Id { get; }
        /// <summary>How much population the city can house (GDD "Housing capacity").</summary>
        public int HousingCapacity { get; }
        /// <summary>The number of jobs (GDD "Employment slots").</summary>
        public int JobSlots { get; }
        public int Civilians { get; }
        /// <summary>The people in jobs: part of <see cref="Civilians"/> and <see cref="Craftsmen"/>, not in addition to them.
        /// Craftsmen take the jobs first (R-CRAFT-002).</summary>
        public int Employed { get; }
        public int Refugees { get; }
        /// <summary>The recruits who finished basic training here (GDD "Progression"; R-TRAIN-003). A count until Phase 5 makes
        /// soldiers units.</summary>
        public int Recruits { get; }
        /// <summary>The craftsmen trained here (D-D4; R-CRAFT-001): a count, not movable units (D-A2), and not part of
        /// <see cref="Civilians"/>.</summary>
        public int Craftsmen { get; }
        /// <summary>Whether the housing step fills the city's free jobs (R-CITY-009, Q-JOBS-1). On at the start; an accepted
        /// AssignWorkers order turns it off for the city.</summary>
        public bool JobsDefault { get; }
        /// <summary>The city's housing condition, 0 to 1, an average over the city (GDD "Housing condition"; D-D2, Q-HOUSE-2;
        /// R-HOUSE-001). Null only on a record made without one (a test's hand-built city); every GDD game's cities have one.</summary>
        public double? Condition { get; }

        public City(string id, int housingCapacity, int jobSlots, int civilians, int employed, int refugees, int recruits = 0,
            double? condition = null, int craftsmen = 0, bool jobsDefault = true)
        {
            Id = id; HousingCapacity = housingCapacity; JobSlots = jobSlots; Civilians = civilians; Employed = employed; Refugees = refugees;
            Recruits = recruits; Condition = condition; Craftsmen = craftsmen; JobsDefault = jobsDefault;
        }

        /// <summary>The same city with new population counts.</summary>
        public City WithCounts(int civilians, int employed, int refugees) => new City(Id, HousingCapacity, JobSlots, civilians, employed, refugees, Recruits, Condition, Craftsmen, JobsDefault);

        /// <summary>The same city with a new number of recruits.</summary>
        public City WithRecruits(int recruits) => new City(Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees, recruits, Condition, Craftsmen, JobsDefault);

        /// <summary>The same city with a new number of craftsmen.</summary>
        public City WithCraftsmen(int craftsmen) => new City(Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees, Recruits, Condition, craftsmen, JobsDefault);

        /// <summary>The same city with its job default on or off.</summary>
        public City WithJobsDefault(bool enabled) => new City(Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees, Recruits, Condition, Craftsmen, enabled);

        /// <summary>The same city with a new housing condition.</summary>
        public City WithCondition(double condition) => new City(Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees, Recruits, condition, Craftsmen, JobsDefault);

        /// <summary>The record as the state hash writes it. The recruits are written only when there are some, the condition
        /// only when it is set (",c" and its 64 bits in hex; readable, the value and then the bits), the craftsmen only when
        /// there are some and the job default only when it is off (",jobs=manual"), so a city with none of them hashes as it
        /// did before they existed (R-LOOP-002's rule).</summary>
        public string Canonical(bool readable = false)
        {
            var text = string.Format(CultureInfo.InvariantCulture, "{0}={1},{2},{3},{4},{5}", Id, HousingCapacity, JobSlots, Civilians, Employed, Refugees)
                + (Recruits == 0 ? "" : "," + Recruits.ToString(CultureInfo.InvariantCulture));
            if (Condition is double c)
            {
                if (c == 0.0) c = 0.0;   // -0 and 0 hash alike, as GameState's numbers do
                var bits = BitConverter.DoubleToInt64Bits(c).ToString("X16", CultureInfo.InvariantCulture);
                text += readable ? ",c" + c.ToString("R", CultureInfo.InvariantCulture) + " [" + bits + "]" : ",c" + bits;
            }
            if (Craftsmen != 0) text += ",craftsmen=" + Craftsmen.ToString(CultureInfo.InvariantCulture);
            if (!JobsDefault) text += ",jobs=manual";
            return text;
        }
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
        /// <summary>A military unit's side (a unit_types.yaml `side`), or "" for a halutz (R-UNIT-001).</summary>
        public string Side { get; }
        /// <summary>A military unit's strength in population units (Q-UNIT-1, Q-UNIT-4), or 0 (R-UNIT-001).</summary>
        public int Strength { get; }
        /// <summary>The unit's standing order and what it names, or "" for none (R-UNIT-001). Later Phase 5 PRs set them.</summary>
        public string Order { get; }
        public string OrderTarget { get; }

        public Unit(int id, string type, int q, int r, int service = 0, string side = "", int strength = 0, string order = "", string orderTarget = "")
        {
            Id = id; Type = type; Q = q; R = r; Service = service; Side = side; Strength = strength; Order = order; OrderTarget = orderTarget;
        }

        /// <summary>The record as the state hash writes it. Each field after the hex is written only when it is not at its
        /// default, so a halutz with no service hashes as it did before Phase 4 PR 6, and with service as before Phase 5
        /// (R-LOOP-002's rule; R-UNIT-002).</summary>
        public string Canonical() => string.Format(CultureInfo.InvariantCulture, "{0}:{1}@{2},{3}", Id, Type, Q, R)
            + (Service == 0 ? "" : "/" + Service.ToString(CultureInfo.InvariantCulture))
            + (Side == "" ? "" : "|side=" + Side)
            + (Strength == 0 ? "" : "|strength=" + Strength.ToString(CultureInfo.InvariantCulture))
            + (Order == "" ? "" : "|order=" + Order)
            + (OrderTarget == "" ? "" : "|target=" + OrderTarget);
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

    /// <summary>A city project (projects.yaml; R-PROJ-001 to R-PROJ-003): <see cref="Kind"/> built for <see cref="City"/> on hex
    /// (<see cref="Q"/>, <see cref="R"/>) with <see cref="TurnsLeft"/> more turns to go, or, once <see cref="BuiltOn"/> is set,
    /// built on that date (its capacity, condition and slots added then, once). A built project keeps its record, so its hex
    /// stays taken (Q-PROJ-2) and the map can draw it (Phase 3 PR 8). No workers (Q-PROJ-3). Immutable; a change replaces the
    /// record.</summary>
    public sealed class CityProject
    {
        public int Id { get; }
        public string Kind { get; }
        public string City { get; }
        public int Q { get; }
        public int R { get; }
        public int TurnsLeft { get; }
        /// <summary>The end date of the turn the project was built on; null while it is being built.</summary>
        public DateTime? BuiltOn { get; }

        public CityProject(int id, string kind, string city, int q, int r, int turnsLeft, DateTime? builtOn = null)
        {
            Id = id; Kind = kind; City = city; Q = q; R = r; TurnsLeft = turnsLeft; BuiltOn = builtOn;
        }

        /// <summary>The same project with <paramref name="turnsLeft"/> turns to go.</summary>
        public CityProject WithTurnsLeft(int turnsLeft) => new CityProject(Id, Kind, City, Q, R, turnsLeft);

        /// <summary>The same project, built on <paramref name="on"/>.</summary>
        public CityProject Built(DateTime on) => new CityProject(Id, Kind, City, Q, R, 0, on);

        /// <summary>The record as the state hash writes it: "id:KIND@CITY,Q,R/TURNSLEFT" while building, "/done,yyyy-MM-dd" once built.</summary>
        public string Canonical() => string.Format(CultureInfo.InvariantCulture, "{0}:{1}@{2},{3},{4}/", Id, Kind, City, Q, R)
            + (BuiltOn is DateTime on ? "done," + on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : TurnsLeft.ToString(CultureInfo.InvariantCulture));
    }
}
