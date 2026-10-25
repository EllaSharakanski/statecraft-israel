#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

/// <summary>#1661 items 4, 5 and 6: the real <c>MapPointer.Update</c> path, driven through its test seam
/// (<c>MapPointer.Tick</c>, as <c>CameraController.Tick</c> is, #890). The existing fixtures reach
/// <c>ProcessPresses</c> and the handlers underneath <c>Update</c>, so nothing drove the frame itself (item 6).
/// With that seam, the two timing questions become answerable: a touch gesture that begins and ends inside one
/// frame (item 4), and the undefined order of the two <c>Update</c> calls (item 5).</summary>
public class MapPointerTickTests : InputTestFixture
{
    static void Send(Object o, string message) =>
        o.GetType().GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(o, null);

    sealed class Rig : System.IDisposable
    {
        public readonly GameObject Go;
        public readonly CameraRig View;
        public readonly CameraController Controller;
        public readonly MapPointer Pointer;
        /// <summary>Every position the pointer asked the UI about: one per click or tap it decided to act on.</summary>
        public readonly List<Vector2> Asked = new List<Vector2>();
        public Rig()
        {
            Go = new GameObject("MapPointerTick", typeof(UnityEngine.Camera));
            View = Go.AddComponent<CameraRig>();
            View.LookAt = new Vector3(500000, 0, 500000); View.Distance = 100000f;
            Controller = Go.AddComponent<CameraController>();
            Controller.Bounds = new Rect(0, 0, 1000000, 1000000);
            Pointer = Go.AddComponent<MapPointer>();
            var asked = Asked;
            Pointer.IsOverUi = p => { asked.Add(p); return true; };   // true: the decision is recorded, no map needed
            var pointer = Pointer;
            Controller.HoldsMouseDrag = () => pointer.RightTapCandidate;   // as GameRoot wires them
            Controller.HoldsTouchDrag = () => pointer.TouchTapCandidate;
            Send(Controller, "Awake"); Send(Controller, "OnEnable"); Send(Pointer, "OnEnable");
        }
        /// <summary>One frame in the order Unity happens to give today when the pointer runs first.</summary>
        public void FramePointerFirst() { Pointer.Tick(); Controller.Tick(0.016f); }
        /// <summary>A tap is decided at the end of the pointer's frame and only <em>enqueued</em> (OnPress), while
        /// ProcessPresses already ran at the top of that same frame. So a tap takes effect on the frame after the finger
        /// lifts, and a test that asserts on the release frame alone asserts too early.</summary>
        public void SettleFrame() { Pointer.Tick(); Controller.Tick(0.016f); }
        /// <summary>The same frame with the other order, which Unity is equally free to choose (item 5).</summary>
        public void FrameCameraFirst() { Controller.Tick(0.016f); Pointer.Tick(); }
        public void Dispose() { Send(Pointer, "OnDisable"); Send(Controller, "OnDisable"); Object.DestroyImmediate(Go); }
    }

    Mouse mouse = null!;
    Touchscreen touch = null!;

    [SetUp]
    public void Devices()
    {
        InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        mouse = InputSystem.AddDevice<Mouse>();
        touch = InputSystem.AddDevice<Touchscreen>();
    }

    /// <summary>Item 6: the real Update path, not the handlers under it. A queued left press is handled by the frame
    /// itself, and only once.</summary>
    [Test]
    public void TheFrameHandlesAQueuedPressOnceThroughTheRealPath()
    {
        using var rig = new Rig();
        rig.Pointer.OnPress(new Vector2(800, 450));
        rig.FramePointerFirst();
        CollectionAssert.AreEqual(new[] { new Vector2(800, 450) }, rig.Asked, "the frame handled the press at its own position");
        rig.FramePointerFirst();
        Assert.AreEqual(1, rig.Asked.Count, "a press is handled once, not again on the next frame");
    }

