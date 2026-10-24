using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Service and Money upkeep for the GDD's military formations (Phase 5 PR 3).</summary>
    public static class Military
    {
        /// <summary>R-UNIT-004: recruits already in play serve this turn. Run before training completes, so training time
        /// never counts as service. Promotion preserves the formation and its standing order; combat adds no service.</summary>
        public static void Serve(GameState s, MvpRules r, List<Cause> why)
        {
            foreach (var u in s.Units.Values.ToList())
            {
                if (u.Type != "recruit" || u.Side != "israel") continue;
                var service = u.Service + 1;
                var veteran = service >= r.VeteranSoldierService!.Value;
                s.Units[u.Id] = new Unit(u.Id, veteran ? "veteran_soldier" : u.Type, u.Q, u.R, service,
                    u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full);
                if (veteran) why.Add(new Cause("city", "units", u.Strength, "Recruits became veteran soldiers"));
            }
        }

        /// <summary>R-UNIT-010: step 7 pays per Israeli military formation, including this turn's graduates. The national
        /// Treasury borrows a shortfall (R-MONEY-003); upkeep never consumes Arms (Q-UNIT-3).</summary>
        public static void PayUpkeep(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            var count = s.Units.Values.Count(u => u.Side == "israel" && c.UnitTypes.Any(t => t.Id == u.Type && t.Side == "israel"));
            var cost = count * r.MilitaryUpkeepPerTurn!.Value;
            if (cost == 0) return;
            why.Add(new Cause("economy", "treasury", -cost, $"{PlayerText.Change(c, "treasury", -cost)} (military upkeep)"));
            EconomySystem.PayOrBorrow(c, s, cost, "for military upkeep", why);
        }
    }
}
