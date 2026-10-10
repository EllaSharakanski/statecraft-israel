using System.IO;
using Statecraft.Game.Boot;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies the repository's content/ into Assets/StreamingAssets/content with a manifest.json before every
    /// player build (git-ignored output), so players read the same content the editor reads from the repo.
    /// Run alone: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ContentSync.Sync</summary>
    public sealed class ContentSync : IPreprocessBuildWithReport
    {
        public static string Target => Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "content");

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Sync();

        public static void Sync()
        {
            var files = ContentManifest.Copy(Paths.Content, Target);
            try { ContentManifest.Verify(Paths.Content, Target); }
            catch (InvalidDataException e) { throw new BuildFailedException("ContentSync: " + e.Message); }
            UnityEngine.Debug.Log($"ContentSync: {files.Count} files to {Target}");
        }
    }
}
