#nullable enable
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>#530: three sourced stills and a closing line. No bridge, commands, RNG or save access.</summary>
    public sealed class OpeningPresenter : IDisposable
    {
        public const string OpenClass = "opening-open";
        /// <summary>Left to right and left-aligned in either UI (Statecraft.uss): what is shown in English.</summary>
        public const string SourceEnglishClass = "opening-source-en";
        readonly VisualElement root, layer, photo;
        readonly Label title, body, arrivals, closing, credit, count, preview;
        readonly Button next, back, skip;
        readonly ScrollView scroll;
        readonly OpeningContent catalog;
        readonly OpeningContent.Document data;
        readonly UiText text;
        readonly Action<bool> shownChanged, finished;
        readonly bool hebrew;
        readonly Action onNext, onBack, onSkip;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<NavigationMoveEvent> onMove;
        readonly PressSource presses;
        bool replay, disposed;
        int closedFrame = -1;
        public int Index { get; private set; }
        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;

        public OpeningPresenter(VisualElement root, OpeningContent catalog, OpeningContent.Document data, UiText text,
            Action<bool> shownChanged, Action<bool> finished)
        {
            this.root = root.Q("root") ?? root; this.catalog = catalog; this.data = data; this.text = text;
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
            // The photo credit is the English credit line in both UIs. The title, text, arrivals and closing follow the UI's
            // language where opening.yaml has their Hebrew (owner, 1 Nov 2026, "Translate to Hebrew (Recommended)"; Render).
            credit.languageDirection = LanguageDirection.LTR;
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
            bool he = hebrew && panel.HasHebrew, heClosing = hebrew && data.Closing.HasHebrew;
            Say(title, he ? panel.TitleHe! : panel.Title, he);
            Say(body, he ? panel.TextHe! : panel.Text, he);
            photo.style.backgroundImage = new StyleBackground(catalog.photos[Index]);
            photo.tooltip = panel.Image.Alt;
            credit.text = panel.Image.Credit;
            Say(arrivals, string.Join(" · ", panel.Arrivals.Select(a =>
                (he ? a.CountryHe : a.Country) + ": " + a.Count.ToString("N0", CultureInfo.InvariantCulture))), he);
            Say(closing, last ? (heClosing ? data.Closing.TextHe! : data.Closing.Text) : "", heClosing);
            closing.style.display = last ? DisplayStyle.Flex : DisplayStyle.None;
            count.text = text.Format("opening.count", Index + 1, data.Panels.Count);
            back.SetEnabled(Index > 0);
            next.text = last ? text[replay ? "opening.return" : "opening.begin"] : text["opening.next"];
            scroll.scrollOffset = Vector2.zero;
        }

        /// <summary>A prose label's words, with their direction and style: Hebrew right to left and set from the right like
        /// the rest of the Hebrew UI; English left to right and left-aligned (SourceEnglishClass) in either UI.</summary>
        static void Say(Label label, string words, bool hebrew)
        {
            label.text = words;
            label.languageDirection = hebrew ? LanguageDirection.RTL : LanguageDirection.LTR;
            label.EnableInClassList(SourceEnglishClass, !hebrew);
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
