using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The end report (D-H4; GDD Phase 7 PR 7, #810), folding in #19's legacy report: what the end screen reads once
    /// <see cref="GameState.GameOver"/> is set. A pure reading of the final state and content (R-END-005): it computes no
    /// rule and writes nothing, so calling it twice gives the same report and leaves the hash as it was. The score is the
    /// state's own total and its parts are <see cref="Sim.ScoreParts.For"/>'s reading of what the state recorded as it scored
    /// (#809, R-SCORE-001): <see cref="Objectives.Record"/> scores with the turn-start rules, which a later reading cannot
    /// always recover across an era boundary, so nothing is recomputed here.</summary>
    public sealed class EndReport
    {
        /// <summary>Why the game ended, one of the <see cref="GameOverCodes"/> (PlayerText.GameOver words it).</summary>
        public string Outcome { get; }
        /// <summary>The date the game ended on (the state's date).</summary>
        public DateTime Date { get; }
        /// <summary><see cref="GameState.Score"/> as the game left it.</summary>
        public int Score { get; }
        /// <summary>The parts of <see cref="Score"/> (R-SCORE-001); they sum to it.</summary>
        public ScoreParts ScoreParts { get; }
        /// <summary>What completing every objective on its historical date scores (Q-SCORE-2, owner, 17 October 2026): objective_points
        /// for each objective, the parts of one part_of group counted once (R-OBJ-006), and no timing bonus on the day. Each pays with
        /// the rules of the turn that ends on its reference date (a group's: its latest part's), as scoring would. The end screen
        /// shows it beside the score; 0 when the content has no GDD objectives.</summary>
        public int ReferenceScore { get; }
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
        /// <summary>The prisoners returned in the exchanges (Q-END-3, R-POW-004), in people as Defence shows them
        /// (<see cref="GameState.PrisonersReturned"/> units times the people per unit); 0 when none went home.</summary>
        public int PrisonersReturned { get; }

        EndReport(string outcome, DateTime date, int score, ScoreParts scoreParts, int referenceScore, IReadOnlyList<EndObjective> objectives, double cohesion, double support,
            double population, PopulationAnchor? history, int prisonersReturned)
        {
            Outcome = outcome; Date = date; Score = score; ScoreParts = scoreParts; ReferenceScore = referenceScore; Objectives = objectives; SocialCohesion = cohesion;
            InternationalSupport = support; Population = population; PopulationHistory = history;
            PrisonersReturned = prisonersReturned;
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
            // The base pays once per objective with no part_of and once per part_of group, as Objectives.Record awards it: a group
            // with its last part, so on the latest part's reference date. The two are separate namespaces: a group id may equal a
            // standalone objective's id and both still pay.
            var paysOn = c.Objectives.Where(o => o.PartOf == "").Select(o => o.ReferenceDate)
                .Concat(c.Objectives.Where(o => o.PartOf != "").GroupBy(o => o.PartOf, StringComparer.Ordinal).Select(g => g.Max(o => o.ReferenceDate)));
            // Scoring reads the rules of the date the turn started from. A turn that ends on a reference date never crosses an era's
            // start (Simulation.TurnEnd cuts it there), so the day before that date is in the era the turn started in.
            var reference = paysOn.Sum(d => c.RulesAt(d.AddDays(-1)).Mvp?.ObjectivePoints ?? 0);
            return new EndReport(s.GameOver, s.Date, s.Score, Sim.ScoreParts.For(s), reference, objectives, s.Indicators["morale"], s.Indicators["intl_standing"],
                s.TotalPopulation, history, s.PrisonersReturned.Values.Sum() * (c.RulesAt(s.Date).Mvp?.PeoplePerUnit ?? 0));   // no prisoners without the GDD rules
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
