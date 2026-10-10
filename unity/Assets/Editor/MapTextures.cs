#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Import settings of the baked map textures: linear data (not sRGB), clamped, mipmapped, 2048. The shading bake
    /// is block-compressed (smooth fields compress well); the coast distance is one uncompressed 8-bit channel (R8), since
    /// compression blocks would show in the 350 m foam band. Idempotent; MapMaterials.Create calls it.</summary>
    public static class MapTextures
    {
        public static void Configure()
        {
            Set(MapTexturePaths.Bake, singleChannel: false);
            Set(MapTexturePaths.Coast, singleChannel: true);
        }

        static void Set(string path, bool singleChannel)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new System.IO.FileNotFoundException("run tools/terrain_bake.py", path);
            imp.textureType = singleChannel ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            if (singleChannel)
            {
                var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
                s.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                imp.SetTextureSettings(s);
            }
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.isReadable = false;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear; imp.maxTextureSize = 2048;
            imp.textureCompression = singleChannel ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
    }
}
