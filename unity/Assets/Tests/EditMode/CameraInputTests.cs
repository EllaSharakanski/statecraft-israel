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
            CameraController.Step(rig, new CameraInput { Zoom = 1, DragPan = Vector2.one * 50, OverUi = true }, 1f, Map);
            Assert.AreEqual(before, (rig.LookAt, rig.Distance, rig.Yaw));
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
}
