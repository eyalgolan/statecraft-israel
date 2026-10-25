using System;
using System.Collections.Generic;
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
        /// <summary>The <see cref="State"/> of a place the player occupies while its timer runs (R-CTRL-011, R-CTRL-012).</summary>
        public const string Occupied = "occupied";
        /// <summary>The <see cref="State"/> of an occupied place whose timer has ended: it awaits EndOccupation (Phase 6 PR 7).</summary>
        public const string AwaitingEnd = "awaiting_end";

        /// <summary>R-CTRL-004: the player's capture check, after the route step and each order, and in resolver step 3.
        /// Only curated enemy-held targets held by Israeli military units alone gain a record. Passing through a hex
        /// captures nothing. The preview has no capture date; Step stamps the turn's end before the resolver.</summary>
        internal static void CaptureHeld(Content c, GameState s, List<Cause> why)
        {
            var enemyHeld = new HashSet<string>(c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities, StringComparer.Ordinal);
            foreach (var t in c.SettlementTargets.Where(x => enemyHeld.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                // An ownerless fled record (the place returned to enemy control) is no record here, so it can be taken again.
                if (Holder(s, t.Q, t.R) != Movement.PlayerSide
                    || (s.Settlements.TryGetValue(t.Id, out var old) && !(old.Owner == "" && old.Fled && !old.Depopulated))) continue;
                var fled = t.Residents == ContentLoader.Fled;
                s.Settlements[t.Id] = new SettlementControl(Movement.PlayerSide, AwaitingChoice, fled: fled);
                why.Add(new Cause("map", t.Id, 0, $"{c.SettlementNames[t.Id]} is captured: Israeli units hold it and no enemy unit is there. "
                    + (fled ? "Its residents fled. " : "") + "It awaits a decision"));
            }
        }

        /// <summary>R-CTRL-004: a new capture keeps the turn's end date even when its same-turn answer changed its state.
        /// Deport keeps that date in its ownerless record (#1327); every other field is preserved.</summary>
        internal static void Stamp(GameState s, DateTime next)
        {
            foreach (var id in s.Settlements.Where(x => x.Value.Since == default
                && (x.Value.Owner == Movement.PlayerSide || x.Value.Depopulated || x.Value.Fled)).Select(x => x.Key).ToList())
            {
                var x = s.Settlements[id];
                s.Settlements[id] = new SettlementControl(x.Owner, x.State, next, x.Depopulated, x.Combatants, x.Buildings, x.Turns, x.Fled);
            }
        }

        /// <summary>Resolver step 3, after enemy combat: perform any remaining player capture, stamp its date, then return
        /// an awaiting-choice target held by enemy military units alone to enemy control. No resident field or RNG changes.</summary>
        public static void Capture(Content c, GameState s, DateTime next, List<Cause> why)
        {
            CaptureHeld(c, s, why);
            Stamp(s, next);
            var enemyHeld = new HashSet<string>(c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities, StringComparer.Ordinal);
            foreach (var t in c.SettlementTargets.Where(x => enemyHeld.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (Holder(s, t.Q, t.R) != Enemy || !s.Settlements.TryGetValue(t.Id, out var record) || record.State != AwaitingChoice) continue;
                Release(s, t.Id);
                why.Add(new Cause("map", t.Id, 0, $"{c.SettlementNames[t.Id]} is under enemy control again: enemy units hold it and no Israeli unit is there"
                    + (record.Fled ? ". " + FledLine : "")));
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
                var refusal = Spend(c, after, ResolveKind) ?? Resolved(c, after, id, outcome, new List<Cause>());
                options.Add(new CaptureOption(outcome, refusal, refusal == null ? Preview(s, after) : null));
            }
            return options;
        }

        /// <summary>The option's Attention, as Step charges it (AttentionSystem.TrySpend): null when paid, else the refusal with
        /// what it needs and what is left.</summary>
        static CaptureRefusal? Spend(Content c, GameState s, string kind)
        {
            var need = AttentionSystem.Cost(c, s, kind); var have = s.Attention;
            return AttentionSystem.TrySpend(c, s, kind) is string no ? new CaptureRefusal(CaptureRefusal.Attention, no, need, have) : null;
        }

        static CapturePreview Preview(GameState s, GameState after) => new CapturePreview(after.Attention - s.Attention,
            after.Treasury - s.Treasury, after.Debt - s.Debt, after.Food - s.Food, after.Arms - s.Arms,
            Indicator(after, "morale") - Indicator(s, "morale"), Indicator(after, "intl_standing") - Indicator(s, "intl_standing"), after.Score - s.Score);

        static double Indicator(GameState s, string id) => s.Indicators.TryGetValue(id, out var v) ? v : 0;

        /// <summary>R-CTRL-007: carries out ResolveCapture. Null when done, else the reason, with the state unchanged: the place
        /// is not awaiting the choice, the outcome is not known, or Deport is not in play (mvp.deport_in_play, off in the real
        /// content until PR 10 lifts it).</summary>
        internal static string? Resolve(Content c, GameState s, string id, string outcome, List<Cause> why) =>
            Resolved(c, s, id, outcome, why)?.Text;

        /// <summary><see cref="Resolve"/> with the refusal's id and values, for the options helper (R-CTRL-008).</summary>
        static CaptureRefusal? Resolved(Content c, GameState s, string id, string outcome, List<Cause> why)
        {
            if (NotAwaiting(s, id) is CaptureRefusal no) return no;
            switch (outcome)
            {
                case Raid: return Withdraw(c, s, id, why, false);
                case Occupy: return StartOccupation(c, s, id, why);
                case Deport:
                    if (!c.RulesAt(s.Date).Mvp!.DeportInPlay) return new CaptureRefusal(CaptureRefusal.NotInPlay, "Deport is not built yet");
                    // R-CTRL-018, OWNER QUESTION 4 (a) on #799: a fled town has no one left to deport, so Deport stays listed
                    // third but is refused with this reason, which the options helper shows on its button.
                    return s.Settlements[id].Fled ? new CaptureRefusal(CaptureRefusal.NoOneToDeport, NoOneToDeport) : Depopulate(c, s, id, why);
                default: return UnknownOutcome;
            }
        }

        /// <summary>Deport's refusal for a fled town (OWNER QUESTION 4 (a) on #799; in the owner's sensitivity packet).</summary>
        const string NoOneToDeport = "Its residents fled when it was captured, so there is no one to deport";

        static CaptureRefusal UnknownOutcome => new CaptureRefusal(CaptureRefusal.UnknownOutcome, "That outcome is not known");

        /// <summary>The kind of <see cref="EndOccupation"/>, with one attention_cost key (R-CTRL-014).</summary>
        public const string EndKind = "end_occupation";
        /// <summary>The outcomes of EndOccupation (R-CTRL-015; Q-PUP-1 (c): no puppet regime).</summary>
        public const string Annex = "annex";
        public const string WithdrawOutcome = "withdraw";
        /// <summary>The <see cref="State"/> of an annexed place: Israeli for good, no timer, upkeep or garrison (R-CTRL-016).</summary>
        public const string Annexed = "annexed";

        /// <summary>R-CTRL-015: the end outcomes in their one fixed order, the ruling's words "annex or withdraw".</summary>
        static readonly string[] EndOutcomes = { Annex, WithdrawOutcome };

        /// <summary>R-CTRL-015: what the end of <paramref name="id"/>'s occupation offers, Annex then Withdraw, each with its
        /// refusal or preview, as <see cref="CaptureOptions"/> does; empty when the place is not awaiting the end. Leaves
        /// <paramref name="s"/> as it found it.</summary>
        public static IReadOnlyList<CaptureOption> EndOptions(Content c, GameState s, string id)
        {
            if (NotEnding(s, id) != null) return Array.Empty<CaptureOption>();
            var options = new List<CaptureOption>();
            foreach (var outcome in EndOutcomes)
            {
                var after = s.Clone();
                var refusal = Spend(c, after, EndKind) ?? Ended(c, after, id, outcome, new List<Cause>());
                options.Add(new CaptureOption(outcome, refusal, refusal == null ? Preview(s, after) : null));
            }
            return options;
        }

        /// <summary>R-CTRL-014 to R-CTRL-017: carries out EndOccupation. Null when done, else the reason, with the state
        /// unchanged: the place is not awaiting the end (a depopulated place never is, R-CTRL-017), the outcome is not known,
        /// or no Israeli military unit is on the place, for either outcome. Withdraw is the Raid withdrawal (R-CTRL-010).</summary>
        internal static string? End(Content c, GameState s, string id, string outcome, List<Cause> why) => Ended(c, s, id, outcome, why)?.Text;

        /// <summary><see cref="End"/> with the refusal's id and values, for the options helper (R-CTRL-015).</summary>
        static CaptureRefusal? Ended(Content c, GameState s, string id, string outcome, List<Cause> why)
        {
            if (NotEnding(s, id) is CaptureRefusal no) return no;
            switch (outcome)
            {
                case Annex: return AnnexPlace(c, s, id, why);
                case WithdrawOutcome:
                    // R-CTRL-014: withdrawing needs the unit that holds the place too, as annexing does.
                    if (!Garrisoned(s, c.SettlementTargets.First(x => x.Id == id)))
                        return new CaptureRefusal(CaptureRefusal.NeedsUnit, $"Withdraw needs an Israeli military unit on {c.SettlementNames[id]}");
                    return Withdraw(c, s, id, why, false);
                default: return UnknownOutcome;
            }
        }

        static CaptureRefusal? NotEnding(GameState s, string id) =>
            s.Settlements.TryGetValue(id, out var x) && x.State == AwaitingEnd ? null
                : new CaptureRefusal(CaptureRefusal.NotAwaiting, "That place is not awaiting the end of its occupation");

        /// <summary>R-CTRL-016 (Q-ANNEX-1; OWNER QUESTIONS 1 (a) and 3 (a) on #799): the record becomes owner=israel,
        /// state=annexed, keeping its capture date and fled, with no timer. A fled place adds nothing else ("Territory only, no
        /// gain"). A stayed place adds one city by the starting-city rule (<see cref="Cities.Annex"/>). Step 10 ignores an
        /// annexed record, so its upkeep stops and no unit has to stay. Needs the garrison, as Occupy does (R-CTRL-011).</summary>
        static CaptureRefusal? AnnexPlace(Content c, GameState s, string id, List<Cause> why)
        {
            var t = c.SettlementTargets.First(x => x.Id == id);   // only a target gets a record (R-CTRL-004)
            var name = c.SettlementNames[id];
            if (!Garrisoned(s, t)) return new CaptureRefusal(CaptureRefusal.NeedsUnit, $"Annex needs an Israeli military unit on {name}");
            var x = s.Settlements[id];
            // Cities.Annex's one refusal: the id is already a city.
            if (!x.Fled && Cities.Annex(c, s, t) is string refused) return new CaptureRefusal(CaptureRefusal.AlreadyCity, refused);
            s.Settlements[id] = new SettlementControl(Movement.PlayerSide, Annexed, x.Since, x.Depopulated, x.Combatants, x.Buildings, 0, x.Fled);
            why.Add(new Cause("map", id, 0, x.Fled
                ? $"{name} is annexed. Its residents fled when it was captured, so no people, homes or jobs are added"
                : $"{name} is annexed. Its residents join the population"));
            return null;
        }

        static CaptureRefusal? NotAwaiting(GameState s, string id) =>
            s.Settlements.TryGetValue(id, out var x) && x.State == AwaitingChoice ? null
                : new CaptureRefusal(CaptureRefusal.NotAwaiting, "That place is not awaiting a decision on its capture");

        /// <summary>R-CTRL-010, Raid and withdraw (Q-RAID-1 (a)): the player's units on the place's hex, soldiers and halutzim
        /// alike, leave in id order, each to the first neighbouring hex in the fixed <see cref="Movement.Neighbours"/> order
        /// that is land, holds no enemy unit, is no enemy-held locality (R-MOVE-005) and has room for it (R-MOVE-003). Leaving
        /// costs no movement points (the default proposed on #797). The record goes (a fled one stays ownerless, <see cref="Release"/>),
        /// so the place is under enemy control again at once, and its residents are unchanged (or fled at capture). Refused, the state untouched, when a unit has nowhere to go, unless
        /// <paramref name="atTheEnd"/>: then that unit stays, the others leave and the record still goes.</summary>
        static CaptureRefusal? Withdraw(Content c, GameState s, string id, List<Cause> why, bool atTheEnd)
        {
            var t = c.SettlementTargets.First(x => x.Id == id);   // only a target gets a record (R-CTRL-004)
            var name = c.SettlementNames[id];
            var moved = new List<(Unit From, Unit To)>();
            if (MoveOut(c, s, t, name, atTheEnd, moved) is CaptureRefusal no) return no;
            var residents = Residents(s.Settlements[id]);
            Release(s, id);
            Withdrew(id, name, moved, why);
            why.Add(new Cause("map", id, 0, moved.Count > 0
                ? $"{name}: Israeli units withdrew and it is under enemy control again. {residents}"
                : $"{name} is under enemy control again. {residents}"));
            return null;
        }

        /// <summary>The line about a fled place's residents after its capture (OWNER QUESTION 2 (b) on #799; in the owner's
        /// sensitivity packet).</summary>
        const string FledLine = "Its residents fled when it was captured";

        /// <summary>What the player-facing lines say about the residents: unchanged for a stayed place, fled for a fled one.</summary>
        static string Residents(SettlementControl x) => x.Fled ? FledLine : "Its residents are unchanged";

        /// <summary>R-CTRL-004, R-CTRL-010 (OWNER QUESTION 2 (b) on #799): a place returns to enemy control. A fled record stays
        /// as an ownerless record with its capture date, the flight remembered; any other record goes.</summary>
        static void Release(GameState s, string id)
        {
            var x = s.Settlements[id];
            if (x.Fled) s.Settlements[id] = new SettlementControl(since: x.Since, fled: true);
            else s.Settlements.Remove(id);
        }

        /// <summary>R-CTRL-010's move: the units leave <paramref name="t"/>'s hex as <see cref="Withdraw"/> says, into
        /// <paramref name="moved"/>. Raid and Deport share it (R-CTRL-018). The reason, with every unit back where it was, when
        /// one has nowhere to go, unless <paramref name="atTheEnd"/>.</summary>
        static CaptureRefusal? MoveOut(Content c, GameState s, SettlementTarget t, string name, bool atTheEnd, List<(Unit From, Unit To)> moved)
        {
            foreach (var u in s.Units.Values.Where(u => u.Q == t.Q && u.R == t.R && !IsEnemy(u)).ToList())   // ascending id
            {
                var to = Movement.Neighbours.Select(d => ((int Q, int R)?)(t.Q + d.Q, t.R + d.R)).FirstOrDefault(h => Free(c, s, u, h!.Value.Q, h.Value.R));
                if (to == null && atTheEnd) continue;
                if (to == null)
                {
                    foreach (var x in moved) s.Units[x.From.Id] = x.From;   // back as it was
                    moved.Clear();
                    return new CaptureRefusal(CaptureRefusal.NoFreeHex, $"A unit has no free hex next to {name} to withdraw to");
                }
                var w = new Unit(u.Id, u.Type, to.Value.Q, to.Value.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
                s.Units[u.Id] = w;
                moved.Add((u, w));
            }
            return null;
        }

        static void Withdrew(string id, string name, List<(Unit From, Unit To)> moved, List<Cause> why)
        {
            foreach (var (from, to) in moved)
                why.Add(UnitLines.Line(WhyKeys.Withdrew,
                    FormattableString.Invariant($"Unit {from.Id} withdrew from {name} to hex {to.Q},{to.R}"),
                    new[] { from }, id, FormattableString.Invariant($"{to.Q},{to.R}")));
        }

        /// <summary>R-CTRL-018 to R-CTRL-021, Deport (impact plan section 4.1; Q-DEP-1 (a) to Q-DEP-5 (c), Q-DEP-7 (c)). The
        /// player's units leave as in Raid, refused the same way (Q-DEP-7 (a): no military advantage over Raid). The record
        /// keeps only the capture date and depopulated: no owner, state or timer, and the buildings at their default (Q-DEP-4
        /// (a), no razing in the MVP). The place counts once in DisplacedByPlayer, its residents nowhere else (Q-DEP-2 (a),
        /// Q-DEP-3 (a)). Social cohesion and International Support fall at once by the labelled costs, through the permanent
        /// shift. It draws nothing from the RNG.</summary>
        static CaptureRefusal? Depopulate(Content c, GameState s, string id, List<Cause> why)
        {
            var t = c.SettlementTargets.First(x => x.Id == id);   // only a target gets a record (R-CTRL-004)
            var name = c.SettlementNames[id];
            var moved = new List<(Unit From, Unit To)>();
            if (MoveOut(c, s, t, name, false, moved) is CaptureRefusal no) return no;
            s.Settlements[id] = new SettlementControl(since: s.Settlements[id].Since, depopulated: true);
            s.DisplacedByPlayer++;
            Withdrew(id, name, moved, why);
            why.Add(new Cause("map", id, 0, $"{name}: Deport. Its residents are displaced and the place is depopulated"));
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires both costs in the GDD loop
            foreach (var (indicator, cost) in new[] { ("morale", r.DeportCohesionCost!.Value), (SupportIndicator, r.DeportSupportCost!.Value) })
            {
                var change = EffectsSystem.Shift(c, s, indicator, -cost);
                why.Add(new Cause("map", indicator, change, $"{PlayerText.Change(c, indicator, change)} (Deport of {name})"));
            }
            // Deport never pays (owner, 22 October 2026, #1327): its Money stays at or below Occupy's and Raid's.
            why.Add(new Cause("economy", "treasury", -r.DeportMoneyCost!.Value, $"{PlayerText.Change(c, "treasury", -r.DeportMoneyCost.Value)} (Deport of {name})"));
            EconomySystem.PayOrBorrow(c, s, r.DeportMoneyCost.Value, $"for the Deport of {name}", why, CauseKeys.Purpose.Deport, id);
            return null;
        }

        /// <summary>International Support, the indicator Deport's costs, drag and ceiling act on (R-CTRL-018).</summary>
        internal const string SupportIndicator = "intl_standing";

        /// <summary>R-CTRL-011, R-CTRL-012 (Q-OCC-1 (a), Q-OCC-3 (a)): Occupy keeps the place under the player's control and
        /// starts its timer at mvp.occupation_turns, the one X for every place. It needs an Israeli military unit on the hex,
        /// the one unit that stays (a halutz holds nothing, #796), and is refused with the state unchanged otherwise. It moves
        /// no unit, and it changes no resident field (R-CTRL-003): only the record's state and timer.</summary>
        static CaptureRefusal? StartOccupation(Content c, GameState s, string id, List<Cause> why)
        {
            var t = c.SettlementTargets.First(x => x.Id == id);   // only a target gets a record (R-CTRL-004)
            var name = c.SettlementNames[id];
            if (!Garrisoned(s, t)) return new CaptureRefusal(CaptureRefusal.NeedsUnit, $"Occupy needs an Israeli military unit on {name}");
            var x = s.Settlements[id];
            var turns = c.RulesAt(s.Date).Mvp!.OccupationTurns!.Value;
            s.Settlements[id] = new SettlementControl(x.Owner, Occupied, x.Since, x.Depopulated, x.Combatants, x.Buildings, turns, x.Fled);
            why.Add(new Cause("map", id, turns, FormattableString.Invariant($"{name} is occupied for {turns} turns: an Israeli unit stays there. ") + Residents(x)));
            return null;
        }

        /// <summary>Whether an Israeli military unit is on <paramref name="t"/>'s hex (a halutz has no side, R-UNIT-001).</summary>
        static bool Garrisoned(GameState s, SettlementTarget t) => s.Units.Values.Any(u => u.Q == t.Q && u.R == t.R && u.Side == Movement.PlayerSide);

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

        /// <summary>Resolver step 10, the occupation timers, in ordinal id order (R-CTRL-011 to R-CTRL-013). An occupied place
        /// with no Israeli military unit left on its hex has been withdrawn from before the timer ended, which is a Raid
        /// (Q-OCC-2 (a)): its record goes, so it is under enemy control again, its residents unchanged, and nothing is paid.
        /// Every other occupation pays mvp.occupation_upkeep_per_turn (Q-OCC-1 (a)), the wait for EndOccupation included, and a
        /// running timer counts one turn down; at 0 the place awaits EndOccupation (Phase 6 PR 7). It draws nothing from the
        /// RNG.</summary>
        internal static void Tick(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            foreach (var id in s.Settlements.Where(kv => kv.Value.State == Occupied || kv.Value.State == AwaitingEnd).Select(kv => kv.Key).ToList())   // ordinal
            {
                var x = s.Settlements[id];
                var name = c.SettlementNames[id];
                if (!Garrisoned(s, c.SettlementTargets.First(t => t.Id == id)))
                {
                    Release(s, id);
                    why.Add(new Cause("map", id, 0, $"{name}: no Israeli unit holds it any more, so the occupation ended as a raid. It is under enemy control again. {Residents(x)}"));
                    continue;
                }
                EconomySystem.PayOrBorrow(c, s, r.OccupationUpkeepPerTurn!.Value, $"for the occupation of {name}", why, CauseKeys.Purpose.Occupation, id);
                if (x.State == AwaitingEnd) continue;
                var left = x.Turns - 1;
                s.Settlements[id] = new SettlementControl(x.Owner, left == 0 ? AwaitingEnd : Occupied, x.Since, x.Depopulated, x.Combatants, x.Buildings, left, x.Fled);
                why.Add(new Cause("map", id, left, left == 0
                    ? $"{name}: the occupation period has ended. It awaits a decision"
                    : FormattableString.Invariant($"{name}: {left} turns of occupation left")));
            }
        }

        /// <summary>Resolver step 10, after the occupation timers (R-CTRL-018; Q-DEP-5 (c)): International Support falls by
        /// mvp.deport_support_drag_per_turn for each place in DisplacedByPlayer, every turn from the Deport turn to the game's
        /// end. It draws nothing from the RNG.</summary>
        internal static void Drag(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            if (s.DisplacedByPlayer == 0) return;
            var change = EffectsSystem.Shift(c, s, SupportIndicator, -r.DeportSupportDragPerTurn!.Value * s.DisplacedByPlayer);
            if (change != 0) why.Add(new Cause("map", SupportIndicator, change, $"{PlayerText.Change(c, SupportIndicator, change)} (the places depopulated by Deport)"));
        }

        /// <summary>R-CTRL-020 (Q-DEP-7 (c), Q-SENS-2 (c)): whether (q, r) is the hex of a place the player has depopulated,
        /// where no city project or corridor segment may be started (no designated site can be on a settlement's hex at all).</summary>
        internal static bool DepopulatedAt(Content c, GameState s, int q, int r) =>
            s.Settlements.Any(kv => kv.Value.Depopulated && c.SettlementTargets.Any(t => t.Id == kv.Key && t.Q == q && t.R == r));

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

    /// <summary>One outcome of a capture as SettlementControl.CaptureOptions offers it (R-CTRL-008), or of an occupation's end
    /// as EndOptions does (R-CTRL-015): why it is refused, or null and its <see cref="Preview"/>. It has no default or selected
    /// flag on purpose.</summary>
    public sealed class CaptureOption
    {
        public string Outcome { get; }
        /// <summary>Why the option is refused, with a stable id the UI shows in the player's language, or null.</summary>
        public CaptureRefusal? Refused { get; }
        /// <summary>The refusal's English reason (the command's), for the log and tests, or null.</summary>
        public string? Refusal => Refused?.Text;
        public CapturePreview? Preview { get; }
        public CaptureOption(string outcome, CaptureRefusal? refused, CapturePreview? preview) { Outcome = outcome; Refused = refused; Preview = preview; }
    }

    /// <summary>Why a capture or end-of-occupation option is refused (R-CTRL-008, R-CTRL-015; #802): a stable <see cref="Id"/>
    /// from <see cref="Ids"/> and the values its line needs beyond the place itself, which is the option's own and which the UI
    /// names in its language. The UI shows a reviewed line for the id and never reads <see cref="Text"/>, the English reason
    /// the command returns (Q-TECH-5).</summary>
    public sealed class CaptureRefusal
    {
        /// <summary>Not enough Attention left. Args: what the option needs, what is left.</summary>
        public const string Attention = "attention";
        /// <summary>No Israeli military unit on the place (Occupy, Annex, Withdraw; R-CTRL-011, R-CTRL-014).</summary>
        public const string NeedsUnit = "needs_unit";
        /// <summary>A unit has no free hex to withdraw to (Raid, Deport, Withdraw; R-CTRL-010).</summary>
        public const string NoFreeHex = "no_free_hex";
        /// <summary>Deport of a place whose residents fled at capture (R-CTRL-018, OWNER QUESTION 4 (a) on #799).</summary>
        public const string NoOneToDeport = "no_one_to_deport";
        /// <summary>Deport while mvp.deport_in_play is off: "not built yet" (R-CTRL-007).</summary>
        public const string NotInPlay = "not_in_play";
        /// <summary>A stayed place whose id is already a city (Cities.Annex).</summary>
        public const string AlreadyCity = "already_city";
        /// <summary>The place is not awaiting this decision. The helpers list no option then.</summary>
        public const string NotAwaiting = "not_awaiting";
        /// <summary>The outcome is not one the command knows. The helpers list only known ones.</summary>
        public const string UnknownOutcome = "unknown_outcome";

        /// <summary>Every id, in a fixed order. Read-only: it cannot be cast back to an array and changed.</summary>
        public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { Attention, NeedsUnit, NoFreeHex, NoOneToDeport, NotInPlay, AlreadyCity, NotAwaiting, UnknownOutcome });

        public string Id { get; }
        public IReadOnlyList<object> Args { get; }
        public string Text { get; }

        public CaptureRefusal(string id, string text, params object[] args)
        {
            if (!Ids.Contains(id)) throw new ArgumentException("Unknown refusal id '" + id + "'", nameof(id));
            Id = id; Text = text; Args = args;
        }
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
