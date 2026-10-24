#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A lazy, user-started silent picture and separate narration. The audio position drives live captions;
    /// neither stream starts until both are ready. Owns its audio/output, and borrows rather than owns the card player.
    /// A row with no picture plays its narration alone and uses no picture player: the advisor's portrait stays.</summary>
    public sealed class AdvisorMedia : IDisposable
    {
        public const int TickMs = 50;
        public const double DriftS = 0.1;
        public const float InlineMinPx = 460;
        public static bool Inline(HudTier tier, float slotHeight) => tier != HudTier.ThreeRow && slotHeight >= InlineMinPx;

        public interface IPicture
        {
            void Load(string url, VisualElement target, Action<string> failed);
            bool Ready { get; }
            void Start();
            void Stop();
            double Time { get; }
            void Seek(double time);
        }

        readonly UiText text;
        readonly IPicture? clipPicture, stillPicture;
        readonly DeclarationRecording.Fetch fetch;
        readonly DeclarationRecording.IOutput output;
        IPicture? picture;
        AudioClip? narration;
        DeclarationRecording.IRequest? request;
        IVisualElementScheduledItem? timer;
        Label? caption;
        IReadOnlyList<double> cues = Array.Empty<double>();
        Action? ended;
        Action<string>? failed;
        string file = "", stem = "";
        bool started, heard, moving, disposed;
        int generation, loadingMs, stalledMs, decodingMs, startingMs, pictureMs, pictureStalledMs;
        ulong bytes, pictureBytes;

        public AdvisorMedia(UiText text, IPicture? clip, IPicture? still, DeclarationRecording.Fetch fetch, DeclarationRecording.IOutput output)
        { this.text = text; clipPicture = clip; stillPicture = still; this.fetch = fetch; this.output = output; }

        public bool Playing { get; private set; }

        public static string Url(string file)
        {
            var url = Application.streamingAssetsPath + "/" + AdvisorCatalog.MediaFolder + "/" + file;
            return url.Contains("://") ? Boot.GameInputs.WithBuildId(url, Application.buildGUID) : "file://" + url;
        }

        public void Play(AdvisorCatalog.Media media, string keyStem, bool hebrew, VisualElement surface, Label caption,
            Action ended, Action<string> failed)
        {
            if (disposed) return;
            Stop();
            this.caption = caption; caption.text = ""; this.ended = ended; this.failed = failed; stem = keyStem;
            var supplied = media.Picture ?? "";
            file = supplied;
            moving = supplied.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);
            picture = supplied == "" ? null : moving ? clipPicture : stillPicture;
            Playing = true;
            var lang = hebrew ? "he" : "en";
            if (!media.Narration.TryGetValue(lang, out var audio) || !media.Cues.TryGetValue(lang, out var starts) || starts.Count == 0)
            { Fail("no narration or captions for " + lang); return; }
            if (supplied == "") file = audio;                              // the failure log names the voice-only line's file
            else if (picture == null) { Fail("no player for " + supplied); return; }
            cues = starts;
            int mine = generation;
            timer = caption.schedule.Execute(Tick).Every(TickMs).StartingIn(TickMs);
            try
            {
                picture?.Load(Url(supplied), surface, why => { if (mine == generation && Playing) Fail("picture: " + why); });
                if (mine != generation || !Playing) return;
                var pending = fetch(Url(audio), (clip, error) =>
                {
                    if (mine != generation || !Playing) { Release(clip); return; }
                    request = null;
                    if (clip == null) { Fail("fetch: " + error); return; }
                    narration = clip; decodingMs = 0;
                });
                // A synchronous fake/cache callback may have completed (or failed) before Fetch returns.
                if (mine == generation && Playing && narration == null) request = pending;
                else if (mine != generation) pending.Abort();
            }
            catch (Exception e) { if (mine == generation && Playing) Fail("load: " + e.Message); }
        }

        internal void Tick()
        {
            if (!Playing) return;
            if (!started)
            {
                if (picture != null && !picture.Ready)
                {
                    pictureMs += TickMs;
                    if (picture is StillPicture still)
                    {
                        ulong progress = still.Bytes;
                        pictureStalledMs = progress > pictureBytes ? 0 : pictureStalledMs + TickMs; pictureBytes = progress;
                        if (pictureStalledMs >= DeclarationRecording.StallTimeoutMs || pictureMs >= DeclarationRecording.LoadCapMs)
                        { Fail("picture fetch timed out"); return; }
                    }
                    else if (pictureMs >= CardVideo.PrepareTimeoutMs) { Fail("picture preparation timed out"); return; }
                }
                if (narration == null)
                {
                    loadingMs += TickMs;
                    var now = request?.Bytes ?? 0;
                    stalledMs = now > bytes ? 0 : stalledMs + TickMs; bytes = now;
                    if (stalledMs >= DeclarationRecording.StallTimeoutMs || loadingMs >= DeclarationRecording.LoadCapMs) Fail("fetch timed out");
                    return;
                }
                var ready = output.Ready(narration);
                if (ready == null) { Fail("narration could not decode"); return; }
                if (!ready.Value)
                {
                    if ((decodingMs += TickMs) >= DeclarationRecording.DecodeTimeoutMs) Fail("narration decode timed out");
                    return;
                }
                if (picture != null && !picture.Ready) return;
                try { picture?.Start(); output.Play(narration); started = true; }
                catch (Exception e) { Fail("start: " + e.Message); return; }
            }
            if (output.IsPlaying && output.Position > 0) heard = true;
            if (!heard && (startingMs += TickMs) >= DeclarationRecording.StartTimeoutMs) { Fail("narration did not start"); return; }
            if (heard && !output.IsPlaying)
            {
                var done = ended; Stop(); done?.Invoke(); return;
            }
            int cue = AdvisorCaptions.At(cues, output.Position);
            if (caption != null) caption.text = cue < 0 ? "" : text[stem + ".cue." + (cue + 1)];
            if (moving && picture != null && Math.Abs(picture.Time - output.Position) > DriftS) picture.Seek(output.Position);
        }

        void Fail(string why)
        {
            var tell = failed; var name = file;
            Stop();
            Debug.LogWarning($"ADVISOR MEDIA failed {name}: {why}");
            tell?.Invoke(why);
        }

        public void Stop()
        {
            generation++; Playing = false;
            timer?.Pause(); timer = null;
            request?.Abort(); request = null;
            picture?.Stop(); picture = null;
            output.Stop(); Release(narration); narration = null;
            if (caption != null) caption.text = "";
            caption = null; ended = null; failed = null;
            started = heard = false;
            loadingMs = stalledMs = decodingMs = startingMs = pictureMs = pictureStalledMs = 0; bytes = pictureBytes = 0;
        }

        static void Release(AudioClip? clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(clip); else UnityEngine.Object.DestroyImmediate(clip);
        }

        public void Dispose() { if (disposed) return; Stop(); output.Dispose(); disposed = true; }
    }

    public static class AdvisorCaptions
    {
        public static int At(IReadOnlyList<double> starts, double position)
        {
            for (int i = starts.Count - 1; i >= 0; i--) if (position >= starts[i]) return i;
            return -1;
        }
    }

    /// <summary>Only the URL this adapter loaded is its own to release, seek or stop.</summary>
    public sealed class ClipPicture : AdvisorMedia.IPicture
    {
        readonly CardVideo video;
        string? url;
        public ClipPicture(CardVideo video) { this.video = video; }
        bool Owns => url != null && video.Current == url;
        public void Load(string url, VisualElement target, Action<string> failed)
        { Stop(); this.url = url; video.Play(url, target, failed, loop: false, held: true); }
        public bool Ready => Owns && video.Prepared;
        public double Time => Owns ? video.Time : 0;
        public void Start() { if (Owns) video.Release(); }
        public void Seek(double time) { if (Owns) video.Seek(time); }
        public void Stop() { if (Owns) video.Stop(); url = null; }
    }

    /// <summary>A fetched texture held until Start. Stop aborts it and invalidates late callbacks.</summary>
    public sealed class StillPicture : AdvisorMedia.IPicture
    {
        UnityWebRequest? request;
        Texture2D? image;
        VisualElement? target;
        int generation;
        internal ulong Bytes => request?.downloadedBytes ?? 0;
        public bool Ready => image != null;
        public double Time => 0;
        public void Seek(double time) { }
        public void Load(string url, VisualElement target, Action<string> failed)
        {
            Stop(); this.target = target; int mine = generation;
            var pending = UnityWebRequestTexture.GetTexture(url);
            request = pending;
            UnityWebRequestAsyncOperation operation;
            try { operation = pending.SendWebRequest(); }
            catch { request = null; pending.Dispose(); throw; }
            operation.completed += _ =>
            {
                if (mine != generation) { pending.Dispose(); return; }
                request = null;
                string? error = null;
                try
                {
                    if (pending.result != UnityWebRequest.Result.Success) error = pending.error ?? "texture fetch failed";
                    else image = DownloadHandlerTexture.GetContent(pending);
                    if (image == null && error == null) error = "no texture";
                }
                catch (Exception e) { error = e.Message; }
                finally { pending.Dispose(); }
                if (error != null) failed(error);
            };
        }
        public void Start() { if (target != null && image != null) target.style.backgroundImage = new StyleBackground(image); }
        public void Stop()
        {
            generation++; request?.Abort(); request = null;
            if (image != null)
            {
                if (target != null && target.style.backgroundImage.value.texture == image) target.style.backgroundImage = StyleKeyword.Null;
                if (Application.isPlaying) UnityEngine.Object.Destroy(image); else UnityEngine.Object.DestroyImmediate(image);
            }
            image = null; target = null;
        }
    }
}
