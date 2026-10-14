using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Project settings (M0 API corrections and M3 web settings): IL2CPP for Standalone and WebGL, the
    /// Statecraft WebGL template (full-window canvas, autoSyncPersistentDataPath), compression off by default (the build
    /// script sets it per target), Map.unity as the only and first scene.
    /// Run headless: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ProjectSetup.Apply</summary>
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/Map.unity";
        public const string WebTemplate = "PROJECT:Statecraft";

        public static void Apply()
        {
            PlayerSettings.companyName = "Statecraft";
            PlayerSettings.productName = "Statecraft Israel";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template = WebTemplate;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            // Issue #437: the files under Build/ are named by their content hash, so a browser that cached an earlier build
            // cannot mix its files into this one; the build's index.html names its own files.
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            // Debug.Log lines (GAME OK, TURN, PROFILE) without a script stack trace; warnings and errors keep theirs (Task 24 review M9).
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var template = player.FindProperty("templateDefaultScene");   // no public API; the sample scene it named is gone
            if (template != null && template.stringValue != BootScene) { template.stringValue = BootScene; player.ApplyModifiedPropertiesWithoutUndo(); }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
