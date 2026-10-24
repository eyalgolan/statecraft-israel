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

/// <summary>Spec 7.1 and 16 at the seven windows: the popup sits in its slot, inside the window, clear of the minimap, the
/// left column and the top bar; its text keeps the 12 px floor; in Hebrew it is in the same place with its inside mirrored.</summary>
public class AdvisorPopupLayoutTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly (int w, int h)[] Windows = { (1920, 1080), (1280, 720), (1366, 768), (1024, 768), (960, 1080), (390, 844), (844, 390) };

    [UnityTest]
    public IEnumerator ThePopupSitsInItsSlotAtEveryWindow()
    {
        var problems = new List<string>();
        foreach (var hebrew in new[] { false, true })
            foreach (var (w, h) in Windows)
                yield return Shown(w, h, hebrew, (root, advisor) =>
                {
                    string at = $"{(hebrew ? "he" : "en")} {w}x{h}";
                    var popup = root.Q("advisor-popup").worldBound;
                    var window = root.worldBound;
                    var slot = advisor.Slot();
                    if (!(popup.width > 0 && popup.height > 0)) { problems.Add($"{at}: not shown"); return; }
                    if (!Inside(popup, window)) problems.Add($"{at}: {popup} leaves the window {window}");
                    if (!Inside(popup, slot)) problems.Add($"{at}: {popup} leaves its slot {slot}");
                    foreach (var other in new[] { "minimap", "left-column", "top-bar" })
                        if (popup.Overlaps(root.Q(other).worldBound)) problems.Add($"{at}: over {other} {root.Q(other).worldBound}");
                    float scale = root.panel.scaledPixelsPerPoint;
                    foreach (var name in new[] { "advisor-name", "advisor-role", "advisor-text", "advisor-got-it" })
                        if (root.Q(name).resolvedStyle.fontSize * scale < HudPresenter.TextFloorPx - 0.01f)
                            problems.Add($"{at}: {name} is under the {HudPresenter.TextFloorPx} px floor");
                    var portrait = root.Q("advisor-portrait").worldBound; var names = root.Q("advisor-name").worldBound;
                    if (portrait.width > 0 && (hebrew ? portrait.xMin < names.xMax : portrait.xMax > names.xMin))
                        problems.Add($"{at}: the portrait is not on the {(hebrew ? "right" : "left")} of the name");
                });
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    /// <summary>The review of Task 6: on its first show the popup must not draw at the layer's top-left before it is placed,
    /// and it must place itself once the layer is laid out (no Place() by hand).</summary>
    [UnityTest]
    public IEnumerator ThePopupNeverShowsBeforeItIsPlaced()
    {
        var problems = new List<string>();
        yield return Shown(1920, 1080, false, (root, advisor) =>
        {
            var popup = root.Q("advisor-popup");
            bool placed = popup.style.left.keyword == StyleKeyword.Undefined;      // a value set by Place
            bool invisible = popup.style.visibility.keyword == StyleKeyword.Undefined && popup.style.visibility.value == Visibility.Hidden;
            if (!placed && !invisible) problems.Add($"drawn before it is placed: visibility {popup.style.visibility}, left {popup.style.left}");
        }, placeByHand: false, justShown: true);
        yield return Shown(1920, 1080, false, (root, advisor) =>
        {
            var popup = root.Q("advisor-popup");
            if (popup.resolvedStyle.visibility != Visibility.Visible) problems.Add($"not visible once laid out: {popup.resolvedStyle.visibility}");
            if (!Inside(popup.worldBound, advisor.Slot())) problems.Add($"{popup.worldBound} leaves its slot {advisor.Slot()}");
            if (popup.worldBound.xMin < 1f && popup.worldBound.yMin < 1f) problems.Add($"at the top-left: {popup.worldBound}");
        }, placeByHand: false);
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    static bool Inside(Rect inner, Rect outer) =>
        inner.xMin >= outer.xMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.yMax <= outer.yMax + 0.5f;

    /// <summary>Game.uxml at w x h with the HUD, the card collapsed, and a two-sentence fixture message shown. With
    /// <paramref name="justShown"/> the check runs right after NewGame, before any frame; with <paramref name="placeByHand"/>
    /// false the popup is left to place itself.</summary>
    internal static IEnumerator Shown(int w, int h, bool hebrew, System.Action<VisualElement, AdvisorPresenter> check,
        bool placeByHand = true, bool justShown = false)
    {
        var text = hebrew
            ? UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text + "\nadvisor.t.teach.text: \"משפט ראשון ארוך למדי כדי לבדוק גלישה. ומשפט שני.\"\n", "strings.he.yaml")
            : UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text + "\nadvisor.t.teach.text: \"A first sentence long enough to wrap. And a second one.\"\n");
        var rt = new RenderTexture(w, h, 0);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt;
        var go = new GameObject("AdvisorLayoutTest"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
        var hud = new HudPresenter(root, bridge, text);
        var nav = new ScreenNav(root);
        var card = new EventCardPresenter(root, bridge, text, null);
        card.Collapse(takeFocus: false);
        var catalog = AdvisorCatalog.Parse(AdvisorPopupTests.Fixture.Replace("level: new,", "level: new, he_review: approved,"));
        var advisor = new AdvisorPresenter(root, bridge, text, new AdvisorSession(catalog, new AdvisorSettings(null), autoShow: true),
            hooks: new AdvisorHooks { Card = card });
        try
        {
            advisor.NewGame();
            if (justShown) { check(root, advisor); yield break; }
            for (int i = 0; i < 5; i++) yield return null;
            if (placeByHand) advisor.Place();
            for (int i = 0; i < 2; i++) yield return null;
            check(root, advisor);
        }
        finally
        {
            advisor.Dispose(); card.Dispose(); nav.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
