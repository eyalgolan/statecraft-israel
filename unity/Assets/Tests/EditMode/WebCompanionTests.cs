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
        using var mirror = new WebCompanion(root, Text, null, () => active, sent.Add);
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
        using var mirror = new WebCompanion(root, Text, null, () => true, _ => { });
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
            using var mirror = new WebCompanion(root, Text, null, () => true, sent.Add);
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
        var mirror = new WebCompanion(root, Text, null, () => true, sent.Add);
        Assert.IsEmpty(sent, "nothing is published before the first frame");
        mirror.Refresh(); var initial = sent.Count;
        Assert.AreEqual(1, initial);
        mirror.Refresh(); Assert.AreEqual(initial, sent.Count);
        root.Q<Label>("toast").text = "Result";
        mirror.Refresh(); Assert.AreEqual(initial + 1, sent.Count);
        mirror.Dispose(); Assert.AreEqual("null", sent[sent.Count - 1]);
        var disposed = sent.Count;
        mirror.Refresh(); mirror.Dispose(); Assert.AreEqual(disposed, sent.Count);
        using var next = new WebCompanion(root, Text, null, () => false, sent.Add);
        next.Refresh();
        Assert.AreEqual("", JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]).title);
    }

    [Test]
    public void APublicationThatThrewIsTriedAgain()
    {
        var root = Tree(); var sent = new List<string>(); var fail = true;
        using var mirror = new WebCompanion(root, Text, null, () => true, json => { if (fail) throw new System.InvalidOperationException("page"); sent.Add(json); });
        Assert.Throws<System.InvalidOperationException>(mirror.Refresh);
        fail = false;
        mirror.Refresh();
        Assert.AreEqual(1, sent.Count, "the unchanged snapshot goes out once the page takes it");
    }

    static int Handlers(Statecraft.Game.UI.SimBridge bridge, string name) =>
        (typeof(Statecraft.Game.UI.SimBridge).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bridge) as System.Delegate)?.GetInvocationList().Length ?? 0;

    /// <summary>Disposal itself unsubscribes and stops the schedule: the disposed flag alone would hide a leak of every old
    /// mirror (and the tree and GameRoot it holds) on each language switch, since the bridge outlives it.</summary>
    [Test]
    public void DisposeRemovesEveryBridgeAndRootHandlerAndStopsTheSchedule()
    {
        var bridge = NewBridge(Locale(false));
        var events = new[] { "Changed", "TurnEnded", "Refused" };
        var before = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(events, n => Handlers(bridge, n)));
        var root = new VisualElement();
        Assert.IsFalse(root.HasTrickleDownHandlers(), "precondition: a bare tree has no handlers");
        var mirror = new WebCompanion(root, Text, bridge, () => false, _ => { });
        foreach (var (name, i) in System.Linq.Enumerable.Select(events, (n, i) => (n, i)))
            Assert.AreEqual(before[i] + 1, Handlers(bridge, name), $"precondition: the mirror listens to {name}");
        Assert.IsTrue(root.HasTrickleDownHandlers(), "precondition: the mirror listens on the root");
        var poll = (IVisualElementScheduledItem)typeof(WebCompanion).GetField("poll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(mirror);
        Assert.IsTrue(poll.isActive, "precondition: the fallback poll runs");
        mirror.Dispose();
        CollectionAssert.AreEqual(before, System.Linq.Enumerable.Select(events, n => Handlers(bridge, n)), "every bridge handler is removed");
        Assert.IsFalse(root.HasTrickleDownHandlers(), "every root handler is removed");
        Assert.IsFalse(poll.isActive, "the fallback poll is stopped");
    }

    [Test]
    public void ATransportThatThrowsOnDisposalDoesNotStopTheTeardown()
    {
        var bridge = NewBridge(Locale(false));
        var before = Handlers(bridge, "Changed");
        var mirror = new WebCompanion(new VisualElement(), Text, bridge, () => false, json => { if (json == "null") throw new System.InvalidOperationException("page"); });
        LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("page"));
        Assert.DoesNotThrow(mirror.Dispose, "GameRoot disposes the presenters owned before the mirror in the same loop");
        Assert.AreEqual(before, Handlers(bridge, "Changed"));
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
        using var mirror = new WebCompanion(ui.Root, Locale(he), null, () => false, _ => { });
        Assert.AreEqual(expected, mirror.Capture(stop).focus);
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = stop; stop.SendEvent(e); }
        Assert.AreEqual(1, inputs, "the mirror's submit handler leaves the map stop's own submit alone");
        Assert.IsTrue(stop.focusable);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RealPresenterRejoinNoteFollowsItsChoiceAndClearsWithIt(bool he)
    {
        var text = Locale(he);
        var c = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);
        var e = System.Linq.Enumerable.First(c.Events, x => x.Id == "armistice_egypt");
        e.Options[0].Rejoin = "first_truce"; // presentation-only fixture: a real event id, because the card now resolves the rejoin target for its effect rows
        var game = Statecraft.Sim.GameSession.New(c, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical);
        game.State.Date = e.From;
        game.State.PendingEvents.Clear(); game.State.PendingEvents.Add(e.Id);
        var bridge = new Statecraft.Game.UI.SimBridge(game, text);
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, bridge, text, null);
        using var mirror = new WebCompanion(ui.Root, text, bridge, () => card.Shown != null, _ => { });
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
            game.Settings.Write(SettingsPresenter.CompanionKey, "on");   // opt-in (owner, 1 November 2026)
            var sent = new List<string>(); game.CompanionSend = sent.Add;
            game.Language = UiLanguage.English;
            game.Begin(inputs, 0);
            Assert.IsEmpty(sent, "nothing is published before the first frame");
            game.Companion!.Refresh();
            Assert.IsNotEmpty(sent, "Begin must create the mirror with the production hookup");
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
            game.Companion!.Refresh();
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

    // The owner's ruling of 1 November 2026, "Opt-in for now (Recommended)": off unless the Settings switch, a stored "on" or
    // "?companion=1" asks for it; a switch pressed in this session wins over both.
    [TestCase(null, null, null, false)]
    [TestCase(null, null, "", false)]
    [TestCase(null, null, "https://example.org/statecraft/index.html", false)]
    [TestCase(null, "off", "https://example.org/statecraft/index.html?lang=he", false)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?companion=1", true)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?lang=he&companion=1#x", true)]
    [TestCase(null, "off", "https://example.org/statecraft/index.html?companion=1", true)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?companion=0", false)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?companion", false)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?companion=10", false)]
    [TestCase(null, null, "https://example.org/statecraft/index.html?xcompanion=1", false)]
    [TestCase(null, null, "https://example.org/companion=1/index.html", false)]
    [TestCase(null, "on", "", true)]
    [TestCase(null, "yes", "", false)]
    [TestCase(false, "on", "https://example.org/statecraft/index.html?companion=1", false)]
    [TestCase(true, "off", "", true)]
    public void TheCompanionIsOptIn(bool? picked, string? stored, string? url, bool expected) =>
        Assert.AreEqual(expected, WebCompanion.On(picked, stored, url));

    [Test]
    public void TheSettingsRowShowsOnlyWhereAPageCanShowTheCompanionAndSaysWhatIsSet([Values(false, true)] bool hebrew)
    {
        var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text,
            hebrew ? "strings.he.yaml" : "strings.en.yaml");
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statecraft-companion-" + System.Guid.NewGuid().ToString("N"));
        try
        {
            using (var ui = new UiTestKit())
            using (new SettingsPresenter(ui.Root, text, new Statecraft.Game.SettingsStore(dir)))
            {
                Assert.AreEqual(text["settings.companion"], ui.Q<Label>("settings-companion-label").text);
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("settings-companion").parent.style.display.value, "no page: no row");
            }
            bool on = false; var asked = new List<bool>();
            using (var ui = new UiTestKit())
            using (new SettingsPresenter(ui.Root, text, new Statecraft.Game.SettingsStore(dir), companion: (() => on, v => { asked.Add(v); on = v; })))
            {
                var button = ui.Q<Button>("settings-companion");
                Assert.AreNotEqual(DisplayStyle.None, button.parent.style.display.value);
                Assert.AreEqual(text["settings.off"], button.text, "off by default");
                UiTestKit.Press(button);
                CollectionAssert.AreEqual(new[] { true }, asked, "turned on at once");
                Assert.AreEqual(text["settings.on"], button.text);
                Assert.AreEqual("on", new Statecraft.Game.SettingsStore(dir).Read(SettingsPresenter.CompanionKey), "and kept");
                UiTestKit.Press(button);
                CollectionAssert.AreEqual(new[] { true, false }, asked);
                Assert.AreEqual(text["settings.off"], button.text);
                Assert.AreEqual("off", new Statecraft.Game.SettingsStore(dir).Read(SettingsPresenter.CompanionKey));
            }
        }
        finally { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
    }

    [Test]
    public void TheRealRootStartsWithoutTheCompanionAndItsSettingTurnsItOnAndOffForThisAndLaterGames()
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
            Assert.IsNull(game.Companion, "off by default, with a page that could show it");
            var ui = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            UiTestKit.Press(ui.Q<Button>("nav-settings"));
            var button = ui.Q<Button>("settings-companion");
            Assert.AreNotEqual(DisplayStyle.None, button.parent.style.display.value, "the row shows where a page can show the companion");
            UiTestKit.Press(button);
            Assert.IsNotNull(game.Companion, "the switch turns it on at once");
            game.Companion!.Refresh();
            Assert.AreEqual("en", JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]).lang);
            game.Begin(inputs, 0);
            Assert.IsNotNull(game.Companion, "a later game keeps it on");
            UiTestKit.Press(ui.Q<Button>("nav-settings"));
            UiTestKit.Press(ui.Q<Button>("settings-companion"));
            Assert.IsNull(game.Companion, "the switch turns it off at once");
            Assert.AreEqual("null", sent[sent.Count - 1], "and clears the page");
            Assert.AreEqual("off", game.Settings.Read(SettingsPresenter.CompanionKey));
            var count = sent.Count;
            game.Begin(inputs, 0);
            Assert.IsNull(game.Companion, "a later game keeps it off");
            game.enabled = false;
            Assert.AreEqual(count, sent.Count, "nothing was published while it was off");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            Statecraft.Game.Map.VisualToggles.Clear();
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }
    }

    static readonly Statecraft.Sim.Content GameContent = Statecraft.Sim.ContentLoader.Load(Statecraft.Game.Paths.Content);

    static Statecraft.Game.UI.SimBridge NewBridge(UiText text) =>
        new Statecraft.Game.UI.SimBridge(Statecraft.Sim.GameSession.New(GameContent, Statecraft.Sim.GoldenRun.Seed, Statecraft.Sim.Mode.Historical), text);

    [Test]
    public void CoveredChoicesAreUnavailableAndComeBackWhenUncovered()
    {
        // The production cover rule: an open screen disables the decision layer, not each button's own state.
        var root = new VisualElement { name = "root" };
        var layer = new VisualElement { name = "event-layer" }; root.Add(layer);
        layer.Add(Tree());
        using var mirror = new WebCompanion(root, Text, null, () => true, _ => { });
        CollectionAssert.AreEqual(new[] { true, false }, System.Linq.Enumerable.Select(mirror.Capture().choices, c => c.available));
        root.AddToClassList(ScreenNav.ScreenOpenClass);
        ScreenNav.DisableCovered(root);
        Assert.IsFalse(layer.enabledSelf, "precondition: the open screen covers the decision layer");
        var covered = mirror.Capture();
        CollectionAssert.AreEqual(new[] { false, false }, System.Linq.Enumerable.Select(covered.choices, c => c.available));
        Assert.AreEqual("First", covered.choices[0].text, "a covered choice stays readable");
        root.RemoveFromClassList(ScreenNav.ScreenOpenClass);
        ScreenNav.DisableCovered(root);
        CollectionAssert.AreEqual(new[] { true, false }, System.Linq.Enumerable.Select(mirror.Capture().choices, c => c.available),
            "uncovered: each choice is back to its own state, the unpaid one still unavailable");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EndTurnReportIsMirroredWithoutAnEventAndClearsWhenClosed(bool he)
    {
        var text = Locale(he);
        var bridge = NewBridge(text);
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var toast = new ToastPresenter(ui.Root, bridge, text);
        using var report = new TurnReportPresenter(ui.Root, bridge, text, nav);
        using var mirror = new WebCompanion(ui.Root, text, bridge, () => false, _ => { });
        Assert.AreEqual("", mirror.Capture().reportTitle);
        Assert.IsEmpty(mirror.Capture().report);
        bridge.EndTurn();
        Assert.IsTrue(report.IsOpen, "precondition: End Turn opens the report");
        var state = mirror.Capture();
        Assert.AreEqual(bridge.State.Turn, state.turn);
        Assert.AreEqual(text["report.title"], state.reportTitle);
        StringAssert.Contains(text["report.title"], state.status, "the status line names the result");
        var expected = new List<string> { text["report.intro"] };
        foreach (var row in ui.Q<ScrollView>("turn-report-rows").Children())
            expected.Add(string.Join("\n", System.Linq.Enumerable.Where(System.Linq.Enumerable.Select(row.Query<Label>().ToList(), l => l.text), t => t.Length > 0)));
        Assert.Greater(expected.Count, 1, "precondition: the report has rows");
        CollectionAssert.AreEqual(expected, state.report, "the report's own words, in its order");
        var capture = System.Environment.GetEnvironmentVariable("WEB_COMPANION_CAPTURE_DIR");
        if (!string.IsNullOrEmpty(capture))
        {
            System.IO.Directory.CreateDirectory(capture);
            System.IO.File.WriteAllText(System.IO.Path.Combine(capture, he ? "report-he.json" : "report-en.json"), JsonUtility.ToJson(state));
        }
        report.Close();
        state = mirror.Capture();
        Assert.AreEqual("", state.reportTitle);
        Assert.IsEmpty(state.report);
        StringAssert.DoesNotContain(text["report.title"], state.status);
    }

    [UnityTest]
    public IEnumerator RefusalWithoutChangedAndTheSameResultOnTheNextTurnArePublished()
    {
        var ui = new UiTestKit();
        var settings = Object.Instantiate(ui.Doc.panelSettings);
        var texture = new RenderTexture(640, 480, 0);
        settings.targetTexture = texture;
        var text = Locale(false);
        var bridge = NewBridge(text);
        try
        {
            ui.Doc.panelSettings = settings; // a rendered runtime panel, so its scheduler runs
            ui.Root.Clear();
            var root = Tree(); ui.Root.Add(root);
            using var toast = new ToastPresenter(root, bridge, text);
            var sent = new List<string>();
            var changed = 0; bridge.Changed += () => changed++;
            var mirror = new WebCompanion(root, text, bridge, () => true, sent.Add);
            Assert.IsEmpty(sent, "nothing is published before the first frame");
            yield return Pump(() => sent.Count > 0);
            Assert.IsNotEmpty(sent, "the first frame publishes");
            WebCompanion.Snapshot Last() => JsonUtility.FromJson<WebCompanion.Snapshot>(sent[sent.Count - 1]);

            var reason = bridge.Queue(new Statecraft.Sim.SetPolicy("curfew", 1));
            Assert.IsNotNull(reason, "precondition: the order is refused");
            Assert.AreEqual(0, changed, "precondition: a refusal raises no Changed");
            var refused = text.Format("toast.refused", reason!);
            yield return Pump(() => Last().status == refused);
            Assert.AreEqual(refused, Last().status, "a refusal without Changed reaches the page");
            var firstTurn = Last().turn;

            bridge.EndTurn();
            Assert.AreEqual(reason, bridge.Queue(new Statecraft.Sim.SetPolicy("curfew", 1)), "precondition: the same refusal");
            yield return Pump(() => Last().turn != firstTurn && Last().status == refused);
            Assert.AreEqual(refused, Last().status);
            Assert.AreEqual(bridge.State.Turn, Last().turn, "the same result on a new turn is a new publication");
            Assert.AreNotEqual(firstTurn, Last().turn);

            bridge.Queue(new Statecraft.Sim.SetPolicy("curfew", 2));   // schedules a publication...
            mirror.Dispose();                                           // ...that disposal cancels
            var count = sent.Count;
            Assert.AreEqual("null", sent[count - 1]);
            yield return Pump(() => false, 0.3);
            Assert.AreEqual(count, sent.Count, "a pending publication never runs after disposal");
            bridge.Queue(new Statecraft.Sim.SetPolicy("curfew", 3));
            yield return Pump(() => false, 0.3);
            Assert.AreEqual(count, sent.Count, "disposal unsubscribes from the bridge");
        }
        finally
        {
            ui.Dispose(); Object.DestroyImmediate(settings); texture.Release(); Object.DestroyImmediate(texture);
        }
    }

    /// <summary>Frames until the condition holds or the time runs out (EditMode yields alone do not pump a UIDocument's
    /// runtime scheduler).</summary>
    static IEnumerator Pump(System.Func<bool> until, double seconds = 2)
    {
        var end = EditorApplication.timeSinceStartup + seconds;
        while (!until() && EditorApplication.timeSinceStartup < end)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
    }
}
