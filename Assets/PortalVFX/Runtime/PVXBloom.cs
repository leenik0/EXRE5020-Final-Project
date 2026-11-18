using UnityEngine;
using UnityEngine.Rendering;

namespace PortalVFX
{
    /// <summary>
    /// Bloom + filmic tonemap for the Built-in pipeline, so portal cores glow instead of clipping.
    /// Put it on the camera (HDR on). Under URP/HDRP it does nothing: use the pipeline's Bloom volume there.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Portal VFX/Bloom (Built-in)")]
    public class PVXBloom : MonoBehaviour
    {
        [Min(0f)] public float threshold = 0.9f;
        [Range(0f, 1f)] public float softKnee = 0.5f;
        [Range(0f, 5f)] public float intensity = 1.4f;
        [Range(0.1f, 1f)] public float scatter = 0.75f;
        [Range(2, 8)] public int iterations = 6;
        [Min(0f)] public float exposure = 1.1f;
        [Range(0f, 2f)] public float vignette = 0.6f;
        [Tooltip("Filmic contrast after tonemapping (1.8 matches URP's ACES look; 1 = flat).")]
        [Range(1f, 2.5f)] public float contrast = 1.8f;
        [Tooltip("Hidden/PortalVFX/Bloom (referenced so it is included in builds).")]
        public Shader shader;

        Material mat;
        readonly RenderTexture[] chain = new RenderTexture[8];

        void OnEnable()
        {
            if (GraphicsSettings.currentRenderPipeline != null) { enabled = false; return; }
            GetComponent<Camera>().allowHDR = true;
        }

        void OnDisable()
        {
            if (mat != null) Destroy(mat);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null)
            {
                if (shader == null) shader = Shader.Find("Hidden/PortalVFX/Bloom");
                if (shader == null) { Graphics.Blit(src, dst); return; }
                mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            // Demo cameras share their look with the URP / HDRP volume.
            var fx = GetComponent<PVXDemoPostFX>();
            float threshold = fx != null ? fx.bloomThreshold : this.threshold;
            float intensity = fx != null ? fx.bloomIntensity : this.intensity;
            float scatter = fx != null ? fx.bloomScatter : this.scatter;
            float vignette = fx != null ? fx.vignette * 2f : this.vignette;
            float exposure = fx != null ? 1f : this.exposure;
            mat.SetFloat("_Threshold", threshold);
            mat.SetFloat("_Knee", Mathf.Max(threshold * softKnee, 1e-4f));
            mat.SetFloat("_BloomIntensity", intensity);
            mat.SetFloat("_Scatter", scatter);
            mat.SetFloat("_Exposure", exposure);
            mat.SetFloat("_Vignette", vignette);
            mat.SetFloat("_Contrast", contrast);
            mat.SetFloat("_Gain", 0.85f + (1f - contrast / 1.8f) * 0.15f);

            int w = src.width / 2, h = src.height / 2;
            var fmt = RenderTextureFormat.DefaultHDR;
            int n = 0;
            chain[0] = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(src, chain[0], mat, 0);
            for (n = 1; n < iterations && w > 8 && h > 8; n++)
            {
                w /= 2; h /= 2;
                chain[n] = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(chain[n - 1], chain[n], mat, 1);
            }
            for (int k = n - 1; k > 0; k--) Graphics.Blit(chain[k], chain[k - 1], mat, 2);
            mat.SetTexture("_BloomTex", chain[0]);
            Graphics.Blit(src, dst, mat, 3);
            for (int k = 0; k < n; k++) { RenderTexture.ReleaseTemporary(chain[k]); chain[k] = null; }
        }
    }
}
