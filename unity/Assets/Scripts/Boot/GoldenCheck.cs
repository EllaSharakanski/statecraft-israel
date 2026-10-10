#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>Runs both golden playthroughs in whatever runtime this is (IL2CPP in players) and reports
    /// "GOLDEN OK &lt;year&gt; &lt;hash&gt;" or "GOLDEN FAIL &lt;year&gt; &lt;hash&gt; expected &lt;hash&gt;" (docs/determinism.md).
    /// Asked for by the URL query "golden" on WebGL, or the command-line flag -golden on desktop.</summary>
    public static class GoldenCheck
    {
        public static bool Requested() => Application.platform == RuntimePlatform.WebGLPlayer
            ? UrlFlags.Has(Application.absoluteURL, "golden")
            : Environment.GetCommandLineArgs().Contains("-golden");

        public static (bool ok, List<string> lines) Run(Content c)
        {
            var lines = new List<string>();
            bool ok = true;
            foreach (var (until, expected) in new[] { (GoldenRun.Until1950, GoldenRun.Hash1950), (GoldenRun.Until1952, GoldenRun.Hash1952) })
            {
                var hash = GoldenRun.Play(c, until).Hash();
                var year = until.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (hash == expected) lines.Add($"GOLDEN OK {year} {hash}");
                else { ok = false; lines.Add($"GOLDEN FAIL {year} {hash} expected {expected}"); }
            }
            return (ok, lines);
        }
    }
}
