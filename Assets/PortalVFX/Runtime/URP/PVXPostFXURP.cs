using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PortalVFX
{
    /// <summary>URP side of PVXDemoPostFX: turns on camera post-processing and adds a global bloom / tonemap volume.</summary>
    static class PVXPostFXURP
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
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) return;
            bool editing = !Application.isPlaying;
            var cam = fx.GetComponent<Camera>();
            // Camera settings only at runtime: changing them in the editor would save URP components into the scene.
            if (!editing)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
            if (fx.transform.Find("PVX PostFX Volume (URP)") != null) return;

            var go = new GameObject("PVX PostFX Volume (URP)");
            // Never saved with the scene, so projects on other pipelines get no missing scripts.
            if (editing) go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            go.transform.SetParent(fx.transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PVX Demo PostFX";
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(fx.bloomThreshold);
            bloom.intensity.Override(fx.bloomIntensity);
            bloom.scatter.Override(fx.bloomScatter);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            profile.Add<Vignette>(true).intensity.Override(fx.vignette);
            volume.sharedProfile = profile;
        }
    }
}
