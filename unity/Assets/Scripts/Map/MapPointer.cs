#nullable enable
using System;
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

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            HandleClick(mouse.position.ReadValue());
        }

        /// <summary>A left click at <paramref name="pos"/> (screen pixels): ignored over the UI (the same IsOverUi the camera
        /// uses), otherwise the hex under the pointer is raised as HexClicked. Returns whether a hex was picked.</summary>
        public bool HandleClick(Vector2 pos)
        {
            if (IsOverUi != null && IsOverUi(pos)) return false;       // the UI is asked first, whatever else is missing
            if (Map == null || Map.Terrain == null) return false;
            var cam = Camera.main;
            var collider = Map.Terrain.GetComponent<TerrainCollider>();
            // only the terrain's own collider: other colliders (markers, future props) never decide which hex is clicked
            if (cam == null || collider == null || !collider.Raycast(cam.ScreenPointToRay(pos), out var hit, 1_000_000f)) return false;
            var hex = Pick(Map, hit.point);
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
