using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Sim
{
    /// <summary>A golden playthrough with scripted player choices (#296). The hash goldens use the reference player (HistoricalAI's orders, then the
    /// GDD orders it states), so they cover the path history took; a scenario covers a path a player might take instead. File: sim/Statecraft.Sim.Tests/scenarios/&lt;name&gt;.yaml.
    /// <code>
    /// name: reckless
    /// seed: 1948          # optional, default GoldenRun.Seed
    /// until: 1949-01-01   # the run stops when the date reaches it
    /// steps:
    ///   - on: 1948-05-15  # the commands go in on the first turn that starts on or after this date, once
    ///     commands:
    ///       - { kind: set_policy, policy: rationing, level: 1.0 }
    /// </code>
    /// Kinds: set_policy (policy, level), allocate_budget (shares: {office: share}), choose_event_option (event, option),
    /// set_mobilization (level), security_order (delta), appoint (office, person), map_action (action, q, r), assign_workers
    /// (city, role, count), train (city, track, count), cancel_training (cohort), pioneer_work (unit, action, site), stop_work
    /// (unit), move (unit, q, r; GDD content only), unit_order (unit, order, and q, r for an attack; GDD content only). A turn with no
    /// scripted command passes; events nobody answered are delegated, as in the game. A command the sim rejects fails the run.</summary>
    public sealed class Scenario
    {
        public string Name { get; set; } = "";
        public ulong? Seed { get; set; }
        public string Until { get; set; } = "";
        public List<ScenarioStep> Steps { get; set; } = new List<ScenarioStep>();

        public static Scenario Parse(string yaml)
        {
            var d = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            var s = d.Deserialize<Scenario>(yaml) ?? throw new InvalidDataException("the scenario file is empty");
            if (string.IsNullOrWhiteSpace(s.Name)) throw new InvalidDataException("a scenario needs a name");
            s.UntilDate = Date(s.Until, "until");
            foreach (var step in s.Steps)
            {
                step.OnDate = Date(step.On, "on");
                foreach (var c in step.Commands) step.Built.Add(Build(c));
            }
            return s;
        }

        [YamlIgnore] public DateTime UntilDate { get; private set; }

        static DateTime Date(string text, string field) =>
            DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d
                : throw new InvalidDataException($"'{field}' must be yyyy-MM-dd, got '{text}'");

        static string Str(Dictionary<string, object> c, string key) =>
            c.TryGetValue(key, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture)! : throw new InvalidDataException($"command '{c["kind"]}' needs '{key}'");
        static double Num(Dictionary<string, object> c, string key) =>
            double.TryParse(Str(c, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : throw new InvalidDataException($"command '{c["kind"]}': '{key}' must be a number");

        /// <summary>The command kinds a scenario can script, with their fields (CommandGuardTests checks every kind is here).</summary>
        static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            ["set_policy"] = new[] { "policy", "level" }, ["allocate_budget"] = new[] { "shares" },
            ["choose_event_option"] = new[] { "event", "option" }, ["set_mobilization"] = new[] { "level" },
            ["security_order"] = new[] { "delta" }, ["appoint"] = new[] { "office", "person" },
            ["map_action"] = new[] { "action", "q", "r" }, ["assign_workers"] = new[] { "city", "role", "count" },
            ["train"] = new[] { "city", "track", "count" }, ["cancel_training"] = new[] { "cohort" },
            ["pioneer_work"] = new[] { "unit", "action", "site" }, ["stop_work"] = new[] { "unit" },
            ["unit_order"] = new[] { "unit", "order", "q", "r" },   // q and r: an attack's hex only (R-COMBAT-001)
            ["start_project"] = new[] { "project", "city", "q", "r" }, ["cancel_project"] = new[] { "project" },
            ["move"] = new[] { "unit", "q", "r" },
            ["buy_arms"] = new[] { "count" }, ["resolve_capture"] = new[] { "settlement", "outcome" },
        };

        public static IReadOnlyCollection<string> Kinds => Allowed.Keys;
        internal static IReadOnlyList<string> FieldsOf(string kind) => Allowed[kind];

        internal static Command Build(Dictionary<string, object> c)
        {
            if (!c.ContainsKey("kind")) throw new InvalidDataException("a command needs 'kind'");
            var kind = Str(c, "kind");
            var allowed = Allowed;
            if (!allowed.TryGetValue(kind, out var keys)) throw new InvalidDataException($"unknown command kind '{kind}' (known: {string.Join(", ", allowed.Keys)})");
            var extra = c.Keys.Where(k => k != "kind" && !keys.Contains(k)).ToList();
            if (extra.Count > 0) throw new InvalidDataException($"command '{kind}' has unknown field '{extra[0]}'");
            switch (kind)
            {
                case "set_policy": return new SetPolicy(Str(c, "policy"), Num(c, "level"));
                case "allocate_budget":
                    if (!(c.TryGetValue("shares", out var sh) && sh is Dictionary<object, object> map)) throw new InvalidDataException("allocate_budget needs 'shares' as a map");
                    return new AllocateBudget(map.ToDictionary(kv => Convert.ToString(kv.Key, CultureInfo.InvariantCulture)!, kv => double.Parse(Convert.ToString(kv.Value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture)));
                case "choose_event_option": return new ChooseEventOption(Str(c, "event"), Str(c, "option"));
                case "set_mobilization": return new SetMobilization(Num(c, "level"));
                case "security_order": return new SecurityOrder(Whole(c, "delta"));
                case "appoint": return new Appoint(Str(c, "office"), Str(c, "person"));
                case "map_action": return new MapAction(Str(c, "action"), Whole(c, "q"), Whole(c, "r"));
                case "assign_workers": return new AssignWorkers(Str(c, "city"), Str(c, "role"), Whole(c, "count"));
                case "train": return new Train(Str(c, "city"), Str(c, "track"), Whole(c, "count"));
                case "cancel_training": return new CancelTraining(Whole(c, "cohort"));
                case "pioneer_work": return new PioneerWork(Whole(c, "unit"), Str(c, "action"), Str(c, "site"));
                case "stop_work": return new StopWork(Whole(c, "unit"));
                case "unit_order": return new UnitOrder(Whole(c, "unit"), Str(c, "order"), c.ContainsKey("q") ? Whole(c, "q") : (int?)null, c.ContainsKey("r") ? Whole(c, "r") : (int?)null);
                case "start_project": return new StartProject(Str(c, "project"), Str(c, "city"), Whole(c, "q"), Whole(c, "r"));
                case "cancel_project": return new CancelProject(Whole(c, "project"));
                case "move": return new Move(Whole(c, "unit"), Whole(c, "q"), Whole(c, "r"));
                case "buy_arms": return new BuyArms(Whole(c, "count"));
                case "resolve_capture": return new ResolveCapture(Str(c, "settlement"), Str(c, "outcome"));
                default: throw new InvalidDataException($"command kind '{kind}' has no builder");
            }
        }

        /// <summary>A whole number: 1.5 is refused instead of silently becoming 1.</summary>
        static int Whole(Dictionary<string, object> c, string key)
        {
            var v = Num(c, key);
            if (v != Math.Floor(v)) throw new InvalidDataException($"'{key}' must be a whole number, got {v.ToString(CultureInfo.InvariantCulture)}");
            return (int)v;
        }

        /// <summary>Plays the scenario and returns the final state. Fails with the date, the order and the sim's reason when a command is rejected.</summary>
        public GameState Play(Content c)
        {
            var s = Simulation.NewGame(c, Seed ?? GoldenRun.Seed, Mode.Historical);
            var pending = Steps.OrderBy(x => x.OnDate).ToList();
            for (int turn = 0; turn < 1000 && s.Date < UntilDate && s.GameOver == null; turn++)
            {
                var now = new List<Command>();
                while (pending.Count > 0 && pending[0].OnDate <= s.Date) { now.AddRange(pending[0].Built); pending.RemoveAt(0); }
                var r = Simulation.Step(c, s, now);
                if (r.Rejected.Count > 0)
                {
                    var x = r.Rejected[0];
                    throw new InvalidOperationException($"scenario '{Name}': on {s.Date:yyyy-MM-dd} the sim rejected {x.Command.Kind} ({Describe(x.Command)}): {x.Reason}");
                }
                s = r.State;
            }
            if (pending.Count > 0) throw new InvalidOperationException($"scenario '{Name}': the step on {pending[0].On} never ran (the run ends {s.Date:yyyy-MM-dd})");
            return s;
        }

        static string Describe(Command c) => c switch
        {
            SetPolicy p => $"{p.Policy} {p.Level}", ChooseEventOption e => $"{e.EventId}/{e.OptionId}", SetMobilization m => m.Level.ToString(CultureInfo.InvariantCulture),
            SecurityOrder o => o.Delta.ToString(CultureInfo.InvariantCulture), Appoint a => $"{a.Office} {a.Person}", MapAction m => $"{m.Action} {m.Q},{m.R}",
            AssignWorkers w => $"{w.City} {w.Role} {w.Count}", Train t => $"{t.City} {t.Track} {t.Count}", CancelTraining t => t.Cohort.ToString(CultureInfo.InvariantCulture),
            PioneerWork p => FormattableString.Invariant($"{p.Unit} {p.Action} {p.Site}"), StopWork w => w.Unit.ToString(CultureInfo.InvariantCulture),
            UnitOrder o => FormattableString.Invariant($"{o.Unit} {o.Order}") + (o.Q is int q && o.R is int r ? FormattableString.Invariant($" {q},{r}") : ""),
            StartProject p => FormattableString.Invariant($"{p.ProjectKind} {p.City} {p.Q},{p.R}"), CancelProject p => p.Project.ToString(CultureInfo.InvariantCulture),
            Move m => FormattableString.Invariant($"{m.Unit} {m.Q},{m.R}"),
            BuyArms a => a.Count.ToString(CultureInfo.InvariantCulture), ResolveCapture r => $"{r.Settlement} {r.Outcome}",
            _ => c.Kind,
        };
    }

    public sealed class ScenarioStep
    {
        public string On { get; set; } = "";
        public List<Dictionary<string, object>> Commands { get; set; } = new List<Dictionary<string, object>>();
        [YamlIgnore] public DateTime OnDate { get; set; }
        [YamlIgnore] public List<Command> Built { get; } = new List<Command>();
    }
}
