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
        public const int BiomeTextureResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
        /// <summary>Terrain.basemapDistance: farther than the camera ever is, so the terrain is always drawn by the Statecraft
        /// graph, never by its low-resolution baked basemap (Unity's default switches at 1 km).</summary>
        public const float NoBasemapDistanceM = 1e7f;
        public const float BiomeLayerTileSizeM = 1000f; // layers are flat colours, so the tiling only needs to be finite
        public const int OverlaySegmentsPerEdge = 10;  // ~500 m sub-segments on a 5 km hex edge, each draped on the terrain
        // ~31 m steps, finer than the heightmap spacing on both axes: 78.9 m E-W x 210.6 m N-S
        // (heightmap.json: 2049 samples, so 2048 intervals, over width_km 161.53 x length_km 431.22).
        public const int OverlaySamplesPerSegment = 16;
        /// <summary>Terrain LOD screen-space error in pixels. Unity's default (5) gives distant cameras (the top-down
        /// capture, the 90 km flythrough) a coarse surface that rises above the draped hex outlines in valleys (K2).</summary>
        public const float TerrainPixelError = 1f;

        /// <summary>The muted land tone of neighbouring land past the map's edge (BorderLand.mat).</summary>
        public static readonly Color BorderLandColour = new Color(0.60f, 0.56f, 0.46f);

        /// <summary>Biome colours of the terrain layers (the minimap uses the same ones).</summary>
        public static IReadOnlyDictionary<string, Color> BiomeColours => Biome;

        static readonly Dictionary<string, Color> Biome = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.16f, 0.42f, 0.55f), ["lake"] = new Color(0.20f, 0.50f, 0.62f),
            ["coastal_plain"] = new Color(0.55f, 0.62f, 0.36f), ["hills"] = new Color(0.62f, 0.58f, 0.40f),
            ["mountains"] = new Color(0.58f, 0.52f, 0.42f), ["valley"] = new Color(0.50f, 0.56f, 0.38f),
            ["desert"] = new Color(0.80f, 0.68f, 0.48f),
        };

        /// <summary>Builds the terrain from the heightmap in <paramref name="mapDir"/> and the
        /// hexes in <paramref name="hexesJson"/>, both read with System.IO. The material is an editor-made asset (Assets/Materials/Terrain.mat) passed in by
        /// the caller: Shader.Find returns null in players (M0 correction 1).
        /// Runtime-created objects are owned by a <see cref="MapRuntimeAssets"/> on the returned GameObject and destroyed with it.</summary>
        public static GameObject Build(string mapDir, string hexesJson, Material terrainMaterial)
        {
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            var (meta, heights) = HeightmapLoader.Load(mapDir);
            return Build(meta, heights, LoadHexes(hexesJson), terrainMaterial);
        }

        /// <summary>Builds the map from already-read inputs (HeightmapLoader.Load and LoadHexes over text and bytes), so a
        /// WebGL build needs no System.IO. Throws InvalidDataException, before creating any object, when the hexes' bounds
        /// differ from the heightmap's by more than <see cref="BoundsTolerance"/>.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial) =>
            Build(meta, heights, hexFile, terrainMaterial, null);

        /// <summary>As Build, with <paramref name="paintGround"/> given the baked biome colour texels (row 0 = south,
        /// <see cref="BiomeTextureResolution"/> square over the map) before they are uploaded, so the settlement kits can tint
        /// the ground of their footprints (art spike) at no cost per frame.</summary>
        public static GameObject Build(HeightmapMeta meta, float[,] heights, HexFile hexFile, Material terrainMaterial,
            Action<Color32[], int, GeoFrame> paintGround)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            if (hexFile == null) throw new ArgumentNullException(nameof(hexFile));
            if (terrainMaterial == null) throw new ArgumentNullException(nameof(terrainMaterial));
            CheckSameArea(meta, hexFile.bounds);
            float widthM = meta.width_km * 1000f, lengthM = meta.length_km * 1000f;
            float reliefM = (meta.elev_max_m - meta.elev_min_m) * VerticalExaggeration;
            var frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, widthM, lengthM);

            var data = new TerrainData { heightmapResolution = meta.width };
            data.size = new Vector3(widthM, reliefM, lengthM);
            data.SetHeights(0, 0, heights);
            Boot.DiagLog.BootPhase("terrain heights set");

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            var assets = terrainGo.AddComponent<MapRuntimeAssets>();
            assets.Own(data);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.heightmapPixelError = TerrainPixelError;
            terrain.basemapDistance = NoBasemapDistanceM;
            data.terrainLayers = new[] { NeutralLayer(assets) };   // one layer, one pass: the graph paints from the biome texture
            data.alphamapResolution = 16;
            MapShaderGlobals.Apply(terrain, meta, hexFile.hex_size_km * 1000f, BakeBiomeTexture(frame, hexFile, assets, paintGround), assets);
            Boot.DiagLog.BootPhase("biome texture baked");
            MapBorders.Apply(hexFile, assets);   // region borders: none drawn until a layer is given (MapBorders.Use)
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

        /// <summary>The single terrain layer the Terrain Lit target needs; the graph ignores its colour.</summary>
        static TerrainLayer NeutralLayer(MapRuntimeAssets assets)
        {
            var tex = assets.Own(new Texture2D(1, 1, TextureFormat.RGB24, false) { name = "TerrainLayerNeutral" }); // no alpha: no smoothness
            tex.SetPixel(0, 0, Color.grey); tex.Apply();
            return assets.Own(new TerrainLayer
            {
                name = "neutral", diffuseTexture = tex, tileSize = new Vector2(BiomeLayerTileSizeM, BiomeLayerTileSizeM),
                smoothness = 0f, metallic = 0f,
            });
        }

        /// <summary>The ground colour of every point, blended from the biomes of nearby hex centres (BiomeBlender) in the
        /// MapPalette.Ground colours, baked once into an sRGB texture over the whole map (texel row 0 = south) that the
        /// terrain shader samples as _ScBiomeTex. Owned by the map.</summary>
        static Texture2D BakeBiomeTexture(GeoFrame frame, HexFile hexes, MapRuntimeAssets assets, Action<Color32[], int, GeoFrame> paint)
        {
            var names = new List<string>(Biome.Keys);
            var colours = new Color[names.Count];
            for (int i = 0; i < names.Count; i++) colours[i] = MapPalette.Ground[names[i]];
            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layerOf.Add(names.IndexOf(h.biome));
            }
            var blender = new BiomeBlender(centres, layerOf, hexes.hex_size_km * 1000f, names.Count);
            int res = BiomeTextureResolution;
            var tex = assets.Own(new Texture2D(res, res, TextureFormat.RGBA32, true, false)
                { name = "BiomeColour", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear });
            var px = new Color32[res * res]; var wts = new float[names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    blender.Weights(new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM), wts);
                    var c = Color.clear;
                    for (int l = 0; l < wts.Length; l++) c += colours[l] * wts[l];
                    c.a = 1f;
                    px[y * res + x] = c;
                }
            paint?.Invoke(px, res, frame);
            tex.SetPixels32(px); tex.Apply(true, true);   // no CPU copy: nothing reads the pixels back (about 5.6 MB on the heap)
            return tex;
        }

        /// <summary>Distances (m) of the border skirt's rings from the map's edge: land holds the edge's height across the
        /// first rings and reaches sea level only at the last, beyond anything the camera can see (SkirtFall).</summary>
        public static readonly float[] SkirtRings = { 0f, 4000f, 20000f, 60000f, 600000f };
        /// <summary>Share of the fall to sea level reached at each ring (land samples only). Visual v1 Task 7: the edge's height
        /// is held for 20 km (the Jordanian plateau, about 900 m, continues past the edge instead of sagging at once) and
        /// eases to sea level by the last ring, beyond anything the camera sees.</summary>
        public static readonly float[] SkirtFall = { 0f, 0.05f, 0.15f, 0.45f, 1f };
        /// <summary>Half-width (m) of the along-edge average the land skirt holds past its inner ring (Task 7): holding each
        /// edge vertex's own height for 20 km drew its ridges and gullies out as long stripes across the skirt, so the rings
        /// past the edge hold the edge's broad height (the plateau) and only ring 0 follows the terrain's edge exactly.</summary>
        public const float SkirtHoldSmoothingM = 8000f;
        /// <summary>A heightmap sample this close to 0 m is sea (tools/dem_to_heightmap.py writes open sea as exactly 0 m).</summary>
        public const float SeaToleranceM = 0.5f;

        /// <summary>The land and sea past the map's edge (fix round 2, R2-1 and N1). Each edge sample continues outward: a sea
        /// sample stays flat at sea level (submesh 0, Sea.mat: the unlit Statecraft/Sea, shaded by the same ScWaterShade as the
        /// terrain's own water, so no seam shows); a land sample meets the terrain's edge at ring 0, then holds the along-edge
        /// mean of the edge's height (SkirtHoldSmoothingM) and eases to sea level by the last ring (SkirtFall; submesh 1, a
        /// muted land tone), so Sinai, Jordan and Lebanon read as land. A segment with a land end is land. Corner fans close the four corners. Every triangle lies outside the
        /// terrain's footprint, so nothing covers the map (the old full sea plane hid all land below about -10 m).</summary>
        public static Mesh BuildBorderSkirt(TerrainData data, float elevMinM, MapRuntimeAssets assets)
        {
            var size = data.size;
            float seaY = (0f - elevMinM) * VerticalExaggeration;
            // One spoke per terrain vertex along each edge (the heightmap's own resolution), so the skirt's inner edge follows
            // the terrain's edge exactly; sampling every second vertex left slivers up to 36 m (fix round 3, m-4).
            int samples = data.heightmapResolution - 1;
            var verts = new List<Vector3>(); var sea = new List<int>(); var land = new List<int>();
            bool IsLand(float h) => Mathf.Abs(h / VerticalExaggeration + elevMinM) > SeaToleranceM;
            // The edge's heights and their running mean over +-SkirtHoldSmoothingM of the edge's own samples (prefix sums; the
            // window is cut short at the edge's ends). Every sample counts, so no ridge between taps aliases (fix round 1, M4).
            float[] HeldAlong(Vector2 a, Vector2 b, float[] h)
            {
                for (int i = 0; i <= samples; i++) h[i] = Height(Vector2.Lerp(a, b, (float)i / samples));
                var sum = new double[samples + 2];
                for (int i = 0; i <= samples; i++) sum[i + 1] = sum[i] + h[i];
                int w = Mathf.Max(1, Mathf.RoundToInt(SkirtHoldSmoothingM / ((b - a).magnitude / samples)));
                var held = new float[samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    int lo = Mathf.Max(0, i - w), hi = Mathf.Min(samples, i + w);
                    held[i] = (float)((sum[hi + 1] - sum[lo]) / (hi - lo + 1));
                }
                return held;
            }
            // One ring of vertices outward from an edge point (h its height, held the height the rings past it hold); returns
            // the index of its first vertex.
            int Spoke(Vector2 p, float h, float held, Vector2 outward, bool isLand, Vector2? outward2 = null)
            {
                int first = verts.Count;
                for (int k = 0; k < SkirtRings.Length; k++)
                {
                    var q = p + (outward + (outward2 ?? Vector2.zero)) * SkirtRings[k];
                    float y = isLand ? Mathf.Lerp(held, seaY, SkirtFall[k]) : seaY;
                    if (k == 0) y = h;                                     // meets the terrain's edge exactly
                    verts.Add(new Vector3(q.x, y, q.y));
                }
                return first;
            }
            void Quad(List<int> into, int a0, int b0)                   // the band between two spokes
            {
                for (int k = 0; k + 1 < SkirtRings.Length; k++)
                {
                    int a = a0 + k, a1 = a0 + k + 1, b = b0 + k, b1 = b0 + k + 1;
                    Tri(into, a, b, b1); Tri(into, a, b1, a1);
                }
            }
            void Tri(List<int> into, int i, int j, int l)               // wound to face up
            {
                var n = Vector3.Cross(verts[j] - verts[i], verts[l] - verts[i]);
                if (n.y < 0) { var t = j; j = l; l = t; }
                if (Mathf.Abs(n.y) > 1e-3f) { into.Add(i); into.Add(j); into.Add(l); }   // drops the degenerate corner centre
            }
            float Height(Vector2 p) => data.GetInterpolatedHeight(Mathf.Clamp01(p.x / size.x), Mathf.Clamp01(p.y / size.z));
            // The four edges, walked along: (start, end, outward).
            var edges = new (Vector2 a, Vector2 b, Vector2 n)[]
            {
                (new Vector2(0, 0), new Vector2(size.x, 0), Vector2.down),                 // south
                (new Vector2(size.x, 0), new Vector2(size.x, size.z), Vector2.right),       // east
                (new Vector2(size.x, size.z), new Vector2(0, size.z), Vector2.up),          // north
                (new Vector2(0, size.z), new Vector2(0, 0), Vector2.left),                  // west
            };
            var heights = new float[edges.Length][]; var helds = new float[edges.Length][];
            for (int e = 0; e < edges.Length; e++) { heights[e] = new float[samples + 1]; helds[e] = HeldAlong(edges[e].a, edges[e].b, heights[e]); }
            for (int e = 0; e < edges.Length; e++)
            {
                var (a, b, n) = edges[e];
                int prev = -1; bool prevLand = false;
                for (int i = 0; i <= samples; i++)
                {
                    var p = Vector2.Lerp(a, b, (float)i / samples);
                    bool isLand = IsLand(heights[e][i]);
                    int spoke = Spoke(p, heights[e][i], helds[e][i], n, isLand);
                    if (prev >= 0) Quad(prevLand || isLand ? land : sea, prev, spoke);
                    prev = spoke; prevLand = isLand;
                }
            }
            // Corners: a fan between the two edges' outward spokes and the diagonal, in the corner sample's kind.
            for (int e = 0; e < edges.Length; e++)
            {
                var corner = edges[e].b; var n1 = edges[e].n; var n2 = edges[(e + 1) % edges.Length].n;
                int e2 = (e + 1) % edges.Length;
                float h = heights[e][samples], held1 = helds[e][samples], held2 = helds[e2][0];   // the corner ends edge e, starts e2
                bool isLand = IsLand(h);
                int s1 = Spoke(corner, h, held1, n1, isLand), sd = Spoke(corner, h, 0.5f * (held1 + held2), n1, isLand, n2), s2 = Spoke(corner, h, held2, n2, isLand);
                Quad(isLand ? land : sea, s1, sd); Quad(isLand ? land : sea, sd, s2);
            }
            var mesh = assets.Own(new Mesh { name = "BorderSkirt", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 });
            mesh.SetVertices(verts);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(sea, 0); mesh.SetTriangles(land, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Lakes: flat water (a 3x3 height range within LakeFlatM) below LakeBelowM, tools/terrain_bake.py's rule.</summary>
        public const float LakeBelowM = -150f, LakeFlatM = 0.25f;
        /// <summary>The water mesh reaches this many heightmap samples past the last wet one: the shore's coverage ramp (about
        /// 70 m inland) and the coast texture's own resolution stay inside it.</summary>
        public const int WaterMeshReach = 2;
        /// <summary>The largest flat block the water mesh draws as one fan (heightmap cells a side).</summary>
        public const int WaterMeshBlock = 32;

        /// <summary>The surface the water inside the map is drawn on (issue #195; MapScene.TerrainWaterName): the terrain's own
        /// heightmap surface wherever the water's coverage can be above 0, so Sea.shader's terrain-water mode can add the water
        /// on top of the terrain instead of the terrain's own shader carrying it. Wet samples are the sea (within SeaToleranceM
        /// of 0 m) and flat rift lakes (LakeBelowM, LakeFlatM); every cell within WaterMeshReach samples of one is drawn at
        /// the heightmap's full resolution, except square blocks whose samples all share one height (the open sea, the lakes'
        /// flat middles), drawn as fans from the centre to every sample on their edge, so no edge has a T-junction.
        /// <paramref name="heights"/> are the ones the terrain was given (Unity's [y, x], 0..1): the terrain stores them at
        /// 16 bits, a few centimetres off, well inside the water's depth pull (Sea.shader).</summary>
        public static Mesh BuildWaterMesh(TerrainData data, float[,] heights, float elevMinM, MapRuntimeAssets assets)
        {
            int res = data.heightmapResolution, cells = res - 1;
            var size = data.size;
            // Thresholds on the 0..1 heights (one sample is h * size.y / VerticalExaggeration + elevMinM metres): the scan
            // below runs over every sample (4.2 million), so it stays a compare (boot time, issue #195).
            float perMetre = VerticalExaggeration / size.y, zero = -elevMinM * perMetre;
            float seaLo = zero - SeaToleranceM * perMetre, seaHi = zero + SeaToleranceM * perMetre;
            float lakeBelow = (LakeBelowM - elevMinM) * perMetre, lakeFlat = LakeFlatM * perMetre;
            // A sample is wet when it is sea, or the flat floor of a rift lake (its 3x3 neighbourhood within LakeFlatM).
            bool Wet(int x, int y)
            {
                float h = heights[y, x];
                if (h > seaLo && h < seaHi) return true;
                if (h >= lakeBelow) return false;
                float lo = h, hi = h;
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        float n = heights[Mathf.Clamp(y + oy, 0, cells), Mathf.Clamp(x + ox, 0, cells)];
                        lo = Mathf.Min(lo, n); hi = Mathf.Max(hi, n);
                    }
                return hi - lo <= lakeFlat;
            }
            // One pass over every sample, with no per-sample array (on WebGL a 4 MB allocation at boot costs more than the
            // scan): which WaterMeshBlock-square blocks hold a wet sample. Only blocks with a wet sample in or beside them
            // are looked at again (a cell is drawn when a wet sample lies within WaterMeshReach of one of its corners, and
            // the reach is under a block), so the land away from the water costs a compare or two a sample (boot time,
            // issue #195).
            const int B = WaterMeshBlock, R = WaterMeshReach;
            int blocks = (cells + B - 1) / B;
            var blockWet = new bool[blocks * blocks];
            // A row at a time (WebGL: the browser optimises a function after its first calls, not inside one long loop).
            void ScanRow(int y)
            {
                int by = Mathf.Min(y / B, blocks - 1) * blocks;
                for (int x = 0; x < res; x++)
                {
                    float h = heights[y, x];
                    if ((h > seaLo && h < seaHi) || (h < lakeBelow && Wet(x, y))) blockWet[by + Mathf.Min(x / B, blocks - 1)] = true;
                }
            }
            for (int y = 0; y < res; y++) ScanRow(y);
            bool Relevant(int bx, int by)
            {
                for (int j = Mathf.Max(0, by - 1); j <= Mathf.Min(blocks - 1, by + 1); j++)
                    for (int i = Mathf.Max(0, bx - 1); i <= Mathf.Min(blocks - 1, bx + 1); i++)
                        if (blockWet[j * blocks + i]) return true;
                return false;
            }

            var verts = new List<Vector3>(1 << 17); var tris = new List<int>(3 << 18);
            float stepX = size.x / cells, stepZ = size.z / cells;
            // Each sample's vertex is made once: samples on the blocks' grid lines are indexed globally, a block's inner
            // samples in a block-local table (no dictionary: boot time).
            var onColumn = new int[(blocks + 1) * res]; var onRow = new int[(blocks + 1) * res];
            var inner = new int[(B - 1) * (B - 1)];
            for (int i = 0; i < onColumn.Length; i++) { onColumn[i] = -1; onRow[i] = -1; }
            int bx0 = 0, by0 = 0;
            int Vertex(int x, int y)
            {
                int[] table; int at;
                if (x % B == 0) { table = onColumn; at = x / B * res + y; }
                else if (y % B == 0) { table = onRow; at = y / B * res + x; }
                else { table = inner; at = (y - by0 - 1) * (B - 1) + (x - bx0 - 1); }
                if (table[at] < 0) { table[at] = verts.Count; verts.Add(new Vector3(x * stepX, heights[y, x] * size.y, y * stepZ)); }
                return table[at];
            }
            // Triangles face up: every cell and fan below is listed anticlockwise seen from above (x east, z north), which
            // Unity's clockwise front faces take in the reverse order.
            void Tri(int i, int j, int l) { tris.Add(i); tris.Add(l); tris.Add(j); }
            // Whether block (x0, y0, s) is all wet and all its samples share one height (bitwise: the sea's 0 m, a lake's
            // quantised level).
            bool Flat(int x0, int y0, int s)
            {
                float h = heights[y0, x0];
                for (int y = y0; y <= y0 + s; y++)
                    for (int x = x0; x <= x0 + s; x++)
                    {
                        float v = heights[y, x];
                        if (v != h || !((v > seaLo && v < seaHi) || (v < lakeBelow && Wet(x, y)))) return false;
                    }
                return true;
            }
            // Within the top-level block being drawn: near[] says, per sample of the block (B + 1 a side), whether a wet
            // sample lies within R: the wetness of the block plus R around it, then a separable dilation (counts of wet
            // samples in a sliding window of 2 R + 1, along x, then along y).
            int side = B + 1, span = side + 2 * R;
            var near = new bool[side * side]; var localWet = new byte[span * span]; var rowNear = new byte[span * side];
            int nx0 = 0, ny0 = 0;
            void Dilate(int x0, int y0)
            {
                nx0 = x0; ny0 = y0;
                for (int j = 0; j < span; j++)
                    for (int i = 0; i < span; i++)
                    {
                        int x = x0 - R + i, y = y0 - R + j;
                        bool w = false;
                        if (x >= 0 && y >= 0 && x < res && y < res)
                        {
                            float h = heights[y, x];                           // the sea test inline: a call costs (boot time)
                            w = (h > seaLo && h < seaHi) || (h < lakeBelow && Wet(x, y));
                        }
                        localWet[j * span + i] = (byte)(w ? 1 : 0);
                    }
                for (int j = 0; j < span; j++)                              // along x: sample i of the block is at i + R
                {
                    int o = j * span, count = 0;
                    for (int k = 0; k < 2 * R; k++) count += localWet[o + k];
                    for (int i = 0; i < side; i++)
                    {
                        count += localWet[o + i + 2 * R];
                        rowNear[j * side + i] = (byte)(count > 0 ? 1 : 0);
                        count -= localWet[o + i];
                    }
                }
                for (int i = 0; i < side; i++)                              // then along y
                {
                    int count = 0;
                    for (int k = 0; k < 2 * R; k++) count += rowNear[k * side + i];
                    for (int j = 0; j < side; j++)
                    {
                        count += rowNear[(j + 2 * R) * side + i];
                        near[j * side + i] = count > 0;
                        count -= rowNear[j * side + i];
                    }
                }
            }
            bool Near(int x, int y) => near[(y - ny0) * side + (x - nx0)];
            bool Drawn(int cx, int cy) => Near(cx, cy) || Near(cx + 1, cy) || Near(cx, cy + 1) || Near(cx + 1, cy + 1);
            bool AnyDrawn(int x0, int y0, int s)
            {
                for (int y = y0; y < y0 + s; y++)
                    for (int x = x0; x < x0 + s; x++)
                        if (Drawn(x, y)) return true;
                return false;
            }
            void Fan(int x0, int y0, int s)                             // from the centre to every sample on the edge: 4 s triangles
            {
                int c = verts.Count;
                verts.Add(new Vector3((x0 + 0.5f * s) * stepX, heights[y0, x0] * size.y, (y0 + 0.5f * s) * stepZ));
                var ring = new List<int>(4 * s);
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + i, y0));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + s, y0 + i));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0 + s - i, y0 + s));
                for (int i = 0; i < s; i++) ring.Add(Vertex(x0, y0 + s - i));
                for (int i = 0; i < ring.Count; i++) Tri(c, ring[i], ring[(i + 1) % ring.Count]);
            }
            void Block(int x0, int y0, int s)                           // inside a dilated top-level block
            {
                if (x0 >= cells || y0 >= cells || !AnyDrawn(x0, y0, Mathf.Min(s, Mathf.Min(cells - x0, cells - y0)))) return;
                if (x0 + s <= cells && y0 + s <= cells && s > 1 && Flat(x0, y0, s)) { Fan(x0, y0, s); return; }
                if (s > 1)
                {
                    int h = s / 2;
                    Block(x0, y0, h); Block(x0 + h, y0, h); Block(x0, y0 + h, h); Block(x0 + h, y0 + h, h);
                    return;
                }
                if (!Drawn(x0, y0)) return;
                int a = Vertex(x0, y0), b = Vertex(x0 + 1, y0), d = Vertex(x0, y0 + 1), e = Vertex(x0 + 1, y0 + 1);
                Tri(a, b, e); Tri(a, e, d);
            }
            for (int by = 0; by < blocks; by++)
                for (int bx = 0; bx < blocks; bx++)
                {
                    if (!Relevant(bx, by)) continue;
                    int x0 = bx * B, y0 = by * B;
                    bx0 = x0; by0 = y0;
                    for (int i = 0; i < inner.Length; i++) inner[i] = -1;
                    if (x0 + B <= cells && y0 + B <= cells && Flat(x0, y0, B)) { Fan(x0, y0, B); continue; }
                    Dilate(x0, y0);
                    Block(x0, y0, B);
                }
            var mesh = assets.Own(new Mesh { name = "TerrainWater", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 });
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            if (!Application.isEditor) mesh.UploadMeshData(true);   // players keep only the GPU copy (about 4.5 MB less heap)
            return mesh;
        }

        /// <summary>Vertices of one draped hex ring (<see cref="DrapedRing"/>).</summary>
        public const int RingVertices = 6 * OverlaySegmentsPerEdge;

        /// <summary>Appends the <see cref="RingVertices"/> vertices of a hex outline of circumradius <paramref name="size"/>
        /// around <paramref name="centre"/> (world x, z), corners from HexMath. Each edge is split into OverlaySegmentsPerEdge
        /// sub-segments, clamped to the terrain bounds; each vertex sits <paramref name="clearance"/> above the highest
        /// terrain sampled along its two adjacent sub-segments, so the straight line between two vertices clears any ridge.
        /// The selection outline uses it (the hex grid itself is drawn by the shaders since visual v1 Task 8).</summary>
        public static void DrapedRing(GeoFrame frame, TerrainData data, Vector2 centre, float size, float clearance, List<Vector3> verts)
        {
            int n = RingVertices;
            var corners = new Vector2[6]; var ground = new Vector2[n]; var segMax = new float[n];
            for (int i = 0; i < 6; i++) corners[i] = HexMath.Corner(centre, size, i);
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
            for (int j = 0; j < n; j++)
                verts.Add(new Vector3(ground[j].x, Mathf.Max(segMax[(j + n - 1) % n], segMax[j]) + clearance, ground[j].y));
        }
    }
}
