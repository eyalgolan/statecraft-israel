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
        /// <summary>The Hebrew strings, loaded when Hebrew is asked for (UiLanguage; #193).</summary>
        public const string StringsHebrew = Folder + "/strings.he.yaml";
        /// <summary>The owner-reviewed English and Hebrew snapshot for settlement-control strings (R-GATE-002).</summary>
        public const string SettlementControlStringsReview = Folder + "/settlement-control-strings-review.yaml";
        public const string FontRegular = Folder + "/Fonts/EBGaramond-Regular.ttf";
        public const string FontSemiBold = Folder + "/Fonts/EBGaramond-SemiBold.ttf";
        /// <summary>The Hebrew face (EB Garamond has no Hebrew; #193): Frank Ruhl Libre, OFL.</summary>
        public const string FontHebrew = Folder + "/Fonts/FrankRuhlLibre/FrankRuhlLibre-Medium.ttf";
    }
}
