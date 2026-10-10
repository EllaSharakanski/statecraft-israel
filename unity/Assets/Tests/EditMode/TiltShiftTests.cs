#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 3: the two-band tilt-shift.</summary>
public class TiltShiftTests
{
    static readonly Vector4 P = new Vector4(0.5f, 0.17f, 0.18f, 10f / 1080f);

    [TearDown]
    public void Reset()
    {
        TiltShift.Enabled = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
    }

    [Test]
    public void TheMiddleBandIsSharpAndBothEdgesBlur()
    {
        Assert.AreEqual(0f, TiltShift.Weight(0.5f, P));
        Assert.AreEqual(0f, TiltShift.Weight(0.34f, P), 1e-6f, "inside the sharp half-width");
        Assert.AreEqual(1f, TiltShift.Weight(0.0f, P)); Assert.AreEqual(1f, TiltShift.Weight(1.0f, P));
        Assert.AreEqual(TiltShift.Weight(0.2f, P), TiltShift.Weight(0.8f, P), 1e-6f, "symmetric about the focus");
        Assert.Greater(TiltShift.Weight(0.15f, P), TiltShift.Weight(0.25f, P), "stronger towards the edge");
    }

    [Test]
    public void TheBlurIsResolutionIndependentAndFollowsTheTopBar()
    {
        Assert.AreEqual(0.5f, TiltShift.FocusCentre(0f));
        Assert.AreEqual(0.45f, TiltShift.FocusCentre(0.1f), 1e-6f, "the middle of the map area below a 10% bar");
        Assert.AreEqual(0.25f, TiltShift.FocusCentre(5f), 1e-6f, "clamped to CameraRig.MaxTopInset");
        Assert.AreEqual(0.45f, TiltShift.Params(0.1f, 30000f).x, 1e-6f);
        Assert.AreEqual(10f / 1080f, TiltShift.MaxRadiusUv, 1e-7f, "10 px of a 1080-line picture, whatever the window or render scale");
        Assert.AreEqual(TiltShift.MaxRadiusUv, TiltShift.Params(0f, 30000f).w, 1e-7f, "full strength when close");
        Assert.AreEqual(TiltShift.MaxRadiusUv * TiltShift.MinStrength, TiltShift.Params(0f, CameraController.MaxDistance).w, 1e-7f, "weakest at the overview");
    }

    [Test]
    public void BothTiersRunTheTiltShiftAndTheFarBlurIsGone()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PostFxPaths.TiltShiftMaterial);
        Assert.IsNotNull(mat); Assert.AreEqual("Statecraft/TiltShift", mat.shader.name);
        foreach (var path in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var f = data.rendererFeatures.OfType<TiltShiftFeature>().SingleOrDefault();
            Assert.IsNotNull(f, path); Assert.IsTrue(f!.isActive, path);
            // The same asset, compared by instance id: Unity can hand out two managed wrappers for one native object
            // (seen for the PC renderer, the active one), so AreSame on the wrappers fails for the right asset.
            Assert.IsNotNull(f.Material, path); Assert.AreEqual(mat.GetInstanceID(), f.Material!.GetInstanceID(), path);
        }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.IsFalse(Camera.main.GetComponent<CameraRig>().Volume.profile.Has<DepthOfField>(), "the far-only Gaussian DoF is retired");
        CollectionAssert.Contains(VisualToggles.Names, "tiltshift");
    }

    [Test]
    public void TheTiltShiftShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/TiltShift")));

    /// <summary>Rendered on both tiers at the gameplay pose: local contrast in the top and bottom bands falls by at least
    /// 40%, the middle band keeps at least 95% of it. (Thresholds leave room for the terrain and grade tracks: a blur
    /// lowers gradient energy whatever the texture.)</summary>
    [UnityTest]
    public IEnumerator TheTopAndBottomBlurAndTheMiddleStaysSharpOnBothTiers()
    {
        int saved = QualitySettings.GetQualityLevel();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        try
        {
            foreach (var level in new[] { 0, 1 })
            {
                QualitySettings.SetQualityLevel(level, true);
                TiltShift.Apply(0f, rig.Distance);
                TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
                TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
                float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
                float top = Ratio(0.90f, 0.98f), bottom = Ratio(0.02f, 0.10f), middle = Ratio(0.46f, 0.54f);
                TestContext.WriteLine($"{QualitySettings.names[level]}: top {top:0.00}, bottom {bottom:0.00}, middle {middle:0.00}");
                Assert.Less(top, 0.6f, "top band blurred"); Assert.Less(bottom, 0.6f, "bottom band blurred");
                Assert.Greater(middle, 0.95f, "middle band sharp");
                // Review N6: the same blur at twice the resolution (the radius is a share of the picture's height).
                TiltShift.Enabled = false; var sharpBig = RenderSample.Render(cam, 1920, 1080);
                TiltShift.Enabled = true; var shiftedBig = RenderSample.Render(cam, 1920, 1080);
                float topBig = shiftedBig.GradientEnergy(0.90f, 0.98f) / sharpBig.GradientEnergy(0.90f, 0.98f);
                TestContext.WriteLine($"{QualitySettings.names[level]}: top at 1920x1080 {topBig:0.00}");
                Assert.AreEqual(top, topBig, 0.1f, "the top band loses the same share of its detail at 1080 lines as at 540");
            }
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>Review I4: an asymmetric case, so a vertically flipped composite or a focus that ignores the top bar fails.
    /// With TopInset 0.3 the focus is at 0.35: the band around 0.35 stays sharp, and 0.70-0.78 (sharp when the focus is at
    /// 0.5) is blurred.</summary>
    [UnityTest]
    public IEnumerator AnOffCentreFocusFollowsTheTopBar()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0.3f; rig.Apply();
        Assert.AreEqual(0.35f, Shader.GetGlobalVector(TiltShift.ParamsId).x, 1e-5f, "CameraRig.Apply passes the inset");
        TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
        TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
        float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
        float focus = Ratio(0.33f, 0.37f), upper = Ratio(0.70f, 0.78f);
        TestContext.WriteLine($"focus band {focus:0.00}, upper band {upper:0.00}");
        Assert.Greater(focus, 0.95f, "sharp at the inset's focus (0.35)");
        Assert.Less(upper, 0.8f, "blurred above it");
        rig.TopInset = 0f; rig.Apply();
    }
}
