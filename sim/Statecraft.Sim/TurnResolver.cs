using System;
using System.Collections.Generic;
using System.Linq;

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
            EnemyAI.Order(c, s, why, next);               // 1. enemy orders, in ascending unit id (R-ENEMY-001)
            Combat.Resolve(c, s, why);                    // 2. combat, in ascending attacker id (R-COMBAT-002)
            SettlementControl.Capture(c, s, next, why);   // 3. capture, in ordinal settlement id (R-CTRL-004, R-CTRL-005)
            Orders.Settle(s, why);                        // 3b. after combat: fortify given this turn is set, and lost where a unit left its hex (R-ORDER-002)
            Training.Progress(c, s, why);                 // 4. training
            Projects.Advance(c, s, next, why);            // 5. construction
            Projects.AdvanceCity(c, s, next, why);        //    and the city projects (R-PROJ-003)
            Cities.Produce(c, s, r, yearFraction, why);   // 6. production
            Pools.Harvest(c, s, r, why);                  //    and the plowed fields' Food (R-FIELD-002)
            Pools.Consume(c, s, r, why);                  // 7. consumption: the nation eats, then imports (R-FOOD-002)
            Prisoners.Upkeep(c, s, r, why);               //    then the prisoners eat, and the soldiers held are paid for (R-POW-002, R-POW-005)
            EconomySystem.PayCivilianUpkeep(c, s, r, yearFraction, why); //    and pays Money upkeep (R-MONEY-002)
            Cities.House(c, s, r, why);                      // 8. housing, then the housing condition; Social cohesion is morale, which the effects web moves after the resolution
            Cities.UpdateCondition(c, s, r, why);
            Cities.Arrive(c, s, r, next, why);            // 9. arrivals
            StartingForces.Arrive(c, s, next);            //    and the brigades dated after the start (R-UNIT-009)
            // 10. Occupation timers: Phase 6 PR 6. On the turn that reaches the game's end, a capture still awaiting the choice
            //     counts as Raid (R-CTRL-008, #797).
            if (next >= c.End) SettlementControl.EndPending(c, s, why);
            Objectives.Complete(c, s, r, next, why);      // 11. objectives and score
            Defeat(s, r, why);                            //     then the economic defeat (D-B5, R-END-002)
            WarLoss(c, s, why);                           //     and the war loss (R-END-003)
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

        /// <summary>R-END-003: an enemy military unit holding the hex of one of the player's GDD cities at the turn's
        /// end ends the game at once with defeat_war (Q-WAR-5; Q-DEF-3, owner, 17 Oct 2026, #784: one turn is enough, any
        /// of the GDD cities, at any time, truces included). A halutz or the player's own unit on the hex ends nothing
        /// (R-UNIT-001: a halutz has no side), and a game the economic count has just ended this turn keeps its code.</summary>
        static void WarLoss(Content c, GameState s, List<Cause> why)
        {
            if (s.GameOver != null) return;
            var cities = new HashSet<(int Q, int R)>();
            foreach (var city in c.Cities)
            {
                var h = c.SettlementHexes.First(x => x.Id == city.Id);   // every city has one (ValidateGdd)
                cities.Add((h.Q, h.R));
            }
            foreach (var u in s.Units.Values)
            {
                if (u.Side == "" || u.Side == Movement.PlayerSide) continue;   // not an enemy (Movement's reading of "the enemy")
                if (!cities.Contains((u.Q, u.R))) continue;
                s.GameOver = GameOverCodes.DefeatWar;
                why.Add(new Cause("era", s.GameOver, u.Id, PlayerText.GameOver(s.GameOver)));   // the game's end, grouped with the MVP end
                return;
            }
        }
    }
}
