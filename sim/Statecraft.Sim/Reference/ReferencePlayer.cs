using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim.Reference
{
    /// <summary>Test policy for the GDD goldens (D-A7): a stated simple policy, not game AI and not historical. It sends
    /// HistoricalAI's orders, then fills each city's jobs with as many civilians as there are jobs, while the turn's Attention
    /// lasts. It lives in Statecraft.Sim only so Unity can replay the MVP goldens (#394); no game mode or UI may reach it
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
            return list;
        }
    }
}
