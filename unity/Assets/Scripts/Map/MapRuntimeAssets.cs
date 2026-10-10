using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Owns the objects MapBuilder creates at runtime (TerrainData, TerrainLayers, their textures, the overlay
    /// mesh) and destroys them with the map, so rebuilding the map does not leak them. [ExecuteAlways] so OnDestroy also
    /// runs in edit mode (edit-mode tests, editor capture tools).</summary>
    [ExecuteAlways]
    public sealed class MapRuntimeAssets : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        readonly List<System.Action> teardown = new List<System.Action>();

        public T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        /// <summary>Runs <paramref name="action"/> when the map is destroyed (newest first), for state the map changes outside
        /// itself: the URP asset's shadow distance, global shader values.</summary>
        public void OnTeardown(System.Action action) => teardown.Add(action ?? throw new System.ArgumentNullException(nameof(action)));

        void OnDestroy()
        {
            for (int i = teardown.Count - 1; i >= 0; i--) teardown[i]();
            teardown.Clear();
            foreach (var o in owned)
            {
                if (o == null) continue;
                if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
            }
            owned.Clear();
        }
    }
}
