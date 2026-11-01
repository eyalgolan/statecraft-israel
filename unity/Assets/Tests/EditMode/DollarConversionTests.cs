#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>R-MONEY-004, "Spend the dollars" (#813; owner, 2 November 2026: "Spend the dollars (Recommended)"): the Defence
/// screen's conversion panel, beside the Arms purchase. It queues one ConvertDollars for End turn, shows the foreign-currency
/// reserve (the one readout the ruling allows, never on the top bar) and says the sim's refusal before End turn.</summary>
public class DollarConversionTests
{
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static UiText En => Text(false);
    static UiText He => Text(true);

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

    // R-MONEY-004: the panel lives in Defence and shows the reserve, the total converted against the cap and the Money the
    // amount brings, in the content's own money format; the top bar gains nothing.
    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void ThePanelShowsTheReserveAndTheMoneyItBrings(bool modern, bool he)
    {
        var b = Bridge(modern, he); var t = Text(he); var c = b.Content; var m = c.RulesAt(b.State.Date).Mvp!;
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var defence = new DefencePresenter(ui.Root, b, t);
        var panel = ui.Q<VisualElement>("defence-dollars");
        Assert.AreEqual(DisplayStyle.Flex, panel.style.display.value);
        Assert.IsTrue(panel.Contains(ui.Q<Label>("dollars-reserves")));
        Assert.AreEqual(t.Format("dollars.reserves", PlayerText.Money(c, "fx_reserves", b.State.FxReserves)), ui.Q<Label>("dollars-reserves").text);
        Assert.AreEqual(t.Format("dollars.total", PlayerText.Money(c, "fx_reserves", 0), PlayerText.Money(c, "fx_reserves", m.DollarsTotal!.Value)),
            ui.Q<Label>("dollars-total").text);
        var amount = ui.Q<IntegerField>("dollars-amount");
        Assert.AreEqual(m.DollarsPerTurn!.Value, amount.value, "the amount starts at the month's limit");
        amount.value = 3;
        Assert.AreEqual(t.Format("dollars.money", PlayerText.Money(c, "treasury", 3 * m.IlPerDollarOn(b.State.Date))), ui.Q<Label>("dollars-money").text);
        CollectionAssert.AreEqual(new[] { "treasury", "immigration", "security", "support", "cohesion", "standing", "fx" }, HudModel.MeterIds,
            "the top bar's meters are unchanged");
        Assert.IsNull(ui.Root.Q("meter-dollars"));
    }

    // R-MONEY-004: Convert queues one order (a second replaces it), the state is untouched until End turn, which converts.
    [TestCase(false)] [TestCase(true)]
    public void ConvertQueuesOneOrderAndEndTurnConverts(bool he)
    {
        var b = Bridge(false, he); var t = Text(he); var c = b.Content;
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        var before = b.State.Hash(); var fx = b.State.FxReserves;
        var amount = ui.Q<IntegerField>("dollars-amount");
        amount.value = 2; ui.Press("convert-dollars");
        amount.value = 3; ui.Press("convert-dollars");
        Assert.AreEqual(3, ((ConvertDollars)b.Queued.Single()).Amount);
        Assert.AreEqual(before, b.State.Hash());
        Assert.AreEqual(t.Format("dollars.queued", PlayerText.Money(c, "fx_reserves", 3)), ui.Q<Label>("dollars-queued").text);
        ui.Press("withdraw-dollars");
        Assert.IsEmpty(b.Queued);
        ui.Press("convert-dollars");
        b.EndTurn();
        Assert.IsEmpty(b.Queued);
        Assert.AreEqual(3, b.State.DollarsConverted);
        Assert.IsTrue(b.Session.Last!.Why.Any(w => w.Key == CauseKeys.DollarsConverted));
        Assert.AreEqual(t.Format("dollars.total", PlayerText.Money(c, "fx_reserves", 3), PlayerText.Money(c, "fx_reserves", c.RulesAt(b.State.Date).Mvp!.DollarsTotal!.Value)),
            ui.Q<Label>("dollars-total").text);
        Assert.AreEqual(t.Format("dollars.reserves", PlayerText.Money(c, "fx_reserves", b.State.FxReserves)), ui.Q<Label>("dollars-reserves").text);
        Assert.AreNotEqual(fx, b.State.FxReserves);
        Assert.IsEmpty(ui.Q<Label>("dollars-queued").text);
    }

