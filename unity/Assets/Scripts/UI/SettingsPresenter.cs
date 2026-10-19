#nullable enable
using System;
using System.IO;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Settings screen (#40), opened from the left column's gear like the other screens (ScreenNav). Its options:
    /// the graphics quality (GraphicsQuality: Auto / High / Low), the UI's language (#1036) and the tilt-shift blur. The
    /// blur option turns it off and on: applied at once (TiltShift.Enabled, what the "tiltshift" toggle sets) and kept in
    /// the SettingsStore, so it holds after a reload. A stored "off" only ever turns the blur off, so ?notiltshift still boots without it. The
    /// button says what is set ("On" / "Off"), not only a colour: the player's choice (Enabled), not the web tier's
    /// automatic last fallback step (TiltShift.FallbackOff), which it never changes.</summary>
    public sealed class SettingsPresenter : IDisposable
    {
        public const string TiltShiftKey = "tiltshift", QualityKey = "quality", LanguageKey = "lang", SelectedClass = "settings-choice-selected";
        readonly SettingsStore? store; readonly UiText text; readonly VisualElement screen; readonly Button tiltShift;
        readonly (Button button, GraphicsQuality.Choice choice, Action pick)[] quality;
        readonly (Button button, string language, Action pick)[] languages;
        readonly Action<string>? switchLanguage;

        /// <param name="store">Where the choice is kept; null keeps nothing (the option still works until the page closes).</param>
        /// <param name="switchLanguage">Puts the UI in the language pressed (GameRoot builds the screens again); null changes
        /// nothing but the stored choice, read at the next start.</param>
        public SettingsPresenter(VisualElement root, UiText text, SettingsStore? store, Action<string>? switchLanguage = null)
        {
            this.store = store; this.text = text; this.switchLanguage = switchLanguage;
            screen = root.Q<VisualElement>("screen-settings");
            screen.Q<Label>("settings-screen-title").text = text["settings.title"];
            screen.Q<Label>("settings-tiltshift-label").text = text["settings.tiltshift"];
            tiltShift = screen.Q<Button>("settings-tiltshift");
            if (store?.Read(TiltShiftKey) == "off") TiltShift.Enabled = false;
            tiltShift.clicked += Flip;
            // Graphics quality (#40): three buttons, the one that is set marked. A stored choice is applied at start unless
            // an instrument flag must see the tier's own look (GraphicsQuality.AtStart); Auto at start changes nothing.
            screen.Q<Label>("settings-quality-label").text = text["settings.quality"];
            var choices = new[] { GraphicsQuality.Choice.Auto, GraphicsQuality.Choice.High, GraphicsQuality.Choice.Low };
            quality = Array.ConvertAll(choices, c =>
            {
                var key = GraphicsQuality.Format(c);
                var b = screen.Q<Button>("settings-quality-" + key);
                b.text = text["settings.quality." + key];
                Action pick = () => Pick(c);
                b.clicked += pick;
                return (b, c, pick);
            });
            var start = GraphicsQuality.AtStart(store?.Read(QualityKey), Application.absoluteURL, Environment.GetCommandLineArgs(),
                Application.platform == RuntimePlatform.WebGLPlayer);
            if (start != GraphicsQuality.Current) GraphicsQuality.Apply(start);
            // The UI's language (#1036): each button names its language in that language, so a player who cannot read the
            // UI's one still finds their own; the one in use is marked. UiLanguage.AtStart reads the stored choice.
            screen.Q<Label>("settings-language-label").text = text["settings.language"];
            screen.Q<Label>("settings-language-note").text = text["settings.language.note"];
            languages = Array.ConvertAll(new[] { (UiLanguage.English, "English"), (UiLanguage.Hebrew, "עברית") }, l =>
            {
                var b = screen.Q<Button>("settings-language-" + l.Item1);
                b.text = l.Item2;
                Action pick = () => PickLanguage(l.Item1);
                b.clicked += pick;
                return (b, l.Item1, pick);
            });
            ScreenNav.OnOpened(screen, Show);   // a dev toggle may have changed it while the screen was shut
            Show();
        }

        public void Dispose()
        {
            tiltShift.clicked -= Flip;
            foreach (var (b, _, pick) in quality) b.clicked -= pick;
            foreach (var (b, _, pick) in languages) b.clicked -= pick;
            if (screen.userData is Action) screen.userData = null;
        }

        void Flip()
        {
            TiltShift.Enabled = !TiltShift.Enabled;
            Show();
            try { store?.Write(TiltShiftKey, TiltShift.Enabled ? "on" : "off"); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the tilt-shift choice ({e.Message}); it holds until the page is closed");
            }
        }

        void Pick(GraphicsQuality.Choice c)
        {
            if (c == GraphicsQuality.Current) return;   // pressing Automatic again must not undo the fallback's step down
            GraphicsQuality.Apply(c);
            Show();
            try { store?.Write(QualityKey, GraphicsQuality.Format(GraphicsQuality.Current)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the quality choice ({e.Message}); it holds until the page is closed");
            }
        }

        /// <summary>Keeps the choice first: the switch disposes this presenter with the rest of the UI.</summary>
        void PickLanguage(string language)
        {
            if (language == text["ui.lang"]) return;
            try { store?.Write(LanguageKey, language); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the language choice ({e.Message}); it holds until the page is closed");
            }
            switchLanguage?.Invoke(language);
        }

        void Show()
        {
            tiltShift.text = text[TiltShift.Enabled ? "settings.on" : "settings.off"];
            foreach (var (b, c, _) in quality) b.EnableInClassList(SelectedClass, c == GraphicsQuality.Current);
            foreach (var (b, l, _) in languages) b.EnableInClassList(SelectedClass, l == text["ui.lang"]);
        }
    }
}
