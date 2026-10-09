using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, Gaussian far blur beyond the look-at point (tilt-shift look).
    /// Gaussian, not Bokeh: physical Bokeh DoF keeps everything sharp at 100 km focus distances.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float Pitch = 50f, Fov = 35f;
        public Vector3 LookAt; public float Distance = 120000f;
        DepthOfField _dof;

        /// <summary>The global post-processing volume Init creates. Its runtime VolumeProfile must be owned by the caller
        /// (MapScene registers it with MapRuntimeAssets).</summary>
        public Volume Volume { get; private set; }

        public void Init()
        {
            var cam = GetComponent<Camera>(); cam.fieldOfView = Fov; cam.farClipPlane = 1_000_000f; cam.nearClipPlane = 50f;
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
            transform.rotation = Quaternion.Euler(Pitch, 0, 0);
            transform.position = LookAt - transform.forward * Distance;
            if (_dof != null) { _dof.gaussianStart.Override(Distance * 1.05f); _dof.gaussianEnd.Override(Distance * 1.8f); }
        }
    }
}
