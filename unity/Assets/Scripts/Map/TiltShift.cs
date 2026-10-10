#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The tilt-shift band (plan Task 3; promo 03 blurs about the bottom 15% and top 20%). CameraRig.Apply calls
    /// Apply every frame with the top bar's inset and the rig distance; TiltShiftFeature draws the blur while Enabled. The
    /// radius is a share of the picture's height, so no pixel size is needed. Weight mirrors TiltWeight in TiltShift.shader.</summary>
    public static class TiltShift
    {
        public static readonly int ParamsId = Shader.PropertyToID("_ScTiltShift");
        public const float HalfWidth = 0.17f, Feather = 0.18f;
        /// <summary>The largest blur radius as a share of the picture's height: 10 px of a 1080-line picture.</summary>
        public const float MaxRadiusUv = 10f / 1080f;
        public const float FullStrengthDistanceM = 60000f, MinStrength = 0.35f;
        public static bool Enabled = true;

        /// <summary>HLSL smoothstep(p.y, p.y + p.z, |y - p.x|).</summary>
        public static float Weight(float y, Vector4 p)
        {
            float t = Mathf.Clamp01((Mathf.Abs(y - p.x) - p.y) / p.z);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The middle of the map area below a top bar covering <paramref name="topInset"/> of the screen.</summary>
        public static float FocusCentre(float topInset) => (1f - Mathf.Clamp(topInset, 0f, CameraRig.MaxTopInset)) * 0.5f;

        public static float Strength(float distance) =>
            Mathf.Lerp(1f, MinStrength, Mathf.InverseLerp(FullStrengthDistanceM, CameraController.MaxDistance, distance));

        public static Vector4 Params(float topInset, float distance) =>
            new Vector4(FocusCentre(topInset), HalfWidth, Feather, MaxRadiusUv * Strength(distance));

        public static void Apply(float topInset, float distance) => Shader.SetGlobalVector(ParamsId, Params(topInset, distance));

        /// <summary>No band set: TiltShiftFeature draws nothing until the next Apply (a camera no rig drives, or the map torn
        /// down), and the toggle is back on. CameraRig.Init hands it to the map's teardown.</summary>
        public static void Reset()
        {
            Enabled = true;
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
        }

        /// <summary>True when a rig has set a band with a radius. With the zero global (before the map's first Apply, or after
        /// its teardown) smoothstep(0, 0, |y|) is about 1 everywhere, so the composite would replace the whole frame with its
        /// half-resolution copy at full cost.</summary>
        public static bool HasBand => Shader.GetGlobalVector(ParamsId).w > 0f;
    }

    /// <summary>Asset paths the tests share with the editor scripts (tests cannot reference Assets/Editor).</summary>
    public static class PostFxPaths
    {
        public const string TiltShiftMaterial = "Assets/Materials/TiltShift.mat";
    }
}
