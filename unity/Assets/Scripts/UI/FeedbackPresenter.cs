#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Report a bug or idea" (#1169): the button in the Settings screen's Community row and the one under the start
    /// screen's buttons open a small panel over everything, with three choices: GitHub's bug form, its feature form (both
    /// prefilled, ReportLinks) and the itch page's comments for a player with no GitHub account. The panel shows, under each
    /// GitHub choice, the exact lines that form opens with, so nothing goes anywhere the player has not seen; the game sends
    /// nothing itself, the player submits on GitHub. Each choice opens through ExternalLink, inside the real press.
    /// <para>While it shows, the root has <see cref="OpenClass"/> and every other part of the UI is disabled, so Tab stays
    /// in the panel; Close, Esc or Cancel give each part its own state back and the focus back to the button that opened it.
    /// The platform line is read on each opening (a resized window changes the screen size).</para></summary>
    public sealed class FeedbackPresenter : IDisposable
    {
        /// <summary>On the "root" element while the panel shows.</summary>
        public const string OpenClass = "feedback-open";

        readonly VisualElement hudRoot, layer;
        readonly Button bug, feature, itch, close, settingsOpen, startOpen;
        readonly Label bugPreview, featurePreview;
        readonly string version;
        readonly Action unbindFeature, unbindItch;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        Action unbindBug = () => { };
        Button? opener;
        int closedAt = -1;
        readonly List<(VisualElement part, bool enabled)> covered = new List<(VisualElement, bool)>();

        /// <param name="version">ReportLinks.Version: the app's version and the content hash.</param>
        public FeedbackPresenter(VisualElement root, UiText? text, string version)
        {
            this.version = version;
            hudRoot = root.Q("root") ?? root;
            layer = root.Q("feedback-layer");
            bug = layer.Q<Button>("feedback-bug"); feature = layer.Q<Button>("feedback-feature");
            itch = layer.Q<Button>("feedback-itch"); close = layer.Q<Button>("feedback-close");
            bugPreview = layer.Q<Label>("feedback-bug-preview"); featurePreview = layer.Q<Label>("feedback-feature-preview");
            settingsOpen = root.Q<Button>("settings-report"); startOpen = root.Q<Button>("start-report");
            if (text != null)
            {
                settingsOpen.text = startOpen.text = layer.Q<Label>("feedback-title").text = text["feedback.open"];
                layer.Q<Label>("feedback-intro").text = text["feedback.intro"];
                bug.text = text["feedback.bug"]; feature.text = text["feedback.feature"];
                itch.text = text["feedback.itch"]; close.text = text["report.close"];
            }
            featurePreview.text = Preview(ReportLinks.FeatureFields());
            unbindFeature = ExternalLink.Bind(feature, ReportLinks.Feature());
            unbindItch = ExternalLink.Bind(itch, ReportLinks.Itch);
            settingsOpen.clicked += OpenFromSettings;
            startOpen.clicked += OpenFromStart;
            close.clicked += Close;
            onCancel = e => { Close(); e.StopPropagation(); };
            layer.RegisterCallback(onCancel);
        }

        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        void OpenFromSettings() => Open(settingsOpen);
        void OpenFromStart() => Open(startOpen);

        /// <summary>Shows the panel with the platform line read now; <paramref name="from"/> gets the focus back on Close.</summary>
        public void Open(Button? from = null)
        {
            if (IsOpen) return;
            opener = from;
            var env = ReportLinks.Environment();
            bugPreview.text = Preview(ReportLinks.BugFields(version, env));
            unbindBug();
            unbindBug = ExternalLink.Bind(bug, ReportLinks.Bug(version, env));
            // Each part's own state comes back on Close. The panel takes every press meanwhile, so no screen opens or closes
            // under it; a resize may still run ScreenNav.DisableCovered, and Close then puts back the states from before.
            covered.Clear();
            foreach (var part in hudRoot.Children().Where(c => c != layer)) { covered.Add((part, part.enabledSelf)); part.SetEnabled(false); }
            hudRoot.AddToClassList(OpenClass);
            bug.Focus();
            // An element shown this frame may not take the focus yet (TurnReportPresenter does the same).
            bug.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement != bug) bug.Focus(); });
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

        /// <summary>GameRoot's Escape poll: closes the panel and says the key is used, also in the frame the panel's own
        /// Cancel already closed it, so the same Escape does not go on to close the screen or the turn report under it.</summary>
        public bool Escape()
        {
            if (IsOpen) { Close(); return true; }
            return closedAt == UnityEngine.Time.frameCount;
        }

        /// <summary>The lines a form opens with, one per field, by the template's field id ("title: Bug: ").</summary>
        static string Preview((string id, string value)[] fields) => string.Join("\n", fields.Select(f => $"{f.id}: {f.value}"));

        public void Dispose()
        {
            Close();
            unbindBug(); unbindFeature(); unbindItch();
            settingsOpen.clicked -= OpenFromSettings;
            startOpen.clicked -= OpenFromStart;
            close.clicked -= Close;
            layer.UnregisterCallback(onCancel);
        }
    }
}
