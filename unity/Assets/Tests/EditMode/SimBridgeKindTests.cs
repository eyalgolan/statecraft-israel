#nullable enable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>#595: a refused order names the kind of thing (policy, city, unit, …) in the UI's language. Each kind word is a
/// string, order.kind.&lt;kind&gt;, in strings.en.yaml and strings.he.yaml; English reads as it did before.</summary>
public class SimBridgeKindTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static readonly Regex Latin = new Regex("[A-Za-z]");

    /// <summary>The kinds SimBridge.Validate can name: every Unknown("kind", …) call in its source. A call whose kind is
    /// not a literal fails the count, so a new kind cannot slip past this test.</summary>
    static string[] KindsInTheBridge()
    {
        var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/SimBridge.cs"));
        var calls = Regex.Matches(source, @"(?<![\w.])Unknown\(").Count - 1;   // less the method's own declaration
        var kinds = Regex.Matches(source, @"(?<![\w.])Unknown\(""([a-z_]+)""").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        Assert.AreEqual(calls, kinds.Count, "every Unknown(...) call names its kind as a literal");
        return kinds.Distinct().OrderBy(k => k, System.StringComparer.Ordinal).ToArray();
    }

    [Test]
    public void EveryKindTheBridgeCanNameHasAnEnglishAndAHebrewWord()
    {
        var kinds = KindsInTheBridge();
        Assert.GreaterOrEqual(kinds.Length, 13, "the kinds are read from the source");
        var en = En;
        var he = He;
        foreach (var k in kinds)
        {
            // English as before #595: the word the bridge passed, "budget line" for budget_line.
            Assert.AreEqual(k.Replace('_', ' '), en["order.kind." + k], k);
            var word = he["order.kind." + k];
            Assert.IsTrue(UiDirection.HasRightToLeft(word), k + ": " + word);
            Assert.IsFalse(Latin.IsMatch(word), k + ": " + word);
            Assert.AreEqual(-1, word.IndexOfAny(new[] { '"', '\'' }), k + ": gershayim and geresh are ״ and ׳, not ASCII quotes");
        }
    }

    [Test]
    public void AnEnglishRefusalReadsAsBefore()
    {
        LogAssert.Expect(LogType.Log, new Regex("unknown policy 'curfew'"));
        Assert.AreEqual("Unknown policy", new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En).Queue(new SetPolicy("curfew", 1)));
        LogAssert.Expect(LogType.Log, new Regex("unknown budget line 'nowhere'"));
        var shares = new System.Collections.Generic.Dictionary<string, double> { ["nowhere"] = 1 };
        Assert.AreEqual("Unknown budget line", new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En).Queue(new AllocateBudget(shares)));
    }

    /// <summary>The issue's done-when: an unknown id queued with the Hebrew strings gives a reason with no Latin kind word
    /// (no Latin letter at all), for a legacy kind and a GDD one.</summary>
    [Test]
    public void AHebrewRefusalHasNoEnglishWord()
    {
        var he = He;
        LogAssert.Expect(LogType.Log, new Regex("unknown policy 'curfew'"));
        var policy = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he).Queue(new SetPolicy("curfew", 1));
        Assert.AreEqual(he.Format("order.unknown", he["order.kind.policy"]), policy);
        LogAssert.Expect(LogType.Log, new Regex("unknown city 'no_such_city'"));
        var city = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he).Queue(new AssignWorkers("no_such_city", "job", 1));
        Assert.AreEqual(he.Format("order.unknown", he["order.kind.city"]), city);
        foreach (var reason in new[] { policy!, city! })
        {
            Assert.IsFalse(Latin.IsMatch(reason), reason);
            Assert.IsTrue(UiDirection.RightToLeft(reason), reason);
        }
    }
}
