using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Every command kind has its touch-points (spec 2026-10-12 section 2.3): an Attention kind with a cost in the real
/// rules, CommandText (Step refuses an unknown type outright), the save format, and the scenario runner. The Unity order
/// queue is the fifth, guarded in Unity's own tests. A new Command type fails EveryCommandTypeHasASample until it is added
/// here, and then fails the others until every touch-point knows it.</summary>
public class CommandGuardTests
{
    public static readonly Dictionary<Type, Command> Samples = new Dictionary<Type, Command>
    {
        [typeof(SetPolicy)] = new SetPolicy("rationing", 0.5),
        [typeof(AllocateBudget)] = new AllocateBudget(new Dictionary<string, double> { ["finance_minister"] = 1.0 }),
        [typeof(ChooseEventOption)] = new ChooseEventOption("drought", "a"),
        [typeof(SetMobilization)] = new SetMobilization(0.5),
        [typeof(SecurityOrder)] = new SecurityOrder(1),
        [typeof(Appoint)] = new Appoint("prime_minister", "pm_one"),
        [typeof(MapAction)] = new MapAction("housing", 1, 2),
        [typeof(AssignWorkers)] = new AssignWorkers("tel_aviv", "job", 1),
        [typeof(Train)] = new Train("tel_aviv", "halutz", 1),
        [typeof(CancelTraining)] = new CancelTraining(1),
        [typeof(PioneerWork)] = new PioneerWork(1, "build_yishuv", "mefalsim"),
        [typeof(StopWork)] = new StopWork(1),
        [typeof(UnitOrder)] = new UnitOrder(1, "fortify"),
        [typeof(StartProject)] = new StartProject("housing", "tel_aviv", 1, 0),
        [typeof(CancelProject)] = new CancelProject(1),
        [typeof(Move)] = new Move(1, 2, 0),
        [typeof(BuyArms)] = new BuyArms(0),   // refused (fewer than one unit), as GovernmentTests.ARefusedGddOrderRefundsItsAttention needs
    };

    static IEnumerable<Type> CommandTypes() =>
        typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract);

    [Fact]
    public void EveryCommandTypeHasASample()
    {
        foreach (var t in CommandTypes()) Assert.True(Samples.ContainsKey(t), $"{t.Name} has no sample: add it, then its touch-points");
    }

    /// <summary>Every kind, the GDD ones included, is priced in the real rules since the cutover (the GDD kinds' costs are
    /// design values, R-LOOP-005).</summary>
    [Fact]
    public void EveryCommandHasAnAttentionKindAndACostInTheRealRules()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        foreach (var cmd in Samples.Values)
        {
            Assert.True(AttentionSystem.IsCommandKind(cmd.Kind), cmd.Kind);
            Assert.True(c.Rules.AttentionCost.ContainsKey(cmd.Kind), cmd.Kind);
        }
    }

    [Fact]
    public void EveryCommandIsAcceptedByStepAndRoundTripsThroughASave()
    {
        var c = ContentLoader.Load(Fixture.Write());
        foreach (var cmd in Samples.Values)
        {
            var g = GameSession.New(c, 1, Mode.Historical);
            g.Step(new[] { cmd });   // CommandText throws on a type it does not know; the save throws on one it cannot write
            var text = g.Save.Write();
            var parsed = SaveGame.Parse(text);
            Assert.Equal(text, parsed.Write());
            Assert.Equal(cmd.GetType(), parsed.Turns.Single().Single().GetType());
        }
    }

    [Fact]
    public void TheScenarioRunnerKnowsEveryKind()
    {
        foreach (var cmd in Samples.Values)
        {
            Assert.Contains(cmd.Kind, Scenario.Kinds);
            var fields = new Dictionary<string, object> { ["kind"] = cmd.Kind };
            foreach (var f in Scenario.FieldsOf(cmd.Kind)) fields[f] = f == "shares" ? new Dictionary<object, object> { ["x"] = "1" } : (object)"1";
            Assert.Equal(cmd.GetType(), Scenario.Build(fields).GetType());
        }
    }

    [Fact]
    public void EveryAttentionKindHasACommand() =>
        Assert.Equal(AttentionSystem.CommandKinds.OrderBy(k => k, StringComparer.Ordinal), Samples.Values.Select(x => x.Kind).OrderBy(k => k, StringComparer.Ordinal));
}
