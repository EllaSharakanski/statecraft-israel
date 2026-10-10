#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Kits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The art spike's procedural settlement kits and trees (issues #31 #32 #34 #37): seeded, sized by population, on
/// land, drawn for WebGL 2, and with shadows that start at the walls.</summary>
public class KitTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShadowFit.Restore();
        VisualToggles.Clear();
        KitScale.Buildings = KitScale.DefaultBuildings; KitScale.Trees = KitScale.DefaultTrees;
    }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    static KitGround Ground(MapScene map) =>
        new KitGround(map.Terrain, map.Frame, map.Hexes, HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta.elev_min_m);

    [Test]
    public void TheSeedIsTheKeyNotTheClock()
    {
        var a = new KitRandom("tel_aviv", 1); var b = new KitRandom("tel_aviv", 1); var c = new KitRandom("jaffa", 1);
        var sa = Enumerable.Range(0, 64).Select(_ => a.Next()).ToArray();
        CollectionAssert.AreEqual(sa, Enumerable.Range(0, 64).Select(_ => b.Next()).ToArray());
        CollectionAssert.AreNotEqual(sa, Enumerable.Range(0, 64).Select(_ => c.Next()).ToArray());
        var r = new KitRandom("x");
        for (int i = 0; i < 1000; i++) { var v = r.Value(); Assert.That(v, Is.InRange(0f, 0.99999994f)); }
    }

    [Test]
    public void EveryBuildDrawsTheSameTownsAndTrees()
    {
        var map = OpenMap();
        var list = map.Settlements;
        var first = SettlementKits.Build(list, Ground(map)); var second = SettlementKits.Build(list, Ground(map));
        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].s.Id, second[i].s.Id);
            Assert.AreEqual(first[i].buildings, second[i].buildings, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.vertexCount, second[i].mesh.vertexCount, first[i].s.Id);
            Assert.AreEqual(first[i].mesh.bounds, second[i].mesh.bounds, first[i].s.Id);
        }
        foreach (var k in first.Concat(second)) Object.DestroyImmediate(k.mesh);
        var t1 = Vegetation.Plant(list, Ground(map)); var t2 = Vegetation.Plant(list, Ground(map));
        Assert.AreEqual(t1.Count, t2.Count);
        for (int i = 0; i < t1.Count; i++) { Assert.AreEqual(t1[i].kind, t2[i].kind); Assert.AreEqual(t1[i].m, t2[i].m); }
        TestContext.WriteLine(map.KitStats);
    }

    [Test]
    public void TownsSpreadInProportionToTheirPeople()
    {
        var map = OpenMap();
        float hex = map.Hexes.hex_size_km * 1000f;
        var byId = map.Settlements.ToDictionary(s => s.Id);
        float R(string id) => SettlementKits.FootprintRadiusM(byId[id], hex);
        float hexArea = 1.5f * Mathf.Sqrt(3f) * hex * hex;
        // Area in hexes = people / PeoplePerHex (Tel Aviv 166,660 in 1945: about 2.8 hexes), floored for the smallest places.
        Assert.AreEqual(166660f / SettlementKits.PeoplePerHex, Mathf.PI * R("tel_aviv") * R("tel_aviv") / hexArea, 0.01f);
        Assert.AreEqual(94310f / 166660f, (R("jaffa") * R("jaffa")) / (R("tel_aviv") * R("tel_aviv")), 0.001f, "area ratio = population ratio");
        Assert.Greater(R("jaffa"), R("lod")); Assert.Greater(R("lod"), R("rishon_lezion"));
        Assert.AreEqual(R("hanita"), R("revivim"), 1e-3f, "the floor");
        // The kits root: one merged mesh per settlement with Kit.mat, and more buildings in bigger towns.
        var root = map.transform.Find(KitLayer.RootName); Assert.IsNotNull(root);
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        foreach (var s in map.Settlements)
        {
            var t = root.Find(s.Id); Assert.IsNotNull(t, s.Id);
            var r = t.GetComponent<MeshRenderer>(); Assert.AreSame(kit, r.sharedMaterial, s.Id);
            Assert.Greater(t.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0, s.Id);
            Assert.IsFalse(map.transform.Find("Settlements").Find(s.Id).GetComponent<MeshRenderer>().enabled, s.Id + ": the marker box is hidden");
        }
        int V(string id) => root.Find(id).GetComponent<MeshFilter>().sharedMesh.vertexCount;
        Assert.Greater(V("tel_aviv"), V("petah_tikva")); Assert.Greater(V("jaffa"), V("lod")); Assert.Greater(V("petah_tikva"), V("hanita"));
        TestContext.WriteLine(map.KitStats);
    }

    [Test]
    public void KitsAndTreesStandOnLand()
    {
        var map = OpenMap();
        var g = Ground(map);
        var root = map.transform.Find(KitLayer.RootName);
        foreach (var s in map.Settlements)
        {
            var b = root.Find(s.Id).GetComponent<MeshRenderer>().bounds;
            Assert.Less(b.size.x, 2.6f * SettlementKits.FootprintRadiusM(s, g.HexSizeM) + 2000f, s.Id + ": the town stays within its footprint");
        }
        var trees = Vegetation.Plant(map.Settlements, g);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Citrus) + 4 * trees.Count(t => t.kind == TreeKind.CitrusPatch), 1000); Assert.Greater(trees.Count(t => t.kind == TreeKind.Olive), 1000);
        Assert.Greater(trees.Count(t => t.kind == TreeKind.Palm), 50); Assert.Greater(trees.Count(t => t.kind == TreeKind.Cypress), 50);
        int wet = 0;
        foreach (var (_, m) in trees)
        {
            Vector3 p = m.GetColumn(3);
            if (g.Water(p.x, p.z)) wet++;
            Assert.AreEqual(g.Y(p.x, p.z), p.y, 0.5f, "rooted on the ground");
        }
        Assert.AreEqual(0, wet, "trees in the water");
    }

    /// <summary>The WebGL checkpoint drew every palm black: a frond's tip quad collapsed to a point, its edge-based normal was
    /// zero and normalize(0) is NaN on WebGL 2 (the tilt-shift then smeared it into black blocks). No part may have one.</summary>
    [Test]
    public void EveryPartHasAUnitNormal()
    {
        foreach (TreeKind k in System.Enum.GetValues(typeof(TreeKind))) Assert.AreEqual(0, Vegetation.Build(k).BadNormals, k.ToString());
        var km = new KitMesh(); var rnd = new KitRandom("parts");
        km.Box(0, 0, 17f, 500f, 300f, -50f, 0f, 400f, new Color32(255, 255, 255, 255), new Color32(200, 200, 200, 255), Facade.WhiteCity, 25f);
        km.Gable(0, 0, 17f, 500f, 300f, 400f, 120f, new Color32(200, 80, 50, 255), new Color32(255, 255, 255, 255));
        km.Pyramid(0, 0, 0f, 100f, 100f, 0f, 80f, new Color32(255, 255, 255, 255));
        km.Dome(0, 0, 0f, 100f, new Color32(255, 255, 255, 255));
        km.Shaft(Vector3.zero, Vector3.up * 300f, 30f, 20f, new Color32(255, 255, 255, 255), 8);
        km.Cone(Vector3.zero, 30f, 40f, new Color32(255, 255, 255, 255), 8);
        km.Crown(Vector3.zero, Vector3.one * 50f, new Color32(40, 80, 30, 255), ref rnd);
        km.Frond(Vector3.zero, Vector3.right, 100f, 25f, 0.7f, new Color32(40, 80, 30, 255));
        Assert.AreEqual(0, km.BadNormals);
    }

    [Test]
    public void TheKitShaderRunsOnWebGL2()
    {
        var shader = Shader.Find("Statecraft/Kit"); Assert.IsNotNull(shader);
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(shader));
        var tree = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tree.mat");
        Assert.AreSame(shader, tree.shader); Assert.IsTrue(tree.enableInstancing, "the trees are GPU-instanced");
    }

    [UnityTest]
    public IEnumerator TheTreesAreDrawnAtTheGameplayPoseAndThinnedAtTheOverview()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var veg = map.GetComponentInChildren<VegetationLayer>(); Assert.IsNotNull(veg);
        PromoPose.Gameplay03.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int near = veg.LastDrawn;
        PromoPose.Diorama02.Apply(rig, map.Frame); RenderSample.Render(cam, 320, 180);
        int far = veg.LastDrawn;
        TestContext.WriteLine($"trees {veg.Count}: drawn at gameplay03 {near}, at diorama02 {far}");
        Assert.Greater(near, 1000); Assert.Less(far, veg.Count * 0.6f, "the overview thins the far tiles");
        VisualToggles.Set("trees", false); RenderSample.Render(cam, 320, 180);
        Assert.AreEqual(far, veg.LastDrawn, "the trees toggle stops the draws (no new submission)");
    }

    /// <summary>Issue #37: at the gameplay and overview zooms a kit's shadow starts at its walls. A lone box the size of the
    /// old markers on the open coastal plain; walking out from its shadow-side wall along the sun's travel, the gap is the
    /// distance to the first ground pixel that is clearly in shadow (darker, shadows on against off, by 60% of the deep
    /// shadow's darkening). With Kit.mat's bias the gap stays under half a shadow texel; the old marker material (URP Lit,
    /// the sun's own bias) is measured alongside as the control.</summary>
    [UnityTest]
    public IEnumerator AKitShadowStartsAtItsWalls([Values(0, 1)] int level, [Values(60000f, 250000f)] float distance)
    {
        int previous = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(level, false);   // 0 = the web tier (1 cascade, bias 1 / 1), 1 = desktop
        try { yield return Measure(level, distance); }
        finally { QualitySettings.SetQualityLevel(previous, false); }
    }

    IEnumerator Measure(int level, float distance)
    {
        var map = OpenMap();
        yield return null;
        foreach (var n in VisualToggles.Names.Where(n => n != "shadows")) VisualToggles.Set(n, false);
        var g = Ground(map);
        var at = map.Frame.ToWorld(34.70, 31.70, 0);
        float w = distance > 100000f ? 1800f : 600f, h = w;
        var km = new KitMesh();
        float y = g.Y(at.x, at.z);
        km.Box(at.x, at.z, 0f, w, w, y - 30f, y, h, new Color32(235, 230, 220, 255), new Color32(200, 200, 190, 255), Facade.Plain, 25f);
        var mesh = km.ToMesh("ShadowProbe");
        var kit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kit.mat");
        var marker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SettlementMarker.mat");
        var go = new GameObject("ShadowProbe"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = kit;
        try
        {
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            rig.LookAt = at; rig.Distance = distance; rig.Yaw = 0f; rig.TopInset = 0f; rig.Apply();
            var sun = MapLighting.FindSun()!; var fall = sun.transform.forward; fall.y = 0f; fall.Normalize();
            float toWall = 0.5f * w / Mathf.Max(Mathf.Abs(fall.x), Mathf.Abs(fall.z));
            float texel = 2f * ShadowFit.Distance(cam, UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset!.cascadeBorder) / 2048f;
            const int steps = 40;
            var pts = new Vector3[steps + 1];
            for (int i = 0; i < steps; i++)
            {
                var p = at + fall * (toWall + 10f + 2f * texel * i / steps); p.y = g.Y(p.x, p.z); pts[i] = p;
            }
            var deep = at + fall * (toWall + h * 1.2f); deep.y = g.Y(deep.x, deep.z); pts[steps] = deep;
            var px = new Vector2[pts.Length];
            float Gap(Material m, out float deepDark)
            {
                mr.sharedMaterial = m;
                VisualToggles.Set("shadows", true); var on = RenderSample.Render(cam, 1920, 1080, pts, px);
                VisualToggles.Set("shadows", false); var off = RenderSample.Render(cam, 1920, 1080, pts, px);
                VisualToggles.Set("shadows", true);
                float Dark(int i) => 1f - RenderSample.Luma(on.At((int)px[i].x, (int)px[i].y)) / Mathf.Max(1e-4f, RenderSample.Luma(off.At((int)px[i].x, (int)px[i].y)));
                deepDark = Dark(steps);
                for (int i = 0; i < steps; i++) if (Dark(i) > 0.6f * deepDark) return 10f + 2f * texel * i / steps;
                return float.PositiveInfinity;
            }
            float kitGap = Gap(kit, out var kitDeep), markerGap = Gap(marker, out var markerDeep);
            float metresPerPixel = 2f * distance * Mathf.Tan(CameraRig.Fov * 0.5f * Mathf.Deg2Rad) / 1080f;
            TestContext.WriteLine($"tier {QualitySettings.names[level]}, {distance / 1000f:0} km, {w:0} m box, shadow texel {texel:0} m, pixel {metresPerPixel:0} m: lit gap between wall and shadow " +
                $"{kitGap:0} m with Kit.mat, {markerGap:0} m with the marker material; deep shadow darker by {kitDeep:P0} / {markerDeep:P0}");
            Assert.Greater(kitDeep, 0.15f, "the box casts a shadow at all");
            Assert.LessOrEqual(kitGap, 0.5f * texel + 2f * metresPerPixel, "the shadow reaches the wall with the kit's bias");
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); }
    }
}
