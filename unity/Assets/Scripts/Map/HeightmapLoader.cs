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
        public static (HeightmapMeta meta, float[,] heights) Load(string dir)
        {
            var metaPath = Path.Combine(dir, "heightmap.json");
            var meta = ParseMeta(metaPath, File.ReadAllText(metaPath));
            if (meta.width != meta.height) throw new InvalidDataException($"heightmap.json: width {meta.width} != height {meta.height}");
            var bytes = File.ReadAllBytes(Path.Combine(dir, "heightmap.r16"));
            if (bytes.Length != meta.width * meta.height * 2) throw new InvalidDataException($"heightmap.r16: {bytes.Length} bytes, expected {meta.width * meta.height * 2}");
            var raw = new ushort[meta.width * meta.height];
            Buffer.BlockCopy(bytes, 0, raw, 0, bytes.Length); // little-endian on all our targets
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
