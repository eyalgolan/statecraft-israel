#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The start screen: a painting of the 14 May 1948 ceremony in the Tel Aviv Museum hall, two lines of the
    /// Declaration of Independence in the official Hebrew with the official English under each (the lines live in
    /// Game.uxml; StartScreenTests checks them against docs/sources/declaration-1948), and Continue / New Game. GameRoot
    /// shows it over the started game before the first turn; automated runs skip it (Skipped).
    /// Continue shows only when the session continues the autosave (GameStart.Begin's resumed) and simply closes the screen:
    /// that session is the save, already replayed. New Game with no save closes it too (the session is already a new game);
    /// with a save it goes through NewGamePresenter's two presses, the same flow as the top bar's New Game, so one stray
    /// press never wipes the save (#175, B6). Continue and Escape withdraw an open request.
    /// While it shows, the root has OpenClass (Statecraft.uss hides the HUD under it, so Tab reaches only these buttons)
    /// and <c>shownChanged</c> is told, so GameRoot can stop the camera keys. The panel's source line says the painting is
    /// AI-generated. Beside New game, "Star on GitHub" opens the repository in a new tab (#1168, ExternalLink). Under ?navlog it
    /// logs where its buttons are (ButtonCentres), so tools/web_check.py clicks them wherever the layout puts them.</summary>
    public sealed class StartScreenPresenter : IDisposable
    {
        /// <summary>On the "root" element while the start screen shows.</summary>
        public const string OpenClass = "start-open";
        /// <summary>On the start screen when the painting is a full-width band with the panel under it (Fit).</summary>
        public const string BandClass = "start-band";
        /// <summary>The painting's width over its height (Assets/UI/StartScreen/declaration-hall.jpg, 2048x1152).</summary>
        public const float ArtAspect = 16f / 9f;
        /// <summary>The URL flag (WebGL) or command-line flag (desktop, "-nostart") that skips the screen by hand.</summary>
        public const string NoStartFlag = "nostart";

        /// <summary>Whether the screen is skipped: the automated runs (the golden check, the smoke run, the shot, frame and
        /// pixel-error profiles, the unit showcase and the border proof, all of which drive or measure the map straight away),
        /// or "nostart" asked for by hand. Flags follow each instrument's own rule: a whole URL query parameter on WebGL
        /// (UrlFlags.Has), a command-line flag on desktop.</summary>
        public static bool Skipped(string? url, string[] args, bool webgl)
        {
            if (GameRoot.SmokeTurns(url, args, webgl) > 0) return true;
            var flags = new[] { NoStartFlag, "golden", "shot", "vprofile", "profile", "units", Statecraft.Game.Map.BorderProof.Flag };
            return webgl ? flags.Any(f => UrlFlags.Has(url, f)) : flags.Any(f => args.Contains("-" + f));
        }

        public static bool Skipped() =>
            Skipped(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        readonly VisualElement hudRoot, screen, art, body, panel;
        readonly Button continueButton, newGameButton;
        readonly NewGamePresenter newGame;
        readonly Action<bool>? shownChanged;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<GeometryChangedEvent> onGeometry;
        readonly Action onContinue, onNewGame, onStarted;
        readonly PressSource presses;
        readonly Action unbindStar;
        readonly Button starButton;
        readonly EventCallback<GeometryChangedEvent> onLaidOut;
        readonly VisualElement[] laidOut;
        string? loggedCentres;

        /// <summary>The buttons ButtonCentres reports, by name.</summary>
        public static readonly string[] Buttons = { "start-continue", "start-new-game", "start-star", "start-listen" };

        /// <summary>Continue is offered: the session continues the autosave.</summary>
        public bool Resumed { get; private set; }

        readonly DeclarationRecording? recording;

        /// <param name="recording">The "Play the recording" button's player (null in tests that do not need it); the screen
        /// stops it when it closes.</param>
        /// <param name="text">The UI's strings for the source line and the two buttons (#193); null keeps Game.uxml's English
        /// (tests). The title is the game's name and the English lines are the Declaration's official translation: neither
        /// is translated.</param>
        public StartScreenPresenter(VisualElement root, NewGamePresenter newGame, bool resumed, Action<bool>? shownChanged = null,
            DeclarationRecording? recording = null, UiText? text = null)
        {
            this.recording = recording;
            this.newGame = newGame; this.shownChanged = shownChanged; Resumed = resumed;
            hudRoot = root.Q("root") ?? root;
            screen = root.Q<VisualElement>("start-screen");
            art = screen.Q<VisualElement>("start-art");
            body = screen.Q<VisualElement>("start-body");
            panel = screen.Q<VisualElement>("start-panel");
            continueButton = screen.Q<Button>("start-continue");
            newGameButton = screen.Q<Button>("start-new-game");
            if (text != null)
            {
                screen.Q<Label>("start-source").text = text["start.source"];
                continueButton.text = text["start.continue"]; newGameButton.text = text["start.new_game"];
                screen.Q<Button>("start-star").text = text["community.star"];
            }
            starButton = screen.Q<Button>("start-star");
            unbindStar = ExternalLink.Bind(starButton, ExternalLink.Repository);   // #1168
            starButton.generateVisualContent += PaintStarSigns;
            onContinue = Continue; onNewGame = NewGame; onStarted = Started;
            continueButton.clicked += onContinue;
            newGameButton.clicked += onNewGame;
            newGame.Started += onStarted;
            // Escape (the UI's Cancel) from a focused start button withdraws an open New Game request. GameRoot also polls
            // the Escape key for when no element has the focus (Escape); doing it twice is harmless.
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            screen.RegisterCallback(onCancel);
            onGeometry = _ => Fit();
            screen.RegisterCallback(onGeometry);
            panel.RegisterCallback(onGeometry);                // the panel's height decides where it goes
            presses = new PressSource(screen);
            // ?navlog: a button moves when it, its row, the panel (bottom-anchored, it grows upward) or the body moves.
            onLaidOut = _ => LogCentres();
            laidOut = new[] { screen, body, panel, screen.Q("start-recording"), screen.Q("start-actions") }
                .Concat(Buttons.Select(n => (VisualElement)screen.Q<Button>(n))).ToArray();
            foreach (var e in laidOut) e.RegisterCallback(onLaidOut);
        }

        /// <summary>Each shown button's centre as a fraction of the panel's width and height, in Buttons' order:
        /// "start buttons start-new-game=0.4167,0.9278 start-star=0.5833,0.9278 ..." (invariant culture). A fraction maps
        /// onto the browser's viewport at any DPR or panel scale; tools/web_check.py clicks there (--startscreen,
        /// --persistence). Null while the screen is closed or not laid out.</summary>
        public string? ButtonCentres()
        {
            var window = screen.panel?.visualTree.worldBound ?? Rect.zero;
            if (!IsOpen || float.IsNaN(window.width) || window.width <= 0 || window.height <= 0) return null;
            var parts = new List<string>();
            foreach (var name in Buttons)
            {
                var b = screen.Q<Button>(name);
                var r = b.worldBound;
                if (!Displayed(b) || float.IsNaN(r.width) || r.width <= 0 || r.height <= 0) continue;
                parts.Add(FormattableString.Invariant(
                    $"{name}={(r.center.x - window.xMin) / window.width:0.0000},{(r.center.y - window.yMin) / window.height:0.0000}"));
            }
            return parts.Count == 0 ? null : "start buttons " + string.Join(" ", parts);
        }

        static bool Displayed(VisualElement e)
        {
            for (var v = e; v != null; v = v.hierarchy.parent)
                if (v.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        /// <summary>Under ?navlog (DiagLog.Nav): logs ButtonCentres on a NAV line whenever it changes while the screen shows.</summary>
        void LogCentres()
        {
            if (!DiagLog.Nav) return;
            var centres = ButtonCentres();
            if (centres == null || centres == loggedCentres) return;
            loggedCentres = centres;
            DiagLog.NavLine(centres);
        }

        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        /// <summary>Shows the screen with Continue only when there is a save to continue, and puts the keyboard focus on the
        /// first button.</summary>
        public void Show()
        {
            continueButton.style.display = Resumed ? DisplayStyle.Flex : DisplayStyle.None;
            hudRoot.AddToClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);   // the HUD under it: hidden by the stylesheet, disabled so no key reaches it
            Fit();
            // Focus once the styles have resolved (a button still hidden by the stylesheet cannot take it).
            var first = Resumed ? continueButton : newGameButton;
            first.Focus();
            screen.schedule.Execute(() => { if (IsOpen && first.focusController?.focusedElement == null) first.Focus(); });
            DiagLog.BootPhase("start screen shown");
            shownChanged?.Invoke(true);
        }

        /// <summary>Hides the screen; the game is on the map. Safe to call when it is not open. Closed from the keyboard, the
        /// focus goes to End Turn (#41 M1), unless a decision card is open: the card takes it (EventCardPresenter).</summary>
        public void Close() => Hide(giveFocus: true);

        void Hide(bool giveFocus)
        {
            if (!IsOpen) return;
            recording?.Stop();
            hudRoot.RemoveFromClassList(OpenClass);
            loggedCentres = null;
            ScreenNav.DisableCovered(hudRoot);
            var focused = screen.focusController?.focusedElement as VisualElement;
            if (focused != null && screen.Contains(focused))
            {
                focused.Blur();
                if (giveFocus && presses.ByKeyboard && !hudRoot.ClassListContains(EventCardPresenter.OpenClass)) FocusReturn.To(hudRoot.Q<Button>("end-turn"));
            }
            shownChanged?.Invoke(false);
        }

        /// <summary>Escape on the screen: withdraws an open New Game request. True when it did.</summary>
        public bool Escape()
        {
            if (!IsOpen || !newGame.Armed) return false;
            newGame.Cancel();
            return true;
        }

        void Continue()
        {
            if (!IsOpen || !Resumed) return;
            newGame.Cancel();
            Close();
        }

        void NewGame()
        {
            if (!IsOpen) return;
            if (!Resumed) { Close(); return; }   // nothing to wipe: the session is already a new game
            newGame.Press();                      // arms on the first press; the second starts the new game (Started closes)
        }

        void Started()
        {
            Resumed = false;                      // the save is gone (or will be replaced): nothing to continue any more
            Close();
        }

        /// <summary>Lays the painting out for the window. At 16:9 and wider it fills the window (cover, cropped at the
        /// bottom) with the panel at the foot over the audience. In a narrower window that is tall enough for the painting's
        /// full-width 16:9 band and the panel under it (a phone held upright, a square window), the band and the panel are
        /// stacked and centred together (BandClass), so nothing of the painting is cropped. Otherwise (16:10, 4:3) it fills
        /// the window anchored at its top centre, so both sides are cropped alike and the aisle stays in the middle.</summary>
        void Fit()
        {
            var size = screen.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0 || size.y <= 0) return;
            var bandHeight = size.x / ArtAspect;
            var panelHeight = panel.layout.height;
            var band = size.x / size.y < ArtAspect - 0.001f && !float.IsNaN(panelHeight) && panelHeight > 0
                && bandHeight + panelHeight + BodyPadding <= size.y;
            screen.EnableInClassList(BandClass, band);
            var top = band ? Mathf.Floor((size.y - bandHeight - panelHeight - BodyPadding) / 2f) : 0f;
            art.style.top = band ? top : new StyleLength(StyleKeyword.Null);
            art.style.height = band ? bandHeight : new StyleLength(StyleKeyword.Null);
            body.style.top = band ? top + bandHeight : new StyleLength(StyleKeyword.Null);
            body.style.justifyContent = band ? Justify.FlexStart : new StyleEnum<Justify>(StyleKeyword.Null);
        }

        /// <summary>The start body's top and bottom padding together in the band layout (Statecraft.uss .start-band .start-body).</summary>
        const float BodyPadding = 32f;

        /// <summary>Star on GitHub's star and GitHub mark (#1168), side by side in the button's left padding (the right in
        /// Hebrew; Statecraft.uss .start-star keeps 80 px for them), painted into the button so it stays a leaf sized by its
        /// text (UiIcon.Paint). Lit while the button has the keyboard focus, as the other gold signs.</summary>
        static void PaintStarSigns(MeshGenerationContext m)
        {
            const float Size = 22f, Edge = 18f, Gap = 6f;
            var e = m.visualElement;
            bool rtl = UiLanguage.Within(e), lit = UiIcon.IsFocused(e);
            float y = (e.layout.height - Size) / 2f;
            float first = rtl ? e.layout.width - Edge - Size : Edge;
            float second = rtl ? first - Gap - Size : first + Size + Gap;
            UiIcon.Paint(m, "star", new Rect(first, y, Size, Size), lit);
            UiIcon.Paint(m, "mark-github", new Rect(second, y, Size, Size), lit);
        }

        public void Dispose()
        {
            Hide(giveFocus: false);
            presses.Dispose();
            continueButton.clicked -= onContinue;
            newGameButton.clicked -= onNewGame;
            newGame.Started -= onStarted;
            unbindStar();
            starButton.generateVisualContent -= PaintStarSigns;
            screen.UnregisterCallback(onCancel);
            screen.UnregisterCallback(onGeometry);
            panel.UnregisterCallback(onGeometry);
            foreach (var e in laidOut) e.UnregisterCallback(onLaidOut);
        }
    }
}
