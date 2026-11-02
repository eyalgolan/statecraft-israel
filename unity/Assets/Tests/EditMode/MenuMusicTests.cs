#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

/// <summary>The start menu's music (#39, MenuMusic): Afik's main theme (PR #2240) from StreamingAssets/audio/music/, looped
/// while the start screen shows and faded out over a second when a game starts. On the web it waits for the first click or
/// key (browsers block audio until then). No file is silence: no fetch on the desktop, one line in the log, no warning,
/// no error. The Settings screen's Music row sets its volume and keeps the choice. The fetch and the audio output are
/// fakes, so no network, audio device or music file is needed.</summary>
public class MenuMusicTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>A fetch the test answers by hand.</summary>
    sealed class FakeFetch
    {
        public readonly List<string> Urls = new List<string>();
        public Action<AudioClip?, string?>? Pending;
        public int Aborts;
        sealed class Request : DeclarationRecording.IRequest
        {
            readonly FakeFetch f; public Request(FakeFetch f) { this.f = f; }
            public ulong Bytes => 0;
            public void Abort() => f.Aborts++;
        }
        public DeclarationRecording.IRequest Start(string url, Action<AudioClip?, string?> done) { Urls.Add(url); Pending = done; return new Request(this); }
    }

    sealed class FakeOutput : MenuMusic.IOutput
    {
        public readonly List<AudioClip> Played = new List<AudioClip>();
        public bool IsPlaying { get; private set; }
        public float Volume { get; set; } = -1f;
        public bool? Decoded = true;
        public int Stops;
        public bool? Ready(AudioClip clip) => Decoded;
        public void Play(AudioClip clip) { Played.Add(clip); IsPlaying = true; }
        public void Stop() { Stops++; IsPlaying = false; }
        public void Dispose() { }
    }

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly FakeFetch Fetch = new FakeFetch();
        public readonly FakeOutput Out = new FakeOutput();
        public readonly MenuMusic Music;
        public bool Ducked;
        public Rig(bool web, string? url = null)
        {
            Music = new MenuMusic(Ui.Root, web, url ?? "https://example.invalid/StreamingAssets/" + MenuMusic.File, Fetch.Start, Out);
            Music.Duck = () => Ducked;
        }
        public AudioClip Answer()
        {
            var clip = AudioClip.Create("theme", 4410, 2, 44100, false);
            Fetch.Pending!(clip, null);
            return clip;
        }
        /// <summary>Lets <paramref name="ms"/> of the watch's ticks pass.</summary>
        public void Pass(int ms) { for (int t = 0; t < ms; t += MenuMusic.WatchMs) Music.Tick(); }
        /// <summary>A click or a key on a start screen button: the music listens on the root, before any button can stop it.</summary>
        public void Click()
        {
            var at = Ui.Q<Button>("start-new-game");
            using var down = PointerDownEvent.GetPooled();
            down.target = at; at.SendEvent(down);
        }
        public void Key()
        {
            var at = Ui.Q<Button>("start-new-game");
            using var k = KeyDownEvent.GetPooled('a', KeyCode.A, EventModifiers.None);
            k.target = at; at.SendEvent(k);
        }
        public void Dispose() { Music.Dispose(); Ui.Dispose(); }
    }

    string dir = "";

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "sc-music-" + Guid.NewGuid().ToString("N"));
        MenuMusic.GestureSeen = false;   // a new page: no click yet
    }

    [TearDown]
    public void TearDown()
    {
        MenuMusic.GestureSeen = false;
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    /// <summary>The player's file: an OGG in StreamingAssets/audio/music/ (owner, 2 Nov 2026), the name Afik's PR #2240 uses.
    /// When a theme is committed there, the player names it: a renamed file fails here, not silently in the game.</summary>
    [Test]
    public void ThePlayerPicksTheThemeInStreamingAssetsMusic()
    {
        Assert.AreEqual("audio/music/Main_1.3.ogg", MenuMusic.File);
        StringAssert.EndsWith("/StreamingAssets/" + MenuMusic.File, MenuMusic.Url().Split('?')[0]);
        StringAssert.StartsWith("file://", MenuMusic.Url(), "the editor and desktop read the file from disk");
        var folder = Path.Combine(Application.streamingAssetsPath, "audio", "music");
        var shipped = Directory.Exists(folder)
            ? Directory.GetFiles(folder).Where(f => !f.EndsWith(".meta") && !Path.GetFileName(f).StartsWith(".")).ToList()
            : new List<string>();
        foreach (var f in shipped) StringAssert.EndsWith(".ogg", f, "music is OGG only (owner, 2 Nov 2026)");
        if (shipped.Count > 0)
            CollectionAssert.Contains(shipped.Select(f => "audio/music/" + Path.GetFileName(f)).ToList(), MenuMusic.File,
                "MenuMusic.File names the committed theme");
    }

    /// <summary>No theme on the desktop (main before #2240 lands): no fetch, nothing played, one plain log line however
    /// often the screen shows or the player clicks, and no warning or error.</summary>
    [Test]
    public void NoFileIsSilentWithOneLogLineAndNoFetch()
    {
        var missing = "file://" + Path.Combine(dir, "none.ogg");
        using var rig = new Rig(web: false, url: missing);
        LogAssert.Expect(LogType.Log, "MUSIC no theme at " + MenuMusic.File + ": the start menu is silent");
        Assert.DoesNotThrow(() => rig.Music.Show(true));
        rig.Click(); rig.Pass(3000);
        rig.Music.Show(false); rig.Music.Show(true); rig.Pass(1000);
        LogAssert.NoUnexpectedReceived();
        Assert.IsEmpty(rig.Fetch.Urls, "no file, no fetch");
        Assert.IsEmpty(rig.Out.Played);
        Assert.AreEqual(MenuMusic.Phase.Absent, rig.Music.State);
    }

    /// <summary>No theme on the web (the server answers 404): silent, one plain log line, no warning or error, and no second
    /// fetch for the rest of the page.</summary>
    [Test]
    public void AFailedFetchIsSilentAndNotRetried()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true);
        rig.Click();
        Assert.AreEqual(1, rig.Fetch.Urls.Count);
        LogAssert.Expect(LogType.Log, "MUSIC no theme at " + MenuMusic.File + ": the start menu is silent (HTTP/1.1 404 Not Found)");
        rig.Fetch.Pending!(null, "HTTP/1.1 404 Not Found");
        rig.Pass(2000); rig.Click(); rig.Key();
        rig.Music.Show(false); rig.Music.Show(true); rig.Pass(1000);
        LogAssert.NoUnexpectedReceived();
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "fetched once");
        Assert.IsEmpty(rig.Out.Played);
    }

    /// <summary>On the web nothing is fetched or played before the first click or key; after it, the theme loads and loops.</summary>
    [Test]
    public void OnTheWebItWaitsForTheFirstClickOrKey([Values(false, true)] bool byKey)
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true);
        rig.Pass(2000);
        Assert.IsEmpty(rig.Fetch.Urls, "no gesture yet");
        if (byKey) rig.Key(); else rig.Click();
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "the first gesture starts it");
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        CollectionAssert.AreEqual(new[] { clip }, rig.Out.Played);
        Assert.AreEqual(MenuMusic.Phase.Playing, rig.Music.State);
        Assert.AreEqual(1f, rig.Out.Volume, 1e-6f, "full by default");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>On the desktop there is no gesture rule: it loads when the start screen shows. A clip still decoding plays
    /// once it is ready.</summary>
    [Test]
    public void OnTheDesktopItStartsWithTheStartScreen()
    {
        var file = Path.Combine(dir, "theme.ogg");
        Directory.CreateDirectory(dir); File.WriteAllBytes(file, new byte[] { 0x4f, 0x67, 0x67, 0x53 });   // exists; the fetch is fake
        using var rig = new Rig(web: false, url: "file://" + file);
        Assert.IsEmpty(rig.Fetch.Urls, "nothing before the screen shows");
        rig.Music.Show(true);
        Assert.AreEqual(1, rig.Fetch.Urls.Count);
        rig.Out.Decoded = false;
        var clip = rig.Answer();
        rig.Pass(500);
        Assert.IsEmpty(rig.Out.Played, "still decoding");
        rig.Out.Decoded = true;
        rig.Pass(MenuMusic.WatchMs);
        CollectionAssert.AreEqual(new[] { clip }, rig.Out.Played);
    }

    /// <summary>The Music volume applies at once; at 0 the theme is never even fetched (no 12 MB download for silence),
    /// and turning it up later starts it.</summary>
    [Test]
    public void TheVolumeAndMuteAreRespected()
    {
        using var rig = new Rig(web: true);
        rig.Music.Volume = 0f;
        rig.Music.Show(true);
        rig.Click(); rig.Pass(1000);
        Assert.IsEmpty(rig.Fetch.Urls, "muted: not fetched");
        rig.Music.Volume = 0.35f;
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "turned up: it starts");
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        Assert.AreEqual(0.35f, rig.Out.Volume, 1e-6f);
        rig.Music.Volume = 0f;
        Assert.AreEqual(0f, rig.Out.Volume, "muted while playing: silent at once");
        rig.Music.Volume = 1f;
        Assert.AreEqual(1f, rig.Out.Volume, 1e-6f);
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>When a game starts (the start screen closes) it fades out over about a second, then stops and lets the clip
    /// go; it does not play during the game.</summary>
    [Test]
    public void ItFadesOutAndStopsWhenAGameStarts()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        Assert.AreEqual(MenuMusic.Phase.Playing, rig.Music.State);
        rig.Music.Show(false);
        Assert.AreEqual(MenuMusic.Phase.Fading, rig.Music.State);
        rig.Pass(MenuMusic.FadeMs / 2);
        Assert.AreEqual(0.5f, rig.Out.Volume, 0.06f, "half way");
        Assert.AreEqual(0, rig.Out.Stops);
        rig.Pass(MenuMusic.FadeMs / 2);
        Assert.AreEqual(1, rig.Out.Stops, "stopped after the fade");
        Assert.AreEqual(MenuMusic.Phase.Idle, rig.Music.State);
        Assert.IsTrue(clip == null, "the clip is released");
        rig.Click(); rig.Pass(2000);
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "no new fetch in the game");
        Assert.AreEqual(1, rig.Out.Played.Count, "no play in the game");
    }

    /// <summary>A game that starts while the theme is still loading cancels the fetch; the late answer plays nothing.</summary>
    [Test]
    public void AGameStartedWhileLoadingCancelsTheFetch()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var pending = rig.Fetch.Pending!;
        rig.Music.Show(false);
        Assert.AreEqual(1, rig.Fetch.Aborts);
        var clip = AudioClip.Create("late", 441, 1, 44100, false);
        pending(clip, null);
        rig.Pass(1000);
        Assert.IsEmpty(rig.Out.Played);
        Assert.IsTrue(clip == null, "the late clip is released");
    }

    /// <summary>Ben-Gurion's recording on the same screen is not played over: the theme dips to silence while it plays and
    /// comes back after.</summary>
    [Test]
    public void ItDipsUnderTheDeclarationRecording()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        rig.Ducked = true;
        rig.Pass(MenuMusic.FadeMs);
        Assert.AreEqual(0f, rig.Out.Volume, 1e-6f, "silent under the recording");
        Assert.AreEqual(MenuMusic.Phase.Playing, rig.Music.State, "paused in volume only: it goes on looping");
        rig.Ducked = false;
        rig.Pass(MenuMusic.FadeMs);
        Assert.AreEqual(1f, rig.Out.Volume, 1e-6f, "back after it");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>The watch that runs while it plays allocates nothing per tick.</summary>
    [Test]
    public void ATickAllocatesNothing()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        Assert.That(() => { for (int i = 0; i < 50; i++) rig.Music.Tick(); }, Is.Not.AllocatingGCMemory());
        rig.Ducked = true;
        Assert.That(() => { for (int i = 0; i < 50; i++) rig.Music.Tick(); }, Is.Not.AllocatingGCMemory());
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>The Settings screen's Music row: Off, Low and High, applied to the music at once,
    /// kept in the SettingsStore across a reload, in English and in Hebrew (whose words await the native reviewer, #564).</summary>
    [Test]
    public void TheMusicRowSetsTheVolumeAtOnceAndSurvivesAReload([Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        using (var rig = new Rig(web: true))
        using (new SettingsPresenter(rig.Ui.Root, text, new SettingsStore(dir), music: rig.Music))
        {
            var ui = rig.Ui;
            Assert.AreEqual(text["settings.music"], ui.Q<Label>("settings-music-label").text);
            foreach (var level in new[] { "off", "low", "high" })
                Assert.AreEqual(text["settings.music." + level], ui.Q<Button>("settings-music-" + level).text);
            UiTestKit.Press(ui.Q<Button>("settings-music-off"));
            Assert.AreEqual(0f, rig.Music.Volume, "off at once");
            Assert.IsTrue(ui.Q<Button>("settings-music-off").ClassListContains(SettingsPresenter.SelectedClass));
            Assert.IsFalse(ui.Q<Button>("settings-music-low").ClassListContains(SettingsPresenter.SelectedClass));
            UiTestKit.Press(ui.Q<Button>("settings-music-high"));
            Assert.AreEqual(1f, rig.Music.Volume, 1e-6f);
            Assert.AreEqual("high", new SettingsStore(dir).Read(SettingsPresenter.MusicKey));
        }
        using (var rig = new Rig(web: true))
        using (new SettingsPresenter(rig.Ui.Root, text, new SettingsStore(dir), music: rig.Music))
        {
            Assert.AreEqual(1f, rig.Music.Volume, 1e-6f, "the stored choice is applied at the next start");
            Assert.IsTrue(rig.Ui.Q<Button>("settings-music-high").ClassListContains(SettingsPresenter.SelectedClass));
        }
    }

    /// <summary>The owner's ruling on #2297 (2 Nov 2026): "Low by default (Recommended)", "35% volume: the theme sits under
    /// the menu". A fresh settings store (no file, or a file with no music line) reads Low, marked, at 0.35.</summary>
    [Test]
    public void AFreshSettingsStoreReadsLow([Values(false, true)] bool otherLines)
    {
        if (otherLines) new SettingsStore(dir).Write(SettingsPresenter.SoundKey, "high");   // a file from before the Music row
        using var rig = new Rig(web: true);
        using var settings = new SettingsPresenter(rig.Ui.Root, En, new SettingsStore(dir), music: rig.Music);
        Assert.AreEqual(0.35f, rig.Music.Volume, 1e-6f, "Low by default");
        Assert.IsTrue(rig.Ui.Q<Button>("settings-music-low").ClassListContains(SettingsPresenter.SelectedClass), "Low is marked");
        Assert.IsFalse(rig.Ui.Q<Button>("settings-music-high").ClassListContains(SettingsPresenter.SelectedClass));
        Assert.IsNull(new SettingsStore(dir).Read(SettingsPresenter.MusicKey), "the default is not written: only a choice is kept");
    }

    /// <summary>The sound effects row and the Music row are separate: one does not change the other.</summary>
    [Test]
    public void TheSoundEffectsRowLeavesTheMusicAlone()
    {
        using var rig = new Rig(web: true);
        using var settings = new SettingsPresenter(rig.Ui.Root, En, new SettingsStore(dir), music: rig.Music);
        UiTestKit.Press(rig.Ui.Q<Button>("settings-sound-off"));
        Assert.AreEqual(0.35f, rig.Music.Volume, 1e-6f, "still the Music row's Low");
        UiTestKit.Press(rig.Ui.Q<Button>("settings-music-off"));
        Assert.AreEqual("off", new SettingsStore(dir).Read(SettingsPresenter.SoundKey));
        Assert.AreEqual("off", new SettingsStore(dir).Read(SettingsPresenter.MusicKey));
    }

    /// <summary>The new words; the Hebrew waits for the native reviewer (#564).</summary>
    [Test]
    public void TheMusicRowIsInBothLanguages()
    {
        Assert.AreEqual("Music", En["settings.music"]);
        Assert.AreEqual("מוזיקה", He["settings.music"]);
        Assert.AreEqual("כבויה", He["settings.music.off"]);
        Assert.AreEqual("חלשה", He["settings.music.low"]);
        Assert.AreEqual("חזקה", He["settings.music.high"]);
    }
}
