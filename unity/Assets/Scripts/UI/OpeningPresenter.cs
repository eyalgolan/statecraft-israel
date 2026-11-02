#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>#530: three sourced stills and a closing line. No bridge, commands, RNG or save access. A panel with a clip
    /// (opening.yaml) holds its photograph for PhotoHoldMs, then loops the clip silently in the same frame, labelled
    /// "AI-generated illustration" (owner ruling of 2 November 2026, "B: A + one new Yemen shot (~2,424)";
    /// docs/event-videos.md).</summary>
    public sealed class OpeningPresenter : IDisposable
    {
        public const string OpenClass = "opening-open";
        /// <summary>How long a panel shows its archival photograph before its clip starts: the photograph always comes first.</summary>
        public const long PhotoHoldMs = 2000;
        const string SourceEnglishClass = "opening-source-en";
        readonly VisualElement root, layer, photo;
        readonly Label title, body, arrivals, closing, credit, count, preview;
        readonly Button next, back, skip;
        readonly ScrollView scroll;
        readonly OpeningContent catalog;
        readonly OpeningContent.Document data;
        readonly UiText text;
        readonly Action<bool> shownChanged, finished;
        readonly Action onNext, onBack, onSkip;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<NavigationMoveEvent> onMove;
        readonly PressSource presses;
        readonly CardVideo? video;
        readonly bool hebrew;
        /// <summary>The panels whose clip failed this session: they keep their photograph.</summary>
        readonly HashSet<string> failed = new HashSet<string>();
        IVisualElementScheduledItem? hold;
        bool replay, disposed;
        int closedFrame = -1;
        public int Index { get; private set; }
        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;
        /// <summary>True while the shown panel holds its photograph before its clip.</summary>
        public bool ClipWaiting => hold != null;

        /// <param name="video">The clips' player, or null when video is off (CardVideo.ForcedOff): then every panel shows
        /// only its photograph.</param>
        public OpeningPresenter(VisualElement root, OpeningContent catalog, OpeningContent.Document data, UiText text,
            Action<bool> shownChanged, Action<bool> finished, CardVideo? video = null)
        {
            this.root = root.Q("root") ?? root; this.catalog = catalog; this.data = data; this.text = text; this.video = video;
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            this.shownChanged = shownChanged; this.finished = finished;
            layer = root.Q("opening-layer"); photo = layer.Q("opening-photo");
            title = layer.Q<Label>("opening-title"); body = layer.Q<Label>("opening-text");
            arrivals = layer.Q<Label>("opening-arrivals"); closing = layer.Q<Label>("opening-closing"); credit = layer.Q<Label>("opening-credit");
            count = layer.Q<Label>("opening-count"); preview = layer.Q<Label>("opening-preview");
            scroll = layer.Q<ScrollView>("opening-scroll");
            next = layer.Q<Button>("opening-next"); back = layer.Q<Button>("opening-back"); skip = layer.Q<Button>("opening-skip");
            back.text = text["opening.back"]; skip.text = text["opening.skip"];
            preview.text = text["opening.preview"]; preview.style.display = data.Reviewed ? DisplayStyle.None : DisplayStyle.Flex;
            // The historical prose, title, arrivals and credit remain the original English, including in the Hebrew UI.
            foreach (var label in new[] { title, body, arrivals, closing, credit }) label.languageDirection = LanguageDirection.LTR;
            onNext = Next; onBack = Back; onSkip = () => Close(finish: true);
            next.clicked += onNext; back.clicked += onBack; skip.clicked += onSkip;
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            onMove = e =>
            {
                if (!IsOpen || e.direction != NavigationMoveEvent.Direction.Next && e.direction != NavigationMoveEvent.Direction.Previous) return;
                var buttons = new[] { back, next, skip }.Where(b => b.enabledInHierarchy).ToArray();
                int at = Array.FindIndex(buttons, b => ReferenceEquals(b, layer.focusController?.focusedElement));
                int step = e.direction == NavigationMoveEvent.Direction.Next ? 1 : -1;
                buttons[at < 0 ? (step > 0 ? 0 : buttons.Length - 1) : (at + step + buttons.Length) % buttons.Length].Focus();
                layer.focusController?.IgnoreEvent(e); e.StopPropagation();
            };
            layer.RegisterCallback(onCancel); layer.RegisterCallback(onMove);
            presses = new PressSource(layer);
            layer.style.display = DisplayStyle.None;
        }

        public void Show(bool replay)
        {
            if (disposed || IsOpen || !data.PhotosReviewed) return;
            this.replay = replay; Index = 0; IsOpen = true;
            root.AddToClassList(OpenClass); layer.style.display = DisplayStyle.Flex;
            ScreenNav.DisableCovered(root);
            shownChanged(true); Render();
            next.Focus();
            layer.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement == null) next.Focus(); });
        }

        void Render()
        {
            bool last = Index == data.Panels.Count - 1;
            var panel = data.Panels[Index];
            title.text = panel.Title;
            body.text = panel.Text;
            StopClip();
            ShowPhoto();
            if (video != null && panel.Clip != null && !failed.Contains(panel.Id))
            {
                int at = Index;
                hold = layer.schedule.Execute(() => { if (IsOpen && Index == at) HoldElapsed(); }).StartingIn(PhotoHoldMs);
            }
            arrivals.text = string.Join(" · ", panel.Arrivals.Select(a =>
                a.Country + ": " + a.Count.ToString("N0", CultureInfo.InvariantCulture)));
            closing.text = last ? data.Closing.Text : "";
            closing.style.display = last ? DisplayStyle.Flex : DisplayStyle.None;
            count.text = text.Format("opening.count", Index + 1, data.Panels.Count);
            back.SetEnabled(Index > 0);
            next.text = last ? text[replay ? "opening.return" : "opening.begin"] : text["opening.next"];
            scroll.scrollOffset = Vector2.zero;
        }

        /// <summary>The shown panel's photograph, its English credit line and its alt text as the tooltip.</summary>
        void ShowPhoto()
        {
            var panel = data.Panels[Index];
            photo.style.backgroundImage = new StyleBackground(catalog.photos[Index]);
            photo.tooltip = panel.Image.Alt;
            SetCredit(panel.Image.Credit, english: true);
        }

        /// <summary>The credit line: the photograph's English credit, or the clip's label in the UI's language.</summary>
        void SetCredit(string words, bool english)
        {
            credit.text = words;
            credit.languageDirection = english || !hebrew ? LanguageDirection.LTR : LanguageDirection.RTL;
            credit.EnableInClassList(SourceEnglishClass, english);
        }

        /// <summary>The photograph's hold is over (test seam: what the scheduled hold calls): the shown panel's clip plays in
        /// the photograph's frame, labelled with the cards' "AI-generated illustration" string and described by its own alt
        /// text. CardVideo keeps the photograph until the clip's first frame; a clip that fails or times out puts the
        /// photograph back for the rest of the session.</summary>
        internal void HoldElapsed()
        {
            hold?.Pause(); hold = null;
            if (!IsOpen || video == null) return;
            var panel = data.Panels[Index];
            if (panel.Clip == null || failed.Contains(panel.Id)) return;
            SetCredit(text["event.ai_label"], english: false);
            photo.tooltip = panel.Clip.Alt;
            var id = panel.Id;
            video.Play(CardVideo.Url(panel.Clip.File), photo, _ =>
            {
                failed.Add(id);
                if (IsOpen && data.Panels[Index].Id == id) ShowPhoto();
            });
        }

        /// <summary>Stops the clip, or its pending start; the caller puts the photograph back.</summary>
        void StopClip()
        {
            hold?.Pause(); hold = null;
            if (video?.Current != null) video.Stop();
        }

        void Next()
        {
            if (!IsOpen) return;
            if (Index == data.Panels.Count - 1) { Close(finish: true); return; }
            Index++; Render();
        }
        void Back() { if (!IsOpen || Index == 0) return; Index--; Render(); }
        public bool Escape() { if (!IsOpen) return false; Close(finish: true); return true; }

        void Close(bool finish)
        {
            if (!IsOpen) return;
            IsOpen = false; closedFrame = Time.frameCount;
            StopClip();
            layer.style.display = DisplayStyle.None; root.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(root); shownChanged(false);
            if (finish && presses.ByKeyboard)
            {
                var target = replay ? root.Q<Button>("start-opening-replay") : root.Q<Button>("end-turn");
                if (replay || !root.ClassListContains(EventCardPresenter.OpenClass)) FocusReturn.To(target);
            }
            if (finish) finished(replay);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Close(finish: false);
            next.clicked -= onNext; back.clicked -= onBack; skip.clicked -= onSkip;
            layer.UnregisterCallback(onCancel); layer.UnregisterCallback(onMove); presses.Dispose();
        }
    }
}
