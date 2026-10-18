using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    public static class ContentLoader
    {
        sealed class OfficesFile { public List<Office> Offices { get; set; } = new List<Office>(); }
        sealed class PartiesFile { public List<Party> Parties { get; set; } = new List<Party>(); }
        sealed class PeopleFile { public List<Person> People { get; set; } = new List<Person>(); }
        sealed class LegislaturesFile { public List<Legislature> Legislatures { get; set; } = new List<Legislature>(); }
        sealed class ErasFile { public List<Era> Eras { get; set; } = new List<Era>(); }
        sealed class GovernmentsFile { public List<Government> Governments { get; set; } = new List<Government>(); }
        sealed class ModelFile
        {
            public List<Indicator> Indicators { get; set; } = new List<Indicator>();
            public List<Policy> Policies { get; set; } = new List<Policy>();
            public List<Effect> Effects { get; set; } = new List<Effect>();
            public List<Situation> Situations { get; set; } = new List<Situation>();
            public List<VoterGroup> VoterGroups { get; set; } = new List<VoterGroup>();
            public List<Actor> Actors { get; set; } = new List<Actor>();
            public List<MoneyAccount> Money { get; set; } = new List<MoneyAccount>();
        }
        sealed class AliyahFile
        {
            public List<Region> Regions { get; set; } = new List<Region>();
            public List<AliyahPeriod> Series { get; set; } = new List<AliyahPeriod>();
        }
        sealed class PopulationFile { public List<PopulationAnchor> Anchors { get; set; } = new List<PopulationAnchor>(); }
        sealed class HexBounds { public double Lon0 { get; set; } public double Lon1 { get; set; } public double Lat0 { get; set; } public double Lat1 { get; set; } }
        /// <summary>hexes.json is JSON, which YAML 1.2 reads as a flow mapping; every key the tool writes is declared.</summary>
        sealed class MapFile
        {
            public List<string> Attribution { get; set; } = new List<string>();
            public HexBounds Bounds { get; set; } = new HexBounds();
            public double HexSizeKm { get; set; }
            public string Layout { get; set; } = "";
            public List<MapHex> Hexes { get; set; } = new List<MapHex>();
        }
        sealed class SettlementHexesFile { public List<SettlementHex> Settlements { get; set; } = new List<SettlementHex>(); }
        sealed class CitiesFile { public List<CityStart> Cities { get; set; } = new List<CityStart>(); }
        sealed class UnitTypesFile { public List<UnitType> Types { get; set; } = new List<UnitType>(); }
        sealed class SitesFile { public List<Site> Sites { get; set; } = new List<Site>(); }
        sealed class ProjectsFile { public List<ProjectKind> Projects { get; set; } = new List<ProjectKind>(); }
        sealed class ObjectivesFile { public List<Objective> Objectives { get; set; } = new List<Objective>(); }

        public const string HexesFile = "map/hexes.json";
        public const string SettlementHexesFileName = "map/settlement_hexes.yaml";
        public const string SettlementsFileName = "map/settlements_1948.yaml";

        /// <summary>The ids and names of settlements_1948.yaml, which the tools own (its other keys are not the sim's business),
        /// so it is read as plain maps rather than through the strict content types.</summary>
        static List<(string id, string name)> ReadSettlements(Func<string, string?> readFile)
        {
            string? text;
            try { text = readFile(SettlementsFileName); }
            catch (IOException e) { throw new ContentException(SettlementsFileName, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(SettlementsFileName, "(file)", e.Message); }
            if (text == null) throw new ContentException(SettlementsFileName, "(file)", "missing file");
            try
            {
                var file = new DeserializerBuilder().Build().Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(text);
                if (file == null || !file.TryGetValue("settlements", out var rows)) throw new ContentException(SettlementsFileName, "settlements", "required");
                return rows.Select((r, i) =>
                {
                    var sid = r.TryGetValue("id", out var id) && id is string s0 && s0 != ""
                        ? s0 : throw new ContentException(SettlementsFileName, $"settlements[{i}].id", "required");
                    // The sim reads these rows untyped, so it checks their keys itself: a misspelt key (name_he_reveiw)
                    // would otherwise load as if absent. The same keys as Unity's SettlementPlacer.Settlement.
                    foreach (var key in r.Keys)
                        if (!SettlementKeys.Contains(key)) throw new ContentException(SettlementsFileName, $"settlements[{sid}].{key}", "unknown field");
                    var sname = r.TryGetValue("name", out var name) && name is string n0 && !string.IsNullOrWhiteSpace(n0)
                        ? n0 : throw new ContentException(SettlementsFileName, $"settlements[{i}].name", "required (the name shown to the player)");
                    HebrewName(SettlementsFileName, $"settlements[{sid}]", "name_he", r.TryGetValue("name_he", out var he) && he is string hs ? hs : "",
                        SettlementSources(r, $"settlements[{sid}].name_he_sources"),
                        r.TryGetValue("name_he_review", out var rv) && rv is string rs ? rs : "", r.ContainsKey("name_he_review"));
                    return (sid, sname);
                }).ToList();
            }
            catch (YamlException e) { throw new ContentException(SettlementsFileName, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
        }

        static readonly HashSet<string> SettlementKeys = new HashSet<string>
        {
            "id", "name", "name_he", "name_he_sources", "name_he_review", "lon", "lat", "type", "founded", "pre_modern", "sources",
        };

        /// <summary>A settlement row's name_he_sources, read from the untyped row: a list of { url, checked } maps.</summary>
        static List<Source> SettlementSources(Dictionary<string, object> row, string field)
        {
            var list = new List<Source>();
            if (!row.TryGetValue("name_he_sources", out var v) || v == null) return list;
            if (!(v is List<object> items)) throw new ContentException(SettlementsFileName, field, "must be a list");
            for (int i = 0; i < items.Count; i++)
            {
                var m = items[i] as Dictionary<object, object>;
                var url = m != null && m.TryGetValue("url", out var u) && u is string us ? us : "";
                var day = m != null && m.TryGetValue("checked", out var d) && d is string ds
                    && DateTime.TryParseExact(ds, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt) ? dt : default;
                list.Add(new Source { Url = url, Checked = day });
            }
            return list;
        }

        static readonly IDeserializer Yaml = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithDuplicateKeyChecking()   // two equal keys would otherwise silently keep only the last value
            .Build();

        static readonly IDeserializer NodeYaml = new DeserializerBuilder().WithDuplicateKeyChecking().Build();
        static readonly ISerializer NodeWriter = new SerializerBuilder().Build();

        /// <summary>Keys an era override may not set: the values <c>NewGame</c> reads once. A later era's start values
        /// belong in start/&lt;era&gt;.yaml (chapters spec section 6), and the set of budget lines in the carry rules (section 4).</summary>
        static readonly string[] StartOnlyKeys = { "start", "treasury_start", "fx_start", "start_rung", "initial_population", "budget_start", "budget_owners" };

        /// <summary>Era rules (#58): for each era with a content/rules/&lt;era&gt;.yaml file, rules.yaml with that file's keys
        /// merged in at the YAML-node level (maps merge key by key, a list or a value replaces), then read with the strict
        /// <see cref="Rules"/> type. The override is first read on its own with the same strict reader, so a misspelt key
        /// or a wrong type fails naming the era file and the line. An override cannot remove a key: an era cannot drop a
        /// budget_start line, a map_actions entry or an attention_cost kind. An override file is read through
        /// <paramref name="readFile"/>, so it counts in the content hash; <paramref name="baseText"/> is the text of
        /// rules.yaml already read (and hashed) once. An era with no file is left out of the map.</summary>
        static Dictionary<string, Rules> ReadEraRules(string baseText, Func<string, string?> readFile, List<Era> eras)
        {
            var result = new Dictionary<string, Rules>();
            foreach (var era in eras)
            {
                if (!Offices.OfficeId.IsMatch(era.Id ?? "")) continue;   // eras.yaml validation names a bad id
                var rel = "rules/" + era.Id + ".yaml";
                string? text;
                try { text = readFile(rel); }
                catch (IOException e) { throw new ContentException(rel, "(file)", e.Message); }
                catch (UnauthorizedAccessException e) { throw new ContentException(rel, "(file)", e.Message); }
                if (text == null) continue;
                Dictionary<object, object> over;
                try
                {
                    var alone = Yaml.Deserialize<Rules>(text) ?? throw new ContentException(rel, "(file)", "empty file: list the keys this era changes");
                    CheckNulls(rel, "", alone);
                    over = NodeYaml.Deserialize<Dictionary<object, object>>(text) ?? throw new ContentException(rel, "(file)", "empty file: list the keys this era changes");
                }
                catch (YamlException e) { throw new ContentException(rel, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
                foreach (var k in StartOnlyKeys)
                    if (over.ContainsKey(k))
                        throw new ContentException(rel, k, k == "start" ? "an era cannot change the start date"
                            : "start values go in start/<era>.yaml (chapters spec section 6), not in an override");
                var merged = NodeYaml.Deserialize<Dictionary<object, object>>(baseText) ?? new Dictionary<object, object>();
                MergeNodes(merged, over);
                try { result[era.Id!] = Yaml.Deserialize<Rules>(NodeWriter.Serialize(merged)) ?? throw new ContentException(rel, "(file)", "empty file"); }
                catch (YamlException e) { throw new ContentException(rel, "(merged with rules.yaml)", e.InnerException?.Message ?? e.Message); }
            }
            return result;
        }

        static void MergeNodes(Dictionary<object, object> into, Dictionary<object, object> over)
        {
            foreach (var kv in over)
                if (into.TryGetValue(kv.Key, out var cur) && cur is Dictionary<object, object> a && kv.Value is Dictionary<object, object> b) MergeNodes(a, b);
                else into[kv.Key] = kv.Value;
        }

        internal const string ObjectivesFileName = "objectives.yaml";

        /// <summary>Like <see cref="Read{T}"/>, but a missing file is null, not an error. The file is read once.</summary>
        static T? ReadOptional<T>(Func<string, string?> readFile, string relPath) where T : class
        {
            string? text;
            try { text = readFile(relPath); }
            catch (IOException e) { throw new ContentException(relPath, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(relPath, "(file)", e.Message); }
            return text == null ? null : Read<T>(_ => text, relPath);
        }

        static T Read<T>(Func<string, string?> readFile, string relPath)
        {
            string? text;
            try { text = readFile(relPath); }
            catch (IOException e) { throw new ContentException(relPath, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(relPath, "(file)", e.Message); }
            if (text == null) throw new ContentException(relPath, "(file)", "missing file");
            T result;
            try { result = Yaml.Deserialize<T>(text) ?? throw new ContentException(relPath, "(file)", "empty file"); }
            catch (YamlException e) { throw new ContentException(relPath, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            CheckNulls(relPath, result is GameEvent ev ? $"events[{ev.Id}]" : "", result!);
            return result;
        }

        /// <summary>A key written with no value (`tags:`) deserialises to null. Null lists and maps become load errors
        /// naming the field; null strings become "" (the default every check already handles). Walks nested content
        /// objects, naming list entries by id when they have one, else by index.</summary>
        static void CheckNulls(string file, string path, object o)
        {
            foreach (var p in o.GetType().GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (p.GetCustomAttribute<YamlIgnoreAttribute>() != null) continue;
                var key = (path == "" ? "" : path + ".") + UnderscoredNamingConvention.Instance.Apply(p.Name);
                var t = p.PropertyType;
                var v = p.GetValue(o);
                if (t == typeof(string)) { if (v == null) p.SetValue(o, ""); else Encodable(file, key, (string)v); }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (v == null) throw new ContentException(file, key, "must be a list");
                    var list = (IList)v;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (item == null) throw new ContentException(file, FormattableString.Invariant($"{key}[{i}]"), "empty entry");
                        if (item is string si) Encodable(file, FormattableString.Invariant($"{key}[{i}]"), si);
                        if (!IsContentType(item.GetType())) continue;
                        var id = item.GetType().GetProperty("Id")?.GetValue(item) as string;
                        CheckNulls(file, string.IsNullOrEmpty(id) ? FormattableString.Invariant($"{key}[{i}]") : $"{key}[{id}]", item);
                    }
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    if (v == null) throw new ContentException(file, key, "must be a map");
                    foreach (DictionaryEntry kv in (IDictionary)v)
                    {
                        if (kv.Key is string sk) Encodable(file, key + "." + sk, sk);
                        if (kv.Value is string sv) Encodable(file, key + "." + kv.Key, sv);
                    }
                }
                else if (v != null && IsContentType(t)) CheckNulls(file, key, v);
            }
        }

        /// <summary>Every id and text must be valid Unicode (no lone surrogate), or orders and saves that carry it would fail
        /// later. Uses the same check as the save format.</summary>
        static void Encodable(string file, string field, string s)
        {
            if (!SaveText.IsEncodable(s)) throw new ContentException(file, field, "must be valid text (no lone surrogate)");
        }

        /// <summary>A person's title_he_form: left out, or feminine with its source; and then every office the person holds
        /// has a feminine title_he_f, so the UI never falls back to the masculine title for her.</summary>
        static void TitleHeForm(Content c, Person p)
        {
            var at = $"people[{p.Id}].title_he_form";
            var sourced = p.BiographySources.ContainsKey("title_he_form");
            if (!p.TitleHeFormGiven)
            {
                if (sourced) throw new ContentException("people.yaml", at, "required: biography_sources.title_he_form is set but title_he_form is not");
                return;
            }
            if (p.TitleHeForm != TitleHeForms.Feminine)
                throw new ContentException("people.yaml", at, $"must be {TitleHeForms.Feminine}, or left out for the masculine title_he");
            if (!sourced) throw new ContentException("people.yaml", $"people[{p.Id}].biography_sources.title_he_form", "required: a feminine title_he_form needs a source (url and checked)");
            foreach (var t in p.Offices)
            {
                var o = c.Offices.FirstOrDefault(x => x.Id == t.Office);
                if (o != null && o.TitleHeF == "")
                    throw new ContentException("offices.yaml", $"offices[{o.Id}].title_he_f", $"required: {p.Id} holds this office and has title_he_form: {TitleHeForms.Feminine}");
            }
        }

        /// <summary>A Hebrew display name (#558): when set it holds Hebrew letters and has at least one source, each an https URL
        /// with its checked date; sources without a name are an error. Empty means no sourced Hebrew name (the UI keeps the English).</summary>
        static void HebrewName(string file, string at, string key, string value, List<Source> sources, string review, bool reviewGiven)
        {
            // The flag is either absent (a person has reviewed the name) or exactly agent-checked; a blank or null value is
            // neither, so it is an error rather than a silent "reviewed".
            if (reviewGiven && review != HebrewReview.AgentChecked)
                throw new ContentException(file, $"{at}.{key}_review", $"must be {HebrewReview.AgentChecked} (pending a native speaker's review, #564), or left out once a person has reviewed the name");
            if (value == "")
            {
                if (sources.Count > 0) throw new ContentException(file, $"{at}.{key}", $"required: {key}_sources is set but the name is empty");
                if (reviewGiven) throw new ContentException(file, $"{at}.{key}", $"required: {key}_review is set but the name is empty");
                return;
            }
            if (!value.Any(ch => ch >= '\u05D0' && ch <= '\u05EA')) throw new ContentException(file, $"{at}.{key}", "must be the Hebrew name, in Hebrew letters");
            if (sources.Count == 0) throw new ContentException(file, $"{at}.{key}_sources", "required: a Hebrew name needs a source (url and checked)");
            for (int i = 0; i < sources.Count; i++)
                if (!sources[i].Url.StartsWith("https://", StringComparison.Ordinal) || sources[i].Checked == default)
                    throw new ContentException(file, FormattableString.Invariant($"{at}.{key}_sources[{i}]"), "needs an https url and a checked date");
        }

        static bool IsContentType(Type t) => t.IsClass && t != typeof(string) && t.Namespace == typeof(Content).Namespace;

        /// <summary>Loads content through <paramref name="readFile"/>, which takes a path relative to the content root
        /// ("people.yaml", "events/x.yaml") and returns the file's text, or null when it is missing. This is the
        /// entry point for platforms without a file system (a WebGL build reads StreamingAssets through a URL).
        /// <paramref name="eventFiles"/> are the event files, relative to the content root; they load in ordinal order.</summary>
        public static Content Load(Func<string, string?> readFile, IEnumerable<string> eventFiles)
        {
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));
            if (eventFiles == null) throw new ArgumentNullException(nameof(eventFiles));
            var read = new List<(string rel, string text)>();
            var inner = readFile;
            readFile = rel =>
            {
                var text = inner(rel);
                if (text != null && text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);   // a byte-order mark is not content
                if (text != null) read.Add((rel, text));
                return text;
            };
            var c = new Content();
            c.Offices = Read<OfficesFile>(readFile, "offices.yaml").Offices;
            c.Parties = Read<PartiesFile>(readFile, "parties.yaml").Parties;
            c.People = Read<PeopleFile>(readFile, "people.yaml").People;
            c.Legislatures = Read<LegislaturesFile>(readFile, "legislatures.yaml").Legislatures;
            c.Governments = Read<GovernmentsFile>(readFile, "governments.yaml").Governments;
            c.Eras = Read<ErasFile>(readFile, "eras.yaml").Eras;
            var m = Read<ModelFile>(readFile, "model.yaml");
            c.Indicators = m.Indicators; c.Policies = m.Policies; c.Effects = m.Effects;
            c.Situations = m.Situations; c.VoterGroups = m.VoterGroups; c.Actors = m.Actors; c.Money = m.Money;
            string rulesText = "";
            c.Rules = Read<Rules>(p => { var t = readFile(p); rulesText = t ?? ""; return t; }, "rules.yaml");
            c.EraRules = ReadEraRules(rulesText, readFile, c.Eras);
            var aliyah = Read<AliyahFile>(readFile, "aliyah.yaml");
            c.Aliyah = aliyah.Series; c.Regions = aliyah.Regions;
            c.PopulationAnchors = Read<PopulationFile>(readFile, "population.yaml").Anchors;
            c.Hexes = Read<MapFile>(readFile, HexesFile).Hexes;
            c.SettlementHexes = Read<SettlementHexesFile>(readFile, SettlementHexesFileName).Settlements;
            var settlements = ReadSettlements(readFile);
            var settlementIds = settlements.Select(x => x.id).ToList();
            foreach (var (id, name) in settlements) c.SettlementNames[id] = name;   // a duplicate id fails in ValidateMap
            // Event files (content/events/*.yaml), one GameEvent each. Keys, for content authors:
            //   id, title, text            text may name office holders only through {office_id} placeholders, which
            //                              must have a holder on every day of the window
            //   anchor: hard | soft        hard needs a historical option; in Historical mode it resolves to it
            //   tags: [...]                holocaust, terror, massacre, war-casualties are sensitive (no money reward)
            //   office                     the minister the event is delegated to
            //   from, to                   the window [from, to); it fires on the first turn that overlaps it
            //   conditions: [{ indicator, min, max }]   indicator bounds that must hold when it fires
            //   requires: [{ event, option } | { event, diverged: true }, ...]   fires only once every entry holds: that
            //                              event was resolved (chosen or delegated) with that option, or with any option but
            //                              its historical one (R-ALT-001, R-ALT-002); the ids must exist, an event cannot
            //                              require itself and requirements cannot form a cycle (EventRules)
            //   options: [{ id, text, historical, recommended, effects: [{ target, delta }], set_rung, set_policy,
            //              grant_units: [{ type, n, at }], city_effects: [{ city, field, delta }] }]
            //   sources: [{ url, checked }], status
            //   objective                  the objective (objectives.yaml) whose completion card this is: it fires only once
            //                              that objective is recorded, and has exactly one option
            //   site                       a swamp site (sites.yaml) whose works the event is about: in the GDD loop it
            //                              fires only while that site is being worked; the legacy loop ignores it
            foreach (var rel in eventFiles.OrderBy(p => p, StringComparer.Ordinal))
            {
                var e = Read<GameEvent>(readFile, rel);
                e.File = rel;
                c.Events.Add(e);
            }
            // Objectives (GDD v0.1, Phase 1 PR 6). Read in both loops, since their cards are events every loop loads; a content
            // set without objectives has no file.
            c.Objectives = ReadOptional<ObjectivesFile>(readFile, ObjectivesFileName)?.Objectives
                .OrderBy(o => o.Id, StringComparer.Ordinal).ToList() ?? new List<Objective>();
            Validate(c, settlementIds);
            if (c.IsGdd)
            {
                // GDD v0.1 city data (Phase 1 PR 3), read once eras.yaml is known to be valid. Only the GDD loop reads it, so
                // the legacy content needs neither file.
                c.Cities = Read<CitiesFile>(readFile, "cities.yaml").Cities;
                c.UnitTypes = Read<UnitTypesFile>(readFile, "unit_types.yaml").Types;
                c.Sites = Read<SitesFile>(readFile, "sites.yaml").Sites;   // sourced facts (Phase 1 PR 5)
                c.ProjectKinds = Read<ProjectsFile>(readFile, "projects.yaml").Projects;   // design values (Phase 3 PR 4)
                ValidateGdd(c, settlementIds);
            }
            EventRules.ValidateStructured(c);   // R-EFX-006: after the GDD records it names are read
            c.BuildWindows();
            c.Hash = HashOf(read);
            return c;
        }

        /// <summary>SHA-256 over "path\nlength\ntext" for each file, CRLF normalised to LF (a Windows checkout hashes alike).</summary>
        static string HashOf(List<(string rel, string text)> files)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (rel, text) in files)
            {
                var t = text.Replace("\r\n", "\n");
                sb.Append(rel).Append('\n').Append(t.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n').Append(t);
            }
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
        }

        /// <summary>Loads content from a directory: every events/*.yaml file is an event.</summary>
        public static Content Load(string contentRoot)
        {
            var eventsDir = Path.Combine(contentRoot, "events");
            var eventFiles = Directory.Exists(eventsDir)
                ? Directory.GetFiles(eventsDir, "*.yaml").Select(f => "events/" + Path.GetFileName(f)).ToList()
                : new List<string>();
            var content = Load(rel =>
            {
                var path = Path.Combine(contentRoot, rel);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }, eventFiles);
            // Only this overload can list the directory. The readFile overload (WebGL) cannot, so a misnamed file is caught
            // here and by RealContentTests before anything ships.
            var rulesDir = Path.Combine(contentRoot, "rules");
            if (Directory.Exists(rulesDir))
                foreach (var f in Directory.GetFiles(rulesDir, "*.yaml").OrderBy(f => f, StringComparer.Ordinal))
                    if (!content.EraRules.ContainsKey(Path.GetFileNameWithoutExtension(f)))
                        throw new ContentException("rules/" + Path.GetFileName(f), "(file)", "the file name must be the id of an era in eras.yaml");
            return content;
        }

        internal const string IdRule = "must be lowercase letters, digits and underscores, starting with a letter";

        /// <summary>Every id has the office-id shape (so it is safe in placeholders, CLI input and save files) and is unique in its list.</summary>
        static void Unique(string file, string list, IEnumerable<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                if (!Offices.OfficeId.IsMatch(id)) throw new ContentException(file, $"{list}[{id}].id", IdRule);
                if (!seen.Add(id)) throw new ContentException(file, $"{list}[{id}].id", "duplicate id");
            }
        }

        static readonly HashSet<string> Statuses = new HashSet<string> { "draft", "fact-checked", "historian-reviewed", "sensitivity-reviewed" };

        /// <summary>A display name: required, and not blank.</summary>
        internal static void Named(string file, string field, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ContentException(file, field, "required (the name shown to the player)");
        }

        /// <summary>A row's sign-off (#375): reviewed_by and reviewed_on together, a GitHub handle, a day as YYYY-MM-DD (read as
        /// text, the same rule as tools/review_record.py and the map's readers). <paramref name="at"/> is the row's path, as in "people[ben_gurion]".</summary>
        internal static void Review(string file, string at, string by, bool byGiven, string on, bool onGiven)
        {
            var p = ReviewRecord.Problem(byGiven, by, onGiven, on);
            if (p != null) throw new ContentException(file, $"{at}.{p.Value.field}", p.Value.message);
        }

        internal static void Status(string file, string field, string status)
        {
            if (!Statuses.Contains(status))
                throw new ContentException(file, field, $"unknown status '{status}' (must be draft, fact-checked, historian-reviewed or sensitivity-reviewed)");
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        internal static void Finite(string file, string field, double v)
        {
            if (!IsFinite(v)) throw new ContentException(file, field, "must be a finite number");
        }

        internal static void Unit(string file, string field, double v)
        {
            if (!(v >= 0 && v <= 1)) throw new ContentException(file, field, "must be a number from 0 to 1");
        }

        static void Count(string file, string field, double v)
        {
            if (!(v >= 0 && IsFinite(v))) throw new ContentException(file, field, "must be a finite number >= 0");
        }

        /// <summary>from is required (a missing date deserialises to DateTime default); to, when given, must be after from.</summary>
        internal static void Period(string file, string path, DateTime from, DateTime? to)
        {
            if (from == default) throw new ContentException(file, path + ".from", "required");
            if (to.HasValue && to.Value <= from) throw new ContentException(file, path + ".to", FormattableString.Invariant($"must be after from ({from:yyyy-MM-dd})"));
        }

        /// <summary>Only one legislature sits, and one government holds office, on any date: "which body sat on this
        /// date" must never be ambiguous. Periods are [from, to); sorted by from, each must end on or before the next
        /// one starts (an open-ended period can only be the last).</summary>
        static void NoOverlap(string file, string list, IEnumerable<(string id, DateTime from, DateTime? to)> periods)
        {
            var sorted = periods.OrderBy(p => p.from).ThenBy(p => p.id, StringComparer.Ordinal).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                var (prev, next) = (sorted[i - 1], sorted[i]);
                if (!prev.to.HasValue || prev.to.Value > next.from)
                    throw new ContentException(file, $"{list}[{next.id}].from", FormattableString.Invariant(
                        $"overlaps {prev.id} ({prev.from:yyyy-MM-dd} to {(prev.to.HasValue ? prev.to.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "open")})"));
            }
        }

        /// <summary>Checks one complete rules set: rules.yaml itself, or an era's rules after its override is merged in.</summary>
        static void ValidateRules(Content c, Rules r, string file, HashSet<string> indicators, HashSet<string> offices, HashSet<string> historicalOffices)
        {
            if (r.Start == default) throw new ContentException(file, "start", "required");
            if (!(r.AttentionPerTurn >= 0 && !double.IsInfinity(r.AttentionPerTurn))) throw new ContentException(file, "attention_per_turn", "must be a finite non-negative number");
            if (!(r.RungAttentionStep >= 0 && !double.IsInfinity(r.RungAttentionStep))) throw new ContentException(file, "rung_attention_step", "must be a finite non-negative number");
            foreach (var kv in r.AttentionCost)
                if (!(kv.Value >= 0 && !double.IsInfinity(kv.Value))) throw new ContentException(file, $"attention_cost.{kv.Key}", "must be a finite non-negative number");
            foreach (var k in r.AttentionCost.Keys)
                if (!AttentionSystem.IsCommandKind(k)) throw new ContentException(file, $"attention_cost.{k}", "unknown command kind");
            foreach (var k in AttentionSystem.CommandKinds)
                if ((c.IsGdd || !AttentionSystem.IsGddKind(k)) && !r.AttentionCost.ContainsKey(k))   // a GDD kind is priced only where it can run
                    throw new ContentException(file, $"attention_cost.{k}", "missing cost");
            if (r.RungMinMobilization.Count != 5) throw new ContentException(file, "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
            for (int i = 0; i < 5; i++)
            {
                var field = FormattableString.Invariant($"rung_min_mobilization[{i}]");
                Unit(file, field, r.RungMinMobilization[i]);
                if (i > 0 && r.RungMinMobilization[i] < r.RungMinMobilization[i - 1])
                    throw new ContentException(file, field, "must not be below the previous rung's floor");
            }
            if (r.TurnLengths.Count != 5) throw new ContentException(file, "turn_lengths", "needs exactly 5 values (rungs 1-5)");
            for (int i = 0; i < 5; i++)
                if (!SimTime.LengthNames.Contains(r.TurnLengths[i]))
                    throw new ContentException(file, FormattableString.Invariant($"turn_lengths[{i}]"), "must be quarter, month or week");
            if (r.StartRung < 1 || r.StartRung > 5) throw new ContentException(file, "start_rung", "must be 1-5");
            if (r.CeasefireWeeks <= 0) throw new ContentException(file, "ceasefire_weeks", "must be > 0");
            foreach (var (key, v) in new[] { ("treasury_start", r.TreasuryStart), ("fx_start", r.FxStart), ("tax_per_capita_per_year", r.TaxPerCapitaPerYear),
                ("spending_per_capita_per_year", r.SpendingPerCapitaPerYear), ("exports_per_year", r.ExportsPerYear), ("imports_per_capita_per_year", r.ImportsPerCapitaPerYear),
                ("mobilization_cost_per_year", r.MobilizationCostPerYear), ("natural_increase_per_year", r.NaturalIncreasePerYear),
                ("capital_inflow_per_year", r.CapitalInflowPerYear), ("fx_crisis_food_per_year", r.FxCrisisFoodPerYear) })
                Finite(file, key, v);
            Unit(file, "appoint_stability_cost_other_party", r.AppointStabilityCostOtherParty);
            Unit(file, "appoint_stability_cost_same_party", r.AppointStabilityCostSameParty);
            if (r.MapActionMaxDistance < 0) throw new ContentException(file, "map_action_max_distance", "must be >= 0");
            if (r.MapActionsPerTurn < 1) throw new ContentException(file, "map_actions_per_turn", "must be >= 1");
            foreach (var kv in r.MapActions)
            {
                if (!MapActions.Actions.Contains(kv.Key)) throw new ContentException(file, $"map_actions.{kv.Key}", "unknown map action (transit_camp or housing)");
                if (!(kv.Value.Cost >= 0 && IsFinite(kv.Value.Cost))) throw new ContentException(file, $"map_actions.{kv.Key}.cost", "must be a finite number >= 0");
                Named(file, $"map_actions.{kv.Key}.name", kv.Value.Name);
                if (kv.Value.From.HasValue && kv.Value.To.HasValue && kv.Value.To.Value <= kv.Value.From.Value)
                    throw new ContentException(file, $"map_actions.{kv.Key}.to", FormattableString.Invariant($"must be after from ({kv.Value.From:yyyy-MM-dd})"));
                foreach (var (key, v) in new[] { ("housing", kv.Value.Housing), ("morale", kv.Value.Morale) })
                    if (!(v >= -1 && v <= 1)) throw new ContentException(file, $"map_actions.{kv.Key}.{key}", "must be a number from -1 to 1");
            }
            if (r.MapActions.Count > 0 && r.MapActionsFrom == default) throw new ContentException(file, "map_actions_from", "required when map_actions are defined");
            if (r.MapActions.Count > 0 && (!indicators.Contains("housing") || !indicators.Contains("morale")))
                throw new ContentException(file, "map_actions", "needs housing and morale indicators in model.yaml");
            if (r.FxCrisisFoodPerYear > 0) throw new ContentException(file, "fx_crisis_food_per_year", "must be <= 0 (a crisis cuts food imports)");
            if (r.FxCrisisFoodPerYear < 0 && !indicators.Contains("food_supply")) throw new ContentException(file, "fx_crisis_food_per_year", "needs a food_supply indicator in model.yaml");
            foreach (var kv in r.InitialPopulation) Count(file, $"initial_population.{kv.Key}", kv.Value);
            // Budget lines (#108). A line is keyed by its own id. With no budget_owners entry the office of that id owns it for
            // the whole era; with an entry, a dated list says which office owns it when. Within one era only: a line that
            // changes across an era boundary is the carry rules' job (chapters spec section 4, #59).
            var era = c.EraAt(r.Start);
            var eraEnd = era?.To ?? DateTime.MaxValue;
            foreach (var kv in r.BudgetOwners)
                if (!r.BudgetStart.ContainsKey(kv.Key)) throw new ContentException(file, $"budget_owners.{kv.Key}", "unknown budget line: list it in budget_start");
            var spans = new List<(string line, string office, DateTime from, DateTime to)>();
            foreach (var kv in r.BudgetStart)
            {
                Unit(file, $"budget_start.{kv.Key}", kv.Value);
                if (!r.BudgetOwners.TryGetValue(kv.Key, out var terms))
                {
                    if (!offices.Contains(kv.Key)) throw new ContentException(file, $"budget_start.{kv.Key}", "unknown office (or give the line an owner in budget_owners)");
                    if (historicalOffices.Contains(kv.Key)) throw new ContentException(file, $"budget_start.{kv.Key}", "historical-only titles cannot have a budget line");
                    if (c.Offices.Any(o => o.Id == kv.Key && o.SeveralHolders)) throw new ContentException(file, $"budget_start.{kv.Key}", "an office with several holders cannot own a budget line");
                    // A budget line must not silently stop mattering when its office is abolished: give it dated owners (#108).
                    var until = c.Offices.First(o => o.Id == kv.Key).Until;
                    if (until.HasValue)
                        throw new ContentException(file, $"budget_start.{kv.Key}", FormattableString.Invariant($"office '{kv.Key}' is abolished on {until:yyyy-MM-dd}: give its budget line to the office that took over (budget_owners)"));
                    spans.Add((kv.Key, kv.Key, r.Start, eraEnd));
                    continue;
                }
                if (!Offices.OfficeId.IsMatch(kv.Key)) throw new ContentException(file, $"budget_start.{kv.Key}", "a budget line id is lowercase letters, digits and underscores");
                var path = $"budget_owners.{kv.Key}";
                if (terms.Count == 0) throw new ContentException(file, path, "needs at least one owner");
                for (int i = 0; i < terms.Count; i++)
                {
                    var t = terms[i]; var tp = FormattableString.Invariant($"{path}[{i}]");
                    if (t.From == default) throw new ContentException(file, tp + ".from", "required");
                    if (t.From < r.Start || t.From >= eraEnd)
                        throw new ContentException(file, tp + ".from", FormattableString.Invariant($"must be inside the era ({r.Start:yyyy-MM-dd} to {eraEnd:yyyy-MM-dd}); a line that changes across eras is the carry rules' job"));
                    if (i == 0 && t.From != r.Start)
                        throw new ContentException(file, tp + ".from", FormattableString.Invariant($"the first owner must start on {r.Start:yyyy-MM-dd}, the first day of the era (no gap)"));
                    if (i > 0 && t.From <= terms[i - 1].From)
                        throw new ContentException(file, tp + ".from", FormattableString.Invariant($"must be after the previous owner's date ({terms[i - 1].From:yyyy-MM-dd})"));
                    if (t.Office == "" || !offices.Contains(t.Office)) throw new ContentException(file, tp + ".office", "unknown office");
                    if (historicalOffices.Contains(t.Office)) throw new ContentException(file, tp + ".office", "historical-only titles cannot have a budget line");
                    // Like policies and effects (#277): the line's factor scales one minister's competence, so it needs one holder.
                    if (c.Offices.Any(o => o.Id == t.Office && o.SeveralHolders)) throw new ContentException(file, tp + ".office", "an office with several holders cannot own a budget line");
                    var end = i + 1 < terms.Count ? terms[i + 1].From : eraEnd;
                    var office = c.Offices.First(o => o.Id == t.Office);
                    if (!office.ExistsOn(t.From))
                        throw new ContentException(file, tp + ".office", FormattableString.Invariant($"office '{t.Office}' does not exist on {t.From:yyyy-MM-dd}"));
                    if (office.Until.HasValue && office.Until.Value < end)
                        throw new ContentException(file, tp + ".office", FormattableString.Invariant($"office '{t.Office}' is abolished on {office.Until:yyyy-MM-dd}, before this owner's stretch ends ({end:yyyy-MM-dd}): add the office that took over"));
                    spans.Add((kv.Key, t.Office, t.From, end));
                }
            }
            // One office owns at most one line on any day, so BudgetFactor(office) has one answer.
            foreach (var a in spans)
                foreach (var b in spans)
                    if (string.CompareOrdinal(a.line, b.line) < 0 && a.office == b.office && a.from < b.to && b.from < a.to)
                        throw new ContentException(file, $"budget_start.{b.line}", $"office '{a.office}' would own both '{a.line}' and '{b.line}' on the same day");
            // The same rule EconomySystem.Allocate applies to every later allocation.
            if (r.BudgetStart.Count > 0 && Math.Abs(r.BudgetStart.Values.Sum() - 1.0) > 1e-6) throw new ContentException(file, "budget_start", "shares must sum to 1");
        }

        // Indicators the code reads by name: food_supply (EconomySystem, FX crisis), housing and morale (MapActions), security
        // and intl_standing (Unity HudModel). A window on one would make those reads miss, so the loader refuses it.
        static readonly string[] CodeNamedIndicators = { "morale", "food_supply", "housing", "security", "intl_standing" };

        /// <summary>Chapters design 3.2: a window must run forward, and nothing may reach a record that is not active for the
        /// whole window of whatever uses it (effect sources and targets, situations, voter-group weights, policy and effect offices).</summary>

        static void ValidateWindows(Content c)
        {
            void Order(string where, DateTime? from, DateTime? to)
            {
                if (from.HasValue && to.HasValue && from.Value >= to.Value) throw new ContentException("model.yaml", where, "from must be before to");
            }
            foreach (var x in c.Indicators)
                if ((x.From.HasValue || x.To.HasValue) && CodeNamedIndicators.Contains(x.Id))
                    throw new ContentException("model.yaml", $"indicators[{x.Id}]", $"'{x.Id}' is read by name in the code, so it cannot have a from/to window");
            foreach (var x in c.Indicators) Order($"indicators[{x.Id}]", x.From, x.To);
            foreach (var x in c.Policies) Order($"policies[{x.Id}]", x.From, x.To);
            foreach (var x in c.Effects) Order($"effects[{x.Id}]", x.From, x.To);
            foreach (var x in c.Situations) Order($"situations[{x.Id}]", x.From, x.To);
            foreach (var x in c.VoterGroups) Order($"voter_groups[{x.Id}]", x.From, x.To);
            foreach (var x in c.Actors) Order($"actors[{x.Id}]", x.From, x.To);

            void Reach(string where, DateTime? from, DateTime? to, string kind, string id, DateTime? rFrom, DateTime? rTo)
            {
                if (!Window.Covers(rFrom, rTo, from, to))
                    throw new ContentException("model.yaml", where, $"{kind} '{id}' is not active for the whole window of this record (give both the same from/to, or a wider one on '{id}')");
            }
            // The reached record's window, or null when the id is a record kind with none: (null, null) means "always".
            (DateTime?, DateTime?) Win(string id)
            {
                var ind = c.Indicators.FirstOrDefault(x => x.Id == id);
                if (ind != null) return (ind.From, ind.To);
                var pol = c.Policies.FirstOrDefault(x => x.Id == id);
                return pol != null ? (pol.From, pol.To) : ((DateTime?)null, (DateTime?)null);
            }
            foreach (var e in c.Effects)
            {
                var s = Win(e.Source); var t = Win(e.Target);
                Reach($"effects[{e.Id}].source", e.From, e.To, "source", e.Source, s.Item1, s.Item2);
                Reach($"effects[{e.Id}].target", e.From, e.To, "target", e.Target, t.Item1, t.Item2);
            }
            foreach (var s in c.Situations)
            {
                var w = Win(s.Indicator);
                Reach($"situations[{s.Id}].indicator", s.From, s.To, "indicator", s.Indicator, w.Item1, w.Item2);
            }
            foreach (var g in c.VoterGroups)
                foreach (var k in g.Weights.Keys)
                {
                    var w = Win(k);
                    Reach($"voter_groups[{g.Id}].weights.{k}", g.From, g.To, "indicator", k, w.Item1, w.Item2);
                }
            // A record with a window of its own must have its office for that whole window. Records with no window keep the
            // existing rule (a policy may name an office that is created later: it has no holder until then).
            void OfficeFor(string where, string office, DateTime? from, DateTime? to)
            {
                if (office == "" || (!from.HasValue && !to.HasValue)) return;
                var o = c.Offices.FirstOrDefault(x => x.Id == office);
                if (o != null && !Window.Covers(o.From, o.Until, from, to))
                    throw new ContentException("model.yaml", where, $"office '{office}' does not exist for the whole window of this record");
            }
            foreach (var p in c.Policies) OfficeFor($"policies[{p.Id}].office", p.Office, p.From, p.To);
            foreach (var e in c.Effects) OfficeFor($"effects[{e.Id}].office", e.Office, e.From, e.To);
        }

        static void Validate(Content c, List<string> settlementIds)
        {
            var offices = new HashSet<string>(c.Offices.Select(o => o.Id));
            var historicalOffices = new HashSet<string>(c.Offices.Where(o => o.HistoricalOnly).Select(o => o.Id));
            var parties = new HashSet<string>(c.Parties.Select(p => p.Id));
            var indicators = new HashSet<string>(c.Indicators.Select(i => i.Id));
            var sources = new HashSet<string>(c.Policies.Select(p => p.Id).Concat(indicators));
            // The cities' employed share (R-CITY-005). Accepted in both loops, so the real content still loads with the loop
            // switch taken out (Unity's SimBridgeGddTests.Legacy); with no cities it reads 0 (EffectsSystem.Source).
            sources.Add(Cities.EmployedShareSource);
            Unique("offices.yaml", "offices", c.Offices.Select(o => o.Id));
            Unique("parties.yaml", "parties", c.Parties.Select(p => p.Id));
            Unique("people.yaml", "people", c.People.Select(p => p.Id));
            Unique("legislatures.yaml", "legislatures", c.Legislatures.Select(l => l.Id));
            Unique("governments.yaml", "governments", c.Governments.Select(g => g.Id));
            Unique("eras.yaml", "eras", c.Eras.Select(e => e.Id));
            ValidateEras(c);
            Unique("model.yaml", "indicators", c.Indicators.Select(i => i.Id));
            Unique("model.yaml", "policies", c.Policies.Select(p => p.Id));
            Unique("model.yaml", "effects", c.Effects.Select(e => e.Id));
            Unique("model.yaml", "situations", c.Situations.Select(s => s.Id));
            Unique("model.yaml", "voter_groups", c.VoterGroups.Select(v => v.Id));
            Unique("model.yaml", "actors", c.Actors.Select(a => a.Id));
            Unique("model.yaml", "money", c.Money.Select(a => a.Id));
            Unique("aliyah.yaml", "regions", c.Regions.Select(g => g.Id));
            // Display names (spec: player-facing text): Cause messages show these, never the ids.
            foreach (var i in c.Indicators) Named("model.yaml", $"indicators[{i.Id}].name", i.Name);
            foreach (var p in c.Policies) Named("model.yaml", $"policies[{p.Id}].name", p.Name);
            foreach (var e in c.Effects) Named("model.yaml", $"effects[{e.Id}].name", e.Name);
            foreach (var s in c.Situations) Named("model.yaml", $"situations[{s.Id}].name", s.Name);
            foreach (var g in c.Regions) Named("aliyah.yaml", $"regions[{g.Id}].name", g.Name);
            // Hebrew display names (#558): each value carries its own sources, and sources need a value.
            foreach (var o in c.Offices)
            {
                HebrewName("offices.yaml", $"offices[{o.Id}]", "title_he", o.TitleHe, o.TitleHeSources, o.TitleHeReview, o.TitleHeReviewGiven);
                HebrewName("offices.yaml", $"offices[{o.Id}]", "title_he_f", o.TitleHeF, o.TitleHeFSources, o.TitleHeFReview, o.TitleHeFReviewGiven);
                if (o.TitleHeF != "" && o.TitleHe == "") throw new ContentException("offices.yaml", $"offices[{o.Id}].title_he", "required: a feminine title_he_f needs the title_he beside it");
                // The player can order a woman into any office, so a Hebrew title without its feminine form would show her the
                // masculine one (owner on #861, 17 October 2026).
                if (o.TitleHe != "" && o.TitleHeF == "") throw new ContentException("offices.yaml", $"offices[{o.Id}].title_he_f", $"required: office {o.Id} has a title_he, so it needs its feminine title_he_f beside it");
            }
            foreach (var p in c.Parties) HebrewName("parties.yaml", $"parties[{p.Id}]", "name_he", p.NameHe, p.NameHeSources, p.NameHeReview, p.NameHeReviewGiven);
            foreach (var l in c.Legislatures) HebrewName("legislatures.yaml", $"legislatures[{l.Id}]", "body_he", l.BodyHe, l.BodyHeSources, l.BodyHeReview, l.BodyHeReviewGiven);
            foreach (var g in c.Governments) HebrewName("governments.yaml", $"governments[{g.Id}]", "name_he", g.NameHe, g.NameHeSources, g.NameHeReview, g.NameHeReviewGiven);
            // People keep their name_he sources in biography_sources (checked below), so only the review flag is checked here (#862).
            foreach (var p in c.People)
            {
                if (p.NameHeReviewGiven && p.NameHeReview != HebrewReview.AgentChecked)
                    throw new ContentException("people.yaml", $"people[{p.Id}].name_he_review", $"must be {HebrewReview.AgentChecked} (pending a native speaker's review, #564), or left out once a person has reviewed the name");
                if (p.NameHeReviewGiven && p.NameHe == "") throw new ContentException("people.yaml", $"people[{p.Id}].name_he", "required: name_he_review is set but the name is empty");
            }
            foreach (var a in c.Money)
            {
                if (Array.IndexOf(PlayerText.MoneyIds, a.Id) < 0)
                    throw new ContentException("model.yaml", $"money[{a.Id}].id", $"must be one of {string.Join(", ", PlayerText.MoneyIds)}");
                Named("model.yaml", $"money[{a.Id}].name", a.Name);
                if (string.IsNullOrWhiteSpace(a.Prefix)) throw new ContentException("model.yaml", $"money[{a.Id}].prefix", "required (the currency the player sees, e.g. 'IL ' or '$')");
            }
            var missingMoney = PlayerText.MoneyIds.FirstOrDefault(id => c.Money.All(a => a.Id != id));
            if (missingMoney != null) throw new ContentException("model.yaml", "money", $"missing {missingMoney}");
            foreach (var p in c.Policies)
                if (indicators.Contains(p.Id)) throw new ContentException("model.yaml", $"policies[{p.Id}].id", "same id as an indicator (effect sources would be ambiguous)");
            // Money, indicators and policies share one namespace in Cause targets and PlayerText.Target (fix round 2, N7).
            foreach (var i in c.Indicators)
                if (Array.IndexOf(PlayerText.MoneyIds, i.Id) >= 0) throw new ContentException("model.yaml", $"indicators[{i.Id}].id", "same id as money (treasury, debt, fx_reserves)");
            foreach (var p in c.Policies)
                if (Array.IndexOf(PlayerText.MoneyIds, p.Id) >= 0) throw new ContentException("model.yaml", $"policies[{p.Id}].id", "same id as money (treasury, debt, fx_reserves)");

            foreach (var p in c.People) { Status("people.yaml", $"people[{p.Id}].status", p.Status); Review("people.yaml", $"people[{p.Id}]", p.ReviewedBy, p.ReviewedByGiven, p.ReviewedOn, p.ReviewedOnGiven); }
            foreach (var l in c.Legislatures) { Status("legislatures.yaml", $"legislatures[{l.Id}].status", l.Status); Review("legislatures.yaml", $"legislatures[{l.Id}]", l.ReviewedBy, l.ReviewedByGiven, l.ReviewedOn, l.ReviewedOnGiven); }
            foreach (var g in c.Governments) { Status("governments.yaml", $"governments[{g.Id}].status", g.Status); Review("governments.yaml", $"governments[{g.Id}]", g.ReviewedBy, g.ReviewedByGiven, g.ReviewedOn, g.ReviewedOnGiven); }
            for (int i = 0; i < c.Aliyah.Count; i++) { Status("aliyah.yaml", FormattableString.Invariant($"series[{i}].status"), c.Aliyah[i].Status); Review("aliyah.yaml", FormattableString.Invariant($"series[{i}]"), c.Aliyah[i].ReviewedBy, c.Aliyah[i].ReviewedByGiven, c.Aliyah[i].ReviewedOn, c.Aliyah[i].ReviewedOnGiven); }
            for (int i = 0; i < c.PopulationAnchors.Count; i++) { Status("population.yaml", FormattableString.Invariant($"anchors[{i}].status"), c.PopulationAnchors[i].Status); Review("population.yaml", FormattableString.Invariant($"anchors[{i}]"), c.PopulationAnchors[i].ReviewedBy, c.PopulationAnchors[i].ReviewedByGiven, c.PopulationAnchors[i].ReviewedOn, c.PopulationAnchors[i].ReviewedOnGiven); }

            foreach (var p in c.People)
            {
                if (p.Portrait != null && p.Portrait.File != "" && !(p.Portrait.FocusY >= 0 && p.Portrait.FocusY <= 1))
                    throw new ContentException("people.yaml", $"people[{p.Id}].portrait.focus_y", "required: the face's height as a share of the picture, from 0 (top) to 1 (bottom)");
                if (!p.Living.HasValue) throw new ContentException("people.yaml", $"people[{p.Id}].living", "required (true or false)");
                if (p.Competence < 0 || p.Competence > 10) throw new ContentException("people.yaml", $"people[{p.Id}].competence", "must be 0-10");
                if (p.Loyalty < 0 || p.Loyalty > 10) throw new ContentException("people.yaml", $"people[{p.Id}].loyalty", "must be 0-10");
                if (p.Party != "" && !parties.Contains(p.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].party", $"unknown party '{p.Party}'");
                if (p.Born.HasValue && p.Died.HasValue && p.Born.Value >= p.Died.Value)
                    throw new ContentException("people.yaml", $"people[{p.Id}].born", FormattableString.Invariant($"must be before {p.Died:yyyy-MM-dd}, the date of death"));
                var firstTerm = p.Offices.Count > 0 ? p.Offices.Min(t => t.From) : (DateTime?)null;
                if (p.Born.HasValue && firstTerm.HasValue && p.Born.Value > firstTerm.Value)
                    throw new ContentException("people.yaml", $"people[{p.Id}].born", FormattableString.Invariant($"must be on or before {firstTerm:yyyy-MM-dd}, when the first office term starts"));
                // A map skips CheckNulls' list and entry checks, so a misspelt key or an empty source would load silently.
                foreach (var kv in p.BiographySources)
                {
                    var field = $"people[{p.Id}].biography_sources.{kv.Key}";
                    if (kv.Key != "name_he" && kv.Key != "title_he_form" && kv.Key != "born" && kv.Key != "died")
                        throw new ContentException("people.yaml", field, "unknown field (must be name_he, title_he_form, born or died)");
                    if (kv.Value == null) throw new ContentException("people.yaml", field, "must be a list");
                    for (int i = 0; i < kv.Value.Count; i++)
                        if (kv.Value[i] == null || string.IsNullOrWhiteSpace(kv.Value[i].Url) || kv.Value[i].Checked == default)
                            throw new ContentException("people.yaml", FormattableString.Invariant($"{field}[{i}]"), "needs a url and a checked date");
                }
                TitleHeForm(c, p);
                foreach (var t in p.Offices)
                {
                    if (string.IsNullOrEmpty(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices", "office term without an office");
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.From == default) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.from", "required");
                    if (t.Party != "" && !parties.Contains(t.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.party", $"unknown party '{t.Party}'");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", FormattableString.Invariant($"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})"));
                }
            }
            foreach (var o in c.Offices.Where(o => o.Successor != ""))
            {
                var at = $"offices[{o.Id}].successor";
                var next = c.Offices.FirstOrDefault(x => x.Id == o.Successor);
                if (next == null) throw new ContentException("offices.yaml", at, $"unknown office '{o.Successor}'");
                if (next == o) throw new ContentException("offices.yaml", at, "an office cannot succeed itself");
                if (!o.Until.HasValue) throw new ContentException("offices.yaml", at, "needs until: only an abolished office has a successor");
                // The successor takes over the old office's budget lines and policies (chapters spec 4.2, #59), so it must be able
                // to own whatever the old office could.
                if (next.SeveralHolders && !o.SeveralHolders)
                    throw new ContentException("offices.yaml", at, $"office '{next.Id}' has several holders, so it cannot take over budget lines and policies");
                if (next.HistoricalOnly && !o.HistoricalOnly)
                    throw new ContentException("offices.yaml", at, $"office '{next.Id}' is a historical title, so it cannot take over budget lines and policies");
                if (next.From.HasValue && next.From.Value > o.Until.Value)
                    throw new ContentException("offices.yaml", at, FormattableString.Invariant($"office '{next.Id}' is established on {next.From:yyyy-MM-dd}, after {o.Id} is abolished ({o.Until:yyyy-MM-dd})"));
                if (next.Until.HasValue && next.Until.Value <= o.Until.Value)
                    throw new ContentException("offices.yaml", at, FormattableString.Invariant($"office '{next.Id}' is abolished on {next.Until:yyyy-MM-dd}, before it can take over on {o.Until:yyyy-MM-dd}"));
            }
            foreach (var o in c.Offices.Where(o => o.From.HasValue))
            {
                if (o.Until.HasValue && o.Until.Value <= o.From!.Value)
                    throw new ContentException("offices.yaml", $"offices[{o.Id}].until", FormattableString.Invariant($"must be after from ({o.From:yyyy-MM-dd})"));
                foreach (var p in c.People)
                    foreach (var t in p.Offices.Where(t => t.Office == o.Id && t.From < o.From!.Value))
                        throw new ContentException("people.yaml", $"people[{p.Id}].offices.{o.Id}.from", FormattableString.Invariant($"must be on or after {o.From:yyyy-MM-dd}, when the office was established"));
            }
            foreach (var o in c.Offices.Where(o => o.Until.HasValue))
                foreach (var p in c.People)
                    foreach (var t in p.Offices.Where(t => t.Office == o.Id))
                        if (!t.To.HasValue || t.To.Value > o.Until!.Value)
                            throw new ContentException("people.yaml", $"people[{p.Id}].offices.{o.Id}.to", FormattableString.Invariant($"must be on or before {o.Until:yyyy-MM-dd}, when the office was abolished"));
            // An open-ended term runs past the authored governments, so it only conflicts with a death before the latest
            // government began (#167). With no governments, every open-ended term conflicts, as before.
            var latestGovernmentStart = c.Governments.Count > 0 ? c.Governments.Max(g => g.From) : DateTime.MaxValue;
            foreach (var p in c.People.Where(p => p.Died.HasValue))
                foreach (var t in p.Offices)
                {
                    if (t.From >= p.Died!.Value)
                        throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.from", FormattableString.Invariant($"must be before {p.Died:yyyy-MM-dd}, the date of death"));
                    if (t.To.HasValue ? t.To.Value > p.Died!.Value : p.Died!.Value < latestGovernmentStart)
                        throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.to", FormattableString.Invariant($"must be on or before {p.Died:yyyy-MM-dd}, the date of death"));
                }
            foreach (var p in c.People)
                for (int i = 0; i < p.Offices.Count; i++)
                    for (int j = i + 1; j < p.Offices.Count; j++)
                    {
                        var (a, b) = (p.Offices[i], p.Offices[j]);
                        if (a.Party == "" || b.Party == "" || a.Party == b.Party) continue;
                        if (a.From < (b.To ?? DateTime.MaxValue) && b.From < (a.To ?? DateTime.MaxValue))
                            throw new ContentException("people.yaml", $"people[{p.Id}].offices", "overlapping terms disagree on party");
                    }
            var terms = c.People.SelectMany(p => p.Offices.Select(t => (person: p.Id, term: t))).ToList();
            for (int i = 0; i < terms.Count; i++)
                for (int j = i + 1; j < terms.Count; j++)
                {
                    var (a, b) = (terms[i], terms[j]);
                    if (a.term.Office != b.term.Office || a.person == b.person) continue;
                    if (c.Offices.Any(o => o.Id == a.term.Office && o.SeveralHolders)) continue;
                    if (a.term.From < (b.term.To ?? DateTime.MaxValue) && b.term.From < (a.term.To ?? DateTime.MaxValue))
                        throw new ContentException("people.yaml", $"offices.{a.term.Office}", $"held by both {a.person} and {b.person} on overlapping dates");
                }
            foreach (var l in c.Legislatures)
            {
                Period("legislatures.yaml", $"legislatures[{l.Id}]", l.From, l.To);
                if (l.Size <= 0) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", "must be > 0");
                foreach (var kv in l.Seats)
                {
                    if (!parties.Contains(kv.Key)) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "unknown party");
                    if (kv.Value < 0) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].seats.{kv.Key}", "must be >= 0");
                }
                if (l.Seats.Values.Sum() != l.Size) throw new ContentException("legislatures.yaml", $"legislatures[{l.Id}].size", FormattableString.Invariant($"seats sum to {l.Seats.Values.Sum()}, size is {l.Size}"));
            }
            foreach (var g in c.Governments)
            {
                Period("governments.yaml", $"governments[{g.Id}]", g.From, g.To);
                foreach (var p in g.Coalition)
                    if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"governments[{g.Id}].coalition.{p}", "unknown party");
                var inOffice = new List<string>(g.Coalition);
                var previous = g.From;
                for (int i = 0; i < g.CoalitionChanges.Count; i++)
                {
                    var ch = g.CoalitionChanges[i];
                    var at = $"governments[{g.Id}].coalition_changes[{i}]";
                    if (ch.Date == default) throw new ContentException("governments.yaml", at + ".date", "required");
                    if (ch.Date <= g.From || (g.To.HasValue && ch.Date >= g.To.Value))
                        throw new ContentException("governments.yaml", at + ".date", FormattableString.Invariant($"must be inside the term, after {g.From:yyyy-MM-dd}") + (g.To.HasValue ? FormattableString.Invariant($" and before {g.To.Value:yyyy-MM-dd}") : ""));
                    if (i > 0 && ch.Date <= previous) throw new ContentException("governments.yaml", at + ".date", FormattableString.Invariant($"must be after the previous change ({previous:yyyy-MM-dd})"));
                    previous = ch.Date;
                    if (ch.Joins.Count == 0 && ch.Leaves.Count == 0) throw new ContentException("governments.yaml", at, "needs joins or leaves");
                    if (ch.Sources.Count == 0) throw new ContentException("governments.yaml", at + ".sources", "required: a coalition change needs a source");
                    foreach (var p in ch.Leaves)
                    {
                        if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"{at}.leaves.{p}", "unknown party");
                        if (!inOffice.Remove(p)) throw new ContentException("governments.yaml", $"{at}.leaves.{p}", "not in the coalition on that day");
                    }
                    foreach (var p in ch.Joins)
                    {
                        if (!parties.Contains(p)) throw new ContentException("governments.yaml", $"{at}.joins.{p}", "unknown party");
                        if (ch.Leaves.Contains(p)) throw new ContentException("governments.yaml", $"{at}.joins.{p}", "cannot join and leave on the same day");
                        if (inOffice.Contains(p)) throw new ContentException("governments.yaml", $"{at}.joins.{p}", "already in the coalition on that day");
                        inOffice.Add(p);
                    }
                }
            }
            NoOverlap("legislatures.yaml", "legislatures", c.Legislatures.Select(l => (l.Id, l.From, l.To)));
            NoOverlap("governments.yaml", "governments", c.Governments.Select(g => (g.Id, g.From, g.To)));
            foreach (var i in c.Indicators)
            {
                Finite("model.yaml", $"indicators[{i.Id}].base", i.Base);
                Unit("model.yaml", $"indicators[{i.Id}].base", i.Base);
            }
            foreach (var p in c.Policies)
            {
                Finite("model.yaml", $"policies[{p.Id}].default", p.Default);
                Unit("model.yaml", $"policies[{p.Id}].default", p.Default);
            }
            foreach (var p in c.Policies)
            {
                if (!offices.Contains(p.Office)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", $"unknown office '{p.Office}'");
                if (historicalOffices.Contains(p.Office)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", "historical-only titles cannot own policies");
                if (c.Offices.Any(o => o.Id == p.Office && o.SeveralHolders)) throw new ContentException("model.yaml", $"policies[{p.Id}].office", "an office with several holders cannot own policies");
            }
            foreach (var e in c.Effects)
            {
                if (!sources.Contains(e.Source)) throw new ContentException("model.yaml", $"effects[{e.Id}].source", $"unknown policy or indicator '{e.Source}'");
                if (!indicators.Contains(e.Target)) throw new ContentException("model.yaml", $"effects[{e.Id}].target", $"unknown indicator '{e.Target}'");
                Finite("model.yaml", $"effects[{e.Id}].k", e.K);
                if (e.Inertia < 1) throw new ContentException("model.yaml", $"effects[{e.Id}].inertia", "must be >= 1");
                if (e.Office != "" && !offices.Contains(e.Office)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", $"unknown office '{e.Office}'");
                if (historicalOffices.Contains(e.Office)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", "historical-only titles cannot own effects");
                if (c.Offices.Any(o => o.Id == e.Office && o.SeveralHolders)) throw new ContentException("model.yaml", $"effects[{e.Id}].office", "an office with several holders cannot own effects");
            }
            foreach (var s in c.Situations)
            {
                if (!indicators.Contains(s.Indicator)) throw new ContentException("model.yaml", $"situations[{s.Id}].indicator", $"unknown indicator '{s.Indicator}'");
                bool below = s.OnBelow.HasValue && s.OffAbove.HasValue, above = s.OnAbove.HasValue && s.OffBelow.HasValue;
                if (below == above) throw new ContentException("model.yaml", $"situations[{s.Id}]", "needs exactly one pair: on_below+off_above or on_above+off_below");
                foreach (var (key, v) in new[] { ("on_below", s.OnBelow), ("off_above", s.OffAbove), ("on_above", s.OnAbove), ("off_below", s.OffBelow) })
                    if (v.HasValue) Unit("model.yaml", $"situations[{s.Id}].{key}", v.Value);
            }
            foreach (var v in c.VoterGroups)
                foreach (var kv in v.Weights)
                {
                    if (!indicators.Contains(kv.Key)) throw new ContentException("model.yaml", $"voter_groups[{v.Id}].weights.{kv.Key}", "unknown indicator");
                    Finite("model.yaml", $"voter_groups[{v.Id}].weights.{kv.Key}", kv.Value);
                }
            foreach (var a in c.Actors) Unit("model.yaml", $"actors[{a.Id}].standing", a.Standing);
            ValidateRules(c, c.Rules, "rules.yaml", indicators, offices, historicalOffices);
            foreach (var kv in c.EraRules)
                ValidateRules(c, kv.Value, "rules/" + kv.Key + ".yaml", indicators, offices, historicalOffices);
            foreach (var g in c.Regions.Where(g => g.From.HasValue && g.To.HasValue && g.To.Value <= g.From.Value))
                throw new ContentException("aliyah.yaml", $"regions[{g.Id}].to", FormattableString.Invariant($"must be after from ({g.From:yyyy-MM-dd})"));
            for (int i = 0; i < c.Aliyah.Count; i++)
            {
                var a = c.Aliyah[i]; var path = FormattableString.Invariant($"series[{i}]");
                if (a.From == default) throw new ContentException("aliyah.yaml", path + ".from", "required");
                if (a.To < a.From) throw new ContentException("aliyah.yaml", path + ".to", "required, on or after from (both dates are inclusive)");
                foreach (var kv in a.Origins)
                {
                    if (c.Regions.All(g => g.Id != kv.Key)) throw new ContentException("aliyah.yaml", $"{path}.origins.{kv.Key}", $"unknown region '{kv.Key}' (add it to regions with a name)");
                    var region = c.Regions.First(g => g.Id == kv.Key);
                    // A period's dates are inclusive, a region's window is [from, to): the period covers [a.From, a.To + 1 day).
                    if (!Window.Covers(region.From, region.To, a.From, a.To.AddDays(1)))
                        throw new ContentException("aliyah.yaml", $"{path}.origins.{kv.Key}", $"region '{kv.Key}' does not exist for the whole period (its window is {(region.From.HasValue ? region.From.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "the start")} to {(region.To.HasValue ? region.To.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "open")})");
                    Count("aliyah.yaml", $"{path}.origins.{kv.Key}", kv.Value);
                }
            }
            for (int i = 0; i < c.PopulationAnchors.Count; i++)
            {
                var a = c.PopulationAnchors[i]; var path = FormattableString.Invariant($"anchors[{i}]");
                if (a.Date == default) throw new ContentException("population.yaml", path + ".date", "required");
                Count("population.yaml", path + ".jews", a.Jews);
            }
            ValidateMap(c, settlementIds);
            ValidateWindows(c);
            EventRules.Validate(c, offices, indicators);
        }

        /// <summary>One or more eras, each with a name and a non-empty range; the first starts on rules.yaml start and
        /// each starts where the one before ended, so every date in range is in exactly one era.</summary>
        static void ValidateEras(Content c)
        {
            if (c.Eras.Count == 0) throw new ContentException("eras.yaml", "eras", "at least one era is required");
            for (int i = 0; i < c.Eras.Count; i++)
            {
                var e = c.Eras[i];
                var at = $"eras[{e.Id}]";
                Named("eras.yaml", at + ".name", e.Name);
                if (e.To == default) throw new ContentException("eras.yaml", at + ".to", "required (the first day after the era)");
                Period("eras.yaml", at, e.From, e.To);
                if (e.Systems == null) throw new ContentException("eras.yaml", at + ".systems", "must be a map of switches (leave it out for the defaults)");
                if (e.Systems.Legislature != EraSystems.State && e.Systems.Legislature != EraSystems.Assembly)
                    throw new ContentException("eras.yaml", at + ".systems.legislature", "must be state or assembly");
                foreach (var a in e.Systems.MapActions)
                    if (!MapActions.Actions.Contains(a)) throw new ContentException("eras.yaml", at + ".systems.map_actions", $"unknown map action '{a}' (transit_camp or housing)");
                if (e.Systems.MapActions.Distinct().Count() != e.Systems.MapActions.Count)
                    throw new ContentException("eras.yaml", at + ".systems.map_actions", "lists an action twice");
                if (e.Systems.Loop != EraSystems.Legacy && e.Systems.Loop != EraSystems.Gdd)
                    throw new ContentException("eras.yaml", at + ".systems.loop", "must be legacy or gdd");
                if (e.Systems.Loop != c.Eras[0].Systems.Loop)
                    throw new ContentException("eras.yaml", at + ".systems.loop", "every era runs the same loop (a save names one ruleset)");
                if (i == 0)
                {
                    if (c.Rules.Start != default && e.From != c.Rules.Start)   // a missing start is rules.yaml's own error
                        throw new ContentException("eras.yaml", at + ".from", FormattableString.Invariant($"must equal start in rules.yaml ({c.Rules.Start:yyyy-MM-dd})"));
                    continue;
                }
                var prev = c.Eras[i - 1];
                if (e.From < prev.To)
                    throw new ContentException("eras.yaml", at + ".from", FormattableString.Invariant($"overlaps {prev.Id} (ends {prev.To:yyyy-MM-dd})"));
                if (e.From > prev.To)
                    throw new ContentException("eras.yaml", at + ".from", FormattableString.Invariant($"leaves a gap after {prev.Id}: must equal its end ({prev.To:yyyy-MM-dd})"));
            }
        }

        /// <summary>Facilities a city may have (cities.yaml facilities): the GDD's training places (D-D5).</summary>
        static readonly string[] Facilities = { "military_camp", "hakhshara" };

        /// <summary>Where a unit type may train (unit_types.yaml facility): a start facility, or a city's completed industry
        /// project (<see cref="Training.Industry"/>; R-CRAFT-001), which no city starts with.</summary>
        static readonly string[] TrainingFacilities = Facilities.Concat(new[] { Training.Industry }).ToArray();

        /// <summary>A military type's sides (unit_types.yaml side): units_1948.yaml's side ids (Q-START-4, owner, 15 October 2026).</summary>
        static readonly string[] Sides = { "israel", "egypt", "jordan", "syria", "iraq", "lebanon", "ala", "local_arab_forces" };

        /// <summary>A military type's classes (unit_types.yaml class; GDD Phase 5 plan, PR 2).</summary>
        static readonly string[] Classes = { "infantry", "artillery", "armour" };

        static readonly string[] CityStatuses = { "sourced", "derived", "wikipedia-only", "design" };
        static readonly System.Text.RegularExpressions.Regex Month = new System.Text.RegularExpressions.Regex(@"^\d{4}-\d{2}\z");

        /// <summary>R-CITY-006: a city value is present and has a known status; a sourced, derived or wikipedia-only value also
        /// has sources, each with a url and a checked date, and the day (YYYY-MM-DD) or month (YYYY-MM) it describes.</summary>
        static CityValue CityValueOf(string file, string at, CityValue? v)
        {
            if (v == null) throw new ContentException(file, at, "required");
            if (v.Status == "") throw new ContentException(file, at + ".status", "required");
            if (!CityStatuses.Contains(v.Status))
                throw new ContentException(file, at + ".status", $"unknown status '{v.Status}' (sourced, derived, wikipedia-only or design)");
            for (int i = 0; i < v.Sources.Count; i++)
                if (v.Sources[i] == null || string.IsNullOrWhiteSpace(v.Sources[i].Url) || v.Sources[i].Checked == default)
                    throw new ContentException(file, FormattableString.Invariant($"{at}.sources[{i}]"), "needs a url and checked date");
            if (v.Date != "" && !ReviewRecord.IsDay(v.Date)
                && !(Month.IsMatch(v.Date) && DateTime.TryParseExact(v.Date, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)))
                throw new ContentException(file, at + ".date", "must be YYYY-MM-DD or YYYY-MM");
            if (v.Status == "design") return v;   // a design value may name what its method rests on, and needs no date
            if (v.Sources.Count == 0) throw new ContentException(file, at + ".sources", $"required: a {v.Status} value needs a source");
            if (v.Date == "") throw new ContentException(file, at + ".date", "required: the day or month the value describes");
            return v;
        }

        static void Whole(string file, string at, CityValue v)
        {
            if (v.Value < 0 || v.Value != decimal.Truncate(v.Value) || v.Value > int.MaxValue)
                throw new ContentException(file, at + ".value", "must be a whole number >= 0");
        }

        /// <summary>The GDD v0.1 city data and rules (Phase 1 PR 3), checked only when systems.loop is gdd.</summary>
        static void ValidateGdd(Content c, List<string> settlementIds)
        {
            const string cf = "cities.yaml", uf = "unit_types.yaml";
            Unique(cf, "cities", c.Cities.Select(x => x.Id));
            var known = new HashSet<string>(settlementIds);
            foreach (var x in c.Cities)
            {
                var at = $"cities[{x.Id}]";
                if (!known.Contains(x.Id)) throw new ContentException(cf, at + ".id", $"is not in {SettlementsFileName}");
                // R-CITY-006: every value is required (never read as 0) and says what it is.
                Whole(cf, at + ".people", CityValueOf(cf, at + ".people", x.People));
                var share = CityValueOf(cf, at + ".employed_share", x.EmployedShare);
                if (share.Value < 0 || share.Value > 1) throw new ContentException(cf, at + ".employed_share.value", "must be a number from 0 to 1");
                Whole(cf, at + ".housing_capacity", CityValueOf(cf, at + ".housing_capacity", x.Housing));
                Whole(cf, at + ".job_slots", CityValueOf(cf, at + ".job_slots", x.Jobs));
                foreach (var f in x.Facilities)
                    if (!Facilities.Contains(f)) throw new ContentException(cf, at + ".facilities", $"unknown facility '{f}' (military_camp or hakhshara)");
                // R-PROJ-005: a resource is a fact about the city, so it is sourced like a sourced city value.
                foreach (var r in x.Resources)
                {
                    var rat = $"{at}.resources[{r.Id}]";
                    if (string.IsNullOrWhiteSpace(r.Id)) throw new ContentException(cf, rat + ".id", "is required");
                    if (r.Sources.Count == 0) throw new ContentException(cf, rat + ".sources", "required: a resource needs a source");
                    for (int i = 0; i < r.Sources.Count; i++)
                        if (r.Sources[i] == null || string.IsNullOrWhiteSpace(r.Sources[i].Url) || r.Sources[i].Checked == default)
                            throw new ContentException(cf, FormattableString.Invariant($"{rat}.sources[{i}]"), "needs a url and checked date");
                }
            }
            Unique(uf, "types", c.UnitTypes.Select(x => x.Id));
            foreach (var x in c.UnitTypes)
            {
                var at = $"types[{x.Id}]";
                if (x.Kind != "population" && x.Kind != "unit") throw new ContentException(uf, at + ".kind", "must be population or unit");
                // R-UNIT-001: a type with a side or a class is military, and needs both; the sides are units_1948.yaml's.
                if (x.Side != "" || x.Class != "")
                {
                    if (x.Kind != "unit") throw new ContentException(uf, at + ".kind", "a military type is a unit");
                    if (x.Side == "") throw new ContentException(uf, at + ".side", "required for a military type");
                    if (x.Class == "") throw new ContentException(uf, at + ".class", "required for a military type");
                    if (!Sides.Contains(x.Side)) throw new ContentException(uf, at + ".side", $"unknown side '{x.Side}' ({string.Join(", ", Sides)})");
                    if (!Classes.Contains(x.Class)) throw new ContentException(uf, at + ".class", $"unknown class '{x.Class}' (infantry, artillery or armour)");
                }
                if (x.Facility == "")
                {
                    // Training (R-TRAIN-001) needs a place to train; values without one would be read by nothing.
                    if (x.TrainingTurns != 0) throw new ContentException(uf, at + ".training_turns", "only a type with a facility trains");
                    if (x.TrainingCost != 0) throw new ContentException(uf, at + ".training_cost", "only a type with a facility trains");
                    continue;
                }
                if (!TrainingFacilities.Contains(x.Facility)) throw new ContentException(uf, at + ".facility", $"unknown facility '{x.Facility}' (military_camp, hakhshara or industry)");
                // A trained count lands in City.Recruits or City.Craftsmen (R-TRAIN-003, R-CRAFT-001); other trained counts
                // wait for their decisions.
                if (x.Kind == "population" && x.Id != "recruit" && x.Id != Training.Craftsman)
                    throw new ContentException(uf, at + ".kind", "only recruit and craftsman are trained counts");
                // Q-CRAFT-1 (owner, 15 October 2026): a craftsman trains in a city with a completed industry project.
                if (x.Id == Training.Craftsman && x.Facility != Training.Industry)
                    throw new ContentException(uf, at + ".facility", "a craftsman trains at industry (Q-CRAFT-1)");
                if (x.TrainingTurns < 1) throw new ContentException(uf, at + ".training_turns", "must be >= 1");
                if (!(x.TrainingCost >= 0 && IsFinite(x.TrainingCost))) throw new ContentException(uf, at + ".training_cost", "must be a finite number >= 0");
            }
            // R-CITY-003: the arrivals are spread over the cities, so there must be one to take them.
            if (c.Cities.Count == 0) throw new ContentException(cf, "cities", "the GDD loop needs at least one city to take the arrivals");
            foreach (var (file, r) in new[] { ("rules.yaml", c.Rules) }.Concat(c.EraRules.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => ("rules/" + kv.Key + ".yaml", kv.Value))))
            {
                var m = r.Mvp ?? throw new ContentException(file, "mvp", "required when eras.yaml systems.loop is gdd");
                if (m.PeoplePerUnit < 1) throw new ContentException(file, "mvp.people_per_unit", "must be >= 1");
                // Arrivals are counted from the game's start in units of one size (R-CITY-003): another size in a later era
                // would lose or double-count units at its boundary.
                if (m.PeoplePerUnit != c.Rules.Mvp!.PeoplePerUnit)
                    throw new ContentException(file, "mvp.people_per_unit", "must be the same in every era (arrivals are counted in one unit size)");
                if (!(m.IncomePerJobPerYear >= 0 && IsFinite(m.IncomePerJobPerYear))) throw new ContentException(file, "mvp.income_per_job_per_year", "must be a finite number >= 0");
                // R-CRAFT-002 (GDD "Population and units": a craftsman "Generates more money than an ordinary civilian"): a missing key reads 0 and is refused.
                if (!(m.CraftsmanIncomeMultiple > 1 && IsFinite(m.CraftsmanIncomeMultiple)))
                    throw new ContentException(file, "mvp.craftsman_income_multiple", "must be a finite number > 1");
                // R-MONEY-002: explicit values keep an omitted kind from silently becoming free upkeep.
                foreach (var (key, v) in new[] {
                    ("city_upkeep_per_year", m.CityUpkeepPerYear), ("housing_upkeep_per_year", m.HousingUpkeepPerYear),
                    ("industry_upkeep_per_year", m.IndustryUpkeepPerYear), ("transit_camp_upkeep_per_year", m.TransitCampUpkeepPerYear),
                    ("military_camp_upkeep_per_year", m.MilitaryCampUpkeepPerYear), ("hakhshara_upkeep_per_year", m.HakhsharaUpkeepPerYear) })
                {
                    if (v == null) throw new ContentException(file, "mvp." + key, "required when eras.yaml systems.loop is gdd");
                    if (!(v >= 0 && IsFinite(v.Value))) throw new ContentException(file, "mvp." + key, "must be a finite number >= 0");
                }
                // R-CITY-003: the cities count arrivals from one day; another day in a later era would lose or double-count units.
                if (m.CityArrivalsFrom == default) throw new ContentException(file, "mvp.city_arrivals_from", "required when eras.yaml systems.loop is gdd");
                if (m.CityArrivalsFrom != c.Rules.Mvp!.CityArrivalsFrom) throw new ContentException(file, "mvp.city_arrivals_from", "must be the same in every era (the cities count arrivals from one day)");
                if (m.BuildYishuvTurns < 1) throw new ContentException(file, "mvp.build_yishuv_turns", "must be >= 1");
                if (!(m.BuildYishuvCost >= 0 && IsFinite(m.BuildYishuvCost))) throw new ContentException(file, "mvp.build_yishuv_cost", "must be a finite number >= 0");
                // R-CAMP-001: required, so a missing key is not read as 0 (a camp built at once, or for nothing).
                if (m.BuildCampTurns == null) throw new ContentException(file, "mvp.build_camp_turns", "required when eras.yaml systems.loop is gdd");
                if (m.BuildCampTurns < 1) throw new ContentException(file, "mvp.build_camp_turns", "must be >= 1");
                if (m.BuildCampCost == null) throw new ContentException(file, "mvp.build_camp_cost", "required when eras.yaml systems.loop is gdd");
                if (!(m.BuildCampCost >= 0 && IsFinite(m.BuildCampCost.Value))) throw new ContentException(file, "mvp.build_camp_cost", "must be a finite number >= 0");
                if (m.PlowFieldsTurns < 1) throw new ContentException(file, "mvp.plow_fields_turns", "must be >= 1");
                if (m.PlowFieldsCost == null) throw new ContentException(file, "mvp.plow_fields_cost", "required when eras.yaml systems.loop is gdd");
                if (!(m.PlowFieldsCost >= 0 && IsFinite(m.PlowFieldsCost.Value))) throw new ContentException(file, "mvp.plow_fields_cost", "must be a finite number >= 0");
                // R-SWAMP-001: required, so a missing key is not read as 0 (a swamp drained at once, or for nothing).
                if (m.DrySwampTurns == null) throw new ContentException(file, "mvp.dry_swamp_turns", "required when eras.yaml systems.loop is gdd");
                if (m.DrySwampTurns < 1) throw new ContentException(file, "mvp.dry_swamp_turns", "must be >= 1");
                if (m.DrySwampCost == null) throw new ContentException(file, "mvp.dry_swamp_cost", "required when eras.yaml systems.loop is gdd");
                if (!(m.DrySwampCost >= 0 && IsFinite(m.DrySwampCost.Value))) throw new ContentException(file, "mvp.dry_swamp_cost", "must be a finite number >= 0");
                if (m.FieldFoodPerTurn == null) throw new ContentException(file, "mvp.field_food_per_turn", "required when eras.yaml systems.loop is gdd");
                if (m.FieldFoodPerTurn < 0) throw new ContentException(file, "mvp.field_food_per_turn", "must be >= 0");
                // R-FOOD-001 to R-FOOD-003: required, so a missing key is never read as 0 (a stock term of 0, or no stock).
                if (m.KFood == null) throw new ContentException(file, "mvp.k_food", "required when eras.yaml systems.loop is gdd");
                if (!(m.KFood >= 0 && m.KFood <= 1)) throw new ContentException(file, "mvp.k_food", "must be between 0 and 1");
                if (m.CoverMonths == null) throw new ContentException(file, "mvp.cover_months", "required when eras.yaml systems.loop is gdd");
                if (m.CoverMonths < 1) throw new ContentException(file, "mvp.cover_months", "must be >= 1");
                // R-VET-001, R-VET-002: required, so a missing key is not read as 0 (a veteran after no work, or one that builds nothing).
                foreach (var (key, v) in new[] { ("veteran_halutz_service", m.VeteranHalutzService), ("veteran_progress", m.VeteranProgress) })
                {
                    if (v == null) throw new ContentException(file, "mvp." + key, "required when eras.yaml systems.loop is gdd");
                    if (v < 1) throw new ContentException(file, "mvp." + key, "must be >= 1");
                }
                // R-HOUSE-001, R-HOUSE-002: required, so a missing key is never read as a condition of 0 or no loss.
                if (m.HousingStartCondition == null) throw new ContentException(file, "mvp.housing_start_condition", "required when eras.yaml systems.loop is gdd");
                Unit(file, "mvp.housing_start_condition", m.HousingStartCondition.Value);
                if (m.OverflowConditionLoss == null) throw new ContentException(file, "mvp.overflow_condition_loss", "required when eras.yaml systems.loop is gdd");
                Unit(file, "mvp.overflow_condition_loss", m.OverflowConditionLoss.Value);
                // R-END-001, R-END-002: required, so a missing key is never read as 0 (a defeat with no debt, or on the first turn).
                if (m.DefeatTurns == null) throw new ContentException(file, "mvp.defeat_turns", "required when eras.yaml systems.loop is gdd");
                if (m.DefeatTurns < 1) throw new ContentException(file, "mvp.defeat_turns", "must be >= 1");
                if (m.DebtCeiling == null) throw new ContentException(file, "mvp.debt_ceiling", "required when eras.yaml systems.loop is gdd");
                if (!(m.DebtCeiling >= 0 && IsFinite(m.DebtCeiling.Value))) throw new ContentException(file, "mvp.debt_ceiling", "must be a finite number >= 0");
                // R-EVT-003 (#107): required, so a missing key is never read as 0 (no floor, or a disloyal minister who does nothing).
                if (m.DelegationLoyaltyFloor == null) throw new ContentException(file, "mvp.delegation_loyalty_floor", "required when eras.yaml systems.loop is gdd");
                if (m.DelegationLoyaltyFloor < 0 || m.DelegationLoyaltyFloor > 10) throw new ContentException(file, "mvp.delegation_loyalty_floor", "must be 0-10");
                if (m.DisloyalEffectShare == null) throw new ContentException(file, "mvp.disloyal_effect_share", "required when eras.yaml systems.loop is gdd");
                Unit(file, "mvp.disloyal_effect_share", m.DisloyalEffectShare.Value);
                if (c.Indicators.All(i => i.Id != Pools.FoodIndicator))
                    throw new ContentException(file, "mvp.k_food", "the Food stock feeds the food_supply indicator, which model.yaml must define");
                // One game has one last day (R-LOOP-004): an era cannot move it.
                if (m.End != c.Rules.Mvp!.End) throw new ContentException(file, "mvp.end", "must be the same in every era (the game has one end)");
                // R-OBJ-002. Required, so a missing key is not read as 0. Zero is allowed (a balance choice); a negative value
                // would take points away for finishing.
                foreach (var (key, v) in new[] { ("objective_points", m.ObjectivePoints), ("objective_bonus_per_month", m.ObjectiveBonusPerMonth), ("objective_bonus_cap", m.ObjectiveBonusCap) })
                {
                    if (v == null) throw new ContentException(file, "mvp." + key, "required when eras.yaml systems.loop is gdd");
                    if (v < 0) throw new ContentException(file, "mvp." + key, "must be >= 0");
                }
            }
            // R-CITY-007: the units the sim starts from, in the one unit size of every era (checked above).
            foreach (var x in c.Cities)
            {
                var ppu = c.Rules.Mvp!.PeoplePerUnit;
                x.Civilians = Cities.StartUnits(x.People!.Value, ppu);
                x.Employed = Cities.NearestUnit(x.People.Value * x.EmployedShare!.Value, ppu);
                x.HousingCapacity = (int)x.Housing!.Value;
                x.JobSlots = (int)x.Jobs!.Value;
                if (x.Employed > x.JobSlots)
                    throw new ContentException(cf, $"cities[{x.Id}].job_slots.value", FormattableString.Invariant($"is fewer than the {x.Employed} employed units of people times employed_share"));
            }
            // Strictly before the last era's end: an end on it is the era's own end, and the game would be over as era_ended.
            if (c.Rules.Mvp!.End is DateTime end && (end <= c.Rules.Start || end >= c.Eras[c.Eras.Count - 1].To))
                throw new ContentException("rules.yaml", "mvp.end", FormattableString.Invariant($"must be after start ({c.Rules.Start:yyyy-MM-dd}) and before the last era's end ({c.Eras[c.Eras.Count - 1].To:yyyy-MM-dd})"));
            ValidateSites(c);
            ValidateObjectiveSites(c);
            ValidateEventSites(c);
            ValidateProjects(c);
        }

        /// <summary>An event's site (R-SWAMP-002): a designated swamp site, whose works the event is about. Only the GDD loop
        /// reads sites.yaml, so only it can check this; the legacy loop ignores the key.</summary>
        static void ValidateEventSites(Content c)
        {
            foreach (var e in c.Events.Where(e => e.Site != ""))
            {
                var site = c.Sites.FirstOrDefault(x => x.Id == e.Site)
                    ?? throw new ContentException(e.File, $"events[{e.Id}].site", $"unknown site '{e.Site}' (sites.yaml)");
                if (!site.Kinds.Contains(Projects.SwampKind))
                    throw new ContentException(e.File, $"events[{e.Id}].site", $"site '{e.Site}' is not a swamp site (sites.yaml kinds)");
            }
        }

        /// <summary>The city project kinds (projects.yaml, R-PROJ-001): unique ids, a name, a cost and turns, and what each adds.
        /// The values are design values (Q-PROJ-1); the checks keep a typo from building a project that never finishes, costs
        /// less than nothing or moves a condition out of its 0 to 1 scale in one step.</summary>
        static void ValidateProjects(Content c)
        {
            const string pf = "projects.yaml";
            Unique(pf, "projects", c.ProjectKinds.Select(x => x.Id));
            foreach (var x in c.ProjectKinds)
            {
                var at = $"projects[{x.Id}]";
                if (string.IsNullOrWhiteSpace(x.Name)) throw new ContentException(pf, at + ".name", "is required");
                if (!(x.Cost >= 0 && !double.IsInfinity(x.Cost))) throw new ContentException(pf, at + ".cost", "must be a finite non-negative number");
                if (x.Turns < 1) throw new ContentException(pf, at + ".turns", "must be at least 1");
                if (x.Capacity < 0) throw new ContentException(pf, at + ".capacity", "must not be negative");
                if (x.JobSlots < 0) throw new ContentException(pf, at + ".job_slots", "must not be negative");
                if (!(x.Condition >= -1 && x.Condition <= 1)) throw new ContentException(pf, at + ".condition", "must be a number from -1 to 1");
                // R-PROJ-005: a kind whose resource no city has could never be built.
                if (!string.IsNullOrEmpty(x.Requires) && !c.Cities.Any(k => k.Resources.Any(r => r.Id == x.Requires)))
                    throw new ContentException(pf, at + ".requires", $"no city in cities.yaml has the resource '{x.Requires}'");
            }
        }

        /// <summary>An objective's site (R-OBJ-001): a designated site of kind yishuv, whose sourced founding date is the
        /// objective's reference date, so the card and the site cannot disagree. Only the GDD loop reads sites.yaml, so only
        /// it can check this; EventRules checks the rest of objectives.yaml in both loops.</summary>
        static void ValidateObjectiveSites(Content c)
        {
            foreach (var o in c.Objectives)
            {
                var at = $"objectives[{o.Id}]";
                if (o.CompletesWhen!.Absorbed != null) continue;   // R-OBJ-004 has no site; the GDD loop always has cities
                var id = o.CompletesWhen.SiteBuilt;   // EventRules requires one kind
                var site = c.Sites.FirstOrDefault(x => x.Id == id)
                    ?? throw new ContentException(ObjectivesFileName, at + ".completes_when.site_built", $"unknown site '{id}' (sites.yaml)");
                if (!site.Kinds.Contains(Projects.YishuvKind))
                    throw new ContentException(ObjectivesFileName, at + ".completes_when.site_built", $"site '{id}' cannot be built as a yishuv");
                if (o.ReferenceDate != site.Founded)
                    throw new ContentException(ObjectivesFileName, at + ".reference_date",
                        FormattableString.Invariant($"must be {site.Founded:yyyy-MM-dd}, the founding date of site '{id}' in sites.yaml"));
            }
        }

        /// <summary>What a designated site may be built as (sites.yaml kinds; R-SITE-004, the GDD's pioneer actions and D-D5's two
        /// facilities). A camp site is a military_camp or a hakhshara (Q-CAMP-3).</summary>
        static readonly string[] SiteKinds = { Projects.YishuvKind, "field", "swamp", "military_camp", "hakhshara" };
        static readonly string[] CampKinds = { "military_camp", "hakhshara" };

        /// <summary>The designated sites (sites.yaml, Phase 1 PR 5): sourced facts on a land hex that holds no 1948 settlement,
        /// founded inside the MVP's years (D-B1). The armistice line hexes are not checked: that file is display only, and a
        /// site may sit on a line hex (#378, question 2).</summary>
        static void ValidateSites(Content c)
        {
            const string sf = "sites.yaml";
            Unique(sf, "sites", c.Sites.Select(x => x.Id));
            var from = new DateTime(1948, 5, 15);
            var to = new DateTime(1952, 1, 1);
            var yishuvAt = new Dictionary<(int, int), string>();
            foreach (var x in c.Sites)
            {
                var at = $"sites[{x.Id}]";
                Named(sf, at + ".name", x.Name);
                Named(sf, at + ".name_he", x.NameHe);
                var hex = c.HexAt(x.Q, x.R);
                if (hex == null || !hex.Land)
                    throw new ContentException(sf, at + ".q", FormattableString.Invariant($"hex q={x.Q} r={x.R} is not a land hex in {HexesFile}"));
                var settled = c.SettlementHexes.FirstOrDefault(h => h.Q == x.Q && h.R == x.R);
                if (settled != null)
                    throw new ContentException(sf, at + ".q", FormattableString.Invariant($"hex q={x.Q} r={x.R} holds the 1948 settlement {settled.Id} ({SettlementHexesFileName})"));
                if (x.Kinds.Count == 0) throw new ContentException(sf, at + ".kinds", "required: one or more of " + string.Join(", ", SiteKinds));
                foreach (var k in x.Kinds)
                    if (!SiteKinds.Contains(k)) throw new ContentException(sf, at + ".kinds", $"unknown kind '{k}' ({string.Join(", ", SiteKinds)})");
                // R-FIELD-001: a field site's built record is its plowed state, so a built yishuv on it would yield Food. Only that
                // pair is refused; other kinds may share a field site.
                if (x.Kinds.Contains(Projects.FieldKind) && x.Kinds.Contains(Projects.YishuvKind))
                    throw new ContentException(sf, at + ".kinds", "a field site cannot also be a yishuv site (its built record is its plowed state)");
                // R-SWAMP-001 (Q-SWAMP-1): the same for a swamp, which drained gives a field site in its hex, to be plowed.
                if (x.Kinds.Contains(Projects.FieldKind) && x.Kinds.Contains(Projects.SwampKind))
                    throw new ContentException(sf, at + ".kinds", "a field site cannot also be a swamp site (its built record is its plowed state)");
                // R-SWAMP-001: nor a yishuv or camp site, whose build would write the same record and so drain the swamp.
                if (x.Kinds.Contains(Projects.SwampKind))
                    foreach (var k in CampKinds.Prepend(Projects.YishuvKind))
                        if (x.Kinds.Contains(k))
                            throw new ContentException(sf, at + ".kinds", $"a swamp site cannot also be a {k} site (its built record is its drained state)");
                if (x.Kinds.Contains(Projects.YishuvKind))
                {
                    // R-SITE-005 (Q-SITE-3): one yishuv per hex, so SettlementPlacer keeps one kit per hex; other kinds may share it.
                    if (yishuvAt.TryGetValue((x.Q, x.R), out var first))
                        throw new ContentException(sf, at + ".q", FormattableString.Invariant($"hex q={x.Q} r={x.R} already holds the yishuv site {first}"));
                    yishuvAt[(x.Q, x.R)] = x.Id;
                    if (x.Founded < from || x.Founded >= to)
                        throw new ContentException(sf, at + ".founded", "required, between 15 May 1948 and 31 December 1951 (the MVP's years, D-B1)");
                }
                else if (x.Founded != default)
                    throw new ContentException(sf, at + ".founded", "only a yishuv site has a founding date");
                // Q-CAMP-1: a camp belongs to one city, which a built camp gives the facility to.
                if (x.Kinds.Any(k => CampKinds.Contains(k)))
                {
                    if (string.IsNullOrWhiteSpace(x.City)) throw new ContentException(sf, at + ".city", "required for a camp site (military_camp, hakhshara)");
                    if (!c.Cities.Any(y => y.Id == x.City)) throw new ContentException(sf, at + ".city", $"unknown city '{x.City}' (cities.yaml)");
                }
                else if (x.City != "")
                    throw new ContentException(sf, at + ".city", "only a camp site has a city");
                if (x.Sources.Count == 0) throw new ContentException(sf, at + ".sources", "required: a designated site is a historical fact");
                for (int i = 0; i < x.Sources.Count; i++)
                    if (x.Sources[i] == null || string.IsNullOrWhiteSpace(x.Sources[i].Url) || x.Sources[i].Checked == default)
                        throw new ContentException(sf, FormattableString.Invariant($"{at}.sources[{i}]"), "needs a url and checked date");
                Status(sf, at + ".status", x.Status);
                Review(sf, at, x.ReviewedBy, x.ReviewedByGiven, x.ReviewedOn, x.ReviewedOnGiven);
            }
        }

        static void ValidateMap(Content c, List<string> settlementIds)
        {
            if (c.Hexes.Count == 0) throw new ContentException(HexesFile, "hexes", "no hexes");
            var seen = new HashSet<(int, int)>();
            foreach (var h in c.Hexes)
            {
                var at = FormattableString.Invariant($"hexes[q={h.Q},r={h.R}]");
                if (!seen.Add((h.Q, h.R))) throw new ContentException(HexesFile, at, "duplicate hex");
                if (h.Biome == "") throw new ContentException(HexesFile, at + ".biome", "required");
            }
            Unique(SettlementHexesFileName, "settlements", c.SettlementHexes.Select(s => s.Id));
            Unique(SettlementsFileName, "settlements", settlementIds);
            var known = new HashSet<string>(settlementIds);
            var placed = new HashSet<string>(c.SettlementHexes.Select(s => s.Id));
            foreach (var s in c.SettlementHexes)
                if (!known.Contains(s.Id))
                    throw new ContentException(SettlementHexesFileName, $"settlements[{s.Id}]", $"is not in {SettlementsFileName}");
            foreach (var id in settlementIds)
                if (!placed.Contains(id))
                    throw new ContentException(SettlementsFileName, $"settlements[{id}]", $"has no hex in {SettlementHexesFileName}");
            foreach (var s in c.SettlementHexes)
            {
                var hex = c.HexAt(s.Q, s.R);
                if (hex == null || !hex.Land)
                    throw new ContentException(SettlementHexesFileName, $"settlements[{s.Id}]", FormattableString.Invariant($"hex q={s.Q} r={s.R} is not a land hex in {HexesFile}"));
            }
        }
    }
}
