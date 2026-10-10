using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

public class MapTests
{
    [Test]
    public void HeightmapRowZeroIsNorth_SoUnityRowZeroIsSouth()
    {
        // 3x3 raster: the north row is all 65535 and the rest 0. After loading, Unity row 2 (north) must be 1.
        var raw = new ushort[] { 65535, 65535, 65535, 0, 0, 0, 0, 0, 0 };
        var h = HeightmapLoader.ToUnityHeights(raw, 3);
        Assert.AreEqual(1f, h[2, 0], 1e-6); Assert.AreEqual(0f, h[0, 0], 1e-6);
    }

    [Test]
    public void GeoFrame_MapsCornersAndJerusalemEastOfTelAviv()
    {
        var f = new GeoFrame(34.2, 35.9, 29.45, 33.35, 161000f, 433000f);
        Assert.AreEqual(0f, f.ToWorld(34.2, 29.45, 0).x, 1e-3);
        Assert.AreEqual(433000f, f.ToWorld(35.9, 33.35, 0).z, 1);
        var telAviv = f.ToWorld(34.78, 32.08, 0); var jerusalem = f.ToWorld(35.22, 31.78, 0);
        Assert.Greater(jerusalem.x, telAviv.x);   // east
        Assert.Less(jerusalem.z, telAviv.z);      // south
    }

    [Test]
    public void PointyTopCornerZeroIsAtMinus30Degrees()
    {
        var c = HexMath.Corner(Vector2.zero, 1f, 0); // Red Blob: angle = 60*i - 30 degrees
        Assert.AreEqual(Mathf.Cos(-30 * Mathf.Deg2Rad), c.x, 1e-5);
        Assert.AreEqual(Mathf.Sin(-30 * Mathf.Deg2Rad), c.y, 1e-5);
    }

