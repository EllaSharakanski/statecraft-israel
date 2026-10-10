using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettlementTests
{
    static readonly HeightmapMeta Meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
    static (double, double, double, double) Bounds => (Meta.lon0, Meta.lon1, Meta.lat0, Meta.lat1);
    static string RealYamlPath => Path.Combine(Paths.Content, "map", "settlements_1948.yaml");

    const string Src = "sources: [{url: u, checked: x}]";

    static string Entry(string id, string extra = "lon: 35, lat: 32, type: town, founded: 1900, " + Src) =>
        $"- {{id: {id}, name: N, {extra}}}\n";

    static InvalidDataException Rejects(string entries) =>
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n" + entries, Bounds));

    static void AssertNames(InvalidDataException ex, string id, string field)
    {
        StringAssert.Contains(SettlementPlacer.DefaultFileName, ex.Message);
        StringAssert.Contains($"'{id}'", ex.Message); StringAssert.Contains(field, ex.Message);
    }

    [Test]
    public void ParsesYamlAndRejectsUnknownType()
    {
        var ok = SettlementPlacer.Parse("settlements:\n- {id: jlm, name: Jerusalem, lon: 35.2256, lat: 31.7789, type: city, founded: null, pre_modern: true, sources: [{url: u, checked: '2026-10-09'}]}\n");
        Assert.AreEqual("Jerusalem", ok[0].Name); Assert.IsNull(ok[0].Founded); Assert.IsTrue(ok[0].PreModern);
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n- {id: q, name: Q, lon: 35, lat: 32, type: castle, founded: 1900, sources: [{url: u, checked: x}]}\n"));
    }

    [Test] public void UnknownTypeNamesIdAndField() =>
        AssertNames(Rejects(Entry("q", "lon: 35, lat: 32, type: castle, founded: 1900, " + Src)), "q", "type");

    [Test] public void DuplicateIdIsRejected() => AssertNames(Rejects(Entry("a") + Entry("a")), "a", "id");

    [Test] public void NonFiniteCoordinateIsRejected() =>
        AssertNames(Rejects(Entry("n", "lon: .nan, lat: 32, type: town, founded: 1900, " + Src)), "n", "lon");

    [Test]
    public void CoordinateOutsideHeightmapBoundsIsRejected()
    {
        AssertNames(Rejects(Entry("cy", "lon: 33.36, lat: 35.17, type: town, founded: 1900, " + Src)), "cy", "lon"); // Nicosia
        AssertNames(Rejects(Entry("s", "lon: 35, lat: 29.40, type: town, founded: 1900, " + Src)), "s", "lat");
    }

    [Test] public void MissingLonIsRejected() => AssertNames(Rejects(Entry("m", "lat: 32, type: town, founded: 1900, " + Src)), "m", "lon");
    [Test] public void MissingLatIsRejected() => AssertNames(Rejects(Entry("m", "lon: 35, type: town, founded: 1900, " + Src)), "m", "lat");

    [Test] public void EmptySourcesIsRejected() =>
        AssertNames(Rejects(Entry("e", "lon: 35, lat: 32, type: town, founded: 1900, sources: []")), "e", "sources");

    [Test] public void EmptySourceUrlIsRejected() =>
        AssertNames(Rejects(Entry("u", "lon: 35, lat: 32, type: town, founded: 1900, sources: [{url: '', checked: x}]")), "u", "url");

    [Test] public void EmptySourceCheckedIsRejected() =>
        AssertNames(Rejects(Entry("c", "lon: 35, lat: 32, type: town, founded: 1900, sources: [{url: u, checked: ''}]")), "c", "checked");

    [Test] public void NullFoundedNeedsPreModern() =>
        AssertNames(Rejects(Entry("f", "lon: 35, lat: 32, type: town, founded: null, " + Src)), "f", "founded");

    [Test]
    public void EmptyIdIsRejected()
    {
        var ex = Rejects("- {id: '', name: N, lon: 35, lat: 32, type: town, founded: 1900, " + Src + "}\n");
        StringAssert.Contains(SettlementPlacer.DefaultFileName, ex.Message); StringAssert.Contains("id", ex.Message);
    }

    [Test] public void EmptyNameIsRejected() =>
        AssertNames(Rejects("- {id: nn, name: '', lon: 35, lat: 32, type: town, founded: 1900, " + Src + "}\n"), "nn", "name");

    [Test]
    public void UnknownKeyAndBadScalarBecomeInvalidDataNamingTheFile()
    {
        StringAssert.Contains(SettlementPlacer.DefaultFileName, Rejects(Entry("k", "lon: 35, lat: 32, type: town, founded: 1900, colour: red, " + Src)).Message);
        StringAssert.Contains(SettlementPlacer.DefaultFileName, Rejects(Entry("b", "lon: east, lat: 32, type: town, founded: 1900, " + Src)).Message);
    }

    // Final fix 3, M5: duplicate keys and malformed ids name the file, the id and the field.
    [Test]
    public void DuplicateKeyIsRejectedNamingIdAndField()
    {
        var ex = Rejects(Entry("a") + Entry("d", "lon: 35, lon: 35.5, lat: 32, type: town, founded: 1900, " + Src));
        AssertNames(ex, "d", "lon"); StringAssert.Contains("duplicate", ex.Message);
    }

    [Test]
    public void DuplicateKeyInBlockStyleIsRejectedNamingIdAndField()
    {
        var ex = Rejects("- id: b\n  name: N\n  lon: 35\n  lat: 32\n  type: town\n  type: city\n  founded: 1900\n  " + Src + "\n");
        AssertNames(ex, "b", "type"); StringAssert.Contains("duplicate", ex.Message);
    }

    [TestCase("Tel_Aviv")]
    [TestCase("tel-aviv")]
    [TestCase("9a")]
    [TestCase("_a")]
    [TestCase("'a b'")]
    public void IdThatIsNotSnakeCaseIsRejected(string id)
    {
        var ex = Rejects(Entry(id));
        AssertNames(ex, id.Trim('\''), "id");
    }

    [Test]
    public void RealSettlementFileParses()
    {
        var list = SettlementPlacer.Parse(File.ReadAllText(RealYamlPath), Bounds);
        Assert.AreEqual(25, list.Count);
    }

}

