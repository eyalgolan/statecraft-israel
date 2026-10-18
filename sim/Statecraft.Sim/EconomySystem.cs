using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EconomySystem
    {
        public const string FxCrisis = "fx_crisis";

        public static void Init(Content c, GameState s)
        {
            s.Treasury = c.Rules.TreasuryStart;
            s.FxReserves = c.Rules.FxStart;
            s.Budget = new Dictionary<string, double>(c.Rules.BudgetStart);
        }

        /// <summary>The treasury's money in and out over <paramref name="yearFraction"/> of a year (1.0 = a year): tax,
        /// spending and the cost of the reserve call-up. Apply uses it, and the Budget screen shows it.</summary>
        public readonly struct TreasuryFlows
        {
            public readonly double Tax, Spending, Mobilization;
            public TreasuryFlows(double tax, double spending, double mobilization) { Tax = tax; Spending = spending; Mobilization = mobilization; }
        }

        public static TreasuryFlows Flows(Content c, GameState s, double yearFraction)
        {
            var r = c.RulesAt(s.Date); var pop = s.TotalPopulation;
            return new TreasuryFlows(r.TaxPerCapitaPerYear * pop * yearFraction, r.SpendingPerCapitaPerYear * pop * yearFraction,
                MobilizationCost(c, s.Date, s.Mobilization, yearFraction));
        }

        /// <summary>The reserve call-up's cost (pay and lost work) for <paramref name="level"/> (0..1) over a share of a year:
        /// the one rule Flows and the Defence screen both use (M3 final review M5).</summary>
        public static double MobilizationCost(Content c, DateTime date, double level, double yearFraction = 1.0) => c.RulesAt(date).MobilizationCostPerYear * level * yearFraction;

        /// <summary>Pays <paramref name="cost"/> from the Treasury, borrowing what it lacks: the shortfall goes to Debt and the
        /// Treasury stops at 0, as for map actions and the deficit. <paramref name="purpose"/> ends the cause ("for training").
        /// The GDD orders pay through it (owner decision "Allow borrowing", 2026-10-15; R-TRAIN-001, R-SITE-001).</summary>
        public static void PayOrBorrow(Content c, GameState s, double cost, string purpose, List<Cause> why)
        {
            s.Treasury -= cost;
            if (s.Treasury >= 0) return;
            s.Debt += -s.Treasury;
            why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {PlayerText.Money(c, "debt", -s.Treasury)} {purpose}"));
            s.Treasury = 0;
        }

        /// <summary>R-MONEY-002: pays civilian-place upkeep in step 7, prorated by the turn's share of a year.</summary>
        public static void PayCivilianUpkeep(Content c, GameState s, MvpRules r, double yearFraction, List<Cause> why)
        {
            var cost = Cities.AnnualUpkeep(c, s, r) * yearFraction;
            if (cost == 0) return;
            why.Add(new Cause("economy", "treasury", -cost, $"{PlayerText.Change(c, "treasury", -cost)} (civilian upkeep)"));
            PayOrBorrow(c, s, cost, "for civilian upkeep", why);
        }

        public static void Apply(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            var r = c.RulesAt(s.Date); var pop = s.TotalPopulation;
            var flows = Flows(c, s, yearFraction);
            var revenue = flows.Tax; var spending = flows.Spending; var mobilization = flows.Mobilization;
            s.Treasury += revenue - spending - mobilization;
            why.Add(new Cause("economy", "treasury", revenue - spending - mobilization,
                $"{PlayerText.Change(c, "treasury", revenue - spending - mobilization)} (tax {PlayerText.Money(c, "treasury", revenue)}, spending {PlayerText.Money(c, "treasury", spending)}, reserve call-up {PlayerText.Money(c, "treasury", mobilization)})"));
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {PlayerText.Money(c, "debt", -s.Treasury)} to cover the deficit"));
                s.Treasury = 0;
            }

            var exports = r.ExportsPerYear * yearFraction;
            var inflow = r.CapitalInflowPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            var net = exports + inflow - imports;
            s.FxReserves += net;
            why.Add(new Cause("economy", "fx_reserves", net, $"{PlayerText.Change(c, "fx_reserves", net)} (exports {PlayerText.Money(c, "fx_reserves", exports)}, donations and loans {PlayerText.Money(c, "fx_reserves", inflow)}, imports {PlayerText.Money(c, "fx_reserves", imports)})"));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted")); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased")); }

            // The consequence: with no currency to pay for them, food imports are cut while the crisis lasts. In the GDD loop
            // the cut falls on the Food stock's imports instead (R-FOOD-002, Q-POOL-4), and food_supply follows the stock.
            if (!c.IsGdd && s.ActiveSituations.Contains(FxCrisis) && r.FxCrisisFoodPerYear < 0)
            {
                var change = EffectsSystem.Shift(c, s, "food_supply", r.FxCrisisFoodPerYear * yearFraction);
                if (change != 0)
                    why.Add(new Cause("economy", "food_supply", change, $"{PlayerText.Change(c, "food_supply", change)} (foreign currency crisis: food imports cut)"));
            }
        }

        /// <summary>Returns null on success, or the reason the allocation was rejected.</summary>
        public static string? Allocate(Content c, GameState s, Dictionary<string, double> shares)
        {
            if (shares == null) return "Budget shares are required";
            var unknown = shares.Keys.OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault(k => c.Offices.All(o => o.Id != k) && !c.Rules.BudgetStart.ContainsKey(k));   // ordinal: play and replay name the same office
            if (unknown != null) return "The budget names a ministry that does not exist in this game";
            if (shares.Values.Any(v => double.IsNaN(v) || double.IsInfinity(v))) return "Budget shares must be finite numbers";
            if (!new HashSet<string>(shares.Keys).SetEquals(c.Rules.BudgetStart.Keys))
                return "The budget must give a share to every ministry, and only to them";
            // Summed in key order, not dictionary order, so play and replay agree to the bit whatever order the caller built the map in.
            if (Math.Abs(shares.OrderBy(kv => kv.Key, StringComparer.Ordinal).Sum(kv => kv.Value) - 1.0) > 1e-6) return "Budget shares must sum to 1";
            if (shares.Values.Any(v => v < 0)) return "Budget shares cannot be negative";
            s.Budget = new Dictionary<string, double>(shares);
            return null;
        }
    }
}
