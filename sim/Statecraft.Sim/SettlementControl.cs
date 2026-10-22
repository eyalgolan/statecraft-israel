using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Settlement control in the GDD v0.1 loop (GDD Phase 6; docs/gdd/settlement-control.md). This half holds resolver
    /// step 3, the capture check (R-CTRL-004, R-CTRL-005), and the answer to a capture: the options helper, ResolveCapture and
    /// Raid and withdraw (R-CTRL-007 to R-CTRL-010). The record itself is in GddState.cs.</summary>
    public sealed partial class SettlementControl
    {
        /// <summary>The <see cref="State"/> of a target the player has captured and has not yet chosen an outcome for.</summary>
        public const string AwaitingChoice = "awaiting_choice";

        /// <summary>Resolver step 3 (R-CTRL-004, R-CTRL-005), after combat, in ordinal settlement id. A curated target that
        /// starts under enemy control (on mvp.enemy_held_localities, owner, 21 October 2026, #796) is captured when the
        /// player's military units are on its hex and no enemy military unit is (Q-CAP-1 (a); Q-CAP-3 (a): only enemy military
        /// units are combatants; #796: a halutz holds nothing): its record names the player, awaits the player's choice and
        /// starts on <paramref name="next"/>, the turn's end. A captured target still awaiting the choice that only enemy
        /// military units are on is under enemy control again, and its record goes (#796). A target that is not enemy-held
        /// at the start (Jaffa, #793) is never captured. Capture writes the settlement state only: no resident field, unit,
        /// resource, score or objective (R-CTRL-005). It draws nothing from the RNG.</summary>
        public static void Capture(Content c, GameState s, DateTime next, List<Cause> why)
        {
            var enemyHeld = new HashSet<string>(c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities, StringComparer.Ordinal);
            foreach (var t in c.SettlementTargets.Where(x => enemyHeld.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                var holder = Holder(s, t.Q, t.R);
                var record = s.Settlements.TryGetValue(t.Id, out var held) ? held : null;
                var name = c.SettlementNames[t.Id];
                if (holder == Movement.PlayerSide && record == null)
                {
                    s.Settlements[t.Id] = new SettlementControl(Movement.PlayerSide, AwaitingChoice, next);
                    why.Add(new Cause("map", t.Id, 0, $"{name} is captured: Israeli units hold it and no enemy unit is there. It awaits a decision"));
                }
                else if (holder == Enemy && record?.State == AwaitingChoice)   // PR 5's outcomes decide what a later state returns to
                {
                    s.Settlements.Remove(t.Id);
                    why.Add(new Cause("map", t.Id, 0, $"{name} is under enemy control again: enemy units hold it and no Israeli unit is there"));
                }
            }
        }

        /// <summary>The kind of <see cref="ResolveCapture"/>: one attention_cost key for every outcome, so none is cheaper (Q-TECH-3).</summary>
        public const string ResolveKind = "resolve_capture";
        public const string Raid = "raid";
        public const string Occupy = "occupy";
        public const string Deport = "deport";

        /// <summary>R-CTRL-008: the outcomes in their one fixed order. Deport is never first (impact plan section 4.1 item 1).</summary>
        static readonly string[] Outcomes = { Raid, Occupy, Deport };

        /// <summary>R-CTRL-008 (spec section 2.6): what the capture of <paramref name="id"/> offers, in the fixed order, each with
        /// the reason it is refused or the preview of what it does; empty when the place is not awaiting the choice. Nothing is
        /// pre-selected. A preview is what resolving the option, Attention included, does to a clone of the state, as Step would,
        /// so it cannot drift from the outcome. Leaves <paramref name="s"/> as it found it.</summary>
        public static IReadOnlyList<CaptureOption> CaptureOptions(Content c, GameState s, string id)
        {
            if (NotAwaiting(s, id) != null) return Array.Empty<CaptureOption>();
            var options = new List<CaptureOption>();
            foreach (var outcome in Outcomes)
            {
                var after = s.Clone();
                var refusal = AttentionSystem.TrySpend(c, after, ResolveKind) ?? Resolve(c, after, id, outcome, new List<Cause>());
                options.Add(new CaptureOption(outcome, refusal, refusal == null ? Preview(s, after) : null));
            }
            return options;
        }

        static CapturePreview Preview(GameState s, GameState after) => new CapturePreview(after.Attention - s.Attention,
            after.Treasury - s.Treasury, after.Debt - s.Debt, after.Food - s.Food, after.Arms - s.Arms,
            Indicator(after, "morale") - Indicator(s, "morale"), Indicator(after, "intl_standing") - Indicator(s, "intl_standing"), after.Score - s.Score);

        static double Indicator(GameState s, string id) => s.Indicators.TryGetValue(id, out var v) ? v : 0;

        /// <summary>R-CTRL-007: carries out ResolveCapture. Null when done, else the reason, with the state unchanged: the place
        /// is not awaiting the choice, the outcome is not known, or it is not built yet (Occupy is PR 6, Deport PR 8).</summary>
        internal static string? Resolve(Content c, GameState s, string id, string outcome, List<Cause> why)
        {
            if (NotAwaiting(s, id) is string no) return no;
            switch (outcome)
            {
                case Raid: return Withdraw(c, s, id, why, false);
                case Occupy: return "Occupy is not built yet";
                case Deport: return "Deport is not built yet";
                default: return "That outcome is not known";
            }
        }

        static string? NotAwaiting(GameState s, string id) =>
            s.Settlements.TryGetValue(id, out var x) && x.State == AwaitingChoice ? null : "That place is not awaiting a decision on its capture";

        /// <summary>R-CTRL-010, Raid and withdraw (Q-RAID-1 (a)): the player's units on the place's hex, soldiers and halutzim
        /// alike, leave in id order, each to the first neighbouring hex in the fixed <see cref="Movement.Neighbours"/> order
        /// that is land, holds no enemy unit, is no enemy-held locality (R-MOVE-005) and has room for it (R-MOVE-003). Leaving
        /// costs no movement points (the default proposed on #797). The record goes, so the place is under enemy control again
        /// at once, and its residents are unchanged. Refused, the state untouched, when a unit has nowhere to go, unless
        /// <paramref name="atTheEnd"/>: then that unit stays, the others leave and the record still goes.</summary>
        static string? Withdraw(Content c, GameState s, string id, List<Cause> why, bool atTheEnd)
        {
            var t = c.SettlementTargets.First(x => x.Id == id);   // only a target gets a record (R-CTRL-004)
            var name = c.SettlementNames[id];
            var moved = new List<(Unit From, Unit To)>();
            foreach (var u in s.Units.Values.Where(u => u.Q == t.Q && u.R == t.R && !IsEnemy(u)).ToList())   // ascending id
            {
                var to = Movement.Neighbours.Select(d => ((int Q, int R)?)(t.Q + d.Q, t.R + d.R)).FirstOrDefault(h => Free(c, s, u, h!.Value.Q, h.Value.R));
                if (to == null && atTheEnd) continue;
                if (to == null)
                {
                    foreach (var (from, _) in moved) s.Units[from.Id] = from;   // back as it was
                    return $"Unit {u.Id} has no free hex next to {name} to withdraw to";
                }
                var w = new Unit(u.Id, u.Type, to.Value.Q, to.Value.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget);
                s.Units[u.Id] = w;
                moved.Add((u, w));
            }
            s.Settlements.Remove(id);
            foreach (var (_, w) in moved)
                why.Add(new Cause("map", w.Id.ToString(CultureInfo.InvariantCulture), 0, FormattableString.Invariant($"Unit {w.Id} withdrew from {name} to hex {w.Q},{w.R}")));
            why.Add(new Cause("map", id, 0, moved.Count > 0
                ? $"{name}: Israeli units withdrew and it is under enemy control again. Its residents are unchanged"
                : $"{name} is under enemy control again. Its residents are unchanged"));
            return null;
        }

        static bool IsEnemy(Unit u) => u.Side != "" && u.Side != Movement.PlayerSide;

        static bool Free(Content c, GameState s, Unit u, int q, int r) =>
            c.HexAt(q, r) is MapHex hex && hex.Land && !s.Units.Values.Any(x => x.Q == q && x.R == r && IsEnemy(x))
            && !Movement.NoEntry(c, s).Contains((q, r)) && Movement.Full(c, s, u, q, r) == null;

        /// <summary>R-CTRL-008 (owner, 18 October 2026 on #797, "Counts as Raid"), where resolver step 10 sits on the turn that
        /// reaches the game's end: every capture still awaiting the choice, in ordinal id order, resolves as Raid. A unit with
        /// nowhere to go stays while the others leave, and the record goes all the same, since the game is over. It runs before objectives and score
        /// (step 11) and changes neither, so it adds nothing to the score.</summary>
        internal static void EndPending(Content c, GameState s, List<Cause> why)
        {
            foreach (var id in s.Settlements.Where(kv => kv.Value.State == AwaitingChoice).Select(kv => kv.Key).ToList())   // ordinal
            {
                why.Add(new Cause("map", id, 0, $"{c.SettlementNames[id]}: no decision was made before the end, so it counts as a raid"));
                Withdraw(c, s, id, why, true);   // at the end it never refuses
            }
        }

        const string Enemy = "enemy";

        /// <summary>Who holds (q, r): <see cref="Movement.PlayerSide"/> when only Israeli military units are on it,
        /// <see cref="Enemy"/> when only enemy ones are, else null (none, or both).</summary>
        static string? Holder(GameState s, int q, int r)
        {
            var on = s.Units.Values.Where(u => u.Q == q && u.R == r && u.Side != "").ToList();
            if (on.Count == 0) return null;
            var player = on.Count(u => u.Side == Movement.PlayerSide);
            return player == on.Count ? Movement.PlayerSide : player == 0 ? Enemy : null;
        }
    }

    /// <summary>One outcome of a capture as SettlementControl.CaptureOptions offers it (R-CTRL-008): the reason it is refused,
    /// or null and its <see cref="Preview"/>. It has no default or selected flag on purpose.</summary>
    public sealed class CaptureOption
    {
        public string Outcome { get; }
        public string? Refusal { get; }
        public CapturePreview? Preview { get; }
        public CaptureOption(string outcome, string? refusal, CapturePreview? preview) { Outcome = outcome; Refusal = refusal; Preview = preview; }
    }

    /// <summary>What resolving a capture option changes at once, as deltas: Attention, Money (the Treasury and the debt), Food,
    /// Arms, Social cohesion (morale), International Support (intl_standing) and the score (spec section 2.6; impact plan
    /// section 4.1's sober cost preview).</summary>
    public sealed class CapturePreview
    {
        public double Attention { get; } public double Money { get; } public double Debt { get; }
        public int Food { get; } public int Arms { get; }
        public double SocialCohesion { get; } public double InternationalSupport { get; }
        public int Score { get; }
        public CapturePreview(double attention, double money, double debt, int food, int arms, double socialCohesion, double internationalSupport, int score)
        {
            Attention = attention; Money = money; Debt = debt; Food = food; Arms = arms;
            SocialCohesion = socialCohesion; InternationalSupport = internationalSupport; Score = score;
        }
    }
}
