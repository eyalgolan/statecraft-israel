#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The start menu's music (#39): the main theme by Afik (afikomposer, PR #2240; CC BY-NC-ND 4.0, owner 2 Nov 2026),
    /// an OGG in StreamingAssets/audio/music/ (docs/audio.md). It loops while the start screen shows and fades out over
    /// FadeMs of real time when a game starts; it never plays in the game. The whole file is downloaded when it is about to
    /// play (UnityWebRequestMultimedia, DeclarationRecording's fetch; not streamed) and decoded (by the browser on the web);
    /// the clip is released when the fade ends. Browsers block audio until the page's first activation (a click, a tap or a
    /// key other than Escape), so on the web nothing is fetched before one (GestureSeen; the desktop has no such rule). The
    /// Settings screen's Music row sets Volume (SettingsPresenter; MenuMusic's own default is the row's Low, so a missed
    /// wiring can't play loud); at 0 it is not even fetched. It drops to DipLevel while Ben-Gurion's recording plays on the
    /// same screen (Duck). No theme (main before #2240, a 404 or any failed fetch) is silence for the rest of the page
    /// (Absent): one fetch at most, one plain log line, no warning and no error. GameRoot keeps one for the page, across the
    /// rebuild a language switch makes, and builds it only in a player that shows the start screen, never in the automated
    /// runs (StartScreenPresenter.Skipped) or in EditMode. Presentation only: the sim and the golden hashes never see it.</summary>
    public sealed class MenuMusic : IDisposable
    {
        /// <summary>The theme's path under StreamingAssets. A renamed file is renamed here too (MenuMusicTests checks a
        /// committed file against it).</summary>
        public const string File = "audio/music/Main_1.3.ogg";

        /// <summary>The fade when a game starts (and the dip under the recording), in real time, and the watch's period while
        /// it loads or plays.</summary>
        public const int FadeMs = 1000, WatchMs = 50;

        /// <summary>The theme's level under Ben-Gurion's recording, as a share of the Music volume. The owner's ruling on #2297
        /// (2 Nov 2026), "Dip under the recording (Recommended)": "The theme drops low while the recording plays, then comes
        /// back". Low, not silence.</summary>
        public const float DipLevel = 0.2f;

        /// <summary>The volume until Settings sets one: the Music row's default, Low (owner, #2297, 2 Nov 2026: "Low by default
        /// (Recommended)"; SettingsPresenter.SoundLevels' "low").</summary>
        public const float DefaultVolume = 0.35f;

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

        /// <summary>True once the page has had an activation (a click, a tap or a key other than Escape): the browser's rule is
        /// per page, so it holds for every instance.</summary>
        public static bool GestureSeen { get; internal set; }

        /// <summary>True once a fetch failed or the file is missing: no theme for the rest of the page, whatever instance asks
        /// again (no second fetch, no second log line). A failed fetch for any reason counts: a reload tries again.</summary>
        public static bool ThemeAbsent { get; internal set; }

        static readonly TrickleDown[] Phases = { TrickleDown.TrickleDown, TrickleDown.NoTrickleDown };

        readonly VisualElement root, listener; readonly bool web; readonly string url;
        readonly DeclarationRecording.Fetch fetch; readonly IOutput output; readonly Func<double> now;
        readonly EventCallback<PointerDownEvent> onPointerDown; readonly EventCallback<PointerUpEvent> onPointerUp;
        readonly EventCallback<KeyDownEvent> onKey;
        readonly IVisualElementScheduledItem timer;
        DeclarationRecording.IRequest? request; AudioClip? clip; VisualElement? hooked;
        bool wanted, playing, fadingOut; int generation; double lastTick;
        float volume = DefaultVolume, dip = 1f, fade = 1f;

        /// <summary>Absent for the page once ThemeAbsent is set; otherwise the instance's own phase.</summary>
        public Phase State => ThemeAbsent ? Phase.Absent : clip == null ? (request != null ? Phase.Loading : Phase.Idle)
            : !playing ? Phase.Loading : fadingOut ? Phase.Fading : Phase.Playing;

        /// <summary>Whether the start screen wants it (Show).</summary>
        internal bool Wanted => wanted;

        /// <summary>True while the theme must drop to DipLevel (GameRoot: while Ben-Gurion's recording plays).</summary>
        public Func<bool>? Duck { get; set; }

        /// <summary>0 (off) to 1 (the theme as mastered); SettingsPresenter sets it from the player's choice. Applies at once;
        /// raised from 0, it starts the theme if the start screen is up.</summary>
        public float Volume
        {
            get => volume;
            set { volume = Mathf.Clamp01(value); Apply(); TryStart(); }
        }

        /// <param name="web">The browser's rule: nothing before the page's first activation.</param>
        /// <param name="url">Test seam; null is Url().</param>
        /// <param name="fetch">Test seam; null fetches with UnityWebRequestMultimedia as Ogg Vorbis.</param>
        /// <param name="output">Test seam; null plays through an AudioSource.</param>
        /// <param name="now">Test seam: the clock, in seconds; null is Time.realtimeSinceStartupAsDouble.</param>
        public MenuMusic(VisualElement root, bool web, string? url = null, DeclarationRecording.Fetch? fetch = null, IOutput? output = null,
            Func<double>? now = null)
        {
            this.root = root; this.web = web;
            this.url = url ?? Url();
            this.fetch = fetch ?? DeclarationRecording.WebFetchOgg;
            this.output = output ?? new SourceOutput();
            this.now = now ?? (() => Time.realtimeSinceStartupAsDouble);
            // On the panel's own root: a key with nothing focused goes there, never through the document's root below it.
            listener = root.panel?.visualTree ?? root;
            onPointerDown = e => PointerDown(e);
            onPointerUp = e => { if (e.pointerType != UnityEngine.UIElements.PointerType.mouse) Gesture(); };     // a touch's release activates
            onKey = e => { if (e.keyCode != KeyCode.Escape && e.character != '\u001b') Gesture(); };   // Escape never activates
            // Both phases: trickle-down sees an event before any element can stop it; bubble-up sees one aimed at the panel's
            // root itself. Gesture is idempotent, so an event seen twice counts once.
            foreach (var phase in Phases)
            {
                listener.RegisterCallback(onPointerDown, phase);
                listener.RegisterCallback(onPointerUp, phase);
                listener.RegisterCallback(onKey, phase);
            }
            timer = root.schedule.Execute(Tick).Every(WatchMs);
            timer.Pause();
        }

        /// <summary>The theme's URL, with the build's id on the web (issue #437), as DeclarationRecording.Url.</summary>
        public static string Url()
        {
            var u = Application.streamingAssetsPath + "/" + File;
            return u.Contains("://") ? Boot.GameInputs.WithBuildId(u, Application.buildGUID) : "file://" + u;
        }

        /// <summary>The start screen shows (true: play, once allowed; a fade under way turns back up) or closes (false: a game
        /// starts, so fade out; a load under way is cancelled).</summary>
        public void Show(bool shown)
        {
            wanted = shown;
            if (shown)
            {
                if (playing) fadingOut = false;   // shown again before the fade ended: it comes back up from where it is
                TryStart();
                return;
            }
            if (playing) fadingOut = true;
            else if (clip != null || request != null) Stop();
        }

        /// <summary>A mouse press activates. A touch or pen press does not (iOS); its release does. The element pressed (a
        /// button) captures the pointer, and a captured release goes to it alone, never through the panel's root, so the
        /// release is also heard on the element pressed.</summary>
        void PointerDown(PointerDownEvent e)
        {
            if (e.pointerType == UnityEngine.UIElements.PointerType.mouse) { Gesture(); return; }
            if (GestureSeen || !(e.target is VisualElement pressed) || pressed == listener) return;
            Unhook();
            hooked = pressed;
            hooked.RegisterCallback(onPointerUp, TrickleDown.TrickleDown);
        }

        void Unhook()
        {
            hooked?.UnregisterCallback(onPointerUp, TrickleDown.TrickleDown);
            hooked = null;
        }

        void Gesture()
        {
            if (GestureSeen) return;
            GestureSeen = true;
            Unhook();
            TryStart();
        }

        void TryStart()
        {
            if (!wanted || ThemeAbsent || clip != null || request != null || volume <= 0f || (web && !GestureSeen)) return;
            if (url.StartsWith("file://", StringComparison.Ordinal) && !System.IO.File.Exists(url.Substring("file://".Length)))
            {
                Absent(null);
                return;
            }
            var mine = ++generation;
            var r = fetch(url, (c, error) =>
            {
                if (mine != generation) { Release(c); return; }   // stopped meanwhile
                request = null;
                if (c == null) { Absent(error); return; }
                clip = c;
                Begin();
            });
            if (mine == generation && clip == null && !ThemeAbsent) { request = r; timer.Resume(); }   // still on its way
        }

        /// <summary>No theme: silent for the rest of the page, with one line in the log (never a warning: main ships no
        /// theme until #2240 lands).</summary>
        void Absent(string? why)
        {
            Stop();
            ThemeAbsent = true;
            Debug.Log("MUSIC no theme at " + File + ": the start menu is silent" + (why == null ? "" : " (" + why + ")"));
        }

        /// <summary>Plays the clip once it is decoded; until then the watch asks again.</summary>
        void Begin()
        {
            var ready = output.Ready(clip!);
            if (ready == null) { Absent("the clip did not decode"); return; }
            if (ready == false) { timer.Resume(); return; }
            playing = true; fadingOut = false; fade = 1f;
            dip = Duck?.Invoke() == true ? DipLevel : 1f;   // the recording may have started since the fetch did (N1)
            lastTick = now();
            Apply();
            output.Play(clip!);
            timer.Resume();
        }

        /// <summary>Every WatchMs while loading or playing: the play once decoded, and the dip and the fade, by the real time
        /// since the last tick (a slow or hitching frame rate does not stretch them). Allocates nothing.</summary>
        internal void Tick()
        {
            if (clip == null) return;
            if (!playing) { Begin(); return; }
            var t = now();
            float step = (float)((t - lastTick) * 1000.0 / FadeMs);
            lastTick = t;
            dip = Duck?.Invoke() == true ? Mathf.Max(DipLevel, dip - step) : Mathf.Min(1f, dip + step);
            fade = fadingOut ? Mathf.Max(0f, fade - step) : Mathf.Min(1f, fade + step);
            if (fadingOut && fade <= 1e-4f) { Stop(); return; }   // float steps: 20 x 0.05 may leave a hair above 0
            // (Shown again before this point, the fade had already turned back up: Show(true) clears fadingOut.)
            Apply();
        }

        void Apply()
        {
            if (playing) output.Volume = volume * dip * fade;
        }

        /// <summary>Stops it, cancels a fetch and releases the clip. Safe to call at any time.</summary>
        void Stop()
        {
            generation++;
            var r = request; request = null; r?.Abort();
            timer.Pause();
            output.Stop();
            Release(clip); clip = null;
            playing = false; fadingOut = false;
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
            Unhook();
            foreach (var phase in Phases)
            {
                listener.UnregisterCallback(onPointerDown, phase);
                listener.UnregisterCallback(onPointerUp, phase);
                listener.UnregisterCallback(onKey, phase);
            }
            output.Dispose();
        }

        /// <summary>A looping AudioSource on its own GameObject, made at the first Play.</summary>
        sealed class SourceOutput : IOutput
        {
            GameObject? go; AudioSource? source; float volume = DefaultVolume;

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
