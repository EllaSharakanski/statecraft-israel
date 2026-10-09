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

        public T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        void OnDestroy()
        {
            foreach (var o in owned)
            {
                if (o == null) continue;
                if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
            }
            owned.Clear();
        }
    }
}
