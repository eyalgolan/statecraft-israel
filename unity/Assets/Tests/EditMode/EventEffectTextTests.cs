#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>R-EVT-005: independent formatter tests; production card integration and its layout/focus gates follow #1091/#1105.</summary>
public class EventEffectTextTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text(bool hebrew) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
        hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static string Plain(string value) => value.Replace(UiText.LeftToRightMark.ToString(), "");
    static readonly string City = C.Cities.First().Id;
    static readonly string Place = PlayerText.Settlement(C, City);
    static readonly string Rejoin = C.Events.First().Id;

    // Direct helper inputs exercise current presentation categories, not new authored content or grant rules.
    static EventOption Option() => new EventOption
    {
        Effects = new List<OptionEffect>
        {
            new OptionEffect { Target = "morale", Delta = .041 },
            new OptionEffect { Target = "treasury", Delta = -1.25 },
            new OptionEffect { Target = "food", Delta = 2.5 },
            new OptionEffect { Target = "arms", Delta = -100 },
        },
        GrantUnits = new List<UnitGrant>
        {
            new UnitGrant { Type = "halutz", N = 2, At = City },
            new UnitGrant { Type = "recruit", N = 1, At = City },
            new UnitGrant { Type = "veteran_halutz", N = 3, At = City },
            new UnitGrant { Type = "untranslated_role_internal_id", N = 4, At = City },
        },
        CityEffects = new List<CityEffect>
        {
            new CityEffect { City = City, Field = EventSystem.HousingField, Delta = 3 },
            new CityEffect { City = City, Field = EventSystem.JobsField, Delta = -100 },
            new CityEffect { City = City, Field = EventSystem.ConditionField, Delta = .051 },
        },
        SetPolicy = new Dictionary<string, double> { ["housing_construction"] = .75 }, SetRung = 4, Rejoin = Rejoin,
    };

    [TestCase(false, "en-US")]
    [TestCase(false, "de-DE")]
    [TestCase(false, "ar-SA")]
    [TestCase(true, "en-US")]
    [TestCase(true, "de-DE")]
    [TestCase(true, "ar-SA")]
    public void EveryExistingCategoryReadsTheSimAmountsInEitherLanguage(bool hebrew, string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
            var hash = s.Hash();
            var option = Option();
            var causes = EventSystem.OptionEffects(C, s, option);
            var rows = EventSystem.OptionEffectPreviews(C, s, option);
            Assert.AreEqual(causes.Select(x => (x.System, x.Target, x.Delta, x.Message)),
                rows.Select(x => (x.Cause.System, x.Cause.Target, x.Cause.Delta, x.Cause.Message)));
            var actual = rows.Select(x => Plain(EventEffectText.Format(x, Text(hebrew)))).ToArray();
            var title = Cabinet.Fill(C, s, C.Events.First(x => x.Id == Rejoin).Title, s.Date);
            // English data names are deliberate fallbacks: current indicator/city/policy rows have no Hebrew field.
            var expected = hebrew ? new[]
            {
                "Social cohesion: שינוי +4.1%", "Money: שינוי IL -1.3m", "מזון +3 יחידות", "נשק -100 יחידות",
                "חלוצים: 2, מיקום: " + Place, "מגויסים: 1, מיקום: " + Place,
                "חלוצים ותיקים: 3, מיקום: " + Place, "יחידות: 4, מיקום: " + Place,
                Place + ": קיבולת דיור, שינוי +3", Place + ": מקומות עבודה, שינוי -100", Place + ": מצב הדיור, שינוי +5.1%",
                "Housing construction: ערך חדש 75%", "רמת הביטחון תיקבע ל־4", "חזרה למסלול ההיסטורי: " + title,
            } : new[]
            {
                "Social cohesion +4.1%", "Money IL -1.3m", "Food +3 units", "Arms -100 units",
                "Halutzim: 2 at " + Place, "Recruits: 1 at " + Place,
                "Veteran Halutzim: 3 at " + Place, "Units: 4 at " + Place,
                Place + ": housing capacity +3", Place + ": job slots -100", Place + ": housing condition +5.1%",
                "Housing construction set to 75%", "Security level set to 4", "Returns to the recorded course: " + title,
            };
            CollectionAssert.AreEqual(expected, actual);
            Assert.AreEqual(hash, s.Hash(), "formatting must not choose, spend, queue or mutate state");
            StringAssert.DoesNotContain("untranslated_role_internal_id", string.Join(" / ", actual));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestCase(0, "לכידות", "לכידות: שינוי +4.1%")]
    [TestCase(4, "עיר לדוגמה", "חלוצים: 2, מיקום: עיר לדוגמה")]
    [TestCase(8, "עיר לדוגמה", "עיר לדוגמה: קיבולת דיור, שינוי +3")]
    [TestCase(11, "בניית דיור", "בניית דיור: ערך חדש 75%")]
    [TestCase(13, "כותרת מתורגמת", "חזרה למסלול ההיסטורי: כותרת מתורגמת")]
    public void TheCallerMaySupplyALocalizedNameWithoutChangingTheSim(int index, string name, string expected)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var row = EventSystem.OptionEffectPreviews(C, s, Option())[index];
        var original = (row.ContextName, row.Amount, row.Cause.Message);
        Assert.AreEqual(expected, Plain(EventEffectText.Format(row, Text(true), name)));
        Assert.AreEqual(original, (row.ContextName, row.Amount, row.Cause.Message), "formatting preserves the sim context");
        Assert.AreEqual(EventEffectText.Format(row, Text(false)), EventEffectText.Format(row, Text(false), ""));
    }

    [Test]
    public void HebrewTemplatesKeepTheSignedAmountTogetherUsingTheExistingBidiFormatter()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var rows = EventSystem.OptionEffectPreviews(C, s, Option());
        var mark = UiText.LeftToRightMark.ToString();
        StringAssert.Contains(mark + "-100" + mark, EventEffectText.Format(rows[3], Text(true)));
        StringAssert.Contains(mark + "+5.1%" + mark, EventEffectText.Format(rows[10], Text(true)));
        StringAssert.Contains(mark + "IL -1.3m" + mark, EventEffectText.Format(rows[1], Text(true), "אוצר"));
    }

    // A project unlock (R-EFX-008) is one of the helper's entries too, so it has a template; the Hebrew kind name comes
    // from the caller (ContentNames.Of), as the other content names do.
    [TestCase(false, null, "{0} can now be built")]
    [TestCase(true, null, "אפשר לבנות עכשיו: {0}")]
    [TestCase(true, "שיכון", "אפשר לבנות עכשיו: שיכון")]
    public void AProjectUnlockReadsInEitherLanguage(bool hebrew, string? name, string expected)
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var kind = C.ProjectKinds.First();
        var row = EventSystem.OptionEffectPreviews(C, s, new EventOption { UnlockProjects = new List<string> { kind.Id } }).Single();
        Assert.AreEqual(OptionEffectPreviewKind.ProjectUnlock, row.Kind);
        Assert.AreEqual(string.Format(expected, PlayerText.Capitalised(kind.Name)), Plain(EventEffectText.Format(row, Text(hebrew), name)));
        if (!hebrew) Assert.AreEqual(row.Cause.Message, Plain(EventEffectText.Format(row, Text(false))));
    }

    [Test]
    public void AnUnrecognizedCityFieldFailsInsteadOfLeakingItsInternalId()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var option = new EventOption { CityEffects = new List<CityEffect>
            { new CityEffect { City = City, Field = "unsupported_internal_field", Delta = 1 } } };
        var row = EventSystem.OptionEffectPreviews(C, s, option).Single();
        Assert.Throws<ArgumentOutOfRangeException>(() => EventEffectText.Format(row, Text(false)));
    }
}
