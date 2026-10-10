#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #33 (promo-land): the land-use mask (tools/land_bake.py) and the fields, terraces and tracks the terrain
/// shader paints from it (StatecraftLand.hlsl).</summary>
public class LandUseTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat";

    [TearDown]
    public void Reset() { RenderSample.TiltShift(true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static Texture2D Read(string path)
    {
        var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        Assert.IsTrue(png.LoadImage(System.IO.File.ReadAllBytes(path)));
        return png;
    }

    /// <summary>Metres to the nearest dirt track at a tracks.png texel (tools/land_bake.py encode_offsets).</summary>
    static float TrackDistanceM(Color c) => new Vector2((c.r * 255f - 127f) / 127f, (c.g * 255f - 127f) / 127f).magnitude * 1000f;

    static HeightmapMeta Meta() =>
        JsonUtility.FromJson<HeightmapMeta>(System.IO.File.ReadAllText(System.IO.Path.Combine(Paths.StreamingMap, "heightmap.json")));

    /// <summary>The texel at (lon, lat), from the source PNG (Texture2D row 0 is south).</summary>
    static Color MaskAt(Texture2D png, HeightmapMeta m, double lon, double lat) =>
        png.GetPixel((int)((lon - m.lon0) / (m.lon1 - m.lon0) * png.width), (int)((lat - m.lat0) / (m.lat1 - m.lat0) * png.height));

    /// <summary>The land weight (alpha) averaged over the 13 x 13 texels (about 1 x 1.4 km) round (lon, lat): parcels left
    /// as grass and hedges vary it texel by texel.</summary>
    static float WeightAround(Texture2D png, HeightmapMeta m, double lon, double lat)
    {
        int x = (int)((lon - m.lon0) / (m.lon1 - m.lon0) * png.width), y = (int)((lat - m.lat0) / (m.lat1 - m.lat0) * png.height);
        float sum = 0; for (int dy = -6; dy <= 6; dy++) for (int dx = -6; dx <= 6; dx++) sum += png.GetPixel(x + dx, y + dy).a;
        return sum / 169f;
    }

    [Test]
    public void TheMasksImportAsLinearClampedDataOnTheTerrain()
    {
        var land = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Land);
        var tracks = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Tracks);
        Assert.AreEqual((2048, 4096), (land.width, land.height), "about 79 x 105 m per texel");
        Assert.AreEqual((512, 1024), (tracks.width, tracks.height), "about 316 x 421 m per texel");
        foreach (var t in new[] { land, tracks })
        {
            Assert.IsFalse(t.isDataSRGB, t.name + ": linear colour and offsets, read as they are");
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode, t.name);
            Assert.AreEqual(FilterMode.Trilinear, t.filterMode, t.name);
            Assert.Greater(t.mipmapCount, 1, t.name);
        }
        Assert.AreEqual(TextureFormat.RG16, tracks.format, "uncompressed: block compression would bend the offsets");
        Assert.AreEqual(2, land.anisoLevel, "seen at a grazing angle");
        Assert.AreEqual(TextureFormat.DXT5, land.format, "colour and weight block-compressed (BC3)");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual(land, mat.GetTexture("_ScLandTex")); Assert.AreEqual(tracks, mat.GetTexture("_ScTrackTex"));
    }

    /// <summary>The land follows the real ground: it covers the coastal plain by Rehovot, the Jezreel valley and (as
    /// garrigue and terraces) the Judean hills, and leaves the central Negev and the Dead Sea to the biome and the water;
    /// the tracks reach the towns they join.</summary>
    [Test]
    public void TheLandFollowsSlopeRainAndTowns()
    {
        var png = Read(MapTexturePaths.Land); var tracks = Read(MapTexturePaths.Tracks); var m = Meta();
        try
        {
            float plain = WeightAround(png, m, 34.84, 31.92), jezreel = WeightAround(png, m, 35.30, 32.62);
            float negev = WeightAround(png, m, 34.90, 30.60), deadSea = WeightAround(png, m, 35.50, 31.50), judea = WeightAround(png, m, 35.15, 31.78);
            TestContext.WriteLine($"land weight: plain {plain:0.000}, Jezreel {jezreel:0.000}, Negev {negev:0.000}, Dead Sea {deadSea:0.000}, Judea {judea:0.000}");
            Assert.Greater(plain, 0.8f, "fields on the coastal plain"); Assert.Greater(jezreel, 0.8f, "fields in the Jezreel valley");
            Assert.Less(negev, 0.02f, "the central Negev stays desert"); Assert.Less(deadSea, 0.02f, "water");
            Assert.Greater(judea, 0.4f, "garrigue and terraces on the Judean hills");
            foreach (var (name, lon, lat) in new[] { ("Jerusalem", 35.2256, 31.7789), ("Nazareth", 35.3035, 32.7019), ("Petah Tikva", 34.8878, 32.0889) })
                Assert.Less(TrackDistanceM(MaskAt(tracks, m, lon, lat)), 250f, name + " is on a track");
            Assert.Greater(TrackDistanceM(MaskAt(tracks, m, 34.90, 30.60)), 1000f, "no track across the empty Negev");
        }
        finally { Object.DestroyImmediate(png); Object.DestroyImmediate(tracks); }
    }

    /// <summary>Rendered at the gameplay pose: the fields, hedges and tracks add local detail to the plain (the flat
    /// mustard ground had little): the gradient energy of the frame's lower half rises by at least 15% against the bare
    /// biome ground (the "land" toggle off), and the mean colour stays the same ground (saturation within 0.06).</summary>
    [UnityTest]
    public IEnumerator TheFieldsAddDetailToThePlainWithoutChangingItsColour()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        RenderSample.TiltShift(false);
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var farmed = RenderSample.Render(cam, 960, 540);
        VisualToggles.Set("land", false);
        var bare = RenderSample.Render(cam, 960, 540);
        TestContext.WriteLine($"gradient {bare.GradientEnergy(0f, 0.5f):0.0000} -> {farmed.GradientEnergy(0f, 0.5f):0.0000}; " +
                              $"saturation {bare.MeanSaturation():0.000} -> {farmed.MeanSaturation():0.000}");
        Assert.Greater(farmed.GradientEnergy(0f, 0.5f), bare.GradientEnergy(0f, 0.5f) * 1.15f);
        Assert.AreEqual(bare.MeanSaturation(), farmed.MeanSaturation(), 0.06f);
    }
}
