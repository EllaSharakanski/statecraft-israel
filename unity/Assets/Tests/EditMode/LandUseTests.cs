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

    /// <summary>The mask at (lon, lat), from the source PNG (Texture2D row 0 is south).</summary>
    static Color MaskAt(Texture2D png, HeightmapMeta m, double lon, double lat) =>
        png.GetPixel((int)((lon - m.lon0) / (m.lon1 - m.lon0) * png.width), (int)((lat - m.lat0) / (m.lat1 - m.lat0) * png.height));

    [Test]
    public void TheMasksImportAsLinearClampedDataOnTheTerrain()
    {
        var land = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Land);
        var tracks = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Tracks);
        Assert.AreEqual((1024, 2048), (land.width, land.height), "about 158 x 211 m per texel");
        Assert.AreEqual((512, 1024), (tracks.width, tracks.height), "about 316 x 421 m per texel");
        foreach (var t in new[] { land, tracks })
        {
            Assert.IsFalse(t.isDataSRGB, t.name + ": weights and offsets, not colour");
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode, t.name);
            Assert.AreEqual(FilterMode.Trilinear, t.filterMode, t.name);
            Assert.Greater(t.mipmapCount, 1, t.name);
        }
        Assert.AreEqual(TextureFormat.RG16, tracks.format, "uncompressed: block compression would bend the offsets");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual(land, mat.GetTexture("_ScLandTex")); Assert.AreEqual(tracks, mat.GetTexture("_ScTrackTex"));
    }

    [Test]
    public void TheLandShaderUsesThePalettesBrightness()
    {
        var m = System.Text.RegularExpressions.Regex.Match(System.IO.File.ReadAllText("Assets/Shaders/StatecraftLand.hlsl"),
            @"static const float ScPaletteBrightness = ([0-9.]+);");
        Assert.IsTrue(m.Success);
        Assert.AreEqual(MapPalette.Brightness, float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 1e-6f);
    }

    /// <summary>The mask follows the real ground: fields on the coastal plain by Rehovot and in the Jezreel valley, none in
    /// the central Negev or on the Dead Sea; the Judean hills wet enough for terraces, the Negev not; the tracks reach the
    /// towns they join.</summary>
    [Test]
    public void TheMaskFollowsSlopeRainAndTowns()
    {
        var png = Read(MapTexturePaths.Land); var tracks = Read(MapTexturePaths.Tracks); var m = Meta();
        try
        {
            Color plain = MaskAt(png, m, 34.84, 31.92), jezreel = MaskAt(png, m, 35.30, 32.62), negev = MaskAt(png, m, 34.90, 30.60);
            Color deadSea = MaskAt(png, m, 35.50, 31.50), judea = MaskAt(png, m, 35.15, 31.78);
            TestContext.WriteLine($"plain {plain}, Jezreel {jezreel}, Negev {negev}, Dead Sea {deadSea}, Judea {judea}");
            Assert.Greater(plain.r, 0.6f, "fields on the coastal plain"); Assert.Greater(jezreel.r, 0.6f, "fields in the Jezreel valley");
            Assert.Less(negev.r, 0.05f, "no dry farming in the central Negev"); Assert.AreEqual(0f, deadSea.r + deadSea.g + deadSea.b, 1e-3f, "water");
            Assert.Greater(judea.b, 0.5f, "the Judean hills are wet enough for terraces"); Assert.Less(negev.b, 0.05f);
            foreach (var (name, lon, lat) in new[] { ("Jerusalem", 35.2256, 31.7789), ("Nazareth", 35.3035, 32.7019), ("Petah Tikva", 34.8878, 32.0889) })
                Assert.Less(TrackDistanceM(MaskAt(tracks, m, lon, lat)), 250f, name + " is on a track");
            Assert.Greater(TrackDistanceM(MaskAt(tracks, m, 34.90, 30.60)), 1000f, "no track across the empty Negev");
        }
        finally { Object.DestroyImmediate(png); Object.DestroyImmediate(tracks); }
    }

    /// <summary>Rendered at the gameplay pose: the fields, hedges and tracks add local detail to the plain (the flat
    /// mustard ground had little): the gradient energy of the frame's lower half rises by at least 15% against an empty
    /// mask, and the mean colour stays the same ground (saturation within 0.06). Terrain.mat changes in memory only.</summary>
    [UnityTest]
    public IEnumerator TheFieldsAddDetailToThePlainWithoutChangingItsColour()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        RenderSample.TiltShift(false);
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath); var real = mat.GetTexture("_ScLandTex"); var realTracks = mat.GetTexture("_ScTrackTex");
        try
        {
            var farmed = RenderSample.Render(cam, 960, 540);
            mat.SetTexture("_ScLandTex", Texture2D.blackTexture); mat.SetTexture("_ScTrackTex", Texture2D.blackTexture);
            var bare = RenderSample.Render(cam, 960, 540);
            TestContext.WriteLine($"gradient {bare.GradientEnergy(0f, 0.5f):0.0000} -> {farmed.GradientEnergy(0f, 0.5f):0.0000}; " +
                                  $"saturation {bare.MeanSaturation():0.000} -> {farmed.MeanSaturation():0.000}");
            Assert.Greater(farmed.GradientEnergy(0f, 0.5f), bare.GradientEnergy(0f, 0.5f) * 1.15f);
            Assert.AreEqual(bare.MeanSaturation(), farmed.MeanSaturation(), 0.06f);
        }
        finally { mat.SetTexture("_ScLandTex", real); mat.SetTexture("_ScTrackTex", realTracks); }
    }
}
