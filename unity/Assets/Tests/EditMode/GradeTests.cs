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

/// <summary>Plan Task 4: the grade, the tonemapper, the vignette, and what they do to readability.</summary>
public class GradeTests
{
    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static (MapScene map, CameraRig rig, VolumeProfile profile) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var rig = Camera.main.GetComponent<CameraRig>();
        return (map, rig, rig.Volume.profile);
    }

    [Test]
    public void TheGradeIsWarmLandTealShadowsAndAGentleVignette()
    {
        var (_, _, profile) = Open();
        Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var ca));
        Assert.AreEqual(MapGrade.Saturation, ca.saturation.value); Assert.Greater(ca.contrast.value, 0f);
        Assert.IsTrue(profile.TryGet<WhiteBalance>(out var wb)); Assert.Greater(wb.temperature.value, 0f, "warm");
        Assert.IsTrue(profile.TryGet<SplitToning>(out var st));
        Assert.Greater(st.shadows.value.b, st.shadows.value.r, "teal shadows"); Assert.Greater(st.highlights.value.r, st.highlights.value.b, "amber highlights");
        Assert.IsTrue(profile.TryGet<Vignette>(out var v)); Assert.That(v.intensity.value, Is.InRange(0.2f, 0.35f));
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tm)); Assert.AreEqual(TonemappingMode.Neutral, tm.mode.value);
        CollectionAssert.Contains(VisualToggles.Names, "grade");
        // The runtime profile is invisible to URP's build-time post-processing stripping, which keeps the tonemapper's
        // uber-shader variants only when some VolumeProfile asset under Assets has a Tonemapping component (URP 17.3
        // ShaderBuildPreprocessor.GetSupportedFeaturesFromVolumes; strip-unused-post-processing is on). The pipeline assets'
        // own profile (Settings/SampleSceneProfile) and the global settings' Settings/DefaultVolumeProfile both carry one today,
        // so this fails only if every such asset loses it, which would make the WebGL player silently drop Neutral.
        bool kept = false;
        foreach (var guid in AssetDatabase.FindAssets("t:VolumeProfile", new[] { "Assets" }))
            kept |= AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid))?.Has<Tonemapping>() == true;
        Assert.IsTrue(kept, "a VolumeProfile asset with Tonemapping keeps _TONEMAP_NEUTRAL in builds");
    }

    static HexJson NearestLandHex(MapScene map, double lon, double lat)
    {
        HexJson? best = null;
        foreach (var h in map.Hexes.hexes)
            if (h.land && h.biome != "lake" && (best == null || (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat) < (best.lon - lon) * (best.lon - lon) + (best.lat - lat) * (best.lat - lat))) best = h;
        return best!;
    }

    /// <summary>Selects the land hex nearest (lon, lat) and returns world points on the band (the middle of its second edge,
    /// read from the band's own mesh, so it holds whatever height the band rides at) and on the land 1 km inside it.</summary>
    static (Vector3 band, Vector3 land) SelectNear(MapScene map, double lon, double lat)
    {
        var hex = NearestLandHex(map, lon, lat);
        map.Select(hex.q, hex.r);
        var sel = map.Terrain.transform.Find(MapScene.SelectionName);
        var v = sel.GetComponent<MeshFilter>().sharedMesh.vertices;
        int n = MapBuilder.RingVertices, j = MapBuilder.OverlaySegmentsPerEdge + MapBuilder.OverlaySegmentsPerEdge / 2;
        var band = sel.TransformPoint((v[j] + v[n + j]) / 2f);
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var inward = new Vector3(centre.x - band.x, 0, centre.z - band.z).normalized;
        var land = new Vector3(band.x, 0, band.z) + inward * 1000f; land.y = map.Terrain.SampleHeight(land);
        return (band, land);
    }

    /// <summary>Review I9: the selected hex still reads on graded land, at both promo poses, graded and ungraded.</summary>
    [UnityTest]
    public IEnumerator TheSelectedHexStillStandsOutOnGradedLand()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        VisualToggles.Set("tiltshift", false);   // the band is a thin line: measured sharp, as LightingTests measures detail
        foreach (var pose in PromoPose.All)
        {
            pose.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
            var (band, land) = SelectNear(map, pose.Lon, pose.Lat);
            foreach (var graded in new[] { true, false })
            {
                VisualToggles.Set("grade", graded);
                var px = new Vector2[2];
                var s = RenderSample.Render(cam, 960, 540, new[] { band, land }, px);
                Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1);
                float de = RenderSample.DeltaE(b, l);
                TestContext.WriteLine($"{pose.Name} graded={graded}: band {b}, land {l}, deltaE {de:0.0}");
                Assert.GreaterOrEqual(de, 20f, $"{pose.Name} graded={graded}");
            }
            VisualToggles.Set("grade", true);
        }
        map.Select(-9999, -9999);
    }

    /// <summary>Review I9: colour-blind players. Under simulated deuteranopia and protanopia (Machado 2009, severity 1), at
    /// the gameplay pose: sea against land, the selection against land and the selection against sea stay at deltaE76 >= 15.
    /// (Borders are not drawn in visual v1; the art track's border task adds its colours here.)</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersStillTellCoastAndSelectionApart()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        VisualToggles.Set("tiltshift", false);   // the band is a thin line: measured sharp
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var (band, land) = SelectNear(map, 34.86, 32.00);
        var sea = map.Frame.ToWorld(34.66, 32.00, 0); sea.y = map.Terrain.SampleHeight(sea);
        var px = new Vector2[3];
        var s = RenderSample.Render(cam, 960, 540, new[] { band, land, sea }, px);
        Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1), w = s.Mean((int)px[2].x, (int)px[2].y, 2);
        foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
        {
            Color B = RenderSample.SimulateCvd(b, kind), L = RenderSample.SimulateCvd(l, kind), W = RenderSample.SimulateCvd(w, kind);
            float seaLand = RenderSample.DeltaE(W, L), selLand = RenderSample.DeltaE(B, L), selSea = RenderSample.DeltaE(B, W);
            TestContext.WriteLine($"{kind}: sea-land {seaLand:0.0}, selection-land {selLand:0.0}, selection-sea {selSea:0.0}");
            Assert.GreaterOrEqual(seaLand, 15f, $"{kind}: the coast");
            Assert.GreaterOrEqual(selLand, 15f, $"{kind}: the selection on land");
            Assert.GreaterOrEqual(selSea, 15f, $"{kind}: the selection against the sea");
        }
        map.Select(-9999, -9999);
    }

    /// <summary>Review I9: with the Neutral tonemapper the sunlit desert does not clip: in the Negev (34.9 E, 30.8 N, 60 km)
    /// and at the gameplay pose, under 1% of pixels have any channel at the top 8-bit step. The control: with the grade's
    /// tonemapper set to None (which also overrides the pipeline profile's Neutral) the graded Negev does clip.</summary>
    [UnityTest]
    public IEnumerator TheSunlitDesertDoesNotClip()
    {
        var (map, rig, profile) = Open();
        yield return null;
        var cam = Camera.main;
        foreach (var (name, lon, lat, distance) in new[] { ("Negev", 34.9, 30.8, 60000f), ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance) })
        {
            rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var s = RenderSample.Render(cam, 480, 270);
            float clipped = s.FractionClipped(0.995f);
            TestContext.WriteLine($"{name}: clipped {clipped:P2}");
            Assert.Less(clipped, 0.01f, name);
        }
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tm));
        rig.LookAt = map.Frame.ToWorld(34.9, 30.8, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        tm.mode.Override(TonemappingMode.None);
        float control = RenderSample.Render(cam, 480, 270).FractionClipped(0.995f);
        tm.mode.Override(MapGrade.Tonemapper);
        TestContext.WriteLine($"Negev without a tonemapper (control): clipped {control:P2}");
        Assert.Greater(control, 0.01f, "without the tonemapper the graded desert clips");
    }

    /// <summary>Rendered at the gameplay pose: the grade lifts mean saturation by at least 5%.</summary>
    [UnityTest]
    public IEnumerator TheGradeLiftsSaturation()
    {
        var (map, rig, _) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        VisualToggles.Set("grade", false); var flat = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("grade", true); var graded = RenderSample.Render(cam, 480, 270);
        TestContext.WriteLine($"saturation {flat.MeanSaturation():0.000} -> {graded.MeanSaturation():0.000}");
        Assert.Greater(graded.MeanSaturation(), flat.MeanSaturation() * 1.05f);
    }

    /// <summary>Rendered: MapGrade's vignette darkens the corners relative to the centre more than the vignette it replaces
    /// (the pipeline's template profile, Settings/SampleSceneProfile, applies a 0.2 black one, which "plain" still shows).</summary>
    [UnityTest]
    public IEnumerator TheVignetteDarkensTheCorners()
    {
        var (map, rig, profile) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        profile.TryGet<Vignette>(out var v);
        v.active = false; var plain = RenderSample.Render(cam, 480, 270);
        v.active = true; var vignetted = RenderSample.Render(cam, 480, 270);
        float Ratio(RenderSample s) => s.MeanLuma(0f, 0f, 0.1f, 0.1f) / s.MeanLuma(0.45f, 0.45f, 0.55f, 0.55f);
        TestContext.WriteLine($"corner/centre {Ratio(plain):0.000} -> {Ratio(vignetted):0.000}");
        Assert.Less(Ratio(vignetted), Ratio(plain) * 0.9f);
    }
}
