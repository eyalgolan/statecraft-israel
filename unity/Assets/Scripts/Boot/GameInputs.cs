#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The map's raw inputs, already read (no System.IO needed afterwards).</summary>
    public sealed class MapInputs
    {
        public string HeightmapJson = "", HexesJson = "", SettlementsYaml = "";
        /// <summary>content/map/overrides.yaml, the map editor's overrides (#1009); "" when the file is missing (no overrides).</summary>
        public string OverridesYaml = "";
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
    /// the content bundle (FetchAsync, tech notes section 2; one file for every content text, #1054).</summary>
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
            OverridesYaml = ReadIfExists(Path.Combine(contentDir, MapOverrides.FileName)),
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
                    OverridesYaml = texts.TryGetValue(MapOverrides.FileName, out var overrides) ? overrides : "",
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

        static async Awaitable<string> Text(StreamingFetch.Download d, string name, float stallSeconds)
        {
            await StreamingFetch.Done(d, name, stallSeconds);
            try { return d.Request.downloadHandler.text; }
            finally { d.Release(); }
        }

        static async Awaitable<byte[]> Bytes(StreamingFetch.Download d, string name, float stallSeconds)
        {
            await StreamingFetch.Done(d, name, stallSeconds);
            try { return d.Request.downloadHandler.data; }
            finally { d.Release(); }
        }

        /// <summary>Issue #437: <paramref name="url"/> with b=<paramref name="buildId"/> added to its query when it is an
        /// http(s) URL and the id is not empty; a file path or file:// URI (desktop, the editor) is returned unchanged.
        /// StreamingAssets keep fixed names, so without it a browser could serve this build a previous build's cached file.</summary>
        public static string WithBuildId(string url, string buildId) =>
            buildId.Length == 0 || !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                ? url : url + (url.IndexOf('?') >= 0 ? "&" : "?") + "b=" + Uri.EscapeDataString(buildId);

        /// <summary>Fetches the content bundle (every content text in one file, ContentManifest.BundleFileName, so the boot
        /// makes a handful of requests and not one per file: #1054) and the heightmap's two files at once, then loads. A
        /// transient failure is retried (StreamingFetch.Done). When one download fails for good, the others still running are
        /// aborted and disposed. Every URL carries <paramref name="buildId"/> (WithBuildId; the player passes
        /// Application.buildGUID).</summary>
        public static Awaitable<GameInputs> FetchAsync(string baseUrl, string buildId = "") => FetchAsync(baseUrl, buildId, StreamingFetch.StallSeconds);

        /// <summary>FetchAsync with another stall limit (the stalled-server test uses a short one).</summary>
        internal static async Awaitable<GameInputs> FetchAsync(string baseUrl, string buildId, float stallSeconds)
        {
            StreamingFetch.Download Get(string path) => StreamingFetch.Get(WithBuildId(baseUrl + "/" + path, buildId));
            var bundle = Get("content/" + ContentManifest.BundleFileName);
            var meta = Get("map/heightmap.json");
            var r16z = Get("map/" + HeightmapCodec.FileName);
            var scenarioRequests = new List<(string name, StreamingFetch.Download request)>();
            try
            {
                var goldenText = GoldenCheck.Requested() ? await Text(Get("golden/manifest.txt"), "golden/manifest.txt", stallSeconds) : "";
                var scenarioNames = goldenText.Length == 0 ? new List<string>() : GoldenData.Parse(goldenText).Select(x => x.name).Where(n => n.StartsWith("scenario-", StringComparison.Ordinal)).Select(n => n.Substring("scenario-".Length)).ToList();
                foreach (var n in scenarioNames) scenarioRequests.Add((n, Get("golden/scenarios/" + n + ".yaml")));
                var (manifest, texts) = ContentManifest.ParseBundle(await Text(bundle, ContentManifest.BundleFileName, stallSeconds));
                var metaText = await Text(meta, "heightmap.json", stallSeconds);
                var r16zBytes = await Bytes(r16z, HeightmapCodec.FileName, stallSeconds);
                DiagLog.BootPhase($"downloaded {texts.Count + 2} files");
                var inputs = FromTexts(texts, manifest, metaText, r16zBytes);
                inputs.Goldens = new GoldenData { Manifest = goldenText };
                foreach (var (name, request) in scenarioRequests) inputs.Goldens.Scenarios[name] = await Text(request, "golden/scenarios/" + name + ".yaml", stallSeconds);
                DiagLog.BootPhase("content parsed and validated");
                return inputs;
            }
            finally
            {
                foreach (var (_, request) in scenarioRequests) request.Release();
                bundle.Release(); meta.Release(); r16z.Release();
            }
        }
    }
}
