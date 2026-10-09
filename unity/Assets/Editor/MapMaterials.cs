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

        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            Make(TerrainPath, "Universal Render Pipeline/Terrain/Lit", null);
            Make(OverlayPath, "Universal Render Pipeline/Unlit", new Color(0.15f, 0.12f, 0.08f, 1f));
            AssetDatabase.SaveAssets();
        }

        static void Make(string path, string shaderName, Color? colour)
        {
            var shader = Shader.Find(shaderName); // editor only: fine here
            if (shader == null) throw new System.InvalidOperationException($"shader not found: {shaderName}");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.enableInstancing = true;
            if (colour.HasValue) mat.color = colour.Value;
            EditorUtility.SetDirty(mat);
        }
    }
}
