using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the map's material assets (M0 correction 1: Shader.Find returns null in players, so materials are
    /// editor-made assets passed to MapBuilder.Build through serialized fields). Idempotent.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.MapMaterials.Create</summary>
    public static class MapMaterials
    {
        public const string Folder = "Assets/Materials";
        public const string TerrainPath = Folder + "/Terrain.mat";
        public const string OverlayPath = Folder + "/HexOverlay.mat";
        public const string MarkerPath = Folder + "/SettlementMarker.mat";
        public const string SelectionPath = Folder + "/HexSelection.mat";
        public const string SeaPath = Folder + "/Sea.mat";
        public const string BorderLandPath = Folder + "/BorderLand.mat";

        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            MapTextures.Configure();
            // The Statecraft terrain graph (tools/terrain_graph.py + StatecraftTerrain.hlsl) with the baked textures.
            var terrain = Make(TerrainPath, "Statecraft/StatecraftTerrain", null);
            terrain.SetTexture("_ScBakeTex", Tex(Statecraft.Game.Map.MapTexturePaths.Bake));
            terrain.SetTexture("_ScCoastTex", Tex(Statecraft.Game.Map.MapTexturePaths.Coast));
            EditorUtility.SetDirty(terrain);
            Make(OverlayPath, "Universal Render Pipeline/Unlit", new Color(0.15f, 0.12f, 0.08f, 1f));
            // Settlement boxes: matte URP Lit; SettlementPlacer sets the per-type _BaseColor through a MaterialPropertyBlock.
            var marker = Make(MarkerPath, "Universal Render Pipeline/Lit", Color.white);
            marker.SetFloat("_Smoothness", 0f); marker.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(marker);
            // The selected hex's outline: its own shader (Assets/Shaders/HexSelection.shader) with a depth offset (fix round 1, M2).
            Make(SelectionPath, "Statecraft/HexSelection", Statecraft.Game.Map.MapScene.SelectionColour);
            // The sea past the map's edge: URP Lit in the sea biome colour, matte like the terrain's layers, so it is lit as the
            // terrain's sea is and no seam shows at the edge (fix round 1).
            var sea = Make(SeaPath, "Universal Render Pipeline/Lit", Statecraft.Game.Map.MapBuilder.BiomeColours["sea"]);
            // Terrain/Lit keeps its specular and environment reflections at smoothness 0, so the plane keeps them too.
            sea.SetFloat("_Smoothness", 0f); sea.SetFloat("_Metallic", 0f);
            sea.SetFloat("_SpecularHighlights", 1f); sea.SetFloat("_EnvironmentReflections", 1f);
            sea.DisableKeyword("_SPECULARHIGHLIGHTS_OFF"); sea.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(sea);
            // Neighbouring land past the map's edge (Sinai, Jordan, Lebanon): one muted land tone, lit and matte like the sea
            // (fix round 2, R2-1).
            var land = Make(BorderLandPath, "Universal Render Pipeline/Lit", Statecraft.Game.Map.MapBuilder.BorderLandColour);
            land.SetFloat("_Smoothness", 0f); land.SetFloat("_Metallic", 0f);
            land.SetFloat("_SpecularHighlights", 1f); land.SetFloat("_EnvironmentReflections", 1f);
            EditorUtility.SetDirty(land);
            AssetDatabase.SaveAssets();
        }

        static Material Make(string path, string shaderName, Color? colour)
        {
            var shader = Shader.Find(shaderName); // editor only: fine here
            if (shader == null) throw new System.InvalidOperationException($"shader not found: {shaderName}");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.enableInstancing = true;
            if (colour.HasValue) mat.color = colour.Value;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D Tex(string path) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(path) ?? throw new System.IO.FileNotFoundException("texture missing (tools/terrain_bake.py)", path);
    }
}
