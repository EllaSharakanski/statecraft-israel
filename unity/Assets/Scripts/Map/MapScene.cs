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
        [SerializeField] Material terrainMaterial, overlayMaterial, markerMaterial, selectionMaterial, seaMaterial;

        public GeoFrame Frame { get; private set; }
        /// <summary>hexes.json as the last Build read it (bounds, hex size, biomes).</summary>
        public HexFile Hexes { get; private set; }
        public Terrain Terrain { get; private set; }
        public Material MarkerMaterial => markerMaterial;

        /// <summary>The selected hex's outline: a child of the terrain (so a rebuild destroys it).</summary>
        public const string SelectionName = "HexSelection";
        /// <summary>The sea past the map's edge: a child of the terrain (so a rebuild destroys it).</summary>
        public const string SeaPlaneName = "SeaPlane";
        /// <summary>How far the sea plane reaches past the terrain on every side; the camera never sees farther (MaxDistance).</summary>
        public const float SeaMarginM = 500_000f;
        /// <summary>Depth of the sea plane under sea level, so the terrain's own sea draws over it inside the map.</summary>
        public const float SeaPlaneDropM = 30f;
        /// <summary>Highlight colour of the selected hex (the UI's gold, brighter, to read against every biome); MapMaterials
        /// bakes it into the editor-made HexSelection.mat.</summary>
        public static readonly Color SelectionColour = new Color(1f, 0.84f, 0.38f);
        /// <summary>Width of the outline band, as a fraction of the hex circumradius (about 600 m on a 5 km hex).</summary>
        public const float SelectionWidth = 0.12f;
        /// <summary>Height of the band above the hex overlay's lines, which it covers (its material also has a depth offset).</summary>
        public const float SelectionLiftM = 20f;

        /// <summary>Outlines hex (q, r) with a band draped on the terrain: the hex overlay's geometry (MapBuilder.DrapedRing,
        /// HexMath corners) drawn with the editor-made HexSelection.mat, whose depth offset keeps the overlay lines under it
        /// from z-fighting (no runtime material). The band is sampled at its outer, middle and inner rings and each index
        /// takes the highest, so terrain inside the band cannot rise through it. A hex that is not on the map hides it.</summary>
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
                renderer.sharedMaterial = selectionMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var mr = go.GetComponent<MeshRenderer>();
            HexJson hex = null;
            foreach (var h in Hexes.hexes) if (h.q == q && h.r == r) { hex = h; break; }
            if (hex == null) { mr.enabled = false; return; }
            var c = Frame.ToWorld(hex.lon, hex.lat, 0);
            var centre = new Vector2(c.x, c.z);
            float size = Hexes.hex_size_km * 1000f, clearance = MapBuilder.OverlayClearanceM + SelectionLiftM;
            var verts = new List<Vector3>(2 * MapBuilder.RingVertices);
            var middle = new List<Vector3>(MapBuilder.RingVertices);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size, clearance, verts);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size * (1f - SelectionWidth), clearance, verts);
            MapBuilder.DrapedRing(Frame, Terrain.terrainData, centre, size * (1f - SelectionWidth / 2), clearance, middle);
            int n = MapBuilder.RingVertices;
            var tris = new List<int>(6 * n);
            for (int j = 0; j < n; j++)
            {
                // Each index at the highest of the outer, middle and inner ring, so the band clears the ground across it.
                float y = Mathf.Max(verts[j].y, Mathf.Max(verts[n + j].y, middle[j].y));
                verts[j] = new Vector3(verts[j].x, y, verts[j].z); verts[n + j] = new Vector3(verts[n + j].x, y, verts[n + j].z);
            }
            for (int j = 0; j < n; j++)
            {
                int a = j, b = (j + 1) % n, ai = n + j, bi = n + (j + 1) % n;
                tris.AddRange(new[] { a, ai, b, b, ai, bi });   // the shader draws both faces (Cull Off)
            }
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            m.Clear();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            mr.enabled = true;
        }

        public void Configure(Material terrain, Material overlay, Material marker, Material selection, Material sea)
        { terrainMaterial = terrain; overlayMaterial = overlay; markerMaterial = marker; selectionMaterial = selection; seaMaterial = sea; }

        /// <summary>Builds from the repository files with System.IO (editor and desktop only).</summary>
        public void Build() => Build(GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap));

        public void Build(MapInputs inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (terrainMaterial == null) throw new InvalidOperationException("MapScene: terrainMaterial is not assigned");
            if (overlayMaterial == null) throw new InvalidOperationException("MapScene: overlayMaterial is not assigned");
            if (markerMaterial == null) throw new InvalidOperationException("MapScene: markerMaterial is not assigned");
            if (selectionMaterial == null) throw new InvalidOperationException("MapScene: selectionMaterial is not assigned");
            if (seaMaterial == null) throw new InvalidOperationException("MapScene: seaMaterial is not assigned");
            var cam = Camera.main;
            if (cam == null) throw new InvalidOperationException("MapScene needs a camera tagged MainCamera");

            Clear();
            var (meta, heights) = HeightmapLoader.Load(inputs.HeightmapJson, inputs.HeightmapR16, MetaPath);
            Hexes = MapBuilder.LoadHexes(inputs.HexesJson, HexesPath);
            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial, overlayMaterial);
            terrainGo.transform.SetParent(transform, true);
            Terrain = terrainGo.GetComponent<Terrain>();
            var size = Terrain.terrainData.size;
            AddSeaPlane(terrainGo.transform, size, (0f - meta.elev_min_m) * MapBuilder.VerticalExaggeration);
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

        /// <summary>A lit plane in the sea colour, just under sea level (0 m: heightmap sea), reaching SeaMarginM past the
        /// terrain: past the map's edge the sea is lit as the terrain's sea is, so no seam shows (fix round 1). Drawn with the
        /// editor-made Sea.mat on Unity's built-in quad; no collider, so clicks still pick only the terrain.</summary>
        void AddSeaPlane(Transform terrain, Vector3 size, float seaLevelY)
        {
            var plane = new GameObject(SeaPlaneName);
            plane.transform.SetParent(terrain, false);
            plane.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = plane.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = seaMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            plane.transform.localPosition = new Vector3(size.x / 2, seaLevelY - SeaPlaneDropM, size.z / 2);
            plane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // the quad faces -Z; this turns it to face up
            plane.transform.localScale = new Vector3(size.x + 2 * SeaMarginM, size.z + 2 * SeaMarginM, 1f);
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
