#nullable enable
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The UI's language buttons, in Settings (#1036) and on the start screen (#2054). Each names its language in
    /// that language, so a player who cannot read the UI's one still finds their own, and the one in use is marked
    /// (SettingsPresenter.SelectedClass). A press keeps the choice under SettingsPresenter.LanguageKey, which
    /// UiLanguage.AtStart reads, and then switches the UI (GameRoot.SwitchLanguage), so the two places share one choice. A
    /// new language is one more entry in All and a button named for it in each place in Game.uxml.</summary>
    public sealed class LanguageButtons : IDisposable
    {
        /// <summary>Each language's code and its name in itself.</summary>
        public static readonly (string language, string name)[] All = { (UiLanguage.English, "English"), (UiLanguage.Hebrew, "עברית") };

        readonly (Button button, string language, Action pick)[] buttons;
        readonly string current;
        readonly SettingsStore? store;
        readonly Action<string>? switchLanguage;

        /// <param name="prefix">The buttons' names before the language's code ("settings-language-", "start-language-").</param>
        /// <param name="current">The UI's language (its strings' "ui.lang").</param>
        /// <param name="store">Where the choice is kept; null keeps nothing.</param>
        /// <param name="switchLanguage">Puts the UI in the language pressed (GameRoot builds the screens again); null changes
        /// nothing but the stored choice, read at the next start.</param>
        public LanguageButtons(VisualElement scope, string prefix, string current, SettingsStore? store, Action<string>? switchLanguage)
        {
            this.current = current; this.store = store; this.switchLanguage = switchLanguage;
            buttons = Array.ConvertAll(All, l =>
            {
                var b = scope.Q<Button>(prefix + l.language);
                b.text = l.name;
                Action pick = () => Pick(l.language);
                b.clicked += pick;
                return (b, l.language, pick);
            });
            Mark();
        }

        /// <summary>Marks the language in use.</summary>
        public void Mark()
        {
            foreach (var (b, l, _) in buttons) b.EnableInClassList(SettingsPresenter.SelectedClass, l == current);
        }

        /// <summary>Keeps the choice first: the switch disposes these buttons with the rest of the UI.</summary>
        void Pick(string language)
        {
            if (language == current) return;
            try { store?.Write(SettingsPresenter.LanguageKey, language); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the language choice ({e.Message}); it holds until the page is closed");
            }
            switchLanguage?.Invoke(language);
        }

        public void Dispose()
        {
            foreach (var (b, _, pick) in buttons) b.clicked -= pick;
        }
    }
}
