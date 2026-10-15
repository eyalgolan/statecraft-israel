#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>The landmark a locality's kit gets at its heart (content/map/localities_1948.yaml, from Village Statistics'
    /// religion columns): a mosque, a church, none (the Druze villages), a water tower (the Jewish places), or both a mosque
    /// and a water tower (a mixed locality: Nesher).</summary>
    public enum Landmark { Mosque, Church, None, WaterTower, Mixed }

    /// <summary>The localities of 15 May 1948 beyond the 25 game settlements (owner rulings 2026-10-11: places of 1,000 or more
    /// people first, shown by their 1948 names with today's name in the label's tooltip) and the Bedouin encampment areas
    /// (one per sub-district whose Village Statistics rows name tribes or nomads, with their total). Read from
    /// content/map/localities_1948.yaml, which the player reads as its baked runtime form (localities_1948.tsv, MapInputs.LocalitiesTsv). Presentation only: the sim never
    /// reads the file, so it is outside the content hash (SaveGameTests.TheLocalitiesFileIsOutsideTheContentHash).</summary>
    public static class Localities
    {
        public const string FileName = "localities_1948.yaml";
        /// <summary>The runtime form the player reads (tools/localities_bake.py bakes it from <see cref="FileName"/>, the sourced
        /// source of truth; pytest fails a stale bake). Tab-separated, one row per line: the boot budget (docs/visual-v1.md).</summary>
        public const string RuntimeFileName = "localities_1948.tsv";

        public sealed class Place
        {
            /// <summary>The kit's and label's settlement record: id, the 1948 name, the repo type, coordinates.</summary>
            public Settlement Settlement = null!;
            /// <summary>The 1948 Hebrew name (#558: the form matching the English name, sourced in the YAML); null where the file
            /// gives none (thirteen villages), when the map keeps the English. Data only until the UI wiring reads it.</summary>
            public string? NameHe;
            /// <summary>The living place's name today (and in Hebrew), only where one carries on (owner ruling 2026-10-12);
            /// null for a place that did not: then no tooltip.</summary>
            public string? Today, TodayHe;
            public int Population;
            public Landmark Landmark;
            /// <summary>A mixed locality (Nesher: Arab and Jewish in one VS row), drawn with both archetypes side by side.</summary>
            public bool Mixed;
            /// <summary>How many sources the row cites (their URLs are in the YAML).</summary>
            public int Sources;
        }

        /// <summary>The one "living place carries on" rule (owner ruling 2026-10-12), shared by the label tooltip and the info
        /// panel: the name to show as today's name, in Hebrew when asked and one is recorded; null when no living place carries
        /// on (<see cref="Place.Today"/> is null) or when today's name is the 1948 name itself (Nablus is Nablus). It shows
        /// only what the file records.</summary>
        public static string? TodayName(Place p, bool hebrew)
        {
            if (p.Today == null || SameName(p.Today, p.Settlement.Name)) return null;
            return hebrew && !string.IsNullOrEmpty(p.TodayHe) ? p.TodayHe : p.Today;
        }

        static bool SameName(string a, string b)
        {
            static string Norm(string x) { var sb = new System.Text.StringBuilder(); foreach (var c in x) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c)); return sb.ToString(); }
            return Norm(a) == Norm(b);
        }

        /// <summary>The places by the hex they lie in (<paramref name="hexOf"/> maps a place to its hex), in the places' order.</summary>
        public static Dictionary<(int q, int r), List<Place>> ByHex(IEnumerable<Place> places, System.Func<Place, (int q, int r)> hexOf)
        {
            var by = new Dictionary<(int q, int r), List<Place>>();
            foreach (var p in places)
            {
                var h = hexOf(p);
                if (!by.TryGetValue(h, out var list)) by[h] = list = new List<Place>();
                list.Add(p);
            }
            return by;
        }

        public sealed class Area
        {
            public string Subdistrict = "";
            public int Population, Sources;
            /// <summary>The sub-district's land hexes as content/map/subdistrict_hexes_1948.yaml draws them (FillHexes, at
            /// load: the localities file keeps no copy of them).</summary>
            public List<(int q, int r)> Hexes = new List<(int, int)>();
        }

        /// <summary>Fills every area's hexes from the sub-district layer: the land, non-lake hexes the layer puts in that
        /// sub-district. An area whose sub-district the layer does not have is an error naming <paramref name="file"/>.</summary>
        public static void FillHexes(Doc doc, Subdistricts1948 subdistricts, HexFile hexes, string file = FileName)
        {
            foreach (var a in doc.Encampments)
            {
                a.Hexes.Clear();
                bool known = false;
                foreach (var region in subdistricts.Regions) known |= region.Key == a.Subdistrict;
                if (!known) throw new InvalidDataException($"{file}: encampments '{a.Subdistrict}' is not a sub-district of {Subdistricts1948.FileName}");
                foreach (var h in hexes.hexes)
                    if (h.land && h.biome != "lake" && subdistricts.SubdistrictAt(h.q, h.r)?.Key == a.Subdistrict) a.Hexes.Add((h.q, h.r));
            }
        }

        /// <summary>As FillHexes, from the sub-district file's text, read with the event reader below: a test aid since issue
        /// #321 (the map reads the sub-districts' runtime form and calls the overload above). KitTests checks the two give the
        /// same hexes.</summary>
        internal static void FillHexes(Doc doc, string regionsYaml, HexFile hexes, string file = FileName)
        {
            const string regionsFile = Subdistricts1948.FileName;
            YMap root;
            try
            {
                var parser = new Parser(new StringReader(regionsYaml));
                parser.Consume<StreamStart>(); parser.Consume<DocumentStart>();
                root = Node(parser) as YMap ?? throw new InvalidDataException($"{regionsFile}: not a mapping");
            }
            catch (YamlException e) { throw new InvalidDataException($"{regionsFile}:{e.Start.Line}: {e.Message}", e); }
            var keyOf = new Dictionary<int, string?>();   // region id -> key, null for style none (outside)
            var keys = new HashSet<string>();
            int Int(object? o, string what) => o is string s && int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)
                ? v : throw new InvalidDataException($"{regionsFile}: {what} is not an integer");
            foreach (var o in List(root, "regions", regionsFile))
            {
                var m = Map(o, $"{regionsFile}: a region");
                string key = Str(m, "key"); keys.Add(key);
                keyOf[Int(m.TryGetValue("id", out var id) ? id : null, "a region id")] = Str(m, "style") == "none" ? null : key;
            }
            var at = new Dictionary<(int, int), string>();
            foreach (var o in List(root, "hexes", regionsFile))
            {
                if (!(o is List<object?> h) || h.Count != 3) throw new InvalidDataException($"{regionsFile}: a hex row is not [q, r, region]");
                int region = Int(h[2], "a hex's region");
                if (!keyOf.TryGetValue(region, out var key)) throw new InvalidDataException($"{regionsFile}: a hex is in region {region}, which is not listed");
                if (key != null) at[(Int(h[0], "a hex's q"), Int(h[1], "a hex's r"))] = key;
            }
            foreach (var a in doc.Encampments)
            {
                a.Hexes.Clear();
                if (!keys.Contains(a.Subdistrict)) throw new InvalidDataException($"{file}: encampments '{a.Subdistrict}' is not a sub-district of {regionsFile}");
                foreach (var h in hexes.hexes)
                    if (h.land && h.biome != "lake" && at.TryGetValue((h.q, h.r), out var k) && k == a.Subdistrict) a.Hexes.Add((h.q, h.r));
            }
        }

        /// <summary>The camps an area draws: one per <see cref="PeoplePerCamp"/> people, rounded half up, with no floor
        /// (Nablus's 220 draw none: review M4).</summary>
        public static int CampsOf(Area a) => (int)System.Math.Floor(a.Population / PeoplePerCamp + 0.5);
        public const double PeoplePerCamp = 1000.0;

        public sealed class Doc { public List<Place> Places = new List<Place>(); public List<Area> Encampments = new List<Area>(); }

        sealed class SourceRow
        {
            public string Url { get; set; } = ""; public string Checked { get; set; } = "";
            public string For { get; set; } = ""; public string Licence { get; set; } = "";
        }

        sealed class TodayRow
        {
            public string Name { get; set; } = ""; public string? NameHe { get; set; } public string Source { get; set; } = "";
        }

        sealed class PlaceRow
        {
            public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string NameBasis { get; set; } = "";
            public TodayRow? Today { get; set; } public string? NameHe { get; set; } public string? NameAr { get; set; }
            public List<SourceRow> NameHeSources { get; set; } = new List<SourceRow>(); public string NameHeReview { get; set; } = ""; public bool NameHeReviewGiven;
            public double Lon { get; set; } = double.NaN; public double Lat { get; set; } = double.NaN;
            public string Kind { get; set; } = ""; public string Type { get; set; } = ""; public string Landmark { get; set; } = "";
            public int Population { get; set; } = -1; public List<string> ListedAs { get; set; } = new List<string>();
            public string Match { get; set; } = ""; public bool OwnerVerify { get; set; } public string Wikidata { get; set; } = "";
            public string? DraftId { get; set; } public string? Note { get; set; }
            public string Checked { get; set; } = ""; public string Status { get; set; } = "";
            public bool ReviewedByGiven, ReviewedOnGiven; public string ReviewedBy = "", ReviewedOn = "";
            public List<SourceRow> Sources { get; set; } = new List<SourceRow>();
            public int Line, SourceCount;
        }

        sealed class AreaRow
        {
            public string Subdistrict { get; set; } = ""; public int Population { get; set; } = -1;
            public List<string> ListedAs { get; set; } = new List<string>(); public string? Note { get; set; }
            public string Checked { get; set; } = ""; public string Status { get; set; } = "";
            public bool ReviewedByGiven, ReviewedOnGiven; public string ReviewedBy = "", ReviewedOn = "";
            public List<SourceRow> Sources { get; set; } = new List<SourceRow>();
            public int Line, SourceCount;
        }

        sealed class FileRow
        {
            public string Status { get; set; } = "";
            public List<PlaceRow> Places { get; set; } = new List<PlaceRow>();
            public List<AreaRow> Encampments { get; set; } = new List<AreaRow>();
        }

        static readonly Regex IdRule = new Regex(@"^[a-z][a-z0-9_]*\z");
        static readonly HashSet<string> Types = new HashSet<string> { "town", "kibbutz", "moshav", "arab_town", "village" };
        static readonly HashSet<string> Statuses = new HashSet<string> { "draft", "owner-verify", "fact-checked", "verified" };
        static readonly Dictionary<string, Landmark> Landmarks = new Dictionary<string, Landmark>
        {
            ["mosque"] = Landmark.Mosque, ["church"] = Landmark.Church, ["none"] = Landmark.None, ["water_tower"] = Landmark.WaterTower,
            ["mosque_and_water_tower"] = Landmark.Mixed,
        };

        /// <summary>Parses the file. Every malformed input throws InvalidDataException naming <paramref name="file"/> (and the
        /// id or sub-district): YAML errors, unknown keys, an empty, non-snake_case or duplicate id (or one of
        /// <paramref name="reserved"/>, the game settlements' ids), an empty name or today's name, an unknown type or
        /// landmark, a missing or out-of-bounds coordinate, a population under 1, a status outside draft / owner-verify /
        /// fact-checked / verified, no checked date, no sources or a source without url and checked; a today without a
        /// name or source; an encampment row without a sub-district or population.</summary>
        public static Doc Parse(string yaml, (double lon0, double lon1, double lat0, double lat1) bounds,
            ICollection<string> reserved, string file = FileName)
        {
            FileRow f;
            try { f = Read(yaml, file); }
            catch (YamlException e) { throw new InvalidDataException($"{file}:{e.Start.Line}: {e.Message}", e); }
            return Validate(f, bounds, reserved, file, runtime: false);
        }

        /// <summary>Parses the runtime form (<see cref="RuntimeFileName"/>) with the same checks as <see cref="Parse"/>, but
        /// for the source URLs, which only the YAML holds: a row must cite at least one. Every malformed line throws
        /// InvalidDataException naming <paramref name="file"/> and the line.</summary>
        public static Doc ParseRuntime(string tsv, (double lon0, double lon1, double lat0, double lat1) bounds,
            ICollection<string> reserved, string file = RuntimeFileName) =>
            Validate(ReadRuntime(tsv, file), bounds, reserved, file, runtime: true);

        static FileRow ReadRuntime(string tsv, string file)
        {
            var f = new FileRow();
            var lines = tsv.Split('\n');
            int Int(string text, string what, int line) =>
                int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)
                    ? v : throw new InvalidDataException($"{file}:{line}: {what} '{text}' is not an integer");
            double Num(string text, string what, int line) =>
                double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
                    ? v : throw new InvalidDataException($"{file}:{line}: {what} '{text}' is not a number");
            for (int i = 0; i < lines.Length; i++)
            {
                var text = lines[i].TrimEnd('\r');
                if (text.Length == 0 || text[0] == '#') continue;
                int line = i + 1;
                var c = text.Split('\t');
                if (c[0] == "place")
                {
                    if (c.Length != 15) throw new InvalidDataException($"{file}:{line}: a place row has {c.Length} fields, not 15");
                    f.Places.Add(new PlaceRow
                    {
                        Id = c[1], Name = c[2], NameHe = c[3].Length == 0 ? null : c[3],
                        Today = c[4].Length == 0 ? null : new TodayRow { Name = c[4], NameHe = c[5] },
                        Lon = Num(c[6], "lon", line), Lat = Num(c[7], "lat", line), Type = c[8], Landmark = c[9],
                        Kind = c[10] == "1" ? "mixed_locality" : c[10] == "0" ? "" : throw new InvalidDataException($"{file}:{line}: mixed '{c[10]}' is not 0 or 1"),
                        Population = Int(c[11], "population", line), Status = c[12], Checked = c[13],
                        SourceCount = Int(c[14], "sources", line), Line = line,
                    });
                }
                else if (c[0] == "camp")
                {
                    if (c.Length != 6) throw new InvalidDataException($"{file}:{line}: a camp row has {c.Length} fields, not 6");
                    f.Encampments.Add(new AreaRow
                    {
                        Subdistrict = c[1], Population = Int(c[2], "population", line), Status = c[3], Checked = c[4],
                        SourceCount = Int(c[5], "sources", line), Line = line,
                    });
                }
                else throw new InvalidDataException($"{file}:{line}: a row starts '{c[0]}', not place or camp");
            }
            return f;
        }

        static Doc Validate(FileRow f, (double lon0, double lon1, double lat0, double lat1) bounds, ICollection<string> reserved,
            string file, bool runtime)
        {
            if (f?.Places == null || f.Places.Count == 0) throw new InvalidDataException($"{file}: no places");
            var doc = new Doc(); var ids = new HashSet<string>();
            foreach (var p in f.Places)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Id)) throw new InvalidDataException($"{file}:{p?.Line}: a place has an empty id");
                string at = $"{file}:{p.Line}: '{p.Id}'";
                if (!IdRule.IsMatch(p.Id)) throw new InvalidDataException($"{at} id must be lower snake_case");
                if (!ids.Add(p.Id)) throw new InvalidDataException($"{at} duplicate id");
                if (reserved.Contains(p.Id)) throw new InvalidDataException($"{at} is a game settlement's id (settlements_1948.yaml keeps those)");
                if (string.IsNullOrWhiteSpace(p.Name)) throw new InvalidDataException($"{at} needs a name");
                if (p.Today != null && (string.IsNullOrWhiteSpace(p.Today.Name) || (!runtime && string.IsNullOrWhiteSpace(p.Today.Source))))
                    throw new InvalidDataException($"{at} today needs a name and a source");
                if (!Types.Contains(p.Type ?? "")) throw new InvalidDataException($"{at} has unknown type '{p.Type}'");
                if (!Landmarks.TryGetValue(p.Landmark ?? "", out var landmark)) throw new InvalidDataException($"{at} has unknown landmark '{p.Landmark}'");
                if (double.IsNaN(p.Lon) || double.IsNaN(p.Lat) || p.Lon < bounds.lon0 || p.Lon > bounds.lon1 || p.Lat < bounds.lat0 || p.Lat > bounds.lat1)
                    throw new InvalidDataException($"{at} lon/lat {p.Lon}, {p.Lat} missing or outside the map");
                if (p.Population < 1) throw new InvalidDataException($"{at} population {p.Population} is not a count of people");
                CheckSourced(at, p.Status, p.Checked, p.Sources, runtime ? p.SourceCount : p.Sources.Count);
                var signOffp = Statecraft.Sim.ReviewRecord.Problem(p.ReviewedByGiven, p.ReviewedBy, p.ReviewedOnGiven, p.ReviewedOn);   // #375; the YAML only (the runtime form does not carry it)
                if (signOffp != null) throw new InvalidDataException($"{at} {signOffp.Value.field}: {signOffp.Value.message}");
                if (p.NameHeSources.Count > 0 && string.IsNullOrWhiteSpace(p.NameHe)) throw new InvalidDataException($"{at} name_he_sources without a name_he");
                // Absent (a person has reviewed the name) or exactly agent-checked: blank, null or a list is neither.
                if (p.NameHeReviewGiven && p.NameHeReview != "agent-checked")
                    throw new InvalidDataException($"{at} name_he_review '{p.NameHeReview}' is not agent-checked (delete the line once a person has reviewed the name)");
                if (p.NameHeReviewGiven && string.IsNullOrWhiteSpace(p.NameHe)) throw new InvalidDataException($"{at} name_he_review without a name_he");
                for (int i = 0; i < p.NameHeSources.Count; i++)
                    if (!p.NameHeSources[i].Url.StartsWith("https://") || string.IsNullOrWhiteSpace(p.NameHeSources[i].Checked))
                        throw new InvalidDataException($"{at} name_he_sources[{i}] needs an https url and a checked date");
                doc.Places.Add(new Place
                {
                    Settlement = new Settlement { Id = p.Id, Name = p.Name, Type = p.Type, Lon = p.Lon, Lat = p.Lat, PreModern = true },
                    NameHe = string.IsNullOrEmpty(p.NameHe) ? null : p.NameHe,
                    Today = p.Today?.Name, TodayHe = p.Today?.NameHe, Population = p.Population, Landmark = landmark,
                    Mixed = p.Kind == "mixed_locality", Sources = runtime ? p.SourceCount : p.Sources.Count,
                });
            }
            var subs = new HashSet<string>();
            foreach (var a in f.Encampments ?? new List<AreaRow>())
            {
                if (a == null || string.IsNullOrWhiteSpace(a.Subdistrict)) throw new InvalidDataException($"{file}:{a?.Line}: an encampment row has no subdistrict");
                string at = $"{file}:{a.Line}: encampments '{a.Subdistrict}'";
                if (!subs.Add(a.Subdistrict)) throw new InvalidDataException($"{at} duplicate subdistrict");
                if (a.Population < 1) throw new InvalidDataException($"{at} population {a.Population} is not a count of people");
                CheckSourced(at, a.Status, a.Checked, a.Sources, runtime ? a.SourceCount : a.Sources.Count);
                var signOffa = Statecraft.Sim.ReviewRecord.Problem(a.ReviewedByGiven, a.ReviewedBy, a.ReviewedOnGiven, a.ReviewedOn);   // #375; the YAML only (the runtime form does not carry it)
                if (signOffa != null) throw new InvalidDataException($"{at} {signOffa.Value.field}: {signOffa.Value.message}");
                doc.Encampments.Add(new Area { Subdistrict = a.Subdistrict, Population = a.Population, Sources = runtime ? a.SourceCount : a.Sources.Count });
            }
            return doc;
        }

        // --- A reader over YamlDotNet's parser events (review I6, boot budget): the reflection deserializer took about 85% of
        // the localities' load (some 80 ms of 100 at cpu 1 in the WebGL player, four times that at cpu 4). Same strictness:
        // unknown and duplicate keys are errors naming the key. ---

        static readonly HashSet<string> FileKeys = new HashSet<string> { "status", "places", "encampments" };
        static readonly HashSet<string> PlaceKeys = new HashSet<string>
        {
            "id", "name", "name_basis", "today", "name_he", "name_ar", "lon", "lat", "kind", "type", "landmark", "population",
            "listed_as", "match", "owner_verify", "wikidata", "draft_id", "note", "checked", "status", "sources", "name_he_sources", "name_he_review", "reviewed_by", "reviewed_on",
        };
        static readonly HashSet<string> NameSourceKeys = new HashSet<string> { "url", "checked" };
        static readonly HashSet<string> AreaKeys = new HashSet<string> { "subdistrict", "population", "listed_as", "note", "checked", "status", "sources", "reviewed_by", "reviewed_on" };
        static readonly HashSet<string> TodayKeys = new HashSet<string> { "name", "name_he", "source" };

        static FileRow Read(string yaml, string file)
        {
            var parser = new Parser(new StringReader(yaml));
            parser.Consume<StreamStart>();
            if (parser.Accept<StreamEnd>(out _)) return new FileRow();
            parser.Consume<DocumentStart>();
            var root = Node(parser) as YMap ?? throw new InvalidDataException($"{file}: not a mapping");
            Keys(root, FileKeys, file);
            var f = new FileRow { Status = Str(root, "status") };
            foreach (var o in List(root, "places", file))
            {
                var m = Map(o, $"{file}: a place");
                string at = $"{file}:{m.Line}: '{Str(m, "id")}'";
                Keys(m, PlaceKeys, at);
                var t = m.TryGetValue("today", out var tv) && tv != null ? Map(tv, at + " today") : null;
                if (t != null) Keys(t, TodayKeys, at + " today");
                f.Places.Add(new PlaceRow
                {
                    Id = Str(m, "id"), Name = Str(m, "name"), NameHe = Str(m, "name_he"), NameHeSources = NameSources(m, at), NameHeReview = Str(m, "name_he_review"), NameHeReviewGiven = m.ContainsKey("name_he_review"), Kind = Str(m, "kind"), Type = Str(m, "type"), Landmark = Str(m, "landmark"),
                    Lon = Num(m, "lon", at), Lat = Num(m, "lat", at), Population = (int)Num(m, "population", at, -1),
                    Today = t == null ? null : new TodayRow { Name = Str(t, "name"), NameHe = Str(t, "name_he"), Source = Str(t, "source") },
                    Checked = Str(m, "checked"), Status = Str(m, "status"), Sources = Sources(m, at), Line = m.Line,
                    ReviewedByGiven = m.ContainsKey("reviewed_by"), ReviewedBy = Str(m, "reviewed_by"), ReviewedOnGiven = m.ContainsKey("reviewed_on"), ReviewedOn = Str(m, "reviewed_on"),
                });
            }
            foreach (var o in List(root, "encampments", file))
            {
                var m = Map(o, $"{file}: an encampment row");
                string at = $"{file}:{m.Line}: encampments '{Str(m, "subdistrict")}'";
                Keys(m, AreaKeys, at);
                f.Encampments.Add(new AreaRow
                {
                    Subdistrict = Str(m, "subdistrict"), Population = (int)Num(m, "population", at, -1),
                    Checked = Str(m, "checked"), Status = Str(m, "status"), Sources = Sources(m, at), Line = m.Line,
                    ReviewedByGiven = m.ContainsKey("reviewed_by"), ReviewedBy = Str(m, "reviewed_by"), ReviewedOnGiven = m.ContainsKey("reviewed_on"), ReviewedOn = Str(m, "reviewed_on"),
                });
            }
            return f;
        }

        /// <summary>A mapping node, with the line it starts on and the line of each key (for the error messages).</summary>
        sealed class YMap : Dictionary<string, object?>
        {
            public int Line; public readonly Dictionary<string, int> KeyLine = new Dictionary<string, int>();
        }

        /// <summary>A node as a tree: a scalar's string (null for "null" or "~" or empty), a list, or a <see cref="YMap"/>.</summary>
        static object? Node(IParser p)
        {
            if (p.TryConsume<Scalar>(out var sc))
                return sc.Style == ScalarStyle.Plain && (sc.Value == "" || sc.Value == "null" || sc.Value == "~") ? null : sc.Value;
            if (p.TryConsume<SequenceStart>(out _))
            {
                var list = new List<object?>();
                while (!p.TryConsume<SequenceEnd>(out _)) list.Add(Node(p));
                return list;
            }
            if (p.TryConsume<MappingStart>(out var ms))
            {
                var map = new YMap { Line = (int)ms.Start.Line };
                while (!p.TryConsume<MappingEnd>(out _))
                {
                    var key = p.Consume<Scalar>();
                    if (map.ContainsKey(key.Value)) throw new YamlException(key.Start, key.End, $"duplicate key '{key.Value}'");
                    map.KeyLine[key.Value] = (int)key.Start.Line;
                    map[key.Value] = Node(p);
                }
                return map;
            }
            var ev = p.Current;
            if (ev != null) throw new YamlException(ev.Start, ev.End, "anchors and aliases are not supported here");
            throw new YamlException("unexpected end of the file");
        }

        static void Keys(YMap m, HashSet<string> allowed, string at)
        {
            foreach (var k in m.Keys) if (!allowed.Contains(k)) throw new InvalidDataException($"{at} has unknown key '{k}' (line {m.KeyLine[k]})");
        }

        static YMap Map(object? o, string at) => o as YMap ?? throw new InvalidDataException($"{at} is not a mapping");

        static List<object?> List(YMap m, string key, string at) =>
            m.TryGetValue(key, out var v) && v != null
                ? v as List<object?> ?? throw new InvalidDataException($"{at} '{key}' is not a list (line {m.KeyLine[key]})") : new List<object?>();

        static string Str(YMap m, string key) => m.TryGetValue(key, out var v) && v is string s ? s : "";

        static double Num(YMap m, string key, string at, double missing = double.NaN)
        {
            if (!m.TryGetValue(key, out var v) || v == null) return missing;
            if (v is string s && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
            throw new InvalidDataException($"{at} {key} is not a number (line {m.KeyLine[key]})");
        }

        /// <summary>A row's sources: a list of URLs, all read on the row's checked date (the file's header says what each
        /// host is the source of).</summary>
        static List<SourceRow> Sources(YMap m, string at)
        {
            var list = new List<SourceRow>();
            foreach (var o in List(m, "sources", at))
                list.Add(new SourceRow { Url = o as string ?? throw new InvalidDataException($"{at} a source is not a URL"), Checked = Str(m, "checked") });
            return list;
        }

        /// <summary>A place's name_he_sources (#558): a list of { url, checked } mappings, each read on its own date.</summary>
        static List<SourceRow> NameSources(YMap m, string at)
        {
            var list = new List<SourceRow>();
            foreach (var o in List(m, "name_he_sources", at))
            {
                var s = Map(o, at + " a name_he source");
                Keys(s, NameSourceKeys, at + " name_he_sources");
                list.Add(new SourceRow { Url = Str(s, "url"), Checked = Str(s, "checked") });
            }
            return list;
        }

        static void CheckSourced(string at, string status, string checkedOn, List<SourceRow> sources, int count)
        {
            if (!Statuses.Contains(status ?? "")) throw new InvalidDataException($"{at} status '{status}' is not draft, owner-verify, fact-checked or verified");
            if (string.IsNullOrWhiteSpace(checkedOn)) throw new InvalidDataException($"{at} has no checked date");
            if (count < 1) throw new InvalidDataException($"{at} sources is empty");
            for (int i = 0; i < sources.Count; i++)
                if (string.IsNullOrWhiteSpace(sources[i]?.Url) || !sources[i].Url.StartsWith("https://"))
                    throw new InvalidDataException($"{at} sources[{i}] is not an https URL");
        }

        static Dictionary<string, Landmark> landmarks = new Dictionary<string, Landmark>();
        static HashSet<string> mixed = new HashSet<string>();

        /// <summary>The landmarks and mixed localities the next kit build reads (MapScene.Build sets them with the places on
        /// every build, empty without the kits, so a later map never inherits an earlier one's: review M13).</summary>
        public static void Use(IEnumerable<Place> places)
        {
            landmarks = new Dictionary<string, Landmark>(); mixed = new HashSet<string>();
            foreach (var p in places) { landmarks[p.Settlement.Id] = p.Landmark; if (p.Mixed) mixed.Add(p.Settlement.Id); }
        }

        /// <summary>Whether a locality is a mixed one (both archetypes side by side).</summary>
        public static bool IsMixed(string id) => mixed.Contains(id);

        /// <summary>A locality's landmark, or null for a game settlement (its archetype's own landmark).</summary>
        public static Landmark? LandmarkOf(string id) => landmarks.TryGetValue(id, out var l) ? l : (Landmark?)null;
    }
}
