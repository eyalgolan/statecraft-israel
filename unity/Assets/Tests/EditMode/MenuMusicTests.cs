#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Is = UnityEngine.TestTools.Constraints.Is;

/// <summary>The start menu's music (#39, MenuMusic): Afik's main theme (PR #2240) from StreamingAssets/audio/music/, looped
/// while the start screen shows and faded out over a second of real time when a game starts. On the web it waits for the
/// page's first activation (a click, a tap's release, a key other than Escape). No file is silence for the page: no fetch on
/// the desktop, one line in the log, no warning, no error. The Settings screen's Music row sets its volume and keeps the
/// choice. The fetch, the audio output and the clock are fakes, so no network, audio device or music file is needed.</summary>
public class MenuMusicTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    const float V = MenuMusic.DefaultVolume;   // MenuMusic's own volume until Settings sets one

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
        /// <summary>The loop region of each play.</summary>
        public readonly List<MenuMusic.LoopRegion> Loops = new List<MenuMusic.LoopRegion>();
        public int Watches;
        public bool IsPlaying { get; private set; }
        public float Volume { get; set; } = -1f;
        public bool? Decoded = true;
        public int Stops;
        public bool? Ready(AudioClip clip) => Decoded;
        public void Play(AudioClip clip, MenuMusic.LoopRegion loop) { Played.Add(clip); Loops.Add(loop); IsPlaying = true; }
        public void Watch() => Watches++;
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
        /// <summary>The fake clock, in milliseconds.</summary>
        public double Clock;
        /// <summary>A loop file that fits Answer's 0.1 s clip: samples 441 to 3,969 of 4,410.</summary>
        public const string FittingLoop = Theme + "loop_start_s: 0.01\nloop_end_s: 0.09\n";
        public Rig(bool web, string? url = null, string? loop = FittingLoop)
        {
            Music = new MenuMusic(Ui.Root, web, url ?? "https://example.invalid/StreamingAssets/" + MenuMusic.File, Fetch.Start, Out,
                () => Clock / 1000.0, loop);
            Music.Duck = () => Ducked;
        }
        public AudioClip Answer()
        {
            var clip = AudioClip.Create("theme", 4410, 2, 44100, false);
            Fetch.Pending!(clip, null);
            return clip;
        }
        /// <summary>Lets <paramref name="ms"/> of real time pass in ticks of <paramref name="frameMs"/> (the last one shorter).</summary>
        public void Pass(int ms, int frameMs = MenuMusic.WatchMs)
        {
            for (int left = ms; left > 0; left -= frameMs) { Clock += Math.Min(frameMs, left); Music.Tick(); }
        }
        Button At => Ui.Q<Button>("start-new-game");
        /// <summary>A mouse press on a start screen button: an activation.</summary>
        public void Click()
        {
            using var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = Vector2.zero });
            down.target = At; At.SendEvent(down);
        }
        /// <summary>A finger's press (no activation on iOS) or its release (an activation).</summary>
        public void Tap(bool release)
        {
            var touch = new Touch { fingerId = 0, phase = release ? TouchPhase.Ended : TouchPhase.Began, position = Vector2.zero };
            if (release) { using var up = PointerUpEvent.GetPooled(touch); up.target = At; At.SendEvent(up); }
            else { using var down = PointerDownEvent.GetPooled(touch); down.target = At; At.SendEvent(down); }
        }
        /// <summary>A key on the focused button, or with nothing focused (aimed at the panel's root).</summary>
        public void Key(KeyCode key = KeyCode.A, char character = 'a', bool unfocused = false)
        {
            VisualElement at = unfocused ? Ui.Root.panel.visualTree : At;
            using var k = KeyDownEvent.GetPooled(character, key, EventModifiers.None);
            k.target = at; at.SendEvent(k);
        }
        public void Dispose() { Music.Dispose(); Ui.Dispose(); }
    }

    string dir = "";

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "sc-music-" + Guid.NewGuid().ToString("N"));
        MenuMusic.GestureSeen = false;   // a new page: no activation yet,
        MenuMusic.ThemeAbsent = false;   // and no failed fetch
    }

    [TearDown]
    public void TearDown()
    {
        MenuMusic.GestureSeen = false;
        MenuMusic.ThemeAbsent = false;
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    /// <summary>The player's file: an OGG in StreamingAssets/audio/music/ (owner, 2 Nov 2026), the name Afik's PR #2240 uses.
    /// Once a theme is committed there, a file and a name that differ fail here (before then the folder check has nothing
    /// to compare).</summary>
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

    /// <summary>The absence is the page's, not one instance's: a later MenuMusic (a rebuild) neither fetches again nor logs
    /// again (second review of #2297, C4).</summary>
    [Test]
    public void TheAbsenceHoldsForThePage()
    {
        using (var first = new Rig(web: true))
        {
            first.Music.Show(true); first.Click();
            LogAssert.Expect(LogType.Log, "MUSIC no theme at " + MenuMusic.File + ": the start menu is silent (HTTP/1.1 404 Not Found)");
            first.Fetch.Pending!(null, "HTTP/1.1 404 Not Found");
        }
        using var second = new Rig(web: true);
        second.Music.Show(true); second.Click(); second.Pass(1000);
        LogAssert.NoUnexpectedReceived();
        Assert.IsEmpty(second.Fetch.Urls, "no second fetch for the page");
        Assert.AreEqual(MenuMusic.Phase.Absent, second.Music.State);
    }

    /// <summary>On the web nothing is fetched or played before the page's first activation; after it, the theme loads and
    /// loops.</summary>
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
        Assert.AreEqual(V, rig.Out.Volume, 1e-6f, "MenuMusic's own volume, with no Settings row applied, is the row's Low");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>Only a real activation counts (second review of #2297, H2): a finger's press is none on iOS (its release
    /// is), and Escape is never one.</summary>
    [Test]
    public void OnlyARealActivationCounts()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true);
        rig.Tap(release: false);
        rig.Key(KeyCode.Escape, '\0');
        rig.Key(KeyCode.None, '\u001b');
        Assert.IsEmpty(rig.Fetch.Urls, "a touch press and Escape are no activation");
        rig.Tap(release: true);
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "a touch's release is");
    }

    /// <summary>A key with nothing focused goes to the panel's own root, above the document's: it still counts (second
    /// review of #2297, H1).</summary>
    [Test]
    public void AKeyWithNothingFocusedCounts()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true);
        rig.Key(unfocused: true);
        Assert.AreEqual(1, rig.Fetch.Urls.Count);
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

    /// <summary>The Music volume applies at once; at 0 the theme is never even fetched (no 5.66 MB download for silence),
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
        Assert.AreEqual(V * 0.5f, rig.Out.Volume, 0.01f, "half way");
        Assert.AreEqual(0, rig.Out.Stops);
        rig.Pass(MenuMusic.FadeMs / 2);
        Assert.AreEqual(1, rig.Out.Stops, "stopped after the fade");
        Assert.AreEqual(MenuMusic.Phase.Idle, rig.Music.State);
        Assert.IsTrue(clip == null, "the clip is released");
        rig.Click(); rig.Pass(2000);
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "no new fetch in the game");
        Assert.AreEqual(1, rig.Out.Played.Count, "no play in the game");
    }

    /// <summary>The fade (and the dip) run on real time: at 60, 10 or 4 frames a second the fade is half way after half a
    /// second and over after one, and a one-second hitch ends it at once (second review of #2297, C2).</summary>
    [Test]
    public void TheFadeTakesASecondOfRealTimeAtAnyFrameRate([Values(16, 100, 250, 1000)] int frameMs)
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        rig.Answer();
        rig.Music.Show(false);
        if (frameMs < MenuMusic.FadeMs)
        {
            rig.Pass(MenuMusic.FadeMs / 2, frameMs);
            Assert.AreEqual(V * 0.5f, rig.Out.Volume, 0.01f, "half way after half a second");
            Assert.AreEqual(0, rig.Out.Stops);
            rig.Pass(MenuMusic.FadeMs / 2, frameMs);
        }
        else rig.Pass(MenuMusic.FadeMs, frameMs);
        Assert.AreEqual(1, rig.Out.Stops, "over after a second");
        Assert.AreEqual(MenuMusic.Phase.Idle, rig.Music.State);
    }

    /// <summary>The start screen shown again before the fade ends (C1): the theme turns back up from where it was, never
    /// stops, and is not fetched again.</summary>
    [Test]
    public void ShownAgainDuringTheFadeItComesBackUp()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        rig.Answer();
        rig.Music.Show(false);
        rig.Pass(MenuMusic.FadeMs / 2);
        rig.Music.Show(true);
        Assert.AreEqual(MenuMusic.Phase.Playing, rig.Music.State, "back to playing at once");
        rig.Pass(MenuMusic.FadeMs);
        Assert.AreEqual(V, rig.Out.Volume, 1e-6f, "back up");
        Assert.AreEqual(0, rig.Out.Stops, "never stopped");
        Assert.AreEqual(1, rig.Fetch.Urls.Count, "fetched once");
        Assert.AreEqual(1, rig.Out.Played.Count, "played once: no restart from the top");
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

    /// <summary>Ben-Gurion's recording on the same screen is not played over. The owner's ruling on #2297 (2 Nov 2026), "Dip
    /// under the recording (Recommended)": "The theme drops low while the recording plays, then comes back". Low, not
    /// silence: MenuMusic.DipLevel (0.2) of the Music volume while it plays, and back after it.</summary>
    [Test]
    public void ItDipsUnderTheDeclarationRecording()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        rig.Ducked = true;
        rig.Pass(MenuMusic.FadeMs);
        Assert.AreEqual(V * MenuMusic.DipLevel, rig.Out.Volume, 1e-6f, "low under the recording, not silent");
        Assert.AreEqual(MenuMusic.Phase.Playing, rig.Music.State, "paused in volume only: it goes on looping");
        rig.Ducked = false;
        rig.Pass(MenuMusic.FadeMs);
        Assert.AreEqual(V, rig.Out.Volume, 1e-6f, "back after it");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>The page's first click is "Play the recording": the theme starts loading on that click, before the recording
    /// plays, and is decoded while the recording is already playing. It must start low, not at full volume over Ben-Gurion's
    /// voice until the next ticks catch up (review of #2297, N1).</summary>
    [Test]
    public void AThemeThatStartsDuringTheRecordingStartsLow()
    {
        using var rig = new Rig(web: true);
        rig.Music.Volume = 0.35f;                         // the Settings default (Low)
        rig.Music.Show(true);
        rig.Click();                                      // the click on Listen: the fetch starts, the recording not yet
        Assert.AreEqual(1, rig.Fetch.Urls.Count);
        rig.Ducked = true;                                // the recording is now playing
        var clip = rig.Answer();                          // the theme arrives and plays at once
        CollectionAssert.AreEqual(new[] { clip }, rig.Out.Played);
        Assert.AreEqual(0.35f * MenuMusic.DipLevel, rig.Out.Volume, 1e-6f, "low from its first sample, relative to the Music volume");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>The watch allocates nothing per tick: playing, dipping and fading.</summary>
    [Test]
    public void ATickAllocatesNothing()
    {
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs);
        Assert.That(() => { for (int i = 0; i < 50; i++) { rig.Clock += MenuMusic.WatchMs; rig.Music.Tick(); } }, Is.Not.AllocatingGCMemory());
        rig.Ducked = true;
        Assert.That(() => { for (int i = 0; i < 50; i++) { rig.Clock += MenuMusic.WatchMs; rig.Music.Tick(); } }, Is.Not.AllocatingGCMemory());
        rig.Music.Show(false);
        Assert.That(() => { for (int i = 0; i < 10; i++) { rig.Clock += MenuMusic.WatchMs; rig.Music.Tick(); } }, Is.Not.AllocatingGCMemory());
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>MenuMusic's own default is the Music row's Low (second review of #2297, C5): a wiring that never applies
    /// Settings cannot play it loud.</summary>
    [Test]
    public void ItsOwnDefaultIsTheRowsLow()
    {
        using var rig = new Rig(web: true);
        Assert.AreEqual(0.35f, rig.Music.Volume, 1e-6f);
        Assert.AreEqual(Array.Find(SettingsPresenter.SoundLevels, l => l.level == SettingsPresenter.MusicDefault).volume, MenuMusic.DefaultVolume);
    }

    /// <summary>GameRoot's wiring (C5): one MenuMusic for the page, made where the start screen shows, at Settings' Low, with
    /// the recording as its duck and the screen's Show; a language switch keeps the same instance (no cut, no new fetch,
    /// C4), and starting a game hands it Show(false).</summary>
    [Test]
    public void GameRootWiresOneMusicForThePage()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        try
        {
            UnityEngine.Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
            var game = UnityEngine.Object.FindFirstObjectByType<GameRoot>();
            game.Store = new SaveStore(Path.Combine(dir, "saves"));
            game.Settings = new SettingsStore(Path.Combine(dir, "settings"));
            game.MenuMusicInEditMode = true;
            MenuMusic.ThemeAbsent = true;   // the wiring, not the file: nothing is fetched here
            game.Begin(Inputs, 0, startScreen: true);
            var ui = UnityEngine.Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            var music = game.Music;
            Assert.IsNotNull(music, "made where the start screen shows");
            Assert.IsTrue(music!.Wanted, "the start screen's Show reached it");
            Assert.AreEqual(0.35f, music.Volume, 1e-6f, "Settings applied the Music row's Low");
            Assert.IsNotNull(music.Duck, "the recording is its duck");
            Assert.IsFalse(music.Duck!(), "nothing is playing yet");
            UiTestKit.Press(ui.Q<Button>("start-language-he"));   // the rebuild a language switch makes
            Assert.AreSame(music, game.Music, "the same music goes on");
            Assert.IsTrue(music.Wanted, "the rebuilt start screen wants it");
            Assert.IsNotNull(music.Duck, "the new screen's recording is its duck");
            UiTestKit.Press(ui.Q<Button>("start-new-game"));
            Assert.IsFalse(music.Wanted, "a game started: the screen closed");
        }
        finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
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

    /// <summary>The shipped theme's loop region (#2338): Afik's proposed loop points, 00:10 to 04:03 (ASSETS.yaml), read from
    /// Resources/Music/menu-music.yaml: samples 441,000 to 10,716,300 at the file's 44,100 Hz, both inside the clip. The
    /// rate and the length come from the committed OGG itself (its Vorbis header and its last page's granule position).</summary>
    [Test]
    public void TheThemeLoopsBetweenAfiksLoopPoints()
    {
        var ogg = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, MenuMusic.File));
        int rate = BitConverter.ToInt32(ogg, 28 + 12);   // first page header (28 bytes with one segment), then "\x01vorbis", version, channels
        int last = -1;
        for (int i = ogg.Length - 4; i >= 0 && last < 0; i--)
            if (ogg[i] == 'O' && ogg[i + 1] == 'g' && ogg[i + 2] == 'g' && ogg[i + 3] == 'S') last = i;
        int samples = (int)BitConverter.ToInt64(ogg, last + 6);
        Assert.AreEqual(44100, rate);
        Assert.AreEqual(11037601, samples, "ffprobe's duration_ts for the file (#2338)");
        var yaml = Resources.Load<TextAsset>(MenuMusic.LoopResource)?.text;
        Assert.IsNotNull(yaml, "Assets/Resources/" + MenuMusic.LoopResource + ".yaml is shipped");
        var loop = MenuMusic.Loop(yaml, samples, rate, out var why);
        Assert.IsNull(why);
        Assert.AreEqual(441000, loop.Start, "00:10");
        Assert.AreEqual(10716300, loop.End, "04:03");
        Assert.Less(loop.End, samples, "inside the clip");
    }

    /// <summary>The schedule the output plays (LoopRegion, which SourceOutput's handover follows): the first play starts at
    /// the top (sample 0, not the loop start) and runs to the loop end; from there playback goes on at the loop start, and
    /// again at every later seam, with no sample left out or played twice. MenuMusic hands the output the region and
    /// watches it every tick. (SourceOutput's scheduling calls: TheTwoSourcesHandOverAtEachSeamOnTheAudioClock.)</summary>
    [Test]
    public void ItPlaysFromTheTopThenLoopsTheRegion()
    {
        var loop = new MenuMusic.LoopRegion(441000, 10716300, 44100);
        Assert.AreEqual(0, loop.SampleAt(0), "the first play starts at 0:00");
        Assert.AreEqual(441000, loop.SampleAt(441000), "the intro plays once, straight through");
        Assert.AreEqual(10716299, loop.SampleAt(10716299), "the last sample before the loop end");
        Assert.AreEqual(10716300, loop.SeamAt(0), "the first seam is at 04:03");
        Assert.AreEqual(441000, loop.SampleAt(loop.SeamAt(0)), "at the loop end it continues at the loop start");
        Assert.AreEqual(441001, loop.SampleAt(loop.SeamAt(0) + 1));
        Assert.AreEqual(10716300 + 10275300, loop.SeamAt(1), "the next seam, one region (3:53) later");
        Assert.AreEqual(10716299, loop.SampleAt(loop.SeamAt(1) - 1));
        Assert.AreEqual(441000, loop.SampleAt(loop.SeamAt(1)));
        using var rig = new Rig(web: true);
        rig.Music.Show(true); rig.Click();
        var clip = rig.Answer();
        Assert.AreEqual(new MenuMusic.LoopRegion(441, 3969, 44100), rig.Out.Loops.Single(), "the output gets the region");
        rig.Pass(MenuMusic.WatchMs * 3);
        Assert.AreEqual(3, rig.Out.Watches, "the output is watched every tick, to schedule its next seam");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>One of SourceOutput's two sources: it records every call, with the value, in order, in a shared log.</summary>
    sealed class RecordingSource : MenuMusic.ISource
    {
        readonly string name; readonly List<string> log;
        public RecordingSource(string name, List<string> log) { this.name = name; this.log = log; }
        public AudioClip? Clip { set => log.Add(name + ".Clip " + (value == null ? "null" : value.name)); }
        public float Volume { set => log.Add(name + ".Volume " + value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)); }
        public bool Loop { set => log.Add(name + ".Loop " + value); }
        public int TimeSamples { set => log.Add(name + ".TimeSamples " + value); }
        public void Play() => log.Add(name + ".Play");
        public void PlayScheduled(double at) => log.Add(name + ".PlayScheduled " + Ms(at));
        public void SetScheduledEndTime(double at) => log.Add(name + ".SetScheduledEndTime " + Ms(at));
        public void Stop() => log.Add(name + ".Stop");
        /// <summary>The audio clock in whole milliseconds, so seams counted in samples compare exactly.</summary>
        static string Ms(double s) => Math.Round(s * 1000.0).ToString(System.Globalization.CultureInfo.InvariantCulture) + " ms";
    }

    /// <summary>The game's output (SourceOutput) hands over between its two sources on the audio clock (#2338). With a
    /// recorded pair of sources and a fake dspTime: the first source starts at sample 0 and its scheduled end is the first
    /// seam; the second starts at the loop start at that same dspTime. After the seam the roles swap and the next seam is
    /// one region later. A watch stalled past the next seam (a hidden tab) starts the loop again at its start, with the
    /// following seam one region after that. This drives the scheduling calls, not Unity's audio: whether the two halves
    /// meet without a gap or a click in a real browser is the listening check in #2338.</summary>
    [Test]
    public void TheTwoSourcesHandOverAtEachSeamOnTheAudioClock()
    {
        var log = new List<string>();
        double dsp = 10.0;
        var made = 0;
        using var output = new MenuMusic.SourceOutput(() => dsp, () => new RecordingSource(made++ == 0 ? "A" : "B", log));
        // A 10 s clip looping 1 s to 8 s: one region (7 s) is longer than Lead, as the theme's 3:53 is.
        var clip = AudioClip.Create("theme", 441000, 1, 44100, false);
        output.Play(clip, new MenuMusic.LoopRegion(44100, 352800, 44100));
        // Starts at 10 s + Lead (100 ms) = 10,100 ms; the first seam is 8 s of clip later.
        CollectionAssert.AreEqual(new[] {
            "A.Stop", "A.Clip null", "B.Stop", "B.Clip null",
            "B.Clip theme", "A.Clip theme", "B.Volume 0.35", "A.Volume 0.35", "A.Loop False", "A.TimeSamples 0",
            "A.PlayScheduled 10100 ms",
            "A.SetScheduledEndTime 18100 ms", "B.TimeSamples 44100", "B.PlayScheduled 18100 ms",
        }, log, "the first source plays from the top; its end and the second's start at the loop start share the seam's dspTime");
        log.Clear();
        dsp = 18.0; output.Watch();
        CollectionAssert.IsEmpty(log, "nothing to do before the seam");
        dsp = 18.2; output.Watch();
        CollectionAssert.AreEqual(new[] { "B.SetScheduledEndTime 25100 ms", "A.TimeSamples 44100", "A.PlayScheduled 25100 ms" }, log,
            "after the seam the second plays on and the first is scheduled for the next seam, one region (7 s) later");
        log.Clear();
        dsp = 40.0; output.Watch();   // stalled: the seams at 25.1 s and 32.1 s passed unwatched
        CollectionAssert.AreEqual(new[] {
            "A.TimeSamples 44100", "A.PlayScheduled 40100 ms",
            "A.SetScheduledEndTime 47100 ms", "B.TimeSamples 44100", "B.PlayScheduled 47100 ms",
        }, log, "a stalled watch starts the loop again at its start (now + Lead), not once per missed seam");
        log.Clear();
        output.Play(clip, new MenuMusic.LoopRegion(0, 441000, 44100));
        CollectionAssert.AreEqual(new[] {
            "A.Stop", "A.Clip null", "B.Stop", "B.Clip null",
            "B.Clip theme", "A.Clip theme", "B.Volume 0.35", "A.Volume 0.35", "A.Loop True", "A.TimeSamples 0", "A.Play",
        }, log, "the whole clip (no region) loops on one source, with nothing scheduled");
        log.Clear();
        dsp = 60.0; output.Watch();
        CollectionAssert.IsEmpty(log, "and the watch leaves it alone");
        UnityEngine.Object.DestroyImmediate(clip);
    }

    /// <summary>The scene's teardown can destroy the music's GameObject before GameRoot.OnDisable disposes the music (the
    /// order varies). Stop, Volume and Dispose on the game's own output then skip the destroyed AudioSources (Unity's null),
    /// as before #2338, instead of throwing MissingReferenceException.</summary>
    [Test]
    public void ADestroyedMusicObjectStopsAndDisposesQuietly()
    {
        var output = new MenuMusic.SourceOutput();
        var clip = AudioClip.Create("theme", 4410, 1, 44100, false);
        output.Play(clip, new MenuMusic.LoopRegion(0, 4410, 44100));
        var go = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).First(s => s.clip == clip).gameObject;
        UnityEngine.Object.DestroyImmediate(go);
        Assert.DoesNotThrow(() => { output.Stop(); output.Volume = 0.5f; output.Dispose(); });
        LogAssert.NoUnexpectedReceived();
        UnityEngine.Object.DestroyImmediate(clip);
    }

    const string Theme = "file: " + MenuMusic.File + "\n";

    /// <summary>No loop file, a broken one, one for another file, or a region that is empty, reversed or past the clip's end:
    /// the whole clip loops (as before #2338), with one plain log line, no warning and no error.</summary>
    [Test]
    public void AMissingOrInvalidLoopRegionLoopsTheWholeClip([Values("", "loop_start_s: [", "file: audio/music/Other.ogg\nloop_start_s: 0.01\nloop_end_s: 0.09\n",
        Theme + "loop_start_s: 0.09\nloop_end_s: 0.01\n", Theme + "loop_start_s: 0.05\nloop_end_s: 0.05\n", Theme + "loop_start_s: 0.01\nloop_end_s: 0.2\n",
        Theme + "loop_start_s: -0.01\nloop_end_s: 0.09\n")] string loop)
    {
        using var rig = new Rig(web: true, loop: loop);
        rig.Music.Show(true); rig.Click();
        LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("^MUSIC no loop region for " + System.Text.RegularExpressions.Regex.Escape(MenuMusic.File) + ": the whole clip loops \\([\\s\\S]+\\)$"));
        var clip = rig.Answer();
        rig.Pass(MenuMusic.WatchMs * 3);
        LogAssert.NoUnexpectedReceived();
        Assert.AreEqual(new MenuMusic.LoopRegion(0, 4410, 44100), rig.Out.Loops.Single(), "the whole clip");
        UnityEngine.Object.DestroyImmediate(clip);
    }
}
