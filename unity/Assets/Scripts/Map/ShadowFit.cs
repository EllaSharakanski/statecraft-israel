#nullable enable
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Shadows at map scale (research notes §0.2): URP has no per-camera shadow distance, so the active URP asset's
    /// shadowDistance follows the rig's distance, and Restore puts the asset's own value back (the map registers Restore as a
    /// teardown action, so destroying the map, a rebuild or the end of a test leaves the asset as it was).</summary>
    public static class ShadowFit
    {
        /// <summary>View depth of the top screen row is 1.66 x the rig distance at the largest top inset (LightingTests).</summary>
        public const float CoverFactor = 1.75f;
        public const float MinDistanceM = 3000f, MaxDistanceM = 450000f;

        static UniversalRenderPipelineAsset? touched;
        static float original;

        public static float Distance(float cameraDistance) => Mathf.Clamp(cameraDistance * CoverFactor, MinDistanceM, MaxDistanceM);

        public static void Apply(float cameraDistance)
        {
            var asset = UniversalRenderPipeline.asset;
            if (asset == null) return;
            if (touched != asset) { Restore(); touched = asset; original = asset.shadowDistance; }   // a quality-level switch
            asset.shadowDistance = Distance(cameraDistance);
        }

        public static void Restore()
        {
            if (touched != null) touched.shadowDistance = original;
            touched = null;
        }
    }
}
