using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The advisors' one sim helper (docs/superpowers/specs/2026-10-23-advisors-design.md, section 5): the player's
    /// cities the enemy is likely to attack or enter this turn. It runs resolver step 1 (EnemyAI.Order, R-ENEMY-001) on a
    /// copy of the state and reads the copy: an attack order on a city's hex, or an enemy unit standing on one. It changes
    /// nothing and draws no random number. "Likely": the player's own orders this turn can still change the board.</summary>
    public static class Threats
    {
        public static IReadOnlyList<string> CitiesAtRisk(Content c, GameState s)
        {
            if (!c.IsGdd || s.GameOver != null) return Array.Empty<string>();
            var copy = s.Clone();
            EnemyAI.Order(c, copy, new List<Cause>(), Simulation.TurnEnd(c, s));
            var cityAt = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var h in c.SettlementHexes) if (s.Cities.ContainsKey(h.Id)) cityAt[Hex(h.Q, h.R)] = h.Id;
            var risk = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var u in copy.Units.Values)
            {
                if (u.Side == "" || u.Side == Movement.PlayerSide) continue;
                if (u.Order == Combat.Attack && cityAt.TryGetValue(u.OrderTarget, out var attacked)) risk.Add(attacked);
                if (cityAt.TryGetValue(Hex(u.Q, u.R), out var entered)) risk.Add(entered);
            }
            return risk.ToList();
        }

        static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);
    }
}
