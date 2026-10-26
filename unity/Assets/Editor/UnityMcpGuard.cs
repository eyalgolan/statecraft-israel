#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Holds Unity-MCP (com.ivanmurzak.unity.mcp, the AI-agent bridge; CONTRIBUTING.md) to this project's policy.
    /// - Every editor: ConnectionMode Custom (the plugin's default is Cloud, an anonymous connection to ai-game.dev on the
    ///   first open) and AuthOption token (the local server checks a Bearer token; Configure writes it into the agent's
    ///   config). Both are recorded as the plugin's own runtime overrides, so they never reach the user's
    ///   UserSettings/AI-Game-Developer-Config.json, and choosing Cloud or another auth in the window snaps back on save.
    ///   The plugin's explicit knobs UNITY_MCP_CONNECTION_MODE and UNITY_MCP_AUTH_OPTION (env var or -arg) are respected.
    ///   The interactive editor keeps the user's Server URL, Connect and Keep-server-running choices.
    /// - Batch mode (tests, builds): also KeepConnected and KeepServerRunning false (no connection, no local server), and
    ///   no plugin window or update check: its first-run window and update popup open on screen even in batch mode, and
    ///   the first-run window's server panel started the local server in a test run.
    /// - Player builds: drops the plugin's runtime assemblies and the NuGet DLLs its resolver put in Assets/Plugins/NuGet
    ///   (IFilterBuildAssemblies). The game never calls them; the plugin offers no switch of its own for this.
    /// Reflection, not a reference: on a fresh clone the plugin compiles only after its resolver has fetched its NuGet DLLs.
    /// The plugin also keeps two keys in this project's editor PlayerPrefs (Unity-MCP.MainWindow.Initialized,
    /// Unity_MCP_SelectedAiAgent); a PlayerPrefs.DeleteAll() in editor code would bring its first-run window back.</summary>
    [InitializeOnLoad]
    public sealed class UnityMcpGuard : IFilterBuildAssemblies
    {
        const string PluginEditorAssembly = "com.IvanMurzak.Unity.MCP.Editor";
        const string PluginEditorType = "com.IvanMurzak.Unity.MCP.UnityMcpPluginEditor";
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        /// <summary>"held local" (interactive editor), "held offline" (batch mode), "plugin not loaded", or why it failed.</summary>
        public static string Status = "not run";

        /// <summary>The plugin callbacks taken off the editor loop in batch mode, as "Type.Method".</summary>
        public static readonly List<string> Unhooked = new List<string>();

        static UnityMcpGuard()
        {
            Status = Hold(Application.isBatchMode);
            if (Status != "held offline" && Status != "held local" && Status != "plugin not loaded")
                Debug.LogWarning("UnityMcpGuard: could not hold Unity-MCP to local Custom mode" + (Application.isBatchMode ? " and offline" : "") + ": " + Status);
        }

        /// <summary>The plugin's editor type if its assembly is loaded in this domain, else null. Never Type.GetType: that
        /// would load a just-compiled plugin assembly on demand into a domain where its [InitializeOnLoad] never ran (the
        /// first batch run of a fresh clone compiles the plugin after its resolver, then runs the tests without a reload).</summary>
        public static Type? LoadedPluginEditorType() =>
            AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == PluginEditorAssembly)?.GetType(PluginEditorType);

        /// <summary>The warm-up launch before batch tests (CONTRIBUTING.md). The plugin compiles only when its gate defines
        /// (UNITY_MCP_READY) are in ProjectSettings when Unity starts. Its resolver fetches the DLLs and adds the defines on
        /// the first editor update, after -executeMethod, so a -quit launch would exit before it ran, and a test run that
        /// adds them starts its tests without a reload. This waits one editor update (the resolver's runs first: it
        /// subscribed at load), saves the defines and exits, 1 when they are still missing. No -quit:
        /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath unity -executeMethod Statecraft.Game.EditorTools.UnityMcpGuard.WarmUp</summary>
        public static void WarmUp()
        {
            EditorApplication.update += Exit;
            static void Exit()
            {
                EditorApplication.update -= Exit;
                AssetDatabase.SaveAssets();
                var ready = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Split(';').Contains("UNITY_MCP_READY");
                Debug.Log("UnityMcpGuard warm-up: gate defines " + (ready ? "in place" : "missing"));
                EditorApplication.Exit(ready ? 0 : 1);
            }
        }

        /// <summary>Where the guard reads command-line arguments from: "plugin ArgsUtils" (the plugin's own parser and key
        /// comparer, so an argument counts for the guard exactly when it counts for the plugin) or "fallback parser".</summary>
        public static string ArgSource = "not read";

        /// <summary>The plugin's own value for an opt-out knob, or null when there is none: the first non-blank of the flag
        /// alias argument (-auth), the -KEY argument and the KEY env var, trimmed with quotes stripped, and only if it parses
        /// as an enum name (case-insensitive). This mirrors EnvironmentUtils.ApplyEnvironmentOverrides: an invalid first
        /// value overrides nothing there, so it is no opt-out here either.</summary>
        public static string? KnobValue(string envKey, string? flagAlias, Type enumType, Func<string, string?> arg, Func<string, string?> env)
        {
            string? raw = null;
            if (flagAlias != null && !string.IsNullOrWhiteSpace(arg(flagAlias))) raw = arg(flagAlias);
            else if (!string.IsNullOrWhiteSpace(arg(envKey))) raw = arg(envKey);
            else if (!string.IsNullOrWhiteSpace(env(envKey))) raw = env(envKey);
            if (raw == null) return null;
            var value = raw.Trim().Trim('"');
            var name = Enum.GetNames(enumType).FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
            return name;
        }

        /// <summary>KnobValue for this process: arguments through the plugin's ArgsUtils.ParseCommandLineArguments.</summary>
        public static string? ExplicitKnob(string envKey, string? flagAlias, Type enumType)
        {
            var args = PluginArgs();
            return KnobValue(envKey, flagAlias, enumType, k => args.Contains(k) ? args[k] as string : null, Environment.GetEnvironmentVariable);
        }

        static System.Collections.IDictionary PluginArgs()
        {
            var utils = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("com.IvanMurzak.McpPlugin.Common.Utils.ArgsUtils")).FirstOrDefault(t => t != null);
            if (utils?.GetMethod("ParseCommandLineArguments", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)?
                    .Invoke(null, null) is System.Collections.IDictionary parsed)
            {
                ArgSource = "plugin ArgsUtils";
                return parsed;
            }
            // Same shape as the plugin's resolver parser: -key value, -key=value, keys case-insensitive.
            ArgSource = "fallback parser";
            var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var argv = Environment.GetCommandLineArgs();
            for (var i = 0; i < argv.Length; i++)
            {
                if (!argv[i].StartsWith("-")) continue;
                var key = argv[i].TrimStart('-');
                var eq = key.IndexOf('=');
                if (eq >= 0) { d[key.Substring(0, eq)] = key.Substring(eq + 1); continue; }
                d[key] = i + 1 < argv.Length && !argv[i + 1].StartsWith("-") ? argv[++i] : null;
            }
            return d;
        }

        /// <summary>Applies the hold and returns the status. Every step runs on its own (a throw or a "plugin internals
        /// changed" result in one step never skips a later one) and the failures are joined. In batch mode the protections
        /// (no connection, no server, no window, no update check) come before the Custom/token override. The two simulate
        /// parameters exist for the tests only.</summary>
        public static string Hold(bool batch, string? simulateOverrideFailure = null, string? simulateThrowInStep = null)
        {
            var editor = LoadedPluginEditorType();
            if (editor == null) return "plugin not loaded";
            var failures = new List<string>();

            void Step(string name, Func<string?> body)
            {
                try
                {
                    if (name == simulateThrowInStep) throw new InvalidOperationException("simulated throw in " + name);
                    var f = body();
                    if (f != null) failures.Add(f);
                }
                catch (Exception e)
                {
                    var inner = e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e;
                    failures.Add($"{name}: {inner.GetType().Name}: {inner.Message}");
                }
            }

            object? instance = null, config = null, record = null;
            MethodInfo? track = null;
            System.Collections.IDictionary? baselines = null;
            Step("plugin config", () =>
            {
                instance = editor.GetProperty("Instance", Any)?.GetValue(null);
                config = editor.GetField("unityConnectionConfig", Any)?.GetValue(instance);
                record = editor.GetProperty("RuntimeOverrides", Any)?.GetValue(instance);
                track = record?.GetType().GetMethod("Track", Any);
                baselines = record?.GetType().GetProperty("BaselineValues")?.GetValue(record) as System.Collections.IDictionary;
                return config == null || record == null || track == null ? "plugin internals changed: config or RuntimeOverrides" : null;
            });

            string? Override(string name, Func<Type, object> value)
            {
                if (config == null || record == null || track == null) return "plugin internals changed: cannot override " + name;
                var p = config.GetType().GetProperty(name);
                if (p == null) return "plugin internals changed: no " + name;
                var target = value(p.PropertyType);
                // Keep the on-disk value as the baseline the plugin saves, even if an env var already overrode it.
                var baseline = baselines != null && baselines.Contains(name) ? baselines[name] : p.GetValue(config);
                track.Invoke(record, new[] { name, baseline, target });
                p.SetValue(config, target);
                return null;
            }

            if (batch)
            {
                Step("KeepConnected", () => Override("KeepConnected", _ => false));
                Step("KeepServerRunning", () => Override("KeepServerRunning", _ => false));
                // Run each static constructor first (it queues the callback), or the unhook could come before the queueing.
                Step("MainWindowInitializer", () => Unhook(editor.Assembly, "com.IvanMurzak.Unity.MCP.Editor.UI.MainWindowInitializer",
                                                            "PerformInitialization", runCctorOf: null, update: true));
                Step("UpdateChecker", () => Unhook(editor.Assembly, "com.IvanMurzak.Unity.MCP.Editor.Utils.UpdateChecker",
                                                    "CheckForUpdatesOnStartup", runCctorOf: "com.IvanMurzak.Unity.MCP.Editor.Startup", update: false));
                Step("plugin windows", () =>
                {
                    foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Assembly == editor.Assembly).ToList())
                        w.Close();
                    return null;
                });
            }
            if (simulateOverrideFailure != null) failures.Add(simulateOverrideFailure);
            else
            {
                foreach (var (name, knob, alias, value) in new[] { ("ConnectionMode", "UNITY_MCP_CONNECTION_MODE", (string?)null, "Custom"),
                                                                    ("AuthOption", "UNITY_MCP_AUTH_OPTION", "auth", "token") })
                    Step(name, () =>
                    {
                        var type = config?.GetType().GetProperty(name)?.PropertyType;
                        if (type == null) return "plugin internals changed: no " + name;
                        return ExplicitKnob(knob, alias, type) != null ? null : Override(name, t => Enum.Parse(t, value));   // the plugin's own opt-out is respected
                    });
            }
            if (failures.Count > 0) return string.Join("; ", failures);
            return batch ? "held offline" : "held local";
        }

        /// <summary>Takes a plugin callback off EditorApplication.update (or delayCall), after running the static
        /// constructor that queues it (the type's own, or runCctorOf's).</summary>
        static string? Unhook(Assembly plugin, string typeName, string method, string? runCctorOf, bool update)
        {
            var type = plugin.GetType(typeName);
            var m = type?.GetMethod(method, Any);
            var queuer = runCctorOf == null ? type : plugin.GetType(runCctorOf);
            if (m == null || queuer == null) return $"plugin internals changed: no {typeName}.{method}";
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(queuer.TypeHandle);
            var d = (EditorApplication.CallbackFunction)Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), m);
            if (update) EditorApplication.update -= d; else EditorApplication.delayCall -= d;
            Unhooked.Add(type!.Name + "." + method);
            return null;
        }

        /// <summary>True for the plugin's own assemblies, its PlayerPrefsEx dependency, and any DLL under
        /// Assets/Plugins/NuGet (the folder the plugin's resolver owns).</summary>
        public static bool IsPluginAssembly(string path)
        {
            var p = path.Replace('\\', '/');
            var file = p.Substring(p.LastIndexOf('/') + 1);
            return p.Contains("/Plugins/NuGet/") || p.StartsWith("Assets/Plugins/NuGet/", StringComparison.Ordinal)
                || file.StartsWith("com.IvanMurzak.", StringComparison.Ordinal)
                || file.StartsWith("Extensions.Unity.PlayerPrefsEx", StringComparison.Ordinal);
        }

        public static string[] Filter(string[] assemblies) => assemblies.Where(a => !IsPluginAssembly(a)).ToArray();

        public int callbackOrder => 0;

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            var kept = Filter(assemblies);
            Debug.Log($"UnityMcpGuard: player build keeps {kept.Length} of {assemblies.Length} assemblies; dropped: " +
                      string.Join(", ", assemblies.Except(kept).Select(a => a.Substring(a.Replace('\\', '/').LastIndexOf('/') + 1))));
            return kept;
        }
    }
}
