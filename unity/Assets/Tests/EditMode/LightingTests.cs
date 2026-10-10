#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 2: the sun, the ambient fill, shadows at map scale and the hero zoom.</summary>
public class LightingTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // destroys any map: its teardown restores
        ShadowFit.Restore();
        VisualToggles.Clear();
    }

    [Test]
    public void TheSunIsLowWarmAndFromTheWestNorthWest()
    {
        var go = new GameObject("Sun"); var sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
        try
        {
            MapLighting.Apply(sun);
            var travel = sun.transform.forward;
            Assert.AreEqual(-Mathf.Sin(MapLighting.SunElevation * Mathf.Deg2Rad), travel.y, 1e-4f, "elevation");
            Assert.That(MapLighting.SunElevation, Is.InRange(25f, 35f), "late afternoon: long shadows");
            Assert.Greater(travel.x, 0f, "the light travels east: the sun is in the west");
            Assert.Less(travel.z, 0f, "and a little south: the sun is north of west");
            Assert.Greater(sun.color.r, sun.color.b + 0.2f, "warm");
            Assert.AreEqual(LightShadows.Soft, sun.shadows);
            Assert.AreSame(sun, RenderSettings.sun);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TheAmbientFillIsCoolFromTheSkyAndDarkFromTheGround()
    {
        MapLighting.Apply(null);
        var c = new Color[2];
        RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up, Vector3.down }, c);
        Assert.Greater(c[0].b, c[0].r, "sky fill is cool");
        Assert.Greater(c[0].grayscale, c[1].grayscale + 0.05f, "brighter from above");
        Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
    }

    [TestCase(6000f)]
    [TestCase(45000f)]
    [TestCase(120000f)]
    [TestCase(250000f)]
    public void TheShadowDistanceCoversTheWholeView(float distance)
    {
        var go = new GameObject("Cam"); var cam = go.AddComponent<Camera>(); var rig = go.AddComponent<CameraRig>();
        try
        {
            cam.fieldOfView = CameraRig.Fov;
            foreach (var aspect in new[] { 16f / 9f, 21f / 9f })
            {
                cam.aspect = aspect;
                rig.LookAt = Vector3.zero; rig.Distance = distance; rig.TopInset = CameraRig.MaxTopInset; rig.Apply();
                foreach (var v in new[] { new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f) })
                {
                    var ray = cam.ViewportPointToRay(new Vector3(v.x, v.y, 0));
                    var hit = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                    var depth = Vector3.Dot(hit - cam.transform.position, cam.transform.forward);
                    Assert.LessOrEqual(depth, ShadowFit.Distance(distance), $"{distance} m, aspect {aspect:0.00}, viewport {v}");
                }
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void FittingFollowsTheCameraAndRestoringPutsTheAssetBack()
    {
        var asset = UniversalRenderPipeline.asset; Assert.IsNotNull(asset);
        var before = asset!.shadowDistance;
        ShadowFit.Apply(60000f); Assert.AreEqual(ShadowFit.Distance(60000f), asset.shadowDistance, 1e-3f);
        ShadowFit.Apply(10000f); Assert.AreEqual(ShadowFit.Distance(10000f), asset.shadowDistance, 1e-3f);
        Assert.AreEqual(ShadowFit.MinDistanceM, ShadowFit.Distance(100f));
        ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance);
        ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance, "restoring twice is harmless");
    }

    [Test]
    public void DestroyingTheMapRestoresTheShadowDistance()
    {
        var asset = UniversalRenderPipeline.asset!; var before = asset.shadowDistance;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.AreNotEqual(before, asset.shadowDistance, "fitted to the camera by Build");
        CollectionAssert.Contains(VisualToggles.Names, "shadows");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.AreEqual(before, asset.shadowDistance, "restored with the map");
    }

    [Test]
    public void TheHeroZoomComesInToSixKilometres()
    {
        Assert.AreEqual(6000f, CameraController.MinDistance);
        Assert.AreEqual(250000f, CameraController.MaxDistance);
    }

    [Test]
    public void TheWebTierShadowMapIs2048()
    {
        var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Assert.AreEqual(2048, web.mainLightShadowmapResolution);
    }

    /// <summary>Rendered: over the Dead Sea escarpment (the steepest relief on the map) the sun now casts shadows. Without
    /// the fit (the asset's 50 m) nothing would. Much of the escarpment's shadow at 30 degrees falls east onto the Dead Sea;
    /// in Track A alone that water is URP-lit terrain and darkens; after the merge the water is ScWaterShade, which samples
    /// the main light's shadow attenuation (Task 7, review I6), so the shadow still lands on it and the 2% floor holds.</summary>
    [UnityTest]
    public IEnumerator ReliefCastsShadowsAtMapScale()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>(); var sun = MapLighting.FindSun();
        Assert.IsNotNull(sun, "Map.unity has a directional light");
        rig.LookAt = map.Frame.ToWorld(35.38, 31.45, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var lit = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("shadows", false);
        var flat = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("shadows", true);
        float shadowed = RenderSample.FractionDarker(lit, flat, 0.04f);
        TestContext.WriteLine($"shadowed share {shadowed:P1}");
        Assert.Greater(shadowed, 0.02f, "at least 2% of the view is in a cast shadow");
    }
}