    [Test]
    public void TruncatedHeightmapIsRejectedWithExpectedSize()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hm_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "heightmap.json"),
            "{\"width\":3,\"height\":3,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367,\"width_km\":161.5,\"length_km\":431.2,\"attribution\":\"x\"}");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "heightmap.r16"), new byte[10]); // 3x3 needs 18 bytes
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => HeightmapLoader.Load(dir));
        StringAssert.Contains("expected 18", ex.Message);
        var metaPath = System.IO.Path.Combine(dir, "heightmap.json");
        var fromBytes = Assert.Throws<System.IO.InvalidDataException>(() => HeightmapLoader.Load(System.IO.File.ReadAllText(metaPath), new byte[10], metaPath));
        Assert.AreEqual(ex.Message, fromBytes.Message);
    }

    // M0 correction 1: no Shader.Find at runtime. Materials are editor-made assets passed into Build.
    const string TerrainMatPath = "Assets/Materials/Terrain.mat", OverlayMatPath = "Assets/Materials/HexOverlay.mat";

    static string HexesJson => System.IO.Path.Combine(Paths.Content, "map", "hexes.json");

    static (Material terrain, Material overlay) Materials()
    {
        var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        var overlayMat = AssetDatabase.LoadAssetAtPath<Material>(OverlayMatPath);
        Assert.IsNotNull(terrainMat, TerrainMatPath); Assert.IsNotNull(overlayMat, OverlayMatPath);
        return (terrainMat, overlayMat);
    }

    [Test]
    public void BuildUsesThePassedTerrainAndOverlayMaterials()
    {
        var (terrainMat, overlayMat) = Materials();
        Assert.AreEqual("Statecraft/StatecraftTerrain", terrainMat.shader.name);
        Assert.AreEqual("Universal Render Pipeline/Unlit", overlayMat.shader.name);

        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        try
        {
            var terrain = go.GetComponent<Terrain>();
            Assert.AreSame(terrainMat, terrain.materialTemplate);
            var overlay = go.transform.Find("HexOverlay");
            Assert.IsNotNull(overlay, "HexOverlay child");
            Assert.AreSame(overlayMat, overlay.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreEqual(2049, terrain.terrainData.heightmapResolution);
            Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length);
            Assert.Greater(overlay.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void BuildRejectsMissingMaterials()
    {
        Assert.Throws<System.ArgumentNullException>(() => MapBuilder.Build(Paths.StreamingMap, HexesJson, null, null));
    }

    [Test]
    public void BiomeLayersAreMatte()
    {
        var (terrainMat, overlayMat) = Materials();
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        try
        {
            var layers = go.GetComponent<Terrain>().terrainData.terrainLayers;
            Assert.IsNotEmpty(layers);
            foreach (var l in layers)
            {
                Assert.AreEqual(0f, l.smoothness, $"{l.name} smoothness"); Assert.AreEqual(0f, l.metallic, $"{l.name} metallic");
                // URP Terrain/Lit uses diffuse alpha as smoothness and forces _SmoothnessN to 1 when the diffuse has alpha.
                Assert.IsFalse(UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(l.diffuseTexture.graphicsFormat),
                    $"{l.name} diffuse must have no alpha channel");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    // --- Overlay drape (M1, L6) ---

    [Test]
    public void EveryOverlayVertexLiesAboveTheTerrainAndInsideItsBounds()
    {
        var (terrainMat, overlayMat) = Materials();
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        try
        {
            var data = go.GetComponent<Terrain>().terrainData;
            var mesh = go.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh;
            var verts = mesh.vertices;
            int land = 0; foreach (var h in MapBuilder.LoadHexes(HexesJson).hexes) if (h.land) land++;
            Assert.AreEqual(land * 6 * MapBuilder.OverlaySegmentsPerEdge, verts.Length);
            int below = 0, outside = 0;
            foreach (var v in verts)
            {
                if (v.x < 0 || v.x > data.size.x || v.z < 0 || v.z > data.size.z) { outside++; continue; }
                if (v.y <= data.GetInterpolatedHeight(v.x / data.size.x, v.z / data.size.z)) below++;
            }
            Assert.AreEqual(0, outside, "vertices outside the terrain bounds");
            Assert.AreEqual(0, below, "vertices at or below the terrain");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // k/7 and k/13 share no interior point with the builder's t/16 sampling grid, so the test does not mirror it.
    static readonly float[] SegmentProbes = Probes();
    static float[] Probes()
    {
        var l = new System.Collections.Generic.List<float>();
        for (int k = 1; k < 7; k++) l.Add(k / 7f);
        for (int k = 1; k < 13; k++) l.Add(k / 13f);
        return l.ToArray();
    }

    [Test]
    public void EveryPointAlongEveryOverlaySegmentLiesAboveTheTerrain()
    {
        // Vertices alone are not enough: a ridge between two draped vertices hides the straight line joining them (K2).
        var (terrainMat, overlayMat) = Materials();
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        try
        {
            var data = go.GetComponent<Terrain>().terrainData;
            var mesh = go.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh;
            var verts = mesh.vertices; var idx = mesh.GetIndices(0);
            int hidden = 0; float worst = 0f;
            for (int i = 0; i < idx.Length; i += 2)
                foreach (var t in SegmentProbes)
                {
                    var p = Vector3.Lerp(verts[idx[i]], verts[idx[i + 1]], t);
                    float deficit = data.GetInterpolatedHeight(p.x / data.size.x, p.z / data.size.z) - p.y;
                    if (deficit >= 0f) hidden++;
                    worst = Mathf.Max(worst, deficit);
                }
            Assert.AreEqual(0, hidden, $"points along overlay segments at or below the terrain (worst {worst:F0} m below)");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TerrainUsesFineLodSoTheCoarseSurfaceDoesNotCoverTheOverlay()
    {
        // At the default heightmapPixelError (5) far cameras get a coarse LOD whose surface sits above the draped lines (K2).
        var (terrainMat, overlayMat) = Materials();
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        try { Assert.AreEqual(MapBuilder.TerrainPixelError, go.GetComponent<Terrain>().heightmapPixelError); Assert.AreEqual(1f, MapBuilder.TerrainPixelError); }
        finally { Object.DestroyImmediate(go); }
    }

    // --- Runtime-created objects are owned and destroyed with the map (L5) ---

    [Test]
    public void DestroyingTheMapDestroysItsRuntimeAssets()
    {
        var (terrainMat, overlayMat) = Materials();
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        var data = go.GetComponent<Terrain>().terrainData;
        var owned = new System.Collections.Generic.List<Object> { data, go.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh };
        foreach (var l in data.terrainLayers) { owned.Add(l); owned.Add(l.diffuseTexture); }
        Object.DestroyImmediate(go);
        foreach (var o in owned) Assert.IsTrue(o == null, $"{o} survived the map");
        Assert.IsTrue(terrainMat != null && overlayMat != null, "material assets must not be destroyed");
    }

    // --- Biome blending (C2) ---

    const float S = 5000f; // hex size (circumradius) in metres
    static readonly float W = Mathf.Sqrt(3f) * S; // centre spacing of neighbouring pointy-top hexes

    [Test]
    public void TexelAtAHexCentreIsDominatedByThatHexsBiome()
    {
        var b = new BiomeBlender(new[] { Vector2.zero, new Vector2(W, 0) }, new[] { 0, 1 }, S, 3);
        var w = new float[3];
        b.Weights(Vector2.zero, w);
        Assert.Greater(w[0], 0.9f);
        b.Weights(new Vector2(W, 0), w);
        Assert.Greater(w[1], 0.9f);
    }

    [Test]
    public void TexelMidwayBetweenTwoBiomesBlendsBoth()
    {
        var b = new BiomeBlender(new[] { Vector2.zero, new Vector2(W, 0) }, new[] { 0, 1 }, S, 3);
        var w = new float[3];
        b.Weights(new Vector2(W / 2, 0), w);
        Assert.That(w[0], Is.InRange(0.3f, 0.7f)); Assert.That(w[1], Is.InRange(0.3f, 0.7f));
        Assert.AreEqual(0f, w[2], 1e-6);
    }

    [Test]
    public void BlendBandIsAboutSixTenthsOfAHex()
    {
        // Along the line between two neighbouring centres, the band where both biomes have > 10% weight spans ~0.6 hex widths.
        var b = new BiomeBlender(new[] { Vector2.zero, new Vector2(W, 0) }, new[] { 0, 1 }, S, 2);
        var w = new float[2]; float lo = float.MaxValue, hi = float.MinValue;
        for (int i = 0; i <= 1000; i++)
        {
            float x = W * i / 1000f; b.Weights(new Vector2(x, 0), w);
            if (w[0] > 0.1f && w[1] > 0.1f) { lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x); }
        }
        Assert.That((hi - lo) / W, Is.InRange(0.55f, 0.65f));
    }

    [Test]
    public void BlendWeightsSumToOneEverywhere()
    {
        // A 7-hex flower with mixed biomes, sampled on a grid that also reaches outside it (fallback to nearest).
        var centres = new System.Collections.Generic.List<Vector2> { Vector2.zero };
        for (int i = 0; i < 6; i++) { float a = Mathf.Deg2Rad * 60f * i; centres.Add(new Vector2(W * Mathf.Cos(a), W * Mathf.Sin(a))); }
        var b = new BiomeBlender(centres, new[] { 0, 1, 2, 3, 1, 2, 0 }, S, 4);
        var w = new float[4];
        for (float x = -3 * S; x <= 3 * S; x += S / 7f)
            for (float y = -3 * S; y <= 3 * S; y += S / 7f)
            {
                b.Weights(new Vector2(x, y), w);
                float sum = 0; foreach (var v in w) { Assert.GreaterOrEqual(v, 0f); sum += v; }
                Assert.AreEqual(1f, sum, 1e-5, $"at {x},{y}");
            }
    }

    // --- Robustness (M2) ---

    static string TempFile(string name, string text)
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "map_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(dir);
        var p = System.IO.Path.Combine(dir, name); System.IO.File.WriteAllText(p, text); return p;
    }

    [TestCase("", "empty")]
    [TestCase("   ", "empty")]
    [TestCase("{\"hex_size_km\":5,\"layout\":\"pointy\"}", "no hexes")]
    [TestCase("{\"hex_size_km\":5,\"layout\":\"pointy\",\"hexes\":null}", "no hexes")]
    [TestCase("{\"hex_size_km\":5,\"layout\":\"pointy\",\"hexes\":[]}", "no hexes")]
    [TestCase("{\"hex_size_km\":0,\"layout\":\"pointy\",\"hexes\":[{\"q\":0,\"r\":0,\"biome\":\"sea\"}]}", "hex_size_km")]
    [TestCase("{\"hex_size_km\":-5,\"layout\":\"pointy\",\"hexes\":[{\"q\":0,\"r\":0,\"biome\":\"sea\"}]}", "hex_size_km")]
    [TestCase("{\"hex_size_km\":5,\"layout\":\"flat\",\"hexes\":[{\"q\":0,\"r\":0,\"biome\":\"sea\"}]}", "layout")]
    [TestCase("{\"hex_size_km\":5,\"layout\":\"pointy\",\"hexes\":[{\"q\":3,\"r\":4,\"biome\":\"jungle\"}]}", "unknown biome 'jungle' at q=3 r=4")]
    [TestCase("{\"hex_size_km\":5,\"layout\":", "malformed JSON")]
    [TestCase("not json at all", "malformed JSON")]
    public void BadHexesJsonIsRejectedNamingFileAndProblem(string json, string problem)
    {
        var path = TempFile("hexes.json", json);
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => MapBuilder.LoadHexes(path));
        StringAssert.Contains(path, ex.Message); StringAssert.Contains(problem, ex.Message);
        var fromText = Assert.Throws<System.IO.InvalidDataException>(() => MapBuilder.LoadHexes(json, path));
        Assert.AreEqual(ex.Message, fromText.Message);
    }

    const string GoodMeta = "\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367";

    [TestCase("", "empty")]
    [TestCase("{\"width\":0,\"height\":0,\"width_km\":161.5,\"length_km\":431.2," + GoodMeta + "}", "width")]
    [TestCase("{\"width\":-3,\"height\":-3,\"width_km\":161.5,\"length_km\":431.2," + GoodMeta + "}", "width")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":0,\"length_km\":431.2," + GoodMeta + "}", "width_km")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":-1," + GoodMeta + "}", "length_km")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,", "malformed JSON")]
    [TestCase("not json at all", "malformed JSON")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":35.9,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367}", "lon1")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":35.9,\"lon1\":34.2,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367}", "lon1")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":33.35,\"lat1\":33.35,\"elev_min_m\":-427,\"elev_max_m\":2367}", "lat1")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":33.35,\"lat1\":29.45,\"elev_min_m\":-427,\"elev_max_m\":2367}", "lat1")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":100,\"elev_max_m\":100}", "elev_max_m")]
    [TestCase("{\"width\":3,\"height\":3,\"width_km\":161.5,\"length_km\":431.2,\"lon0\":34.2,\"lon1\":35.9,\"lat0\":29.45,\"lat1\":33.35,\"elev_min_m\":2367,\"elev_max_m\":-427}", "elev_max_m")]
    public void BadHeightmapMetaIsRejected(string json, string problem)
    {
        var meta = TempFile("heightmap.json", json);
        var dir = System.IO.Path.GetDirectoryName(meta);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "heightmap.r16"), new byte[18]);
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => HeightmapLoader.Load(dir));
        StringAssert.Contains("heightmap.json", ex.Message); StringAssert.Contains(problem, ex.Message);
        var fromText = Assert.Throws<System.IO.InvalidDataException>(() => HeightmapLoader.Load(json, new byte[18], meta));
        Assert.AreEqual(ex.Message, fromText.Message);
    }

    [Test]
    public void UnknownBiomeIsRejectedBeforeAnyObjectIsCreated()
    {
        var (terrainMat, overlayMat) = Materials();
        var path = TempFile("hexes.json", "{\"hex_size_km\":5,\"layout\":\"pointy\",\"hexes\":[{\"q\":0,\"r\":0,\"biome\":\"sea\"},{\"q\":1,\"r\":0,\"biome\":\"tundra\"}]}");
        int terrainsBefore = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length;
        int dataBefore = Resources.FindObjectsOfTypeAll<TerrainData>().Length;
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => MapBuilder.Build(Paths.StreamingMap, path, terrainMat, overlayMat));
        StringAssert.Contains("unknown biome 'tundra'", ex.Message);
        Assert.AreEqual(terrainsBefore, Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, "orphan Terrain left behind");
        Assert.AreEqual(dataBefore, Resources.FindObjectsOfTypeAll<TerrainData>().Length, "orphan TerrainData left behind");
    }

    // --- Text/bytes inputs, for the WebGL build that cannot use System.IO on streamingAssetsPath (final fix 3, I2) ---

    static string MetaPath => System.IO.Path.Combine(Paths.StreamingMap, "heightmap.json");
    static string R16Path => System.IO.Path.Combine(Paths.StreamingMap, "heightmap.r16");

    [Test]
    public void HeightmapFromBytesMatchesHeightmapFromPath()
    {
        var (pm, ph) = HeightmapLoader.Load(Paths.StreamingMap);
        var (bm, bh) = HeightmapLoader.Load(System.IO.File.ReadAllText(MetaPath), System.IO.File.ReadAllBytes(R16Path), MetaPath);
        Assert.AreEqual(JsonUtility.ToJson(pm), JsonUtility.ToJson(bm));
        Assert.AreEqual(ph.GetLength(0), bh.GetLength(0)); Assert.AreEqual(ph.GetLength(1), bh.GetLength(1));
        int differ = 0;
        for (int y = 0; y < ph.GetLength(0); y++) for (int x = 0; x < ph.GetLength(1); x++) if (ph[y, x] != bh[y, x]) differ++;
        Assert.AreEqual(0, differ, "height samples that differ");
    }

    [Test]
    public void HexesFromTextMatchHexesFromPath()
    {
        var fromPath = MapBuilder.LoadHexes(HexesJson);
        var fromText = MapBuilder.LoadHexes(System.IO.File.ReadAllText(HexesJson), HexesJson);
        Assert.AreEqual(JsonUtility.ToJson(fromPath), JsonUtility.ToJson(fromText));
    }

    [Test]
    public void BuildFromReadInputsMatchesBuildFromPaths()
    {
        var (terrainMat, overlayMat) = Materials();
        var a = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        var (meta, heights) = HeightmapLoader.Load(System.IO.File.ReadAllText(MetaPath), System.IO.File.ReadAllBytes(R16Path), MetaPath);
        var b = MapBuilder.Build(meta, heights, MapBuilder.LoadHexes(System.IO.File.ReadAllText(HexesJson), HexesJson), terrainMat, overlayMat);
        try
        {
            var ta = a.GetComponent<Terrain>().terrainData; var tb = b.GetComponent<Terrain>(); Assert.IsNotNull(tb, "terrain from read inputs");
            Assert.AreEqual(ta.size, tb.terrainData.size);
            Assert.AreEqual(ta.heightmapResolution, tb.terrainData.heightmapResolution);
            Assert.AreEqual(ta.GetHeights(0, 0, ta.heightmapResolution, ta.heightmapResolution), tb.terrainData.GetHeights(0, 0, ta.heightmapResolution, ta.heightmapResolution));
            Assert.AreEqual(a.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh.vertices,
                b.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh.vertices);
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
    }

    // --- hexes.json and the heightmap must cover the same area (final fix 3, M3) ---

    [TestCase(1e-6, 0, 0, 0)]
    [TestCase(0, -1e-6, 0, 0)]
    [TestCase(0, 0, 1e-6, 0)]
    [TestCase(0, 0, 0, -1e-6)]
    public void HexBoundsThatDifferFromTheHeightmapAreRejectedBeforeAnyObjectIsCreated(double dLon0, double dLon1, double dLat0, double dLat1)
    {
        var (terrainMat, overlayMat) = Materials();
        var meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
        string D(double v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        var json = "{\"bounds\":{\"lon0\":" + D(meta.lon0 + dLon0) + ",\"lon1\":" + D(meta.lon1 + dLon1) + ",\"lat0\":" + D(meta.lat0 + dLat0) + ",\"lat1\":" + D(meta.lat1 + dLat1)
            + "},\"hex_size_km\":5,\"layout\":\"pointy\",\"hexes\":[{\"q\":0,\"r\":0,\"lon\":35,\"lat\":32,\"land\":true,\"biome\":\"hills\"}]}";
        var path = TempFile("hexes.json", json);
        int terrainsBefore = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length;
        var ex = Assert.Throws<System.IO.InvalidDataException>(() => MapBuilder.Build(Paths.StreamingMap, path, terrainMat, overlayMat));
        StringAssert.Contains("hexes.json", ex.Message); StringAssert.Contains("heightmap.json", ex.Message); StringAssert.Contains("bounds", ex.Message);
        Assert.AreEqual(terrainsBefore, Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, "orphan Terrain left behind");
    }

    [Test]
    public void RealHexesJsonCarriesTheHeightmapBounds()
    {
        var b = MapBuilder.LoadHexes(HexesJson).bounds; var meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
        Assert.AreEqual(meta.lon0, b.lon0); Assert.AreEqual(meta.lon1, b.lon1); Assert.AreEqual(meta.lat0, b.lat0); Assert.AreEqual(meta.lat1, b.lat1);
    }
}
