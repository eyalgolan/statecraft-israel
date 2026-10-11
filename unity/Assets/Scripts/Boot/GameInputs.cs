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
        /// <summary>content/map/route_paths_1948.yaml: the traced railways and roads the map draws (issue #35; map-only).</summary>
        public string RoutesYaml = "";
        /// <summary>content/map/settlement_population_1945.yaml (the settlement kits' footprints; "" when absent). The sim
        /// never reads it.</summary>
        public string PopulationYaml = "";
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

        public static MapInputs MapFromDisk(string contentDir, string mapDir) => new MapInputs
        {
            HeightmapJson = File.ReadAllText(Path.Combine(mapDir, "heightmap.json")),
            HeightmapR16z = File.ReadAllBytes(Path.Combine(mapDir, HeightmapCodec.FileName)),
            HexesJson = File.ReadAllText(Path.Combine(contentDir, "map", "hexes.json")),
            SettlementsYaml = File.ReadAllText(Path.Combine(contentDir, "map", SettlementPlacer.DefaultFileName)),
            RoutesYaml = ReadIfExists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Routes.RoutePaths.DefaultFileName)),
            PopulationYaml = File.Exists(Path.Combine(contentDir, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName))
                ? File.ReadAllText(Path.Combine(contentDir, "map", Statecraft.Game.Map.Kits.KitPopulation.FileName)) : "",
        };

        /// <summary>A map-only, cosmetic input: "" when the file is missing (the map then skips that layer with a warning).</summary>
        static string ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

        public static GameInputs FromDisk(string contentDir, string mapDir) =>
            new GameInputs { Content = ContentLoader.Load(contentDir), Map = MapFromDisk(contentDir, mapDir) };

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
                    RoutesYaml = texts.TryGetValue("map/" + Statecraft.Game.Map.Routes.RoutePaths.DefaultFileName, out var routes) ? routes : "",
                    PopulationYaml = texts.TryGetValue("map/" + Statecraft.Game.Map.Kits.KitPopulation.FileName, out var pop) ? pop : "",
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

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice) until r is done. On a
        /// transient failure r is released and its URL fetched again after Backoff, up to MaxAttempts in all. Returns the
        /// request that succeeded (the caller releases it); on a final failure every attempt is released and an
        /// IOException names the file. Retrying changes only when a file arrives, never its bytes or the parse order.</summary>
        static async Awaitable<UnityWebRequest> Done(UnityWebRequest r, string name)
        {
            for (var attempt = 1; ; attempt++)
            {
                while (!r.isDone) await Awaitable.NextFrameAsync();
                if (r.result == UnityWebRequest.Result.Success) return r;
                var error = $"{name}: {r.error} ({r.url})";
                var retry = attempt < MaxAttempts && Transient(r.result, r.responseCode);
                var url = r.url;
                Release(r);
                if (!retry) throw new IOException(attempt > 1 ? $"{error} after {attempt} attempts" : error);
                Debug.LogWarning($"{error}; attempt {attempt + 1} of {MaxAttempts} in {Backoff(attempt):0.#} s");
                var until = Time.realtimeSinceStartup + Backoff(attempt);
                while (Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
                r = Start(url);
            }
        }

        static async Awaitable<string> Text(UnityWebRequest r, string name)
        {
            r = await Done(r, name);
            try { return r.downloadHandler.text; }
            finally { Release(r); }
        }

        static async Awaitable<byte[]> Bytes(UnityWebRequest r, string name)
        {
            r = await Done(r, name);
            try { return r.downloadHandler.data; }
            finally { Release(r); }
        }

        /// <summary>Fetches the manifest, then starts every download before awaiting any (parallel), then loads. A transient
        /// failure is retried (Done). When one download fails for good, the others still running are aborted and disposed.</summary>
        public static async Awaitable<GameInputs> FetchAsync(string baseUrl)
        {
            var manifest = ContentManifest.Parse(await Text(Start(baseUrl + "/content/" + ContentManifest.FileName), ContentManifest.FileName));
            var requests = manifest.Select(f => (file: f, request: Start(baseUrl + "/content/" + f))).ToList();
            var meta = Start(baseUrl + "/map/heightmap.json");
            var r16z = Start(baseUrl + "/map/" + HeightmapCodec.FileName);
            try
            {
                var texts = new Dictionary<string, string>();
                foreach (var (file, request) in requests) texts[file] = await Text(request, file);
                var metaText = await Text(meta, "heightmap.json");
                var r16zBytes = await Bytes(r16z, HeightmapCodec.FileName);
                DiagLog.BootPhase($"downloaded {texts.Count + 3} files");
                var inputs = FromTexts(texts, manifest, metaText, r16zBytes);
                DiagLog.BootPhase("content parsed and validated");
                return inputs;
            }
            finally
            {
                foreach (var (_, request) in requests) Release(request);
                Release(meta); Release(r16z);
            }
        }
    }
}
