#nullable enable
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

/// <summary>#1399: the Why lines name units by their names, in English and Hebrew, never by the sim's number.</summary>
public class WhyUnitNamesTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [Test]
    public void EveryKeyHasALineInBothLanguages()
    {
        foreach (var key in WhyKeys.All)
        {
            Assert.IsTrue(En.Has("why.line." + key), key);
            Assert.IsTrue(He.Has("why.line." + key), key);
        }
    }

    [Test]
    public void AStartingUnitIsNamedByItsRowAndARaisedUnitByItsTypeAndHome()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var golani = Rows.Single(r => r.Id == C.StartingForces[0].Row);
        Assert.AreEqual(golani.Name, UnitNames.Of(new UnitRef(s.Units[0]), C, Rows, En));
        var raised = new UnitRef(new Unit(s.NextId, "recruit", 0, 0, side: "israel", strength: 2, homeCity: "tel_aviv"));
        var name = UnitNames.Of(raised, C, Rows, En);
        StringAssert.Contains("Tel Aviv", name);
        StringAssert.DoesNotContain(raised.Id.ToString(), name);
    }

    [Test]
    public void AMoveLineHasNoSimNumberInEitherLanguage()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var move = GoldenRun.SmokeMove(C, s);
        var line = Simulation.Preview(C, s, new Command[] { move }).Why.Single(w => w.Key == WhyKeys.UnitMoved);
        foreach (var text in new[] { En, He })
        {
            var shown = UnitNames.Line(line, C, Rows, text);
            StringAssert.DoesNotContain("Unit 0", shown);
            StringAssert.DoesNotMatch(@"\bunit \d", shown);
        }
    }
}