/// <summary>MapScene, built in edit mode with the editor-made material assets. Each test runs in a fresh empty scene so it
/// never touches the runner's own camera (R4). The scene replaces the runner's untitled one (NewSceneMode.Single):
/// Unity refuses an additive new scene while an untitled scene is unsaved, which is the state batch test runs start in.</summary>
public class MapSceneTests
{
    static readonly HeightmapMeta Meta = HeightmapLoader.Load(Paths.StreamingMap).meta;
    static (double, double, double, double) Bounds => (Meta.lon0, Meta.lon1, Meta.lat0, Meta.lat1);
    static string RealYamlPath => Path.Combine(Paths.Content, "map", "settlements_1948.yaml");

    const string TerrainMatPath = "Assets/Materials/Terrain.mat", OverlayMatPath = "Assets/Materials/HexOverlay.mat",
        MarkerMatPath = "Assets/Materials/SettlementMarker.mat", SelectionMatPath = "Assets/Materials/HexSelection.mat";

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    // Replacing the scene destroys everything the test built (MapRuntimeAssets frees the runtime assets with it).
    [TearDown] public void DiscardScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static Material Load(string path) { var m = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.IsNotNull(m, path); return m; }

    static MapScene NewMap(bool withCamera = true)
    {
        if (withCamera) new GameObject("TestMainCamera") { tag = "MainCamera" }.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), Load(OverlayMatPath), Load(MarkerMatPath), Load(SelectionMatPath));
        return map;
    }

    static int CountNamed(string name)
    {
        int n = 0; foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == name) n++;
        return n;
    }

    static void AssertOneOfEach()
    {
        Assert.AreEqual(1, Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, "terrains");
        Assert.AreEqual(1, Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length, "volumes");
        Assert.AreEqual(1, CountNamed("Settlements"), "Settlements roots");
        Assert.AreEqual(1, Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None).Length, "camera rigs");
    }

    /// <summary>Playtest 1, item 9: beyond the terrain the view shows sea, not a grey void. The diorama pitch never shows
    /// the sky, so the camera clears to the sea biome colour.</summary>
    [Test]
    public void TheCameraClearsToTheSeaColour()
    {
        var map = NewMap(); map.Build();
        var cam = Camera.main;
        Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags);
        Assert.AreEqual(MapBuilder.BiomeColours["sea"], cam.backgroundColor);
        // Fix round 1 (review M5): the top-bar inset shifts the frustum up, so check the worst case the rig allows: the
        // top ray rises to atan((1 + inset) tan(Fov/2)) above the axis. Analytically, and on the camera's own rays.
        var worst = Mathf.Rad2Deg * Mathf.Atan((1 + CameraRig.MaxTopInset) * Mathf.Tan(Mathf.Deg2Rad * CameraRig.Fov / 2));
        Assert.Greater(CameraRig.Pitch - worst, 0f, "the top of the view looks down at the largest inset: no sky to show");
        var rig = cam.GetComponent<CameraRig>();
        rig.TopInset = 1f; rig.Apply();                                    // clamped to MaxTopInset
        foreach (var x in new[] { 0f, 0.5f, 1f })
            Assert.Less(cam.ViewportPointToRay(new Vector3(x, 1f, 0)).direction.y, 0f, $"top edge at x={x} looks down");
    }

    /// <summary>Playtest 1, item 3: with the top bar covering the top <c>TopInset</c> of the screen, the look-at point is
    /// framed in the middle of the map area below the bar, not in the middle of the whole screen.</summary>
    [Test]
    public void TheLookAtPointIsCentredBelowTheTopBar()
    {
        var map = NewMap(); map.Build();
        var cam = Camera.main;
        var rig = cam.GetComponent<CameraRig>();
        Assert.AreEqual(0.5f, cam.WorldToViewportPoint(rig.LookAt).y, 1e-3f, "no inset: the screen's centre");
        rig.TopInset = 0.1f; rig.Apply();
        var v = cam.WorldToViewportPoint(rig.LookAt);
        Assert.AreEqual(0.45f, v.y, 1e-3f, "the middle of the 90% below the bar (viewport y is up)");
        Assert.AreEqual(0.5f, v.x, 1e-3f);
        rig.TopInset = 0f; rig.Apply();
        Assert.AreEqual(0.5f, cam.WorldToViewportPoint(rig.LookAt).y, 1e-3f, "the inset is undone");
    }

    /// <summary>Playtest 1, item 10: the selected hex gets an outline in the highlight colour, draped on the terrain like
    /// the hex overlay (HexMath corners), drawn with the editor-made overlay material (no runtime material).</summary>
    [Test]
    public void SelectingAHexOutlinesItOnTheTerrain()
    {
        var map = NewMap(); map.Build();
        var selection = Load(SelectionMatPath);
        // Fix round 1, M2: its own editor-made material, whose shader pulls the band towards the camera in depth (Offset), so
        // the overlay lines under its edge cannot z-fight with it at long range.
        Assert.AreEqual("Statecraft/HexSelection", selection.shader.name);
        StringAssert.IsMatch(@"(?m)^\s*Offset\s+-\d+\s*,\s*-\d+", File.ReadAllText(AssetDatabase.GetAssetPath(selection.shader)));
        Assert.Greater(selection.renderQueue, Load(OverlayMatPath).renderQueue, "drawn after the overlay lines");
        float size = map.Hexes.hex_size_km * 1000f;
        bool Inland(HexJson h)   // whole hex on the terrain, so no corner is clamped to the map's edge
        {
            var w = map.Frame.ToWorld(h.lon, h.lat, 0);
            return w.x > size && w.x < map.Frame.WidthM - size && w.z > size && w.z < map.Frame.LengthM - size;
        }
        // The roughest inland hexes: the band must clear the ground across its whole width there, not just at its edges.
        var land = System.Array.FindAll(map.Hexes.hexes, h => h.land && (h.biome == "mountains" || h.biome == "hills") && Inland(h));
        Assert.Greater(land.Length, 2);
        foreach (var h in land)
        {
            map.Select(h.q, h.r);
            var sel = map.Terrain.transform.Find(MapScene.SelectionName);
            Assert.IsNotNull(sel, "the selection outline exists");
            var mr = sel.GetComponent<MeshRenderer>();
            Assert.IsTrue(mr.enabled, "the outline is shown");
            Assert.AreSame(selection, mr.sharedMaterial, "the editor-made selection material, not an instance");
            var verts = sel.GetComponent<MeshFilter>().sharedMesh.vertices;
            int n = MapBuilder.RingVertices;
            Assert.AreEqual(2 * n, verts.Length);
            var centre = map.Frame.ToWorld(h.lon, h.lat, 0);
            var c2 = new Vector2(centre.x, centre.z);
            for (int i = 0; i < 6; i++)
            {
                var corner = HexMath.Corner(c2, size, i);
                Assert.IsTrue(System.Array.Exists(verts, v => (new Vector2(v.x, v.z) - corner).magnitude < 1f), $"corner {i} of ({h.q}, {h.r})");
            }
            for (int j = 0; j < n; j++)
            {
                Assert.LessOrEqual((new Vector2(verts[j].x, verts[j].z) - c2).magnitude, size + 1f, "inside the hex");
                var mid = (verts[j] + verts[n + j]) / 2;    // the middle of the band, between its outer and inner edge
                Assert.Greater(mid.y, map.Terrain.SampleHeight(mid) + MapBuilder.OverlayClearanceM - 1f,
                    $"({h.q}, {h.r}) vertex {j}: the band clears the ground across its width");
            }
        }
        map.Select(-9999, -9999);
        Assert.IsFalse(map.Terrain.transform.Find(MapScene.SelectionName).GetComponent<MeshRenderer>().enabled, "no hex there: no outline");
    }

    [Test]
    public void BuildWithoutMaterialsNamesTheMissingField()
    {
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), Load(OverlayMatPath), null, Load(SelectionMatPath));
        var ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("markerMaterial", ex.Message);
        map.Configure(Load(TerrainMatPath), Load(OverlayMatPath), Load(MarkerMatPath), null);
        ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        StringAssert.Contains("selectionMaterial", ex.Message);
    }

    [Test]
    public void BuildWithoutMainCameraThrows()
    {
        var map = NewMap(withCamera: false);
        Assert.IsNull(Camera.main);
        var ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
        Assert.AreEqual("MapScene needs a camera tagged MainCamera", ex.Message);
    }

    [Test]
    public void BuildTwiceLeavesOneTerrainOneSettlementsRootAndOneVolume()
    {
        var map = NewMap();
        map.Build(); map.Build();
        AssertOneOfEach();
    }

    [Test]
    public void RebuildByAFreshComponentStillClearsTheOldMap()
    {
        // After a domain reload a MapScene has lost any non-serialized references to what it built (R2).
        var map = NewMap(); map.Build();
        var go = map.gameObject; Object.DestroyImmediate(map);
        var fresh = go.AddComponent<MapScene>();
        fresh.Configure(Load(TerrainMatPath), Load(OverlayMatPath), Load(MarkerMatPath), Load(SelectionMatPath));
        fresh.Build();
        AssertOneOfEach();
    }

    [Test]
    public void RebuildingDoesNotLeakDepthOfFieldComponents()
    {
        var map = NewMap(); map.Build();
        int after1 = Resources.FindObjectsOfTypeAll<DepthOfField>().Length;
        map.Build(); map.Build();
        Assert.AreEqual(after1, Resources.FindObjectsOfTypeAll<DepthOfField>().Length);
    }

    [Test]
    public void EveryMarkerSitsOnItsSourcedPositionWithTheMarkerMaterial()
    {
        var map = NewMap(); map.Build();
        var marker = Load(MarkerMatPath);
        Assert.AreEqual("Universal Render Pipeline/Lit", marker.shader.name);
        Assert.AreEqual(0f, marker.GetFloat("_Smoothness"), "matte marker");

        // The frame comes from the heightmap meta, independently of the code under test (R5).
        Assert.AreEqual(Meta.lon0, map.Frame.Lon0); Assert.AreEqual(Meta.lon1, map.Frame.Lon1);
        Assert.AreEqual(Meta.lat0, map.Frame.Lat0); Assert.AreEqual(Meta.lat1, map.Frame.Lat1);
        Assert.AreEqual(Meta.width_km * 1000f, map.Frame.WidthM, 1f); Assert.AreEqual(Meta.length_km * 1000f, map.Frame.LengthM, 1f);

        var root = GameObject.Find("Settlements").transform;
        foreach (var s in SettlementPlacer.Parse(File.ReadAllText(RealYamlPath), Bounds))
        {
            var box = root.Find(s.Id); Assert.IsNotNull(box, s.Id);
            var want = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            Assert.AreEqual(want.x, box.position.x, 1f, s.Id + " x"); Assert.AreEqual(want.z, box.position.z, 1f, s.Id + " z");
            float baseY = box.position.y - box.lossyScale.y / 2;
            Assert.AreEqual(map.Terrain.SampleHeight(box.position), baseY, 1f, s.Id + " base y");
            Assert.AreSame(marker, box.GetComponent<Renderer>().sharedMaterial, s.Id + " material");
            Assert.IsNull(box.GetComponent<Collider>(), s.Id + " has a collider (R1)");
        }
        var jerusalem = root.Find("jerusalem").position;
        Assert.AreEqual((35.2256 - Meta.lon0) / (Meta.lon1 - Meta.lon0) * Meta.width_km * 1000.0, jerusalem.x, 1.0, "Jerusalem x, absolute");
        Assert.AreEqual((31.7789 - Meta.lat0) / (Meta.lat1 - Meta.lat0) * Meta.length_km * 1000.0, jerusalem.z, 1.0, "Jerusalem z, absolute");
        var telAviv = root.Find("tel_aviv").position; var haifa = root.Find("haifa").position;
        Assert.Less(telAviv.x, jerusalem.x, "Tel Aviv west of Jerusalem");
        Assert.Greater(haifa.z, telAviv.z, "Haifa north of Tel Aviv"); Assert.Greater(haifa.z, jerusalem.z, "Haifa north of Jerusalem");
    }

    [Test]
    public void LabelsAreLightWithADarkShadowAboveTheMarkerFacingTheRig()
    {
        var map = NewMap(); map.Build();
        var root = GameObject.Find("Settlements").transform;
        var cam = Camera.main.transform;
        foreach (var s in SettlementPlacer.Parse(File.ReadAllText(RealYamlPath), Bounds))
        {
            var box = root.Find(s.Id); var label = root.Find(s.Id + "_label"); Assert.IsNotNull(label, s.Id + " label");
            var text = label.GetComponent<TextMesh>(); Assert.AreEqual(s.Name, text.text);
            Assert.Greater(Mathf.Min(text.color.r, text.color.g, text.color.b), 0.85f, s.Id + " label is light");
            Assert.AreEqual(CameraRig.Pitch, label.eulerAngles.x, 0.01f, s.Id + " label pitch");
            // The whole label sits above the marker's top in screen terms: its base is above every point of the box.
            var top = box.position + Vector3.up * box.lossyScale.y / 2;
            var farTopEdge = top + Vector3.forward * box.lossyScale.z / 2;
            Assert.Greater(Vector3.Dot(label.position - farTopEdge, cam.up), 0f, s.Id + " label base above the box's far top edge");
            var shadow = label.Find("shadow"); Assert.IsNotNull(shadow, s.Id + " shadow");
            var st = shadow.GetComponent<TextMesh>(); Assert.AreEqual(s.Name, st.text);
            Assert.Less(Mathf.Max(st.color.r, st.color.g, st.color.b), 0.2f, s.Id + " shadow is dark");
            Assert.Greater(Vector3.Dot(shadow.position - label.position, cam.forward), 0f, s.Id + " shadow behind the label");
        }
    }

    [Test]
    public void CaptureOfASceneWithoutMapSceneNamesTheSceneAndTheFix()
    {
        // MapCapture lives in the predefined editor assembly, which an asmdef cannot reference, so reach it by reflection.
        var capture = System.Type.GetType("MapCapture, Assembly-CSharp-Editor");
        Assert.IsNotNull(capture, "MapCapture type");
        var require = capture.GetMethod("RequireMapScene"); Assert.IsNotNull(require, "MapCapture.RequireMapScene");
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => require.Invoke(null, null));
        Assert.IsInstanceOf<System.InvalidOperationException>(ex.InnerException);
        StringAssert.Contains("Assets/Map.unity", ex.InnerException.Message);
        StringAssert.Contains("MakeScene", ex.InnerException.Message);
    }

    static readonly string[] TelAvivCluster = { "tel_aviv", "jaffa", "petah_tikva", "rishon_lezion", "lod", "ramla", "rehovot" };

    static Rect ScreenRect(Camera cam, Bounds b)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var p = cam.WorldToScreenPoint(c);
            x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    static float Gap(Rect a, Rect b) =>
        new Vector2(Mathf.Max(0, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax)), Mathf.Max(0, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax))).magnitude;

    [Test]
    public void EveryTelAvivClusterLabelIsUnambiguouslyClosestToItsOwnMarker()
    {
        // At flythrough frame 120 (1080p), each label's screen box must be at most half as far from its own marker as from
        // any other marker of the cluster, so no label reads as a neighbour's.
        var map = NewMap(); map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt;
        try
        {
            rig.LookAt = map.Frame.ToWorld(34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 0); rig.Distance = 90000f; rig.Apply();
            var root = GameObject.Find("Settlements").transform;
            foreach (var id in TelAvivCluster)
            {
                var label = ScreenRect(cam, root.Find(id + "_label").GetComponent<MeshRenderer>().bounds);
                float own = Gap(label, ScreenRect(cam, root.Find(id).GetComponent<MeshRenderer>().bounds));
                foreach (var other in TelAvivCluster)
                {
                    if (other == id) continue;
                    float gap = Gap(label, ScreenRect(cam, root.Find(other).GetComponent<MeshRenderer>().bounds));
                    Assert.Greater(gap, 2f * own + 4f, $"{id} label is {own:F0} px from its marker but {gap:F0} px from {other}'s");
                }
            }
        }
        finally { cam.targetTexture = null; Object.DestroyImmediate(rt); }
    }
}
