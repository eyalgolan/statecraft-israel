#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#1170: sharing a post with a screenshot. The post text in both languages with the in-game date and turn, the
/// share links' encoding, the 1920 px cap on the picture, and the panel: it captures before it shows, offers the browser's
/// share sheet where files can be shared and otherwise the download with the four share links. Through ShareShot's
/// injectable hooks, so nothing is captured, saved or opened for real.</summary>
public class ShareShotTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [Test]
    public void ThePostNamesTheGameTheDateTheTurnAndTheItchPage()
    {
        Assert.AreEqual("I'm running Israel in 1948 in Statecraft: Israel, a free open-source strategy game. 15 May 1948, turn 0. "
            + "Play: https://eyalgolan.itch.io/statecraft-israel", ShareShot.Text(En, "15 May 1948", 0));
        var he = ShareShot.Text(He, "15 במאי 1948", 3);
        StringAssert.Contains("15 במאי 1948", he);
        StringAssert.Contains("3", he);
        StringAssert.EndsWith("https://eyalgolan.itch.io/statecraft-israel", he);
        Assert.IsTrue(UiDirection.HasRightToLeft(he), "the Hebrew post is Hebrew");
    }

    [Test]
    public void TheShareLinksCarryTheTextAndTheUrlEncoded()
    {
        const string text = "Play: a & b #1, 15 במאי";
        var enc = Uri.EscapeDataString(text); var url = Uri.EscapeDataString(ShareShot.Itch);
        Assert.AreEqual("https://x.com/intent/post?text=" + enc, ShareShot.IntentUrl("x", text));
        Assert.AreEqual("https://www.facebook.com/sharer/sharer.php?u=" + url, ShareShot.IntentUrl("facebook", text));
        Assert.AreEqual("https://wa.me/?text=" + enc, ShareShot.IntentUrl("whatsapp", text));
        Assert.AreEqual("https://www.linkedin.com/sharing/share-offsite/?url=" + url, ShareShot.IntentUrl("linkedin", text));
        StringAssert.Contains("a%20%26%20b%20%231", enc, "'&' and '#' never end the query early");
    }

    [Test]
    public void ThePictureIsCappedAt1920OnTheLongSide()
    {
        Assert.AreEqual(new Vector2Int(1280, 720), ShareShot.Fit(1280, 720));
        Assert.AreEqual(new Vector2Int(1920, 1080), ShareShot.Fit(3840, 2160));
        Assert.AreEqual(new Vector2Int(887, 1920), ShareShot.Fit(1170, 2532), "a phone held upright, DPR 3");
        var big = new Texture2D(3000, 1000, TextureFormat.RGB24, false);
        try
        {
            var png = ShareShot.Encode(big);
            var back = new Texture2D(2, 2);
            Assert.IsTrue(back.LoadImage(png));
            Assert.AreEqual(1920, back.width); Assert.AreEqual(640, back.height);
            Object.DestroyImmediate(back);
        }
        finally { Object.DestroyImmediate(big); }
    }

    [Test]
    public void TheDownloadIsNamedForTheInGameDate()
    {
        Assert.AreEqual("statecraft-1948-05-15.png", ShareShot.FileName(new DateTime(1948, 5, 15)));
    }

    /// <summary>The Settings Share button: the frame is captured before the panel shows, then kept for the browser; where it
    /// can share files the panel offers Share (the share sheet gets the post text and the itch URL), elsewhere the same
    /// button downloads the picture and the four share links and the "attach" note show. Esc closes it.</summary>
    [Test]
    public void SharingCapturesFirstAndOffersTheSheetOrTheFallback([Values(false, true)] bool canShareFiles, [Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var opened = new List<string>(); var kept = new List<string>(); var shared = new List<string>();
        bool shownAtCapture = true;
        ExternalLink.Opener = opened.Add;
        ShareShot.Keep = (png, name) => kept.Add(name);
        ShareShot.CanShareFiles = () => canShareFiles;
        ShareShot.Share = (post, url) => shared.Add(post + " | " + url);
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            SharePresenter? made = null;
            using var share = made = new SharePresenter(ui.Root, bridge, text, done =>
            {
                shownAtCapture = made!.IsOpen;
                done(new byte[] { 1, 2, 3 });
            });
            var button = ui.Q<Button>("settings-share");
            Assert.AreEqual(text["share.open"], button.text);
            UiTestKit.Press(button);
            Assert.IsFalse(shownAtCapture, "the panel is not in the picture");
            Assert.IsTrue(share.IsOpen);
            CollectionAssert.AreEqual(new[] { "statecraft-1948-05-15.png" }, kept);
            var post = ShareShot.Text(text, HudModel.From(bridge, text).Date, bridge.State.Turn);
            Assert.AreEqual(post, ui.Q<Label>("share-post").text, "the panel shows the post");
            Assert.IsFalse(ui.Q<VisualElement>("screen-settings").enabledSelf, "out of Tab's reach under the panel");
            var fallback = ui.Q<VisualElement>("share-fallback");
            Assert.AreEqual(DisplayStyle.None, fallback.style.display.value, "no share links before the press");
            Assert.AreEqual(canShareFiles ? text["share.native"] : text["share.download"], ui.Q<Button>("share-go").text);
            SettingsScreenTests.PressWithPointer(ui.Q<Button>("share-go"));
            CollectionAssert.AreEqual(new[] { post + " | " + ShareShot.Itch }, shared, "the press asks the browser once");
            Assert.AreEqual(canShareFiles ? DisplayStyle.None : DisplayStyle.Flex, fallback.style.display.value,
                "the share links and the attach note only where files cannot be shared");
            if (!canShareFiles)
            {
                foreach (var site in ShareShot.Sites) UiTestKit.Press(ui.Q<Button>("share-" + site));
                CollectionAssert.AreEqual(Array.ConvertAll(ShareShot.Sites, s => ShareShot.IntentUrl(s, post)), opened);
                Assert.AreEqual(text["share.attach"], ui.Q<Label>("share-attach").text);
            }
            var go = ui.Q<Button>("share-go");
            using (var esc = NavigationCancelEvent.GetPooled()) { esc.target = go; go.SendEvent(esc); }
            Assert.IsFalse(share.IsOpen, "Esc closes it");
            Assert.IsTrue(ui.Q<VisualElement>("screen-settings").enabledSelf);
        }
        finally
        {
            ExternalLink.Opener = ExternalLink.Default;
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.CanShareFiles = ShareShot.DefaultCanShareFiles; ShareShot.Share = ShareShot.DefaultShare;
        }
    }

    /// <summary>With the screens' navigation wired as GameRoot wires it, Esc in the share panel closes the panel only:
    /// ScreenNav's Cancel handler trickles down from the root before the panel's own and asks the panel first.</summary>
    [Test]
    public void EscapeInTheSharePanelLeavesTheSettingsScreenOpen()
    {
        ShareShot.Keep = (png, name) => { };
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
            using var nav = new ScreenNav(ui.Root);
            using var share = new SharePresenter(ui.Root, bridge, En, done => done(new byte[] { 1 }));
            nav.TakesEscape = share.Escape;
            nav.Open("settings");
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            Assert.IsTrue(share.IsOpen);
            var go = ui.Q<Button>("share-go");
            using (var esc = NavigationCancelEvent.GetPooled()) { esc.target = go; go.SendEvent(esc); }
            Assert.IsFalse(share.IsOpen, "Esc closes the panel");
            Assert.AreEqual("settings", nav.Current, "and only the panel");
        }
        finally { ShareShot.Keep = ShareShot.DefaultKeep; }
    }

    /// <summary>A share sheet the browser refused on the release (not a cancel): the picture downloaded instead, and the
    /// share links show (ShareShot.Failed, polled by the panel's scheduler). A failed capture opens nothing and leaves
    /// Share working.</summary>
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator ARefusedShareSheetShowsTheShareLinks()
    {
        bool failed = false, broken = true;
        ShareShot.Keep = (png, name) => { }; ShareShot.CanShareFiles = () => true; ShareShot.Share = (a, b) => { };
        ShareShot.Failed = () => failed;
        var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
            using var share = new SharePresenter(ui.Root, bridge, En, done => done(broken ? null : new byte[] { 1 }));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            Assert.IsFalse(share.IsOpen, "no picture, no panel");
            broken = false;
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            Assert.IsTrue(share.IsOpen, "the next press works");
            SettingsScreenTests.PressWithPointer(ui.Q<Button>("share-go"));
            var fallback = ui.Q<VisualElement>("share-fallback");
            Assert.AreEqual(DisplayStyle.None, fallback.style.display.value, "the sheet is open: no links yet");
            failed = true;
            var until = Time.realtimeSinceStartup + 5f;   // the panel looks every 250 ms
            while (fallback.style.display.value != DisplayStyle.Flex && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, fallback.style.display.value, "refused: the share links show");
        }
        finally
        {
            ui.Dispose();
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.CanShareFiles = ShareShot.DefaultCanShareFiles;
            ShareShot.Share = ShareShot.DefaultShare; ShareShot.Failed = ShareShot.DefaultFailed;
        }
    }

    /// <summary>#1238: Copy asks the browser once, on the player's press, to copy the picture; the browser falls back to the
    /// post text (with the itch link) and reports which one landed, or that neither did (ShareShot.jslib, in that order).
    /// The panel says which, in both languages, polling ShareShot.Copied as it polls Failed; nothing shows while the copy
    /// is still pending, and on a failure the Share or Download button stays where it was. The order itself (picture, then
    /// text, then the error) lives in the jslib's promises, which an edit-mode test cannot run; the PR's browser check
    /// covers it.</summary>
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator CopySaysWhetherThePictureOrTheTextWasCopied(
        [Values(ShareShot.CopyResult.Picture, ShareShot.CopyResult.Text, ShareShot.CopyResult.Failed)] ShareShot.CopyResult result,
        [Values(false, true)] bool hebrew)
    {
        var text = hebrew ? He : En;
        var copies = new List<string>();
        var copied = ShareShot.CopyResult.Pending;
        ShareShot.Keep = (png, name) => { }; ShareShot.Share = (a, b) => { };
        ShareShot.Copy = copies.Add; ShareShot.Copied = () => copied;
        var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            using var share = new SharePresenter(ui.Root, bridge, text, done => done(new byte[] { 1 }));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            var copy = ui.Q<Button>("share-copy");
            var said = ui.Q<Label>("share-copied");
            Assert.IsNotNull(copy, "the panel has a Copy button");
            Assert.AreEqual(text["share.copy"], copy.text);
            Assert.AreEqual(DisplayStyle.None, said.style.display.value, "nothing to say before the press");
            SettingsScreenTests.PressWithPointer(copy);
            CollectionAssert.AreEqual(new[] { ui.Q<Label>("share-post").text }, copies, "one copy, of the post, on the press");
            // The batch editor idles between frames unless asked for one, and the panel's scheduler runs on those frames.
            var until = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < until) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            Assert.AreEqual(DisplayStyle.None, said.style.display.value, "still pending after a few looks: nothing yet");
            copied = result;
            until = Time.realtimeSinceStartup + 4f;   // the panel looks every 250 ms
            while (said.style.display.value != DisplayStyle.Flex && Time.realtimeSinceStartup < until) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            Assert.AreEqual(DisplayStyle.Flex, said.style.display.value);
            var key = result switch { ShareShot.CopyResult.Picture => "share.copied_picture", ShareShot.CopyResult.Text => "share.copied_text", _ => "share.copy_failed" };
            Assert.AreEqual(text[key], said.text);
            var go = ui.Q<Button>("share-go");
            Assert.IsTrue(go.enabledInHierarchy && go.style.display.value != DisplayStyle.None, "the Share or Download button stays available");
            UiTestKit.Press(ui.Q<Button>("share-close"));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            Assert.AreEqual(DisplayStyle.None, said.style.display.value, "a new picture, a new copy: the old message goes");
        }
        finally
        {
            ui.Dispose();
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.Share = ShareShot.DefaultShare;
            ShareShot.Copy = ShareShot.DefaultCopy; ShareShot.Copied = ShareShot.DefaultCopied;
        }
    }

    /// <summary>A Copy press whose release never reached the browser stays Pending; after a few seconds the panel says
    /// the copy failed instead of staying silent.</summary>
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator ACopyThatNeverAnswersSaysItFailed()
    {
        ShareShot.Keep = (png, name) => { };
        ShareShot.Copy = t => { }; ShareShot.Copied = () => ShareShot.CopyResult.Pending;
        var ui = new UiTestKit();
        try
        {
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
            using var share = new SharePresenter(ui.Root, bridge, En, done => done(new byte[] { 1 }));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            SettingsScreenTests.PressWithPointer(ui.Q<Button>("share-copy"));
            var said = ui.Q<Label>("share-copied");
            var until = Time.realtimeSinceStartup + 7f;
            while (said.style.display.value != DisplayStyle.Flex && Time.realtimeSinceStartup < until) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            Assert.AreEqual(DisplayStyle.Flex, said.style.display.value);
            Assert.AreEqual(En["share.copy_failed"], said.text);
        }
        finally
        {
            ui.Dispose();
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.Copy = ShareShot.DefaultCopy; ShareShot.Copied = ShareShot.DefaultCopied;
        }
    }

    /// <summary>The turn report's Share opens the same panel over the report.</summary>
    [Test]
    public void TheTurnReportHasAShareButton()
    {
        ShareShot.Keep = (png, name) => { };
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
            using var share = new SharePresenter(ui.Root, bridge, En, done => done(new byte[] { 1 }));
            Assert.AreEqual(En["share.open"], ui.Q<Button>("turn-report-share").text);
            UiTestKit.Press(ui.Q<Button>("turn-report-share"));
            Assert.IsTrue(share.IsOpen);
            UiTestKit.Press(ui.Q<Button>("share-close"));
            Assert.IsFalse(share.IsOpen);
        }
        finally { ShareShot.Keep = ShareShot.DefaultKeep; }
    }
}
