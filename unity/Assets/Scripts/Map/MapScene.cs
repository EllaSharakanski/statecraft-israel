using System;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Builds the whole map at runtime: terrain, hexes, 1948 settlements, camera. Materials are editor-made assets
    /// (M0 correction 1). Build is idempotent: it destroys what the previous Build made first.</summary>
    public sealed class MapScene : MonoBehaviour
    {
        [SerializeField] Material terrainMaterial, overlayMaterial, markerMaterial;

        public GeoFrame Frame { get; private set; }
        public Terrain Terrain { get; private set; }
        GameObject _settlements, _postFx;

        void Awake() => Build();

        public void Configure(Material terrain, Material overlay, Material marker)
        { terrainMaterial = terrain; overlayMaterial = overlay; markerMaterial = marker; }

        public void Build()
        {
            if (terrainMaterial == null) throw new InvalidOperationException("MapScene: terrainMaterial is not assigned");
            if (overlayMaterial == null) throw new InvalidOperationException("MapScene: overlayMaterial is not assigned");
            if (markerMaterial == null) throw new InvalidOperationException("MapScene: markerMaterial is not assigned");
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("MapScene needs a camera tagged MainCamera");

            Clear();
            var terrainGo = MapBuilder.Build(Paths.StreamingMap, Path.Combine(Paths.Content, "map", "hexes.json"), terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            var (meta, _) = HeightmapLoader.Load(Paths.StreamingMap);
            Frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            var list = SettlementPlacer.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", "settlements_1948.yaml")),
                (meta.lon0, meta.lon1, meta.lat0, meta.lat1));
            _settlements = SettlementPlacer.Place(list, Frame, Terrain, markerMaterial);
            _settlements.transform.SetParent(transform, true);

            if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
            _postFx = rig.Volume.gameObject; _postFx.transform.SetParent(transform, true);
            terrainGo.GetComponent<MapRuntimeAssets>().Own(rig.Volume.profile);
        }

        void Clear()
        {
            // The VolumeProfile is owned by the terrain's MapRuntimeAssets, so destroying the terrain destroys it.
            foreach (var go in new[] { Terrain != null ? Terrain.gameObject : null, _settlements, _postFx })
                if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            Terrain = null; _settlements = null; _postFx = null;
        }
    }
}
