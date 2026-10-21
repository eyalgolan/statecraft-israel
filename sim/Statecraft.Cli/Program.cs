using System.Globalization;
using Statecraft.Cli;
using Statecraft.Sim;

// Text front end for the headless sim.
//   dotnet run --project Statecraft.Cli -- [--content DIR] [--seed N] [--auto historical --turns N]
//   dotnet run --project Statecraft.Cli -- --preview [--until YYYY-MM-DD] [--set key=value]... [--show id,id] [--every N]
//       writer preview: the historical run with some rules.yaml values changed in memory, against the real content
//   dotnet run --project Statecraft.Cli -- --release-gate [--content DIR]   (exit 0 when shareable, 3 when not)
//   dotnet run --project Statecraft.Cli -- --timeline-report [--content DIR]   (each road not taken and what follows it)
// Interactive commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n (end turn) | q
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string? Arg(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

foreach (var flag in new[] { "--content", "--seed", "--turns", "--auto", "--until", "--set", "--show", "--every" })
{
    var at = Array.IndexOf(args, flag);
    if (at >= 0 && (at + 1 >= args.Length || args[at + 1].StartsWith("--", StringComparison.Ordinal)))
    { Console.Error.WriteLine($"Missing value for {flag}"); return 1; }
}
var autoMode = Arg("--auto");
if (autoMode != null && autoMode != "historical") { Console.Error.WriteLine($"Unknown --auto mode: '{autoMode}'"); return 1; }

string FindContent()
{
    for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "content", "people.yaml"))) return Path.Combine(d.FullName, "content");
    throw new DirectoryNotFoundException();
}

if (Array.IndexOf(args, "--preview") >= 0)
{
    try
    {
        var sets = new List<string>();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--set") sets.Add(args[i + 1]);
        var until = new DateTime(1950, 1, 1);
        var untilText = Arg("--until");
        if (untilText != null && !DateTime.TryParseExact(untilText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out until))
        { Console.Error.WriteLine($"Invalid value for --until: '{untilText}' (use YYYY-MM-DD)"); return 1; }
        ulong pseed = GoldenRun.Seed;
        var pseedText = Arg("--seed");
        if (pseedText != null && !InputParse.TryULong(pseedText, out pseed)) { Console.Error.WriteLine($"Invalid value for --seed: '{pseedText}'"); return 1; }
        int every = 8;
        var everyText = Arg("--every");
        if (everyText != null && !InputParse.TryInt(everyText, out every)) { Console.Error.WriteLine($"Invalid value for --every: '{everyText}'"); return 1; }
        var show = (Arg("--show") ?? "morale,food_supply,treasury,fx").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Console.Write(Preview.Run(Arg("--content") ?? FindContent(), until, pseed, sets, show, every));
        return 0;
    }
    catch (ArgumentException e) { Console.Error.WriteLine($"Preview error: {e.Message}"); return 1; }
    catch (DirectoryNotFoundException) { Console.Error.WriteLine("Content error: content directory not found (use --content <path>)"); return 2; }
    catch (ContentException e) { Console.Error.WriteLine($"Content error: {e.Message}"); return 2; }
}

Content content;
try { content = ContentLoader.Load(Arg("--content") ?? FindContent()); }
catch (DirectoryNotFoundException) { Console.Error.WriteLine("Content error: content directory not found (use --content <path>)"); return 2; }
catch (ContentException e) { Console.Error.WriteLine($"Content error: {e.Message}"); return 2; }

// --release-gate: the sensitivity gate before a build is shared (spec 5.5; tools/package_release.py, Build -shareable).
// Exit 0 and "RELEASE GATE OK" when every sensitive event is sensitivity-reviewed and (R-ALT-011, Q-ALT-9) every
// consequence card and every event with variants has a person's reviewed_by; else exit 3 naming them.
if (Array.IndexOf(args, "--release-gate") >= 0)
{
    var open = ReleaseGate.SensitiveUnreviewed(content);
    var unsigned = ReleaseGate.AlternateUnsigned(content);
    if (open.Count == 0 && unsigned.Count == 0) { Console.WriteLine("RELEASE GATE OK"); return 0; }
    if (open.Count > 0)
        Console.WriteLine("RELEASE GATE REFUSED: the owner's sensitivity review is missing for: " + string.Join(", ", open)
            + " (set status: sensitivity-reviewed in each event's YAML; see docs/m3-release.md)");
    // Its own REFUSED line too: tools/package_release.py keeps only lines containing "RELEASE GATE".
    if (unsigned.Count > 0)
        Console.WriteLine("RELEASE GATE REFUSED: no person has signed off (reviewed_by) these alternate cards: " + string.Join(", ", unsigned)
            + " (see docs/m3-release.md)");
    foreach (var id in unsigned) Console.WriteLine("  unsigned alternate card: " + id);
    return 3;
}

