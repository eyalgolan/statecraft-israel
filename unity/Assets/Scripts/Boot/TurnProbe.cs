#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Statecraft.Game.Boot
{
    /// <summary>Issue #2905: where an End turn's main-thread time goes, off unless asked for: "?turnlog" (WebGL page URL) or
    /// -turnlog (desktop), read by <see cref="DiagLog.Init"/>. While on, SimBridge.EndTurn opens a turn (<see cref="BeginTurn"/>),
    /// times the sim step, every handler of its Changed, CombatResolved and TurnEnded events (<see cref="Handler"/>) and the
    /// sections the map layers mark (<see cref="Lap"/>), then writes one console line (<see cref="EndTurn"/>):
    /// <code>TURNLOG v=1 turn=N date=yyyy-MM-dd total_ms=X step_ms=X handlers=N Event/Name=X ... @Section=X ...</code>
    /// Every field after "TURNLOG" is key=value with no spaces; times are milliseconds with two decimals (invariant culture).
    /// A handler key is its event and <see cref="HandlerName"/> (a second handler of the same name gets "#2", and so on), in
    /// the order the handlers ran; a section key is "@" and its name, its time summed over the turn, in the order first seen.
    /// Presentation only: it never changes what runs or in what order, and with the flag off every hook is one static bool
    /// read (no Stopwatch read, no allocation).</summary>
    public static class TurnProbe
    {
        public const string Flag = "turnlog", Prefix = "TURNLOG", Version = "1";

        /// <summary>Asked for (?turnlog / -turnlog). Set by DiagLog.Init, or by a test.</summary>
        public static bool On { get; set; }

        /// <summary>Between <see cref="BeginTurn"/> and <see cref="EndTurn"/> of a probed turn: only then is anything timed.</summary>
        public static bool Active { get; private set; }

        /// <summary>The last line written (null before the first probed turn).</summary>
        public static string? Last { get; private set; }

        static List<(string key, double ms)>? handlers;
        static List<(string key, double ms)>? sections;
        static long turnStart;
        static double stepMs;
        static int turn;
        static DateTime date;

        /// <summary>Opens a probed turn when <see cref="On"/>; otherwise does nothing.</summary>
        public static void BeginTurn(int turnNumber, DateTime turnDate)
        {
            if (!On) return;
            handlers ??= new List<(string, double)>(); sections ??= new List<(string, double)>();
            handlers.Clear(); sections.Clear();
            turn = turnNumber; date = turnDate; stepMs = 0;
            Active = true;
            turnStart = Stopwatch.GetTimestamp();
        }

        /// <summary>A timestamp to time from, or 0 when no turn is probed (no clock read).</summary>
        public static long Start() => Active ? Stopwatch.GetTimestamp() : 0;

        /// <summary>Adds the time since <paramref name="from"/> to section <paramref name="name"/> and returns now, the start of
        /// the next lap. Returns 0 at once when no turn is probed.</summary>
        public static long Lap(string name, long from)
        {
            if (!Active) return 0;
            var now = Stopwatch.GetTimestamp();
            var ms = Ms(from, now);
            for (int i = 0; i < sections!.Count; i++)
                if (sections[i].key == name) { sections[i] = (name, sections[i].ms + ms); return now; }
            sections.Add((name, ms));
            return now;
        }

        /// <summary>The sim step's time (SimBridge.EndTurn's step call).</summary>
        public static void Step(long from)
        {
            if (!Active) return;
            stepMs += Ms(from, Stopwatch.GetTimestamp());
        }

        /// <summary>One handler of <paramref name="evt"/> ran from <paramref name="from"/> until now.</summary>
        public static void Handler(string evt, Delegate handler, long from)
        {
            if (!Active) return;
            var ms = Ms(from, Stopwatch.GetTimestamp());
            var key = evt + "/" + HandlerName(handler);
            int same = 1;
            foreach (var (k, _) in handlers!) if (k == key || k.StartsWith(key + "#", StringComparison.Ordinal)) same++;
            handlers.Add((same == 1 ? key : key + "#" + same.ToString(CultureInfo.InvariantCulture), ms));
        }

        /// <summary>Closes the probed turn and writes its line through <paramref name="log"/> (the console in the game).
        /// Returns the line, or null when no turn was probed.</summary>
        public static string? EndTurn(Action<string> log)
        {
            if (!Active) return null;
            var total = Ms(turnStart, Stopwatch.GetTimestamp());
            Active = false;
            Last = Line(turn, date, total, stepMs, handlers!, sections!);
            log(Last);
            return Last;
        }

        /// <summary>The line's format (see the class summary).</summary>
        public static string Line(int turnNumber, DateTime turnDate, double totalMs, double simMs,
            IReadOnlyList<(string key, double ms)> handlerTimes, IReadOnlyList<(string key, double ms)> sectionTimes)
        {
            var b = new StringBuilder(Prefix);
            b.Append(" v=").Append(Version);
            b.Append(" turn=").Append(turnNumber.ToString(CultureInfo.InvariantCulture));
            b.Append(" date=").Append(turnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            b.Append(" total_ms=").Append(F(totalMs));
            b.Append(" step_ms=").Append(F(simMs));
            b.Append(" handlers=").Append(handlerTimes.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var (k, ms) in handlerTimes) b.Append(' ').Append(Key(k)).Append('=').Append(F(ms));
            foreach (var (k, ms) in sectionTimes) b.Append(" @").Append(Key(k)).Append('=').Append(F(ms));
            return b.ToString();
        }

        /// <summary>Made on the first probed handler only, so the flag off allocates nothing, not even at start-up.</summary>
        static class Names
        {
            internal static readonly Regex LocalFunction = new Regex(@"^<([^>]*)>g__([^|]+)\|", RegexOptions.CultureInvariant);
            internal static readonly Regex Lambda = new Regex(@"^<([^>]*)>b__", RegexOptions.CultureInvariant);
        }

        /// <summary>A handler's readable name: its class (the outermost class a compiler-made one sits in) and its method; a
        /// lambda is "Class.Method.lambda" after the method that made it, a local function "Class.Method.Local". The
        /// compiler's numbering is dropped, so the name stays the same when the code around it changes.</summary>
        public static string HandlerName(Delegate handler)
        {
            var m = handler.Method;
            var type = m.DeclaringType;
            while (type != null && type.DeclaringType != null && type.Name.StartsWith("<", StringComparison.Ordinal)) type = type.DeclaringType;
            var owner = type?.Name ?? "?";
            var name = m.Name;
            var local = Names.LocalFunction.Match(name);
            if (local.Success) return owner + "." + local.Groups[1].Value + "." + local.Groups[2].Value;
            var lambda = Names.Lambda.Match(name);
            if (lambda.Success) return owner + "." + lambda.Groups[1].Value + ".lambda";
            return owner + "." + name;
        }

        static string Key(string k) => k.Replace(' ', '_').Replace('=', '_');
        static string F(double ms) => ms.ToString("0.00", CultureInfo.InvariantCulture);
        static double Ms(long from, long to) => (to - from) * 1000.0 / Stopwatch.Frequency;
    }
}
