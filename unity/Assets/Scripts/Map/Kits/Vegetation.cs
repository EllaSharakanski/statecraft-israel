#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map.Kits
{
    public enum TreeKind { Citrus, Olive, Palm, Cypress }

    /// <summary>Procedural trees at the symbolic scale (issue #34): citrus groves (dense rows of round dark-green trees with
    /// fruit specks) on the coastal plain round the towns, olives scattered over the hills, palms by the shore of the coastal
    /// towns, cypress windbreaks by villages. There is no land-use layer yet (Deferred row "Land-use texture"), so where
    /// each grows is a seeded rule over the biome, elevation, slope and the settlements; every draw is seeded (no
    /// System.Random), so a map always grows the same trees.</summary>
    public static class Vegetation
    {
        /// <summary>One tree's mesh in real metres (the instance matrix applies KitScale.Trees).</summary>
        public static Mesh BuildMesh(TreeKind kind) => Build(kind).ToMesh("Tree_" + kind);

        /// <summary>The tree's parts before upload (tests read them).</summary>
        public static KitMesh Build(TreeKind kind)
        {
            var km = new KitMesh();
            var rnd = new KitRandom("tree:" + kind, 3);
            Color32 C(float r, float g, float b) => new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 0);
            switch (kind)
            {
                case TreeKind.Citrus:
                {
                    // Promo 03's orange trees: a dark, lumpy crown of three clusters, heavy with fruit.
                    var leaf = C(0.17f, 0.30f, 0.12f); var fruit = C(0.92f, 0.50f, 0.09f);
                    km.Shaft(new Vector3(0, -0.6f, 0), new Vector3(0, 1.0f, 0), 0.18f, 0.14f, C(0.33f, 0.25f, 0.17f), 5);
                    km.Crown(new Vector3(0, 2.3f, 0), new Vector3(2.1f, 1.8f, 2.1f), leaf, ref rnd, 0.10f, 8, 6, 0.13f, fruit, 0.55f);
                    km.Crown(new Vector3(0.9f, 2.9f, 0.5f), new Vector3(1.3f, 1.1f, 1.3f), leaf, ref rnd, 0.12f, 7, 5, 0.16f, fruit, 0.5f);
                    km.Crown(new Vector3(-0.8f, 2.8f, -0.6f), new Vector3(1.2f, 1.0f, 1.2f), leaf, ref rnd, 0.12f, 7, 5, 0.16f, fruit, 0.5f);
                    break;
                }
                case TreeKind.Olive:
                    km.Shaft(new Vector3(0, -0.6f, 0), new Vector3(0.15f, 1.7f, 0.1f), 0.32f, 0.22f, C(0.36f, 0.31f, 0.25f), 5);
                    km.Crown(new Vector3(0.5f, 2.9f, 0.2f), new Vector3(2.0f, 1.5f, 1.9f), C(0.50f, 0.55f, 0.39f), ref rnd, 0.16f, 8, 5);
                    km.Crown(new Vector3(-0.8f, 2.6f, -0.4f), new Vector3(1.6f, 1.3f, 1.5f), C(0.46f, 0.52f, 0.37f), ref rnd, 0.18f, 7, 5);
                    km.Crown(new Vector3(-0.1f, 3.5f, 0.6f), new Vector3(1.3f, 1.0f, 1.3f), C(0.55f, 0.60f, 0.44f), ref rnd, 0.15f, 7, 4);
                    break;
                case TreeKind.Palm:
                {
                    var trunk = C(0.48f, 0.40f, 0.30f);
                    var mid = new Vector3(0.35f, 6f, 0.1f); var top = new Vector3(0.9f, 11.5f, 0.25f);
                    km.Shaft(new Vector3(0, -0.6f, 0), mid, 0.30f, 0.24f, trunk, 6);
                    km.Shaft(mid, top, 0.24f, 0.21f, trunk, 6);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * Mathf.PI * 2f / 9f + rnd.Range(-0.15f, 0.15f);
                        km.Frond(top, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), rnd.Range(3.8f, 4.6f), 1.1f, rnd.Range(0.55f, 0.85f),
                            i % 3 == 0 ? C(0.36f, 0.45f, 0.20f) : C(0.28f, 0.40f, 0.17f));
                    }
                    break;
                }
                default:
                    km.Shaft(new Vector3(0, -0.6f, 0), new Vector3(0, 0.8f, 0), 0.2f, 0.18f, C(0.30f, 0.24f, 0.18f), 5);
                    km.Crown(new Vector3(0, 4.6f, 0), new Vector3(1.05f, 4.4f, 1.05f), C(0.14f, 0.27f, 0.13f), ref rnd, 0.08f, 7, 7, 0f, default, 0.9f);
                    break;
            }
            return km;
        }

        /// <summary>Where each tree stands: (kind, world matrix) for the whole map, seeded.</summary>
        public static List<(TreeKind kind, Matrix4x4 m)> Plant(IReadOnlyList<Settlement> settlements, KitGround ground)
        {
            var trees = new List<(TreeKind, Matrix4x4)>();
            float s = KitScale.Trees;
            // Counts fall with the square of the tree scale (k = 1 at the default), so a larger tree covers the same ground
            // with fewer instances instead of burying the towns.
            float ks = s / KitScale.DefaultTrees;
            void Add(TreeKind k, float x, float z, ref KitRandom rnd, float size = 1f)
            {
                float y = ground.Y(x, z);
                var scale = s * size * rnd.Range(0.85f, 1.15f);
                trees.Add((k, Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, rnd.Range(0f, 360f), 0), new Vector3(scale, scale * rnd.Range(0.9f, 1.1f), scale))));
            }

            // Citrus groves round the coastal-plain towns: rectangular blocks of rows, each grove on its own angle.
            foreach (var st in settlements)
            {
                var c3 = ground.Frame.ToWorld(st.Lon, st.Lat, 0); var c = new Vector2(c3.x, c3.z);
                var biome = ground.Biome(c.x, c.y);
                if (biome != "coastal_plain" && biome != "valley" && biome != "sea") continue;
                if (ground.ElevM(c.x, c.y) > 300f) continue;
                var rnd = new KitRandom(st.Id, 11);
                float R = SettlementKits.FootprintRadiusM(st, ground.HexSizeM);
                int groves = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(KitPopulation.Of(st.Id), 400)) / 9f / ks), 1, 28);
                for (int g = 0, tries = 0; g < groves && tries < groves * 12; tries++)
                {
                    float a = rnd.Range(0f, 2f * Mathf.PI), r = R * 0.95f + rnd.Range(0f, 8500f);
                    var gc = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!ground.Inside(gc.x, gc.y) || ground.Water(gc.x, gc.y) || ground.InTown(gc.x, gc.y, 0.9f)) continue;
                    var gb = ground.Biome(gc.x, gc.y);
                    if (gb != "coastal_plain" && gb != "valley" || ground.ElevM(gc.x, gc.y) > 250f || ground.Slope(gc.x, gc.y, 800f) > 0.06f) continue;
                    g++;
                    float yaw = rnd.Range(0f, 90f) * Mathf.Deg2Rad, ca = Mathf.Cos(yaw), sa = Mathf.Sin(yaw);
                    // Grove sizes grow with the square root of the tree scale, so a bigger tree means fewer, not only larger, rows.
                    float grow = Mathf.Sqrt(s / KitScale.DefaultTrees);
                    float w = rnd.Range(1300f, 3000f) * grow, d = rnd.Range(900f, 2000f) * grow, step = 4.9f * s;
                    // A ragged block: each row stops at its own length, and some groves lose a corner (an L shape).
                    bool notch = rnd.Chance(0.3f); float nu = rnd.Range(0.1f, 0.3f) * w, nv = rnd.Range(0.1f, 0.3f) * d;
                    for (float v = -d / 2; v <= d / 2; v += step)
                    {
                        float rowEnd = w / 2 - rnd.Range(0f, 0.25f) * w;
                        for (float u = -w / 2; u <= rowEnd; u += step)
                        {
                            if (notch && u > nu && v > nv) continue;
                            if (rnd.Chance(0.06f)) continue;   // a gap: a dead tree, a well, a packing shed's yard
                            float x = gc.x + u * ca - v * sa, z = gc.y + u * sa + v * ca;
                            if (ground.Water(x, z) || ground.InTown(x, z, 0.85f)) continue;
                            Add(TreeKind.Citrus, x, z, ref rnd, rnd.Range(0.9f, 1.05f));
                        }
                    }
                }
            }

            // Olives over the hills and mountains: one jittered cell per 1.2 km, in clumps (Perlin), denser by the old towns.
            {
                var rnd = new KitRandom("olives", 13);
                // About 2.6 crowns apart where they grow (promo 03's hills), but never more than one cell per 1.1 km: at small
                // tree scales the hills would need hundreds of thousands of trees (the WebGL budget, docs/visual-v1.md).
                float cell = Mathf.Max(1100f, 2.6f * 4.5f * s);
                for (float z = cell / 2; z < ground.Length; z += cell)
                    for (float x = cell / 2; x < ground.Width; x += cell)
                    {
                        float px = x + rnd.Range(-0.45f, 0.45f) * cell, pz = z + rnd.Range(-0.45f, 0.45f) * cell;
                        float clump = Mathf.PerlinNoise(px / 7000f + 13.1f, pz / 7000f + 7.7f);
                        float p = Mathf.Clamp01((clump - 0.30f) * 1.8f) * 0.8f;
                        if (p <= 0f && rnd.Value() > 0.03f) continue;
                        if (!rnd.Chance(Mathf.Max(p, 0.03f))) continue;
                        var b = ground.Biome(px, pz);
                        bool plain = b == "coastal_plain";
                        if (!plain && b != "hills" && b != "mountains" && b != "valley") continue;
                        if (plain && !rnd.Chance(0.35f)) continue;   // lone trees between the fields
                        float e = ground.ElevM(px, pz);
                        if (e < (plain ? 5f : 40f) || e > 950f || ground.Water(px, pz) || ground.InTown(px, pz, 0.9f) || ground.Slope(px, pz, 600f) > 0.35f) continue;
                        int n = rnd.Range(1, 4);
                        for (int i = 0; i < n; i++)
                        {
                            float tx = px + rnd.Range(-0.5f, 0.5f) * cell, tz = pz + rnd.Range(-0.5f, 0.5f) * cell;
                            if (!ground.Water(tx, tz) && !ground.InTown(tx, tz, 0.85f)) Add(TreeKind.Olive, tx, tz, ref rnd);
                        }
                    }
            }

            // Palms by the shore of the coastal towns and in their streets; cypress windbreaks by villages and moshavot.
            foreach (var st in settlements)
            {
                var c3 = ground.Frame.ToWorld(st.Lon, st.Lat, 0); var c = new Vector2(c3.x, c3.z);
                var rnd = new KitRandom(st.Id, 17);
                float R = SettlementKits.FootprintRadiusM(st, ground.HexSizeM);
                var arch = SettlementKits.ArchetypeOf(st);
                bool coastal = ground.SeaDistance(c.x, c.y, 6000f) < 6000f;
                if (coastal)
                {
                    int n = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(KitPopulation.Of(st.Id), 400)) * 0.9f / (ks * ks)), 4, 260);
                    for (int i = 0, tries = 0; i < n && tries < n * 10; tries++)
                    {
                        float a = rnd.Range(0f, 2f * Mathf.PI), r = (R + 2500f) * Mathf.Sqrt(rnd.Value());
                        float x = c.x + Mathf.Cos(a) * r, z = c.y + Mathf.Sin(a) * r;
                        if (ground.Water(x, z)) continue;
                        bool shore = ground.SeaDistance(x, z, 1600f) < 1600f;
                        if (!shore && !rnd.Chance(0.35f)) continue;   // mostly by the shore, some in the streets
                        i++;
                        int clump = rnd.Range(1, 4);
                        for (int k = 0; k < clump; k++)
                        {
                            float px = x + rnd.Range(-1f, 1f) * 6f * s, pz = z + rnd.Range(-1f, 1f) * 6f * s;
                            if (!ground.Water(px, pz)) Add(TreeKind.Palm, px, pz, ref rnd);
                        }
                    }
                }
                if (arch == Archetype.Moshava || arch == Archetype.ArabTown)
                {
                    int rows = Mathf.Max(1, Mathf.RoundToInt((arch == Archetype.Moshava ? 4 : 3) / ks));
                    for (int row = 0; row < rows; row++)
                    {
                        float a = rnd.Range(0f, 2f * Mathf.PI), r = R * rnd.Range(0.5f, 1.15f);
                        var o = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        var dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                        int n = rnd.Range(5, 11);
                        for (int k = 0; k < n; k++)
                        {
                            var p = o + dir * ((k - n / 2f) * 3.2f * s);
                            if (!ground.Water(p.x, p.y)) Add(TreeKind.Cypress, p.x, p.y, ref rnd, rnd.Range(0.9f, 1.1f));
                        }
                    }
                }
            }
            return trees;
        }
    }
}
