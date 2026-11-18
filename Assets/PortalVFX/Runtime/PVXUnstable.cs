using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PortalVFX
{
    /// <summary>
    /// Makes a portal look like it is about to fail: it flickers and browns out, shudders, glitches in size,
    /// throws energy surges and pulls its light with it. Raise "instability" over time (or call Destabilize)
    /// and call Collapse() to make it violently implode and close.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Unstable Portal")]
    public class PVXUnstable : MonoBehaviour
    {
        [Range(0f, 1f)] public float instability = 0.6f;
        [Tooltip("Brightness flicker depth at full instability.")]
        [Range(0f, 1f)] public float flicker = 0.75f;
        [Tooltip("Positional shudder in metres at full instability.")]
        public float shake = 0.06f;
        [Tooltip("Random size glitches at full instability.")]
        [Range(0f, 0.5f)] public float sizeGlitch = 0.12f;
        [Tooltip("Energy surges (teleport flashes) per second at full instability.")]
        public float surgesPerSecond = 0.8f;
        [Tooltip("Seconds the collapse takes before the portal closes.")]
        public float collapseTime = 1.4f;
        [Tooltip("Collapse on its own after being open this long (0 = only when Collapse() is called).")]
        public float collapseAfter = 0f;
        public UnityEvent onSurge = new UnityEvent();
        public UnityEvent onCollapsed = new UnityEvent();

        public bool Collapsing { get; private set; }

        PVXPortal portal;
        Vector3 offset;
        Vector3 scaleMul = Vector3.one, glitch, appliedOffset, appliedScale = Vector3.one;
        float seed, brownout, nextSurge, openTime;

        void Awake()
        {
            portal = GetComponent<PVXPortal>();
            seed = Random.value * 50f;
        }

        void OnDisable()
        {
            RemoveOffsets();
            if (portal != null) portal.IntensityScale = 1f;
        }

        /// <summary>Raise instability to target over the given seconds.</summary>
        public void Destabilize(float target = 1f, float seconds = 3f) => StartCoroutine(Ramp(target, seconds));

        public void Stabilize(float seconds = 1.5f) => StartCoroutine(Ramp(0f, seconds));

        /// <summary>Violent flicker and shudder, then the portal closes.</summary>
        public void Collapse()
        {
            if (!Collapsing) StartCoroutine(CollapseRoutine());
        }

        IEnumerator Ramp(float target, float seconds)
        {
            float from = instability;
            for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                instability = Mathf.Lerp(from, target, k);
                yield return null;
            }
            instability = target;
        }

        IEnumerator CollapseRoutine()
        {
            Collapsing = true;
            float from = instability;
            for (float k = 0f; k < 1f; k += Time.deltaTime / collapseTime)
            {
                instability = Mathf.Lerp(from, 1f, k) + k * 0.6f;
                if (Random.value < Time.deltaTime * 6f) Surge();
                yield return null;
            }
            Surge();
            portal.Close();
            while (portal.State != PVXState.Closed) yield return null;
            instability = from;
            Collapsing = false;
            onCollapsed.Invoke();
        }

        void Surge()
        {
            portal.Teleport();
            brownout = 0f;
            onSurge.Invoke();
        }

        void Update()
        {
            RemoveOffsets();
            if (portal.State == PVXState.Closed) { portal.IntensityScale = 1f; openTime = 0f; return; }
            openTime += Time.deltaTime;
            if (collapseAfter > 0f && openTime > collapseAfter && !Collapsing) Collapse();
            float u = instability, t = Time.time;

            // Flicker: fast noise plus occasional brownouts that dip and recover.
            float n = Mathf.PerlinNoise(t * 14f, seed);
            if (Random.value < Time.deltaTime * 1.5f * u) brownout = 1f;
            brownout = Mathf.MoveTowards(brownout, 0f, Time.deltaTime * 4f);
            portal.IntensityScale = Mathf.Max(0.05f, 1f - u * flicker * (n * 0.6f + brownout * 0.8f) + u * 0.25f * Mathf.Pow(n, 6f) * 4f);

            if (Time.time >= nextSurge)
            {
                if (u > 0.05f && Random.value < 0.6f) Surge();
                nextSurge = Time.time + Random.Range(0.5f, 1.5f) / Mathf.Max(0.05f, surgesPerSecond * u);
            }

            offset = new Vector3(Mathf.PerlinNoise(t * 23f, seed + 1f) - 0.5f, Mathf.PerlinNoise(t * 21f, seed + 2f) - 0.5f, 0f) * 2f * shake * u;
            offset = transform.rotation * offset;
            float g = Random.value < Time.deltaTime * 8f * u ? Random.Range(-sizeGlitch, sizeGlitch) * u : 0f;
            glitch = Vector3.Lerp(glitch, Vector3.zero, Time.deltaTime * 12f) + new Vector3(g, -g * 0.5f, 0f);
            scaleMul = Vector3.one + glitch;
        }

        void LateUpdate()
        {
            RemoveOffsets();
            if (portal.State == PVXState.Closed) return;
            appliedOffset = offset;
            appliedScale = scaleMul;
            transform.position += appliedOffset;
            transform.localScale = Vector3.Scale(transform.localScale, appliedScale);
        }

        /// <summary>Undo last frame's shudder and glitch so the portal's real transform is never drifted.</summary>
        void RemoveOffsets()
        {
            transform.position -= appliedOffset;
            appliedOffset = Vector3.zero;
            var s = transform.localScale;
            transform.localScale = new Vector3(s.x / appliedScale.x, s.y / appliedScale.y, s.z / appliedScale.z);
            appliedScale = Vector3.one;
        }
    }
}
