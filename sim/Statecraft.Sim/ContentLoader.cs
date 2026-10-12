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

        public const string HexesFile = "map/hexes.json";
        public const string SettlementHexesFileName = "map/settlement_hexes.yaml";
        public const string SettlementsFileName = "map/settlements_1948.yaml";

        /// <summary>The ids of settlements_1948.yaml, which the tools own (its other keys are not the sim's business), so it is
        /// read as plain maps rather than through the strict content types.</summary>
        static List<string> ReadSettlementIds(Func<string, string?> readFile)
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
                return rows.Select((r, i) => r.TryGetValue("id", out var id) && id is string sid && sid != ""
                    ? sid : throw new ContentException(SettlementsFileName, $"settlements[{i}].id", "required")).ToList();
            }
            catch (YamlException e) { throw new ContentException(SettlementsFileName, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
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
            var settlementIds = ReadSettlementIds(readFile);
            // Event files (content/events/*.yaml), one GameEvent each. Keys, for content authors:
            //   id, title, text            text may name office holders only through {office_id} placeholders, which
            //                              must have a holder on every day of the window
            //   anchor: hard | soft        hard needs a historical option; in Historical mode it resolves to it
            //   tags: [...]                holocaust, terror, massacre, war-casualties are sensitive (no money reward)
            //   office                     the minister the event is delegated to
            //   from, to                   the window [from, to); it fires on the first turn that overlaps it
            //   conditions: [{ indicator, min, max }]   indicator bounds that must hold when it fires
            //   requires: { event, option }   fires only once that other event was resolved (chosen or delegated)
            //                              with that option; both ids must exist, an event cannot require itself and
            //                              requirements cannot form a cycle (EventRules)
            //   options: [{ id, text, historical, recommended, effects: [{ target, delta }], set_rung, set_policy }]
            //   sources: [{ url, checked }], status
            foreach (var rel in eventFiles.OrderBy(p => p, StringComparer.Ordinal))
            {
                var e = Read<GameEvent>(readFile, rel);
                e.File = rel;
                c.Events.Add(e);
            }
            Validate(c, settlementIds);
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
        static void Named(string file, string field, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ContentException(file, field, "required (the name shown to the player)");
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
                if (!r.AttentionCost.ContainsKey(k)) throw new ContentException(file, $"attention_cost.{k}", "missing cost");
            if (r.RungMinMobilization.Count != 5) throw new ContentException(file, "rung_min_mobilization", "needs exactly 5 values (rungs 1-5)");
            for (int i = 0; i < 5; i++)
            {
                var field = FormattableString.Invariant($"rung_min_mobilization[{i}]");
                Unit(file, field, r.RungMinMobilization[i]);
                if (i > 0 && r.RungMinMobilization[i] < r.RungMinMobilization[i - 1])
                    throw new ContentException(file, field, "must not be below the previous rung's floor");
            }
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

        static void Validate(Content c, List<string> settlementIds)
        {
            var offices = new HashSet<string>(c.Offices.Select(o => o.Id));
            var historicalOffices = new HashSet<string>(c.Offices.Where(o => o.HistoricalOnly).Select(o => o.Id));
            var parties = new HashSet<string>(c.Parties.Select(p => p.Id));
            var indicators = new HashSet<string>(c.Indicators.Select(i => i.Id));
            var sources = new HashSet<string>(c.Policies.Select(p => p.Id).Concat(indicators));
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

            foreach (var p in c.People) Status("people.yaml", $"people[{p.Id}].status", p.Status);
            foreach (var l in c.Legislatures) Status("legislatures.yaml", $"legislatures[{l.Id}].status", l.Status);
            foreach (var g in c.Governments) Status("governments.yaml", $"governments[{g.Id}].status", g.Status);
            for (int i = 0; i < c.Aliyah.Count; i++) Status("aliyah.yaml", FormattableString.Invariant($"series[{i}].status"), c.Aliyah[i].Status);
            for (int i = 0; i < c.PopulationAnchors.Count; i++) Status("population.yaml", FormattableString.Invariant($"anchors[{i}].status"), c.PopulationAnchors[i].Status);

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
                    if (kv.Key != "name_he" && kv.Key != "born" && kv.Key != "died")
                        throw new ContentException("people.yaml", field, "unknown field (must be name_he, born or died)");
                    if (kv.Value == null) throw new ContentException("people.yaml", field, "must be a list");
                    for (int i = 0; i < kv.Value.Count; i++)
                        if (kv.Value[i] == null || string.IsNullOrWhiteSpace(kv.Value[i].Url) || kv.Value[i].Checked == default)
                            throw new ContentException("people.yaml", FormattableString.Invariant($"{field}[{i}]"), "needs a url and a checked date");
                }
                foreach (var t in p.Offices)
                {
                    if (string.IsNullOrEmpty(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices", "office term without an office");
                    if (!offices.Contains(t.Office)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", "unknown office");
                    if (t.From == default) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.from", "required");
                    if (t.Party != "" && !parties.Contains(t.Party)) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}.party", $"unknown party '{t.Party}'");
                    if (t.To.HasValue && t.To.Value <= t.From) throw new ContentException("people.yaml", $"people[{p.Id}].offices.{t.Office}", FormattableString.Invariant($"term ends ({t.To:yyyy-MM-dd}) before it starts ({t.From:yyyy-MM-dd})"));
                }
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
            for (int i = 0; i < c.Aliyah.Count; i++)
            {
                var a = c.Aliyah[i]; var path = FormattableString.Invariant($"series[{i}]");
                if (a.From == default) throw new ContentException("aliyah.yaml", path + ".from", "required");
                if (a.To < a.From) throw new ContentException("aliyah.yaml", path + ".to", "required, on or after from (both dates are inclusive)");
                foreach (var kv in a.Origins)
                {
                    if (c.Regions.All(g => g.Id != kv.Key)) throw new ContentException("aliyah.yaml", $"{path}.origins.{kv.Key}", $"unknown region '{kv.Key}' (add it to regions with a name)");
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
