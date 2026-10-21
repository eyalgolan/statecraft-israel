using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The end report (D-H4; GDD Phase 7 PR 7, #810), folding in #19's legacy report: what the end screen reads once
    /// <see cref="GameState.GameOver"/> is set. A pure reading of the final state and content (R-END-005): it computes no
    /// rule and writes nothing, so calling it twice gives the same report and leaves the hash as it was. The score is the
    /// state's own total; its parts wait for #809, because <see cref="Objectives.Record"/> scores with the turn-start rules,
    /// which a later reading cannot always recover across an era boundary.</summary>
    public sealed class EndReport
    {
        /// <summary>Why the game ended, one of the <see cref="GameOverCodes"/> (PlayerText.GameOver words it).</summary>
        public string Outcome { get; }
        /// <summary>The date the game ended on (the state's date).</summary>
        public DateTime Date { get; }
        /// <summary><see cref="GameState.Score"/> as the game left it.</summary>
        public int Score { get; }
        /// <summary>Every objective, in objectives.yaml's order (Q-CARD-2), completed or not.</summary>
        public IReadOnlyList<EndObjective> Objectives { get; }
        /// <summary>The GDD's two national parameters (0 to 1: morale shown as Social cohesion, intl_standing as International
        /// Support), shown without a historical value (Q-END-2).</summary>
        public double SocialCohesion { get; }
        public double InternationalSupport { get; }
        /// <summary>The Jewish population at the end (<see cref="GameState.TotalPopulation"/>).</summary>
        public double Population { get; }
        /// <summary>The one comparison with history (Q-END-2, R-END-006): the latest CBS anchor (population.yaml) dated on or
        /// before <see cref="Date"/>, with its sources; null when the game ended before the first anchor.</summary>
        public PopulationAnchor? PopulationHistory { get; }

        EndReport(string outcome, DateTime date, int score, IReadOnlyList<EndObjective> objectives, double cohesion, double support,
            double population, PopulationAnchor? history)
        {
            Outcome = outcome; Date = date; Score = score; Objectives = objectives; SocialCohesion = cohesion;
            InternationalSupport = support; Population = population; PopulationHistory = history;
        }

        /// <summary>The report of a game that has ended; a game still on has none.</summary>
        public static EndReport For(Content c, GameState s)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s.GameOver == null) throw new InvalidOperationException("the game has not ended, so it has no end report");
            var objectives = c.Objectives.Select(o => s.Objectives.TryGetValue(o.Id, out var done)
                ? new EndObjective(o.Id, o.Card, o.ReferenceDate, done, Sim.Objectives.MonthsFromHistory(done, o.ReferenceDate))
                : new EndObjective(o.Id, o.Card, o.ReferenceDate, null, null)).ToList();
            // The anchors are few and dated; the latest one not after the end is the comparison (ties cannot happen: dates are unique).
            var history = c.PopulationAnchors.Where(a => a.Date <= s.Date).OrderBy(a => a.Date).LastOrDefault();
            return new EndReport(s.GameOver, s.Date, s.Score, objectives, s.Indicators["morale"], s.Indicators["intl_standing"],
                s.TotalPopulation, history);
        }
    }

    /// <summary>One objective in the end report: its card, the historical date, and the player's date with the months from
    /// history (<see cref="Objectives.MonthsFromHistory"/>: positive when early) when it was completed.</summary>
    public sealed class EndObjective
    {
        public string Id { get; }
        /// <summary>The card event's id (content/events/), whose title the screen shows.</summary>
        public string Card { get; }
        public DateTime Historical { get; }
        public DateTime? Completed { get; }
        public int? MonthsFromHistory { get; }

        public EndObjective(string id, string card, DateTime historical, DateTime? completed, int? monthsFromHistory)
        {
            Id = id; Card = card; Historical = historical; Completed = completed; MonthsFromHistory = monthsFromHistory;
        }
    }
}
