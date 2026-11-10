#nullable enable
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.Video;

/// <summary>Issue #2564: CardVideo reuses itself for the next clip (card to next card, outcome to next outcome), so a
/// prepareCompleted or errorReceived that the previous clip's player sends late must not prepare, start or fail the clip
/// playing now. The callbacks are fired by hand (reflection, as EventVideoTests and OpeningClipTests reach Fail): the
/// edit-mode VideoPlayer's own timing is not under test, and an EditMode run cannot make a real player answer late.</summary>
public class CardVideoStaleCallbackTests
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    static VideoPlayer PlayerOf(CardVideo video) => (VideoPlayer)typeof(CardVideo).GetField("player", Flags)!.GetValue(video)!;
    static void Prepared(CardVideo video, VideoPlayer p) =>
        typeof(CardVideo).GetMethod("OnPrepared", Flags)!.Invoke(video, new object[] { p });
    static void Error(CardVideo video, VideoPlayer p, string message) =>
        typeof(CardVideo).GetMethod("OnError", Flags)!.Invoke(video, new object[] { p, message });

    bool quiet;
    [SetUp] public void QuietVideoPlayer() { quiet = LogAssert.ignoreFailingMessages; LogAssert.ignoreFailingMessages = true; }   // the edit-mode VideoPlayer may log while it prepares
    [TearDown] public void Restore() => LogAssert.ignoreFailingMessages = quiet;

    [Test]
    public void ALatePrepareFromThePreviousClipDoesNotPrepareTheNextOne()
    {
        using var video = new CardVideo();
        var header = new VisualElement();
        video.Play("a.mp4", header, _ => { });
        var first = PlayerOf(video);
        video.Play("b.mp4", header, _ => { }, held: true);   // held: a prepare only marks it Prepared, nothing starts

        Prepared(video, first);
        Assert.IsFalse(video.Prepared, "a.mp4's late prepareCompleted must not mark b.mp4 prepared");
        Assert.IsFalse(video.Showing);
        Assert.AreEqual("b.mp4", video.Current);

        Prepared(video, PlayerOf(video));
        Assert.IsTrue(video.Prepared, "b.mp4's own prepareCompleted still prepares it");
    }

    [Test]
    public void ALateErrorFromThePreviousClipDoesNotFailTheNextOne()
    {
        using var video = new CardVideo();
        var header = new VisualElement();
        int aFailed = 0, bFailed = 0; string? bWhy = null;
        video.Play("a.mp4", header, _ => aFailed++);
        var first = PlayerOf(video);
        video.Play("b.mp4", header, why => { bFailed++; bWhy = why; });

        Error(video, first, "a.mp4 broke");
        Assert.AreEqual(0, bFailed, "a.mp4's late errorReceived must not fail b.mp4");
        Assert.AreEqual(0, aFailed, "a.mp4 was stopped: nobody is told about it");
        Assert.AreEqual("b.mp4", video.Current, "b.mp4 keeps playing");

        Error(video, PlayerOf(video), "b.mp4 broke");
        Assert.AreEqual(1, bFailed, "b.mp4's own errorReceived still fails it, once");
        Assert.AreEqual("b.mp4 broke", bWhy);
        Assert.IsNull(video.Current);
    }
}
