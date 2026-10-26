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

    /// <summary>Every unit the Why lines can name: each starting force (Israel's brigades and every Arab force, by its row),
    /// and each type a unit of Israel's is raised as in play (recruits, veterans, the guns, the armoured cars and the halutz
    /// groups, which have no row), with a home city and without one.</summary>
    static IEnumerable<UnitRef> EveryUnit(GameState s)
    {
        foreach (var id in Enumerable.Range(0, C.StartingForces.Count).Where(s.Units.ContainsKey)) yield return new UnitRef(s.Units[id]);
        foreach (var type in C.UnitTypes.Where(t => t.Kind == "unit" && (t.Side == "" || t.Side == Movement.PlayerSide)))
            foreach (var home in new[] { "tel_aviv", "" })
                yield return new UnitRef(new Unit(s.NextId, type.Id, 0, 0, side: type.Side, strength: 2, homeCity: home));
    }

    /// <summary>Each line's values after its unit names, as the sim gives them (UnitLines.Line and the engagement's own).</summary>
    static string[] Args(string key) => key switch
    {
        WhyKeys.UnitMoved => new[] { "5,5", "2" },
        WhyKeys.AttackCalledOff => new[] { "5,5", "2", "1" },
        WhyKeys.Engagement => new[] { "5,5", "80", "10", "20", "1" },
        WhyKeys.Destroyed => new string[0],
        WhyKeys.SurrenderedPrisoners => new[] { "300" },
        WhyKeys.SurrenderedHeld => new[] { "300" },
        WhyKeys.SentHome => new[] { "300", "tel_aviv" },
        WhyKeys.Withdrew => new[] { "tel_aviv", "5,5" },
        _ => new[] { "5,5" },
    };

    /// <summary>Codex's review of #1527, finding 2: a unit's name never meets a verb or pronoun that fits only some names.
    /// English: every name is singular, as the lines' verbs are ("was destroyed", "its people"); a raised unit is named as one
    /// formation, never by a plural type label ("Recruits"). Hebrew: the lines agree with no name's gender or number (HE-15 in
    /// docs/WRITING_UI_HE.md; the Arab forces' names are masculine, Israel's feminine, the type labels plural): the name
    /// opens the line as its label ("{0}: ..."), or sits in a noun phrase ("ההתקפה של {0}", the engagement's brackets), or
    /// is followed by no verb ("{0} בדרך"). Every unit, through every line, in both languages.</summary>
    [Test]
    public void EveryUnitReadsGrammaticallyInEveryLineInBothLanguages()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var en = En; var he = He; var rows = Rows;   // each property parses its file again: once here, not per line
        var enemy = new UnitRef(s.Units.Values.First(u => u.Side != "" && u.Side != Movement.PlayerSide));
        var plurals = new[] { "city.track_recruit", "unit.type.veteran_soldier", "city.track_armoured_car", "city.track_halutz" }.Select(k => en[k]).ToList();
        var checkedLines = 0;
        foreach (var unit in EveryUnit(s))
        {
            var english = UnitNames.Of(unit, C, rows, en);
            Assert.IsFalse(plurals.Any(p => english.StartsWith(p, System.StringComparison.Ordinal)), $"{unit.Type}: '{english}' is plural");
            // Every line, and a one-point move too (why.line.unit_moved_one, #1633).
            foreach (var (key, args) in WhyKeys.All.Select(k => (k, Args(k))).Append((WhyKeys.UnitMoved, new[] { "5,5", "1" })))
            {
                var units = key == WhyKeys.Engagement ? new[] { unit, enemy } : new[] { unit };
                var cause = new Cause("map", "0", 0, "fallback", key: key, units: units, args: args);
                foreach (var text in new[] { en, he })
                {
                    var name = UnitNames.Of(unit, C, rows, text);
                    StringAssert.DoesNotContain(unit.Type, name, $"{unit.Type} has a name in {text["ui.lang"]}");
                    var line = UnitNames.Line(cause, C, rows, text).Replace(UiText.LeftToRightMark.ToString(), "");   // the marks around a Latin value
                    StringAssert.Contains(name, line, key);
                    StringAssert.DoesNotContain("{", line, key);
                    if (text == he)
                        Assert.IsTrue(line.StartsWith(name + ": ", System.StringComparison.Ordinal) || line.Contains("של " + name)
                            || key == WhyKeys.Engagement || key == WhyKeys.OnItsWay && line.StartsWith(name + " בדרך ", System.StringComparison.Ordinal),
                            $"{key}: no verb may agree with '{name}': {line}");
                    checkedLines++;
                }
            }
        }
        Assert.Greater(checkedLines, 0);
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
