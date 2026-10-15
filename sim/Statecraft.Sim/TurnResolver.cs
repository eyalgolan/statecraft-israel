using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 turn's resolution phase (spec 2026-10-12 section 2.3), in the fixed order the owner approved
    /// (D-A6, #456). Simulation.Step calls it after the orders and before the legacy systems, only when systems.loop is gdd.
    /// Steps not built yet are named where they will go.</summary>
    public static class TurnResolver
    {
        /// <summary>Resolves the turn that starts on s.Date and ends on <paramref name="next"/> (<paramref name="yearFraction"/>
        /// of a year long).</summary>
        public static void Resolve(Content c, GameState s, DateTime next, double yearFraction, List<Cause> why)
        {
            if (!c.IsGdd) return;
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires it in the GDD loop
            // 1-3. Enemy orders, combat, capture: Phase 5.
            Training.Progress(c, s, why);                 // 4. training
            Projects.Advance(c, s, next, why);            // 5. construction
            Cities.Produce(c, s, r, yearFraction, why);   // 6. production
            // 7. Consumption: Phase 2.
            Cities.House(c, s, why);                      // 8. housing, then Social cohesion
            Cities.UpdateCohesion(s, r);
            Cities.Arrive(c, s, r, next, why);            // 9. arrivals
            // 10. Occupation timers: Phase 6.
            Objectives.Complete(c, s, r, next, why);      // 11. objectives and score
        }
    }
}
