#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>The opening film (docs/event-videos.md, "The opening film"): film.yaml's checks and the owner's open decisions;
/// the film ends, however it ends, in the opening it comes before, which stays the fallback; a missing file goes straight
/// on with no warning or error; Skip shows from the first frame; Hebrew captions wait for their approval; the Settings
/// rows set its volume. AdvisorMediaTests covers the sync it shares with the advisors.</summary>
public class OpeningFilmTests
{
    const string Fixture = @"
auto_play_on_new_game: true
real_recording: false
ai_label: advisor.media_label
picture_host: """"
picture: x.mp4
audio: x.ogg
audio_with_recording: y.ogg
ai_generated: true
sensitive: true
sensitivity_reviewed: false
he_review: pending
cues:
  - { start: 0.0, end: 2.0 }
  - { start: 3.0, end: 5.0 }
";
    static string Strings(string lang) => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/UI/strings." + lang + ".yaml").text;
    static UiText Text(string lang) => UiText.Parse(Strings(lang), "strings." + lang + ".yaml");
    static string Shipped => AssetDatabase.LoadAssetAtPath<TextAsset>(FilmContent.SourcePath).text;
    static FilmContent.Document Data(string change = "", string to = "") =>
        FilmContent.Parse(change.Length == 0 ? Fixture : Fixture.Replace(change, to));

    sealed class Pending : DeclarationRecording.IRequest
    {
        public bool Aborted;
        public ulong Bytes => 0;
        public void Abort() => Aborted = true;
    }

    sealed class Rig : IDisposable
    {
        public readonly UiTestKit Ui = new UiTestKit();
        public readonly AdvisorMediaTests.FakePicture Picture = new AdvisorMediaTests.FakePicture();
        public readonly AdvisorMediaTests.FakeOutput Output = new AdvisorMediaTests.FakeOutput();
        public readonly AdvisorMedia Media;
        public readonly FilmPresenter Film;
        public readonly List<bool> Shown = new List<bool>();
        public readonly Pending Request = new Pending();
        public readonly List<string> Asked = new List<string>();
        public Action<bool>? Answer;
        public int Then;
        /// <param name="found">The check's answer at once, or null to answer later through Answer.</param>
        public Rig(FilmContent.Document? data = null, bool? found = true, string lang = "en")
        {
            var film = data ?? Data();
            bool hebrew = lang == "he" && film.HebrewApproved;
            Media = new AdvisorMedia(Text(hebrew ? "he" : "en"), Picture, null, AdvisorMediaTests.Immediate, Output, f => FilmContent.Url(film, f), FilmContent.PrepareTimeoutMs);
            Film = new FilmPresenter(Ui.Root, film, Text(lang), Shown.Add, Media, (url, answer) =>
            {
                Asked.Add(url);
                if (found != null) { answer(found.Value); return null; }
                Answer = answer;
                return Request;
            });
        }
        public void Play(bool replay = false) => Film.Play(replay, () => Then++);
        public bool Layer => Ui.Q<VisualElement>("film-layer").style.display.value == DisplayStyle.Flex;
        public string Caption => Ui.Q<Label>("film-caption").text;
        public void Running() { Picture.Ready = true; Media.Tick(); }
        public void At(float seconds) { Output.Position = seconds; Media.Tick(); }
        public void Dispose() { Film.Dispose(); Ui.Dispose(); }
    }

    [Test]
    public void TheShippedFileParsesAndItsCuesAreTheStringsInBothLanguages()
    {
        var film = FilmContent.Parse(Shipped);
        Assert.Greater(film.Cues.Count, 0);
        foreach (var lang in new[] { "en", "he" })
        {
            var keys = Regex.Matches(Strings(lang), @"^film\.cue\.(\d+):", RegexOptions.Multiline).Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(1, film.Cues.Count).ToList(), keys, lang + ": one key per cue, in order");
        }
        foreach (var key in new[] { film.AiLabel, FilmContent.RecordingCredit, "film.skip" })
        {
            StringAssert.Contains("\n" + key + ":", Strings("en"));
            StringAssert.Contains("\n" + key + ":", Strings("he"));
        }
    }

    /// <summary>The owner's rulings still open: flipping one is a deliberate change that updates this pin.</summary>
    [Test]
    public void TheShippedFileKeepsTheRulesUntilTheOwnerDecides()
    {
        var film = FilmContent.Parse(Shipped);
        Assert.IsFalse(film.RealRecording, "D-R and docs/declaration-recording.md: the recording never plays on New game");
        Assert.AreEqual(film.Audio, film.SoundFile);
        Assert.IsFalse(film.SensitivityReviewed, "the final file does not exist, so nothing has reviewed it");
        Assert.IsFalse(FilmContent.MayPlay(film, editorPreview: false), "no player build plays an unreviewed film");
        Assert.IsTrue(FilmContent.MayPlay(film, editorPreview: true));
        Assert.IsFalse(film.HebrewApproved, "agents never approve Hebrew");
        Assert.AreEqual("", film.PictureHost, "the picture stays bundled, under the repository's limit, until the owner rules");
    }

    [Test]
    public void TheRecordingChoicePicksTheSoundFile()
    {
        Assert.AreEqual("x.ogg", FilmContent.Row(Data()).Narration["en"]);
        var film = Data("real_recording: false", "real_recording: true");
        Assert.AreEqual("y.ogg", FilmContent.Row(film).Narration["en"]);
        using var rig = new Rig(film);
        StringAssert.Contains(Text("en")[FilmContent.RecordingCredit], rig.Ui.Q<Label>("film-label").text, "the terms ask for the creator and the archive");
    }

    [TestCase("picture: x.mp4", "picture: ../x.mp4")]
    [TestCase("audio: x.ogg", "audio: x.wav")]
    [TestCase("picture_host: \"\"", "picture_host: http://example.com/")]
    [TestCase("picture_host: \"\"", "picture_host: https://example.com")]
    [TestCase("ai_generated: true", "ai_generated: false")]
    [TestCase("he_review: pending", "he_review: done")]
    [TestCase("{ start: 3.0, end: 5.0 }", "{ start: 1.0, end: 5.0 }")]
    [TestCase("{ start: 3.0, end: 5.0 }", "{ start: 5.0, end: 3.0 }")]
    [TestCase("real_recording: false", "unknown_key: false")]
    public void AMalformedFileIsRefused(string change, string to) =>
        Assert.Throws<InvalidOperationException>(() => Data(change, to));

    [Test]
    public void APresentFilmOpensWithSkipAndEndsInTheOpening()
    {
        using var rig = new Rig();
        rig.Play();
        Assert.IsTrue(rig.Layer);
        CollectionAssert.AreEqual(new[] { true }, rig.Shown);
        Assert.IsTrue(rig.Ui.Root.Q("root").ClassListContains(OpeningPresenter.OpenClass), "covers what the opening covers");
        Assert.AreEqual(0, rig.Then);
        rig.Running();
        Assert.IsTrue(rig.Picture.Started && rig.Output.Played, "both in the same tick");
        rig.At(1f);
        Assert.AreEqual(0, rig.Then);
        rig.Output.IsPlaying = false;
        rig.Media.Tick();
        Assert.AreEqual(1, rig.Then, "the sound's end ends the film");
        Assert.IsFalse(rig.Layer);
        Assert.IsFalse(rig.Ui.Root.Q("root").ClassListContains(OpeningPresenter.OpenClass));
        CollectionAssert.AreEqual(new[] { true, false }, rig.Shown);
    }

    [Test]
    public void AMissingFileGoesStraightOnWithoutOpeningAndIsNotAskedForAgain()
    {
        using var rig = new Rig(found: false);
        var lines = new List<LogType>();
        Application.LogCallback heard = (message, trace, type) => lines.Add(type);
        Application.logMessageReceived += heard;
        try { rig.Play(); }
        finally { Application.logMessageReceived -= heard; }
        CollectionAssert.AreEqual(new[] { LogType.Log }, lines, "one plain line: no warning and no error");
        Assert.AreEqual(1, rig.Then, "in the same call: today's route");
        Assert.IsFalse(rig.Layer);
        Assert.IsEmpty(rig.Shown, "the layer never opened");
        Assert.IsFalse(rig.Output.Played || rig.Picture.Loads > 0);
        rig.Play(replay: true);
        Assert.AreEqual(2, rig.Then);
        Assert.AreEqual(1, rig.Asked.Count);
    }

    [Test]
    public void AFileFoundMissingLaterClosesTheLayerAndGoesOn()
    {
        using var rig = new Rig(found: null);
        rig.Play();
        Assert.IsTrue(rig.Layer, "covered while the web's check runs");
        Assert.AreEqual(0, rig.Then);
        rig.Answer!(false);
        Assert.IsFalse(rig.Layer);
        Assert.AreEqual(1, rig.Then);
    }

    [Test]
    public void ACheckThatNeverAnswersGoesOnAndALateAnswerIsIgnored()
    {
        using var rig = new Rig(found: null);
        rig.Play();
        rig.Film.ProbeTimedOut();
        Assert.AreEqual(1, rig.Then);
        rig.Answer!(true);
        Assert.AreEqual(1, rig.Then);
        Assert.IsFalse(rig.Layer || rig.Output.Played);
    }

    [Test]
    public void SkipWorksFromTheFirstFrameBeforeAnythingHasLoaded()
    {
        using var rig = new Rig(found: null);
        rig.Play();
        var skip = rig.Ui.Q<Button>("film-skip");
        Assert.AreEqual(Text("en")["film.skip"], skip.text);
        Assert.IsTrue(rig.Layer && skip.enabledInHierarchy, "shown and usable before anything has answered");
        UiTestKit.Press(skip);
        Assert.AreEqual(1, rig.Then);
        Assert.IsTrue(rig.Request.Aborted);
        rig.Answer!(true);
        Assert.IsFalse(rig.Output.Played, "a late answer does not start a skipped film");
        UiTestKit.Press(skip);
        Assert.AreEqual(1, rig.Then, "once");
    }

    [Test]
    public void SkipStopsBothStreams()
    {
        using var rig = new Rig();
        rig.Play();
        rig.Running();
        UiTestKit.Press(rig.Ui.Q<Button>("film-skip"));
        Assert.IsTrue(rig.Picture.Stopped && rig.Output.Stopped);
        Assert.AreEqual(1, rig.Then);
    }

    [Test]
    public void EscapeClosesTheFilmOnly()
    {
        using var rig = new Rig();
        Assert.IsFalse(rig.Film.Escape(), "closed: it takes no press");
        rig.Play();
        Assert.IsTrue(rig.Film.Escape());
        Assert.AreEqual(1, rig.Then);
        Assert.IsTrue(rig.Film.ClosedThisFrame, "the press is used up: GameRoot does not also close the stills");
        Assert.IsTrue(rig.Film.HoldsThePointer);
    }

    [Test]
    public void AFailedFilmGoesOnWithOneLogLine()
    {
        using var rig = new Rig();
        rig.Play();
        rig.Output.Decoded = false;
        LogAssert.Expect(LogType.Warning, new Regex("^ADVISOR MEDIA failed x\\.mp4"));
        rig.Picture.Ready = true;
        for (int i = 0; i * AdvisorMedia.TickMs <= DeclarationRecording.DecodeTimeoutMs && rig.Then == 0; i++) rig.Media.Tick();
        Assert.AreEqual(1, rig.Then);
        Assert.IsFalse(rig.Layer);
    }

    [Test]
    public void AutoPlayGovernsNewGameOnly()
    {
        using var rig = new Rig(Data("auto_play_on_new_game: true", "auto_play_on_new_game: false"));
        rig.Play();
        Assert.AreEqual(1, rig.Then);
        Assert.IsEmpty(rig.Asked, "not even a check");
        rig.Play(replay: true);
        Assert.IsTrue(rig.Layer, "the replay button still plays it");
    }

    [Test]
    public void CaptionsFollowTheSoundAndBlankInTheGaps()
    {
        using var rig = new Rig();
        rig.Play();
        rig.Running();
        rig.At(1f);
        Assert.AreEqual(Text("en")["film.cue.1"], rig.Caption);
        rig.At(2.5f);
        Assert.AreEqual("", rig.Caption, "after a cue's end, before the next start");
        rig.At(3.5f);
        Assert.AreEqual(Text("en")["film.cue.2"], rig.Caption);
        rig.At(6f);
        Assert.AreEqual("", rig.Caption);
    }

    [Test]
    public void TheHebrewUiShowsEnglishCaptionsUntilTheHebrewIsApproved()
    {
        using (var pending = new Rig(lang: "he"))
        {
            pending.Play(); pending.Running(); pending.At(1f);
            Assert.AreEqual(Text("en")["film.cue.1"], pending.Caption);
            Assert.AreEqual(LanguageDirection.LTR, pending.Ui.Q<Label>("film-caption").languageDirection);
        }
        using var approved = new Rig(Data("he_review: pending", "he_review: approved"), lang: "he");
        approved.Play(); approved.Running(); approved.At(1f);
        Assert.AreEqual(Text("he")["film.cue.1"], approved.Caption);
        Assert.AreEqual(LanguageDirection.RTL, approved.Ui.Q<Label>("film-caption").languageDirection);
    }

    /// <summary>The Settings rows: off is 0 (SettingsPresenter.SoundLevels). Silent only when both are off.</summary>
    [TestCase(0f, 0f, 0f)]
    [TestCase(1f, 0f, 1f)]
    [TestCase(0f, 0.35f, 0.35f)]
    [TestCase(0.35f, 1f, 1f)]
    public void TheVolumeComesFromTheSettingsRows(float sound, float music, float expected) =>
        Assert.AreEqual(expected, FilmContent.Volume(sound, music), 0.0001f);

    [Test]
    public void TheGamesOutputPlaysAtTheVolumeItIsGiven()
    {
        var output = new DeclarationRecording.SourceOutput { Volume = 0f };
        var clip = AudioClip.Create("probe", 48000, 1, 48000, false);
        try
        {
            output.Play(clip);
            Assert.AreEqual(0f, output.Source!.volume, "muted before it starts");
            Assert.IsTrue(output.Source.isPlaying, "a muted film still runs: its clock drives the captions");
            output.Volume = 0.35f;
            Assert.AreEqual(0.35f, output.Source.volume, 0.0001f);
        }
        finally { output.Dispose(); UnityEngine.Object.DestroyImmediate(clip); }
    }

    [Test]
    public void ACheckOnDiskFindsTheFile()
    {
        var file = Path.GetTempFileName();
        try
        {
            bool? found = null;
            Assert.IsNull(FilmPresenter.Exists("file://" + file, f => found = f));
            Assert.AreEqual(true, found);
            Assert.IsNull(FilmPresenter.Exists("file://" + file + ".none", f => found = f));
            Assert.AreEqual(false, found);
        }
        finally { File.Delete(file); }
    }

    /// <summary>Picture and sound each stay under the repository's limit unless the owner rules otherwise (film.yaml): mirrors
    /// check_repo.py's size check. Passes while the files do not exist.</summary>
    [Test]
    public void BundledFilesStayUnderTheRepositoryLimit()
    {
        var film = FilmContent.Parse(Shipped);
        foreach (var path in new[] { FilmContent.PictureFolder + "/" + film.Picture, FilmContent.AudioFolder + "/" + film.Audio, FilmContent.AudioFolder + "/" + film.AudioWithRecording })
        {
            var full = Path.Combine(Application.streamingAssetsPath, path);
            if (File.Exists(full)) Assert.LessOrEqual(new FileInfo(full).Length, FilmContent.BundledLimitBytes, path);
        }
    }
}
