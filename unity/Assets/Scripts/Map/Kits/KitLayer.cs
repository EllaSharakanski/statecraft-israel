#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map.Kits
{
    /// <summary>Builds the art spike's procedural layer under a returned "Kits" root: one static mesh per settlement (its
    /// buildings, merged: one draw each, culled by its bounds) drawn with the editor-made Kit.mat, and the instanced trees
    /// (<see cref="VegetationLayer"/>) drawn with Tree.mat. Meshes are owned by the map's MapRuntimeAssets, so a rebuild
    /// frees them. Registers the "kits" and "trees" visual toggles, so the frame-time profile prices each.</summary>
    public static class KitLayer
    {
        public const string RootName = "Kits", VegetationName = "Vegetation";

        public sealed class Stats
        {
            public int Buildings, Vertices, Triangles, Trees; public float BuildMs, PlantMs;
            public override string ToString() =>
                $"KITS buildings={Buildings} vertices={Vertices} triangles={Triangles} trees={Trees} build_ms={BuildMs:0} plant_ms={PlantMs:0}";
        }

        public static GameObject Build(IReadOnlyList<Settlement> settlements, Terrain terrain, GeoFrame frame, HexFile hexes, float elevMinM,
            Material kitMaterial, Material treeMaterial, MapRuntimeAssets assets, out Stats stats)
        {
            stats = new Stats();
            var root = new GameObject(RootName);
            var ground = new KitGround(terrain, frame, hexes, elevMinM);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (var (s, mesh, n) in SettlementKits.Build(settlements, ground))
            {
                assets.Own(mesh);
                var go = new GameObject(s.Id);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = kitMaterial;
                r.shadowCastingMode = ShadowCastingMode.On;
                stats.Buildings += n; stats.Vertices += mesh.vertexCount; stats.Triangles += (int)mesh.GetIndexCount(0) / 3;
            }
            stats.BuildMs = (float)clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var trees = Vegetation.Plant(settlements, ground);
            var veg = new GameObject(VegetationName);
            veg.transform.SetParent(root.transform, false);
            var layer = veg.AddComponent<VegetationLayer>();
            layer.Init(trees, treeMaterial, assets);
            stats.Trees = trees.Count;
            stats.PlantMs = (float)clock.Elapsed.TotalMilliseconds;
            assets.OnTeardown(VisualToggles.Register("kits", on =>
            {
                if (root == null) return;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = on;
            }));
            assets.OnTeardown(VisualToggles.Register("trees", on => { if (layer != null) layer.Draw = on; }));
            return root;
        }
    }
}
