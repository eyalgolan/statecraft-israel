using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>#1707, R-UNIT-011: combat experience from survived engagements under exact gdd-0.3, on today's aggregate units
/// (owner, 2 November 2026: "Build it now (Recommended)"). The owner's approved answers on #1668 (1 November 2026,
/// https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6553884547): Q14 "Yes. Artillery fires alone from
/// range, and it earns experience for each engagement it survives, like any unit."; Q15 "It stops."; Q16 "Under gdd-0.3, yes:
/// veterancy comes from combat experience. gdd-0.1 keeps R-UNIT-004." Gdd03MovementFortTests' board and the real catalogue
/// (combat_xp_gain 1, combat_xp_full 5); the fixture's two-face table agrees on both faces, so no test depends on the draw:
/// below 2:1 both sides lose 1, from 2:1 the defenders lose 1 and retreat.</summary>
public class Gdd03CombatXpTests
{
    static Content Load() => Gdd03MovementFortTests.Load();
    static Content Legacy() => Gdd03MovementFortTests.Load(SaveGame.GddRuleset);

    static Unit Ours(int id, int q, int r, int strength, string type = "recruit", int xp = 0) =>
        new Unit(id, type, q, r, side: "israel", strength: strength, combatXp: xp);
    static Unit Theirs(int id, int q, int r, int strength, int xp = 0) =>
        new Unit(id, "enemy", q, r, side: "egypt", strength: strength, combatXp: xp);

