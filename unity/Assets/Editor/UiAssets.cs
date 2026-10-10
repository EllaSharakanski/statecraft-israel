using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates the UI Toolkit PanelSettings asset (tech notes 1.3): scale with screen size against 1920x1080,
    /// matching width and height equally, with the default runtime theme. An asset, not a runtime instance, so its theme
    /// and shader references are included in player builds (the M0 Shader.Find lesson). Idempotent.
    /// Run headless: Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.UiAssets.Create</summary>
    public static class UiAssets
    {
        public static void Create()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPaths.Theme);
            if (theme == null) throw new System.IO.FileNotFoundException("theme missing (commit Assets/UI/StatecraftTheme.tss)", UiPaths.Theme);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, UiPaths.Panel); }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
        }
    }
}
