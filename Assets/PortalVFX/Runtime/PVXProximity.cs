using UnityEngine;

namespace PortalVFX
{
    /// <summary>
    /// Makes a portal react to something approaching: it glows brighter, its idle particles thicken and
    /// orbiting parts spin a little faster. Purely visual; works with any PVXPortal.
    /// </summary>
    [RequireComponent(typeof(PVXPortal))]
    [AddComponentMenu("Portal VFX/Proximity Reaction")]
    public class PVXProximity : MonoBehaviour
    {
        [Tooltip("What the portal reacts to. Defaults to the main camera.")]
        public Transform target;
        public float centerHeight = 2f;
        [Min(0.1f)] public float radius = 5f;
        [Range(0f, 2f)] public float glowBoost = 0.45f;
        [Range(0f, 4f)] public float particleBoost = 1.5f;
        [Min(0.1f)] public float response = 3f;

        public float Excitement { get; private set; }

        PVXPortal portal;
        PVXQuality quality;
        float baseIntensity;
        ParticleSystem[] loops;
        float[] baseRates;

        void Awake()
        {
            portal = GetComponent<PVXPortal>();
            quality = GetComponent<PVXQuality>();
            baseIntensity = portal.intensity;
            var list = new System.Collections.Generic.List<ParticleSystem>();
            foreach (var g in GetComponentsInChildren<PVXFxGroup>(true))
                if (g.role == PVXFxRole.Loop) list.AddRange(g.GetComponentsInChildren<ParticleSystem>(true));
            loops = list.ToArray();
            baseRates = new float[loops.Length];
            for (int i = 0; i < loops.Length; i++) baseRates[i] = loops[i].emission.rateOverTimeMultiplier;
        }

        void Update()
        {
            var t = target != null ? target : (Camera.main != null ? Camera.main.transform : null);
            float goal = 0f;
            if (t != null && portal.State != PVXState.Closed)
            {
                float d = Vector3.Distance(t.position, transform.position + Vector3.up * centerHeight);
                goal = Mathf.Clamp01(1f - d / radius);
                goal = goal * goal * (3f - 2f * goal);
            }
            Excitement = Mathf.MoveTowards(Excitement, goal, Time.deltaTime * response);
            portal.intensity = baseIntensity * (1f + Excitement * glowBoost);
            for (int i = 0; i < loops.Length; i++)
            {
                var em = loops[i].emission;
                em.rateOverTimeMultiplier = baseRates[i] * (1f + Excitement * particleBoost) * (quality != null ? quality.EmissionScale : 1f);
            }
        }

        void OnDisable()
        {
            if (portal != null) portal.intensity = baseIntensity;
        }
    }
}
