#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>#1527's follow-up: the unit lines (toast and Why) name a place, never a hex's coordinates; a one-point move
/// says "point".</summary>
public class WhyHexPlacesTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
    static readonly Dictionary<string, string> NamesHe = new Dictionary<string, string> { ["haifa"] = "חיפה", ["tel_aviv"] = "תל אביב" };
    static readonly Regex Coordinates = new Regex(@"\d+\s*,\s*-?\d+");

    static UnitRef Golani => new UnitRef(Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical).Units[0]);
    static string GolaniName(UiText text) => UnitNames.Of(Golani, C, Rows, text);

    static Cause Moved(string hex, int cost) => new Cause("map", "0", cost, "", key: WhyKeys.UnitMoved, units: new[] { Golani },
        args: new[] { hex, cost.ToString(System.Globalization.CultureInfo.InvariantCulture) });

    [Test]
    public void AMoveOntoATownsHexNamesTheTownAndOnePointIsSingular()
    {
        Assert.AreEqual(GolaniName(En) + " moved to Haifa for 1 movement point", UnitNames.Line(Moved("5,8", 1), C, Rows, En));
        var he = UnitNames.Line(Moved("5,8", 1), C, Rows, He, NamesHe);
        StringAssert.Contains("משושה חיפה", he);
        StringAssert.Contains("נקודת תנועה אחת", he);
        StringAssert.DoesNotContain("1 נקודות", he);
    }

    [Test]
    public void AMoveElsewhereSaysHowFarAndWhichWayFromTheNearestTown()
    {
        // (6,8) is the hex east of Haifa's (5,8): 8.5 km between the centres, said to the nearest 5 km.
        Assert.AreEqual(GolaniName(En) + " moved to a hex 10 km east of Haifa for 2 movement points", UnitNames.Line(Moved("6,8", 2), C, Rows, En));
        StringAssert.Contains("משושה 10 ק״מ ממזרח לחיפה", UnitNames.Line(Moved("6,8", 2), C, Rows, He, NamesHe));
        // (-3,20) is one hex from Tel Aviv, Lod and Rishon LeZion alike: the first in settlement_hexes.yaml's order wins.
        StringAssert.Contains("a hex 10 km south-east of Tel Aviv", UnitNames.Line(Moved("-3,20", 2), C, Rows, En));
    }

    /// <summary>A one-letter prefix joins a Hebrew place name directly, a name of two words included ("לחיפה", "בתל
    /// אביב"), and takes a maqaf before a name with no Hebrew form yet, which is shown in English (HE-07: "מ־Jaffa").</summary>
    [Test]
    public void APrefixJoinsAHebrewPlaceNameAndTakesAMaqafBeforeAnEnglishOne()
    {
        const char lrm = UiText.LeftToRightMark;
        StringAssert.Contains("אזרחים בתל אביב", UnitNames.Line(new Cause("map", "0", 0, "", key: WhyKeys.SentHome,
            units: new[] { Golani }, args: new[] { "500", "tel_aviv" }), C, Rows, He, NamesHe));
        // NamesHe has no Jaffa: its English name, after a maqaf. The hex it withdrew to is Haifa's, in Hebrew.
        StringAssert.Contains("נסוגה מ־" + lrm + "Jaffa" + lrm + " למשושה חיפה", UnitNames.Line(new Cause("map", "0", 0, "",
            key: WhyKeys.Withdrew, units: new[] { Golani }, args: new[] { "jaffa", "5,8" }), C, Rows, He, NamesHe));
        // No Hebrew names at all: Haifa in English, after a maqaf.
        StringAssert.Contains("משושה " + lrm + "10" + lrm + " ק״מ ממזרח ל־" + lrm + "Haifa" + lrm,
            UnitNames.Line(Moved("6,8", 2), C, Rows, He, null));
    }

    /// <summary>Every unit line, in both languages, with a hex of negative q (the Hebrew bidi case: "-3,20" showed as
    /// "3,20-"): no coordinates remain.</summary>
    [Test]
    public void NoUnitLineShowsCoordinatesInEitherLanguage()
    {
        const string hex = "-3,20";
        var args = new Dictionary<string, string[]>
        {
            [WhyKeys.UnitMoved] = new[] { hex, "2" },
            [WhyKeys.AttackCalledOff] = new[] { hex, "3", "1" },
            [WhyKeys.Engagement] = new[] { hex, "120", "10", "20", "1" },
            [WhyKeys.Destroyed] = new string[0],
            [WhyKeys.SurrenderedPrisoners] = new[] { "500" },
            [WhyKeys.SurrenderedHeld] = new[] { "500" },
            [WhyKeys.SentHome] = new[] { "500", "tel_aviv" },
            [WhyKeys.Withdrew] = new[] { "jaffa", hex },
        };
        foreach (var key in WhyKeys.All)
        {
            var units = key == WhyKeys.Engagement ? new[] { Golani, Golani } : new[] { Golani };
            var cause = new Cause("map", "0", 0, "", key: key, units: units, args: args.TryGetValue(key, out var a) ? a : new[] { hex });
            foreach (var (text, names) in new[] { (En, (Dictionary<string, string>?)null), (He, NamesHe) })
            {
                var line = UnitNames.Line(cause, C, Rows, text, names);
                Assert.IsFalse(Coordinates.IsMatch(line), $"{key}: {line}");
                StringAssert.DoesNotContain("hex -", line, key);
            }
        }
    }
}
