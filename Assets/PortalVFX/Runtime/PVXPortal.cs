using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PortalVFX
{
    public enum PVXState { Closed, Opening, Open, Closing }

    /// <summary>
    /// Drives one portal through Opening → Idle loop → Teleport → Closing.
    /// Every child PVXElement animates from the shared open progress; every child PVXFxGroup
    /// plays its particle systems when its state starts.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Portal VFX/Portal")]
    public class PVXPortal : MonoBehaviour
    {
        [Header("Quick Settings")]
        [Tooltip("Uniform size of the whole portal (scales the object, particles follow).")]
        [Min(0.05f)] public float size = 1f;
        [Tooltip("Speed of everything: opening, closing, teleport, element motion and particles.")]
        [Min(0f)] public float speed = 1f;
        [Tooltip("Colour multiplier for every part, light included.")]
        [ColorUsage(false, true)] public Color tint = Color.white;
        [Range(-180f, 180f)] public float hueShift;
        [Tooltip("Brightness of every part, light included.")]
        [Min(0f)] public float intensity = 1f;

        [Header("Playback")]
        [Tooltip("Open automatically when the object is enabled.")]
        public bool openOnEnable = true;
        [Min(0.05f)] public float openDuration = 1f;
        [Min(0.05f)] public float closeDuration = 0.6f;
        [Min(0.05f)] public float teleportDuration = 0.6f;

        [Header("Events")]
        public UnityEvent onOpening = new UnityEvent();
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onTeleport = new UnityEvent();
        public UnityEvent onClosing = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        [SerializeField, HideInInspector] float appliedSize = 1f;

        /// <summary>Extra brightness factor for effects layered on top (PVXUnstable flicker). Not serialized.</summary>
        public float IntensityScale { get; set; } = 1f;

        public PVXState State { get; private set; } = PVXState.Closed;
        /// <summary>Linear open progress, 0 = closed, 1 = open.</summary>
        public float OpenAmount { get; private set; }
        /// <summary>Teleport flash envelope, 0..1.</summary>
        public float Pulse { get; private set; }

        static readonly int OpenId = Shader.PropertyToID("_Open");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int HueId = Shader.PropertyToID("_HueShift");
        static readonly int TintId = Shader.PropertyToID("_GlobalTint");
        static readonly int IntensityId = Shader.PropertyToID("_GlobalIntensity");

        struct Slot
        {
            public Renderer renderer;
            public PVXElement element;
        }

        readonly List<Slot> slots = new List<Slot>();
        readonly List<PVXElement> elements = new List<PVXElement>();
        readonly List<PVXFxGroup> groups = new List<PVXFxGroup>();
        readonly List<ParticleSystem> particles = new List<ParticleSystem>();
        readonly List<float> particleSpeeds = new List<float>();
        float appliedSpeed = -1f;
        MaterialPropertyBlock block;
        float pulseTime = -1f;
        bool initialised;

        void Init()
        {
            if (initialised) return;
            initialised = true;
            block = new MaterialPropertyBlock();
            GetComponentsInChildren(true, elements);
            GetComponentsInChildren(true, groups);
            foreach (var e in elements) e.Capture();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                slots.Add(new Slot { renderer = r, element = r.GetComponentInParent<PVXElement>() });
            GetComponentsInChildren(true, particles);
            foreach (var ps in particles) particleSpeeds.Add(ps.main.simulationSpeed);
        }

        void OnValidate()
        {
            // Size scales relative to what was last applied, so a portal scaled by hand keeps its proportions.
            if (!Mathf.Approximately(size, appliedSize) && appliedSize > 0f)
            {
                transform.localScale *= size / appliedSize;
                appliedSize = size;
            }
        }

        void ApplyParticleSpeed()
        {
            if (Mathf.Approximately(appliedSpeed, speed)) return;
            appliedSpeed = speed;
            for (int i = 0; i < particles.Count; i++)
            {
                if (particles[i] == null) continue;
                var main = particles[i].main;
                main.simulationSpeed = particleSpeeds[i] * speed;
            }
        }

        void OnEnable()
        {
            Init();
            SetClosedInstant();
            if (openOnEnable) Open();
        }

        public void Open()
        {
            Init();
            if (State == PVXState.Open || State == PVXState.Opening) return;
            State = PVXState.Opening;
            PlayGroups(PVXFxRole.Open);
            PlayGroups(PVXFxRole.Loop);
            onOpening.Invoke();
        }

        public void Close()
        {
            Init();
            if (State == PVXState.Closed || State == PVXState.Closing) return;
            State = PVXState.Closing;
            StopGroups(PVXFxRole.Loop);
            StopGroups(PVXFxRole.Open);
            PlayGroups(PVXFxRole.Close);
            onClosing.Invoke();
        }

        /// <summary>Flash the portal for the moment something passes through. Works while opening or open.</summary>
        public void Teleport()
        {
            Init();
            if (State == PVXState.Closed || State == PVXState.Closing) return;
            pulseTime = 0f;
            PlayGroups(PVXFxRole.Teleport);
            onTeleport.Invoke();
        }

        public void Toggle()
        {
            if (State == PVXState.Closed || State == PVXState.Closing) Open(); else Close();
        }

        public void SetOpenInstant()
        {
            Init();
            OpenAmount = 1f;
            State = PVXState.Open;
            PlayGroups(PVXFxRole.Loop);
            Apply(0f);
        }

        public void SetClosedInstant()
        {
            Init();
            OpenAmount = 0f;
            Pulse = 0f;
            pulseTime = -1f;
            State = PVXState.Closed;
            foreach (var g in groups) g.Stop(true);
            Apply(0f);
        }

        void Update()
        {
            ApplyParticleSpeed();
            float dt = Time.deltaTime * speed;
            switch (State)
            {
                case PVXState.Opening:
                    OpenAmount += dt / openDuration;
                    if (OpenAmount >= 1f)
                    {
                        OpenAmount = 1f;
                        State = PVXState.Open;
                        onOpened.Invoke();
                    }
                    break;
                case PVXState.Closing:
                    OpenAmount -= dt / closeDuration;
                    if (OpenAmount <= 0f)
                    {
                        OpenAmount = 0f;
                        State = PVXState.Closed;
                        onClosed.Invoke();
                    }
                    break;
            }

            if (pulseTime >= 0f)
            {
                pulseTime += dt / teleportDuration;
                Pulse = pulseTime < 0.15f ? pulseTime / 0.15f : Mathf.Pow(Mathf.Clamp01(1f - (pulseTime - 0.15f) / 0.85f), 2f);
                if (pulseTime >= 1f)
                {
                    pulseTime = -1f;
                    Pulse = 0f;
                }
            }

            Apply(dt);
        }

        void Apply(float dt)
        {
            bool closing = State == PVXState.Closing;
            foreach (var e in elements) e.Evaluate(OpenAmount, closing, Pulse, dt);

            foreach (var s in slots)
            {
                if (s.renderer == null) continue;
                s.renderer.GetPropertyBlock(block);
                float open = s.element != null ? s.element.ShaderOpen : 1f;
                float fill = s.element != null ? s.element.ShaderFill : 1f;
                block.SetFloat(OpenId, open);
                block.SetFloat(FillId, fill);
                block.SetFloat(PulseId, Pulse);
                block.SetFloat(HueId, hueShift);
                block.SetColor(TintId, tint);
                block.SetFloat(IntensityId, intensity * IntensityScale);
                s.renderer.SetPropertyBlock(block);
                // Hidden while fully closed so closed portals cost nothing to draw.
                s.renderer.enabled = OpenAmount > 0f || State != PVXState.Closed || s.element == null;
            }
        }

        void PlayGroups(PVXFxRole role)
        {
            foreach (var g in groups) if (g.role == role) g.Play();
        }

        void StopGroups(PVXFxRole role)
        {
            foreach (var g in groups) if (g.role == role) g.Stop(false);
        }
    }
}
