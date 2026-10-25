using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class WhyScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryCauseOfTheLastTurnIsListedUnderItsSystem()
    {
        using var ui = new UiTestKit();
        // The legacy loop: the call-up order is refused in the GDD loop (R-WAR-002, #750), and this test reads its cause.
        var bridge = new SimBridge(GameSession.New(SimBridgeGddTests.Legacy, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("Nothing has happened yet: end the turn to see its causes.", ui.Q<Label>("why-empty").text);
        bridge.Queue(new SetMobilization(1.0));
        var r = bridge.EndTurn();
        Assert.AreEqual("15 May 1948 to 15 June 1948", ui.Q<Label>("why-turn").text);
        var lines = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        Assert.AreEqual(r.Why.Count, lines.Count);
        Assert.AreEqual("Immigration and population", ui.Q<Label>("why-group-population").text);
        Assert.IsTrue(lines.Any(l => l.text == "Reserve call-up set to 100%"));
        var groups = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-group").ToList().Select(l => l.name).ToList();
        Assert.Less(groups.IndexOf("why-group-security"), groups.IndexOf("why-group-economy"));
    }

    [Test]
    public void TheLinesAreTheSimsOwnMessagesInGroupOrder()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text) { Units = Rows };
        var r = bridge.EndTurn();
        var expected = WhyPresenter.Order.SelectMany(sys => r.Why.Where(w => w.System == sys).Select(w => CauseText.Line(w, C, Rows, Text))).ToList();
        var shown = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Select(l => l.text).ToList();
        Assert.AreEqual(expected, shown);
    }

    [Test]
    public void EverySystemTheSimReportsHasAGroupName()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 80 && g.State.GameOver == null; i++)
        {
            g.Step(HistoricalAI.Commands(C, g.State));
            foreach (var w in g.Last!.Why) seen.Add(w.System);
        }
        CollectionAssert.IsSubsetOf(seen, WhyPresenter.Order, "a system with no group would show its raw id to players");
        var text = Text;
        foreach (var sys in WhyPresenter.Order) Assert.DoesNotThrow(() => { var _ = text["why." + sys]; }, sys);
    }

    [Test]
    public void ADisposedPanelHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var why = new WhyPresenter(ui.Root, bridge, Text);
        why.Dispose();
        bridge.EndTurn();
        Assert.IsNotNull(ui.Root.Q<Label>("why-empty"), "still the empty note: the panel did not re-render");
    }

    static (UiTestKit ui, SimBridge bridge, WhyPresenter why) Open()
    {
        var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        return (ui, bridge, new WhyPresenter(ui.Root, bridge, Text));
    }

    [Test]
    public void ASystemWithoutAGroupShowsAsOtherAndLast()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var real = bridge.Session.Last!;
            var why2 = new System.Collections.Generic.List<Cause>(real.Why) { new Cause("zzz", "t", 0, "something odd happened") };
            why.Show(new StepResult(real.State, why2, real.Rejected));
            var rows = ui.Q<ScrollView>("why-rows");
            var headers = rows.Query<Label>(className: "why-group").ToList();
            Assert.AreEqual("why-group-zzz", headers.Last().name);
            Assert.AreEqual("Other", headers.Last().text);
            Assert.IsFalse(rows.Query<Label>().ToList().Any(l => l.text == "zzz"), "the raw id is never shown");
            Assert.AreEqual("Something odd happened", rows.Query<Label>(className: "why-line").ToList().Last().text);
        }
    }

    [Test]
    public void EverySystemNamedInTheSimSourcesHasAGroup()
    {
        var text = Text;
        var found = new System.Collections.Generic.SortedSet<string>();
        int literal = 0, all = 0;
        foreach (var f in System.IO.Directory.GetFiles(System.IO.Path.Combine(Paths.RepoRoot, "sim", "Statecraft.Sim"), "*.cs"))
        {
            var source = System.IO.File.ReadAllText(f);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, "new Cause\\(\"([a-z_]+)\""))
            { found.Add(m.Groups[1].Value); literal++; }
            all += System.Text.RegularExpressions.Regex.Matches(source, "new\\s+Cause\\s*\\(").Count;
        }
        Assert.AreEqual(all, literal, "every 'new Cause(' must name its system as a plain literal, or this scan cannot see it");
        Assert.Greater(found.Count, 5, "the scan found the sim's causes");
        foreach (var sys in found)
        {
            CollectionAssert.Contains(WhyPresenter.Order, sys, sys);
            Assert.DoesNotThrow(() => { var _ = text["why." + sys]; }, sys);
        }
    }

    [Test]
    public void ATurnWithNoCausesSaysSo()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var real = bridge.Session.Last!;
            why.Show(new StepResult(real.State, new Cause[0], real.Rejected));
            Assert.AreEqual("Nothing notable happened this turn.", ui.Q<Label>("why-nothing").text);
        }
    }

    [Test]
    public void ASecondTurnReplacesTheFirstAndAnUnchangedResultIsNotRebuilt()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var first = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
            why.Render();
            Assert.AreSame(first[0], ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList()[0], "same result: no rebuild");
            var r = bridge.EndTurn();
            var second = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
            Assert.AreEqual(r.Why.Count, second.Count);
            Assert.AreNotSame(first[0], second[0]);
            Assert.AreEqual("15 June 1948 to 15 July 1948", ui.Q<Label>("why-turn").text);
        }
    }

    [Test]
    public void ReplacingTheSessionRerenders()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            Assert.IsNull(ui.Root.Q<Label>("why-empty"));
            var other = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            bridge.Replace(other);
            Assert.IsNotNull(ui.Root.Q<Label>("why-empty"), "a new game has no last turn");
            var loaded = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            loaded.Step(new Command[0]);
            bridge.Replace(loaded);
            Assert.IsNull(ui.Root.Q<Label>("why-empty"));
            Assert.AreEqual(loaded.Last!.Why.Count, ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Count);
        }
    }

    /// <summary>#701: in the Hebrew UI the turn's span names its months in Hebrew (date.*), not "15 May 1948".</summary>
    [Test]
    public void InHebrewTheTurnsSpanIsAHebrewDate()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he);
        new WhyPresenter(ui.Root, bridge, he);
        bridge.EndTurn();
        Assert.AreEqual("מ־15 במאי 1948 עד 15 ביוני 1948", ui.Q<Label>("why-turn").text);
    }

    /// <summary>Playtest 1, item 4: the effects group covers effects that are not policies too.</summary>
    [Test]
    public void TheEffectsGroupIsCalledOngoingEffects() => Assert.AreEqual("Ongoing effects", Text["why.effects"]);

    /// <summary>Playtest 1, item 5: every date the player sees goes through Statecraft.Sim.PlayerText ("15 May 1948") or,
    /// where the UI's language names the month, through the date.* strings (Control1948.DateText, #687, #701), so no UI
    /// string carries a date format of its own.</summary>
    [Test]
    public void NoUiStringFormatsADateItself()
    {
        var yaml = AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text;
        foreach (var line in yaml.Split('\n'))
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, "yyyy|MMM|MM-dd|:d "), "date format in strings.en.yaml: " + line);
    }
}
