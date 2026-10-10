#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 8: the shader-drawn hex grid.</summary>
public class HexGridTests
{
    const float Size = 5000f;

    /// <summary>Track A's tilt-shift (merged later) blurs the top and bottom bands of every render; the width and colour
    /// renders here must not see it (task-3 carry, merge note m6). A no-op while the switch does not exist.</summary>
    internal static void TiltShift(bool on)
    {
        if (System.Linq.Enumerable.Contains(VisualToggles.Names, "tiltshift")) VisualToggles.Set("tiltshift", on);
    }

    [TearDown]
    public void Reset() { TiltShift(true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    [Test]
    public void CentresAreTheInradiusFromEveryEdgeAndCornersAndEdgesAreZero()
    {
        var c = HexGrid.Centre(3, -2, Size);
        float inradius = Mathf.Sqrt(3f) / 2f * Size;
        Assert.AreEqual(inradius, HexGrid.EdgeDistance(c, Size), 1e-2f);
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, 0f), Size), 1e-2f, "middle of the east edge");
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, -Size / 2f), Size), 1e-2f, "a corner");
        Assert.AreEqual(inradius / 2f, HexGrid.EdgeDistance(c + new Vector2(inradius / 2f, 0f), Size), 1e-2f);
    }

    [Test]
    public void NearestCentreAndToWorldInvertTheGridMapping()
    {
        var c = HexGrid.Centre(-2, 5, Size);
        Assert.AreEqual(c, HexGrid.NearestCentre(c + new Vector2(1000f, -800f), Size), "a point inside a hex rounds to its centre");
        var origin = new Vector3(10f, 5f, 20f); const float length = 431000f;
        var world = HexGrid.ToWorld(c, origin, length);
        Assert.AreEqual(c.x, HexGrid.FromWorld(world, origin, length).x, 1e-2f);
        Assert.AreEqual(c.y, HexGrid.FromWorld(world, origin, length).y, 1e-2f);
        Assert.Less(HexGrid.ToWorld(new Vector2(-4330f, 100f), origin, length).x, origin.x, "west of the map stays west");
    }

    /// <summary>The shader's layout is tools/hexgrid.py's: every hex centre in hexes.json lies on a grid centre.</summary>
    [Test]
    public void ItAgreesWithEveryHexCentreInHexesJson()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        foreach (var h in map.Hexes.hexes)
        {
            var p = HexGrid.FromWorld(map.Frame.ToWorld(h.lon, h.lat, 0), map.Terrain.transform.position, map.Frame.LengthM);
            Assert.AreEqual(0f, (p - HexGrid.Centre(h.q, h.r, size)).magnitude, 50f, $"hex ({h.q}, {h.r}) centre");
            Assert.AreEqual(inradius, HexGrid.EdgeDistance(p, size), 50f, $"hex ({h.q}, {h.r})");
        }
    }

    static (MapScene map, Camera cam, CameraRig rig) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = Camera.main;
        var rig = cam.GetComponent<CameraRig>();
        TiltShift(false);
        return (map, cam, rig);
    }

    /// <summary>Rendered: the middle of a land hex's east edge changes with the grid on (the promo's land lines are light).</summary>
    [UnityTest]
    public IEnumerator GridLinesShowOnHexEdges()
    {
        var (map, cam, rig) = Open();
        yield return null;
        var hex = System.Array.Find(map.Hexes.hexes, h => h.land && h.biome == "hills");
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var edge = centre + new Vector3(Mathf.Sqrt(3f) / 2f * map.Hexes.hex_size_km * 1000f, 0f, 0f);
        edge.y = map.Terrain.SampleHeight(edge);
        rig.LookAt = centre; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[1];
        var on = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", false);
        var off = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", true);
        float change = 0f;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At((int)px[0].x + dx, (int)px[0].y + dy)) - RenderSample.Luma(off.At((int)px[0].x + dx, (int)px[0].y + dy))));
        TestContext.WriteLine($"edge luma change {change:0.000}");
        Assert.Greater(change, 0.03f);
    }

    /// <summary>Rendered at the closest zoom and the overview (250 km): lines are visible but cover little of the view
    /// (no moiré wash at the overview, still drawn up close).</summary>
    [UnityTest]
    public IEnumerator GridLinesStayThinAtBothZoomExtremes()
    {
        var (map, cam, rig) = Open();
        yield return null;
        // Look at the middle of a hex edge (review minor 2: a close view spans less than a few hexes, so an arbitrary point
        // might show no line at all).
        float size = map.Hexes.hex_size_km * 1000f;
        var origin = map.Terrain.transform.position;
        var centre = HexGrid.NearestCentre(HexGrid.FromWorld(map.Frame.ToWorld(35.0, 31.6, 0), origin, map.Frame.LengthM), size);
        var edgeMid = HexGrid.ToWorld(centre + new Vector2(Mathf.Sqrt(3f) / 2f * size, 0f), origin, map.Frame.LengthM);
        edgeMid.y = map.Terrain.SampleHeight(edgeMid);
        foreach (var distance in new[] { CameraController.MinDistance, CameraController.MaxDistance })
        {
            rig.LookAt = edgeMid; rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var on = RenderSample.Render(cam, 960, 540);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 960, 540);
            VisualToggles.Set("grid", true);
            float covered = RenderSample.FractionDarker(on, off, 0.02f) + RenderSample.FractionDarker(off, on, 0.02f);
            TestContext.WriteLine($"{distance} m: grid covers {covered:P2}");
            Assert.That(covered, Is.InRange(0.0005f, 0.25f), $"{distance} m");
        }
    }

    [Test]
    public void TheLineMeshAndItsMaterialAreGone()
    {
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexOverlay.mat"));
        var map = new GameObject("m").AddComponent<MapScene>();
        try { Assert.IsNull(new SerializedObject(map).FindProperty("overlayMaterial")); }
        finally { Object.DestroyImmediate(map.gameObject); }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var built = Object.FindFirstObjectByType<MapScene>(); built.Build();
        Assert.IsNull(built.Terrain.transform.Find("HexOverlay"));
        CollectionAssert.Contains(VisualToggles.Names, "grid");
    }
}
