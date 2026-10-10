#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Writes the visual v1 tier decisions (QualityTierValues; docs/visual-v1.md "Tier decisions") to the URP assets:
    /// web (Mobile_*): Forward, no depth or opaque copy, one hard cascade; desktop (PC_*): four soft cascades, no depth or
    /// opaque copy, SSAO off (the baked AO replaces it); the tilt-shift's resolution on both; the bloom of the pipeline
    /// assets' own volume profile. Idempotent. Run headless:
    /// Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.QualityTiers.Apply</summary>
    public static class QualityTiers
    {
        public const string WebAsset = "Assets/Settings/Mobile_RPAsset.asset", DesktopAsset = "Assets/Settings/PC_RPAsset.asset";
        public const string WebRenderer = "Assets/Settings/Mobile_Renderer.asset", DesktopRenderer = "Assets/Settings/PC_Renderer.asset";

        public static void Apply()
        {
            Pipeline(WebAsset, QualityTierValues.WebRenderScale, 1, QualityTierValues.WebShadowMap, soft: false);
            Pipeline(DesktopAsset, 1f, 4, QualityTierValues.DesktopShadowMap, soft: true);
            Renderer(WebRenderer, QualityTierValues.WebTiltShiftDownsample, ssao: null);
            Renderer(DesktopRenderer, QualityTierValues.DesktopTiltShiftDownsample, ssao: false);
            Bloom(WebAsset, QualityTierValues.Bloom);
            Bloom(DesktopAsset, QualityTierValues.Bloom);
            AssetDatabase.SaveAssets();
        }

        static UniversalRenderPipelineAsset Load(string path) =>
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) ?? throw new System.IO.FileNotFoundException(path);

        static void Pipeline(string path, float renderScale, int cascades, int shadowMap, bool soft)
        {
            var so = new SerializedObject(Load(path));
            so.FindProperty("m_RenderScale").floatValue = renderScale;
            so.FindProperty("m_ShadowCascadeCount").intValue = cascades;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = shadowMap;
            so.FindProperty("m_SoftShadowsSupported").boolValue = soft;
            so.FindProperty("m_RequireDepthTexture").boolValue = false;
            so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Renderer(string path, int tiltShiftDownsample, bool? ssao)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path) ?? throw new System.IO.FileNotFoundException(path);
            foreach (var f in data.rendererFeatures)
            {
                if (f is TiltShiftFeature t) { t.Downsample = tiltShiftDownsample; EditorUtility.SetDirty(t); }
                else if (f != null && ssao.HasValue && f.GetType().Name == "ScreenSpaceAmbientOcclusion") { f.SetActive(ssao.Value); EditorUtility.SetDirty(f); }
            }
            EditorUtility.SetDirty(data);
        }

        /// <summary>The pipeline asset's own volume profile (today the template's Settings/SampleSceneProfile on both tiers)
        /// keeps its Neutral tonemapper either way: it is what keeps the tonemapper's variants in builds (GradeTests).</summary>
        static void Bloom(string path, bool on)
        {
            var profile = Load(path).volumeProfile ?? throw new System.InvalidOperationException(path + " has no volume profile");
            if (profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom)) { bloom.active = on; EditorUtility.SetDirty(bloom); }
            EditorUtility.SetDirty(profile);
        }
    }
}
