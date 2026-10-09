using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SettlementTests
{
    static readonly (double, double, double, double) Bounds = (34.20, 35.90, 29.45, 33.35); // heightmap.json lon0, lon1, lat0, lat1

    static string Entry(string id, string extra = "lon: 35, lat: 32, type: town, founded: 1900, sources: [{url: u, checked: x}]") =>
        $"- {{id: {id}, name: N, {extra}}}\n";

    static InvalidDataException Rejects(string yaml) =>
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n" + yaml, Bounds));

    [Test]
    public void ParsesYamlAndRejectsUnknownType()
    {
        var ok = SettlementPlacer.Parse("settlements:\n- {id: jlm, name: Jerusalem, lon: 35.2256, lat: 31.7789, type: city, founded: null, pre_modern: true, sources: [{url: u, checked: '2026-10-09'}]}\n");
        Assert.AreEqual("Jerusalem", ok[0].Name); Assert.IsNull(ok[0].Founded); Assert.IsTrue(ok[0].PreModern);
        Assert.Throws<InvalidDataException>(() => SettlementPlacer.Parse("settlements:\n- {id: q, name: Q, lon: 35, lat: 32, type: castle, founded: 1900, sources: [{url: u, checked: x}]}\n"));
    }

    [Test]
    public void UnknownTypeNamesIdAndField()
    {
        var ex = Rejects(Entry("q", "lon: 35, lat: 32, type: castle, founded: 1900, sources: [{url: u, checked: x}]"));
        StringAssert.Contains("'q'", ex.Message); StringAssert.Contains("type", ex.Message);
    }

    [Test]
    public void DuplicateIdIsRejected()
    {
        var ex = Rejects(Entry("a") + Entry("a"));
        StringAssert.Contains("'a'", ex.Message); StringAssert.Contains("id", ex.Message);
    }

    [Test]
    public void NonFiniteCoordinateIsRejected()
    {
        var ex = Rejects(Entry("n", "lon: .nan, lat: 32, type: town, founded: 1900, sources: [{url: u, checked: x}]"));
        StringAssert.Contains("'n'", ex.Message); StringAssert.Contains("lon", ex.Message);
    }

    [Test]
    public void CoordinateOutsideHeightmapBoundsIsRejected()
    {
        var ex = Rejects(Entry("cy", "lon: 33.36, lat: 35.17, type: town, founded: 1900, sources: [{url: u, checked: x}]")); // Nicosia
        StringAssert.Contains("'cy'", ex.Message); StringAssert.Contains("lon", ex.Message);
        ex = Rejects(Entry("s", "lon: 35, lat: 29.40, type: town, founded: 1900, sources: [{url: u, checked: x}]"));
        StringAssert.Contains("'s'", ex.Message); StringAssert.Contains("lat", ex.Message);
    }

    [Test]
    public void EmptySourcesIsRejected()
    {
        var ex = Rejects(Entry("e", "lon: 35, lat: 32, type: town, founded: 1900, sources: []"));
        StringAssert.Contains("'e'", ex.Message); StringAssert.Contains("sources", ex.Message);
    }

    [Test]
    public void NullFoundedNeedsPreModern()
    {
        var ex = Rejects(Entry("f", "lon: 35, lat: 32, type: town, founded: null, sources: [{url: u, checked: x}]"));
        StringAssert.Contains("'f'", ex.Message); StringAssert.Contains("founded", ex.Message);
    }

    [Test]
    public void RealSettlementFileParses()
    {
        var list = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", "settlements_1948.yaml")), Bounds);
        Assert.AreEqual(25, list.Count);
    }

    // --- MapScene (built in edit mode with the editor-made material assets) ---

    const string TerrainMatPath = "Assets/Materials/Terrain.mat", OverlayMatPath = "Assets/Materials/HexOverlay.mat",
        MarkerMatPath = "Assets/Materials/SettlementMarker.mat";

    static Material Load(string path) { var m = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.IsNotNull(m, path); return m; }

    static (MapScene map, GameObject cam) NewMap()
    {
        var cam = new GameObject("TestMainCamera") { tag = "MainCamera" }; cam.AddComponent<Camera>();
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load(TerrainMatPath), Load(OverlayMatPath), Load(MarkerMatPath));
        return (map, cam);
    }

    // Camera.main may be a camera of the test runner's scene rather than ours, so remove every rig Build added.
    static void Cleanup(MapScene map, GameObject cam)
    {
        Object.DestroyImmediate(map.gameObject);
        foreach (var rig in Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None)) Object.DestroyImmediate(rig);
        Object.DestroyImmediate(cam);
    }

    [Test]
    public void BuildWithoutMaterialsNamesTheMissingField()
    {
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        try
        {
            map.Configure(Load(TerrainMatPath), Load(OverlayMatPath), null);
            var ex = Assert.Throws<System.InvalidOperationException>(() => map.Build());
            StringAssert.Contains("markerMaterial", ex.Message);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void BuildTwiceLeavesOneTerrainOneSettlementsRootAndOneVolume()
    {
        var (map, cam) = NewMap();
        try
        {
            map.Build(); map.Build();
            Assert.AreEqual(1, Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length, "terrains");
            Assert.AreEqual(1, Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length, "volumes");
            int roots = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == "Settlements") roots++;
            Assert.AreEqual(1, roots, "Settlements roots");
            Assert.AreEqual(1, Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None).Length, "camera rigs"); // on Camera.main
        }
        finally { Cleanup(map, cam); }
    }

    [Test]
    public void EveryMarkerSitsOnItsSourcedPositionWithTheMarkerMaterial()
    {
        var (map, cam) = NewMap();
        try
        {
            map.Build();
            var marker = Load(MarkerMatPath);
            Assert.AreEqual("Universal Render Pipeline/Lit", marker.shader.name);
            Assert.AreEqual(0f, marker.GetFloat("_Smoothness"), "matte marker");
            var root = GameObject.Find("Settlements").transform;
            var list = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", "settlements_1948.yaml")), Bounds);
            foreach (var s in list)
            {
                var box = root.Find(s.Id); Assert.IsNotNull(box, s.Id);
                var want = map.Frame.ToWorld(s.Lon, s.Lat, 0);
                Assert.AreEqual(want.x, box.position.x, 1f, s.Id + " x"); Assert.AreEqual(want.z, box.position.z, 1f, s.Id + " z");
                float baseY = box.position.y - box.lossyScale.y / 2;
                Assert.AreEqual(map.Terrain.SampleHeight(box.position), baseY, 1f, s.Id + " base y");
                Assert.AreSame(marker, box.GetComponent<Renderer>().sharedMaterial, s.Id + " material");
            }
            var telAviv = root.Find("tel_aviv").position; var jerusalem = root.Find("jerusalem").position; var haifa = root.Find("haifa").position;
            Assert.Less(telAviv.x, jerusalem.x, "Tel Aviv west of Jerusalem");
            Assert.Greater(haifa.z, telAviv.z, "Haifa north of Tel Aviv"); Assert.Greater(haifa.z, jerusalem.z, "Haifa north of Jerusalem");
        }
        finally { Cleanup(map, cam); }
    }
}
