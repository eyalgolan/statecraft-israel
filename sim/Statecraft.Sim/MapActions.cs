using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Something the player built on a hex. Immutable, so GameState.Clone can copy the list shallowly.</summary>
    public sealed class Placement
    {
        public string Action { get; }
        public int Q { get; }
        public int R { get; }
        public DateTime Date { get; }
        public Placement(string action, int q, int r, DateTime date) { Action = action; Q = q; R = r; Date = date; }
    }

    /// <summary>A map-action refusal's stable UI kind and invariant values, beside the English text used by the CLI.</summary>
    public sealed class MapActionReason
    {
        public string Kind { get; }
        public IReadOnlyList<string> Values { get; }
        public string Text { get; }
        public MapActionReason(string kind, string text, params string[] values)
        {
            Kind = kind; Text = text; Values = values;
        }
    }

    /// <summary>Map actions (M3 scope: absorption from 1950). A transit camp (ma'abara) or a housing project goes on a land
    /// hex within rules map_action_max_distance of a hex that owns a settlement (content/map/settlement_hexes.yaml). It
    /// costs treasury (a shortfall is borrowed, as in EconomySystem) and moves housing (absorption) and morale (support).
    /// At most rules map_actions_per_turn are built in one turn (upkeep and camp capacity are M4).</summary>
    public static class MapActions
    {
        public const string Kind = "map_action";
        public const string TransitCamp = "transit_camp";
        public const string Housing = "housing";
        public static readonly IReadOnlyList<string> Actions = new[] { TransitCamp, Housing };
        public const string MaabarotPolicy = "maabarot";

        /// <summary>Axial hex distance (Red Blob Games).</summary>
        public static int Distance(int q1, int r1, int q2, int r2) =>
            (Math.Abs(q1 - q2) + Math.Abs(q1 + r1 - q2 - r2) + Math.Abs(r1 - r2)) / 2;

        /// <summary>The placement on (q, r), or null.</summary>
        public static Placement? At(GameState s, int q, int r) => s.Placements.FirstOrDefault(p => p.Q == q && p.R == r);

        static int ActionsThisTurn(GameState s) => s.MapActionsTurnDate == s.Date ? s.MapActionsThisTurn : 0;

        /// <summary>Null when <paramref name="action"/> may be placed on (q, r) now, else the reason shown to the player.</summary>
        public static string? Check(Content c, GameState s, string action, int q, int r) => CheckReason(c, s, action, q, r)?.Text;

        /// <summary>Null when the action may be placed, else a stable kind and invariant values for a UI, plus English text.</summary>
        public static MapActionReason? CheckReason(Content c, GameState s, string action, int q, int r)
        {
            // No id in either reason: a command from an old save or a stale UI may name an action this content lacks; the
            // replay refuses it here, before anything is formatted, so a placement always has its rule (fix round 3, m-6).
            if (!Actions.Contains(action)) return new MapActionReason("unknown", "There is no such map action");
            // eras.yaml systems.map_actions: the actions this era allows
            if (!c.SystemsAt(s.Date).MapActions.Contains(action)) return new MapActionReason("chapter", "That map action is not available in this chapter");
            var rules = c.RulesAt(s.Date);   // the era the turn starts in
            var rule = rules.MapActions.TryGetValue(action, out var x) ? x : null;
            if (rule == null) return new MapActionReason("game", "That map action is not available in this game");
            // rules.yaml map_actions from/to (chapters spec 3.2): [from, to), like every content window. The reason names the day.
            if (!Window.Active(rule.From, rule.To, s.Date))
            {
                var before = s.Date < rule.From.GetValueOrDefault();
                var date = before ? rule.From!.Value : rule.To!.Value.AddDays(-1);
                var words = before
                    ? $"{PlayerText.Capitalised(PlayerText.MapAction(c, action))} can be placed from {PlayerText.Date(date)}"
                    : $"{PlayerText.Capitalised(PlayerText.MapAction(c, action))} could be placed until {PlayerText.Date(date)}";
                return new MapActionReason(before ? "action_from" : "action_until", words, action,
                    date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }
            if (s.Date < rules.MapActionsFrom)
                return new MapActionReason("all_from", $"Transit camps and housing projects can be placed from {PlayerText.Date(rules.MapActionsFrom)}",
                    rules.MapActionsFrom.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            if (action == TransitCamp && !(s.Policies.TryGetValue(MaabarotPolicy, out var m) && m >= 0.5))
                return new MapActionReason("maabarot_policy", "Transit camps need the ma'abarot policy");
            var hex = c.HexAt(q, r);
            if (hex == null || !hex.Land) return new MapActionReason("not_land", "This hex is not land");
            var max = rules.MapActionMaxDistance;
            if (!c.SettlementHexes.Any(sh => Distance(q, r, sh.Q, sh.R) <= max))
                return new MapActionReason("too_far", FormattableString.Invariant($"This hex is more than {max} {(max == 1 ? "hex" : "hexes")} from a settlement"),
                    max.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var here = At(s, q, r);
            if (here != null && !(action == Housing && here.Action == TransitCamp))
                return new MapActionReason("occupied", $"This hex already has {PlayerText.MapAction(c, here.Action)}", here.Action);
            // The cap comes last so a player who is over it still sees the specific reason an order is illegal. It counts
            // accepted actions (not surviving placements): housing on a camp removes the camp but still used an action.
            if (ActionsThisTurn(s) >= rules.MapActionsPerTurn)
                return new MapActionReason("turn_cap", FormattableString.Invariant($"No more than {rules.MapActionsPerTurn} map actions in one turn"),
                    rules.MapActionsPerTurn.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return null;
        }

        /// <summary>Returns null and builds, or the reason (state untouched). Housing on a transit camp replaces the camp.</summary>
        public static string? Apply(Content c, GameState s, string action, int q, int r, List<Cause> why)
        {
            var reason = Check(c, s, action, q, r);
            if (reason != null) return reason;
            var rule = c.RulesAt(s.Date).MapActions[action];
            if (s.MapActionsTurnDate != s.Date) { s.MapActionsTurnDate = s.Date; s.MapActionsThisTurn = 0; }
            s.MapActionsThisTurn++;
            s.Placements.RemoveAll(p => p.Q == q && p.R == r);
            s.Placements.Add(new Placement(action, q, r, s.Date));
            s.Treasury -= rule.Cost;
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {PlayerText.Money(c, "debt", -s.Treasury)} to build {PlayerText.MapAction(c, action)}"));
                s.Treasury = 0;
            }
            var housing = rule.Housing == 0 ? 0 : EffectsSystem.Shift(c, s, "housing", rule.Housing);
            var morale = rule.Morale == 0 ? 0 : EffectsSystem.Shift(c, s, "morale", rule.Morale);
            why.Add(new Cause("map", action, 1, FormattableString.Invariant(
                $"Built {PlayerText.MapAction(c, action)}: cost {PlayerText.Money(c, "treasury", rule.Cost)}, {PlayerText.Change(c, "housing", housing)}, {PlayerText.Change(c, "morale", morale)}")));
            return null;
        }
    }
}
