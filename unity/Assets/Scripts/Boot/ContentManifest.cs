#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.Boot
{
    /// <summary>The list of content files a player needs, so a WebGL build (no directory listing over HTTP) can fetch
    /// them: content/manifest.json in StreamingAssets, {"files": [...]} with paths relative to content/, '/' separated,
    /// ordinal order. The editor build step (ContentSync) copies the files and writes the manifest.</summary>
    public static class ContentManifest
    {
        public const string FileName = "manifest.json";
        /// <summary>The map files players read: hexes.json and every map/*.yaml (settlements; settlement hexes for the sim).
        /// The precipitation files are tool inputs only.</summary>
        public const string HexesJson = "map/hexes.json";

        [Serializable] sealed class ManifestJson { public string[] files = new string[0]; }

        /// <summary>Every *.yaml in <paramref name="contentDir"/>, events/ and map/, plus map/hexes.json; ordinal order.</summary>
        public static List<string> Collect(string contentDir)
        {
            IEnumerable<string> Yaml(string sub) => Directory.GetFiles(Path.Combine(contentDir, sub), "*.yaml")
                .Select(f => (sub == "" ? "" : sub + "/") + Path.GetFileName(f));
            if (!File.Exists(Path.Combine(contentDir, HexesJson))) throw new FileNotFoundException("content file missing", HexesJson);
            return Yaml("").Concat(Yaml("events")).Concat(Yaml("map")).Append(HexesJson)
                .OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        public static string Write(IEnumerable<string> files) => JsonUtility.ToJson(new ManifestJson { files = files.ToArray() }, true);

        /// <summary>Parses manifest.json; throws InvalidDataException when it is not a manifest.</summary>
        public static List<string> Parse(string json)
        {
            ManifestJson? m;
            try { m = JsonUtility.FromJson<ManifestJson>(json); }
            catch (ArgumentException e) { throw new InvalidDataException($"{FileName}: malformed JSON ({e.Message})", e); }
            if (m?.files == null || m.files.Length == 0) throw new InvalidDataException($"{FileName}: no files");
            return m.files.ToList();
        }

        /// <summary>Replaces <paramref name="targetDir"/> with a copy of the collected files plus manifest.json.</summary>
        public static List<string> Copy(string contentDir, string targetDir)
        {
            var files = Collect(contentDir);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            foreach (var f in files)
            {
                var to = Path.Combine(targetDir, f);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                File.Copy(Path.Combine(contentDir, f), to);
            }
            File.WriteAllText(Path.Combine(targetDir, FileName), Write(files));
            return files;
        }
    }
}
