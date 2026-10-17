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

        /// <summary>R-FOOD-001: the stock starts at cover_months of the start's need, so food_supply starts at its level.</summary>
        public static void Init(Content c, GameState s)
        {
            if (!c.IsGdd) return;
            var r = c.RulesAt(s.Date).Mvp!;
            s.FoodNeed = Need(s, r);
            s.Food = r.CoverMonths!.Value * s.FoodNeed;
            s.FoodUnmet = 0;
        }

        /// <summary>The nation's need for one turn: its head count in population units, rounded half away from zero.</summary>
        static int Need(GameState s, MvpRules r) => (int)Math.Round(s.TotalPopulation / r.PeoplePerUnit, MidpointRounding.AwayFromZero);

        /// <summary>R-FIELD-002, step 6 after the cities' production (D-A6): in ordinal site id order, every plowed field (a built
        /// field site, R-FIELD-001) adds field_food_per_turn to the stock, with no workers (Q-FIELD-2). It comes before the
        /// nation eats in step 7, so in a turn with imports the import only tops the stock up by less.</summary>
        public static void Harvest(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            var yield = r.FieldFoodPerTurn!.Value;   // the loader requires it in the GDD loop
            if (yield == 0) return;
            foreach (var f in c.Sites.Where(x => x.Kinds.Contains(Projects.FieldKind)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (!(s.Sites.TryGetValue(f.Id, out var w) && w.BuiltOn != null)) continue;
                s.Food += yield;
                why.Add(new Cause("economy", "food", yield, FormattableString.Invariant($"{f.Name}: the plowed land yielded {yield} units of Food")));
            }
        }

        /// <summary>R-FOOD-002, step 7: the nation eats the turn's need from the stock, as far as the stock goes (it stops at
        /// 0, and the rest is unmet), then the import brings the stock back to cover_months of the need, unless foreign
        /// currency is exhausted (the fx_crisis situation at the turn's start), when no Food is imported.</summary>
        public static void Consume(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            var need = Need(s, r);
            var eaten = Math.Min(need, s.Food);
            s.Food -= eaten;
            s.FoodNeed = need;
            s.FoodUnmet = need - eaten;
            if (s.FoodUnmet > 0)
                why.Add(new Cause("economy", "food", -s.FoodUnmet, FormattableString.Invariant($"Food ran out: {s.FoodUnmet} of {need} units of the month's need went unmet")));
            if (s.ActiveSituations.Contains(EconomySystem.FxCrisis))
            {
                why.Add(new Cause("economy", "food", -eaten, FormattableString.Invariant($"No Food imported (foreign currency crisis): the stock fell to {s.Food} units")));
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
            if (!c.IsGdd || s.FoodNeed == 0) return 0.0;
            var r = c.RulesAt(s.Date).Mvp!;
            var full = (long)r.CoverMonths!.Value * s.FoodNeed;
            return r.KFood!.Value * (s.Food - full) / full - (double)s.FoodUnmet / s.FoodNeed;
        }
    }
}
