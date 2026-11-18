using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PortalVFX
{
    /// <summary>
    /// HDRP side of PVXDemoPostFX: solid-colour clear from the camera, fixed exposure (so the unlit portal colours
    /// look as authored instead of being auto-exposed), bloom, ACES tonemapping and vignette in a global volume.
    /// </summary>
    static class PVXPostFXHDRP
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() { PVXDemoPostFX.PipelineHandler -= Apply; PVXDemoPostFX.PipelineHandler += Apply; }

#if UNITY_EDITOR
        // Also in the editor, so the Scene view shows the glow without pressing Play.
        [UnityEditor.InitializeOnLoadMethod]
        static void RegisterInEditor() => Register();
#endif

        static void Apply(PVXDemoPostFX fx)
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)) return;
            bool editing = !Application.isPlaying;
            var cam = fx.GetComponent<Camera>();
            if (!editing)
            {
                var hd = cam.GetComponent<HDAdditionalCameraData>();
                if (hd == null) hd = cam.gameObject.AddComponent<HDAdditionalCameraData>();
                hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hd.backgroundColorHDR = cam.backgroundColor;
                hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
            }
            if (fx.transform.Find("PVX PostFX Volume (HDRP)") != null) return;

            var go = new GameObject("PVX PostFX Volume (HDRP)");
            // Never saved with the scene, so projects on other pipelines get no missing scripts.
            if (editing) go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            go.transform.SetParent(fx.transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PVX Demo PostFX";
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(fx.hdrpExposure);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(fx.bloomThreshold);
            bloom.intensity.Override(Mathf.Clamp01(fx.bloomIntensity * 0.15f));
            bloom.scatter.Override(fx.bloomScatter);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            profile.Add<Vignette>(true).intensity.Override(fx.vignette);
            profile.Add<Fog>(true).enabled.Override(false);
            volume.sharedProfile = profile;
        }
    }
}
