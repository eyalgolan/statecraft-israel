#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Settings screen (#40), opened from the left column's gear like the other screens (ScreenNav). Its options:
    /// the graphics quality (GraphicsQuality: Auto / High / Low), the sound effects volume (GameAudio, #39), the UI's language (#1036),
    /// the UI scale (100% / 125% / 150%), the tilt-shift blur, and a Community row whose "Star on GitHub" opens the repository
    /// (#1168, ExternalLink). The blur option turns it off and on: applied at once (TiltShift.Enabled, what the "tiltshift"
    /// toggle sets) and kept in the SettingsStore, so it holds after a reload. A stored "off" only ever turns the blur off, so ?notiltshift still boots without it. The
    /// button says what is set ("On" / "Off"), not only a colour: the player's choice (Enabled), not the web tier's
    /// automatic last fallback step (TiltShift.FallbackOff), which it never changes.</summary>
    public sealed class SettingsPresenter : IDisposable
    {
        public const string TiltShiftKey = "tiltshift", QualityKey = "quality", LanguageKey = "lang", UiScaleKey = "uiscale", SoundKey = "sfx", SelectedClass = "settings-choice-selected";
        /// <summary>The owner's UI scales (#40, 18 Oct 2026), in percent; the first is the default.</summary>
        public static readonly int[] UiScales = { 100, 125, 150 };
        /// <summary>The owner's ruling on #1011 (19 Oct 2026): Settings offers only the UI scales verified to fit, and 100% always,
        /// as the fallback. A scale above 100% is verified on the window shapes (width / height, narrowest to widest) where
        /// the layout suites pass with the panel at that scale: UiLayoutTests, InfoPanelLayoutTests, TopBarScoreLayoutTests,
        /// HebrewUiTests, LeftColumnTests, PromoScreensTests and TopBarTests, whose windows run from 360x800 to 2560x1080.
        /// None is yet: at 125% and 150% those suites fail on every window shape (PR #1011, 18 Oct 2026), so the game offers
        /// 100% only. To offer a scale, run the suites at it and enter the narrowest and widest shapes where they all pass.
        /// Tests enter shapes here to drive the rule and put the table back.</summary>
        public static readonly Dictionary<int, (float narrowest, float widest)> VerifiedShapes = new Dictionary<int, (float, float)>();
        /// <summary>The sound effects' volume steps (#39): the stored word and GameAudio's volume. With no stored choice, "high".</summary>
        public static readonly (string level, float volume)[] SoundLevels = { ("off", 0f), ("low", 0.35f), ("high", 1f) };
        readonly SettingsStore? store; readonly UiText text; readonly VisualElement root, screen; readonly Button tiltShift;
        readonly (Button button, GraphicsQuality.Choice choice, Action pick)[] quality;
        readonly (Button button, string language, Action pick)[] languages;
        readonly (Button button, string level, Action pick)[] sound;
        readonly GameAudio? audio;
        string soundLevel;
        readonly Action<string>? switchLanguage;
        readonly PanelSettings? panel; readonly Vector2Int reference; int uiScale = UiScales[0], chosen = UiScales[0];
        readonly (Button button, int percent, Action pick)[] scales;
        readonly Action unbindStar;

        /// <param name="store">Where the choice is kept; null keeps nothing (the option still works until the page closes).</param>
        /// <param name="switchLanguage">Puts the UI in the language pressed (GameRoot builds the screens again); null changes
        /// nothing but the stored choice, read at the next start.</param>
        /// <param name="panel">The UI's panel, which the UI scale scales; null leaves the scale alone.</param>
        /// <param name="audio">The sound effects the volume applies to; null keeps only the choice.</param>
        public SettingsPresenter(VisualElement root, UiText text, SettingsStore? store, Action<string>? switchLanguage = null, PanelSettings? panel = null, GameAudio? audio = null)
        {
            this.store = store; this.text = text; this.switchLanguage = switchLanguage; this.root = root; this.audio = audio;
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
            // Sound effects (#39): three volume steps, the one that is set marked, applied to GameAudio at once and kept.
            screen.Q<Label>("settings-sound-label").text = text["settings.sound"];
            var storedSound = store?.Read(SoundKey);
            soundLevel = Array.Exists(SoundLevels, l => l.level == storedSound) ? storedSound! : "high";
            ApplySound();
            sound = Array.ConvertAll(SoundLevels, l =>
            {
                var b = screen.Q<Button>("settings-sound-" + l.level);
                b.text = text["settings.sound." + l.level];
                Action pick = () => PickSound(l.level);
                b.clicked += pick;
                return (b, l.level, pick);
            });
            // UI scale (#40): the panel scales with the screen from its 1920x1080 reference (ScaleWithScreenSize); dividing the
            // reference by the choice multiplies that scale by it, whatever the window. The whole UI is one panel. Dispose
            // puts the reference back: in the editor the panel is a project asset, and a choice must not stay in it.
            this.panel = panel;
            reference = panel != null ? panel.referenceResolution : default;
            screen.Q<Label>("settings-uiscale-label").text = text["settings.uiscale"];
            scales = Array.ConvertAll(UiScales, percent =>
            {
                var b = screen.Q<Button>("settings-uiscale-" + percent);
                b.text = PlayerText.Percent(Level(percent));
                Action pick = () => PickScale(percent);
                b.clicked += pick;
                return (b, percent, pick);
            });
            var storedScale = store?.Read(UiScaleKey);
            int startScale = Array.Find(UiScales, p => p.ToString() == storedScale);   // 0 when the file names none of them
            chosen = startScale > 0 ? startScale : UiScales[0];
            ApplyScale(Fits(chosen) ? chosen : UiScales[0]);
            // The fit depends on the window's shape, which a resize changes: the offer and the applied scale follow the
            // root's geometry (as TopBarInset does). The file keeps the choice, so a shape that holds it again gets it back.
            root.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            // Community (#1168): Star on GitHub opens the repository in a new tab.
            screen.Q<Label>("settings-community-label").text = text["settings.community"];
            var star = screen.Q<Button>("settings-star");
            star.text = text["community.star"];
            unbindStar = ExternalLink.Bind(star, ExternalLink.Repository);
            ScreenNav.OnOpened(screen, Show);   // a dev toggle may have changed it while the screen was shut
            Show();
        }

        public void Dispose()
        {
            tiltShift.clicked -= Flip;
            foreach (var (b, _, pick) in quality) b.clicked -= pick;
            foreach (var (b, _, pick) in languages) b.clicked -= pick;
            foreach (var (b, _, pick) in sound) b.clicked -= pick;
            foreach (var (b, _, pick) in scales) b.clicked -= pick;
            unbindStar();
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            if (panel != null) panel.referenceResolution = reference;
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

        void PickSound(string level)
        {
            soundLevel = level;
            ApplySound();
            Show();
            try { store?.Write(SoundKey, level); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the sound choice ({e.Message}); it holds until the page is closed");
            }
        }

        void ApplySound()
        {
            if (audio != null) audio.Volume = Array.Find(SoundLevels, l => l.level == soundLevel).volume;
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

        void PickScale(int percent)
        {
            chosen = percent;
            ApplyScale(percent);
            Show();
            try { store?.Write(UiScaleKey, percent.ToString()); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Settings: could not keep the UI scale ({e.Message}); it holds until the page is closed");
            }
        }

        /// <summary>A UI scale's level (1.25 for 125%): the stored values and the buttons' names are whole percents.</summary>
        static double Level(int percent) => percent / 100.0;

        /// <summary>Whether the scale is verified to fit this window (VerifiedShapes); 100% always is. Before the first
        /// layout the shape is unknown and a verified scale counts: nothing is drawn yet, and OnGeometry checks again.</summary>
        bool Fits(int percent)
        {
            if (percent == UiScales[0]) return true;
            if (!VerifiedShapes.TryGetValue(percent, out var shapes)) return false;
            float shape = root.layout.width / root.layout.height;
            return !(shape > 0) || (shape >= shapes.narrowest && shape <= shapes.widest);
        }

        void OnGeometry(GeometryChangedEvent e)
        {
            var fit = Fits(chosen) ? chosen : UiScales[0];
            if (fit != uiScale) ApplyScale(fit);
            Show();
        }

        void ApplyScale(int percent)
        {
            uiScale = percent;
            var level = Level(percent);
            if (panel != null)
                panel.referenceResolution = new Vector2Int(Mathf.RoundToInt((float)(reference.x / level)), Mathf.RoundToInt((float)(reference.y / level)));
        }

        void Show()
        {
            tiltShift.text = text[TiltShift.Enabled ? "settings.on" : "settings.off"];
            foreach (var (b, c, _) in quality) b.EnableInClassList(SelectedClass, c == GraphicsQuality.Current);
            foreach (var (b, l, _) in languages) b.EnableInClassList(SelectedClass, l == text["ui.lang"]);
            foreach (var (b, l, _) in sound) b.EnableInClassList(SelectedClass, l == soundLevel);
            foreach (var (b, p, _) in scales)
            {
                b.EnableInClassList(SelectedClass, p == uiScale);
                b.style.display = Fits(p) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