// --timeline-report (R-ALT-011): one line per road not taken, in window order: who proposed it (basis), the consequence
// cards that follow it, the events whose variants name it, and the record's cards it suppresses (they require the
// historical option). A writer's coverage view; "-" for none.
if (Array.IndexOf(args, "--timeline-report") >= 0)
{
    static bool Names(EventRequirement r, GameEvent e, EventOption o) => r.Event == e.Id && (r.Diverged || r.Option == o.Id);
    static string Ids(IEnumerable<GameEvent> es)
    {
        var ids = es.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return ids.Count == 0 ? "-" : string.Join(",", ids);
    }
    foreach (var e in content.Events.Where(x => Timeline.HistoricalOption(x) != null).OrderBy(x => x.From).ThenBy(x => x.Id, StringComparer.Ordinal))
    {
        var record = Timeline.HistoricalOption(e)!.Id;
        foreach (var o in e.Options.Where(x => !x.Historical))
            Console.WriteLine($"{e.Id}/{o.Id}  basis={(o.Alternative?.Basis is { Length: > 0 } b ? b : "-")}"
                + $"  cards={Ids(content.Events.Where(k => k.Kind == EventKinds.Consequence && k.Requires.Any(r => Names(r, e, o))))}"
                + $"  variants={Ids(content.Events.Where(k => k.Variants.Any(v => v.When.Any(r => Names(r, e, o)))))}"
                + $"  breaks={Ids(content.Events.Where(k => k.Requires.Any(r => r.Event == e.Id && !r.Diverged && r.Option == record)))}");
    }
    return 0;
}

ulong seed = 1948;
int turns = 1000;
var seedText = Arg("--seed");
if (seedText != null && !InputParse.TryULong(seedText, out seed)) { Console.Error.WriteLine($"Invalid value for --seed: '{seedText}'"); return 1; }
var turnsText = Arg("--turns");
if (turnsText != null && !InputParse.TryInt(turnsText, out turns)) { Console.Error.WriteLine($"Invalid value for --turns: '{turnsText}'"); return 1; }
var state = Simulation.NewGame(content, seed, Mode.Historical);
bool auto = autoMode == "historical";

for (int t = 0; t < turns && state.GameOver == null; t++)
{
    Briefing(content, state);
    var commands = auto ? HistoricalAI.Commands(content, state).ToList() : ReadCommands();
    if (commands == null) break;
    var result = Simulation.Step(content, state, commands);
    foreach (var r in result.Rejected) Console.WriteLine($"  rejected: {r.Reason}");
    foreach (var w in result.Why.Where(w => w.System != "effects" && w.System != "economy")) Console.WriteLine($"  {w}");
    state = result.State;
}
Console.WriteLine($"End {state.Date:yyyy-MM-dd}: population {state.TotalPopulation:N0}, level {state.Rung}, treasury {state.Treasury:0.0}, debt {state.Debt:0.0}, fx {state.FxReserves:0.0}, stability {state.CoalitionStability:0.00}{(state.GameOver != null ? ", GAME OVER: " + state.GameOver : "")}");
Console.WriteLine($"State hash {state.Hash()}");
return 0;

static void Briefing(Content c, GameState s)
{
    var gov = Offices.Government(c, s.Date);
    Console.WriteLine($"== {s.Date:yyyy-MM-dd} | turn {s.Turn} | {gov?.Name} | security level {s.Rung} | attention {s.Attention:0.0}");
    foreach (var id in s.PendingEvents)
    {
        var e = c.Events.First(x => x.Id == id);
        var fillDate = EventSystem.FillDate(e, s.Date);   // after the window ends, name the last in-window holder
        Console.WriteLine($"  EVENT {e.Id}: {Offices.Fill(c, e.Title, fillDate)} - {EventSystem.Render(c, e, s.Date)}");
        foreach (var o in e.Options) Console.WriteLine($"    {o.Id}: {Offices.Fill(c, o.Text, fillDate)}");
    }
}

static List<Command>? ReadCommands()
{
    var list = new List<Command>();
    while (true)
    {
        Console.Write("> ");
        var parts = (Console.ReadLine() ?? "q").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;
        switch (parts[0])
        {
            case "q" when parts.Length == 1: return null;
            case "n" when parts.Length == 1: return list;
            case "c" when parts.Length == 3: list.Add(new ChooseEventOption(parts[1], parts[2])); break;
            case "p" when parts.Length == 3:
                if (InputParse.TryDouble(parts[2], out var level)) list.Add(new SetPolicy(parts[1], level)); else Console.WriteLine($"Invalid number: '{parts[2]}'");
                break;
            case "m" when parts.Length == 2:
                if (InputParse.TryDouble(parts[1], out var mob)) list.Add(new SetMobilization(mob)); else Console.WriteLine($"Invalid number: '{parts[1]}'");
                break;
            case "s" when parts.Length == 2:
                if (InputParse.TryInt(parts[1], out var delta)) list.Add(new SecurityOrder(delta)); else Console.WriteLine($"Invalid number: '{parts[1]}'");
                break;
            default: Console.WriteLine("commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n | q"); break;
        }
    }
}
