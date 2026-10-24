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

/// <summary>#1119 B reads the presented tree after its handlers, without adding game commands or moving focus.</summary>
public class AccessibilityCompanionTests
{
    static readonly Content Content = ContentLoader.Load(Paths.Content);
    static UiText Text(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text);

    sealed class Kit : System.IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly SimBridge Bridge;
        public readonly ToastPresenter Toast;
        public readonly EventCardPresenter Card;
        public readonly AccessibilityCompanion Companion;
        public readonly List<string> Sent = new List<string>();
        readonly List<System.IDisposable> owned = new List<System.IDisposable>();
        public AccessibilityCompanion.Snapshot Last => JsonUtility.FromJson<AccessibilityCompanion.Snapshot>(Sent.Last());
        public Kit(bool he)
        {
            var text = Text(he);
            Ui.Q<VisualElement>("root").RemoveFromClassList("loading");
            Bridge = new SimBridge(GameSession.New(Content, GoldenRun.Seed, Mode.Historical), text);
            owned.Add(Toast = new ToastPresenter(Ui.Root, Bridge, text));
            owned.Add(new HudPresenter(Ui.Root, Bridge, text));
            var nav = new ScreenNav(Ui.Root); owned.Add(nav);
            owned.Add(Card = new EventCardPresenter(Ui.Root, Bridge, text, null));
            owned.Add(new WhyPresenter(Ui.Root, Bridge, text));
            owned.Add(new TurnReportPresenter(Ui.Root, Bridge, text, nav));
            owned.Add(Companion = new AccessibilityCompanion(Ui.Root, Bridge, text, Sent.Add));
        }
        public void Dispose() { for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); Ui.Dispose(); }
    }
    // Count UI scheduler ticks: an EditMode coroutine yield is not necessarily a runtime-panel update.
    static IEnumerator Frames(VisualElement root)
    {
        for (int i = 0; i < 4; i++)
        {
            bool ran = false;
            var probe = root.schedule.Execute(() => ran = true);
            var deadline = UnityEditor.EditorApplication.timeSinceStartup + 3;
            while (!ran && UnityEditor.EditorApplication.timeSinceStartup < deadline)
            { UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            probe.Pause();
            Assert.IsTrue(ran, "the runtime panel must advance a UI tick");
        }
    }
    static void Submit(VisualElement root) { using var e = NavigationSubmitEvent.GetPooled(); e.target = root; root.SendEvent(e); }

    [UnityTest]
    public IEnumerator TheEnglishAndHebrewPayloadReadsTheCardChoicesFocusAndLiteralText()
    {
        foreach (var he in new[] { false, true })
        {
            using var k = new Kit(he);
            yield return Frames(k.Ui.Root);
            var p = k.Last;
            Assert.AreEqual(he ? "he" : "en", p.language);
            Assert.AreEqual(k.Ui.Q<Label>("event-title").text, p.title);
            Assert.IsNotEmpty(p.title);
            Assert.AreEqual(k.Ui.Q<Label>("event-text").text, p.body);
            var options = k.Ui.Q<VisualElement>("event-options").Query<Button>().ToList();
            CollectionAssert.AreEqual(options.Select(b => b.text), p.choices.Select(c => c.text));
            CollectionAssert.AreEqual(options.Select(b => b.enabledInHierarchy), p.choices.Select(c => c.enabled));
            options[0].Focus();
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(options[0].text, k.Last.focus);
            Assert.AreSame(options[0], k.Ui.Root.panel.focusController.focusedElement);
            k.Ui.Q<Label>("event-title").text = "<img src=x onerror=alert(1)>";
            k.Ui.Q<Label>("event-office").text = "תפקיד \u200eIL 1.2m\u200e";
            options[0].SetEnabled(false);
            k.Ui.Q<VisualElement>("event-options").Add(new Label("preview +1"));
            Submit(k.Ui.Root);
            yield return Frames(k.Ui.Root);
            Assert.AreEqual("<img src=x onerror=alert(1)>", k.Last.title);
            CollectionAssert.Contains(k.Last.context, "תפקיד \u200eIL 1.2m\u200e");
            Assert.IsFalse(k.Last.choices[0].enabled);
            CollectionAssert.Contains(k.Last.choices.Last().notes, "preview +1");
            int before = k.Sent.Count;
            for (int i = 0; i < 100; i++) k.Ui.Q<Label>("event-title").text = "latest " + i;
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(before + 1, k.Sent.Count, "a burst coalesces to one final snapshot");
            Assert.AreEqual("latest 99", k.Last.title);
        }
    }

    [UnityTest]
    public IEnumerator RefusalWithoutChangedAndAppendedNoticesAreReadAfterThePresenters()
    {
        foreach (var he in new[] { false, true })
        {
            using var k = new Kit(he);
            yield return Frames(k.Ui.Root);
            int changed = 0; k.Bridge.Changed += () => changed++;
            k.Bridge.Refused += _ => k.Toast.Append("save notice");
            var refusal = k.Bridge.Queue(new ChooseEventOption("missing-event", "missing-option"));
            Assert.IsNotNull(refusal);
            Assert.AreEqual(0, changed);
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(k.Ui.Q<Label>("toast").text, k.Last.status);
            StringAssert.Contains(refusal, k.Last.status);
            StringAssert.EndsWith("save notice", k.Last.status);
            var late = k.Ui.Q<Label>("toast").text.Replace("save notice", "late notice");
            k.Toast.Show(late); // same line count, no bridge event/click/focus change
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(late, k.Last.status);
        }
    }

    [UnityTest]
    public IEnumerator AShownCardIsReadAfterStylesResolveWithoutAnotherInput()
    {
        using var k = new Kit(false);
        yield return Frames(k.Ui.Root);
        var card = k.Ui.Q<VisualElement>("event-card");
        card.style.display = DisplayStyle.None;
        Submit(k.Ui.Root);
        yield return Frames(k.Ui.Root);
        Assert.IsEmpty(k.Last.title);
        card.style.display = DisplayStyle.Flex;
        Submit(k.Ui.Root); // like closing the start screen: schedule before the new styles have resolved
        yield return Frames(k.Ui.Root);
        Assert.AreEqual(k.Ui.Q<Label>("event-title").text, k.Last.title);
        Assert.IsNotEmpty(k.Last.choices);
    }

    [UnityTest]
    public IEnumerator TheFinalTurnReportIsReadAndDisposeClearsAndCancelsPendingWork()
    {
        foreach (var he in new[] { false, true })
        {
            using var k = new Kit(he);
            yield return Frames(k.Ui.Root);
            k.Bridge.EndTurn();
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(k.Bridge.State.Turn, k.Last.turn);
            StringAssert.Contains(k.Ui.Q<Label>("turn-report-title").text, k.Last.status);
            foreach (var label in k.Ui.Q<VisualElement>("turn-report-rows").Query<Label>().ToList())
                StringAssert.Contains(label.text, k.Last.status);
            Assert.IsEmpty(k.Last.title, "a covered event is not in the reading panel");
            Assert.IsEmpty(k.Last.choices);
            Submit(k.Ui.Root); // schedule a publication, then dispose before it runs
            k.Companion.Dispose();
            Assert.IsEmpty(k.Last.title); Assert.IsEmpty(k.Last.body);
            Assert.IsEmpty(k.Last.status); Assert.IsEmpty(k.Last.focus);
            Assert.IsEmpty(k.Last.choices);
            int sent = k.Sent.Count;
            k.Companion.Dispose();
            k.Bridge.EndTurn();
            Submit(k.Ui.Root);
            yield return Frames(k.Ui.Root);
            Assert.AreEqual(sent, k.Sent.Count, "no late publication, or duplicate clearing on a second Dispose");
        }
    }
}
