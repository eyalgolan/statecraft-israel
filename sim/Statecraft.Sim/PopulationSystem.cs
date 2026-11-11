using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Population by origin group, plus the annexed residents' own group (R-CITY-008). Historical mode takes arrivals from the sourced aliyah series.</summary>
    public static class PopulationSystem
    {
        /// <summary>The source key prefix of a natural-increase record on a Historical ledger (#1725 PR 7): this prefix plus the
        /// city id; its ordinal is the record's place in that city's Cities.Apportion share of all natural-increase records.</summary>
        internal const string IncreasePrefix = "increase/";

        public static void Init(Content c, GameState s)
        {
            PopulationLedger.RefuseAggregate(s);
            s.Population = new Dictionary<string, double>(c.Rules.InitialPopulation);
        }

        /// <summary>Arrivals in [from, to). Each series period is inclusive of both dates and spread evenly by day.</summary>
        public static Dictionary<string, double> Arrivals(Content c, DateTime from, DateTime to)
        {
            var result = new Dictionary<string, double>();
            foreach (var p in c.Aliyah)
            {
                var pEnd = p.To.AddDays(1);
                var start = from > p.From ? from : p.From;
                var end = to < pEnd ? to : pEnd;
                var overlap = (end - start).TotalDays;
                if (overlap <= 0) continue;
                var share = overlap / (pEnd - p.From).TotalDays;
                foreach (var kv in p.Origins)
                    result[kv.Key] = (result.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value * share;
            }
            return result;
        }

        /// <summary>Natural increase, then the turn's CBS arrivals, by origin group. With an initialized Historical ledger
        /// (#1725 PR 7) the ledger's records are the people (R-CITY-008: "never extra people"): the arrivals are Cities.Arrive's
        /// refugee records (R-CITY-003), counted once there, so this adds only the natural increase, as records
        /// (<see cref="LedgerIncrease"/>), and leaves GameState.Population and the causes alone.</summary>
        public static void Apply(Content c, GameState s, DateTime to, double yearFraction, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            if (s.PopulationLedger.IsHistorical) { LedgerIncrease(c, s, yearFraction); return; }
            var rate = c.RulesAt(s.Date).NaturalIncreasePerYear * yearFraction;
            foreach (var k in s.Population.Keys.ToList()) s.Population[k] *= 1 + rate;
            foreach (var kv in Arrivals(c, s.Date, to).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Population[kv.Key] = (s.Population.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                if (kv.Value > 0) why.Add(new Cause("population", kv.Key, kv.Value, FormattableString.Invariant($"{kv.Value:N0} immigrants arrived ({PlayerText.Region(c, kv.Key)})"), key: CauseKeys.Arrived));
            }
        }

        /// <summary>R-CITY-008 on an initialized Historical ledger (#1725 PR 7): natural increase at the national rate (GDD Q11,
        /// "stays the national rate, 0.0169 a year") in whole records. The owner, 8 November 2026: "Everyone alive
        /// (Recommended)": the base is every living record. "Carry the fraction (Recommended)": the turn owes the carry plus the
        /// rate times the turn's share of a year times that base, in records; each whole record of it is added and the rest
        /// carried. In doubles, with the aggregate's operations in a fixed
        /// order, the carry written as IEEE bits (docs/determinism.md). "Same split as arrivals
        /// (Recommended)": the records are split over the cities by Cities.Apportion of all natural-increase records so far,
        /// keyed <see cref="IncreasePrefix"/> plus the city id with that city's ordinal, so a replay adds the same records.
        /// "Free civilian (Recommended)": each is a free civilian of its city with no work experience and no reception wait,
        /// in the shape of a census record (HistoricalPopulation.Initialize: reception.turns done, no assignment, profession,
        /// tier, qualification or equipment). All or nothing. A negative rate or turn has no ruling and is refused.</summary>
        static void LedgerIncrease(Content c, GameState s, double yearFraction)
        {
            var ledger = s.PopulationLedger;
            // The aggregate's own rate, in the same double arithmetic and order (Apply: `var rate =
            // c.RulesAt(s.Date).NaturalIncreasePerYear * yearFraction;`).
            var rate = c.RulesAt(s.Date).NaturalIncreasePerYear * yearFraction;
            if (!(rate >= 0))
                throw new InvalidOperationException("A negative natural increase on the ledger has no rule (R-CITY-008).");
            var owed = ledger.NaturalIncreaseCarry + rate * ledger.Units.Count;
            var whole = checked((int)Math.Floor(owed));
            var had = Cities.Apportion(c, ledger.NaturalIncreaseRecords);
            var rows = new List<PopulationInventoryRow>();
            foreach (var kv in Cities.Apportion(c, checked(ledger.NaturalIncreaseRecords + whole)))
                for (var ordinal = had[kv.Key]; ordinal < kv.Value; ordinal++)
                {
                    var region = c.PlayerRegions.AtLocation(kv.Key)?.Id
                        ?? throw new InvalidOperationException("No region in regions.yaml for city " + kv.Key + " (R-POP-004).");
                    rows.Add(new PopulationInventoryRow(IncreasePrefix + kv.Key, ordinal, new PopulationUnit(0, PopulationStatus.FreeCivilian,
                        ledger.ReceptionLimit, new PopulationLocation(region, city: kv.Key), PopulationAssignment.Unassigned,
                        CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None)));
                }
            ledger.RegisterNaturalIncrease(rows, owed - whole);
        }
    }
}
