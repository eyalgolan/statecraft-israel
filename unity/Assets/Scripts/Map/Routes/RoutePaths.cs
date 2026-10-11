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
            }
            doc.Routes.RemoveAll(skipped.Contains);
            return doc.Routes;
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
