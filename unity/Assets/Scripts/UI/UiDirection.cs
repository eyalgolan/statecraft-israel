#nullable enable
using System;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The paragraph direction of every text in the Hebrew UI (#193). The UI strings are Hebrew, but dates, money,
    /// names and event text still arrive in English (#55), and UI Toolkit has no "auto" direction: an English line laid out
    /// as a right-to-left paragraph is reordered ("15 May 1948" reads "May 1948 15", "+5%" reads "5%+"). So each text
    /// element takes its direction from its own text, by its first strong character, as HTML's dir="auto" does: Hebrew is
    /// right to left, anything else (and a line of digits and signs alone) left to right. It follows a text when it changes,
    /// and looks at the whole tree again after a turn, a click or a key press, which is when presenters build new elements.
    /// The start screen's Hebrew lines set their own direction in Game.uxml and are left alone.</summary>
    public sealed class UiDirection : IDisposable
    {
        const string OwnDirectionClass = "start-he";
        readonly VisualElement root; readonly SimBridge bridge;
        readonly EventCallback<ChangeEvent<string>> onText;
        readonly EventCallback<ClickEvent> onClick;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        readonly Action later;
        IVisualElementScheduledItem? pending;

        public UiDirection(VisualElement root, SimBridge bridge)
        {
            this.root = root; this.bridge = bridge;
            onText = e => { if (e.target is TextElement t) Apply(t, e.newValue); };
            later = () => { pending?.Pause(); pending = root.schedule.Execute(Apply); };   // next frame: after the presenters have rendered
            onClick = _ => later();
            onSubmit = _ => later();
            root.RegisterCallback(onText, TrickleDown.TrickleDown);
            root.RegisterCallback(onClick, TrickleDown.TrickleDown);
            root.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
            bridge.Changed += later;
            Apply();
        }

        /// <summary>Whether a text is laid out right to left: its first letter is Hebrew or Arabic. A text with no letter
        /// (a number, a percentage) is not. The direction marks (U+200E, U+200F) are not letters and are passed over, so a
        /// line that starts with a value UiText.Format has put between marks takes its direction from the value.</summary>
        public static bool RightToLeft(string? text)
        {
            if (text == null) return false;
            foreach (var c in text)
            {
                if (IsRightToLeft(c)) return true;
                if (char.IsLetter(c)) return false;
            }
            return false;
        }

        /// <summary>Whether a text has a Hebrew or Arabic character anywhere in it.</summary>
        public static bool HasRightToLeft(string? text)
        {
            if (text == null) return false;
            foreach (var c in text) if (IsRightToLeft(c)) return true;
            return false;
        }

        static bool IsRightToLeft(char c) => c >= '֐' && c <= 'ࣿ' || c >= 'יִ' && c <= '﷿' || c >= 'ﹰ' && c <= '﻿';

        static void Apply(TextElement t, string? text)
        {
            if (t.ClassListContains(OwnDirectionClass)) return;
            t.languageDirection = RightToLeft(text) ? LanguageDirection.RTL : LanguageDirection.LTR;
        }

        /// <summary>Sets the direction of every text element under the root from its text.</summary>
        public void Apply() => root.Query<TextElement>().ForEach(t => Apply(t, t.text));

        public void Dispose()
        {
            pending?.Pause(); pending = null;
            bridge.Changed -= later;
            root.UnregisterCallback(onText, TrickleDown.TrickleDown);
            root.UnregisterCallback(onClick, TrickleDown.TrickleDown);
            root.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
            root.Query<TextElement>().ForEach(t => { if (!t.ClassListContains(OwnDirectionClass)) t.languageDirection = LanguageDirection.Inherit; });
        }
    }
}
