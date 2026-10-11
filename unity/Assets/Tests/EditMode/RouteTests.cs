using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Routes;
using Statecraft.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>The 1948 railways and roads (issue #35): the traced data, the ribbons, the train, the toggles and the look.</summary>
public class RouteTests
{
    static readonly (double, double, double, double) Bounds = (34.2, 35.9, 29.45, 33.35);
    static string CommittedYaml => File.ReadAllText(Path.Combine(Paths.Content, "map", RoutePaths.DefaultFileName));

    [Test]
    public void TheCommittedRoutesParse()
    {
        var routes = RoutePaths.Parse(CommittedYaml, Bounds);
        Assert.GreaterOrEqual(routes.Count, 19);
        var ids = routes.Select(r => r.Id).ToList();
        CollectionAssert.IsSubsetOf(new[] { "jaffa_jerusalem_railway", "kantara_main_line_south", "eastern_railway",
            "hadera_haifa_railway", "jaffa_jerusalem_road", "burma_road", "tel_aviv_haifa_road", "coastal_road_south" }, ids);
        Assert.AreEqual(RoutePaths.Rail, routes.First(r => r.Id == RouteLayer.TrainRoute).Kind);
        Assert.IsTrue(routes.All(r => r.Status == "draft" || r.Status == "fact-checked"));
    }

    [TestCase("routes: []", "no routes")]
    [TestCase("routes:\n- {id: a, kind: canal, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "unknown kind 'canal'")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5]]}", "'a' needs at least two points")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5], [40.0, 31.6]]}", "'a' point 1 (40, 31.6) is outside the map")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6]]}", "'a' point 1 is not a [lon, lat] pair")]
    [TestCase("routes:\n- {id: Bad, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'Bad' id must be lower snake_case")]
    [TestCase("routes:\n- {id: a, kind: road, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}\n- {id: a, kind: rail, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "'a' duplicate id")]
    [TestCase("routes:\n- {id: a, kind: road, colour: red, status: draft, length_km: 1, points: [[34.5, 31.5], [34.6, 31.6]]}", "colour")]
    public void BadRouteFilesNameTheFileAndTheProblem(string yaml, string expected)
    {
        var e = Assert.Throws<InvalidDataException>(() => RoutePaths.Parse(yaml, Bounds, "r.yaml"));
        StringAssert.StartsWith("r.yaml:", e.Message);
        StringAssert.Contains(expected, e.Message);
    }

    /// <summary>Map-only: the sim's content hash is the same with and without the route files (so neither moves the
    /// goldens), and the WebGL manifest ships the traced file the map reads.</summary>
    [Test]
    public void TheRoutesDoNotMoveTheContentHash()
    {
        var copy = Path.Combine(Path.GetTempPath(), "statecraft-noroutes-" + System.Guid.NewGuid().ToString("N"));
        try
        {
            Statecraft.Game.Boot.ContentManifest.Copy(Paths.Content, copy);
            foreach (var f in Directory.GetFiles(Path.Combine(copy, "map"), "route*_1948.yaml")) File.Delete(f);
            Assert.AreEqual(ContentLoader.Load(Paths.Content).Hash, ContentLoader.Load(copy).Hash);
            CollectionAssert.Contains(Statecraft.Game.Boot.ContentManifest.Collect(Paths.Content), "map/" + RoutePaths.DefaultFileName);
        }
        finally { if (Directory.Exists(copy)) Directory.Delete(copy, true); }
    }

    [Test]
    public void ResampleKeepsTheEndsAndTheSpacing()
    {
        var (pts, along) = RouteLayer.Resample(new List<Vector2> { new Vector2(0, 0), new Vector2(250, 0), new Vector2(250, 100) }, 120f);
        Assert.AreEqual(new Vector2(0, 0), pts[0]);
        Assert.AreEqual(new Vector2(250, 100), pts[pts.Count - 1]);
        Assert.AreEqual(350f, along[along.Count - 1], 1e-3f);
        for (int i = 1; i + 1 < along.Count; i++) Assert.AreEqual(120f * i, along[i], 1e-3f);
    }

    static MapScene BuildMap()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("BorderLand"));
        map.Build();
        return map;
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

    static Transform RoutesRoot(MapScene map) => map.Terrain.transform.Find(RouteLayer.RootName);

    [Test]
    public void TheMapBuildsRibbonsOnTheGroundAndTheTogglesHideThem()
    {
        var map = BuildMap();
        List<Object> owned;
        try
        {
            var root = RoutesRoot(map);
            Assert.IsNotNull(root, "MapScene.Build adds the routes under the terrain (a rebuild destroys them)");
            var rail = root.Find($"{RouteLayer.RibbonsName}/Rail").GetComponent<MeshFilter>().sharedMesh;
            var roads = root.Find($"{RouteLayer.RibbonsName}/Roads").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(Resources.Load<Material>(RouteLayer.RailMaterialResource), root.Find($"{RouteLayer.RibbonsName}/Rail").GetComponent<MeshRenderer>().sharedMaterial);
            // Budget: well under 100k triangles in all (WebGL's integrated GPUs draw millions; two draw calls).
            int tris = (int)((rail.GetIndexCount(0) + roads.GetIndexCount(0)) / 3);
            TestContext.WriteLine($"rail {rail.vertexCount} vertices, roads {roads.vertexCount}, {tris} triangles");
            Assert.Less(tris, 100000);
            // The meshes are freed after upload (no CPU copy), so check a fresh build of the same data: every vertex
            // clears the ground under it by the clearance, and lies on the terrain.
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            var check = RouteLayer.Ribbon(routes.Where(r => r.Kind == RoutePaths.Rail).Select(r => (RouteLayer.World(map.Frame, r), RouteLayer.RailWidthM)), data, origin, "check");
            try
            {
                Assert.AreEqual(rail.vertexCount, check.vertexCount, "the same data gives the same mesh");
                foreach (var v in check.vertices)
                {
                    Assert.GreaterOrEqual(v.y - RouteLayer.Ground(data, origin, new Vector2(v.x, v.z)), RouteLayer.ClearanceM - 0.01f);
                    Assert.That(v.x, Is.InRange(origin.x, origin.x + data.size.x)); Assert.That(v.z, Is.InRange(origin.z, origin.z + data.size.z));
                }
                var again = RouteLayer.Ribbon(routes.Where(r => r.Kind == RoutePaths.Rail).Select(r => (RouteLayer.World(map.Frame, r), RouteLayer.RailWidthM)), data, origin, "again");
                CollectionAssert.AreEqual(check.vertices, again.vertices, "deterministic");
                CollectionAssert.AreEqual(check.uv, again.uv, "deterministic");
                Object.DestroyImmediate(again);
            }
            finally { Object.DestroyImmediate(check); }

            CollectionAssert.Contains(VisualToggles.Names, RouteLayer.RoutesToggle);
            CollectionAssert.Contains(VisualToggles.Names, RouteLayer.TrainToggle);
            VisualToggles.Set(RouteLayer.RoutesToggle, false);
            Assert.IsFalse(root.Find(RouteLayer.RibbonsName).gameObject.activeSelf);
            Assert.IsTrue(root.Find("Train").gameObject.activeSelf, "each toggle prices its own part");
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            Assert.IsFalse(root.Find("Train").gameObject.activeSelf);
            VisualToggles.SetAll(true);
            Assert.IsTrue(root.Find(RouteLayer.RibbonsName).gameObject.activeSelf && root.Find("Train").gameObject.activeSelf);
            owned = root.GetComponentsInChildren<MeshFilter>(true).Select(f => (Object)f.sharedMesh).ToList();
            owned.Add(root.Find("Train/Locomotive").GetComponent<MeshRenderer>().sharedMaterial);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
        Assert.IsTrue(owned.All(o => o == null), "destroying the map frees the route meshes and the train's material");
        CollectionAssert.DoesNotContain(VisualToggles.Names, RouteLayer.RoutesToggle, "the map's teardown removes the switches");
        CollectionAssert.DoesNotContain(VisualToggles.Names, RouteLayer.TrainToggle);
    }

    [Test]
    public void TheStillTrainStandsOnTheLineInTheGameplayView()
    {
        var map = BuildMap();
        try
        {
            var train = RoutesRoot(map).GetComponentInChildren<RouteTrain>();
            Assert.IsNotNull(train.StillHead, "an editor build holds the train still (screenshots repeat)");
            Assert.AreEqual(2 + TrainModels.Coaches, train.Cars.Count);
            // Its middle by Mikve Israel, east of Jaffa, as the promo's train.
            var still = map.Frame.ToWorld(RouteTrain.StillAt.lon, RouteTrain.StillAt.lat, 0);
            var mid = Vector3.Lerp(train.Cars[0].position, train.Cars[train.Cars.Count - 1].position, 0.5f);
            Assert.Less(new Vector2(mid.x - still.x, mid.z - still.z).magnitude, 1500f);
            // About the promo's 8.8 km, and each car on the ribbon (ground + clearance + ride), heading inland (east) first.
            Assert.That(train.Length, Is.InRange(6000f, 10000f));
            var data = map.Terrain.terrainData; var origin = map.Terrain.transform.position;
            foreach (var c in train.Cars)
                Assert.AreEqual(RouteLayer.Ground(data, origin, new Vector2(c.position.x, c.position.z)) + RouteLayer.ClearanceM + RouteTrain.RideM,
                    c.position.y, 60f, c.name);
            Assert.Greater(train.Cars[0].position.x, train.Cars[train.Cars.Count - 1].position.x, "the locomotive leads towards Lydda");
            var cam = Camera.main; PromoPose.Gameplay03.Apply(cam.GetComponent<CameraRig>(), map.Frame);
            foreach (var c in train.Cars)
            {
                var vp = cam.WorldToViewportPoint(c.position);
                Assert.That(vp.x, Is.InRange(0.05f, 0.95f), c.name); Assert.That(vp.y, Is.InRange(0.05f, 0.95f), c.name);
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TheTrainShuttlesWithTheLocomotiveLeading()
    {
        var map = BuildMap();
        try
        {
            var train = RoutesRoot(map).GetComponentInChildren<RouteTrain>();
            var (start, end) = train.Stretch;
            Assert.Greater(end - start, 2f * train.Length, "Jaffa to Lydda is longer than the train");
            var (h0, d0) = train.HeadAt(0f);
            Assert.AreEqual(start + train.Length, h0, 1e-2f); Assert.AreEqual(1, d0);
            float leg = (end - start - train.Length) / RouteTrain.SpeedMps + RouteTrain.DwellSeconds;
            var (h1, d1) = train.HeadAt(leg - 0.01f);
            Assert.AreEqual(end, h1, 1f, "at Lydda before turning"); Assert.AreEqual(1, d1);
            var (h2, d2) = train.HeadAt(leg + 0.01f);
            Assert.AreEqual(-1, d2, "back towards Jaffa");
            Assert.AreEqual(end - train.Length, h2, 5f, "the consist stays where it stopped; the locomotive is now at the other end");
            // Smooth: no jump between consecutive frames while running.
            float prev = train.HeadAt(1f).head;
            for (float t = 1f + 1f / 60f; t < leg - RouteTrain.DwellSeconds; t += 1f / 60f)
            {
                float h = train.HeadAt(t).head;
                Assert.Less(Mathf.Abs(h - prev), RouteTrain.SpeedMps / 60f + 0.01f);
                prev = h;
            }
        }
        finally { Object.DestroyImmediate(map.gameObject); }
    }

    [Test]
    public void TrainModelsAreRepeatable()
    {
        var a = TrainModels.Consist(); var b = TrainModels.Consist();
        int tris = 0;
        for (int i = 0; i < a.Count; i++)
        {
            CollectionAssert.AreEqual(a[i].mesh.vertices, b[i].mesh.vertices, a[i].name);
            CollectionAssert.AreEqual(a[i].mesh.triangles, b[i].mesh.triangles, a[i].name);
            tris += a[i].mesh.triangles.Length / 3;
        }
        Assert.Less(tris, 5000, $"train triangles {tris}");
        foreach (var c in a.Concat(b)) Object.DestroyImmediate(c.mesh);
    }

    [Test]
    public void TheRouteShaderRunsOnWebGL2()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/Route")));
        var rail = Resources.Load<Material>(RouteLayer.RailMaterialResource); var road = Resources.Load<Material>(RouteLayer.RoadMaterialResource);
        Assert.AreEqual(1f, rail.GetFloat("_Kind")); Assert.AreEqual(0f, road.GetFloat("_Kind"));
        Assert.Less(rail.renderQueue, road.renderQueue, "the railway draws first, so it stays on top where it crosses a road");
    }

    /// <summary>The look at gameplay03 (promo 03): the railway reads darker than the ground beside it (ballast and
    /// sleepers) and a road lighter (pale dirt), and switching the routes off restores the ground.</summary>
    [Test]
    public void AtTheGameplayPoseTheRailwayIsDarkAndTheRoadPale()
    {
        var map = BuildMap();
        try
        {
            RenderSample.TiltShift(false);
            VisualToggles.Set(RouteLayer.TrainToggle, false);
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            var routes = RoutePaths.Parse(CommittedYaml, Bounds);
            // A railway point and a road point in open country near the pose's centre (east of Tel Aviv).
            Vector3 OnRoute(string id, double lon, double lat)
            {
                var line = RouteLayer.World(map.Frame, routes.First(r => r.Id == id));
                var want = map.Frame.ToWorld(lon, lat, 0);
                var best = line.OrderBy(p => (p - new Vector2(want.x, want.z)).sqrMagnitude).First();
                return new Vector3(best.x, RouteLayer.Ground(map.Terrain.terrainData, map.Terrain.transform.position, best), best.y);
            }
            var railAt = OnRoute("eastern_railway", 34.93, 32.06);
            var roadAt = OnRoute("tel_aviv_tulkarm_nablus_road", 34.92, 32.09);
            var px = new Vector2[2];
            var on = RenderSample.Render(cam, 1920, 1080, new[] { railAt, roadAt }, px);
            VisualToggles.Set(RouteLayer.RoutesToggle, false);
            var off = RenderSample.Render(cam, 1920, 1080);
            Color railOn = on.Mean((int)px[0].x, (int)px[0].y, 1), railOff = off.Mean((int)px[0].x, (int)px[0].y, 1);
            Color roadOn = on.Mean((int)px[1].x, (int)px[1].y, 1), roadOff = off.Mean((int)px[1].x, (int)px[1].y, 1);
            TestContext.WriteLine($"rail {railOn} over ground {railOff}; road {roadOn} over ground {roadOff}");
            Assert.Less(RenderSample.Luma(railOn), RenderSample.Luma(railOff) - 0.05f, "the railway is darker than the ground");
            Assert.Greater(RenderSample.Luma(roadOn), RenderSample.Luma(roadOff) + 0.03f, "the road is paler than the ground");
        }
        finally { VisualToggles.SetAll(true); Object.DestroyImmediate(map.gameObject); }
    }
}
