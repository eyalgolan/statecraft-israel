using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the UI Toolkit PanelSettings asset (tech notes 1.3): scale with screen size against 1920x1080,
    /// matching width and height equally, with the default runtime theme and the ICU data of the advanced text generator.
    /// An asset, not a runtime instance, so its theme, shader and ICU references are included in player builds (the M0
    /// Shader.Find lesson). Idempotent.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.UiAssets.Create</summary>
    public static class UiAssets
    {
        /// <summary>The ICU data Unity ships for the advanced text generator (a built-in extra resource, about 380 KB).</summary>
        public const string IcuData = "icudt73l.bytes";

        public static void Create()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPaths.Theme);
            if (theme == null) throw new System.IO.FileNotFoundException("theme missing (commit Assets/UI/StatecraftTheme.tss)", UiPaths.Theme);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, UiPaths.Panel); }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            // The advanced text generator (right-to-left Hebrew on the start screen; #193) reads ICU data from the panel
            // settings. With UI Toolkit's "Enable Advanced Text" on (ProjectSettings/UIToolkitProjectSettings.asset) the
            // editor assigns it on load; it is saved here so the asset, and so the player build, always carries it.
            var so = new SerializedObject(ps);
            var icu = so.FindProperty("m_ICUDataAsset");
            if (icu == null) throw new System.InvalidOperationException("PanelSettings has no m_ICUDataAsset (Unity version change?)");
            icu.objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<TextAsset>(IcuData);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
        }
    }
}
