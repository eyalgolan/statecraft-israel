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
        /// <summary>The anchor's place, as the row names it.</summary>
        public string Anchor { get; set; } = "";
        public string Type { get; set; } = "";
        public string Side { get; set; } = "";
        public int Strength { get; set; }
        public int Q { get; set; }
        public int R { get; set; }
        /// <summary>The row's review status (draft, to_verify, fact-checked); the release gate reads it (Q-START-3).</summary>
        public string Status { get; set; } = "";
        /// <summary>The hex of the row's 11 June 1948 anchor that the enemy AI marches on (R-ENEMY-002; Q-ENEMY-1), or null
        /// when the row has none on two sources.</summary>
        public (int Q, int R)? Script { get; set; }
        /// <summary>The end of that anchor's window (its `to`): from a turn starting on it the unit follows the goals.</summary>
        public DateTime ScriptUntil { get; set; }
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
            var hexes = Hexes(readFile, rows);
            var start = c.Rules.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var ppu = c.RulesAt(c.Rules.Start).Mvp!.PeoplePerUnit;   // the start day's rules, as Cities.Init reads them
            var list = new List<StartingForce>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ContentException(FileName, $"units[{i}]", "empty row");
                var id = Text(row, "id");
                if (id == "") throw new ContentException(FileName, $"units[{i}].id", "required");
                var at = $"units[{id}]";
                if (row.ContainsKey("hidden") || Text(row, "service") != "army") continue;
                if (!(row.TryGetValue("strength", out var so) && so is Dictionary<object, object> strength) || Text(strength, "as_of") != start) continue;
                if (!(row.TryGetValue("anchors", out var ao) && ao is List<object> anchors)) continue;
                int k = anchors.FindIndex(a => a is Dictionary<object, object> m
                    && string.CompareOrdinal(Text(m, "from"), start) <= 0 && string.CompareOrdinal(start, Text(m, "to")) < 0);
                if (k < 0) continue;
                var anchor = (Dictionary<object, object>)anchors[k];
                var side = Text(row, "side");
                var people = Number(strength, "value", $"{at}.strength.value");
                if (people < 0 || people != decimal.Truncate(people)) throw new ContentException(FileName, $"{at}.strength.value", "must be a whole number >= 0");
                var units = Cities.StartUnits(people, ppu);   // the nearest unit, a half up, at least 1 for a force with people
                if (units == 0) continue;                     // a strength of nobody starts nothing
                if (!hexes.TryGetValue((id, k), out var qr))
                    throw new ContentException(HexesFileName, "anchors", $"no hex for {at}.anchors[{k}] ({FileName}); rerun tools/unit_anchor_hexes.py");
                var (q, r) = qr;
                var hex = c.HexAt(q, r);
                if (hex == null) throw new ContentException(FileName, $"{at}.anchors[{k}]", FormattableString.Invariant($"hex q={q} r={r} is not on the map ({ContentLoader.HexesFile})"));
                if (!hex.Land) throw new ContentException(FileName, $"{at}.anchors[{k}]", FormattableString.Invariant($"hex q={q} r={r} is not a land hex in {ContentLoader.HexesFile}"));
                var force = new StartingForce
                {
                    Row = id, Anchor = Text(anchor, "place"), Type = TypeFor(c, side, at), Side = side,
                    Strength = units, Q = q, R = r, Status = Text(row, "status"),
                };
                if (side != Movement.PlayerSide) Script(c, force, anchors, id, at, hexes);
                list.Add(force);
            }
            return list;
        }

        /// <summary>R-ENEMY-002 (Q-ENEMY-1: the 15 May and 11 June 1948 positions two sources agree on, arab-forces-1948.md
        /// section 10). The 15 May position is the start; the script's target is the row's first anchor from 11 June that cites
        /// at least two sources other than Wikipedia and Wikidata (which the file uses for coordinates only), on that anchor's
        /// hex. A row with none has no script and its unit follows the goals.</summary>
        static void Script(Content c, StartingForce force, List<object> anchors, string id, string at,
            Dictionary<(string id, int anchor), (int q, int r)> hexes)
        {
            for (int j = 0; j < anchors.Count; j++)
            {
                if (!(anchors[j] is Dictionary<object, object> a) || Text(a, "from") != EnemyAI.ScriptDate) continue;
                var sourced = a.TryGetValue("sources", out var so) && so is List<object> sources ? sources.Count(x =>
                    x is Dictionary<object, object> m && Text(m, "url") is var url && url != ""
                    && !url.Contains("wikipedia.org", StringComparison.Ordinal) && !url.Contains("wikidata.org", StringComparison.Ordinal)) : 0;
                if (sourced < 2) continue;
                if (!hexes.TryGetValue((id, j), out var qr))
                    throw new ContentException(HexesFileName, "anchors", $"no hex for {at}.anchors[{j}] ({FileName}); rerun tools/unit_anchor_hexes.py");
                if (!(c.HexAt(qr.q, qr.r) is MapHex hex) || !hex.Land)
                    throw new ContentException(FileName, $"{at}.anchors[{j}]", FormattableString.Invariant($"hex q={qr.q} r={qr.r} is not a land hex in {ContentLoader.HexesFile}"));
                if (!DateTime.TryParseExact(Text(a, "to"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until))
                    throw new ContentException(FileName, $"{at}.anchors[{j}].to", "required, a date 'YYYY-MM-DD'");
                force.Script = (qr.q, qr.r);
                force.ScriptUntil = until;
                return;
            }
        }

        /// <summary>Adds the starting forces to a new game, in the content's order, taking the next ids: the unit with id i
        /// of a new game is <c>c.StartingForces[i]</c>.</summary>
        public static void Init(Content c, GameState s)
        {
            foreach (var f in c.StartingForces)
            {
                var id = s.NextId++;
                s.Units.Add(id, new Unit(id, f.Type, f.Q, f.R, side: f.Side, strength: f.Strength));
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

        /// <summary>A file's list under <paramref name="key"/>, read untyped (only the keys the start needs are read).</summary>
        static List<Dictionary<object, object>> Rows(Func<string, string?> readFile, string fileName, string key)
        {
            string? text;
            try { text = readFile(fileName); }
            catch (System.IO.IOException e) { throw new ContentException(fileName, "(file)", e.Message); }
            catch (UnauthorizedAccessException e) { throw new ContentException(fileName, "(file)", e.Message); }
            if (text == null) throw new ContentException(fileName, "(file)", "missing file");
            Dictionary<string, List<Dictionary<object, object>>>? file;
            try { file = new DeserializerBuilder().Build().Deserialize<Dictionary<string, List<Dictionary<object, object>>>>(text); }
            catch (YamlException e) { throw new ContentException(fileName, $"line {e.Start.Line}", e.InnerException?.Message ?? e.Message); }
            if (file == null || !file.TryGetValue(key, out var rows) || rows == null) throw new ContentException(fileName, key, "required");
            return rows;
        }

        /// <summary>unit_anchor_hexes.yaml by (row id, anchor index). Every row must name an anchor of <paramref name="units"/>,
        /// once: a row that does not means the file is stale (the tool was not rerun after units_1948.yaml changed).</summary>
        static Dictionary<(string id, int anchor), (int q, int r)> Hexes(Func<string, string?> readFile, List<Dictionary<object, object>> units)
        {
            var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var u in units)
                if (u != null) anchors[Text(u, "id")] = u.TryGetValue("anchors", out var ao) && ao is List<object> list ? list.Count : 0;
            var hexes = new Dictionary<(string, int), (int, int)>();
            var rows = Rows(readFile, HexesFileName, "anchors");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? throw new ContentException(HexesFileName, $"anchors[{i}]", "empty row");
                var id = Text(row, "id");
                var at = id == "" ? $"anchors[{i}]" : $"anchors[{id}]";
                int k = Int(row, "anchor", at), q = Int(row, "q", at), r = Int(row, "r", at);
                if (!anchors.TryGetValue(id, out var n) || k < 0 || k >= n)
                    throw new ContentException(HexesFileName, at, FormattableString.Invariant($"no anchor {k} of a row '{id}' in {FileName}; rerun tools/unit_anchor_hexes.py"));
                if (!hexes.TryAdd((id, k), (q, r)))
                    throw new ContentException(HexesFileName, at, FormattableString.Invariant($"anchor {k} is listed twice"));
            }
            return hexes;
        }

        static int Int(Dictionary<object, object> m, string key, string at) =>
            int.TryParse(Text(m, key), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n)
                ? n : throw new ContentException(HexesFileName, $"{at}.{key}", "required, a whole number");

        static string Text(Dictionary<object, object> m, string key) => m.TryGetValue(key, out var v) && v is string s ? s : "";

        static decimal Number(Dictionary<object, object> m, string key, string field) =>
            decimal.TryParse(Text(m, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                ? d : throw new ContentException(FileName, field, "required, a number");
    }
}