    static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);

    static PreviewResult Attack(Content c, GameState s, int unit, int q, int r)
    {
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(unit, Combat.Attack, q, r) });
        Assert.Empty(now.Rejected);
        return now;
    }

    // R-UNIT-011 (#1707 row 1): an attacker and a defender that both survive (6 against 6: both lose 1) each gain 1.
    [Fact]
    public void AttackerAndDefenderThatSurviveGainOne()
    {
        var c = Load();
        var now = Attack(c, With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 6)), 1, 3, 1);
        Assert.Equal((5, 1), (now.State.Units[1].Strength, now.State.Units[1].CombatXp));
        Assert.Equal((5, 1), (now.State.Units[2].Strength, now.State.Units[2].CombatXp));
    }

    // R-UNIT-011: a defender that loses and retreats has survived the engagement.
    [Fact]
    public void RetreatedDefenderGainsOne()
    {
        var c = Load();
        var now = Attack(c, With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 2)), 1, 3, 1);
        var d = now.State.Units[2];
        Assert.Equal((4, 1, 1, 1), (d.Q, d.R, d.Strength, d.CombatXp));
        Assert.Equal(1, now.State.Units[1].CombatXp);
    }

    // R-UNIT-011: a destroyed or surrendered unit gains nothing: its record is gone and no id is used. A unit that stood by gains nothing.
    [Fact]
    public void DestroyedAndSurrenderedGainNothing()
    {
        var c = Load();
        var s = With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 1));
        var now = Attack(c, s, 1, 3, 1);
        Assert.False(now.State.Units.ContainsKey(2));
        Assert.Equal(s.NextId, now.State.NextId);
        Assert.Equal(1, now.State.Units[1].CombatXp);

        // (5, 0) is a corner: its board neighbours (4, 0), (4, 1) and (5, 1) all hold Israeli units, so it surrenders.
        var t = With(c, Ours(1, 4, 0, 6), Ours(3, 4, 1, 1), Ours(4, 5, 1, 1), Theirs(2, 5, 0, 2));
        var held = Attack(c, t, 1, 5, 0);
        Assert.False(held.State.Units.ContainsKey(2));
        Assert.Contains(held.Why, w => w.Key == WhyKeys.SurrenderedPrisoners);
        Assert.Equal(t.NextId, held.State.NextId);
        Assert.Equal((1, 0, 0), (held.State.Units[1].CombatXp, held.State.Units[3].CombatXp, held.State.Units[4].CombatXp));
    }

    // R-UNIT-011, Q14: artillery that fires alone from range survives (the guns take no attacker loss) and gains 1.
    [Fact]
    public void ArtilleryFiringAloneGainsOne()
    {
        var c = Load();
        var now = Attack(c, With(c, Ours(1, 1, 1, 6, "gun"), Theirs(2, 3, 1, 6)), 1, 3, 1);
        Assert.Equal((6, 1), (now.State.Units[1].Strength, now.State.Units[1].CombatXp));
        Assert.Equal(1, now.State.Units[2].CombatXp);
    }

    // R-UNIT-011, Q15: experience stops at combat_xp_full, for the guns and the enemy as for anyone. A veteran_soldier is a
    // veteran already (OQ1) and gains none.
    [Fact]
    public void XpStopsAtFullAndAVeteranGainsNone()
    {
        var c = Load();
        var now = Attack(c, With(c, Ours(1, 1, 1, 6, "gun", 5), Theirs(2, 3, 1, 6, 5)), 1, 3, 1);
        Assert.Equal((5, 5), (now.State.Units[1].CombatXp, now.State.Units[2].CombatXp));
        var vet = Attack(c, With(c, Ours(1, 2, 1, 6, "veteran_soldier"), Theirs(2, 3, 1, 6, 2)), 1, 3, 1);
        Assert.Equal((0, 3), (vet.State.Units[1].CombatXp, vet.State.Units[2].CombatXp));
    }

    // R-UNIT-011 (#1707: "service time and travel do not count"): moving, ordering and whole turns of service give no XP.
    [Fact]
    public void MovesOrdersAndServiceGiveNoXp()
    {
        var c = Load();
        var s = With(c, Ours(1, 0, 1, 6, xp: 2));
        var next = Simulation.Step(c, s, new Command[] { new Move(1, 1, 1), new UnitOrder(1, Orders.Fortify) }).State;
        next = Simulation.Step(c, next, new Command[0]).State;
        Assert.Equal((1, 2, 2), (next.Units[1].Q, next.Units[1].Service, next.Units[1].CombatXp));
    }

    // R-UNIT-011, R-COMBAT-002 (#1707 row 2): the experience draws nothing: the engagement draws its one face, as under gdd-0.1.
    [Fact]
    public void NoExtraDraw()
    {
        foreach (var c in new[] { Load(), Legacy() })
        {
            var s = With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 6));
            var rng = new Rng(s.RngState);
            rng.NextDouble();
            Assert.Equal(rng.State, Attack(c, s, 1, 3, 1).State.RngState);
        }
    }

    // R-UNIT-011, #1707 plan section 3.2: a unit's experience survives every change that rebuilds its record (13 sim sites
    // on main): a move, a go-to and its arrival, fortify and its setting, hold, an attack's own record, a loss, a retreat, an
    // enemy attack order and its end, a truce ending one, a turn of service and a promotion by service (gdd-0.1), a
    // replacement joining it, and a withdrawal by Raid. EverySimCopyOfAUnitPassesItsCombatXp covers the halutz-only sites.
    [Fact]
    public void XpSurvivesEveryUnitChange()
    {
        var c = Load();
        var spent = new Dictionary<int, int>(); var why = new List<Cause>();

        var s = With(c, Ours(1, 0, 1, 6, xp: 3));
        Assert.Null(Movement.Move(c, s, 1, 1, 1, spent, why));                     // Movement.Move
        Assert.Equal(3, s.Units[1].CombatXp);
        Assert.Null(Movement.GiveGoTo(c, s, 1, 5, 1, spent, why));                 // Movement.GiveGoTo
        Assert.Equal(3, s.Units[1].CombatXp);
        for (int turn = 0; turn < 3; turn++) { spent.Clear(); Movement.ContinueRoutes(c, s, spent, why); }   // arrival: Stop
        Assert.Equal(("", 5, 3), (s.Units[1].Order, s.Units[1].Q, s.Units[1].CombatXp));
        Assert.Null(Orders.Give(c, s, 1, Orders.Fortify, why));                    // Orders.With: fortifying
        Orders.Settle(s, why);                                                     // Orders.With: fortify
        Assert.Equal((Orders.Fortify, 3), (s.Units[1].Order, s.Units[1].CombatXp));
        Assert.Null(Orders.Give(c, s, 1, Orders.Hold, why));
        Assert.Equal((Orders.Hold, 3), (s.Units[1].Order, s.Units[1].CombatXp));

        // An attack (Strike's own record), its loss (Combat.Lose) and the defender's retreat each keep the old XP: 3 + 1.
        var fight = Attack(c, With(c, Ours(1, 2, 1, 6, xp: 3), Theirs(2, 3, 1, 6, 3)), 1, 3, 1).State;
        Assert.Equal((5, 4, 5, 4), (fight.Units[1].Strength, fight.Units[1].CombatXp, fight.Units[2].Strength, fight.Units[2].CombatXp));
        var rout = Attack(c, With(c, Ours(1, 2, 1, 6, xp: 3), Theirs(2, 3, 1, 2, 3)), 1, 3, 1).State;
        Assert.Equal((4, 4), (rout.Units[2].Q, rout.Units[2].CombatXp));

        // The enemy's standing attack (Combat.Give) and its end with no enemy there (Combat.End).
        var e = With(c, Theirs(2, 3, 1, 6, 3), Ours(1, 2, 1, 6));
        Assert.Null(Combat.Give(c, e, 2, 2, 1, why, enemy: true));
        Assert.Equal((Combat.Attack, 3), (e.Units[2].Order, e.Units[2].CombatXp));
        e.Units.Remove(1);
        Combat.Resolve(c, e, why);
        Assert.Equal(("", 3), (e.Units[2].Order, e.Units[2].CombatXp));

        // A turn of service under gdd-0.3 and, under gdd-0.1, the promotion by service (Military.Serve).
        var served = With(c, Ours(1, 0, 1, 6, xp: 3));
        Military.Serve(c, served, c.RulesAt(served.Date).Mvp!, why);
        Assert.Equal((1, 3), (served.Units[1].Service, served.Units[1].CombatXp));
        var old = Legacy();
        var promoted = With(old, new Unit(1, "recruit", 0, 1, 2, side: "israel", strength: 6, combatXp: 3));
        Military.Serve(old, promoted, old.RulesAt(promoted.Date).Mvp!, why);
        Assert.Equal(("veteran_soldier", 3), (promoted.Units[1].Type, promoted.Units[1].CombatXp));

        // A truce ends an enemy's standing attack (EnemyAI.Order), on the real content's 15 June 1948 truce turn.
        var real = Real.Value;
        var t = Simulation.NewGame(real, GoldenRun.Seed, Mode.Historical);
        var (id, u) = t.Units.First(kv => kv.Value.Side != "" && kv.Value.Side != Movement.PlayerSide);
        t.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Combat.Attack, $"{u.Q + 1},{u.R}", u.HomeCity, u.Full, 3);
        t.Date = new DateTime(1948, 6, 15);
        EnemyAI.Order(real, t, why, new DateTime(1948, 7, 15));
        Assert.Equal(3, t.Units[id].CombatXp);

        // A replacement cohort joining the unit (Training.Progress, R-TRAIN-004), on the real content's Tel Aviv camp.
        var r = Simulation.NewGame(real, GoldenRun.Seed, Mode.Historical);
        var tlv = real.SettlementHexes.Single(h => h.Id == "tel_aviv");
        var rid = r.NextId++;
        r.Units.Add(rid, new Unit(rid, "recruit", tlv.Q, tlv.R, side: "israel", strength: 2, homeCity: "tel_aviv", full: 5, combatXp: 3));
        r.Arms = 100;
        Assert.Null(Training.Reinforce(real, r, rid, "tel_aviv", 3, why));
        for (int turn = 0; turn < real.UnitTypes.Single(x => x.Id == "recruit").TrainingTurns; turn++) Training.Progress(real, r, why);
        Assert.Equal((5, 3), (r.Units[rid].Strength, r.Units[rid].CombatXp));

        // A withdrawal by Raid (SettlementControl's move out, R-CTRL-010), on RaidTests' fixture.
        var raid = CaptureTests.Load(CaptureTests.AllTargets);
        var w = CombatTests.With(raid, new Unit(1, "soldier", 1, 0, side: "israel", strength: 2, combatXp: 3));
        w.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, w.Date);
        var left = Simulation.Step(raid, w, new Command[] { new ResolveCapture("town_a", SettlementControl.Raid) }).State;
        Assert.Equal((2, 0, 3), (left.Units[1].Q, left.Units[1].R, left.Units[1].CombatXp));
    }

    static readonly Lazy<Content> Real = new Lazy<Content>(() => ContentLoader.Load(RealContentTests.ContentRoot()));

    // R-UNIT-011: the structural check behind XpSurvivesEveryUnitChange. Every sim site that rebuilds a unit from an existing
    // one, new Unit(x.Id, ...), passes x.CombatXp, the halutz sites (Corridors, Projects) included, so a new field is never
    // dropped by a copy. At least the 16 copy sites main had when #1707 started. The one exception is the award itself
    // (Combat.Experience), which writes the new count.
    [Fact]
    public void EverySimCopyOfAUnitPassesItsCombatXp()
    {
        var sim = Path.Combine(Path.GetDirectoryName(RealContentTests.ContentRoot())!, "sim", "Statecraft.Sim");
        int copies = 0, awards = 0;
        foreach (var file in Directory.GetFiles(sim, "*.cs"))
        {
            var source = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(source, @"new Unit\((\w+)\.Id,"))
            {
                int depth = 0, end = m.Index + "new Unit".Length;
                for (; end < source.Length; end++)
                {
                    if (source[end] == '(') depth++;
                    else if (source[end] == ')' && --depth == 0) break;
                }
                var call = source.Substring(m.Index, end - m.Index + 1);
                if (Path.GetFileName(file) == "Combat.cs" && call.EndsWith("u.Full, xp)")) { awards++; continue; }
                Assert.True(call.Contains(m.Groups[1].Value + ".CombatXp"), Path.GetFileName(file) + " drops CombatXp: " + call);
                copies++;
            }
        }
        Assert.Equal(1, awards);
        Assert.True(copies >= 16, copies + " copy sites found");
    }

    // R-UNIT-011, R-UNIT-002: XP is hashed only when it is not 0, after |full=.
    [Fact]
    public void XpIsHashedOnlyWhenNotZero()
    {
        Assert.Equal("1:recruit@2,1|side=israel|strength=6", new Unit(1, "recruit", 2, 1, side: "israel", strength: 6).Canonical());
        Assert.Equal("1:recruit@2,1|side=israel|strength=5|full=6|xp=1",
            new Unit(1, "recruit", 2, 1, side: "israel", strength: 5, full: 6, combatXp: 1).Canonical());
    }

    // R-UNIT-011, R-UNIT-004: gdd-0.1 never writes XP: the same engagement leaves every unit's record free of it.
    [Fact]
    public void Gdd01NeverWritesXp()
    {
        var c = Legacy();
        var now = Attack(c, With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 6)), 1, 3, 1);
        Assert.All(now.State.Units.Values, u => Assert.Equal(0, u.CombatXp));
        Assert.DoesNotContain("|xp=", now.State.Describe());
    }

    // R-UNIT-011, R-TURN-004: the player's attack is fought in the preview and again in the turn, with the same XP.
    [Fact]
    public void PreviewAndStepAgree()
    {
        var c = Load();
        var s = With(c, Ours(1, 2, 1, 6), Theirs(2, 3, 1, 6));
        var cmd = new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) };
        var preview = Simulation.Preview(c, s, cmd).State;
        var step = Simulation.Step(c, s, cmd).State;
        Assert.Equal((1, 1), (preview.Units[1].CombatXp, preview.Units[2].CombatXp));
        Assert.Equal((step.Units[1].CombatXp, step.Units[2].CombatXp), (preview.Units[1].CombatXp, preview.Units[2].CombatXp));
    }

    // R-UNIT-011: XP is derived from the commands, so a replay of the same commands from the same start gives the same hash.
    [Fact]
    public void ReplayKeepsXp()
    {
        var c = Load();
        GameState Play()
        {
            var s = With(c, Ours(1, 2, 1, 9), Theirs(2, 3, 1, 9));   // even, so the enemy never attacks back (R-ENEMY-003)
            for (int turn = 0; turn < 3; turn++) s = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) }).State;
            return s;
        }
        var first = Play();
        Assert.Equal((3, 3), (first.Units[1].CombatXp, first.Units[2].CombatXp));
        Assert.Contains("|xp=3", first.Describe());
        Assert.Equal(first.Hash(), Play().Hash());
    }

    // R-UNIT-011: gdd-0.3 reads both values from the catalogue at load, with no fallback: Deferred, missing or 0 is refused.
    [Theory]
    [InlineData("military.combat_xp_gain", "basis: Deferred, expression: pending, note: fixture")]
    [InlineData("military.combat_xp_full", "basis: Agreed, values: [0], note: fixture")]
    [InlineData("military.combat_xp_gain", null)]
    [InlineData("military.combat_xp_full", null)]
    public void CatalogueRefusal(string key, string? row)
    {
        var f = Gdd03MovementFortTests.Files();
        var catalogue = f[Gdd03BalanceCatalogue.FileName];
        f[Gdd03BalanceCatalogue.FileName] = row == null ? catalogue.Replace("\"" + key + "\":", "\"unused.fixture\":")
            : Regex.Replace(catalogue, "(?m)^  \"" + Regex.Escape(key) + "\":\\n(?:    .*\\n)*", "  \"" + key + "\": { " + row + " }\n");
        Assert.NotEqual(catalogue, f[Gdd03BalanceCatalogue.FileName]);
        Assert.Contains(key, Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(f))).Message);
    }

    // R-UNIT-011, R-UNIT-004, Q16: five survived engagements make a recruit a veteran_soldier, with R-UNIT-004's Why cause.
    [Fact]
    public void RecruitBecomesVeteranAfterFiveSurvivedEngagements()
    {
        var c = Load();
        var s = With(c, Ours(1, 2, 1, 9), Theirs(2, 3, 1, 30));
        for (int i = 1; i <= 5; i++)
        {
            var now = Attack(c, s, 1, 3, 1);
            s = now.State;
            Assert.Equal((i, i == 5 ? "veteran_soldier" : "recruit"), (s.Units[1].CombatXp, s.Units[1].Type));
            Assert.Equal(i == 5 ? 1 : 0, now.Why.Count(w => w.Key == CauseKeys.RecruitsVeteran));
        }
        var vet = s.Units[1];
        Assert.Equal((1, 2, 1, 0, "israel", 4, 9), (vet.Id, vet.Q, vet.R, vet.Service, vet.Side, vet.Strength, vet.Full));
    }

    // R-UNIT-011 (#1707: "New qualification applies from the next engagement"): the promoting engagement is fought at the
    // recruit's ratio. The fixture's veteran counts at 300% here so the two ratios differ.
    [Fact]
    public void PromotedInAnEngagementFoughtItAsARecruit()
    {
        var c = Load();
        c.UnitTypes.Single(t => t.Id == "veteran_soldier").CombatPercent = 300;
        var s = With(c, Ours(1, 2, 1, 9, xp: 4), Theirs(2, 3, 1, 30));
        var before = Combat.Preview(c, s, 1, 3, 1)!.Value.RatioPercent;
        var now = Attack(c, s, 1, 3, 1);
        Assert.Equal("veteran_soldier", now.State.Units[1].Type);
        Assert.Equal(before.ToString(System.Globalization.CultureInfo.InvariantCulture), now.Why.Single(w => w.Key == WhyKeys.Engagement).Args[1]);
        Assert.NotEqual(before, Combat.Preview(c, now.State, 1, 3, 1)!.Value.RatioPercent);
    }

    // R-UNIT-004 re-pointed, Q16: under gdd-0.3 service is a statistic only: a recruit with 20 turns and no XP stays one.
    [Fact]
    public void Gdd03ServiceNeverPromotes()
    {
        var c = Load();
        var s = With(c, new Unit(1, "recruit", 0, 1, 20, side: "israel", strength: 6));
        var why = new List<Cause>();
        Military.Serve(c, s, c.RulesAt(s.Date).Mvp!, why);
        Assert.Equal(("recruit", 21), (s.Units[1].Type, s.Units[1].Service));
        Assert.Empty(why);
    }

    // R-UNIT-004 unchanged under gdd-0.1: the fixture's veteran_soldier_service (3) still promotes, with its cause.
    [Fact]
    public void Gdd01ServiceStillPromotes()
    {
        var c = Legacy();
        var s = With(c, new Unit(1, "recruit", 0, 1, 2, side: "israel", strength: 6));
        var why = new List<Cause>();
        Military.Serve(c, s, c.RulesAt(s.Date).Mvp!, why);
        Assert.Equal(("veteran_soldier", 3), (s.Units[1].Type, s.Units[1].Service));
        Assert.Single(why, w => w.Key == CauseKeys.RecruitsVeteran);
    }
}
