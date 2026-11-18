using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Post-processing for the demo scenes, in whatever render pipeline the project uses: bloom, filmic tonemapping
    /// and a light vignette so portal cores glow instead of clipping. Built-in uses PVXBloom on the camera; under
    /// URP / HDRP the matching PortalVFX.Runtime.URP / .HDRP module (compiled only when that pipeline is installed)
    /// creates a global Volume from these settings at runtime. Scenes therefore carry no pipeline-specific components.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Portal VFX/Demo/Post FX (any pipeline)")]
    public class PVXDemoPostFX : MonoBehaviour
    {
        [Min(0f)] public float bloomThreshold = 0.8f;
        [Min(0f)] public float bloomIntensity = 2.2f;
        [Range(0f, 1f)] public float bloomScatter = 0.72f;
        [Range(0f, 1f)] public float vignette = 0.3f;
        [Tooltip("HDRP only: fixed exposure (EV100). 0 keeps the unlit portal colours as authored.")]
        public float hdrpExposure = 0f;

        /// <summary>Set by a pipeline module; called once per enabled component.</summary>
        public static System.Action<PVXDemoPostFX> PipelineHandler;

        void OnEnable() => PipelineHandler?.Invoke(this);
    }
}
