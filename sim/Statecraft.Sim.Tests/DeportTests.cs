using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Deport (GDD Phase 6 PR 8, #800), under owner ruling 1, "honest cost, no reward" (GDD; CONTENT_POLICY section 4;
/// impact plan section 4.1). The owner's picks of 18 October 2026 (#783, "Accept all 16 drafts"): Q-DEP-2 (a), every resident
/// the state holds; Q-DEP-3 (a), a DisplacedByPlayer count only; Q-DEP-4 (a), no razing in the MVP; Q-DEP-5 (c), fixed costs,
/// a drag and a lower International Support ceiling; Q-DEP-7 (c). The real content keeps Deport out of play
/// (mvp.deport_in_play: false); these tests reach it through fixture content (Q-TECH-3). The places are CaptureTests'
/// fixture values, not history, and the fixture's costs are 0.2 Social cohesion and 0.1 International Support, a drag of 0.01
/// a turn and a ceiling 0.3 lower, per place.</summary>
public class DeportTests
{
    /// <summary>CaptureTests' fixture with Deport in play and International Support in the model (the fixture's model has
    /// none; the real one has it, model.yaml). <paramref name="edit"/> changes the files further.</summary>
    internal static Content Load(Action<Dictionary<string, string>>? edit = null) => CaptureTests.Load(files =>
    {
        files["rules.yaml"] = files["rules.yaml"].Replace("enemy_held_localities:", "deport_in_play: true\n  enemy_held_localities:");
        files["model.yaml"] = Fixture.Files["model.yaml"].Replace("indicators:\n", "indicators:\n  - id: intl_standing\n    base: 0.5\n    name: International support\n");
        edit?.Invoke(files);
    }, CaptureTests.AllTargets);

    internal static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);

    /// <summary>A new game with <paramref name="units"/> and town_a captured and awaiting the choice.</summary>
    internal static GameState Awaiting(Content c, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);
    static ResolveCapture Answer(string outcome, string id = "town_a") => new ResolveCapture(id, outcome);
    static double Support(GameState s) => s.Indicators["intl_standing"];
    static double Cohesion(GameState s) => s.Indicators["morale"];

    /// <summary><paramref name="s"/> with <paramref name="outcome"/> resolved on a copy.</summary>
    static GameState Resolved(Content c, GameState s, string outcome, string id = "town_a")
    {
        var t = s.Clone();
        Assert.Null(SettlementControl.Resolve(c, t, id, outcome, new List<Cause>()));
        return t;
    }

    // R-CTRL-018 (Q-DEP-5 (c)): Social cohesion and International Support fall at once by the labelled costs, the place is
    // counted in DisplacedByPlayer, and the options helper shows exactly that as Deport's preview (spec section 2.6).
    [Fact]
    public void TheCostsApplyAtOnce()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var raid = Resolved(c, s, SettlementControl.Raid);
        var deport = Resolved(c, s, SettlementControl.Deport);
        Assert.Equal(Cohesion(raid) - 0.2, Cohesion(deport), 9);
        Assert.Equal(Support(raid) - 0.1, Support(deport), 9);
        Assert.Equal(raid.Treasury - raid.Debt - 121, deport.Treasury - deport.Debt, 9);   // the fixture's deport_money_cost, borrowed where the Treasury lacks it
        Assert.Equal((0, 1), (raid.DisplacedByPlayer, deport.DisplacedByPlayer));
        Assert.Contains("displaced=1", deport.Describe());
        var option = SettlementControl.CaptureOptions(c, s, "town_a").Single(o => o.Outcome == SettlementControl.Deport);
        Assert.Null(option.Refusal);
        var p = option.Preview!;
        Assert.Equal((0.0, 0, 0, 0), (p.Attention, p.Food, p.Arms, p.Score));
        Assert.Equal(-121, p.Money - p.Debt, 9);
        Assert.Equal((-0.2, -0.1), (Math.Round(p.SocialCohesion, 9), Math.Round(p.InternationalSupport, 9)));
    }

    // R-CTRL-018: the International Support drag runs every turn's step 10 from the Deport turn on, for the rest of the game,
    // per place deported, on top of the one-time cost. Nothing in the fixture moves International Support otherwise.
    [Fact]
    public void TheDragIsPermanent()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(2, 4, 0));
        s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        var one = Step(c, s, Answer(SettlementControl.Deport));
        Assert.Empty(one.Rejected);
        Assert.Contains(one.Why, x => x.Target == "intl_standing" && x.Message.Contains("Deport"));
        var a = one.State;
        var b = Step(c, s, Answer(SettlementControl.Deport), Answer(SettlementControl.Deport, "town_c")).State;
        Assert.Equal(2, b.DisplacedByPlayer);
        for (int turn = 1; turn <= 12; turn++)
        {
            Assert.Equal(0.5 - 0.1 - 0.01 * turn, Support(a), 9);
            Assert.Equal(0.5 - 0.2 - 0.02 * turn, Support(b), 9);
            a = Step(c, a).State;
            b = Step(c, b).State;
        }
    }

    // R-CTRL-018 (Q-DEP-5 (c)): International Support has a lower ceiling for the rest of the game, 1 less the drop per place,
    // and no later change can lift it above that.
    [Fact]
    public void TheCeilingHoldsForTheRestOfTheGame()
    {
        var c = Load();
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0)), Answer(SettlementControl.Deport)).State;
        EffectsSystem.Shift(c, s, "intl_standing", 1.0);
        Assert.Equal(0.7, Support(s), 9);
        for (int turn = 0; turn < 3; turn++) s = Step(c, s).State;
        EffectsSystem.Shift(c, s, "intl_standing", 1.0);
        Assert.Equal(0.7, Support(s), 9);
        Assert.Equal(0.5, EffectsSystem.Shift(c, CombatTests.With(c), "intl_standing", 1.0), 9);   // no Deport, no ceiling: 0.5 to 1
    }

    // R-CTRL-018: the ceiling holds on the effects step too, where the level is recomputed (EffectsSystem.Level), not only
    // where a shift writes it. A fixture effect lifts International Support by 1.0 in one turn (inertia 1). The turn's drag
    // (step 10) comes first, 0.39 to 0.38; the effects step then stops the level at the ceiling, 0.7, not at 0.88.
    [Fact]
    public void TheCeilingHoldsWhenAnEffectLiftsSupport()
    {
        var c = Load(files => files["model.yaml"] = files["model.yaml"].Replace("effects:\n",
            "effects:\n  - id: support_lift\n    source: rationing\n    target: intl_standing\n    k: 1.0\n    inertia: 1\n    name: Support lift\n"));
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0)), Answer(SettlementControl.Deport)).State;
        Assert.Equal(0.4 - 0.01, Support(s), 9);
        s.Policies["rationing"] = 1.0;
        var next = Step(c, s);
        Assert.Equal(0.7 - 0.38, next.Why.Single(x => x.System == "effects" && x.Target == "intl_standing").Delta, 9);
        Assert.Equal(0.7, Support(next.State), 9);
    }

    // R-CTRL-019 (Q-DEP-2 (a), Q-DEP-3 (a)): the residents become the DisplacedByPlayer count only, outside the player's
    // control. No city's refugees, civilians or other people, no cohort, no unit and no prisoner count grows: the turn ends
    // as Raid's does in all of them.
    [Fact]
    public void ResidentsNeverBecomePlayerUnits()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0));
        var raid = Step(c, s, Answer(SettlementControl.Raid)).State;
        var deport = Step(c, s, Answer(SettlementControl.Deport)).State;
        Assert.Equal(raid.Cities.Values.Select(x => x.Canonical(false)), deport.Cities.Values.Select(x => x.Canonical(false)));
        Assert.Equal(raid.Units.Values.Select(x => x.Canonical()), deport.Units.Values.Select(x => x.Canonical()));
        Assert.Equal(raid.Cohorts.Values.Select(x => x.Canonical()), deport.Cohorts.Values.Select(x => x.Canonical()));
        Assert.Equal(raid.PrisonersHeld, deport.PrisonersHeld);
        Assert.Equal(raid.PrisonersReturned, deport.PrisonersReturned);
        Assert.Equal(raid.SoldiersHeld, deport.SoldiersHeld);
        Assert.Equal(raid.Population, deport.Population);
        Assert.Equal(raid.TotalPopulation, deport.TotalPopulation);
    }

    // R-CTRL-018 (Q-DEP-7 (a)): the player's units leave the hex exactly as in Raid (R-CTRL-010), so Deport frees no unit and
    // keeps none there, and it is refused the same way when a unit has nowhere to go.
    [Fact]
    public void TheUnitsLeaveAsInARaid()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(2, 1, 0));
        var raid = Resolved(c, s, SettlementControl.Raid);
        var deport = Resolved(c, s, SettlementControl.Deport);
        Assert.Equal(raid.Units.Values.Select(x => x.Canonical()), deport.Units.Values.Select(x => x.Canonical()));
        Assert.Contains(Movement.NoEntry(c, deport), h => h == (1, 0));
        var boxed = Awaiting(c, Soldier(1, 1, 0), CaptureTests.Enemy(2, 0, 0), CaptureTests.Enemy(3, 2, 0), CaptureTests.Enemy(4, 1, 1), CaptureTests.Enemy(5, 0, 1));
        var before = boxed.Describe();
        var why = new List<Cause>();
        var raidRefusal = SettlementControl.Resolve(c, boxed.Clone(), "town_a", SettlementControl.Raid, why);
        Assert.NotNull(raidRefusal);
        Assert.Equal(raidRefusal, SettlementControl.Resolve(c, boxed, "town_a", SettlementControl.Deport, why));
        Assert.Equal(before, boxed.Describe());
    }

    // R-CTRL-020 (impact plan section 4.1 item 4; Q-DEP-7 (c); Q-SENS-2 (c)): a depopulated place adds nothing. A reflection
    // test over every command kind: each that can name a place or a hex is aimed at the depopulated one and refused, and
    // every other names none. The fixture puts a road segment on the place's hex, which is also town_a's city's own hex, and
    // no site can be on a settlement's hex. No effect target can name a place. The record keeps no timer, so step 10 neither counts it down nor
    // charges upkeep, and a later capture check never takes it again.
    [Fact]
    public void ADepopulatedPlaceAddsNothing()
    {
        var c = Load(files => files["corridors.yaml"] = Fixture.GddCorridors.Replace("hexes: [[2, 0], [3, 0]]", "hexes: [[1, 0], [2, 0]]"));
        // No designated site can be on a 1948 settlement's hex at all: the loader refuses it (so no pioneer site needs a check).
        var onThePlace = Assert.Throws<ContentException>(() => Load(files => files["sites.yaml"] = files["sites.yaml"].Replace("q: 2\n    r: 0\n    kinds: [yishuv]", "q: 1\n    r: 0\n    kinds: [yishuv]")));
        Assert.Contains("holds the 1948 settlement town_a", onThePlace.Message);
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0)), Answer(SettlementControl.Deport)).State;
        var ctrl = s.Settlements["town_a"];
        Assert.Equal(("", "", true, 0, ""), (ctrl.Owner, ctrl.State, ctrl.Depopulated, ctrl.Turns, ctrl.Buildings));
        var segment = c.Corridors[0].Id + ":1";
        var aimed = new Dictionary<Type, Command[]?>
        {
            [typeof(MapAction)] = new Command[] { new MapAction(MapActions.Housing, 1, 0) },
            [typeof(StartProject)] = c.ProjectKinds.Select(k => (Command)new StartProject(k.Id, "town_a", 1, 0)).ToArray(),
            [typeof(PioneerWork)] = new Command[] { new PioneerWork(2, Corridors.BuildRoad, segment) },
            [typeof(Move)] = new Command[] { new Move(1, 1, 0), new Move(2, 1, 0) },
            [typeof(UnitOrder)] = new Command[] { new UnitOrder(1, Combat.Attack, 1, 0) },
            [typeof(ResolveCapture)] = new Command[] { Answer(SettlementControl.Raid), Answer(SettlementControl.Occupy), Answer(SettlementControl.Deport) },
            // These name no place or hex: a city, a cohort, a unit's own work, a policy, a budget, an event, an office or a count.
            [typeof(SetPolicy)] = null, [typeof(AllocateBudget)] = null, [typeof(ChooseEventOption)] = null, [typeof(SetMobilization)] = null,
            [typeof(SecurityOrder)] = null, [typeof(Appoint)] = null, [typeof(AssignWorkers)] = null, [typeof(Train)] = null,
            [typeof(CancelTraining)] = null, [typeof(Reinforce)] = null, [typeof(ReturnCraftsmen)] = null, [typeof(ReassignCraftsmen)] = null, [typeof(ReassignPioneers)] = null, [typeof(StopWork)] = null, [typeof(CancelProject)] = null, [typeof(BuyArms)] = null,
        };
        var kinds = typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract).OrderBy(t => t.Name, StringComparer.Ordinal);
        Assert.Equal(kinds.Select(t => t.Name), aimed.Keys.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t => t.Name));
        var checkedByTheRule = 0;
        foreach (var command in aimed.Values.Where(x => x != null).SelectMany(x => x!))
        {
            var refused = Assert.Single(Step(c, s, command).Rejected);
            // StartProject and PioneerWork: what a raided place would allow, the depopulated one refuses for that reason.
            var raided = s.Clone();
            raided.Settlements.Remove("town_a");
            if ((command is StartProject || command is PioneerWork) && Step(c, raided, command).Rejected.Count == 0)
            {
                Assert.Contains("depopulated", refused.Reason);
                checkedByTheRule++;
            }
        }
        Assert.True(checkedByTheRule >= 2);   // the road segment and at least one city project
        var hexes = c.SettlementHexes.Select(h => h.Id).ToHashSet();
        Assert.DoesNotContain(EventRules.SpecialTargets.Concat(c.Indicators.Select(i => i.Id)), hexes.Contains);
        var next = Step(c, s);
        Assert.Equal(ctrl.Canonical(), next.State.Settlements["town_a"].Canonical());
        Assert.DoesNotContain(next.Why, x => x.System == "map" && x.Target == "town_a");   // no timer, no upkeep, no capture
        var back = s.Clone();
        back.Units[1] = Soldier(1, 1, 0);
        SettlementControl.Capture(c, back, back.Date.AddDays(7), new List<Cause>());
        Assert.Equal(ctrl.Canonical(), back.Settlements["town_a"].Canonical());
    }

    // R-CTRL-021 (Q-DEP-4 (a), D-F3): deportation and razing are separate rules, with no razing option in the MVP. The
    // buildings field stays at its default, and the capture offers no fourth outcome.
    [Fact]
    public void ThereIsNoRazing()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        Assert.Equal("", Resolved(c, s, SettlementControl.Deport).Settlements["town_a"].Buildings);
        Assert.Equal(new[] { SettlementControl.Raid, SettlementControl.Occupy, SettlementControl.Deport },
            SettlementControl.CaptureOptions(c, s, "town_a").Select(o => o.Outcome));
    }

    // R-CTRL-007 (Q-TECH-3; plan, PR 8 sensitivity gate): with mvp.deport_in_play off, as in the real content, Deport keeps
    // the "not built yet" refusal and the state is unchanged.
    [Fact]
    public void DeportStaysOutOfPlayUntilItIsSwitchedOn()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = Awaiting(c, Soldier(1, 1, 0));
        var before = s.Describe();
        Assert.Contains("not built yet", SettlementControl.Resolve(c, s, "town_a", SettlementControl.Deport, new List<Cause>()));
        Assert.Equal(before, s.Describe());
    }

    // R-CTRL-007 in the real content (#1327 nit 3): with deport_in_play off in rules.yaml, a player unit on Jaffa's hex with
    // Jaffa awaiting the choice still has Deport refused, and nothing is depopulated or displaced.
    [Fact]
    public void TheRealContentRefusesDeport()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.False(c.RulesAt(s.Date).Mvp!.DeportInPlay);
        var jaffa = c.SettlementTargets.Single(t => t.Id == "jaffa");
        var u = s.Units.Values.First(x => x.Side == Movement.PlayerSide);
        s.Units[u.Id] = new Unit(u.Id, u.Type, jaffa.Q, jaffa.R, side: u.Side, strength: u.Strength);
        s.Settlements["jaffa"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        var result = Simulation.Step(c, s, new Command[] { new ResolveCapture("jaffa", SettlementControl.Deport) });
        Assert.Contains("not built yet", Assert.Single(result.Rejected).Reason);
        Assert.Equal(0, result.State.DisplacedByPlayer);
        Assert.False(result.State.Settlements["jaffa"].Depopulated);
    }

    // R-CTRL-018, the real content's deport_money_cost (owner, 22 October 2026, #1327): at least the most Occupy can pay in
    // upkeep, every turn of the game, plus the dearest city project a depopulated hex shared with a city no longer takes.
    // DeportNeverDominates checks the fixture; this keeps the real value on the same side of that bound.
    [Fact]
    public void TheRealDeportCostCoversOccupyAndTheDearestProject()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var m = c.Rules.Mvp!;
        var turns = (c.End.Year - c.Rules.Start.Year) * 12 + c.End.Month - c.Rules.Start.Month;   // monthly turns (D-B1)
        Assert.Equal(44, turns);
        Assert.True(m.DeportMoneyCost >= m.OccupationUpkeepPerTurn * turns + c.ProjectKinds.Max(k => k.Cost));
    }

    // Determinism: Deport and the drag draw nothing from the RNG.
    [Fact]
    public void DeportDrawsNothing()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var rng = s.RngState;
        Assert.Null(SettlementControl.Resolve(c, s, "town_a", SettlementControl.Deport, new List<Cause>()));
        SettlementControl.Drag(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
        Assert.Equal(rng, s.RngState);
    }

    [Theory]
    [InlineData("deport_cohesion_cost: 0.2", "", "mvp.deport_cohesion_cost", "required")]
    [InlineData("deport_support_cost: 0.1", "deport_support_cost: -0.1", "mvp.deport_support_cost", "must be a finite number >= 0")]
    [InlineData("deport_support_drag_per_turn: 0.01", "", "mvp.deport_support_drag_per_turn", "required")]
    [InlineData("deport_support_ceiling_drop: 0.3", "deport_support_ceiling_drop: -1", "mvp.deport_support_ceiling_drop", "must be a finite number >= 0")]
    [InlineData("deport_money_cost: 121", "", "mvp.deport_money_cost", "required")]
    public void BadValuesFailNamingTheField(string from, string to, string field, string message)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }
}
