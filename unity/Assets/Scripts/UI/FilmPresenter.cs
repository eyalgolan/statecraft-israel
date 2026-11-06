#nullable enable
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The opening film (docs/event-videos.md, "The opening film"), over the three-photograph opening, which stays
    /// the fallback: Play always ends in its continuation, whether the film ran, was skipped, failed or is not there. The
    /// picture and the sound play through AdvisorMedia: neither starts until both are ready, the sound's clock drives the
    /// captions, and the picture is seeked back when it drifts. Skip shows and takes the focus from the first frame, before
    /// anything has loaded; Escape does the same; each closes the film only, one layer (#2609). A film with no picture file
    /// goes straight on, with one plain log line, no warning and no error, and is not asked for again. It wears the opening's
    /// OpenClass while it shows, so the stylesheet, ScreenNav and the advisors treat it as the opening they already wait for.
    /// Nothing is fetched before the player's click. Presentation only: no bridge, commands, RNG or save access.</summary>
    public sealed class FilmPresenter : IDisposable
    {
        /// <summary>How long the check for the picture file may take before the film is treated as absent (DESIGN VALUE).</summary>
        public const long ProbeTimeoutMs = 5000;

        /// <summary>Asks whether <paramref name="url"/> exists and calls <paramref name="found"/> once, at once or later. The
        /// returned request, if any, aborts the check.</summary>
        public delegate DeclarationRecording.IRequest? Probe(string url, Action<bool> found);

        /// <summary>Test seam for the tests that build a whole GameRoot, which has no probe to pass: the film exists or not
        /// as the test says. Reset it after the test.</summary>
        internal static Probe? ProbeForTests;

        readonly VisualElement root, layer, picture;
        readonly Label caption;
        readonly Button skip;
        readonly FilmContent.Document data;
        readonly AdvisorCatalog.Media row;
        readonly AdvisorMedia media;
        readonly Probe probe;
        readonly Action<bool> shownChanged;
        readonly Action onSkip;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<NavigationMoveEvent> onMove;
        Action? then;
        DeclarationRecording.IRequest? request;
        IVisualElementScheduledItem? watch;
        bool probing, absent, disposed;
        int generation, closedFrame = -1;

        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public bool HoldsThePointer => IsOpen || ClosedThisFrame;

        /// <param name="shownChanged">Told true before the film shows, false as it closes.</param>
        /// <param name="media">The film's own player: the caption language is the UiText it was made with.</param>
        /// <param name="probe">Test seam; null checks the file (Exists).</param>
        public FilmPresenter(VisualElement root, FilmContent.Document data, UiText text, Action<bool> shownChanged, AdvisorMedia media, Probe? probe = null)
        {
            this.root = root.Q("root") ?? root; this.data = data; this.shownChanged = shownChanged; this.media = media;
            this.probe = probe ?? ProbeForTests ?? Exists;
            row = FilmContent.Row(data);
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            layer = root.Q("film-layer"); picture = layer.Q("film-picture");
            caption = layer.Q<Label>("film-caption"); skip = layer.Q<Button>("film-skip");
            caption.languageDirection = hebrew && data.HebrewApproved ? LanguageDirection.RTL : LanguageDirection.LTR;
            var label = layer.Q<Label>("film-label");
            label.text = text[data.AiLabel] + (data.RealRecording ? " · " + text[FilmContent.RecordingCredit] : "");
            label.languageDirection = hebrew ? LanguageDirection.RTL : LanguageDirection.LTR;
            skip.text = text["film.skip"];
            onSkip = Close;
            skip.clicked += onSkip;
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            onMove = e =>
            {   // Skip is the only stop: Tab stays on it.
                if (!IsOpen || e.direction != NavigationMoveEvent.Direction.Next && e.direction != NavigationMoveEvent.Direction.Previous) return;
                skip.Focus(); layer.focusController?.IgnoreEvent(e); e.StopPropagation();
            };
            layer.RegisterCallback(onCancel); layer.RegisterCallback(onMove);
            layer.style.display = DisplayStyle.None;
        }

        /// <summary>Plays the film, then <paramref name="then"/>; with no film to play, only <paramref name="then"/>, in this
        /// call. A replay always asks for the film; a new game only when film.yaml's auto_play_on_new_game says so.</summary>
        public void Play(bool replay, Action then)
        {
            if (disposed || IsOpen) return;
            if (absent || !replay && !data.AutoPlayOnNewGame) { then(); return; }
            int mine = ++generation;
            bool? found = null;
            var pending = probe(FilmContent.Url(data, data.Picture), answer =>
            {
                found = answer;
                if (mine == generation && IsOpen) Answered(answer);
            });
            if (found == false) { Absent(); then(); return; }           // nothing to show: the layer never opens
            this.then = then;
            IsOpen = true;
            root.AddToClassList(OpeningPresenter.OpenClass); layer.style.display = DisplayStyle.Flex;
            ScreenNav.DisableCovered(root);
            shownChanged(true);
            skip.Focus();
            layer.schedule.Execute(() => { if (IsOpen && layer.focusController?.focusedElement == null) skip.Focus(); });
            if (found == true) { Begin(); return; }
            probing = true; request = pending;
            watch = layer.schedule.Execute(ProbeTimedOut).StartingIn(ProbeTimeoutMs);
        }

        /// <summary>The check took too long (test seam: what the scheduled watch calls).</summary>
        internal void ProbeTimedOut() { if (IsOpen && probing) Answered(false); }

        void Answered(bool found)
        {
            probing = false; request = null;
            watch?.Pause(); watch = null;
            if (found) { Begin(); return; }
            Absent(); Close();
        }

        void Absent()
        {
            absent = true;
            Debug.Log("FILM absent " + data.Picture + ": the opening goes on without it");
        }

        // A film that fails to load is logged by AdvisorMedia; the player goes on to the opening.
        void Begin() => media.Play(row, FilmContent.CueStem, false, picture, caption, Close, _ => Close());

        public bool Escape() { if (!IsOpen) return false; Close(); return true; }

        void Close()
        {
            if (!IsOpen) return;
            IsOpen = false; closedFrame = Time.frameCount; generation++;
            probing = false; watch?.Pause(); watch = null;
            var pending = request; request = null; pending?.Abort();
            media.Stop();
            picture.style.backgroundImage = StyleKeyword.Null;
            layer.style.display = DisplayStyle.None; root.RemoveFromClassList(OpeningPresenter.OpenClass);
            ScreenNav.DisableCovered(root); shownChanged(false);
            var next = then; then = null;
            if (!disposed) next?.Invoke();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Close();
            skip.clicked -= onSkip;
            layer.UnregisterCallback(onCancel); layer.UnregisterCallback(onMove);
            media.Dispose();
        }

        /// <summary>The game's check: the file itself on disk (Editor, desktop), answered at once; a HEAD request on the web.
        /// A host that refuses HEAD reads as absent, which falls through to the opening.</summary>
        internal static DeclarationRecording.IRequest? Exists(string url, Action<bool> found)
        {
            const string local = "file://";
            if (url.StartsWith(local, StringComparison.OrdinalIgnoreCase)) { found(System.IO.File.Exists(url.Substring(local.Length))); return null; }
            return new HeadRequest(url, found);
        }

        sealed class HeadRequest : DeclarationRecording.IRequest
        {
            readonly UnityWebRequest request;
            bool aborted, finished;

            public HeadRequest(string url, Action<bool> found)
            {
                request = UnityWebRequest.Head(url);
                request.SendWebRequest().completed += _ =>
                {
                    finished = true;
                    bool ok = request.result == UnityWebRequest.Result.Success;
                    request.Dispose();
                    if (!aborted) found(ok);
                };
            }

            public ulong Bytes => 0;

            public void Abort() { if (aborted || finished) return; aborted = true; request.Abort(); }
        }
    }
}
