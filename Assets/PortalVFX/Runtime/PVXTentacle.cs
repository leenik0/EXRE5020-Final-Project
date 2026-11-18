using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Curling energy tendril on a LineRenderer. Grows out with the portal's open progress,
    /// sways continuously and periodically pulls back in.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [AddComponentMenu("Portal VFX/Tentacle")]
    public class PVXTentacle : MonoBehaviour
    {
        public float length = 1.8f;
        [Range(4, 64)] public int segments = 24;
        public float waveAmplitude = 0.25f;
        public float waveFrequency = 2.2f;
        public float waveSpeed = 2.5f;
        [Tooltip("How far the tip curls sideways.")]
        public float curl = 0.6f;
        [Tooltip("Seconds per reach-and-retract cycle.")]
        public float retractPeriod = 3.5f;
        public float phase;

        LineRenderer line;
        PVXPortal portal;
        Vector3[] points;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            portal = GetComponentInParent<PVXPortal>();
            points = new Vector3[segments];
            line.positionCount = segments;
        }

        /// <summary>Builds the line once outside Play mode (thumbnails, editor previews).</summary>
        public void PreviewNow()
        {
            Awake();
            Update();
        }

        void Update()
        {
            float t = Time.time + phase;
            float open = portal != null ? portal.OpenAmount : 1f;
            float pulse = portal != null ? portal.Pulse : 0f;
            float reach = 0.55f + 0.45f * Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t / retractPeriod * 2f, 1f));
            float len = length * open * reach * (1f + pulse * 0.4f);

            for (int i = 0; i < segments; i++)
            {
                float u = i / (float)(segments - 1);
                float d = u * len;
                float sway = Mathf.Sin(u * waveFrequency * Mathf.PI * 2f - t * waveSpeed) * waveAmplitude * u;
                float c = u * u * curl * Mathf.Sin(t * 0.7f);
                points[i] = new Vector3(sway + c, d, Mathf.Cos(u * 5f - t * 1.3f) * waveAmplitude * 0.5f * u);
            }
            line.SetPositions(points);
        }
    }
}
