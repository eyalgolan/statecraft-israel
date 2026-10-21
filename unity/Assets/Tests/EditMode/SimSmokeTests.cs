using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Sim;

public class SimSmokeTests
{
    [Test]
    public void ContentLoads_AndStepIsDeterministicInsideUnity()
    {
        var content = ContentLoader.Load(Paths.Content);
        var a = Simulation.NewGame(content, 42UL, Mode.Historical);
        var b = Simulation.NewGame(content, 42UL, Mode.Historical);
        var ra = Simulation.Step(content, a, new List<Command>());
        var rb = Simulation.Step(content, b, new List<Command>());
        Assert.AreEqual(ra.State.Hash(), rb.State.Hash());
        Assert.IsNotEmpty(ra.State.Hash());
    }

    /// <summary>The save format's strict UTF-8 decoding, on the Unity editor's Mono runtime (it must not depend on the
    /// BCL's Uri class): an overlong escape is refused, a surrogate pair round-trips.</summary>
    [Test]
    public void SaveTextIsStrictUtf8OnMono()
    {
        var hash = new string('a', 64);
        var overlong = "statecraft-save 1\ncontent " + hash + "\nseed 1\nmode Historical\nturn\npolicy a%C0%AF 3FE0000000000000\nend " + hash + "\n";
        var ex = Assert.Throws<SaveException>(() => SaveGame.Parse(overlong));
        Assert.AreEqual("line 6: bad text 'a%C0%AF'", ex.Message);

        var content = ContentLoader.Load(Paths.Content);
        var g = GameSession.New(content, 1UL, Mode.Historical);
        g.Step(new List<Command> { new SetPolicy("a\uD83D\uDE00b", 1.0) });
        var text = g.Save.Write();
        StringAssert.Contains("policy a%F0%9F%98%80b 3FF0000000000000", text);
        var loaded = GameSession.Load(content, text, out _);
        Assert.AreEqual(g.State.Hash(), loaded.State.Hash());
        Assert.AreEqual(text, loaded.Save.Write());
    }
}
