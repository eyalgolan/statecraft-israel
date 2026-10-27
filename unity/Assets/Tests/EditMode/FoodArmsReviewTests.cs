#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class FoodArmsReviewTests
{
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static SimBridge Bridge(bool he, bool modern = false, bool legacy = false)
    {
        Content c;
        if (legacy) c = SimBridgeGddTests.LoadLegacy();
        else if (!modern) c = ContentLoader.Load(Paths.Content);
        else
        {
            c = ContentLoader.Load(rel =>
            {
                var path = Path.Combine(Paths.Content, rel);
                return !File.Exists(path) ? null : rel == "eras.yaml"
                    ? File.ReadAllText(path).Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : File.ReadAllText(path);
            }, Directory.GetFiles(Path.Combine(Paths.Content, "events"), "*.yaml").Select(x => "events/" + Path.GetFileName(x)));
        }
        return new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), Text(he));
    }

    // S2: mutate only the PRE-turn fixture, never manufacture a delta after the sim has run.
    [TestCase(false, false)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(true, true)]
    public void RealFoodChangeExplainsTheActualStockInReportAndWhy(bool he, bool crisis)
    {
        var b = Bridge(he); var t = Text(he);
        if (crisis) { b.State.ActiveSituations.Add(EconomySystem.FxCrisis); b.State.FxReserves = -100; }
        else b.State.Food += 7; // cover plus seven: normal consumption/top-up, no unmet-need cause; delta is real.
        int before = b.State.Food;
        using var ui = new UiTestKit();
        using var hud = new HudPresenter(ui.Root, b, t);
        using var nav = new ScreenNav(ui.Root);
        using var report = new TurnReportPresenter(ui.Root, b, t, nav);
        b.EndTurn();
        Assert.IsNotNull(b.Session.Last);
        Assert.AreNotEqual(before, b.State.Food, "a real consumption/import delta is required");
        long delta = (long)b.State.Food - before;
        var expected = b.Session.Last!.Why.Where(w => w.Target == Pools.FoodTarget && w.Delta != 0)
            .OrderByDescending(w => Math.Abs(w.Delta)).Take(HudModel.MaxCauses)
            .Select(w => CauseText.Line(w, b.Content, Array.Empty<Statecraft.Game.Map.Units.UnitEntry>(), t)).ToArray();
        if (crisis) { Assert.IsNotEmpty(expected); StringAssert.Contains("No Food imported", b.Session.Last.Why.Single(w => w.Target == Pools.FoodTarget && w.Message.Contains("No Food imported")).Message); }
        else Assert.IsEmpty(expected, "do not invent causes for normal automatic imports");
        using var why = new WhyPresenter(ui.Root, b, t);
        StringAssert.Contains(t.Format("hud.stock", b.State.Food), ui.Q<Label>("why-stock-food").text);
        var row = TurnReportPresenter.Lines(b, t).Single(x => x.Meter == "food");
        CollectionAssert.AreEqual(expected, row.Causes);
        Assert.AreEqual(t.Format("report.stock_change", t["hud.caption.food"], t.Format("hud.stock", b.State.Food), t.Format("hud.delta_stock", delta)), row.Heading);
        Assert.AreEqual(row.Heading, ui.Q<Label>("turn-report-heading-food").text);
        CollectionAssert.AreEqual(expected, ui.Q<VisualElement>("turn-report-food").Query<Label>(className: "turn-report-cause").ToList().Select(x => x.text));
    }

    static IEnumerator Frames(int n = 15) { for (int i = 0; i < n; i++) yield return null; }
    static string Inv(FormattableString value) => FormattableString.Invariant(value);
    static void Snapshot(RenderTexture rt, string name)
    {
        var dir = Environment.GetEnvironmentVariable("FOOD_ARMS_CAPTURE_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        var prior = RenderTexture.active;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        try { RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(dir, name + ".png"), image.EncodeToPNG()); }
        finally { RenderTexture.active = prior; Object.DestroyImmediate(image); }
    }
    static IEnumerator Panel(int width, int height, bool he, Func<VisualElement, RenderTexture, IEnumerator> check)
    {
        var rt = new RenderTexture(width, height, 0); rt.Create();
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.black;
        var go = new GameObject("FoodArmsReview"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
        try
        {
            var root = doc.rootVisualElement;
            if (he) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
            root.Q("root").RemoveFromClassList("loading");
            yield return Frames(); yield return check(root, rt);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
    }

    // S1: original arithmetic, including hidden cohesion, copied from the task's fresh main FloorGrowth.
    // No general hidden-caption filter is allowed.
    [UnityTest]
    public IEnumerator LegacyRaisedFontKeepsTheOriginalGrowthAndTierBoundaries()
    {
        foreach (bool he in new[] { false, true })
        foreach (var size in new[] { (1024,768), (960,600), (800,600), (1280,720), (375,667) })
            yield return Panel(size.Item1, size.Item2, he, Check);
        IEnumerator Check(VisualElement root, RenderTexture rt)
        {
            var b = Bridge(root.Q("root").ClassListContains(UiLanguage.HebrewClass), legacy:true);
            var t = Text(root.Q("root").ClassListContains(UiLanguage.HebrewClass));
            using var hud = new HudPresenter(root,b,t);
            yield return Frames(25);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            float small = (float)typeof(HudPresenter).GetField("smallTextSize", flags)!.GetValue(hud)!;
            Assert.Greater(small, HudPresenter.SmallTextPx, "fixture must exercise raised-font arithmetic");
            float turnWidth = (float)typeof(HudPresenter).GetProperty("TurnBoxWidth", flags)!.GetValue(hud)!;
            float plateWidth = (float)typeof(HudPresenter).GetMethod("RaisedPlateWidth", flags)!.Invoke(hud,null)!;
            float expected = turnWidth - 150f + plateWidth - 230f;
            var captions = (System.Collections.Generic.List<Label>)typeof(HudPresenter).GetField("smallText", flags)!.GetValue(hud)!;
            var cohesion = captions.Single(x => root.Q("meter-cohesion").Contains(x));
            Assert.Greater(cohesion.resolvedStyle.fontSize,0,"exercise original hidden-cohesion measurement");
            foreach (var l in captions)
            {
                if (l.name == "attention" || string.IsNullOrEmpty(l.text)) continue;
                var current = l.resolvedStyle.fontSize; if (!(current>0)) continue;
                var oneLine = l.MeasureTextSize(l.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).x;
                expected += oneLine * small/current - Mathf.Min(oneLine*15f/current,120f);
            }
            expected = Mathf.Max(0,expected);
            float actual = (float)typeof(HudPresenter).GetMethod("FloorGrowth",flags)!.Invoke(hud,null)!;
            Assert.AreEqual(expected,actual,.01f,"legacy must retain the exact original hidden-cohesion growth term");
            var bar=root.Q("top-bar"); var frame=root.Q("root"); float width=bar.layout.width;
            Assert.AreEqual(width-expected<HudPresenter.CompactBelowPx,bar.ClassListContains(HudPresenter.CompactClass));
            Assert.AreEqual(width-expected<HudPresenter.NarrowBelowPx || b.Content.UsesAttention && small>HudPresenter.OneRowSmallMaxPx,frame.ClassListContains(HudPresenter.NarrowClass));
            Snapshot(rt,Inv($"legacy-{(t.Has("arms.count")?"candidate":"base")}-{(UiDirection.HasRightToLeft(t["hud.caption.treasury"])?"he":"en")}-{rt.width}x{rt.height}"));
        }
    }

    static void Reach(VisualElement root, ScrollView scroll, VisualElement e)
    {
        var viewport=scroll.contentViewport.worldBound; var bounds=e.worldBound;
        Assert.Greater(bounds.width,4,e.name); Assert.Greater(bounds.height,4,e.name);
        Assert.GreaterOrEqual(bounds.xMin,viewport.xMin-1,e.name); Assert.LessOrEqual(bounds.xMax,viewport.xMax+1,e.name);
        Assert.GreaterOrEqual(bounds.yMin,viewport.yMin-1,e.name); Assert.LessOrEqual(bounds.yMax,viewport.yMax+1,e.name);
        Assert.IsTrue(e.enabledInHierarchy,e.name);
        if (e is Button || e is IntegerField)
        {
            var picked = root.panel.Pick(bounds.center);
            Assert.IsTrue(picked == e || e.Contains(picked), e.name + " must receive a pointer at its visible centre");
        }
    }

    [UnityTest]
    public IEnumerator PurchaseControlsAreReachableWithKeyboardInEnglishAndHebrew()
    {
        foreach(bool he in new[]{false,true})
        foreach(var size in new[]{(1920,1080),(1280,720),(375,667)})
            yield return Panel(size.Item1,size.Item2,he,Check);
        IEnumerator Check(VisualElement root,RenderTexture rt)
        {
            bool he=root.Q("root").ClassListContains(UiLanguage.HebrewClass);
            // Modern and gdd-0.1 run separately, without changing production rules or Attention helpers.
            foreach(bool modern in new[]{false,true})
            {
                var b=Bridge(he,modern); var t=Text(he);
                using var hud=new HudPresenter(root,b,t); using var nav=new ScreenNav(root); using var defence=new DefencePresenter(root,b,t);
                UiTestKit.Press(root.Q<Button>("nav-defence")); yield return Frames();
                Assert.IsTrue(ScreenNav.IsOpen(root.Q("screen-defence")));
                var scroll=root.Q<ScrollView>("defence-body"); var count=root.Q<IntegerField>("arms-count");
                var buy=root.Q<Button>("buy-arms"); var withdraw=root.Q<Button>("withdraw-arms");
                var input = count.Q(className: "unity-base-field__input");
                Assert.IsNotNull(input);
                var ink = input.resolvedStyle.color; var paper = input.resolvedStyle.backgroundColor;
                Assert.Greater(Mathf.Abs(ink.grayscale - paper.grayscale), .5f, "amount text must contrast with its input background");
                foreach(var amount in new[]{2,5})
                {
                    scroll.ScrollTo(count); yield return Frames(4); Reach(root,scroll,count);
                    count.Focus(); yield return null;
                    Assert.IsTrue(count.Contains(root.panel.focusController.focusedElement as VisualElement) || root.panel.focusController.focusedElement==count);
                    count.value=amount;
                    scroll.ScrollTo(buy); yield return Frames(4); Reach(root,scroll,buy);
                    buy.Focus(); yield return null; Assert.AreEqual(buy,root.panel.focusController.focusedElement);
                    UiTestKit.Press(buy); Assert.AreEqual(amount,((BuyArms)b.Queued.Single()).Count);
                    Assert.AreEqual(t.Format("arms.queued",amount),root.Q<Label>("arms-queued").text);
                }
                scroll.ScrollTo(withdraw); yield return Frames(4); Reach(root,scroll,withdraw);
                withdraw.Focus(); yield return null; Assert.AreEqual(withdraw,root.panel.focusController.focusedElement);
                UiTestKit.Press(withdraw); Assert.IsEmpty(b.Queued);
                if(!modern)
                {
                    b.State.Attention=0; count.value=3; scroll.ScrollTo(buy); yield return Frames(4);
                    UiTestKit.Press(buy); Assert.IsEmpty(b.Queued); Assert.IsNotEmpty(root.Q<Label>("arms-refusal").text);
                }
                else { Assert.AreEqual(DisplayStyle.None,root.Q("arms-attention").resolvedStyle.display); Assert.IsEmpty(root.Q<Label>("arms-attention").text); }
                foreach(var name in new[]{"arms-money","arms-queued","arms-refusal"})
                {
                    var l=root.Q<Label>(name); if(string.IsNullOrEmpty(l.text))continue;
                    scroll.ScrollTo(l); yield return Frames(4); Reach(root,scroll,l);
                    var needed=l.MeasureTextSize(l.text,l.contentRect.width,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(needed.y,l.contentRect.height+1,name+" must wrap without clipping");
                }
                Snapshot(rt,Inv($"purchase-{(he?"he":"en")}-{(modern?"gdd03":"gdd01")}-{rt.width}x{rt.height}"));
                nav.Open(null); yield return Frames(3);
            }
        }
    }

    // REV2 S1: Withdraw disables itself once the order is gone. Pressed from the keyboard, the focus must move to an
    // enabled purchase control, not be left nowhere, so Tab and Shift+Tab carry on from the purchase panel.
    [UnityTest]
    public IEnumerator KeyboardWithdrawKeepsTheFocusInThePurchasePanel()
    {
        foreach(bool he in new[]{false,true})
            yield return Panel(1280,720,he,Check);
        IEnumerator Check(VisualElement root,RenderTexture rt)
        {
            bool he=root.Q("root").ClassListContains(UiLanguage.HebrewClass);
            var b=Bridge(he); var t=Text(he);
            using var hud=new HudPresenter(root,b,t); using var nav=new ScreenNav(root); using var defence=new DefencePresenter(root,b,t);
            nav.Open("defence"); yield return Frames();
            var fc=root.panel.focusController; var arms=root.Q("defence-arms");
            var count=root.Q<IntegerField>("arms-count"); var buy=root.Q<Button>("buy-arms"); var withdraw=root.Q<Button>("withdraw-arms");
            count.value=2; UiTestKit.Press(buy); Assert.AreEqual(2,((BuyArms)b.Queued.Single()).Count, "precondition: one order queued");
            yield return null; Assert.IsTrue(withdraw.enabledSelf, "precondition: Withdraw is enabled while an order is queued");
            withdraw.Focus(); yield return null; Assert.AreEqual(withdraw,fc.focusedElement, "precondition: Withdraw has the focus");
            using (var k=KeyDownEvent.GetPooled('\n',KeyCode.Return,EventModifiers.None)) { k.target=withdraw; withdraw.SendEvent(k); }
            using (var s=NavigationSubmitEvent.GetPooled()) { s.target=withdraw; withdraw.SendEvent(s); }
            yield return Frames(3);
            Assert.IsEmpty(b.Queued); Assert.IsFalse(withdraw.enabledSelf, "Withdraw disables itself with nothing queued");
            var at=fc.focusedElement as VisualElement;
            Assert.IsNotNull(at, "the keyboard focus must not be dropped");
            Assert.AreEqual(buy,at, "the focus moves to Buy, the enabled control beside Withdraw");
            Assert.IsTrue(at!.enabledInHierarchy && arms.Contains(at));
            using (var e=NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Previous)) { e.target=at; at.SendEvent(e); }
            yield return null;
            var back=fc.focusedElement as VisualElement;
            Assert.IsTrue(back!=null && (back==count || count.Contains(back)), "Shift+Tab from Buy goes back to the amount: " + back?.name);
            using (var e=NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target=back; back!.SendEvent(e); }
            yield return null;
            Assert.AreEqual(buy,fc.focusedElement, "Tab from the amount comes back to Buy");
            using (var e=NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { e.target=buy; buy.SendEvent(e); }
            yield return null;
            var next=fc.focusedElement as VisualElement;
            Assert.IsTrue(next!=null && next!=withdraw && next.enabledInHierarchy, "Tab from Buy skips the disabled Withdraw: " + next?.name);
            // A pointer press moves no focus (PressSource / PointerBlur policy), so a mouse Withdraw hands none out.
            UiTestKit.Press(buy); Assert.IsNotEmpty(b.Queued); yield return null;
            DecisionCardTests.PressWithPointer(withdraw); yield return Frames(3);
            Assert.IsEmpty(b.Queued); Assert.AreNotEqual(buy, fc.focusedElement, "a pointer Withdraw hands Buy no focus");
            nav.Open(null); yield return Frames(3);
        }
    }
}
