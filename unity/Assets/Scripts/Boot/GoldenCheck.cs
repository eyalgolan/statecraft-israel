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
            var lines = new List<string>();
            bool ok = true;
            IReadOnlyList<(string name, string hash)> goldens;
            try { goldens = GoldenData.Parse(data.Manifest); }
            catch (Exception e) { return (false, new List<string> { $"GOLDEN FAIL manifest {e.Message}" }); }
            if (goldens.Count == 0) return (false, new List<string> { "GOLDEN FAIL manifest has no goldens" });
            foreach (var (name, expected) in goldens)
            {
                string hash;
                try { hash = Play(name, c, data).Hash(); }
                catch (Exception e) { ok = false; lines.Add($"GOLDEN FAIL {name} {e.GetType().Name}: {e.Message}"); continue; }
                if (hash == expected) lines.Add($"GOLDEN OK {name} {hash}");
                else { ok = false; lines.Add($"GOLDEN FAIL {name} {hash} expected {expected}"); }
            }
            return (ok, lines);
        }

        /// <summary>The run a manifest name pins (docs/determinism.md): hash1950 and hash1952 (GoldenRun.Play), mvp-end
        /// (GoldenRun.PlayToEnd), smoke&lt;N&gt; (GoldenRun.PlaySmoke), scenario-&lt;name&gt; (that scenario file) and the
        /// era goldens hash_&lt;era&gt;_&lt;year&gt; (GoldenRun.PlayEra to 1 January of that year, as EraGolden names them).</summary>
        internal static GameState Play(string name, Content c, GoldenData data)
        {
            if (name == "hash1950") return GoldenRun.Play(c, GoldenRun.Until1950);
            if (name == "hash1952") return GoldenRun.Play(c, GoldenRun.Until1952);
            if (name == "mvp-end") return GoldenRun.PlayToEnd(c);
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
