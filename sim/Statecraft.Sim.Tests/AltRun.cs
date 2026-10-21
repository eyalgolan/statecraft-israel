using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>A run that leaves the record: the reference player's orders (GoldenRun.Orders) on the golden seed, except that
/// each event in <c>forced</c> is answered with the given option. Test code with a stated policy, like the scenarios.</summary>
public static class AltRun
{
    public static List<GameState> Play(Content c, IReadOnlyDictionary<string, string> forced)
    {
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var run = new List<GameState> { s };
        while (s.GameOver == null && run.Count < 1000)
        {
            var orders = GoldenRun.Orders(c, s)
                .Select(o => o is ChooseEventOption ch && forced.TryGetValue(ch.EventId, out var opt) ? new ChooseEventOption(ch.EventId, opt) : o)
                .ToList();
            s = Simulation.Step(c, s, orders).State;
            // A forced choice goes only through an order the reference player sends; if Attention ran short and a minister
            // answered instead, the run is not the road it claims to be, so it says so.
            foreach (var kv in forced)
                if (s.ChosenOptions.TryGetValue(kv.Key, out var got) && got != kv.Value)
                    throw new System.InvalidOperationException($"AltRun: {kv.Key} was answered {got}, not the forced {kv.Value}");
            run.Add(s);
        }
        return run;
    }

    /// <summary>The choices that lead to <paramref name="card"/>: for each requirement, its option, or for `diverged: true`
    /// the event's first non-historical option in file order; walked back through a parent consequence card.</summary>
    public static Dictionary<string, string> RoadTo(Content c, GameEvent card)
    {
        var road = new Dictionary<string, string>();
        foreach (var r in card.Requires)
        {
            var parent = c.Events.First(x => x.Id == r.Event);
            road[r.Event] = r.Diverged ? parent.Options.First(o => !o.Historical).Id : r.Option;
            if (parent.Kind == EventKinds.Consequence)
                foreach (var kv in RoadTo(c, parent)) road[kv.Key] = kv.Value;
        }
        return road;
    }
}
