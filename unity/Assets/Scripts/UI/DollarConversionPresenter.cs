#nullable enable
using System;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Defence screen's dollar conversion (#813, R-MONEY-004; owner, 2 November 2026: "Spend the dollars
    /// (Recommended)"): one replaceable ConvertDollars at End turn, as the Arms purchase. It shows the foreign-currency reserve,
    /// the one readout of it the ruling allows beside this order, and the sim's own refusal before End turn. The reserve and
    /// Money change only when the sim executes the order.</summary>
    public sealed class DollarConversionPresenter : IDisposable
    {
        readonly VisualElement panel;
        readonly SimBridge bridge;
        readonly UiText text;
        readonly IntegerField amount;
        readonly Button convert, withdraw;
        readonly Action onConvert, onWithdraw;
        readonly EventCallback<ChangeEvent<int>> onAmount;
        readonly PressSource presses;

        public DollarConversionPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            panel = root.Q("defence-dollars"); this.bridge = bridge; this.text = text;
            amount = panel.Q<IntegerField>("dollars-amount");
            convert = panel.Q<Button>("convert-dollars"); withdraw = panel.Q<Button>("withdraw-dollars");
            amount.label = text["dollars.amount"];
            panel.Q<Label>("dollars-title").text = text["dollars.title"];
            panel.Q<Label>("dollars-cost").text = text["dollars.cost"];
            convert.text = text["dollars.convert"]; withdraw.text = text["dollars.withdraw"];
            onConvert = () =>
            {
                var reason = bridge.Queue(new ConvertDollars(amount.value));
                Render();
                if (reason != null) panel.Q<Label>("dollars-refusal").text = reason;
            };
            presses = new PressSource(panel);
            onWithdraw = () =>
            {
                var queued = bridge.QueuedLike(new ConvertDollars(1));
                if (queued == null) return;
                var keyboard = presses.ByKeyboard;
                bridge.Withdraw(queued);
                // Render has just disabled Withdraw, which had the focus. Keep the keyboard in the panel: Convert, else the amount.
                if (keyboard) FocusReturn.ToOrWithin(convert, null, panel);
            };
            onAmount = _ => Render();
            convert.clicked += onConvert; withdraw.clicked += onWithdraw; amount.RegisterValueChangedCallback(onAmount);
            bridge.Changed += Render;
            // The amount starts at the month's limit, or at the order already queued this turn.
            if (bridge.QueuedLike(new ConvertDollars(1)) is ConvertDollars old) amount.SetValueWithoutNotify(old.Amount);
            else if (bridge.Content.IsGdd) amount.SetValueWithoutNotify(bridge.Content.RulesAt(bridge.State.Date).Mvp!.DollarsPerTurn!.Value);
            Render();
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            panel.style.display = c.IsGdd ? DisplayStyle.Flex : DisplayStyle.None;
            if (!c.IsGdd) return;
            var m = c.RulesAt(s.Date).Mvp!;
            var order = new ConvertDollars(amount.value);
            var queued = bridge.QueuedLike(order) as ConvertDollars;
            panel.Q<Label>("dollars-reserves").text = text.Format("dollars.reserves", PlayerText.Money(c, "fx_reserves", s.FxReserves));
            panel.Q<Label>("dollars-total").text = text.Format("dollars.total",
                PlayerText.Money(c, "fx_reserves", s.DollarsConverted), PlayerText.Money(c, "fx_reserves", m.DollarsTotal!.Value));
            // The rate in force on the turn's date, as EconomySystem.ConvertDollars uses it.
            panel.Q<Label>("dollars-money").text = text.Format("dollars.money", PlayerText.Money(c, "treasury", Math.Max(0, amount.value) * m.IlPerDollarOn(s.Date)));
            var attention = panel.Q<Label>("dollars-attention");
            attention.style.display = c.UsesAttention ? DisplayStyle.Flex : DisplayStyle.None;
            attention.text = c.UsesAttention ? text.Format("arms.attention", bridge.Price(order)!.Value, bridge.AttentionLeft!.Value) : "";
            attention.pickingMode = c.UsesAttention ? PickingMode.Position : PickingMode.Ignore;
            panel.Q<Label>("dollars-queued").text = queued == null ? "" : text.Format("dollars.queued", PlayerText.Money(c, "fx_reserves", queued.Amount));
            convert.SetEnabled(s.GameOver == null && amount.value > 0);
            withdraw.SetEnabled(s.GameOver == null && queued != null);
            // The sim's own check (EconomySystem.DollarsRefusal) on the state as it is now: End turn refuses the same amount.
            var refused = s.GameOver != null ? null : EconomySystem.DollarsRefusal(c, s, amount.value);
            panel.Q<Label>("dollars-refusal").text = s.GameOver != null ? text["order.game_over"]
                : refused != null ? SimBridge.ReasonText(text, refused)
                : !bridge.CanPay(order) ? text["dollars.no_attention"] : "";
        }

        public void Dispose()
        {
            bridge.Changed -= Render; presses.Dispose();
            convert.clicked -= onConvert; withdraw.clicked -= onWithdraw; amount.UnregisterValueChangedCallback(onAmount);
        }
    }
}
