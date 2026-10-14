#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace Statecraft.Game.UI
{
    /// <summary>A silent clip player (decision videos, 2026-10-13): one VideoPlayer, made the first time a clip is asked for
    /// (never at boot), playing a clip from StreamingAssets by URL (the only source the Web player takes) into one reused
    /// 960x540 RenderTexture, which the target element shows as its background. The decision card has one, for its looping
    /// illustration; the End Turn outcome clips have their own (OutcomePresenter), which plays each clip once. No audio is
    /// output (the clips have no audio track). A clip that fails (errorReceived) or is not prepared within its timeout is
    /// stopped and reported once. The texture is cleared at each Play, so a new clip never opens on the last frame of the
    /// previous one. Presentation only.</summary>
    public sealed class CardVideo : IDisposable
    {
        public const int Width = 960, Height = 540;
        /// <summary>How long a clip may take to be ready before the card gives up on it.</summary>
        public const long PrepareTimeoutMs = 8000;

        GameObject? go; VideoPlayer? player; RenderTexture? rt;
        VisualElement? target; Action<string>? onFail; Action? onEnded; IVisualElementScheduledItem? timeout;

        /// <summary>Video is off in the automated runs (the smoke run, the golden check, the shot, frame and pixel-error
        /// profiles, the unit showcase and the border proof), so their captures and checks never depend on decoding: the
        /// card then has no Illustration switch at all. "nostart" (by hand) leaves it on. Flags as StartScreenPresenter.Skipped:
        /// a whole URL query parameter on WebGL, a command-line flag on desktop.</summary>
        public static bool ForcedOff(string? url, string[] args, bool webgl)
        {
            if (GameRoot.SmokeTurns(url, args, webgl) > 0) return true;
            var flags = new[] { "golden", "shot", "vprofile", "profile", "units", Statecraft.Game.Map.BorderProof.Flag };
            return webgl ? Array.Exists(flags, f => Boot.UrlFlags.Has(url, f)) : Array.Exists(flags, f => Array.IndexOf(args, "-" + f) >= 0);
        }

        public static bool ForcedOff() =>
            ForcedOff(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>The clip's URL in the build's StreamingAssets/video folder, with the build's id on the web (issue #437).</summary>
        public static string Url(string file) => Boot.GameInputs.WithBuildId(Application.streamingAssetsPath + "/video/" + file, Application.buildGUID);

        /// <summary>The URL playing (or being prepared), or null.</summary>
        public string? Current { get; private set; }

        /// <summary>True once the current clip's first frame is in the texture.</summary>
        public bool Showing { get; private set; }

        /// <summary>Plays <paramref name="url"/> into <paramref name="header"/>'s background; <paramref name="failed"/> is told
        /// (once) if it fails or times out. The header keeps whatever it shows until the first frame is ready. A card's
        /// illustration loops; an outcome clip (<paramref name="loop"/> false) plays once and then tells
        /// <paramref name="ended"/>.</summary>
        /// <param name="prepareTimeoutMs">How long it may take to be ready (default PrepareTimeoutMs).</param>
        public void Play(string url, VisualElement header, Action<string> failed, bool loop = true, Action? ended = null,
            long prepareTimeoutMs = PrepareTimeoutMs)
        {
            Stop();
            Ensure();
            var was = RenderTexture.active; RenderTexture.active = rt; GL.Clear(false, true, Color.black); RenderTexture.active = was;
            target = header; onFail = failed; onEnded = loop ? null : ended; Current = url;
            player!.isLooping = loop;
            player.url = url;
            player.Prepare();
            timeout = header.schedule.Execute(() => { if (Current == url && !Showing) Fail("not ready after " + prepareTimeoutMs + " ms"); }).StartingIn(prepareTimeoutMs);
        }

        /// <summary>Puts the playing clip back on <paramref name="header"/>'s background (after the card reset it to the
        /// photograph); nothing while the clip is still being prepared.</summary>
        public void ShowIn(VisualElement header)
        {
            target = header;
            if (Showing) header.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        }

        /// <summary>Stops the clip; the header's background is the caller's to set back.</summary>
        public void Stop()
        {
            timeout?.Pause(); timeout = null;
            if (player != null) { player.Stop(); player.url = ""; }
            target = null; onFail = null; onEnded = null; Current = null; Showing = false;
        }

        public void Dispose()
        {
            Stop();
            if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
            if (rt != null) { rt.Release(); if (Application.isPlaying) UnityEngine.Object.Destroy(rt); else UnityEngine.Object.DestroyImmediate(rt); }
            go = null; player = null; rt = null;
        }

        void Ensure()
        {
            if (player != null) return;
            rt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32) { name = "CardVideo" };
            rt.Create();
            go = new GameObject("CardVideo");
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);   // edit-mode tests make one too
            player = go.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = rt;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.isLooping = true;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            player.prepareCompleted += OnPrepared;
            player.loopPointReached += _ => { var told = onEnded; if (told == null) return; Stop(); told(); };   // a once-only clip ended
            player.errorReceived += OnError;
        }

        void OnPrepared(VideoPlayer p)
        {
            if (target == null) return;
            target.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            Showing = true;
            p.Play();
            Debug.Log("VIDEO playing " + Current);
        }

        void OnError(VideoPlayer p, string message) => Fail(message);

        void Fail(string why)
        {
            var told = onFail; var url = Current;
            Stop();
            Debug.LogWarning($"VIDEO failed {url}: {why}");
            told?.Invoke(why);
        }
    }
}
