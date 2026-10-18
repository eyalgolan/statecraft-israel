#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Networking;

namespace Statecraft.Game.Boot
{
    /// <summary>The map's raw inputs, already read (no System.IO needed afterwards).</summary>
    public sealed class MapInputs
    {
        public string HeightmapJson = "", HexesJson = "", SettlementsYaml = "";
        /// <summary>content/map/subdistrict_hexes_1948.yaml: the 1948 sub-districts the map outlines (display only).</summary>
        public string RegionsYaml = "";
        /// <summary>The baked runtime forms of the region files the map reads at boot (tools/regions_bake.py; issue #321;
        /// "" when absent: the YAML is read, with a warning).</summary>
        public string RegionsTsv = "", ArmisticeTsv = "", ControlTsv = "";
        /// <summary>content/map/route_paths_1948.yaml: the traced railways and roads the map draws (issue #35; map-only).</summary>
        public string RoutesYaml = "";
        /// <summary>content/map/route_paths_1948.tsv, the baked runtime form of RoutesYaml the map reads at boot (issue #321;
        /// "" when absent: the YAML is read, with a warning).</summary>
        public string RoutesTsv = "";
        /// <summary>content/map/units_1948.yaml: the sourced units the map draws in play (docs/units.md; map-only, "" when
        /// absent).</summary>
        public string UnitsYaml = "";
        /// <summary>content/map/settlement_population_1945.yaml (the settlement kits' footprints; "" when absent). The sim
        /// never reads it.</summary>
        public string PopulationYaml = "";
        /// <summary>content/map/localities_1948.tsv, the baked runtime form of localities_1948.yaml (the 1948 localities and
        /// Bedouin camp areas the kits and labels draw; "" when absent). The sim never reads it.</summary>
        public string LocalitiesTsv = "";
        /// <summary>content/map/armistice_hexes_1949.yaml: the 1949 armistice lines drawn over it from their dates (display
        /// only; "" when absent: no armistice layer, like the other cosmetic map inputs).</summary>
        public string ArmisticeYaml = "";
        /// <summary>content/map/control_hexes_1948_49.yaml: who held which areas, 1948-49, by period (display only; "" when
        /// absent: the older drawing, sub-districts with the armistice zones over them).</summary>
        public string ControlYaml = "";
        /// <summary>heightmap.r16z as read: packed (HeightmapCodec); HeightmapLoader decodes it when the map is built.</summary>
        public byte[] HeightmapR16z = new byte[0];
    }

    /// <summary>Everything the game loads at start: the sim content and the map inputs. Desktop and the editor read
    /// files (FromDisk); WebGL cannot read StreamingAssets with System.IO, so it fetches them with UnityWebRequest from
    /// the manifest (FetchAsync, tech notes section 2).</summary>
    public sealed class GameInputs
    {
        public Content Content = null!;
        public MapInputs Map = null!;
        public GoldenData Goldens = null!;

