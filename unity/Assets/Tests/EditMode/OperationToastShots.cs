#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Writes the stills for #2621's toast over Operation Hiram's card to `.superpowers/toast-2621/` (local, not committed):
/// the card at 2560x1080 on the historical run's Hiram turn (15 November 1948), with the real photographs, its requirement
/// lines (the units in place) and the longest
/// toast DecisionCardTests uses, in English and Hebrew. The environment variable STATECRAFT_SHOT_TAG names the set ("before"
/// or "after"); each still's log line gives the toast's and the credit's rectangles. Explicit, so it does not run with the
/// checks.</summary>
[Explicit("writes stills for a pull request, not a check")]
public class OperationToastShots
{
    const string Event = "operation_hiram";
    const int Width = 2560, Height = 1080;
    const string LongToast = "Not done: Not enough attention (need 4.0, have 2.0)\nNot done: No more than 2 map actions in one turn\n"
        + "Not done: The Knesset must approve it: the coalition holds 46 of 120 seats\nYour game was saved, but the browser refused the autosave";
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
        Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "SHOT {0}: toast {1}, credit {2}, title {3}, options {4}",
            name, root.Q("toast").worldBound, root.Q("event-photo-credit").worldBound, root.Q("event-title").worldBound, root.Q("event-options").worldBound));
    }

    IEnumerator Shots(bool hebrew)
    {
        var set = System.Environment.GetEnvironmentVariable("STATECRAFT_SHOT_TAG") ?? "after";
        var dir = Path.Combine(Directory.GetCurrentDirectory(), ".superpowers", "toast-2621");
        Directory.CreateDirectory(dir);
        var tag = $"{set}-{(hebrew ? "he" : "en")}-{Width}x{Height}";
        var rt = new RenderTexture(Width, Height, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel));
        ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = Color.black;
        var go = new GameObject("OperationToastShots");
        go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = ps;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        var text = hebrew ? UiText.Parse(UiPaths.HebrewText(), "strings.he.yaml")
            : UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
        var root = doc.rootVisualElement;
        root.Q("root").RemoveFromClassList("loading");
        root.Q("root").EnableInClassList(UiLanguage.HebrewClass, hebrew);
        // The historical run to Hiram's turn, so every unit stands where the game puts it (the 7th Brigade arrived, and the
        // automated player has staged Carmeli and the 7th Brigade in Galilee); Hiram is the only card.
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        while (!g.State.PendingEvents.Contains(Event)) g.Step(HistoricalAI.Commands(C, g.State));
        g.State.PendingEvents.RemoveAll(id => id != Event);
        var bridge = new SimBridge(g, text);
        var hud = new HudPresenter(root, bridge, text);
        var card = new EventCardPresenter(root, bridge, text,
            AssetDatabase.LoadAssetAtPath<PortraitCatalog>("Assets/UI/Portraits/PortraitCatalog.asset"),
            AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset"))
        { Units = UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName))) };
        try
        {
            card.Render();
            for (int i = 0; i < 20 && float.IsNaN(root.Q("minimap").worldBound.width); i++) yield return null;
            yield return Frames(8);
            Shot(rt, dir, tag + "-card", root);
            var toast = root.Q<Label>("toast");
            toast.text = LongToast;
            toast.style.display = DisplayStyle.Flex;
            yield return Frames(6);
            Shot(rt, dir, tag + "-toast", root);
        }
        finally
        {
            card.Dispose(); hud.Dispose();
            Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    [UnityTest] public IEnumerator English() { yield return Shots(false); }
    [UnityTest] public IEnumerator Hebrew() { yield return Shots(true); }
}
