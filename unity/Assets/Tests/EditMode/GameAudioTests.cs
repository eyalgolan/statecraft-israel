#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The sound effects (#39): GameAudio plays the clip mapped to each cue when its hook fires (End Turn, a refused
/// order, an event card coming into view), at the Settings volume; volume 0 is silent; a missing clip plays nothing and
/// breaks nothing. The audio output is a fake, so no audio device is needed.</summary>
public class GameAudioTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    sealed class FakeOutput : GameAudio.IOutput
    {
        public readonly List<(string clip, float volume)> Played = new List<(string, float)>();
        public void Play(AudioClip clip, float volume) => Played.Add((clip.name, volume));
        public void Dispose() { }
    }

    readonly List<AudioClip> made = new List<AudioClip>();
    string dir = "";

    /// <summary>A clip named after the Resources path asked for, so a test can see which cue played.</summary>
    AudioClip? Fake(string path)
    {
        var clip = AudioClip.Create(path, 441, 1, 44100, false);
        made.Add(clip);
        return clip;
    }

    [SetUp]
    public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "sc-audio-" + System.Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        foreach (var c in made) Object.DestroyImmediate(c);
        made.Clear();
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    static SimBridge NoEvent()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.PendingEvents.Clear();
        return new SimBridge(g, En);
    }

    static SimBridge Showing(string eventId)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = C.Events.First(e => e.Id == eventId).From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(eventId);
        return new SimBridge(g, En);
    }

    static string PathOf(GameAudio.Cue cue) => GameAudio.Folder + "/" + GameAudio.Files[cue];

    [Test]
    public void EndTurnPlaysTheTurnCue()
    {
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var audio = new GameAudio(bridge, null, Fake, output);
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.TurnEnded), 1f) }, output.Played);
    }

    [Test]
    public void ARefusedOrderPlaysTheRefusalCue()
    {
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var audio = new GameAudio(bridge, null, Fake, output);
        Assert.IsNotNull(bridge.Queue(new ChooseEventOption("no_such_event", "a")), "refused: no such event");
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.Refused), 1f) }, output.Played);
    }

    [Test]
    public void AnEventCardPlaysItsCueOnceWhenItComesIntoView()
    {
        var id = C.Events.First(e => e.Options.Count > 0).Id;
        var bridge = Showing(id);
        var output = new FakeOutput();
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, bridge, En, null);
        using var audio = new GameAudio(bridge, card, Fake, output);
        card.Render();   // the same card again (any order queued, any refresh): no second sound
        card.Render();
        Assert.AreEqual(0, output.Played.Count, "the card was already in view when the audio was built (GameRoot builds it after the card, so a rebuild is silent)");
        bridge.Session.State.PendingEvents.Clear();
        card.Render();
        bridge.Session.State.PendingEvents.Add(id);
        card.Render();
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.EventCard), 1f) }, output.Played, "it came back into view: once");
    }

    [Test]
    public void ATurnWithRefusedOrdersSoundsAsARefusal()
    {
        // A turn the sim refuses outright reports every order as refused (SimBridge.EndTurn): one refusal cue, no turn cue.
        var e = C.Events.First(x => x.Options.Count > 0);
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        g.State.Date = e.From;
        g.State.PendingEvents.Clear(); g.State.PendingEvents.Add(e.Id);
        var bridge = new SimBridge(g, En, (s, cmds) => throw new System.ArgumentException("refused by the test"));
        Assert.IsNull(bridge.Queue(new ChooseEventOption(e.Id, e.Options[0].Id)), "an order the turn then refuses");
        var output = new FakeOutput();
        using var audio = new GameAudio(bridge, null, Fake, output);
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "End Turn refused: refused by the test");
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.Refused), 1f) }, output.Played);
    }

    [Test]
    public void ACardUnderSettingsWaitsForItsFirstVisibleCue()
    {
        var id = C.Events.First(e => e.Options.Count > 0).Id;
        var bridge = NoEvent();
        bridge.Session.State.Date = C.Events.First(e => e.Id == id).From;
        var output = new FakeOutput();
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        using var card = new EventCardPresenter(ui.Root, bridge, En, null);
        using var audio = new GameAudio(bridge, card, Fake, output);
        var layer = ui.Q<VisualElement>("event-layer");
        Assert.IsNull(card.Shown, "no card was already visible when audio subscribed");
        Assert.AreEqual(0, output.Played.Count);
        nav.Toggle("settings");
        Assert.AreEqual("settings", nav.Current);
        Assert.IsFalse(layer.enabledInHierarchy, "Settings really covers the event layer");
        bridge.Session.State.PendingEvents.Add(id);
        card.Render();
        Assert.AreEqual(id, card.Shown?.Id);
        Assert.IsFalse(card.Collapsed);
        Assert.IsFalse(layer.enabledInHierarchy, "rendering retains the Settings cover");
        var coveredCount = output.Played.Count;
        Assert.IsTrue(nav.Close());
        Assert.IsNull(nav.Current);
        Assert.IsTrue(layer.enabledInHierarchy, "closing Settings uncovers the pending card");
        Assert.AreEqual(id, card.Shown?.Id);
        var firstVisibleCount = output.Played.Count;
        card.Render();
        var repeatedCount = output.Played.Count;
        TestContext.WriteLine($"COVERED_CARD cue counts covered={coveredCount} firstVisible={firstVisibleCount} repeated={repeatedCount}");
        Assert.AreEqual(0, coveredCount, "a card covered by Settings has not come into view");
        Assert.AreEqual(1, firstVisibleCount, "the first uncovered view sounds once");
        Assert.AreEqual(1, repeatedCount, "another render does not replay the cue");
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.EventCard), 1f) }, output.Played);
    }

    [Test]
    public void ACardHiddenInACaptureRunMakesNoSound()
    {
        var id = C.Events.First(e => e.Options.Count > 0).Id;
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, bridge, En, null, hiddenInCapture: true);
        using var audio = new GameAudio(bridge, card, Fake, output);
        bridge.Session.State.PendingEvents.Add(id);
        card.Render();
        Assert.AreEqual(0, output.Played.Count);
    }

    [Test]
    public void ACardUnderTheStartScreenMakesNoSound()
    {
        var id = C.Events.First(e => e.Options.Count > 0).Id;
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var ui = new UiTestKit();
        using var card = new EventCardPresenter(ui.Root, bridge, En, null, underStartScreen: true);
        using var audio = new GameAudio(bridge, card, Fake, output);
        bridge.Session.State.PendingEvents.Add(id);
        card.Render();
        Assert.AreEqual(0, output.Played.Count, "not heard until the start screen closes (and the browser has had a gesture)");
    }

    [Test]
    public void VolumeZeroIsSilentAndLowIsQuieter()
    {
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var audio = new GameAudio(bridge, null, Fake, output);
        audio.Volume = 0f;
        bridge.EndTurn();
        bridge.Queue(new ChooseEventOption("no_such_event", "a"));
        Assert.AreEqual(0, output.Played.Count, "volume 0 plays nothing");
        audio.Volume = 0.35f;
        bridge.EndTurn();
        CollectionAssert.AreEqual(new[] { (PathOf(GameAudio.Cue.TurnEnded), 0.35f) }, output.Played);
    }

    [Test]
    public void AMissingClipPlaysNothingAndBreaksNothing()
    {
        var bridge = NoEvent();
        var output = new FakeOutput();
        using var audio = new GameAudio(bridge, null, _ => null, output);
        Assert.DoesNotThrow(() => bridge.EndTurn());
        Assert.DoesNotThrow(() => bridge.Queue(new ChooseEventOption("no_such_event", "a")));
        Assert.AreEqual(0, output.Played.Count);
    }

    [Test]
    public void EveryCueHasAShippedClipThatIsSmall()
    {
        foreach (GameAudio.Cue cue in System.Enum.GetValues(typeof(GameAudio.Cue)))
        {
            var clip = Resources.Load<AudioClip>(PathOf(cue));
            Assert.IsNotNull(clip, cue + ": Resources/" + PathOf(cue));
            Assert.Less(clip.length, 1f, cue + ": a short effect");
            Assert.IsTrue(clip.preloadAudioData, cue + ": loaded with the scene, so the first cue is not late (the web decodes asynchronously)");
            var file = Directory.GetFiles(Path.Combine(Application.dataPath, "Resources", GameAudio.Folder), GameAudio.Files[cue] + ".*")
                .Single(f => !f.EndsWith(".meta"));
            Assert.Less(new FileInfo(file).Length, 16 * 1024, cue + ": " + file + " stays small (WebGL download)");
        }
    }

    [Test]
    public void TheSoundOptionSetsTheVolumeAtOnceAndSurvivesAReload([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var bridge = NoEvent();
        using (var ui = new UiTestKit())
        using (var audio = new GameAudio(bridge, null, Fake, new FakeOutput()))
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir), audio: audio))
        {
            Assert.AreEqual(1f, audio.Volume, "full by default, with no file");
            Assert.AreEqual(text["settings.sound"], ui.Q<Label>("settings-sound-label").text);
            foreach (var level in new[] { "off", "low", "high" })
                Assert.AreEqual(text["settings.sound." + level], ui.Q<Button>("settings-sound-" + level).text);
            Assert.IsTrue(ui.Q<Button>("settings-sound-high").ClassListContains(SettingsPresenter.SelectedClass));
            UiTestKit.Press(ui.Q<Button>("settings-sound-off"));
            Assert.AreEqual(0f, audio.Volume, "off at once");
            Assert.IsTrue(ui.Q<Button>("settings-sound-off").ClassListContains(SettingsPresenter.SelectedClass));
            Assert.IsFalse(ui.Q<Button>("settings-sound-high").ClassListContains(SettingsPresenter.SelectedClass));
            UiTestKit.Press(ui.Q<Button>("settings-sound-low"));
            Assert.AreEqual(0.35f, audio.Volume, 1e-6f);
        }
        using (var ui = new UiTestKit())
        using (var audio = new GameAudio(bridge, null, Fake, new FakeOutput()))
        using (new SettingsPresenter(ui.Root, text, new SettingsStore(dir), audio: audio))
        {
            Assert.AreEqual(0.35f, audio.Volume, 1e-6f, "the stored choice is applied at the next start");
            Assert.IsTrue(ui.Q<Button>("settings-sound-low").ClassListContains(SettingsPresenter.SelectedClass));
        }
    }
}
