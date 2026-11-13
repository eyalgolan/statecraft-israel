using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    /// <summary>One unit a Historical game starts with (R-UNIT-006): the units_1948.yaml row it comes from, the anchor it
    /// stands at, its type, side and strength in population units, and the anchor's hex.</summary>
    public sealed class StartingForce
    {
        public string Row { get; set; } = "";
        /// <summary>Player-facing names from the same units_1948.yaml row, used when a sim report names a required brigade.</summary>
        public string Name { get; set; } = "";
        /// <summary>The anchor's place, as the row names it.</summary>
        public string Anchor { get; set; } = "";
        public string Type { get; set; } = "";
        public string Side { get; set; } = "";
        public int Strength { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        /// <summary>The row's review status (draft, to_verify, fact-checked); the release gate reads it (Q-START-3).</summary>
        public string Status { get; set; } = "";
        /// <summary>A fact-checked row's `checked_on`, the day of its person check ('YYYY-MM-DD'); empty for any other row.
        /// The release gate counts a field sourced after it as unchecked (#2722).</summary>
        public string CheckedOn { get; set; } = "";
        /// <summary>The latest `checked` day among the sources of the row's strength, each anchor and each order target, in
        /// the file's order, with the field's name (#2722): "strength", "anchors[k] (place)", "order_targets[k] (place)".</summary>
        public List<(string Field, string Checked)> SourcesChecked { get; } = new List<(string Field, string Checked)>();
        /// <summary>R-UNIT-009 (#1241): the day a row dated after the start arrives (its strength's as_of), or null for a row
        /// that starts the game. Its unit's id is reserved at the start and the resolver adds it on that day's turn.</summary>
        public DateTime? Arrives { get; set; }
        /// <summary>The enemy AI's script (R-ENEMY-002; Q-ENEMY-1; #1270): the hex and window end (`to`) of each of the row's
        /// anchors from 11 June 1948 on two sources, in the file's order. Empty when the row has none.</summary>
        public List<(int Q, int R, DateTime Until)> Script { get; } = new List<(int Q, int R, DateTime Until)>();
    }

    /// <summary>The sourced starting forces, both sides (GDD Phase 5 PR 4; docs/rules/mvp.md R-UNIT-006, R-UNIT-007).
    /// The sim reads map/units_1948.yaml (Q-START-1, owner, 15 October 2026), so the file is in the content hash. Only the
    /// keys the start needs are read, untyped, as for settlements_1948.yaml: the map's reader (Unity's UnitData) and
    /// tools/tests/test_units_1948.py own the rest of the row.</summary>
    public static class StartingForces
    {
        public const string FileName = "map/units_1948.yaml";
        /// <summary>Each anchor's hex, written by tools/unit_anchor_hexes.py with the map's projection and rounding
        /// (tools/hexgrid.py, Unity's HexPicker), so the sim needs no trigonometry (docs/determinism.md).</summary>
        public const string HexesFileName = "map/unit_anchor_hexes.yaml";
        /// <summary>Every IDF brigade starts as a veteran soldier (#743, owner, 20 October 2026).</summary>
        public const string IsraeliBrigadeType = "veteran_soldier";

        /// <summary>The starting forces in the file's order (R-UNIT-006): every row that is not hidden, is an army row (the
        /// squadron and the ships stay tokens, Q-UNIT-5), has a strength for the start day and an anchor window
        /// [from, to) covering it. The first such anchor places the unit on its hex in unit_anchor_hexes.yaml (the hex holding
        /// its coordinates, precomputed by tools/unit_anchor_hexes.py). Strength is the row's people in population units,
        /// the nearest unit with a half up and at least 1 (#743). A row it cannot place is a load error.</summary>
        internal static List<StartingForce> Read(Func<string, string?> readFile, Content c)
        {
            var rows = Rows(readFile, FileName, "units");
            var hexes = Hexes(readFile, rows, "anchors", "anchor");
            c.UnitAnchorHexes = hexes;   // R-ENEMY-005: the operation cards' orders name these
            foreach (var row in rows)    // and end with their windows; EventRules refuses an order to an anchor with no end
                if (row != null && row.TryGetValue("anchors", out var ao) && ao is List<object> windows)
                    for (int k = 0; k < windows.Count; k++)
                        if (windows[k] is Dictionary<object, object> a && DateTime.TryParseExact(Text(a, "to"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                            c.UnitAnchorEnds[(Text(row, "id"), k)] = to;
            var targetHexes = Hexes(readFile, rows, "order_targets", "target");
            OrderTargets(c, rows, targetHexes);
            OrderPlaces(c, rows, hexes, targetHexes);
            var start = c.Rules.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var ppu = c.RulesAt(c.Rules.Start).Mvp!.PeoplePerUnit;   // the start day's rules, as Cities.Init reads them
            var list = new List<StartingForce>();
            var arrivals = new List<StartingForce>();   // R-UNIT-009: after every starting row, so unit i is still row i
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ContentException(FileName, $"units[{i}]", "empty row");
                var id = Text(row, "id");
                if (id == "") throw new ContentException(FileName, $"units[{i}].id", "required");
                var at = $"units[{id}]";
                // #2722: a fact-checked row records the day of its person check, so a field sourced later can be told apart.
                if (Text(row, "status") == "fact-checked"
                    && !DateTime.TryParseExact(Text(row, "checked_on"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    throw new ContentException(FileName, $"{at}.checked_on", "required on a fact-checked row, the day of its person check 'YYYY-MM-DD'");
                if (row.ContainsKey("hidden") || Text(row, "service") != "army") continue;
                if (!(row.TryGetValue("strength", out var so) && so is Dictionary<object, object> strength)) continue;
                // R-UNIT-009: a strength dated after the start is the row's arrival day; one dated before starts nothing.
                var day = Text(strength, "as_of");
                if (!DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
                    throw new ContentException(FileName, $"{at}.strength.as_of", "required, a date 'YYYY-MM-DD'");
                if (asOf < c.Rules.Start) continue;
                if (!(row.TryGetValue("anchors", out var ao) && ao is List<object> anchors)) continue;
                int k = anchors.FindIndex(a => a is Dictionary<object, object> m
                    && string.CompareOrdinal(Text(m, "from"), day) <= 0 && string.CompareOrdinal(day, Text(m, "to")) < 0);
                if (k < 0) continue;
                var anchor = (Dictionary<object, object>)anchors[k];
                var side = Text(row, "side");
                var people = Number(strength, "value", $"{at}.strength.value");
                if (people < 1 || people != decimal.Truncate(people)) throw new ContentException(FileName, $"{at}.strength.value", "must be a whole number >= 1");
                var units = Cities.StartUnits(people, ppu);   // the nearest unit, a half up, at least 1 (people is >= 1 here)
                if (asOf > c.Rules.Start && side != Movement.PlayerSide)
                    throw new ContentException(FileName, $"{at}.strength.as_of", "an enemy-side force may not arrive after the game starts (R-ENEMY-004)");
                if (!hexes.TryGetValue((id, k), out var qr))
                    throw new ContentException(HexesFileName, "anchors", $"no hex for {at}.anchors[{k}] ({FileName}); rerun tools/unit_anchor_hexes.py");
                var (q, r) = qr;
                var hex = c.HexAt(q, r);
                if (hex == null) throw new ContentException(FileName, $"{at}.anchors[{k}]", FormattableString.Invariant($"hex q={q} r={r} is not on the map ({ContentLoader.HexesFile})"));
                if (!hex.Land) throw new ContentException(FileName, $"{at}.anchors[{k}]", FormattableString.Invariant($"hex q={q} r={r} is not a land hex in {ContentLoader.HexesFile}"));
                var force = new StartingForce
                {
                    Row = id, Name = Text(row, "name"), Anchor = Text(anchor, "place"), Type = TypeFor(c, side, at), Side = side,
                    Strength = units, Q = q, R = r, Status = Text(row, "status"), Arrives = day == start ? (DateTime?)null : asOf,
                    CheckedOn = Text(row, "checked_on"),
                };
                force.SourcesChecked.Add(("strength", LatestCheck(strength)));
                for (int j = 0; j < anchors.Count; j++)
                    if (anchors[j] is Dictionary<object, object> a) force.SourcesChecked.Add((FormattableString.Invariant($"anchors[{j}] ({Text(a, "place")})"), LatestCheck(a)));
                if (row.TryGetValue("order_targets", out var oo) && oo is List<object> targets)
                    for (int j = 0; j < targets.Count; j++)
                        if (targets[j] is Dictionary<object, object> t) force.SourcesChecked.Add((FormattableString.Invariant($"order_targets[{j}] ({Text(t, "place")})"), LatestCheck(t)));
                if (side != Movement.PlayerSide) Script(c, force, anchors, id, at, hexes);
                (day == start ? list : arrivals).Add(force);
            }
            list.AddRange(arrivals);
            return list;
        }

        /// <summary>R-ENEMY-002 (Q-ENEMY-1: the 15 May and 11 June 1948 positions two sources agree on, arab-forces-1948.md
        /// section 10; #1270 past 8 July 1948). The 15 May position is the start; the script is every anchor of the row from
        /// 11 June that cites at least two sources other than Wikipedia and Wikidata (which the file uses for coordinates
        /// only), on that anchor's hex, in the file's order. A row with none has no script and its unit follows the goals.</summary>
        static void Script(Content c, StartingForce force, List<object> anchors, string id, string at,
            Dictionary<(string id, int anchor), (int q, int r)> hexes)
        {
            for (int j = 0; j < anchors.Count; j++)
            {
                if (!(anchors[j] is Dictionary<object, object> a) || string.CompareOrdinal(Text(a, "from"), EnemyAI.ScriptDate) < 0) continue;
                // Distinct URLs, so one source listed twice counts once; hosts are case-insensitive, so EN.WIKIPEDIA.ORG is Wikipedia.
                var sourced = a.TryGetValue("sources", out var so) && so is List<object> sources ? sources
                    .Select(x => x is Dictionary<object, object> m ? Text(m, "url") : "")
                    .Where(url => url != "" && !url.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase)
                        && !url.Contains("wikidata.org", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.Ordinal).Count() : 0;
                if (sourced < 2) continue;
                if (!hexes.TryGetValue((id, j), out var qr))
                    throw new ContentException(HexesFileName, "anchors", $"no hex for {at}.anchors[{j}] ({FileName}); rerun tools/unit_anchor_hexes.py");
                if (!(c.HexAt(qr.q, qr.r) is MapHex hex) || !hex.Land)
                    throw new ContentException(FileName, $"{at}.anchors[{j}]", FormattableString.Invariant($"hex q={qr.q} r={qr.r} is not a land hex in {ContentLoader.HexesFile}"));
                if (!DateTime.TryParseExact(Text(a, "to"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until))
                    throw new ContentException(FileName, $"{at}.anchors[{j}].to", "required, a date 'YYYY-MM-DD'");
                force.Script.Add((qr.q, qr.r, until));
            }
        }

        /// <summary>Adds the starting forces to a new game, in the content's order, taking the next ids: the unit with id i
        /// of a new game is <c>c.StartingForces[i]</c>. An arriving row's id is taken too, so it is free when it arrives
        /// (R-UNIT-009). With an initialized gdd-0.3 ledger (the dark start, #1725 PR 5), each own starting formation's
        /// strength must be its active-soldier records serving in it (R-POP-004), or nothing is added. Any other initialized
        /// ledger (synthetic or Open) is refused, with nothing added (R-POP-002).</summary>
        public static void Init(Content c, GameState s)
        {
            if (s.PopulationLedger.IsInitialized) { PopulationLedger.RefuseUnlessHistorical(s); RequireLedgerSoldiers(c, s); }
            foreach (var f in c.StartingForces)
            {
                var id = s.NextId++;
                if (f.Arrives == null) s.Units.Add(id, new Unit(id, f.Type, f.Q, f.R, side: f.Side, strength: f.Strength));
            }
        }

        /// <summary>R-POP-004: an own formation in the start inventory gives its strength as active soldiers whose service
        /// target is its source key (HistoricalPopulation.FormationPrefix plus its row), so the formations the game places and
        /// the ledger's soldiers are the same people.</summary>
        static void RequireLedgerSoldiers(Content c, GameState s)
        {
            var serving = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target != null)
                .GroupBy(p => p.Assignment.Target!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            foreach (var f in HistoricalPopulation.OpeningFormations(c))
            {
                var key = HistoricalPopulation.FormationPrefix + f.Row;
                if ((serving.TryGetValue(key, out var n) ? n : 0) != f.Strength)
                    throw new InvalidOperationException($"Starting formation '{f.Row}' has strength {f.Strength} but the population ledger serves it with {n} (R-POP-004).");
            }
        }

        /// <summary>R-UNIT-009 (#1241, owner, 21 October 2026, answer 1(a)): resolver step 9b. A row dated after the start
        /// arrives in the resolution of the turn from s.Date to <paramref name="next"/> that reaches its day, once, as unit
        /// <c>c.StartingForces[i]</c>'s id i, on its anchor's hex with its strength. Like a starting force it ignores the stack
        /// limit and what else stands on the hex.
        /// With an initialized Historical ledger (#1725 PR 7, producer 08) the brigade also brings its strength as new
        /// active-soldier records serving in it (<see cref="HistoricalPopulation.ArrivalRows"/>; owner, 8 November 2026:
        /// "Soldiers of brigade (Recommended)", "New people (Recommended)"), registered all or nothing before its unit is added.
        /// A brigade whose records are already registered adds nothing, so a replay of the turn adds nobody twice (coordinator
        /// call, 8 November 2026, Q7 (a)); its records are registered together, so its first ordinal marks the arrival. A
        /// synthetic or Open ledger is still refused (R-POP-002).</summary>
        public static void Arrive(Content c, GameState s, DateTime next)
        {
            PopulationLedger.RefuseUnlessHistorical(s);   // #1725 PR 7: a Historical ledger's records are then the soldiers
            bool Due(StartingForce f) => f.Arrives is DateTime day && s.Date < day && day <= next;
            // Every due force is checked before any is added, so a refusal leaves the state unchanged.
            foreach (var f in c.StartingForces)
                if (Due(f) && f.Side != Movement.PlayerSide)
                    throw new InvalidOperationException($"Enemy-side starting force '{f.Row}' may not arrive after the game starts (R-ENEMY-004)");
            var ledger = s.PopulationLedger.IsInitialized ? s.PopulationLedger : null;
            var arriving = new List<int>();
            var rows = new List<PopulationInventoryRow>();
            for (int i = 0; i < c.StartingForces.Count; i++)
            {
                var f = c.StartingForces[i];
                if (!Due(f)) continue;
                if (ledger != null)
                {
                    if (ledger.HasArrived(HistoricalPopulation.FormationPrefix + f.Row, 0)) continue;
                    rows.AddRange(HistoricalPopulation.ArrivalRows(c, f));
                }
                arriving.Add(i);
            }
            // All or nothing: an id already taken would refuse at Add, so it is refused before the records are registered.
            foreach (var i in arriving)
                if (s.Units.ContainsKey(i))
                    throw new InvalidOperationException($"Unit {i} of starting force '{c.StartingForces[i].Row}' is already in the game (R-UNIT-009)");
            if (ledger != null && rows.Count > 0) ledger.RegisterArrivals(rows);
            foreach (var i in arriving)
            {
                var f = c.StartingForces[i];
                s.Units.Add(i, new Unit(i, f.Type, f.Q, f.R, side: f.Side, strength: f.Strength));
            }
        }

        /// <summary>A starting row's unit type: Israel's brigade type, or the one military type of the row's side (one
        /// labelled design-value infantry type per Arab side with a row; #743, owner, 20 October 2026).</summary>
        static string TypeFor(Content c, string side, string at)
        {
            var types = side == "israel"
                ? c.UnitTypes.Where(t => t.Id == IsraeliBrigadeType && t.Side == side).ToList()
                : c.UnitTypes.Where(t => t.Side == side && side != "").ToList();
            if (types.Count != 1)
                throw new ContentException(FileName, at + ".side", types.Count == 0
                    ? $"no military type of side '{side}' in unit_types.yaml" + (side == "israel" ? $" (the brigades start as {IsraeliBrigadeType})" : "")
                    : $"side '{side}' has several military types in unit_types.yaml ({string.Join(", ", types.Select(t => t.Id))}); a starting row needs one");
            return types[0].Id;
        }

        /// <summary>R-ENEMY-005 (owner, 2 November 2026, #2087, option 1): each row's `order_targets`, where an operation card may
        /// send its unit, with the hex tools/unit_anchor_hexes.py gives it and the `to` that ends the card's order. Never a
        /// start and never drawn, but sourced and dated as an anchor: one with no sources or no from/to date is a load error.</summary>
        static void OrderTargets(Content c, List<Dictionary<object, object>> rows, Dictionary<(string id, int anchor), (int q, int r)> hexes)
        {
            foreach (var row in rows)
            {
                if (row == null || !(row.TryGetValue("order_targets", out var to) && to is List<object> targets)) continue;
                var id = Text(row, "id");
                for (int k = 0; k < targets.Count; k++)
                {
                    var at = FormattableString.Invariant($"units[{id}].order_targets[{k}]");
                    if (!(targets[k] is Dictionary<object, object> t)) throw new ContentException(FileName, at, "empty order target");
                    if (!(t.TryGetValue("sources", out var so) && so is List<object> sources && sources.Count > 0))
                        throw new ContentException(FileName, at + ".sources", "required, at least one source");
                    if (!DateTime.TryParseExact(Text(t, "from"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        throw new ContentException(FileName, at + ".from", "required, a date 'YYYY-MM-DD'");
                    if (!DateTime.TryParseExact(Text(t, "to"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until))
                        throw new ContentException(FileName, at + ".to", "required, a date 'YYYY-MM-DD'");
                    if (!hexes.TryGetValue((id, k), out var qr))
                        throw new ContentException(HexesFileName, "order_targets", $"no hex for {at} ({FileName}); rerun tools/unit_anchor_hexes.py");
                    c.UnitOrderTargets[(id, k)] = (qr.q, qr.r, until);
                }
            }
        }

        /// <summary>#2705: the place each row's order target or anchor names (`place`) by the row and its hex, the order targets
        /// first, so an event card can say where its unit order sends the unit (R-EVT-005). Presentation only; the Hebrew
        /// `place_he` stays Unity's to read (HebrewOverlay.UnityReads).</summary>
        static void OrderPlaces(Content c, List<Dictionary<object, object>> rows, Dictionary<(string id, int anchor), (int q, int r)> anchorHexes,
            Dictionary<(string id, int anchor), (int q, int r)> targetHexes)
        {
            foreach (var row in rows)
            {
                if (row == null) continue;
                var id = Text(row, "id");
                foreach (var (key, hexes) in new[] { ("order_targets", targetHexes), ("anchors", anchorHexes) })
                    if (row.TryGetValue(key, out var lo) && lo is List<object> places)
                        for (int k = 0; k < places.Count; k++)
                            if (places[k] is Dictionary<object, object> p && hexes.TryGetValue((id, k), out var qr)
                                && !c.UnitOrderPlaces.ContainsKey((id, qr.q, qr.r)))
                                c.UnitOrderPlaces[(id, qr.q, qr.r)] = Text(p, "place");
            }
        }

        /// <summary>A file's list under <paramref name="key"/>, read untyped (only the keys the start needs are read). An
        /// <paramref name="optional"/> key that is absent reads as an empty list.</summary>
        static List<Dictionary<object, object>> Rows(Func<string, string?> readFile, string fileName, string key, bool optional = false)
        {
            string? text;
            try { text = readFile(fileName); }
            catch (System.IO.IOException e) { throw new ContentException(fileName, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(fileName, "(file)", e.Message); }
            if (text == null) throw new ContentException(fileName, "(file)", "missing file");
            Dictionary<string, List<Dictionary<object, object>>>? file;
            try { file = new DeserializerBuilder().Build().Deserialize<Dictionary<string, List<Dictionary<object, object>>>>(text); }
            catch (YamlException e) { throw new ContentException(fileName, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            if (file != null && file.TryGetValue(key, out var rows) && rows != null) return rows;
            if (optional && (file == null || !file.ContainsKey(key))) return new List<Dictionary<object, object>>();
            throw new ContentException(fileName, key, "required");
        }

        /// <summary>unit_anchor_hexes.yaml's <paramref name="key"/> list (anchors, or order_targets, R-ENEMY-005) by (row id,
        /// index). Every row must name one entry of <paramref name="units"/>' list under the same key, once: a row that does
        /// not means the file is stale (the tool was not rerun after units_1948.yaml changed).</summary>
        static Dictionary<(string id, int anchor), (int q, int r)> Hexes(Func<string, string?> readFile, List<Dictionary<object, object>> units, string key, string index)
        {
            var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var u in units)
                if (u != null) anchors[Text(u, "id")] = u.TryGetValue(key, out var ao) && ao is List<object> list ? list.Count : 0;
            var hexes = new Dictionary<(string, int), (int, int)>();
            var rows = Rows(readFile, HexesFileName, key, optional: key != "anchors");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ContentException(HexesFileName, $"{key}[{i}]", "empty row");
                var id = Text(row, "id");
                var at = id == "" ? $"{key}[{i}]" : $"{key}[{id}]";
                int k = Int(row, index, at), q = Int(row, "q", at), r = Int(row, "r", at);
                if (!anchors.TryGetValue(id, out var n) || k < 0 || k >= n)
                    throw new ContentException(HexesFileName, at, FormattableString.Invariant($"no {index} {k} of a row '{id}' in {FileName}; rerun tools/unit_anchor_hexes.py"));
                if (!hexes.TryAdd((id, k), (q, r)))
                    throw new ContentException(HexesFileName, at, FormattableString.Invariant($"{index} {k} is listed twice"));
            }
            return hexes;
        }

        static int Int(Dictionary<object, object> m, string key, string at) =>
            int.TryParse(Text(m, key), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n)
                ? n : throw new ContentException(HexesFileName, $"{at}.{key}", "required, a whole number");

        /// <summary>The latest `checked` day among an entry's `sources` ('YYYY-MM-DD' compares as text), or "" with none.</summary>
        static string LatestCheck(Dictionary<object, object> m) =>
            m.TryGetValue("sources", out var so) && so is List<object> sources
                ? sources.OfType<Dictionary<object, object>>().Select(x => Text(x, "checked")).OrderBy(x => x, StringComparer.Ordinal).LastOrDefault() ?? ""
                : "";

        static string Text(Dictionary<object, object> m, string key) => m.TryGetValue(key, out var v) && v is string s ? s : "";

        static decimal Number(Dictionary<object, object> m, string key, string field) =>
            decimal.TryParse(Text(m, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? d : throw new ContentException(FileName, field, "required, a number");
    }
}
