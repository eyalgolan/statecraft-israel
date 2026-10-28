#nullable enable
using System.Collections;
using System.IO;
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

/// <summary>Writes the before and after stills for #1853's card scroll to `.superpowers/card-scroll-1853/` (local, not
/// committed): one long card (forex_crisis_1951, with the real portraits and photographs) on a 1920x1080 window at 150%
/// and at 125% and 100%, and at 150% in Hebrew. The "before" state moves the options and the Attention line back out of the body,
/// where they were before the change (Game.uxml), on the same card, state and window; "after" is the change as it ships,
/// and "after-scrolled" is the body scrolled to its last option. Explicit, so it does not run with the checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class EventCardScrollShots
{
    const string Event = "forex_crisis_1951";
    static readonly Content C = ContentLoader.Load(Paths.Content);

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void Shot(RenderTexture rt, string dir, string name, VisualElement root)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        var card = root.Q("event-card").worldBound; var hide = root.Q("event-hide").worldBound;
        var body = root.Q<ScrollView>("event-scroll");
        Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "SHOT {0}: card {1}, Hide {2}, body content {3:0} in view {4:0}, offset {5:0}", name, card, hide,
            body.contentContainer.layout.height, body.contentViewport.layout.height, body.scrollOffset.y));
    }

    IEnumerator Shots(int uiScale, bool hebrew)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "card-scroll-1853");
        Directory.CreateDirectory(dir);
        var tag = $"{(hebrew ? "he" : "en")}-1920x1080-{uiScale}";
        var rt = new RenderTexture(1920, 1080, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.black;   // else each still shows the frames before it
        if (uiScale != 100) ps.referenceResolution = new Vector2Int(Mathf.RoundToInt(1920f * 100f / uiScale), Mathf.RoundToInt(1080f * 100f / uiScale));
        var go = new GameObject("EventCardScrollShots");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var text = hebrew ? UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml")
            : UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == Event).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(Event);
        var bridge = new SimBridge(g, text);
        var hud = new HudPresenter(root, bridge, text);
        var card = new EventCardPresenter(root, bridge, text,
            AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset"),
            AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset"));
        try
        {
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            yield return Frames(8);
            Shot(rt, dir, tag + "-after", root);
            var body = root.Q<ScrollView>("event-scroll");
            var options = root.Q("event-options");
            // To the end of the body: ScrollTo(last option) left the Hebrew body unmoved in this capture (the Tab test proves
            // focus does scroll it), so set the offset to the scroller's range directly.
            body.scrollOffset = new Vector2(0f, body.verticalScroller.highValue);
            yield return Frames(4);
            Shot(rt, dir, tag + "-after-scrolled", root);
            body.scrollOffset = Vector2.zero;
            // Before: the Attention line and the options back after the body, as Game.uxml had them before #1853.
            var cardElement = root.Q("event-card");
            cardElement.Insert(cardElement.IndexOf(body) + 1, root.Q("event-delegate"));
            cardElement.Insert(cardElement.IndexOf(body) + 2, options);
            yield return Frames(8);
            Shot(rt, dir, tag + "-before", root);
        }
        finally
        {
            card.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    [UnityTest] public IEnumerator English150() { yield return Shots(150, false); }
    [UnityTest] public IEnumerator English125() { yield return Shots(125, false); }
    [UnityTest] public IEnumerator English100() { yield return Shots(100, false); }
    [UnityTest] public IEnumerator Hebrew150() { yield return Shots(150, true); }
}
