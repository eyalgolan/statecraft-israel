using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EventSystem
    {
        public const int MaxSoftEventsPerTurn = 3;

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>Fires every unfired event whose `requires` (if any) is met, whose window [From, To) overlaps the turn just played,
        /// [<paramref name="turnStart"/>, s.Date), or contains s.Date; <paramref name="turnStart"/> defaults to s.Date
        /// (game start). Every eligible hard anchor fires; MaxSoftEventsPerTurn caps only the non-hard events per turn,
        /// which fill the slots that remain (max(0, cap - hard count)) in the usual order (From, then id). The fired
        /// events keep the usual order.
        /// Late fire: an event whose window overlapped the turn just played can fire after its window has ended; its
        /// conditions are evaluated at the end of the turn, and its text is filled at the last in-window day
        /// (<see cref="FillDate"/>).</summary>
        public static void Trigger(Content c, GameState s, List<Cause> why, DateTime? turnStart = null)
        {
            var start = turnStart ?? s.Date;
            var eligible = c.Events
                .Where(e => !s.FiredEvents.Contains(e.Id) && e.From <= s.Date && (e.To == null || start < e.To.Value))
                .Where(e => e.Requires == null || (s.ChosenOptions.TryGetValue(e.Requires.Event, out var picked) && picked == e.Requires.Option))
                // R-OBJ-003: an objective's card waits for the objective. Only the GDD loop records one, so in the legacy loop it never fires.
                .Where(e => e.Objective == "" || s.Objectives.ContainsKey(e.Objective))
                // A late fire can come after a dated indicator's window has ended (chapters design 3.2): a condition on it is not met.
                .Where(e => e.Conditions.All(k => s.Indicators.TryGetValue(k.Indicator, out var v)
                                               && (!k.Min.HasValue || v >= k.Min.Value) && (!k.Max.HasValue || v <= k.Max.Value)))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
            var hardCount = eligible.Count(e => e.Anchor == "hard");
            var chosen = new HashSet<string>(eligible.Where(e => e.Anchor == "hard")
                .Concat(eligible.Where(e => e.Anchor != "hard").Take(Math.Max(0, MaxSoftEventsPerTurn - hardCount))).Select(e => e.Id));
            foreach (var e in eligible.Where(e => chosen.Contains(e.Id)))
            {
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Cabinet.Fill(c, s, e.Title, FillDate(e, s.Date))}"));
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

        static void Resolve(Content c, GameState s, string eventId, EventOption o, string how, List<Cause> why)
        {
            s.PendingEvents.Remove(eventId);
            s.ChosenOptions[eventId] = o.Id;
            var fillDate = FillDate(Get(c, eventId), s.Date);
            var title = Cabinet.Fill(c, s, Get(c, eventId).Title, fillDate);
            why.Add(new Cause("events", eventId, 0, $"{how}: {Cabinet.Fill(c, s, o.Text, fillDate)}"));
            // A late-fired event can be resolved after a dated record's window has ended (chapters design 3.2): nothing reaches it.
            foreach (var fx in o.Effects.Where(fx => EventRules.SpecialTargets.Contains(fx.Target) || s.Indicators.ContainsKey(fx.Target)))
            {
                var change = fx.Delta;
                if (fx.Target == "treasury")
                {
                    // R-MONEY-003: the cost first, then what was borrowed for it, as every other payment borrows.
                    why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})"));
                    if (fx.Delta < 0) EconomySystem.PayOrBorrow(c, s, -fx.Delta, $"for {title}", why);
                    else s.Treasury += fx.Delta;
                    continue;
                }
                if (fx.Target == "fx_reserves") s.FxReserves += fx.Delta;
                else change = EffectsSystem.Shift(c, s, fx.Target, fx.Delta);
                why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})"));
            }
            foreach (var kv in o.SetPolicy.Where(k => s.Policies.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Policies[kv.Key] = kv.Value;
                why.Add(new Cause("events", kv.Key, kv.Value, $"{PlayerText.Target(c, kv.Key)} set to {PlayerText.Percent(kv.Value)} ({title})"));
            }
            if (o.SetRung.HasValue) SecuritySystem.SetRung(c, s, o.SetRung.Value, why);
        }

        /// <summary>Unanswered events go to the responsible minister: with probability competence/10 they pick the
        /// recommended option (else the first), otherwise a random option. In Historical mode a hard-anchor event
        /// resolves to its historical option without drawing from the RNG (major anchors happen). An objective's card has one
        /// option and resolves to it without a draw (R-OBJ-003: GDD Phase 1 adds no RNG draw).</summary>
        public static void Delegate(Content c, GameState s, List<Cause> why)
        {
            var rng = new Rng(s.RngState);
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                var minister = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office);
                if (e.Objective != "")
                {
                    Resolve(c, s, id, e.Options[0], $"delegated to {minister?.Name ?? "the cabinet"}", why);
                    continue;
                }
                var competence = (minister?.Competence ?? 5) / 10.0;
                var anchored = s.Mode == Mode.Historical && e.Anchor == "hard" ? e.Options.FirstOrDefault(o => o.Historical) : null;
                var option = anchored ?? (rng.NextDouble() < competence
                    ? e.Options.FirstOrDefault(o => o.Recommended) ?? e.Options[0]
                    : e.Options[(int)(rng.NextDouble() * e.Options.Count)]);
                Resolve(c, s, id, option, $"delegated to {minister?.Name ?? "the cabinet"}", why);
            }
            s.RngState = rng.State;
        }

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));

        /// <summary>Event text as the player sees it now: names follow the player's appointments.</summary>
        public static string Render(Content c, GameState s, GameEvent e) => Cabinet.Fill(c, s, e.Text, FillDate(e, s.Date));
    }
}
