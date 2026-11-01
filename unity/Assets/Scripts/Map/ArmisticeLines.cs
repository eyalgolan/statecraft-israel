#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Game.Boot;
using Statecraft.Game.Map.Routes;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Statecraft.Game.Map
{
    /// <summary>The 1949 armistice lines as their true polylines (#237; option 3 of docs/visual-v1.md, "Region borders"):
    /// every feature of content/map/armistice_lines_1949.geojson drawn as a thin dark line draped on the ground, over the
    /// hex-snapped bands (<see cref="Armistice1949"/>), which stay as they are. On unless turned off (owner, 1
    /// November 2026: "On by default (Recommended)"; #196): "noarmisticelines" in the WebGL page URL, -noarmisticelines on
    /// desktop and in the editor, builds nothing, and the "armisticelines" visual toggle hides a built overlay (the
    /// minimap's Borders button works it with the bands: owner, 2 November 2026, "Tie to Borders button (Recommended)"). A release build draws only the features the owner has signed off
    /// (<see cref="ForBuild"/>) and ships only those (<see cref="ReleaseText"/>). Zoomed out past the gameplay zoom the lines
    /// widen with the distance (<see cref="WidthScale"/>). Each feature shows from its own shown_from date, as the bands do. Features traced
    /// from the low-resolution scan (source_quality: low_res_scan, the Jordan lines) are drawn dotted and a step lighter, so
    /// they read as low accuracy without a legend and without colour alone, and apart from the bands' grey dash for
    /// no-man's land and the demilitarized zones (owner, 2 November 2026: "Restyle Jordan lines (Recommended)"). Zones (the Demilitarized Zones, including the
    /// two Syrian parts too narrow to hold a hex centre) are drawn as their closed outlines. Reuses the route ribbons'
    /// draping (<see cref="RouteLayer.Geometry"/>) and the selected hex's unlit material (HexSelection.mat, copied per
    /// style). Display only: the sim never reads the file, and this changes no sim state and no other map layer.</summary>
    public static class ArmisticeLines
    {
        public const string FileName = "armistice_lines_1949.geojson", RootName = "ArmisticeLines", ToggleName = "armisticelines",
            OffFlag = "noarmisticelines";
        /// <summary>The line's width on the ground: about 4 px at the gameplay zoom (52 m a pixel), a third of a road's, so
        /// it reads as a drawn line, not as a band or a route.</summary>
        public const float WidthM = 200f;
        /// <summary>The low-accuracy lines' width: half as wide again, so the dashes hold their own between the hill
        /// villages' trees and houses, and the wider line hints at the wider error (about +-1-3 km).</summary>
        public const float LowAccuracyWidthM = 300f;
        /// <summary>The camera distance the widths above are set for: the opening and gameplay zoom (CameraRig.Distance;
        /// about 52 m a pixel at 1080 px). Farther out the lines widen with the distance (<see cref="WidthScale"/>), since a
        /// fixed ground width thins to about a pixel at the full-map zoom (owner review of #2012).</summary>
        public const float WidthsSetAtM = 90000f;
        /// <summary>The low-accuracy dots: a DashM period, DashOn of it drawn (300 m marks, as long as the line is wide, and
        /// 450 m gaps; about 6 and 9 px at the gameplay zoom), a pattern apart from MapBorders' neutral dash (1.3 km).</summary>
        public const float DashM = 750f, DashOn = 0.4f;
        /// <summary>sRGB. Firm: a near-black ink that stands out on the gold and olive ground and on both sides' bands.
        /// Low accuracy: a dark grey, a step lighter, so the dashes read as the weaker line (a mid grey was lost on the hills'
        /// ground in the captures).</summary>
        public static readonly Color FirmColour = new Color(0.10f, 0.09f, 0.08f), LowAccuracyColour = new Color(0.24f, 0.23f, 0.21f);

        public sealed class Feature
        {
            public string Id = ""; public DateTime ShownFrom; public bool LowAccuracy;
            /// <summary>The owner's sensitivity review is recorded (status: sensitivity-reviewed; docs/m3-release.md, "Map lines").</summary>
            public bool Reviewed;
            /// <summary>The lines in lon/lat: one per LineString, per MultiLineString member, or per polygon ring (closed).</summary>
            public List<List<(double lon, double lat)>> Parts = new List<List<(double lon, double lat)>>();
        }

        /// <summary>Whether this run draws the overlay (MapScene.Build asks). A seam: a test sets it to build the map with or
        /// without it.</summary>
        internal static Func<bool> Wanted = Requested;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        /// <summary>True unless the run turns the overlay off (<see cref="OffFlag"/>, as "notiltshift" turns tilt-shift off).
        /// The earlier opt-in flag ("armisticelines") is no longer read, so an old link still shows the lines.</summary>
        public static bool Requested(string? url, string[] args, bool webgl) =>
            !(webgl ? UrlFlags.Has(url, OffFlag) : args.Contains("-" + OffFlag));

        /// <summary>Parses the GeoJSON (JSON is YAML, so the YAML reader the content already uses reads it). Every malformed
        /// input throws InvalidDataException naming the file and the feature.</summary>
        public static List<Feature> Parse(string geojson)
        {
            Dictionary<object, object>? doc;
            try { doc = new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(geojson); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: line {e.Start.Line}: {e.InnerException?.Message ?? e.Message}", e); }
            if (doc == null || !doc.TryGetValue("features", out var fs) || !(fs is List<object> list) || list.Count == 0)
                throw new InvalidDataException($"{FileName}: no features");
            var features = new List<Feature>();
            foreach (var item in list)
            {
                var f = item as Dictionary<object, object> ?? throw new InvalidDataException($"{FileName}: a feature is not an object");
                var props = Map(f, "properties", "?");
                var id = Str(props, "id") ?? throw new InvalidDataException($"{FileName}: a feature has no id");
                if (features.Any(x => x.Id == id)) throw new InvalidDataException($"{FileName}: {id}: twice");
                if (!DateTime.TryParseExact(Str(props, "shown_from"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
                    throw new InvalidDataException($"{FileName}: {id}: shown_from is not a yyyy-MM-dd date");
                var geometry = Map(f, "geometry", id);
                var type = Str(geometry, "type");
                var coords = geometry.TryGetValue("coordinates", out var c) ? c as List<object> : null;
                if (coords == null) throw new InvalidDataException($"{FileName}: {id}: no coordinates");
                var feature = new Feature { Id = id, ShownFrom = from, LowAccuracy = Str(props, "source_quality") == "low_res_scan",
                    Reviewed = Str(props, "status") == "sensitivity-reviewed" };
                switch (type)
                {
                    case "LineString": feature.Parts.Add(Line(coords, id)); break;
                    case "MultiLineString": case "Polygon": foreach (var l in coords) feature.Parts.Add(Line(l, id)); break;
                    case "MultiPolygon": foreach (var p in coords) foreach (var l in p as List<object> ?? new List<object>()) feature.Parts.Add(Line(l, id)); break;
                    default: throw new InvalidDataException($"{FileName}: {id}: geometry '{type}' is not a line or a polygon");
                }
                if (feature.Parts.Count == 0) throw new InvalidDataException($"{FileName}: {id}: no coordinates");
                features.Add(feature);
            }
            return features;
        }

        /// <summary>The features a build draws: every one in the editor and development builds; in a release (shareable)
        /// build only those the owner has signed off, since the lines "need the same review before a shared build shows
        /// them" (docs/m3-release.md, "Map lines"; the five flagged on 23 October 2026 stay draft, #196).</summary>
        public static List<Feature> ForBuild(IEnumerable<Feature> features, bool devBuild) =>
            features.Where(f => devBuild || f.Reviewed).ToList();

        /// <summary>The file as a release build ships it (ContentManifest.Copy for a release player): only the features
        /// <see cref="ForBuild"/> draws in a release build, cut out of the text so the header and every kept feature stay as
        /// written. A release build must not ship the draft lines it does not draw (owner review of #2012).</summary>
        public static string ReleaseText(string geojson)
        {
            var signedOff = new HashSet<string>(ForBuild(Parse(geojson), devBuild: false).Select(f => f.Id));
            // The "features" array's items, split at its top-level commas (brackets and braces counted outside strings). The
            // header holds no "features" key or text (tools/armistice_digitise.py writes it), so the first one is the array's.
            int open = geojson.IndexOf('[', geojson.IndexOf("\"features\"", StringComparison.Ordinal)), from = open + 1, depth = 0, i = from;
            var kept = new List<string>();
            for (bool quoted = false; ; i++)
            {
                char c = geojson[i];
                if (quoted) { if (c == '\\') i++; else if (c == '"') quoted = false; }
                else if (c == '"') quoted = true;
                else if (depth == 0 && (c == ',' || c == ']'))
                {
                    var item = geojson.Substring(from, i - from);
                    if (signedOff.Contains(Parse("{\"features\": [" + item + "]}")[0].Id)) kept.Add(item);
                    from = i + 1;
                    if (c == ']') break;
                }
                else if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']') depth--;
            }
            return geojson.Substring(0, open + 1) + string.Join(",", kept) + geojson.Substring(i);
        }

        /// <summary>How much wider than WidthM / LowAccuracyWidthM, and how much longer the dashes, a line is drawn seen from
        /// <paramref name="distanceM"/>: 1 up to <see cref="WidthsSetAtM"/>, then in proportion to the distance (so about the
        /// gameplay width on screen), in steps of a factor of the square root of 2 so each step's mesh is made once, up to
        /// the farthest zoom (CameraController.MaxDistance: about 2.8).</summary>
        public static float WidthScale(float distanceM)
        {
            float d = Mathf.Min(distanceM, CameraController.MaxDistance);
            return d <= WidthsSetAtM ? 1f : Mathf.Pow(2f, Mathf.Round(2f * Mathf.Log(d / WidthsSetAtM, 2f)) / 2f);
        }

        static Dictionary<object, object> Map(Dictionary<object, object> o, string key, string id) =>
            o.TryGetValue(key, out var v) && v is Dictionary<object, object> d ? d : throw new InvalidDataException($"{FileName}: {id}: no {key}");

        static string? Str(Dictionary<object, object> o, string key) => o.TryGetValue(key, out var v) ? v as string : null;

        static List<(double lon, double lat)> Line(object o, string id)
        {
            var points = o as List<object> ?? throw new InvalidDataException($"{FileName}: {id}: a line is not a list of points");
            var line = new List<(double lon, double lat)>(points.Count);
            foreach (var p in points)
            {
                if (!(p is List<object> xy) || xy.Count < 2
                    || !double.TryParse(xy[0] as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
                    || !double.TryParse(xy[1] as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                    || double.IsNaN(lon) || double.IsInfinity(lon) || double.IsNaN(lat) || double.IsInfinity(lat))
                    throw new InvalidDataException($"{FileName}: {id}: a point is not [lon, lat]");
                line.Add((lon, lat));
            }
            if (line.Count < 2) throw new InvalidDataException($"{FileName}: {id}: a line needs two points");
            return line;
        }

        /// <summary>The drawn pieces of a dashed <paramref name="line"/> (world x, z): from its start, DashOn of every DashM
        /// period (times <paramref name="scale"/>, the <see cref="WidthScale"/> it is drawn at), each piece following the
        /// line's bends.</summary>
        public static List<List<Vector2>> Dashes(List<Vector2> line, float scale = 1f)
        {
            var dashes = new List<List<Vector2>>();
            List<Vector2>? dash = null;
            float s = 0f, period = DashM * scale, on = period * DashOn;
            for (int i = 1; i < line.Count; i++)
            {
                var a = line[i - 1]; var b = line[i]; float len = (b - a).magnitude;
                float t = 0f;
                while (t < len)
                {
                    float phase = (s + t) % period;
                    bool drawn = phase < on;
                    float step = Mathf.Min(len - t, drawn ? on - phase : period - phase);
                    if (drawn)
                    {
                        if (dash == null) { dash = new List<Vector2> { Vector2.Lerp(a, b, t / len) }; dashes.Add(dash); }
                        dash.Add(Vector2.Lerp(a, b, (t + step) / len));
                    }
                    else dash = null;
                    t += Mathf.Max(step, 1e-3f);
                }
                s += len;
            }
            dashes.RemoveAll(d => d.Count < 2);
            return dashes;
        }

        /// <summary>Builds the overlay under <paramref name="terrain"/> (a map rebuild destroys it): one child per feature,
        /// shown as at <paramref name="date"/>, each a ribbon mesh in a copy of <paramref name="lineMaterial"/> (the selected
        /// hex's unlit material) per style, at the gameplay width until the camera says otherwise
        /// (ArmisticeLine.ShowFor). Meshes and materials are owned by the root's MapRuntimeAssets, which also
        /// unregisters the "armisticelines" toggle. Returns the root.</summary>
        public static GameObject Build(Terrain terrain, GeoFrame frame, IReadOnlyList<Feature> features, Material lineMaterial, DateTime date)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            if (lineMaterial == null) throw new ArgumentNullException(nameof(lineMaterial));
            var root = new GameObject(RootName);
            root.transform.SetParent(terrain.transform, false);
            root.transform.position = Vector3.zero;   // children are in world space
            var assets = root.AddComponent<MapRuntimeAssets>();
            var lines = new GameObject("Lines");
            lines.transform.SetParent(root.transform, false);
            Material Copy(string name, Color c)
            {
                var m = assets.Own(new Material(lineMaterial) { name = name });
                m.SetColor("_BaseColor", c);
                return m;
            }
            var firm = Copy("ArmisticeLine", FirmColour); var low = Copy("ArmisticeLineLowAccuracy", LowAccuracyColour);
            var data = terrain.terrainData; var origin = terrain.transform.position;
            foreach (var f in features)
            {
                var world = f.Parts.Select(part => part.Select(p => { var v = frame.ToWorld(p.lon, p.lat, 0); return new Vector2(v.x, v.z); }).ToList()).ToList();
                // The line's mesh at a width scale: widths and dashes scaled together, so the dashed lines keep their look.
                Mesh MeshAt(float scale)
                {
                    var pieces = new List<(string id, List<Vector2> line, float width)>();
                    foreach (var line in world)
                        if (f.LowAccuracy) pieces.AddRange(Dashes(line, scale).Select(d => (f.Id, d, LowAccuracyWidthM * scale)));
                        else pieces.Add((f.Id, line, WidthM * scale));
                    var mesh = assets.Own(RouteLayer.Geometry(pieces, data, origin).ToMesh($"{RootName} {f.Id}"));
                    mesh.UploadMeshData(true);
                    return mesh;
                }
                var go = new GameObject(f.Id);
                go.transform.SetParent(lines.transform, false);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = f.LowAccuracy ? low : firm;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var mark = go.AddComponent<ArmisticeLine>();
                mark.Id = f.Id; mark.ShownFrom = f.ShownFrom; mark.LowAccuracy = f.LowAccuracy;
                mark.MeshAt = MeshAt; mark.DrawAt(1f);
            }
            ShowAt(root, date);
            assets.OnTeardown(VisualToggles.Register(ToggleName, on => { if (lines != null) lines.SetActive(on); }));
            return root;
        }

        /// <summary>Shows each line of the map's overlay from its date (the game calls it on every state change, beside
        /// RouteLayer.ShowAt). A map without the overlay is left alone.</summary>
        public static void ShowAt(MapScene map, DateTime date)
        {
            if (map == null || map.Terrain == null) return;
            var root = map.Terrain.transform.Find(RootName);
            if (root != null) ShowAt(root.gameObject, date);
        }

        public static void ShowAt(GameObject root, DateTime date)
        {
            foreach (var l in root.GetComponentsInChildren<ArmisticeLine>(true)) l.gameObject.SetActive(date >= l.ShownFrom);
        }
    }
}
