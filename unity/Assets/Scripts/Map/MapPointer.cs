#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Statecraft.Game.Map
{
    /// <summary>Left click on the map (not on the UI) selects the hex under the pointer: a ray from the camera hits the
    /// terrain's collider, the hit becomes lon/lat (GeoFrame) and then a hex (HexPicker).</summary>
    public sealed class MapPointer : MonoBehaviour
    {
        public MapScene? Map;
        public Func<Vector2, bool>? IsOverUi;
        public event Action<int, int>? HexClicked;

        // Every press, at the position of its own input event (InputConfig keeps a release from merging into the next move),
        // queued and evaluated in order in Update: several presses in one slow frame are each checked where they happened,
        // never all at the frame's last pointer position (playtest 2, P2-4).
        readonly Queue<Vector2> presses = new Queue<Vector2>();
        InputAction? press;

        void OnEnable()
        {
            press = new InputAction("map-press", InputActionType.Button, "<Mouse>/leftButton");
            press.performed += OnPressed;
            press.Enable();
        }

        void OnDisable()
        {
            if (press != null) { press.performed -= OnPressed; press.Dispose(); press = null; }
            presses.Clear();
        }

        void OnPressed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Pointer p) OnPress(p.position.ReadValue());   // read during the event: its own position
        }

        /// <summary>Queues a press at <paramref name="at"/> (screen pixels, y up).</summary>
        internal void OnPress(Vector2 at) => presses.Enqueue(at);

        /// <summary>Handles every queued press, in order, each at its own position.</summary>
        internal void ProcessPresses()
        {
            while (presses.Count > 0) HandleClick(presses.Dequeue());
        }

        void Update() => ProcessPresses();

        /// <summary>A left click at <paramref name="pos"/> (screen pixels): ignored over the UI (the same IsOverUi the camera
        /// uses), otherwise the hex under the pointer is raised as HexClicked. Returns whether a hex was picked.</summary>
        public bool HandleClick(Vector2 pos)
        {
            var overUi = IsOverUi != null && IsOverUi(pos);
            Boot.DiagLog.NavLine(FormattableString.Invariant($"map click at ({pos.x:0}, {pos.y:0}) overUi={overUi}"));
            if (overUi) return false;                                    // the UI is asked first, whatever else is missing
            if (Map == null || Map.Terrain == null) return false;
            var cam = Camera.main;
            var collider = Map.Terrain.GetComponent<TerrainCollider>();
            // only the terrain's own collider: other colliders (markers, future props) never decide which hex is clicked
            if (cam == null || collider == null || !collider.Raycast(cam.ScreenPointToRay(pos), out var hit, 1_000_000f)) return false;
            var hex = Pick(Map, hit.point);
            Map.Select(hex.q, hex.r);                                   // the map marks the hex the info panel describes
            HexClicked?.Invoke(hex.q, hex.r);
            return true;
        }

        public static (int q, int r) Pick(MapScene map, Vector3 world)
        {
            var (lon, lat) = map.Frame.ToLonLat(world);
            return HexPicker.FromLonLat(lon, lat, map.Hexes.bounds, map.Hexes.hex_size_km);
        }
    }
}
