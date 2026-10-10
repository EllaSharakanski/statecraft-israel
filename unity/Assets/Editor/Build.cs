#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Headless player builds. Map.unity is the boot scene; ContentSync copies content/ into StreamingAssets
    /// first (IPreprocessBuildWithReport). Exits non-zero on failure.
    ///   Web: Unity -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web
    ///        [-compression disabled|gzip|brotli] [-out Builds/Web-disabled]
    ///   Mac: Unity -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac
    ///        [-out Builds/Mac/Statecraft.app] [-ffp-contract-off]</summary>
    public static class Build
    {
        static string? Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static bool Flag(string name) => Environment.GetCommandLineArgs().Contains(name);

        public static WebGLCompressionFormat Compression(string? name) => (name ?? "disabled") switch
        {
            "disabled" => WebGLCompressionFormat.Disabled,
            "gzip" => WebGLCompressionFormat.Gzip,
            "brotli" => WebGLCompressionFormat.Brotli,
            _ => throw new ArgumentException($"-compression must be disabled, gzip or brotli (got '{name}')"),
        };

        public static void Web()
        {
            var name = Arg("-compression") ?? "disabled";
            ProjectSetup.Apply();
            PlayerSettings.WebGL.compressionFormat = Compression(name);
            PlayerSettings.WebGL.decompressionFallback = false;   // itch.io serves .br/.gz with Content-Encoding
            bool ok;
            try { ok = Run(BuildTarget.WebGL, Arg("-out") ?? "Builds/Web-" + name); }
            finally { ProjectSetup.Apply(); }                      // back to the committed settings (compression Disabled)
            ExitIfFailed(ok);
        }

        public static void Mac()
        {
            ProjectSetup.Apply();
            // Apple clang fuses a*b+c into fmadd by default (tech notes 4.2); turn it off if the Mac golden check fails.
            PlayerSettings.SetAdditionalIl2CppArgs(Flag("-ffp-contract-off") ? "--compiler-flags=-ffp-contract=off" : "");
            bool ok;
            try { ok = Run(BuildTarget.StandaloneOSX, Arg("-out") ?? "Builds/Mac/Statecraft.app"); }
            finally { PlayerSettings.SetAdditionalIl2CppArgs(""); AssetDatabase.SaveAssets(); }
            ExitIfFailed(ok);
        }

        /// <summary>Exits with code 1 after the caller's settings have been restored.</summary>
        static void ExitIfFailed(bool ok) { if (!ok) EditorApplication.Exit(1); }

        static bool Run(BuildTarget target, string location)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene }, locationPathName = location, target = target, options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"BUILD {s.result} {target} -> {location}: {s.totalSize / 1048576.0:0.0} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            return s.result == BuildResult.Succeeded;
        }
    }
}
