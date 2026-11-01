using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EconomySystem
    {
        public const string FxCrisis = "fx_crisis";

        /// <summary>The command kind that converts foreign currency into Money (R-MONEY-004).</summary>
        public const string ConvertDollarsKind = "convert_dollars";
        public const string RefusedDollarsMinimum = "Convert at least $1m";
        public const string RefusedDollarsNotAtWar = "Dollars can be converted only during the war";
        public const string RefusedDollarsMonthly = "That is more dollars than may be converted this month";
        public const string RefusedDollarsTotal = "That would pass the total of dollars that may be converted";
        public const string RefusedDollarsReserves = "There are not enough dollars in the reserves";

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
            PopulationLedger.RefuseAggregate(s);
            var r = c.RulesAt(s.Date); var pop = s.TotalPopulation;
            return new TreasuryFlows(r.TaxPerCapitaPerYear * pop * yearFraction, r.SpendingPerCapitaPerYear * pop * yearFraction,
                MobilizationCost(c, s.Date, s.Mobilization, yearFraction));
        }

        /// <summary>The reserve call-up's cost (pay and lost work) for <paramref name="level"/> (0..1) over a share of a year:
        /// the one rule Flows and the Defence screen both use (M3 final review M5).</summary>
        public static double MobilizationCost(Content c, DateTime date, double level, double yearFraction = 1.0) => c.RulesAt(date).MobilizationCostPerYear * level * yearFraction;

        /// <summary>Pays <paramref name="cost"/> from the Treasury, borrowing what it lacks: the shortfall goes to Debt and the
        /// Treasury stops at 0, as for map actions and the deficit. <paramref name="purpose"/> ends the cause ("for training").
        /// The GDD orders pay through it (owner decision "Allow borrowing", 2026-10-15; R-TRAIN-001, R-SITE-001).
        /// <paramref name="purposeKey"/> (a CauseKeys.Purpose) and <paramref name="purposeArgs"/> say the same purpose for the
        /// UI (CauseKeys.Borrowed, B1); <paramref name="purpose"/> stays the English Message.</summary>
        public static void PayOrBorrow(Content c, GameState s, double cost, string purpose, List<Cause> why, string purposeKey, params string[] purposeArgs)
        {
            s.Treasury -= cost;
            if (s.Treasury >= 0) return;
            s.Debt += -s.Treasury;
            why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {PlayerText.Money(c, "debt", -s.Treasury)} {purpose}",
                key: CauseKeys.Borrowed, args: new[] { purposeKey }.Concat(purposeArgs).ToList()));
            s.Treasury = 0;
        }

        /// <summary>R-MONEY-004 (#813; owner, 2 November 2026: "Spend the dollars (Recommended)"): during the war (rung 4 or 5)
        /// <paramref name="amount"/> US$ millions of foreign currency leave the reserves and amount times the mvp.il_per_dollar rate in
        /// force on the turn's date, in IL millions, joins the Treasury, so the turn's deficit borrows that much less (R-MONEY-003). Refused, state untouched,
        /// below $1m, outside the war (<paramref name="startRung"/>, the rung the turn started at, below 4), past
        /// mvp.dollars_per_turn with <paramref name="thisTurn"/> (this turn's earlier conversions), past mvp.dollars_total, or
        /// below 0 reserves. Step has already refused it in the legacy loop.</summary>
        public static string? ConvertDollars(Content c, GameState s, int amount, int thisTurn, int startRung, List<Cause> why)
        {
            var refused = DollarsRefusal(c, s, amount, thisTurn, startRung);
            if (refused != null) return refused;
            var money = amount * c.RulesAt(s.Date).Mvp!.IlPerDollarOn(s.Date);
            s.FxReserves -= amount;
            s.Treasury += money;
            s.DollarsConverted += amount;
            why.Add(new Cause("economy", "treasury", money,
                $"Converted {PlayerText.Money(c, "fx_reserves", amount)} of foreign currency into Money: {PlayerText.MoneyChange(c, "treasury", money)}",
                key: CauseKeys.DollarsConverted, args: new[] { CauseKeys.Number(amount) }));
            return null;
        }

        /// <summary>R-MONEY-004's refusal of <paramref name="amount"/> in <paramref name="s"/>, after <paramref name="thisTurn"/>
        /// dollars converted earlier in the same turn, or null when it would be carried out. The war is
        /// <paramref name="startRung"/>, the rung the turn started at (an event's set_rung earlier in the turn does not change
        /// it), else the state's rung, which is the turn's start before End turn. ConvertDollars refuses on it, and the Defence
        /// screen says it before End turn.</summary>
        public static string? DollarsRefusal(Content c, GameState s, int amount, int thisTurn = 0, int? startRung = null)
        {
            var r = c.RulesAt(s.Date).Mvp!;   // the loader requires the three values in the GDD loop
            if (amount < 1) return RefusedDollarsMinimum;
            if ((startRung ?? s.Rung) < WarRung) return RefusedDollarsNotAtWar;
            if ((long)thisTurn + amount > r.DollarsPerTurn!.Value) return RefusedDollarsMonthly;
            if ((long)s.DollarsConverted + amount > r.DollarsTotal!.Value) return RefusedDollarsTotal;
            if (s.FxReserves - amount < 0) return RefusedDollarsReserves;
            return null;
        }

        /// <summary>The lowest rung of the war (R-MONEY-004): the rungs 4 and 5 of the #813 measurement, 15 May 1948 to 15
        /// March 1949 in the historical run.</summary>
        public const int WarRung = 4;

        /// <summary>R-MONEY-002: pays civilian-place upkeep in step 7, prorated by the turn's share of a year.</summary>
        public static void PayCivilianUpkeep(Content c, GameState s, MvpRules r, double yearFraction, List<Cause> why)
        {
            var cost = Cities.AnnualUpkeep(c, s, r) * yearFraction;
            if (cost == 0) return;
            why.Add(new Cause("economy", "treasury", -cost, $"{PlayerText.Change(c, "treasury", -cost)} (civilian upkeep)", key: CauseKeys.CivilianUpkeep));
            PayOrBorrow(c, s, cost, "for civilian upkeep", why, CauseKeys.Purpose.CivilianUpkeep);
        }

        public static void Apply(Content c, GameState s, double yearFraction, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            var r = c.RulesAt(s.Date); var pop = s.TotalPopulation;
            var flows = Flows(c, s, yearFraction);
            var revenue = flows.Tax; var spending = flows.Spending; var mobilization = flows.Mobilization;
            s.Treasury += revenue - spending - mobilization;
            why.Add(new Cause("economy", "treasury", revenue - spending - mobilization,
                $"{PlayerText.Change(c, "treasury", revenue - spending - mobilization)} (tax {PlayerText.Money(c, "treasury", revenue)}, spending {PlayerText.Money(c, "treasury", spending)}, reserve call-up {PlayerText.Money(c, "treasury", mobilization)})",
                key: CauseKeys.MoneyFlows, args: new[] { CauseKeys.Number(revenue), CauseKeys.Number(spending), CauseKeys.Number(mobilization) }));
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                why.Add(new Cause("economy", "debt", -s.Treasury, $"borrowed {PlayerText.Money(c, "debt", -s.Treasury)} to cover the deficit",
                    key: CauseKeys.Borrowed, args: new[] { CauseKeys.Purpose.Deficit }));
                s.Treasury = 0;
            }

            var exports = r.ExportsPerYear * yearFraction;
            var inflow = r.CapitalInflowPerYear * yearFraction;
            var imports = r.ImportsPerCapitaPerYear * pop * yearFraction;
            var net = exports + inflow - imports;
            s.FxReserves += net;
            why.Add(new Cause("economy", "fx_reserves", net, $"{PlayerText.Change(c, "fx_reserves", net)} (exports {PlayerText.Money(c, "fx_reserves", exports)}, donations and loans {PlayerText.Money(c, "fx_reserves", inflow)}, imports {PlayerText.Money(c, "fx_reserves", imports)})",
                key: CauseKeys.FxFlows, args: new[] { CauseKeys.Number(exports), CauseKeys.Number(inflow), CauseKeys.Number(imports) }));

            bool on = s.ActiveSituations.Contains(FxCrisis);
            if (!on && s.FxReserves < 0) { s.ActiveSituations.Add(FxCrisis); why.Add(new Cause("economy", FxCrisis, 1, "Foreign currency reserves are exhausted", key: CauseKeys.FxCrisisBegan)); }
            else if (on && s.FxReserves > 0.25 * r.FxStart) { s.ActiveSituations.Remove(FxCrisis); why.Add(new Cause("economy", FxCrisis, -1, "Foreign currency crisis eased", key: CauseKeys.FxCrisisEased)); }

            // The consequence: with no currency to pay for them, food imports are cut while the crisis lasts. In the GDD loop
            // the cut falls on the Food stock's imports instead (R-FOOD-002, Q-POOL-4), and food_supply follows the stock.
            if (!c.IsGdd && s.ActiveSituations.Contains(FxCrisis) && r.FxCrisisFoodPerYear < 0)
            {
                var change = EffectsSystem.Shift(c, s, "food_supply", r.FxCrisisFoodPerYear * yearFraction);
                if (change != 0)
                    why.Add(new Cause("economy", "food_supply", change, $"{PlayerText.Change(c, "food_supply", change)} (foreign currency crisis: food imports cut)", key: CauseKeys.FxCrisisFood));
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

        /// <summary>#1109: whether the Budget screen's <paramref name="draft"/> differs from what End turn will apply (the
        /// AllocateBudget in <paramref name="pending"/>, this turn's queue, else today's budget) and Submit can still make them
        /// agree this turn: a draft back at today's budget withdraws the queued one for nothing; any other needs the Attention
        /// left once a queued budget is refunded (SimBridge.CanPay's rule) to pay for allocate_budget. False when the game is
        /// over. A query only: the side menu's Budget badge reads it, so the trap of an edited budget that End turn never
        /// applies is visible.</summary>
        public static bool Unsubmitted(Content c, GameState s, IReadOnlyDictionary<string, double> draft, IEnumerable<Command> pending)
        {
            var list = pending.ToList();
            var queued = list.OfType<AllocateBudget>().LastOrDefault();
            if (s.GameOver != null || Same(draft, queued?.Shares ?? s.Budget)) return false;
            return Same(draft, s.Budget) || !c.UsesAttention
                || AttentionSystem.Cost(c, s, "allocate_budget") <= AttentionSystem.Left(c, s, list.Where(x => !(x is AllocateBudget))) + 1e-9;
        }

        /// <summary>The Budget screen's comparison: every line of either map within 1e-9, a missing line counting as 0.</summary>
        static bool Same(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b) =>
            a.Keys.Union(b.Keys).All(k => Math.Abs((a.TryGetValue(k, out var x) ? x : 0) - (b.TryGetValue(k, out var y) ? y : 0)) <= 1e-9);
    }
}
