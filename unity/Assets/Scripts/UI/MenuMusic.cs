#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

        /// <summary>The theme's loop points (#2338), Afik's proposed 00:10 to 04:03 (ASSETS.yaml), as data:
        /// Assets/Resources/Music/menu-music.yaml (`file`, `loop_start_s`, `loop_end_s`). In Resources, not next to the OGG:
        /// the music folder holds OGG files only, and Resources needs no second fetch on the web.</summary>
        public const string LoopResource = "Music/menu-music";

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

        /// <summary>Where the theme plays. The game's is two AudioSources on their own GameObject (SourceOutput).</summary>
        public interface IOutput : IDisposable
        {
            /// <summary>As DeclarationRecording.IOutput.Ready: true once decoded, false while not yet, null when it failed.</summary>
            bool? Ready(AudioClip clip);
            /// <summary>Plays the clip from sample 0 to the loop's End once, then its Start to End again and again
            /// (LoopRegion.SampleAt), the next part scheduled on the same audio-clock time the last one ends.</summary>
            void Play(AudioClip clip, LoopRegion loop);
            /// <summary>Every WatchMs while it plays: time to schedule the next seam.</summary>
            void Watch();
            void Stop();
            float Volume { set; }
        }

        /// <summary>The part of the clip that loops, in samples at Frequency: playback runs from sample 0 to End once, then
        /// from Start to End again and again. The whole clip is (0, samples).</summary>
        public readonly struct LoopRegion : IEquatable<LoopRegion>
        {
            public readonly int Start, End, Frequency;
            public LoopRegion(int start, int end, int frequency) { Start = start; End = end; Frequency = frequency; }

            /// <summary>The clip's sample heard after <paramref name="played"/> samples of playback.</summary>
            public long SampleAt(long played) => played < End ? played : Start + (played - End) % (End - Start);

            /// <summary>How many samples of playback come before seam <paramref name="n"/> (from 0), where End hands over to Start.
            /// Counted in samples, not by adding seconds up, so no rounding drifts over an hour on the menu.</summary>
            public long SeamAt(long n) => End + n * (End - Start);

            public bool Equals(LoopRegion o) => Start == o.Start && End == o.End && Frequency == o.Frequency;
            public override bool Equals(object? o) => o is LoopRegion r && Equals(r);
            public override int GetHashCode() => (Start * 31 + End) * 31 + Frequency;
            public override string ToString() => Start + ".." + End + " @ " + Frequency + " Hz";
        }

        sealed class LoopFile
        {
            public string File { get; set; } = "";
            public double LoopStartS { get; set; } = -1;
            public double LoopEndS { get; set; } = -1;
        }

        /// <summary>The loop region the file gives for a clip of <paramref name="samples"/> at <paramref name="frequency"/>;
        /// no file, a broken one, one for another file, or a region that is empty, reversed or outside the clip is the whole
        /// clip, with <paramref name="why"/> saying which (null when the file's region holds).</summary>
        internal static LoopRegion Loop(string? yaml, int samples, int frequency, out string? why)
        {
            why = null;
            LoopFile? f = null;
            if (string.IsNullOrWhiteSpace(yaml)) why = "no Resources/" + LoopResource + ".yaml";
            else
            {
                try
                {
                    f = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .WithDuplicateKeyChecking().Build().Deserialize<LoopFile>(yaml);
                }
                catch (YamlDotNet.Core.YamlException e) { why = e.Message; }
            }
            if (why == null && f == null) why = "an empty loop file";
            if (why == null && f!.File != File) why = "the loop file is for '" + f.File + "'";
            if (why != null) return new LoopRegion(0, samples, frequency);
            long start = (long)Math.Round(f!.LoopStartS * frequency), end = (long)Math.Round(f.LoopEndS * frequency);
            if (start < 0 || end <= start || end > samples)
            {
                why = "loop " + f.LoopStartS + " s to " + f.LoopEndS + " s does not fit a clip of " + samples + " samples at " + frequency + " Hz";
                return new LoopRegion(0, samples, frequency);
            }
            return new LoopRegion((int)start, (int)end, frequency);
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
        readonly DeclarationRecording.Fetch fetch; readonly IOutput output; readonly Func<double> now; readonly string? loopYaml;
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
        /// <param name="loop">Test seam: the loop file's text; null reads Resources/Music/menu-music.yaml (LoopResource).</param>
        public MenuMusic(VisualElement root, bool web, string? url = null, DeclarationRecording.Fetch? fetch = null, IOutput? output = null,
            Func<double>? now = null, string? loop = null)
        {
            loopYaml = loop ?? Resources.Load<TextAsset>(LoopResource)?.text;
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
            var loop = Loop(loopYaml, clip!.samples, clip.frequency, out var why);
            if (why != null) Debug.Log("MUSIC no loop region for " + File + ": the whole clip loops (" + why + ")");
            output.Play(clip, loop);
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
            output.Watch();
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

        /// <summary>Two AudioSources on their own GameObject, made at the first Play. The whole clip loops on one
        /// (AudioSource.loop). A loop region hands over between the two at each seam on the audio clock: the playing one's
        /// scheduled end and the other's scheduled start (at Start) are the same dspTime (the design; nobody has listened to the
        /// seam in a web build yet, #2338). On the web, Unity's Audio.js turns these into AudioBufferSourceNode start(when,
        /// offset) and stop(when) (Chromium decodes every clip to a buffer). AudioSource.loop cannot do it: it always loops the
        /// whole clip.</summary>
        internal sealed class SourceOutput : IOutput
        {
            /// <summary>The first play's lead on the audio clock, so both of the first seam's halves can be scheduled.</summary>
            const double Lead = 0.1;
            readonly Func<double> dsp; readonly Func<ISource> make;
            /// <summary>The sources are AudioSources on our own GameObject: something else may destroy it (Unity's null), and
            /// the next Play then makes both again.</summary>
            readonly bool own;
            GameObject? go; ISource? playing, next; float volume = DefaultVolume;
            LoopRegion loop; bool region; double startDsp, seamDsp; long seam;

            /// <param name="dsp">Test seam: the audio clock, in seconds; null is AudioSettings.dspTime.</param>
            /// <param name="make">Test seam: makes one of the two sources; null adds an AudioSource to the MenuMusic
            /// GameObject. EditMode has no running audio clock, so a test records the scheduling calls instead.</param>
            internal SourceOutput(Func<double>? dsp = null, Func<ISource>? make = null)
            {
                this.dsp = dsp ?? (() => AudioSettings.dspTime);
                this.make = make ?? MakeSource;
                own = make == null;
            }

            public bool? Ready(AudioClip clip) => DeclarationRecording.SourceOutput.ReadyState(clip);

            public void Play(AudioClip clip, LoopRegion loop)
            {
                if (playing == null || (own && go == null)) { playing = make(); next = make(); }
                Stop();
                this.loop = loop;
                region = loop.Start != 0 || loop.End != clip.samples;
                playing!.Clip = next!.Clip = clip;
                playing.Volume = next.Volume = volume;
                playing.Loop = !region;
                playing.TimeSamples = (int)loop.SampleAt(0);
                if (!region) { playing.Play(); return; }
                startDsp = dsp() + Lead;
                playing.PlayScheduled(startDsp);
                seam = 0;
                Schedule();
            }

            ISource MakeSource()
            {
                if (go == null) go = new GameObject("MenuMusic");
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.loop = false; s.spatialBlend = 0f;
                return new UnitySource(s);
            }

            /// <summary>Seam number `seam`: the playing source ends and the other starts where the loop goes on, at one time.</summary>
            void Schedule()
            {
                seamDsp = startDsp + (double)loop.SeamAt(seam) / loop.Frequency;
                playing!.SetScheduledEndTime(seamDsp);
                next!.TimeSamples = (int)loop.SampleAt(loop.SeamAt(seam));
                next.PlayScheduled(seamDsp);
            }

            /// <summary>Once a seam has passed, the source that took over plays on and the next seam is scheduled, a whole
            /// region (3:53 for the theme) ahead.</summary>
            public void Watch()
            {
                var now = dsp();
                if (!region || now < seamDsp) return;
                (playing, next) = (next, playing);
                seam++;
                if (startDsp + (double)loop.SeamAt(seam) / loop.Frequency <= now + Lead)
                {
                    // More than a region late: a hidden tab stalls this watch but not the audio clock, so the source that took
                    // over ran past the loop end and stopped. Scheduling the missed seams would restart once per region in a
                    // row; instead the loop starts again at its start, as if seam 0 fell just now.
                    var at = now + Lead;
                    startDsp = at - (double)loop.End / loop.Frequency;
                    playing!.TimeSamples = loop.Start;
                    playing.PlayScheduled(at);
                    seam = 1;
                }
                Schedule();
            }

            public void Stop()
            {
                region = false;
                if (playing != null) { playing.Stop(); playing.Clip = null; }
                if (next != null) { next.Stop(); next.Clip = null; }
            }

            public float Volume
            {
                set
                {
                    volume = value;
                    if (playing != null) playing.Volume = value;
                    if (next != null) next.Volume = value;
                }
            }

            public void Dispose()
            {
                if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
                go = null; playing = null; next = null;
            }
        }

        /// <summary>The calls SourceOutput makes on one of its two sources: an AudioSource in the game (UnitySource), a
        /// recorder in MenuMusicTests, so the handover at each seam can be checked without an audio clock.</summary>
        internal interface ISource
        {
            AudioClip? Clip { set; }
            float Volume { set; }
            bool Loop { set; }
            int TimeSamples { set; }
            void Play();
            void PlayScheduled(double at);
            void SetScheduledEndTime(double at);
            void Stop();
        }

        /// <summary>Every call skips a destroyed AudioSource (Unity's null): a scene teardown can destroy the MenuMusic
        /// GameObject before GameRoot.OnDisable disposes the music.</summary>
        sealed class UnitySource : ISource
        {
            readonly AudioSource s;
            public UnitySource(AudioSource s) { this.s = s; }
            public AudioClip? Clip { set { if (s != null) s.clip = value; } }
            public float Volume { set { if (s != null) s.volume = value; } }
            public bool Loop { set { if (s != null) s.loop = value; } }
            public int TimeSamples { set { if (s != null) s.timeSamples = value; } }
            public void Play() { if (s != null) s.Play(); }
            public void PlayScheduled(double at) { if (s != null) s.PlayScheduled(at); }
            public void SetScheduledEndTime(double at) { if (s != null) s.SetScheduledEndTime(at); }
            public void Stop() { if (s != null) s.Stop(); }
        }
    }
}
