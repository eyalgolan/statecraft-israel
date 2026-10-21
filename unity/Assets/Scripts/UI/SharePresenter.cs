#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Share" (#1170): the button in the Settings screen's Community row and the one on the turn report capture the
    /// frame, then open a small panel over everything with the post (ShareShot.Text) and one Share button. The capture runs
    /// before the panel shows, so the panel is never in the picture. Where the browser can share files, Share opens its share
    /// sheet with the picture, the post and the itch URL; elsewhere the same button downloads the picture, and the panel then
    /// shows the X, Facebook, WhatsApp and LinkedIn share links and says to attach the picture, which a link cannot carry.
    /// Copy (#1238) puts the picture on the clipboard, or the post text where the browser refuses the picture, and says which.
    /// <para>While it shows, the root has <see cref="OpenClass"/> and every other part of the UI is disabled, so Tab stays in
    /// the panel; Close, Esc or Cancel give each part its own state back and the focus back to the button that opened it.
    /// <see cref="Escape"/> is what GameRoot's Escape poll and ScreenNav (TakesEscape) ask first.</para></summary>
    public sealed class SharePresenter : IDisposable
    {
        /// <summary>On the "root" element while the panel shows.</summary>
        public const string OpenClass = "share-open";
        /// <summary>The share links' names: brands, the same in every language.</summary>
        static readonly Dictionary<string, string> SiteNames = new Dictionary<string, string>
            { ["x"] = "X", ["facebook"] = "Facebook", ["whatsapp"] = "WhatsApp", ["linkedin"] = "LinkedIn" };

        readonly VisualElement hudRoot, layer, fallback;
        readonly Button go, copy, close, settingsOpen, reportOpen;
        readonly Label post, copied;
        readonly SimBridge bridge; readonly UiText text;
        readonly Action<Action<byte[]?>> capture;
        readonly EventCallback<PointerDownEvent> onGoPointer;
        readonly EventCallback<NavigationSubmitEvent> onGoSubmit;
        readonly EventCallback<PointerDownEvent> onCopyPointer;
        readonly EventCallback<NavigationSubmitEvent> onCopySubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly List<Action> unbindSites = new List<Action>();
        readonly List<(VisualElement part, bool enabled)> covered = new List<(VisualElement, bool)>();
        Button? opener;
        IVisualElementScheduledItem? copyPoll;
        bool capturing, canShareFiles, disposed;
        int closedAt = -1;

        /// <param name="capture">Captures the frame at its end and hands over its PNG (GameRoot: ShareShot.Capture as a
        /// coroutine; a test hands over bytes at once).</param>
        public SharePresenter(VisualElement root, SimBridge bridge, UiText text, Action<Action<byte[]?>> capture)
        {
            this.bridge = bridge; this.text = text; this.capture = capture;
            hudRoot = root.Q("root") ?? root;
            layer = root.Q("share-layer");
            fallback = layer.Q("share-fallback");
            go = layer.Q<Button>("share-go"); close = layer.Q<Button>("share-close");
            post = layer.Q<Label>("share-post");
            copy = layer.Q<Button>("share-copy"); copied = layer.Q<Label>("share-copied");
            copy.text = text["share.copy"];
            settingsOpen = root.Q<Button>("settings-share"); reportOpen = root.Q<Button>("turn-report-share");
            settingsOpen.text = reportOpen.text = text["share.open"];
            layer.Q<Label>("share-title").text = text["share.title"];
            layer.Q<Label>("share-attach").text = text["share.attach"];
            close.text = text["report.close"];
            foreach (var site in ShareShot.Sites) layer.Q<Button>("share-" + site).text = SiteNames[site];
            fallback.style.display = DisplayStyle.None;
            // On the press, not on `clicked` (the release): the web build arms its release listener then (ExternalLink.Bind).
            onGoPointer = e => { if (e.button == 0) Go(); };
            onGoSubmit = _ => Go();
            go.RegisterCallback(onGoPointer, TrickleDown.TrickleDown);
            go.RegisterCallback(onGoSubmit, TrickleDown.TrickleDown);
            onCopyPointer = e => { if (e.button == 0) Copy(); };
            onCopySubmit = _ => Copy();
            copy.RegisterCallback(onCopyPointer, TrickleDown.TrickleDown);
            copy.RegisterCallback(onCopySubmit, TrickleDown.TrickleDown);
            settingsOpen.clicked += OpenFromSettings;
            reportOpen.clicked += OpenFromReport;
            close.clicked += Close;
            onCancel = e => { Close(); e.StopPropagation(); };
            layer.RegisterCallback(onCancel);
        }

        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        void OpenFromSettings() => Open(settingsOpen);
        void OpenFromReport() => Open(reportOpen);

        /// <summary>Captures the frame, keeps its PNG for the browser, then shows the panel; <paramref name="from"/> gets the
        /// focus back on Close.</summary>
        public void Open(Button? from = null)
        {
            if (IsOpen || capturing) return;
            capturing = true;
            capture(png =>
            {
                capturing = false;
                if (png == null || disposed) return;   // no picture (ShareShot.Capture logged why), or the UI was rebuilt meanwhile
                var shown = HudModel.From(bridge, text);
                ShareShot.Keep(png, ShareShot.FileName(bridge.State.Date));
                canShareFiles = ShareShot.CanShareFiles();
                post.text = ShareShot.Text(text, shown.Date, bridge.State.Turn);
                go.text = text[canShareFiles ? "share.native" : "share.download"];
                fallback.style.display = DisplayStyle.None;
                copyPoll?.Pause();
                copied.style.display = DisplayStyle.None;
                foreach (var unbind in unbindSites) unbind();
                unbindSites.Clear();
                foreach (var site in ShareShot.Sites) unbindSites.Add(ExternalLink.Bind(layer.Q<Button>("share-" + site), ShareShot.IntentUrl(site, post.text)));
                opener = from;
                // Each part's own state comes back on Close. The panel takes every press meanwhile, so no screen opens or
                // closes under it; a resize may still run ScreenNav.DisableCovered, and Close then puts back the states from before.
                covered.Clear();
                foreach (var part in hudRoot.Children().Where(c => c != layer)) { covered.Add((part, part.enabledSelf)); part.SetEnabled(false); }
                hudRoot.AddToClassList(OpenClass);
                go.Focus();
                // An element shown this frame may not take the focus yet (TurnReportPresenter does the same).
                go.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement != go) go.Focus(); });
            });
        }

        /// <summary>Share: the browser's share sheet, or the download and then the share links.</summary>
        void Go()
        {
            if (!IsOpen) return;
            ShareShot.Share(post.text, ShareShot.Itch);
            if (!canShareFiles) { fallback.style.display = DisplayStyle.Flex; return; }
            // The share sheet can still be refused on the release (ShareShot.jslib); then the picture downloads and the
            // share links show, as where files cannot be shared. A refusal comes at once; a few seconds is enough to wait.
            go.schedule.Execute(() => { if (IsOpen && ShareShot.Failed()) fallback.style.display = DisplayStyle.Flex; }).Every(250).ForDuration(3000);
        }

        /// <summary>Copy: the browser copies on the release and answers after a promise (ShareShot.jslib), so the panel looks
        /// for the answer as it looks for a refused share sheet, and then says what was copied, or that nothing was.</summary>
        void Copy()
        {
            if (!IsOpen) return;
            copyPoll?.Pause();
            copied.style.display = DisplayStyle.None;
            ShareShot.Copy(post.text);
            // A press whose release never reached the browser (shorter than a frame, or another press first) copies
            // nothing and stays Pending: after a few seconds the panel says so rather than staying silent.
            var giveUpAt = UnityEngine.Time.realtimeSinceStartup + 4.5f;
            copyPoll = copy.schedule.Execute(() =>
            {
                var result = ShareShot.Copied();
                if (result == ShareShot.CopyResult.Pending && UnityEngine.Time.realtimeSinceStartup >= giveUpAt) result = ShareShot.CopyResult.Failed;
                if (!IsOpen || result == ShareShot.CopyResult.Pending) return;
                copied.text = text[result switch
                {
                    ShareShot.CopyResult.Picture => "share.copied_picture",
                    ShareShot.CopyResult.Text => "share.copied_text",
                    _ => "share.copy_failed",
                }];
                copied.style.display = DisplayStyle.Flex;
                copyPoll?.Pause();
            }).Every(250).ForDuration(5000);
        }

        public void Close()
        {
            if (!IsOpen) return;
            closedAt = UnityEngine.Time.frameCount;
            hudRoot.RemoveFromClassList(OpenClass);
            foreach (var (part, enabled) in covered) part.SetEnabled(enabled);
            covered.Clear();
            opener?.Focus();
            opener = null;
        }

        /// <summary>GameRoot's Escape poll and ScreenNav ask this first: it closes the panel and says the key is used, also in
        /// the frame the panel was already closed, so the same Escape does not go on to close the screen or the turn report
        /// under it.</summary>
        public bool Escape()
        {
            if (IsOpen) { Close(); return true; }
            return closedAt == UnityEngine.Time.frameCount;
        }

        public void Dispose()
        {
            disposed = true;
            Close();
            foreach (var unbind in unbindSites) unbind();
            go.UnregisterCallback(onGoPointer, TrickleDown.TrickleDown);
            go.UnregisterCallback(onGoSubmit, TrickleDown.TrickleDown);
            copyPoll?.Pause();
            copy.UnregisterCallback(onCopyPointer, TrickleDown.TrickleDown);
            copy.UnregisterCallback(onCopySubmit, TrickleDown.TrickleDown);
            settingsOpen.clicked -= OpenFromSettings;
            reportOpen.clicked -= OpenFromReport;
            close.clicked -= Close;
            layer.UnregisterCallback(onCancel);
        }
    }
}
