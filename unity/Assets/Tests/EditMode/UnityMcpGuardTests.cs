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

        static Type? PluginEditor() =>
            (Type?)Guard().GetMethod("LoadedPluginEditorType", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);

        static string Status() => (string)Guard().GetField("Status", Any)!.GetValue(null)!;

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
            if (PluginEditor() == null) { Assert.AreEqual("plugin not loaded", Status()); return; }
            // Every player assembly compiled from the plugin's packages, and every managed plugin DLL a WebGL build would
            // take that is not ours, must be dropped by the filter (by its real output name and asset path).
            var fromPackages = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => a.sourceFiles.Any(f => f.StartsWith("Packages/com.ivanmurzak.unity.mcp/", StringComparison.Ordinal)
                                                 || f.StartsWith("Packages/extensions.unity.playerprefsex/", StringComparison.Ordinal)))
                .Select(a => "Library/Bee/PlayerScriptAssemblies/" + System.IO.Path.GetFileName(a.outputPath))
                .ToList();
            CollectionAssert.IsNotEmpty(fromPackages, "the plugin compiles player assemblies");
            var dlls = PluginImporter.GetAllImporters()
                .Where(p => !p.isNativePlugin && p.assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                            && (p.GetCompatibleWithPlatform(BuildTarget.WebGL)
                                || (p.GetCompatibleWithAnyPlatform() && !p.GetExcludeFromAnyPlatform(BuildTarget.WebGL)))
                            && p.assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                            && !p.assetPath.StartsWith("Assets/Plugins/Statecraft/", StringComparison.Ordinal))
                .Select(p => p.assetPath)
                .ToList();
            CollectionAssert.IsNotEmpty(dlls, "the resolver's runtime DLLs are WebGL-compatible");
            var all = fromPackages.Concat(dlls).ToArray();
            CollectionAssert.IsEmpty(Filter(all), "kept by the player-build filter");
        }

        [Test]
        public void ThePluginIsHeldToLocalCustomModeWithTokenAuth()
        {
            var editor = PluginEditor();
#if UNITY_MCP_READY
            // The gate defines are in place, so the plugin compiled and must be loaded; a renamed type or assembly would
            // otherwise turn every check below into a silent pass.
            Assert.IsNotNull(editor, "UNITY_MCP_READY is defined but the plugin editor type is not loaded");
#endif
            // On a fresh clone's first run the plugin compiles after its resolver, so the tests run before a reload loads it.
            if (editor == null) { Assert.AreEqual("plugin not loaded", Status()); return; }
            Assert.AreEqual(Application.isBatchMode ? "held offline" : "held local", Status());
            var instance = editor.GetProperty("Instance", Any)!.GetValue(null);
            var config = editor.GetField("unityConnectionConfig", Any)!.GetValue(instance)!;
            object Get(string name) => config.GetType().GetProperty(name)!.GetValue(config)!;
            // The plugin's own UNITY_MCP_CONNECTION_MODE / UNITY_MCP_AUTH_OPTION knobs are an explicit opt-out the guard respects.
            if (Environment.GetEnvironmentVariable("UNITY_MCP_CONNECTION_MODE") == null)
                Assert.AreEqual("Custom", Get("ConnectionMode").ToString(), "never the plugin's cloud service");
            if (Environment.GetEnvironmentVariable("UNITY_MCP_AUTH_OPTION") == null)
                Assert.AreEqual("token", Get("AuthOption").ToString(), "the local server checks a token");
            // Recorded as runtime overrides, so the plugin never writes them into the user's own settings file.
            var record = editor.GetProperty("RuntimeOverrides", Any)!.GetValue(instance)!;
            bool Tracked(string name) => (bool)record.GetType().GetMethod("Contains")!.Invoke(record, new object[] { name })!;
            if (Environment.GetEnvironmentVariable("UNITY_MCP_CONNECTION_MODE") == null) Assert.IsTrue(Tracked("ConnectionMode"));
            if (Environment.GetEnvironmentVariable("UNITY_MCP_AUTH_OPTION") == null) Assert.IsTrue(Tracked("AuthOption"));
            if (!Application.isBatchMode) return;   // the interactive editor keeps the user's connect and server choices
            Assert.IsFalse((bool)Get("KeepConnected"), "no connection");
            Assert.IsFalse((bool)Get("KeepServerRunning"), "no local server");
            Assert.IsTrue(Tracked("KeepConnected") && Tracked("KeepServerRunning"));
        }

        [Test]
        public void InBatchModeThePluginOpensNoWindowStartsNoServerAndSkipsItsUpdateCheck()
        {
            var editor = PluginEditor();
            if (!Application.isBatchMode || editor == null) { CollectionAssert.Contains(new[] { "held local", "plugin not loaded" }, Status()); return; }
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
        public void TheTeamKillSwitchForTheUpdatePopupIsOn()
        {
            var editor = PluginEditor();
            if (editor == null) { Assert.AreEqual("plugin not loaded", Status()); return; }
            var checker = editor.Assembly.GetType("com.IvanMurzak.Unity.MCP.Editor.Utils.UpdateChecker")!;
            Assert.IsTrue((bool)checker.GetProperty("IsDisabledForProject")!.GetValue(null)!,
                "ProjectSettings/AI-Game-Developer-UpdateSettings.asset turns the update popup off for the team");
        }
    }
}
