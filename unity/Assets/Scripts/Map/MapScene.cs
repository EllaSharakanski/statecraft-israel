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
            var yamlPath = Path.Combine(Paths.Content, "map", SettlementPlacer.DefaultFileName);
            var list = SettlementPlacer.Parse(File.ReadAllText(yamlPath), (meta.lon0, meta.lon1, meta.lat0, meta.lat1), yamlPath);
            SettlementPlacer.Place(list, Frame, Terrain, markerMaterial).transform.SetParent(transform, true);

            if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
            rig.Volume.transform.SetParent(transform, true);
            // VolumeProfile's own teardown only releases its components, it does not destroy them (Core RP 17.3), so own both.
            var assets = terrainGo.GetComponent<MapRuntimeAssets>();
            foreach (var c in rig.Volume.profile.components) assets.Own(c);
            assets.Own(rig.Volume.profile);
        }

        /// <summary>Destroys every child: all of them are things Build made (terrain, Settlements, PostFX). Walking the
        /// hierarchy rather than remembered fields keeps Build idempotent after a domain reload (R2). The VolumeProfile is
        /// owned by the terrain's MapRuntimeAssets, so destroying the terrain destroys it.</summary>
        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var go = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { go.transform.SetParent(null); Destroy(go); } else DestroyImmediate(go);
            }
            Terrain = null;
        }
    }
}
