using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: picks each pending event's historical option (else its first), then sends
    /// reserves home down to the current security level's minimum call-up (the army demobilised after the 1949
    /// armistices). The call-up is retired in the GDD loop (R-WAR-002), so there it sends none. It sends no budget allocation, so the budget stays at rules.yaml budget_start. Like the UI's order
    /// queue (SimBridge.Queue), it leaves out an order the turn's remaining attention cannot cover, so Step never has to
    /// refuse one of its orders.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var wanted = s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Command)new ChooseEventOption(e.Id, (e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0]).Id))
                .ToList();
            var floor = SecuritySystem.MinMobilization(c, s.Date, s.Rung);
            if (!c.IsGdd && s.Mobilization > floor) wanted.Add(new SetMobilization(floor));   // retired in the GDD loop (R-WAR-002)
            var list = new List<Command>();
            var left = s.Attention;
            foreach (var cmd in wanted)
            {
                var cost = AttentionSystem.Cost(c, s, cmd.Kind);   // the turn's starting rung, as Step prices it
                if (cost > left + 1e-9) continue;                   // AttentionSystem.TrySpend's rule
                left -= cost;
                list.Add(cmd);
            }
            return list;
        }
    }
}