    /// <summary>Item 6: a tap that spans two frames still selects where the finger landed, driven through the frame.</summary>
    [Test]
    public void ATapOverTwoFramesSelectsWhereTheFingerLanded()
    {
        using var rig = new Rig();
        var at = new Vector2(600, 300);
        BeginTouch(1, at);
        rig.FramePointerFirst();
        Assert.IsEmpty(rig.Asked, "nothing while the finger is down");
        EndTouch(1, at + new Vector2(2, -1));
        rig.FramePointerFirst();
        Assert.IsEmpty(rig.Asked, "the tap is decided on the release frame but only queued");
        rig.SettleFrame();
        CollectionAssert.AreEqual(new[] { at }, rig.Asked, "a 2 px wiggle is a tap, at where the finger landed");
    }

    /// <summary>Item 4, failing first: a flick that begins and ends inside one frame. The finger travelled far past the
    /// slop, so it was a drag and must not select. Today MapPointer samples one position per frame and sets the tap's
    /// start from it, so the travel is invisible and the gesture comes back as a tap.</summary>
    [Test]
    public void AFlickInsideOneFrameIsNotATap()
    {
        using var rig = new Rig();
        var from = new Vector2(400, 400);
        BeginTouch(1, from);
        MoveTouch(1, from + new Vector2(200, 0));
        EndTouch(1, from + new Vector2(200, 0));
        rig.FramePointerFirst();
        rig.SettleFrame();   // a queued tap would be handled here; without this the assert passes for the wrong reason
        Assert.IsEmpty(rig.Asked, "a 200 px flick inside one frame is a drag, not a tap");
    }

    /// <summary>Item 5, and it fails today: a one-finger drag past the slop pans when the pointer's Update runs first
    /// and does not pan at all when the camera's does. Neither component declares an execution order, so Unity is free
    /// to choose either.
    /// <para>Why the pan is lost, not merely late. On the frame the finger crosses the slop the camera asks
    /// <c>HoldsTouchDrag</c> before <c>MapPointer.Tick</c> has set <c>moved</c>, so it still believes the press may be a
    /// tap and keeps the delta (<c>HoldDrag</c>, <c>heldDrag += drag</c>). On the next frame the finger is up, so
    /// <c>down</c> is false and <c>HoldDrag</c>'s first branch drops the kept delta, as its contract says it should when
    /// "the press ends while still held (it was a click)". The gesture was not a click; the pointer had simply not
    /// recorded the movement yet when the camera asked.</para>
    /// <para>Ignored rather than deleted because the fix is a design choice, not a correction: either give the two
    /// components a <c>DefaultExecutionOrder</c> (this project uses none today, so it would be the first), or let
    /// <c>HoldDrag</c> replay a kept delta whose press had already left the slop, which cuts against its documented
    /// contract and the #1654 ruling that a press is a click or a pan and never both. #1661 item 5.</para></summary>
    [Test]
    [Ignore("#1661 item 5: fails today; the fix is an owner/maintainer choice between a DefaultExecutionOrder and changing HoldDrag's contract")]
    public void TheHeldPanDoesNotDependOnWhichUpdateRunsFirst()
    {
        var reached = new List<Vector3>();
        foreach (var cameraFirst in new[] { false, true })
        {
            using var rig = new Rig();
            var from = new Vector2(700, 400);
            BeginTouch(1, from);
            if (cameraFirst) rig.FrameCameraFirst(); else rig.FramePointerFirst();
            // past the slop in one step: from this frame on the gesture is a pan, never a tap
            MoveTouch(1, from + new Vector2(TapPress.SlopPixels + 40, 0));
            if (cameraFirst) rig.FrameCameraFirst(); else rig.FramePointerFirst();
            EndTouch(1, from + new Vector2(TapPress.SlopPixels + 40, 0));
            if (cameraFirst) rig.FrameCameraFirst(); else rig.FramePointerFirst();
            rig.SettleFrame();
            reached.Add(rig.View.LookAt);
            Assert.IsEmpty(rig.Asked, $"a drag past the slop never taps (cameraFirst={cameraFirst})");
        }
        Assert.AreEqual(reached[0], reached[1], "the pan reaches the same place whichever Update runs first");
    }
}
