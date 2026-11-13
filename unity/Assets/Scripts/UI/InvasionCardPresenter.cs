#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Arab armies' invasion card on New game (#3142; owner: "we need event cards about the arab armies
    /// invasions, that would show the player where the enemy forces are located"; 11 Nov 2026: "One card, step per front
    /// (Recommended)", "Replace the 2 advisor steps (Recommended)", "Info only, turn 1 only (Recommended)"). One card in the
    /// event-card frame, docked to the side so the map stays in view: an intro, then one step per front, each handing its
    /// units_1948.yaml row to <c>focus</c> (GameRoot centres the camera on that army's token); Next, and Continue on the
    /// last step. Escape is the same as Continue: it closes the card. No bridge, commands, RNG or save access. GameRoot shows
    /// it after the raid card and hands over to the turn 1 advisor chain when it closes.</summary>
    public sealed class InvasionCardPresenter : IDisposable
    {
        public const string OpenClass = "invasion-open";
        readonly VisualElement root, layer;
        readonly Label title, body, count, preview;
        readonly Button next;
        readonly ScrollView scroll;
        readonly InvasionCardContent.Document data;
        readonly UiText text;
        readonly Action<bool> shownChanged;
        readonly Action<string?> focus;
        readonly Action finished;
        readonly bool hebrew;
        readonly Action onNext;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly PressSource presses;
        bool disposed;
        int closedFrame = -1, shownFrame = -1;
        public bool IsOpen { get; private set; }
        /// <summary>The step on show (0, the intro, while closed).</summary>
        public int Index { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;

        /// <param name="focus">Called with each step's row as it shows (null on the intro).</param>
        /// <param name="finished">Called when the player closes the card (Continue or Escape), not on Dispose.</param>
        public InvasionCardPresenter(VisualElement root, InvasionCardContent.Document data, UiText text, Action<bool> shownChanged,
            Action<string?> focus, Action finished)
        {
            this.root = root.Q("root") ?? root; this.data = data; this.text = text; this.shownChanged = shownChanged;
            this.focus = focus; this.finished = finished;
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            layer = root.Q("invasion-layer");
            title = layer.Q<Label>("invasion-title"); body = layer.Q<Label>("invasion-text");
            count = layer.Q<Label>("invasion-count"); preview = layer.Q<Label>("invasion-preview");
            scroll = layer.Q<ScrollView>("invasion-scroll");
            next = layer.Q<Button>("invasion-next");
            preview.text = text["opening.preview"]; preview.style.display = data.Reviewed ? DisplayStyle.None : DisplayStyle.Flex;
            // The Hebrew UI shows the Hebrew where the file has it, as the raid card does; a player build shows the card only
            // once that Hebrew is approved (InvasionCardContent.MayPresent).
            Say(title, hebrew && data.HasHebrew ? data.TitleHe! : data.Title, hebrew && data.HasHebrew);
            count.languageDirection = hebrew ? LanguageDirection.RTL : LanguageDirection.LTR;
            onNext = Next;
            next.clicked += onNext;
            // One press, two readers (#3000), as on the raid card: the Escape or click that closed the raid card shows this
            // one, whose Show focuses Next, and the same press can reach it as the UI Cancel in that frame or the next. That
            // Cancel was the raid card's: used, this card stays up.
            onCancel = e =>
            {
                if (IsOpen && Time.frameCount - shownFrame <= 1) { e.StopPropagation(); return; }
                if (Escape()) e.StopPropagation();
            };
            layer.RegisterCallback(onCancel);
            presses = new PressSource(layer);
            layer.style.display = DisplayStyle.None;
        }

        /// <summary>Opens the card on its intro; nothing while it is open or after Dispose.</summary>
        public void Show()
        {
            if (disposed || IsOpen) return;
            IsOpen = true; shownFrame = Time.frameCount; Index = 0;
            root.AddToClassList(OpenClass); layer.style.display = DisplayStyle.Flex;
            ScreenNav.DisableCovered(root);
            shownChanged(true);
            Render();
            next.Focus();
            layer.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement == null) next.Focus(); });
        }

        /// <summary>Next, or Continue on the last step (which closes the card).</summary>
        void Next()
        {
            if (!IsOpen) return;
            if (Index >= data.Steps.Count - 1) { Close(finish: true); return; }
            Index++;
            Render();
        }

        void Render()
        {
            var step = data.Steps[Index];
            bool he = hebrew && data.HasHebrew;
            Say(body, he ? step.TextHe! : step.Text, he);
            count.text = text.Format("opening.count", Index + 1, data.Steps.Count);
            next.text = text[Index >= data.Steps.Count - 1 ? "invasion.continue" : "opening.next"];
            scroll.scrollOffset = Vector2.zero;
            focus(step.Focus);
        }

        public bool Escape() { if (!IsOpen) return false; Close(finish: true); return true; }

        void Close(bool finish)
        {
            if (!IsOpen) return;
            IsOpen = false; closedFrame = Time.frameCount; Index = 0;
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
            next.clicked -= onNext;
            layer.UnregisterCallback(onCancel); presses.Dispose();
        }
    }
}
