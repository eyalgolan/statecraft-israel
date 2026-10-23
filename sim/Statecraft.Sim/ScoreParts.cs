namespace Statecraft.Sim
{
    /// <summary>The final score's parts (R-SCORE-001; Q-SCORE-1, owner, 17 October 2026, #784): objective base points, their
    /// timing bonuses and prisoner points, nothing else. A pure reading of the parts the state recorded as it scored, so they
    /// always sum to <see cref="GameState.Score"/>; the end report (#810) lists them.</summary>
    public sealed class ScoreParts
    {
        /// <summary>objective_points, once per objective (R-OBJ-002; a multipart objective's with its last part, R-OBJ-006).</summary>
        public int ObjectivePoints { get; }
        /// <summary>The months-early bonuses (R-OBJ-002, R-OBJ-005).</summary>
        public int TimingBonuses { get; }
        /// <summary>points_per_prisoner_unit_returned per unit returned (R-POW-004).</summary>
        public int PrisonerPoints { get; }

        ScoreParts(int objectivePoints, int timingBonuses, int prisonerPoints)
        {
            ObjectivePoints = objectivePoints; TimingBonuses = timingBonuses; PrisonerPoints = prisonerPoints;
        }

        public static ScoreParts For(GameState s) =>
            new ScoreParts(s.ScoreObjectivePoints, s.ScoreTimingBonuses, s.ScorePrisonerPoints);
    }
}
