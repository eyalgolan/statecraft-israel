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

    // The owner's ruling: Food and Arms stay off the main bar, Arms lives in Defence, and both stocks show in the turn report and Why.
    [TestCase(false)] [TestCase(true)]
    public void OwnerRulingKeepsTheMainBarMeterSet(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        CollectionAssert.AreEqual(new[] { "treasury", "immigration", "security", "support", "cohesion", "standing", "fx" }, HudModel.MeterIds);
        Assert.IsNull(ui.Root.Q("meter-food"));
        Assert.IsNull(ui.Root.Q("meter-arms"));
    }

    [TestCase(false)] [TestCase(true)]
    public void ArmsStockLivesInDefenceWithoutABarDuplicate(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        var stock = ui.Q<Label>("arms-stock");
        Assert.IsTrue(ui.Q<VisualElement>("defence-arms").Contains(stock));
        Assert.AreEqual(t.Format("arms.stock", b.State.Arms), stock.text);
        ui.Q<IntegerField>("arms-count").value = 3;
        ui.Press("buy-arms");
        Assert.AreEqual(t.Format("arms.stock", b.State.Arms), stock.text, "queueing does not spend or deliver stock");
        b.EndTurn();
        Assert.AreEqual(t.Format("arms.stock", b.State.Arms), stock.text, "Defence refreshes after the sim delivers");
        Assert.IsNull(ui.Root.Q("meter-arms"), "the owner moved this stock off the bar");
    }

    [TestCase(false)] [TestCase(true)]
    [SetCulture("he-IL")]
    public void ReportIncludesBothCurrentStocksEvenWhenArmsDidNotChange(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var report = new TurnReportPresenter(ui.Root, b, t, nav);
        b.EndTurn();
        Assert.AreEqual(b.Session.Previous!.Arms, b.State.Arms, "no purchase in this fixture");
        foreach (var id in new[] { "food", "arms" })
        {
            var row = TurnReportPresenter.Lines(b, t).Single(x => x.Meter == id);
            var now = id == "food" ? b.State.Food : b.State.Arms;
            var before = id == "food" ? b.Session.Previous!.Food : b.Session.Previous!.Arms;
            AssertStockDirection(he, t, now, (long)now - before, row.Heading);
            Assert.AreEqual(row.Heading, ui.Q<Label>("turn-report-heading-" + id).text);
        }
    }

    [TestCase(false)] [TestCase(true)]
    [SetCulture("he-IL")]
    public void WhyIncludesBothStocksAndKeepsEveryExistingCause(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var why = new WhyPresenter(ui.Root, b, t);
        Assert.IsNotNull(ui.Root.Q<Label>("why-stock-food"));
        Assert.IsNotNull(ui.Root.Q<Label>("why-stock-arms"));
        b.Queue(new BuyArms(3)); b.EndTurn();
        foreach (var id in new[] { "food", "arms" })
        {
            var now = id == "food" ? b.State.Food : b.State.Arms;
            var before = id == "food" ? b.Session.Previous!.Food : b.Session.Previous!.Arms;
            AssertStockDirection(he, t, now, (long)now - before, ui.Q<Label>("why-stock-" + id).text);
        }
        var actual = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Select(l => l.text).ToArray();
        var expected = WhyPresenter.Order.SelectMany(sys => b.Session.Last!.Why.Where(w => w.System == sys))
            .Select(w => CauseText.Line(w, b.Content, Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), t)).ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    // Literal expected output, independent of the translated summary patterns.
    [TestCase(false, 0, "")] [TestCase(true, 0, "")]
    [TestCase(false, -12, "-12")] [TestCase(true, -12, "-12")]
    [TestCase(false, 12, "+12")] [TestCase(true, 12, "+12")]
    [SetCulture("he-IL")]
    public void StockSummaryPatternsProtectHebrewNumbersAndLeaveEnglishUnchanged(bool he, int delta, string signed)
    {
        var t = Text(he);
        foreach (var id in new[] { "food", "arms" })
        {
            var caption = t["hud.caption." + id];
            var count = t.Format("hud.stock", 1200);
            var actual = delta == 0
                ? t.Format("report.stock_line", caption, count)
                : t.Format("report.stock_change", caption, count, t.Format("hud.delta_stock", delta));
            var expected = he ? caption + " במלאי: \u200e1,200\u200e" : caption + ": 1,200";
            if (delta != 0) expected += he ? " (שינוי של \u200e" + signed + "\u200e)" : " (" + signed + ")";
            Assert.AreEqual(expected, actual, id);
        }
    }

    // Ordinal on purpose: StringAssert compares by culture, which ignores U+200E, so it can neither see nor rule out the marks.
    static void AssertStockDirection(bool he, UiText t, int now, long delta, string heading)
    {
        var count = t.Format("hud.stock", now);
        var want = he ? "\u200e" + count + "\u200e" : count;
        Assert.IsTrue(heading.IndexOf(want, StringComparison.Ordinal) >= 0, $"'{want}' in '{heading}'");
        if (delta != 0)
        {
            var signed = t.Format("hud.delta_stock", delta);
            var wantSigned = he ? "\u200e" + signed + "\u200e" : signed;
            Assert.IsTrue(heading.IndexOf(wantSigned, StringComparison.Ordinal) >= 0, $"'{wantSigned}' in '{heading}'");
        }
        if (!he) Assert.AreEqual(-1, heading.IndexOf('\u200e'), "English headings carry no direction marks: " + heading);
    }

    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void StocksChangeOnlyAtEndTurnAndTheReportUsesSimCauses(bool modern, bool he)
    {
        var b = Bridge(modern, he); var t = Text(he);
        var start = b.State.Arms;
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var defence = new DefencePresenter(ui.Root, b, t);
        Assert.IsNull(ui.Root.Q("meter-food"));
        Assert.IsNull(ui.Root.Q("meter-arms"));
        Assert.AreEqual(t.Format("arms.stock", start), ui.Q<Label>("arms-stock").text);
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
        Assert.AreEqual(t.Format("arms.stock", start + 3), ui.Q<Label>("arms-stock").text);
        Assert.IsEmpty(ui.Q<Label>("arms-queued").text);
        var expected = b.Session.Last!.Why.Where(w => w.Target == Pools.ArmsTarget && w.Delta != 0)
            .OrderByDescending(w => Math.Abs(w.Delta)).Take(HudModel.MaxCauses).Select(w => w.Message).ToArray();
        CollectionAssert.AreEqual(expected, TurnReportPresenter.StockCauses(b.Session.Last, "arms", w => w.Message));
        Assert.IsNotEmpty(expected);
        Assert.IsTrue(TurnReportPresenter.Lines(b, t).Any(x => x.Meter == "arms"));
        StringAssert.Contains(t.Format("hud.stock", start + 3), TurnReportPresenter.Lines(b, t).Single(x => x.Meter == "arms").Heading);
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
        CollectionAssert.AreEqual(new[] { "Food first", "Food tied" }, TurnReportPresenter.StockCauses(turn, "food", w => w.Message));
        CollectionAssert.AreEqual(new[] { "Arms" }, TurnReportPresenter.StockCauses(turn, "arms", w => w.Message));
    }

    [Test]
    public void LegacyLoopHidesThePoolsAndPurchasePanel()
    {
        var t = Text(false);
        var b = new SimBridge(GameSession.New(SimBridgeGddTests.LoadLegacy(), GoldenRun.Seed, Mode.Historical), t);
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var defence = new DefencePresenter(ui.Root, b, t);
        Assert.IsNull(ui.Root.Q("meter-food"));
        Assert.IsNull(ui.Root.Q("meter-arms"));
        Assert.IsEmpty(TurnReportPresenter.StockLines(b, t));
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("defence-arms").style.display.value);
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
    public IEnumerator RestoredBarFitsInEnglishAndHebrew()
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
                Assert.IsNull(root.Q("meter-food"));
                Assert.IsNull(root.Q("meter-arms"));
                var visible = root.Q("meters").Children().Where(x => x.resolvedStyle.display != DisplayStyle.None).ToList();
                for (int i = 0; i < visible.Count; i++)
                for (int j = i + 1; j < visible.Count; j++)
                {
                    // As TopBarTests: shared snapped edges may differ by a subpixel, ink must stay disjoint.
                    static Rect In(Rect r) => new Rect(r.x + .5f, r.y + .5f, r.width - 1, r.height - 1);
                    Assert.IsFalse(In(visible[i].worldBound).Overlaps(In(visible[j].worldBound)),
                        FormattableString.Invariant($"{size.x}x{size.y} he={he}: {visible[i].name} {visible[i].worldBound} overlaps {visible[j].name} {visible[j].worldBound}"));
                }
            }
            finally
            {
                hud?.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(settings);
                rt.Release(); Object.DestroyImmediate(rt);
            }
        }
    }
}
