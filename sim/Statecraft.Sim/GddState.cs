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
}
