#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Statecraft.Tests.EditMode
{
    /// <summary>Unity-MCP (com.ivanmurzak.unity.mcp) is an interactive-editor tool. In batch mode (tests and builds) it must
    /// not connect anywhere or start its local server, and no part of it may ship in a player build.</summary>
    public sealed class UnityMcpGuardTests
    {
        static Type Guard()
        {
            var t = Type.GetType("Statecraft.Game.EditorTools.UnityMcpGuard, Assembly-CSharp-Editor");
            Assert.IsNotNull(t, "the UnityMcpGuard editor hook exists");
            return t!;
        }

        static string[] Filter(params string[] assemblies) =>
            (string[])Guard().GetMethod("Filter", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { assemblies })!;

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
        public void InBatchModeThePluginStaysOfflineForThisProcessOnly()
        {
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var status = (string)Guard().GetField("Status", any)!.GetValue(null)!;
            var editor = (Type?)Guard().GetMethod("LoadedPluginEditorType", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            // Not Assume: Unity's runner exits 2 on an inconclusive test. The interactive editor is left alone, and on a
            // fresh clone's first run the plugin compiles after its resolver, so the tests run before a reload loads it.
            if (!Application.isBatchMode) { Assert.AreEqual("interactive editor", status); return; }
            if (editor == null) { Assert.AreEqual("plugin not loaded", status); return; }
            Assert.AreEqual("held offline", status);
            var instance = editor!.GetProperty("Instance", any)!.GetValue(null);
            var config = editor.GetField("unityConnectionConfig", any)!.GetValue(instance)!;
            Assert.IsFalse((bool)config.GetType().GetProperty("KeepConnected")!.GetValue(config)!, "no connection");
            Assert.IsFalse((bool)config.GetType().GetProperty("KeepServerRunning")!.GetValue(config)!, "no local server");
            // Recorded as runtime overrides, so the plugin never writes them into the user's own settings file.
            var record = editor.GetProperty("RuntimeOverrides", any)!.GetValue(instance)!;
            var contains = record.GetType().GetMethod("Contains")!;
            Assert.IsTrue((bool)contains.Invoke(record, new object[] { "KeepConnected" })!);
            Assert.IsTrue((bool)contains.Invoke(record, new object[] { "KeepServerRunning" })!);
            // The plugin's first-run window would otherwise open on screen in batch mode, and its server panel starts
            // the local server (seen in a fresh clone's test run, 2026-10-10).
            var window = editor.Assembly.GetType("com.IvanMurzak.Unity.MCP.Editor.UI.MainWindowEditor")!;
            Assert.IsEmpty(Resources.FindObjectsOfTypeAll(window), "no plugin window in batch mode");
            var server = editor.Assembly.GetType("com.IvanMurzak.Unity.MCP.Editor.McpServerManager")!;
            Assert.IsFalse((bool)server.GetProperty("IsRunning")!.GetValue(null)!, "no local server process");
            Assert.IsFalse((bool)server.GetProperty("IsStarting")!.GetValue(null)!, "no local server starting");
        }
    }
}
