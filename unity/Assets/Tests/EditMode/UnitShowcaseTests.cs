using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.Map.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class UnitShowcaseTests
{
    static Mesh[] AllMeshes() => new[]
    {
        UnitModels.Rifleman(UnitModels.Headgear.SlouchHat, UnitModels.RiflePose.Ready, false, "a"),
        UnitModels.Rifleman(UnitModels.Headgear.SteelHelmet, UnitModels.RiflePose.Grounded, true, "b"),
        UnitModels.Rifleman(UnitModels.Headgear.WoolCap, UnitModels.RiflePose.Slung, false, "c"),
        UnitModels.Spitfire("s"), UnitModels.SpitfirePropeller("p"), UnitModels.Corvette("k"),
    };

    [Test]
    public void ShowcaseIsAskedForOnlyByUnitsOrShot()
    {
        Assert.IsTrue(UnitShowcase.Requested("https://x/index.html?units", new string[0], true));
        Assert.IsTrue(UnitShowcase.Requested("https://x/index.html?golden&shot", new string[0], true));
        Assert.IsFalse(UnitShowcase.Requested("https://x/index.html?golden&smoke=5", new string[0], true));
        Assert.IsFalse(UnitShowcase.Requested("https://x/index.html?vprofile", new string[0], true), "profiles price it only with &units");
        Assert.IsFalse(UnitShowcase.Requested("https://x/units/index.html?unitsx", new string[0], true));
        Assert.IsTrue(UnitShowcase.Requested(null, new[] { "player", "-units" }, false));
        Assert.IsFalse(UnitShowcase.Requested(null, new[] { "player" }, false));
        Assert.IsTrue(UnitShowcase.StillRequested("https://x/?shot", new string[0], true));
        Assert.IsFalse(UnitShowcase.StillRequested("https://x/?units", new string[0], true));
    }

    [Test]
    public void ModelsAreRepeatableAndWithinBudget()
    {
        var a = AllMeshes(); var b = AllMeshes();
        int total = 0;
        for (int i = 0; i < a.Length; i++)
        {
            CollectionAssert.AreEqual(a[i].vertices, b[i].vertices, a[i].name);
            CollectionAssert.AreEqual(a[i].triangles, b[i].triangles, a[i].name);
            CollectionAssert.AreEqual(a[i].uv, b[i].uv, a[i].name);
            total += a[i].triangles.Length / 3;
        }
        // Budget: the whole slice under 25k triangles (a WebGL integrated GPU draws millions; this keeps a full front of
        // trios cheap when #104 instances them).
        Assert.Less(total, 25000, $"slice triangles {total}");
        foreach (var m in a.Concat(b)) Object.DestroyImmediate(m);
    }

    [Test]
    public void EveryTriangleFacesAlongItsNormals()
    {
        foreach (var mesh in AllMeshes())
        {
            var v = mesh.vertices; var n = mesh.normals; var t = mesh.triangles;
            int bad = 0, counted = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                var face = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (face.sqrMagnitude < 1e-12f) continue;
                counted++;
                if (Vector3.Dot(face, n[t[i]] + n[t[i + 1]] + n[t[i + 2]]) < 0f) bad++;
            }
            Assert.AreEqual(0, bad, $"{mesh.name}: {bad} of {counted} triangles face against their normals");
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void RiflemanStandsOnTheOriginAtAMansHeight()
    {
        foreach (UnitModels.Headgear hat in System.Enum.GetValues(typeof(UnitModels.Headgear)))
        {
            var m = UnitModels.Rifleman(hat, UnitModels.RiflePose.Grounded, false, "r");
            Assert.That(m.bounds.min.y, Is.InRange(-0.01f, 0.02f), $"{hat}: feet on the ground");
            Assert.That(m.bounds.max.y, Is.InRange(1.70f, 1.95f), $"{hat}: about a man's height");
            Object.DestroyImmediate(m);
        }
    }

    [Test]
    public void PaletteSlotsSampleTheirOwnColour()
    {
        var tex = UnitPalette.MakeTexture();
        for (int slot = 0; slot < UnitPalette.Count; slot++)
        {
            var c = tex.GetPixelBilinear(UnitPalette.U(slot), 0.5f);
            var want = UnitPalette.Shade(slot, 0.5f);
            Assert.That(Mathf.Abs(c.r - want.r) + Mathf.Abs(c.g - want.g) + Mathf.Abs(c.b - want.b), Is.LessThan(0.03f), $"slot {slot}");
        }
        Object.DestroyImmediate(tex);
    }

    [Test]
    public void FlightHoldsItsStillPhaseAndKeepsItsAltitude()
    {
        var go = new GameObject("f"); var prop = new GameObject("p");
        try
        {
            var f = go.AddComponent<ShowcaseFlight>();
            f.Configure(new Vector3(1000, 0, 2000), 500f, 300f, 10f, 20f, prop.transform, 0.25f);
            var first = go.transform.position;
            f.Configure(new Vector3(1000, 0, 2000), 500f, 300f, 10f, 20f, prop.transform, 0.25f);
            Assert.AreEqual(first, go.transform.position);
            Assert.AreEqual(300f, first.y, 1e-3f);
            Assert.AreEqual(500f, Vector2.Distance(new Vector2(first.x, first.z), new Vector2(1000, 2000)), 0.01f);
            // Nose along the path (tangent to the circle).
            var radial = (first - new Vector3(1000, first.y, 2000)).normalized;
            Assert.AreEqual(0f, Vector3.Dot(Vector3.ProjectOnPlane(go.transform.forward, Vector3.up).normalized, radial), 1e-3f);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(prop); }
    }

    [Test]
    public void SpawnBuildsTheSliceAtGameplay03AndTheToggleHidesIt()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var map = new GameObject("MapScene").AddComponent<MapScene>();
        map.Configure(Load("Terrain"), Load("SettlementMarker"), Load("HexSelection"), Load("Sea"), Load("BorderLand"));
        map.Build();
        try
        {
            var root = UnitShowcase.Spawn(map, still: true);
            Assert.AreSame(map.Terrain.transform, root.transform.parent, "a map rebuild destroys it");
            Assert.AreEqual(3, root.GetComponentsInChildren<MeshFilter>().Count(f => f.name.StartsWith("Rifleman")));
            Assert.IsNotNull(root.transform.Find("SelectionRing"));
            Assert.IsNotNull(root.transform.Find("Corvette1948"));
            var plane = root.GetComponentInChildren<ShowcaseFlight>();
            Assert.AreEqual(UnitShowcase.StillPhase, plane.StillPhase);
            // Each figure stands on the ground under it.
            foreach (var f in root.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("Rifleman")))
                Assert.AreEqual(UnitShowcase.Ground(map, f.transform.position).y, f.transform.position.y, 1f, f.name);
            // The aircraft clears the highest ground under its loop.
            var loop = map.Frame.ToWorld(UnitShowcase.LoopCentre.lon, UnitShowcase.LoopCentre.lat, 0);
            Assert.Greater(plane.transform.position.y, UnitShowcase.LoopFloor(map, loop, UnitShowcase.LoopRadiusM) + UnitShowcase.AltitudeM * 0.99f);
            // Everything stands inside the gameplay03 view.
            var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
            PromoPose.Gameplay03.Apply(rig, map.Frame);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var vp = cam.WorldToViewportPoint(r.bounds.center);
                Assert.That(vp.x, Is.InRange(0.02f, 0.98f), r.name); Assert.That(vp.y, Is.InRange(0.02f, 0.98f), r.name); Assert.Greater(vp.z, 0f, r.name);
            }
            CollectionAssert.Contains(VisualToggles.Names, UnitShowcase.ToggleName);
            VisualToggles.Set(UnitShowcase.ToggleName, false);
            Assert.IsFalse(root.activeSelf);
            VisualToggles.Set(UnitShowcase.ToggleName, true);
            Assert.IsTrue(root.activeSelf);
            // A second spawn replaces the first rather than stacking.
            UnitShowcase.Spawn(map, still: true);
            int count = 0; foreach (Transform c in map.Terrain.transform) if (c.name == UnitShowcase.RootName) count++;
            Assert.AreEqual(1, count);
        }
        finally { Object.DestroyImmediate(map.gameObject); }
        CollectionAssert.DoesNotContain(VisualToggles.Names, UnitShowcase.ToggleName, "the map's teardown removes the switch");
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
}
