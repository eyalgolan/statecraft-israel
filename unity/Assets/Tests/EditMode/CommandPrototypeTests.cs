using System;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Prototype;
using UnityEngine;

public sealed class CommandPrototypeTests
{
    [TestCase("https://local/?prototype=command", true)]
    [TestCase("https://local/?map-look&prototype=command#x", true)]
    [TestCase("https://local/?prototype=commander", false)]
    [TestCase("https://local/#?prototype=command", false)]
    [TestCase("https://local/?other=prototype=command", false)]
    [TestCase("https://local/?return=/?prototype=command", false)]
    [TestCase("https://local/path/&prototype=command", false)]
    public void OnlyTheExplicitModeIsRequested(string url, bool expected) =>
        Assert.AreEqual(expected, CommandPrototype.Requested(url, Array.Empty<string>(), true));

    [TestCase(false)]
    [TestCase(true)]
    public void ProceduralPropMeshesKeepPrimitiveSizeAndOutwardNormals(bool sphere) {
        var mesh = CommandPrototype.CreateShapeMesh(sphere);
        try {
            Assert.Less(mesh.bounds.center.magnitude, .0001f);
            Assert.Less(Vector3.Distance(Vector3.one, mesh.bounds.size), .0001f);
            Assert.Greater(mesh.vertexCount, 0);
            var positions = mesh.vertices; var normals = mesh.normals;
            for (int i = 0; i < positions.Length; i++) {
                Assert.Greater(Vector3.Dot(positions[i], normals[i]), 0, "Every visible face points away from the shape centre.");
                Assert.AreEqual(1, normals[i].magnitude, .0001f);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }

    [Test] public void DesktopUsesItsOwnFlag() {
        Assert.IsTrue(CommandPrototype.Requested("", new[] { "app", "-prototype", "command" }, false));
        Assert.IsFalse(CommandPrototype.Requested("?prototype=command", Array.Empty<string>(), false));
        Assert.IsFalse(CommandPrototype.Requested("", new[] { "-prototype", "command-extra" }, false));
    }

    [Test] public void PauseFreezesMovementEconomyAndThreatClock() {
        var m = new CommandModel(); var u = m.Units.First(x => x.Kind == CommandKind.Scout);
        m.Order(u.Id, new CommandPoint(60, 60)); m.Paused = true;
        var before = u.Position; m.Advance(20);
        Assert.AreEqual(before.X, u.Position.X); Assert.AreEqual(0, m.Elapsed);
        Assert.AreEqual(140, m.Money); Assert.AreEqual(0, m.Wave);
    }

    [Test] public void WorkerHarvestsOnlyAfterArrivingAndCanBeRedirected() {
        var m = new CommandModel(); var worker = m.Units.First(x => x.Kind == CommandKind.Worker);
        m.Order(worker.Id, CommandModel.Grove); m.Advance(2);
        Assert.AreEqual(140, m.Money);
        m.Advance(20); Assert.Greater(m.Money, 140);
        m.Order(worker.Id, CommandModel.Base); var money = m.Money;
        m.Advance(3); Assert.AreEqual(money, m.Money);
    }

    [Test] public void MixedSelectionHarvestPreservesBrigadeAttackAndConvoyRoute() {
        var m = new CommandModel();
        var brigade = m.Units.First(u => u.Kind == CommandKind.Brigade);
        var convoy = m.Units.First(u => u.Kind == CommandKind.Convoy);
        var worker = m.Units.First(u => u.Kind == CommandKind.Worker);
        var threat = m.Units.First(u => u.Kind == CommandKind.Threat);
        m.Order(brigade.Id, threat.Position, threat.Id); m.Order(convoy.Id, CommandModel.Outpost);
        var brigadeDestination = brigade.Destination; var convoyDestination = convoy.Destination;
        Assert.AreEqual(1, m.Harvest(new[] { brigade.Id, convoy.Id, worker.Id }));
        Assert.IsTrue(worker.Gathering); Assert.AreEqual(CommandModel.Grove, worker.Destination);
        Assert.AreEqual(threat.Id, brigade.TargetId); Assert.AreEqual(brigadeDestination, brigade.Destination);
        Assert.AreEqual(convoyDestination, convoy.Destination); Assert.IsTrue(convoy.Moving);
    }

    [Test] public void ConvoyMustReturnToDepotBeforeAnotherDelivery() {
        var m = new CommandModel(); var c = m.Units.First(x => x.Kind == CommandKind.Convoy);
        m.Order(c.Id, CommandModel.Outpost); m.Advance(36);
        Assert.AreEqual(1, m.Deliveries); Assert.AreEqual(0, c.Cargo);
        m.Order(c.Id, CommandModel.Outpost); m.Advance(1); Assert.AreEqual(1, m.Deliveries);
        m.Order(c.Id, CommandModel.Base); m.Advance(36); Assert.AreEqual(30, c.Cargo);
    }

    [Test] public void InvalidBuildDoesNotChargeAndExistingBuildCannotBeDuplicated() {
        var m = new CommandModel(); var w = m.Units.First(x => x.Kind == CommandKind.Worker);
        Assert.IsFalse(m.Build(w.Id, CommandKind.Settlement)); Assert.AreEqual(140, m.Money);
        Assert.IsTrue(m.Build(w.Id, CommandKind.Camp)); Assert.AreEqual(105, m.Money);
        Assert.IsFalse(m.Build(w.Id, CommandKind.Camp)); Assert.AreEqual(105, m.Money);
        m.Advance(9); Assert.IsTrue(m.CampReady);
    }

    [Test] public void ScoutsRescueImmigrantsWhoBecomeWorkersAtCompletedCamp() {
        var m = new CommandModel(); var w = m.Units.First(x => x.Kind == CommandKind.Worker);
        m.Build(w.Id, CommandKind.Camp); m.Advance(9);
        var scout = m.Units.First(x => x.Kind == CommandKind.Scout);
        m.Order(scout.Id, CommandModel.RescueOne); m.Advance(30);
        Assert.GreaterOrEqual(m.RescuedGroups, 1);
        Assert.Greater(m.Units.Count(x => x.Kind == CommandKind.Worker), 1);
    }

    [Test] public void FrameChunkingProducesIdenticalMissionState() {
        var a = new CommandModel(); var b = new CommandModel();
        a.Order(3, CommandModel.Grove); b.Order(3, CommandModel.Grove);
        a.Advance(30); for (var i = 0; i < 300; i++) b.Advance(.1);
        Assert.AreEqual(a.Money, b.Money); Assert.AreEqual(a.Supplies, b.Supplies);
        Assert.AreEqual(a.Elapsed, b.Elapsed, .00001);
        Assert.AreEqual(a.Units[0].Health, b.Units[0].Health);
    }

    [Test] public void CompleteEscortRescueAndConstructionMissionCanBeWon() {
        var m = new CommandModel();
        Assert.IsTrue(m.Build(3, CommandKind.Camp));
        m.Order(3, CommandModel.Grove); m.Order(1, CommandModel.RescueOne);
        m.Order(0, new CommandPoint(72, 31)); m.Order(2, CommandModel.Outpost);
        m.Advance(30);
        m.Order(1, CommandModel.RescueTwo); m.Order(2, CommandModel.Base);
        m.Advance(30); m.Order(2, CommandModel.Outpost);
        var worker = m.Units.Last(u => u.Kind == CommandKind.Worker);
        m.Order(worker.Id, CommandModel.Outpost); m.Advance(30);
        Assert.IsTrue(m.Build(worker.Id, CommandKind.Settlement));
        m.Advance(16);
        Assert.AreEqual(CommandOutcome.Won, m.Outcome);
        Assert.AreEqual(2, m.Deliveries); Assert.AreEqual(2, m.RescuedGroups);
        Assert.IsTrue(m.SettlementReady);
        var time = m.Elapsed; m.Advance(20); Assert.AreEqual(time, m.Elapsed);
    }

    [Test] public void InvalidCoordinatesAndUnaffordableRecruitmentDoNotMutateResources() {
        var m = new CommandModel();
        Assert.IsFalse(m.Order(0, new CommandPoint(float.NaN, 50)));
        Assert.IsFalse(m.Order(0, new CommandPoint(50, float.PositiveInfinity)));
        Assert.IsFalse(m.Order(99, CommandModel.Base));
        Assert.IsTrue(m.Recruit()); Assert.IsTrue(m.Recruit());
        var money = m.Money; var supplies = m.Supplies;
        Assert.IsFalse(m.Recruit()); Assert.AreEqual(money, m.Money); Assert.AreEqual(supplies, m.Supplies);
    }

    [Test] public void RestartAndTerminalStatesAreIndependent() {
        var failed = new CommandModel(); failed.Units.First(x => x.Kind == CommandKind.Depot).Health = 0;
        failed.Advance(.1); Assert.AreEqual(CommandOutcome.Lost, failed.Outcome);
        var elapsed = failed.Elapsed; failed.Advance(20); Assert.AreEqual(elapsed, failed.Elapsed);
        var fresh = new CommandModel(); Assert.AreEqual(CommandOutcome.Playing, fresh.Outcome);
        Assert.AreEqual(140, fresh.Money); Assert.AreEqual(0, fresh.Deliveries);
    }
}
