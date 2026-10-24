#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The signals a message waits for (spec 5). Event signals are raised by a call (AdvisorPresenter.Notice) and
    /// hold for the turn they fired on; state signals hold while the shown state says so and are read on Changed and
    /// TurnEnded only, never each frame (spec 16). A `when` condition measures one number on the state.</summary>
    public static class AdvisorSignals
    {
        public const string NewGame = "new_game", TurnEnded = "turn_ended", Opening = "opening",
            AttentionRefused = "attention_refused", BudgetUnsubmitted = "budget_unsubmitted", CityShown = "city_shown", UnitShown = "unit_shown";
        public static readonly IReadOnlyCollection<string> Events = new[] { NewGame, TurnEnded, Opening, AttentionRefused, BudgetUnsubmitted, CityShown, UnitShown };
        public static readonly IReadOnlyDictionary<string, Func<AdvisorContext, bool>> States = new Dictionary<string, Func<AdvisorContext, bool>>
        {
            // Spec 6's critical warnings and the defeat lines (R-END-001 to R-END-003, docs/rules/mvp.md:377-381).
            ["city_at_risk"] = ctx => Threats.CitiesAtRisk(ctx.Content, ctx.State).Count > 0,
            ["exhausted"] = ctx => ctx.State.ExhaustedTurns > 0,
            ["in_debt"] = ctx => ctx.State.Debt > 0,
            ["food_short"] = ctx => ctx.State.FoodUnmet > 0,
            ["defeat_war"] = ctx => ctx.State.GameOver == GameOverCodes.DefeatWar,
            ["defeat_economy"] = ctx => ctx.State.GameOver == GameOverCodes.DefeatEconomy,
            ["project_unlocked"] = ctx => ctx.State.UnlockedProjects.Count > 0,
            // A halutz with no project (R-SITE-001, D-D6: one project per unit): pioneer work is waiting to be given.
            ["idle_pioneer"] = ctx => ctx.State.Units.Values.Any(u => u.Side == "" && Projects.IsPioneer(u.Type) && !Projects.IsBusy(ctx.State, u.Id)),
            ["corridor_started"] = ctx => ctx.State.Corridors.Count > 0,
            ["cabinet_vacancy"] = ctx => ctx.Bridge.Vacancies.Count > 0,
            ["situation_active"] = ctx => ctx.State.ActiveSituations.Count > 0,
            ["standing_order"] = ctx => ctx.State.Units.Values.Any(u => u.Side == Movement.PlayerSide && u.Order != ""),
            ["combat_seen"] = ctx => ctx.Bridge.Session.Last?.Why.Any(w => w.Key == WhyKeys.Engagement) == true,
            // Owner, 24 October 2026: "Fire when low". Owner-set DESIGN VALUE ("Approve 3 / costliest action", same day): low is
            // at or below the costliest single Arms action (ArmsReserve), so an empty stock is low too.
            ["arms_low"] = ctx => ctx.Content.IsGdd && ctx.State.Arms <= ArmsReserve(ctx),
            ["capture_waiting"] = ctx => ctx.State.Settlements.Values.Any(x => x.State == SettlementControl.AwaitingChoice),
            ["enemy_moved"] = ctx => ctx.Bridge.Session.Last?.Why.Any(w => w.EnemyOrder != "") == true,
            ["objective_done"] = ctx => ctx.State.Objectives.Count > 0,
            ["truce_active"] = ctx => Combat.TruceUntil(ctx.State) != null,
        };
        /// <summary>Each condition's measure; a name ends in _at_least or _at_most, which says how its bound reads.</summary>
        public static readonly IReadOnlyDictionary<string, Func<AdvisorContext, double>> Measures = new Dictionary<string, Func<AdvisorContext, double>>
        {
            ["debt_ratio_at_least"] = ctx => ctx.Content.RulesAt(ctx.State.Date).Mvp?.DebtCeiling is double ceiling && ceiling > 0 ? ctx.State.Debt / ceiling : 0,
            ["refugees_at_least"] = ctx => ctx.State.Cities.Values.Sum(x => x.Refugees),
            ["cards_waiting_at_least"] = ctx => ctx.Card?.Shown != null ? 1 : 0,
        };
        public static IEnumerable<string> Names => Events.Concat(States.Keys);

        /// <summary>The Arms the costliest single Arms action takes under the rules in force: a recruit group
        /// (mvp.arms_per_recruit_group, R-ARMS-003), an attack (mvp.arms_per_attack, R-COMBAT-004), or one of our unit types'
        /// training_arms (R-UNIT-008) or arms_per_fire (R-COMBAT-007). At or below it, one such action may be all the stock
        /// pays for. There is no Arms upkeep or refill (R-ARMS-001, R-ARMS-002), so a per-turn burn rate would not be stable.
        /// Presentation only; the sim is not asked.</summary>
        public static int ArmsReserve(AdvisorContext ctx)
        {
            var mvp = ctx.Content.RulesAt(ctx.State.Date).Mvp;
            int reserve = Math.Max(mvp?.ArmsPerRecruitGroup ?? 0, mvp?.ArmsPerAttack ?? 0);
            foreach (var t in ctx.Content.UnitTypes)
                if (t.Side == Movement.PlayerSide) reserve = Math.Max(reserve, Math.Max(t.TrainingArms ?? 0, t.ArmsPerFire ?? 0));
            return reserve;
        }

        /// <summary>Whether a message's `when` holds: each named measure against its bound (spec 4.1).</summary>
        public static bool When(AdvisorContext ctx, IReadOnlyDictionary<string, double> when) =>
            when.All(kv => kv.Key.EndsWith("_at_least", StringComparison.Ordinal)
                ? Measures[kv.Key](ctx) >= kv.Value - 1e-9
                : Measures[kv.Key](ctx) <= kv.Value + 1e-9);

        /// <summary>The signals that hold now: this turn's event signals, and each state signal whose reading is true.</summary>
        public static HashSet<string> Holding(AdvisorContext ctx, AdvisorSession session)
        {
            var holding = new HashSet<string>(session.FiredTurn == ctx.State.Turn ? session.Fired : Enumerable.Empty<string>(), StringComparer.Ordinal);
            foreach (var kv in States) if (kv.Value(ctx)) holding.Add(kv.Key);
            return holding;
        }
    }

    /// <summary>What a signal reads: the content, the state the screen shows, the bridge and the decision card.</summary>
    public sealed class AdvisorContext
    {
        public AdvisorContext(SimBridge bridge, EventCardPresenter? card) { Bridge = bridge; Card = card; }
        public SimBridge Bridge { get; }
        public EventCardPresenter? Card { get; }
        public Statecraft.Sim.Content Content => Bridge.Content;
        public Statecraft.Sim.GameState State => Bridge.State;
    }
}
