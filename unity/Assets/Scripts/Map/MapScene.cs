using System;
using System.Collections.Generic;
using System.IO;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Builds the whole map: terrain, hexes, 1948 settlements, camera. Materials are editor-made assets
    /// (M0 correction 1). Build is idempotent: it destroys what the previous Build made first. In play mode the Boot
    /// component calls Build(inputs) once the inputs are loaded (fetched on WebGL); editor tools call Build().</summary>
    public sealed class MapScene : MonoBehaviour
    {
        [SerializeField] Material terrainMaterial, overlayMaterial, markerMaterial;

        public GeoFrame Frame { get; private set; }
        /// <summary>hexes.json as the last Build read it (bounds, hex size, biomes).</summary>
        public HexFile Hexes { get; private set; }
        public Terrain Terrain { get; private set; }
        public Material MarkerMaterial => markerMaterial;

        /// <summary>The selected hex's outline: a child of the terrain (so a rebuild destroys it).</summary>
        public const string SelectionName = "HexSelection";
        /// <summary>Highlight colour of the selected hex (the UI's gold, brighter, to read against every biome).</summary>
        public static readonly Color SelectionColour = new Color(1f, 0.84f, 0.38f);
        /// <summary>Width of the outline band, as a fraction of the hex circumradius (about 600 m on a 5 km hex).</summary>
        public const float SelectionWidth = 0.12f;
        /// <summary>Height of the band above the hex overlay's lines, which it covers.</summary>
        public const float SelectionLiftM = 20f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>Outlines hex (q, r) with a band draped on the terrain in <see cref="SelectionColour"/>: the hex overlay's
        /// geometry (MapBuilder.DrapedRing, HexMath corners) and its editor-made material, coloured through a
        /// MaterialPropertyBlock (no runtime material). A hex that is not on the map hides the outline.</summary>
        public void Select(int q, int r)
        {
            if (Terrain == null || Hexes == null) return;
            var found = Terrain.transform.Find(SelectionName);
            var go = found != null ? found.gameObject : null;
            if (go == null)
            {
                go = new GameObject(SelectionName);
                go.transform.SetParent(Terrain.transform, false);
                var mesh = Terrain.GetComponent<MapRuntimeAssets>().Own(new Mesh { name = SelectionName });
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = overlayMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColor, SelectionColour);
                renderer.SetPropertyBlock(block);
            }
            var mr = go.GetComponent<MeshRenderer>();
            HexJson hex = null;
            foreach (var h in Hexes.hexes) if (h.q == q && h.r == r) { hex = h; break; }
            if (hex == null) { mr.enabled = false; return; }
            var c = Frame.ToWorld(hex.lon, hex.lat, 0);
            var centre = new Vector2(c.x, c.z);
            float size = Hexes.hex_size_km * 1000f, clearance = MapBuilder.OverlayClearanceM + SelectionLiftM;
            var verts = new List<Vector3>(2 * MapBuilder.RingVertices);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size, clearance, verts);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size * (1f - SelectionWidth), clearance, verts);
            int n = MapBuilder.RingVertices;
            var tris = new List<int>(12 * n);
            for (int j = 0; j < n; j++)
            {
                // Both rings at the higher of the two, so the band clears the ground between them too.
                float y = Mathf.Max(verts[j].y, verts[n + j].y);
                verts[j] = new Vector3(verts[j].x, y, verts[j].z); verts[n + j] = new Vector3(verts[n + j].x, y, verts[n + j].z);
            }
            for (int j = 0; j < n; j++)
            {
                int a = j, b = (j + 1) % n, ai = n + j, bi = n + (j + 1) % n;
                tris.AddRange(new[] { a, ai, b, b, ai, bi, a, b, ai, b, bi, ai });   // both windings: seen from any yaw
            }
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            m.Clear();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            mr.enabled = true;
        }

        public void Configure(Material terrain, Material overlay, Material marker)
        { terrainMaterial = terrain; overlayMaterial = overlay; markerMaterial = marker; }

        /// <summary>Builds from the repository files with System.IO (editor and desktop only).</summary>
        public void Build() => Build(GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap));

        public void Build(MapInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (terrainMaterial == null) throw new InvalidOperationException("MapScene: terrainMaterial is not assigned");
            if (overlayMaterial == null) throw new InvalidOperationException("MapScene: overlayMaterial is not assigned");
            if (markerMaterial == null) throw new InvalidOperationException("MapScene: markerMaterial is not assigned");
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("MapScene needs a camera tagged MainCamera");

            Clear();
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16, MetaPath);
            Hexes = MapBuilder.LoadHexes(inputs.HexesJson, HexesPath);
            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            Frame = new GeoFrame(meta.lon0, meta.lon1, meta.lat0, meta.lat1, size.x, size.z);
            var list = SettlementPlacer.Parse(inputs.SettlementsYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), SettlementsPath);
            SettlementPlacer.Place(list, Frame, Terrain, markerMaterial).transform.SetParent(transform, true);

            if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.LookAt = Frame.ToWorld(34.95, 31.95, 0); rig.Init(); // central Israel: Tel Aviv–Jerusalem corridor
            rig.Volume.transform.SetParent(transform, true);
            // VolumeProfile's own teardown only releases its components, it does not destroy them (Core RP 17.3), so own both.
            var assets = terrainGo.GetComponent<MapRuntimeAssets>();
            foreach (var c in rig.Volume.profile.components) assets.Own(c);
            assets.Own(rig.Volume.profile);
        }

        static string MetaPath => "heightmap.json";
        static string HexesPath => "hexes.json";
        static string SettlementsPath => SettlementPlacer.DefaultFileName;

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
            Hexes = null;
        }
    }
}
