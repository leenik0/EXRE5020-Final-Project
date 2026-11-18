using System;
using UnityEngine;

namespace PortalVFX
{
    public enum PVXQualityLevel { Low, Medium, High }

    /// <summary>
    /// Scales a portal's cost: particle emission and caps, screen distortion and live portal views.
    /// Each portal follows the global level unless "override" is set. Change all at once with PVXQuality.SetGlobal.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Portal VFX/Quality")]
    public class PVXQuality : MonoBehaviour
    {
        public static PVXQualityLevel Global { get; private set; } = PVXQualityLevel.High;
        public static event Action GlobalChanged;

        public bool overrideGlobal;
        public PVXQualityLevel level = PVXQualityLevel.High;

        [Header("Distance LOD")]
        [Tooltip("Thin out particles and drop distortion as the camera gets far away.")]
        public bool distanceLod = true;
        [Tooltip("Full detail closer than this.")]
        public float lodNear = 14f;
        [Tooltip("Minimum detail (lodMinimum) beyond this.")]
        public float lodFar = 45f;
        [Range(0.05f, 1f)] public float lodMinimum = 0.25f;

        /// <summary>Current distance factor, 1 = full detail.</summary>
        public float LodFactor { get; private set; } = 1f;
        float nextLodCheck;

        public PVXQualityLevel Effective => overrideGlobal ? level : Global;

        /// <summary>Particle emission multiplier for the current level (other scripts that scale emission multiply by this).</summary>
        public float EmissionScale => (Effective == PVXQualityLevel.Low ? 0.4f : Effective == PVXQualityLevel.Medium ? 0.7f : 1f) * LodFactor;

        ParticleSystem[] systems;
        float[] rates;
        int[] caps;
        Renderer[] distortion;
        PVXPortalLink link;

        public static void SetGlobal(PVXQualityLevel l)
        {
            Global = l;
            GlobalChanged?.Invoke();
        }

        void Awake()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
            rates = new float[systems.Length];
            caps = new int[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                rates[i] = systems[i].emission.rateOverTimeMultiplier;
                caps[i] = systems[i].main.maxParticles;
            }
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "PortalVFX/Distortion") list.Add(r);
            distortion = list.ToArray();
            link = GetComponent<PVXPortalLink>();
        }

        void OnEnable()
        {
            GlobalChanged += Apply;
            Apply();
        }

        void OnDisable() => GlobalChanged -= Apply;

        void Update()
        {
            if (!distanceLod || Time.time < nextLodCheck) return;
            nextLodCheck = Time.time + 0.25f + UnityEngine.Random.value * 0.1f;
            var cam = Camera.main;
            if (cam == null) return;
            float d = Vector3.Distance(cam.transform.position, transform.position);
            float k = Mathf.Lerp(1f, lodMinimum, Mathf.InverseLerp(lodNear, Mathf.Max(lodNear + 0.1f, lodFar), d));
            // Step in quarters so Apply() (which touches every system) runs only when the level really changes.
            k = Mathf.Max(lodMinimum, Mathf.Round(k * 4f) / 4f);
            if (Mathf.Approximately(k, LodFactor)) return;
            LodFactor = k;
            Apply();
        }

        void OnValidate()
        {
            if (Application.isPlaying && systems != null) Apply();
        }

        public void Apply()
        {
            if (systems == null) return;
            var q = Effective;
            float emit = EmissionScale;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                var em = systems[i].emission;
                em.rateOverTimeMultiplier = rates[i] * emit;
                var main = systems[i].main;
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(caps[i] * emit));
            }
            bool farAway = LodFactor < 0.6f;
            foreach (var r in distortion) if (r != null) r.gameObject.SetActive(q != PVXQualityLevel.Low && !farAway);
            if (link != null)
            {
                link.resolution = q == PVXQualityLevel.High ? 512 : 256;
                link.liveView = q != PVXQualityLevel.Low && link.liveView;
            }
        }
    }
}
