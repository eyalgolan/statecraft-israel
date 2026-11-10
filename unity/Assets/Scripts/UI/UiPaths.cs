#nullable enable
namespace Statecraft.Game.UI
{
    /// <summary>Project paths of the UI assets, shared by the editor scripts (Assets/Editor) and the edit-mode tests,
    /// which cannot reference the editor assembly.</summary>
    public static class UiPaths
    {
        public const string Folder = "Assets/UI";
        public const string Panel = Folder + "/StatecraftPanel.asset";
        public const string Theme = Folder + "/StatecraftTheme.tss";
        public const string GameUxml = Folder + "/Game.uxml";
        public const string Strings = Folder + "/strings.en.yaml";
        /// <summary>The Hebrew strings (UiLanguage; #193): translations/he/ui.yaml since #1618 step 4, beside the content and
        /// outside Assets/, so the AssetDatabase cannot load it. Relative to the project folder, where the editor runs.</summary>
        public const string StringsHebrew = "../translations/he/ui.yaml";
        /// <summary>The Hebrew strings' text, read from the repository's translations/ (the editor and the edit-mode tests).</summary>
        public static string HebrewText() => System.IO.File.ReadAllText(System.IO.Path.Combine(Paths.Translations, "he", "ui.yaml"));
        /// <summary>The UI strings' text in Hebrew or English (the editor and the edit-mode tests).</summary>
        public static string StringsText(bool hebrew) => hebrew ? HebrewText()
            : System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "UI", "strings.en.yaml"));
        /// <summary>The owner-reviewed English and Hebrew snapshot for settlement-control strings (R-GATE-002).</summary>
        public const string SettlementControlStringsReview = Folder + "/settlement-control-strings-review.yaml";
        public const string FontRegular = Folder + "/Fonts/EBGaramond-Regular.ttf";
        public const string FontSemiBold = Folder + "/Fonts/EBGaramond-SemiBold.ttf";
        /// <summary>The Hebrew face (EB Garamond has no Hebrew; #193): Frank Ruhl Libre, OFL.</summary>
        public const string FontHebrew = Folder + "/Fonts/FrankRuhlLibre/FrankRuhlLibre-Medium.ttf";
    }
}
