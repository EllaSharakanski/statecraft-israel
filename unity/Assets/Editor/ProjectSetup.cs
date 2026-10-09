using UnityEditor;
using UnityEditor.Build;

namespace Statecraft.Game.EditorTools
{
    /// <summary>One-time project settings (M0 API corrections): IL2CPP for Standalone and WebGL, WebGL compression off.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ProjectSetup.Apply</summary>
    public static class ProjectSetup
    {
        public static void Apply()
        {
            PlayerSettings.companyName = "Statecraft";
            PlayerSettings.productName = "Statecraft Israel";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            AssetDatabase.SaveAssets();
        }
    }
}
