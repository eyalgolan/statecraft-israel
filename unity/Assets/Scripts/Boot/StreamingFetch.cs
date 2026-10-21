#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Statecraft.Game.Boot
{
    /// <summary>The one retry policy for every StreamingAssets download the game makes as it starts (issue #1054): the content
    /// bundle, the heightmap and the golden files (GameInputs.FetchAsync), and The 1948 Collection's checks file and bundles
    /// (CollectionStream.BundleSource). A download that gets no response (reset, refused, or no byte for the stall limit) or
    /// a server-side status (408, 429, 5xx) is started again after Backoff, or after the response's Retry-After when that is
    /// longer (at most MaxRetryAfter), up to MaxAttempts in all. Then the caller fails as before: the boot shows BOOT FAIL
    /// naming the file, and the collection leaves that bundle's models off the map. Retrying changes only when a file
    /// arrives, never its bytes.</summary>
    public static class StreamingFetch
    {
        /// <summary>Attempts per file, the first included. The boot fetches its files at once; one transient failure (a reset
        /// connection, a busy server, a 429 from a rate-limited host) used to fail the whole boot.</summary>
        internal const int MaxAttempts = 4;

        /// <summary>Seconds to wait before attempt <paramref name="attempt"/> + 1: 0.5, 1, 2.</summary>
        internal static float Backoff(int attempt) => 0.5f * (1 << (attempt - 1));

        /// <summary>The longest wait a Retry-After header can ask for (seconds); a longer one waits this long. Without a
        /// limit, a host that answers "Retry-After: 3600" would leave the loading screen up for an hour. The value needs the
        /// maintainer's sign-off (#1054).</summary>
        internal const float MaxRetryAfter = 10f;

        /// <summary>Seconds a download may go without receiving a byte before it is aborted as stalled and retried like a
        /// dropped connection (#911). A limit on silence, not on the whole download: the 4 MB heightmap on a slow
        /// connection keeps receiving bytes and is never cut off, while a server that accepts the request and never
        /// answers reaches the retry and then BOOT FAIL instead of leaving the loading screen up forever. A fixed limit on
        /// the whole fetch failed slow but healthy fetches before (#503). The value needs the maintainer's sign-off (#911).</summary>
        internal const float StallSeconds = 30f;

        /// <summary>Worth another attempt: no response at all (reset, refused, timed out) or a server-side status (408, 429,
        /// 5xx). A 404 or any other client error, and a body that failed to decode, fail at once.</summary>
        internal static bool Transient(UnityWebRequest.Result result, long status) =>
            result == UnityWebRequest.Result.ConnectionError
            || (result == UnityWebRequest.Result.ProtocolError && (status >= 500 || status == 408 || status == 429));

        /// <summary>Seconds a Retry-After header asks for at <paramref name="nowUtc"/>: delay-seconds or an HTTP-date (RFC 9110
        /// section 10.2.3); 0 when absent, malformed or in the past.</summary>
        internal static float RetryAfterSeconds(string? value, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0f;
            var v = value!.Trim();
            if (long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)) return seconds;
            if (DateTime.TryParseExact(v, "r", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
                return (float)Math.Max(0, (at - nowUtc).TotalSeconds);
            return 0f;
        }

        /// <summary>Seconds to wait before attempt <paramref name="attempt"/> + 1: Backoff, or the Retry-After when it is
        /// longer, capped at MaxRetryAfter.</summary>
        internal static float Delay(int attempt, string? retryAfter, DateTime nowUtc) =>
            Math.Max(Backoff(attempt), Math.Min(RetryAfterSeconds(retryAfter, nowUtc), MaxRetryAfter));

        /// <summary>One file's download, attempt by attempt, as the retry loop sees it (Download in the game, a fake in the
        /// tests).</summary>
        internal interface IDownload
        {
            bool IsDone { get; }
            ulong DownloadedBytes { get; }
            UnityWebRequest.Result Result { get; }
            long Status { get; }
            string? Error { get; }
            string Url { get; }
            /// <summary>The response's Retry-After header (null when it has none).</summary>
            string? RetryAfter { get; }
            /// <summary>Aborts (when still running) and disposes the current attempt; safe to call twice.</summary>
            void Release();
            /// <summary>Starts the next attempt (after Release).</summary>
            void Restart();
        }

        static readonly HashSet<UnityWebRequest> open = new HashSet<UnityWebRequest>();
        /// <summary>Requests started and not yet released (diagnostic; 0 once a fetch has ended, whether it succeeded or failed).</summary>
        public static int OpenRequests => open.Count;

        /// <summary>A UnityWebRequest made by <c>make</c> and sent at once; Restart makes and sends a new one. The caller
        /// reads <see cref="Request"/> once Done returns and then calls Release.</summary>
        public sealed class Download : IDownload
        {
            readonly Func<UnityWebRequest> make;
            public UnityWebRequest Request { get; private set; }

            internal Download(Func<UnityWebRequest> make) { this.make = make; Request = Send(make()); }

            static UnityWebRequest Send(UnityWebRequest r)
            {
                open.Add(r);
                r.SendWebRequest();
                return r;
            }

            public bool IsDone => Request.isDone;
            public ulong DownloadedBytes => Request.downloadedBytes;
            public UnityWebRequest.Result Result => Request.result;
            public long Status => Request.responseCode;
            public string? Error => Request.error;
            public string Url => Request.url;
            public string? RetryAfter => Request.GetResponseHeader("Retry-After");

            public void Release()
            {
                if (!open.Remove(Request)) return;
                if (!Request.isDone) Request.Abort();
                Request.Dispose();
            }

            public void Restart() { Release(); Request = Send(make()); }
        }

        /// <summary>A GET of <paramref name="url"/>, sent now (so a caller can start every download before awaiting any).</summary>
        public static Download Get(string url) => new Download(() => UnityWebRequest.Get(url));

        /// <summary>A request <paramref name="make"/> builds (for example an AssetBundle request), sent now.</summary>
        public static Download Start(Func<UnityWebRequest> make) => new Download(make);

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice) until the download is
        /// done, or aborts it when no byte has arrived for <paramref name="stallSeconds"/>. On a transient failure (a stall
        /// counts as a dropped connection) the attempt is released and another started after Delay, up to MaxAttempts in
        /// all. Returns once an attempt succeeded (the caller reads it, then releases it); on a final failure the attempt is
        /// released and an IOException names the file.</summary>
        internal static async Awaitable Done(IDownload d, string name, float stallSeconds)
        {
            for (var attempt = 1; ; attempt++)
            {
                var stalled = false;
                var bytes = d.DownloadedBytes;
                var lastByte = Time.realtimeSinceStartup;
                while (!d.IsDone)
                {
                    if (d.DownloadedBytes != bytes) { bytes = d.DownloadedBytes; lastByte = Time.realtimeSinceStartup; }
                    else if (Time.realtimeSinceStartup - lastByte > stallSeconds) { stalled = true; break; }
                    await Awaitable.NextFrameAsync();
                }
                if (!stalled && d.Result == UnityWebRequest.Result.Success) return;
                var error = stalled
                    ? FormattableString.Invariant($"{name}: no data for {stallSeconds:0.#} s ({d.Url})")
                    : $"{name}: {d.Error} ({d.Url})";
                var retry = attempt < MaxAttempts && (stalled || Transient(d.Result, d.Status));
                var wait = retry ? Delay(attempt, stalled ? null : d.RetryAfter, DateTime.UtcNow) : 0f;
                d.Release();
                if (!retry) throw new IOException(attempt > 1 ? $"{error} after {attempt} attempts" : error);
                Debug.LogWarning(FormattableString.Invariant($"{error}; attempt {attempt + 1} of {MaxAttempts} in {wait:0.#} s"));
                var until = Time.realtimeSinceStartup + wait;
                while (Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
                d.Restart();
            }
        }
    }
}
