using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim.Reference
{
    /// <summary>Test policy for the GDD goldens (D-A7): a stated simple policy, not game AI and not historical. It sends
    /// HistoricalAI's orders, then fills each city's jobs with as many civilians as there are jobs, then, when there is no
    /// halutz and none in training, trains one halutz group in the first city with a Hakhshara and a civilian left without
    /// work, while the turn's Attention and the Treasury last. It lives in Statecraft.Sim only so Unity can replay the MVP goldens (#394); no game mode or UI may reach it
    /// (ReferencePlayerTests.NoGameModeOrUiReachesIt).</summary>
    public static class ReferencePlayer
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = new List<Command>(HistoricalAI.Commands(c, s));
            var left = s.Attention - list.Sum(x => AttentionSystem.Cost(c, s, x.Kind));
            foreach (var city in s.Cities.Values)
            {
                var want = Math.Min(city.JobSlots, city.Civilians);
                if (want == city.Employed) continue;
                var cost = AttentionSystem.Cost(c, s, Cities.Kind);
                if (cost > left + 1e-9) continue;   // AttentionSystem.TrySpend's rule
                list.Add(new AssignWorkers(city.Id, "job", want));
                left -= cost;
            }
            var halutz = c.UnitTypes.FirstOrDefault(t => t.Id == "halutz");
            if (halutz == null || s.Units.Values.Any(u => u.Type == halutz.Id) || s.Cohorts.Values.Any(k => k.Type == halutz.Id)) return list;
            var trainCost = AttentionSystem.Cost(c, s, Training.TrainKind);
            if (trainCost > left + 1e-9 || halutz.TrainingCost > s.Treasury) return list;
            foreach (var city in s.Cities.Values)
            {
                var employed = list.OfType<AssignWorkers>().Where(a => a.City == city.Id).Select(a => a.Count).DefaultIfEmpty(city.Employed).Last();
                if (city.Civilians - employed < 1 || !c.Cities.First(x => x.Id == city.Id).Facilities.Contains(halutz.Facility)) continue;
                list.Add(new Train(city.Id, halutz.Id, 1));
                break;
            }
            return list;
        }
    }
}
