using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Jagged electric arc on a LineRenderer, regenerated every few frames.
    /// Ring mode jumps between random points on a circle (local XY plane); otherwise it runs start → end.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [AddComponentMenu("Portal VFX/Lightning Arc")]
    public class PVXLightning : MonoBehaviour
    {
        public bool ringMode = true;
        public float ringRadius = 1.5f;
        [Tooltip("Ring mode: max angle between arc ends, degrees.")]
        public float maxArc = 90f;
        public Vector3 start = new Vector3(0f, -1f, 0f);
        public Vector3 end = new Vector3(0f, 1f, 0f);
        [Range(2, 64)] public int segments = 14;
        public float jitter = 0.18f;
        [Tooltip("Seconds between shape changes.")]
        public float refresh = 0.06f;
        [Range(0f, 1f)] public float idleChance = 0.25f;

        LineRenderer line;
        float baseWidth = -1f;
        float timer;
        Vector3[] points;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            if (baseWidth < 0f) baseWidth = line.widthMultiplier;
            line.useWorldSpace = false;
            points = new Vector3[segments];
            line.positionCount = segments;
            Rebuild();
        }

        /// <summary>Builds one arc outside Play mode (thumbnails, editor previews).</summary>
        public void PreviewNow()
        {
            Awake();
            line.widthMultiplier = baseWidth;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = refresh * Random.Range(0.6f, 1.4f);
            Rebuild();
        }

        void Rebuild()
        {
            if (points == null || points.Length != segments)
            {
                points = new Vector3[segments];
                line.positionCount = segments;
            }

            // Some frames show nothing, which reads as crackling instead of a constant tube.
            line.widthMultiplier = Random.value < idleChance ? 0f : baseWidth * Random.Range(0.6f, 1.2f);

            Vector3 a = start, b = end;
            float a0 = 0f, a1 = 0f;
            if (ringMode)
            {
                a0 = Random.Range(0f, 360f);
                a1 = a0 + Random.Range(-maxArc, maxArc);
            }

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Vector3 p;
                if (ringMode)
                {
                    float ang = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad;
                    float r = ringRadius * (1f + Mathf.Sin(t * Mathf.PI) * Random.Range(-0.15f, 0.25f));
                    p = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                }
                else
                {
                    p = Vector3.Lerp(a, b, t);
                }
                float envelope = Mathf.Sin(t * Mathf.PI);
                p += Random.insideUnitSphere * jitter * envelope;
                points[i] = p;
            }
            line.SetPositions(points);
        }
    }
}
