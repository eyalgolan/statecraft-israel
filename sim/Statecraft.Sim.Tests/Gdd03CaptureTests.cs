using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statecraft.Sim;

public class Gdd03CaptureTests
{
    static readonly string Catalogue = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));

    static Content Load(string identity = SaveGame.Gdd03Ruleset, bool shared = false, bool fled = false) => CaptureTests.Load(files =>
    {
        files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: " + identity);
        files[Gdd03BalanceCatalogue.FileName] = Catalogue;
        if (shared)
            foreach (var key in new[] { "settlement_targets.yaml", "map/settlement_hexes.yaml" })
                files[key] = files[key].Replace("q: 4, r: 0", "q: 1, r: 0");
    }, fled ? new[] { "town_a" } : Array.Empty<string>(), CaptureTests.AllTargets);

    // R-CTRL-004, R-CTRL-023: red on main: moving/continuing a route acquires automatically.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovementAndResolverNeverBypassConfirmation(bool route)
    {
        var c = Load();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 2,
            order: route ? Movement.GoTo : "", orderTarget: route ? "1,0" : ""));
        var commands = route ? Array.Empty<Command>() : new Command[] { new Move(1, 1, 0) };
        var preview = Simulation.Preview(c, s, commands);
        Assert.Empty(preview.Rejected);
        Assert.Equal((1, 0), (preview.State.Units[1].Q, preview.State.Units[1].R));
        Assert.Empty(preview.State.Settlements);
        Assert.Empty(Simulation.Step(c, s, commands).State.Settlements);
    }

}
