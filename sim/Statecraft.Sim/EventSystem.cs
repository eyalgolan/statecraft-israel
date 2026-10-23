using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EventSystem
    {
        public const int MaxSoftEventsPerTurn = 3;
        /// <summary>The facility a grant with no `at` stands at (Q-HFX-2) and the city fields an option may change (R-EFX-004).</summary>
        public const string MilitaryCamp = "military_camp", HousingField = "housing_capacity", JobsField = "job_slots", ConditionField = "condition";

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>Fires every unfired event whose `requires` (if any) is met, whose window [From, To) overlaps the turn just played,
        /// [<paramref name="turnStart"/>, s.Date), or contains s.Date; <paramref name="turnStart"/> defaults to s.Date
        /// (game start). Every eligible hard anchor fires; MaxSoftEventsPerTurn caps only the non-hard events per turn,
        /// which fill the slots that remain (max(0, cap - hard count)) in the usual order (consequence cards first, R-ALT-008, then From, objective cards last in objectives.yaml's order, R-OBJ-007, then id). The fired
        /// events keep the usual order.
        /// Late fire: an event whose window overlapped the turn just played can fire after its window has ended; its
        /// conditions are evaluated at the end of the turn, and its text is filled at the last in-window day
        /// (<see cref="FillDate"/>).</summary>
        public static void Trigger(Content c, GameState s, List<Cause> why, DateTime? turnStart = null)
        {
            var start = turnStart ?? s.Date;
            var eligible = c.Events
                .Where(e => !s.FiredEvents.Contains(e.Id) && e.From <= s.Date && (e.To == null || start < e.To.Value))
                .Where(e => Timeline.AllMet(c, s, e))
                // R-OBJ-003: an objective's card waits for the objective. Only the GDD loop records one, so in the legacy loop it never fires.
                .Where(e => e.Objective == "" || s.Objectives.ContainsKey(e.Objective))
                // R-SWAMP-002: an event about a site's works waits for those works, and stops once the site is built (drained).
                // The legacy loop reads no sites, so it ignores the key and the legacy goldens hold.
                .Where(e => e.Site == "" || !c.IsGdd || (s.Sites.TryGetValue(e.Site, out var works) && works.BuiltOn == null))
                // R-POW-004: a front's exchange card waits for prisoners held either way. Only the GDD loop takes any, so the
                // legacy loop never fires one.
                .Where(e => e.Prisoners == "" || Prisoners.Held(s, e.Prisoners))
                // A late fire can come after a dated indicator's window has ended (chapters design 3.2): a condition on it is not met.
                .Where(e => e.Conditions.All(k => s.Indicators.TryGetValue(k.Indicator, out var v)
                                               && (!k.Min.HasValue || v >= k.Min.Value) && (!k.Max.HasValue || v <= k.Max.Value)))
                // R-ALT-008: a road the player took is not crowded out of its window. With no consequence card eligible the first
                // key is the same for every event, and (From, id) is a total order (ids are unique), so the order is as before.
                // R-OBJ-007 (Q-CARD-2): objective cards of the same From come after that From's other events, in objectives.yaml's order.
                .OrderBy(e => e.Kind == EventKinds.Consequence ? 0 : 1).ThenBy(e => e.From)
                .ThenBy(e => e.Objective == "" ? -1 : c.Objectives.FindIndex(o => o.Id == e.Objective)).ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
            var hardCount = eligible.Count(e => e.Anchor == "hard");
            var chosen = new HashSet<string>(eligible.Where(e => e.Anchor == "hard")
                .Concat(eligible.Where(e => e.Anchor != "hard").Take(Math.Max(0, MaxSoftEventsPerTurn - hardCount))).Select(e => e.Id));
            foreach (var e in eligible.Where(e => chosen.Contains(e.Id)))
            {
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Title(c, s, e)}"));
            }
        }

        /// <summary>The date whose office holders an event's text names: the given date, or the last in-window day
        /// once the window has ended (load-time coverage guarantees a holder there).</summary>
        public static DateTime FillDate(GameEvent e, DateTime date) => e.To.HasValue && date >= e.To.Value ? e.To.Value.AddDays(-1) : date;

        /// <summary>Returns null on success, or the reason the choice was rejected.</summary>
        public static string? Choose(Content c, GameState s, string eventId, string optionId, List<Cause> why)
        {
            if (!s.PendingEvents.Contains(eventId)) return "That event is not waiting for a decision";
            var option = Get(c, eventId).Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null) return "That event has no such option";
            Resolve(c, s, eventId, option, "chosen", why);
            return null;
        }

        /// <param name="share">The share of each numeric effect that applies: 1, except for a disloyal minister's delegation (R-EVT-003).</param>
        static void Resolve(Content c, GameState s, string eventId, EventOption o, string how, List<Cause> why, double share = 1.0)
        {
            s.PendingEvents.Remove(eventId);
            s.ChosenOptions[eventId] = o.Id;
            var fillDate = FillDate(Get(c, eventId), s.Date);
            var title = Title(c, s, Get(c, eventId));
            why.Add(new Cause("events", eventId, 0, $"{how}: {Cabinet.Fill(c, s, o.Text, fillDate)}"));
            foreach (var fx in o.Effects.Where(fx => Reaches(s, fx.Target)))
            {
                var delta = fx.Delta * share;
                var change = delta;
                if (fx.Target == "treasury")
                {
                    // R-MONEY-003: the cost first, then what was borrowed for it, as every other payment borrows.
                    why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})"));
                    if (delta < 0) EconomySystem.PayOrBorrow(c, s, -delta, $"for {title}", why);
                    else s.Treasury += delta;
                    continue;
                }
                if (fx.Target == Pools.FoodTarget)
                {
                    // R-EFX-001: an event moves the national Food stock by whole units (the delta rounds half away
                    // from zero, and the loader bounds it to the int range). The stock stays in 0..int.MaxValue,
                    // computed in long so nothing wraps, and the cause carries the change that applied.
                    var want = (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    var applied = Math.Max(-(long)s.Food, Math.Min(want, (long)int.MaxValue - s.Food));
                    s.Food = (int)(s.Food + applied);
                    change = applied;
                }
                else if (fx.Target == Pools.ArmsTarget)
                {
                    // R-EFX-001 (#781): the Arms stock moves exactly as the Food stock does above.
                    var want = (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    var applied = Math.Max(-(long)s.Arms, Math.Min(want, (long)int.MaxValue - s.Arms));
                    s.Arms = (int)(s.Arms + applied);
                    change = applied;
                }
                else if (fx.Target == "fx_reserves") s.FxReserves += delta;
                else change = EffectsSystem.Shift(c, s, fx.Target, delta);
                why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})"));
            }
            // R-EFX-006: the structured effects after the numeric ones, in list order. The loader has checked every target.
            foreach (var g in o.GrantUnits)
            {
                // R-EFX-003 (Q-HFX-2): free; the deal's cost is the option's numeric effects. The units take the next ids.
                var (place, q, r) = GrantPlace(c, g);
                for (int i = 0; i < g.N; i++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, g.Type, q, r)); }
                why.Add(new Cause("events", g.Type, g.N, $"{Granted(g, place)} ({title})"));
            }
            foreach (var x in o.CityEffects)
            {
                // R-EFX-004: only the named field changes. Housing capacity stays at 0 or more; job slots never fall below the
                // city's employed, so a cut takes only free slots; the condition stays from 0 to 1 (R-HOUSE-001). The loader
                // has checked the capacity and slot deltas are whole. The cause carries the change that applied.
                var city = s.Cities[x.City];
                if (x.Field == ConditionField)
                {
                    // A hand-built test city may have no condition; every GDD game's cities have one (R-HOUSE-001).
                    if (city.Condition is not double now) continue;
                    var condition = Math.Min(1.0, Math.Max(0.0, now + x.Delta));
                    s.Cities[x.City] = city.WithCondition(condition);
                    why.Add(new Cause("events", x.City + "." + x.Field, condition - now, $"{CityChange(c, x.City, x.Field, condition - now)} ({title})"));
                    continue;
                }
                var delta = (int)x.Delta;
                var housing = x.Field == HousingField ? Math.Max(0, city.HousingCapacity + delta) : city.HousingCapacity;
                var jobs = x.Field == JobsField ? Math.Max(city.Employed, city.JobSlots + delta) : city.JobSlots;
                s.Cities[x.City] = new City(city.Id, housing, jobs, city.Civilians, city.Employed, city.Refugees, city.Recruits, city.Condition, city.Craftsmen, city.JobsDefault);
                var applied = housing - city.HousingCapacity + jobs - city.JobSlots;
                why.Add(new Cause("events", x.City + "." + x.Field, applied, $"{CityChange(c, x.City, x.Field, applied)} ({title})"));
            }
            // R-EFX-008: after the city effects. A kind already unlocked stays so, with no second cause.
            foreach (var kind in o.UnlockProjects)
                if (s.UnlockedProjects.Add(kind)) why.Add(new Cause("events", kind, 1, $"{Unlocked(c, kind)} ({title})"));
            foreach (var kv in o.SetPolicy.Where(k => s.Policies.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Policies[kv.Key] = kv.Value;
                why.Add(new Cause("events", kv.Key, kv.Value, $"{PlayerText.Target(c, kv.Key)} set to {PlayerText.Percent(kv.Value)} ({title})"));
            }
            if (o.SetRung.HasValue) SecuritySystem.SetRung(c, s, o.SetRung.Value, why);
            // R-POW-004: an exchange card returns its front's prisoners, whichever way it was resolved (it has one option).
            if (Get(c, eventId).Prisoners is var front && front != "") Prisoners.Return(c, s, front, why);
        }

        /// <summary>A late-fired event can be resolved after a dated record's window has ended (chapters design 3.2): an
        /// effect on a target that no longer exists reaches nothing.</summary>
        static bool Reaches(GameState s, string target) => EventRules.SpecialTargets.Contains(target) || s.Indicators.ContainsKey(target);

        /// <summary>Where a grant stands (R-EFX-003, Q-HFX-2): the named city's settlement hex, or with no `at` the settlement
        /// hex of the one city with the military camp (the loader checks each exists).</summary>
        static (string Name, int Q, int R) GrantPlace(Content c, UnitGrant g)
        {
            var city = GrantCity(c, g);
            var hex = c.SettlementHexes.First(h => h.Id == city);   // every settlement has one (ValidateMap)
            return (PlayerText.Settlement(c, city), hex.Q, hex.R);
        }

        static string GrantCity(Content c, UnitGrant g) =>
            g.At != "" ? g.At : c.Cities.Single(x => x.Facilities.Contains(MilitaryCamp)).Id;

        static string Granted(UnitGrant g, string place) =>
            FormattableString.Invariant($"{g.N} {g.Type} unit{(g.N == 1 ? "" : "s")} at {place}");

        static string Unlocked(Content c, string kind) =>
            $"{PlayerText.Capitalised(c.ProjectKinds.First(k => k.Id == kind).Name)} can now be built";

        static string CityChange(Content c, string city, string field, double delta) =>
            field == ConditionField
                ? $"{PlayerText.Settlement(c, city)}: housing condition {PlayerText.PercentChange(delta)}"
                : FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: {field.Replace('_', ' ')} {(delta > 0 ? "+" : "")}{delta}");

        /// <summary>R-EVT-005: every effect of an option, in the order choosing it applies them (Resolve): the numeric
        /// effects, the unit grants, city effects and project unlocks (R-EFX-006), the policies it sets (by id), the security
        /// level, then rejoin. Each amount is the option's own; Resolve may apply less where an indicator meets its bound or
        /// the Food or Arms stock its floor, and rounds both to whole units. The event card will read this list (Q-EVT-4,
        /// owner 17 October 2026; Phase 7 PR 8), so no number is computed in Unity.</summary>
        public static List<Cause> OptionEffects(Content c, GameState s, EventOption o) =>
            OptionEffectRows(c, s, o).Select(r => r.Cause).ToList();

        /// <summary>R-EVT-005: the same entries as <see cref="OptionEffects"/>, in the same order, each with read-only
        /// presentation context: its category, the place or row it names and its amount already formatted by the sim. A
        /// localized UI supplies nouns, not game rules. Both come from one walk of the option's effects, so they cannot drift.</summary>
        public static IReadOnlyList<OptionEffectPreview> OptionEffectPreviews(Content c, GameState s, EventOption o) =>
            OptionEffectRows(c, s, o).AsReadOnly();

        static List<OptionEffectPreview> OptionEffectRows(Content c, GameState s, EventOption o)
        {
            var rows = new List<OptionEffectPreview>();
            void Add(OptionEffectPreviewKind kind, string target, double delta, string message, string contextId, string contextName,
                string amount, string field = "") =>
                rows.Add(new OptionEffectPreview(new Cause("events", target, delta, message), kind, contextId, contextName, amount, field));

            foreach (var fx in o.Effects.Where(fx => Reaches(s, fx.Target)))
            {
                var stock = fx.Target == Pools.FoodTarget || fx.Target == Pools.ArmsTarget;
                var name = stock ? (fx.Target == Pools.FoodTarget ? "Food" : "Arms") : PlayerText.Target(c, fx.Target);
                var amount = stock ? PlayerText.UnitsChangeNumber(fx.Delta)
                    : PlayerText.IsMoney(c, fx.Target) ? PlayerText.MoneyChange(c, fx.Target, fx.Delta) : PlayerText.PercentChange(fx.Delta);
                Add(stock ? OptionEffectPreviewKind.Stock : OptionEffectPreviewKind.Amount, fx.Target, fx.Delta,
                    PlayerText.Change(c, fx.Target, fx.Delta), fx.Target, name, amount);
            }
            foreach (var g in o.GrantUnits)
            {
                var place = GrantPlace(c, g).Name;
                Add(OptionEffectPreviewKind.UnitGrant, g.Type, g.N, Granted(g, place), GrantCity(c, g), place,
                    FormattableString.Invariant($"{g.N}"));
            }
            foreach (var x in o.CityEffects)
            {
                var amount = x.Field == ConditionField ? PlayerText.PercentChange(x.Delta)
                    : FormattableString.Invariant($"{(x.Delta > 0 ? "+" : "")}{x.Delta}");
                Add(OptionEffectPreviewKind.City, x.City + "." + x.Field, x.Delta, CityChange(c, x.City, x.Field, x.Delta),
                    x.City, PlayerText.Settlement(c, x.City), amount, x.Field);
            }
            foreach (var k in o.UnlockProjects)
                Add(OptionEffectPreviewKind.ProjectUnlock, k, 1, Unlocked(c, k), k,
                    PlayerText.Capitalised(c.ProjectKinds.First(x => x.Id == k).Name), "");
            foreach (var kv in o.SetPolicy.Where(k => s.Policies.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var name = PlayerText.Target(c, kv.Key);
                var amount = PlayerText.Percent(kv.Value);
                Add(OptionEffectPreviewKind.SetPolicy, kv.Key, kv.Value, $"{name} set to {amount}", kv.Key, name, amount);
            }
            if (o.SetRung is int rung)
            {
                var amount = FormattableString.Invariant($"{rung}");
                Add(OptionEffectPreviewKind.SetRung, "rung", rung, $"Security level set to {amount}", "", "", amount);
            }
            // R-ALT-009: the way back to the record is shown with the other effects (R-EVT-005).
            if (o.Rejoin != "")
            {
                var title = Cabinet.Fill(c, s, c.Events.First(x => x.Id == o.Rejoin).Title, s.Date);
                Add(OptionEffectPreviewKind.Rejoin, "rejoin", 0, $"Returns to the recorded course: {title}", o.Rejoin, title, "");
            }
            return rows;
        }

        /// <summary>The minister <see cref="Delegate"/> hands the event to in this state: its office's holder today, or null
        /// (no office, or nobody holds it: the cabinet decides). Simulation.DelegatedTo asks it after the queued orders.</summary>
        public static Person? DelegatedTo(Content c, GameState s, GameEvent e) => e.Office == "" ? null : Cabinet.Holder(c, s, e.Office);

        /// <summary>Unanswered events go to the responsible minister: with probability competence/10 they pick the
        /// recommended option (else the first), otherwise a random option. In Historical mode a hard-anchor event
        /// resolves to its historical option without drawing from the RNG (major anchors happen). An objective's card has one
        /// option and resolves to it without a draw (R-OBJ-003: GDD Phase 1 adds no RNG draw).
        /// In the GDD loop a drawn option of a minister whose loyalty is below the floor applies only disloyal_effect_share of
        /// each numeric effect (R-EVT-003; #107, owner, 18 October 2026). Why: loyalty decides how hard a minister works for a
        /// decision he was left to make, competence which decision he makes; anchors and objective cards stay whole.</summary>
        public static void Delegate(Content c, GameState s, List<Cause> why)
        {
            var rng = new Rng(s.RngState);
            var mvp = c.IsGdd ? c.RulesAt(s.Date).Mvp : null;   // the legacy loop has no floor, so its goldens hold
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                var minister = DelegatedTo(c, s, e);
                if (e.Objective != "" || e.Prisoners != "")   // R-POW-004: an exchange card, like an objective's, has one option and draws nothing
                {
                    Resolve(c, s, id, e.Options[0], $"delegated to {minister?.Name ?? "the cabinet"}", why);
                    continue;
                }
                var competence = (minister?.Competence ?? 5) / 10.0;
                var anchored = s.Mode == Mode.Historical && e.Anchor == "hard" ? e.Options.FirstOrDefault(o => o.Historical) : null;
                var option = anchored ?? (rng.NextDouble() < competence
                    ? e.Options.FirstOrDefault(o => o.Recommended) ?? e.Options[0]
                    : e.Options[(int)(rng.NextDouble() * e.Options.Count)]);
                var share = anchored == null && mvp != null && minister != null && minister.Loyalty < mvp.DelegationLoyaltyFloor
                    ? mvp.DisloyalEffectShare!.Value : 1.0;
                var how = $"delegated to {minister?.Name ?? "the cabinet"}" + (share < 1 ? ", carried out half-heartedly" : "");
                Resolve(c, s, id, option, how, why, share);
            }
            s.RngState = rng.State;
        }

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));

        /// <summary>The card's title as the player sees it now: a matching variant's, else the event's (R-ALT-010).</summary>
        public static string Title(Content c, GameState s, GameEvent e)
        {
            var v = Timeline.Variant(c, s, e);
            return Cabinet.Fill(c, s, v != null && v.Title != "" ? v.Title : e.Title, FillDate(e, s.Date));
        }

        /// <summary>Event text as the player sees it now: a matching variant's, with names following the player's appointments.</summary>
        public static string Render(Content c, GameState s, GameEvent e) =>
            Cabinet.Fill(c, s, Timeline.Variant(c, s, e)?.Text ?? e.Text, FillDate(e, s.Date));
    }
}
