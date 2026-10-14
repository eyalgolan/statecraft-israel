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
    };

    static IEnumerable<Type> CommandTypes() =>
        typeof(Command).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract);

    [Test]
    public void EveryCommandTypeHasASample()
    {
        var missing = CommandTypes().Where(t => !Samples.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.IsEmpty(missing, "add a sample here, then a branch in SimBridge.Validate and SimBridge.Key");
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
