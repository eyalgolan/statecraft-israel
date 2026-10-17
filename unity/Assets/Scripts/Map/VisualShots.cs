#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Screenshots of the running player at each promo pose, written to the console as base64 chunks, because a
    /// WebGL page cannot write files: "SHOT &lt;pose&gt; begin &lt;n&gt; &lt;sha256&gt;", then n lines "SHOT &lt;pose&gt; &lt;i&gt;/&lt;n&gt; &lt;data&gt;",
    /// and "SHOT done" at the end. tools/web_check.py --shots reassembles and checks them. Asked for by "shot" in the page
    /// URL or -shot on desktop.</summary>
    public static class VisualShots
    {
        public const int SettleFrames = 30;
        public const int ChunkChars = 8000;   // well under any console line limit; a cut line fails the SHA-256 check

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "shot") : args.Contains("-shot");

        public static IEnumerable<string> Lines(string name, byte[] png)
        {
            var b64 = Convert.ToBase64String(png);
            int n = Math.Max(1, (b64.Length + ChunkChars - 1) / ChunkChars);
            string hash;
            using (var sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(png).Select(b => b.ToString("x2")));
            yield return $"SHOT {name} begin {n} {hash}";
            for (int i = 0; i < n; i++)
                yield return $"SHOT {name} {i + 1}/{n} {b64.Substring(i * ChunkChars, Math.Min(ChunkChars, b64.Length - i * ChunkChars))}";
        }

        /// <summary>The visual toggles that are off for the close checks, which look at the ground itself (the baked
        /// shading's steps): the haze, and the settlement trees, since one kit tree's crown filled hero06judea once the 1948
        /// localities had trees (audit 2026-10-13).</summary>
        public static readonly string[] OffForCloseChecks = { "haze", "trees" };

        public static IEnumerator Run(CameraRig rig, GeoFrame frame, Action<string> log)
        {
            foreach (var pose in PromoPose.All) yield return Shoot(rig, frame, pose, log);
            foreach (var pose in PromoPose.CloseUps) yield return Shoot(rig, frame, pose, log);   // issue #659: the models up close
            foreach (var pose in PromoPose.TownChecks) yield return Shoot(rig, frame, pose, log);   // #659: the other town types
            var off = TurnOff(OffForCloseChecks);
            try { foreach (var pose in PromoPose.CloseChecks) yield return Shoot(rig, frame, pose, log); }
            finally { TurnOn(off); }
            log("SHOT done");
        }

        /// <summary>Turns off each of <paramref name="names"/> that is registered and returns those (TurnOn puts them back).</summary>
        public static List<string> TurnOff(IEnumerable<string> names)
        {
            var off = names.Where(n => VisualToggles.Names.Contains(n)).ToList();
            foreach (var n in off) VisualToggles.Set(n, false);
            return off;
        }

        /// <summary>Turns on each of <paramref name="names"/> that is still registered.</summary>
        public static void TurnOn(IEnumerable<string> names)
        {
            foreach (var n in names) if (VisualToggles.Names.Contains(n)) VisualToggles.Set(n, true);
        }

        static IEnumerator Shoot(CameraRig rig, GeoFrame frame, PromoPose pose, Action<string> log)
        {
            pose.Apply(rig, frame);
            for (int i = 0; i < SettleFrames; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            try { foreach (var line in Lines(pose.Name, tex.EncodeToPNG())) log(line); }
            finally { UnityEngine.Object.Destroy(tex); }
        }
    }
}
