#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Defence screen, laid out as promo/final/08: the leadership (the Prime Minister, the Minister of Defense
    /// and the Chief of Staff, each with portrait, party, competence and loyalty), the always-on escalation ladder (spec
    /// section 3) with the current level, the ordered one and each level's turn length, the UN ceasefire clock in full war,
    /// security orders to raise or lower the level, the reserve call-up as a bar with the level's minimum marked and its
    /// yearly cost, and the defence share of the national budget. Orders are queued for End Turn. The promo's forces
    /// readiness and strength, intelligence and procurement panels are left out: the sim does not model them yet. The call-up
    /// and the budget share show what the turn will end with (a queued order's value, else today's). The screen renders only
    /// while it is open (a change while it is hidden is caught up when ScreenNav opens it).</summary>
    public sealed class DefencePresenter : IDisposable
    {
        public const double MobilizationStep = 0.05;
        /// <summary>The leadership cards, in order, and the budget line that is the defence share.</summary>
        public static readonly string[] Leaders = { "prime_minister", "defense_minister", "chief_of_staff" };
        public const string BudgetLine = "defense_minister";
        /// <summary>The leadership portrait box in logical pixels (Statecraft.uss .leader .card-portrait).</summary>
        public static readonly UnityEngine.Vector2 LeaderPortrait = new UnityEngine.Vector2(190f, 200f);
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly Button escalate, deEscalate, mobUp, mobDown; readonly Action onEscalate, onDeEscalate, onMobUp, onMobDown;

        public DefencePresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits = null)
        {
            screen = root.Q<VisualElement>("screen-defence"); this.bridge = bridge; this.text = text; this.portraits = portraits;
            escalate = screen.Q<Button>("escalate"); deEscalate = screen.Q<Button>("de-escalate");
            mobUp = screen.Q<Button>("mob-up"); mobDown = screen.Q<Button>("mob-down");
            onEscalate = () => Order(+1); onDeEscalate = () => Order(-1);
            onMobUp = () => Mobilize(+MobilizationStep); onMobDown = () => Mobilize(-MobilizationStep);
            escalate.clicked += onEscalate; deEscalate.clicked += onDeEscalate; mobUp.clicked += onMobUp; mobDown.clicked += onMobDown;
            foreach (var (name, key) in Sections) screen.Q<Label>(name).text = text[key].ToUpperInvariant();   // the promo's capitals; a caseless script is unchanged
            screen.Q<Label>("defence-screen-title").text = text["defence.title"];
            deEscalate.text = text["defence.lower"]; escalate.text = text["defence.raise"];
            bridge.Changed += OnChanged;
            ScreenNav.OnOpened(screen, () => { if (dirty) Render(); });
            Render();
        }

        /// <summary>The section captions and their strings.</summary>
        static readonly (string name, string key)[] Sections =
        {
            ("defence-title-leadership", "defence.section_leadership"), ("defence-title-ladder", "defence.section_ladder"),
            ("defence-title-callup", "defence.section_callup"), ("defence-title-budget", "defence.section_budget"),
        };

        bool dirty;

        /// <summary>How many times the screen was rendered (a hidden screen is not).</summary>
        public int Renders { get; private set; }

        void OnChanged() { if (ScreenNav.IsOpen(screen)) Render(); else dirty = true; }

        /// <summary>Unsubscribes from the bridge and the buttons and empties the leadership cards (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= OnChanged;
            if (screen.userData is System.Action) screen.userData = null;
            escalate.clicked -= onEscalate; deEscalate.clicked -= onDeEscalate; mobUp.clicked -= onMobUp; mobDown.clicked -= onMobDown;
            foreach (var office in Leaders) PersonCard.Clear(screen.Q<VisualElement>("lead-" + office));
            screen.Q<UiBar>("mob-bar").Mark = null;
        }

        /// <summary>The defence share of spending the turn will end with: a queued budget's, else today's (as the call-up).</summary>
        public double? ProposedBudgetShare
        {
            get
            {
                var queued = (bridge.QueuedLike(new AllocateBudget(new System.Collections.Generic.Dictionary<string, double>())) as AllocateBudget)?.Shares;
                if (queued != null && queued.TryGetValue(BudgetLine, out var q)) return q;
                return bridge.State.Budget.TryGetValue(BudgetLine, out var today) ? today : (double?)null;
            }
        }

        int QueuedDelta => (bridge.QueuedLike(new SecurityOrder(0)) as SecurityOrder)?.Delta ?? 0;

        /// <summary>The call-up level the turn will end with: the queued order's, else today's.</summary>
        public double ProposedMobilization => (bridge.QueuedLike(new SetMobilization(0)) as SetMobilization)?.Level ?? bridge.State.Mobilization;

        /// <summary>The security level the turn will end with: today's, moved by the queued order.</summary>
        int TargetRung => SecuritySystem.ClampRung(bridge.State.Rung + QueuedDelta);

        void Order(int step)
        {
            var s = bridge.State;
            var target = SecuritySystem.ClampRung(s.Rung + QueuedDelta + step);
            if (target == s.Rung) Withdraw(new SecurityOrder(0)); else bridge.Queue(new SecurityOrder(target - s.Rung));
            DropNoOpCallUp();
        }

        void Withdraw(Command like)
        {
            var queued = bridge.QueuedLike(like);
            if (queued != null) bridge.Withdraw(queued);
        }

        /// <summary>The call-up the turn will end with if <paramref name="level"/> is ordered (the sim raises a lower one to the minimum).</summary>
        double EffectiveAt(double level) => SecuritySystem.EffectiveMobilization(bridge.Content, bridge.State.Date, TargetRung, level);

        bool SameAsToday(double level) => Math.Abs(EffectiveAt(level) - EffectiveAt(bridge.State.Mobilization)) < 1e-9;

        /// <summary>A queued call-up order that would change nothing (after the minimum is applied) is withdrawn, so it
        /// neither costs attention nor shows a stale proposed level.</summary>
        void DropNoOpCallUp()
        {
            if (bridge.QueuedLike(new SetMobilization(0)) is SetMobilization q && SameAsToday(q.Level)) bridge.Withdraw(q);
        }

        void Mobilize(double step)
        {
            var floor = SecuritySystem.MinMobilization(bridge.Content, bridge.State.Date, TargetRung);
            var level = Math.Max(floor, Math.Min(1, Math.Round((EffectiveAt(ProposedMobilization) + step) / MobilizationStep) * MobilizationStep));
            if (SameAsToday(level)) Withdraw(new SetMobilization(0)); else bridge.Queue(new SetMobilization(level));
        }

        /// <summary>The string key for a turn length. A switch, so a new TurnLength value fails loudly (and in a test).</summary>
        public static string TurnKey(TurnLength length)
        {
            switch (length)
            {
                case TurnLength.Quarter: return "defence.turn_quarter";
                case TurnLength.Month: return "defence.turn_month";
                case TurnLength.Week: return "defence.turn_week";
                default: throw new ArgumentOutOfRangeException(nameof(length), length, "no string for this turn length");
            }
        }

        public void Render()
        {
            dirty = false; Renders++;
            var c = bridge.Content; var s = bridge.State;
            var delta = QueuedDelta;
            var target = TargetRung;
            for (int rung = 1; rung <= 5; rung++)
            {
                screen.Q<Label>($"rung-{rung}-name").text = text[$"defence.rung{rung}"];
                // The content's turn_lengths (R-TURN-001): every rung reads "month" in the GDD game.
                screen.Q<Label>($"rung-{rung}-turn").text = text[TurnKey(SecuritySystem.TurnLengthFor(c, s.Date, rung))];
                var row = screen.Q<VisualElement>($"rung-{rung}");
                row.EnableInClassList("rung-current", rung == s.Rung);
                row.EnableInClassList("rung-target", rung == target && target != s.Rung);   // the level an order will move to
            }
            string NameOrVacant(Person? p) => p == null ? text["cabinet.vacant"] : ContentNames.Of(text, p);
            screen.Q<Label>("defence-command").text = text.Format("defence.command",
                NameOrVacant(Cabinet.Holder(c, s, "defense_minister")), NameOrVacant(Cabinet.Holder(c, s, "chief_of_staff")));
            // One card per person: a leader who holds several of these offices (Ben-Gurion, Prime Minister and Minister of
            // Defense) has one card listing them all; a vacancy keeps its own card.
            var lineup = Cabinet.Lineup(c, s);
            var seats = Leaders.Select(o => lineup.FirstOrDefault(x => x.Office.Id == o)).ToList();
            for (int i = 0; i < Leaders.Length; i++)
            {
                var card = screen.Q<VisualElement>("lead-" + Leaders[i]);
                var seat = seats[i];
                var shownEarlier = seat?.Holder != null && seats.Take(i).Any(x => x?.Holder?.Id == seat.Holder.Id);
                card.style.display = seat == null || shownEarlier ? DisplayStyle.None : DisplayStyle.Flex;
                if (seat == null || shownEarlier) continue;
                var titles = seat.Holder == null ? ContentNames.Of(text, seat.Office, null)
                    : string.Join("\n", seats.Skip(i).Where(x => x?.Holder?.Id == seat.Holder.Id).Select(x => ContentNames.Of(text, x!.Office, seat.Holder)));   // one office a line: the narrow card measured "A · B" as two lines but drew one (a blank line at 1366x768)
                PersonCard.Fill(card, "lead-" + Leaders[i], c, s, seat.Holder, titles, seat.Appointed, text, portraits, LeaderPortrait);
            }
            var ceasefire = screen.Q<Label>("ceasefire");
            if (s.Rung == 5 && target == 5) ceasefire.text = text.Format("defence.ceasefire", s.CeasefireWeeksLeft);
            else if (s.Rung == 5) ceasefire.text = text["defence.ceasefire_stops"];                       // a lowering is queued
            else if (target == 5) ceasefire.text = text.Format("defence.ceasefire_starts", SecuritySystem.CeasefireWeeksAfter(c, s.Date, s.Rung, s.CeasefireWeeksLeft, target));
            else ceasefire.text = "";
            ceasefire.style.display = ceasefire.text == "" ? DisplayStyle.None : DisplayStyle.Flex;
            screen.Q<Label>("order-pending").text = delta == 0 ? "" : text.Format("defence.order", s.Rung, target);
            var floor = SecuritySystem.MinMobilization(c, s.Date, target);
            var proposed = EffectiveAt(ProposedMobilization);   // what the call-up will be: the sim raises a lower order to the minimum
            screen.Q<Label>("mob-level").text = text.Format("defence.mob", PlayerText.Percent(proposed), target, PlayerText.Percent(floor));
            screen.Q<Label>("mob-percent").text = PlayerText.Percent(proposed);
            var bar = screen.Q<UiBar>("mob-bar");
            bar.Value = (float)proposed; bar.Mark = (float)floor;
            var share = ProposedBudgetShare;
            screen.Q<UiBar>("defence-budget-bar").Value = share.HasValue ? (float)share.Value : 0f;
            screen.Q<Label>("defence-budget-share").text = share.HasValue ? text.Format("defence.budget_share", PlayerText.Percent(share.Value)) : text["defence.budget_none"];
            screen.Q<Label>("mob-cost").text = text.Format("defence.mob_cost", PlayerText.Money(c, "treasury", EconomySystem.MobilizationCost(c, s.Date, proposed)));
            screen.Q<Button>("escalate").SetEnabled(target < 5);
            screen.Q<Button>("de-escalate").SetEnabled(target > 1);
        }
    }
}
