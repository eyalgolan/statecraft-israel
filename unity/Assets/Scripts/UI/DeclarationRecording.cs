#nullable enable
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The start screen's "Play the recording" button: David Ben-Gurion reading the sentence of the Declaration of
    /// Independence that proclaims the state, from the archive's recording of the ceremony of 14 May 1948, Israel State
    /// Archives item ISA-Collections-Recordings-00113hj (docs/declaration-recording.md; docs/oss/DECISIONS.md D-R).
    /// The file is the archive's own Opus upload, cut by stream copy (no re-encode) and then decoded to 16-bit PCM WAV,
    /// which every browser and Unity's own reader play (the Web player could not load the Ogg Opus cut); it sits in StreamingAssets (its SHA-256 is pinned in a test), is fetched only when the player presses the button,
    /// and the browser decodes it. It plays only from that press, never by itself, never on Continue or New Game; it stops
    /// when the start screen closes. While the file loads the button reads "Loading…" (pressing it again cancels); the
    /// captions and "Stop" show only once the audio is actually playing; a failed or slow load ends with a message.
    /// GameRoot builds it only when the start screen is shown, which automated runs skip (StartScreenPresenter.Skipped),
    /// and the button's row is hidden by the stylesheet until this class shows it.</summary>
    public sealed class DeclarationRecording : IDisposable
    {
        /// <summary>The recording's path under StreamingAssets, and the SHA-256 of the committed excerpt
        /// (DeclarationRecordingTests checks the shipped file against it).</summary>
        public const string File = "audio/declaration-1948-isa-00113hj.wav";
        public const string Sha256 = "24e7bc48f5d0134bbe28f0f81b3668235398e3a4bfd83d54aa8650aeae2da370";

        /// <summary>The limits before a press ends with "start.recording_failed". The fetch fails when no byte has arrived for
        /// StallTimeoutMs (a slow link that is still receiving is never cut off), or in any case after LoadCapMs (the 1.8 MB
        /// file takes about 60 s at 250 kbit/s). Decoding has its own budget, DecodeTimeoutMs from the moment the file has
        /// arrived; the audio then has StartTimeoutMs after Play to start.</summary>
        public const int StallTimeoutMs = 8000, LoadCapMs = 60000, DecodeTimeoutMs = 5000, StartTimeoutMs = 3000;
        const int WatchMs = 200;

        /// <summary>A fetch under way: the bytes received so far, and a call that aborts it (after which its done callback is
        /// not called).</summary>
        public interface IRequest
        {
            ulong Bytes { get; }
            void Abort();
        }

        /// <summary>Fetches <paramref name="url"/> as a clip and calls <paramref name="done"/> once, with the clip or with
        /// an error (never both).</summary>
        public delegate IRequest Fetch(string url, Action<AudioClip?, string?> done);

        /// <summary>Where the clip plays. The game's is an AudioSource on its own GameObject (SourceOutput).</summary>
        public interface IOutput : IDisposable
        {
            /// <summary>True when the clip's audio data is decoded and it can play (on the Web the browser decodes it after
            /// the fetch, asynchronously); asks for the data when it is not loaded yet. Null when it failed to load.</summary>
            bool? Ready(AudioClip clip);
            void Play(AudioClip clip);
            void Stop();
            bool IsPlaying { get; }
            /// <summary>The play position in seconds: it moves only when the audio really plays.</summary>
            float Position { get; }
        }

        public enum Phase { Idle, Loading, Playing }

        readonly Button button; readonly VisualElement caption, row; readonly UiText text;
        readonly Action onPress; readonly Action<string> say; readonly Fetch fetch; readonly IOutput output;
        AudioClip? clip; IRequest? request; IVisualElementScheduledItem? timer;
        bool heard, started; int waitedMs, stalledMs, generation; ulong bytesSeen;

        /// <summary>Idle; Loading from the press until the audio is heard; Playing while it is heard.</summary>
        public Phase State { get; private set; }

        /// <summary>True from the press until the recording ends, fails or is stopped (including while it loads).</summary>
        public bool Playing => State != Phase.Idle;

        /// <param name="say">Shows a message (the toast) when the recording cannot be played.</param>
        /// <param name="fetch">Test seam; null fetches with UnityWebRequestMultimedia.</param>
        /// <param name="output">Test seam; null plays through an AudioSource.</param>
        public DeclarationRecording(VisualElement root, UiText text, Action<string> say, Fetch? fetch = null, IOutput? output = null)
        {
            this.text = text; this.say = say;
            this.fetch = fetch ?? WebFetch;
            this.output = output ?? new SourceOutput();
            row = root.Q<VisualElement>("start-recording");
            button = root.Q<Button>("start-listen");
            caption = root.Q<VisualElement>("start-caption");
            button.text = text["start.listen"];
            row.Q<Label>("start-recording-credit").text = text["start.recording_credit"];
            onPress = Toggle;
            button.clicked += onPress;
            row.style.display = DisplayStyle.Flex;
            caption.style.display = DisplayStyle.None;
        }

        /// <summary>The URL the button fetches, with the build's id on the web (issue #437).</summary>
        public static string Url()
        {
            var url = Application.streamingAssetsPath + "/" + File;
            return url.Contains("://") ? Boot.GameInputs.WithBuildId(url, Application.buildGUID) : "file://" + url;
        }

        /// <summary>The button: play; or, while loading or playing, stop.</summary>
        public void Toggle()
        {
            if (Playing) { Stop(); return; }
            State = Phase.Loading;
            button.text = text["start.loading"];
            waitedMs = 0; stalledMs = 0; bytesSeen = 0; heard = false; started = false;
            timer = button.schedule.Execute(Tick).Every(WatchMs).StartingIn(WatchMs);
            var mine = ++generation;
            request = fetch(Url(), (c, error) =>
            {
                if (mine != generation || State != Phase.Loading) { Release(c); return; }   // stopped, or pressed again, meanwhile
                request = null;
                if (c == null) { Fail("fetch: " + error); return; }
                clip = c;
                waitedMs = 0;   // the decode budget starts now
                Begin();
            });
            if (State != Phase.Loading || clip != null) request = null;   // the fetch answered at once
        }

        /// <summary>Plays the fetched clip once its data is ready (Tick tries again until then).</summary>
        void Begin()
        {
            var ready = output.Ready(clip!);
            if (ready == null) { Fail("the clip did not decode"); return; }
            if (ready == false) return;
            output.Play(clip!);
            started = true;
            waitedMs = 0;
        }

        void Heard()
        {
            heard = true;
            State = Phase.Playing;
            button.text = text["start.stop"];
            caption.style.display = DisplayStyle.Flex;
            Debug.Log(FormattableString.Invariant($"RECORDING playing {File} ({clip!.length:0.00} s, at {output.Position:0.00} s)"));
        }

        /// <summary>Every WatchMs while loading or playing: the load, decode and start timeouts; the play once the data is
        /// decoded; the first moment the audio is heard (playing, and its position has moved); and its end. The end is "was
        /// heard, now is not playing", so a slow first start never cancels itself.</summary>
        internal void Tick()
        {
            if (!Playing) return;
            waitedMs += WatchMs;
            if (clip == null)
            {
                var bytes = request?.Bytes ?? 0;
                if (bytes > bytesSeen) { bytesSeen = bytes; stalledMs = 0; } else stalledMs += WatchMs;
                if (stalledMs >= StallTimeoutMs) Fail($"no data for {StallTimeoutMs} ms");
                else if (waitedMs >= LoadCapMs) Fail($"not loaded after {LoadCapMs} ms");
                return;
            }
            if (!started)
            {
                Begin();
                if (Playing && !started && waitedMs >= DecodeTimeoutMs) Fail("timed out decoding");
                return;
            }
            if (!heard)
            {
                if (output.IsPlaying && output.Position > 0f) Heard();
                else if (waitedMs >= StartTimeoutMs) Fail("did not start");
                return;
            }
            if (!output.IsPlaying) Stop();
        }

        void Fail(string why)
        {
            Debug.LogWarning("RECORDING failed " + File + ": " + why);
            Stop();
            say(text["start.recording_failed"]);
        }

        /// <summary>Stops it (the start screen closing, the button, the end of the clip) and releases the clip. Safe to
        /// call when it is not playing.</summary>
        public void Stop()
        {
            generation++;
            var r = request; request = null; r?.Abort();
            timer?.Pause(); timer = null;
            output.Stop();
            Release(clip); clip = null;
            heard = false; started = false;
            State = Phase.Idle;
            button.text = text["start.listen"];
            caption.style.display = DisplayStyle.None;
        }

        static void Release(AudioClip? c)
        {
            if (c == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(c); else UnityEngine.Object.DestroyImmediate(c);
        }

        public void Dispose()
        {
            Stop();
            button.clicked -= onPress;
            output.Dispose();
        }

        /// <summary>The game's fetch: UnityWebRequestMultimedia from StreamingAssets, as WAV.</summary>
        internal static IRequest WebFetch(string url, Action<AudioClip?, string?> done) => new WebRequest(url, done);

        sealed class WebRequest : IRequest
        {
            readonly UnityWebRequest request;
            bool aborted, finished;

            public WebRequest(string url, Action<AudioClip?, string?> done)
            {
                request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
                request.SendWebRequest().completed += _ => Done(done);
            }

            public ulong Bytes => finished ? 0 : request.downloadedBytes;

            public void Abort() { if (aborted || finished) return; aborted = true; request.Abort(); }

            void Done(Action<AudioClip?, string?> done)
            {
                finished = true;
                AudioClip? c = null; string? error = null;
                if (!aborted)
                {
                    if (request.result != UnityWebRequest.Result.Success) error = request.error;
                    else
                    {
                        try { c = DownloadHandlerAudioClip.GetContent(request); }
                        catch (Exception e) { error = e.Message; }
                        if (c == null && error == null) error = "no clip";
                    }
                }
                request.Dispose();
                if (!aborted) done(c, error);
            }
        }

        /// <summary>An AudioSource on its own GameObject, made at the first Play.</summary>
        public sealed class SourceOutput : IOutput
        {
            GameObject? go; AudioSource? source;

            public bool? Ready(AudioClip clip)
            {
                switch (clip.loadState)
                {
                    case AudioDataLoadState.Loaded: return true;
                    case AudioDataLoadState.Failed: return null;
                    case AudioDataLoadState.Unloaded: clip.LoadAudioData(); return false;
                    default: return false;
                }
            }

            public void Play(AudioClip clip)
            {
                if (go == null)
                {
                    go = new GameObject("DeclarationRecording");
                    source = go.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
                }
                source!.clip = clip;
                source.Play();
            }

            public void Stop() { if (source != null) { source.Stop(); source.clip = null; } }

            public bool IsPlaying => source != null && source.isPlaying;

            public float Position => source != null ? source.time : 0f;

            public AudioSource? Source => source;

            public void Dispose()
            {
                if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
                go = null; source = null;
            }
        }
    }
}
