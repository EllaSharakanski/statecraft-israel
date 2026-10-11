#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>One frame of camera input, in the units the actions give: Pan in [-1, 1] per axis (keys), DragPan in
    /// screen pixels (right-button drag), Zoom in wheel notches (positive = in), Orbit in [-1, 1] (Q/E).</summary>
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
    /// distance, zoom is clamped to [MinDistance, MaxDistance], and the view stays on the map (issue #174): the look-at
    /// point is held far enough inside the map that the visible ground does not leave it, so the border skirt never fills
    /// the view at gameplay zoom. Where the view is wider than the map (the overview), it is centred on the map instead.</summary>
    [RequireComponent(typeof(CameraRig))]
    public sealed class CameraController : MonoBehaviour
    {
        /// <summary>6 km is the hero zoom (owner ruling 2026-10-11: symbolic-scale settlements need a closer view); 250 km
        /// shows the country.</summary>
        public const float MinDistance = 6000f, MaxDistance = 250000f;
        public const float PanPerSecond = 0.8f;        // distances per second at full key press
        public const float DragPanPerPixel = 0.0012f;  // distances per pixel of drag
        public const float ZoomPerStep = 0.1f;         // fraction of the distance per wheel notch
        public const float OrbitDegreesPerSecond = 60f;
        public const float MaxFrameSeconds = 0.1f;     // a stalled frame (tab in the background) must not fling the camera

        /// <summary>Answers "is the pointer over the UI?" for a screen position (y up). Null = never.</summary>
        public Func<Vector2, bool>? IsOverUi;
        /// <summary>The map rectangle on the ground (x, z), in world metres; the view is kept inside it (Step).</summary>
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
                // The Input System's default scroll behaviour (UniformAcrossAllPlatforms) gives 1 per notch on every platform
                // (measured in headed Chrome on WebGL, Task 24: a 100 px notch -> 1.0); clamp bursts.
                Zoom = Mathf.Clamp(zoomRaw, -3f, 3f),
                Orbit = orbitValue, OverUi = over, DragFromUi = dragFromUi,
            };
        }

        /// <summary>Applies one frame of input to the rig and calls rig.Apply(). Pure apart from the rig, so it is testable.</summary>
        public static void Step(CameraRig rig, CameraInput input, float dt, Rect bounds)
        {
            var zoom = input.OverUi ? 0f : input.Zoom;
            var drag = input.DragFromUi ? Vector2.zero : input.DragPan;
            rig.Yaw = Mathf.Repeat(rig.Yaw + input.Orbit * OrbitDegreesPerSecond * dt, 360f);
            // (1 - step)^zoom: one notch in is exactly ZoomPerStep closer, and n notches out undo n notches in (Task 24 review M3).
            rig.Distance = Mathf.Clamp(rig.Distance * Mathf.Pow(1f - ZoomPerStep, zoom), MinDistance, MaxDistance);
            var yaw = Quaternion.Euler(0, rig.Yaw, 0);
            var move = input.Pan * (PanPerSecond * dt) - drag * DragPanPerPixel;
            var delta = yaw * new Vector3(move.x, 0, move.y) * rig.Distance;
            var p = rig.LookAt + delta;
            // Issue #174: clamping only the look-at point let up to three quarters of the view (at a corner) show the border
            // skirt, whose land is a flat slab with a hard seam against the terrain. The view's ground footprint around the
            // look-at point depends on the distance, yaw, aspect and top inset, not on where the point is, so it is measured
            // once at this frame's pose and the point is held where the whole footprint stays on the map.
            rig.LookAt = p; rig.Apply();
            var view = ViewFootprint(rig);
            rig.LookAt = new Vector3(Fit(p.x, bounds.xMin, bounds.xMax, view.xMin, view.xMax), p.y,
                Fit(p.z, bounds.yMin, bounds.yMax, view.yMin, view.yMax));
            rig.Apply();
        }

        /// <summary>Corners of the visible map area in viewport units: the top inset's band is covered by the UI.</summary>
        static readonly Vector2[] FootprintCorners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

        /// <summary>The ground the rig's camera shows, as offsets (x, z) from the look-at point: the bounding box of the four
        /// corner rays of the visible area (below the top inset) cut with the plane y = ShadowFit.GroundY, the terrain's lowest
        /// point. Every real ground hit is nearer than the plane's, so the box holds everything the view can show (the frustum
        /// meets a plane in a convex quad, so its corners bound it). A rig without a camera shows a point (Rect.zero); a
        /// corner that does not look down gives an infinite box. Reads the camera as the rig's last Apply left it.</summary>
        public static Rect ViewFootprint(CameraRig rig)
        {
            var cam = rig.GetComponent<Camera>();
            if (cam == null) return Rect.zero;
            float top = 1f - Mathf.Clamp(rig.TopInset, 0f, CameraRig.MaxTopInset);
            float x0 = float.PositiveInfinity, x1 = float.NegativeInfinity, z0 = float.PositiveInfinity, z1 = float.NegativeInfinity;
            foreach (var c in FootprintCorners)
            {
                var ray = cam.ViewportPointToRay(new Vector3(c.x, c.y * top, 0f));
                if (ray.direction.y > -1e-4f)
                    return Rect.MinMaxRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);
                var hit = ray.origin + ray.direction * ((ShadowFit.GroundY - ray.origin.y) / ray.direction.y);
                x0 = Mathf.Min(x0, hit.x); x1 = Mathf.Max(x1, hit.x); z0 = Mathf.Min(z0, hit.z); z1 = Mathf.Max(z1, hit.z);
            }
            return Rect.MinMaxRect(x0 - rig.LookAt.x, z0 - rig.LookAt.z, x1 - rig.LookAt.x, z1 - rig.LookAt.z);
        }

        /// <summary>One axis of the look-at point: within [lo, hi] such that [v + viewMin, v + viewMax] stays in it. A view
        /// wider than the map is centred on it (continuous as the view grows, so zooming out never jumps); an unbounded
        /// view falls back to keeping the point itself on the map.</summary>
        static float Fit(float v, float lo, float hi, float viewMin, float viewMax)
        {
            float a = lo - viewMin, b = hi - viewMax;
            if (a <= b) return Mathf.Clamp(v, a, b);
            return float.IsInfinity(a) || float.IsInfinity(b) || float.IsNaN(a) || float.IsNaN(b) ? Mathf.Clamp(v, lo, hi) : 0.5f * (a + b);
        }
    }
}
