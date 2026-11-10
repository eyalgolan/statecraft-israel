#nullable enable
using System;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>Diagnostic console lines, off unless asked for: "?bootlog" (WebGL page URL) or -bootlog (desktop) logs each
    /// boot phase with its time since the player started and since the previous phase ("BOOT phase=… at=… ms dt=… ms");
    /// "?navlog" / -navlog logs every UI pointer and submit event, every screen toggle and every map click with the frame
    /// it happened in ("NAV f=… …"). Playtest 2, P2-1 and P2-4. "?turnlog" / -turnlog times each End turn's sim step and
    /// handlers, one "TURNLOG …" line a turn (TurnProbe, #2905).</summary>
    public static class DiagLog
    {
        public static bool Boot { get; set; }
        public static bool Nav { get; set; }

        /// <summary>Whether <paramref name="flag"/> is asked for: a whole query parameter on WebGL (UrlFlags.Has), else a
        /// command-line flag "-<paramref name="flag"/>".</summary>
        public static bool Requested(string flag, string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, flag) : args.Contains("-" + flag);

        /// <summary>Reads the flags from this player's URL or command line.</summary>
        public static void Init()
        {
            var webgl = Application.platform == RuntimePlatform.WebGLPlayer;
            var url = Application.absoluteURL; var args = Environment.GetCommandLineArgs();
            Boot = Requested("bootlog", url, args, webgl);
            Nav = Requested("navlog", url, args, webgl);
            TurnProbe.On = Requested(TurnProbe.Flag, url, args, webgl);
        }

        static double last = -1;

        public static void BootPhase(string phase)
        {
            if (!Boot) return;
            var now = Time.realtimeSinceStartupAsDouble;
            Debug.Log(FormattableString.Invariant($"BOOT phase={phase} at={now * 1000:0} ms dt={(last < 0 ? now : now - last) * 1000:0} ms"));
            last = now;
        }

        public static void NavLine(string what)
        {
            if (!Nav) return;
            Debug.Log(FormattableString.Invariant($"NAV f={Time.frameCount} t={Time.realtimeSinceStartupAsDouble * 1000:0} {what}"));
        }
    }
}
