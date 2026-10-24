using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>R-CTRL-022, DeportNeverDominates (impact plan section 4.1 item 5; Q-TECH-4 as drafted, with Q-DEP-7 (b) and (c);
/// Q-DEP-5 (c): "must hold with a margin"). From each capture state, the state is cloned three times and each clone answers
/// the capture with one outcome, then steps with the reference policy (GoldenRun's orders) to the game's end. At the turn's
/// end and at the end, Deport is no better than Raid or Occupy on any measure, Money included in every state (owner, 22
/// October 2026, #1327: the fixture's deport_money_cost covers Occupy's upkeep to the end and the shared hex). The measures: Money,
/// foreign currency, Food, Arms, Attention, coalition stability, every indicator, the score, the objectives and the day each
/// was met (the war objective's timing), the player's units and their strength, housing, jobs, people, built sites, and not
/// losing the war (Q-DEP-7 (b)). On Social cohesion and International Support it is below both by at least
/// <see cref="Margin"/>. The goldens hold no capture state until a capture is reachable in the real content (#1304), so the
/// fixture states carry the test: town_a captured at full and at minimum strength, with a halutz, with an enemy beside it,
/// and with a second place awaiting, with the fixture's one city moved to town_d's hex, out of reach. "shared hex" keeps the
/// city on town_a's own hex, as Tel Aviv shares Jaffa's: there no city project can be started on the depopulated hex
/// (R-CTRL-020), so the reference policy builds less and spends less (IL 3.0m in the fixture), which the Money cost covers.
/// Food ends the same under all three outcomes in these states, so a Food gain from Deport shows only in
/// DeportTests.TheCostsApplyAtOnce, not here. Each run is also the fixture stand-in for the paired mvp-occupy-cost and mvp-deport-cost scenarios
/// (spec section 3).</summary>
public class DeportNeverDominatesTests
{
    /// <summary>How far below both other outcomes Deport must leave Social cohesion and International Support (Q-DEP-5 (c));
    /// test code, a quarter of the fixture's smaller cost.</summary>
    const double Margin = 0.025;

    static readonly string[] Outcomes = { SettlementControl.Raid, SettlementControl.Occupy, SettlementControl.Deport };

    public static IEnumerable<object[]> CaptureStates() => new[] { "full", "minimum", "halutz", "enemy beside", "two places", "shared hex" }.Select(x => new object[] { x });

    static GameState State(Content c, string name)
    {
        var soldier = DeportTests.Soldier(1, 1, 0);
        var s = name switch
        {
            "full" => DeportTests.Awaiting(c, soldier),
            "minimum" => DeportTests.Awaiting(c, new Unit(1, "soldier", 1, 0, side: Movement.PlayerSide, strength: 1)),
            "halutz" => DeportTests.Awaiting(c, soldier, new Unit(2, "halutz", 1, 0)),
            "enemy beside" => DeportTests.Awaiting(c, soldier, CaptureTests.Enemy(2, 3, 0)),
            "two places" => DeportTests.Awaiting(c, soldier, DeportTests.Soldier(2, 4, 0)),
            _ => DeportTests.Awaiting(c, soldier),
        };
        if (name == "two places") s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        return s;
    }

    [Theory]
    [MemberData(nameof(CaptureStates))]
    public void DeportNeverDominates(string name)
    {
        var shared = name == "shared hex";
        var c = shared ? DeportTests.Load() : DeportTests.Load(files => files["cities.yaml"] = files["cities.yaml"].Replace("id: town_a,", "id: town_d,"));
        Assert.Equal(shared ? "town_a" : "town_d", Assert.Single(c.Cities).Id);
        var s = State(c, name);
        var turnEnd = new Dictionary<string, GameState>();
        var end = new Dictionary<string, GameState>();
        foreach (var outcome in Outcomes)
        {
            var first = Simulation.Step(c, s.Clone(), new Command[] { new ResolveCapture("town_a", outcome) });
            Assert.Empty(first.Rejected);
            turnEnd[outcome] = first.State;
            end[outcome] = ToTheEnd(c, first.State);
        }
        foreach (var at in new[] { turnEnd, end })
        {
            var raid = Measures(at[SettlementControl.Raid]);
            var occupy = Measures(at[SettlementControl.Occupy]);
            var deport = Measures(at[SettlementControl.Deport]);
            foreach (var key in deport.Keys)
            {
                var margin = key == "ind:morale" || key == "ind:intl_standing" ? Margin : 0.0;
                foreach (var (other, values) in new[] { ("Raid", raid), ("Occupy", occupy) })
                {
                    var v = values.GetValueOrDefault(key, double.MinValue);
                    Assert.True(deport[key] <= v - margin + 1e-9, $"{name}, {(at == end ? "the end" : "the turn's end")}: Deport's {key} is {deport[key]}, {other}'s {v}");
                }
            }
        }
    }

    /// <summary>Steps <paramref name="s"/> with GoldenRun's orders until the game ends.</summary>
    static GameState ToTheEnd(Content c, GameState s)
    {
        while (s.GameOver == null && s.Date < c.End) s = Simulation.Step(c, s, GoldenRun.Orders(c, s)).State;
        return s;
    }

    /// <summary>Every measure compared, each read so that more is better for the player.</summary>
    static Dictionary<string, double> Measures(GameState s)
    {
        var m = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["money"] = s.Treasury - s.Debt, ["fx"] = s.FxReserves, ["food"] = s.Food, ["arms"] = s.Arms, ["attention"] = s.Attention,
            ["stability"] = s.CoalitionStability, ["score"] = s.Score, ["objectives"] = s.Objectives.Count,
            ["units"] = s.Units.Values.Count(u => u.Side == Movement.PlayerSide || u.Side == ""),
            ["strength"] = s.Units.Values.Where(u => u.Side == Movement.PlayerSide).Sum(u => u.Strength),
            ["housing"] = s.Cities.Values.Sum(x => x.HousingCapacity), ["jobs"] = s.Cities.Values.Sum(x => x.JobSlots),
            ["people"] = s.TotalPopulation, ["sites"] = s.Sites.Values.Count(w => w.BuiltOn != null),
            ["not lost"] = s.GameOver == GameOverCodes.DefeatWar || s.GameOver == GameOverCodes.DefeatEconomy ? 0 : 1,
        };
        foreach (var kv in s.Indicators) m["ind:" + kv.Key] = kv.Value;
        foreach (var kv in s.Objectives) m["met:" + kv.Key] = -kv.Value.Ticks;   // earlier is better
        return m;
    }
}
