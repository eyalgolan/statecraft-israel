#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Ben-Gurion's 1948 recording on the start screen (docs/declaration-recording.md): the committed excerpt is pinned
/// by hash; it plays only from its button; while it loads the button says so, and the captions and "Stop" show only once
/// the audio is heard; a failed or slow load ends with a message; the end of the clip is "was playing, now is not"; the
/// clip is released on stop. The fetch and the audio output are fakes here (the game's are a UnityWebRequest and an
/// AudioSource), so every path runs in edit mode without a network or an audio device.</summary>
public class DeclarationRecordingTests
{
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static string FilePath => Path.Combine(Application.streamingAssetsPath, DeclarationRecording.File);

    [Test]
    public void TheCommittedExcerptIsPinnedByHash()
    {
        Assert.IsTrue(File.Exists(FilePath), FilePath);
        using var sha = SHA256.Create();
        var hash = string.Concat(sha.ComputeHash(File.ReadAllBytes(FilePath)).Select(b => b.ToString("x2")));
        Assert.AreEqual(DeclarationRecording.Sha256, hash, "the committed excerpt (docs/declaration-recording.md)");
        Assert.AreEqual(1784396, new FileInfo(FilePath).Length);
        Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(FilePath), 0, 4), "a WAV file");
        Assert.IsNull(AssetImporter.GetAtPath("Assets/StreamingAssets/" + DeclarationRecording.File) as AudioImporter,
            "StreamingAssets: Unity does not import or re-encode it");
    }

    /// <summary>A fetch the test answers by hand.</summary>
    sealed class FakeFetch
    {
        public readonly List<string> Urls = new List<string>();
        public Action<AudioClip?, string?>? Pending;
        public int Aborts;
        public ulong Bytes;
        sealed class Request : DeclarationRecording.IRequest
        {
            readonly FakeFetch f; public Request(FakeFetch f) { this.f = f; }
            public ulong Bytes => f.Bytes;
            public void Abort() => f.Aborts++;
        }
        public DeclarationRecording.IRequest Start(string url, Action<AudioClip?, string?> done) { Urls.Add(url); Pending = done; Bytes = 0; return new Request(this); }
        public AudioClip Answer()
        {
            var clip = AudioClip.Create("fake", 4800, 1, 48000, false);
            Pending!(clip, null);
            return clip;
        }
    }

    sealed class FakeOutput : DeclarationRecording.IOutput
    {
        public readonly List<AudioClip> Played = new List<AudioClip>();
        public bool IsPlaying { get; set; }
        public float Position => IsPlaying ? 0.2f : 0f;
        public bool? Decoded = true;
        public bool StartsAtOnce = true;
        public int Stops;
        public bool? Ready(AudioClip clip) => Decoded;
        public void Play(AudioClip clip) { Played.Add(clip); IsPlaying = StartsAtOnce; }
        public void Stop() { Stops++; IsPlaying = false; }
        public void Dispose() { }
    }

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly FakeFetch Fetch = new FakeFetch();
        public readonly FakeOutput Out = new FakeOutput();
        public readonly List<string> Said = new List<string>();
        public readonly DeclarationRecording Recording;
        public Rig() { Recording = new DeclarationRecording(Ui.Root, Text, Said.Add, Fetch.Start, Out); }
        public Button Listen => Ui.Q<Button>("start-listen");
        public bool Captions => Ui.Root.Q("start-caption").style.display.value == DisplayStyle.Flex;
        public void Press() => UiTestKit.Press(Listen);
        public void Ticks(int ms) { for (int t = 0; t < ms; t += 200) Recording.Tick(); }
        /// <summary>Ticks while the fetch keeps receiving a few bytes each time (a slow link).</summary>
        public void TicksReceiving(int ms) { for (int t = 0; t < ms; t += 200) { Fetch.Bytes += 4000; Recording.Tick(); } }
        public void Dispose() { Recording.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void ThePressLoadsThenPlaysAndOnlyThenShowsTheCaptionsAndStop()
    {
        using var r = new Rig();
        Assert.AreEqual("Recording: Ralph Hellinger, Israel State Archives", r.Ui.Q<Label>("start-recording-credit").text);
        Assert.AreEqual("Play the recording", r.Listen.text);
        Assert.IsFalse(r.Recording.Playing, "nothing plays by itself");
        Assert.IsEmpty(r.Fetch.Urls, "nothing is fetched before the press");

        r.Press();
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State);
        Assert.AreEqual(1, r.Fetch.Urls.Count);
        StringAssert.EndsWith("/" + DeclarationRecording.File, r.Fetch.Urls[0]);
        Assert.AreEqual("Loading the recording…", r.Listen.text);
        Assert.IsFalse(r.Captions, "no captions over silence while it loads");
        Assert.IsEmpty(r.Out.Played);

        LogAssert.Expect(LogType.Log, new Regex("^RECORDING playing " + Regex.Escape(DeclarationRecording.File) + @" \(0\.10 s, at 0\.20 s\)$"));
        var clip = r.Fetch.Answer();
        Assert.AreEqual(new[] { clip }, r.Out.Played, "the fetched clip is played");
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State, "not heard until the watch sees it play");
        Assert.IsFalse(r.Captions);
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Playing, r.Recording.State);
        Assert.AreEqual("Stop the recording", r.Listen.text);
        Assert.IsTrue(r.Captions, "captions while it is heard");

        r.Press();
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.IsFalse(r.Captions);
        Assert.AreEqual("Play the recording", r.Listen.text);
        Assert.IsFalse(r.Out.IsPlaying, "the press stops the audio");
        Assert.IsTrue(clip == null, "the clip is released on stop");
        Assert.IsEmpty(r.Said);
    }

    [Test]
    public void ASlowStartIsWaitedForAndTheEndIsWasPlayingNowIsNot()
    {
        using var r = new Rig();
        r.Out.StartsAtOnce = false;                      // the browser's decode takes a while
        r.Press();
        r.Fetch.Answer();
        r.Ticks(1000);
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State, "a slow first start does not cancel itself");
        Assert.IsFalse(r.Captions);
        LogAssert.Expect(LogType.Log, new Regex("^RECORDING playing "));
        r.Out.IsPlaying = true;
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Playing, r.Recording.State);
        Assert.IsTrue(r.Captions);
        r.Ticks(2000);
        Assert.IsTrue(r.Recording.Playing, "it plays on");
        r.Out.IsPlaying = false;                          // the clip ended
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual("Play the recording", r.Listen.text);
        Assert.IsEmpty(r.Said, "the end is not a failure");
    }

    [Test]
    public void ItPlaysOnlyOnceTheBrowserHasDecodedTheClip()
    {
        using var r = new Rig();
        r.Out.Decoded = false;                             // the Web player decodes it after the fetch
        r.Press();
        r.Fetch.Answer();
        r.Ticks(1000);
        Assert.IsEmpty(r.Out.Played, "not played before its data is decoded");
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State);
        r.Out.Decoded = true;
        LogAssert.Expect(LogType.Log, new Regex("^RECORDING playing "));
        r.Ticks(400);
        Assert.AreEqual(1, r.Out.Played.Count);
        Assert.AreEqual(DeclarationRecording.Phase.Playing, r.Recording.State);
        r.Recording.Stop();
        r.Out.Decoded = null;                              // a clip that fails to decode
        r.Press();
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*did not decode"));
        r.Fetch.Answer();
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(new[] { "The recording could not be played." }, r.Said);
    }

    [Test]
    public void AFailedFetchEndsWithAMessage()
    {
        using var r = new Rig();
        r.Press();
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*404"));
        r.Fetch.Pending!(null, "HTTP/1.1 404 Not Found");
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(new[] { "The recording could not be played." }, r.Said);
        Assert.IsFalse(r.Captions);
        Assert.IsEmpty(r.Out.Played);
    }

    [Test]
    public void ASlowLinkThatKeepsReceivingIsNotCutOff()
    {
        using var r = new Rig();
        r.Press();
        r.TicksReceiving(30000);                          // 30 s at 20 kB/s: far past the stall limit, but never stalled
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State, "still receiving: still loading");
        Assert.AreEqual(0, r.Fetch.Aborts);
        LogAssert.Expect(LogType.Log, new Regex("^RECORDING playing "));
        r.Fetch.Answer();                                 // it arrives; decoding has its own budget from now
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Playing, r.Recording.State);
        r.Recording.Stop();

        r.Press();                                        // a link that never stops trickling meets the overall cap
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*not loaded after 60000 ms"));
        r.TicksReceiving(DeclarationRecording.LoadCapMs);
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(1, r.Said.Count);
    }

    [Test]
    public void ADecodeHasItsOwnBudgetFromTheFileArriving()
    {
        using var r = new Rig();
        r.Out.Decoded = false;
        r.Press();
        r.TicksReceiving(7800);                           // the file lands late
        r.Fetch.Answer();
        r.Ticks(DeclarationRecording.DecodeTimeoutMs - 200);
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State, "a late file still gets the whole decode budget");
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*timed out decoding"));
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
    }

    [Test]
    public void AStalledFetchTimesOutAndAStuckStartToo()
    {
        using var r = new Rig();
        r.Press();
        r.TicksReceiving(1000);
        r.Ticks(DeclarationRecording.StallTimeoutMs - 200);
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State, "still within the stall limit");
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*no data for 8000 ms"));
        r.Ticks(200);
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(1, r.Fetch.Aborts, "the request is aborted");
        Assert.AreEqual(1, r.Said.Count);
        var late = AudioClip.Create("late", 4800, 1, 48000, false);
        r.Fetch.Pending!(late, null);                    // a late answer is dropped and released
        Assert.IsEmpty(r.Out.Played);
        Assert.IsTrue(late == null, "a late clip is released");

        r.Out.StartsAtOnce = false;
        r.Press();
        r.Fetch.Answer();
        LogAssert.Expect(LogType.Warning, new Regex("^RECORDING failed .*did not start"));
        r.Ticks(DeclarationRecording.StartTimeoutMs);
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(2, r.Said.Count);
    }

    [Test]
    public void StoppingWhileLoadingAbortsAndAPressAgainStartsAfresh()
    {
        using var r = new Rig();
        r.Press();
        var first = r.Fetch.Pending!;
        r.Press();                                        // cancel
        Assert.AreEqual(DeclarationRecording.Phase.Idle, r.Recording.State);
        Assert.AreEqual(1, r.Fetch.Aborts);
        r.Press();
        Assert.AreEqual(2, r.Fetch.Urls.Count, "fetched again: the clip is not kept after a stop");
        first(AudioClip.Create("stale", 4800, 1, 48000, false), null);   // the cancelled request answers late
        Assert.IsEmpty(r.Out.Played, "a stale answer never plays");
        Assert.AreEqual(DeclarationRecording.Phase.Loading, r.Recording.State);
    }

    /// <summary>The game's output really calls AudioSource.Play with the clip, and Stop releases it.</summary>
    [Test]
    public void TheGamesOutputPlaysTheClipOnAnAudioSource()
    {
        var output = new DeclarationRecording.SourceOutput();
        var clip = AudioClip.Create("probe", 48000, 1, 48000, false);
        try
        {
            Assert.AreEqual(true, output.Ready(clip), "a clip made in memory is loaded");
            output.Play(clip);
            Assert.IsNotNull(output.Source);
            Assert.AreSame(clip, output.Source!.clip);
            Assert.IsFalse(output.Source.playOnAwake);
            Assert.IsFalse(output.Source.loop);
            Assert.AreEqual(0f, output.Source.spatialBlend);
            Assert.IsTrue(output.Source.isPlaying, "AudioSource.Play was called");
            output.Stop();
            Assert.IsFalse(output.Source.isPlaying);
            Assert.IsNull(output.Source.clip);
        }
        finally { output.Dispose(); UnityEngine.Object.DestroyImmediate(clip); }
    }

    /// <summary>Automated runs skip the start screen, so GameRoot never builds the recording for them (GameSceneTests
    /// checks the booted game); without it the stylesheet keeps the button's row hidden.</summary>
    [Test]
    public void AutomatedRunsSkipTheStartScreenAndTheRowIsHiddenByDefault()
    {
        foreach (var flag in new[] { "golden", "shot", "profile", "vprofile", "units" })
        {
            Assert.IsTrue(StartScreenPresenter.Skipped(null, new[] { "-" + flag }, webgl: false), flag);
            Assert.IsTrue(StartScreenPresenter.Skipped("https://x/?" + flag, new string[0], webgl: true), flag);
        }
        StringAssert.IsMatch(@"\.start-recording \{ display: none;", File.ReadAllText(UiPaths.Folder + "/Statecraft.uss"),
            "the row is hidden unless DeclarationRecording shows it");
    }

    [Test]
    public void TheHebrewCaptionIsRightToLeftInTheHebrewFace()
    {
        using var ui = new UiTestKit();
        var he = ui.Q<Label>("start-caption-he");
        Assert.AreEqual(LanguageDirection.RTL, he.languageDirection, "Hebrew right to left");
        Assert.IsTrue(he.ClassListContains("start-he"), "Frank Ruhl Libre through the advanced text generator, as the panel's Hebrew");
        // The words themselves are checked against the sources in StartScreenTests.Quoted.
    }
}
