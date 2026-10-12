#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Budget screen: each budgeted ministry's share of spending, its minister and the effect multiplier the
    /// share gives (the sim's own EffectsSystem.BudgetFactor), the money position, and whether the legislature must approve. +/- moves
    /// a ministry by 5 points and rescales the others so shares always sum to 1. Submit queues AllocateBudget.</summary>
    public sealed class BudgetPresenter : IDisposable
    {
        public const double Step = 0.05;
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;
        readonly Button reset, submit;
        Dictionary<string, double> draft = new Dictionary<string, double>();
        GameSession session;

        public BudgetPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-budget"); this.bridge = bridge; this.text = text;
            reset = screen.Q<Button>("budget-reset"); submit = screen.Q<Button>("budget-submit");
            reset.clicked += Reset; submit.clicked += Submit;
            session = bridge.Session;
            draft = Current();
            bridge.Changed += OnChanged;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons and removes the rows (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= OnChanged;
            reset.clicked -= Reset; submit.clicked -= Submit;
            screen.Q<ScrollView>("budget-rows")?.Clear();
        }

        void Reset() { draft = Current(); Render(); }

        // A different session (a new game or a loaded save) always replaces the draft. Within a session, a draft the player
        // has edited survives End Turn (it was never submitted, so it is never applied; the status still says "Changed");
        // an untouched one follows the budget.
        void OnChanged()
        {
            if (bridge.Session != session) { session = bridge.Session; draft = Current(); }
            else if (!Dirty && QueuedShares == null) draft = Current();
            Render();
        }

        IReadOnlyDictionary<string, double>? QueuedShares => (bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>())) as AllocateBudget)?.Shares;

        static bool Same(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b) =>
            a.Keys.Union(b.Keys).All(k => Math.Abs((a.TryGetValue(k, out var x) ? x : 0) - (b.TryGetValue(k, out var y) ? y : 0)) <= 1e-9);

        /// <summary>The shares the screen proposes.</summary>
        public IReadOnlyDictionary<string, double> Draft => draft;

        Dictionary<string, double> Current() => new Dictionary<string, double>(bridge.State.Budget);

        // Compares the union of the draft's and the budget's offices.
        bool Dirty => !Same(draft, bridge.State.Budget);

        /// <summary>Moves <paramref name="office"/> by <paramref name="delta"/> (clamped to [0, 1]) and rescales the other
        /// shares proportionally; the largest other share absorbs rounding so the total is exactly 1.</summary>
        public static Dictionary<string, double> Adjust(IReadOnlyDictionary<string, double> shares, string office, double delta)
        {
            if (!shares.ContainsKey(office)) return new Dictionary<string, double>(shares);   // not a budget line: nothing to move
            var target = Math.Max(0, Math.Min(1, shares[office] + delta));
            var others = shares.Keys.Where(k => k != office).OrderBy(k => k, StringComparer.Ordinal).ToList();
            var rest = others.Sum(k => shares[k]);
            var result = new Dictionary<string, double> { [office] = target };
            foreach (var k in others) result[k] = rest > 0 ? shares[k] / rest * (1 - target) : (1 - target) / others.Count;
            if (others.Count > 0)
            {
                var largest = others.OrderByDescending(k => result[k]).ThenBy(k => k, StringComparer.Ordinal).First();
                result[largest] = Math.Max(0, 1 - result.Where(kv => kv.Key != largest).Sum(kv => kv.Value));
            }
            return result;
        }

        public void Change(string office, double delta)
        {
            if (!draft.ContainsKey(office)) draft = Current();   // a draft without this line is stale: start again from the budget
            draft = Adjust(draft, office, delta);
            Render();
        }

        void Submit()
        {
            var queued = QueuedShares;
            if (!Dirty)
            {
                // Same as today's budget: nothing to put to the vote and no attention to spend; an earlier submission is withdrawn.
                if (queued != null) bridge.Withdraw(bridge.QueuedLike(new AllocateBudget(new Dictionary<string, double>()))!);
                return;
            }
            // The bridge copies the shares and reports a refusal itself; the sim checks coverage and the sum.
            bridge.Queue(new AllocateBudget(new Dictionary<string, double>(draft)));
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var rows = screen.Q<ScrollView>("budget-rows");
            rows.Clear();
            foreach (var office in c.Rules.BudgetStart.Keys)
            {
                var o = c.Offices.First(x => x.Id == office);
                var share = draft.TryGetValue(office, out var v) ? v : 0;
                var row = new VisualElement { name = "budget-" + office };
                row.AddToClassList("row");
                // A budget line can run before its ministry exists (Education from 10 March 1949): say so, not "vacant".
                var who = o.From.HasValue && s.Date < o.From.Value ? text.Format("budget.not_established", PlayerText.Date(o.From.Value))
                    : Cabinet.Holder(c, s, office)?.Name ?? text["budget.vacant"];
                row.Add(new Label(text.Format("budget.office", o.Title, who)) { name = "budget-" + office + "-name" });
                row[0].AddToClassList("grow");
                row.Add(new Button(() => Change(office, -Step)) { name = "budget-" + office + "-down", text = "-" });
                row.Add(new Label(text.Format("budget.share", PlayerText.Percent(share))) { name = "budget-" + office + "-share" });
                row.Add(new Button(() => Change(office, +Step)) { name = "budget-" + office + "-up", text = "+" });
                var factor = EffectsSystem.BudgetFactor(draft, office);
                row.Add(new Label(text.Format("budget.factor", PlayerText.Percent(factor))) { name = "budget-" + office + "-factor" });
                row[4].AddToClassList("muted");
                rows.Add(row);
            }
            var leg = Offices.Legislature(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            // eras.yaml systems.legislature: an assembly's vote is held but does not bind (#58)
            var advisory = c.SystemsAt(s.Date).Legislature == EraSystems.Assembly;
            screen.Q<Label>("budget-vote").text = leg == null ? "" : !leg.BudgetApproval ? text.Format("budget.no_vote", leg.Body)
                : text.Format(advisory ? "budget.vote_advisory" : "budget.vote_needed", leg.Body, seats, size);
            screen.Q<Label>("budget-money").text = text.Format("budget.money", PlayerText.Money(c, "treasury", s.Treasury), PlayerText.Money(c, "debt", s.Debt), PlayerText.Money(c, "fx_reserves", s.FxReserves));
            var flows = EconomySystem.Flows(c, s, 1.0);   // the sim's own arithmetic, for a year
            screen.Q<Label>("budget-flow").text = text.Format("budget.flow", PlayerText.Money(c, "treasury", flows.Tax), PlayerText.Money(c, "treasury", flows.Spending), PlayerText.Money(c, "treasury", flows.Mobilization));
            var queuedShares = QueuedShares;
            // The status says what is actually queued: edits after Submit are "Changed" again.
            screen.Q<Label>("budget-status").text = queuedShares != null ? (Same(draft, queuedShares) ? text["budget.queued"] : text["budget.unsaved"])
                : Dirty ? text["budget.unsaved"] : "";
            submit.SetEnabled(Dirty || queuedShares != null);
        }
    }
}
