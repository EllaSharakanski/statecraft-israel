#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper owned by the map (the
    /// pipeline assets' template profile already applied one; without any, the graded Negev clips about 2.5% of its
    /// pixels: GradeTests.TheSunlitDesertDoesNotClip), slightly brighter, more contrast (saturation neutral since the merge point), a warm white balance, teal shadows against amber
    /// highlights (the promo's complementary sea and land), and a gentle dark-navy vignette. LDR grading on both tiers: the
    /// tonemapper and one LUT in the uber pass.</summary>
    public static class MapGrade
    {
        /// <summary>Neutral, not ACES: it only rolls off highlights, so the sea's blue and the amber land keep their hue (plan Task 4).</summary>
        public const TonemappingMode Tonemapper = TonemappingMode.Neutral;
        /// <summary>Temperature 5, not the plan's 9 (Task 4 merge arithmetic, docs/visual-v1.md "Task 4: grade"): at 9 the t04
        /// diorama warmth was 0.118, and Track B's land (+0.021 before the grade, about +0.027 after it) would put the merged
        /// diorama at the promo's 0.145 with nothing left for Track B's later haze and palette work. Each unit is about 0.003
        /// of frame warmth, so 5 aims the merge at about 0.133.</summary>
        public const float Temperature = 5f;
        /// <summary>Saturation 0, not Task 4's 16 (merge-point carry, review I1): with Track B's palette the merged WebGL
        /// diorama02 measured 0.53 against the promo's 0.43 at 16; the bar is promo + 0.05 (docs/visual-v1.md "Merge point").</summary>
        public const float Saturation = 0f;
        public const float PostExposure = 0.15f, Contrast = 14f, Tint = 2f;
        public const float SplitBalance = -15f, VignetteIntensity = 0.26f, VignetteSmoothness = 0.45f;
        public static readonly Color SplitShadows = new Color(0.36f, 0.52f, 0.60f);
        public static readonly Color SplitHighlights = new Color(0.96f, 0.78f, 0.55f);
        public static readonly Color VignetteColour = new Color(0.05f, 0.06f, 0.10f);

        public static void Apply(VolumeProfile profile)
        {
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(Tonemapper);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(PostExposure); ca.contrast.Override(Contrast); ca.saturation.Override(Saturation);
            ca.hueShift.Override(0f); ca.colorFilter.Override(Color.white);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(Temperature); wb.tint.Override(Tint);
            var st = profile.Add<SplitToning>(true);
            st.shadows.Override(SplitShadows); st.highlights.Override(SplitHighlights); st.balance.Override(SplitBalance);
            var v = profile.Add<Vignette>(true);
            v.intensity.Override(VignetteIntensity); v.smoothness.Override(VignetteSmoothness); v.color.Override(VignetteColour);
            v.center.Override(new Vector2(0.5f, 0.5f)); v.rounded.Override(false);
        }

        /// <summary>The "grade" toggle. Off falls back to the pipeline assets' own profile (Settings/SampleSceneProfile: Neutral
        /// tonemapping and a 0.2 black vignette; its bloom is off on both tiers since Task 10), which every frame had before Task 4, not to a raw image.</summary>
        public static void SetActive(VolumeProfile profile, bool on)
        {
            foreach (var c in profile.components)
                if (c is Tonemapping || c is ColorAdjustments || c is WhiteBalance || c is SplitToning || c is Vignette) c.active = on;
        }
    }
}
