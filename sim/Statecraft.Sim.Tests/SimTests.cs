using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

public class SimTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());
    static readonly IReadOnlyList<Command> None = new Command[0];

    [Fact]
    public void NewGameStartsAtRulesStartAtWar()
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        Assert.Equal(new DateTime(1948, 5, 15), s.Date);
        Assert.Equal(5, s.Rung);
        Assert.Equal(3, s.Attention);
        Assert.Equal(1000, s.TotalPopulation);
    }

    [Fact]
    public void OpenModeIsNotAvailableYet() =>
        Assert.Throws<NotSupportedException>(() => Simulation.NewGame(C, 1, Mode.Open));

    [Fact]
    public void StepAdvancesByRungTurnLengthAndExplainsItself()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, None);
        Assert.Equal(new DateTime(1948, 5, 22), r.State.Date);   // rung 5 = weekly
        Assert.Equal(1, r.State.Turn);
        Assert.Contains(r.Why, w => w.System == "population");
        Assert.Contains(r.Why, w => w.System == "economy");
    }

    [Fact]
    public void StepDoesNotMutateItsInput()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var before = s0.Hash();
        Simulation.Step(C, s0, new Command[] { new SetMobilization(1.0) });
        Assert.Equal(before, s0.Hash());
    }

    [Fact]
    public void InvalidCommandsAreRejectedWithReasonsAndAttentionRefunded()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("curfew", 1), new SetMobilization(2) });
        Assert.Equal(new[] { "That policy does not exist in this game", "Mobilization must be between 0 and 1" }, r.Rejected.Select(x => x.Reason));
    }

    [Fact]
    public void AttentionLimitsCommandsPerTurn()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // attention 3, rung 5 -> each command costs 2
        var r = Simulation.Step(C, s0, new Command[] { new SetMobilization(0.9), new SetMobilization(1.0) });
        Assert.Single(r.Rejected);
        Assert.StartsWith("Not enough attention", r.Rejected[0].Reason);
    }

    [Fact]
    public void SecurityOrderChangesRung()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-2) });
        Assert.Equal(3, r.State.Rung);
        Assert.Equal(new DateTime(1948, 8, 15), r.State.Date);  // now quarterly
    }

    [Fact]
    public void GameOverRejectsEverything()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical); s0.GameOver = "coalition_collapse";
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-1) });
        Assert.Equal("The game is over: The coalition collapsed", r.Rejected[0].Reason);
        Assert.Equal(s0.Hash(), r.State.Hash());
    }

    [Fact]
    public void SameSeedAndCommandsGiveIdenticalHashes()
    {
        string Play(ulong seed)
        {
            var s = Simulation.NewGame(C, seed, Mode.Historical);
            for (int i = 0; i < 20; i++)
                s = Simulation.Step(C, s, i == 3 ? new Command[] { new SecurityOrder(-3) } : HistoricalAI.Commands(C, s)).State;
            return s.Hash();
        }
        Assert.Equal(Play(7), Play(7));
        Assert.NotEqual(Play(7), Play(8));
    }

    // ---- Beyond the brief: post-H1 rules and robustness ----

    const string Shortage = @"
id: shortage
title: Bread lines
text: '{finance_minister} reports shortages.'
office: finance_minister
from: 1948-05-15
options:
  - id: ration
    text: Ration food
    recommended: true
    effects:
      - target: food_supply
        delta: 0.2
  - id: import
    text: Buy abroad
    historical: true
    effects:
      - target: fx_reserves
        delta: -10
";

    const string Raid = @"
id: raid
title: Border raid
text: 'A raid on the border.'
office: defense_minister
from: 1948-05-15
options:
  - id: hold
    text: Hold the line
    effects:
      - target: morale
        delta: -0.05
  - id: strike
    text: Strike back
    effects:
      - target: morale
        delta: 0.05
