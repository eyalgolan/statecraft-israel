#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>Runs every manifest golden in whatever runtime this is (IL2CPP in players) and reports
    /// "GOLDEN OK &lt;name&gt; &lt;hash&gt;" or "GOLDEN FAIL &lt;name&gt; &lt;hash&gt; expected &lt;hash&gt;" (docs/determinism.md).
    /// Asked for by the URL query "golden" on WebGL, or the command-line flag -golden on desktop.</summary>
    public static class GoldenCheck
    {
        public static bool Requested() => Application.platform == RuntimePlatform.WebGLPlayer
            ? UrlFlags.Has(Application.absoluteURL, "golden")
            : Environment.GetCommandLineArgs().Contains("-golden");

        /// <summary>The exit code a boot error must quit with: 1 for a desktop -golden run (so it never hangs), else null.</summary>
        public static int? QuitCodeOnError(bool requested, bool webgl) => requested && !webgl ? 1 : (int?)null;

        /// <summary>Plays every golden the manifest lists, in its order, one OK or FAIL line each. A golden this code cannot
        /// play (an unknown name, a missing scenario file) or a run that throws is a FAIL line, never a skipped one, and an
        /// empty manifest fails: a full-set pass needs every line.</summary>
        public static (bool ok, List<string> lines) Run(Content c, GoldenData data)
        {
            var goldens = Manifest(data, out var fail);
            if (goldens == null) return (false, new List<string> { fail });
            var lines = new List<string>();
            bool ok = true;
            foreach (var (name, expected) in goldens) ok &= Check(name, expected, c, data, lines);
            return (ok, lines);
        }

        /// <summary><see cref="Run"/> with a frame between goldens, for the WebGL player: its garbage collector runs only
        /// between frames, so one call that plays every golden keeps all their garbage until the call ends (the 8 Nov 2026
        /// nightly's OOM at the 2048 MB heap cap). The lines and the verdict are Run's.</summary>
        public static async Awaitable<(bool ok, List<string> lines)> RunAsync(Content c, GoldenData data)
        {
            var goldens = Manifest(data, out var fail);
            if (goldens == null) return (false, new List<string> { fail });
            var lines = new List<string>();
            bool ok = true;
            foreach (var (name, expected) in goldens)
            {
                await Awaitable.NextFrameAsync();
                ok &= Check(name, expected, c, data, lines);
            }
            return (ok, lines);
        }

        /// <summary>The manifest's goldens, or null with the FAIL line when it cannot be read or pins none.</summary>
        static IReadOnlyList<(string name, string hash)>? Manifest(GoldenData data, out string fail)
        {
            fail = "";
            try
            {
                var goldens = GoldenData.Parse(data.Manifest);
                if (goldens.Count != 0) return goldens;
                fail = "GOLDEN FAIL manifest has no goldens";
            }
            catch (Exception e) { fail = $"GOLDEN FAIL manifest {e.Message}"; }
            return null;
        }

        /// <summary>Plays one golden and adds its OK or FAIL line; true when it matched.</summary>
        static bool Check(string name, string expected, Content c, GoldenData data, List<string> lines)
        {
            string hash;
            try { hash = Play(name, c, data).Hash(); }
            catch (Exception e) { lines.Add($"GOLDEN FAIL {name} {e.GetType().Name}: {e.Message}"); return false; }
            if (hash == expected) { lines.Add($"GOLDEN OK {name} {hash}"); return true; }
            lines.Add($"GOLDEN FAIL {name} {hash} expected {expected}");
            return false;
        }

        /// <summary>The run a manifest name pins (docs/determinism.md): hash1950 and hash1952 (GoldenRun.Play), mvp-end
        /// (GoldenRun.PlayToEnd), smoke&lt;N&gt; (GoldenRun.PlaySmoke), smoke-orders (GoldenRun.PlaySmokeOrders), scenario-&lt;name&gt; (that scenario file) and the
        /// era goldens hash_&lt;era&gt;_&lt;year&gt; (GoldenRun.PlayEra to 1 January of that year, as EraGolden names them).</summary>
        internal static GameState Play(string name, Content c, GoldenData data)
        {
            if (name == "hash1950") return GoldenRun.Play(c, GoldenRun.Until1950);
            if (name == "hash1952") return GoldenRun.Play(c, GoldenRun.Until1952);
            if (name == "mvp-end") return GoldenRun.PlayToEnd(c);
            if (name == "smoke-orders") return GoldenRun.PlaySmokeOrders(c);
            if (name.StartsWith("smoke", StringComparison.Ordinal)
                && int.TryParse(name.Substring("smoke".Length), NumberStyles.None, CultureInfo.InvariantCulture, out var turns) && turns > 0)
                return GoldenRun.PlaySmoke(c, turns);
            if (name.StartsWith("scenario-", StringComparison.Ordinal))
            {
                var scenarioName = name.Substring("scenario-".Length);
                if (!data.Scenarios.TryGetValue(scenarioName, out var yaml)) throw new InvalidOperationException($"golden scenario '{scenarioName}' is missing");
                var scenario = Scenario.Parse(yaml);
                if (scenario.Name != scenarioName) throw new InvalidOperationException($"golden scenario file '{scenarioName}' names '{scenario.Name}'");
                return scenario.Play(c);
            }
            if (name.StartsWith("hash_", StringComparison.Ordinal))
            {
                var split = name.LastIndexOf('_');
                if (split <= 5 || !int.TryParse(name.Substring(split + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var year))
                    throw new InvalidOperationException($"unknown golden '{name}'");
                return GoldenRun.PlayEra(c, name.Substring(5, split - 5), new DateTime(year, 1, 1));
            }
            throw new InvalidOperationException($"unknown golden '{name}'");
        }
    }
}
