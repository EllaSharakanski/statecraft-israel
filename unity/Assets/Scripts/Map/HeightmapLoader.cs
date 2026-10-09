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
            var meta = JsonUtility.FromJson<HeightmapMeta>(File.ReadAllText(Path.Combine(dir, "heightmap.json")));
            if (meta.width != meta.height) throw new InvalidDataException($"heightmap.json: width {meta.width} != height {meta.height}");
            var bytes = File.ReadAllBytes(Path.Combine(dir, "heightmap.r16"));
            if (bytes.Length != meta.width * meta.height * 2) throw new InvalidDataException($"heightmap.r16: {bytes.Length} bytes, expected {meta.width * meta.height * 2}");
            var raw = new ushort[meta.width * meta.height];
            Buffer.BlockCopy(bytes, 0, raw, 0, bytes.Length); // little-endian on all our targets
            return (meta, ToUnityHeights(raw, meta.width));
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
