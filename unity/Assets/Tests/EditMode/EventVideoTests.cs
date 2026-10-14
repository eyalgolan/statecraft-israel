#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>Decision videos (owner rulings 2026-10-13; docs/event-videos.md): the clip mapping is sound and is not game
/// content; video is off in every automated run; the Illustration switch shows only for an event with a clip and a shown
/// photograph, swaps the credit for the "AI-generated illustration" label and back; a clip that fails puts the photograph
/// back and drops the switch for that event; Tab reaches the switch after the options and Hide; a sensitive clip waits for
/// the owner's review before a shareable build.</summary>
public class EventVideoTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static EventImageCatalog Photos => AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
    static IReadOnlyList<EventVideoMapping.Row> Rows => EventVideoMapping.Parse(File.ReadAllText(EventVideoMapping.Path));

    [Test]
    public void TheMappingIsSoundAndEveryClipShips()
    {
        var rows = Rows;
        Assert.GreaterOrEqual(rows.Count, 10, "about 12 events have a clip");
        var problems = EventVideoMapping.Problems(rows, C, id => Photos.Find(id) != null, f => File.Exists(Path.Combine(EventVideoMapping.ClipFolder, f)));
        Assert.IsEmpty(problems, string.Join("\n", problems));
        Assert.IsNotEmpty(EventVideoMapping.Load(), "the game finds the mapping in Resources");
        // Every file in the folder has a row (no stray clip ships).
        var files = Directory.GetFiles(EventVideoMapping.ClipFolder, "*.mp4").Select(Path.GetFileName).OrderBy(x => x).ToArray();
        var outcomes = EventVideoMapping.ParseOutcomes(File.ReadAllText(EventVideoMapping.Path));   // the End Turn clips (OutcomeClipTests)
        CollectionAssert.AreEqual(rows.Concat(outcomes).Select(r => r.File).OrderBy(x => x).ToArray(), files);
    }

    [Test]
    public void TheMappingIsNotGameContent()
    {
        var content = Path.GetFullPath(Paths.Content);
        Assert.IsFalse(Path.GetFullPath(EventVideoMapping.Path).StartsWith(content, StringComparison.Ordinal));
        foreach (var f in Directory.GetFiles(content, "*.yaml", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(f);
            StringAssert.DoesNotContain("event-videos", text, f);
            StringAssert.DoesNotContain("StreamingAssets/video", text, f);
        }
    }

    [Test]
    public void TheMappingRefusesDeportAClipWithoutAPhotographAndAnUnlabelledRow()
    {
        var c = ContentLoader.Load(Paths.Content);   // a copy to change
        var ev = c.Events.First(e => e.Id == "law_of_return");
        ev.Options.Add(new EventOption { Id = "deport_residents", Text = "x" });
        var row = Rows.First(r => r.Event == "law_of_return");
        var problems = EventVideoMapping.Problems(new[] { row }, c, _ => true, _ => true);
        Assert.IsTrue(problems.Any(p => p.Contains("Deport never gets any video")), string.Join("\n", problems));
        var unlabelled = new EventVideoMapping.Row { Event = "hula_drainage", File = "hula_drainage.mp4", Alt = "a marsh", AiGenerated = false };
        problems = EventVideoMapping.Problems(new[] { unlabelled }, C, _ => false, _ => true);
        Assert.IsTrue(problems.Any(p => p.Contains("no archival photograph")), "no photograph");
        Assert.IsTrue(problems.Any(p => p.Contains("ai_generated must be true")), "not marked AI");
        Assert.IsTrue(problems.Any(p => p.Contains("alt text")), "no proper alt text");
    }

    [Test]
    public void VideoIsOffInEveryAutomatedRunAndOnByHand()
    {
        foreach (var q in new[] { "?smoke=5", "?golden", "?golden&smoke=5", "?shot", "?vprofile", "?profile", "?units", "?borderproof" })
            Assert.IsTrue(CardVideo.ForcedOff("http://x/index.html" + q, new string[0], webgl: true), q);
        foreach (var a in new[] { "-golden", "-shot", "-smoke", "-units" })
            Assert.IsTrue(CardVideo.ForcedOff(null, new[] { "Statecraft", a }, webgl: false), a);
        Assert.IsFalse(CardVideo.ForcedOff("http://x/index.html", new string[0], webgl: true), "a player");
        Assert.IsFalse(CardVideo.ForcedOff("http://x/index.html?nostart", new string[0], webgl: true), "nostart is by hand");
        Assert.IsFalse(CardVideo.ForcedOff("http://x/index.html?shots", new string[0], webgl: true), "a whole parameter only");
    }

    sealed class Rig : IDisposable
    {
        public readonly VisualElement Root; public readonly EventCardPresenter Card; public readonly CardVideo? Video;
        readonly GameObject go; readonly PanelSettings ps; readonly RenderTexture rt;
        public Rig(string eventId, Func<string, EventVideoMapping.Row?>? clipFor, bool video = true)
        {
            rt = new RenderTexture(1920, 1080, 0);
            ps = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
            go = new GameObject("VideoCardTest"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
            go.SetActive(true);
            Root = doc.rootVisualElement; Root.Q("root").RemoveFromClassList("loading");
            var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            g.State.Date = C.Events.First(e => e.Id == eventId).From;
            g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
            Video = video ? new CardVideo() : null;
            Card = new EventCardPresenter(Root, new SimBridge(g, Text), Text, null, Photos, Video, clipFor);
        }
        public Button Switch => Root.Q<Button>("event-illustration");
        public Label Credit => Root.Q<Label>("event-photo-credit");
        public bool Shows(string name) => Root.Q(name).resolvedStyle.display == DisplayStyle.Flex;
        public void Dispose()
        {
            Card.Dispose(); Video?.Dispose();
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(ps); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    static Func<string, EventVideoMapping.Row?> From(IReadOnlyList<EventVideoMapping.Row> rows) => id => rows.FirstOrDefault(r => r.Event == id);

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void Press(Button b) { using (var e = NavigationSubmitEvent.GetPooled()) { e.target = b; b.SendEvent(e); } }

    [UnityTest]
    public IEnumerator TheSwitchShowsOnlyForAnEventWithAClipAndSwapsTheLabel()
    {
        LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log while it prepares; the card is what is checked
        using (var r = new Rig("idf_founding", From(Rows)))   // a photograph, no clip
        {
            yield return Frames(4);
            Assert.IsFalse(r.Shows("event-illustration"), "no clip: no switch");
        }
        using (var r = new Rig("law_of_return", null))   // video off (CardVideo.ForcedOff): GameRoot passes no clips
        {
            yield return Frames(4);
            Assert.IsFalse(r.Shows("event-illustration"), "video off: no switch");
        }
        var row = Rows.First(x => x.Event == "law_of_return");
        using (var r = new Rig("law_of_return", From(Rows)))
        {
            yield return Frames(4);
            var credit = r.Credit.text;
            Assert.IsTrue(r.Shows("event-illustration"), "a clip and a photograph: the switch shows");
            Assert.AreEqual("Illustration", r.Switch.text);
            Press(r.Switch);
            yield return Frames(2);
            Assert.AreEqual("Photograph", r.Switch.text);
            Assert.AreEqual("AI-generated illustration", r.Credit.text, "the AI label replaces the credit");
            Assert.AreEqual(row.Alt, r.Root.Q("event-photo").tooltip, "the clip's alt text");
            Assert.IsTrue(r.Card.IllustrationShown);
            Press(r.Switch);
            yield return Frames(2);
            Assert.AreEqual("Illustration", r.Switch.text);
            Assert.AreEqual(credit, r.Credit.text, "the photograph's credit is back");
            Assert.IsFalse(r.Card.IllustrationShown);
        }
    }

    /// <summary>The fallback, driven through CardVideo's own failure path (what errorReceived and the 8 s timeout call;
    /// the edit-mode VideoPlayer's own timing is not what is under test: the WebGL check renames a clip away and sees
    /// "VIDEO failed" and the photograph stay).</summary>
    [UnityTest]
    public IEnumerator AClipThatFailsPutsThePhotographBackAndDropsTheSwitch()
    {
        LogAssert.ignoreFailingMessages = true;   // the edit-mode VideoPlayer may log while it prepares
        using var r = new Rig("law_of_return", From(Rows));
        yield return Frames(4);
        var credit = r.Credit.text;
        Press(r.Switch);
        yield return Frames(1);
        Assert.IsTrue(r.Card.IllustrationShown, "precondition: the clip was asked for");
        typeof(CardVideo).GetMethod("Fail", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(r.Video, new object[] { "simulated error" });
        yield return Frames(2);
        Assert.IsFalse(r.Card.IllustrationShown, "the failed clip is stopped");
        Assert.IsFalse(r.Shows("event-illustration"), "and the switch is gone for this event");
        Assert.AreEqual(credit, r.Credit.text, "the photograph and its credit are back");
        Assert.AreEqual("Illustration", r.Switch.text);
    }

    [UnityTest]
    public IEnumerator TabReachesTheSwitchAfterTheOptionsAndHide()
    {
        using var r = new Rig("law_of_return", From(Rows));
        yield return Frames(4);
        var card = r.Root.Q("event-card");
        var focusables = card.Query<Button>().ToList().Where(b => b.resolvedStyle.display == DisplayStyle.Flex && b.focusable).ToList();
        var order = focusables.Select(b => b.name).ToList();
        Assert.AreEqual("event-illustration", order.Last(), string.Join(", ", order));
        Assert.Less(order.IndexOf("event-hide"), order.IndexOf("event-illustration"));
        Assert.Less(order.IndexOf(r.Root.Q("event-options").Q<Button>().name), order.IndexOf("event-hide"));
        Assert.IsTrue(focusables.All(b => b.tabIndex == 0), "tree order is Tab order");
        // The switch sits over the header's top right corner.
        var sw = r.Switch.worldBound; var band = r.Root.Q("event-photo").worldBound;
        Assert.IsTrue(band.Contains(new Vector2(sw.xMin, sw.yMin)) && band.Contains(new Vector2(sw.xMax, sw.yMax)), $"the switch {sw} lies on the header {band}");
        Assert.Less(band.xMax - sw.xMax, 20f, "at its right edge");
        Assert.Less(sw.yMin - band.yMin, 20f, "at its top");
    }

    [Test]
    public void ASensitiveClipWaitsForTheOwnersReviewBeforeAShareableBuild()
    {
        var gate = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor")!.GetMethod("GateRefusal", BindingFlags.Public | BindingFlags.Static)!;
        var shareable = new[] { "Unity", "-shareable" };
        var refusal = (string?)gate.Invoke(null, new object?[] { shareable, C, null, false, null, Rows });
        StringAssert.Contains("illustration clips: operation_yoav, ten_days", refusal ?? "");
        Assert.IsNull(gate.Invoke(null, new object?[] { new[] { "Unity" }, C, null, false, null, Rows }), "a dev build is not gated");
        var reviewed = Rows.Select(x => { x.SensitivityReviewed = x.Sensitive; return x; }).ToList();
        Assert.IsNull(gate.Invoke(null, new object?[] { shareable, C, null, false, null, reviewed }), "reviewed: the clips may ship");
        CollectionAssert.AreEqual(new[] { "operation_yoav", "ten_days" }, EventVideoMapping.SensitiveUnreviewed(Rows));
    }
}
