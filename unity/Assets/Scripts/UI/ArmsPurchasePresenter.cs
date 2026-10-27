#nullable enable
using System;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>One replaceable purchase at End turn. Stocks change only when the sim executes the order.</summary>
    public sealed class ArmsPurchasePresenter : IDisposable
    {
        readonly VisualElement panel;
        readonly SimBridge bridge;
        readonly UiText text;
        readonly IntegerField count;
        readonly Button buy, withdraw;
        readonly Action onBuy, onWithdraw;
        readonly EventCallback<ChangeEvent<int>> onCount;
        readonly PressSource presses;

        public ArmsPurchasePresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            panel = root.Q("defence-arms"); this.bridge = bridge; this.text = text;
            count = panel.Q<IntegerField>("arms-count");
            buy = panel.Q<Button>("buy-arms"); withdraw = panel.Q<Button>("withdraw-arms");
            count.label = text["arms.count"];
            panel.Q<Label>("arms-title").text = text["hud.caption.arms"];
            buy.text = text["arms.buy"]; withdraw.text = text["arms.withdraw"];
            onBuy = () =>
            {
                var reason = bridge.Queue(new BuyArms(count.value));
                Render();
                panel.Q<Label>("arms-refusal").text = reason ?? "";
            };
            presses = new PressSource(panel);
            onWithdraw = () =>
            {
                var queued = bridge.QueuedLike(new BuyArms(1));
                if (queued == null) return;
                var keyboard = presses.ByKeyboard;
                bridge.Withdraw(queued);
                // Render has just disabled Withdraw, which had the focus. Keep the keyboard in the panel: Buy, else the amount.
                if (keyboard) FocusReturn.ToOrWithin(buy, null, panel);
            };
            onCount = _ => Render();
            buy.clicked += onBuy; withdraw.clicked += onWithdraw; count.RegisterValueChangedCallback(onCount);
            bridge.Changed += Render;
            if (bridge.QueuedLike(new BuyArms(1)) is BuyArms old) count.SetValueWithoutNotify(old.Count);
            Render();
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            panel.style.display = c.IsGdd ? DisplayStyle.Flex : DisplayStyle.None;
            if (!c.IsGdd) return;
            var order = new BuyArms(count.value);
            var queued = bridge.QueuedLike(order) as BuyArms;
            panel.Q<Label>("arms-stock").text = text.Format("arms.stock", s.Arms);
            // Use the active era's price, just as Pools.BuyArms does. No training or combat-use assumption.
            var cost = count.value * c.RulesAt(s.Date).Mvp!.ArmsPrice!.Value;
            panel.Q<Label>("arms-money").text = text.Format("arms.money", PlayerText.Money(c, "treasury", cost));
            var attention = panel.Q<Label>("arms-attention");
            attention.style.display = c.UsesAttention ? DisplayStyle.Flex : DisplayStyle.None;
            attention.text = c.UsesAttention ? text.Format("arms.attention", bridge.Price(order)!.Value, bridge.AttentionLeft!.Value) : "";
            attention.pickingMode = c.UsesAttention ? PickingMode.Position : PickingMode.Ignore;
            panel.Q<Label>("arms-queued").text = queued == null ? "" : text.Format("arms.queued", queued.Count);
            // Attention refusal uses Queue's own check and wording. Keep the button available to explain it.
            buy.SetEnabled(s.GameOver == null && count.value > 0);
            withdraw.SetEnabled(s.GameOver == null && queued != null);
            panel.Q<Label>("arms-refusal").text = s.GameOver != null ? text["order.game_over"]
                : count.value < 1 ? text["arms.minimum"]
                : !bridge.CanPay(order) ? text["arms.no_attention"] : "";
        }

        public void Dispose()
        {
            bridge.Changed -= Render; presses.Dispose();
            buy.clicked -= onBuy; withdraw.clicked -= onWithdraw; count.UnregisterValueChangedCallback(onCount);
        }
    }
}
