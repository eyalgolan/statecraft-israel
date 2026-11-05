using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The national Food pool of the GDD v0.1 loop (Phase 2 PR 3; D-C2 "National pools"). One unit of Food is one
    /// population unit's food for one month, the nation eats its population in units each turn, and there is no storage cap
    /// (Q-POOL-2). Food comes in as a monthly import that keeps the stock at cover_months of the need, cut while foreign
    /// currency is exhausted (Q-POOL-4). The stock stops at 0, and what it cannot cover is the unmet share (Q-POOL-5).
    /// food_supply stays the 0 to 1 level the UI and events read, with a term from the stock (maintainer decision of 16
    /// October 2026, #382, option (b)). Runs only when eras.yaml systems.loop is gdd. Integers only, so no rounding drifts.</summary>
    public static class Pools
    {
        /// <summary>The indicator the Food stock feeds (model.yaml; the loader requires it in the GDD loop).</summary>
        public const string FoodIndicator = "food_supply";

        /// <summary>The event-effect target that moves the Food stock itself (R-EFX-001).</summary>
        public const string FoodTarget = "food";

        /// <summary>The event-effect target that moves the Arms stock (R-EFX-001, #781).</summary>
        public const string ArmsTarget = "arms";

        /// <summary>The command kind that buys Arms with Money (R-ARMS-002).</summary>
        public const string BuyArmsKind = "buy_arms";

        /// <summary>R-FOOD-001: the stock starts at cover_months of the start's need, so food_supply starts at its level.</summary>
        public static void Init(Content c, GameState s)
        {
            PopulationLedger.RefuseAggregate(s);
            if (!c.IsGdd) return;
            var r = c.RulesAt(s.Date).Mvp!;
            s.FoodNeed = Need(s, r);
            s.Food = r.CoverMonths!.Value * s.FoodNeed;
            s.FoodUnmet = 0;
            s.Arms = r.ArmsStart!.Value;   // R-ARMS-001; the loader requires it in the GDD loop
        }

        /// <summary>R-ARMS-002's refusal at the debt ceiling (#2198); SimBridge.ReasonKeys gives it the UI's words.</summary>
        public const string RefusedDebtCeiling = "Buying that many Arms would take the debt past its ceiling";

        /// <summary>R-ARMS-002 (D-G6): <paramref name="count"/> Arms join the stock at once, and the Treasury pays
        /// count * arms_price, borrowing what it lacks (R-MONEY-003). Refused, state untouched, for fewer than one unit, a
        /// stock that would pass a whole number's range, or borrowing that would take the debt past mvp.debt_ceiling (#2198,
        /// owner 2 Nov 2026: "Refuse buys over ceiling (Recommended)"; the debt after is PayOrBorrow's, the debt plus what the
        /// Treasury lacks, and exactly at the ceiling is allowed). A purchase the Treasury pays in full borrows nothing and is
        /// never refused at the ceiling. Step has already refused it in the legacy loop.</summary>
        public static string? BuyArms(Content c, GameState s, int count, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            if (count < 1) return "Buy at least 1 Arms";
            if ((long)s.Arms + count > int.MaxValue) return "That is more Arms than the stock can hold";
            if (BorrowsPastDebtCeiling(c, s, count)) return RefusedDebtCeiling;
            var cost = count * c.RulesAt(s.Date).Mvp!.ArmsPrice!.Value;
            EconomySystem.PayOrBorrow(c, s, cost, "for Arms", why, CauseKeys.Purpose.Arms);
            s.Arms += count;
            why.Add(new Cause("economy", "arms", count, FormattableString.Invariant($"Bought {count} Arms, cost {PlayerText.Money(c, "treasury", cost)}"), key: CauseKeys.ArmsBought, args: new[] { CauseKeys.Number(cost) }));
            return null;
        }

        /// <summary>#2198: true when paying for <paramref name="count"/> Arms now would borrow (PayOrBorrow: the Treasury stops at 0
        /// and the rest goes to Debt) and take the debt past mvp.debt_ceiling. Exactly at the ceiling is not past it. BuyArms
        /// refuses on it, and the Defence screen says so before End turn.</summary>
        public static bool BorrowsPastDebtCeiling(Content c, GameState s, int count)
        {
            var r = c.RulesAt(s.Date).Mvp!;
            var left = s.Treasury - count * r.ArmsPrice!.Value;
            return left < 0 && s.Debt - left > r.DebtCeiling!.Value;   // the loader requires both in the GDD loop
        }

        /// <summary>#2206: whether End turn would refuse <paramref name="count"/> Arms for the ceiling (BuyArms, the check above)
        /// if it ran <paramref name="orders"/>, the turn's orders, from <paramref name="turnStart"/>, after Buy queued them: the
        /// purchase takes a queued purchase's slot, else goes last (SimBridge.Queue). The orders run on a copy through
        /// Simulation.Preview, End turn's own order phase (RunOrders: conversions before the purchase, Attention, refusals),
        /// so everything queued before the purchase counts as End turn counts it. The Defence screen's warning.</summary>
        public static bool BorrowsPastDebtCeiling(Content c, GameState turnStart, IReadOnlyList<Command> orders, int count)
        {
            var buy = new BuyArms(count);
            var batch = orders.ToList();
            var slot = batch.FindIndex(x => x is BuyArms);
            if (slot >= 0) batch[slot] = buy; else batch.Add(buy);
            return Simulation.Preview(c, turnStart, batch).Rejected.Any(r => ReferenceEquals(r.Command, buy) && r.Reason == RefusedDebtCeiling);
        }

        /// <summary>The nation's need for one turn: its head count in population units, rounded half away from zero.</summary>
        static int Need(GameState s, MvpRules r) => (int)Math.Round(s.TotalPopulation / r.PeoplePerUnit, MidpointRounding.AwayFromZero);

        /// <summary>R-FIELD-002, step 6 after the cities' production (D-A6): in ordinal site id order, every plowed field (a built
        /// field site, R-FIELD-001) adds field_food_per_turn to the stock, with no workers (Q-FIELD-2). It comes before the
        /// nation eats in step 7, so in a turn with imports the import only tops the stock up by less.</summary>
        public static void Harvest(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            var yield = r.FieldFoodPerTurn!.Value;   // the loader requires it in the GDD loop
            if (yield == 0) return;
            foreach (var f in c.Sites.Where(x => x.Kinds.Contains(Projects.FieldKind)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (!(s.Sites.TryGetValue(f.Id, out var w) && w.BuiltOn != null)) continue;
                s.Food += yield;
                why.Add(new Cause("economy", "food", yield, FormattableString.Invariant($"{f.Name}: the plowed land yielded {yield} units of Food"), key: CauseKeys.FoodHarvest, args: new[] { f.Id }));
            }
        }

        /// <summary>R-FOOD-001 under gdd-0.3 (#2470): the month's need of a population ledger, one ration (one population
        /// unit's food) for each living own population id in <paramref name="snapshot"/>, whatever its status or
        /// assignment. The order of the snapshot does not matter; a repeated id or a missing record is refused.</summary>
        internal static int Rations(IEnumerable<PopulationUnit> snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var ids = new HashSet<int>();
            foreach (var p in snapshot)
                if (p == null || !ids.Add(p.Id)) throw new ArgumentException("A Food snapshot holds each population id once.", nameof(snapshot));
            return ids.Count;
        }

        /// <summary>R-FOOD-002, step 7: the nation eats the turn's need from the stock, as far as the stock goes (it stops at
        /// 0, and the rest is unmet), then the import brings the stock back to cover_months of the need, unless foreign
        /// currency is exhausted (the fx_crisis situation at the turn's start), when no Food is imported. The need is the
        /// aggregate head count; a state with an initialized population ledger is refused here (R-POP-002).</summary>
        public static void Consume(Content c, GameState s, MvpRules r, List<Cause> why) => Consume(c, s, r, null, why);

        /// <summary>R-FOOD-002 under gdd-0.3 (#2470): with a <paramref name="month"/> context the need is <see cref="Rations"/>
        /// of the month's frozen opening snapshot (<see cref="Gdd03SyntheticMonthContext.OpeningPeople"/>), not the live
        /// ledger, so a ledger change before step 7 does not move the month's need. The live ledger is checked first
        /// (<see cref="PopulationLedger.Reconcile"/>) so a bad ledger moves no Food. Without a month it is the aggregate need.</summary>
        internal static void Consume(Content c, GameState s, MvpRules r, Gdd03SyntheticMonthContext? month, List<Cause> why)
        {
            if (month == null) PopulationLedger.RefuseAggregate(s);
            else if (!c.UsesPopulationLedger || !s.PopulationLedger.IsInitialized)
                throw new NotSupportedException("A monthly Food snapshot needs an initialized gdd-0.3 population ledger.");
            else s.PopulationLedger.Reconcile();
            var need = month != null ? Rations(month.OpeningPeople) : Need(s, r);
            var eaten = Math.Min(need, s.Food);
            s.Food -= eaten;
            s.FoodNeed = need;
            s.FoodUnmet = need - eaten;
            if (s.FoodUnmet > 0)
                why.Add(new Cause("economy", "food", -s.FoodUnmet, FormattableString.Invariant($"Food ran out: {s.FoodUnmet} of {need} units of the month's need went unmet"), key: CauseKeys.FoodShortage, args: new[] { CauseKeys.Number(need) }));
            if (s.ActiveSituations.Contains(EconomySystem.FxCrisis))
            {
                why.Add(new Cause("economy", "food", -eaten, FormattableString.Invariant($"No Food imported (foreign currency crisis): the stock fell to {s.Food} units"), key: CauseKeys.FoodWithoutImports, args: new[] { CauseKeys.Number(s.Food) }));
                return;
            }
            var target = (long)r.CoverMonths!.Value * need;
            if (s.Food < target) s.Food = (int)target;
        }

        /// <summary>R-FOOD-003: food_supply's stock term, k_food * (stock - full) / full - unmet / need, where full is
        /// cover_months of the last need. It is exactly 0 at a full stock, -k_food when the stock is empty, and the unmet
        /// share lowers it further; food_supply's clamp to 0..1 bounds it. 0 in the legacy loop and with no need.</summary>
        public static double FoodTerm(Content c, GameState s)
        {
            PopulationLedger.RefuseAggregate(s);
            if (!c.IsGdd || s.FoodNeed == 0) return 0.0;
            var r = c.RulesAt(s.Date).Mvp!;
            var full = (long)r.CoverMonths!.Value * s.FoodNeed;
            return r.KFood!.Value * (s.Food - full) / full - (double)s.FoodUnmet / s.FoodNeed;
        }
    }
}
