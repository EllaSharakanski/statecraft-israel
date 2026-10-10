using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>The import tool lives in the editor assembly, which tests cannot reference, so it is called by reflection.</summary>
public class PortraitImportTests
{
    const string Folder = "Assets/UI/Portraits";

    static void RunImport()
    {
        var t = Type.GetType("Statecraft.Game.EditorTools.PortraitImport, Assembly-CSharp-Editor");
        Assert.IsNotNull(t, "PortraitImport not found");
        t!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
    }

    static string Snapshot()
    {
        using var sha = SHA256.Create();
        var sb = new System.Text.StringBuilder();
        foreach (var f in Directory.GetFiles(Folder).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(Path.GetFileName(f)).Append(' ').Append(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f)))).Append('\n');
        return sb.ToString();
    }

    [Test]
    public void RunningTheImportAgainChangesNothing()
    {
        var before = Snapshot();
        RunImport();
        Assert.AreEqual(before, Snapshot());
    }

    [Test]
    public void NoPortraitIsLargerThan512PixelsOnAnyTarget()
    {
        var paths = Directory.GetFiles(Folder, "*.jpg");
        Assert.AreEqual(13, paths.Length);
        foreach (var path in paths.Select(p => p.Replace('\\', '/')))
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.LessOrEqual(imp.maxTextureSize, 512, path);
            foreach (var target in new[] { "DefaultTexturePlatform", "Standalone", "WebGL" })
                Assert.LessOrEqual(imp.GetPlatformTextureSettings(target).maxTextureSize, 512, path + " " + target);
            StringAssert.DoesNotContain("  maxTextureSize: 2048", File.ReadAllText(path + ".meta"), path + " legacy field");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.LessOrEqual(Mathf.Max(tex.width, tex.height), 512, path + " source size");
        }
    }
}
