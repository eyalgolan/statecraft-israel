using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

/// <summary>The M3 player settings (ProjectSetup.Apply) and the custom WebGL template.</summary>
public class ProjectSettingsTests
{
    [Test]
    public void PlayersUseIl2CppTheStatecraftTemplateAndMapUnityAsTheBootScene()
    {
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL));
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
        Assert.AreEqual("PROJECT:Statecraft", PlayerSettings.WebGL.template);
        Assert.IsFalse(PlayerSettings.WebGL.decompressionFallback);
        Assert.AreEqual(WebGLCompressionFormat.Disabled, PlayerSettings.WebGL.compressionFormat);
        Assert.AreEqual(new[] { "Assets/Map.unity" }, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
    }

    /// <summary>Issue #437: every WebGL build names the files under Build/ by their content hash, so a browser that cached
    /// an earlier build cannot mix its files into a newer one; index.html names the files of its own build.</summary>
    [Test]
    public void WebBuildFilesAreNamedByTheirContentHash()
    {
        Assert.IsTrue(PlayerSettings.WebGL.nameFilesAsHashes);
    }

    [Test]
    public void TheNewSceneTemplateDoesNotPointAtTheDeletedSampleScene()
    {
        var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        Assert.AreEqual("Assets/Map.unity", player.FindProperty("templateDefaultScene").stringValue);
    }

    [Test]
    public void TheTemplateFillsItsGameRegionAndPersistsSaves()
    {
        var html = File.ReadAllText("Assets/WebGLTemplates/Statecraft/index.html");
        // #1119 B reserves a separate companion region; the canvas fills its game region rather than covering it.
        // The real browser layout check covers desktop/phone bounds and safe insets. Keep the save contract here.
        StringAssert.Contains("#player-layout { display: grid;", html);
        StringAssert.Contains("<main id=\"game-view\">", html);
        StringAssert.Contains("#unity-canvas { width: 100%; height: 100%;", html);
        StringAssert.Contains("<aside id=\"companion\"", html);
        StringAssert.Contains("autoSyncPersistentDataPath: true", html);
        StringAssert.DoesNotContain("alert(", html);
    }

    [Test]
    public void TemplatePackagesAreGone()
    {
        var manifest = File.ReadAllText("Packages/manifest.json");
        foreach (var p in new[] { "com.unity.ai.navigation", "com.unity.collab-proxy", "com.unity.multiplayer.center",
                                  "com.unity.visualscripting", "com.unity.timeline", "com.unity.ugui" })
            StringAssert.DoesNotContain("\"" + p + "\"", manifest);
        Assert.IsFalse(File.Exists("Assets/Scenes/SampleScene.unity"));
    }

    /// <summary>Task 24 review M9: Debug.Log lines (GAME OK, TURN, PROFILE) print without a script stack trace in players;
    /// warnings, errors and exceptions keep theirs.</summary>
    [Test]
    public void LogLinesCarryNoStackTraceButErrorsDo()
    {
        Assert.AreEqual(UnityEngine.StackTraceLogType.None, PlayerSettings.GetStackTraceLogType(UnityEngine.LogType.Log));
        foreach (var t in new[] { UnityEngine.LogType.Error, UnityEngine.LogType.Assert, UnityEngine.LogType.Warning, UnityEngine.LogType.Exception })
            Assert.AreEqual(UnityEngine.StackTraceLogType.ScriptOnly, PlayerSettings.GetStackTraceLogType(t), t.ToString());
    }
}
