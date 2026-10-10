using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInputTests
{
    static CameraRig NewRig()
    {
        var go = new GameObject("RigTest", typeof(Camera));
        var rig = go.AddComponent<CameraRig>();
        rig.LookAt = new Vector3(50000, 0, 50000); rig.Distance = 100000f;
        return rig;
    }

    static readonly Rect Map = new Rect(0, 0, 160000, 430000);

    [Test]
    public void TheCameraActionMapIsInTheProjectWideAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var map = asset.FindActionMap("Camera", true);
        foreach (var name in new[] { "Pan", "DragPan", "Zoom", "Orbit" }) Assert.IsNotNull(map.FindAction(name), name);
        Assert.AreEqual(8, map.FindAction("Pan").bindings.Count(b => b.isPartOfComposite && b.path.StartsWith("<Keyboard>/")));
        Assert.AreEqual(3, map.FindAction("DragPan").bindings.Count);
        Assert.AreEqual(1, map.FindAction("Zoom").bindings.Count);
        Assert.AreEqual(3, map.FindAction("Orbit").bindings.Count);
        foreach (var a in map.actions)
            foreach (var b in a.bindings.Where(b => !b.isComposite))
                Assert.IsNotNull(UnityEngine.InputSystem.InputControlPath.TryGetDeviceLayout(b.path), a.name + " " + b.path);
        Assert.IsNotNull(asset.FindActionMap("UI", true).FindAction("Click"), "UI Toolkit needs the UI map's actions");
    }

    [Test]
    public void PanMovesTheLookAtNorthAtYawZero()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up }, 0.5f, Map);
            Assert.AreEqual(50000f, rig.LookAt.x, 1e-2);
            Assert.AreEqual(50000f + 0.4f * 100000f, rig.LookAt.z, 1e-1);   // 0.8 distances per second for half a second
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void ZoomIsClampedAndOrbitTurnsTheView()
    {
        var rig = NewRig();
        try
        {
            for (int i = 0; i < 100; i++) CameraController.Step(rig, new CameraInput { Zoom = 3 }, 0.016f, Map);
            Assert.AreEqual(CameraController.MinDistance, rig.Distance, 1e-3);
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.5f, Map);
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.AreEqual(30f, rig.transform.eulerAngles.y, 1e-3);
            CameraController.Step(rig, new CameraInput { Pan = Vector2.left }, 100f, Map);
            Assert.AreEqual(0f, rig.LookAt.x, 1e-3);                       // clamped to the map edge
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void PointerInputIsIgnoredOverTheUi()
    {
        var rig = NewRig();
        try
        {
            var before = (rig.LookAt, rig.Distance, rig.Yaw);
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50, OverUi = true, DragFromUi = true }, 1f, Map);
            Assert.AreEqual(before, (rig.LookAt, rig.Distance, rig.Yaw));
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50 }, 1f, Map);   // control: same input off the UI moves the rig
            Assert.AreNotEqual(before.Distance, rig.Distance);
            Assert.AreNotEqual(before.LookAt, rig.LookAt);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void KeysStillPanAndOrbitWhileThePointerIsOverTheUi()
    {
        var rig = NewRig();
        try
        {
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up, Orbit = 1, Zoom = 1, OverUi = true }, 0.5f, Map);
            Assert.AreEqual(100000f, rig.Distance, 1e-3);                 // the wheel was over the UI
            Assert.AreEqual(30f, rig.Yaw, 1e-3);
            Assert.Greater(rig.LookAt.z, 50000f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void MaxDistanceClampsAndYawWraps()
    {
        var rig = NewRig();
        try
        {
            for (int i = 0; i < 200; i++) CameraController.Step(rig, new CameraInput { Zoom = -3 }, 0.016f, Map);
            Assert.AreEqual(CameraController.MaxDistance, rig.Distance, 1e-3);
            rig.Yaw = 350f;
            CameraController.Step(rig, new CameraInput { Orbit = 1 }, 0.5f, Map);   // +30
            Assert.AreEqual(20f, rig.Yaw, 1e-3);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void PanFollowsTheYaw()
    {
        var rig = NewRig();
        try
        {
            rig.Yaw = 90f;   // looking east: forward is +x
            CameraController.Step(rig, new CameraInput { Pan = Vector2.up }, 0.5f, Map);
            Assert.AreEqual(50000f + 0.4f * 100000f, rig.LookAt.x, 1f);
            Assert.AreEqual(50000f, rig.LookAt.z, 1f);
        }
        finally { Object.DestroyImmediate(rig.gameObject); }
    }

    [Test]
    public void TwoHalfStepsEqualOneFullStep()
    {
        var a = NewRig(); var b = NewRig();
        try
        {
            var inp = new CameraInput { Pan = new Vector2(1, 1), Orbit = 1, Zoom = 0 };
            a.Yaw = b.Yaw = 0;
            CameraController.Step(a, inp, 0.1f, Map);
            CameraController.Step(b, inp, 0.05f, Map); CameraController.Step(b, inp, 0.05f, Map);
            Assert.AreEqual(a.Yaw, b.Yaw, 1e-3);
            // the pan direction turns with the yaw between the half steps (about 1.5 degrees here), so allow 0.5% of the distance
            Assert.AreEqual(a.LookAt.x, b.LookAt.x, 500f);
            Assert.AreEqual(a.LookAt.z, b.LookAt.z, 500f);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }

    [Test]
    public void AStalledFrameMovesNoMoreThanTheCap()
    {
        var a = NewRig(); var b = NewRig();
        try
        {
            var inp = new CameraInput { Pan = Vector2.up, Orbit = 1 };
            CameraController.Step(a, inp, CameraController.ClampDt(5f), Map);
            CameraController.Step(b, inp, CameraController.MaxFrameSeconds, Map);
            Assert.AreEqual(b.LookAt, a.LookAt);
            Assert.AreEqual(b.Yaw, a.Yaw, 1e-4);
        }
        finally { Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); }
    }

    static CameraController NewController(out GameObject go)
    {
        go = new GameObject("CtlTest", typeof(Camera));
        var c = go.AddComponent<CameraController>();
        c.IsOverUi = p => p.x < 100;   // the panel is the left strip
        return c;
    }

    [Test]
    public void NoPointerMeansNotOverTheUi()
    {
        var c = NewController(out var go);
        try
        {
            bool probed = false; c.IsOverUi = _ => { probed = true; return true; };
            var inp = c.Gather(Vector2.zero, Vector2.zero, 120f, 0, false, null);
            Assert.IsFalse(inp.OverUi); Assert.IsFalse(probed);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ADragStartedOverTheMapKeepsPanningOverAPanel()
    {
        var c = NewController(out var go);
        try
        {
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(500, 0));
            var inp = c.Gather(Vector2.zero, Vector2.one, 120f, 0, true, new Vector2(50, 0));   // now over the panel
            Assert.IsFalse(inp.DragFromUi); Assert.IsTrue(inp.OverUi);                          // wheel is still the UI's
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ADragStartedOverTheUiNeverPansTheMap()
    {
        var c = NewController(out var go);
        try
        {
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(50, 0));
            var inp = c.Gather(Vector2.zero, Vector2.one, 0, 0, true, new Vector2(500, 0));     // moved onto the map
            Assert.IsTrue(inp.DragFromUi);
            c.Gather(Vector2.zero, Vector2.zero, 0, 0, false, new Vector2(500, 0));             // released
            Assert.IsFalse(c.Gather(Vector2.zero, Vector2.zero, 0, 0, true, new Vector2(500, 0)).DragFromUi);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
