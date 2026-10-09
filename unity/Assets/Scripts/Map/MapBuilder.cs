using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable] public sealed class HexJson { public int q, r; public double lon, lat; public float elev_m; public bool land; public string biome; }
    [Serializable] public sealed class HexFile { public float hex_size_km; public string layout; public HexJson[] hexes; }

    public static class MapBuilder
    {
        public const float VerticalExaggeration = 3f; // diorama look; real relief would read flat at 400 km scale

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        /// <summary>Builds the terrain (with a "HexOverlay" child) from the heightmap in <paramref name="mapDir"/> and the
        /// hexes in <paramref name="hexesJson"/>. Materials are editor-made assets (Assets/Materials/Terrain.mat,
        /// HexOverlay.mat) passed in by the caller: Shader.Find returns null in players (M0 correction 1).</summary>
        public static GameObject Build(string mapDir, string hexesJson, Material terrainMaterial, Material overlayMaterial)
        {
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            if (overlayMaterial == null) throw new ArgumentNullException(nameof(overlayMaterial));
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            float widthM = meta.width_km * 1000f, lengthM = meta.length_km * 1000f;
            float reliefM = (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration;
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, widthM, lengthM);

            var hexFile = JsonUtility.FromJson<HexFile>(File.ReadAllText(hexesJson));
            if (hexFile.layout != "pointy") throw new InvalidDataException($"hexes.json: layout '{hexFile.layout}' unsupported");

            var data = new TerrainData { heightmapResolution = meta.width };
            data.size = new Vector3(widthM, reliefM, lengthM);
            data.SetHeights(0, 0, heights);

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            terrainGo.GetComponent<Terrain>().materialTemplate = terrainMaterial;
            ApplyBiomeColours(data, frame, hexFile);

            var overlay = BuildHexOverlay(frame, hexFile, data, overlayMaterial);
            overlay.transform.SetParent(terrainGo.transform, false);
            return terrainGo;
        }

        /// <summary>One terrain layer per biome; each alphamap texel takes the biome of its nearest hex centre.</summary>
        static void ApplyBiomeColours(TerrainData data, GeoFrame frame, HexFile hexes)
        {
            var names = new List<string>(Biome.Keys);
            var layers = new TerrainLayer[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Biome[names[i]]); tex.Apply();
                layers[i] = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(1000, 1000) };
            }
            data.terrainLayers = layers;
            int res = 512; data.alphamapResolution = res;
            var centres = new List<(Vector2 p, int layer)>();
            foreach (var h in hexes.hexes)
            {
                int idx = names.IndexOf(h.biome);
                if (idx < 0) throw new InvalidDataException($"hexes.json: unknown biome '{h.biome}' at q={h.q} r={h.r}");
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add((new Vector2(w.x, w.z), idx));
            }
            float cell = hexes.hex_size_km * 1000f * 2f;
            var grid = BucketGrid(centres, cell);
            var a = new float[res, res, names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    var p = new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM);
                    a[y, x, Nearest(grid, p, cell)] = 1f;
                }
            data.SetAlphamaps(0, 0, a);
        }

        static Dictionary<(int, int), List<(Vector2 p, int layer)>> BucketGrid(List<(Vector2 p, int layer)> pts, float cell)
        {
            var g = new Dictionary<(int, int), List<(Vector2, int)>>();
            foreach (var c in pts)
            {
                var k = ((int)(c.p.x / cell), (int)(c.p.y / cell));
                if (!g.TryGetValue(k, out var l)) g[k] = l = new List<(Vector2, int)>();
                l.Add(c);
            }
            return g;
        }

        static int Nearest(Dictionary<(int, int), List<(Vector2 p, int layer)>> g, Vector2 p, float cell)
        {
            int cx = (int)(p.x / cell), cy = (int)(p.y / cell); float best = float.MaxValue; int layer = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (g.TryGetValue((cx + dx, cy + dy), out var l))
                        foreach (var c in l) { float d = (c.p - p).sqrMagnitude; if (d < best) { best = d; layer = c.layer; } }
            return layer;
        }

        /// <summary>Thin line mesh of every land hex outline, draped 30 m above the terrain.</summary>
        static GameObject BuildHexOverlay(GeoFrame frame, HexFile hexes, TerrainData data, Material material)
        {
            float size = hexes.hex_size_km * 1000f;
            var verts = new List<Vector3>(); var idx = new List<int>();
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                var c = frame.ToWorld(h.lon, h.lat, 0);
                int start = verts.Count;
                for (int i = 0; i < 6; i++)
                {
                    var k = HexMath.Corner(new Vector2(c.x, c.z), size, i);
                    float y = data.GetInterpolatedHeight(k.x / frame.WidthM, k.y / frame.LengthM) + 30f;
                    verts.Add(new Vector3(k.x, y, k.y));
                }
                for (int i = 0; i < 6; i++) { idx.Add(start + i); idx.Add(start + (i + 1) % 6); }
            }
            var mesh = new Mesh { name = "HexOverlay", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetIndices(idx, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            var go = new GameObject("HexOverlay");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
