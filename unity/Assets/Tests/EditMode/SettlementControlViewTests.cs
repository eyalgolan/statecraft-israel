#nullable enable
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

public class SettlementControlViewTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [TestCase(SettlementControl.AwaitingChoice, false, "awaiting_choice")]
    [TestCase(SettlementControl.Occupied, false, "occupied")]
    [TestCase(SettlementControl.AwaitingEnd, false, "awaiting_end")]
    [TestCase("", true, "depopulated")]
    public void EveryRecordOnMainHasOneView(string state, bool depopulated, string expected)
    {
        var owner = depopulated ? "" : "israel";
        var record = new SettlementControl(owner, state, depopulated: depopulated,
            turns: state == SettlementControl.Occupied ? 4 : 0);
        Assert.AreEqual(expected, SettlementControlView.Key(record));
    }

    [Test]
    public void NoRecordOrAnUnknownStateHasNoView()
    {
        Assert.IsNull(SettlementControlView.Key(null));
        Assert.IsNull(SettlementControlView.Key(new SettlementControl("israel", "something_new")));
    }

    [Test]
    public void TheOccupiedPanelLineCarriesTheTurnsLeft()
    {
        var occupied = new SettlementControl("israel", SettlementControl.Occupied, turns: 4);
        Assert.AreEqual(En["map.control.occupied"], SettlementControlView.MapText(En, occupied));
        Assert.AreEqual(En.Format("info.control.occupied", "Lydda", 4), SettlementControlView.PanelText(En, "Lydda", occupied));
    }

    [Test]
    public void OneTurnLeftReadsRightInBothLanguages()
    {
        var last = new SettlementControl("israel", SettlementControl.Occupied, turns: 1);
        Assert.AreEqual("Lydda: occupied, turns left: 1", SettlementControlView.PanelText(En, "Lydda", last), "count-neutral: no \"1 turns\"");
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        StringAssert.EndsWith("תורות שנותרו: " + UiText.LeftToRightMark + "1" + UiText.LeftToRightMark,
            SettlementControlView.PanelText(he, "לוד", last), "count-neutral label form, the number after the colon");
    }

    [Test]
    public void NewSettlementControlStringsHaveEnglishHebrewParity()
    {
        var en = En;
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        var keys = new[]
        {
            "map.control.awaiting_choice", "map.control.occupied", "map.control.awaiting_end", "map.control.depopulated",
            "info.control.awaiting_choice", "info.control.occupied", "info.control.awaiting_end", "info.control.depopulated",
            "hud.standing_displaced_places",
        };
        foreach (var key in keys)
        {
            Assert.IsTrue(en.Has(key), "English: " + key);
            Assert.IsTrue(he.Has(key), "Hebrew: " + key);
            CollectionAssert.AreEqual(Placeholders(en[key]), Placeholders(he[key]), key + ": placeholders");
            StringAssert.IsMatch("[א-ת]", he[key], key + ": Hebrew letters");
            if (key.StartsWith("map.control."))
                Assert.IsEmpty(Placeholders(en[key]), key + ": map labels carry no values");
        }
    }

    static string[] Placeholders(string value) => Regex.Matches(value, @"\{(\d+)(?:[^{}]*)\}")
        .Cast<Match>().Select(x => x.Groups[1].Value).Distinct().OrderBy(x => x).ToArray();
}
