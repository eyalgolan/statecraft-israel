#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Statecraft.Tests.EditMode
{
    /// <summary>Unity-MCP (com.ivanmurzak.unity.mcp) is held to local Custom mode with token auth in every editor; in batch
    /// mode (tests and builds) it must not connect anywhere, start its local server or open a window; and no part of it may
    /// ship in a player build.</summary>
    public sealed class UnityMcpGuardTests
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        static Type Guard()
        {
            var t = Type.GetType("Statecraft.Game.EditorTools.UnityMcpGuard, Assembly-CSharp-Editor");
            Assert.IsNotNull(t, "the UnityMcpGuard editor hook exists");
            return t!;
        }

        static string[] Filter(params string[] assemblies) =>
            (string[])Guard().GetMethod("Filter", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { assemblies })!;

        static Type PluginEditor()
        {
            var editor = (Type?)Guard().GetMethod("LoadedPluginEditorType", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            Assert.IsNotNull(editor,
                "com.ivanmurzak.unity.mcp is a direct dependency in Packages/manifest.json; its editor assembly must be loaded for UnityMcpGuard integration tests");
            return editor!;
        }

        static string Status() => (string)Guard().GetField("Status", Any)!.GetValue(null)!;

        /// <summary>The guard's reading of the plugin's own knob in this process (real args and env).</summary>
        static string? Knob(string envKey, string? alias, Type enumType) =>
            (string?)Guard().GetMethod("ExplicitKnob", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object?[] { envKey, alias, enumType });

        /// <summary>The pure rule, with given args and env: the plugin's precedence (flag alias, then the -KEY arg, then the
        /// env var; blank values skipped) and its parse (trimmed, quotes stripped, enum name case-insensitive).</summary>
        static string? KnobValue(string envKey, string? alias, Type enumType, Dictionary<string, string?> args, Dictionary<string, string?> env) =>
            (string?)Guard().GetMethod("KnobValue", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object?[]
            {
                envKey, alias, enumType,
                new Func<string, string?>(k => args.TryGetValue(k, out var v) ? v : null),
                new Func<string, string?>(k => env.TryGetValue(k, out var v) ? v : null),
            });

        enum Mode { Custom, Cloud }
        enum Auth { none, token, oauth }

        static Dictionary<string, string?> D(params (string k, string? v)[] kv) => kv.ToDictionary(x => x.k, x => x.v);

        [Test]
        public void TheOptOutMatchesThePluginsOwnKnob()
        {
            // env var only, any case of the value
            Assert.AreEqual("Cloud", KnobValue("UNITY_MCP_CONNECTION_MODE", null, typeof(Mode), D(), D(("UNITY_MCP_CONNECTION_MODE", " \"cloud\" "))));
            // an invalid value is no opt-out: the plugin's TryParse fails and keeps the disk value, so the guard still holds
            Assert.IsNull(KnobValue("UNITY_MCP_CONNECTION_MODE", null, typeof(Mode), D(), D(("UNITY_MCP_CONNECTION_MODE", "Clowd"))));
            // a -KEY argument beats the env var, even when the argument is invalid (the plugin stops at the first non-blank)
            Assert.AreEqual("Custom", KnobValue("UNITY_MCP_CONNECTION_MODE", null, typeof(Mode), D(("UNITY_MCP_CONNECTION_MODE", "Custom")), D(("UNITY_MCP_CONNECTION_MODE", "Cloud"))));
            Assert.IsNull(KnobValue("UNITY_MCP_CONNECTION_MODE", null, typeof(Mode), D(("UNITY_MCP_CONNECTION_MODE", "Clowd")), D(("UNITY_MCP_CONNECTION_MODE", "Cloud"))));
            // a blank argument is skipped
            Assert.AreEqual("Cloud", KnobValue("UNITY_MCP_CONNECTION_MODE", null, typeof(Mode), D(("UNITY_MCP_CONNECTION_MODE", " ")), D(("UNITY_MCP_CONNECTION_MODE", "Cloud"))));
            // the -auth alias beats -UNITY_MCP_AUTH_OPTION and the env var
            Assert.AreEqual("none", KnobValue("UNITY_MCP_AUTH_OPTION", "auth", typeof(Auth), D(("auth", "None"), ("UNITY_MCP_AUTH_OPTION", "token")), D(("UNITY_MCP_AUTH_OPTION", "oauth"))));
            Assert.AreEqual("oauth", KnobValue("UNITY_MCP_AUTH_OPTION", "auth", typeof(Auth), D(), D(("UNITY_MCP_AUTH_OPTION", "OAUTH"))));
            Assert.IsNull(KnobValue("UNITY_MCP_AUTH_OPTION", "auth", typeof(Auth), D(), D()));
        }

        [Test]
        public void TheGuardReadsArgumentsThroughThePluginsOwnParser()
        {
            PluginEditor();
            // Same dictionary as the plugin (its key comparer included), so -unity_mcp_auth_option or -AUTH match exactly
            // when, and only when, they match for the plugin.
            Assert.AreEqual("plugin ArgsUtils", Guard().GetField("ArgSource", Any)!.GetValue(null));
        }

        [Test]
        public void PlayerBuildsDropThePluginAndTheDllsItsResolverDownloaded()
        {
            var kept = Filter(
                "Library/Bee/PlayerScriptAssemblies/Statecraft.Game.dll",
                "Library/Bee/PlayerScriptAssemblies/com.IvanMurzak.Unity.MCP.Runtime.dll",
                "Library/Bee/PlayerScriptAssemblies/com.IvanMurzak.Unity.MCP.TestFiles.dll",
                "Library/Bee/PlayerScriptAssemblies/Extensions.Unity.PlayerPrefsEx.dll",
                "/abs/unity/Assets/Plugins/NuGet/McpPlugin.dll",
                @"C:\abs\unity\Assets\Plugins\NuGet\System.Text.Json.dll",
                "Assets/Plugins/Statecraft/Statecraft.Sim.dll",
                "Assets/Plugins/Statecraft/YamlDotNet.dll",
                "/Applications/Unity/Managed/UnityEngine/UnityEngine.CoreModule.dll");
            CollectionAssert.AreEqual(new[]
            {
                "Library/Bee/PlayerScriptAssemblies/Statecraft.Game.dll",
                "Assets/Plugins/Statecraft/Statecraft.Sim.dll",
                "Assets/Plugins/Statecraft/YamlDotNet.dll",
                "/Applications/Unity/Managed/UnityEngine/UnityEngine.CoreModule.dll",
            }, kept);
        }

        [Test]
        public void PlayerBuildsDropEveryRealPluginAssemblyAndDll()
        {
            PluginEditor();
            // Every player assembly compiled from the plugin's packages, and every managed plugin DLL a WebGL build would
            // take that is not ours, must be dropped by the filter (by its real output name and asset path).
            var fromPackages = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => a.sourceFiles.Any(f => f.StartsWith("Packages/com.ivanmurzak.unity.mcp/", StringComparison.Ordinal)
                                                 || f.StartsWith("Packages/extensions.unity.playerprefsex/", StringComparison.Ordinal)))
                .Select(a => "Library/Bee/PlayerScriptAssemblies/" + System.IO.Path.GetFileName(a.outputPath))
                .ToList();
            CollectionAssert.IsNotEmpty(fromPackages, "the plugin compiles player assemblies");
            bool WebGl(PluginImporter p) => !p.isNativePlugin && p.assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && (p.GetCompatibleWithPlatform(BuildTarget.WebGL) || (p.GetCompatibleWithAnyPlatform() && !p.GetExcludeFromAnyPlatform(BuildTarget.WebGL)));
            var importers = PluginImporter.GetAllImporters().Where(WebGl).Select(p => p.assetPath).ToList();
            // The resolver's install folder (NuGetConfig.InstallPath). If upstream moves it, this list empties and fails.
            var dlls = importers.Where(a => a.StartsWith("Assets/Plugins/NuGet/", StringComparison.Ordinal)).ToList();
            CollectionAssert.IsNotEmpty(dlls, "the resolver's runtime DLLs in Assets/Plugins/NuGet are WebGL-compatible");
            // Any other managed DLL a WebGL build would take must be ours or explicitly allowed; a new one is either the
            // plugin's (the filter must drop it) or a deliberate third-party addition (add it here, with its provenance).
            var allowed = new[] { "Assets/Plugins/Statecraft/" };
            var unexpected = importers.Where(a => a.StartsWith("Assets/", StringComparison.Ordinal)
                                                  && !a.StartsWith("Assets/Plugins/NuGet/", StringComparison.Ordinal)
                                                  && !allowed.Any(x => a.StartsWith(x, StringComparison.Ordinal))).ToList();
            CollectionAssert.IsEmpty(unexpected, "a managed DLL outside Assets/Plugins/NuGet and the allow-list would ship in WebGL builds");
            var all = fromPackages.Concat(dlls).ToArray();
            CollectionAssert.IsEmpty(Filter(all), "kept by the player-build filter");
        }

        [Test]
        public void ThePluginIsHeldToLocalCustomModeWithTokenAuth()
        {
            var editor = PluginEditor();
            Assert.AreEqual(Application.isBatchMode ? "held offline" : "held local", Status());
            var instance = editor.GetProperty("Instance", Any)!.GetValue(null);
            var config = editor.GetField("unityConnectionConfig", Any)!.GetValue(instance)!;
            object Get(string name) => config.GetType().GetProperty(name)!.GetValue(config)!;
            // The plugin's own UNITY_MCP_CONNECTION_MODE / UNITY_MCP_AUTH_OPTION knobs are an explicit opt-out the guard respects.
            var modeKnob = Knob("UNITY_MCP_CONNECTION_MODE", null, Get("ConnectionMode").GetType());
            var authKnob = Knob("UNITY_MCP_AUTH_OPTION", "auth", Get("AuthOption").GetType());
            Assert.AreEqual(modeKnob ?? "Custom", Get("ConnectionMode").ToString(), "never the plugin's cloud service, unless its own knob says so");
            Assert.AreEqual(authKnob ?? "token", Get("AuthOption").ToString(), "the local server checks a token, unless the plugin's own knob says otherwise");
            // Recorded as runtime overrides, so the plugin never writes them into the user's own settings file.
            var record = editor.GetProperty("RuntimeOverrides", Any)!.GetValue(instance)!;
            bool Tracked(string name) => (bool)record.GetType().GetMethod("Contains")!.Invoke(record, new object[] { name })!;
            if (modeKnob == null) Assert.IsTrue(Tracked("ConnectionMode"));
            if (authKnob == null) Assert.IsTrue(Tracked("AuthOption"));
            if (!Application.isBatchMode) return;   // the interactive editor keeps the user's connect and server choices
            Assert.IsFalse((bool)Get("KeepConnected"), "no connection");
            Assert.IsFalse((bool)Get("KeepServerRunning"), "no local server");
            Assert.IsTrue(Tracked("KeepConnected") && Tracked("KeepServerRunning"));
        }

        [Test]
        public void InBatchModeThePluginOpensNoWindowStartsNoServerAndSkipsItsUpdateCheck()
        {
            var editor = PluginEditor();
            if (!Application.isBatchMode) { Assert.AreEqual("held local", Status()); return; }
            // The first-run window opened on screen in a batch test run (2026-10-10) and its server panel started the
            // local server; the update popup is the same kind of window.
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Assembly == editor.Assembly);
            CollectionAssert.IsEmpty(windows.Select(w => w.GetType().FullName), "no plugin window in batch mode");
            var server = editor.Assembly.GetType("com.IvanMurzak.Unity.MCP.Editor.McpServerManager")!;
            Assert.IsFalse((bool)server.GetProperty("IsRunning")!.GetValue(null)!, "no local server process");
            Assert.IsFalse((bool)server.GetProperty("IsStarting")!.GetValue(null)!, "no local server starting");
            var unhooked = (IReadOnlyCollection<string>)Guard().GetField("Unhooked", Any)!.GetValue(null)!;
            CollectionAssert.IsSupersetOf(unhooked, new[] { "MainWindowInitializer.PerformInitialization", "UpdateChecker.CheckForUpdatesOnStartup" });
        }

        [Test]
        public void ABatchHoldStillAppliesWhenTheCustomOrTokenOverrideFails()
        {
            var editor = PluginEditor();
            if (!Application.isBatchMode) { Assert.AreEqual("held local", Status()); return; }
            var instance = editor.GetProperty("Instance", Any)!.GetValue(null);
            var config = editor.GetField("unityConnectionConfig", Any)!.GetValue(instance)!;
            var keep = config.GetType().GetProperty("KeepConnected")!;
            var server = config.GetType().GetProperty("KeepServerRunning")!;
            keep.SetValue(config, true);
            server.SetValue(config, true);
            try
            {
                // A plugin bump that renames ConnectionMode/AuthOption must not take the batch protections down with it.
                var status = (string)Guard().GetMethod("Hold", BindingFlags.Public | BindingFlags.Static)!
                    .Invoke(null, new object?[] { true, "simulated override failure", null })!;
                StringAssert.Contains("simulated override failure", status);
                Assert.IsFalse((bool)keep.GetValue(config)!, "no connection even when the Custom/token override failed");
                Assert.IsFalse((bool)server.GetValue(config)!, "no local server even when the Custom/token override failed");
            }
            finally
            {
                keep.SetValue(config, false);
                server.SetValue(config, false);
                Guard().GetMethod("Hold", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object?[] { true, null, null });
            }
        }

        [Test]
        public void EveryBatchProtectionStillAppliesWhenAnEarlierStepThrows()
        {
            var editor = PluginEditor();
            if (!Application.isBatchMode) { Assert.AreEqual("held local", Status()); return; }
            var instance = editor.GetProperty("Instance", Any)!.GetValue(null);
            var config = editor.GetField("unityConnectionConfig", Any)!.GetValue(instance)!;
            var keep = config.GetType().GetProperty("KeepConnected")!;
            var server = config.GetType().GetProperty("KeepServerRunning")!;
            var unhooked = (List<string>)Guard().GetField("Unhooked", Any)!.GetValue(null)!;
            var hold = Guard().GetMethod("Hold", BindingFlags.Public | BindingFlags.Static)!;
            try
            {
                foreach (var step in new[] { "KeepConnected", "MainWindowInitializer" })
                {
                    keep.SetValue(config, true);
                    server.SetValue(config, true);
                    unhooked.Clear();
                    var status = (string)hold.Invoke(null, new object?[] { true, null, step })!;
                    StringAssert.Contains("simulated throw in " + step, status, step);
                    if (step != "KeepConnected") Assert.IsFalse((bool)keep.GetValue(config)!, step + ": KeepConnected still forced off");
                    Assert.IsFalse((bool)server.GetValue(config)!, step + ": KeepServerRunning still forced off");
                    CollectionAssert.Contains(unhooked, "UpdateChecker.CheckForUpdatesOnStartup", step + ": the update check is still unhooked");
                    if (step != "MainWindowInitializer")
                        CollectionAssert.Contains(unhooked, "MainWindowInitializer.PerformInitialization", step + ": the first-run window is still unhooked");
                    var mode = config.GetType().GetProperty("ConnectionMode")!;
                    Assert.AreEqual(Knob("UNITY_MCP_CONNECTION_MODE", null, mode.PropertyType) ?? "Custom", mode.GetValue(config)!.ToString(), step + ": still held to Custom");
                }
            }
            finally
            {
                keep.SetValue(config, false);
                server.SetValue(config, false);
                hold.Invoke(null, new object?[] { true, null, null });
            }
        }

        [Test]
        public void SavingTheConfigNeverWritesTheOverrides()
        {
            var editor = PluginEditor();
            var instance = editor.GetProperty("Instance", Any)!.GetValue(null)!;
            var config = editor.GetField("unityConnectionConfig", Any)!.GetValue(instance)!;
            var record = editor.GetProperty("RuntimeOverrides", Any)!.GetValue(instance)!;
            var baselines = (System.Collections.IDictionary)record.GetType().GetProperty("BaselineValues")!.GetValue(record)!;
            var path = (string)editor.GetProperty("AssetsFileAbsolutePath", Any)!.GetValue(null)!;
            // The test writes through the plugin's real Save, so it snapshots the user's file (or its absence) and puts it
            // back byte for byte afterwards, also when an assertion fails.
            var before = System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;
            try
            {
                // The plugin's own Save: it writes the baselines and keeps the overrides in memory.
                instance.GetType().GetMethod("Save", Any)!.Invoke(instance, new object[] { false });
                var json = System.IO.File.ReadAllText(path);
                string OnDisk(string key) => System.Text.RegularExpressions.Regex.Match(json, "\"" + key + "\":\\s*\"(\\w+)\"").Groups[1].Value;
                foreach (var (field, key) in new[] { ("ConnectionMode", "connectionMode"), ("AuthOption", "authOption") })
                {
                    if (!baselines.Contains(field)) continue;   // the plugin's own knob is in charge of this field
                    Assert.AreEqual(baselines[field]!.ToString(), OnDisk(key), field + " on disk is the user's own value");
                }
            }
            finally
            {
                if (before == null) System.IO.File.Delete(path);
                else System.IO.File.WriteAllBytes(path, before);
            }
            if (before == null) Assert.IsFalse(System.IO.File.Exists(path), "no settings file is left behind");
            else CollectionAssert.AreEqual(before, System.IO.File.ReadAllBytes(path), "the user's settings file is restored byte-identical");
            var modeKnob = Knob("UNITY_MCP_CONNECTION_MODE", null, config.GetType().GetProperty("ConnectionMode")!.PropertyType);
            Assert.AreEqual(modeKnob ?? "Custom", config.GetType().GetProperty("ConnectionMode")!.GetValue(config)!.ToString(), "still held in memory after Save");
        }

        [Test]
        public void TheTeamKillSwitchForTheUpdatePopupIsOn()
        {
            var editor = PluginEditor();
            var checker = editor.Assembly.GetType("com.IvanMurzak.Unity.MCP.Editor.Utils.UpdateChecker")!;
            Assert.IsTrue((bool)checker.GetProperty("IsDisabledForProject")!.GetValue(null)!,
                "ProjectSettings/AI-Game-Developer-UpdateSettings.asset turns the update popup off for the team");
        }
    }
}
