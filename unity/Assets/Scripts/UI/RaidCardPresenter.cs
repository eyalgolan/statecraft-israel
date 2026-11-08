#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Tel Aviv raid card on New game (owner, 8 Nov 2026: "then show an event card with an explanation about the
    /// bombing and how many were killed ... the player only has an option of clicking ok"): one card in the event-card frame,
    /// with its silent clip looping, labelled "AI-generated illustration", and only OK. Escape is the same OK. No bridge,
    /// commands, RNG or save access. GameRoot shows it after the opening and hands over to the turn 1 advisor chain on OK
    /// ("Film, raid, card, advisors (Recommended)").</summary>
    public sealed class RaidCardPresenter : IDisposable
    {
        public const string OpenClass = "raid-open";
        readonly VisualElement root, layer, media;
        readonly Label title, body, credit, ai, preview;
        readonly Button ok;
        readonly ScrollView scroll;
        readonly RaidCardContent.Document data;
        readonly Action<bool> shownChanged;
        readonly Action finished;
        readonly CardVideo? video;
        readonly bool hebrew;
        readonly Action onOk;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly PressSource presses;
        bool disposed;
        int closedFrame = -1;
        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;

        /// <param name="video">The clip's player, or null when video is off (CardVideo.ForcedOff): the card then shows no
        /// media frame.</param>
        public RaidCardPresenter(VisualElement root, RaidCardContent.Document data, UiText text, Action<bool> shownChanged,
            Action finished, CardVideo? video = null)
        {
            this.root = root.Q("root") ?? root; this.data = data; this.shownChanged = shownChanged; this.finished = finished;
            this.video = video;
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            layer = root.Q("raid-layer"); media = layer.Q("raid-media");
            title = layer.Q<Label>("raid-title"); body = layer.Q<Label>("raid-text");
            credit = layer.Q<Label>("raid-credit"); ai = layer.Q<Label>("raid-ai"); preview = layer.Q<Label>("raid-preview");
            scroll = layer.Q<ScrollView>("raid-scroll");
            ok = layer.Q<Button>("raid-ok");
            ok.text = text["raid.ok"];
            preview.text = text["opening.preview"]; preview.style.display = data.Reviewed ? DisplayStyle.None : DisplayStyle.Flex;
            // The Hebrew UI shows the Hebrew where the file has it, as the opening does; a player build shows the card only
            // once that Hebrew is approved (RaidCardContent.MayPresent).
            bool he = hebrew && data.HasHebrew;
            Say(title, he ? data.TitleHe! : data.Title, he);
            Say(body, he ? data.TextHe! : data.Text, he);
            // The photographs' credit stays the English credit line in both UIs; the AI label is in the UI's language.
            credit.text = data.Clip.Credit; credit.languageDirection = LanguageDirection.LTR;
            ai.text = text["event.ai_label"]; ai.languageDirection = hebrew ? LanguageDirection.RTL : LanguageDirection.LTR;
            media.tooltip = hebrew && !string.IsNullOrWhiteSpace(data.Clip.AltHe) ? data.Clip.AltHe : data.Clip.Alt;
            onOk = () => Close(finish: true);
            ok.clicked += onOk;
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            layer.RegisterCallback(onCancel);
            presses = new PressSource(layer);
            layer.style.display = DisplayStyle.None;
        }

        /// <summary>Opens the card; nothing while it is open (the start screen's close and New game's Started can both
        /// report one new game) or after Dispose.</summary>
        public void Show()
        {
            if (disposed || IsOpen) return;
            IsOpen = true;
            root.AddToClassList(OpenClass); layer.style.display = DisplayStyle.Flex;
            ScreenNav.DisableCovered(root);
            shownChanged(true);
            scroll.scrollOffset = Vector2.zero;
            ShowMedia(video != null);
            if (video != null) video.Play(CardVideo.Url(data.Clip.File), media, _ => ClipFailed());
            ok.Focus();
            layer.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement == null) ok.Focus(); });
        }

        /// <summary>The clip failed or timed out (test seam: what CardVideo's failure calls): the card keeps its text and
        /// OK and drops the empty frame and its label.</summary>
        internal void ClipFailed()
        {
            if (video?.Current != null) video.Stop();
            ShowMedia(false);
        }

        void ShowMedia(bool on)
        {
            var display = on ? DisplayStyle.Flex : DisplayStyle.None;
            media.style.display = display; ai.style.display = display; credit.style.display = display;
            if (!on) media.style.backgroundImage = StyleKeyword.None;
        }

        public bool Escape() { if (!IsOpen) return false; Close(finish: true); return true; }

        void Close(bool finish)
        {
            if (!IsOpen) return;
            IsOpen = false; closedFrame = Time.frameCount;
            if (video?.Current != null) video.Stop();
            media.style.backgroundImage = StyleKeyword.None;
            layer.style.display = DisplayStyle.None; root.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(root); shownChanged(false);
            if (finish && presses.ByKeyboard && !root.ClassListContains(EventCardPresenter.OpenClass)) FocusReturn.To(root.Q<Button>("end-turn"));
            if (finish) finished();
        }

        /// <summary>A prose label's words and direction: Hebrew right to left; English left to right and left-aligned in
        /// either UI (the opening's class).</summary>
        static void Say(Label label, string words, bool hebrew)
        {
            label.text = words;
            label.languageDirection = hebrew ? LanguageDirection.RTL : LanguageDirection.LTR;
            label.EnableInClassList(OpeningPresenter.SourceEnglishClass, !hebrew);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Close(finish: false);
            ok.clicked -= onOk;
            layer.UnregisterCallback(onCancel); presses.Dispose();
        }
    }
}
