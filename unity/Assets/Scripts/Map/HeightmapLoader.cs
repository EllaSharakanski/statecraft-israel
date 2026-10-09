using System;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable]
    public sealed class HeightmapMeta
    {
        public int width, height; public double lon0, lon1, lat0, lat1;
        public float elev_min_m, elev_max_m, width_km, length_km; public string attribution;
    }

    public static class HeightmapLoader
    {
        /// <summary>Reads heightmap.json and heightmap.r16 from <paramref name="dir"/> with System.IO (editor and desktop).</summary>
        public static (HeightmapMeta meta, float[,] heights) Load(string dir)
        {
            var metaPath = Path.Combine(dir, "heightmap.json");
            return Load(File.ReadAllText(metaPath), File.ReadAllBytes(Path.Combine(dir, "heightmap.r16")), metaPath);
        }

        /// <summary>Parses already-read heightmap inputs (no System.IO, so a WebGL build can pass what UnityWebRequest
        /// fetched). Errors name <paramref name="metaSource"/> for the meta, as the path overload names the file.</summary>
        public static (HeightmapMeta meta, float[,] heights) Load(string metaJson, byte[] r16, string metaSource = "heightmap.json")
        {
            var meta = ParseMeta(metaSource, metaJson);
            if (meta.width != meta.height) throw new InvalidDataException($"heightmap.json: width {meta.width} != height {meta.height}");
            if (r16 == null) throw new ArgumentNullException(nameof(r16));
            if (r16.Length != meta.width * meta.height * 2) throw new InvalidDataException($"heightmap.r16: {r16.Length} bytes, expected {meta.width * meta.height * 2}");
            var raw = new ushort[meta.width * meta.height];
            Buffer.BlockCopy(r16, 0, raw, 0, r16.Length); // little-endian on all our targets
            return (meta, ToUnityHeights(raw, meta.width));
        }

        static HeightmapMeta ParseMeta(string path, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException($"{path}: empty file");
            HeightmapMeta meta;
            try { meta = JsonUtility.FromJson<HeightmapMeta>(text); }
            catch (ArgumentException e) { throw new InvalidDataException($"{path}: malformed JSON ({e.Message})", e); }
            if (meta == null) throw new InvalidDataException($"{path}: empty or null document");
            if (meta.width <= 0 || meta.height <= 0) throw new InvalidDataException($"{path}: width {meta.width} / height {meta.height} must be > 0");
            if (!(meta.width_km > 0)) throw new InvalidDataException($"{path}: width_km {meta.width_km} must be > 0");
            if (!(meta.length_km > 0)) throw new InvalidDataException($"{path}: length_km {meta.length_km} must be > 0");
            if (!(meta.lon1 > meta.lon0)) throw new InvalidDataException($"{path}: lon1 {meta.lon1} must be > lon0 {meta.lon0}");
            if (!(meta.lat1 > meta.lat0)) throw new InvalidDataException($"{path}: lat1 {meta.lat1} must be > lat0 {meta.lat0}");
            if (!(meta.elev_max_m > meta.elev_min_m)) throw new InvalidDataException($"{path}: elev_max_m {meta.elev_max_m} must be > elev_min_m {meta.elev_min_m}");
            return meta;
        }

        /// <summary>File row 0 is north; Unity heights[y, x] has y = 0 at south.</summary>
        public static float[,] ToUnityHeights(ushort[] raw, int n)
        {
            var h = new float[n, n];
            for (int fileRow = 0; fileRow < n; fileRow++)
                for (int x = 0; x < n; x++)
                    h[n - 1 - fileRow, x] = raw[fileRow * n + x] / 65535f;
            return h;
        }
    }
}
