using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    [Serializable] public sealed class HexJson { public int q, r; public double lon, lat; public float elev_m; public bool land; public string biome; }
    [Serializable] public sealed class HexBounds { public double lon0, lon1, lat0, lat1; }
    [Serializable] public sealed class HexFile { public HexBounds bounds; public float hex_size_km; public string layout; public HexJson[] hexes; }

    public static class MapBuilder
    {
        public const float VerticalExaggeration = 3f; // diorama look; real relief would read flat at 400 km scale
        public const int AlphamapResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
        public const float BiomeLayerTileSizeM = 1000f; // layers are flat colours, so the tiling only needs to be finite
        public const int OverlaySegmentsPerEdge = 10;  // ~500 m sub-segments on a 5 km hex edge, each draped on the terrain
        public const float OverlayClearanceM = 40f * VerticalExaggeration; // above the (exaggerated) terrain surface
        // ~31 m steps, finer than the heightmap spacing on both axes: 78.9 m E-W x 210.6 m N-S
        // (heightmap.json: 2049 samples, so 2048 intervals, over width_km 161.53 x length_km 431.22).
        public const int OverlaySamplesPerSegment = 16;
        /// <summary>Terrain LOD screen-space error in pixels. Unity's default (5) gives distant cameras (the top-down
        /// capture, the 90 km flythrough) a coarse surface that rises above the draped hex outlines in valleys (K2).</summary>
        public const float TerrainPixelError = 1f;

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        /// <summary>Builds the terrain (with a "HexOverlay" child) from the heightmap in <paramref name="mapDir"/> and the
        /// hexes in <paramref name="hexesJson"/>, both read with System.IO. Materials are editor-made assets (Assets/Materials/Terrain.mat,
        /// HexOverlay.mat) passed in by the caller: Shader.Find returns null in players (M0 correction 1).
        /// Runtime-created objects are owned by a <see cref="MapRuntimeAssets"/> on the returned GameObject and destroyed with it.</summary>
        public static GameObject Build(string mapDir, string hexesJson, Material terrainMaterial, Material overlayMaterial)
        {
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            if (overlayMaterial == null) throw new ArgumentNullException(nameof(overlayMaterial));
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            return Build(meta, heights, LoadHexes(hexesJson), terrainMaterial, overlayMaterial);
        }

        /// <summary>Builds the map from already-read inputs (HeightmapLoader.Load and LoadHexes over text and bytes), so a
        /// WebGL build needs no System.IO. Throws InvalidDataException, before creating any object, when the hexes' bounds
        /// differ from the heightmap's by more than <see cref="BoundsTolerance"/>.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial, Material overlayMaterial)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            if (hexFile == null) throw new ArgumentNullException(nameof(hexFile));
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            if (overlayMaterial == null) throw new ArgumentNullException(nameof(overlayMaterial));
            CheckSameArea(meta, hexFile.bounds);
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
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.heightmapPixelError = TerrainPixelError;
            ApplyBiomeColours(data, frame, hexFile, assets);

            var overlay = BuildHexOverlay(frame, hexFile, data, overlayMaterial, assets);
            overlay.transform.SetParent(terrainGo.transform, false);
            return terrainGo;
        }

        /// <summary>Reads and validates hexes.json (non-empty, hex_size_km > 0, pointy layout, known biomes); problems throw
        /// InvalidDataException naming the file. Build calls this before creating any object.</summary>
        public static HexFile LoadHexes(string path) => LoadHexes(File.ReadAllText(path), path);

        /// <summary>Parses already-read hexes.json text; errors name <paramref name="path"/>, as the path overload does.</summary>
        public static HexFile LoadHexes(string text, string path)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException($"{path}: empty file");
            HexFile f;
            try { f = JsonUtility.FromJson<HexFile>(text); }
            catch (ArgumentException e) { throw new InvalidDataException($"{path}: malformed JSON ({e.Message})", e); }
            if (f == null) throw new InvalidDataException($"{path}: empty file");
            if (f.hexes == null || f.hexes.Length == 0) throw new InvalidDataException($"{path}: no hexes");
            if (!(f.hex_size_km > 0)) throw new InvalidDataException($"{path}: hex_size_km {f.hex_size_km} must be > 0");
            if (f.layout != "pointy") throw new InvalidDataException($"{path}: layout '{f.layout}' unsupported (expected 'pointy')");
            foreach (var h in f.hexes)
                if (h == null || h.biome == null || !Biome.ContainsKey(h.biome))
                    throw new InvalidDataException($"{path}: unknown biome '{h?.biome}' at q={h?.q} r={h?.r}");
            return f;
        }

        public const double BoundsTolerance = 1e-9; // degrees

        /// <summary>hexes.json and the heightmap must describe the same lon/lat rectangle, or hexes and settlements land
        /// in the wrong place on the terrain.</summary>
        static void CheckSameArea(HeightmapMeta meta, HexBounds b)
        {
            if (b == null || Math.Abs(b.lon0 - meta.lon0) > BoundsTolerance || Math.Abs(b.lon1 - meta.lon1) > BoundsTolerance
                || Math.Abs(b.lat0 - meta.lat0) > BoundsTolerance || Math.Abs(b.lat1 - meta.lat1) > BoundsTolerance)
                throw new InvalidDataException(FormattableString.Invariant(
                    $"hexes.json bounds (lon {b?.lon0}..{b?.lon1}, lat {b?.lat0}..{b?.lat1}) differ from heightmap.json (lon {meta.lon0}..{meta.lon1}, lat {meta.lat0}..{meta.lat1}) by more than {BoundsTolerance}"));
        }

        /// <summary>One terrain layer per biome; each alphamap texel blends the biomes of nearby hex centres (BiomeBlender).</summary>
        static void ApplyBiomeColours(TerrainData data, GeoFrame frame, HexFile hexes, MapRuntimeAssets assets)
        {
            var names = new List<string>(Biome.Keys);
            var layers = new TerrainLayer[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                var tex = assets.Own(new Texture2D(1, 1, TextureFormat.RGB24, false) { name = "Biome_" + names[i] }); // no alpha: Terrain/Lit would read it as smoothness 1
                tex.SetPixel(0, 0, Biome[names[i]]); tex.Apply();
                layers[i] = assets.Own(new TerrainLayer
                {
                    name = names[i], diffuseTexture = tex, tileSize = new Vector2(BiomeLayerTileSizeM, BiomeLayerTileSizeM),
                    smoothness = 0f, metallic = 0f, // matte: URP Terrain/Lit's default 0.5 smoothness gives white streaks on slopes
                });
            }
            data.terrainLayers = layers;
            int res = AlphamapResolution; data.alphamapResolution = res;

            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                int idx = names.IndexOf(h.biome); // validated in LoadHexes, before any object exists
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

        /// <summary>Line mesh of every land hex outline. Each edge is split into OverlaySegmentsPerEdge sub-segments; vertices
        /// are clamped to the terrain bounds. Each vertex sits OverlayClearanceM above the highest terrain sampled along its
        /// two adjacent sub-segments, so the straight line between two vertices clears any ridge between them.</summary>
        static GameObject BuildHexOverlay(GeoFrame frame, HexFile hexes, TerrainData data, Material material, MapRuntimeAssets assets)
        {
            float size = hexes.hex_size_km * 1000f;
            int n = 6 * OverlaySegmentsPerEdge;
            var verts = new List<Vector3>(); var idx = new List<int>();
            var corners = new Vector2[6]; var ground = new Vector2[n]; var segMax = new float[n];
            foreach (var h in hexes.hexes)
            {
                if (!h.land) continue;
                var c = frame.ToWorld(h.lon, h.lat, 0);
                for (int i = 0; i < 6; i++) corners[i] = HexMath.Corner(new Vector2(c.x, c.z), size, i);
                for (int i = 0; i < 6; i++)
                    for (int s = 0; s < OverlaySegmentsPerEdge; s++)
                    {
                        var k = Vector2.Lerp(corners[i], corners[(i + 1) % 6], (float)s / OverlaySegmentsPerEdge);
                        k.x = Mathf.Clamp(k.x, 0f, frame.WidthM); k.y = Mathf.Clamp(k.y, 0f, frame.LengthM);
                        ground[i * OverlaySegmentsPerEdge + s] = k;
                    }
                for (int j = 0; j < n; j++)
                {
                    float m = float.MinValue;
                    for (int t = 0; t <= OverlaySamplesPerSegment; t++)
                    {
                        var p = Vector2.Lerp(ground[j], ground[(j + 1) % n], (float)t / OverlaySamplesPerSegment);
                        m = Mathf.Max(m, data.GetInterpolatedHeight(p.x / frame.WidthM, p.y / frame.LengthM));
                    }
                    segMax[j] = m;
                }
                int start = verts.Count;
                for (int j = 0; j < n; j++)
                    verts.Add(new Vector3(ground[j].x, Mathf.Max(segMax[(j + n - 1) % n], segMax[j]) + OverlayClearanceM, ground[j].y));
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
