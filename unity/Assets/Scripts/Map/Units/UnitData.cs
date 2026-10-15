#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map.Units
{
    /// <summary>A cited source: its URL and the day it was checked.</summary>
    public sealed class UnitSource
    {
        public string Url { get; set; } = "";
        public string Checked { get; set; } = "";
    }

    /// <summary>Where a unit stood: a named place for a dated window [From, To).</summary>
    public sealed class UnitAnchor
    {
        public string Place { get; set; } = "";
        public double Lon { get; set; }
        public double Lat { get; set; }
        public string From { get; set; } = "";
        /// <summary>The sourced start, as the card shows it ("February 1948"), or null when no source dates the arrival
        /// (the card then gives no start; the game's first day is never shown as one).</summary>
        public string? Since { get; set; }
        public string To { get; set; } = "";
        /// <summary>Why the window ends: <see cref="UnitData.EndBySource"/> (with <see cref="EndNote"/>) or
        /// <see cref="UnitData.EndByFrontArmistice"/> (with <see cref="Front"/>).</summary>
        public string EndBasis { get; set; } = "";
        public string? EndNote { get; set; }
        public string? Front { get; set; }
        /// <summary>Which side of the place the token stands on, where the nearest clear spot would be the other side's
        /// ground: a sourced point the placer moves towards (within the one-hex limit; review MF1), or null.</summary>
        public UnitToward? Toward { get; set; }
        /// <summary>The hex [q, r] the token stands on, chosen on the unit's own side next to its place (with
        /// <see cref="Toward"/> giving the side and its source), or null for the placer's own choice.</summary>
        public List<int>? PlaceHex { get; set; }
        /// <summary>"month" when a window edge is known only to the month (the first of the month is used).</summary>
        public string? Precision { get; set; }
        public string? Note { get; set; }
        public List<UnitSource> Sources { get; set; } = new List<UnitSource>();
    }

    /// <summary>An aircraft type a squadron flew for a dated window.</summary>
    public sealed class UnitAircraft
    {
        public string Type { get; set; } = "";
        public string From { get; set; } = "";
        public string? To { get; set; }
        public string? Note { get; set; }
        public List<UnitSource> Sources { get; set; } = new List<UnitSource>();
    }

    /// <summary>A sourced point on the unit's own side of its place (UnitAnchor.Toward).</summary>
    public sealed class UnitToward
    {
        public string Place { get; set; } = "";
        public double Lon { get; set; }
        public double Lat { get; set; }
        public string Note { get; set; } = "";
        public List<UnitSource> Sources { get; set; } = new List<UnitSource>();
    }

    /// <summary>A unit's sourced disbandment: no window may run past it.</summary>
    public sealed class UnitDisbanded
    {
        public string Date { get; set; } = "";
        public string? Precision { get; set; }
        public List<UnitSource> Sources { get; set; } = new List<UnitSource>();
    }

    /// <summary>One unit of content/map/units_1948.yaml (docs/units.md, "Units in play").</summary>
    public sealed class UnitEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Number { get; set; } = "";
        public string Service { get; set; } = "";
        public string? Formed { get; set; }
        public string? FirstCommander { get; set; }
        public string Sector { get; set; } = "";
        public bool WikipediaOnly { get; set; }
        public string Status { get; set; } = "";
        /// <summary>A person's sign-off beside the status (#375, "humans only"): both or neither, a GitHub handle and a day;
        /// Statecraft.Sim.ReviewRecord checks them. A key that is present but blank counts as present.</summary>
        public string? ReviewedBy { get => reviewedBy; set { reviewedBy = value; ReviewedByGiven = true; } }
        [YamlIgnore] public bool ReviewedByGiven { get; private set; }
        string? reviewedBy;
        public string? ReviewedOn { get => reviewedOn; set { reviewedOn = value; ReviewedOnGiven = true; } }
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
        /// <summary>Why the map does not draw the unit (the sources name it but do not place it), or null.</summary>
        public string? Hidden { get; set; }
        public List<string>? ToVerify { get; set; }
        /// <summary>The riflemen's headgear (<see cref="UnitData.Headgears"/>), only where a source ties it to the unit.</summary>
        public List<string>? Headgear { get; set; }
        public List<UnitSource>? HeadgearSources { get; set; }
        public UnitDisbanded? Disbanded { get; set; }
        public List<UnitAnchor> Anchors { get; set; } = new List<UnitAnchor>();
        public List<UnitAircraft>? Aircraft { get; set; }
        public List<UnitSource>? Sources { get; set; }

        /// <summary>Every source of the row: its own, its anchors' and its aircraft's.</summary>
        public IEnumerable<UnitSource> AllSources()
        {
            if (Sources != null) foreach (var s in Sources) yield return s;
            foreach (var a in Anchors)
            {
                foreach (var s in a.Sources) yield return s;
                if (a.Toward != null) foreach (var s in a.Toward.Sources) yield return s;
            }
            if (Aircraft != null) foreach (var t in Aircraft) foreach (var s in t.Sources) yield return s;
            if (HeadgearSources != null) foreach (var s in HeadgearSources) yield return s;
            if (Disbanded != null) foreach (var s in Disbanded.Sources) yield return s;
        }
    }

    public sealed class UnitFile { public List<UnitEntry> Units { get; set; } = new List<UnitEntry>(); }

    /// <summary>Reads content/map/units_1948.yaml. The map draws it (UnitRoster, UnitLayer); the sim never reads it, so it
    /// is not in the content hash (as route_paths_1948.yaml).</summary>
    public static class UnitData
    {
        public const string DefaultFileName = "units_1948.yaml";
        public const string Army = "army", Air = "air", Navy = "navy";
        public const string AviaS199 = "avia-s199", Spitfire = "spitfire";
        public const string EndBySource = "source", EndByFrontArmistice = "front_armistice";
        public const string SlouchHat = "slouch_hat", SteelHelmet = "steel_helmet", WoolCap = "wool_cap";
        public static readonly IReadOnlyList<string> Headgears = new[] { SlouchHat, SteelHelmet, WoolCap };
        /// <summary>The uniform default where no source ties headgear to a unit: the 1948 photographs' bush hat and Mk II
        /// steel helmet (docs/units.md, "Sources").</summary>
        public static readonly IReadOnlyList<string> DefaultHeadgear = new[] { SlouchHat, SteelHelmet, SlouchHat };

        /// <summary>The day each front's armistice was signed (https://en.wikipedia.org/wiki/1949_Armistice_Agreements,
        /// checked 2026-10-11). A window with no sourced end ends on its front's day (end_basis: front_armistice).</summary>
        public static readonly IReadOnlyDictionary<string, DateTime> FrontArmistice = new Dictionary<string, DateTime>
        {
            ["egypt"] = new DateTime(1949, 2, 24), ["lebanon"] = new DateTime(1949, 3, 23),
            ["jordan"] = new DateTime(1949, 4, 3), ["syria"] = new DateTime(1949, 7, 20),
        };
        static readonly HashSet<string> Services = new HashSet<string> { Army, Air, Navy };
        static readonly HashSet<string> AircraftTypes = new HashSet<string> { AviaS199, Spitfire };
        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");
        static readonly Regex DayRule = new Regex(@"^\d{4}-\d{2}-\d{2}\z");

        /// <summary>A 'YYYY-MM-DD' day. Throws FormatException on anything else.</summary>
        public static DateTime Day(string? text)
        {
            if (text == null || !DayRule.IsMatch(text)) throw new FormatException($"'{text}' is not 'YYYY-MM-DD'");
            if (!DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                throw new FormatException($"'{text}' is not a date");
            return d;
        }

        /// <summary>The units, or none (with a warning) when the file is missing or malformed: a cosmetic layer never stops
        /// the game (as RoutePaths.ReadOrNone).</summary>
        public static List<UnitEntry> ReadOrNone(string? yaml, Action<string> warn, string file = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(yaml)) { warn($"{file}: missing, so the map draws no units"); return new List<UnitEntry>(); }
            try { return Parse(yaml!, file); }
            catch (InvalidDataException e) { warn($"{e.Message}; the map draws no units"); return new List<UnitEntry>(); }
        }

        /// <summary>Parses and checks the file. Every problem throws InvalidDataException naming the file and the unit: YAML
        /// syntax, unknown or duplicate keys, a bad or duplicate id, an unknown service or aircraft type, a missing name or
        /// sector, a bad date or an empty window, an anchor off the globe or without a source, a source without url or
        /// check date, a drawn unit with no anchors, or an air unit without aircraft.</summary>
        public static List<UnitEntry> Parse(string yaml, string file = DefaultFileName)
        {
            UnitFile doc;
            try
            {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).WithDuplicateKeyChecking()
                    .Build().Deserialize<UnitFile>(yaml);
            }
            catch (YamlException e) { throw new InvalidDataException($"{file}: {e.Message}", e); }
            if (doc?.Units == null || doc.Units.Count == 0) throw new InvalidDataException($"{file}: no units");
            var ids = new HashSet<string>();
            foreach (var u in doc.Units)
            {
                if (u == null || string.IsNullOrWhiteSpace(u.Id)) throw new InvalidDataException($"{file}: a unit has an empty id");
                var at = $"{file}: '{u.Id}'";
                if (!IdRule.IsMatch(u.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!ids.Add(u.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (!Services.Contains(u.Service)) throw new InvalidDataException($"{at} has unknown service '{u.Service}' (army, air or navy)");
                if (string.IsNullOrWhiteSpace(u.Name) || string.IsNullOrWhiteSpace(u.Sector)) throw new InvalidDataException($"{at} needs a name and a sector");
                var signOff = Statecraft.Sim.ReviewRecord.Problem(u.ReviewedByGiven, u.ReviewedBy, u.ReviewedOnGiven, u.ReviewedOn);
                if (signOff != null) throw new InvalidDataException($"{at} {signOff.Value.field}: {signOff.Value.message}");
                if (u.Disbanded != null)
                {
                    try { Day(u.Disbanded.Date); }
                    catch (FormatException e) { throw new InvalidDataException($"{at} disbanded: {e.Message}", e); }
                    if (u.Disbanded.Sources == null || u.Disbanded.Sources.Count == 0) throw new InvalidDataException($"{at} disbanded has no source");
                }
                u.Anchors ??= new List<UnitAnchor>();
                if (u.Hidden == null && u.Anchors.Count == 0) throw new InvalidDataException($"{at} has no anchors (give one, or say why it is hidden)");
                foreach (var a in u.Anchors)
                {
                    if (a == null || string.IsNullOrWhiteSpace(a.Place)) throw new InvalidDataException($"{at} an anchor has no place");
                    var where = $"{at} anchor '{a.Place}'";
                    Window(where, a.From, a.To, toRequired: true);
                    if (!(a.Lon >= -180 && a.Lon <= 180 && a.Lat >= -90 && a.Lat <= 90)) throw new InvalidDataException($"{where} has bad coordinates");
                    if (a.Precision != null && a.Precision != "month") throw new InvalidDataException($"{where} precision must be 'month' or left out");
                    if (a.Toward != null && (string.IsNullOrWhiteSpace(a.Toward.Note) || a.Toward.Sources == null || a.Toward.Sources.Count == 0))
                        throw new InvalidDataException($"{where} toward needs a note and a source");
                    if (a.PlaceHex != null && (a.PlaceHex.Count != 2 || a.Toward == null))
                        throw new InvalidDataException($"{where} place_hex is [q, r] and needs a toward saying which side and why");
                    if (a.EndBasis == EndBySource)
                    {
                        if (string.IsNullOrWhiteSpace(a.EndNote)) throw new InvalidDataException($"{where} ends by source but has no end_note saying which");
                        if (a.Front != null) throw new InvalidDataException($"{where} ends by source, so it takes no front");
                    }
                    else if (a.EndBasis == EndByFrontArmistice)
                    {
                        if (a.Front == null || !FrontArmistice.TryGetValue(a.Front, out var day)) throw new InvalidDataException($"{where} needs a front (egypt, lebanon, jordan or syria)");
                        if (Day(a.To) != day) throw new InvalidDataException($"{where} must end on the {a.Front} armistice, {day:yyyy-MM-dd}");
                    }
                    else throw new InvalidDataException($"{where} needs end_basis: source or front_armistice");
                    if (u.Disbanded != null && Day(a.To) > Day(u.Disbanded.Date)) throw new InvalidDataException($"{where} runs past the unit's disbandment");
                    a.Sources ??= new List<UnitSource>();
                    if (a.Sources.Count == 0) throw new InvalidDataException($"{where} has no source");
                }
                if (u.Service == Air)
                {
                    if (u.Aircraft == null || u.Aircraft.Count == 0) throw new InvalidDataException($"{at} is an air unit without aircraft");
                    foreach (var t in u.Aircraft)
                    {
                        if (t == null || !AircraftTypes.Contains(t.Type)) throw new InvalidDataException($"{at} has an unknown aircraft type '{t?.Type}'");
                        Window($"{at} aircraft '{t.Type}'", t.From, t.To, toRequired: false);
                        t.Sources ??= new List<UnitSource>();
                        if (t.Sources.Count == 0) throw new InvalidDataException($"{at} aircraft '{t.Type}' has no source");
                    }
                }
                else if (u.Aircraft != null) throw new InvalidDataException($"{at} lists aircraft but is not an air unit");
                if (u.Headgear != null)
                {
                    if (u.Service != Army || u.Headgear.Count != 3 || u.Headgear.Exists(h => !((IList<string>)Headgears).Contains(h)))
                        throw new InvalidDataException($"{at} headgear must be three of {string.Join(", ", Headgears)}, on an army unit");
                    if (u.HeadgearSources == null || u.HeadgearSources.Count == 0) throw new InvalidDataException($"{at} headgear has no source");
                }
                foreach (var s in u.AllSources())
                {
                    if (s == null || string.IsNullOrWhiteSpace(s.Url) || !s.Url.StartsWith("https://", StringComparison.Ordinal))
                        throw new InvalidDataException($"{at} has a source without an https url");
                    try { Day(s.Checked); }
                    catch (FormatException e) { throw new InvalidDataException($"{at} source {s.Url}: checked {e.Message}", e); }
                }
            }
            return doc.Units;
        }

        static void Window(string where, string? from, string? to, bool toRequired)
        {
            if (toRequired && string.IsNullOrEmpty(to)) throw new InvalidDataException($"{where}: to is empty (give the window's end)");
            try
            {
                var f = Day(from);
                if (to != null && Day(to) <= f) throw new InvalidDataException($"{where}: to must be after from");
            }
            catch (FormatException e) { throw new InvalidDataException($"{where} from/to: {e.Message}", e); }
        }

        /// <summary>True when every source of the row is a Wikipedia article or a Wikidata item (the file's wikipedia_only).</summary>
        public static bool IsWikipediaOnly(UnitEntry u) => IsWikipediaOnly(u.AllSources());

        /// <summary>True when every one of <paramref name="sources"/> is a Wikipedia article or a Wikidata item (the card's
        /// draft note, per anchor).</summary>
        public static bool IsWikipediaOnly(IEnumerable<UnitSource> sources)
        {
            bool any = false;
            foreach (var s in sources)
            {
                any = true;
                var host = new Uri(s.Url).Host;
                if (!(host.EndsWith(".wikipedia.org", StringComparison.Ordinal) || host == "www.wikidata.org")) return false;
            }
            return any;
        }
    }
}
