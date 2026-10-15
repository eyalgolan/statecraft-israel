#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Statecraft.Game.Map
{
    /// <summary>The runtime form of the map's region files (tools/regions_bake.py; issue #321): subdistrict_hexes_1948,
    /// armistice_hexes_1949 and control_hexes_1948_49 as .tsv beside their YAML, read at boot instead of the YAML. The
    /// layout is the YAML's own shape (top-level scalars, lists of mappings, lists of lists), so each class builds the same
    /// rows from either source and keeps its own checks. <see cref="Read"/> refuses a file of another version, made from
    /// another YAML (the YAML's FNV-1a check), with another row count, without its last body_check row or not matching it
    /// (a file cut anywhere, even inside a number), or with a malformed row; the classes then read the YAML, with one
    /// warning.</summary>
    public static class RegionsRuntime
    {
        /// <summary>Bump it, with tools/regions_bake.py's VERSION, when the layout or its reading changes;
        /// tools/tests/test_regions_bake.py pins a fingerprint of this reader and of the classes' runtime mapping.</summary>
        public const int Version = 2;   // 2: every scalar as its own text (review of #601)

        /// <summary>A value: a scalar's own text (null for a plain null) or a list of tokens. Integers and booleans are read
        /// from the text as the classes' YAML path reads them.</summary>
        public sealed class Value
        {
            public char Kind; public string? Text; public List<string>? Items;
        }

        /// <summary>One mapping of a top-level list; <see cref="Done"/> refuses a field the caller did not read (YamlDotNet
        /// refuses an unknown key in the YAML the same way).</summary>
        public sealed class Row
        {
            readonly string file; readonly int line;
            readonly Dictionary<string, Value> fields = new Dictionary<string, Value>();
            readonly HashSet<string> read = new HashSet<string>();
            internal Row(string file, int line) { this.file = file; this.line = line; }
            internal void Add(string key, Value v)
            {
                if (fields.ContainsKey(key)) throw new InvalidDataException($"{file}:{line}: field '{key}' twice");
                fields[key] = v;
            }
            Value? Get(string key, char kind)
            {
                read.Add(key);
                if (!fields.TryGetValue(key, out var v)) return null;
                if ((kind == 'l') != (v.Kind == 'l')) throw new InvalidDataException($"{file}:{line}: field '{key}' is not of the expected type");
                return v;
            }
            /// <summary>The field's text; <paramref name="absent"/> when the row has no such field (the YAML class's default).</summary>
            public string? Str(string key, string? absent) { var v = Get(key, 's'); return v == null ? absent : v.Text; }
            /// <summary>An integer field from its text (a decimal integer only; anything else, as YamlDotNet's 0x0F, is refused
            /// and the YAML read instead).</summary>
            public int Int(string key, int absent)
            {
                var v = Get(key, 's');
                if (v == null) return absent;
                return v.Text != null && DecimalInt(v.Text, out var n) ? n
                    : throw new InvalidDataException($"{file}:{line}: field '{key}' '{v.Text}' is not an integer");
            }
            /// <summary>A boolean field from its text, "true" or "false" only (anything else is refused and the YAML read).</summary>
            public bool Bool(string key, bool absent)
            {
                var v = Get(key, 's');
                if (v == null) return absent;
                return v.Text == "true" ? true : v.Text == "false" ? false
                    : throw new InvalidDataException($"{file}:{line}: field '{key}' '{v.Text}' is not true or false");
            }
            public List<string> List(string key) { var v = Get(key, 'l'); return v == null ? new List<string>() : new List<string>(v.Items!); }
            public void Done()
            {
                foreach (var k in fields.Keys) if (!read.Contains(k)) throw new InvalidDataException($"{file}:{line}: unknown field '{k}'");
            }
        }

        /// <summary>A read runtime file: its top-level scalars, lists of mappings and lists of lists, by key.</summary>
        public sealed class Doc
        {
            readonly string file;
            readonly Dictionary<string, Value> scalars = new Dictionary<string, Value>();
            readonly Dictionary<string, List<Row>> items = new Dictionary<string, List<Row>>();
            readonly Dictionary<string, List<string[]>> tuples = new Dictionary<string, List<string[]>>();
            readonly HashSet<string> keys = new HashSet<string>(), read = new HashSet<string>();
            internal Doc(string file) { this.file = file; }
            internal void Key(string key, int line)
            {
                if (!keys.Add(key)) throw new InvalidDataException($"{file}:{line}: key '{key}' twice");
            }
            internal void Scalar(string key, Value v, int line) { Key(key, line); scalars[key] = v; }
            internal List<Row> ItemsOf(string key, int line)
            {
                if (!items.TryGetValue(key, out var list)) { Key(key, line); items[key] = list = new List<Row>(); }
                return list;
            }
            internal void Tuples(string key, List<string[]> rows, int line) { Key(key, line); tuples[key] = rows; }
            internal void Empty(string key, int line) { Key(key, line); }

            public string? Str(string key, string? absent)
            {
                read.Add(key);
                if (!scalars.TryGetValue(key, out var v)) return absent;
                if (v.Kind == 'l') throw new InvalidDataException($"{file}: '{key}' is not a string");
                return v.Text;
            }
            public List<string> List(string key)
            {
                read.Add(key);
                if (!scalars.TryGetValue(key, out var v)) return new List<string>();
                if (v.Kind != 'l') throw new InvalidDataException($"{file}: '{key}' is not a list");
                return new List<string>(v.Items!);
            }
            /// <summary>The mappings of a top-level list, each read by <paramref name="map"/> (which must read every field).</summary>
            public List<T> Items<T>(string key, Func<Row, T> map)
            {
                read.Add(key);
                var list = new List<T>();
                if (items.TryGetValue(key, out var rows)) foreach (var r in rows) { list.Add(map(r)); r.Done(); }
                return list;
            }
            /// <summary>The elements of a top-level list of lists, as their tokens.</summary>
            public List<List<string>> Strings(string key)
            {
                read.Add(key);
                var list = new List<List<string>>();
                if (tuples.TryGetValue(key, out var rows)) foreach (var r in rows) list.Add(new List<string>(r));
                return list;
            }
            public List<List<int>> Ints(string key)
            {
                var list = new List<List<int>>();
                foreach (var r in Strings(key))
                {
                    var ints = new List<int>(r.Count);
                    foreach (var t in r)
                        ints.Add(DecimalInt(t, out var n) ? n
                            : throw new InvalidDataException($"{file}: '{key}' holds '{t}', not an integer"));
                    list.Add(ints);
                }
                return list;
            }
            /// <summary>Refuses a key the class did not read (YamlDotNet refuses an unknown key the same way).</summary>
            public void Done()
            {
                foreach (var k in keys) if (!read.Contains(k)) throw new InvalidDataException($"{file}: unknown key '{k}'");
            }
        }

        /// <summary>A plain decimal integer, read as the YAML path reads it. A leading zero is refused: YamlDotNet (the
        /// region classes' deserializer, 18.1.0) reads "012" as octal 10 and "08" not at all, where this would read 12 and 8
        /// (review of #601); such a file then falls back to the YAML.</summary>
        internal static bool DecimalInt(string text, out int n)
        {
            n = 0;
            int i = text.Length > 0 && (text[0] == '+' || text[0] == '-') ? 1 : 0;
            if (text.Length > i + 1 && text[i] == '0') return false;
            return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out n);
        }

        /// <summary>FNV-1a over the UTF-8 bytes of the data rows, each followed by '\n' (tools/regions_bake.py's body_check).</summary>
        public static int BodyCheck(IEnumerable<string> rows)
        {
            uint h = 2166136261;
            foreach (var row in rows)
            {
                foreach (var b in Encoding.UTF8.GetBytes(row)) { h ^= b; h *= 16777619; }
                h ^= (byte)'\n'; h *= 16777619;
            }
            return (int)h;
        }

        /// <summary>Reads a runtime file made from <paramref name="yaml"/>; throws InvalidDataException naming
        /// <paramref name="file"/> (and the line, for a malformed row) otherwise.</summary>
        public static Doc Read(string tsv, string yaml, string file)
        {
            var doc = new Doc(file);
            int? version = null, check = null, count = null, body = null;
            var rows = new List<string>();
            var lines = tsv.Split('\n');
            // The version first, so a file of another layout says so rather than failing on a row it cannot read (review of #601).
            foreach (var l in lines)
                if (l.StartsWith("meta\tversion\t", StringComparison.Ordinal))
                {
                    var v = l.TrimEnd('\r').Substring("meta\tversion\t".Length);
                    if (v != Version.ToString(CultureInfo.InvariantCulture))
                        throw new InvalidDataException($"{file}: version {v}, this build reads {Version} (re-bake: tools/regions_bake.py)");
                    break;
                }
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
                if (c[0] == "meta")
                {
                    if (c.Length != 3) throw new InvalidDataException($"{file}:{line}: a meta row has {c.Length} fields, not 3");
                    switch (c[1])
                    {
                        case "version": version = Int(c[2], "version", line); break;
                        case "yaml_check": check = Int(c[2], "yaml_check", line); break;
                        case "rows": count = Int(c[2], "rows", line); break;
                        case "body_check": body = Int(c[2], "body_check", line); break;
                        default: throw new InvalidDataException($"{file}:{line}: unknown meta '{c[1]}'");
                    }
                    continue;
                }
                rows.Add(text);
                switch (c[0])
                {
                    case "scalar":
                        if (c.Length != 3) throw new InvalidDataException($"{file}:{line}: a scalar row has {c.Length} fields, not 3");
                        doc.Scalar(c[1], Parse(c[2], file, line), line);
                        break;
                    case "item":
                        if (c.Length < 2) throw new InvalidDataException($"{file}:{line}: an item row has no table");
                        var row = new Row(file, line);
                        for (int k = 2; k < c.Length; k++)
                        {
                            int eq = c[k].IndexOf('=');
                            if (eq <= 0) throw new InvalidDataException($"{file}:{line}: field '{c[k]}' is not name=value");
                            row.Add(c[k].Substring(0, eq), Parse(c[k].Substring(eq + 1), file, line));
                        }
                        doc.ItemsOf(c[1], line).Add(row);
                        break;
                    case "tuples":
                        if (c.Length != 4) throw new InvalidDataException($"{file}:{line}: a tuples row has {c.Length} fields, not 4");
                        int n = Int(c[2], "count", line);
                        var parts = c[3].Length == 0 ? Array.Empty<string>() : c[3].Split(';');
                        if (parts.Length != n) throw new InvalidDataException($"{file}:{line}: {parts.Length} tuples, the row says {n}");
                        var tuples = new List<string[]>(parts.Length);
                        foreach (var p in parts) tuples.Add(p.Split(' '));
                        doc.Tuples(c[1], tuples, line);
                        break;
                    case "empty":
                        if (c.Length != 2) throw new InvalidDataException($"{file}:{line}: an empty row has {c.Length} fields, not 2");
                        doc.Empty(c[1], line);
                        break;
                    default: throw new InvalidDataException($"{file}:{line}: unknown row '{c[0]}'");
                }
            }
            if (version == null || check == null || count == null) throw new InvalidDataException($"{file}: no version, yaml_check or rows row");
            if (version != Version) throw new InvalidDataException($"{file}: version {version}, this build reads {Version} (re-bake: tools/regions_bake.py)");
            if (check != Routes.RouteRibbonFile.Check(yaml)) throw new InvalidDataException($"{file}: made from another YAML (re-bake: tools/regions_bake.py)");
            if (rows.Count != count) throw new InvalidDataException($"{file}: {rows.Count} rows, the file says {count}");
            if (body == null) throw new InvalidDataException($"{file}: no body_check row at the end (cut short?)");
            if (body != BodyCheck(rows)) throw new InvalidDataException($"{file}: the rows do not match body_check (cut short or edited?)");
            return doc;
        }

        static Value Parse(string text, string file, int line)
        {
            if (text.Length < 2 || text[1] != ':') throw new InvalidDataException($"{file}:{line}: value '{text}' has no type");
            var body = text.Substring(2);
            switch (text[0])
            {
                case 's': return new Value { Kind = 's', Text = Unescape(body, file, line) };
                case 'n':
                    if (body.Length != 0) throw new InvalidDataException($"{file}:{line}: a null with text");
                    return new Value { Kind = 'n', Text = null };
                case 'l': return new Value { Kind = 'l', Items = body.Length == 0 ? new List<string>() : new List<string>(body.Split(',')) };
                default: throw new InvalidDataException($"{file}:{line}: unknown type '{text[0]}'");
            }
        }

        static string Unescape(string s, string file, int line)
        {
            if (s.IndexOf('\\') < 0) return s;
            var b = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\') { b.Append(s[i]); continue; }
                if (++i >= s.Length) throw new InvalidDataException($"{file}:{line}: a trailing backslash");
                b.Append(s[i] switch
                {
                    '\\' => '\\', 't' => '\t', 'r' => '\r', 'n' => '\n',
                    _ => throw new InvalidDataException($"{file}:{line}: unknown escape '\\{s[i]}'"),
                });
            }
            return b.ToString();
        }

        /// <summary>The boot read for a region file: the class's rows from the runtime file (<paramref name="rows"/>, which
        /// reads every field and calls Done) when it is for this YAML, else, with one warning through <paramref name="warn"/>,
        /// <paramref name="parse"/> over the YAML. Then <paramref name="build"/>, the class's checks, outside the fallback: a
        /// content error is the YAML's own (both sources would fail it), so it throws as the YAML path does, with no warning
        /// and no second read (review of #601).</summary>
        public static T ReadOrYaml<TDoc, T>(string? tsv, string yaml, string file, Func<Doc, TDoc> rows, Func<TDoc, T> build,
            Func<string, T> parse, Action<string> warn)
        {
            if (string.IsNullOrWhiteSpace(tsv)) { warn($"{file}: missing; reading the YAML"); return parse(yaml); }
            TDoc doc;
            try { doc = rows(Read(tsv!, yaml, file)); }
            catch (InvalidDataException e) { warn($"{e.Message}; reading the YAML"); return parse(yaml); }
            return build(doc);
        }
    }
}
