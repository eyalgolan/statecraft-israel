#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>#1170: sharing a post with a screenshot. The post text in both languages with the in-game date and turn, the
/// share links' encoding, the 1920 px cap on the picture, and the panel: it captures before it shows, previews the picture,
/// offers the browser's share sheet where files can be shared and otherwise the download, and the site buttons (#1237):
/// share links, and Instagram, which has none. Through ShareShot's injectable hooks, so nothing is captured, saved or
/// opened for real.</summary>
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
        Assert.AreEqual("https://t.me/share/url?url=" + url + "&text=" + enc, ShareShot.IntentUrl("telegram", text));
        Assert.AreEqual("https://www.linkedin.com/sharing/share-offsite/?url=" + url, ShareShot.IntentUrl("linkedin", text));
        StringAssert.Contains("a%20%26%20b%20%231", enc, "'&' and '#' never end the query early");
    }

    /// <summary>#1237: the owner's list, in the owner's order; LinkedIn stays, at the end (owner, 21 Oct 2026). Instagram has
    /// no web share link.</summary>
    [Test]
    public void TheSitesAreTheOwnersListInOrder()
    {
        CollectionAssert.AreEqual(new[] { "x", "facebook", "instagram", "whatsapp", "telegram", "linkedin" }, ShareShot.Sites);
        Assert.Throws<ArgumentException>(() => ShareShot.IntentUrl("instagram", "a"), "Instagram has no share link");
        using var ui = new UiTestKit();
        var row = ui.Q<VisualElement>("share-x").parent;
        CollectionAssert.AreEqual(Array.ConvertAll(ShareShot.Sites, s => "share-" + s), row.Children().Select(c => c.name).ToArray(),
            "the panel shows the buttons in the list's order");
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
    /// button downloads the picture and the "attach" note shows. The site buttons show either way. Esc closes it.</summary>
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
            Assert.AreEqual(DisplayStyle.None, fallback.style.display.value, "no attach note before the press");
            for (VisualElement? e = ui.Q<Button>("share-x"); e != null && e.name != "share-panel"; e = e.parent)
                Assert.AreNotEqual(DisplayStyle.None, e.style.display.value, $"the site buttons show from the start ({e.name})");
            Assert.AreEqual(canShareFiles ? text["share.native"] : text["share.download"], ui.Q<Button>("share-go").text);
            SettingsScreenTests.PressWithPointer(ui.Q<Button>("share-go"));
            CollectionAssert.AreEqual(new[] { post + " | " + ShareShot.Itch }, shared, "the press asks the browser once");
            Assert.AreEqual(canShareFiles ? DisplayStyle.None : DisplayStyle.Flex, fallback.style.display.value,
                "the attach note only where files cannot be shared");
            var links = ShareShot.Sites.Where(s => s != "instagram").ToArray();
            foreach (var site in links) UiTestKit.Press(ui.Q<Button>("share-" + site));
            CollectionAssert.AreEqual(links.Select(s => ShareShot.IntentUrl(s, post)), opened);
            Assert.AreEqual(text["share.attach"], ui.Q<Label>("share-attach").text);
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

    /// <summary>#1237: Instagram has no share link. Where the browser can share files its button opens the share sheet with
    /// the picture, as Share does; elsewhere the same press downloads the picture and a note says to post it from the
    /// Instagram app. On the press, as Share (the web build arms its one-shot release listener then); nothing opens before.</summary>
    [Test]
    public void InstagramOpensTheSheetOrDownloadsWithANote([Values(false, true)] bool canShareFiles, [Values(false, true)] bool hebrew,
        [Values(false, true)] bool keyboard)
    {
        var text = hebrew ? He : En;
        var opened = new List<string>(); var shared = new List<string>();
        ExternalLink.Opener = opened.Add;
        ShareShot.Keep = (png, name) => { };
        ShareShot.CanShareFiles = () => canShareFiles;
        ShareShot.Share = (post, url) => shared.Add(post + " | " + url);
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), text);
            using var share = new SharePresenter(ui.Root, bridge, text, done => done(new byte[] { 1 }));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            var button = ui.Q<Button>("share-instagram");
            var note = ui.Q<Label>("share-instagram-note");
            Assert.AreEqual("Instagram", button.text, "a brand, the same in every language");
            Assert.AreEqual(DisplayStyle.None, note.style.display.value, "no note before the press");
            CollectionAssert.IsEmpty(shared, "nothing is shared before the press");
            if (keyboard) UiTestKit.Press(button); else SettingsScreenTests.PressWithPointer(button);
            var post = ui.Q<Label>("share-post").text;
            CollectionAssert.AreEqual(new[] { post + " | " + ShareShot.Itch }, shared, "the share sheet, or the download, once");
            CollectionAssert.IsEmpty(opened, "no web page: Instagram has no share link");
            Assert.AreEqual(canShareFiles ? DisplayStyle.None : DisplayStyle.Flex, note.style.display.value,
                "the note only where the picture downloaded");
            Assert.AreEqual(text["share.instagram"], note.text);
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("share-fallback").style.display.value, "not the other sites' note");
        }
        finally
        {
            ExternalLink.Opener = ExternalLink.Default;
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.CanShareFiles = ShareShot.DefaultCanShareFiles; ShareShot.Share = ShareShot.DefaultShare;
        }
    }

    /// <summary>#1237: the panel previews the picture it shares, from the captured PNG. The texture is released when the
    /// panel closes, so none is left over between opens; a PNG that does not decode shows no preview.</summary>
    [Test]
    public void ThePanelPreviewsThePictureAndReleasesItOnClose()
    {
        ShareShot.Keep = (png, name) => { };
        var frame = new Texture2D(64, 36, TextureFormat.RGB24, false);
        var real = frame.EncodeToPNG();
        Object.DestroyImmediate(frame);
        byte[] png = real;
        try
        {
            using var ui = new UiTestKit();
            var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
            var share = new SharePresenter(ui.Root, bridge, En, done => done(png));
            var preview = ui.Q<VisualElement>("share-preview");
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            var first = preview.style.backgroundImage.value.texture;
            Assert.IsNotNull(first, "the preview shows the picture");
            Assert.AreEqual(64, first.width); Assert.AreEqual(36, first.height);
            Assert.AreEqual(DisplayStyle.Flex, preview.style.display.value);
            UiTestKit.Press(ui.Q<Button>("share-close"));
            Assert.IsTrue(first == null, "released on Close");
            Assert.IsNull(preview.style.backgroundImage.value.texture);
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            var second = preview.style.backgroundImage.value.texture;
            Assert.IsNotNull(second, "a new picture on the next open");
            share.Dispose();
            Assert.IsTrue(second == null, "released when the UI is rebuilt with the panel open");
            png = new byte[] { 1, 2, 3 };
            using var again = new SharePresenter(ui.Root, bridge, En, done => done(png));
            UiTestKit.Press(ui.Q<Button>("settings-share"));
            Assert.IsTrue(again.IsOpen, "the panel still opens");
            Assert.AreEqual(DisplayStyle.None, preview.style.display.value, "no preview for a picture that does not decode");
            Assert.IsNull(preview.style.backgroundImage.value.texture);
        }
        finally { ShareShot.Keep = ShareShot.DefaultKeep; }
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
    /// note to attach it shows (ShareShot.Failed, polled by the panel's scheduler). A failed capture opens nothing and leaves
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
            Assert.AreEqual(DisplayStyle.None, fallback.style.display.value, "the sheet is open: no note yet");
            failed = true;
            var until = Time.realtimeSinceStartup + 5f;   // the panel looks every 250 ms
            while (fallback.style.display.value != DisplayStyle.Flex && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, fallback.style.display.value, "refused: the attach note shows");
        }
        finally
        {
            ui.Dispose();
            ShareShot.Keep = ShareShot.DefaultKeep; ShareShot.CanShareFiles = ShareShot.DefaultCanShareFiles;
            ShareShot.Share = ShareShot.DefaultShare; ShareShot.Failed = ShareShot.DefaultFailed;
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