";

    static Content WithEvents() =>
        ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["shortage.yaml"] = Shortage, ["raid.yaml"] = Raid }));

    sealed class Bogus : Command { public override string Kind => "bogus"; }

    /// <summary>A Command type this assembly does not know cannot be saved, so Step refuses it up front (a programmer error,
    /// like a null command) instead of rejecting it and leaving a game whose save cannot be written.</summary>
    [Fact]
    public void ACommandTypeTheSimDoesNotKnowIsRefusedBeforeAnythingRuns()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var ex = Assert.Throws<ArgumentException>(() => Simulation.Step(C, s0, new Command[] { new SetMobilization(1.0), new Bogus() }));
        Assert.Contains("unknown command type", ex.Message);
    }

    [Fact]
    public void PolicyLevelMustBeFinite()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("rationing", double.NaN) });
        Assert.Equal("Policy level must be between 0 and 1", r.Rejected.Single().Reason);
        Assert.Equal(0.0, r.State.Policies["rationing"]);
    }

    [Fact]
    public void PolicyPassesWithAMajority()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // council: alpha holds 3 of 5
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("rationing", 0.5) });
        Assert.Empty(r.Rejected);
        Assert.Equal(0.5, r.State.Policies["rationing"]);
        Assert.Contains(r.Why, w => w.System == "policy" && w.Message == "Rationing set to 50%");
    }

    [Fact]
    public void PartialBudgetIsRejectedWithEconomyReason()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["defense_minister"] = 1.0 }) });
        Assert.Equal("The budget must give a share to every ministry, and only to them", r.Rejected.Single().Reason);
        Assert.Equal(1.0, r.State.Budget["finance_minister"]);
    }

    [Fact]
    public void MalformedBudgetIsRejectedBeforeAnyVoteIsCast()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1949, 4, 1);   // House needs to approve budgets; gov2 holds 10/10
        s0.CoalitionStability = 0.3;           // so a vote would draw from the RNG
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 0.7 }) });
        Assert.Equal("Budget shares must sum to 1", r.Rejected.Single().Reason);
        Assert.Equal(s0.RngState, r.State.RngState);
    }

    [Fact]
    public void BudgetVoteFailsWithoutAMajority()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1949, 3, 1);   // House sits, gov1 (alpha, 4 of 10) has no majority
        var r = Simulation.Step(C, s0, new Command[] { new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }) });
        Assert.Empty(r.Rejected);   // a vote held and lost is an outcome, not a rejection
        Assert.Contains(r.Why, w => w.System == "politics" && w.Target == "budget" && w.Message == "House voted the budget down");
    }

    // ---- Final fix 3: a defeated vote is an outcome, not a rejection ----

    [Fact]
    public void DefeatedPolicyVoteIsChargedAndReportedAsACause()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1949, 3, 1);   // House sits, gov1 (alpha, 4 of 10) has no majority: every vote fails
        s0.CoalitionStability = 0.3;
        s0.Rung = 1; s0.Attention = 2;         // each set_policy costs 1: two afford, the third cannot
        var cmd = new SetPolicy("rationing", 0.9);
        var r = Simulation.Step(C, s0, new Command[] { cmd, cmd, cmd });
        Assert.Equal(new[] { "Not enough attention (need 1.0, have 0.0)" }, r.Rejected.Select(x => x.Reason));
        Assert.Equal(0.0, r.State.Policies["rationing"]);
        Assert.Equal(2, r.Why.Count(w => w.System == "politics" && w.Target == "rationing" && w.Message == "House voted down Rationing at 90%"));
    }

    [Theory]
    [InlineData(1UL)] [InlineData(2UL)] [InlineData(3UL)] [InlineData(4UL)] [InlineData(5UL)]
    public void RepeatedPolicyVotesCostAttentionWhetherTheyPassOrFail(ulong seed)
    {
        var s0 = Simulation.NewGame(C, seed, Mode.Historical);   // council: alpha holds 3 of 5
        s0.CoalitionStability = 0.3;   // below 0.5: each vote passes with probability 0.6
        s0.Rung = 1; s0.Attention = 2;
        var cmd = new SetPolicy("rationing", 0.9);
        var r = Simulation.Step(C, s0, new Command[] { cmd, cmd, cmd });
        Assert.Equal(new[] { "Not enough attention (need 1.0, have 0.0)" }, r.Rejected.Select(x => x.Reason));
    }

    [Fact]
    public void ValidationFailureIsStillRefunded()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Rung = 1; s0.Attention = 1;
        var r = Simulation.Step(C, s0, new Command[] { new SetPolicy("no_such_policy", 0.5), new SetPolicy("rationing", 0.5) });
        Assert.Equal(new[] { "That policy does not exist in this game" }, r.Rejected.Select(x => x.Reason));
        Assert.Equal(0.5, r.State.Policies["rationing"]);
        Assert.Equal(s0.RngState, r.State.RngState);
    }

    [Fact]
    public void SecurityOrderDoesNotOverflow()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(int.MaxValue) });
        Assert.Empty(r.Rejected);
        Assert.Equal(5, r.State.Rung);
    }

    [Fact]
    public void HistoricalAIChoosesTheHistoricalOption()
    {
        var c = WithEvents();
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        Assert.Equal(new[] { "raid", "shortage" }, s.PendingEvents);
        // 3 attention and each choice costs 2: like the UI's queue, it sends only the choice it can afford.
        var affordable = HistoricalAI.Commands(c, s).Cast<ChooseEventOption>().ToList();
        Assert.Equal(new[] { ("raid", "hold") }, affordable.Select(x => (x.EventId, x.OptionId)));
        s.Attention = 10;
        var cmds = HistoricalAI.Commands(c, s).Cast<ChooseEventOption>().ToList();
        Assert.Equal(new[] { ("raid", "hold"), ("shortage", "import") }, cmds.Select(x => (x.EventId, x.OptionId)));
    }

    [Fact]
    public void UnaffordableEventChoicesAreDelegated()
    {
        var c = WithEvents();
        var s0 = Simulation.NewGame(c, 42, Mode.Historical);
        var both = new Command[] { new ChooseEventOption("raid", "hold"), new ChooseEventOption("shortage", "import") };
        var r = Simulation.Step(c, s0, both);   // 3 attention, each choice costs 2
        Assert.StartsWith("Not enough attention", r.Rejected.Single().Reason);
        Assert.Empty(r.State.PendingEvents);
        Assert.Contains(r.Why, w => w.System == "events" && w.Message.StartsWith("Chosen: "));
        Assert.Contains(r.Why, w => w.System == "events" && w.Message.StartsWith("Delegated to Def Two"));
    }

    [Fact]
    public void DeterministicWithEventsAndDelegation()
    {
        var c = WithEvents();
        (string hash, string why) Play(ulong seed)
        {
            var s = Simulation.NewGame(c, seed, Mode.Historical);
            var lines = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                var r = Simulation.Step(c, s, i % 2 == 0 ? None : HistoricalAI.Commands(c, s));
                lines.AddRange(r.Why.Select(w => w.ToString()));
                s = r.State;
            }
            return (s.Hash(), string.Join("\n", lines));
        }
        Assert.Equal(Play(11), Play(11));
        Assert.NotEqual(Play(11).hash, Play(12).hash);
    }

    // ---- Fix round 1 ----

    const string Partition = @"
