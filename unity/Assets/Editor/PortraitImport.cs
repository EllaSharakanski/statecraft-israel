using System.IO;
using System.Linq;
using System.Text;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies every portrait people.yaml names (public-domain files under promo/ref/people) into
    /// Assets/UI/Portraits/&lt;person id&gt;.jpg, sets web-friendly import settings, writes PORTRAITS.txt (id, licence,
    /// source) and rebuilds PortraitCatalog.asset. Idempotent.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PortraitImport.Run</summary>
    public static class PortraitImport
    {
        public const string Folder = "Assets/UI/Portraits";
        public const string CatalogPath = Folder + "/PortraitCatalog.asset";

        public static void Run()
        {
            var content = ContentLoader.Load(Paths.Content);
            Directory.CreateDirectory(Folder);
            var credits = new StringBuilder("# Portraits shipped in the build (copied from people.yaml portrait records; public domain only).\n");
            var catalog = AssetDatabase.LoadAssetAtPath<PortraitCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PortraitCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.entries.Clear();
            foreach (var p in content.People.Where(p => p.Portrait != null && p.Portrait.File != ""))
            {
                if (p.Portrait!.License != "Public domain" && p.Portrait.License != "CC0")
                    throw new System.InvalidOperationException($"people.yaml: {p.Id} portrait licence '{p.Portrait.License}' is not public domain or CC0");
                var source = Path.Combine(Paths.RepoRoot, p.Portrait.File);
                if (!File.Exists(source)) throw new FileNotFoundException($"people.yaml: {p.Id} portrait file missing", source);
                var target = $"{Folder}/{p.Id}.jpg";
                File.Copy(source, target, true);
                AssetDatabase.ImportAsset(target);
                var importer = (TextureImporter)AssetImporter.GetAtPath(target);
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = 512;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.crunchedCompression = true;
                importer.compressionQuality = 50;
                importer.SaveAndReimport();
                catalog.entries.Add(new PortraitCatalog.Entry { personId = p.Id, image = AssetDatabase.LoadAssetAtPath<Texture2D>(target) });
                credits.Append(p.Id).Append(" | ").Append(p.Portrait.License).Append(" | ").Append(p.Portrait.Source).Append('\n');
            }
            File.WriteAllText(Folder + "/PORTRAITS.txt", credits.ToString());
            AssetDatabase.ImportAsset(Folder + "/PORTRAITS.txt");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
