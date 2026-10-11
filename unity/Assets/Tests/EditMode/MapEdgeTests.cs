using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Issue #174 (integration bug B11): panning past the terrain's edge filled the view with flat fills and a hard
/// seam. Two causes: the look-at clamp let the view run off the map onto the border skirt (a flat slab), and at the high
/// north-east corner (the ground about 6 km up at 3x relief) the hero-zoom camera, 4.6 km above the look-at plane y = 0,
/// sat inside the hills and showed only haze. Rendered at the worst pan pose: the hero zoom pushed into each corner of the
/// map, through the same CameraController.Step the game runs every frame, with the bounds and ground GameRoot gives it.</summary>
public class MapEdgeTests
{
    const int W = 1600, H = 900, Tile = 100;

    [SetUp] public void FreshScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    [TearDown] public void DiscardScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    /// <summary>The camera bounds as GameRoot sets them: the terrain's footprint (and CameraController.GroundOf its ground).</summary>
    static Rect Bounds(MapScene map)
    {
        var t = map.Terrain; var size = t.terrainData.size;
        return new Rect(t.transform.position.x, t.transform.position.z, size.x, size.z);
    }

    /// <summary>Where the renders are written (the before/after images for the issue): the project's Logs folder (git-ignored;
    /// Unity empties Temp when it quits).</summary>
    static string ShotDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "map-edge");

    [UnityTest]
    public IEnumerator PanningIntoACornerShowsNoSkirtSeamOrFlatFill()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var skirt = map.Terrain.transform.Find(MapScene.BorderSkirtName).GetComponent<MeshRenderer>();
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain);
        var rt = new RenderTexture(W, H, 24);
        Directory.CreateDirectory(ShotDir);
        RenderSample.TiltShift(false);   // the blur bands would read as flat tiles on the terrain too
        try
        {
            cam.targetTexture = rt;      // the clamp measures the view at the render's aspect
            yield return null;
            foreach (var (name, corner, push) in new[]
            {
                ("ne", new Vector2(bounds.xMax, bounds.yMax), new Vector2(1, 1)), ("nw", new Vector2(bounds.xMin, bounds.yMax), new Vector2(-1, 1)),
                ("se", new Vector2(bounds.xMax, bounds.yMin), new Vector2(1, -1)), ("sw", new Vector2(bounds.xMin, bounds.yMin), new Vector2(-1, -1)),
            })
            {
                rig.LookAt = new Vector3(corner.x, 0, corner.y); rig.Distance = CameraController.MinDistance; rig.Yaw = 0; rig.TopInset = 0;
                for (int i = 0; i < 20; i++) CameraController.Step(rig, new CameraInput { Pan = push }, CameraController.MaxFrameSeconds, bounds, ground);
                var with = RenderSample.Render(cam, W, H);
                skirt.enabled = false;
                var without = RenderSample.Render(cam, W, H);
                skirt.enabled = true;
                File.WriteAllBytes(Path.Combine(ShotDir, $"edge-{name}.png"), Png(with));

                // 1. The skirt draws no pixel of the view: hiding it changes nothing (any skirt pixel would show the clear colour).
                int skirtPixels = 0;
                for (int i = 0; i < with.Pixels.Length; i++)
                {
                    Color a = with.Pixels[i], b = without.Pixels[i];
                    if (Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)) > 2.5f / 255f) skirtPixels++;
                }
                // 2. No flat fill: every 100 px tile carries the terrain's texture (relief, land use, the grid), not one flat tone.
                float flattest = float.MaxValue; int fx = 0, fy = 0;
                for (int ty = 0; ty < H / Tile; ty++)
                    for (int tx = 0; tx < W / Tile; tx++)
                    {
                        float sd = LumaStdDev(with, tx * Tile, ty * Tile);
                        if (sd < flattest) { flattest = sd; fx = tx; fy = ty; }
                    }
                TestContext.WriteLine($"{name}: look-at ({rig.LookAt.x:0}, {rig.LookAt.y:0}, {rig.LookAt.z:0}) of ({bounds.xMax:0}, {bounds.yMax:0}); camera {rig.transform.position.y:0} over ground {ground(new Vector2(rig.transform.position.x, rig.transform.position.z)):0}; skirt pixels " +
                    $"{skirtPixels} ({100f * skirtPixels / with.Pixels.Length:0.00}%); flattest tile ({fx}, {fy}) luma sd {flattest:0.0000}");
                Assert.AreEqual(0, skirtPixels, $"{name} corner: the border skirt shows in the view");
                Assert.Greater(flattest, FlatTileStdDev, $"{name} corner: tile ({fx}, {fy}) is a flat fill");
            }
        }
        finally
        {
            RenderSample.TiltShift(true);
            skirt.enabled = true;
            cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt);
        }
    }

    /// <summary>A tile's luma standard deviation below this is a flat fill. Measured on the corner renders: the skirt's
    /// flat slab tiles sit well under it, the terrain's flattest tile (sea included, which carries the grid and the waves)
    /// well over it.</summary>
    const float FlatTileStdDev = 0.004f;

    static float LumaStdDev(RenderSample s, int x0, int y0)
    {
        double sum = 0, sq = 0; int n = 0;
        for (int y = y0; y < y0 + Tile; y++)
            for (int x = x0; x < x0 + Tile; x++) { double l = RenderSample.Luma(s.At(x, y)); sum += l; sq += l * l; n++; }
        double mean = sum / n;
        return (float)System.Math.Sqrt(System.Math.Max(0, sq / n - mean * mean));
    }

    static byte[] Png(RenderSample s)
    {
        var tex = new Texture2D(s.Width, s.Height, TextureFormat.RGB24, false);
        try { tex.SetPixels(s.Pixels); tex.Apply(); return tex.EncodeToPNG(); }
        finally { Object.DestroyImmediate(tex); }
    }

    /// <summary>The promo and close-check poses already frame only the map where the view fits it, so the clamp leaves
    /// them where they are (the shots in the WebGL player run through Step every frame); diorama02, wider than the map,
    /// is centred on it east to west, a few hundred metres from its pose.</summary>
    [Test]
    public void ThePromoPosesStayPut()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        try
        {
            foreach (var pose in new System.Collections.Generic.List<PromoPose>(PromoPose.All) { PromoPose.CloseChecks[0], PromoPose.CloseChecks[1] })
            {
                pose.Apply(rig, map.Frame); rig.TopInset = 0;
                var before = rig.LookAt;
                CameraController.Step(rig, new CameraInput(), 0f, Bounds(map), CameraController.GroundOf(map.Terrain));
                float moved = (rig.LookAt - before).magnitude;
                TestContext.WriteLine($"{pose.Name}: moved {moved:0} m");
                Assert.Less(moved, pose.Distance > 100000f ? 1000f : 1f, pose.Name);
            }
        }
        finally { cam.ResetAspect(); }
    }

    /// <summary>Issue #174: at the hero zoom, anywhere on the map and at any yaw, the camera keeps MinClearanceShare of its
    /// height above the ground beneath it, so it never sits inside the hills (before, about 6% of the map put it underground).</summary>
    [Test]
    public void TheCameraNeverSinksIntoTheGround()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        cam.aspect = 16f / 9f;
        var bounds = Bounds(map); var ground = CameraController.GroundOf(map.Terrain);
        try
        {
            int lifted = 0, n = 0; float least = float.MaxValue;
            for (int i = 0; i <= 40; i++)
                for (int j = 0; j <= 40; j++)
                    foreach (var yaw in new[] { 0f, 120f, 240f })
                    {
                        rig.LookAt = new Vector3(bounds.xMin + bounds.width * i / 40f, 0, bounds.yMin + bounds.height * j / 40f);
                        rig.Distance = CameraController.MinDistance; rig.Yaw = yaw; rig.TopInset = 0;
                        CameraController.Step(rig, new CameraInput(), 0f, bounds, ground);
                        var c = rig.transform.position;
                        float height = rig.Distance * Mathf.Sin(CameraRig.Pitch * Mathf.Deg2Rad);
                        float share = (c.y - ground(new Vector2(c.x, c.z))) / height;
                        least = Mathf.Min(least, share); n++;
                        if (rig.LookAt.y > 0f) lifted++;
                    }
            TestContext.WriteLine($"{n} poses, {lifted} lifted, least clearance {least:0.000} of the camera's height");
            Assert.GreaterOrEqual(least, CameraController.MinClearanceShare - 1e-3f);
            Assert.Greater(lifted, 0, "the rule took effect somewhere (the Golan, the Galilee, the Jordanian plateau)");
        }
        finally { cam.ResetAspect(); }
    }
}
