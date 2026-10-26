#nullable enable
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class FoodArmsHudTests
{
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static SimBridge Bridge(bool modern, bool he)
    {
        var dir = Paths.Content;
        var c = ContentLoader.Load(rel =>
        {
            var path = System.IO.Path.Combine(dir, rel);
            if (!System.IO.File.Exists(path)) return null;
            var value = System.IO.File.ReadAllText(path);
            return modern && rel == "eras.yaml" ? value.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : value;
        }, System.IO.Directory.GetFiles(System.IO.Path.Combine(dir, "events"), "*.yaml").Select(x => "events/" + System.IO.Path.GetFileName(x)));
        var b = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text(he));
        if (modern)
        {
            foreach (var r in c.EraRules.Values.Append(c.Rules))
            { r.AttentionCost.Clear(); r.AttentionPerTurn = double.NaN; }
            b.State.Attention = double.NaN;
        }
        return b;
    }

    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void StocksChangeOnlyAtEndTurnAndTheHudUsesSimCauses(bool modern, bool he)
    {
        var b = Bridge(modern, he); var t = Text(he);
        var start = b.State.Arms;
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var defence = new DefencePresenter(ui.Root, b, t);
        var opening = HudModel.From(b, t);
        Assert.IsTrue(opening.ShowsPools);
        Assert.AreEqual(t.Format("hud.stock", b.State.Food), opening.Food.Value);
        Assert.AreEqual(t.Format("hud.stock", start), opening.Arms.Value);
        Assert.IsEmpty(opening.Arms.Delta);
        Assert.AreEqual(t["hud.caption.food"], ui.Q<VisualElement>("meter-food").Q<Label>(className: "meter-name").text);
        Assert.AreEqual(t["hud.caption.arms"], ui.Q<VisualElement>("meter-arms").Q<Label>(className: "meter-name").text);
        ui.Q<IntegerField>("arms-count").value = 3;
        Assert.AreEqual(t.Format("arms.money", PlayerText.Money(b.Content, "treasury", 3 * b.Content.RulesAt(b.State.Date).Mvp!.ArmsPrice!.Value)),
            ui.Q<Label>("arms-money").text);
        ui.Press("buy-arms");
        Assert.AreEqual(start, b.State.Arms);
        Assert.AreEqual(3, ((BuyArms)b.Queued.Single()).Count);
        Assert.AreEqual(t.Format("arms.queued", 3), ui.Q<Label>("arms-queued").text);
        b.EndTurn();
        Assert.IsEmpty(b.Queued);
        Assert.AreEqual(start + 3, b.State.Arms);
        Assert.AreEqual(t.Format("hud.delta_stock", 3), HudModel.From(b, t).Arms.Delta);
        Assert.IsEmpty(ui.Q<Label>("arms-queued").text);
        var expected = b.Session.Last!.Why.Where(w => w.Target == Pools.ArmsTarget && w.Delta != 0)
            .OrderByDescending(w => Math.Abs(w.Delta)).Take(HudModel.MaxCauses).Select(w => w.Message).ToArray();
        CollectionAssert.AreEqual(expected, HudModel.Causes(b.Session.Last, "arms", w => w.Message));
        Assert.IsNotEmpty(expected);
        Assert.IsTrue(TurnReportPresenter.Lines(b, t).Any(x => x.Meter == "arms"));
        StringAssert.Contains(t.Format("hud.stock", start + 3), ui.Q<VisualElement>("meter-arms").tooltip);
    }

    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void ReplaceWithdrawAndRefuseWithoutChangingStocks(bool modern, bool he)
    {
        var b = Bridge(modern, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        var before = b.State.Hash();
        var count = ui.Q<IntegerField>("arms-count");
        count.value = 2; ui.Press("buy-arms");
        count.value = 5; ui.Press("buy-arms");
        Assert.AreEqual(5, ((BuyArms)b.Queued.Single()).Count);
        Assert.AreEqual(before, b.State.Hash());
        if (!modern)
            Assert.AreEqual(b.TurnStart.Attention - b.Price(new BuyArms(5))!.Value, b.AttentionLeft!.Value, 1e-9);
        else
        {
            Assert.IsNull(b.Price(new BuyArms(5)));
            Assert.IsNull(b.AttentionLeft);
            Assert.AreEqual(DisplayStyle.None, ui.Q<Label>("arms-attention").style.display.value);
            Assert.IsEmpty(ui.Q<Label>("arms-attention").text);
        }
        ui.Press("withdraw-arms");
        Assert.IsEmpty(b.Queued);
        Assert.AreEqual(before, b.State.Hash());
        count.value = 0;
        Assert.IsFalse(ui.Q<Button>("buy-arms").enabledSelf);
        Assert.AreEqual(t["arms.minimum"], ui.Q<Label>("arms-refusal").text);
        count.value = 2;
        if (!modern)
        {
            b.State.Attention = 0;
            count.value = 3; // refresh the actual control
            string? refused = null; b.Refused += reason => refused = reason;
            Assert.IsTrue(ui.Q<Button>("buy-arms").enabledSelf, "a refusal remains explainable");
            ui.Press("buy-arms");
            Assert.IsNotNull(refused);
            Assert.AreEqual(refused, ui.Q<Label>("arms-refusal").text);
            Assert.IsEmpty(b.Queued);
        }
        b.State.GameOver = "finished";
        count.value = 4;
        Assert.IsFalse(ui.Q<Button>("buy-arms").enabledSelf);
        Assert.IsFalse(ui.Q<Button>("withdraw-arms").enabledSelf);
        Assert.AreEqual(t["order.game_over"], ui.Q<Label>("arms-refusal").text);
    }

    [Test]
    public void PoolCausesIgnoreTheSupplyIndicatorAndKeepTheSimTieOrder()
    {
        var b = Bridge(false, false);
        var turn = new StepResult(b.State, new[]
        {
            new Cause("economy", "food_supply", 1000, "indicator"),
            new Cause("economy", "food", -10, "food first"),
            new Cause("events", "food", 10, "food tied"),
            new Cause("economy", "food", 1, "food smaller"),
            new Cause("events", "arms", 3, "arms"),
        }, Array.Empty<Rejection>());
        CollectionAssert.AreEqual(new[] { "Food first", "Food tied" }, HudModel.Causes(turn, "food", w => w.Message));
        CollectionAssert.AreEqual(new[] { "Arms" }, HudModel.Causes(turn, "arms", w => w.Message));
    }

    [Test]
    public void LegacyLoopHidesThePoolsAndPurchasePanel()
    {
        var t = Text(false);
        var b = new SimBridge(GameSession.New(SimBridgeGddTests.LoadLegacy(), GoldenRun.Seed, Mode.Historical), t);
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var defence = new DefencePresenter(ui.Root, b, t);
        Assert.IsFalse(HudModel.From(b, t).ShowsPools);
        foreach (var name in new[] { "meter-food", "meter-arms", "defence-arms" })
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>(name).style.display.value);
    }

    [TestCase(false)] [TestCase(true)]
    public void RebuildingTheScreenKeepsTheQueuedAmountAndDisposalRemovesHandlers(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        Assert.IsNull(b.Queue(new BuyArms(9)));
        using var ui = new UiTestKit();
        var presenter = new ArmsPurchasePresenter(ui.Root, b, t);
        Assert.AreEqual(9, ui.Q<IntegerField>("arms-count").value);
        presenter.Dispose();
        ui.Press("withdraw-arms");
        Assert.AreEqual(9, ((BuyArms)b.Queued.Single()).Count);
        using var next = new ArmsPurchasePresenter(ui.Root, b, t);
        ui.Press("withdraw-arms");
        Assert.IsEmpty(b.Queued);
    }

    // Real panels at desktop, laptop and phone sizes; EN and mirrored HE. No screenshot is claimed by this test draft.
    [UnityTest]
    public IEnumerator ResourceMetersFitInEnglishAndHebrew()
    {
        foreach (var he in new[] { false, true })
        foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(375, 667) })
        {
            var rt = new RenderTexture(size.x, size.y, 0);
            var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
            settings.targetTexture = rt;
            var go = new GameObject("FoodArmsLayout"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = settings;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            var root = doc.rootVisualElement;
            HudPresenter? hud = null;
            try
            {
                if (he) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                for (int i = 0; i < 20; i++) yield return null;
                root.Q("root").RemoveFromClassList("loading");
                var b = Bridge(false, he); var t = Text(he);
                b.EndTurn();
                b.State.Food = 123456; b.State.Arms = 12345;
                hud = new HudPresenter(root, b, t);
                for (int i = 0; i < 20; i++) yield return null;
                var bar = root.Q("top-bar").worldBound;
                foreach (var id in new[] { "food", "arms" })
                {
                    var meter = root.Q("meter-" + id); var value = root.Q<Label>(id + "-value");
                    Assert.Greater(value.worldBound.width, 0);
                    Assert.GreaterOrEqual(meter.worldBound.xMin, bar.xMin - 1);
                    Assert.LessOrEqual(meter.worldBound.xMax, bar.xMax + 1);
                    Assert.GreaterOrEqual(meter.worldBound.yMin, bar.yMin - 1);
                    Assert.LessOrEqual(meter.worldBound.yMax, bar.yMax + 1);
                    var caption = meter.Q<Label>(className: "meter-name");
                    Assert.GreaterOrEqual(caption.resolvedStyle.fontSize * root.panel.scaledPixelsPerPoint, HudPresenter.TextFloorPx - .1f);
                    Assert.LessOrEqual(value.MeasureTextSize(value.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x,
                        value.worldBound.width + 1, id + " stock must not clip");
                }
                var visible = root.Q("meters").Children().Where(x => x.resolvedStyle.display != DisplayStyle.None).ToList();
                for (int i = 0; i < visible.Count; i++)
                for (int j = i + 1; j < visible.Count; j++)
                    Assert.IsFalse(visible[i].worldBound.Overlaps(visible[j].worldBound), "meters must not overlap");
            }
            finally
            {
                hud?.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(settings);
                rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
