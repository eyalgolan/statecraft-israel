using Statecraft.Sim;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// R-SAVE-001/002: independent fresh-main baseline, not two loads through candidate code.
public class PopulationLedgerCompatibilityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OldModeCorpusRemainsByteIdentical(bool gdd)
    {
        var root = RealContentTests.ContentRoot();
        // Real gdd-0.1 corpus and the repository's valid legacy fixture corpus.
        var dir = gdd ? root : Fixture.Write(new(), new()
        {
            ["probe.yaml"] = "id: probe\ntitle: Fixture\ntext: Fixture.\nanchor: hard\nfrom: 1948-05-15\noptions:\n  - id: ok\n    text: Accept\n    historical: true\n    effects: [{ target: morale, delta: -0.1 }]\n"
        });
        var c = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(dir, rel); return File.Exists(path) ? File.ReadAllText(path) : null;
        }, Directory.GetFiles(Path.Combine(dir, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
        var rows = new List<object>();
        var session = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        rows.Add(new { content = c.Hash, save = session.Save.Write(), state = session.State.Hash() });
        for (int i = 0; i < 1000 && session.State.GameOver == null; i++)
        {
            var commands = GoldenRun.Orders(c, session.State).Concat(new Command[] { new ChooseEventOption("missing", "missing") }).ToList();
            var r = session.Step(commands);
            rows.Add(new
            {
                save = session.Save.Write(), state = session.State.Hash(), rng = session.State.RngState,
                pending = session.State.PendingEvents.ToArray(), fired = session.State.FiredEvents.ToArray(),
                chosen = session.State.ChosenOptions.ToArray(),
                rejected = r.Rejected.Select(x => new { kind = x.Command.Kind, x.Reason }).ToArray(),
                why = r.Why.Select(x => new { x.System, x.Target, x.Delta, x.Message, x.Key, units = x.Units.ToArray(), args = x.Args.ToArray(), x.EnemyOrder, attackers = x.EnemyAttackers.ToArray() }).ToArray()
            });
        }
        Assert.InRange(rows.Count, 2, 1001);
        Assert.NotNull(session.State.GameOver);
        var saveText = session.Save.Write();
        Assert.StartsWith("statecraft-save 3\n", saveText);
        var replay = GameSession.Load(c, saveText, out var warnings);
        Assert.Empty(warnings); Assert.Equal(saveText, replay.Save.Write()); Assert.Equal(session.State.Hash(), replay.State.Hash());
        var json = JsonSerializer.Serialize(rows);
        // Independently captured on fresh main fd638dfc8 before applying any production hunk.
        // Full schema3 saves/reports/rejections/RNG/order corpus; do not refresh to hide a rules regression.
        Assert.Equal(gdd ? "65a9d7fc4de643991e6abd659199b979e0e29d888339cbfb380f093256025080" : "feed4769200b559a03766726eae7b9b0dc20b0365270083420ecbd3ad6d67e72", c.Hash);
        Assert.Equal(gdd ? "6b18944a1c105a5c4f384844a1279ad0736e04dc13e4c7632b44bb6791cd66a8" : "83eeb71d4bc95619ee6719cf90b6b12aa1ff9bfa67454e2a57bc36faf1fa8d96",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
    }
}
