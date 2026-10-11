#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Headless player builds. Map.unity is the boot scene; ContentSync copies content/ into StreamingAssets
    /// first (IPreprocessBuildWithReport). Exits non-zero on failure.
    ///   Web: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web
    ///        [-compression disabled|gzip|brotli] [-out Builds/Web-disabled]
    ///   Mac: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac
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

        /// <summary>Why a shareable build may not be made, or null: every event with a sensitive tag needs the owner's
        /// sensitivity review first (status sensitivity-reviewed; docs/m3-release.md, M3 final review I4), and so does every
        /// sensitive event photograph the build would carry (EventImageCatalog; docs/event-images.md).</summary>
        public static string? ShareableRefusal(Statecraft.Sim.Content c, Statecraft.Game.UI.EventImageCatalog? photos)
        {
            var open = Statecraft.Sim.ReleaseGate.SensitiveUnreviewed(c);
            var reasons = new System.Collections.Generic.List<string>();
            if (open.Count > 0)
                reasons.Add("a shareable build needs the owner's sensitivity review of: " + string.Join(", ", open) + " (set status: sensitivity-reviewed; see docs/m3-release.md)");
            if (EventImageRefusal(photos) is string pictures) reasons.Add(pictures);
            return reasons.Count == 0 ? null : string.Join("; ", reasons);
        }

        /// <summary>Why the event photographs may not ship in a shareable build, or null: a picture marked sensitive needs
        /// the owner's sensitivity review (sensitivity_reviewed: true in Assets/UI/EventImages/event-images.yaml, then
        /// EventImageImport.Run). An OWNER step, like the event review.</summary>
        public static string? EventImageRefusal(Statecraft.Game.UI.EventImageCatalog? photos)
        {
            var open = photos != null ? photos.SensitiveUnreviewed() : (System.Collections.Generic.IReadOnlyList<string>)Array.Empty<string>();
            return open.Count == 0 ? null
                : "a shareable build needs the owner's sensitivity review of the event photographs: " + string.Join(", ", open)
                  + " (set sensitivity_reviewed: true in " + Statecraft.Game.UI.EventImageMapping.Path + ", run EventImageImport; see docs/event-images.md)";
        }

        /// <summary>The gate for a build made with these command-line args: a -shareable build (Web or Mac) is refused
        /// until the reviews are done; a dev build is never gated (web_check and the golden checks need them).</summary>
        public static string? GateRefusal(string[] args, Statecraft.Sim.Content c, Statecraft.Game.UI.EventImageCatalog? photos) =>
            Array.IndexOf(args, "-shareable") >= 0 ? ShareableRefusal(c, photos) : null;

        /// <summary>Logs "BUILD REFUSED ..." and exits 1 when the gate refuses this build; true if it did. The photographs
        /// checked are the catalog the build ships (Resources/EventImageCatalog.asset).</summary>
        static bool RefuseUnlessShareable()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-shareable") < 0) return false;
            var photos = AssetDatabase.LoadAssetAtPath<Statecraft.Game.UI.EventImageCatalog>(EventImageImport.CatalogPath);
            if (!(GateRefusal(args, Statecraft.Sim.ContentLoader.Load(Paths.Content), photos) is string refusal)) return false;
            Debug.LogError("BUILD REFUSED " + refusal);
            EditorApplication.Exit(1);
            return true;
        }

        public static void Web()
        {
            if (RefuseUnlessShareable()) return;
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
            if (RefuseUnlessShareable()) return;
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

        /// <summary>The real size of the build output (summary.totalSize also counts build intermediates).</summary>
        static double OutputMegabytes(string path)
        {
            if (File.Exists(path)) return new FileInfo(path).Length / 1048576.0;
            return Directory.Exists(path) ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length) / 1048576.0 : 0;
        }

        static bool Run(BuildTarget target, string location)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene }, locationPathName = location, target = target, options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"BUILD {s.result} {target} -> {location}: {OutputMegabytes(location):0.0} MB on disk, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s");
            return s.result == BuildResult.Succeeded;
        }
    }
}
