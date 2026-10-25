#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>B1: the turn report, the meter tip and the Why screen word the economy, population, effect and security lines
/// (CauseKeys) in the player's language through CauseText. In English every row is the sim's Message, word for word, so
/// the English report is what it was before B1; in Hebrew each keyed line is Hebrew, its amounts the only Latin text.</summary>
public class CauseTextTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static readonly IReadOnlyList<UnitEntry> NoRows = Array.Empty<UnitEntry>();
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    /// <summary>The golden seed's whole game, played by the reference player, turn by turn.</summary>
    static IEnumerable<StepResult> Turns()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        for (int t = 0; t < 1000 && s.GameOver == null; t++)
        {
            var r = Simulation.Step(C, s, GoldenRun.Orders(C, s));
            yield return r;
            s = r.State;
        }
    }

    /// <summary>Before B1 the report and the tip showed each cause's Message; now CauseText's English. For a seeded game,
    /// every turn and every meter, the rows are the same, character for character, and so is every Why line.</summary>
    [Test]
    public void TheEnglishRowsAreTheSameAsBefore()
    {
        var en = En; int rows = 0, keyed = 0;
        foreach (var turn in Turns())
        {
            foreach (var id in HudModel.MeterIds)
            {
                var before = HudModel.Causes(turn, id, w => w.Message);
                CollectionAssert.AreEqual(before, HudModel.Causes(turn, id, w => CauseText.Line(w, C, NoRows, en)), id);
                rows += before.Count;
            }
            foreach (var w in turn.Why.Where(w => CauseKeys.All.Contains(w.Key)))
            {
                Assert.AreEqual(w.Message, CauseText.Line(w, C, NoRows, en), w.Key);
                keyed++;
            }
        }
        Assert.Greater(rows, 100, "precondition: the game has report rows");
        Assert.Greater(keyed, 1000, "precondition: the game has keyed lines");
    }

    /// <summary>In Hebrew every keyed line of the seeded game is Hebrew: right to left, every placeholder filled, and no
    /// Latin letter outside a value UiText marked left to right (an amount such as "IL -6.8m").</summary>
    [Test]
    public void EveryKeyedLineIsHebrewInHebrew()
    {
        var he = He; var seen = new HashSet<string>();
        foreach (var turn in Turns())
            foreach (var w in turn.Why.Where(w => CauseKeys.All.Contains(w.Key)))
            {
                AssertHebrew(CauseText.Line(w, C, NoRows, he), w.Key);
                seen.Add(w.Key);
            }
        Assert.GreaterOrEqual(seen.Count, 8, "precondition: the game reaches most keys: " + string.Join(", ", seen));
    }

    /// <summary>The keyed lines the seeded game does not reach: a call-up order, a situation that ends, civilian upkeep and
    /// its borrowing, the currency crisis on both sides, and the legacy loop's food cut and UN ceasefire.</summary>
    [Test]
    public void TheRareLinesAreWordedInBothLanguages()
    {
        var why = new List<Cause>();
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        Assert.IsNull(SecuritySystem.SetMobilization(C, s, 1.0, why));
        s.ActiveSituations.Add("food_crisis"); s.Indicators["food_supply"] = 0.6;
        EffectsSystem.UpdateSituations(C, s, why);
        // Civilian upkeep: the real content's places cost nothing yet, so the line is the sim's own (EconomySystem.PayCivilianUpkeep),
        // and the borrowing comes from PayOrBorrow with that purpose.
        why.Add(new Cause("economy", "treasury", -2, PlayerText.Change(C, "treasury", -2) + " (civilian upkeep)", key: CauseKeys.CivilianUpkeep));
        EconomySystem.PayOrBorrow(C, s, s.Treasury + 3, "for civilian upkeep", why, CauseKeys.CivilianUpkeep);
        s.ActiveSituations.Add(EconomySystem.FxCrisis); s.FxReserves = 1e6;
        EconomySystem.Apply(C, s, 0.25, why);
        s.ActiveSituations.Remove(EconomySystem.FxCrisis); s.FxReserves = -1e6;
        EconomySystem.Apply(C, s, 0.25, why);

        var legacy = SimBridgeGddTests.Legacy;
        var l = Simulation.NewGame(legacy, GoldenRun.Seed, Mode.Historical);
        l.ActiveSituations.Add(EconomySystem.FxCrisis); l.FxReserves = -1e6;
        EconomySystem.Apply(legacy, l, 0.25, why);
        l.Rung = 5; l.CeasefireWeeksLeft = 1;
        SecuritySystem.Apply(legacy, l, 1, why);

        var keys = why.Where(w => CauseKeys.All.Contains(w.Key)).Select(w => w.Key).Distinct().ToList();
        foreach (var key in new[] { CauseKeys.CallUpSet, CauseKeys.SituationEnded, CauseKeys.CivilianUpkeep, CauseKeys.Borrowed,
                     CauseKeys.FxCrisisEased, CauseKeys.FxCrisisBegan, CauseKeys.FxCrisisFood, CauseKeys.Ceasefire })
            CollectionAssert.Contains(keys, key, "precondition");
        var en = En; var he = He;
        foreach (var w in why.Where(w => CauseKeys.All.Contains(w.Key)))
        {
            var content = w.Target == "food_supply" || w.Key == CauseKeys.Ceasefire ? legacy : C;
            Assert.AreEqual(w.Message, CauseText.Line(w, content, NoRows, en), w.Key);
            AssertHebrew(CauseText.Line(w, content, NoRows, he), w.Key);
        }
    }

    /// <summary>A line with no key yet keeps the sim's English; a unit line is still UnitNames' (#1399).</summary>
    [Test]
    public void OtherLinesAreUnchanged()
    {
        var plain = new Cause("economy", "treasury", -1, "Money IL -1.0m (military upkeep)");
        Assert.AreEqual(plain.Message, CauseText.Line(plain, C, NoRows, He));
        var turn = Turns().First(t => t.Why.Any(w => WhyKeys.All.Contains(w.Key)));
        foreach (var w in turn.Why.Where(w => WhyKeys.All.Contains(w.Key)))
            Assert.AreEqual(UnitNames.Line(w, C, NoRows, He), CauseText.Line(w, C, NoRows, He));
    }

    static void AssertHebrew(string line, string key)
    {
        Assert.IsTrue(UiDirection.RightToLeft(line), $"{key}: '{line}' reads right to left");
        StringAssert.DoesNotContain("{", line, key);
        var unmarked = Regex.Replace(line, UiText.LeftToRightMark + "[^" + UiText.LeftToRightMark + "]*" + UiText.LeftToRightMark, "");
        Assert.IsFalse(Regex.IsMatch(unmarked, "[A-Za-z]"), $"{key}: '{line}' has Latin text outside a marked value");
    }
}