        public static MapInputs MapFromDisk(string contentDir, string mapDir) => new MapInputs
        {
            HeightmapJson = File.ReadAllText(Path.Combine(mapDir, "heightmap.json")),
            HeightmapR16z = File.ReadAllBytes(Path.Combine(mapDir, HeightmapCodec.FileName)),
            HexesJson = File.ReadAllText(Path.Combine(contentDir, "map", "hexes.json")),
            SettlementsYaml = File.ReadAllText(Path.Combine(contentDir, "map", SettlementPlacer.DefaultFileName)),
            RegionsYaml = File.ReadAllText(Path.Combine(contentDir, "map", Subdistricts1948.FileName)),
            RoutesYaml = ReadIfExists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Routes.RoutePaths.DefaultFileName)),
            RoutesTsv = ReadIfExists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Routes.RoutePaths.RuntimeFileName)),
            UnitsYaml = ReadIfExists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Units.UnitData.DefaultFileName)),
            PopulationYaml = File.Exists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))
                ? File.ReadAllText(Path.Combine(contentDir, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName)) : "",
            LocalitiesTsv = ReadIfExists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Kits.Localities.RuntimeFileName)),
            ArmisticeYaml = ReadIfExists(Path.Combine(contentDir, "map", Armistice1949.FileName)),
            RegionsTsv = ReadIfExists(Path.Combine(contentDir, "map", Subdistricts1948.RuntimeFileName)),
            ArmisticeTsv = ReadIfExists(Path.Combine(contentDir, "map", Armistice1949.RuntimeFileName)),
            ControlTsv = ReadIfExists(Path.Combine(contentDir, "map", Control1948.RuntimeFileName)),
            ControlYaml = ReadIfExists(Path.Combine(contentDir, "map", Control1948.FileName)),
        };

        /// <summary>A map-only, cosmetic input: "" when the file is missing (the map then skips that layer with a warning).</summary>
        static string ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

        /// <summary>The inputs from disk. The golden data (#394) is read in the editor (the repository's manifest and
        /// scenarios) and, in a player, only for a -golden run (the copies ContentSync ships), so a normal boot never
        /// depends on it.</summary>
        public static GameInputs FromDisk(string contentDir, string mapDir)
        {
            var root = Application.isEditor
                ? Path.Combine(Paths.RepoRoot, "sim", "Statecraft.Sim.Tests", "golden")
                : Path.Combine(Application.streamingAssetsPath, "golden");
            var goldens = Application.isEditor || GoldenCheck.Requested() ? GoldenData.FromDisk(root) : new GoldenData();
            return new GameInputs { Content = ContentLoader.Load(contentDir), Map = MapFromDisk(contentDir, mapDir), Goldens = goldens };
        }

        /// <summary>Builds the inputs from fetched texts (keys are manifest paths) and the packed heightmap's bytes.</summary>
        public static GameInputs FromTexts(IReadOnlyDictionary<string, string> texts, IEnumerable<string> manifest, string heightmapJson, byte[] r16z)
        {
            var events = manifest.Where(f => f.StartsWith("events/", StringComparison.Ordinal)).ToList();
            var content = ContentLoader.Load(rel => texts.TryGetValue(rel, out var t) ? t : null, events);
            return new GameInputs
            {
                Content = content,
                Map = new MapInputs
                {
                    HeightmapJson = heightmapJson, HeightmapR16z = r16z,
                    HexesJson = texts["map/hexes.json"], SettlementsYaml = texts["map/" + SettlementPlacer.DefaultFileName],
                    RegionsYaml = texts["map/" + Subdistricts1948.FileName],
                    RoutesYaml = texts.TryGetValue("map/" + Statecraft.Game.Map.Routes.RoutePaths.DefaultFileName, out var routes) ? routes : "",
                    RoutesTsv = texts.TryGetValue("map/" + Statecraft.Game.Map.Routes.RoutePaths.RuntimeFileName, out var routesTsv) ? routesTsv : "",
                    UnitsYaml = texts.TryGetValue("map/" + Statecraft.Game.Map.Units.UnitData.DefaultFileName, out var unitsYaml) ? unitsYaml : "",
                    PopulationYaml = texts.TryGetValue("map/" + Statecraft.Game.Map.Kits.KitPopulation.FileName, out var pop) ? pop : "",
                    LocalitiesTsv = texts.TryGetValue("map/" + Statecraft.Game.Map.Kits.Localities.RuntimeFileName, out var loc) ? loc : "",
                    ArmisticeYaml = texts.TryGetValue("map/" + Armistice1949.FileName, out var armistice) ? armistice : "",
                    RegionsTsv = texts.TryGetValue("map/" + Subdistricts1948.RuntimeFileName, out var regionsTsv) ? regionsTsv : "",
                    ArmisticeTsv = texts.TryGetValue("map/" + Armistice1949.RuntimeFileName, out var armisticeTsv) ? armisticeTsv : "",
                    ControlTsv = texts.TryGetValue("map/" + Control1948.RuntimeFileName, out var controlTsv) ? controlTsv : "",
                    ControlYaml = texts.TryGetValue("map/" + Control1948.FileName, out var control) ? control : "",
                },
            };
        }

        /// <summary>StreamingAssets as a URL: the page-relative URL on WebGL, a file:// URI elsewhere.</summary>
        public static string StreamingUrl() =>
            Application.streamingAssetsPath.Contains("://") ? Application.streamingAssetsPath : new Uri(Application.streamingAssetsPath).AbsoluteUri;

        static readonly HashSet<UnityWebRequest> open = new HashSet<UnityWebRequest>();
        /// <summary>Requests started and not yet released (diagnostic; 0 once a fetch has ended, whether it succeeded or failed).</summary>
        public static int OpenRequests => open.Count;

        static UnityWebRequest Start(string url)
        {
            var r = UnityWebRequest.Get(url);
            open.Add(r);
            r.SendWebRequest();
            return r;
        }

        /// <summary>Aborts (when still running) and disposes a request; safe to call twice.</summary>
        static void Release(UnityWebRequest r)
        {
            if (!open.Remove(r)) return;
            if (!r.isDone) r.Abort();
            r.Dispose();
        }

        /// <summary>Attempts per file, the first included. The boot fetches every file at once; one transient failure
        /// (a reset connection, a busy server) used to fail the whole boot (BOOT FAIL israel_bonds.yaml under load).</summary>
        internal const int MaxAttempts = 4;

        /// <summary>Seconds to wait before attempt <paramref name="attempt"/> + 1: 0.5, 1, 2.</summary>
        internal static float Backoff(int attempt) => 0.5f * (1 << (attempt - 1));

        /// <summary>Worth another attempt: no response at all (reset, refused, timed out) or a server-side status (408, 429,
        /// 5xx). A 404 or any other client error, and a body that failed to decode, fail at once.</summary>
        internal static bool Transient(UnityWebRequest.Result result, long status) =>
            result == UnityWebRequest.Result.ConnectionError
            || (result == UnityWebRequest.Result.ProtocolError && (status >= 500 || status == 408 || status == 429));

        /// <summary>Seconds a download may go without receiving a byte before it is aborted as stalled and retried like a
        /// dropped connection (#911). A limit on silence, not on the whole download: the 4 MB heightmap on a slow
        /// connection keeps receiving bytes and is never cut off, while a server that accepts the request and never
        /// answers reaches the retry and then BOOT FAIL instead of leaving the loading screen up forever. A fixed limit on
        /// the whole fetch failed slow but healthy fetches before (#503). The value needs the maintainer's sign-off (#911).</summary>
        internal const float StallSeconds = 30f;

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice) until r is done, or
        /// aborts it when no byte has arrived for <paramref name="stallSeconds"/>. On a transient failure (a stall counts as
        /// a connection error) r is released and its URL fetched again after Backoff, up to MaxAttempts in all. Returns the
        /// request that succeeded (the caller releases it); on a final failure every attempt is released and an
        /// IOException names the file. Retrying changes only when a file arrives, never its bytes or the parse order.</summary>
        static async Awaitable<UnityWebRequest> Done(UnityWebRequest r, string name, float stallSeconds)
        {
            for (var attempt = 1; ; attempt++)
            {
                var stalled = false;
                var bytes = r.downloadedBytes;
                var lastByte = Time.realtimeSinceStartup;
                while (!r.isDone)
                {
                    if (r.downloadedBytes != bytes) { bytes = r.downloadedBytes; lastByte = Time.realtimeSinceStartup; }
                    else if (Time.realtimeSinceStartup - lastByte > stallSeconds) { stalled = true; break; }
                    await Awaitable.NextFrameAsync();
                }
                if (!stalled && r.result == UnityWebRequest.Result.Success) return r;
                var error = stalled
                    ? FormattableString.Invariant($"{name}: no data for {stallSeconds:0.#} s ({r.url})")
                    : $"{name}: {r.error} ({r.url})";
                var retry = attempt < MaxAttempts
                    && (stalled ? Transient(UnityWebRequest.Result.ConnectionError, 0) : Transient(r.result, r.responseCode));
                var url = r.url;
                Release(r);
                if (!retry) throw new IOException(attempt > 1 ? $"{error} after {attempt} attempts" : error);
                Debug.LogWarning($"{error}; attempt {attempt + 1} of {MaxAttempts} in {Backoff(attempt):0.#} s");
                var until = Time.realtimeSinceStartup + Backoff(attempt);
                while (Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
                r = Start(url);
            }
        }

        static async Awaitable<string> Text(UnityWebRequest r, string name, float stallSeconds)
        {
            r = await Done(r, name, stallSeconds);
            try { return r.downloadHandler.text; }
            finally { Release(r); }
        }

        static async Awaitable<byte[]> Bytes(UnityWebRequest r, string name, float stallSeconds)
        {
            r = await Done(r, name, stallSeconds);
            try { return r.downloadHandler.data; }
            finally { Release(r); }
        }

        /// <summary>Issue #437: <paramref name="url"/> with b=<paramref name="buildId"/> added to its query when it is an
        /// http(s) URL and the id is not empty; a file path or file:// URI (desktop, the editor) is returned unchanged.
        /// StreamingAssets keep fixed names, so without it a browser could serve this build a previous build's cached file.</summary>
        public static string WithBuildId(string url, string buildId) =>
            buildId.Length == 0 || !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                ? url : url + (url.IndexOf('?') >= 0 ? "&" : "?") + "b=" + Uri.EscapeDataString(buildId);

        /// <summary>Fetches the manifest, then starts every download before awaiting any (parallel), then loads. A transient
        /// failure is retried (Done). When one download fails for good, the others still running are aborted and disposed.
        /// Every URL carries <paramref name="buildId"/> (WithBuildId; the player passes Application.buildGUID).</summary>
        public static Awaitable<GameInputs> FetchAsync(string baseUrl, string buildId = "") => FetchAsync(baseUrl, buildId, StallSeconds);

        /// <summary>FetchAsync with another stall limit (the stalled-server test uses a short one).</summary>
        internal static async Awaitable<GameInputs> FetchAsync(string baseUrl, string buildId, float stallSeconds)
        {
            var manifest = ContentManifest.Parse(await Text(Start(WithBuildId(baseUrl + "/content/" + ContentManifest.FileName, buildId)), ContentManifest.FileName, stallSeconds));
            var goldenText = GoldenCheck.Requested()
                ? await Text(Start(WithBuildId(baseUrl + "/golden/manifest.txt", buildId)), "golden/manifest.txt", stallSeconds) : "";
            var scenarioNames = goldenText.Length == 0 ? new List<string>() : GoldenData.Parse(goldenText).Select(x => x.name).Where(n => n.StartsWith("scenario-", StringComparison.Ordinal)).Select(n => n.Substring("scenario-".Length)).ToList();
            var requests = manifest.Select(f => (file: f, request: Start(WithBuildId(baseUrl + "/content/" + f, buildId)))).ToList();
            var scenarioRequests = scenarioNames.Select(n => (name: n, request: Start(WithBuildId(baseUrl + "/golden/scenarios/" + n + ".yaml", buildId)))).ToList();
            var meta = Start(WithBuildId(baseUrl + "/map/heightmap.json", buildId));
            var r16z = Start(WithBuildId(baseUrl + "/map/" + HeightmapCodec.FileName, buildId));
            try
            {
                var texts = new Dictionary<string, string>();
                foreach (var (file, request) in requests) texts[file] = await Text(request, file, stallSeconds);
                var metaText = await Text(meta, "heightmap.json", stallSeconds);
                var r16zBytes = await Bytes(r16z, HeightmapCodec.FileName, stallSeconds);
                DiagLog.BootPhase($"downloaded {texts.Count + 3} files");
                var inputs = FromTexts(texts, manifest, metaText, r16zBytes);
                inputs.Goldens = new GoldenData { Manifest = goldenText };
                foreach (var (name, request) in scenarioRequests) inputs.Goldens.Scenarios[name] = await Text(request, "golden/scenarios/" + name + ".yaml", stallSeconds);
                DiagLog.BootPhase("content parsed and validated");
                return inputs;
            }
            finally
            {
                foreach (var (_, request) in requests) Release(request);
                foreach (var (_, request) in scenarioRequests) Release(request);
                Release(meta); Release(r16z);
            }
        }
    }
}
