using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Shows a renderer only during the first part of opening (and the last part of closing), e.g. a glass
    /// pane that is visible before it shatters. Fades the material _Opacity through the property block.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("Portal VFX/Shatter Hide")]
    public class PVXShatterHide : MonoBehaviour
    {
        [Tooltip("Open progress (0..1) at which the renderer is gone.")]
        [Range(0.01f, 1f)] public float hideAfter = 0.3f;
        public float opacity = 0.6f;

        static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        Renderer rend;
        PVXPortal portal;
        MaterialPropertyBlock block;

        void LateUpdate() => Apply();

        public void Apply()
        {
            if (rend == null)
            {
                rend = GetComponent<Renderer>();
                portal = GetComponentInParent<PVXPortal>();
                block = new MaterialPropertyBlock();
            }
            float open = portal != null ? portal.OpenAmount : 1f;
            float k = 1f - Mathf.Clamp01(open / hideAfter);
            rend.GetPropertyBlock(block);
            block.SetFloat(OpacityId, opacity * Mathf.Sqrt(k));
            rend.SetPropertyBlock(block);
        }
    }
}