    // R-MONEY-004: the panel says, before End turn, the reason the sim would refuse the amount (EconomySystem's own check),
    // in the player's language; Convert stays available so the refusal stays explainable.
    [TestCase(false)] [TestCase(true)]
    public void TheSimsRefusalIsShownBeforeEndTurn(bool he)
    {
        var b = Bridge(false, he); var t = Text(he);
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        var amount = ui.Q<IntegerField>("dollars-amount");
        var refusal = ui.Q<Label>("dollars-refusal");
        amount.value = 4;
        Assert.IsEmpty(refusal.text, "$4m in the war is allowed");
        amount.value = 5;
        Assert.AreEqual(t["dollars.refused_monthly"], refusal.text);
        amount.value = 0;
        Assert.AreEqual(t["dollars.refused_minimum"], refusal.text);
        Assert.IsFalse(ui.Q<Button>("convert-dollars").enabledSelf);
        b.State.DollarsConverted = 98;
        amount.value = 3;
        Assert.AreEqual(t["dollars.refused_total"], refusal.text);
        b.State.DollarsConverted = 0; b.State.FxReserves = 2;
        amount.value = 4; amount.value = 3;
        Assert.AreEqual(t["dollars.refused_reserves"], refusal.text);
        b.State.FxReserves = 40; b.State.Rung = 3;
        amount.value = 4; amount.value = 3;
        Assert.AreEqual(t["dollars.refused_not_at_war"], refusal.text);
        Assert.IsTrue(ui.Q<Button>("convert-dollars").enabledSelf, "a refusal remains explainable");
        b.State.GameOver = "finished";
        amount.value = 2;
        Assert.AreEqual(t["order.game_over"], refusal.text);
        Assert.IsFalse(ui.Q<Button>("convert-dollars").enabledSelf);
    }

    // R-MONEY-004: the legacy loop has no conversion.
    [Test]
    public void TheLegacyLoopHidesThePanel()
    {
        var t = En;
        var b = new SimBridge(GameSession.New(SimBridgeGddTests.LoadLegacy(), GoldenRun.Seed, Mode.Historical), t);
        using var ui = new UiTestKit();
        using var defence = new DefencePresenter(ui.Root, b, t);
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("defence-dollars").style.display.value);
    }

    // R-MONEY-004: every refusal reaches the end-of-turn toast (SimBridge.ReasonText) as the sim's English, and in Hebrew
    // with no Latin letters.
    [Test]
    public void EveryRefusalHasAStringInBothLanguages()
    {
        var refusals = typeof(EconomySystem).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.Name.StartsWith("RefusedDollars")).Select(f => (string)f.GetRawConstantValue()).ToList();
        Assert.AreEqual(5, refusals.Count);
        foreach (var reason in refusals)
        {
            Assert.AreEqual(reason, SimBridge.ReasonText(En, reason), "the English string is the sim's own");
            var he = SimBridge.ReasonText(He, reason);
            Assert.AreNotEqual(reason, he, "no Hebrew string for: " + reason);
            StringAssert.IsMatch("[א-ת]", he, reason);
            StringAssert.DoesNotMatch("[A-Za-z]", he, reason);
        }
    }

    // R-MONEY-004: the cause line reads the same as the sim's English, and in Hebrew in the player's language.
    [Test]
    public void TheCauseLineIsWordedInBothLanguages()
    {
        var b = Bridge(false, false);
        var line = Simulation.Step(b.Content, b.State, new Command[] { new ConvertDollars(4) }).Why.Single(w => w.Key == CauseKeys.DollarsConverted);
        Assert.AreEqual(line.Message, CauseText.Line(line, b.Content, System.Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), En));
        var he = CauseText.Line(line, b.Content, System.Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), He);
        StringAssert.IsMatch("[א-ת]", he);
        StringAssert.Contains(PlayerText.Money(b.Content, "fx_reserves", 4), he);
    }
}
