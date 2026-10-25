using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Jewish population by origin group. Historical mode takes arrivals from the sourced aliyah series.</summary>
    public static class PopulationSystem
    {
        public static void Init(Content c, GameState s) => s.Population = new Dictionary<string, double>(c.Rules.InitialPopulation);

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

        public static void Apply(Content c, GameState s, DateTime to, double yearFraction, List<Cause> why)
        {
            var rate = c.RulesAt(s.Date).NaturalIncreasePerYear * yearFraction;
            foreach (var k in s.Population.Keys.ToList()) s.Population[k] *= 1 + rate;
            foreach (var kv in Arrivals(c, s.Date, to).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Population[kv.Key] = (s.Population.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                if (kv.Value > 0) why.Add(new Cause("population", kv.Key, kv.Value, FormattableString.Invariant($"{kv.Value:N0} immigrants arrived ({PlayerText.Region(c, kv.Key)})"), key: CauseKeys.Arrived));
            }
        }
    }
}
