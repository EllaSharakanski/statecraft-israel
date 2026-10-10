#nullable enable
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Keeps Unity-MCP (com.ivanmurzak.unity.mcp, the AI-agent bridge; CONTRIBUTING.md) to the interactive editor.
    /// - Batch mode (tests, builds): the plugin would otherwise connect on every domain reload (by default to its cloud
    ///   service, whose refusal is logged as an error and fails whichever test is running) or start its local server.
    ///   This sets KeepConnected and KeepServerRunning to false for this process only, recorded as the plugin's own
    ///   runtime overrides so they never reach the user's UserSettings/AI-Game-Developer-Config.json. Reflection, not a
    ///   reference: on a fresh clone the plugin compiles only after its resolver has fetched its NuGet DLLs.
    /// - Player builds: drops the plugin's runtime assemblies and the NuGet DLLs its resolver put in Assets/Plugins/NuGet
    ///   (IFilterBuildAssemblies). The game never calls them; the plugin offers no switch of its own for this.</summary>
    [InitializeOnLoad]
    public sealed class UnityMcpGuard : IFilterBuildAssemblies
    {
        const string PluginEditorType = "com.IvanMurzak.Unity.MCP.UnityMcpPluginEditor, com.IvanMurzak.Unity.MCP.Editor";
        static readonly string[] Overrides = { "KeepConnected", "KeepServerRunning" };
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        /// <summary>What the batch-mode hold did: "held offline", "interactive editor", "plugin not compiled" or why it failed.</summary>
        public static string Status = "not run";

        static UnityMcpGuard()
        {
            Status = Application.isBatchMode ? HoldOffline() : "interactive editor";
            if (Status != "held offline" && Status != "interactive editor" && Status != "plugin not compiled")
                Debug.LogWarning("UnityMcpGuard: could not hold Unity-MCP offline in batch mode: " + Status);
        }

        static string HoldOffline()
        {
            var editor = Type.GetType(PluginEditorType);
            if (editor == null) return "plugin not compiled";
            try
            {
                var instance = editor.GetProperty("Instance", Any)?.GetValue(null);
                var config = editor.GetField("unityConnectionConfig", Any)?.GetValue(instance);
                var record = editor.GetProperty("RuntimeOverrides", Any)?.GetValue(instance);
                var track = record?.GetType().GetMethod("Track", Any);
                var baselines = record?.GetType().GetProperty("BaselineValues")?.GetValue(record) as System.Collections.IDictionary;
                if (config == null || record == null || track == null) return "plugin internals changed";
                foreach (var name in Overrides)
                {
                    var p = config.GetType().GetProperty(name);
                    if (p == null) return "plugin internals changed: no " + name;
                    // Keep the on-disk value as the baseline the plugin saves, even if an env var already overrode it.
                    var baseline = baselines != null && baselines.Contains(name) ? baselines[name] : p.GetValue(config);
                    track.Invoke(record, new[] { name, baseline, (object)false });
                    p.SetValue(config, false);
                }
                return "held offline";
            }
            catch (Exception e)
            {
                return e.GetType().Name + ": " + e.Message;
            }
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
