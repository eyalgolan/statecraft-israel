using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Population by origin group, plus the annexed residents' own group (R-CITY-008). Historical mode takes arrivals from the sourced aliyah series.</summary>
    public static class PopulationSystem
    {
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
        /// (#1725 PR 7) the ledger's records are the people (R-CITY-008: "never extra people"), so this adds none: the
        /// arrivals are Cities.Arrive's refugee records (R-CITY-003), counted once there, and GameState.Population and the
        /// causes are left alone. Natural increase on the ledger is refused: its rounding is ruled (owner, 8 November 2026:
        /// "Carry the fraction (Recommended)"), but who the new people are and where they live is not.</summary>
        public static void Apply(Content c, GameState s, DateTime to, double yearFraction, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            var rate = c.RulesAt(s.Date).NaturalIncreasePerYear * yearFraction;
            if (s.PopulationLedger.IsHistorical)
            {
                if (rate != 0) throw new NotSupportedException("natural increase on the ledger awaits the owner's placement ruling");
                return;
            }
            foreach (var k in s.Population.Keys.ToList()) s.Population[k] *= 1 + rate;
            foreach (var kv in Arrivals(c, s.Date, to).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Population[kv.Key] = (s.Population.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                if (kv.Value > 0) why.Add(new Cause("population", kv.Key, kv.Value, FormattableString.Invariant($"{kv.Value:N0} immigrants arrived ({PlayerText.Region(c, kv.Key)})"), key: CauseKeys.Arrived));
            }
        }
    }
}
