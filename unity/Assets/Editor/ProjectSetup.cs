using UnityEditor;
using UnityEditor.Build;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Project settings (M0 API corrections and M3 web settings): IL2CPP for Standalone and WebGL, the
    /// Statecraft WebGL template (full-window canvas, autoSyncPersistentDataPath), compression off by default (the build
    /// script sets it per target), Map.unity as the only and first scene.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ProjectSetup.Apply</summary>
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/Map.unity";
        public const string WebTemplate = "PROJECT:Statecraft";

        public static void Apply()
        {
            PlayerSettings.companyName = "Statecraft";
            PlayerSettings.productName = "Statecraft Israel";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template = WebTemplate;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
