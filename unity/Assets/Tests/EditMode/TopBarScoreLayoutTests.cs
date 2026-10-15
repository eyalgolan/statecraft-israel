#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The score in the top bar's date box (#380, 7b): a worst-case score stays inside the box's frame, on no other
/// text, and whole, at every window size the HUD's tests use, in English and Hebrew.</summary>
public class TopBarScoreLayoutTests
{
    static readonly Content Slice = MvpSlice.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>Five digits with the thousands separator: well past what a Phase 1 or MVP run can score.</summary>
    const int WorstScore = 99999;
    /// <summary>The widest date line the box was sized for ("Turn 120  30 September 1956", Statecraft.uss .date-box).</summary>
    static readonly System.DateTime WorstDate = new System.DateTime(1956, 9, 30);
    const int WorstTurn = 120;

    static Rect ContentBox(VisualElement e)
    {
        var b = e.worldBound; var s = e.resolvedStyle;
        return Rect.MinMaxRect(b.xMin + s.borderLeftWidth + s.paddingLeft, b.yMin + s.borderTopWidth + s.paddingTop,
            b.xMax - s.borderRightWidth - s.paddingRight, b.yMax - s.borderBottomWidth - s.paddingBottom);
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>The label's text, measured on one line, fits its own box.</summary>
    static bool Whole(TextElement l) =>
        l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x <= l.contentRect.width + 1f;

    static bool Apart(Rect a, Rect b) => !Rect.MinMaxRect(a.xMin + 0.5f, a.yMin + 0.5f, a.xMax - 0.5f, a.yMax - 0.5f).Overlaps(b);

    [UnityTest]
    public IEnumerator AWorstCaseScoreStaysInsideTheDateBox()
    {
        var problems = new List<string>();
        foreach (var (language, text, hebrew) in new[] { ("en", En, false), ("he", He, true) })
            foreach (var (w, h) in InfoPanelLayoutTests.Windows)
            {
                var rt = new RenderTexture(w, h, 0);
                var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
                ps.targetTexture = rt;
                var go = new GameObject("ScoreLayoutTest");
                go.SetActive(false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
                go.SetActive(true);
                var root = doc.rootVisualElement;
                root.Q("root").RemoveFromClassList("loading");
                root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
                var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), text);
                bridge.State.Score = WorstScore;
                bridge.State.Date = WorstDate;
                bridge.State.Turn = WorstTurn;
                var hud = new HudPresenter(root, bridge, text);
                try
                {
                    var score = root.Q<Label>("score");
                    for (int i = 0; i < 20 && float.IsNaN(score.worldBound.width); i++) yield return null;
                    for (int i = 0; i < 3; i++) yield return null;   // the bar's tier classes settle after the first layout
                    var at = $"{language} {w}x{h}";
                    Assert.AreEqual(text.Format("hud.score", WorstScore), score.text, $"{at}: precondition: the worst score shows");
                    Assert.AreEqual(text.Format("hud.date", PlayerText.Date(WorstDate)), root.Q<Label>("date").text, $"{at}: precondition: the widest date shows");
                    var box = root.Q(className: "date-box");
                    if (!Inside(score.worldBound, ContentBox(box))) problems.Add($"{at}: '{score.text}' {score.worldBound} leaves the date box's frame {ContentBox(box)}");
                    if (!Whole(score)) problems.Add($"{at}: '{score.text}' is clipped in {score.contentRect}");
                    foreach (var other in new[] { "year", "turn", "date" })
                    {
                        var o = root.Q<Label>(other);
                        if (!Apart(score.worldBound, o.worldBound)) problems.Add($"{at}: '{score.text}' {score.worldBound} overlaps {other} '{o.text}' {o.worldBound}");
                        if (!Whole(o)) problems.Add($"{at}: {other} '{o.text}' is clipped in {o.contentRect}");
                        // The turn and date labels' own fit in the frame at this widest date is #625 (on main before this PR,
                        // worse in Hebrew); this test checks only that the score keeps clear of them.
                    }
                }
                finally
                {
                    hud.Dispose();
                    Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
                }
            }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
