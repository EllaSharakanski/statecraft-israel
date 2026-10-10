#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>One frame of camera input, in the units the actions give: Pan in [-1, 1] per axis (keys), DragPan in
    /// screen pixels (right-button drag), Zoom in scroll units (positive = in), Orbit in [-1, 1] (Q/E).</summary>
    public struct CameraInput
    {
        public Vector2 Pan, DragPan; public float Zoom, Orbit;
        /// <summary>True while the pointer is over the UI: the camera then ignores pointer input (wheel zoom, drag).</summary>
        public bool OverUi;
        /// <summary>True when the current right-button drag started over the UI (latched on button-down): that drag never pans the map.</summary>
        public bool DragFromUi;
    }

    /// <summary>Pans, zooms and orbits the diorama camera from the project-wide "Camera" action map (Input System only).
    /// While the pointer is over the UI (IsOverUi, wired by the scene) the wheel and the drag belong to the UI; keyboard pan
    /// and orbit still move the camera (plan amendment A4). Pan speed scales with
    /// distance, zoom is clamped to [MinDistance, MaxDistance], and the look-at point stays on the map.</summary>
    [RequireComponent(typeof(CameraRig))]
    public sealed class CameraController : MonoBehaviour
    {
        public const float MinDistance = 20000f, MaxDistance = 250000f;
        public const float PanPerSecond = 0.8f;        // distances per second at full key press
        public const float DragPanPerPixel = 0.0012f;  // distances per pixel of drag
        public const float ZoomPerStep = 0.1f;         // fraction of the distance per scroll notch (scroll units are normalised to notches)
        public const float OrbitDegreesPerSecond = 60f;
        public const float MaxFrameSeconds = 0.1f;     // a stalled frame (tab in the background) must not fling the camera

        /// <summary>Answers "is the pointer over the UI?" for a screen position (y up). Null = never.</summary>
        public Func<Vector2, bool>? IsOverUi;
        /// <summary>The map rectangle on the ground (x, z), in world metres; the look-at point is clamped to it.</summary>
        public Rect Bounds = new Rect(0, 0, float.MaxValue, float.MaxValue);

        CameraRig rig = null!;
        InputAction? pan, dragPan, zoom, orbit;

        void Awake() => rig = GetComponent<CameraRig>();

        void OnEnable()
        {
            var map = InputSystem.actions?.FindActionMap("Camera", throwIfNotFound: true);
            if (map == null) return;
            pan = map.FindAction("Pan", true); dragPan = map.FindAction("DragPan", true);
            zoom = map.FindAction("Zoom", true); orbit = map.FindAction("Orbit", true);
            map.Enable();
        }

        void OnDisable() => InputSystem.actions?.FindActionMap("Camera")?.Disable();

        bool dragFromUi, dragWasDown;

        void Update()
        {
            if (pan == null || dragPan == null || zoom == null || orbit == null) return;
            Vector2? pointer = Pointer.current != null ? Pointer.current.position.ReadValue() : (Vector2?)null;
            var down = Mouse.current != null && Mouse.current.rightButton.isPressed;
            Step(rig, Gather(pan.ReadValue<Vector2>(), dragPan.ReadValue<Vector2>(), zoom.ReadValue<float>(),
                orbit.ReadValue<float>(), down, pointer), ClampDt(Time.unscaledDeltaTime), Bounds);
        }

        public static float ClampDt(float dt) => Mathf.Min(dt, MaxFrameSeconds);

        /// <summary>Builds one frame of input from raw action values. The wheel is checked against the UI at the pointer every frame;
        /// the drag is checked once, when the button goes down, and that answer holds until it is released. No pointer means not over the UI.</summary>
        public CameraInput Gather(Vector2 panValue, Vector2 dragValue, float zoomRaw, float orbitValue, bool dragDown, Vector2? pointer)
        {
            var over = pointer.HasValue && IsOverUi != null && IsOverUi(pointer.Value);
            if (dragDown && !dragWasDown) dragFromUi = over;
            if (!dragDown) dragFromUi = false;
            dragWasDown = dragDown;
            return new CameraInput
            {
                Pan = panValue, DragPan = dragValue,
                Zoom = Mathf.Clamp(zoomRaw / 120f, -3f, 3f),   // browsers report 100-120 per notch; clamp bursts
                Orbit = orbitValue, OverUi = over, DragFromUi = dragFromUi,
            };
        }

        /// <summary>Applies one frame of input to the rig and calls rig.Apply(). Pure apart from the rig, so it is testable.</summary>
        public static void Step(CameraRig rig, CameraInput input, float dt, Rect bounds)
        {
            var zoom = input.OverUi ? 0f : input.Zoom;
            var drag = input.DragFromUi ? Vector2.zero : input.DragPan;
            rig.Yaw = Mathf.Repeat(rig.Yaw + input.Orbit * OrbitDegreesPerSecond * dt, 360f);
            rig.Distance = Mathf.Clamp(rig.Distance * (1f - zoom * ZoomPerStep), MinDistance, MaxDistance);
            var yaw = Quaternion.Euler(0, rig.Yaw, 0);
            var move = input.Pan * (PanPerSecond * dt) - drag * DragPanPerPixel;
            var delta = yaw * new Vector3(move.x, 0, move.y) * rig.Distance;
            var p = rig.LookAt + delta;
            rig.LookAt = new Vector3(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), p.y, Mathf.Clamp(p.z, bounds.yMin, bounds.yMax));
            rig.Apply();
        }
    }
}
