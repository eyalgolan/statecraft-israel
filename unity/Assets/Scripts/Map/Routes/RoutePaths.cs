#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>One traced route of content/map/route_paths_1948.yaml: a railway, a main road or a track (the 1948 Burma
    /// Road), as a lon/lat polyline (issue #35; tools/routes_1948.py writes the file, routes_1948.yaml holds the sources).</summary>
    public sealed class RoutePath
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Status { get; set; } = "";
        public double LengthKm { get; set; }
        /// <summary>The display window: a quoted 'YYYY' (1 January) or 'YYYY-MM-DD'. The map draws the route while the game
        /// date is on or after From and, when To is given, before To (C1: the Burma Road from 14 June 1948).</summary>
        public string From { get; set; } = "";
        public string? To { get; set; }
        /// <summary>The trace's quality, written by the tool: the share of traced pixels on the route's own map symbol,
        /// and the longest run off it (m).</summary>
        public double OnSymbol { get; set; }
        public double MaxGapM { get; set; }
        /// <summary>[lon, lat] pairs, in order along the route.</summary>
        public List<List<double>> Points { get; set; } = new List<List<double>>();
        /// <summary>The railway's train service in normal play (issue #668), or null: from the runtime file's service row
        /// (tools/route_paths_bake.py reads it from routes_1948.yaml); route_paths_1948.yaml never carries one.</summary>
        [YamlIgnore] public RouteService? Service { get; set; }
    }

    /// <summary>A railway's train service (issue #668; maintainer decision on #35, 16 October 2026): the date regular
    /// service on it resumed (a quoted 'YYYY-MM-DD', sourced in routes_1948.yaml) and the stretch the train runs, between
    /// the line's points nearest Start and End (lon, lat).</summary>
    public sealed class RouteService
    {
        public string From = "";
        public double StartLon, StartLat, EndLon, EndLat;
    }

    public sealed class RoutePathFile { public List<RoutePath> Routes { get; set; } = new List<RoutePath>(); }

    /// <summary>A route's display window as dates.</summary>
    public readonly struct RouteWindow : IEquatable<RouteWindow>
    {
        public readonly DateTime From; public readonly DateTime? To;
        public RouteWindow(DateTime from, DateTime? to) { From = from; To = to; }
        public bool Covers(DateTime date) => date >= From && (To == null || date < To.Value);
        public bool Equals(RouteWindow o) => From == o.From && To == o.To;
        public override bool Equals(object? o) => o is RouteWindow w && Equals(w);
        public override int GetHashCode() => HashCode.Combine(From, To);
        public override string ToString() => To == null ? $"{From:yyyy-MM-dd}" : $"{From:yyyy-MM-dd}..{To:yyyy-MM-dd}";
    }

    /// <summary>Reads route_paths_1948.yaml. The map draws it (RouteLayer); the sim never reads it, so it is not in the
    /// content hash (like settlements_1948.yaml's coordinates).</summary>
    public static class RoutePaths
    {
        public const string DefaultFileName = "route_paths_1948.yaml";
        /// <summary>The baked runtime form the player reads at boot (tools/route_paths_bake.py; issue #321, as #291's
        /// localities_1948.tsv): one row per route, its points in one field.</summary>
        public const string RuntimeFileName = "route_paths_1948.tsv";
        /// <summary>Bump it, with tools/route_paths_bake.py's VERSION, when the runtime file's layout or its reading
        /// (ParseRuntime, RuntimeRows) changes; tools/tests/test_route_paths_bake.py pins a fingerprint of that code.</summary>
        public const int RuntimeVersion = 3;   // 2: the body_check row (review of #547); 3: the service rows (#668)
        public const string Rail = "rail", Road = "road", Track = "track";
        static readonly HashSet<string> Kinds = new HashSet<string> { Rail, Road, Track };
        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");
        static readonly Regex DateRule = new Regex(@"^\d{4}(-\d{2}-\d{2})?\z");

        /// <summary>A window bound: 'YYYY' is 1 January of that year. Throws FormatException on anything else.</summary>
        public static DateTime Bound(string text)
        {
            if (text == null || !DateRule.IsMatch(text)) throw new FormatException($"'{text}' is not 'YYYY' or 'YYYY-MM-DD'");
            try
            {
                return text.Length == 4
                    ? new DateTime(int.Parse(text, CultureInfo.InvariantCulture), 1, 1)
                    : DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException e) { throw new FormatException($"'{text}' is not a date", e); }   // year 0000
        }

        public static RouteWindow WindowOf(RoutePath r) => new RouteWindow(Bound(r.From), r.To == null ? (DateTime?)null : Bound(r.To));

        /// <summary>The routes to draw, or none (with a warning through <paramref name="warn"/>) when the file is missing
        /// (an empty text: a stale manifest or an old content copy) or malformed: a cosmetic layer never stops the map
        /// from building (review I3, as promo-art reads its population file).</summary>
        public static List<RoutePath> ReadOrNone(string? yaml, (double lon0, double lon1, double lat0, double lat1)? bounds,
            Action<string> warn, string file = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(yaml)) { warn($"{file}: missing, so the map draws no railways or roads"); return new List<RoutePath>(); }
            try { return Parse(yaml!, bounds, file, warn); }
            catch (InvalidDataException e) { warn($"{e.Message}; the map draws no railways or roads"); return new List<RoutePath>(); }
        }

        /// <summary>The boot read (issue #321): the routes from the runtime file when it was baked from this YAML by this
        /// version (ParseRuntime), else, with one warning through <paramref name="warn"/>, from the YAML (ReadOrNone).</summary>
        public static List<RoutePath> ReadRuntimeOrYaml(string? tsv, string? yaml, (double lon0, double lon1, double lat0, double lat1)? bounds,
            Action<string> warn)
        {
            // One cause, one warning (review of #547, N1): with no YAML, only its own warning.
            if (string.IsNullOrWhiteSpace(yaml)) return ReadOrNone(yaml, bounds, warn);
            if (string.IsNullOrWhiteSpace(tsv)) { warn($"{RuntimeFileName}: missing; reading the YAML"); return ReadOrNone(yaml, bounds, warn); }
            RoutePathFile doc;
            try { doc = RuntimeDoc(tsv!, yaml!, RuntimeFileName); }
            catch (InvalidDataException e) { warn($"{e.Message}; reading the YAML"); return ReadOrNone(yaml, bounds, warn); }
            // The file is this YAML's bake, so the YAML would fail the checks the same way: no second parse.
            try { return Validate(doc, bounds, RuntimeFileName, warn); }
            catch (InvalidDataException e) { warn($"{e.Message}; the map draws no railways or roads"); return new List<RoutePath>(); }
        }

        /// <summary>Parses the runtime file with the same checks as <see cref="Parse"/>. Throws InvalidDataException naming
        /// <paramref name="file"/> (and the line, for a malformed row) when it is of another version, was baked from another
        /// YAML than <paramref name="yaml"/> (RouteRibbonFile.Check of its text), or does not parse.</summary>
        public static List<RoutePath> ParseRuntime(string tsv, string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null,
            Action<string>? skipBadWindow = null, string file = RuntimeFileName)
        {
            return Validate(RuntimeDoc(tsv, yaml, file), bounds, file, skipBadWindow);
        }

        /// <summary>The runtime file's routes before the reader's checks; throws when it is of another version, made from
        /// another YAML, cut short or malformed.</summary>
        static RoutePathFile RuntimeDoc(string tsv, string yaml, string file)
        {
            var (version, check, routes) = RuntimeRows(tsv, file);
            if (version != RuntimeVersion) throw new InvalidDataException($"{file}: version {version}, this build reads {RuntimeVersion} (re-bake: tools/route_paths_bake.py)");
            if (check != RouteRibbonFile.Check(yaml)) throw new InvalidDataException($"{file}: made from another {DefaultFileName} (re-bake: tools/route_paths_bake.py)");
            return new RoutePathFile { Routes = routes };
        }

        /// <summary>The runtime file's body_check: FNV-1a over the UTF-8 bytes of its route and service rows, each followed by '\n', in
        /// order (tools/route_paths_bake.py's body_check).</summary>
        public static int BodyCheck(IEnumerable<string> routeRows)
        {
            uint h = 2166136261;
            foreach (var row in routeRows)
            {
                foreach (var b in System.Text.Encoding.UTF8.GetBytes(row)) { h ^= b; h *= 16777619; }
                h ^= (byte)'\n'; h *= 16777619;
            }
            return (int)h;
        }

        static (int version, int check, List<RoutePath> routes) RuntimeRows(string tsv, string file)
        {
            int? version = null, check = null, count = null, body = null;
            var routes = new List<RoutePath>();
            var rows = new List<string>();
            var lines = tsv.Split('\n');
            double Num(string text, string what, int line) =>
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                    ? v : throw new InvalidDataException($"{file}:{line}: {what} '{text}' is not a number");
            int Int(string text, string what, int line) =>
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                    ? v : throw new InvalidDataException($"{file}:{line}: {what} '{text}' is not an integer");
            for (int i = 0; i < lines.Length; i++)
            {
                var text = lines[i].TrimEnd('\r');
                if (text.Length == 0 || text[0] == '#') continue;
                int line = i + 1;
                if (body != null) throw new InvalidDataException($"{file}:{line}: a row after body_check, which must be the last");
                var c = text.Split('\t');
                if (c[0] == "meta" && c.Length == 3 && c[1] == "body_check") body = Int(c[2], "body_check", line);
                else if (c[0] == "meta" && c.Length == 3 && c[1] == "version") version = Int(c[2], "version", line);
                else if (c[0] == "meta" && c.Length == 3 && c[1] == "yaml_check") check = Int(c[2], "yaml_check", line);
                else if (c[0] == "meta" && c.Length == 3 && c[1] == "routes") count = Int(c[2], "routes", line);
                else if (c[0] == "service")
                {
                    if (c.Length != 5) throw new InvalidDataException($"{file}:{line}: a service row has {c.Length} fields, not 5");
                    rows.Add(text);
                    var route = routes.Find(r => r.Id == c[1]) ?? throw new InvalidDataException($"{file}:{line}: a service for '{c[1]}', which no route row before it has");
                    if (route.Service != null) throw new InvalidDataException($"{file}:{line}: a second service for '{c[1]}'");
                    (double, double) End(string pair, string what)
                    {
                        int sp = pair.IndexOf(' ');
                        if (sp < 0 || pair.IndexOf(' ', sp + 1) >= 0) throw new InvalidDataException($"{file}:{line}: {what} '{pair}' is not 'lon lat'");
                        return (Num(pair.Substring(0, sp), what + " lon", line), Num(pair.Substring(sp + 1), what + " lat", line));
                    }
                    var (startLon, startLat) = End(c[3], "start"); var (endLon, endLat) = End(c[4], "end");
                    route.Service = new RouteService { From = c[2], StartLon = startLon, StartLat = startLat, EndLon = endLon, EndLat = endLat };
                }
                else if (c[0] == "route")
                {
                    if (c.Length != 11) throw new InvalidDataException($"{file}:{line}: a route row has {c.Length} fields, not 11");
                    rows.Add(text);
                    var pairs = c[10].Split(';');
                    if (pairs.Length != Int(c[9], "point_count", line))
                        throw new InvalidDataException($"{file}:{line}: {pairs.Length} points, the row says {c[9]}");
                    var points = new List<List<double>>(pairs.Length);
                    foreach (var pair in pairs)
                    {
                        int sp = pair.IndexOf(' ');
                        if (sp < 0 || pair.IndexOf(' ', sp + 1) >= 0) throw new InvalidDataException($"{file}:{line}: point '{pair}' is not 'lon lat'");
                        points.Add(new List<double> { Num(pair.Substring(0, sp), "lon", line), Num(pair.Substring(sp + 1), "lat", line) });
                    }
                    routes.Add(new RoutePath
                    {
                        Id = c[1], Kind = c[2], Status = c[3], From = c[4], To = c[5].Length == 0 ? null : c[5],
                        LengthKm = Num(c[6], "length_km", line), OnSymbol = Num(c[7], "on_symbol", line), MaxGapM = Num(c[8], "max_gap_m", line),
                        Points = points,
                    });
                }
                else throw new InvalidDataException($"{file}:{line}: unknown row '{c[0]}'");
            }
            if (version == null || check == null || count == null) throw new InvalidDataException($"{file}: no version, yaml_check or routes row");
            // A file cut anywhere, even inside a number, loses the last row or no longer matches it (review of #547, S1).
            if (body == null) throw new InvalidDataException($"{file}: no body_check row at the end (cut short?)");
            if (body != BodyCheck(rows)) throw new InvalidDataException($"{file}: the route rows do not match body_check (cut short or edited?)");
            if (routes.Count != count) throw new InvalidDataException($"{file}: {routes.Count} routes, the file says {count}");
            return (version.Value, check.Value, routes);
        }

        /// <summary>Parses the file. Every problem throws InvalidDataException naming <paramref name="file"/> and, for
        /// content errors, the route id: YAML syntax, duplicate or unknown keys, no routes, a bad or duplicate id, an unknown
        /// kind, fewer than two points, a point that is not a [lon, lat] pair of finite numbers or lies outside
        /// <paramref name="bounds"/> when given, or a bad from/to (unless <paramref name="skipBadWindow"/> is given: then that
        /// route alone is left out and reported through it).</summary>
        public static List<RoutePath> Parse(string yaml, (double lon0, double lon1, double lat0, double lat1)? bounds = null,
            string file = DefaultFileName, Action<string>? skipBadWindow = null)
        {
            RoutePathFile doc;
            try
            {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking()
                    .Build().Deserialize<RoutePathFile>(yaml);
            }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            return Validate(doc, bounds, file, skipBadWindow);
        }

        static List<RoutePath> Validate(RoutePathFile? doc, (double lon0, double lon1, double lat0, double lat1)? bounds, string file,
            Action<string>? skipBadWindow)
        {
            if (doc?.Routes == null || doc.Routes.Count == 0) throw new InvalidDataException($"{file}: no routes");
            var ids = new HashSet<string>();
            var skipped = new List<RoutePath>();
            foreach (var r in doc.Routes)
            {
                if (r == null || string.IsNullOrWhiteSpace(r.Id)) throw new InvalidDataException($"{file}: a route has an empty id");
                var at = $"{file}: '{r.Id}'";
                if (!IdRule.IsMatch(r.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!ids.Add(r.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (!Kinds.Contains(r.Kind)) throw new InvalidDataException($"{at} has unknown kind '{r.Kind}' (rail, road or track)");
                if (r.Points == null || r.Points.Count < 2) throw new InvalidDataException($"{at} needs at least two points");
                string? badWindow = null;
                try
                {
                    var window = WindowOf(r);
                    if (window.To != null && window.To <= window.From) badWindow = $"{at} to must be after from";
                }
                catch (FormatException e) { badWindow = $"{at} from/to: {e.Message}"; }
                if (badWindow != null)
                {
                    // The tolerant read (ReadOrNone) skips just this route with a warning (re-review nit 1); a strict read throws.
                    if (skipBadWindow == null) throw new InvalidDataException(badWindow);
                    skipBadWindow($"{badWindow}; that route is not drawn");
                    skipped.Add(r);
                }
                for (int i = 0; i < r.Points.Count; i++)
                {
                    var p = r.Points[i];
                    if (p == null || p.Count != 2 || !IsFinite(p[0]) || !IsFinite(p[1]))
                        throw new InvalidDataException($"{at} point {i} is not a [lon, lat] pair of numbers");
                    if (bounds is { } b && (p[0] < b.lon0 || p[0] > b.lon1 || p[1] < b.lat0 || p[1] > b.lat1))
                        throw new InvalidDataException(FormattableString.Invariant($"{at} point {i} ({p[0]}, {p[1]}) is outside the map"));
                }
                if (r.Service != null && !skipped.Contains(r) && ServiceProblem(r, bounds) is { } problem)
                {
                    // After the route's own checks, so one cause gives one warning. A bad service never costs the routes
                    // (the train is cosmetic): the tolerant read drops the train with a warning; a strict read throws.
                    if (skipBadWindow == null) throw new InvalidDataException($"{at} {problem}");
                    skipBadWindow($"{at} {problem}; no train runs on it");
                    r.Service = null;
                }
            }
            doc.Routes.RemoveAll(skipped.Contains);
            return doc.Routes;
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Why the route's service cannot run (issue #668), or null: only on a railway, a 'YYYY-MM-DD' date inside the
        /// route's window, and both ends finite and on the map.</summary>
        static string? ServiceProblem(RoutePath r, (double lon0, double lon1, double lat0, double lat1)? bounds)
        {
            var s = r.Service!;
            if (r.Kind != Rail) return "has a service but is not a railway";
            DateTime from;
            try { from = Bound(s.From); }
            catch (FormatException e) { return $"service from: {e.Message}"; }
            if (s.From.Length != 10) return $"service from '{s.From}' is not a day ('YYYY-MM-DD')";
            try { if (!WindowOf(r).Covers(from)) return $"service from {s.From} is outside the route's window"; }
            catch (FormatException) { return "service on a route with a bad window"; }
            foreach (var (lon, lat, what) in new[] { (s.StartLon, s.StartLat, "start"), (s.EndLon, s.EndLat, "end") })
                if (!IsFinite(lon) || !IsFinite(lat) || bounds is { } b && (lon < b.lon0 || lon > b.lon1 || lat < b.lat0 || lat > b.lat1))
                    return FormattableString.Invariant($"service {what} ({lon}, {lat}) is outside the map");
            return null;
        }
    }
}
