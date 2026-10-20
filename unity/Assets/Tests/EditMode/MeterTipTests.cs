#nullable enable
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Each top-bar number explains itself (#1037, MeterTip): on hover, or after a tap, a tip under the bar gives the
/// meter in words (its tooltip) and the turn's main causes from the sim (HudModel.Causes), inside the window.</summary>
public class MeterTipTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    static void Enter(VisualElement e) { using var ev = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = e.worldBound.center }); ev.target = e; e.SendEvent(ev); }
    static void Leave(VisualElement e) { using var ev = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero }); ev.target = e; e.SendEvent(ev); }
    static void Tap(VisualElement e) { using var ev = ClickEvent.GetPooled(); ev.target = e; e.SendEvent(ev); }
    static void MouseClick(VisualElement e) { using var ev = ClickEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = e.worldBound.center }); ev.target = e; e.SendEvent(ev); }
    /// <summary>A finger going down on <paramref name="e"/>; on the map, with nothing under it, the panel sends it to its root.</summary>
    static void Press(VisualElement e) { using var ev = PointerDownEvent.GetPooled(new Touch { fingerId = 0, phase = TouchPhase.Began }); ev.target = e; e.SendEvent(ev); }

    /// <summary>Shown only when MeterTip set it so (an unset inline display reads as Flex, the default).</summary>
    static bool Shown(VisualElement tip) => tip.style.display.keyword == StyleKeyword.Undefined && tip.style.display.value == DisplayStyle.Flex;
    static string Words(VisualElement root) => root.Q<Label>("meter-tip-words").text;
    static string CausesLine(VisualElement root) => root.Q<Label>("meter-tip-causes").text;

    /// <summary>After the fixed opening turn, at 1280x720 and 390x844 in English and Hebrew: hovering the bar's first and
    /// last numbers shows each one's words and the sim's causes under the bar and inside the window; leaving hides it.</summary>
    [UnityTest]
    public IEnumerator HoverExplainsANumberUnderTheBarInsideTheWindow()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in new[] { (1280, 720), (390, 844) })
            {
                string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                yield return Rendered(w, h, hebrew, Check);

                IEnumerator Check(VisualElement root, SimBridge bridge, UiText text)
                {
                    var tip = root.Q(MeterTip.TipName);
                    foreach (var id in new[] { "treasury", "fx", "security" })
                    {
                        var meter = root.Q("meter-" + id);
                        Enter(meter);
                        for (int i = 0; i < 3; i++) yield return null;   // the tip is placed once its width is laid out
                        var causes = HudModel.Causes(bridge.Session.Last, id);
                        if (!Shown(tip)) { problems.Add($"{at} {id}: hovering shows no tip"); continue; }
                        if (Words(root) != meter.tooltip + "\n" + text["hud.causes"]) problems.Add($"{at} {id}: the tip's words are '{Words(root)}', not its tooltip and the causes' heading");
                        if (CausesLine(root) != string.Join("\n", causes)) problems.Add($"{at} {id}: the tip's causes are '{CausesLine(root)}', not the sim's '{string.Join(" | ", causes)}'");
                        if (UiDirection.RightToLeft(Words(root)) != hebrew) problems.Add($"{at} {id}: the words are not in the UI's language");
                        if (UiDirection.HasRightToLeft(CausesLine(root))) problems.Add($"{at} {id}: the sim's causes are expected in English (#55), in their own label");
                        if (causes.Count == 0) problems.Add($"{at} {id}: precondition: the opening turn has causes for it");
                        Rect t = tip.worldBound, m = meter.worldBound, win = root.worldBound;
                        if (t.xMin < win.xMin - 0.5f || t.xMax > win.xMax + 0.5f || t.yMax > win.yMax + 0.5f) problems.Add($"{at} {id}: the tip {t} leaves the window {win}");
                        if (t.yMin < m.yMax) problems.Add($"{at} {id}: the tip {t} is not under its number {m}");
                        Leave(meter);
                        if (Shown(tip)) problems.Add($"{at} {id}: the tip stays after the pointer leaves");
                    }
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>A touchscreen has no hover: a tap pins the tip, a tap on another number moves it, a second tap closes it, and
    /// End Turn closes it; the meter's tooltip stays its words alone (the accessibility text, HudModel.Spoken).</summary>
    [UnityTest]
    public IEnumerator ATapPinsTheTipAndASecondTapOrEndTurnClosesIt()
    {
        var problems = new List<string>();
        yield return Rendered(390, 844, false, Check);

        IEnumerator Check(VisualElement root, SimBridge bridge, UiText text)
        {
            var tip = root.Q(MeterTip.TipName);
            VisualElement money = root.Q("meter-treasury"), security = root.Q("meter-security");
            if (money.tooltip.Contains(text["hud.causes"])) problems.Add("the tooltip itself carries the causes");
            Enter(money); Tap(money); Leave(money);
            yield return null;
            if (!Shown(tip)) problems.Add("a tap does not keep the tip");
            Tap(security);
            yield return null;
            if (!Shown(tip) || !Words(root).StartsWith(security.tooltip)) problems.Add($"a tap on another number does not move the tip: '{Words(root)}'");
            Tap(security);
            if (Shown(tip)) problems.Add("a second tap does not close the tip");
            Tap(money);
            if (!Shown(tip)) problems.Add("a tap after closing does not open it again");
            bridge.EndTurn();
            if (Shown(tip)) problems.Add("End Turn does not close the tip");
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>#1108: a mouse has hover, so a click on a number does not pin its tip and leaving hides it; a tapped tip closes
    /// on the next tap anywhere (the map, another control), while a tap on a number still moves or closes it.</summary>
    [UnityTest]
    public IEnumerator AClickedTipHidesOnLeaveAndATappedTipOnTheNextTapAnywhere()
    {
        var problems = new List<string>();
        yield return Rendered(1280, 720, false, Check);

        IEnumerator Check(VisualElement root, SimBridge bridge, UiText text)
        {
            var tip = root.Q(MeterTip.TipName);
            VisualElement money = root.Q("meter-treasury"), security = root.Q("meter-security");
            Enter(money); MouseClick(money); Leave(money);
            if (Shown(tip)) problems.Add("a mouse click keeps the tip after the pointer leaves");
            foreach (var (where, e) in new[] { ("the map", root.panel.visualTree), ("another control", root.Q("end-turn")) })
            {
                Tap(money);
                if (!Shown(tip)) { problems.Add($"precondition: a tap shows the tip (before {where})"); continue; }
                Press(e);
                if (Shown(tip)) problems.Add($"a tap on {where} does not close a tapped tip");
            }
            Tap(money);
            Press(security); Tap(security);
            if (!Shown(tip) || !Words(root).StartsWith(security.tooltip)) problems.Add($"a tap on another number does not move the tip: '{Words(root)}'");
            Press(security); Tap(security);
            if (Shown(tip)) problems.Add("a second tap on the number does not close the tip");
            Tap(money); Press(security); MouseClick(security);
            if (Shown(tip)) problems.Add("a mouse click on a number does not drop a tap's pin (a touchscreen laptop)");
            yield break;
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>Renders Game.uxml at w x h with the HUD after one turn, then runs <paramref name="check"/>, which may wait frames.</summary>
    static IEnumerator Rendered(int w, int h, bool hebrew, System.Func<VisualElement, SimBridge, UiText, IEnumerator> check)
    {
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("MeterTipLayout"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var text = hebrew ? He : En;
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        try
        {
            bridge.EndTurn();
            for (int i = 0; i < 30 && !(root.Q("meter-fx").layout.width > 0); i++) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            var inner = check(root, bridge, text);
            while (inner.MoveNext()) yield return inner.Current;
        }
        finally
        {
            hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
