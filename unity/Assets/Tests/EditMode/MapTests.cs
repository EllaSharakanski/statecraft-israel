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
    }

    // M0 correction 1: no Shader.Find at runtime. Materials are editor-made assets passed into Build.
    const string TerrainMatPath = "Assets/Materials/Terrain.mat", OverlayMatPath = "Assets/Materials/HexOverlay.mat";

    static string HexesJson => System.IO.Path.Combine(Paths.Content, "map", "hexes.json");

    [Test]
    public void BuildUsesThePassedTerrainAndOverlayMaterials()
    {
        var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        var overlayMat = AssetDatabase.LoadAssetAtPath<Material>(OverlayMatPath);
        Assert.IsNotNull(terrainMat, TerrainMatPath); Assert.IsNotNull(overlayMat, OverlayMatPath);
        Assert.AreEqual("Universal Render Pipeline/Terrain/Lit", terrainMat.shader.name);
        Assert.AreEqual("Universal Render Pipeline/Unlit", overlayMat.shader.name);

        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat, overlayMat);
        var terrain = go.GetComponent<Terrain>();
        try
        {
            Assert.AreSame(terrainMat, terrain.materialTemplate);
            var overlay = go.transform.Find("HexOverlay");
            Assert.IsNotNull(overlay, "HexOverlay child");
            Assert.AreSame(overlayMat, overlay.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.AreEqual(2049, terrain.terrainData.heightmapResolution);
            Assert.Greater(overlay.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0);
        }
        finally
        {
            Object.DestroyImmediate(terrain.terrainData);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BuildRejectsMissingMaterials()
    {
        Assert.Throws<System.ArgumentNullException>(() => MapBuilder.Build(Paths.StreamingMap, HexesJson, null, null));
    }
}