id: partition
title: The armistice line
text: 'Where should the line run?'
anchor: hard
office: defense_minister
from: 1948-05-15
options:
  - id: hold_north
    text: Hold the north
    recommended: true
  - id: accept_line
    text: Accept the line
    historical: true
  - id: push_on
    text: Push on
";

    [Fact]
    public void HardAnchorDelegatedInHistoricalModeTakesTheHistoricalOptionWithoutRng()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["partition.yaml"] = Partition }));
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var s0 = Simulation.NewGame(c, seed, Mode.Historical);
            Assert.Equal(new[] { "partition" }, s0.PendingEvents);
            var r = Simulation.Step(c, s0, None);
            Assert.Contains(r.Why, w => w.System == "events" && w.Message == "Delegated to Pm One: Accept the line");
            Assert.Equal(seed, r.State.RngState);
        }
    }

    [Fact]
    public void HardAnchorOutsideHistoricalModeKeepsCompetenceDelegation()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["partition.yaml"] = Partition }));
        var s0 = Simulation.NewGame(c, 5, Mode.Historical);
        s0.Mode = Mode.Open;
        var r = Simulation.Step(c, s0, None);
        Assert.NotEqual(5UL, r.State.RngState);
        Assert.Contains(r.Why, w => w.System == "events" && w.Message.StartsWith("Delegated to Pm One: "));
    }

    const string Late = @"
id: late
title: A later matter
text: 'Something new.'
from: 1948-05-22
options:
  - id: ok
    text: Fine
";

    [Fact]
    public void CollapseTurnTriggersNoNewEvents()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["late.yaml"] = Late }));
        var s0 = Simulation.NewGame(c, 42, Mode.Historical);
        s0.CoalitionStability = 0.2;
        s0.IndicatorShifts["morale"] = -0.45;   // approval 0.05 drags stability below the collapse line this turn
        var r = Simulation.Step(c, s0, None);
        Assert.Equal("coalition_collapse", r.State.GameOver);
        Assert.Equal(new DateTime(1948, 5, 22), r.State.Date);
        Assert.Equal(1, r.State.Turn);
        Assert.Empty(r.State.PendingEvents);
        Assert.DoesNotContain("late", r.State.FiredEvents);
    }

    [Fact]
    public void AttentionIsPricedAtTheRungTheTurnStartedAt()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);   // rung 5: every command costs 2 of 3
        var r = Simulation.Step(C, s0, new Command[] { new SecurityOrder(-4), new SetPolicy("rationing", 0.5) });
        Assert.Equal(1, r.State.Rung);
        Assert.Equal(new[] { "Not enough attention (need 2.0, have 1.0)" }, r.Rejected.Select(x => x.Reason));
        Assert.Equal(0.0, r.State.Policies["rationing"]);
    }

    [Fact]
    public void DeterministicWhenPolicyVotesDrawFromTheRng()
    {
        (string hash, ulong rng, string log) Play(ulong seed)
        {
            var s = Simulation.NewGame(C, seed, Mode.Historical);
            s.CoalitionStability = 0.3;   // below 0.5: each vote passes with probability 2 * stability
            var lines = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var r = Simulation.Step(C, s, new Command[] { new SetPolicy("rationing", i % 2 == 0 ? 0.6 : 0.2) });
                lines.AddRange(r.Why.Select(w => w.ToString()));
                lines.AddRange(r.Rejected.Select(x => x.Reason));
                s = r.State;
            }
            return (s.Hash(), s.RngState, string.Join("\n", lines));
        }
        var a = Play(21);
        Assert.Equal(a, Play(21));
        Assert.NotEqual(21UL, a.rng);
    }
}
