#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>Every concrete command type in the sim assembly has a real branch in SimBridge.Validate, so the queue never
/// refuses a valid order with the default "That order is not valid" (#380). A new command type fails here until it has a
/// sample below and its branch in SimBridge. The samples mirror the sim's CommandGuardTests.Samples.</summary>
public class SimBridgeGuardTests
{
    static readonly Content Slice = MvpSlice.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    static readonly Dictionary<Type, Command> Samples = new Dictionary<Type, Command>
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
    };

    static IEnumerable<Type> CommandTypes() =>
        typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract);

    [Test]
    public void EveryCommandTypeHasASample()
    {
        var missing = CommandTypes().Where(t => !Samples.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.IsEmpty(missing, "add a sample here, then a branch in SimBridge.Validate and SimBridge.Key");
    }

    /// <summary>The command types whose order is one per turn by design, so their key is their kind (SimBridge.Key's
    /// default): one budget, one call-up level, one security order.</summary>
    static readonly HashSet<Type> OnePerTurn = new HashSet<Type> { typeof(AllocateBudget), typeof(SetMobilization), typeof(SecurityOrder) };

    /// <summary>Every other command type has its own branch in SimBridge.Key, so its orders are not one per turn by accident
    /// (a new type with a Validate branch and no Key branch would silently keep only the last order of the turn).</summary>
    [Test]
    public void EveryCommandTypeHasAKeyBranchOrIsOnePerTurn()
    {
        foreach (var t in CommandTypes().Where(Samples.ContainsKey))
        {
            var key = SimBridge.Key(Samples[t]);
            if (OnePerTurn.Contains(t)) Assert.AreEqual(Samples[t].Kind, key, t.Name);
            else Assert.AreNotEqual(Samples[t].Kind, key, t.Name + " has no branch in SimBridge.Key: add one, or list it in OnePerTurn");
        }
    }

    [Test]
    public void NoCommandTypeFallsToTheDefaultBranch()
    {
        var text = Text;
        foreach (var t in CommandTypes().Where(Samples.ContainsKey))
        {
            var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), text);
            var reason = bridge.Queue(Samples[t]);
            Assert.AreNotEqual(text["order.invalid"], reason, t.Name + " reaches SimBridge.Validate's default branch");
        }
    }
}
