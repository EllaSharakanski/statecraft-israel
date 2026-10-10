#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Import settings of the baked map textures: linear data (not sRGB), clamped, mipmapped, trilinear, 2048. The
    /// shading bake (curvature, sky-view) is block-compressed; the coast distance and the AO are each one uncompressed 8-bit
    /// channel (R8): compression blocks would show in the 350 m foam band, and BC1 stepped the AO on broad slopes (Task 6
    /// fix round 1). The land-use mask (1024 x 2048, RGB) is block-compressed too; the track offsets (512 x 1024) are RG16.
    /// Idempotent; MapMaterials.Create calls it.</summary>
    public static class MapTextures
    {
        public static void Configure()
        {
            Set(MapTexturePaths.Bake, singleChannel: false);
            Set(MapTexturePaths.Coast, singleChannel: true);
            Set(MapTexturePaths.Ao, singleChannel: true);
            Set(MapTexturePaths.Land, singleChannel: false);   // smooth weights: block compression is fine
            Set(MapTexturePaths.Tracks, singleChannel: false);
            // The track offsets: two uncompressed channels (RG16) on every platform, so the filter keeps them affine.
            var imp = (TextureImporter)AssetImporter.GetAtPath(MapTexturePaths.Tracks);
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            foreach (var platform in new[] { "Standalone", "WebGL" })
                imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    { name = platform, overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.RG16 });
            imp.SaveAndReimport();
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
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear;   // no mip seams across the pitched view
            imp.anisoLevel = path == MapTexturePaths.Bake ? 2 : 1;   // the shading is seen at a grazing angle
            imp.maxTextureSize = 2048;
            imp.textureCompression = singleChannel ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
    }
}
