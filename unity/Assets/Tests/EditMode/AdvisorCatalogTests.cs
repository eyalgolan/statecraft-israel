#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;

/// <summary>The advisors' data file (spec 4.1): it parses, and the checks name every broken row, so a bad row fails here
/// and never at a player's turn.</summary>
public class AdvisorCatalogTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    const string Head = "advisors:\n  - { id: ben_gurion, person: ben_gurion }\n  - { id: meyerson, person: golda_meir }\n  - { id: dayan, person: dayan }\n";

    static readonly AdvisorCatalog.Known Fixture = new AdvisorCatalog.Known
    {
        Signals = new[] { "new_game", "turn_ended" }, Conditions = new[] { "debt_ratio_at_least" },
        Targets = new[] { "attention" }, Screens = new[] { "budget" }, Quotes = new[] { "q1" },
    };

    static UiText Strings(params string[] keys) =>
        UiText.Parse("ui.lang: en\n" + string.Join("", keys.Select(k => k + ": \"One short line.\"\n")));

    static List<string> ProblemsOf(string messages, params string[] keys) =>
        AdvisorCatalog.Parse(Head + messages).Problems(Strings(keys), Strings(keys), Fixture);

    [TestCase("{ picture: ../x.mp4, narration: { en: x.en.wav }, cues: { en: [0] }, label: lab }", "file name") ]
    [TestCase("{ picture: x.gif, narration: { en: x.en.wav }, cues: { en: [0.0] }, label: lab }", "picture must be .mp4, .jpg or .png")]
    [TestCase("{ picture: missing.mp4, narration: { en: x.en.wav }, cues: { en: [0.0] }, label: lab }", "no StreamingAssets/advisors/missing.mp4")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.mp3 }, cues: { en: [0.0] }, label: lab }", "narration must be .wav")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.wav }, cues: { en: [0.5] }, label: lab }", "cues start at 0")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.wav }, cues: { en: [0.0, 3.0, 2.0] }, label: lab }", "cues must rise")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.wav, he: x.he.wav }, cues: { en: [0.0], he: [0.0] }, label: lab }", "Hebrew narration before its approval")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.wav }, cues: { en: [0.0, 2.0] }, label: lab }", "no advisor.x.cue.2")]
    [TestCase("{ picture: x.mp4, narration: { en: x.en.wav }, cues: { en: [0.0] }, label: nolabel }", "no nolabel")]
    [TestCase("{ label: lab }", "media needs narration")]          // no picture and no narration: nothing to play (spec 12)
    public void EachBrokenMediaRowIsNamed(string media, string expected)
    {
        var known = new AdvisorCatalog.Known { Signals = new[] { "new_game" }, StreamingFile = f => f.StartsWith("x.", System.StringComparison.Ordinal) };
        var c = AdvisorCatalog.Parse(Head + "messages:\n  - { id: x, advisor: dayan, signal: new_game, level: new, media: " + media + " }\n");
        var t = Strings("advisor.x.text", "advisor.x.cue.1", "lab");
        var problems = c.Problems(t, t, known);
        Assert.IsTrue(problems.Any(p => p.Contains(expected)), $"expected '{expected}' in:\n{string.Join("\n", problems)}");
    }

    [Test]
    public void AGoodMediaRowHasNoProblems()
    {
        var known = new AdvisorCatalog.Known { Signals = new[] { "new_game" }, StreamingFile = _ => true };
        var c = AdvisorCatalog.Parse(Head + "messages:\n  - { id: x, advisor: dayan, signal: new_game, level: new, he_review: approved, " +
            "media: { picture: x.jpg, narration: { en: x.en.wav, he: x.he.wav }, cues: { en: [0.0, 2.5], he: [0.0, 2.8] }, label: lab } }\n");
        var t = Strings("advisor.x.text", "advisor.x.cue.1", "advisor.x.cue.2", "lab");
        Assert.IsNotNull(c.Messages.Single().Media);
        Assert.AreEqual("x.jpg", c.Messages.Single().Media!.Picture);
        Assert.IsEmpty(c.Problems(t, t, known));
    }

    /// <summary>The owner's ruling of 24 October 2026 (#1547): a line without a clip keeps the advisor's portrait, so the
    /// picture is optional when the row has narration.</summary>
    [Test]
    public void AGoodNarrationOnlyRowHasNoProblems()
    {
        var known = new AdvisorCatalog.Known { Signals = new[] { "new_game" }, StreamingFile = _ => true };
        var c = AdvisorCatalog.Parse(Head + "messages:\n  - { id: x, advisor: dayan, signal: new_game, level: new, media: { narration: { en: x.en.wav }, cues: { en: [0.0] }, label: lab } }\n");
        Assert.IsNull(c.Messages.Single().Media!.Picture, "no picture: the portrait stays");
        Assert.IsEmpty(c.Problems(Strings("advisor.x.text", "advisor.x.cue.1", "lab"), Strings("advisor.x.text", "advisor.x.cue.1", "lab"), known));
    }

    [Test]
    public void ParsesAMessageWithEveryField()
    {
        var c = AdvisorCatalog.Parse(Head + "messages:\n  - { id: a.one, advisor: meyerson, signal: turn_ended, when: { debt_ratio_at_least: 0.8 }, " +
            "level: warnings, priority: 2, repeat: every, cooldown_turns: 3, actions: [{ show_me: attention }, { open_screen: budget }], he_review: approved }\n");
        var m = c.Find("a.one")!;
        Assert.AreEqual(("meyerson", "turn_ended", "warnings", 2, "every", 3), (m.Advisor, m.Signal, m.Level, m.Priority, m.Repeat, m.CooldownTurns));
        Assert.AreEqual(0.8, m.When["debt_ratio_at_least"], 1e-9);
        CollectionAssert.AreEqual(new[] { "attention", "" }, m.Actions.Select(a => a.ShowMe).ToArray());
        Assert.IsFalse(m.Teaching); Assert.IsFalse(m.Critical);
        Assert.IsTrue(m.ApprovedIn(hebrew: true));
        Assert.IsEmpty(ProblemsOf("messages:\n  - { id: a.one, advisor: meyerson, signal: turn_ended, level: new, he_review: pending }\n", "advisor.a.one.text"));
    }

    [TestCase("{ id: Bad Id, advisor: dayan, signal: new_game, level: new }", "lower-case letters")]
    [TestCase("{ id: x, advisor: golda, signal: new_game, level: new }", "unknown advisor 'golda'")]
    [TestCase("{ id: x, advisor: dayan, signal: moon, level: new }", "unknown signal 'moon'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, when: { tide_at_least: 1 } }", "unknown condition 'tide_at_least'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: loud }", "level must be one of")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, priority: 4 }", "priority must be 1 to 3")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, repeat: every }", "every needs cooldown_turns")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, cooldown_turns: 2 }", "once has no cooldown_turns")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, requires: [nobody] }", "requires unknown message 'nobody'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, actions: [{ show_me: moon }] }", "unknown show_me target 'moon'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, actions: [{ open_screen: moon }] }", "unknown screen 'moon'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, actions: [{ encyclopedia: moon }] }", "unknown page 'moon'")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, actions: [{ show_me: attention, open_screen: budget }] }", "exactly one of")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, actions: [{ open_card: true }, { open_card: true }, { open_card: true }] }", "at most two actions")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, quote: q9 }", "not on the verified list")]
    [TestCase("{ id: x, advisor: dayan, signal: new_game, level: new, he_review: maybe }", "he_review must be")]
    public void EachBrokenRowIsNamed(string row, string expected)
    {
        var problems = ProblemsOf("messages:\n  - " + row + "\n", "advisor.x.text", "advisor.Bad Id.text");
        Assert.IsTrue(problems.Any(p => p.Contains(expected)), $"expected '{expected}' in:\n{string.Join("\n", problems)}");
    }

    [Test]
    public void TextIsCheckedInBothLanguages()
    {
        var c = AdvisorCatalog.Parse(Head + "messages:\n  - { id: x, advisor: dayan, signal: new_game, level: new }\n");
        var en = UiText.Parse("advisor.x.text: \"" + string.Join(" ", Enumerable.Repeat("word", 36)) + "\"\n");
        var he = UiText.Parse("other: \"x\"\n");
        var problems = c.Problems(en, he, Fixture);
        Assert.IsTrue(problems.Any(p => p.Contains("over 35 words")), string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("no advisor.x.text in strings.he.yaml")), string.Join("\n", problems));
        var dash = UiText.Parse("advisor.x.text: \"One \u2014 two.\"\n");
        Assert.IsTrue(c.Problems(dash, dash, Fixture).Any(p => p.Contains("em dash")));
    }

    [Test]
    public void RequiresHasNoCycleAndDisagreementIsMutual()
    {
        var cycle = ProblemsOf("messages:\n  - { id: a, advisor: dayan, signal: new_game, level: new, requires: [b] }\n" +
                               "  - { id: b, advisor: dayan, signal: new_game, level: new, requires: [a] }\n", "advisor.a.text", "advisor.b.text");
        Assert.IsTrue(cycle.Any(p => p.Contains("requires has a cycle")), string.Join("\n", cycle));
        var oneWay = ProblemsOf("messages:\n  - { id: a, advisor: dayan, signal: new_game, level: warnings, disagrees_with: b }\n" +
                                "  - { id: b, advisor: meyerson, signal: new_game, level: warnings }\n", "advisor.a.text", "advisor.b.text");
        Assert.IsTrue(oneWay.Any(p => p.Contains("names this one back")), string.Join("\n", oneWay));
    }

    [Test]
    public void EveryAdvisorIsListedOnce()
    {
        var c = AdvisorCatalog.Parse("advisors:\n  - { id: dayan, person: dayan }\n  - { id: dayan, person: dayan }\nmessages: []\n");
        var problems = c.Problems(Strings(), Strings(), Fixture);
        foreach (var expected in new[] { "'ben_gurion' must be listed once", "'meyerson' must be listed once", "'dayan' must be listed once" })
            Assert.IsTrue(problems.Any(p => p.Contains(expected)), expected);
    }

    /// <summary>Spec 4.1's load test: the shipped file, against the game's own signals, targets, screens, quotes and files.</summary>
    [Test]
    public void TheShippedFileHasNoProblems()
    {
        var c = AdvisorCatalog.Load();
        CollectionAssert.AreEqual(AdvisorCatalog.AdvisorIds, c.Advisors.Select(a => a.Id).ToArray());
        var problems = c.Problems(En, He, AdvisorCatalog.Shipped());
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
