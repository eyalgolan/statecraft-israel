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

        /// <summary>R-ALT-001 and R-ALT-002: one requirement holds.</summary>
        public static bool Met(Content c, GameState s, EventRequirement r)
        {
            if (r.Diverged) return Diverged(c, s, r.Event);
            return s.ChosenOptions.TryGetValue(r.Event, out var picked) && picked == r.Option;
        }

        /// <summary>R-ALT-001: every requirement of the event holds (true when it has none).</summary>
        public static bool AllMet(Content c, GameState s, GameEvent e) => e.Requires.All(r => Met(c, s, r));
    }
}
