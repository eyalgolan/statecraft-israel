using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Plays what really happened: stages every future historical operation with ordinary go-to orders, picks each
    /// pending event's historical option (else its first), and answers each capture awaiting the choice, in ordinal id order,
    /// with Raid, never Deport (R-CTRL-009; Q-REF-1 (a), owner, 18
    /// October 2026, #783 batch 3; impact plan section 4.1 item 1), then sends reserves home down to the current security
    /// level's minimum call-up (the army demobilised after the 1949 armistices). In the GDD loop the call-up order is retired
    /// and the sim itself sends the reserves home with the rung (R-WAR-002), so there it sends none. It sends no budget
    /// allocation, so the budget stays at rules.yaml budget_start. Like the UI's order queue (SimBridge.Queue), it leaves out an order the
    /// turn's remaining attention cannot cover, so Step never has to refuse one of its orders.</summary>
    public static class HistoricalAI
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var wanted = OperationMoves(c, s, HistoricalOption).ToList();
            var staged = Simulation.Preview(c, s, wanted).State;
            wanted.AddRange(s.PendingEvents
                .Select(id => c.Events.First(e => e.Id == id))
                .Select(e => (Event: e, Option: HistoricalOption(e)))
                // If a long route has not finished, leave the card pending. End-turn delegation sees this turn's movement
                // and either makes the same historical decision then or postpones without a refused player command.
                .Where(x => EventSystem.OperationStatus(c, staged, x.Option).Met)
                // Q3: an operation whose front's armistice is pending or signed is left to delegation, which waits or lapses it.
                .Where(x => EventSystem.ArmisticeBlock(c, staged, x.Event, x.Option) == null)
                .Select(x => (Command)new ChooseEventOption(x.Event.Id, x.Option.Id))
                .ToList());
            // Settlements is ordinal. Each Raid is tried on a copy after the ones before it, as Step will run them, and one the
            // sim would refuse (a unit with nowhere to go) is left out, so that capture waits.
            var probe = s.Clone();
            foreach (var kv in s.Settlements.Where(kv => kv.Value.State == SettlementControl.AwaitingChoice))
                if (SettlementControl.Resolve(c, probe, kv.Key, SettlementControl.Raid, new List<Cause>()) == null)
                    wanted.Add(new ResolveCapture(kv.Key, SettlementControl.Raid));
            var floor = SecuritySystem.MinMobilization(c, s.Date, s.Rung);
            if (!c.IsGdd && s.Mobilization > floor) wanted.Add(new SetMobilization(floor));   // retired in the GDD loop (R-WAR-002)
            if (!c.UsesAttention) return wanted;
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

        /// <summary>The test-only reference player may replace one of this policy's staging go-to orders with an attack. Remove an
        /// operation answer if the ordinary commands that still precede it no longer put its required units in place.</summary>
        internal static void RemoveOperationChoicesWithoutStaging(Content c, GameState s, List<Command> commands)
        {
            for (int i = 0; i < commands.Count;)
            {
                if (commands[i] is ChooseEventOption choice)
                {
                    var option = c.Events.First(e => e.Id == choice.EventId).Options.First(o => o.Id == choice.OptionId);
                    if (option.RequiresUnitRegions.Count != 0
                        && !EventSystem.OperationStatus(c, Simulation.Preview(c, s, commands.Take(i).ToList()).State, option).Met)
                    {
                        commands.RemoveAt(i);
                        continue;
                    }
                }
                i++;
            }
        }

        static EventOption HistoricalOption(GameEvent e) => e.Options.FirstOrDefault(o => o.Historical) ?? e.Options[0];

        /// <summary>R-ENEMY-007: ordinary player go-to orders that put every unit needed by the next operation this
        /// automated player will choose into one of that option's allowed regions. Planning begins before the card opens;
        /// the normal route step spreads a long journey over turns. A lapsed card is not planned again.</summary>
        public static IReadOnlyList<Command> OperationMoves(Content c, GameState s, Func<GameEvent, EventOption> choose)
        {
            if (!c.IsGdd) return Array.Empty<Command>();
            var nextByUnit = c.Events
                // A card past its window still counts while it waits, postponed, for its units (R-ENEMY-007).
                .Where(e => e.To == null || s.Date < e.To.Value || s.PostponedOperations.ContainsKey(e.Id))
                .Where(e => !s.FiredEvents.Contains(e.Id) || s.PendingEvents.Contains(e.Id))
                // A card one of whose requirements is already decided the other way can never fire, so nothing is staged
                // for it (an undecided requirement still allows staging, as the card may yet come).
                .Where(e => !e.Requires.Any(r => !r.Diverged && s.ChosenOptions.ContainsKey(r.Event) && !Timeline.Met(c, s, r)))
                .Select(e => (Event: e, Option: choose(e)))
                .Where(x => x.Option.RequiresUnitRegions.Count != 0)
                .SelectMany(x => x.Option.UnitOrders.Select(o => (x.Event, x.Option, Row: o.Unit)))
                .OrderBy(x => x.Event.From).ThenBy(x => x.Event.Id, StringComparer.Ordinal)
                .GroupBy(x => x.Row, StringComparer.Ordinal).Select(g => g.First())
                .OrderBy(x => x.Row, StringComparer.Ordinal);
            var commands = new List<Command>();
            var probe = s.Clone();
            var spent = new Dictionary<int, int>();
            // Step advances standing routes before it applies these returned commands. Plan against that same state and
            // movement budget, otherwise replacing a route could be valid in the probe but refused after Step advances it.
            Movement.ContinueRoutes(c, probe, spent, new List<Cause>());
            // Staging is a move, not an attack: an enemy-held place in an allowed region (Acre in Galilee) is never its target,
            // so putting a unit in place captures nothing (R-ENEMY-007).
            var closed = Movement.NoEntry(c, probe);
            foreach (var need in nextByUnit)
            {
                int id = c.StartingForces.FindIndex(f => f.Row == need.Row);
                if (id < 0 || !probe.Units.TryGetValue(id, out var unit)) continue;   // a dated brigade is planned after it arrives
                var force = c.StartingForces[id];
                // The automated player can issue only the same ordinary go-to that a player can. A replacement in the
                // starting slot, an immobile force, or the last garrison of an occupied place is not available to stage.
                // probe includes earlier queued moves, so two units cannot both be planned away from one occupation.
                if (unit.Side != force.Side || unit.Type != force.Type || Movement.Allowance(c, unit) == 0
                    || SettlementControl.LastGarrison(c, probe, unit)) continue;
                var here = c.PlayerRegions.AtHex(unit.Q, unit.R);
                if (here != null && need.Option.RequiresUnitRegions.Contains(here.Id, StringComparer.Ordinal)) continue;
                if (unit.Order == Movement.GoTo)
                {
                    var target = unit.OrderTarget.Split(',');
                    if (target.Length == 2
                        && int.TryParse(target[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                        && int.TryParse(target[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
                    {
                        var destination = c.PlayerRegions.AtHex(q, r);
                        if (destination != null && need.Option.RequiresUnitRegions.Contains(destination.Id, StringComparer.Ordinal)) continue;
                    }
                }
                var routes = Movement.Routes(c, probe, id, spent);   // one search for all the candidate hexes
                var targetHex = need.Option.RequiresUnitRegions
                    .SelectMany(region => c.PlayerRegions.Regions[region].MemberHexes)
                    .Distinct().Where(h => !closed.Contains((h.Q, h.R)))
                    .Select(h => (Hex: h, Route: routes(h.Q, h.R)))
                    .Where(x => x.Route.Count != 0 && Movement.Full(c, probe, unit, x.Hex.Q, x.Hex.R) == null)
                    .OrderBy(x => x.Route[x.Route.Count - 1].Turn).ThenBy(x => x.Route.Count)
                    .ThenBy(x => x.Hex.Q).ThenBy(x => x.Hex.R).FirstOrDefault();
                if (targetHex.Route != null
                    && Movement.GiveGoTo(c, probe, id, targetHex.Hex.Q, targetHex.Hex.R, spent, new List<Cause>()) == null)
                    commands.Add(new UnitOrder(id, Movement.GoTo, targetHex.Hex.Q, targetHex.Hex.R));
            }
            return commands;
        }
    }
}
