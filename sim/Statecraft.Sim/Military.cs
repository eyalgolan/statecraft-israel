using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Service and Money upkeep for the GDD's military formations (Phase 5 PR 3).</summary>
    public static class Military
    {
        /// <summary>R-UNIT-004: recruits already in play serve this turn. Run before training completes, so training time
        /// never counts as service. Promotion preserves the formation and its standing order; combat adds no service. Under
        /// exact gdd-0.3 service is a statistic only and never promotes: veterancy comes from combat experience (R-UNIT-011, Q16).
        /// It runs on an initialized Historical ledger (the dark start, #1725 PR 7): service stays on the formation, so it reads
        /// and writes no ledger record; a synthetic or Open ledger stays refused (R-POP-002). Maintainer calls by coordinator
        /// eyalgolan-30, 8 November 2026, not owner rulings.</summary>
        public static void Serve(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: service is the formation's, no ledger field
            var byService = !Gdd03Military.Applies(c);
            foreach (var u in s.Units.Values.ToList())
            {
                if (!Promotes(u)) continue;
                var served = new Unit(u.Id, u.Type, u.Q, u.R, u.Service + 1, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp);
                s.Units[u.Id] = byService && served.Service >= r.VeteranSoldierService!.Value ? Promote(served, why) : served;
            }
        }

        /// <summary>R-UNIT-004, R-UNIT-011: an Israeli recruit formation is the one unit that becomes a veteran_soldier.</summary>
        internal static bool Promotes(Unit u) => u.Type == "recruit" && u.Side == "israel";

        /// <summary>R-UNIT-004, R-UNIT-011: the recruit <paramref name="u"/> as a veteran_soldier, keeping id, hex, service, side,
        /// strength, standing order, home, Full and combat experience, with the Why cause.</summary>
        internal static Unit Promote(Unit u, List<Cause> why)
        {
            why.Add(new Cause("city", "units", u.Strength, "Recruits became veteran soldiers", key: CauseKeys.RecruitsVeteran));
            return new Unit(u.Id, VeteranSoldier, u.Q, u.R, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp);
        }

        /// <summary>The Israeli veteran infantry type the loader requires when recruits exist (R-UNIT-004).</summary>
        internal const string VeteranSoldier = "veteran_soldier";

        /// <summary>R-UNIT-010: step 7 pays per Israeli military formation, including this turn's graduates. The national
        /// Treasury borrows a shortfall (R-MONEY-003); upkeep never consumes Arms (Q-UNIT-3).</summary>
        public static void PayUpkeep(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            var count = s.Units.Values.Count(u => u.Side == "israel" && c.UnitTypes.Any(t => t.Id == u.Type && t.Side == "israel"));
            var cost = count * r.MilitaryUpkeepPerTurn!.Value;
            if (cost == 0) return;
            why.Add(new Cause("economy", "treasury", -cost, $"{PlayerText.Change(c, "treasury", -cost)} (military upkeep)", key: CauseKeys.MilitaryUpkeep));
            EconomySystem.PayNamedOrBorrow(c, s, cost, "for military upkeep", why, CauseKeys.Purpose.MilitaryUpkeep);
        }
    }
}
