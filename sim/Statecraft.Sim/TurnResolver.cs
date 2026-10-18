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
            Projects.AdvanceCity(c, s, next, why);        //    and the city projects (R-PROJ-003)
            Cities.Produce(c, s, r, yearFraction, why);   // 6. production
            Pools.Harvest(c, s, r, why);                  //    and the plowed fields' Food (R-FIELD-002)
            Pools.Consume(c, s, r, why);                  // 7. consumption: the nation eats, then imports (R-FOOD-002)
            EconomySystem.PayCivilianUpkeep(c, s, r, yearFraction, why); //    and pays Money upkeep (R-MONEY-002)
            Cities.House(c, s, why);                      // 8. housing, then the housing condition; Social cohesion is morale, which the effects web moves after the resolution
            Cities.UpdateCondition(c, s, r, why);
            Cities.Arrive(c, s, r, next, why);            // 9. arrivals
            // 10. Occupation timers: Phase 6.
            Objectives.Complete(c, s, r, next, why);      // 11. objectives and score
            Defeat(s, r, why);                            //     then the defeat (D-B5); the war loss waits for Phase 5
        }

        /// <summary>R-END-001, R-END-002: counts the turn ends with Money (the debt at or above mvp.debt_ceiling) and Food (an
        /// empty national stock) both exhausted; a turn with either not exhausted resets the count, and mvp.defeat_turns in a
        /// row end the game. The debt is read as this step finds it: the turn's deficit is borrowed after the resolution, so
        /// it counts from the next turn's end.</summary>
        static void Defeat(GameState s, MvpRules r, List<Cause> why)
        {
            s.ExhaustedTurns = s.Debt >= r.DebtCeiling!.Value && s.Food == 0 ? s.ExhaustedTurns + 1 : 0;
            if (s.ExhaustedTurns < r.DefeatTurns!.Value) return;
            s.GameOver = GameOverCodes.DefeatEconomy;
            why.Add(new Cause("era", s.GameOver, s.ExhaustedTurns, PlayerText.GameOver(s.GameOver)));   // the game's end, grouped with the MVP end
        }
    }
}
