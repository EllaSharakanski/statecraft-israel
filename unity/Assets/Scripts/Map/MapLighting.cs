#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>The map's light (research notes §1 "Lighting", promo 03): a low, warm late-afternoon sun from the
    /// west-north-west, whose long shadows fall to the right of the default view, and a cool sky fill that keeps the shadow
    /// side readable. The sun is fixed in the world, so orbiting the camera moves the light across the screen.</summary>
    public static class MapLighting
    {
        public const float SunElevation = 30f;   // degrees above the horizon
        public const float SunAzimuth = 285f;    // degrees clockwise from north: west-north-west
        public const float SunIntensity = 1.6f;
        public static readonly Color SunColour = new Color(1.00f, 0.84f, 0.64f);
        public static readonly Color AmbientSky = new Color(0.52f, 0.60f, 0.72f);
        public static readonly Color AmbientEquator = new Color(0.48f, 0.46f, 0.42f);
        public static readonly Color AmbientGround = new Color(0.26f, 0.22f, 0.17f);

        /// <summary>The scene's first directional light, or null.</summary>
        public static Light? FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
                if (l.type == LightType.Directional) return l;
            return null;
        }

        /// <summary>Points, colours and enables shadows on <paramref name="sun"/> (when there is one) and sets the trilight
        /// ambient and its probe.</summary>
        public static void Apply(Light? sun)
        {
            if (sun != null)
            {
                // A light shines along its forward axis: towards the azimuth opposite the sun, tilted down by the elevation.
                sun.transform.rotation = Quaternion.Euler(SunElevation, SunAzimuth - 180f, 0f);
                sun.color = SunColour; sun.intensity = SunIntensity;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky; RenderSettings.ambientEquatorColor = AmbientEquator; RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.ambientProbe = AmbientProbe();
        }

        /// <summary>The trilight ambient as spherical harmonics (set explicitly: a runtime change of the trilight colours does
        /// not rebuild the probe the shaders read).</summary>
        public static SphericalHarmonicsL2 AmbientProbe()
        {
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(AmbientEquator);
            sh.AddDirectionalLight(Vector3.up, AmbientSky - AmbientEquator, 1f);
            sh.AddDirectionalLight(Vector3.down, AmbientGround - AmbientEquator, 1f);
            return sh;
        }
    }
}
