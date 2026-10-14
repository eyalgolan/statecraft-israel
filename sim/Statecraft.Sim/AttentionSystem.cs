using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>The PM's per-turn capacity. Each command costs attention; costs rise with the security level.</summary>
    public static class AttentionSystem
    {
        static readonly HashSet<string> Kinds = new HashSet<string>
            { "set_policy", "allocate_budget", "choose_event_option", "set_mobilization", "security_order", "appoint", "map_action", "assign_workers", "train", "cancel_training" };

        /// <summary>The kinds only the GDD v0.1 loop runs. Rules price them only when systems.loop is gdd, and the legacy loop
        /// refuses them before charging Attention.</summary>
        static readonly HashSet<string> GddKinds = new HashSet<string> { "assign_workers", "train", "cancel_training" };

        public static IReadOnlyCollection<string> CommandKinds => Kinds;

        public static bool IsCommandKind(string kind) => Kinds.Contains(kind);

        public static bool IsGddKind(string kind) => GddKinds.Contains(kind);

        public static void Refill(Content c, GameState s) => s.Attention = c.RulesAt(s.Date).AttentionPerTurn;

        /// <summary>Cost at <paramref name="rung"/> if given, else at the state's current rung.</summary>
        public static double Cost(Content c, GameState s, string kind, int? rung = null)
        {
            var r = c.RulesAt(s.Date);   // the era the turn starts in (docs/determinism.md, "Rules per era")
            return r.AttentionCost[kind] * (1 + r.RungAttentionStep * ((rung ?? s.Rung) - 1));
        }

        /// <summary>Returns null and deducts on success, or the reason it could not be afforded.
        /// <paramref name="rung"/> prices the command at that security level (the turn's starting rung).</summary>
        public static string? TrySpend(Content c, GameState s, string kind, int? rung = null)
        {
            var cost = Cost(c, s, kind, rung);
            if (cost > s.Attention + 1e-9)
                return string.Format(CultureInfo.InvariantCulture, "Not enough attention (need {0:0.0}, have {1:0.0})", cost, s.Attention);
            s.Attention -= cost;
            return null;
        }
    }
}
