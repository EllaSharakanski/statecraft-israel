#nullable enable
namespace Statecraft.Game.Map
{
    /// <summary>The quality-tier decisions of visual v1 Task 10 (evidence: docs/visual-v1.md "Tier decisions"). Change a value
    /// only with new measurements recorded there; QualityTiers.Apply writes them to the URP assets.</summary>
    public static class QualityTierValues
    {
        /// <summary>1.0, not 0.8: at 1.0 every budget held (headless p95 5 / 11 ms, headed 5 / 11 ms, headed DPR 2 5 / 19 ms
        /// at gameplay03 / diorama02 against 16.7, 20 and 33 ms).</summary>
        public const float WebRenderScale = 1f;
        /// <summary>2048 kept although the diorama02 shadow cost (2.96 ms, spread 0.32) is over 2.5 ms plus the spread: a 1024
        /// build measured 2.84 ms (headless), 2.94 ms (headed) and 5.03 ms (DPR 2) against 2.96 / 2.90 / 4.99 at 2048, so the
        /// cost is the caster pass's geometry, not the map's resolution, and halving it would only blur the relief.</summary>
        public const int WebShadowMap = 2048, WebTiltShiftDownsample = 2;
        public const int DesktopShadowMap = 2048, DesktopTiltShiftDownsample = 2;
        /// <summary>The pipeline profile's bloom (the URP template's 0.25 above 1), off on both tiers: rendered, it changed at
        /// most 2/255 of any pixel at either pose on either tier, for 0.1-0.4 ms on the web and 0.13-0.17 ms on the macOS
        /// player.</summary>
        public const bool Bloom = false;
    }
}
