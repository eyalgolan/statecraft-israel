#nullable enable
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>#2530: a refusal's English count names its noun in the singular for 1 ("1 civilian without work"), and reads
/// as the sim's own English for every other count; the Hebrew strings name the number in a label, grammatical for any
/// count. The sim's refusal formats are unchanged.</summary>
public class RefusalTextTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [Test]
    public void ACivilianCountOfOneNamesTheSingularInEnglishOnly()
    {
        var town = "Rishon LeZion";
        Assert.AreEqual(town + " has 1 civilian without work",
            SimBridge.ReasonText(En, string.Format(System.Globalization.CultureInfo.InvariantCulture, Training.RefusedIdle, town, 1)));
        Assert.AreEqual(town + " has 1 civilian or craftsman",
            SimBridge.ReasonText(En, string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeople, town, 1)));
        Assert.AreEqual(town + " has 1 civilian or craftsman without a job",
            SimBridge.ReasonText(En, string.Format(System.Globalization.CultureInfo.InvariantCulture, Workshops.RefusedPeopleNoJob, town, 1)));
        // #3020: the ledger game's free-record counts name theirs too.
        Assert.AreEqual(town + " has 1 free civilian without an assignment",
            SimBridge.ReasonText(En, string.Format(System.Globalization.CultureInfo.InvariantCulture, PopulationLedger.Refusals["Workshops.LedgerRefusedFree"], town, 1)));
        Assert.AreEqual(town + " has 1 free pioneer without work",
            SimBridge.ReasonText(En, string.Format(System.Globalization.CultureInfo.InvariantCulture, PopulationLedger.Refusals["Qualification.LedgerRefusedFreePioneers"], town, 1)));
        // Every other count is the sim's own English, word for word.
        var reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, Training.RefusedIdle, town, 6);
        Assert.AreEqual(reason, SimBridge.ReasonText(En, reason));
        // The Hebrew names the number in its label: no noun to inflect.
        var he = SimBridge.ReasonText(He, string.Format(System.Globalization.CultureInfo.InvariantCulture, Training.RefusedIdle, town, 1));
        StringAssert.IsMatch("[\u05D0-\u05EA]", he);
        StringAssert.Contains("1", he);
    }
}
