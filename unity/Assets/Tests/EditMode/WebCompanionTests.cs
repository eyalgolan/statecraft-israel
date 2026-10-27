#nullable enable
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class WebCompanionTests
{
    static UiText Text => UiText.Parse("ui.lang: en\ncompanion.title: Game companion\ncompanion.help: Read only\ncompanion.focus: Game focus\ncompanion.status: Status\ncompanion.choices: Choices\ncompanion.empty: No decision waiting\ncompanion.unavailable: Unavailable\n");

    static VisualElement Tree()
    {
        var root = new VisualElement();
        var card = new VisualElement { name = "event-card" }; root.Add(card);
        card.Add(new Label("A <title> & a name") { name = "event-title" });
        card.Add(new Label("Body שלום") { name = "event-text" });
        var options = new VisualElement { name = "event-options" }; card.Add(options);
        options.Add(new Button { text = "First", name = "option-internal-id" });
        var disabled = new Button { text = "Second" }; disabled.SetEnabled(false); options.Add(disabled);
        root.Add(new Label("Not done: reason") { name = "toast" });
        return root;
    }

    [Test]
    public void MirrorsRenderedWordsAndChoiceOrderWithoutInvokingControls()
    {
        var root = Tree(); bool active = true; int inputs = 0;
        root.Q<Button>().clicked += () => inputs++;
        var sent = new List<string>();
        using var mirror = new WebCompanion(root, Text, () => active, sent.Add);
        var state = mirror.Capture(root.Q<Button>());
        Assert.AreEqual("A <title> & a name", state.title);
        CollectionAssert.Contains(state.body, "Body שלום");
        Assert.AreEqual("First", state.choices[0].text);
        Assert.IsTrue(state.choices[0].available);
        Assert.AreEqual("Second", state.choices[1].text);
        Assert.IsFalse(state.choices[1].available);
        Assert.AreEqual("First", state.focus);
        Assert.AreEqual("Not done: reason", state.status);
        Assert.AreEqual(0, inputs);
        Assert.IsFalse(JsonUtility.ToJson(state).Contains("option-internal-id"));
        active = false; // presenter retains old label text after its last event is answered
        state = mirror.Capture();
        Assert.AreEqual("", state.title);
        Assert.IsEmpty(state.body);
        Assert.IsEmpty(state.choices);
    }

    [Test]
    public void HiddenStatusAndFocusAreClearedAndUnnamedCardHasItsTitle()
    {
        var root = Tree();
        using var mirror = new WebCompanion(root, Text, () => true, _ => { });
        Assert.AreEqual("A <title> & a name", mirror.Capture(root.Q("event-card")).focus);
        root.style.display = DisplayStyle.None;
        var state = mirror.Capture(root.Q<Button>());
        Assert.AreEqual("", state.focus);
        Assert.AreEqual("", state.status);
    }


    [UnityTest]
    public IEnumerator ScheduledMirrorFollowsActualPanelFocusAndToastChanges()
    {
        var ui = new UiTestKit();
        var settings = Object.Instantiate(ui.Doc.panelSettings);
        var texture = new RenderTexture(640, 480, 0);
        settings.targetTexture = texture;
        try
        {
            ui.Doc.panelSettings = settings; // a rendered runtime panel, as KeyboardFocusTests uses
            ui.Root.Clear();
            var root = Tree(); ui.Root.Add(root);
            var sent = new List<string>();
            using var mirror = new WebCompanion(root, Text, () => true, sent.Add);
            yield return null;
            yield return null;
            var button = root.Q<Button>();
            button.Focus();
            for (int i = 0; i < 10 && root.focusController.focusedElement != button; i++) { yield return null; button.Focus(); }
            Assert.AreSame(button, root.focusController.focusedElement, "precondition: real runtime keyboard focus");
            Assert.AreEqual("First", mirror.Capture().focus, "precondition: capture sees actual keyboard focus");
            root.Q<Label>("toast").text = "New result";
            var until = EditorApplication.timeSinceStartup + 2;
            WebCompanion.Snapshot state;
            do
            {
                // EditMode yields alone do not pump a UIDocument's runtime timer scheduler.
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
                state = JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]);
            } while ((state.focus != "First" || state.status != "New result") && EditorApplication.timeSinceStartup < until);
            Assert.AreEqual("First", state.focus);
            Assert.AreEqual("New result", state.status);
            mirror.Dispose();
            var count = sent.Count;
            root.Q<Label>("toast").text = "After disposal";
            until = EditorApplication.timeSinceStartup + 0.3;
            while (EditorApplication.timeSinceStartup < until) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            Assert.AreEqual(count, sent.Count, "the disposed schedule sends no stale state");
        }
        finally
        {
            ui.Dispose(); Object.DestroyImmediate(settings); texture.Release(); Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void SendsOnlyChangedSnapshotsAndDisposalClearsAndStopsPublishing()
    {
        var root = Tree(); var sent = new List<string>();
        var mirror = new WebCompanion(root, Text, () => true, sent.Add);
        mirror.Refresh(); var initial = sent.Count;
        mirror.Refresh(); Assert.AreEqual(initial, sent.Count);
        root.Q<Label>("toast").text = "Result";
        mirror.Refresh(); Assert.AreEqual(initial + 1, sent.Count);
        mirror.Dispose(); Assert.AreEqual("null", sent[sent.Count - 1]);
        var disposed = sent.Count;
        mirror.Refresh(); mirror.Dispose(); Assert.AreEqual(disposed, sent.Count);
        using var next = new WebCompanion(root, Text, () => false, sent.Add);
        next.Refresh();
        Assert.AreEqual("", JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]).title);
    }

    static UiText Locale(bool he) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he ? UiPaths.StringsHebrew : UiPaths.Strings).text, he ? "strings.he.yaml" : "strings.en.yaml");

    [TestCase(false, "Map")]
    [TestCase(true, "מפה")]
    public void RealMapStopHasALocalizedNameAndKeepsItsInput(bool he, string expected)
    {
        using var ui = new UiTestKit();
        var stop = ui.Root.Q("map-focus");
        Assert.IsTrue(stop.focusable);
        int inputs = 0;
        stop.RegisterCallback<NavigationSubmitEvent>(_ => inputs++);
        using var mirror = new WebCompanion(ui.Root, Locale(he), () => false, _ => { });
        Assert.AreEqual(expected, mirror.Capture(stop).focus);
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = stop; stop.SendEvent(e); }
        Assert.AreEqual(1, inputs, "the mirror registers no input handlers");
        Assert.IsTrue(stop.focusable);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RealPresenterRejoinNoteFollowsItsChoiceAndClearsWithIt(bool he)
    {
        var text = Locale(he);
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var e = System.Linq.Enumerable.First(c.Events, x => x.Id == "armistice_egypt");
        e.Options[0].Rejoin = "fixture-record"; // presentation-only fixture: the real presenter renders its existing note
        var game = Statecraft.Sim.GameSession.New(c, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical);
        game.State.Date = e.From;
        game.State.PendingEvents.Clear(); game.State.PendingEvents.Add(e.Id);
        var bridge = new Statecraft.Game.UI.SimBridge(game, text);
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        using var mirror = new WebCompanion(ui.Root, text, () => card.Shown != null, _ => { });
        Assert.AreEqual(e.Id, card.Shown!.Id);
        var capture = System.Environment.GetEnvironmentVariable("WEB_COMPANION_CAPTURE_DIR");
        if (!string.IsNullOrEmpty(capture))
        {
            System.IO.Directory.CreateDirectory(capture);
            System.IO.File.WriteAllText(System.IO.Path.Combine(capture, he ? "event-he.json" : "event-en.json"), JsonUtility.ToJson(mirror.Capture()));
        }
        Assert.AreEqual(text["event.rejoin"], mirror.Capture().choices[0].note);
        Assert.AreEqual("", mirror.Capture().choices[1].note);
        card.Collapse();
        Assert.AreEqual(text["event.rejoin"], mirror.Capture().choices[0].note);
        card.Reopen();
        Assert.AreEqual(text["event.rejoin"], mirror.Capture().choices[0].note);
        game.State.PendingEvents.Clear(); game.State.PendingEvents.Add("first_truce");
        card.Render();
        Assert.AreEqual("first_truce", card.Shown!.Id);
        foreach (var choice in mirror.Capture().choices) Assert.AreEqual("", choice.note);
        game.State.PendingEvents.Clear(); card.Render();
        Assert.IsEmpty(mirror.Capture().choices);
    }

    [Test]
    public void ActualRootRebuildLanguageSwitchAndDisableOwnTheOutboundLifetime()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statecraft-companion-" + System.Guid.NewGuid().ToString("N"));
        var inputs = Statecraft.Game.Boot.GameInputs.FromDisk(Statecraft.Game.Paths.Content, Statecraft.Game.Paths.StreamingMap);
        try
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Map.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            Object.FindFirstObjectByType<Statecraft.Game.Map.MapScene>().Build(inputs.Map);
            var game = Object.FindFirstObjectByType<GameRoot>();
            game.Store = new Statecraft.Game.SaveStore(dir);
            game.Settings = new Statecraft.Game.SettingsStore(dir);
            var sent = new List<string>(); game.CompanionSend = sent.Add;
            game.Language = UiLanguage.English;
            game.Begin(inputs, 0);
            Assert.IsNotEmpty(sent, "Begin must invoke the production hookup");
            var old = game.Companion!;
            Assert.AreEqual("en", JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]).lang);
            var session = game.Bridge!.Session;
            var state = game.Bridge.State;
            game.Card!.Collapse();
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            UiTestKit.Press(ui.Q<Button>("nav-settings"));
            UiTestKit.Press(ui.Q<Button>("settings-language-he"));
            Assert.AreEqual("he", game.Language);
            Assert.AreSame(session, game.Bridge!.Session);
            Assert.AreSame(state, game.Bridge.State);
            Assert.Contains("null", sent, "the old mirror is cleared before publishing its replacement");
            Assert.AreEqual("he", JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]).lang);
            var count = sent.Count; old.Refresh(); Assert.AreEqual(count, sent.Count);
            var current = game.Companion!;
            game.Begin(inputs, 0);
            Assert.AreNotSame(current, game.Companion);
            count = sent.Count; current.Refresh(); Assert.AreEqual(count, sent.Count);
            game.enabled = false; Assert.AreEqual("null", sent[sent.Count - 1]);
            Assert.IsNull(game.Companion);
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            Statecraft.Game.Map.VisualToggles.Clear();
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }
}
