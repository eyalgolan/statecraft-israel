using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Statecraft.Sim
{
    public sealed class SaveException : Exception
    {
        /// <summary><see cref="Code"/> of a save made under the rules before this build's (a schema 1 save, or ruleset legacy
        /// in the GDD game): R-SAVE-001.</summary>
        public const string OlderRules = "older_rules";
        /// <summary><see cref="Code"/> of a save made by a newer version of the game: a newer schema, or a ruleset this build
        /// does not know (R-SAVE-002).</summary>
        public const string NewerVersion = "newer_version";

        /// <summary>1-based line of the save text the error is on (0 when it concerns the whole save).</summary>
        public int Line { get; }
        /// <summary>A stable refusal code the UI translates (<see cref="OlderRules"/>, <see cref="NewerVersion"/>), or null for a
        /// damaged save, whose reason the UI words itself. The English message never asks the player to start a new game:
        /// the UI says that once (N4).</summary>
        public string? Code { get; }
        public SaveException(int line, string message, string? code = null)
            : base(line > 0 ? FormattableString.Invariant($"line {line}: {message}") : message) { Line = line; Code = code; }
        /// <summary>A save whose replay threw <paramref name="inner"/>: kept, with its stack, for the log (#631).</summary>
        public SaveException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// A save is seed + command log + content hash + schema version (spec section 3); loading replays it. Text format,
    /// one item per line:
    ///   statecraft-save 2          schema version (a schema 1 save has no ruleset line and is legacy)
    ///   content &lt;hex&gt;             Content.Hash of the content it was played with
    ///   seed &lt;ulong&gt;  /  mode Historical
    ///   ruleset legacy|gdd-0.1     the rules it was played under (Content.Ruleset); another ruleset refuses it (R-SAVE-001),
    ///                              and one this build does not know was made by a newer version (R-SAVE-002)
    ///   turn                       starts the next turn's command list (one per Simulation.Step)
    ///   &lt;command&gt; ...              policy ID LEVEL | budget OFFICE=SHARE,... | event ID OPTION | mobilize LEVEL |
    ///                              security DELTA | appoint OFFICE PERSON | map ACTION Q R | work CITY ROLE N |
    ///                              train CITY TRACK N | untrain COHORT | pioneer UNIT ACTION SITE | stop UNIT
    ///   end &lt;hex&gt;                 GameState.Hash() after the last turn (checks the replay)
    /// Doubles are IEEE bit patterns in hex, so replays are exact; strings are percent-escaped (Uri.EscapeDataString).
    /// </summary>
    public sealed class SaveGame
    {
        public const int SchemaVersion = 2;
        /// <summary>The ruleset of the legacy loop (Content.Ruleset).</summary>
        public const string LegacyRuleset = "legacy";
        /// <summary>The ruleset of the GDD v0.1 loop (eras.yaml systems.loop: gdd).</summary>
        public const string GddRuleset = "gdd-0.1";
        /// <summary>Turns are at least a week long, so 2000 turns is 38 years, far past any content; a save with more is hostile or damaged.</summary>
        public const int MaxTurns = 2000;
        const string Magic = "statecraft-save";

        /// <summary>Each turn as its canonical command lines. The save keeps text, not the caller's objects, so nothing
        /// outside can change a recorded turn; <see cref="Turns"/> parses fresh copies on demand.</summary>
        readonly List<List<string>> turns = new List<List<string>>();

        public string ContentHash { get; }
        public ulong Seed { get; }
        public Mode Mode { get; }
        /// <summary>The ruleset the game was played under: <see cref="LegacyRuleset"/> or <see cref="GddRuleset"/> (R-SAVE-001).</summary>
        public string Ruleset { get; }
        /// <summary>The recorded turns as new command objects each time it is read (changing them changes nothing in the save).</summary>
        public IReadOnlyList<IReadOnlyList<Command>> Turns =>
            turns.Select(t => (IReadOnlyList<Command>)t.Select(l => ParseCommand(0, l)).ToList().AsReadOnly()).ToList().AsReadOnly();
        /// <summary>GameState.Hash() after the last recorded turn (the new game's hash before any turn).</summary>
        public string FinalHash { get; private set; }

        public SaveGame(string contentHash, ulong seed, Mode mode, string ruleset, string initialHash)
        {
            ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));
            if (ruleset != LegacyRuleset && ruleset != GddRuleset) throw new ArgumentException($"unknown ruleset '{ruleset}'", nameof(ruleset));
            Seed = seed; Mode = mode; Ruleset = ruleset;
            FinalHash = initialHash ?? throw new ArgumentNullException(nameof(initialHash));
        }

        /// <summary>Appends one turn: the commands passed to Simulation.Step and the state it returned.</summary>
        internal void Record(IReadOnlyList<Command> commands, GameState after)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (after == null) throw new ArgumentNullException(nameof(after));
            // A recorded turn is immutable: the caller keeps its own objects (an AllocateBudget's dictionary is theirs to reuse).
            turns.Add(commands.Select(Format).ToList());
            FinalHash = after.Hash();
        }

        public string Write()
        {
            var sb = new StringBuilder();
            sb.Append(Magic).Append(' ').Append(SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("content ").Append(ContentHash).Append('\n');
            sb.Append("seed ").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("mode ").Append(Mode.ToString()).Append('\n');
            sb.Append("ruleset ").Append(Ruleset).Append('\n');
            foreach (var t in turns)
            {
                sb.Append("turn\n");
                foreach (var line in t) sb.Append(line).Append('\n');
            }
            sb.Append("end ").Append(FinalHash).Append('\n');
            return sb.ToString();
        }

        static string Esc(string s) => SaveText.Escape(s);
        static string Num(double v) => BitConverter.DoubleToInt64Bits(v).ToString("X16", CultureInfo.InvariantCulture);
        static string Int(int v) => v.ToString(CultureInfo.InvariantCulture);

        static string Format(Command cmd) => cmd switch
        {
            SetPolicy p => $"policy {Esc(p.Policy)} {Num(p.Level)}",
            AllocateBudget b => "budget " + (b.Shares == null ? "~" : b.Shares.Count == 0 ? "-" :
                string.Join(",", b.Shares.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => Esc(kv.Key) + "=" + Num(kv.Value)))),
            ChooseEventOption e => $"event {Esc(e.EventId)} {Esc(e.OptionId)}",
            SetMobilization m => $"mobilize {Num(m.Level)}",
            SecurityOrder o => $"security {Int(o.Delta)}",
            Appoint a => $"appoint {Esc(a.Office)} {Esc(a.Person)}",
            MapAction m => $"map {Esc(m.Action)} {Int(m.Q)} {Int(m.R)}",
            AssignWorkers w => $"work {Esc(w.City)} {Esc(w.Role)} {Int(w.Count)}",
            Train t => $"train {Esc(t.City)} {Esc(t.Track)} {Int(t.Count)}",
            CancelTraining t => $"untrain {Int(t.Cohort)}",
            PioneerWork p => $"pioneer {Int(p.Unit)} {Esc(p.Action)} {Esc(p.Site)}",
            StopWork w => $"stop {Int(w.Unit)}",
            _ => throw new ArgumentException($"Cannot save command kind '{cmd.Kind}'", nameof(cmd)),
        };

        /// <summary>Parses save text; any malformed line throws SaveException naming the line.</summary>
        public static SaveGame Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);   // a byte-order mark is not part of the save
            var lines = text.Replace("\r\n", "\n").Split('\n');
            int n = lines.Length;
            while (n > 0 && lines[n - 1] == "") n--;
            if (n < 5) throw new SaveException(0, "Not a Statecraft save (too short)");
            if (lines.Take(n).Count(l => l == "turn") > MaxTurns)
                throw new SaveException(0, FormattableString.Invariant($"save has {lines.Take(n).Count(l => l == "turn")} turns; the most this game plays is {MaxTurns}"));
            string[] Head(int i, string key, int parts)
            {
                var f = lines[i].Split(' ');
                if (f[0] != key || f.Length != parts) throw new SaveException(i + 1, $"expected '{key}'");
                return f;
            }
            var magic = Head(0, Magic, 2);
            if (!int.TryParse(magic[1], NumberStyles.None, CultureInfo.InvariantCulture, out var version)) throw new SaveException(1, "bad schema version");
            if (version > SchemaVersion) throw new SaveException(1, FormattableString.Invariant($"save schema version {version} is newer than this game ({SchemaVersion})"), SaveException.NewerVersion);
            if (version < 1) throw new SaveException(1, FormattableString.Invariant($"unknown save schema version {version}"));
            var content = Head(1, "content", 2)[1];
            if (!IsHash(content)) throw new SaveException(2, "bad content hash");
            if (!ulong.TryParse(Head(2, "seed", 2)[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seed)) throw new SaveException(3, "bad seed");
            var modeName = Head(3, "mode", 2)[1];
            if (!Enum.GetNames(typeof(Mode)).Contains(modeName)) throw new SaveException(4, "bad mode");   // names only: Enum.TryParse would accept "0"
            var mode = (Mode)Enum.Parse(typeof(Mode), modeName);
            if (mode == Mode.Open) throw new SaveException(4, "mode Open is not supported");   // Simulation.NewGame cannot start it before M4
            var first = 4;   // the first line after the header
            var ruleset = LegacyRuleset;
            if (version >= 2)
            {
                if (n < 6) throw new SaveException(0, "Not a Statecraft save (too short)");
                ruleset = Head(4, "ruleset", 2)[1];
                // R-SAVE-002 (N2): a ruleset this build does not know comes from a newer version, like a newer schema.
                if (ruleset != LegacyRuleset && ruleset != GddRuleset)
                    throw new SaveException(5, $"This save was made with a newer version of the game (ruleset '{ruleset}').", SaveException.NewerVersion);
                first = 5;
            }
            var end = Head(n - 1, "end", 2)[1];
            if (!IsHash(end)) throw new SaveException(n, "bad end hash");
            var save = new SaveGame(content, seed, mode, ruleset, end);
            List<string>? current = null;
            for (int i = first; i < n - 1; i++)
            {
                if (lines[i] == "turn") { current = new List<string>(); save.turns.Add(current); continue; }
                if (current == null) throw new SaveException(i + 1, "command before the first 'turn'");
                current.Add(Format(ParseCommand(i + 1, lines[i])));   // canonical form, validated now
            }
            return save;
        }

        static bool IsHash(string s) => s.Length == 64 && s.All(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'));

        static Command ParseCommand(int line, string text)
        {
            var f = text.Split(' ');
            void Arity(int count) { if (f.Length != count) throw new SaveException(line, $"'{f[0]}' needs {count - 1} values"); }
            string Str(string s)
            {
                return SaveText.Unescape(s) ?? throw new SaveException(line, $"bad text '{s}'");
            }
            double Dbl(string s) => long.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var bits) && s.Length == 16
                ? BitConverter.Int64BitsToDouble(bits) : throw new SaveException(line, $"bad number '{s}'");
            int I(string s) => int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v) ? v : throw new SaveException(line, $"bad integer '{s}'");
            switch (f[0])
            {
                case "policy": Arity(3); return new SetPolicy(Str(f[1]), Dbl(f[2]));
                case "budget":
                    Arity(2);
                    if (f[1] == "~") return new AllocateBudget(null!);          // a null budget: replays with the same rejection reason
                    var shares = new Dictionary<string, double>();
                    if (f[1] != "-")
                        foreach (var pair in f[1].Split(','))
                        {
                            var kv = pair.Split('=');
                            if (kv.Length != 2) throw new SaveException(line, $"bad budget share '{pair}'");
                            shares[Str(kv[0])] = Dbl(kv[1]);
                        }
                    return new AllocateBudget(shares);
                case "event": Arity(3); return new ChooseEventOption(Str(f[1]), Str(f[2]));
                case "mobilize": Arity(2); return new SetMobilization(Dbl(f[1]));
                case "security": Arity(2); return new SecurityOrder(I(f[1]));
                case "appoint": Arity(3); return new Appoint(Str(f[1]), Str(f[2]));
                case "map": Arity(4); return new MapAction(Str(f[1]), I(f[2]), I(f[3]));
                case "work": Arity(4); return new AssignWorkers(Str(f[1]), Str(f[2]), I(f[3]));
                case "train": Arity(4); return new Train(Str(f[1]), Str(f[2]), I(f[3]));
                case "untrain": Arity(2); return new CancelTraining(I(f[1]));
                case "pioneer": Arity(4); return new PioneerWork(I(f[1]), Str(f[2]), Str(f[3]));
                case "stop": Arity(2); return new StopWork(I(f[1]));
                default: throw new SaveException(line, $"unknown command '{f[0]}'");
            }
        }
    }

    /// <summary>The save format's own percent-encoding (RFC 3986: unreserved characters stay, everything else is %XX of its
    /// UTF-8 bytes), so a save does not depend on the runtime's Uri class (Mono and .NET differ).</summary>
    internal static class SaveText
    {
        static readonly System.Text.UTF8Encoding Strict = new System.Text.UTF8Encoding(false, true);

        static bool Unreserved(byte b) => (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || (b >= '0' && b <= '9') || b == '-' || b == '.' || b == '_' || b == '~';

        /// <summary>False for a string that is not valid Unicode text (a lone surrogate), which cannot be saved losslessly.</summary>
        public static bool IsEncodable(string s)
        {
            try { Strict.GetBytes(s); return true; }
            catch (System.Text.EncoderFallbackException) { return false; }
        }

        public static string Escape(string s)
        {
            var sb = new StringBuilder();
            foreach (var b in Strict.GetBytes(s))
            {
                if (Unreserved(b)) sb.Append((char)b);
                else sb.Append('%').Append("0123456789ABCDEF"[b >> 4]).Append("0123456789ABCDEF"[b & 15]);
            }
            return sb.ToString();
        }

        /// <summary>Null when the text has a malformed escape or is not valid UTF-8.</summary>
        public static string? Unescape(string s)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '%') { if (s[i] > 127) return null; bytes.Add((byte)s[i]); continue; }
                if (i + 2 >= s.Length) return null;
                int hi = HexVal(s[i + 1]), lo = HexVal(s[i + 2]);
                if (hi < 0 || lo < 0) return null;
                bytes.Add((byte)(hi * 16 + lo));
                i += 2;
            }
            try { return Strict.GetString(bytes.ToArray()); }
            catch (System.Text.DecoderFallbackException) { return null; }
        }

        static int HexVal(char c) => c >= '0' && c <= '9' ? c - '0' : c >= 'A' && c <= 'F' ? c - 'A' + 10 : c >= 'a' && c <= 'f' ? c - 'a' + 10 : -1;
    }
}
