using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, Gaussian far blur beyond the look-at point (tilt-shift look).
    /// Yaw turns the view around the look-at point (0 = looking north); CameraController drives LookAt, Distance and Yaw.
    /// Gaussian, not Bokeh: physical Bokeh DoF keeps everything sharp at 100 km focus distances.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 50f, Fov = 35f;
        public Vector3 LookAt; public float Distance = 120000f; public float Yaw;
        /// <summary>Share of the screen height (0..1) the UI covers at the top. The view is shifted so LookAt sits in the
        /// middle of the map area below it (an off-centre frustum: picking rays and WorldToScreenPoint follow it).</summary>
        public float TopInset;
        DepthOfField _dof;

        /// <summary>The global post-processing volume Init creates. Its runtime VolumeProfile must be owned by the caller
        /// (MapScene registers it with MapRuntimeAssets).</summary>
        public Volume Volume { get; private set; }

        public void Init()
        {
            var cam = GetComponent<Camera>(); cam.fieldOfView = Fov; cam.farClipPlane = 1_000_000f; cam.nearClipPlane = 50f;
            // Beyond the terrain the view shows open sea, not a grey void (playtest 1, item 9). At this pitch the top of the
            // view still looks down, so the sky never shows and a solid clear in the sea biome colour is the whole fix.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MapBuilder.BiomeColours["sea"];
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.isGlobal = true;
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _dof = vol.profile.Add<DepthOfField>(true); _dof.mode.Override(DepthOfFieldMode.Gaussian);
            _dof.gaussianMaxRadius.Override(1.5f);
            Volume = vol;
            Apply();
        }

        public void Apply()
        {
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = LookAt - transform.forward * Distance;
            if (_dof != null) { _dof.gaussianStart.Override(Distance * 1.05f); _dof.gaussianEnd.Override(Distance * 1.8f); }
            ApplyProjection();
        }

        /// <summary>Sets the camera's projection for <see cref="TopInset"/> only (the transform is left alone). Apply calls it
        /// every frame, so a new window aspect is picked up too.</summary>
        public void ApplyProjection()
        {
            var cam = GetComponent<Camera>();
            if (cam == null) return;
            cam.ResetProjectionMatrix();   // follows the aspect and FOV again
            if (TopInset <= 0f) return;
            // NDC y = (m11 y + m12 z) / -z, so m12 = s moves every point down by s: the centre of the area below the bar
            // (NDC y from -1 to 1 - 2 TopInset) is at -TopInset.
            var p = cam.projectionMatrix; p[1, 2] = Mathf.Clamp(TopInset, 0f, 0.5f); cam.projectionMatrix = p;
        }
    }
}
