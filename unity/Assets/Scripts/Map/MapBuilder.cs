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
        public const int AlphamapResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
        public const float BiomeLayerTileSizeM = 1000f; // layers are flat colours, so the tiling only needs to be finite
        public const int OverlaySegmentsPerEdge = 10;  // ~500 m sub-segments on a 5 km hex edge, each draped on the terrain
        public const float OverlayClearanceM = 40f * VerticalExaggeration; // above the (exaggerated) terrain surface

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        /// <summary>Builds the terrain (with a "HexOverlay" child) from the heightmap in <paramref name="mapDir"/> and the
        /// hexes in <paramref name="hexesJson"/>. Materials are editor-made assets (Assets/Materials/Terrain.mat,
        /// HexOverlay.mat) passed in by the caller: Shader.Find returns null in players (M0 correction 1).
        /// Runtime-created objects are owned by a <see cref="MapRuntimeAssets"/> on the returned GameObject and destroyed with it.</summary>
        public static GameObject Build(string mapDir, string hexesJson, Material terrainMaterial, Material overlayMaterial)
        {
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            if (overlayMaterial == null) throw new ArgumentNullException(nameof(overlayMaterial));
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            var hexFile = LoadHexes(hexesJson);
            float widthM = meta.width_km * 1000f, lengthM = meta.length_km * 1000f;
            float reliefM = (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration;
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, widthM, lengthM);

            var data = new TerrainData { heightmapResolution = meta.width };
            data.size = new Vector3(widthM, reliefM, lengthM);
            data.SetHeights(0, 0, heights);

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            var assets = terrainGo.AddComponent<MapRuntimeAssets>();
            assets.Own(data);
            terrainGo.GetComponent<Terrain>().materialTemplate = terrainMaterial;
            ApplyBiomeColours(data, frame, hexFile, hexesJson, assets);

            var overlay = BuildHexOverlay(frame, hexFile, data, overlayMaterial, assets);
            overlay.transform.SetParent(terrainGo.transform, false);
            return terrainGo;
        }

        /// <summary>Reads and validates hexes.json; problems throw InvalidDataException naming the file.</summary>
        public static HexFile LoadHexes(string path)
        {
            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException($"{path}: empty file");
            HexFile f;
            try { f = JsonUtility.FromJson<HexFile>(text); }
            catch (ArgumentException e) { throw new InvalidDataException($"{path}: malformed JSON ({e.Message})", e); }
            if (f == null) throw new InvalidDataException($"{path}: empty file");
            if (f.hexes == null || f.hexes.Length == 0) throw new InvalidDataException($"{path}: no hexes");
            if (!(f.hex_size_km > 0)) throw new InvalidDataException($"{path}: hex_size_km {f.hex_size_km} must be > 0");
            if (f.layout != "pointy") throw new InvalidDataException($"{path}: layout '{f.layout}' unsupported (expected 'pointy')");
            return f;
        }

        /// <summary>One terrain layer per biome; each alphamap texel blends the biomes of nearby hex centres (BiomeBlender).</summary>
        static void ApplyBiomeColours(TerrainData data, GeoFrame frame, HexFile hexes, string path, MapRuntimeAssets assets)
        {
            var names = new List<string>(Biome.Keys);
            var layers = new TerrainLayer[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                var tex = assets.Own(new Texture2D(1, 1) { name = "Biome_" + names[i] });
                tex.SetPixel(0, 0, Biome[names[i]]); tex.Apply();
                layers[i] = assets.Own(new TerrainLayer { name = names[i], diffuseTexture = tex, tileSize = new Vector2(BiomeLayerTileSizeM, BiomeLayerTileSizeM) });
            }
            data.terrainLayers = layers;
            int res = AlphamapResolution; data.alphamapResolution = res;

            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                int idx = names.IndexOf(h.biome);
                if (idx < 0) throw new InvalidDataException($"{path}: unknown biome '{h.biome}' at q={h.q} r={h.r}");
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layerOf.Add(idx);
            }
            var blender = new BiomeBlender(centres, layerOf, hexes.hex_size_km * 1000f, names.Count);
            var a = new float[res, res, names.Count]; var wts = new float[names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    blender.Weights(new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM), wts);
                    for (int l = 0; l < wts.Length; l++) a[y, x, l] = wts[l];
                }
            data.SetAlphamaps(0, 0, a);
        }

        /// <summary>Line mesh of every land hex outline. Each edge is split into OverlaySegmentsPerEdge sub-segments whose
        /// vertices are draped OverlayClearanceM above the terrain; vertices are clamped to the terrain bounds.</summary>
        static GameObject BuildHexOverlay(GeoFrame frame, HexFile hexes, TerrainData data, Material material, MapRuntimeAssets assets)
        {
            float size = hexes.hex_size_km * 1000f;
            int n = 6 * OverlaySegmentsPerEdge;
            var verts = new List<Vector3>(); var idx = new List<int>();
            var corners = new Vector2[6];
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                var c = frame.ToWorld(h.lon, h.lat, 0);
                for (int i = 0; i < 6; i++) corners[i] = HexMath.Corner(new Vector2(c.x, c.z), size, i);
                int start = verts.Count;
                for (int i = 0; i < 6; i++)
                    for (int s = 0; s < OverlaySegmentsPerEdge; s++)
                    {
                        var k = Vector2.Lerp(corners[i], corners[(i + 1) % 6], (float)s / OverlaySegmentsPerEdge);
                        k.x = Mathf.Clamp(k.x, 0f, frame.WidthM); k.y = Mathf.Clamp(k.y, 0f, frame.LengthM);
                        float y = data.GetInterpolatedHeight(k.x / frame.WidthM, k.y / frame.LengthM) + OverlayClearanceM;
                        verts.Add(new Vector3(k.x, y, k.y));
                    }
                for (int i = 0; i < n; i++) { idx.Add(start + i); idx.Add(start + (i + 1) % n); }
            }
            var mesh = assets.Own(new Mesh { name = "HexOverlay", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 });
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
