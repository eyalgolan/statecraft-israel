using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    public static class EffectsSystem
    {
        public static void Init(Content c, GameState s)
        {
            var v = c.At(s.Date);   // the records that exist on the start date (all of them when none has a window)
            foreach (var i in v.Indicators) { s.Indicators[i.Id] = i.Base; s.IndicatorShifts[i.Id] = 0.0; }
            foreach (var p in v.Policies) s.Policies[p.Id] = p.Default;
            foreach (var e in v.Effects) s.EffectContributions[e.Id] = 0;
        }

        /// <summary>Competence (0.5 + competence/20) of the office holder (player appointments included) times budget
        /// factor. No office: 1.0.</summary>
        public static double Multiplier(Content c, GameState s, string office)
        {
            if (office == "") return 1.0;
            var holder = Cabinet.Holder(c, s, office);
            var competence = holder == null ? 0.5 : 0.5 + holder.Competence / 20.0;
            return competence * BudgetFactor(c, s, office);
        }

        /// <summary>The factor of the budget line that <paramref name="office"/> owns on the state's date (#108: a line can
        /// change owner on a date, see <see cref="Offices.BudgetOwner"/>). An equal split of the budget is neutral (1.0); a
        /// line's factor runs from 0.5 (no money) to 1.5. An office that owns no line (the player can never fund it) is
        /// neutral too: treating it as unfunded would halve its effects permanently with no way to fix it.</summary>
        public static double BudgetFactor(Content c, GameState s, string office)
        {
            var line = Offices.BudgetLineOf(c, office, s.Date);
            return line == null ? 1.0 : BudgetFactor(s.Budget, line);
        }

        /// <summary>The same rule for one budget line of any set of shares (the Budget screen shows the factor of a draft budget).</summary>
        public static double BudgetFactor(IReadOnlyDictionary<string, double> budget, string line)
        {
            if (!budget.TryGetValue(line, out var share)) return 1.0;
            return Math.Min(1.5, 0.5 + share * budget.Count / 2.0);
        }

        public static void Apply(Content c, GameState s, List<Cause> why)
        {
            // Read every source before writing anything, so effect order cannot change results.
            var v = c.At(s.Date);
            var targets = v.Effects.ToDictionary(e => e.Id, e =>
                e.K * Source(s, e.Source) * Multiplier(c, s, e.Office));
            var moved = new HashSet<string>();
            foreach (var e in v.Effects)
            {
                var old = s.EffectContributions[e.Id];
                var now = old + (targets[e.Id] - old) / e.Inertia;
                s.EffectContributions[e.Id] = now;
                if (Math.Abs(now - old) > 1e-9) moved.Add(e.Id);
            }
            var before = v.Indicators.ToDictionary(i => i.Id, i => s.Indicators[i.Id]);
            foreach (var i in v.Indicators) s.Indicators[i.Id] = Level(c, s, i);
            // One cause per indicator that actually changed (a clamped indicator reports nothing).
            foreach (var i in v.Indicators)
            {
                var delta = s.Indicators[i.Id] - before[i.Id];
                if (Math.Abs(delta) <= 1e-9) continue;
                var names = v.Effects.Where(e => e.Target == i.Id && moved.Contains(e.Id)).Select(e => e.Name).ToList();
                var from = names.Count > 0 ? $" ({string.Join(", ", names)})" : "";
                why.Add(new Cause("effects", i.Id, delta, $"{PlayerText.Change(c, i.Id, delta)}{from}"));
            }
            UpdateSituations(c, s, why);
        }

        /// <summary>An effect's source level: a policy, an indicator, or the cities' employed share (R-CITY-005). The legacy
        /// loop has no cities, so there the share reads 0 and the effect adds nothing (not 1, which would lift morale).</summary>
        static double Source(GameState s, string id) =>
            s.Policies.TryGetValue(id, out var p) ? p
            : id == Cities.EmployedShareSource ? (s.Cities.Count == 0 ? 0.0 : Cities.EmployedShare(s))
            : s.Indicators[id];

        /// <summary>clamp(base + effect contributions) to [0, 1], before event shifts. In the GDD loop food_supply also carries
        /// the Food stock's term (R-FOOD-003), exactly 0 at a full cover, and housing's base is the cities' people-weighted
        /// mean condition when anybody is housed (R-HOUSE-004).</summary>
        internal static double Inner(Content c, GameState s, Indicator i) =>
            Math.Max(0, Math.Min(1, Base(c, s, i) + c.At(s.Date).Effects.Where(e => e.Target == i.Id).Sum(e => s.EffectContributions[e.Id])
                + (i.Id == Pools.FoodIndicator ? Pools.FoodTerm(c, s) : 0.0)));

        /// <summary>The indicator's base: model.yaml's, except housing's in the GDD loop (R-HOUSE-004).</summary>
        static double Base(Content c, GameState s, Indicator i) =>
            c.IsGdd && i.Id == Cities.HousingIndicator ? Cities.HousingLevel(s) ?? i.Base : i.Base;

        /// <summary>clamp(inner + event shift) to [0, 1].</summary>
        internal static double Level(Content c, GameState s, Indicator i) =>
            Math.Max(0, Math.Min(1, Inner(c, s, i) + s.IndicatorShifts[i.Id]));

        /// <summary>Moves an indicator's level by <paramref name="delta"/> through its permanent shift (the layer event
        /// choices and crises use), clamped to [0, 1]. The shift is set so the level lands exactly on the visible result.
        /// Returns the change actually applied (0 when the indicator was already at the clamp).</summary>
        public static double Shift(Content c, GameState s, string indicator, double delta)
        {
            var ind = c.At(s.Date).Indicators.First(i => i.Id == indicator);
            var level = Level(c, s, ind);
            var newLevel = Math.Max(0, Math.Min(1, level + delta));
            s.IndicatorShifts[ind.Id] = newLevel - Inner(c, s, ind);
            s.Indicators[ind.Id] = newLevel;
            return newLevel - level;
        }

        public static void UpdateSituations(Content c, GameState s, List<Cause> why)
        {
            foreach (var sit in c.At(s.Date).Situations)
            {
                var v = s.Indicators[sit.Indicator];
                bool on = s.ActiveSituations.Contains(sit.Id);
                bool turnOn = sit.OnBelow.HasValue ? v < sit.OnBelow.Value : v > sit.OnAbove!.Value;
                bool turnOff = sit.OffAbove.HasValue ? v > sit.OffAbove.Value : v < sit.OffBelow!.Value;
                if (!on && turnOn) { s.ActiveSituations.Add(sit.Id); why.Add(new Cause("situations", sit.Id, 1, $"{sit.Name} began ({PlayerText.Target(c, sit.Indicator)} at {PlayerText.Percent(v)})")); }
                else if (on && turnOff) { s.ActiveSituations.Remove(sit.Id); why.Add(new Cause("situations", sit.Id, -1, $"{sit.Name} ended ({PlayerText.Target(c, sit.Indicator)} at {PlayerText.Percent(v)})")); }
            }
        }

        public static double Approval(Content c, GameState s) =>
            c.At(s.Date).VoterGroups.Count == 0 ? 0.5 :
            c.At(s.Date).VoterGroups.Average(g => Math.Max(0, Math.Min(1, g.Weights.Sum(w => w.Value * s.Indicators[w.Key]))));
    }
}
