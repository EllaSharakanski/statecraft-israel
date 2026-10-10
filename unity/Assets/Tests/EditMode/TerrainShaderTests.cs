#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 6: the Statecraft terrain graph, its baked inputs and what they do on screen.</summary>
public class TerrainShaderTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat";

    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    /// <summary>Review C1, the positive half of the Task 6 gate: the generated passes include StatecraftTerrain.hlsl (the Custom
    /// Function is in File mode), and the HLSL really paints the terrain: with a magenta biome texture a flat coastal-plain
    /// pixel comes out magenta, not black or the sea colour.</summary>
    [UnityTest]
    public IEnumerator TheCustomFunctionDrivesTheTerrain()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader;
        var data = ShaderUtil.GetShaderData(shader);
        int including = 0;
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            for (int p = 0; p < sub.PassCount; p++) if (sub.GetPass(p).SourceCode.Contains("StatecraftTerrain.hlsl")) including++;
        }
        Assert.Greater(including, 0, "no generated pass includes StatecraftTerrain.hlsl: the Custom Function is not in File mode");
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        // A flat coastal-plain point (review N7): no rock blend and no stone tint can stand between the texture and the screen.
        var plain = map.Frame.ToWorld(34.90, 31.95, 0); plain.y = map.Terrain.SampleHeight(plain);
        rig.LookAt = plain; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var magenta = new Texture2D(1, 1, TextureFormat.RGBA32, false); magenta.SetPixel(0, 0, Color.magenta); magenta.Apply();
        var real = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        try
        {
            Shader.SetGlobalTexture(MapShaderGlobals.Biome, magenta);
            var px = new Vector2[1];
            var s = RenderSample.Render(cam, 480, 270, new[] { plain }, px);
            var c = s.Mean((int)px[0].x, (int)px[0].y, 2);
            TestContext.WriteLine($"land with a magenta biome texture: {c}");
            Assert.Greater(c.r + c.g + c.b, 0.15f, "not black");
            Assert.Greater(Mathf.Min(c.r, c.b), c.g + 0.1f, "magenta: the biome texture reaches the screen through the HLSL");
        }
        finally { Shader.SetGlobalTexture(MapShaderGlobals.Biome, real); Object.DestroyImmediate(magenta); }
    }

    [Test]
    public void TheTerrainUsesTheStatecraftGraphWithItsBakedTextures()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual("Statecraft/StatecraftTerrain", mat.shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(mat.shader), string.Join("\n", ShaderUtil.GetShaderMessages(mat.shader).Select(m => m.message)));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake), mat.GetTexture("_ScBakeTex"));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast), mat.GetTexture("_ScCoastTex"));
        CollectionAssert.AreEquivalent(MapBuilder.BiomeColours.Keys, MapPalette.Ground.Keys, "a ground colour for every biome");
    }

    [Test]
    public void TheTerrainShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader));

    [Test]
    public void TheBakedTexturesImportAsLinearClampedData()
    {
        var bake = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake);
        var coast = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast);
        foreach (var t in new[] { bake, coast })
        {
            Assert.AreEqual(2048, t.width, t.name); Assert.AreEqual(2048, t.height, t.name);
            Assert.IsFalse(t.isDataSRGB, t.name + " is data, not colour");
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode, t.name);
            Assert.Greater(t.mipmapCount, 1, t.name);
        }
        Assert.AreEqual(TextureFormat.R8, coast.format, "one uncompressed channel: compression blocks would show in the foam band");
    }

    [Test]
    public void TheTerrainDrawsInOnePassAndNeverFallsBackToItsBasemap()
    {
        var terrain = OpenMap().Terrain;
        Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length, "one layer: one pass (7 layers drew every pixel twice)");
        Assert.GreaterOrEqual(terrain.basemapDistance, 1e6f, "the camera is always farther than the 1 km default");
    }

    [Test]
    public void TheBiomeTextureIsOwnedByTheMapAndSetForTheShaders()
    {
        var map = OpenMap();
        var biome = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        Assert.IsNotNull(biome); Assert.AreEqual(MapBuilder.BiomeTextureResolution, biome.width);
        var m = Shader.GetGlobalVector(MapShaderGlobals.Map);
        Assert.AreEqual(map.Terrain.terrainData.size.x, m.x, 1f); Assert.AreEqual(map.Terrain.terrainData.size.z, m.y, 1f);
        Assert.AreEqual(map.Hexes.hex_size_km * 1000f, Shader.GetGlobalVector(MapShaderGlobals.MapOrigin).w, 1e-3f);
        CollectionAssert.Contains(VisualToggles.Names, "haze");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.IsTrue(biome == null, "destroyed with the map");
    }

    /// <summary>Rendered by Tel Aviv: 10 km offshore is the sea colour, 10 km inland the ground colour.</summary>
    [UnityTest]
    public IEnumerator TheCoastSeparatesLandFromSea()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(34.78, 32.08, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var sea = map.Frame.ToWorld(34.66, 32.08, 0); var land = map.Frame.ToWorld(34.89, 32.08, 0);
        sea.y = map.Terrain.SampleHeight(sea); land.y = map.Terrain.SampleHeight(land);
        var px = new Vector2[2];
        var s = RenderSample.Render(cam, 960, 540, new[] { sea, land }, px);
        Color a = s.Mean((int)px[0].x, (int)px[0].y, 3), b = s.Mean((int)px[1].x, (int)px[1].y, 3);
        TestContext.WriteLine($"sea {a}, land {b}");
        Assert.Greater(a.b, a.r + 0.15f, "sea"); Assert.Greater(b.r, b.b + 0.05f, "land");
    }

    /// <summary>Rendered over the Judean hills: the bakes (AO, curvature, sky-view) raise the relief's local contrast
    /// (RenderSample.GradientEnergy) by at least 8% against a neutral bake (AO 1, curvature 0.5, sky 1). Local, not the
    /// frame's luma spread: this view takes in the Dead Sea, and the land-to-sea step dominates LumaStd, so the AO's overall
    /// darkening of the land lowers it (measured 0.0830 -> 0.0827) while the relief is visibly carved (gradient +13%).
    /// Terrain.mat is changed in memory only and restored.</summary>
    [UnityTest]
    public IEnumerator TheBakesCarveTheRelief()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(35.25, 31.70, 0); rig.Distance = 45000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        var real = mat.GetTexture("_ScBakeTex");
        var neutral = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        neutral.SetPixel(0, 0, new Color(1f, 0.5f, 1f, 1f)); neutral.Apply();
        try
        {
            var carved = RenderSample.Render(cam, 480, 270);
            mat.SetTexture("_ScBakeTex", neutral);
            var plain = RenderSample.Render(cam, 480, 270);
            TestContext.WriteLine($"relief gradient {plain.GradientEnergy(0f, 1f):0.0000} -> {carved.GradientEnergy(0f, 1f):0.0000}");
            Assert.Greater(carved.GradientEnergy(0f, 1f), plain.GradientEnergy(0f, 1f) * 1.08f);
        }
        finally { mat.SetTexture("_ScBakeTex", real); Object.DestroyImmediate(neutral); }
    }

    /// <summary>Rendered at the country-wide pose: the haze moves the far (top) band more than the near (bottom) band. Both
    /// bands are sampled in the column x 0.45..0.6, which is the map's land from top to bottom at this pose: in Task 6 only
    /// the land carries haze, and across the full width the far band is mostly the Mediterranean and the border skirt
    /// (no haze until Task 7), which hid the effect (full width: far 0.023, near 0.026; land column: far 0.069, near 0.026).</summary>
    [UnityTest]
    public IEnumerator TheHazeThickensWithDistance()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set("haze", false); var clear = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("haze", true); var hazy = RenderSample.Render(cam, 480, 270);
        const float x0 = 0.45f, x1 = 0.6f;
        float far = Mathf.Abs(hazy.MeanLuma(x0, 0.8f, x1, 0.95f) - clear.MeanLuma(x0, 0.8f, x1, 0.95f));
        float near = Mathf.Abs(hazy.MeanLuma(x0, 0.05f, x1, 0.2f) - clear.MeanLuma(x0, 0.05f, x1, 0.2f));
        TestContext.WriteLine($"haze shift far {far:0.000}, near {near:0.000}");
        Assert.Greater(far, near + 0.005f);
    }
}
