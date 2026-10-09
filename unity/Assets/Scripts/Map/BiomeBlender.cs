using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Blended biome weights (spec §6): every hex centre within <see cref="BlendRadiusHexes"/> hex radii of a point
    /// contributes 1 - smoothstep(distance / radius); weights are normalised to sum to 1. A point no centre reaches (map
    /// edge, beyond the outermost centres) takes the nearest centre's biome. Deterministic: fixed iteration order.</summary>
    public sealed class BiomeBlender
    {
        public const float BlendRadiusHexes = 1.25f;

        readonly Dictionary<(int, int), List<(Vector2 p, int layer)>> grid = new Dictionary<(int, int), List<(Vector2, int)>>();
        readonly List<(Vector2 p, int layer)> all = new List<(Vector2, int)>();
        readonly float radius, cell; readonly int layerCount;

        public BiomeBlender(IReadOnlyList<Vector2> centres, IReadOnlyList<int> layers, float hexSizeM, int layerCount)
        {
            if (centres.Count != layers.Count) throw new ArgumentException("centres and layers differ in length");
            if (!(hexSizeM > 0)) throw new ArgumentOutOfRangeException(nameof(hexSizeM));
            this.layerCount = layerCount;
            radius = BlendRadiusHexes * hexSizeM;
            cell = radius; // a radius-sized cell: the 3x3 neighbourhood covers every centre within the radius
            for (int i = 0; i < centres.Count; i++)
            {
                if (layers[i] < 0 || layers[i] >= layerCount) throw new ArgumentOutOfRangeException(nameof(layers));
                var c = (centres[i], layers[i]);
                all.Add(c);
                var k = Key(centres[i]);
                if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<(Vector2, int)>();
                l.Add(c);
            }
        }

        (int, int) Key(Vector2 p) => (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));

        /// <summary>Fills <paramref name="w"/> (length = layer count) with normalised weights at <paramref name="p"/>.</summary>
        public void Weights(Vector2 p, float[] w)
        {
            if (w.Length != layerCount) throw new ArgumentException($"weights array length {w.Length} != {layerCount}");
            Array.Clear(w, 0, w.Length);
            var (cx, cy) = Key(p); float total = 0f;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (grid.TryGetValue((cx + dx, cy + dy), out var l))
                        foreach (var c in l)
                        {
                            float t = (c.p - p).magnitude / radius;
                            if (t >= 1f) continue;
                            float f = 1f - t * t * (3f - 2f * t); // 1 - smoothstep
                            w[c.layer] += f; total += f;
                        }
            if (total > 0f) { for (int i = 0; i < w.Length; i++) w[i] /= total; return; }
            w[NearestLayer(p)] = 1f;
        }

        int NearestLayer(Vector2 p)
        {
            float best = float.MaxValue; int layer = 0;
            foreach (var c in all) { float d = (c.p - p).sqrMagnitude; if (d < best) { best = d; layer = c.layer; } }
            return layer;
        }
    }
}
