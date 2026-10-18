#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Prices each visual feature on the real player. At every promo pose it runs every configuration (everything on:
    /// "all"; each toggle off alone: "-name"; everything off: "none") Repeats times, in a different interleaved order each
    /// repeat (Order), 300 frames after 60 warm-up frames each (after one unmeasured 60-frame pass of every configuration
    /// before the first pose), and logs one line per run with the resolution and the URP
    /// render scale, then one summary line per configuration: the median of the run averages, their spread (max - min) and
    /// the median p95. A feature costs median(all) - median(-name); it is over a budget only when that exceeds the budget
    /// plus the larger spread (docs/visual-v1.md). Asked for by "vprofile" in the WebGL page URL (a whole query parameter)
    /// or -vprofile on desktop, where the player quits when done. Run it under tools/machine_lock.py --exclusive.</summary>
    public static class FrameProfile
    {
        public const int WarmupFrames = 60, MeasuredFrames = 300, Repeats = 3;
        /// <summary>The moving variant's turn (degrees of yaw per frame, about the look-at point).</summary>
        public const float MovingDegreesPerFrame = 0.5f;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "vprofile") : args.Contains("-vprofile");

        /// <summary>The moving variant ("vmoving" with "vprofile" in the WebGL URL, or -vmoving on desktop; Task 9 fix round 1):
        /// every run restarts from its pose and turns the camera MovingDegreesPerFrame each frame, warm-up and measured
        /// frames alike, so features that work only when the view changes (the map labels' placement) are priced too. Its
        /// lines carry the pose name with "-moving".</summary>
        public static bool MovingRequested() =>
            MovingRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool MovingRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "vmoving") : args.Contains("-vmoving");

        /// <summary>Issue #659: "vclose" with "vprofile" in the WebGL URL (or -vclose on desktop) also prices the close views
        /// (PromoPose.CloseUps), where the towns are MATKOT's building models; their lines carry those poses' names.</summary>
        public static bool CloseRequested() =>
            CloseRequested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool CloseRequested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "vclose") : args.Contains("-vclose");

        public static string Line(string label, string pose, string resolution, float renderScale, int rep, IReadOnlyList<float> frameMs)
        {
            var (avg, p95) = FrameStats.Summary(frameMs);
            return string.Format(CultureInfo.InvariantCulture,
                "PROFILE visual={0} pose={1} res={2} scale={3:0.00} rep={4} frames={5} avg_ms={6:0.00} p95_ms={7:0.00}",
                label, pose, resolution, renderScale, rep, frameMs.Count, avg, p95);
        }

        public static string Summary(string label, string pose, IReadOnlyList<(float avg, float p95)> runs)
        {
            if (runs == null || runs.Count == 0) throw new ArgumentException("no runs", nameof(runs));
            float Median(IEnumerable<float> xs) { var s = xs.OrderBy(x => x).ToList(); return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2f; }
            var avgs = runs.Select(r => r.avg).ToList();
            return string.Format(CultureInfo.InvariantCulture,
                "PROFILE summary visual={0} pose={1} runs={2} median_avg_ms={3:0.00} spread_ms={4:0.00} median_p95_ms={5:0.00}",
                label, pose, runs.Count, Median(avgs), avgs.Max() - avgs.Min(), Median(runs.Select(r => r.p95)));
        }

        /// <summary>Run order of <paramref name="count"/> configurations in repeat <paramref name="rep"/>: forward, reversed,
        /// then forward from the middle, so no configuration always runs first (warm) or last (hot).</summary>
        public static IReadOnlyList<int> Order(int count, int rep)
        {
            var forward = Enumerable.Range(0, count).ToList();
            switch (rep % 3)
            {
                case 0: return forward;
                case 1: forward.Reverse(); return forward;
                default: return forward.Skip(count / 2).Concat(forward.Take(count / 2)).ToList();
            }
        }

        public static IEnumerator Run(CameraRig rig, GeoFrame frame, Action<string> log, bool quitWhenDone) =>
            Run(rig, frame, log, quitWhenDone, MovingRequested());

        public static IEnumerator Run(CameraRig rig, GeoFrame frame, Action<string> log, bool quitWhenDone, bool moving)
        {
            var configs = new List<(string label, string? off)> { ("all", null) };
            foreach (var name in VisualToggles.Names) configs.Add(("-" + name, name));
            configs.Add(("none", "*"));
            bool first = true;
            var poses = new List<PromoPose>(PromoPose.All);
            if (CloseRequested()) poses.AddRange(PromoPose.CloseUps);
            foreach (var pose in poses)
            {
                pose.Apply(rig, frame);
                if (first)
                {
                    // One discarded pass of every configuration: the first measured runs after the load were noisier
                    // (spreads about half the median, review M6).
                    first = false;
                    foreach (var (_, off) in configs)
                    {
                        VisualToggles.SetAll(off != "*");
                        if (off != null && off != "*") VisualToggles.Set(off, false);
                        for (int f = 0; f < WarmupFrames; f++) yield return null;
                    }
                }
                var poseName = moving ? pose.Name + "-moving" : pose.Name;
                var results = configs.ToDictionary(c => c.label, c => new List<(float avg, float p95)>());
                for (int rep = 0; rep < Repeats; rep++)
                    foreach (var i in Order(configs.Count, rep))
                    {
                        var (label, off) = configs[i];
                        VisualToggles.SetAll(off != "*");
                        if (off != null && off != "*") VisualToggles.Set(off, false);
                        if (moving) pose.Apply(rig, frame);   // every run follows the same path
                        for (int f = 0; f < WarmupFrames; f++) { if (moving) Turn(rig); yield return null; }
                        var times = new List<float>(MeasuredFrames);
                        for (int f = 0; f < MeasuredFrames; f++) { if (moving) Turn(rig); yield return null; times.Add(Time.unscaledDeltaTime * 1000f); }
                        var scale = UniversalRenderPipeline.asset != null ? UniversalRenderPipeline.asset.renderScale : 1f;
                        log(Line(label, poseName, $"{Screen.width}x{Screen.height}", scale, rep, times));
                        results[label].Add(FrameStats.Summary(times));
                    }
                foreach (var (label, _) in configs) log(Summary(label, poseName, results[label]));
                if (moving) pose.Apply(rig, frame);
            }
            VisualToggles.SetAll(true);
            log("PROFILE visual done");
            if (quitWhenDone) Application.Quit(0);
        }

        static void Turn(CameraRig rig) { rig.Yaw += MovingDegreesPerFrame; rig.Apply(); }
    }
}
