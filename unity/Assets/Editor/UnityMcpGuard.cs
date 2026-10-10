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

        /// <summary>True when the plugin's own knob for this field is set, as an env var or a -NAME command-line argument.</summary>
        static bool ExplicitKnob(string name) =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
            || Environment.GetCommandLineArgs().Any(a => a.TrimStart('-').Split('=')[0] == name);

        static string Hold(bool batch)
        {
            var editor = LoadedPluginEditorType();
            if (editor == null) return "plugin not loaded";
            try
            {
                var instance = editor.GetProperty("Instance", Any)?.GetValue(null);
                var config = editor.GetField("unityConnectionConfig", Any)?.GetValue(instance);
                var record = editor.GetProperty("RuntimeOverrides", Any)?.GetValue(instance);
                var track = record?.GetType().GetMethod("Track", Any);
                var baselines = record?.GetType().GetProperty("BaselineValues")?.GetValue(record) as System.Collections.IDictionary;
                if (config == null || record == null || track == null) return "plugin internals changed";

                string? Override(string name, Func<Type, object> value)
                {
                    var p = config.GetType().GetProperty(name);
                    if (p == null) return "plugin internals changed: no " + name;
                    var target = value(p.PropertyType);
                    // Keep the on-disk value as the baseline the plugin saves, even if an env var already overrode it.
                    var baseline = baselines != null && baselines.Contains(name) ? baselines[name] : p.GetValue(config);
                    track.Invoke(record, new[] { name, baseline, target });
                    p.SetValue(config, target);
                    return null;
                }

                var failed = (ExplicitKnob("UNITY_MCP_CONNECTION_MODE") ? null : Override("ConnectionMode", t => Enum.Parse(t, "Custom")))
                          ?? (ExplicitKnob("UNITY_MCP_AUTH_OPTION") ? null : Override("AuthOption", t => Enum.Parse(t, "token")));
                if (failed != null) return failed;
                if (!batch) return "held local";

                failed = Override("KeepConnected", _ => false) ?? Override("KeepServerRunning", _ => false);
                if (failed != null) return failed;
                // Run each static constructor first (it queues the callback), or the unhook could come before the queueing.
                failed = Unhook(editor.Assembly, "com.IvanMurzak.Unity.MCP.Editor.UI.MainWindowInitializer", "PerformInitialization", runCctorOf: null, update: true)
                      ?? Unhook(editor.Assembly, "com.IvanMurzak.Unity.MCP.Editor.Utils.UpdateChecker", "CheckForUpdatesOnStartup",
                                runCctorOf: "com.IvanMurzak.Unity.MCP.Editor.Startup", update: false);
                if (failed != null) return failed;
                foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Assembly == editor.Assembly).ToList())
                    w.Close();
                return "held offline";
            }
            catch (Exception e)
            {
                return e.GetType().Name + ": " + e.Message;
            }
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
