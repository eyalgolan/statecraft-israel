#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The start menu's music (#39): the main theme by Afik (afikomposer, PR #2240; CC BY-NC-ND 4.0, owner 2 Nov 2026),
    /// an OGG in StreamingAssets/audio/music/ (docs/audio.md). It loops while the start screen shows and fades out over
    /// FadeMs when a game starts; it never plays in the game. The file is fetched only when it is about to play, with
    /// UnityWebRequestMultimedia (DeclarationRecording's fetch), and the browser decodes it on the web. Browsers block audio
    /// until the page's first click or key, so on the web nothing is fetched before one (GestureSeen; the desktop has no such
    /// rule). The Settings screen's Music row sets Volume (SettingsPresenter); at 0 it is not even fetched. It drops to DipLevel
    /// while Ben-Gurion's recording plays on the same screen (Duck). No theme (main before #2240, or a 404 on the web) is
    /// silence: no fetch on the desktop, one plain log line, no warning and no error. Presentation only: the sim and the
    /// golden hashes never see it. GameRoot builds it only in a player that shows the start screen, never in the automated
    /// runs (StartScreenPresenter.Skipped) or in EditMode.</summary>
    public sealed class MenuMusic : IDisposable
    {
        /// <summary>The theme's path under StreamingAssets. A renamed file is renamed here too (MenuMusicTests checks the
        /// committed file against it).</summary>
        public const string File = "audio/music/Main_1.3.ogg";

        /// <summary>The fade when a game starts (and the dip under the recording), and the watch's step while it plays.</summary>
        public const int FadeMs = 1000, WatchMs = 50;

        /// <summary>The theme's level under Ben-Gurion's recording, as a share of the Music volume. The owner's ruling on #2297
        /// (2 Nov 2026), "Dip under the recording (Recommended)": "The theme drops low while the recording plays, then comes
        /// back". Low, not silence.</summary>
        public const float DipLevel = 0.2f;

        /// <summary>Where the theme plays. The game's is a looping AudioSource on its own GameObject (SourceOutput).</summary>
        public interface IOutput : IDisposable
        {
            /// <summary>As DeclarationRecording.IOutput.Ready: true once decoded, false while not yet, null when it failed.</summary>
            bool? Ready(AudioClip clip);
            /// <summary>Plays the clip on a loop.</summary>
            void Play(AudioClip clip);
            void Stop();
            float Volume { set; }
        }

        /// <summary>Idle (not wanted, or waiting for the screen, the first gesture or a volume above 0); Loading (fetching or
        /// decoding); Playing; Fading (the game has started); Absent (no theme: silent for the rest of the page).</summary>
        public enum Phase { Idle, Loading, Playing, Fading, Absent }

        /// <summary>True once the page has had a click or a key: the browser's rule is per page, so it holds across the
        /// rebuild a language switch makes.</summary>
        public static bool GestureSeen { get; internal set; }

        readonly VisualElement root; readonly bool web; readonly string url;
        readonly DeclarationRecording.Fetch fetch; readonly IOutput output;
        readonly EventCallback<PointerDownEvent> onPointer; readonly EventCallback<KeyDownEvent> onKey;
        readonly IVisualElementScheduledItem timer;
        DeclarationRecording.IRequest? request; AudioClip? clip;
        bool wanted; int generation, fadeLeftMs;
        float volume = 1f, dip = 1f;

        public Phase State { get; private set; }

        /// <summary>True while the theme must drop to DipLevel (GameRoot: while Ben-Gurion's recording plays).</summary>
        public Func<bool>? Duck { get; set; }

        /// <summary>0 (off) to 1 (the theme as mastered); SettingsPresenter sets it from the player's choice. Applies at once;
        /// raised from 0, it starts the theme if the start screen is up.</summary>
        public float Volume
        {
            get => volume;
            set { volume = Mathf.Clamp01(value); Apply(); TryStart(); }
        }

        /// <param name="web">The browser's rule: nothing before the page's first click or key.</param>
        /// <param name="url">Test seam; null is Url().</param>
        /// <param name="fetch">Test seam; null fetches with UnityWebRequestMultimedia as Ogg Vorbis.</param>
        /// <param name="output">Test seam; null plays through an AudioSource.</param>
        public MenuMusic(VisualElement root, bool web, string? url = null, DeclarationRecording.Fetch? fetch = null, IOutput? output = null)
        {
            this.root = root; this.web = web;
            this.url = url ?? Url();
            this.fetch = fetch ?? DeclarationRecording.WebFetchOgg;
            this.output = output ?? new SourceOutput();
            onPointer = _ => Gesture();
            onKey = _ => Gesture();
            root.RegisterCallback(onPointer, TrickleDown.TrickleDown);
            root.RegisterCallback(onKey, TrickleDown.TrickleDown);
            timer = root.schedule.Execute(Tick).Every(WatchMs);
            timer.Pause();
        }

        /// <summary>The theme's URL, with the build's id on the web (issue #437), as DeclarationRecording.Url.</summary>
        public static string Url()
        {
            var u = Application.streamingAssetsPath + "/" + File;
            return u.Contains("://") ? Boot.GameInputs.WithBuildId(u, Application.buildGUID) : "file://" + u;
        }

        /// <summary>The start screen shows (true: play, once allowed) or closes (false: a game starts, so fade out).</summary>
        public void Show(bool shown)
        {
            wanted = shown;
            if (shown) { TryStart(); return; }
            if (State == Phase.Playing) { State = Phase.Fading; fadeLeftMs = FadeMs; }
            else if (State == Phase.Loading) Stop();
        }

        void Gesture()
        {
            if (GestureSeen) return;
            GestureSeen = true;
            TryStart();
        }

        void TryStart()
        {
            if (!wanted || State != Phase.Idle || volume <= 0f || (web && !GestureSeen)) return;
            if (url.StartsWith("file://", StringComparison.Ordinal) && !System.IO.File.Exists(url.Substring("file://".Length)))
            {
                Absent(null);
                return;
            }
            State = Phase.Loading;
            timer.Resume();
            var mine = ++generation;
            var r = fetch(url, (c, error) =>
            {
                if (mine != generation || State != Phase.Loading) { Release(c); return; }   // stopped meanwhile
                request = null;
                if (c == null) { Absent(error); return; }
                clip = c;
                Tick();
            });
            if (mine == generation && State == Phase.Loading && clip == null) request = r;   // still on its way
        }

        /// <summary>No theme: silent for the rest of the page, with one line in the log (never a warning: main ships no
        /// theme until #2240 lands).</summary>
        void Absent(string? why)
        {
            Stop();
            State = Phase.Absent;
            Debug.Log("MUSIC no theme at " + File + ": the start menu is silent" + (why == null ? "" : " (" + why + ")"));
        }

        /// <summary>Every WatchMs while loading or playing: the play once decoded, the dip under the recording and the fade
        /// when a game starts. Allocates nothing.</summary>
        internal void Tick()
        {
            if (State == Phase.Loading)
            {
                if (clip == null) return;
                var ready = output.Ready(clip);
                if (ready == null) { Absent("the clip did not decode"); return; }
                if (ready == false) return;
                State = Phase.Playing;
                dip = Duck?.Invoke() == true ? DipLevel : 1f;   // the recording may have started since the fetch did (N1)
                Apply();
                output.Play(clip);
                return;
            }
            if (State != Phase.Playing && State != Phase.Fading) return;
            float step = (float)WatchMs / FadeMs;
            dip = Duck?.Invoke() == true ? Mathf.Max(DipLevel, dip - step) : Mathf.Min(1f, dip + step);
            if (State == Phase.Fading)
            {
                fadeLeftMs -= WatchMs;
                if (fadeLeftMs <= 0) { Stop(); return; }
            }
            Apply();
        }

        void Apply()
        {
            if (State != Phase.Loading && State != Phase.Playing && State != Phase.Fading) return;
            float fade = State == Phase.Fading ? (float)fadeLeftMs / FadeMs : 1f;
            output.Volume = volume * dip * fade;
        }

        /// <summary>Stops it, cancels a fetch and releases the clip. Safe to call at any time.</summary>
        void Stop()
        {
            generation++;
            var r = request; request = null; r?.Abort();
            timer.Pause();
            output.Stop();
            Release(clip); clip = null;
            State = Phase.Idle;
        }

        static void Release(AudioClip? c)
        {
            if (c == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(c); else UnityEngine.Object.DestroyImmediate(c);
        }

        public void Dispose()
        {
            wanted = false;
            Stop();
            root.UnregisterCallback(onPointer, TrickleDown.TrickleDown);
            root.UnregisterCallback(onKey, TrickleDown.TrickleDown);
            output.Dispose();
        }

        /// <summary>A looping AudioSource on its own GameObject, made at the first Play.</summary>
        sealed class SourceOutput : IOutput
        {
            GameObject? go; AudioSource? source; float volume = 1f;

            public bool? Ready(AudioClip clip) => DeclarationRecording.SourceOutput.ReadyState(clip);

            public void Play(AudioClip clip)
            {
                if (go == null)
                {
                    go = new GameObject("MenuMusic");
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
                }
                source!.volume = volume;
                source.clip = clip;
                source.Play();
            }

            public void Stop() { if (source != null) { source.Stop(); source.clip = null; } }

            public float Volume { set { volume = value; if (source != null) source.volume = value; } }

            public void Dispose()
            {
                if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
                go = null; source = null;
            }
        }
    }
}
