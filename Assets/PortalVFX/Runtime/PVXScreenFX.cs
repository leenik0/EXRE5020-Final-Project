using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// First-person feel for passing through a portal: put on the camera and point it at the player's
    /// PVXTeleportable. While the player dissolves the view is yanked forward (FOV punch), shakes, and a
    /// swirl of speed lines and a flash in the portal colour close in; it all unwinds as the player rebuilds.
    /// Works in every pipeline (no post-processing needed).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Portal VFX/Screen FX")]
    [DefaultExecutionOrder(100)] // after camera rigs have placed the camera this frame
    public class PVXScreenFX : MonoBehaviour
    {
        public PVXTeleportable target;
        [Tooltip("Additive overlay material (PortalVFX/Surface with speed-line arms and a clear centre).")]
        public Material overlayMaterial;
        public float fovPunch = 28f;
        public float shake = 0.06f;
        [Range(0f, 3f)] public float overlayIntensity = 1.2f;

        Camera cam;
        float baseFov;
        float amount, goal, speed = 2f;
        Transform overlay;
        Renderer overlayRenderer;
        MaterialPropertyBlock block;
        Color tint = Color.white;
        static readonly int OpenId = Shader.PropertyToID("_Open");
        static readonly int IntensityId = Shader.PropertyToID("_GlobalIntensity");
        static readonly int TintId = Shader.PropertyToID("_GlobalTint");

        void Awake()
        {
            cam = GetComponent<Camera>();
            baseFov = cam.fieldOfView;
            block = new MaterialPropertyBlock();
            if (overlayMaterial != null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "PortalScreenFX";
                overlay = go.transform;
                overlay.SetParent(transform, false);
                overlayRenderer = go.GetComponent<Renderer>();
                overlayRenderer.sharedMaterial = overlayMaterial;
                overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlayRenderer.enabled = false;
            }
        }

        void OnEnable() { if (target != null) target.Phase += OnPhase; }
        void OnDisable() { if (target != null) target.Phase -= OnPhase; }

        void OnPhase(PVXTeleportable t, bool dissolving)
        {
            tint = t.CurrentEdge;
            goal = dissolving ? 1f : 0f;
            speed = 1f / Mathf.Max(0.05f, t.dissolveTime);
        }

        void LateUpdate()
        {
            amount = Mathf.MoveTowards(amount, goal, Time.deltaTime * speed);
            float k = amount * amount * (3f - 2f * amount);
            cam.fieldOfView = baseFov + fovPunch * k;
            if (k > 0.001f) transform.position += Random.insideUnitSphere * shake * k;

            if (overlay == null) return;
            overlayRenderer.enabled = k > 0.001f;
            if (!overlayRenderer.enabled) return;
            // Fill the view just past the near plane.
            float d = cam.nearClipPlane + 0.05f;
            float h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
            overlay.localPosition = new Vector3(0f, 0f, d);
            overlay.localRotation = Quaternion.identity;
            overlay.localScale = new Vector3(h * cam.aspect, h, 1f);
            overlayRenderer.GetPropertyBlock(block);
            block.SetFloat(OpenId, Mathf.Lerp(0.6f, 1f, k));
            block.SetFloat(IntensityId, overlayIntensity * k);
            var c = tint;
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b), 1e-3f);
            block.SetColor(TintId, new Color(c.r / m, c.g / m, c.b / m, 1f));
            overlayRenderer.SetPropertyBlock(block);
        }
    }
}
