using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Where the player's course stands against the record. Every answer is derived from
    /// <see cref="GameState.ChosenOptions"/> and the content, so nothing here is state
    /// (docs/superpowers/plans/2026-10-17-alternate-timelines.md, A.4.1).</summary>
    public static class Timeline
    {
        public static EventOption? HistoricalOption(GameEvent e) => e.Options.FirstOrDefault(o => o.Historical);

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>R-ALT-002: the event was resolved, it has a historical option, and the chosen option is another.</summary>
        public static bool Diverged(Content c, GameState s, string eventId)
        {
            if (!s.ChosenOptions.TryGetValue(eventId, out var picked)) return false;
            var historical = HistoricalOption(Get(c, eventId));
            return historical != null && picked != historical.Id;
        }

        /// <summary>R-ALT-009: a consequence card's chosen option rejoined this event to the record.</summary>
        public static bool Rejoined(Content c, GameState s, string eventId) =>
            c.Events.Any(e => e.Kind == EventKinds.Consequence && s.ChosenOptions.TryGetValue(e.Id, out var o)
                              && e.Options.Any(x => x.Id == o && x.Rejoin == eventId));

        /// <summary>R-ALT-001 and R-ALT-002: one requirement holds.</summary>
        public static bool Met(Content c, GameState s, EventRequirement r)
        {
            if (r.Diverged) return Diverged(c, s, r.Event);
            if (!s.ChosenOptions.TryGetValue(r.Event, out var picked)) return false;
            if (picked == r.Option) return true;
            // R-ALT-009: after a rejoin, the record's own option counts as taken; `diverged` stays true.
            return Rejoined(c, s, r.Event) && HistoricalOption(Get(c, r.Event))?.Id == r.Option;
        }

        /// <summary>R-ALT-001: every requirement of the event holds (true when it has none).</summary>
        public static bool AllMet(Content c, GameState s, GameEvent e) => e.Requires.All(r => Met(c, s, r));

        /// <summary>R-ALT-010: the first variant, in file order, whose every `when` entry holds; null for the event's own text.</summary>
        public static EventVariant? Variant(Content c, GameState s, GameEvent e) =>
            e.Variants.FirstOrDefault(v => v.When.All(r => Met(c, s, r)));

        /// <summary>One row of the timeline screen: the player's answer beside the record's.</summary>
        public sealed class TimelineEntry
        {
            public TimelineEntry(string ev, string chosen, string historical, bool diverged, string rejoinedBy)
            { Event = ev; Chosen = chosen; Historical = historical; Diverged = diverged; RejoinedBy = rejoinedBy; }
            public string Event { get; } public string Chosen { get; } public string Historical { get; }
            public bool Diverged { get; } public string RejoinedBy { get; }
        }

        /// <summary>R-ALT-011: every resolved event, by window start then ordinal id. <c>Historical</c> is empty for an event
        /// with no historical option; <c>RejoinedBy</c> is the consequence card whose option rejoined it, or empty.</summary>
        public static IReadOnlyList<TimelineEntry> Entries(Content c, GameState s) =>
            c.Events.Where(e => s.ChosenOptions.ContainsKey(e.Id))
                .OrderBy(e => e.From).ThenBy(e => e.Id, StringComparer.Ordinal)
                .Select(e => new TimelineEntry(e.Id, s.ChosenOptions[e.Id], HistoricalOption(e)?.Id ?? "", Diverged(c, s, e.Id),
                    c.Events.Where(k => k.Kind == EventKinds.Consequence && s.ChosenOptions.TryGetValue(k.Id, out var o)
                                        && k.Options.Any(x => x.Id == o && x.Rejoin == e.Id))
                            .Select(k => k.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault() ?? ""))
                .ToList();

        /// <summary>R-ALT-011: the road a consequence card follows, as the player took it: the first requirement that names a
        /// road not taken, with the option chosen there. Null for any other event.</summary>
        public static (string Event, string Option)? Origin(Content c, GameState s, GameEvent card)
        {
            if (card.Kind != EventKinds.Consequence) return null;
            var r = card.Requires.First(x => EventRules.IsAlternativeRoad(c, x));
            return (r.Event, s.ChosenOptions.TryGetValue(r.Event, out var o) ? o : r.Option);
        }
    }
}
